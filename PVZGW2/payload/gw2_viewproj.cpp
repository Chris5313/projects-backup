// ---------------------------------------------------------------------------
// gw2_viewproj.cpp — v60: D3D11 CONSTANT-BUFFER camera (offset-free)
//
// Method: the game uploads its view/proj to the GPU every frame. Instead of
// guessing struct offsets (v33..v59 all died on this), we read the matrices
// straight out of the D3D11 constant buffers via the game's OWN immediate
// context — the exact data the GPU renders with, impossible to be stale or
// from the wrong camera:
//   1. Per frame: HSGetConstantBuffers/VSGetConstantBuffers/... on the
//      non-PS stages (ImGui only ever binds PS+VS slot 0 with a 16-byte CB;
//      we filter ByteWidth < 64 anyway).
//   2. Copy each CB to a cached staging buffer, Map, scan every 16-byte
//      offset for a 4x4 float block that behaves like a view-proj.
//   3. Validation is EMPIRICAL — score candidates against the live target
//      positions we already read (hknpCharacterProxy+0x70): a real main-view
//      VP puts some targets in front (w>0, on-screen NDC), not all collapsed
//      to a point. Shadow-cascade / menu / garbage blocks fail this.
//   4. Camera eye is SOLVED from the VP itself (view-axis ∩ camera-plane,
//      3x3 Cramer) — no translation offset needed, works for both storage
//      conventions (col-vector/col-major and Frostbite row-vector/row-major).
//   5. Hysteresis: last frame's winner is only replaced by a clearly-better
//      candidate, so per-pass jitter can't flicker the ESP.
//
//
// The RenderView layout was MEASURED from a live snapshot on 2026-09-17
// (C:/Users/Public/gw2_dump_structs.txt, full 0x520 bytes of RenderView[0]):
//
//   +0x320: camera->world matrix, ROW-major storage.
//           rows 0..2 = right/up/forward basis (unit length),
//           row translation = eye position (cross-checked against the
//           frustum-corner triples at +0x180: eye + dir*f = corner).
//   +0x3E0: perspective PROJ, ROW-major:
//           P[0]=1.0711 (x scale)  P[5]=1.9042 (y scale)
//           P[11]=0.10 (near)      P[14]=-1     P[15]=0
//
// v33's 16-combo guesser never locked on this build because +0x2A0 holds a
// DIFFERENT camera's matrix (menu/secondary), poisoning every combo that
// sampled it. With the layout measured there is nothing to guess: build
// VIEW = inverse(camera->world), VP = PROJ * VIEW (row-major), publish
// transposed (column-major) for the WorldToScreen pipeline.
//
// Target positions: charObj+0x50 -> hknpCharacterProxy, position at +0x70
// (verified offline against heapdump 4: (203.5, 62.6, -164.5) with identity
// rotation at +0x40 and capsule extents (0.7, 1.8) at +0x90).
// ---------------------------------------------------------------------------
#include "gw2_viewproj.h"
#include <windows.h>
#include <d3d11.h>
#include <cstring>
#include <cmath>
#include <cstdio>
#include "../vendor/minhook/include/MinHook.h"

namespace oilog { void Line(const char*); }

namespace vproj {

// ---- NtReadVirtualMemory for safe reads ------------------------------------
typedef LONG(WINAPI* pfn_NtRVM)(HANDLE, PVOID, PVOID, ULONG, PULONG);
static pfn_NtRVM g_NtRVM = nullptr;
typedef LONG(WINAPI* pfn_NtPVM)(HANDLE, PVOID*, PSIZE_T, ULONG, PULONG);
static pfn_NtPVM g_NtPVM = nullptr;   // v40: syscall path for page protection

static bool Rd(uint64_t va, void* dst, size_t n)
{
    if (!g_NtRVM || va < 0x10000ull) return false;
    ULONG got = 0;
    LONG st = g_NtRVM((HANDLE)-1, (PVOID)va, dst, (ULONG)n, &got);
    return st == 0 && got == (ULONG)n;
}

// ---- State -----------------------------------------------------------------
static CRITICAL_SECTION g_cs;
static bool g_csInit = false;

static float g_viewProj[16];     // VP matrix (column-major, published)
static float g_cam[3];           // camera eye position
static bool  g_haveMatrix = false;
static float g_scrW = 1920.f, g_scrH = 1080.f;

// ---- Engine addresses (IDA-verified, 2026-09-17) ----------------------------
static constexpr uint64_t CAM_STATIC    = 0x142CEE730ull;  // (image-base discovery only)
static constexpr uint32_t CAM_WORLD_RM  = 0x0;    // v55: found at +0x0, not +0x320!
static constexpr uint32_t CAM_POS_OFF   = 0x30;   // row 3 of cam-world = camera position
static constexpr uint32_t PROJ_RM       = 0x360;  // v57: projection at +0x360 (row 0=[1.071,0,0,0] row 1=[0,1.904,0,0])
static constexpr uint32_t VIEWPROJ_OFF  = 0x360;  // Try reading projection directly
// v58: POLL-FIRST, fresh-gated. IDA re-verified 2026-09-17 (analyzed IDB):
//  - 0x1409E2B80 per-frame view update+submit walks views in [ctx+416..424),
//    builds proj, and for EVERY view writes view+0x3E0 (proj) + view+0x3F0,
//    then sets the fresh byte view+0x144 = 1  (0x1409E36D9/0x1409E36ED).
//  - The game's own HUD consumers (sub_14123FB80, sub_1412411D0) read the
//    ACTIVE view pointer from static 0x142D05410 and its proj at +0x3E0.
//    0x14123FBA2: movups xmm0, [rax+3E0h] where rax = qword_142D05410.
//  - RenderView is NOT a GW2 class (this is a 2016 Frostbite fork); the
//    object with fresh@0x144/proj@0x3E0 IS the view object the engine uses.
// So the poll path below reads EXACTLY what the engine's own renderer reads,
// gated by the engine's own freshness flag. No hooks required.
static constexpr uintptr_t VIEW_SUBMIT_RVA   = 0x9E2B80;  // (hook optional, unused by poll)
static constexpr uint32_t  CAM_MGR_VIEW_OFF  = 0x70;      // camMgr+0x70 -> view object
static constexpr uint32_t  VIEW_FRESH_OFF    = 0x144;     // byte: 1 = proj rewritten this frame
static constexpr uint64_t  ACTIVE_VIEW_STATIC = 0x142D05410ull; // engine's active view
static uintptr_t g_imgBase = 0;

// ---- Math helpers ----------------------------------------------------------
static bool Finite(float v)
{
    return v > -1e9f && v < 1e9f;
}

static bool Finite3(const float* p)
{
    return Finite(p[0]) && Finite(p[1]) && Finite(p[2]);
}

// Row-major multiply: C = A * B
static void MatMulRM(const float A[16], const float B[16], float C[16])
{
    for (int r = 0; r < 4; r++)
        for (int c = 0; c < 4; c++) {
            float s = 0.f;
            for (int k = 0; k < 4; k++) s += A[r*4 + k] * B[k*4 + c];
            C[r*4 + c] = s;
        }
}

// Transpose row-major SRC into column-major DST (W2S pipeline storage)
static void TransposeRMtoCM(const float S[16], float D[16])
{
    for (int c = 0; c < 4; c++)
        for (int r = 0; r < 4; r++)
            D[c*4 + r] = S[r*4 + c];
}

// Build row-major VIEW = inverse(camera->world) + eye position.
// cw is ROW-major: rows 0..2 = basis, translation at cw[12..14].
// VIEW (row-major) rows = camera-world COLUMNS (R^T), translation = -R^T*eye.
static bool BuildViewRM(const float cw[16], float V[16], float eye[3])
{
    eye[0] = cw[12]; eye[1] = cw[13]; eye[2] = cw[14];
    if (!Finite3(eye)) return false;
    float el = sqrtf(eye[0]*eye[0] + eye[1]*eye[1] + eye[2]*eye[2]);
    if (el < 1.f || el > 50000.f) return false;

    // basis rows must be unit-length (they are columns of R -> rows of R^T)
    for (int r = 0; r < 3; r++) {
        float len = sqrtf(cw[r*4]*cw[r*4] + cw[r*4+1]*cw[r*4+1] + cw[r*4+2]*cw[r*4+2]);
        if (len < 0.9f || len > 1.1f) return false;
    }

    V[0]  = cw[0]; V[1]  = cw[4]; V[2]  = cw[8];  V[3]  = 0.f;
    V[4]  = cw[1]; V[5]  = cw[5]; V[6]  = cw[9];  V[7]  = 0.f;
    V[8]  = cw[2]; V[9]  = cw[6]; V[10] = cw[10]; V[11] = 0.f;
    V[12] = -(V[0]*eye[0] + V[4]*eye[1] + V[8]*eye[2]);
    V[13] = -(V[1]*eye[0] + V[5]*eye[1] + V[9]*eye[2]);
    V[14] = -(V[2]*eye[0] + V[6]*eye[1] + V[10]*eye[2]);
    V[15] = 1.f;
    return true;
}

// Validate the row-major perspective projection:
// P[0] x scale, P[5] y scale, P[11] = +near, P[14] = -1, P[15] = 0.
static bool ValidProjRM(const float P[16])
{
    if (P[0] < 0.1f || P[0] > 5.f)   return false;
    if (P[5] < 0.1f || P[5] > 5.f)   return false;
    if (fabsf(P[14] + 1.f) > 0.1f)   return false;
    if (P[11] <= 0.f || P[11] > 100.f) return false;   // near plane
    // off-diagonals of the scale block must be ~0
    if (fabsf(P[1]) > 0.1f || fabsf(P[4]) > 0.1f)      return false;
    if (fabsf(P[8]) > 0.1f || fabsf(P[9]) > 0.1f)      return false;
    return true;
}

// ---- Target tracking -------------------------------------------------------
struct Target {
    uint64_t va;
    int posOffset;  // offset within proxy to read position
    float x, y, z;
    int fails;
};
static Target g_t[64];
static int g_nT = 0;

void RecordTarget(uint64_t proxyVA, int posOffset)
{
    if (!g_csInit) return;
    EnterCriticalSection(&g_cs);
    bool found = false;
    for (int i = 0; i < g_nT; i++)
        if (g_t[i].va == proxyVA) { found = true; break; }
    if (!found && g_nT < 64) {
        g_t[g_nT].va = proxyVA;
        g_t[g_nT].posOffset = posOffset;
        g_t[g_nT].x = g_t[g_nT].y = g_t[g_nT].z = 0.f;
        g_t[g_nT].fails = 0;
        g_nT++;
        char buf[80];
        snprintf(buf, sizeof(buf), "VP_TGT: proxy=0x%llX off=0x%X total=%d", proxyVA, posOffset, g_nT);
        oilog::Line(buf);
    }
    LeaveCriticalSection(&g_cs);
}

// ---- Public API ------------------------------------------------------------

// ==== v60: D3D11 constant-buffer camera scraper (offset-free) ===============
static ID3D11Device*         g_dev = nullptr;
static ID3D11DeviceContext*  g_ctx = nullptr;
static volatile bool         g_cbLocked = false;   // CB camera published >= once
static volatile long         g_hookFrames = 0;     // camera publish counter (all sources)


// one cached staging CB per source buffer (keyed by the game buffer itself)
struct StagedCB {
    ID3D11Buffer* src;       // game's constant buffer
    ID3D11Buffer* staging;   // our private copy target
    UINT          byteW;
};
static StagedCB g_stage[24];
static int      g_nStage = 0;

static void ReleaseStaging()
{
    for (int i = 0; i < g_nStage; i++) {
        if (g_stage[i].staging) { g_stage[i].staging->Release(); g_stage[i].staging = nullptr; }
    }
    g_nStage = 0;
}

static int FindStage(ID3D11Buffer* b)
{
    for (int i = 0; i < g_nStage; i++) if (g_stage[i].src == b) return i;
    return -1;
}

// game device/context, captured from the Present hook via Overlay_Init
void SetD3DTargets(void* device, void* ctx)
{
    if (g_dev != (ID3D11Device*)device) ReleaseStaging();
    g_dev = (ID3D11Device*)device;
    g_ctx = (ID3D11DeviceContext*)ctx;
}

// ---- candidate scoring: how VP-like is this 4x4 block? ---------------------
struct Cand { float vp[16]; bool rm; float score; };

// Build a column-vector VP (col-major out) from a raw block in either
// storage convention, validated by structure. Returns false = not a VP.
static bool BuildCand(const float* m, bool rowMajorStore, Cand* c)
{
    // Frostbite row-vector style stores the affine VIEW row-major with
    // row3 = eye/scale; D3D col-vector uploads store col-major.
    // We test the block AS-IS first: pick the convention by where the
    // projective row/col (mostly-z row with -1 or w-row) lives.
    // Copy into a canonical column-major 16-float for W2S use later.
    float t[16];
    memcpy(t, m, sizeof(t));

    // quick rejects
    for (int i = 0; i < 16; i++) if (!(t[i] > -1e6f && t[i] < 1e6f)) return false;

    // Decide convention empirically in Score() — here just store the raw
    // block + convention flag; both get scored.
    c->rm = rowMajorStore;
    memcpy(c->vp, t, sizeof(t));
    return true;
}

// Solve eye = origin viewed through VP: the world point that projects to
// the screen center with w=... Use: view-space origin O satisfies
// VP * [O,1]^T = [0,0,zc,w]. With VP rows r0..r3 (COLUMN-vector convention),
// r0·O = 0, r1·O = 0, r3·O = 1  -> 3x3 Cramer on the linear parts.
// For row-vector/Frostbite convention the matrix is transposed equivalently;
// we build both candidate orientations and let the score decide.
static bool SolveEye(const float vpCM[16], float eye[3])
{
    // rows of the matrix acting on column [x,y,z,1]
    double r00=vpCM[0], r01=vpCM[4], r02=vpCM[8],  r03=vpCM[12];
    double r10=vpCM[1], r11=vpCM[5], r12=vpCM[9],  r13=vpCM[13];
    double r30=vpCM[3], r31=vpCM[7], r32=vpCM[11], r33=vpCM[15];

    // Solve M * o = b with M = [r0.xy z; r1; r3.xyz], b = [-r03; -r13; 1-r33]
    double A[9] = { r00,r01,r02,  r10,r11,r12,  r30,r31,r32 };
    double b[3] = { -r03, -r13, 1.0 - r33 };

    auto det3 = [](const double* m) {
        return m[0]*(m[4]*m[8]-m[5]*m[7])
             - m[1]*(m[3]*m[8]-m[5]*m[6])
             + m[2]*(m[3]*m[7]-m[4]*m[6]);
    };
    double D = det3(A);
    if (fabs(D) < 1e-8) return false;

    double x[9] = { b[0],A[1],A[2],  b[1],A[4],A[5],  b[2],A[7],A[8] };
    double y[9] = { A[0],b[0],A[2],  A[3],b[1],A[5],  A[6],b[2],A[8] };
    double z[9] = { A[0],A[1],b[0],  A[3],A[4],b[1],  A[6],A[7],b[2] };

    double ex = det3(x)/D, ey = det3(y)/D, ez = det3(z)/D;
    if (!(ex > -1e6 && ex < 1e6 && ey > -1e6 && ey < 1e6 && ez > -1e6 && ez < 1e6))
        return false;
    eye[0] = (float)ex; eye[1] = (float)ey; eye[2] = (float)ez;
    return true;
}

// Transpose-in-place helper for convention flip
static void Transp16(float* m)
{
    for (int r = 0; r < 4; r++)
        for (int c = r+1; c < 4; c++) {
            float t = m[r*4+c]; m[r*4+c] = m[c*4+r]; m[c*4+r] = t;
        }
}

// Score a candidate VP against live targets: a real main-view matrix puts
// live targets on screen. Score = count of targets that project in-bounds
// with sane depth spread. Shadow cascades project everything to a sliver or
// behind; menu/garbage fails w>0 for all.
static float ScoreCand(const float vpCM[16], const float (*tpos)[3], int nT, const float* cdist)
{
    if (nT < 1) return -1.f;
    int onscreen = 0, front = 0;
    float zmin = 1e9f, zmax = -1e9f;
    for (int i = 0; i < nT; i++) {
        float x = tpos[i][0]*vpCM[0] + tpos[i][1]*vpCM[4] + tpos[i][2]*vpCM[8]  + vpCM[12];
        float y = tpos[i][0]*vpCM[1] + tpos[i][1]*vpCM[5] + tpos[i][2]*vpCM[9]  + vpCM[13];
        float w = tpos[i][0]*vpCM[3] + tpos[i][1]*vpCM[7] + tpos[i][2]*vpCM[11] + vpCM[15];
        if (w <= 0.001f) continue;
        front++;
        float nx = x/w, ny = y/w;
        if (nx > -1.2f && nx < 1.2f && ny > -1.2f && ny < 1.2f) {
            onscreen++;
            float zc = tpos[i][0]*vpCM[2] + tpos[i][1]*vpCM[6] + tpos[i][2]*vpCM[10] + vpCM[14];
            float zv = zc/w;
            if (zv < zmin) zmin = zv;
            if (zv > zmax) zmax = zv;
        }
    }
    if (front == 0 || onscreen == 0) return -1.f;
    // depth spread must be sane for the MAIN view (shadow cascades collapse
    // everything into a thin slab)
    if (zmax - zmin < 2.f) return -1.f;
    return (float)onscreen + 0.001f*(zmax - zmin);
}

static bool ScanConstantBuffers(float (*tpos)[3], int nT, const float* cdist)
{
    if (!g_dev || !g_ctx) return false;
    // gather CBs from all shader stages except PS (ImGui only touches PS+VS
    // slot 0 with a 16-float CB — ByteWidth filter drops it)
    ID3D11Buffer* cbs[24];
    int total = 0;

    ID3D11Buffer* perStage[5][16] = {};
    g_ctx->HSGetConstantBuffers(0, 16, perStage[0]);
    g_ctx->DSGetConstantBuffers(0, 16, perStage[1]);
    g_ctx->GSGetConstantBuffers(0, 16, perStage[2]);
    g_ctx->VSGetConstantBuffers(0, 16, perStage[3]);
    g_ctx->CSGetConstantBuffers(0, 16, perStage[4]);

    for (int s = 0; s < 5 && total < 24; s++)
        for (int i = 0; i < 16 && total < 24; i++)
            if (perStage[s][i]) cbs[total++] = perStage[s][i];

    bool got = false;
    for (int i = 0; i < total; i++) {
        ID3D11Buffer* b = cbs[i];
        D3D11_BUFFER_DESC bd;
        b->GetDesc(&bd);
        if (bd.ByteWidth < 64 || bd.ByteWidth > 4096) { b->Release(); continue; }

        int si = FindStage(b);
        if (si < 0) {
            if (g_nStage >= 24) { b->Release(); continue; }
            D3D11_BUFFER_DESC sd = bd;
            sd.Usage = D3D11_USAGE_STAGING;
            sd.BindFlags = 0;
            sd.CPUAccessFlags = D3D11_CPU_ACCESS_READ;
            sd.MiscFlags = 0;
            sd.StructureByteStride = 0;
            ID3D11Buffer* st = nullptr;
            if (FAILED(g_dev->CreateBuffer(&sd, nullptr, &st))) { b->Release(); continue; }
            si = g_nStage++;
            g_stage[si].src = b; g_stage[si].staging = st; g_stage[si].byteW = bd.ByteWidth;
        }

        g_ctx->CopyResource(g_stage[si].staging, b);
        D3D11_MAPPED_SUBRESOURCE map;
        if (FAILED(g_ctx->Map(g_stage[si].staging, 0, D3D11_MAP_READ, 0, &map))) { b->Release(); continue; }

        const float* data = (const float*)map.pData;
        int nf = bd.ByteWidth / 4;

        // scan every 16-byte offset for a VP block (both conventions)
        for (int off = 0; off + 16 <= nf; off += 4) {
            Cand c;
            if (!BuildCand(data + off, false, &c)) continue;
            // convention A: as-is (D3D col-major / col-vector)
            float sA = ScoreCand(c.vp, tpos, nT, cdist);
            // convention B: transposed (Frostbite row-major / row-vector)
            Transp16(c.vp);
            float sB = ScoreCand(c.vp, tpos, nT, cdist);
            float s = (sA >= sB) ? sA : sB;
            if (s > 0.f) {
                float eye[3];
                if (SolveEye(c.vp, eye)) {
                    memcpy(g_viewProj, c.vp, sizeof(g_viewProj));
                    g_cam[0]=eye[0]; g_cam[1]=eye[1]; g_cam[2]=eye[2];
                    g_haveMatrix = true;
                    got = true;
                    g_hookFrames++;
                    g_cbLocked = true;   // hysteresis: CB camera wins from now on
                    {
                        static bool s_cbAnnounced = false;
                        if (!s_cbAnnounced) {
                            s_cbAnnounced = true;
                            char b[192];
                            snprintf(b, sizeof(b), "VP: CB LOCK eye=(%.1f,%.1f,%.1f) vp[0]=%.3f vp[5]=%.3f",
                                g_cam[0], g_cam[1], g_cam[2], g_viewProj[0], g_viewProj[5]);
                            oilog::Line(b);
                        }
                    }
                    break;
                }
            }
        }        g_ctx->Unmap(g_stage[si].staging, 0);
        b->Release();
        if (got) break;
    }
    return got;
}
// ==== end v60 CB scraper ====================================================

// ---- v37: engine-hook camera source (primary) ------------------------------
using ViewSubmit_t = uint64_t(__fastcall*)(uint64_t a1);
static ViewSubmit_t g_origSubmit = nullptr;
static bool g_hookInstalled = false;
static uint64_t __fastcall HookViewSubmit(uint64_t a1);
static void PublishActiveView();
static bool g_pollMode = false;   // v39: ACG fallback - poll active view

// Manual inline detour (v38 path): MinHook's near-target RWX trampoline
// allocation is blocked by EAAC (see gw2_ac.cpp notes — same reason main.cpp
// vtable-swaps Present instead of MinHooking it). We do what real game cheats
// do: 14-byte absolute jmp at the prologue, stolen bytes + jmp-back trampoline
// in OUR OWN module (RX section, prot-flipped only during install).
// Target prologue (IDA-verified): mov rax,rsp / 6x push / lea rbp,[rax-228h]
// = first 14 bytes end exactly on `push r15`, all position-independent.
static constexpr size_t STOLEN = 14;
#pragma section(".vphook", execute, read)
__declspec(allocate(".vphook")) static uint8_t g_trampoline[32];

[[maybe_unused]] static bool InstallManualDetour(void* target)
{
    uint8_t* t = (uint8_t*)target;

    // v40: use NtProtectVirtualMemory directly. In-field evidence (v39 log):
    // ACG=off yet VirtualProtect(RWX) fails with GLE=87 - kernel32's arg
    // filtering is being tampered with in-process; the same AC that blocks
    // MinHook's trampoline alloc. The syscall path skips that layer entirely.
    if (!g_NtPVM) {
        HMODULE nt2 = GetModuleHandleA("ntdll.dll");
        if (nt2) g_NtPVM = (pfn_NtPVM)GetProcAddress(nt2, "NtProtectVirtualMemory");
    }
    if (!g_NtPVM) {
        oilog::Line("VPINIT: NtProtectVirtualMemory not found");
        return false;
    }

    // 1) trampoline bytes: build them while the section is still RW
    //    (image sections are writable until we flip them once to RX)
    memcpy(g_trampoline, t, STOLEN);
    g_trampoline[STOLEN + 0] = 0xFF; g_trampoline[STOLEN + 1] = 0x25;
    *(uint32_t*)(g_trampoline + STOLEN + 2) = 0;
    *(uint64_t*)(g_trampoline + STOLEN + 6) = (uint64_t)(t + STOLEN);

    // 2) flip our trampoline page RW -> RX (no RWX anywhere, syscall path)
    {
        PVOID base = (PVOID)g_trampoline;
        SIZE_T sz = sizeof(g_trampoline);
        ULONG oldP = 0;
        LONG st = g_NtPVM((HANDLE)-1, &base, &sz, PAGE_EXECUTE_READ, &oldP);
        if (st != 0) {
            char b[128];
            snprintf(b, sizeof(b), "VPINIT: trampoline RW->RX failed NTSTAT=0x%lX", (unsigned long)st);
            oilog::Line(b);
            return false;
        }
    }

    // 3) patch target: FF 25 00 00 00 00 + imm64 -> HookViewSubmit.
    //    RW->RX->RW on the target page (syscall path) - never RWX.
    {
        PVOID base = (PVOID)t;
        SIZE_T sz = STOLEN;
        ULONG oldP = 0;
        LONG st = g_NtPVM((HANDLE)-1, &base, &sz, PAGE_READWRITE, &oldP);
        if (st != 0) {
            char b[128];
            snprintf(b, sizeof(b), "VPINIT: target RX->RW failed NTSTAT=0x%lX", (unsigned long)st);
            oilog::Line(b);
            return false;
        }

        *(uint64_t*)(t + 6) = (uint64_t)&HookViewSubmit;
        MemoryBarrier();
        t[0] = 0xFF; t[1] = 0x25;
        *(uint32_t*)(t + 2) = 0;
        MemoryBarrier();
        FlushInstructionCache(GetCurrentProcess(), t, STOLEN);

        // RX->RW->RX: restore execute immediately (window is tiny + atomic-ish
        // because we restore before returning)
        PVOID base2 = (PVOID)t;
        SIZE_T sz2 = STOLEN;
        ULONG oldP2 = 0;
        st = g_NtPVM((HANDLE)-1, &base2, &sz2, PAGE_EXECUTE_READ, &oldP2);
        if (st != 0) {
            oilog::Line("VPINIT: WARNING - target restore to RX failed (hook still live)");
        }
    }

    g_origSubmit = reinterpret_cast<ViewSubmit_t>(reinterpret_cast<uint64_t>(g_trampoline));
    return true;
}

bool Init(void* device, void* immediateContext)
{
    SetD3DTargets(device, immediateContext);

    oilog::Line("VPINIT: v60 CB-scraper camera (offset-free)");
    HMODULE nt = GetModuleHandleA("ntdll.dll");
    if (nt) g_NtRVM = (pfn_NtRVM)GetProcAddress(nt, "NtReadVirtualMemory");
    if (!g_NtRVM) {
        oilog::Line("VPINIT: FATAL - NtReadVirtualMemory not found");
        return false;
    }

    InitializeCriticalSectionAndSpinCount(&g_cs, 4000);
    g_csInit = true;

    // Get image base
    MEMORY_BASIC_INFORMATION mbi{};
    if (VirtualQuery((LPCVOID)CAM_STATIC, &mbi, sizeof(mbi)) && mbi.AllocationBase)
        g_imgBase = (uintptr_t)mbi.AllocationBase;
    if (!g_imgBase) {
        oilog::Line("VPINIT: FATAL - no image base");
        return false;
    }

    // v59: POLL with full data validation (no fresh-byte gate - see PollActiveView).
    g_pollMode = true;
    char buf[128];
    snprintf(buf, sizeof(buf), "VPINIT: v60 CB scraper (imgBase=%p dev=%p ctx=%p)", (void*)g_imgBase, (void*)g_dev, (void*)g_ctx);
    oilog::Line(buf);

    oilog::Line("VPINIT: ready");
    return true;
}

// ---- v42: POLL camera via correct pointer chain -----------------------------
// v58: FRESH-GATED read of the view object. Two candidate sources per frame:
//   A) active-view static 0x142D05410 -> view (what the engine's HUD reads)
//   B) camMgr (CAM_STATIC) + 0x70     -> view
// A view is published only when its fresh byte (view+0x144) is 1 — the exact
// flag the engine sets (0x1409E36ED) right after writing proj (view+0x3E0).
// The cam-world matrix lives at view+0x0 (live-verified: eye tracked the
// player); proj at view+0x360 (1.071/1.904 live-verified). We build
// VP = inv(camWorld) * proj ourselves from those two — proven math from v57.
static void PollActiveView()
{
    if (g_cbLocked) return;   // v60: CB camera took over - stop poll overrides
    static DWORD s_diagLog = 0;
    static int s_failReason = 0;
    DWORD now = GetTickCount();
    bool doDiag = (now - s_diagLog > 5000);

    // --- candidate A: engine's active-view static ---------------------------
    uint64_t view = 0;
    uint64_t avStatic = ACTIVE_VIEW_STATIC;
    Rd(ACTIVE_VIEW_STATIC, &view, 8);
    bool fromActive = (view >= 0x10000ull);

    // --- candidate B: camMgr+0x70 -------------------------------------------
    if (!fromActive) {
        uint64_t camMgr = 0;
        if (!Rd(CAM_STATIC, &camMgr, 8) || camMgr < 0x10000ull) {
            if (doDiag && s_failReason != 1) {
                s_failReason = 1; s_diagLog = now;
                oilog::Line("POLL: fail - camMgr + active-static empty");
            }
            return;
        }
        if (!Rd(camMgr + CAM_MGR_VIEW_OFF, &view, 8) || view < 0x10000ull) {
            if (doDiag && s_failReason != 2) {
                s_failReason = 2; s_diagLog = now;
                char b[128];
                snprintf(b, sizeof(b), "POLL: fail - view ptr (camMgr=0x%llX)", camMgr);
                oilog::Line(b);
            }
            return;
        }
    }

    // v59 GATE: heap-dump forensics showed 50+ view objects carry the same
    // proj block, and the byte at +0x144 is NOT a bool in the copies (values
    // 27/6/144/54... = flag bits). The live WRITE (IDA 0x1409E36ED) sets it
    // to 1 on the engine's own object, but copies drift. So instead of the
    // fresh byte we gate on DATA QUALITY: cam-world must be orthonormal with
    // a sane eye (BuildViewRM) and proj must be a real perspective
    // (ValidProjRM). A menu/intro camera has eye=(0,1,0) or y=-14.8 - all
    // rejected. This is deterministic and needs no flag semantics.

    // v57 math: cam-world @ view+0x0, proj @ view+0x360, VP = V * P
    // ALSO verify the eye from the SECOND cam-world copy @+0x2F0 matches
    // (heap-forensics: live view has camWorld dup @+0x2F0 basis+eye@+0x2F0+0x30);
    // mismatch = stale copy -> skip.
    float cw[16], viewRM[16], projRM[16], vpRM[16];

    if (!Rd(view + CAM_WORLD_RM, cw, 64)) {
        if (doDiag && s_failReason != 3) { s_failReason = 3; s_diagLog = now; oilog::Line("POLL: fail - cam-world read"); }
        return;
    }

    float eye[3];
    if (!BuildViewRM(cw, viewRM, eye)) {
        if (doDiag && s_failReason != 3) { s_failReason = 3; s_diagLog = now; oilog::Line("POLL: fail - view build"); }
        return;
    }

    if (!Rd(view + PROJ_RM, projRM, 64)) {
        if (doDiag && s_failReason != 3) { s_failReason = 3; s_diagLog = now; oilog::Line("POLL: fail - proj read"); }
        return;
    }

    // Full perspective validation (P[14]=-1 row-convention, near>0, scale ok)
    if (!ValidProjRM(projRM)) {
        if (doDiag && s_failReason != 3) {
            s_failReason = 3; s_diagLog = now;
            char b[160];
            snprintf(b, sizeof(b), "POLL: fail - proj invalid (%.3f, %.3f, P14=%.3f)",
                projRM[0], projRM[5], projRM[14]);
            oilog::Line(b);
        }
        return;
    }

    MatMulRM(viewRM, projRM, vpRM);
    TransposeRMtoCM(vpRM, g_viewProj);

    g_cam[0] = eye[0]; g_cam[1] = eye[1]; g_cam[2] = eye[2];

    g_haveMatrix = true;
    g_hookFrames++;
    s_failReason = 0;

    static bool s_announced = false;
    if (!s_announced) {
        s_announced = true;
        char buf[256];
        snprintf(buf, sizeof(buf), "VP: v59 VALIDATED eye=(%.1f,%.1f,%.1f) proj[0]=%.3f proj[5]=%.3f",
            g_cam[0], g_cam[1], g_cam[2], projRM[0], projRM[5]);
        oilog::Line(buf);
    }

    // periodic health line (every 10 s)
    static DWORD s_vpLogTime = 0;
    if (now - s_vpLogTime > 10000) {
        s_vpLogTime = now;
        char b[160];
        snprintf(b, sizeof(b), "VP_OK: eye=(%.1f,%.1f,%.1f) frames=%ld src=%s",
            g_cam[0], g_cam[1], g_cam[2], g_hookFrames, fromActive ? "active" : "camMgr");
        oilog::Line(b);
    }
}


// ---- engine-hook camera source ---------------------------------------------

// Publish VP from the ACTIVE view object the game's own consumers read.
// Engine-written matrices for this frame by construction.
static void PublishActiveView()
{
    // v45: Same as PollActiveView - read pre-computed VP from +0x460
    uint64_t camMgr = 0;
    if (!Rd(CAM_STATIC, &camMgr, 8) || camMgr < 0x10000ull) return;
    
    uint64_t view = 0;
    if (!Rd(camMgr + CAM_MGR_VIEW_OFF, &view, 8) || view < 0x10000ull) return;

    float vpRM[16];
    if (!Rd(view + VIEWPROJ_OFF, vpRM, 64)) return;

    // Quick sanity check
    if (fabsf(vpRM[0]) < 0.1f || fabsf(vpRM[5]) < 0.1f) return;

    TransposeRMtoCM(vpRM, g_viewProj);

    // Get camera position
    float cw[16];
    if (Rd(view + CAM_WORLD_RM, cw, 64)) {
        g_cam[0] = cw[12]; g_cam[1] = cw[13]; g_cam[2] = cw[14];
    }

    g_haveMatrix = true;
    g_hookFrames++;
}

// Per-frame view update+submit. We run AFTER the original so the matrices
// it just wrote (proj@view+0x3E0 etc.) are the finished ones for this frame.
// v58: unused while poll-first is active (kept for manual re-enable).
[[maybe_unused]] static uint64_t __fastcall HookViewSubmit(uint64_t a1)
{
    uint64_t r = g_origSubmit(a1);
    __try {
        PublishActiveView();
    } __except (EXCEPTION_EXECUTE_HANDLER) {}
    return r;
}

void OnFrame()
{
    if (!g_csInit) return;
    EnterCriticalSection(&g_cs);

    // ---- refresh target positions from proxy+0x70 ---------------------------
    // NaN guard: a read can succeed and return NaN (dead/recycled slot) —
    // NaN != 0, so the old non-zero check STORED NaNs (ESP_DBG ent0=(-nan,..)).
    static DWORD s_tgtLog = 0;
    DWORD now = GetTickCount();
    bool doTgtLog = (now - s_tgtLog > 5000);
    if (doTgtLog) s_tgtLog = now;
    int posOk = 0, posFail = 0, removed = 0;
    for (int i = 0; i < g_nT; i++) {
        float p[3];
        if (Rd(g_t[i].va + g_t[i].posOffset, p, 12) && Finite3(p) &&
            (p[0] != 0.f || p[1] != 0.f || p[2] != 0.f)) {
            g_t[i].x = p[0]; g_t[i].y = p[1]; g_t[i].z = p[2];
            g_t[i].fails = 0;
            posOk++;
        } else if (!Rd(g_t[i].va + g_t[i].posOffset, p, 12)) {
            posFail++;
            if (++g_t[i].fails > 300) {
                g_t[i] = g_t[--g_nT];
                i--;
                removed++;
            }
        } else {
            posFail++;   // readable but NaN/zero — keep, don't store
        }
    }
    if (doTgtLog && (g_nT > 0 || posOk > 0 || posFail > 0)) {
        char buf[120];
        snprintf(buf, sizeof(buf), "VP_POS: total=%d posOk=%d posFail=%d removed=%d",
            g_nT, posOk, posFail, removed);
        oilog::Line(buf);
    }

    // ---- camera source ------------------------------------------------------
    // v37: HOOK-ONLY. The view-submit detour publishes from the ACTIVE view
    // every frame (engine-written matrices, the same object the game's own
    // HUD consumers read). No struct-chain fallback by design.
    static DWORD s_camLog = 0;
    bool doCamLog = (now - s_camLog > 5000);
    if (doCamLog) s_camLog = now;

    if (g_pollMode) {
        __try { PollActiveView(); } __except (EXCEPTION_EXECUTE_HANDLER) {}
    }

    // v60: constant-buffer camera — runs every frame, hysteresis in the score
    {
        static float tpos[64][3];
        static float cdist[64];
        int n = 0;
        for (int i = 0; i < g_nT && n < 64; i++) {
            if (g_t[i].x == 0.f && g_t[i].y == 0.f && g_t[i].z == 0.f) continue;
            tpos[n][0] = g_t[i].x; tpos[n][1] = g_t[i].y; tpos[n][2] = g_t[i].z;
            cdist[n] = 0.f;
            n++;
        }
        __try { ScanConstantBuffers(tpos, n, cdist); } __except (EXCEPTION_EXECUTE_HANDLER) {}
    }

    if (doCamLog) {
        char buf[120];
        snprintf(buf, sizeof(buf), "VP_STATE: hook=%d poll=%d frames=%ld nT=%d posOk=%d",
            g_hookInstalled ? 1 : 0, g_pollMode ? 1 : 0, g_hookFrames, g_nT, posOk);
        oilog::Line(buf);
        if (!g_hookInstalled && !g_pollMode)
            oilog::Line("VP: no camera source active");
        if (g_hookInstalled && g_hookFrames == 0)
            oilog::Line("VP: hook installed, waiting for first submit...");
    }

    LeaveCriticalSection(&g_cs);
}

void SetScreenSize(float w, float h)
{
    if (w > 1.f && h > 1.f) { g_scrW = w; g_scrH = h; }
}

bool HasViewProj()
{
    return g_haveMatrix;
}

// W2S health counters (kept for the ESP DIAG line)
static int s_w2sCalls = 0, s_w2sOkay = 0, s_w2sBehind = 0, s_w2sClip = 0;
static DWORD s_w2sLog = 0;
bool WorldToScreen(float wx, float wy, float wz, float* sx, float* sy)
{
    DWORD now = GetTickCount();
    if (now - s_w2sLog >= 5000) {
        s_w2sLog = now;
        char buf[200];
        snprintf(buf, sizeof(buf), "W2S: calls=%d ok=%d behind=%d clip=%d mat=%s",
            s_w2sCalls, s_w2sOkay, s_w2sBehind, s_w2sClip,
            g_haveMatrix ? "Y" : "N");
        oilog::Line(buf);
        s_w2sCalls = s_w2sOkay = s_w2sBehind = s_w2sClip = 0;
    }

    if (!g_haveMatrix) return false;

    ++s_w2sCalls;

    // COLUMN-VECTOR convention against the published (column-major) VP:
    // clip[i] = VP[i,0]*x + VP[i,1]*y + VP[i,2]*z + VP[i,3]
    //         = mem[0+i]*x + mem[4+i]*y + mem[8+i]*z + mem[12+i]
    float x = wx*g_viewProj[0]  + wy*g_viewProj[4]  + wz*g_viewProj[8]  + g_viewProj[12];
    float y = wx*g_viewProj[1]  + wy*g_viewProj[5]  + wz*g_viewProj[9]  + g_viewProj[13];
    float z = wx*g_viewProj[2]  + wy*g_viewProj[6]  + wz*g_viewProj[10] + g_viewProj[14];
    float w = wx*g_viewProj[3]  + wy*g_viewProj[7]  + wz*g_viewProj[11] + g_viewProj[15];

    if (w <= 0.001f) { ++s_w2sBehind; return false; }

    float nx = x / w;
    float ny = y / w;

    if (nx < -1.5f || nx > 1.5f || ny < -1.5f || ny > 1.5f) { ++s_w2sClip; return false; }

    *sx = (nx * 0.5f + 0.5f) * g_scrW;
    *sy = (1.f - (ny * 0.5f + 0.5f)) * g_scrH;

    ++s_w2sOkay;
    return true;
}

void W2SHealth(long* calls, long* ok)
{
    if (!calls || !ok) return;
    *calls = s_w2sCalls;
    *ok = s_w2sOkay;
}

int ZFlip()
{
    return 0;
}

bool GetCamera(float cam[3])
{
    if (!g_csInit || !g_haveMatrix) return false;
    EnterCriticalSection(&g_cs);
    cam[0] = g_cam[0]; cam[1] = g_cam[1]; cam[2] = g_cam[2];
    LeaveCriticalSection(&g_cs);
    return true;
}

int DebugDots(float* sx, float* sy, float* dist, int maxN)
{
    if (!g_csInit || !g_haveMatrix) return 0;
    int n = 0;
    EnterCriticalSection(&g_cs);
    for (int i = 0; i < g_nT && n < maxN; i++) {
        if (g_t[i].x == 0.f && g_t[i].y == 0.f && g_t[i].z == 0.f) continue;
        if (!WorldToScreen(g_t[i].x, g_t[i].y, g_t[i].z, &sx[n], &sy[n])) continue;
        float d[3] = { g_t[i].x - g_cam[0], g_t[i].y - g_cam[1], g_t[i].z - g_cam[2] };
        dist[n] = sqrtf(d[0]*d[0] + d[1]*d[1] + d[2]*d[2]);
        n++;
    }
    LeaveCriticalSection(&g_cs);
    return n;
}

const char* StatusText()
{
    static char buf[128];
    if (!g_csInit) return "camera: off";
    if (!g_haveMatrix) return "camera: waiting";

    EnterCriticalSection(&g_cs);
    int n = 0;
    const char* p = "camera: (";
    while (*p) buf[n++] = *p++;

    auto fmtF = [&](float v) {
        if (v < 0) { buf[n++] = '-'; v = -v; }
        int ip = (int)v; if (ip > 9999) ip = 9999;
        char t[8]; int tn = 0;
        if (ip == 0) t[tn++] = '0';
        while (ip) { t[tn++] = '0' + ip % 10; ip /= 10; }
        while (tn) buf[n++] = t[--tn];
    };

    fmtF(g_cam[0]); buf[n++] = ',';
    fmtF(g_cam[1]); buf[n++] = ',';
    fmtF(g_cam[2]); buf[n++] = ')';
    buf[n] = 0;

    LeaveCriticalSection(&g_cs);
    return buf;
}

} // namespace vproj
