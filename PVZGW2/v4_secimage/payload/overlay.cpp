// ---------------------------------------------------------------------------
// GW2 overlay — ImGui (DX11) rendering over the hooked Present.
// Uses the israeliclient 2.0 UI framework for styled widgets and tabs.
// ---------------------------------------------------------------------------
#include <windows.h>
#include <d3d11.h>
#include "imgui.h"
#include "imgui_impl_dx11.h"

extern IMGUI_IMPL_API LRESULT ImGui_ImplWin32_WndProcHandler(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam);
#include "imgui_impl_win32.h"

#include "framework/gui_gw2.h"
#include "framework/functions.h"

// --- init-only logging (NO per-frame I/O — causes kernel object churn/BSOD) --
static void OILog(const char* msg)
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

static bool g_initialized = false;
static bool g_menuOpen    = false;
static int  g_warmup      = 0;

static HWND    g_hwnd = nullptr;
static WNDPROC g_origWndProc = nullptr;
static ID3D11RenderTargetView*  g_rtv = nullptr;

static LRESULT CALLBACK HookWndProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam)
{
    if (g_initialized)
        ImGui_ImplWin32_WndProcHandler(hwnd, msg, wParam, lParam);
    return CallWindowProcW(g_origWndProc, hwnd, msg, wParam, lParam);
}

bool Overlay_Init(void* swapchain, HWND hwnd)
{
    if (g_initialized) return true;

    OILogClear();
    OILog("OI: begin");

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

    GW2_Framework_Init(device);

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

bool Overlay_RenderFrame(void* swapchain)
{
    if (!g_initialized) return false;
    if (g_warmup < 30) { g_warmup++; return false; }

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

    // Always render watermark (even when menu closed)
    if (!g_menuOpen) {
        GW2_Framework_RenderWatermark();
    } else {
        // Full menu render (includes everything)
        GW2_Framework_Render();
    }

    g_pd3dDeviceContext->OMSetRenderTargets(1, &g_rtv, nullptr);
    ImGui_ImplDX11_RenderDrawData(ImGui::GetDrawData());

    // Unbind + release RTV immediately - NEVER hold backbuffer refs between
    // frames. Outstanding refs cause ResizeBuffers -> DXGI_ERROR_INVALID_CALL
    // (DirectX error on Win10) or TDR -> BSOD (Win11).
    g_pd3dDeviceContext->OMSetRenderTargets(0, nullptr, nullptr);
    if (g_rtv) { g_rtv->Release(); g_rtv = nullptr; }

    return true;
}