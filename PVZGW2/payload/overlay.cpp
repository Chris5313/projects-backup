// ---------------------------------------------------------------------------
// GW2 overlay — ImGui (DX11) rendering over the hooked Present.
// Uses the israeliclient 2.0 UI framework for styled widgets and tabs.
// ---------------------------------------------------------------------------
#include "../vendor/minhook/include/MinHook.h"
#include <windows.h>
#include <d3d11.h>
#include "imgui.h"
#include "imgui_impl_dx11.h"

extern IMGUI_IMPL_API LRESULT ImGui_ImplWin32_WndProcHandler(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam);
#include "imgui_impl_win32.h"

#include "framework/gui_gw2.h"
#include "framework/functions.h"

#include "gw2_viewproj.h"
#include "gw2_entities.h"
#include "gw2_trigger.h"
#include "gw2_aimbot.h"

namespace autodump { const char* StatusText(); }   // main.cpp — F3 pair state

// --- init-only logging (NO per-frame I/O — causes kernel object churn/BSOD) --
// v21: EXPORTED — gw2_viewproj's own Log() went silent (no VPINIT lines in
// any recent build); Init/hook one-shot facts now use this proven writer.
void OILog(const char* msg)
{
    HANDLE h = CreateFileA("C:\\Users\\Public\\gw2_payload.log", FILE_APPEND_DATA,
        FILE_SHARE_READ | FILE_SHARE_WRITE, nullptr, OPEN_ALWAYS,
        FILE_ATTRIBUTE_NORMAL, nullptr);
    if (h != INVALID_HANDLE_VALUE) {
        SetFilePointer(h, 0, nullptr, FILE_END);
        DWORD w = 0;
        size_t len = 0; while (msg[len]) len++;
        WriteFile(h, msg, (DWORD)len, &w, nullptr);
        WriteFile(h, "\r\n", 2, &w, nullptr);
        CloseHandle(h);
    }
}

static void OILogClear()
{
    HANDLE h = CreateFileA("C:\\Users\\Public\\gw2_payload.log", GENERIC_WRITE,
        FILE_SHARE_READ | FILE_SHARE_WRITE, nullptr, CREATE_ALWAYS,
        FILE_ATTRIBUTE_NORMAL, nullptr);
    if (h != INVALID_HANDLE_VALUE) CloseHandle(h);
}

namespace oilog { void Line(const char* m) { OILog(m); } }

static bool g_initialized = false;
static bool g_menuOpen    = false;
static int  g_warmup      = 0;

static HWND    g_hwnd = nullptr;
static WNDPROC g_origWndProc = nullptr;
static ID3D11RenderTargetView*  g_rtv = nullptr;

static LRESULT CALLBACK HookWndProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam)
{
    if (g_initialized)
    {
        ImGui_ImplWin32_WndProcHandler(hwnd, msg, wParam, lParam);

        // When menu is open, block mouse and keyboard input from reaching the game
        if (g_menuOpen)
        {
            ImGuiIO& io = ImGui::GetIO();
            
            // Block mouse messages when ImGui wants mouse
            if (io.WantCaptureMouse && (msg >= WM_MOUSEFIRST && msg <= WM_MOUSELAST))
                return 1;
            
            // Block keyboard messages when ImGui wants keyboard
            if (io.WantCaptureKeyboard && (msg >= WM_KEYFIRST && msg <= WM_KEYLAST))
                return 1;
            
            // Block raw input too (some games use this)
            if (msg == WM_INPUT)
                return 0;
        }
    }
    return CallWindowProcW(g_origWndProc, hwnd, msg, wParam, lParam);
}
// --- GetAsyncKeyState / GetKeyState hooks ---
// Games often poll input directly via GetAsyncKeyState (bypassing WndProc).
// When menu is open, return 0 for ALL keys except our own menu toggle (F1)
// so game doesn't see ESC, movement keys, mouse buttons, etc.
typedef SHORT (WINAPI *GetAsyncKeyState_t)(int);
typedef SHORT (WINAPI *GetKeyState_t)(int);
static GetAsyncKeyState_t g_origGetAsyncKeyState = nullptr;
static GetKeyState_t      g_origGetKeyState      = nullptr;

static SHORT WINAPI HookGetAsyncKeyState(int vKey)
{
    // Menu open + not our F1 toggle → hide from game
    if (g_menuOpen && vKey != 0x70 /* VK_F1 */)
        return 0;
    return g_origGetAsyncKeyState(vKey);
}

static SHORT WINAPI HookGetKeyState(int vKey)
{
    if (g_menuOpen && vKey != 0x70)
        return 0;
    return g_origGetKeyState(vKey);
}

static void InstallInputHooks()
{
    HMODULE user32 = GetModuleHandleA("user32.dll");
    if (!user32) return;
    void* pGAKS = (void*)GetProcAddress(user32, "GetAsyncKeyState");
    void* pGKS  = (void*)GetProcAddress(user32, "GetKeyState");
    if (pGAKS && MH_CreateHook(pGAKS, (void*)&HookGetAsyncKeyState, (void**)&g_origGetAsyncKeyState) == MH_OK)
        MH_EnableHook(pGAKS);
    if (pGKS && MH_CreateHook(pGKS, (void*)&HookGetKeyState, (void**)&g_origGetKeyState) == MH_OK)
        MH_EnableHook(pGKS);
    OILog("OI: input hooks installed");
}


bool Overlay_Init(void* swapchain, HWND hwnd)
{
    if (g_initialized) return true;

    OILogClear();
    OILog("OI: begin");
    OILog("OI: build v33 (measured view resolver — 16 combos, score-lock)");

    IDXGISwapChain* sc = (IDXGISwapChain*)swapchain;
    ID3D11Device* device = nullptr;
    ID3D11DeviceContext* ctx = nullptr;

    if (FAILED(sc->GetDevice(__uuidof(ID3D11Device), (void**)&device))) {
        OILog("OI: GetDevice FAILED"); return false;
    }
    device->GetImmediateContext(&ctx);

    g_hwnd = hwnd;
    if (!g_hwnd) g_hwnd = GetActiveWindow();
    if (!g_hwnd) g_hwnd = GetForegroundWindow();
    if (!g_hwnd) { OILog("OI: no hwnd"); device->Release(); ctx->Release(); return false; }

    IMGUI_CHECKVERSION();
    ImGui::CreateContext();
    ImGui::StyleColorsDark();
    ImGui_ImplWin32_Init(g_hwnd);
    ImGui_ImplDX11_Init(device, ctx);

    g_origWndProc = (WNDPROC)SetWindowLongPtrW(g_hwnd, GWLP_WNDPROC, (LONG_PTR)HookWndProc);

    g_pd3dDevice = device;
    g_pd3dDeviceContext = ctx;
    g_pSwapChain = sc;

    // hook Map/Unmap/UpdateSubresource on the immediate context for the
    // runtime viewProj capture (see gw2_viewproj.cpp)
    vproj::Init(device, ctx);

    GW2_Framework_Init(device);

    InstallInputHooks();

    g_initialized = true;
    OILog("OI: complete");
    return true;
}

void Overlay_Shutdown()
{
    if (!g_initialized) return;
    g_initialized = false;

    GW2_Framework_Shutdown();

    if (g_hwnd && g_origWndProc)
        SetWindowLongPtrW(g_hwnd, GWLP_WNDPROC, (LONG_PTR)g_origWndProc);
    g_origWndProc = nullptr;
    g_hwnd = nullptr;

    if (g_rtv) { g_rtv->Release(); g_rtv = nullptr; }

    ImGui_ImplDX11_Shutdown();
    ImGui_ImplWin32_Shutdown();
    ImGui::DestroyContext();

    if (g_pd3dDeviceContext) { g_pd3dDeviceContext->Release(); g_pd3dDeviceContext = nullptr; }
    if (g_pd3dDevice) { g_pd3dDevice->Release(); g_pd3dDevice = nullptr; }
    g_pSwapChain = nullptr;
}

bool Overlay_IsMenuOpen() { return g_menuOpen; }
void Overlay_ToggleMenu() { g_menuOpen = !g_menuOpen; }

// ---- entity ESP layer (always rendered, even with the menu closed) ---------
// Box ESP from world positions: feet (proxy origin) to head (+1.7 m),
// projected through the captured viewProj. Team color from the class string
// ('Z'/'P' side char — works in Garden Ops AND PvP). Toggle + style live in
// the Visuals tab (var->c_esp). Local player gets a cyan diamond instead of
// a box (the 3rd-person camera orbits YOU — nearest non-AI entity).
// v19 FIX: the old label copy was  while (t[k]) { b[k] = t[k]; k++; }  —
// it started reading EACH label at the buffer offset k (nonzero), copying
// garbage from adjacent rdata. Every DIAG line since v17 was corrupted
// (missing labels, mojibake). DiagStr resets k per label.
static void DiagStr(char* b, size_t& k, const char* t)
{
    while (*t && k < 180) b[k++] = *t++;   // APPEND (v20: resetting k truncated
}                                          // every line to its last label)

static void DiagNum(char* b, size_t& k, int v)
{
    char t[12]; int tn = 0;
    if (v == 0) t[tn++] = '0';
    while (v > 0 && tn < 11) { t[tn++] = (char)('0' + v % 10); v /= 10; }
    while (tn) b[k++] = t[--tn];
}

static void DrawEsp()
{
    // v24 DIAG: log entry every 2s to confirm we're being called
    static DWORD s_entryLog = 0;
    DWORD now = GetTickCount();
    if (now - s_entryLog >= 2000) {
        s_entryLog = now;
        char b[128];
        const char* p = "ESP ENTRY: var=";
        int k = 0; while (*p) b[k++] = *p++;
        b[k++] = var ? 'Y' : 'N';
        p = " enable=";
        while (*p) b[k++] = *p++;
        b[k++] = (var && var->c_esp.enable) ? 'Y' : 'N';
        p = " hasvp=";
        while (*p) b[k++] = *p++;
        b[k++] = vproj::HasViewProj() ? 'Y' : 'N';
        b[k] = 0;
        OILog(b);
    }
    
    if (!var || !var->c_esp.enable) return;

    float wx[64], wy[64], wz[64], ds[64];
    int   team[64], loc[64], ai[64], fresh[64];
    int n = ent::DebugList(wx, wy, wz, ds, team, loc, ai, fresh, 64);
    ImDrawList* dl = ImGui::GetBackgroundDrawList();

    // v17 draw funnel: how far each entity gets before a gate culls it.
    // list > dist > gates (self/fresh/dedup/filter) > proj (W2S) > drawn.
    int fDist = 0, fGates = 0, fW2S = 0, fDrawn = 0;

    // local team for the enemies/teammates filter (fallback: any known team)
    int localTeam = -1, localIdx = -1;
    for (int i = 0; i < n; i++) if (loc[i]) { localTeam = team[i]; localIdx = i; break; }

    // Never draw your own character. Your player owns TWO co-located
    // charObjs (physics twin, dump9) but only ONE row carries the local
    // flag — and the twin's faction string may not have resolved yet
    // (team = -1). Robust exclusion: ANY non-AI entity within 3.5 m of
    // the local row is you (twin), regardless of its team state; if no
    // local row exists yet, exclude the non-AI entity nearest the camera
    // (the 3rd-person camera orbits you — that nearest entity is you).
    float lx = 0, ly = 0, lz = 0;
    bool haveLocal = localIdx >= 0;
    if (haveLocal) { lx = wx[localIdx]; ly = wy[localIdx]; lz = wz[localIdx]; }
    else {
        // fallback: nearest non-AI entity to the camera = self
        float cam[3];
        if (vproj::GetCamera(cam)) {
            float best = 1e30f;
            for (int i = 0; i < n; i++) {
                if (ai[i]) continue;
                float dx = wx[i] - cam[0], dy = wy[i] - cam[1], dz = wz[i] - cam[2];
                float dd = dx*dx + dy*dy + dz*dz;
                if (dd < best) { best = dd; lx = wx[i]; ly = wy[i]; lz = wz[i]; haveLocal = true; }
            }
        }
    }
    // v26 DEBUG: log camera + first entity position every 4s
    static DWORD s_posLog = 0;
    if (n > 0 && GetTickCount() - s_posLog >= 4000) {
        s_posLog = GetTickCount();
        char pb[300]; int pk = 0;
        
        // Camera position
        float cam[3] = {0,0,0};
        bool haveCam = vproj::GetCamera(cam);
        const char* hdr = "ESP_DBG: cam=";
        while (*hdr) pb[pk++] = *hdr++;
        if (haveCam) {
            pk += sprintf_s(pb+pk, 100, "(%.1f,%.1f,%.1f)", cam[0], cam[1], cam[2]);
        } else {
            hdr = "NONE"; while (*hdr) pb[pk++] = *hdr++;
        }
        
        // First entity position
        hdr = " ent0=";
        while (*hdr) pb[pk++] = *hdr++;
        pk += sprintf_s(pb+pk, 100, "(%.1f,%.1f,%.1f)", wx[0], wy[0], wz[0]);
        
        // First entity team + distance
        hdr = " tm=";
        while (*hdr) pb[pk++] = *hdr++;
        pk += sprintf_s(pb+pk, 50, "%d d=%.0f ai=%d", team[0], ds[0], ai[0]);
        
        // max_dist setting
        hdr = " maxd=";
        while (*hdr) pb[pk++] = *hdr++;
        pk += sprintf_s(pb+pk, 20, "%d", var->c_esp.max_dist);
        
        pb[pk] = 0;
        OILog(pb);
    }


    for (int i = 0; i < n; i++) {
        if (ds[i] > (float)var->c_esp.max_dist) continue;
        ++fDist;

        // never draw yourself (flagged row, twin near the local row, or
        // nearest-to-camera fallback row)
        if (loc[i]) continue;
        if (haveLocal && !ai[i]) {
            float dx = wx[i] - lx, dy = wy[i] - ly, dz = wz[i] - lz;
            if (dx*dx + dy*dy + dz*dz < 12.25f) continue;   // < 3.5 m = you/twin
        }
        // GHOST FILTER (9/3 "two boxes inside one NPC, wrong color"): dead
        // entities get new charObjs at fresh addresses, and the game REUSES
        // the old row's memory — the old row then reads live-looking data
        // (stale cached team) and draws a second, wrong-colored box on the
        // same body. fresh[] = position moved within the last ~4 s. v18:
        // gate is now a TOGGLE (require_fresh, default OFF) — the proxy-
        // read failure mode all-or-nothing culled every box, the "no ESP
        // with everything healthy" signature of v14-v17.
        if (var->c_esp.require_fresh && !fresh[i]) continue;
        ++fGates;
        // TWIN DEDUP (9/3 "2 boxes inside each other"): Garden Ops zombies
        // own TWO charObj rows at the same position (AI entity pairs, both
        // read live-looking motion). First-wins: if an earlier row this
        // frame already drew within 2 m of this one, skip. Covers twins,
        // respawn overlaps, and camera-parallax coincidences.
        {
            float ex = wx[i], ey = wy[i], ez = wz[i];
            bool dup = false;
            for (int j = 0; j < i; j++) {
                // apply the same gates row j had to pass to have drawn
                if (loc[j] || !fresh[j]) continue;
                if (ds[j] > (float)var->c_esp.max_dist) continue;
                if (haveLocal && !ai[j] &&
                    (wx[j]-lx)*(wx[j]-lx)+(wy[j]-ly)*(wy[j]-ly)+(wz[j]-lz)*(wz[j]-lz) < 12.25f) continue;
                if (var->c_esp.filter == 1 && localTeam >= 0 && team[j] == localTeam) continue;
                if (var->c_esp.filter == 2 && !(localTeam >= 0 && team[j] == localTeam)) continue;
                float ddx = wx[j]-ex, ddy = wy[j]-ey, ddz = wz[j]-ez;
                if (ddx*ddx + ddy*ddy + ddz*ddz < 4.f) { dup = true; break; }   // < 2 m
            }
            if (dup) continue;
        }
        // filter: 0 all / 1 enemies / 2 teammates
        if (var->c_esp.filter == 1 && localTeam >= 0 && team[i] == localTeam) continue;
        if (var->c_esp.filter == 2 && !(localTeam >= 0 && team[i] == localTeam)) continue;
        if (team[i] < 0) continue;  // skip unknown team (no yellow boxes)

        // project feet + head anchors
        float fsx, fsy, hsx, hsy;
        if (!vproj::WorldToScreen(wx[i], wy[i], wz[i], &fsx, &fsy)) continue;
        if (!vproj::WorldToScreen(wx[i], wy[i] + 1.7f, wz[i], &hsx, &hsy)) continue;
        ++fW2S;

        float hgt = fsy - hsy;                     // pixels: feet below head
        float xc  = (fsx + hsx) * 0.5f;

        // team color from the menu pickers (alpha included)
        const float* cf = var->c_esp.unknown_color;
        if (team[i] == 0)      cf = var->c_esp.plant_color;
        else if (team[i] == 1) cf = var->c_esp.zombie_color;
        ImU32 col = ImGui::ColorConvertFloat4ToU32(ImVec4(cf[0], cf[1], cf[2], cf[3]));
        if (ai[i]) {
            float a = var->c_esp.npc_alpha * cf[3];
            if (a < 0.01f) a = 0.01f;
            col = (col & 0x00FFFFFF) | ((ImU32)(a * 255.f) << 24);
        }
        if (ai[i] && !var->c_esp.show_ai) continue;
        
        // Debug: log why entities aren't drawn
        static DWORD s_hgtLog = 0;
        if (hgt < 3.f && GetTickCount() - s_hgtLog > 5000) {
            s_hgtLog = GetTickCount();
            char hb[200];
            snprintf(hb, sizeof(hb), "HGT_SKIP: hgt=%.2f fsy=%.1f hsy=%.1f d=%.0f ai=%d show_ai=%d",
                hgt, fsy, hsy, ds[i], ai[i], var->c_esp.show_ai);
            OILog(hb);
        }
        if (hgt < 3.f) continue;                   // degenerate at range

        float w = hgt * 0.55f;
        ImVec2 bmin(xc - w * 0.5f, hsy), bmax(xc + w * 0.5f, fsy);
        ++fDrawn;

        // v43 DEBUG: log first drawn box details every 5s
        static DWORD s_boxLog = 0;
        if (fDrawn == 1 && GetTickCount() - s_boxLog >= 5000) {
            s_boxLog = GetTickCount();
            char boxb[200];
            snprintf(boxb, sizeof(boxb),
                "BOX_DBG: world=(%.1f,%.1f,%.1f) scr=(%.0f,%.0f)-(%.0f,%.0f) h=%.1f d=%.0fm tm=%d",
                wx[i], wy[i], wz[i], bmin.x, bmin.y, bmax.x, bmax.y, hgt, ds[i], team[i]);
            OILog(boxb);
        }

        if (var->c_esp.box_type == 1) {
            // corner box: 4 L-shaped corners, 30% arm length
            float ax = w * 0.3f, ay = hgt * 0.3f;
            dl->AddRect(bmin, bmax, IM_COL32(0, 0, 0, 120), 0.f, 0, 3.0f);
            dl->AddLine(ImVec2(bmin.x, bmin.y), ImVec2(bmin.x + ax, bmin.y), col, 2.f);
            dl->AddLine(ImVec2(bmin.x, bmin.y), ImVec2(bmin.x, bmin.y + ay), col, 2.f);
            dl->AddLine(ImVec2(bmax.x, bmin.y), ImVec2(bmax.x - ax, bmin.y), col, 2.f);
            dl->AddLine(ImVec2(bmax.x, bmin.y), ImVec2(bmax.x, bmin.y + ay), col, 2.f);
            dl->AddLine(ImVec2(bmin.x, bmax.y), ImVec2(bmin.x + ax, bmax.y), col, 2.f);
            dl->AddLine(ImVec2(bmin.x, bmax.y), ImVec2(bmin.x, bmax.y - ay), col, 2.f);
            dl->AddLine(ImVec2(bmax.x, bmax.y), ImVec2(bmax.x - ax, bmax.y), col, 2.f);
            dl->AddLine(ImVec2(bmax.x, bmax.y), ImVec2(bmax.x, bmax.y - ay), col, 2.f);
        } else {
            // 2D box: optional translucent fill + border
            if (var->c_esp.fill_box)
                dl->AddRectFilled(bmin, bmax, (col & 0x00FFFFFF) | 0x28000000);
            dl->AddRect(bmin, bmax, col, 0.f, 0, 1.5f);
        }

        if (var->c_esp.distance) {
            int d = (int)(ds[i] + 0.5f);
            char txt[16]; int k = 0;
            char t[12]; int tn = 0;
            if (d == 0) t[tn++] = '0';
            while (d > 0 && tn < 11) { t[tn++] = (char)('0' + d % 10); d /= 10; }
            while (tn) txt[k++] = t[--tn];
            txt[k++] = 'm'; txt[k] = 0;
            dl->AddText(ImVec2(xc - 10, bmax.y + 3), IM_COL32(255, 255, 255, 220), txt);
        }
    }

    // v24 funnel line every 3 s — names the exact stage where boxes die
    {
        static DWORD s_lastDiag = 0;
        DWORD now = GetTickCount();
        if (now - s_lastDiag >= 3000) {
            s_lastDiag = now;
            char b[192]; size_t k = 0;
            const char* t = "ESP DIAG: list=";
            DiagStr(b, k, t);
            DiagNum(b, k, n);
            DiagStr(b, k, " dist=");  DiagNum(b, k, fDist);
            DiagStr(b, k, " gates="); DiagNum(b, k, fGates);
            DiagStr(b, k, " proj=");  DiagNum(b, k, fW2S);
            DiagStr(b, k, " drawn="); DiagNum(b, k, fDrawn);
            DiagStr(b, k, vproj::HasViewProj() ? " cam=OK" : " cam=WAIT");
            DiagStr(b, k, vproj::ZFlip() ? " z=FWD*" : " z=std");
            // v18: ref = project the local player (known on-screen point).
            // ref=(x,y) proves the whole world->screen pipeline on a
            // guaranteed target; ref=FAIL means every point gets culled.
            float rx, ry;
            float rwx, rwy, rwz;
            if (haveLocal) { rwx = lx; rwy = ly; rwz = lz; }
            else { rwx = 0; rwy = 0; rwz = 0; }
            if (vproj::WorldToScreen(rwx, rwy, rwz, &rx, &ry)) {
                DiagStr(b, k, " ref=(");
                DiagNum(b, k, (int)(rx + 0.5f));
                b[k++] = ',';
                DiagNum(b, k, (int)(ry + 0.5f));
                b[k++] = ')';
            } else {
                DiagStr(b, k, " ref=FAIL");
            }
            // v18: W2S success ratio over the last second (all-behind cull)
            long wc = 0, wok = 0;
            vproj::W2SHealth(&wc, &wok);
            DiagStr(b, k, " w2s=");
            DiagNum(b, k, (int)wok);
            b[k++] = '/';
            DiagNum(b, k, (int)wc);
            b[k] = 0;
            OILog(b);
        }
    }
}

static void DrawStatus()
{
    // Debug status text - commented out
    // if (!var || !var->c_esp.status_info) return;
    // ImDrawList* dl = ImGui::GetBackgroundDrawList();
    // dl->AddText(ImVec2(10, 10), IM_COL32(120, 255, 120, 230), ent::StatusText());
    // dl->AddText(ImVec2(10, 28), IM_COL32(120, 255, 120, 210), vproj::StatusText());
    // dl->AddText(ImVec2(10, 64), IM_COL32(255, 120, 120, 230), trigger::StatusText());
}

bool Overlay_RenderFrame(void* swapchain)
{
    if (!g_initialized) return false;
    if (g_warmup < 30) { g_warmup++; return false; }

    // live target refresh + viewProj lock maintenance (cheap, no I/O)
    vproj::OnFrame();
    ent::OnFrame();
    // triggerbot: fires synthetic clicks while enabled and an enemy sits on
    // the crosshair. Skipped while the menu is open.
    if (!g_menuOpen) trigger::OnFrame();
    IDXGISwapChain* sc = (IDXGISwapChain*)swapchain;

    // Fresh backbuffer RTV every frame
    if (g_rtv) { g_rtv->Release(); g_rtv = nullptr; }
    {
        ID3D11Texture2D* bb = nullptr;
        if (SUCCEEDED(sc->GetBuffer(0, __uuidof(ID3D11Texture2D), (void**)&bb)) && bb) {
            g_pd3dDevice->CreateRenderTargetView(bb, nullptr, &g_rtv);
            bb->Release();
        }
    }
    if (!g_rtv) return false;

    ImGui_ImplDX11_NewFrame();
    ImGui_ImplWin32_NewFrame();

    // Frame lifecycle is owned HERE — the old flow only worked with the menu
    // open because GW2_Framework_Render called NewFrame/Render internally.
    // With the menu closed GetDrawData() returned NULL and the vendored
    // RenderDrawData dereferenced it (crash at [rcx+0x28], every frame).
    ImGui::NewFrame();

    // viewProj debug layer: ALWAYS visible (this is the ESP foundation).
    // The GUI itself still renders only while the menu is open.
    vproj::SetScreenSize(ImGui::GetIO().DisplaySize.x, ImGui::GetIO().DisplaySize.y);
    DrawEsp();
    aimbot::OnFrame();   // Process aimbot targeting/movement
    aimbot::DrawFOV();   // Draw FOV circles
    DrawStatus();
    
    // Watermark - always rendered (draggable only when menu open)
    GW2_Framework_RenderWatermark();

    if (g_menuOpen)
        GW2_Framework_Render();

    ImGui::Render();

    g_pd3dDeviceContext->OMSetRenderTargets(1, &g_rtv, nullptr);
    ImGui_ImplDX11_RenderDrawData(ImGui::GetDrawData());

    // Unbind + release RTV immediately — NEVER hold backbuffer refs between
    // frames. Outstanding refs cause ResizeBuffers → DXGI_ERROR_INVALID_CALL
    // (DirectX error on Win10) or TDR → BSOD (Win11).
    g_pd3dDeviceContext->OMSetRenderTargets(0, nullptr, nullptr);
    if (g_rtv) { g_rtv->Release(); g_rtv = nullptr; }

    return true;
}