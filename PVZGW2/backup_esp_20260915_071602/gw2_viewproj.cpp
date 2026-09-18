// ---------------------------------------------------------------------------
// gw2_viewproj.cpp — Direct memory read ESP (v33: measured view resolver)
//
// Reads camera and projection directly from RenderView structure:
//   +0x2A0 / +0x320: candidate camera-world matrices (BOTH sampled live)
//   +0x3E0: PROJ matrix (perspective)
//
// v33 stops ASSERTING the layout and MEASURES it: 16 candidate readings
// (slot x row/col x proj-orient x z-sign) each build a VP, and the combo
// that projects the most live entities on-screen wins and locks. A wrong
// frozen-dump layout cannot win — only the reading that matches real
// gameplay geometry does.
//
// Computes VIEW = inverse(camera world) then VP = PROJ * VIEW
// Math: clip = VP * pos (column-vector convention)
// ---------------------------------------------------------------------------
#include "gw2_viewproj.h"
#include <windows.h>
#include <cstring>
#include <cmath>
#include <cstdio>

namespace oilog { void Line(const char*); }

namespace vproj {

// ---- NtReadVirtualMemory for safe reads ------------------------------------
typedef LONG(WINAPI* pfn_NtRVM)(HANDLE, PVOID, PVOID, ULONG, PULONG);
static pfn_NtRVM g_NtRVM = nullptr;

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

static float g_viewProj[16];     // VP matrix (column-major)
static float g_cam[3];           // camera eye position
static bool  g_haveMatrix = false;
static float g_scrW = 1920.f, g_scrH = 1080.f;

// ---- Engine addresses (dump-verified 2026-09-11) ---------------------------
static constexpr uint64_t CAM_STATIC    = 0x142CEE730ull;
static constexpr uint32_t MGR_VIEWS_OFF = 0x68;
static constexpr uint32_t VIEW_STRIDE   = 0x520;
static constexpr uint32_t CAM_WORLD_OFF     = 0x2A0;  // candidate camera-world slot A
static constexpr uint32_t CAM_WORLD_OFF_ALT = 0x320;  // candidate camera-world slot B
static constexpr uint32_t PROJ_OFF          = 0x3E0;  // PROJ matrix (perspective)
// ---- Math helpers ----------------------------------------------------------
static float Dot3(const float* a, const float* b)
{
    return a[0]*b[0] + a[1]*b[1] + a[2]*b[2];
}

static float Len3(const float* v)
{
    return sqrtf(Dot3(v, v));
}

static bool Finite(float v)
{
    return v > -1e9f && v < 1e9f;
}

// Column-major matrix multiplication: C = A * B
// Memory layout: mem[col*4 + row] = M[row, col]
// Result: C[i,j] = sum_k A[i,k] * B[k,j]
static void MatMulColMajor(const float A[16], const float B[16], float C[16])
{
    for (int col = 0; col < 4; col++) {
        for (int row = 0; row < 4; row++) {
            float s = 0.f;
            for (int k = 0; k < 4; k++) {
                // A[row,k] = A[k*4 + row], B[k,col] = B[col*4 + k]
                s += A[k*4 + row] * B[col*4 + k];
            }
            C[col*4 + row] = s;
        }
    }
}

// Build VIEW from camera-world matrix (inverse of camera world)
// Camera-world is column-major: columns are right/up/fwd, column 3 is eye position
// VIEW = inverse = [R^T | -R^T*eye]
static bool BuildViewFromCamWorld(const float cw[16], float V[16], float eye[3])
{
    // Column-major: cw[col*4 + row] = M[row, col]
    // Column 0 = right, Column 1 = up, Column 2 = forward, Column 3 = eye
    eye[0] = cw[12]; eye[1] = cw[13]; eye[2] = cw[14];
    
    // Validate eye position
    if (!Finite(eye[0]) || !Finite(eye[1]) || !Finite(eye[2])) return false;
    float eyeLen = sqrtf(eye[0]*eye[0] + eye[1]*eye[1] + eye[2]*eye[2]);
    if (eyeLen < 1.f || eyeLen > 50000.f) return false;
    
    // Check rotation columns are unit length
    for (int col = 0; col < 3; col++) {
        float len = sqrtf(cw[col*4]*cw[col*4] + cw[col*4+1]*cw[col*4+1] + cw[col*4+2]*cw[col*4+2]);
        if (len < 0.9f || len > 1.1f) return false;
    }
    
    // Build VIEW = [R^T | -R^T*eye] in column-major
    // R^T means transpose: VIEW column i = camera-world row i (of rotation part)
    // Column 0 of VIEW = row 0 of R = [cw[0], cw[4], cw[8]]
    V[0] = cw[0];  V[1] = cw[4];  V[2] = cw[8];   V[3] = 0;
    V[4] = cw[1];  V[5] = cw[5];  V[6] = cw[9];   V[7] = 0;
    V[8] = cw[2];  V[9] = cw[6];  V[10] = cw[10]; V[11] = 0;
    
    // Translation = -R^T * eye
    V[12] = -(V[0]*eye[0] + V[4]*eye[1] + V[8]*eye[2]);
    V[13] = -(V[1]*eye[0] + V[5]*eye[1] + V[9]*eye[2]);
    V[14] = -(V[2]*eye[0] + V[6]*eye[1] + V[10]*eye[2]);
    V[15] = 1.f;
    
    return true;
}

// Validate VIEW matrix (column-major)
// VIEW[col*4 + row] = M[row, col]
// Should have: rotation in [0..2,0..2], translation in [0..2,3], [3,*]=(0,0,0,1)
static bool ValidView(const float V[16])
{
    // Column 3 (translation) should have reasonable values
    float tx = V[12], ty = V[13], tz = V[14], tw = V[15];
    if (!Finite(tx) || !Finite(ty) || !Finite(tz)) return false;
    if (fabsf(tw - 1.f) > 0.1f) return false;
    
    // Translation should be reasonable
    float tLen = sqrtf(tx*tx + ty*ty + tz*tz);
    if (tLen < 1.f || tLen > 50000.f) return false;
    
    // Row 3 (bottom row) should be (0,0,0,1)
    // In column-major: [3], [7], [11], [15] = row 3
    if (fabsf(V[3]) > 0.01f || fabsf(V[7]) > 0.01f || fabsf(V[11]) > 0.01f)
        return false;
    
    // Check rotation columns are unit length
    for (int col = 0; col < 3; col++) {
        float len = sqrtf(V[col*4]*V[col*4] + V[col*4+1]*V[col*4+1] + V[col*4+2]*V[col*4+2]);
        if (len < 0.9f || len > 1.1f) return false;
    }
    
    return true;
}

// Validate projection matrix (column-major)
// Column-major layout: mem[col*4 + row] = M[row,col]
// Standard perspective: P[0,0]=xscale, P[1,1]=yscale, P[2,3]=-1 or 1, P[3,2]=near
static bool ValidProj(const float P[16])
{
    // P[0] = P[0,0] = x scale (column 0, row 0)
    // P[5] = P[1,1] = y scale (column 1, row 1)
    if (P[0] < 0.1f || P[0] > 5.f) return false;
    if (P[5] < 0.1f || P[5] > 5.f) return false;
    
    // P[14] = P[2,3] = perspective divide factor (-1 or 1)
    // (column 3, row 2)
    if (fabsf(fabsf(P[14]) - 1.f) > 0.1f) return false;
    
    // Off-diagonal elements in columns 0,1 should be ~0
    if (fabsf(P[1]) > 0.1f || fabsf(P[2]) > 0.1f) return false;  // col 0
    if (fabsf(P[4]) > 0.1f || fabsf(P[6]) > 0.1f) return false;  // col 1
    
    return true;
}

// ---- Logging ---------------------------------------------------------------
static void LogStatus(const char* prefix, float x, float y, float z)
{
    char buf[160];
    int n = 0;
    while (*prefix && n < 100) buf[n++] = *prefix++;
    buf[n++] = '(';
    
    auto fmtF = [&](float v) {
        if (v < 0) { buf[n++] = '-'; v = -v; }
        int ip = (int)v;
        int fp = (int)((v - ip) * 100);
        if (ip == 0) { buf[n++] = '0'; }
        else {
            char t[12]; int tn = 0;
            while (ip) { t[tn++] = '0' + ip % 10; ip /= 10; }
            while (tn) buf[n++] = t[--tn];
        }
        buf[n++] = '.';
        buf[n++] = '0' + fp / 10;
        buf[n++] = '0' + fp % 10;
    };
    
    fmtF(x); buf[n++] = ','; buf[n++] = ' ';
    fmtF(y); buf[n++] = ','; buf[n++] = ' ';
    fmtF(z); buf[n++] = ')';
    buf[n] = 0;
    oilog::Line(buf);
}

// ---- Target tracking -------------------------------------------------------
struct Target {
    uint64_t va;
    float x, y, z;
    int fails;
};
static Target g_t[64];
static int g_nT = 0;

void RecordTarget(uint64_t proxyVA)
{
    if (!g_csInit) return;
    EnterCriticalSection(&g_cs);
    bool found = false;
    for (int i = 0; i < g_nT; i++)
        if (g_t[i].va == proxyVA) { found = true; break; }
    if (!found && g_nT < 64) {
        g_t[g_nT].va = proxyVA;
        g_t[g_nT].x = g_t[g_nT].y = g_t[g_nT].z = 0.f;
        g_t[g_nT].fails = 0;
        g_nT++;
    }
    LeaveCriticalSection(&g_cs);
}

// ---- v33: measured view resolver -------------------------------------------
// 16 candidates: camera-world from +0x2A0 or +0x320, interpreted column- or
// row-major; projection raw or transposed; view z-row as-built or negated.
// Score = fraction of live targets <150 m that land on-screen with w>0.
// Needs >=3 near targets to score at all; 0.45 to lock. Re-verified every 5 s.
static float g_raw[2][16];        // raw bytes from the two camera slots
static float g_projRaw[16];       // raw bytes from +0x3E0
static bool  g_haveRaw = false;
static int   g_lock   = -1;       // locked combo: (vi<<2)|(po<<1)|zf, -1 = scoring
static DWORD g_lockAt = 0;
static DWORD g_recheck = 0;

static const char* ComboName(int ci)
{
    static const char* slotN[2] = { "2A0", "320" };
    static const char* majN[2]  = { "col", "row" };
    static char buf[32];
    snprintf(buf, sizeof(buf), "v%s%s/p%s/z%s",
        slotN[(ci >> 2) & 1], majN[(ci >> 2) & 1],
        majN[(ci >> 1) & 1], (ci & 1) ? "-" : "+");
    return buf;
}

// Build VP for combo ci: VIEW from camera-world reading, VP = P * V
static bool ComboBuild(int ci, float outVP[16], float outEye[3])
{
    int vi = (ci >> 2) & 3;
    int po = (ci >> 1) & 1;
    int zf = ci & 1;
    const float* src = g_raw[vi >> 1];

    // camera-world candidate -> column-major (cols = basis, col 3 = eye)
    float cw[16];
    if (vi & 1) {
        // row-major source (rows = basis, translation at 12..14) -> transpose
        for (int c = 0; c < 4; c++)
            for (int r = 0; r < 4; r++)
                cw[c*4+r] = src[r*4+c];
    } else {
        memcpy(cw, src, 64);
    }

    float V[16];
    if (!BuildViewFromCamWorld(cw, V, outEye)) return false;

    if (zf) {
        // mirrored basis naming (right/up/-forward): negate view z row + its
        // translation component so w>0 means "in front" for this naming too
        V[8] = -V[8]; V[9] = -V[9]; V[10] = -V[10]; V[14] = -V[14];
    }

    // projection candidate
    float P[16];
    if (po & 1) {
        for (int c = 0; c < 4; c++)
            for (int r = 0; r < 4; r++)
                P[c*4+r] = g_projRaw[r*4+c];
    } else {
        memcpy(P, g_projRaw, 64);
    }

    MatMulColMajor(P, V, outVP);
    return true;
}

// Score a combo: fraction of live near targets that land on-screen, w>0.
// -1 = not enough evidence (<3 near targets).
static float ComboScore(int ci)
{
    float VP[16], eye[3];
    if (!ComboBuild(ci, VP, eye)) return -1.f;
    int nearCnt = 0, onCnt = 0;
    for (int i = 0; i < g_nT; i++) {
        if (g_t[i].x == 0.f && g_t[i].y == 0.f && g_t[i].z == 0.f) continue;
        float dx = g_t[i].x - eye[0];
        float dy = g_t[i].y - eye[1];
        float dz = g_t[i].z - eye[2];
        float d = sqrtf(dx*dx + dy*dy + dz*dz);
        if (d < 1.f || d > 150.f) continue;
        ++nearCnt;
        float x = g_t[i].x*VP[0] + g_t[i].y*VP[4] + g_t[i].z*VP[8]  + VP[12];
        float y = g_t[i].x*VP[1] + g_t[i].y*VP[5] + g_t[i].z*VP[9]  + VP[13];
        float w = g_t[i].x*VP[3] + g_t[i].y*VP[7] + g_t[i].z*VP[11] + VP[15];
        if (w <= 0.001f) continue;
        float nx = x / w, ny = y / w;
        if (nx > -1.1f && nx < 1.1f && ny > -1.1f && ny < 1.1f) ++onCnt;
    }
    if (nearCnt < 3) return -1.f;
    return (float)onCnt / (float)nearCnt;
}

// ---- Public API ------------------------------------------------------------
bool Init(void* device, void* immediateContext)
{
    (void)device; (void)immediateContext;
    
    oilog::Line("VPINIT: v33 measured resolver (16 combos, score-lock)");
    HMODULE nt = GetModuleHandleA("ntdll.dll");
    if (nt) g_NtRVM = (pfn_NtRVM)GetProcAddress(nt, "NtReadVirtualMemory");
    if (!g_NtRVM) {
        oilog::Line("VPINIT: FATAL - NtReadVirtualMemory not found");
        return false;
    }
    
    InitializeCriticalSectionAndSpinCount(&g_cs, 4000);
    g_csInit = true;
    
    oilog::Line("VPINIT: ready");
    return true;
}

void OnFrame()
{
    if (!g_csInit) return;
    EnterCriticalSection(&g_cs);
    
    // Update target positions
    for (int i = 0; i < g_nT; i++) {
        float p[3];
        if (Rd(g_t[i].va + 0x70, p, 12)) {
            if (p[0] != 0.f || p[1] != 0.f || p[2] != 0.f) {
                g_t[i].x = p[0]; g_t[i].y = p[1]; g_t[i].z = p[2];
                g_t[i].fails = 0;
            }
        } else if (++g_t[i].fails > 300) {
            g_t[i] = g_t[--g_nT];
            i--;
        }
    }
    
    // Sample raw matrices: both candidate camera slots + projection
    uint64_t mgr = 0, views = 0;
    bool ok = false;
    static int  s_fails = 0;
    static bool s_loggedRdFail = false;
    
    if (Rd(CAM_STATIC, &mgr, 8) && mgr > 0x10000ull &&
        Rd(mgr + MGR_VIEWS_OFF, &views, 8) && views > 0x10000ull) {
        bool r0 = Rd(views + CAM_WORLD_OFF,     g_raw[0], 64);   // +0x2A0
        bool r1 = Rd(views + CAM_WORLD_OFF_ALT, g_raw[1], 64);   // +0x320
        bool rp = Rd(views + PROJ_OFF, g_projRaw, 64);
        if (r0 && r1 && rp) {
            // projection must validate raw or transposed
            bool okP = ValidProj(g_projRaw);
            if (!okP) {
                float PT[16];
                for (int c = 0; c < 4; c++)
                    for (int r = 0; r < 4; r++)
                        PT[c*4+r] = g_projRaw[r*4+c];
                okP = ValidProj(PT);
            }
            if (okP) {
                g_haveRaw = true;
                s_loggedRdFail = false;
            }
        }
    }
    
    if (!g_haveRaw) {
        s_fails++;
        g_haveMatrix = false;
        if (!s_loggedRdFail) {
            s_loggedRdFail = true;
            oilog::Line("VP: camera struct read failed (chain or proj)");
        }
    } else if (g_lock < 0) {
        // ---- scoring: every combo projects live entities; best locks
        float best = -1.f; int bi = -1;
        for (int ci = 0; ci < 16; ci++) {
            float s = ComboScore(ci);
            if (s > best) { best = s; bi = ci; }
        }
        if (bi >= 0 && best >= 0.45f) {
            g_lock = bi;
            g_lockAt = GetTickCount();
            char buf[96];
            snprintf(buf, sizeof(buf), "VP: LOCKED %s score=%.2f", ComboName(g_lock), best);
            oilog::Line(buf);
        } else if (g_lockAt == 0) {
            g_lockAt = GetTickCount();
        } else if (GetTickCount() - g_lockAt > 10000) {
            g_lockAt = GetTickCount();
            char buf[96];
            snprintf(buf, sizeof(buf), "VP: scoring (%d targets, best %.2f)", g_nT, best);
            oilog::Line(buf);
        }
        g_haveMatrix = false;
    } else {
        // ---- locked: publish VP + eye, re-verify every 5 s
        float VP[16], eye[3];
        if (ComboBuild(g_lock, VP, eye)) {
            DWORD now = GetTickCount();
            if (g_recheck == 0 || now - g_recheck > 5000) {
                g_recheck = now;
                float s = ComboScore(g_lock);
                if (s >= 0.f && s < 0.15f) {
                    float bb = -1.f; int bn = -1;
                    for (int ci = 0; ci < 16; ci++) {
                        float t = ComboScore(ci);
                        if (t > bb) { bb = t; bn = ci; }
                    }
                    if (bn >= 0 && bn != g_lock && bb > 0.6f) {
                        g_lock = bn;
                        char buf[96];
                        snprintf(buf, sizeof(buf), "VP: SWITCH %s score=%.2f", ComboName(bn), bb);
                        oilog::Line(buf);
                    }
                }
            }
            memcpy(g_viewProj, VP, 64);
            g_cam[0] = eye[0]; g_cam[1] = eye[1]; g_cam[2] = eye[2];
            g_haveMatrix = true;
            ok = true;
            s_fails = 0;
        } else {
            g_haveMatrix = false;
        }
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

// v25 DEBUG: track W2S failures
static int s_w2sCalls = 0, s_w2sOkay = 0, s_w2sBehind = 0, s_w2sClip = 0;
static DWORD s_w2sLog = 0;
bool WorldToScreen(float wx, float wy, float wz, float* sx, float* sy)
{
    // Log every 5s
    DWORD now = GetTickCount();
    static DWORD s_lastDump = 0;
    bool doDump = (now - s_lastDump >= 5000);
    
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
    
    // COLUMN-VECTOR convention: clip = VP * pos (column-major storage)
    // For column-major: clip[i] = VP[i,0]*pos.x + VP[i,1]*pos.y + VP[i,2]*pos.z + VP[i,3]*pos.w
    //                          = mem[0+i]*pos.x + mem[4+i]*pos.y + mem[8+i]*pos.z + mem[12+i]*pos.w
    float x = wx*g_viewProj[0]  + wy*g_viewProj[4]  + wz*g_viewProj[8]  + g_viewProj[12];
    float y = wx*g_viewProj[1]  + wy*g_viewProj[5]  + wz*g_viewProj[9]  + g_viewProj[13];
    float z = wx*g_viewProj[2]  + wy*g_viewProj[6]  + wz*g_viewProj[10] + g_viewProj[14];
    float w = wx*g_viewProj[3]  + wy*g_viewProj[7]  + wz*g_viewProj[11] + g_viewProj[15];
    
    // Log first entity each dump cycle for debugging
    if (doDump) {
        s_lastDump = now;
        char buf[200];
        snprintf(buf, sizeof(buf), "W2S test pos=(%.1f,%.1f,%.1f) -> clip=(%.2f,%.2f,%.2f,%.2f)",
            wx, wy, wz, x, y, z, w);
        oilog::Line(buf);
    }
    
    // Behind camera check (now w > 0 means in front)
    if (w <= 0.001f) { ++s_w2sBehind; return false; }
    
    // NDC
    float nx = x / w;
    float ny = y / w;
    
    // Clip check
    if (nx < -1.5f || nx > 1.5f || ny < -1.5f || ny > 1.5f) { ++s_w2sClip; return false; }
    
    // To screen coords
    *sx = (nx * 0.5f + 0.5f) * g_scrW;
    *sy = (1.f - (ny * 0.5f + 0.5f)) * g_scrH;  // flip Y for screen coords
    
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
        dist[n] = Len3(d);
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
