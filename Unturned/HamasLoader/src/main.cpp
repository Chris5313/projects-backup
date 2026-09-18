#include "sound.h"
#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif
#include <windows.h>
#include <dwmapi.h>
#include <d3d11.h>
#include <tchar.h>

#include "imgui.h"
#include "imgui_impl_win32.h"
#include "imgui_impl_dx11.h"

#include "gui.h"
#include "style.h"
#include "download.h"
#include "discord_log.h"
#include <shlobj.h>
#include <cstdio>

// ---- DX11 globals ----
static ID3D11Device*            g_pd3dDevice = nullptr;
static ID3D11DeviceContext*     g_pd3dDeviceContext = nullptr;
static IDXGISwapChain*          g_pSwapChain = nullptr;
static bool                     g_SwapChainOccluded = false;
static UINT                     g_ResizeWidth = 0, g_ResizeHeight = 0;
static ID3D11RenderTargetView*  g_mainRenderTargetView = nullptr;

// Window dimensions
static constexpr int WIN_W = 1050;
static constexpr int WIN_H = 680;

// Forward declarations
static bool CreateDeviceD3D(HWND hWnd);
static void CleanupDeviceD3D();
static void CreateRenderTarget();
static void CleanupRenderTarget();
LRESULT WINAPI WndProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam);

// Forward declare ImGui Win32 handler
extern IMGUI_IMPL_API LRESULT ImGui_ImplWin32_WndProcHandler(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam);

int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE, LPSTR, int) {
    // All assets embedded as resources — no external files needed
    // Check admin privileges
    {
        BOOL isAdmin = FALSE;
        PSID adminGroup = nullptr;
        SID_IDENTIFIER_AUTHORITY ntAuth = SECURITY_NT_AUTHORITY;
        if (AllocateAndInitializeSid(&ntAuth, 2, SECURITY_BUILTIN_DOMAIN_RID,
                DOMAIN_ALIAS_RID_ADMINS, 0, 0, 0, 0, 0, 0, &adminGroup)) {
            CheckTokenMembership(nullptr, adminGroup, &isAdmin);
            FreeSid(adminGroup);
        }
        GUI::NotAdmin = !isAdmin;
    }
    ImGui_ImplWin32_EnableDpiAwareness();

    // Register window class
    WNDCLASSEXW wc = {};
    wc.cbSize        = sizeof(wc);
    wc.style         = CS_CLASSDC;
    wc.lpfnWndProc   = WndProc;
    wc.hInstance      = hInstance;
    wc.lpszClassName = L"HamasClientLoader";
    wc.hCursor       = LoadCursor(nullptr, IDC_ARROW);
    ::RegisterClassExW(&wc);

    // Center the window on screen
    int screenW = GetSystemMetrics(SM_CXSCREEN);
    int screenH = GetSystemMetrics(SM_CYSCREEN);
    int posX = (screenW - WIN_W) / 2;
    int posY = (screenH - WIN_H) / 2;

    // Borderless popup window
    HWND hwnd = ::CreateWindowExW(
        WS_EX_TOPMOST,
        wc.lpszClassName,
        L"Hamas Client",
        WS_POPUP | WS_VISIBLE,
        posX, posY, WIN_W, WIN_H,
        nullptr, nullptr, hInstance, nullptr
    );

    // Set taskbar icon
    HICON hIcon = LoadIconW(hInstance, MAKEINTRESOURCEW(1));
    if (hIcon) {
        SendMessageW(hwnd, WM_SETICON, ICON_BIG, (LPARAM)hIcon);
        SendMessageW(hwnd, WM_SETICON, ICON_SMALL, (LPARAM)hIcon);
    }

    // Extend frame for DWM shadow / rounded corners
    MARGINS margins = { -1, -1, -1, -1 };
    DwmExtendFrameIntoClientArea(hwnd, &margins);

    // Set DWM window corner preference (Windows 11)
    enum DWM_WINDOW_CORNER_PREFERENCE { DWMWCP_DEFAULT = 0, DWMWCP_DONOTROUND = 1, DWMWCP_ROUND = 2, DWMWCP_ROUNDSMALL = 3 };
    auto cornerPref = DWMWCP_ROUND;
    DwmSetWindowAttribute(hwnd, 33 /* DWMWA_WINDOW_CORNER_PREFERENCE */, &cornerPref, sizeof(cornerPref));

    // Initialize Direct3D
    if (!CreateDeviceD3D(hwnd)) {
        CleanupDeviceD3D();
        ::UnregisterClassW(wc.lpszClassName, hInstance);
        return 1;
    }

    // Setup ImGui context
    IMGUI_CHECKVERSION();
    ImGui::CreateContext();
    ImGuiIO& io = ImGui::GetIO();
    io.ConfigFlags |= ImGuiConfigFlags_NavEnableKeyboard;
    io.IniFilename = nullptr; // No imgui.ini

    // Apply custom style
    Style::ApplyStyle();

    // Setup backends
    ImGui_ImplWin32_Init(hwnd);
    ImGui_ImplDX11_Init(g_pd3dDevice, g_pd3dDeviceContext);

    // ---- Load Fonts from embedded resources ----
    static const ImWchar fa_ranges[] = { 0xf000, 0xf8ff, 0 };

    // Helper: get resource pointer + size
    auto GetRes = [](int id, DWORD* outSize) -> const void* {
        HRSRC hr = FindResource(nullptr, MAKEINTRESOURCE(id), RT_RCDATA);
        if (!hr) return nullptr;
        HGLOBAL hg = LoadResource(nullptr, hr);
        if (!hg) return nullptr;
        *outSize = SizeofResource(nullptr, hr);
        return LockResource(hg);
    };

    DWORD interSize = 0, faSize = 0;
    const void* interData = GetRes(201, &interSize);   // IDR_FONT_INTER
    const void* faData    = GetRes(202, &faSize);       // IDR_FONT_FA

    // Arabic glyphs from system font (Segoe UI) for slogan rendering
    static const ImWchar arabic_ranges[] = { 0x0600, 0x06FF, 0xFB50, 0xFDFF, 0xFE70, 0xFEFF, 0 };
    auto LoadSegoeUI = [](DWORD* outSize) -> void* {
        char winDir[MAX_PATH];
        GetWindowsDirectoryA(winDir, MAX_PATH);
        char fontPath[MAX_PATH];
        snprintf(fontPath, MAX_PATH, "%s\\Fonts\\segoeui.ttf", winDir);
        HANDLE hFile = CreateFileA(fontPath, GENERIC_READ, FILE_SHARE_READ, nullptr, OPEN_EXISTING, 0, nullptr);
        if (hFile == INVALID_HANDLE_VALUE) return nullptr;
        *outSize = GetFileSize(hFile, nullptr);
        void* buf = IM_ALLOC(*outSize);
        DWORD read = 0;
        ReadFile(hFile, buf, *outSize, &read, nullptr);
        CloseHandle(hFile);
        return buf;
    };

    // ImGui takes ownership of font data — we must give it a copy
    auto CopyForImGui = [](const void* src, DWORD size) -> void* {
        void* copy = IM_ALLOC(size);
        if (copy) memcpy(copy, src, size);
        return copy;
    };

    // Inter 18px — main text font
    if (interData) {
        ImFontConfig fc; fc.FontDataOwnedByAtlas = true;
        GUI::FontText = io.Fonts->AddFontFromMemoryTTF(CopyForImGui(interData, interSize), interSize, 18.0f, &fc);
    }
    if (!GUI::FontText) GUI::FontText = io.Fonts->AddFontDefault();

    // Merge Font Awesome into text font
    if (faData) {
        ImFontConfig cfg; cfg.MergeMode = true; cfg.PixelSnapH = true;
        cfg.GlyphMinAdvanceX = 18.0f; cfg.GlyphOffset = ImVec2(0, 2.0f);
        cfg.FontDataOwnedByAtlas = true;
        io.Fonts->AddFontFromMemoryTTF(CopyForImGui(faData, faSize), faSize, 16.0f, &cfg, fa_ranges);
    }
    // Merge Arabic glyphs into text font
    { DWORD arSz = 0; void* arBuf = LoadSegoeUI(&arSz);
      if (arBuf) { ImFontConfig ac; ac.MergeMode = true; ac.PixelSnapH = true;
          ac.FontDataOwnedByAtlas = true;
          io.Fonts->AddFontFromMemoryTTF(arBuf, arSz, 18.0f, &ac, arabic_ranges); } }

    // Inter 28px — title font
    if (interData) {
        ImFontConfig fc; fc.FontDataOwnedByAtlas = true;
        GUI::FontTitle = io.Fonts->AddFontFromMemoryTTF(CopyForImGui(interData, interSize), interSize, 28.0f, &fc);
    }
    if (!GUI::FontTitle) GUI::FontTitle = GUI::FontText;

    if (faData) {
        ImFontConfig cfg; cfg.MergeMode = true; cfg.PixelSnapH = true;
        cfg.GlyphMinAdvanceX = 28.0f; cfg.GlyphOffset = ImVec2(0, 4.0f);
        cfg.FontDataOwnedByAtlas = true;
        io.Fonts->AddFontFromMemoryTTF(CopyForImGui(faData, faSize), faSize, 22.0f, &cfg, fa_ranges);
    }
    // Merge Arabic glyphs into title font
    { DWORD arSz = 0; void* arBuf = LoadSegoeUI(&arSz);
      if (arBuf) { ImFontConfig ac; ac.MergeMode = true; ac.PixelSnapH = true;
          ac.FontDataOwnedByAtlas = true;
          io.Fonts->AddFontFromMemoryTTF(arBuf, arSz, 28.0f, &ac, arabic_ranges); } }

    // Inter 14px — small font
    if (interData) {
        ImFontConfig fc; fc.FontDataOwnedByAtlas = true;
        GUI::FontSmall = io.Fonts->AddFontFromMemoryTTF(CopyForImGui(interData, interSize), interSize, 14.0f, &fc);
    }
    if (!GUI::FontSmall) GUI::FontSmall = GUI::FontText;

    if (faData) {
        ImFontConfig cfg; cfg.MergeMode = true; cfg.PixelSnapH = true;
        cfg.GlyphMinAdvanceX = 14.0f; cfg.GlyphOffset = ImVec2(0, 1.0f);
        cfg.FontDataOwnedByAtlas = true;
        io.Fonts->AddFontFromMemoryTTF(CopyForImGui(faData, faSize), faSize, 13.0f, &cfg, fa_ranges);
    }
    // Merge Arabic glyphs into small font
    { DWORD arSz = 0; void* arBuf = LoadSegoeUI(&arSz);
      if (arBuf) { ImFontConfig ac; ac.MergeMode = true; ac.PixelSnapH = true;
          ac.FontDataOwnedByAtlas = true;
          io.Fonts->AddFontFromMemoryTTF(arBuf, arSz, 14.0f, &ac, arabic_ranges); } }

    // Build font atlas
    io.Fonts->Build();

    // Load textures (logo)
    GUI::Init(g_pd3dDevice);
    Sound::Init();

    // Initialize Discord logging and post loader-open event
    DiscordLog::Init();
    DiscordLog::PostEvent(DiscordLog::EVENT_LOADER_OPEN);

    // ---- Main loop ----
    bool done = false;
    const float clearColor[4] = { 0.039f, 0.039f, 0.047f, 1.0f }; // #0a0a0c

    while (!done) {
        MSG msg;
        while (::PeekMessage(&msg, nullptr, 0U, 0U, PM_REMOVE)) {
            ::TranslateMessage(&msg);
            ::DispatchMessage(&msg);
            if (msg.message == WM_QUIT)
                done = true;
        }
        if (done) break;

        // Handle occlusion
        if (g_SwapChainOccluded && g_pSwapChain->Present(0, DXGI_PRESENT_TEST) == DXGI_STATUS_OCCLUDED) {
            ::Sleep(10);
            continue;
        }
        g_SwapChainOccluded = false;

        // Handle resize
        if (g_ResizeWidth != 0 && g_ResizeHeight != 0) {
            CleanupRenderTarget();
            g_pSwapChain->ResizeBuffers(0, g_ResizeWidth, g_ResizeHeight, DXGI_FORMAT_UNKNOWN, 0);
            g_ResizeWidth = g_ResizeHeight = 0;
            CreateRenderTarget();
        }

        // New frame
        ImGui_ImplDX11_NewFrame();
        ImGui_ImplWin32_NewFrame();
        ImGui::NewFrame();

        // Render with transition animation
        GUI::RenderFrame((float)WIN_W, (float)WIN_H);

        // Check close
        if (GUI::WantsClose) {
            done = true;
        }

        ImGui::Render();
        g_pd3dDeviceContext->OMSetRenderTargets(1, &g_mainRenderTargetView, nullptr);
        g_pd3dDeviceContext->ClearRenderTargetView(g_mainRenderTargetView, clearColor);
        ImGui_ImplDX11_RenderDrawData(ImGui::GetDrawData());

        HRESULT hr = g_pSwapChain->Present(1, 0); // VSync
        g_SwapChainOccluded = (hr == DXGI_STATUS_OCCLUDED);
    }

    // Cleanup — Discord shutdown posts LOADER_CLOSE and drains queue
    DiscordLog::Shutdown();
    GUI::Shutdown();
    ImGui_ImplDX11_Shutdown();
    ImGui_ImplWin32_Shutdown();
    ImGui::DestroyContext();
    CleanupDeviceD3D();
    ::DestroyWindow(hwnd);
    ::UnregisterClassW(wc.lpszClassName, hInstance);

    return 0;
}

// ---- D3D11 Setup ----
static bool CreateDeviceD3D(HWND hWnd) {
    DXGI_SWAP_CHAIN_DESC sd = {};
    sd.BufferCount                        = 2;
    sd.BufferDesc.Width                   = 0;
    sd.BufferDesc.Height                  = 0;
    sd.BufferDesc.Format                  = DXGI_FORMAT_R8G8B8A8_UNORM;
    sd.BufferDesc.RefreshRate.Numerator   = 60;
    sd.BufferDesc.RefreshRate.Denominator = 1;
    sd.Flags                              = DXGI_SWAP_CHAIN_FLAG_ALLOW_MODE_SWITCH;
    sd.BufferUsage                        = DXGI_USAGE_RENDER_TARGET_OUTPUT;
    sd.OutputWindow                       = hWnd;
    sd.SampleDesc.Count                   = 1;
    sd.SampleDesc.Quality                 = 0;
    sd.Windowed                           = TRUE;
    sd.SwapEffect                         = DXGI_SWAP_EFFECT_DISCARD;

    UINT flags = 0;
    D3D_FEATURE_LEVEL featureLevel;
    const D3D_FEATURE_LEVEL levels[] = { D3D_FEATURE_LEVEL_11_0, D3D_FEATURE_LEVEL_10_0 };

    HRESULT hr = D3D11CreateDeviceAndSwapChain(
        nullptr, D3D_DRIVER_TYPE_HARDWARE, nullptr, flags,
        levels, 2, D3D11_SDK_VERSION, &sd,
        &g_pSwapChain, &g_pd3dDevice, &featureLevel, &g_pd3dDeviceContext);

    if (hr == DXGI_ERROR_UNSUPPORTED) {
        hr = D3D11CreateDeviceAndSwapChain(
            nullptr, D3D_DRIVER_TYPE_WARP, nullptr, flags,
            levels, 2, D3D11_SDK_VERSION, &sd,
            &g_pSwapChain, &g_pd3dDevice, &featureLevel, &g_pd3dDeviceContext);
    }
    if (FAILED(hr)) return false;

    CreateRenderTarget();
    return true;
}

static void CleanupDeviceD3D() {
    CleanupRenderTarget();
    if (g_pSwapChain)       { g_pSwapChain->Release();       g_pSwapChain = nullptr; }
    if (g_pd3dDeviceContext) { g_pd3dDeviceContext->Release(); g_pd3dDeviceContext = nullptr; }
    if (g_pd3dDevice)       { g_pd3dDevice->Release();       g_pd3dDevice = nullptr; }
}

static void CreateRenderTarget() {
    ID3D11Texture2D* backBuffer;
    g_pSwapChain->GetBuffer(0, IID_PPV_ARGS(&backBuffer));
    g_pd3dDevice->CreateRenderTargetView(backBuffer, nullptr, &g_mainRenderTargetView);
    backBuffer->Release();
}

static void CleanupRenderTarget() {
    if (g_mainRenderTargetView) { g_mainRenderTargetView->Release(); g_mainRenderTargetView = nullptr; }
}

// ---- Window Procedure ----
LRESULT WINAPI WndProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam) {
    if (ImGui_ImplWin32_WndProcHandler(hWnd, msg, wParam, lParam))
        return true;

    switch (msg) {
    case WM_SIZE:
        if (wParam == SIZE_MINIMIZED) return 0;
        g_ResizeWidth  = (UINT)LOWORD(lParam);
        g_ResizeHeight = (UINT)HIWORD(lParam);
        return 0;

    case WM_NCHITTEST: {
        // If any ImGui widget is hovered/active, or any custom-drawn
        // interactive element is under the cursor, let ImGui handle it.
        // Otherwise, treat the entire window as a drag handle (HTCAPTION).
        if (ImGui::GetCurrentContext()) {
            if (ImGui::IsAnyItemHovered() || ImGui::IsAnyItemActive() ||
                GUI::HoveringInteractive) {
                return HTCLIENT;
            }
        }
        return HTCAPTION;
    }

    case WM_SYSCOMMAND:
        if ((wParam & 0xfff0) == SC_KEYMENU)
            return 0;
        break;

    case WM_DESTROY:
        ::PostQuitMessage(0);
        return 0;
    }

    return ::DefWindowProcW(hWnd, msg, wParam, lParam);
}
