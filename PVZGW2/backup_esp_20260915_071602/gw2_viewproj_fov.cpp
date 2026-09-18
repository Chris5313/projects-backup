// ---------------------------------------------------------------------------
// gw2_viewproj_fov.cpp — FOV-based WorldToScreen (simplified v22)
//
// This version reads ONLY the camera-world matrix from the game and builds
// the projection matrix ourselves using a fixed FOV. This eliminates:
// - Complex CB hook calibration
// - Multi-slot camera scanning  
// - Z-convention auto-detection
// - Projection matrix search
//
// The camera-world matrix at RenderView+0x2A0 is a 4x4 row-major matrix:
//   Row 0: camera right axis (unit vector)
//   Row 1: camera up axis (unit vector)
//   Row 2: camera forward axis (unit vector, points INTO scene)
//   Row 3: camera eye position (world coords), w=1
//
// We build:
//   VIEW = inverse of camera-world (transpose rotation + -eye·basis)
//   PROJ = perspective(fov=75°, aspect=w/h, near=0.1, far=5000)
//   VP = PROJ * VIEW (row-major)
// ---------------------------------------------------------------------------
#include "gw2_viewproj.h"
#include <windows.h>
#include <cstring>
#include <cmath>

// overlay logger
namespace oilog { void Line(const char*); }

namespace vproj {

// ---- NtReadVirtualMemory: fault-proof reads ---------------------------------
typedef LONG(WINAPI* pfn_NtRVM)(HANDLE, PVOID, PVOID, ULONG, PULONG);
static pfn_NtRVM g_NtRVM = nullptr;

static bool Rd(uint64_t va, void* dst, size_t n)
{
    if (!g_NtRVM) return false;
    ULONG got = 0;
    return g_NtRVM((HANDLE)-1, (PVOID)va, dst, (ULONG)n, &got) == 0 && got == (ULONG)n;
}

// ---- shared state -----------------------------------------------------------
static CRITICAL_SECTION g_cs;
static bool g_csInit = false;

static float g_viewProj[16];      // current VP matrix (row-major)
static float g_cam[3];            // camera eye position
static bool  g_haveMatrix = false;
static float g_scrW = 1920.f, g_scrH = 1080.f;

// ---- ENGINE CAMERA CHAIN (dump-verified) ------------------------------------
// static 0x142CEE730 -> CameraManager
// manager+0x68       -> RenderView[0]
// view[0]+0x2A0      -> camera-world matrix (64 bytes)
// view[0]+0x320      -> VIEW matrix (64 bytes) - backup
// view[0]+0x3E0      -> PROJECTION matrix (64 bytes) - for FOV extraction
static constexpr uint64_t CAM_STATIC     = 0x142CEE730ull;
static constexpr uint32_t CAM_MGR_VIEWS  = 0x68;
static constexpr uint32_t CAM_WORLD_OFF  = 0x2A0;  // camera-world matrix
static constexpr uint32_t CAM_VIEW_OFF   = 0x320;  // direct VIEW matrix
static constexpr uint32_t CAM_PROJ_OFF   = 0x3E0;  // game's projection

// FOV: GW2 uses ~75° vertical FOV (we can also extract from game's proj)
static float g_fovY = 75.0f * 3.14159265f / 180.0f;  // radians
static constexpr float NEAR_PLANE = 0.1f;
static constexpr float FAR_PLANE = 5000.0f;

// ---- math helpers -----------------------------------------------------------
static float Dot3(const float* a, const float* b)
{
    return a[0]*b[0] + a[1]*b[1] + a[2]*b[2];
}

static float Len3(const float* a)
{
    float d = Dot3(a, a);
    return d > 0.f ? sqrtf(d) : 0.f;
}

static bool Finite(float v)
{
    return v > -1e12f && v < 1e12f;
}

// Multiply two 4x4 row-major matrices: C = A * B
static void MatMul4x4(const float A[16], const float B[16], float C[16])
{
    for (int r = 0; r < 4; r++) {
        for (int c = 0; c < 4; c++) {
            float s = 0.f;
            for (int k = 0; k < 4; k++)
                s += A[r*4 + k] * B[k*4 + c];
            C[r*4 + c] = s;
        }
    }
}

// Build perspective projection matrix (row-major, RH, reversed-z like game)
// Game's projection has m[3][2] = -1, meaning w_clip = -z_view
static void BuildProjection(float P[16], float fovY, float aspect, float zn, float zf)
{
    float h = 1.f / tanf(fovY * 0.5f);  // cot(fov/2)
    float w = h / aspect;
    // Reversed-Z RH projection (matches game's +0x3E0)
    // For RH with -Z into screen: w_clip = -z_view
    memset(P, 0, 64);
    P[0]  = w;                        // [0][0] = x scale
    P[5]  = h;                        // [1][1] = y scale
    P[10] = zn / (zf - zn);           // [2][2] ~ 0 for large far
    P[11] = zn;                       // [2][3] = near plane
    P[14] = -1.f;                     // [3][2] = -1 for RH
}

// Extract FOV from game's projection matrix
static float ExtractFOV(const float proj[16])
{
    // proj[5] = cot(fov/2), so fov = 2 * atan(1/proj[5])
    if (proj[5] > 0.1f && proj[5] < 10.f) {
        return 2.f * atanf(1.f / proj[5]);
    }
    return 75.0f * 3.14159265f / 180.0f;  // default
}

// Build VIEW matrix from camera-world matrix
// Camera-world (row-major):
//   [rightX rightY rightZ 0]
//   [upX    upY    upZ    0]
//   [fwdX   fwdY   fwdZ   0]
//   [eyeX   eyeY   eyeZ   1]
// VIEW = inverse of camera-world
static bool BuildViewFromCamWorld(const float cw[16], float V[16], float eye[3])
{
    // Extract basis vectors and eye
    float right[3] = { cw[0],  cw[1],  cw[2]  };
    float up[3]    = { cw[4],  cw[5],  cw[6]  };
    float fwd[3]   = { cw[8],  cw[9],  cw[10] };
    eye[0] = cw[12]; eye[1] = cw[13]; eye[2] = cw[14];
    
    // Validate: finite values
    for (int i = 0; i < 3; i++)
        if (!Finite(right[i]) || !Finite(up[i]) || !Finite(fwd[i]) || !Finite(eye[i]))
            return false;
    
    // Validate: w components should be 0,0,0,1
    if (fabsf(cw[3]) > 0.01f || fabsf(cw[7]) > 0.01f || 
        fabsf(cw[11]) > 0.01f || fabsf(cw[15] - 1.f) > 0.01f)
        return false;
    
    // Validate: unit length basis vectors
    float lr = Len3(right), lu = Len3(up), lf = Len3(fwd);
    if (lr < 0.9f || lr > 1.1f || lu < 0.9f || lu > 1.1f || lf < 0.9f || lf > 1.1f)
        return false;
    
    // Validate: roughly orthogonal
    if (fabsf(Dot3(right, up)) > 0.1f || fabsf(Dot3(right, fwd)) > 0.1f ||
        fabsf(Dot3(up, fwd)) > 0.1f)
        return false;
    
    // Validate: eye in reasonable range
    if (fabsf(eye[0]) > 100000.f || fabsf(eye[1]) > 100000.f || fabsf(eye[2]) > 100000.f)
        return false;
    
    // Build VIEW matrix: transpose of rotation + translation
    // For an orthonormal basis, inverse rotation = transpose
    // V = | R^T   -R^T * eye |
    //     | 0      1         |
    // Note: For RH with camera looking down -Z, we negate fwd
    V[0] = right[0];  V[1] = up[0];  V[2]  = -fwd[0];  V[3]  = 0;
    V[4] = right[1];  V[5] = up[1];  V[6]  = -fwd[1];  V[7]  = 0;
    V[8] = right[2];  V[9] = up[2];  V[10] = -fwd[2];  V[11] = 0;
    
    // For RH, the translation is computed with negated fwd
    float negFwd[3] = { -fwd[0], -fwd[1], -fwd[2] };
    V[12] = -Dot3(right, eye);
    V[13] = -Dot3(up, eye);
    V[14] = -Dot3(negFwd, eye);  // This gives +Dot3(fwd, eye)
    V[15] = 1.f;
    
    return true;
}

// ---- logging ----------------------------------------------------------------
#ifdef GW2_LOG_PATH
static HANDLE g_hLog = INVALID_HANDLE_VALUE;
static void Log(const char* msg)
{
    if (g_hLog == INVALID_HANDLE_VALUE) {
        g_hLog = CreateFileA(GW2_LOG_PATH, FILE_APPEND_DATA,
            FILE_SHARE_READ | FILE_SHARE_WRITE, nullptr, OPEN_ALWAYS,
            FILE_ATTRIBUTE_NORMAL, nullptr);
    }
    if (g_hLog == INVALID_HANDLE_VALUE) return;
    char buf[512];
    size_t n = 0;
    while (msg[n] && n < 500) { buf[n] = msg[n]; n++; }
    buf[n++] = '\r'; buf[n++] = '\n';
    DWORD w = 0;
    WriteFile(g_hLog, buf, (DWORD)n, &w, nullptr);
}
#else
#define Log(m) ((void)0)
#endif

static void FmtFloat(char* buf, size_t& k, float v)
{
    if (v < 0) { buf[k++] = '-'; v = -v; }
    int ip = (int)v;
    int fp = (int)((v - ip) * 100);
    char t[16]; int tn = 0;
    if (ip == 0) t[tn++] = '0';
    while (ip && tn < 10) { t[tn++] = '0' + ip % 10; ip /= 10; }
    while (tn) buf[k++] = t[--tn];
    buf[k++] = '.';
    buf[k++] = '0' + fp / 10;
    buf[k++] = '0' + fp % 10;
}

// ---- target tracking (for entity distances) ---------------------------------
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

// ---- public API -------------------------------------------------------------
bool Init(void* device, void* immediateContext)
{
    (void)device; (void)immediateContext;
    
    oilog::Line("VPINIT: FOV-based v22 starting");
    
    HMODULE nt = GetModuleHandleA("ntdll.dll");
    if (nt) g_NtRVM = (pfn_NtRVM)GetProcAddress(nt, "NtReadVirtualMemory");
    if (!g_NtRVM) {
        oilog::Line("VPINIT: FATAL NtReadVirtualMemory missing");
        return false;
    }
    
    InitializeCriticalSectionAndSpinCount(&g_cs, 4000);
    g_csInit = true;
    
    oilog::Line("VPINIT: ready (FOV-based projection, RH convention)");
    return true;
}

void OnFrame()
{
    if (!g_csInit) return;
    EnterCriticalSection(&g_cs);
    
    // Update target positions
    for (int i = 0; i < g_nT; i++) {
        float p[3] = { 0, 0, 0 };
        if (Rd(g_t[i].va + 0x70, p, 12) && (p[0] != 0.f || p[1] != 0.f || p[2] != 0.f)) {
            g_t[i].x = p[0]; g_t[i].y = p[1]; g_t[i].z = p[2];
            g_t[i].fails = 0;
        } else if (++g_t[i].fails > 300) {
            g_t[i] = g_t[--g_nT];
            i--;
        }
    }
    
    // Read camera chain
    static int s_fails = 0;
    static DWORD s_lastLog = 0;
    
    uint64_t mgr = 0, views = 0;
    bool ok = false;
    
    if (Rd(CAM_STATIC, &mgr, 8) && mgr >= 0x10000ull && mgr < 0x800000000000ull) {
        if (Rd(mgr + CAM_MGR_VIEWS, &views, 8) && views >= 0x10000ull && views < 0x800000000000ull) {
            
            // Try to read game's projection to extract FOV
            float gameProj[16];
            if (Rd(views + CAM_PROJ_OFF, gameProj, 64)) {
                float extractedFov = ExtractFOV(gameProj);
                if (fabsf(extractedFov - g_fovY) > 0.1f) {
                    g_fovY = extractedFov;
                }
            }
            
            // Try reading camera-world at +0x2A0
            float camWorld[16];
            if (Rd(views + CAM_WORLD_OFF, camWorld, 64)) {
                float viewMat[16], projMat[16];
                float eye[3];
                if (BuildViewFromCamWorld(camWorld, viewMat, eye)) {
                    float aspect = g_scrW / g_scrH;
                    BuildProjection(projMat, g_fovY, aspect, NEAR_PLANE, FAR_PLANE);
                    
                    // VP = PROJ * VIEW
                    MatMul4x4(projMat, viewMat, g_viewProj);
                    
                    g_cam[0] = eye[0];
                    g_cam[1] = eye[1];
                    g_cam[2] = eye[2];
                    
                    g_haveMatrix = true;
                    ok = true;
                    s_fails = 0;
                }
            }
            
            // If camera-world failed, try indirect descriptor
            if (!ok) {
                uint64_t probe[2] = { 0, 0 };
                if (Rd(views, probe, 16) && probe[0] >= 0x10000ull && probe[0] < 0x800000000000ull && probe[0] != views) {
                    float camWorld[16];
                    if (Rd(probe[0] + CAM_WORLD_OFF, camWorld, 64)) {
                        float viewMat[16], projMat[16];
                        float eye[3];
                        if (BuildViewFromCamWorld(camWorld, viewMat, eye)) {
                            float aspect = g_scrW / g_scrH;
                            BuildProjection(projMat, g_fovY, aspect, NEAR_PLANE, FAR_PLANE);
                            MatMul4x4(projMat, viewMat, g_viewProj);
                            g_cam[0] = eye[0]; g_cam[1] = eye[1]; g_cam[2] = eye[2];
                            g_haveMatrix = true;
                            ok = true;
                            s_fails = 0;
                        }
                    }
                }
            }
        }
    }
    
    if (!ok) {
        s_fails++;
        if (s_fails == 1 || (GetTickCount() - s_lastLog > 5000)) {
            s_lastLog = GetTickCount();
            oilog::Line("VP: camera chain read failed");
            g_haveMatrix = false;
        }
    } else if (GetTickCount() - s_lastLog > 10000) {
        // Periodic status log
        s_lastLog = GetTickCount();
        char buf[160];
        size_t k = 0;
        const char* m = "VP: OK eye=(";
        while (*m) buf[k++] = *m++;
        FmtFloat(buf, k, g_cam[0]); buf[k++] = ',';
        FmtFloat(buf, k, g_cam[1]); buf[k++] = ',';
        FmtFloat(buf, k, g_cam[2]); buf[k++] = ')';
        m = " fov=";
        while (*m) buf[k++] = *m++;
        FmtFloat(buf, k, g_fovY * 180.f / 3.14159265f);
        buf[k] = 0;
        oilog::Line(buf);
    }
    
    LeaveCriticalSection(&g_cs);
}

void SetScreenSize(float w, float h)
{
    if (w > 1.f && h > 1.f) {
        g_scrW = w;
        g_scrH = h;
    }
}

bool HasViewProj()
{
    return g_haveMatrix;
}

bool WorldToScreen(float wx, float wy, float wz, float* sx, float* sy)
{
    if (!g_haveMatrix) return false;
    
    // Transform: clip = VP * world (row-major)
    float x = g_viewProj[0]*wx + g_viewProj[1]*wy + g_viewProj[2]*wz + g_viewProj[3];
    float y = g_viewProj[4]*wx + g_viewProj[5]*wy + g_viewProj[6]*wz + g_viewProj[7];
    float w = g_viewProj[12]*wx + g_viewProj[13]*wy + g_viewProj[14]*wz + g_viewProj[15];
    
    // Behind camera? (w should be positive for points in front)
    if (w <= 0.05f) return false;
    
    // NDC
    float nx = x / w;
    float ny = y / w;
    
    // Off-screen?
    if (nx < -1.3f || nx > 1.3f || ny < -1.3f || ny > 1.3f) return false;
    
    // Convert to pixels
    *sx = (nx * 0.5f + 0.5f) * g_scrW;
    *sy = (1.f - (ny * 0.5f + 0.5f)) * g_scrH;  // Y is flipped for screen coords
    
    return true;
}

void W2SHealth(long* calls, long* ok)
{
    // Stub for compatibility
    *calls = 0;
    *ok = 0;
}

int ZFlip()
{
    return 0;  // We control the convention, no flipping needed
}

bool GetCamera(float cam[3])
{
    if (!g_csInit || !g_haveMatrix) return false;
    EnterCriticalSection(&g_cs);
    cam[0] = g_cam[0];
    cam[1] = g_cam[1];
    cam[2] = g_cam[2];
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
    size_t k = 0;
    const char* m = "camera: eye=(";
    while (*m) buf[k++] = *m++;
    FmtFloat(buf, k, g_cam[0]); buf[k++] = ',';
    FmtFloat(buf, k, g_cam[1]); buf[k++] = ',';
    FmtFloat(buf, k, g_cam[2]); buf[k++] = ')';
    buf[k] = 0;
    LeaveCriticalSection(&g_cs);
    return buf;
}

} // namespace vproj
