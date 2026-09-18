// ---------------------------------------------------------------------------
// gw2_viewproj.cpp — see gw2_viewproj.h.
//
// 2026-09-03 REWRITE: the camera is read directly from the game's own
// CameraManager (Ghidra-decoded, dump-verified chain):
//     static 0x142CEE730 -> CameraManager
//     manager + 0x68     -> RenderView array (viewId * 0x520)
// v14: RenderView+0x2A0 = camera-world (row-major: rows right/up/back/eye,
//       eye.w == 1), RenderView+0x3E0 = projection (row-major, reversed-z:
//       w_clip = -z_view, >0 in front). +0x230/+0x360 are SLIDING structural
//       windows -- matrix rows scroll through them every frame; never
//       fixed-offset read those again.
//     view[0] + 0x230    = viewProj (row-major)
// Three pointer reads per frame = the game's current-frame camera matrix.
//
// The old D3D11 constant-buffer scraping system (Map/Unmap hooks, slot pools,
// checksums, shadow discriminators, discovery) was DELETED: the game rotates
// its CBs and offsets, and stale-but-plausible matrices caused every camera
// artifact (swimming, blinking, cannon breaks). This file no longer hooks
// anything — it is a pure reader.
//
// Threading: OnFrame runs on the Present thread; RecordTarget on the scan
// thread. Shared state is guarded by one critical section.
// ---------------------------------------------------------------------------
#include "gw2_viewproj.h"
#include "config.h"

// overlay's proven logger (OI:) — v21: viewproj's own Log() went silent in
// recent builds for unexplained reasons; critical one-shot facts now go
// through the same writer that demonstrably reaches the log file.
namespace oilog { void Line(const char*); }
#include <windows.h>
#include <d3d11.h>
#include <cstring>
#include <cmath>

namespace vproj {

// ---- logging (append handle, same pattern as main.cpp) ---------------------
static void Log(const char* msg)
{
#ifdef GW2_LOG_PATH
    static HANDLE hLog = INVALID_HANDLE_VALUE;
    if (hLog == INVALID_HANDLE_VALUE) {
        HANDLE h = CreateFileA(GW2_LOG_PATH, FILE_APPEND_DATA,
            FILE_SHARE_READ | FILE_SHARE_WRITE, nullptr, OPEN_ALWAYS,
            FILE_ATTRIBUTE_NORMAL, nullptr);
        if (h == INVALID_HANDLE_VALUE) return;
        HANDLE prev = InterlockedCompareExchangePointer(
            (PVOID volatile*)&hLog, h, (PVOID)INVALID_HANDLE_VALUE);
        if (prev != INVALID_HANDLE_VALUE) CloseHandle(h);
    }
    char buf[640];
    size_t n = 0;
    while (msg[n] && n < sizeof buf - 3) { buf[n] = msg[n]; n++; }
    buf[n++] = '\r'; buf[n++] = '\n';
    DWORD w = 0;
    WriteFile(hLog, buf, (DWORD)n, &w, nullptr);
#endif
}

#ifdef GW2_RELEASE
#define Log(m) ((void)0)
#endif

// Event log — NEVER compiled out (unlike Log above): rare camera/projection
// events only (zoom FOV recalibration, camera teleports, view-chain loss),
// rate-limited. These are the forensics for the "ESP breaks on aim-zoom"
// bug: Super Brainz hand-blast zoom changes the game FOV, and the old code
// calibrated P exactly ONCE (hooks disarmed via !g_haveProj), so every zoom
// projected through a stale hipfire projection.
static void EvLog(const char* msg)
{
#ifdef GW2_LOG_PATH
    static HANDLE hLog = INVALID_HANDLE_VALUE;
    if (hLog == INVALID_HANDLE_VALUE) {
        HANDLE h = CreateFileA(GW2_LOG_PATH, FILE_APPEND_DATA,
            FILE_SHARE_READ | FILE_SHARE_WRITE, nullptr, OPEN_ALWAYS,
            FILE_ATTRIBUTE_NORMAL, nullptr);
        if (h == INVALID_HANDLE_VALUE) return;
        HANDLE prev = InterlockedCompareExchangePointer(
            (PVOID volatile*)&hLog, h, (PVOID)INVALID_HANDLE_VALUE);
        if (prev != INVALID_HANDLE_VALUE) CloseHandle(h);
    }
    char buf[640];
    size_t n = 0;
    while (msg[n] && n < sizeof buf - 3) { buf[n] = msg[n]; n++; }
    buf[n++] = '\r'; buf[n++] = '\n';
    DWORD w = 0;
    WriteFile(hLog, buf, (DWORD)n, &w, nullptr);
#endif
}

static size_t SLen(const char* s) { size_t n = 0; while (s[n]) n++; return n; }
static void SCopy(char* d, const char* s, size_t& k) { while (*s) d[k++] = *s++; }

// manual float format: sign, integer part, '.', 2 fraction digits
static void FmtF(char* o, size_t& k, float v)
{
    if (!(v > -1e12f && v < 1e12f)) { SCopy(o, "nan", k); return; }
    if (v < 0) { o[k++] = '-'; v = -v; }
    float r = v + 0.005f;
    unsigned ip = (unsigned)r;
    if (ip >= 1000000000u) { SCopy(o, "big", k); return; }
    char t[12]; int tn = 0;
    if (ip == 0) t[tn++] = '0';
    while (ip && tn < 11) { t[tn++] = (char)('0' + ip % 10); ip /= 10; }
    while (tn) o[k++] = t[--tn];
    o[k++] = '.';
    unsigned fp = (unsigned)((r - (float)(unsigned)r) * 100.f);
    o[k++] = (char)('0' + fp / 10);
    o[k++] = (char)('0' + fp % 10);
}

static void FmtU(char* o, size_t& k, unsigned long long v)
{
    char t[24]; int tn = 0;
    if (v == 0) t[tn++] = '0';
    while (v && tn < 23) { t[tn++] = (char)('0' + v % 10); v /= 10; }
    while (tn) o[k++] = t[--tn];
}

static void FmtHex(char* o, size_t& k, unsigned long long v)
{
    static const char d[] = "0123456789ABCDEF";
    o[k++] = '0'; o[k++] = 'x';
    int sh = 60; bool seen = false;
    while (sh >= 0) {
        char c = d[(v >> sh) & 0xF];
        if (c != '0' || seen || sh == 0) { o[k++] = c; seen = true; }
        sh -= 4;
    }
}

// ---- NtReadVirtualMemory: fault-proof reads (no SEH in manual map) ----------
typedef LONG(WINAPI* pfn_NtRVM)(HANDLE, PVOID, PVOID, ULONG, PULONG);
static pfn_NtRVM g_NtRVM = nullptr;

// ---- shared state ------------------------------------------------------------
static CRITICAL_SECTION g_cs;
static bool g_csInit = false;

static float g_viewProj[16];          // current frame's camera matrix
static float g_lockCam[3];            // solved camera position
static bool  g_haveMatrix = false;    // engine chain delivering
static int   g_engineCamFails = 0;    // consecutive bad reads (diagnostics)

static float g_scrW = 1920.f, g_scrH = 1080.f;

// live proxies (RecordTarget from scan; positions refreshed per frame —
// used for the adoption sanity check and the status line)
struct Target {
    uint64_t va;        // hknpCharacterProxy instance
    float   x, y, z;    // live position (0,0,0 = unknown/pooled)
    int     fails;      // consecutive read failures
};
static Target g_t[64];
static int    g_nT = 0;
static volatile LONG g_nLive = 0;
static float g_centroid[3];

// gameplay gate helper: a live proxy within 3 m of the given camera point
static bool LiveEntityNear(const float cam[3])
{
    for (int i = 0; i < g_nT; i++) {
        if (g_t[i].x == 0.f && g_t[i].y == 0.f && g_t[i].z == 0.f) continue;
        float dx = g_t[i].x - cam[0];
        float dy = g_t[i].y - cam[1];
        float dz = g_t[i].z - cam[2];
        if (dx*dx + dy*dy + dz*dz < 9.f) return true;   // < 3 m
    }
    return false;
}

// ---- ENGINE CAMERA CHAIN (Ghidra-decoded, dump-verified 2026-09-03) --------
//   static 0x142CEE730 -> CameraManager
//   manager+0x68       -> RenderView array (viewId * 0x520 each)
static constexpr uint64_t CAM_STATIC          = 0x142CEE730ull;
static constexpr uint32_t CAM_MGR_VIEWS       = 0x68;
// v15: candidate camera-world slots in the RenderView pool. The 0x520
// struct carries several per-pass cameras; the dump proved valid copies at
// 0x2A0 with matching-basis copies nearby (0x3B0/0x4C0 region). Slot
// identity is NOT trusted — the per-frame scanner picks the stable one
// every frame (see OnFrame) and the DIAG log labels every slot.
static constexpr uint32_t RV_CAM_SLOTS[3]     = { 0x2A0, 0x3B0, 0x4C0 };
static constexpr uint32_t RV_PROJ_DELTA       = 0x140;    // cam_k -> proj_k (observed 0x2A0 -> 0x3E0)
static constexpr uint32_t RV_PROJ_ALT         = 0x3E0;    // slot-0 projection fallback
static int      g_camLocked = -1;    // index into the scanned camera pool
static bool     g_haveBasis = false;
static float    g_lockFwd[3], g_lockUp[3];    // last locked basis (relock identity)
static float g_view[16];             // view (rows right/up/forward*, see z note)
                                     // z row = zs*fwd with T = -zs*fwd.eye;
                                     // zs=-1 => classic back-basis view
// v19 z-convention resolver state (used by OnFrame + WorldToScreen)
static int    g_zFlipDone = 0;      // 1 = engine row measured FORWARD, negated
static long   g_zVotes = 0, g_zIn = 0, g_zOut = 0;
static DWORD  g_zWin = 0;
static float g_proj[16];             // engine projection (reversed-z, RH)
static bool  g_haveView = false;     // camera-world chain delivering
static bool  g_haveProj = false;     // engine projection delivering
static bool Rd(uint64_t va, void* dst, size_t n)
{
    if (!g_NtRVM) return false;
    ULONG got = 0;
    return g_NtRVM((HANDLE)-1, (PVOID)va, dst, (ULONG)n, &got) == 0 && got == (ULONG)n;
}

// ---- tiny math ----------------------------------------------------------------
static float Dot3(const float* a, const float* b) { return a[0]*b[0] + a[1]*b[1] + a[2]*b[2]; }
static float Len3(const float* a) { float d = Dot3(a, a); return d > 0.f ? sqrtf(d) : 0.f; }
static bool  Fin(float v) { return v > -1e12f && v < 1e12f; } // NaN fails both

// v15 CAMERA READS — self-measuring, offset-guess-free.
//
// v14 lesson (live log 13:59): the RenderView is a POOL of per-pass
// cameras — several offsets hold valid-looking matrices and the +0x2A0
// copy is rewritten by different passes between frames (eye teleported
// all over, mirrored bases in the log). Offsets from one frozen dump
// cannot identify the world camera. So: scan candidate slots every frame,
// keep the one that is STABLE frame-to-frame (shadow/reflection cameras
// move or flip every frame; the world camera tracks gameplay smoothly),
// and LOG each slot's relation to the pick so the log itself proves the
// layout — no more guessing.

struct CamRead {
    float    eye[3], right[3], up[3], fwd[3];   // fwd = engine 3rd row AS STORED
                                                    // (naming measured live, v19)
    uint32_t off;      // RenderView offset of this camera-world
};

static bool BasisFromCamWorld(const float* cw, CamRead& out)
{
    float right[3] = { cw[0],  cw[1],  cw[2]  };
    float up[3]    = { cw[4],  cw[5],  cw[6]  };
    float fwd[3]   = { cw[8],  cw[9],  cw[10] };
    float eye[3]   = { cw[12], cw[13], cw[14] };
    for (int i = 0; i < 3; i++)
        if (!Fin(right[i]) || !Fin(up[i]) || !Fin(fwd[i]) || !Fin(eye[i])) return false;
    if (cw[3] != 0.f || cw[7] != 0.f || cw[11] != 0.f || cw[15] != 1.f) return false;
    float lr = Len3(right), lu = Len3(up), lf = Len3(fwd);
    if (lr < 0.9f || lr > 1.1f || lu < 0.9f || lu > 1.1f || lf < 0.9f || lf > 1.1f)
        return false;
    if (fabsf(Dot3(right, up)) > 0.02f || fabsf(Dot3(right, fwd)) > 0.02f ||
        fabsf(Dot3(up, fwd))   > 0.02f) return false;
    memcpy(out.right, right, 12);
    memcpy(out.up,    up,    12);
    memcpy(out.fwd,   fwd,   12);
    memcpy(out.eye,   eye,   12);
    return true;
}

// Returns RenderView[0] base VA + all valid camera slots this frame.
static bool ReadCameras(CamRead* cams, int& nCams, int maxCams, uint64_t& viewsVA)
{
    nCams = 0;
    viewsVA = 0;
    uint64_t mgr = 0, views = 0;
    if (!Rd(CAM_STATIC, &mgr, 8)) return false;
    if (mgr < 0x10000ull || mgr >= 0x800000000000ull) return false;
    if (!Rd(mgr + CAM_MGR_VIEWS, &views, 8)) return false;
    if (views < 0x10000ull || views >= 0x800000000000ull) return false;
    viewsVA = views;

    // +0x68 may point straight at view[0] (dump: 0x50B54170) or at a
    // descriptor whose first qword is the real base. Sniff both.
    uint64_t probe[2] = { 0, 0 };
    if (Rd(views, probe, 16) && probe[0] >= 0x10000ull && probe[0] < 0x800000000000ull &&
        probe[0] != views) {
        float cw[16];
        CamRead t;
        if (Rd(probe[0] + RV_CAM_SLOTS[0], cw, 64) && BasisFromCamWorld(cw, t))
            views = probe[0];
    }

    for (int s = 0; s < 3 && nCams < maxCams; s++) {
        float cw[16];
        if (!Rd(views + RV_CAM_SLOTS[s], cw, 64)) continue;
        CamRead c;
        c.off = RV_CAM_SLOTS[s];
        if (!BasisFromCamWorld(cw, c)) continue;
        cams[nCams++] = c;
    }
    return nCams > 0;
}

// v16: the projection is found by SHAPE every frame, not by offset.
// A reversed-z RH perspective projection is unmistakable wherever the game
// stores it: rows 0/1 are pure positive scale (the cot(fov/2) pair), row 2
// is (0, 0, ~0, near), row 3 is (0, 0, -1, 0). Scan the whole 0x520
// RenderView in 4-float steps — the first match wins and the DIAG2 log
// line proves the live layout.
static uint32_t g_projOff = 0;          // where the projection was found
static bool FindProjection(uint64_t viewsVA, float m[16])
{
    float buf[0x520 / 4];
    if (!Rd(viewsVA, buf, sizeof buf)) return false;
    for (uint32_t o = 0; o + 64 <= 0x520; o += 4) {
        const float* p = buf + o / 4;
        bool ok = true;
        for (int j = 0; j < 16 && ok; j++) if (!Fin(p[j])) ok = false;
        if (!ok) continue;
        // rows 0/1: pure positive scale, no cross terms
        if (p[0] < 0.2f || p[0] > 8.f) continue;
        if (p[5] < 0.2f || p[5] > 8.f) continue;
        if (p[1] != 0.f || p[2] != 0.f || p[3] != 0.f) continue;
        if (p[4] != 0.f || p[6] != 0.f || p[7] != 0.f) continue;
        // row 2: (0, 0, ~0, near), near in (1e-6, 100)
        if (p[8] != 0.f || p[9] != 0.f) continue;
        if (p[10] < -0.01f || p[10] > 0.01f) continue;
        if (p[11] < 1e-6f || p[11] > 100.f) continue;
        // row 3: (0, 0, -1, 0)
        if (p[12] != 0.f || p[13] != 0.f || p[15] != 0.f) continue;
        if (fabsf(p[14] + 1.f) > 1e-6f) continue;
        memcpy(m, p, 64);
        g_projOff = o;
        return true;
    }
    return false;
}

// stored row-major (conv 0): clip = M * v
static void EffectiveRows(const float* m, int conv, float R[4][4])
{
    for (int r = 0; r < 4; r++)
        for (int c = 0; c < 4; c++)
            R[r][c] = conv ? m[c * 4 + r] : m[r * 4 + c];
}
// cheap structural prefilter
static bool Structural(const float R[4][4])
{
    for (int r = 0; r < 4; r++)
        for (int c = 0; c < 4; c++)
            if (!Fin(R[r][c])) return false;
    float wl = Len3(R[3]);
    if (wl < 0.05f || wl > 8.f) return false;
    float n0 = Len3(R[0]);
    float n1 = Len3(R[1]);
    if (n0 < 0.05f || n0 > 10.f || n1 < 0.05f || n1 > 10.f) return false;
    if (fabsf(Dot3(R[0], R[3])) > 0.15f * n0 * wl) return false;
    if (fabsf(Dot3(R[1], R[3])) > 0.15f * n1 * wl) return false;
    float rx = n0 / wl, ry = n1 / wl;
    if (rx < 0.15f || rx > 6.5f || ry < 0.15f || ry > 6.5f) return false;
    if (fabsf(R[3][0]) < 1e-6f && fabsf(R[3][1]) < 1e-6f && fabsf(R[3][2]) < 1e-6f)
        return false;
    return true;
}

// Cramer solve of 3x3 A*p = b
static bool Solve3(const float A[3][3], const float* b, float* p)
{
    float det =
        A[0][0] * (A[1][1] * A[2][2] - A[1][2] * A[2][1]) -
        A[0][1] * (A[1][0] * A[2][2] - A[1][2] * A[2][0]) +
        A[0][2] * (A[1][0] * A[2][1] - A[1][1] * A[2][0]);
    if (fabsf(det) < 1e-12f) return false;
    float inv = 1.f / det;
    p[0] = (b[0] * (A[1][1] * A[2][2] - A[1][2] * A[2][1]) -
            A[0][1] * (b[1] * A[2][2] - A[1][2] * b[2]) +
            A[0][2] * (b[1] * A[2][1] - A[1][1] * b[2])) * inv;
    p[1] = (A[0][0] * (b[1] * A[2][2] - A[1][2] * b[2]) -
            b[0] * (A[1][0] * A[2][2] - A[1][2] * A[2][0]) +
            A[0][2] * (A[1][0] * b[2] - b[1] * A[2][0])) * inv;
    p[2] = (A[0][0] * (A[1][1] * b[2] - b[1] * A[2][1]) -
            A[0][1] * (A[1][0] * b[2] - b[1] * A[2][0]) +
            b[0] * (A[1][0] * A[2][1] - A[1][1] * A[2][0])) * inv;
    return Fin(p[0]) && Fin(p[1]) && Fin(p[2]);
}

// v21: derive the camera eye from the ACTIVE viewProj and publish it.
// DebugList measures entity distances from g_lockCam — the CB method
// (primary again) never wrote it, so with a stale/wrong camera every
// entity's distance inflated and the 250 m ESP gate culled the whole list
// (v20 log: list=57 dist=0 — the silent box killer). Rows 0/1/3 give a
// sign-safe solve (w-row and z-row enter squared/linear on x,y only).
// Also resets the z-convention vote: a new matrix identity re-measures.
static void PublishCameraFromVP()
{
    float R[4][4];
    EffectiveRows(g_viewProj, 0, R);
    float A[3][3] = {
        { R[0][0], R[0][1], R[0][2] },
        { R[1][0], R[1][1], R[1][2] },
        { R[3][0], R[3][1], R[3][2] },
    };
    float b[3] = { -R[0][3], -R[1][3], -R[3][3] };
    float cam[3];
    if (!Solve3(A, b, cam)) return;
    if (!Fin(cam[0]) || !Fin(cam[1]) || !Fin(cam[2])) return;
    if (fabsf(cam[0]) > 1e6f || fabsf(cam[1]) > 1e6f || fabsf(cam[2]) > 1e6f) return;
    g_lockCam[0] = cam[0]; g_lockCam[1] = cam[1]; g_lockCam[2] = cam[2];
    if (g_zFlipDone) { g_zFlipDone = 0; g_zVotes = 0; g_zIn = 0; g_zOut = 0; }
}

// Build a perspective projection matrix (row-major, LH, depth [0,1])
// fov in radians, aspect = width/height
static void BuildPerspective(float P[16], float fovY, float aspect, float zn, float zf)
{
    float h = 1.f / tanf(fovY * 0.5f);
    float w = h / aspect;
    float q = zf / (zf - zn);
    memset(P, 0, 64);
    P[0] = w;             // [0][0]
    P[5] = h;             // [1][1]
    P[10] = q;            // [2][2]
    P[11] = -zn * q;      // [2][3]
    P[14] = 1.f;          // [3][2]
}

// Fallback projection for when D3D hooks fail to calibrate
static int   g_noCalibFrames = 0;
static bool  g_usedFallbackProj = false;

// ---- public API -----------------------------------------------------------------
void RecordTarget(uint64_t proxyVA)
{
    if (!g_csInit) return;
    EnterCriticalSection(&g_cs);
    bool known = false;
    for (int i = 0; i < g_nT; i++)
        if (g_t[i].va == proxyVA) { known = true; break; }
    if (!known && g_nT < 64) {
        g_t[g_nT].va = proxyVA;
        g_t[g_nT].x = g_t[g_nT].y = g_t[g_nT].z = 0.f;
        g_t[g_nT].fails = 0;
        g_nT++;
    }
    LeaveCriticalSection(&g_cs);
}

// ---- PROJECTION CALIBRATION via D3D CB hook ---------------------------------
// The game builds viewProj = P * V per frame in registers and uploads it to a
// WRITE_DISCARD constant buffer (proven: no struct-owned viewProj exists in
// CPU RAM). Each candidate upload is verified against our engine view (same
// camera within 3 m) and P = VP_cb * V^-1 is extracted.
//
// 2026-09-09v3 WORLD-TRACK architecture (lag + blink + swim, log-driven):
// The friend/dev log showed the exact loop: "FOV change committed" (no zoom
// pressed) -> "direct VP stale" storms -> "slot lock timed out". Two causes:
//   (a) Frostbite rotates WRITE_DISCARD ring buffers WIDER than the 8-slot
//       lock — most frames none of the locked buffers carried the world VP,
//       the snapshot went stale and the shaky P * V fallback took over.
//   (b) matrices with the SAME camera but DIFFERENT focal (weapon-viewmodel
//       VP) pass the 3 m camera gate, so P kept flip-flopping between focals.
// v3: track EVERY mapped constant buffer per frame (descriptor cache with
// NEGATIVE caching — the 09-08 lag was QI/GetDesc on non-CB Maps that never
// hit the positive-only cache), and accept a candidate as THE world VP only
// when its focal matches the established world focal (weapon VP is rejected
// automatically). A genuine zoom (aim) is detected by the old focal
// vanishing for 6 consecutive frames while a new one persists — the world
// VP uploads every frame, so the weapon VP can never win a race it doesn't
// already own. Direct snapshot stays primary; P * V stays the menu/loading
// fallback only.
static uint32_t g_vpFrame = 0;           // Present-frame counter
// slot identity of the world VP (v4) — the resource+offset that carried the
// last validated world matrix. The game re-uploads the world VP to this same
// slot every frame (zoom included), so slot identity IS the world-VP identity.
static void*     g_slotRes = nullptr;
static uint32_t  g_slotOff = 0;
static int       g_slotConv = 0;         // storage convention that validated
static void*    g_slotBRes = nullptr;   // zoom-slot buffer (adopted on aim)
static int      g_aMismatch = 0;        // consecutive frames slot A missed the engine camera
static int       g_slotMissing = 0;    // frames since the locked slot last hit

// per-frame acceptance state (reset in OnFrame, set in the Unmap hook)
static bool     g_frameMatched = false;  // world VP accepted this frame

// DIRECT VP SNAPSHOT — the game's own current-frame viewProj. Using it
// directly for WorldToScreen removes the P * V reconstruction entirely (that
// reconstruction mixed data updated at different times in the frame — boxes
// swam while the camera moved).
static float    g_cbVP[16];         // normalized row-major game VP
static bool     g_cbVPValid = false;
static uint32_t g_cbVPAge = 0;      // Present frames since last refresh
static void*    g_slotData = nullptr;   // LATEST pData seen at Map (world buf)
static bool CalibrateProj(const float* cbVP, int conv, void* res, uint32_t byteOff)
{
    // cbVP: 16 floats as stored. Effective rows with conv.
    float C[4][4];
    EffectiveRows(cbVP, conv, C);
    float V[4][4];
    EffectiveRows(g_view, 0, V);

    // sanity: C's camera must match our engine camera (within 25 m)
    float A[3][3] = {
        { C[0][0], C[0][1], C[0][2] },
        { C[1][0], C[1][1], C[1][2] },
        { C[3][0], C[3][1], C[3][2] },
    };
    float b[3] = { -C[0][3], -C[1][3], -C[3][3] };
    float cam[3];
    if (!Solve3(A, b, cam)) return false;
    float d[3] = { cam[0] - g_lockCam[0], cam[1] - g_lockCam[1], cam[2] - g_lockCam[2] };
    float camDist = Len3(d);
    // TIGHT 3 m gate (was 25 m): the MAIN viewProj solves EXACTLY to the view's
    // camera (same eye). Measured: shadow/twin VPs sit ~9.7 m off — a 25 m gate
    // accepted them and their perspective difference put every box in the wrong
    // place ("under the map"). 3 m admits only the true main VP.
    if (camDist > 3.f) return false;
    // GAMEPLAY GATE (2026-09-09 tiny-boxes fix): the 9/9 PM session locked
    // the slot in the MENU — LSCAN ent=0 five times, then 'slot locked'.
    // A menu/character-select camera matrix projects world entities into a
    // garbage far-away placement (tiny boxes at the bottom of the screen).
    // The real gameplay world camera ORBITS a live character: require at
    // least one live proxy within 3 m of the candidate's camera. Menu
    // cameras float far from the (few, idle) proxies; gameplay always has
    // the local player's proxies right at the boom distance.
    {
        bool nearLive = false;
        for (int i = 0; i < g_nT && !nearLive; i++) {
            if (g_t[i].x == 0.f && g_t[i].y == 0.f && g_t[i].z == 0.f) continue;
            float dx = g_t[i].x - cam[0];
            float dy = g_t[i].y - cam[1];
            float dz = g_t[i].z - cam[2];
            if (dx*dx + dy*dy + dz*dz < 9.f) nearLive = true;   // < 3 m
        }
        if (!nearLive) return false;
    }
    bool camFresh = (camDist <= 0.30f);

    // V^-1 via GENERAL 4x4 Gauss-Jordan. The old special-case affine inverse
    // assumed row 3 = [0,0,0,1] — WRONG for this engine: the view's 4th row
    // is another rotation axis (general Vi row3 = (10,-7.3,-7.3,0) in dump-8),
    // producing a garbage P and boxes projecting nowhere. Proven offline:
    // P*V*entity == VP*entity EXACTLY with the general inverse (dump-8
    // crowd centroid round-trip match to 0.01).
    float Vi[4][4];
    {
        // augmented [V | I] -> [I | V^-1]
        float A[4][8];
        for (int r = 0; r < 4; r++) {
            for (int c = 0; c < 4; c++) { A[r][c] = V[r][c]; A[r][4 + c] = (r == c) ? 1.f : 0.f; }
        }
        for (int col = 0; col < 4; col++) {
            int piv = col;
            for (int r = col + 1; r < 4; r++)
                if (fabsf(A[r][col]) > fabsf(A[piv][col])) piv = r;
            if (fabsf(A[piv][col]) < 1e-12f) return false;   // singular — skip
            if (piv != col)
                for (int c = 0; c < 8; c++) { float t = A[col][c]; A[col][c] = A[piv][c]; A[piv][c] = t; }
            float d = A[col][col];
            for (int c = 0; c < 8; c++) A[col][c] /= d;
            for (int r = 0; r < 4; r++) {
                if (r == col) continue;
                float f = A[r][col];
                if (f == 0.f) continue;
                for (int c = 0; c < 8; c++) A[r][c] -= f * A[col][c];
            }
        }
        for (int r = 0; r < 4; r++)
            for (int c = 0; c < 4; c++) Vi[r][c] = A[r][4 + c];
    }
    // verify: V * Vi ~ identity (guards against a garbage inversion)
    {
        float test[4][4] = {};
        for (int r = 0; r < 4; r++)
            for (int c = 0; c < 4; c++)
                for (int k = 0; k < 4; k++)
                    test[r][c] += V[r][k] * Vi[k][c];
        float idn = 0.f;
        for (int i = 0; i < 4; i++) idn += fabsf(test[i][i] - 1.f);
        for (int r = 0; r < 4; r++)
            for (int c = 0; c < 4; c++)
                if (r != c) idn += fabsf(test[r][c]);
        if (idn > 0.01f) return false;   // not clean — skip this candidate
    }

    // P = C * Vi
    float P[4][4] = {};
    for (int r = 0; r < 4; r++)
        for (int c = 0; c < 4; c++)
            for (int k = 0; k < 4; k++)
                P[r][c] += C[r][k] * Vi[k][c];

    // ---- gates passed; now decide: is this the WORLD VP? ---------------------
    // SLOT-IDENTITY rule (v4, log+math driven). The v3 focal gate was WRONG:
    // Frostbite's VP rows are not unit-normalized (dump-8: view row3 =
    // (10,-7.3,-7.3)), so the extracted focal changes with camera ROTATION —
    // every camera move read as a "zoom change" (78 false "zoom adopted"
    // events in one session, boxes threw themselves everywhere).
    //
    // The stable identity of the world VP is WHERE it lives: the game uploads
    // the world viewProj to the same constant-buffer slot (same resource,
    // same byte offset, same convention) every frame, and the weapon
    // viewmodel VP to a DIFFERENT slot. So:
    //   - first valid candidate locks the slot identity;
    //   - every later frame: a candidate from the LOCKED slot is the world
    //     VP — snapshot it (zoom data arrives in the same slot, so aiming
    //     tracks with zero extra logic);
    //   - candidates from other slots are ignored (weapon VP never wins);
    //   - if the locked slot produces nothing for ~1 s (buffer pool rotated
    //     to fresh allocations), re-lock on the next passing candidate.

    // normalized row-major candidate (the snapshot format)
    float Rn[4][4];
    EffectiveRows(cbVP, conv, Rn);
    if (!g_haveProj) {
        // DISCOVERY acceptance (first lock) — the Map-time read takes over
        // from the next frame. Slot identity + projection both locked here.
        memcpy(g_cbVP, Rn, sizeof g_cbVP);
        g_cbVPValid = true;
        g_cbVPAge = 0;
        memcpy(g_proj, P, sizeof g_proj);
        g_haveProj = true;
        g_slotRes = res;
        g_slotOff = byteOff;
        g_slotConv = conv;
        g_slotMissing = 0;
        g_frameMatched = true;
        EvLog("VP EVENT: projection CALIBRATED from CB (slot locked)");
        return true;
    }
    // already locked: the Unmap discovery scan should not even reach here —
    // the Map-time read owns steady state. Accept same-slot updates only.
    if (res == g_slotRes && byteOff == g_slotOff) {
        memcpy(g_cbVP, Rn, sizeof g_cbVP);
        g_cbVPValid = true;
        g_cbVPAge = 0;
        g_slotMissing = 0;
        g_frameMatched = true;
        memcpy(g_proj, P, sizeof g_proj);
    }
    return true;
}

// ---- CB hooks (MAP-TIME READ, v5 — the definitive design) --------------------
// THE LESSON (three builds of evidence): Frostbite rotates WRITE_DISCARD
// buffer OBJECTS per Map and does not reliably Unmap them — an Unmap-driven
// snapshot only refreshed every ~3-4 frames (the "direct VP stale" storm in
// every log), and every stale frame meant the shaky P*V fallback. Reading on
// Map fixes it structurally:
//
//   When the game calls Map(DISCARD) on buffer B, pData is the SAME memory
//   B's last Map returned — still holding LAST frame's uploaded contents.
//   So on every Map of the world buffer we read the matrix that the game
//   wrote there the previous time it used this buffer. One frame of latency,
//   EVERY frame, no Unmap needed at all. Present runs after the frame's
//   Maps, so the snapshot is always current enough.
//
// Discovery (before first lock) needs Unmap-time content on ONE buffer at a
// time only — still cheap. After the lock, Map hooks do ALL the work with
// ZERO critical-section entry on the fast path (res identity is compared
// locklessly; buffer writes are 8-byte aligned and idempotent).
typedef HRESULT(STDMETHODCALLTYPE* MapFn)(ID3D11DeviceContext*, ID3D11Resource*,
    UINT, D3D11_MAP, UINT, D3D11_MAPPED_SUBRESOURCE*);
typedef void(STDMETHODCALLTYPE* UnmapFn)(ID3D11DeviceContext*, ID3D11Resource*, UINT);
static MapFn   g_origMap = nullptr;
static UnmapFn g_origUnmap = nullptr;
static ID3D11DeviceContext* g_ctx = nullptr;

static const uint32_t CAL_SNAP_MAX = 16384;

// ---- candidate ring (v7.1 SINGLE-WRITER) -------------------------------------
// v7 race postmortem (log 12:33, ~1400 "freed buffer dropped" + C0000005 at
// the same rip every frame): the Map hook (GAME RENDER thread) and OnFrame
// (PRESENT thread) both wrote g_cand[] — torn {res,data} pairs were read,
// faulted, and the ring self-destructed. No ESP ever validated.
// v7.1 discipline: the render thread NEVER touches the ring. It publishes
// {res, data} pairs into a fixed-size handoff of EXCHANGE slots (one
// InterlockedExchangePointer per slot — a slot publish is atomic; a slot is
// claimed with a second exchange). The PRESENT thread is the ONLY ring
// writer: it claims handoff slots, merges them into the ring, then polls.
// All ring reads happen on the thread that wrote them. No torn state.
struct Cand { void* res; void* data; uint32_t width; };
static Cand    g_cand[16];                 // Present-thread owned
static int     g_nCand = 0;                // Present-thread owned

// handoff slots: render thread publishes, Present thread claims.
// pub[i] != null means a fresh {res,data,width} waits; claiming swaps to null.
static void* volatile g_hoPub[32];         // published Cand* (heap-free: pool below)
static Cand   g_hoPool[32];                // backing storage for the pointers
static volatile LONG g_hoCursor = 0;       // render-thread round-robin publisher

static void CandPublish(void* res, void* data, uint32_t width)   // RENDER THREAD ONLY
{
    LONG slot = (InterlockedIncrement(&g_hoCursor) - 1) & 31;
    g_hoPool[slot].res = res;
    g_hoPool[slot].data = data;
    g_hoPool[slot].width = width;
    InterlockedExchangePointer(&g_hoPub[slot], (void*)&g_hoPool[slot]);
    // If the consumer had not claimed the previous publish yet, it is
    // overwritten — fine: the consumer polls every frame, so at most a few
    // very short-lived entries are lost, never correctness.
}

// claim ALL pending handoff entries into the fresh-frame buffer (PRESENT
// THREAD ONLY). v12 freeze fix: we only ever read pointers the game handed
// us THIS frame — a Map'd DISCARD allocation is valid from its Map until
// the driver cycles the pool, and same-frame reads sit safely inside that
// window. The old persistent ring kept stale pointers across frames; at
// camera teleports the driver recycles the pool aggressively and every
// stale read faulted (garden join / char switch freezes, C0000005 before
 // calibration in the 9/9 log).
static Cand    g_fresh[16];                // this frame's Map'd buffers
static int     g_nFresh = 0;               // Present-thread owned

static void CandClaimFresh(void)
{
    g_nFresh = 0;
    for (int i = 0; i < 32 && g_nFresh < 16; i++) {
        void* p = InterlockedExchangePointer(&g_hoPub[i], nullptr);
        if (!p) continue;
        Cand* c = (Cand*)p;
        if (!c->res || !c->data) continue;
        // width guard: skip buffers too small for the locked offset
        if (g_haveProj && c->width && g_slotOff + 64 > c->width) continue;
        // dedup by resource (game can Map the same buffer twice a frame)
        bool dup = false;
        for (int j = 0; j < g_nFresh && !dup; j++)
            if (g_fresh[j].res == c->res) dup = true;
        if (dup) continue;
        g_fresh[g_nFresh].res = c->res;
        g_fresh[g_nFresh].data = c->data;
        g_fresh[g_nFresh].width = c->width;
        g_nFresh++;
    }
}

// resource descriptor cache (res -> is CB + width). Negatives are cached too,
// so a steady-state Map costs zero COM calls. Access is single-threaded per
// context in practice (immediate context), and entries are append-only.
struct ResDesc { void* res; uint32_t width; bool isCb; };
static ResDesc g_resCache[1024];
static int     g_nRes = 0;

static bool CbIsBuffer(void* res, uint32_t* width)
{
    for (int i = 0; i < g_nRes; i++)
        if (g_resCache[i].res == res) {
            if (!g_resCache[i].isCb) return false;
            *width = g_resCache[i].width;
            return true;
        }
    ID3D11Buffer* buf = nullptr;
    bool isCb = false; uint32_t w = 0;
    if (SUCCEEDED(((ID3D11Resource*)res)->QueryInterface(__uuidof(ID3D11Buffer), (void**)&buf)) && buf) {
        D3D11_BUFFER_DESC d;
        buf->GetDesc(&d);
        buf->Release();
        if ((d.BindFlags & D3D11_BIND_CONSTANT_BUFFER) && d.ByteWidth >= 64) {
            isCb = true; w = d.ByteWidth;
        }
    }
    if (g_nRes < 1024) {
        g_resCache[g_nRes].res = res;
        g_resCache[g_nRes].width = w;
        g_resCache[g_nRes].isCb = isCb;
        g_nRes++;
    }
    *width = w;
    return isCb;
}

static HRESULT STDMETHODCALLTYPE CalMapHook(ID3D11DeviceContext* self, ID3D11Resource* res,
    UINT sub, D3D11_MAP type, UINT flags, D3D11_MAPPED_SUBRESOURCE* out)
{
    HRESULT hr = g_origMap(self, res, sub, type, flags, out);
    if (!SUCCEEDED(hr) || !out || !out->pData) return hr;
    if (!(type == D3D11_MAP_WRITE || type == D3D11_MAP_WRITE_DISCARD ||
          type == D3D11_MAP_WRITE_NO_OVERWRITE)) return hr;

    // v7.1: publish to the handoff (render thread NEVER touches the ring —
    // that race faulted every frame in the 12:33 log). Cached descriptor
    // check; the Present thread merges and polls.
    uint32_t w = 0;
    if (CbIsBuffer(res, &w) && w >= 64) {
        CandPublish(res, out->pData, w);
        static long s_pubOnce = 0;
        if (InterlockedCompareExchange(&s_pubOnce, 1, 0) == 0)
            EvLog("CAL: Map-hook candidate published (first)");   // buffered writer
    } else {
        static long s_nonBufOnce = 0;
        if (InterlockedCompareExchange(&s_nonBufOnce, 1, 0) == 0)
            EvLog("CAL: Map fired but dest not a >=64B CB buffer");
    }
    return hr;
}

static void STDMETHODCALLTYPE CalUnmapHook(ID3D11DeviceContext* self, ID3D11Resource* res, UINT sub)
{
    // v13 (render-thread freeze fix): PURE PASSTHROUGH — no memory reads here, ever.
    // The old discovery path claimed a Map handoff pointer and scanned its
    // CONTENT on this (render) thread. If Present did not run between the
    // Map and this Unmap (loading screens, garden join, char switch), the
    // handoff still held a PREVIOUS frame's pointer; the driver had already
    // recycled that allocation, and the read faulted INSIDE the game's own
    // Unmap call — the render thread dies mid-D3D-call and the game wedges
    // (VEH C0000005 rip ~0x74C4CB9 in all three freeze logs, then silence).
    // Discovery no longer needs it: the Present-thread poll (CandClaimFresh +
    // the unlocked discovery loop) does full-frame discovery every frame with
    // same-frame pointers only, and CalUpdateSubresourceHook reads only
    // caller srcData valid during the call. Nothing left to do here.
    g_origUnmap(self, res, sub);
}

// UpdateSubresource (immediate-context vtable 48) — the third upload path.
// The old proven CB-scraping system hooked exactly this; v4-v6 did not.
// pSrcData is caller memory holding the FULL new constant-buffer content
// DURING the call = current-frame data, no ring staleness, no saved
// pointers. If the destination is our locked world buffer, this is the
// freshest possible source: snapshot straight from it.
typedef void(STDMETHODCALLTYPE* UpdateSubresourceFn)(
    ID3D11DeviceContext*, ID3D11Resource*, UINT, const D3D11_BOX*,
    const void*, UINT, UINT);
static UpdateSubresourceFn g_origUpdateSubresource = nullptr;

static void STDMETHODCALLTYPE CalUpdateSubresourceHook(
    ID3D11DeviceContext* self, ID3D11Resource* res, UINT sub, const D3D11_BOX* box,
    const void* srcData, UINT srcRowPitch, UINT srcDepthPitch)
{
    g_origUpdateSubresource(self, res, sub, box, srcData, srcRowPitch, srcDepthPitch);
    if (!srcData) return;

    if (!g_haveProj) {
        // discovery: scan the staging data like the Unmap path
        // v21: g_nLive>=4 gate dropped — Structural+CalibrateProj math is the
        // real validator; the live-count heuristic could silently block
        // discovery forever.
        if (g_haveView) {
            static long s_scanOnce = 0;
            if (InterlockedCompareExchange(&s_scanOnce, 1, 0) == 0)
                EvLog("CAL: UpdateSubresource discovery scanning (first)");
            EnterCriticalSection(&g_cs);
            for (uint32_t off = 0; off + 64 <= CAL_SNAP_MAX; off += 16) {
                const float* m = (const float*)((const uint8_t*)srcData + off);
                float R[4][4];
                bool any = false;
                for (int r = 0; r < 4 && !any; r++)
                    for (int c = 0; c < 4 && !any; c++)
                        if (!Fin(m[r * 4 + c])) { any = true; }
                if (any) continue;
                for (int conv = 0; conv < 2; conv++) {
                    EffectiveRows(m, conv, R);
                    if (!Structural(R)) continue;
                    if (CalibrateProj(m, conv, res, off))
                        break;
                }
            }
            LeaveCriticalSection(&g_cs);
        }
        return;
    }

    // locked: destination == our world buffer → direct current-frame read
    if (res == g_slotRes && g_slotOff + 64 <= CAL_SNAP_MAX && g_haveView) {
        const float* m = (const float*)((const uint8_t*)srcData + g_slotOff);
        float R[4][4];
        EffectiveRows(m, g_slotConv, R);
        if (Structural(R)) {
            float A[3][3] = {
                { R[0][0], R[0][1], R[0][2] },
                { R[1][0], R[1][1], R[1][2] },
                { R[3][0], R[3][1], R[3][2] },
            };
            float b[3] = { -R[0][3], -R[1][3], -R[3][3] };
            float cam[3];
            if (Solve3(A, b, cam)) {
                float d[3] = { cam[0] - g_lockCam[0], cam[1] - g_lockCam[1], cam[2] - g_lockCam[2] };
                if (Len3(d) <= 3.f) {
                    memcpy(g_cbVP, R, sizeof g_cbVP);
                    g_cbVPValid = true;
                    g_cbVPAge = 0;
                    g_frameMatched = true;
                }
            }
        }
    }
}

bool Init(void* device, void* immediateContext)
{
    oilog::Line("VPINIT: entered");
    HMODULE nt = GetModuleHandleA("ntdll.dll");
    if (nt) g_NtRVM = (pfn_NtRVM)GetProcAddress(nt, "NtReadVirtualMemory");
    if (!g_NtRVM) { oilog::Line("VPINIT: FATAL NtReadVirtualMemory missing"); return false; }
    InitializeCriticalSectionAndSpinCount(&g_cs, 4000);
    g_csInit = true;

    // v20: OLD METHOD RE-ENABLED per user decision. The Map/Unmap/
    // UpdateSubresource calibration hooks draw the game's own viewProj
    // straight from its constant buffers — the method that DREW BOXES
    // (v12 era). Engine RenderView path stays as fallback only.
    if (immediateContext) {
        g_ctx = (ID3D11DeviceContext*)immediateContext;
        g_ctx->AddRef();
        void** vt = *(void***)g_ctx;
        g_origMap   = (MapFn)vt[14];
        g_origUnmap = (UnmapFn)vt[15];
        g_origUpdateSubresource = (UpdateSubresourceFn)vt[48];
        auto PatchPtr = [](void** slot, void* value) -> bool {
            DWORD old = 0;
            if (!VirtualProtect(slot, sizeof(void*), PAGE_READWRITE, &old)) return false;
            *slot = value;
            VirtualProtect(slot, sizeof(void*), old, &old);
            return true;
        };
        bool ok = PatchPtr(&vt[14], (void*)&CalMapHook) &&
                  PatchPtr(&vt[15], (void*)&CalUnmapHook) &&
                  PatchPtr(&vt[48], (void*)&CalUpdateSubresourceHook);
        oilog::Line(ok ? "VPINIT: CB hooks installed (Map/Unmap/UpdateSubresource)"
                       : "VPINIT: CB hook patch FAILED");
    } else {
        oilog::Line("VPINIT: no immediate context — CB hooks SKIPPED");
    }
    oilog::Line("VPINIT: ready (v21: CB primary, engine fallback)");
    return true;
}

// per-frame: refresh target positions, read the engine camera, validate.
void OnFrame()
{
    if (!g_csInit) return;
    EnterCriticalSection(&g_cs);

    // live proxy positions (kept for the status line + adoption sanity)
    float cx = 0.f, cy = 0.f, cz = 0.f;
    int live = 0;
    for (int i = 0; i < g_nT; i++) {
        float p[3] = { 0, 0, 0 };
        if (Rd(g_t[i].va + 0x70, p, 12) && (p[0] != 0.f || p[1] != 0.f || p[2] != 0.f)) {
            g_t[i].x = p[0]; g_t[i].y = p[1]; g_t[i].z = p[2];
            g_t[i].fails = 0;
            cx += p[0]; cy += p[1]; cz += p[2];
            live++;
        } else if (g_t[i].x == 0.f && g_t[i].y == 0.f && g_t[i].z == 0.f) {
            g_t[i].fails = 0;
        } else if (++g_t[i].fails > 300) {
            g_t[i] = g_t[--g_nT];
            i--;
        }
    }
    if (live > 0) {
        g_centroid[0] = cx / live; g_centroid[1] = cy / live; g_centroid[2] = cz / live;
    }
    g_nLive = live;

    // THE CAMERA — v6 MEMORY POLL (the definitive fix for stutter):
    // Previous designs depended on WHEN the game Maps/Unmaps the buffer —
    // and Frostbite's rotation made that unreliable (456 stale events in
    // one session even with the Map-time read). Now we don't wait for any
    // event: the Present hook runs AFTER this frame's rendering, so the
    // world VP the game just uploaded is ALREADY in the mapped memory.
    // Read it right here, validate, snapshot. Every frame, no misses.
    //   pData: refreshed by the Map hook whenever the world buffer cycles
    //   allocations (WRITE_DISCARD pool rotation) — the only thing Map is
    //   still needed for. Camera gates + structural checks keep garbage out.
    if (true) {
        // v20: the D3D constant-buffer poll is PRIMARY again (user call).
        // When the CB slot delivers this frame, its matrix is published and
        // the engine P*V rebuild below SKIPS (see g_cbVPValid gate) — the
        // engine path only fills gaps (CB stale > 10 frames / pre-lock).
        // v10 DUAL-SLOT ZOOM TRACKING (Super Brainz aim offset fix):
        // The zoom VP and the hipfire VP live in DIFFERENT buffers (the
        // game swaps the whole pass when you aim). v9 polled only the
        // proven hipfire slot with an 8 m gate — during zoom that slot
        // holds the STALE hipfire matrix (its camera is the old boom
        // position, within 8 m, so it kept winning) -> boxes rendered with
        // the hipfire camera while the screen showed the aim camera ->
        // parallax offset that grows with distance ("a lil to the top when
        // far away"). The engine view chain is the ground truth: the game's
        // current camera. So:
        //   slot A (hipfire, identity-proven): accepted while its matrix's
        //     camera matches the engine camera within 1.0 m, or during a
        //     short boom-swing (mismatch < 20 frames — one-frame lag on a
        //     fast jump is normal and recovers).
        //   When A mismatches > 20 frames (a persistent state = ZOOM), hunt
        //     slot B: any ring buffer at the locked offset whose camera
        //     matches the engine camera within 0.5 m — that is the aim
        //     camera's own VP, position-exact. Use B while zoomed, A when
        //     not. The weapon-camera cannot impersonate B: its camera sits
        //     at the character eye, not at the engine camera position.
        // v12: claim THIS frame's Map'd buffers (fresh pointers only — see
        // CandClaimFresh). We never touch pointers older than this frame,
        // so driver pool recycling can never fault us. Plus the circuit
        // breaker from v11 as a last resort.
        static DWORD s_ringDisarmUntil = 0;
        static int   s_ringFaultStreak = 0;
        DWORD nowTick = GetTickCount();
        if (nowTick >= s_ringDisarmUntil) {
        CandClaimFresh();
        float bestR[4][4];
        float bestDist = 1e30f;
        bool  haveBest = false;
        int   dropped = 0;

        // find A (locked world slot) and B (zoom slot) among THIS frame's buffers
        int idxA = -1, idxB = -1;
        for (int i = 0; i < g_nFresh; i++) {
            if (g_fresh[i].res == g_slotRes) idxA = i;
            else if (g_slotBRes && g_fresh[i].res == g_slotBRes) idxB = i;
        }

        // ---- evaluate A -------------------------------------------------
        float distA = 1e30f;
        bool  aOK = false;
        float Ra[4][4];
        if (idxA >= 0) {
            __try {
                const float* m = (const float*)((const uint8_t*)g_fresh[idxA].data + g_slotOff);
                EffectiveRows(m, g_slotConv, Ra);
                if (Structural(Ra)) {
                    float A[3][3] = {
                        { Ra[0][0], Ra[0][1], Ra[0][2] },
                        { Ra[1][0], Ra[1][1], Ra[1][2] },
                        { Ra[3][0], Ra[3][1], Ra[3][2] },
                    };
                    float b[3] = { -Ra[0][3], -Ra[1][3], -Ra[3][3] };
                    float cam[3];
                    if (Solve3(A, b, cam)) {
                        float d[3] = { cam[0]-g_lockCam[0], cam[1]-g_lockCam[1], cam[2]-g_lockCam[2] };
                        distA = Len3(d);
                        if (distA <= 1.0f || g_aMismatch < 20) aOK = true;   // fresh or transient swing
                    }
                }
            } __except (EXCEPTION_EXECUTE_HANDLER) {
                dropped++;
            }
        }
        if (aOK && distA > 1.0f) g_aMismatch++;
        else if (aOK)            g_aMismatch = 0;
        else if (idxA >= 0)      g_aMismatch++;   // structural/solve fail — mismatch too
        else                     g_aMismatch = 0; // A absent this frame — not a zoom signal

        // ---- unlocked: poll-time discovery on this frame's buffers -------
        if (!g_haveProj) {
            for (int i = 0; i < g_nFresh; i++) {
                if (g_fresh[i].width && g_slotOff + 64 > g_fresh[i].width) continue;
                __try {
                    const float* m = (const float*)((const uint8_t*)g_fresh[i].data + g_slotOff);
                    float R[4][4];
                    EffectiveRows(m, g_slotConv, R);
                    if (!Structural(R)) continue;
                    float A[3][3] = {
                        { R[0][0], R[0][1], R[0][2] },
                        { R[1][0], R[1][1], R[1][2] },
                        { R[3][0], R[3][1], R[3][2] },
                    };
                    float b[3] = { -R[0][3], -R[1][3], -R[3][3] };
                    float cam[3];
                    if (!Solve3(A, b, cam)) continue;
                    float d[3] = { cam[0]-g_lockCam[0], cam[1]-g_lockCam[1], cam[2]-g_lockCam[2] };
                    if (Len3(d) > 3.f) continue;
                    if (!LiveEntityNear(cam)) continue;
                    if (!CalibrateProj(m, g_slotConv, g_fresh[i].res, g_slotOff)) continue;
                    break;
                } __except (EXCEPTION_EXECUTE_HANDLER) {
                    dropped++;
                }
            }
        }
        // ---- locked: A fresh, or B when A is persistently stale (zoom) --
        else if (aOK) {
            memcpy(bestR, Ra, sizeof bestR);
            bestDist = distA;
            haveBest = true;
        } else {
            // A persistently stale — zoom. Try known B first...
            if (idxB >= 0) {
                __try {
                    const float* m = (const float*)((const uint8_t*)g_fresh[idxB].data + g_slotOff);
                    float R[4][4];
                    EffectiveRows(m, g_slotConv, R);
                    if (Structural(R)) {
                        float A[3][3] = {
                            { R[0][0], R[0][1], R[0][2] },
                            { R[1][0], R[1][1], R[1][2] },
                            { R[3][0], R[3][1], R[3][2] },
                        };
                        float b[3] = { -R[0][3], -R[1][3], -R[3][3] };
                        float cam[3];
                        if (Solve3(A, b, cam)) {
                            float d[3] = { cam[0]-g_lockCam[0], cam[1]-g_lockCam[1], cam[2]-g_lockCam[2] };
                            if (Len3(d) <= 0.5f) {
                                memcpy(bestR, R, sizeof bestR);
                                haveBest = true;
                            }
                        }
                    }
                } __except (EXCEPTION_EXECUTE_HANDLER) {
                    dropped++;
                }
            }
            // ...else HUNT the zoom VP among this frame's buffers: camera
            // must match the engine camera within 0.5 m (the aim camera's
            // own VP, position-exact).
            if (!haveBest) {
                for (int i = 0; i < g_nFresh; i++) {
                    if (i == idxA) continue;
                    if (g_fresh[i].width && g_slotOff + 64 > g_fresh[i].width) continue;
                    __try {
                        const float* m = (const float*)((const uint8_t*)g_fresh[i].data + g_slotOff);
                        float R[4][4];
                        EffectiveRows(m, g_slotConv, R);
                        if (!Structural(R)) continue;
                        float A[3][3] = {
                            { R[0][0], R[0][1], R[0][2] },
                            { R[1][0], R[1][1], R[1][2] },
                            { R[3][0], R[3][1], R[3][2] },
                        };
                        float b[3] = { -R[0][3], -R[1][3], -R[3][3] };
                        float cam[3];
                        if (!Solve3(A, b, cam)) continue;
                        float d[3] = { cam[0]-g_lockCam[0], cam[1]-g_lockCam[1], cam[2]-g_lockCam[2] };
                        if (Len3(d) > 0.5f) continue;
                        if (!LiveEntityNear(cam)) continue;
                        memcpy(bestR, R, sizeof bestR);
                        haveBest = true;
                        g_slotBRes = g_fresh[i].res;   // remember for next zoom
                        EvLog("VP EVENT: zoom slot adopted");
                        break;
                    } __except (EXCEPTION_EXECUTE_HANDLER) {
                        dropped++;
                    }
                }
            }
        }
        if (haveBest) {
            s_ringFaultStreak = 0;
            memcpy(g_cbVP, bestR, sizeof g_cbVP);
            g_cbVPValid = true;
            g_cbVPAge = 0;
            g_frameMatched = true;
        } else if (dropped > 0) {
            // faults this frame with no result — count toward the breaker
            if (++s_ringFaultStreak >= 3) {
                s_ringFaultStreak = 0;
                s_ringDisarmUntil = nowTick + 2000;
                g_cbVPValid = false;
                g_slotBRes = nullptr;
                EvLog("VP EVENT: camera reads disarmed 2s (fault streak) — P*V fallback");
            }
        } else {
            s_ringFaultStreak = 0;   // clean frame
        }
        } // circuit-breaker window
    }
    if (g_cbVPValid) {
        if (++g_cbVPAge > 10) {
            g_cbVPValid = false;
            EvLog("VP EVENT: direct VP stale — P*V fallback active");
        } else {
            memcpy(g_viewProj, g_cbVP, sizeof g_viewProj);
            g_haveMatrix = true;
            PublishCameraFromVP();   // v21: DebugList distances need a live cam
        }
    }

    // ---- per-frame bookkeeping (reset for the NEXT frame) --------------------
    {
        g_vpFrame++;
        if (!g_frameMatched) {
            if (++g_slotMissing == 60)
                EvLog("VP EVENT: world slot quiet — awaiting re-lock");
        } else {
            g_slotMissing = 0;
        }
        g_frameMatched = false;
    }

    // v15 CAMERA — scan the RenderView camera pool, lock the STABLE slot.
    // The world camera is the one whose eye barely moves frame-to-frame;
    // shadow/reflection slots jitter or flip every frame. The DIAG log
    // labels every slot against the pick (near/mirror/far) so the layout
    // is proven by evidence, not guesses.
    CamRead cams[4];
    int   nCams = 0;
    uint64_t viewsVA = 0;
    if (ReadCameras(cams, nCams, 4, viewsVA)) {
        g_engineCamFails = 0;

        // per-slot drift since last frame
        static float s_prevEye[4][3];
        static bool  s_havePrev[4];
        float drift[4] = { 0.f, 0.f, 0.f, 0.f };
        for (int i = 0; i < nCams; i++) {
            if (s_havePrev[i]) {
                float d3[3] = { cams[i].eye[0] - s_prevEye[i][0],
                                cams[i].eye[1] - s_prevEye[i][1],
                                cams[i].eye[2] - s_prevEye[i][2] };
                drift[i] = Len3(d3);
            }
        }

        // pick: keep a stable lock; on loss, prefer the slot matching the
        // last locked basis (camera identity survives slot moves), else
        // the calmest slot
        int pick = g_camLocked;
        if (pick < 0 || pick >= nCams || drift[pick] > 1.5f) {
            pick = -1;
            if (g_haveBasis) {
                for (int i = 0; i < nCams; i++)
                    if (Dot3(cams[i].fwd, g_lockFwd) > 0.985f &&
                        Dot3(cams[i].up,  g_lockUp)  > 0.985f) { pick = i; break; }
            }
            if (pick < 0) {
                float best = 1e30f;
                for (int i = 0; i < nCams; i++)
                    if (drift[i] < best) { best = drift[i]; pick = i; }
            }
        }

        static DWORD s_lastDiag = 0;
        bool switched = (pick != g_camLocked);
        if (switched) {  // new camera identity => re-measure z convention
            g_zFlipDone = 0; g_zVotes = 0; g_zIn = 0; g_zOut = 0;
        }
        if (switched || GetTickCount() - s_lastDiag > 5000) {
            s_lastDiag = GetTickCount();
            char b[240]; size_t k = 0;
            SCopy(b, "VP DIAG: pick=", k); FmtU(b, k, (unsigned long long)pick);
            SCopy(b, " slots=", k);        FmtU(b, k, (unsigned long long)nCams);
            for (int i = 0; i < nCams && k < 170; i++) {
                float de[3] = { cams[i].eye[0] - cams[pick].eye[0],
                                cams[i].eye[1] - cams[pick].eye[1],
                                cams[i].eye[2] - cams[pick].eye[2] };
                float dist = Len3(de);
                float bd = Dot3(cams[i].fwd, cams[pick].fwd);
                SCopy(b, " s", k); FmtU(b, k, (unsigned long long)i);
                SCopy(b, "@", k);  FmtHex(b, k, (unsigned long long)cams[i].off);
                if (dist < 4.f && bd > 0.95f)      SCopy(b, "=near", k);
                else if (dist < 4.f && bd < -0.9f) SCopy(b, "=mirror", k);
                else if (dist > 50.f)              SCopy(b, "=far", k);
                else                               SCopy(b, "=mid", k);
            }
            SCopy(b, " eye=(", k);
            FmtF(b, k, cams[pick].eye[0]); SCopy(b, ",", k);
            FmtF(b, k, cams[pick].eye[1]); SCopy(b, ",", k);
            FmtF(b, k, cams[pick].eye[2]); SCopy(b, ")", k);
            b[k] = 0;
            EvLog(b);
        }
        g_camLocked = pick;

        const CamRead& C = cams[pick];
        g_lockCam[0] = C.eye[0]; g_lockCam[1] = C.eye[1]; g_lockCam[2] = C.eye[2];
        g_view[0]  = C.right[0]; g_view[1]  = C.right[1]; g_view[2]  = C.right[2]; g_view[3]  = -Dot3(C.right, C.eye);
        g_view[4]  = C.up[0];    g_view[5]  = C.up[1];    g_view[6]  = C.up[2];    g_view[7]  = -Dot3(C.up, C.eye);
        // z row: zs=+1 uses the engine row AS STORED (T = -fwd.eye — the only
        // identity the dump could verify); zs=-1 after the v19 measurement
        // says the row is forward-named => classic back-basis view.
        float zs = g_zFlipDone ? -1.f : 1.f;
        g_view[8]  = zs * C.fwd[0];  g_view[9]  = zs * C.fwd[1];  g_view[10] = zs * C.fwd[2];
        g_view[11] = -zs * Dot3(C.fwd, C.eye);
        g_view[12] = 0.f;        g_view[13] = 0.f;        g_view[14] = 0.f;        g_view[15] = 1.f;
        g_haveView = true;
        g_haveBasis = true;
        memcpy(g_lockFwd, C.fwd, 12);
        memcpy(g_lockUp,   C.up,   12);
        for (int i = 0; i < nCams; i++) {
            memcpy(s_prevEye[i], cams[i].eye, 12);
            s_havePrev[i] = true;
        }

        // projection found by shape, wherever the game keeps it
        float projMem[16];
        if (FindProjection(viewsVA, projMem)) {
            memcpy(g_proj, projMem, sizeof g_proj);
            g_haveProj = true;
        }
        {
            static DWORD s_lastPDiag = 0;
            if (GetTickCount() - s_lastPDiag > 5000) {
                s_lastPDiag = GetTickCount();
                char b[160]; size_t k = 0;
                if (g_haveProj) {
                    SCopy(b, "VP DIAG2: proj@", k); FmtHex(b, k, g_projOff);
                    SCopy(b, " s=(", k); FmtF(b, k, g_proj[0]);
                    SCopy(b, ",", k);    FmtF(b, k, g_proj[5]); SCopy(b, ")", k);
                } else {
                    SCopy(b, "VP DIAG2: proj NOT FOUND in 0x520 struct", k);
                }
                b[k] = 0;
                EvLog(b);
            }
        }
        if (g_haveProj && !g_cbVPValid) {   // v20: CB matrix wins when live
                PublishCameraFromVP();   // v21: fallback path feeds the cam too
                // VP = P * V (row-major; RH view with z=back, reversed-z P:
                // w_clip = -z_view > 0 in front — WorldToScreen's w>0 test holds)
                for (int r = 0; r < 4; r++)
                    for (int c = 0; c < 4; c++) {
                        float s = 0.f;
                        for (int k = 0; k < 4; k++)
                            s += g_proj[r * 4 + k] * g_view[k * 4 + c];
                        g_viewProj[r * 4 + c] = s;
                    }
                g_haveMatrix = true;
                g_noCalibFrames = 0;
            } else {
                g_noCalibFrames++;
                if (g_noCalibFrames == 1 || g_noCalibFrames % 300 == 0)
                    EvLog("VP EVENT: projection unreadable — world-to-screen paused");
            }
    } else {
        if (++g_engineCamFails == 1 || g_engineCamFails % 60 == 0)
            EvLog("VP EVENT: engine camera chain unreadable");
    }

    LeaveCriticalSection(&g_cs);
}

void SetScreenSize(float w, float h)
{
    if (w > 1.f && h > 1.f) { g_scrW = w; g_scrH = h; }
}

bool HasViewProj() { return g_haveMatrix; }

// v19 Z-CONVENTION RESOLVER — the decisive fix for the v14-v18 "no ESP".
// Evidence chain: camera + projection + entities all healthy, yet 125 W2S
// calls in one second put ZERO points in front (v18 log). Geometry says a
// point that NDC-projects on-screen MUST be in front; the engine's own
// reversed-z projection (shape-verified, assumption-free) says in-front
// means z_view < 0 (w_clip = -z_view > 0). So: measure z_view's sign on
// on-screen points. Majority-positive => the camera-world's 3rd row is
// FORWARD-named, not back (the dump check T = -basis.eye is convention-
// blind, it cannot distinguish) => build the view with the row negated.
// Runs on the present thread (same as OnFrame) — no locking hazards.
// W2S health: matrix-valid calls only. all-calls/zero-success with a valid
// matrix = the silent all-behind cull signature (v18 evidence line).
static long   s_w2sCalls = 0, s_w2sOk = 0;
static DWORD  s_w2sBucket = 0;
static int    s_behindStreak = 0;

void W2SHealth(long* calls, long* ok) { *calls = s_w2sCalls; *ok = s_w2sOk; }

int ZFlip() { return g_zFlipDone; }

static void W2STick()
{
    DWORD b = GetTickCount() / 1000;
    if (b != s_w2sBucket) {
        if (s_w2sCalls > 30 && s_w2sOk == 0) {
            if (++s_behindStreak == 2)
                EvLog("VP EVENT: W2S 2s ALL-BEHIND — matrix valid but every point culled (w<=0): convention/struct suspect");
        } else if (s_w2sOk > 0) {
            s_behindStreak = 0;
        }
        s_w2sBucket = b;
        s_w2sCalls = 0;
        s_w2sOk = 0;
    }
}

bool WorldToScreen(float wx, float wy, float wz, float* sx, float* sy)
{
    W2STick();                 // advance 1 s bucket, evaluate the previous one
    if (!g_haveMatrix) return false;
    s_w2sCalls++;              // matrix-valid calls only — health means something
    float R[4][4];
    EffectiveRows(g_viewProj, 0, R);
    float w = R[3][0] * wx + R[3][1] * wy + R[3][2] * wz + R[3][3];
    float x = R[0][0] * wx + R[0][1] * wy + R[0][2] * wz + R[0][3];
    float y = R[1][0] * wx + R[1][1] * wy + R[1][2] * wz + R[1][3];

    // z-convention voting (see block comment): NDC magnitudes are unaffected
    // by a wrong z row (rows 0/1 never touch it), so on-screen-ness is a
    // trustworthy gate even while the convention is wrong. NO w>0 requirement
    // here — the broken state culls via w, the vote must still see the point.
    float zc = R[2][0] * wx + R[2][1] * wy + R[2][2] * wz + R[2][3];
    float nx = x / w, ny = y / w;
    if (fabsf(w) > 0.01f && fabsf(nx) < 0.9f && fabsf(ny) < 0.9f) {
        DWORD bkt = GetTickCount() / 2000;
        if (bkt != g_zWin) { g_zWin = bkt; g_zVotes = 0; g_zIn = 0; g_zOut = 0; }
        g_zVotes++;
        if (zc < 0.f) g_zIn++; else g_zOut++;
        if (!g_zFlipDone && g_zVotes >= 30 && g_zOut * 10 > g_zVotes * 9) {
            g_zFlipDone = 1;   // view rebuild applies zs = -1 next OnFrame tick
            EvLog("VP EVENT: on-screen points measured z_view>0 — engine row is FORWARD, view z row negated (v19 auto)");
        }
    }

    if (w <= 0.05f) return false;
    if (nx < -1.3f || nx > 1.3f || ny < -1.3f || ny > 1.3f) return false;
    *sx = (nx * 0.5f + 0.5f) * g_scrW;
    *sy = (1.f - (ny * 0.5f + 0.5f)) * g_scrH;
    s_w2sOk++;
    return true;
}

bool GetCamera(float cam[3])
{
    // v21: serve the published camera whenever it exists (CB path keeps
    // g_lockCam fresh even while g_haveMatrix flickers between frames).
    if (!g_csInit) return false;
    if (!g_haveMatrix && !g_lockCam[0] && !g_lockCam[1] && !g_lockCam[2]) return false;
    EnterCriticalSection(&g_cs);
    memcpy(cam, g_lockCam, sizeof g_lockCam);
    LeaveCriticalSection(&g_cs);
    return true;
}

int DebugDots(float* sx, float* sy, float* dist, int maxN)
{
    if (!g_csInit || !g_haveMatrix) return 0;
    int n = 0;
    EnterCriticalSection(&g_cs);
    float cam[3];
    memcpy(cam, g_lockCam, sizeof cam);

    for (int i = 0; i < g_nT && n < maxN; i++) {
        if (g_t[i].x == 0.f && g_t[i].y == 0.f && g_t[i].z == 0.f) continue;
        if (!WorldToScreen(g_t[i].x, g_t[i].y, g_t[i].z, &sx[n], &sy[n])) continue;
        float d[3] = { g_t[i].x - cam[0], g_t[i].y - cam[1], g_t[i].z - cam[2] };
        dist[n] = Len3(d);
        n++;
    }
    LeaveCriticalSection(&g_cs);
    return n;
}

const char* StatusText()
{
    static char b[160];
    if (!g_csInit) return "camera: off";
    if (!g_haveMatrix) return "camera: waiting (engine chain unread)";
    EnterCriticalSection(&g_cs);
    size_t k = 0;
    SCopy(b, "camera: ENGINE cam=(", k);
    FmtF(b, k, g_lockCam[0]); SCopy(b, ",", k);
    FmtF(b, k, g_lockCam[1]); SCopy(b, ",", k);
    FmtF(b, k, g_lockCam[2]); SCopy(b, ")", k);
    LeaveCriticalSection(&g_cs);
    b[k] = 0;
    return b;
}

} // namespace vproj
