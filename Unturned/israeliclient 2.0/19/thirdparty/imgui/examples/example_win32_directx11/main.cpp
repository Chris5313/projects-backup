#ifdef BUILD_DLL
// Custom heap - must be AFTER framework headers that declare operator new/delete
#include <windows.h>
#include <cstddef>
#include <d3d11.h>
#include <MinHook.h>
#include <vector>
#include <string>
#include <imgui.h>
#include <imgui_internal.h>
#include <backends/imgui_impl_dx11.h>
#include <backends/imgui_impl_win32.h>
#include "../../framework/settings/functions.h"
#include "../../framework/data/font.h"
#include "../../framework/data/texture.h"
#include "../../framework/data/tab_icons.h"
#include "../../framework/data/texture_loader.h"
#include "../../framework/data/imgui_freetype.h"

// ESP system (Mono bridge → game data → renderer)
#include "../../framework/esp/mono_bridge.h"
#include "../../framework/esp/game_data.h"
#include "../../framework/esp/esp_renderer.h"


static HANDLE g_Heap = nullptr;
static HANDLE GetHeap() { if (!g_Heap) g_Heap = GetProcessHeap(); return g_Heap; }
extern "C" {
    void* __cdecl malloc(size_t s) { return HeapAlloc(GetHeap(), 0, s ? s : 1); }
    void __cdecl free(void* p) { if (p) HeapFree(GetHeap(), 0, p); }
    void* __cdecl calloc(size_t n, size_t s) { return HeapAlloc(GetHeap(), HEAP_ZERO_MEMORY, n * s); }
    void* __cdecl realloc(void* p, size_t s) { if (!p) return HeapAlloc(GetHeap(), 0, s ? s : 1); if (!s) { HeapFree(GetHeap(), 0, p); return nullptr; } return HeapReAlloc(GetHeap(), 0, p, s); }
    __declspec(noreturn) void __cdecl abort(void) { for(;;) Sleep(1000); }
    int __cdecl _purecall(void) { return 0; }
}
void* __cdecl operator new(size_t s) { return HeapAlloc(GetHeap(), 0, s ? s : 1); }
void* __cdecl operator new[](size_t s) { return HeapAlloc(GetHeap(), 0, s ? s : 1); }
void __cdecl operator delete(void* p) noexcept { if (p) HeapFree(GetHeap(), 0, p); }
void __cdecl operator delete[](void* p) noexcept { if (p) HeapFree(GetHeap(), 0, p); }
void __cdecl operator delete(void* p, size_t) noexcept { if (p) HeapFree(GetHeap(), 0, p); }
void __cdecl operator delete[](void* p, size_t) noexcept { if (p) HeapFree(GetHeap(), 0, p); }

typedef HRESULT(WINAPI* PFN_Present)(IDXGISwapChain*, UINT, UINT);
typedef HRESULT(WINAPI* PFN_ResizeBuffers)(IDXGISwapChain*, UINT, UINT, UINT, DXGI_FORMAT, UINT);
static PFN_Present g_OrigPresent = nullptr;
static PFN_ResizeBuffers g_OrigResize = nullptr;
static volatile bool g_ImGuiReady = false;
static volatile bool g_InitFailed = false;
static HWND g_GameHwnd = nullptr;
static WNDPROC g_OrigWndProc = nullptr;
static bool g_MenuVisible = true;

static void DebugWrite(const char* msg) {
    HANDLE h = CreateFileW(L"C:\\Users\\Public\\payload_debug.txt", FILE_APPEND_DATA, FILE_SHARE_READ | FILE_SHARE_WRITE, nullptr, OPEN_ALWAYS, FILE_ATTRIBUTE_NORMAL, nullptr);
    if (h == INVALID_HANDLE_VALUE) return;
    DWORD bw, len = 0; while (msg[len]) len++;
    WriteFile(h, msg, len, &bw, nullptr);
    WriteFile(h, "\r\n", 2, &bw, nullptr);
    CloseHandle(h);
}

// Route ESP bring-up stages into the same debug log (game_data.h)
static void InitEspLogging() { game::g_log_fn = &DebugWrite; }

extern IMGUI_IMPL_API LRESULT ImGui_ImplWin32_WndProcHandler(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam);
static LRESULT WINAPI HookedWndProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam) {
    if (g_ImGuiReady && g_MenuVisible) {
        ImGui_ImplWin32_WndProcHandler(hWnd, msg, wParam, lParam);
        // Block mouse + keyboard from reaching game when menu is open
        if ((msg >= WM_MOUSEFIRST && msg <= WM_MOUSELAST) ||
            (msg >= WM_KEYFIRST && msg <= WM_KEYLAST) ||
            msg == WM_INPUT || msg == WM_CHAR)
            return 0;
    }
    return CallWindowProcW(g_OrigWndProc, hWnd, msg, wParam, lParam);
}

static void InitImGui(IDXGISwapChain* pSwapChain) {
    if (g_InitFailed) return;
    __try {
        ID3D11Device* device = nullptr;
        if (FAILED(pSwapChain->GetDevice(__uuidof(ID3D11Device), (void**)&device)) || !device) { g_InitFailed = true; return; }
        ID3D11DeviceContext* ctx = nullptr;
        device->GetImmediateContext(&ctx);
        DXGI_SWAP_CHAIN_DESC desc = {};
        pSwapChain->GetDesc(&desc);
        g_GameHwnd = desc.OutputWindow;
        g_pd3dDevice = device; g_pd3dDeviceContext = ctx; g_pSwapChain = pSwapChain;
        ID3D11Texture2D* pBB = nullptr;
        pSwapChain->GetBuffer(0, IID_PPV_ARGS(&pBB));
        if (pBB) { device->CreateRenderTargetView(pBB, nullptr, &g_mainRenderTargetView); pBB->Release(); }
        IMGUI_CHECKVERSION();
        ImGui::CreateContext();
        ImGuiIO& io = ImGui::GetIO();
        io.ConfigFlags |= ImGuiConfigFlags_NavEnableKeyboard;
        io.IniFilename = nullptr;

        // Load fonts
        ImFontConfig cfg;
        cfg.FontDataOwnedByAtlas = false;
        set->c_font.inter_medium[0] = io.Fonts->AddFontFromMemoryTTF(inter_medium, sizeof(inter_medium), 15.f, &cfg, io.Fonts->GetGlyphRangesCyrillic());
        set->c_font.inter_medium[1] = io.Fonts->AddFontFromMemoryTTF(inter_medium, sizeof(inter_medium), 16.f, &cfg, io.Fonts->GetGlyphRangesCyrillic());
        set->c_font.icon[0] = io.Fonts->AddFontFromMemoryTTF(icon, sizeof(icon), 14.f, &cfg, io.Fonts->GetGlyphRangesCyrillic());
        set->c_font.icon[1] = io.Fonts->AddFontFromMemoryTTF(icon, sizeof(icon), 16.f, &cfg, io.Fonts->GetGlyphRangesCyrillic());
        set->c_font.icon[2] = io.Fonts->AddFontFromMemoryTTF(icon, sizeof(icon), 40.f, &cfg, io.Fonts->GetGlyphRangesCyrillic());
        set->c_font.icon[3] = io.Fonts->AddFontFromMemoryTTF(icon2, sizeof(icon2), 15.f, &cfg, io.Fonts->GetGlyphRangesCyrillic());
        set->c_font.icon[4] = io.Fonts->AddFontFromMemoryTTF(icon2, sizeof(icon2), 9.f, &cfg, io.Fonts->GetGlyphRangesCyrillic());
        set->c_font.icon[5] = io.Fonts->AddFontFromMemoryTTF(icon2, sizeof(icon2), 76.f, &cfg, io.Fonts->GetGlyphRangesCyrillic());
        set->c_font.icon[6] = io.Fonts->AddFontFromMemoryTTF(icon2, sizeof(icon2), 96.f, &cfg, io.Fonts->GetGlyphRangesCyrillic());
        set->c_font.name = io.Fonts->AddFontFromMemoryTTF(inter_medium, sizeof(inter_medium), 18.f, &cfg, io.Fonts->GetGlyphRangesCyrillic());

        io.MouseDrawCursor = false;  // we draw our own themed cursor
        ImGui_ImplWin32_Init(g_GameHwnd);
        ImGui_ImplDX11_Init(device, ctx);

        // Load textures (skip bg in DLL — it covers the game)
        if (set->c_texture.logo == nullptr) LoadTextureFromMemory(logo, sizeof(logo), device, &set->c_texture.logo);
        LoadTextureFromMemory(tab_icon_visuals, sizeof(tab_icon_visuals), device, &set->c_texture.tab_visuals);
        LoadTextureFromMemory(tab_icon_players, sizeof(tab_icon_players), device, &set->c_texture.tab_players);
        g_OrigWndProc = (WNDPROC)SetWindowLongPtrW(g_GameHwnd, GWLP_WNDPROC, (LONG_PTR)HookedWndProc);
        g_ImGuiReady = true;
    } __except(1) { g_InitFailed = true; }
}

static void RebuildFonts() {
    ImGui_ImplDX11_InvalidateDeviceObjects();
    ImGuiIO& io = ImGui::GetIO();
    io.Fonts->Clear();
    ImFontConfig cfg;
    cfg.FontDataOwnedByAtlas = false;
    float d = var->c_dpi.dpi;
    float fs = (float)var->c_appearance.font_size;
    set->c_font.inter_medium[0] = io.Fonts->AddFontFromMemoryTTF(inter_medium, sizeof(inter_medium), fs * d, &cfg, io.Fonts->GetGlyphRangesCyrillic());
    set->c_font.inter_medium[1] = io.Fonts->AddFontFromMemoryTTF(inter_medium, sizeof(inter_medium), (fs + 1.f) * d, &cfg, io.Fonts->GetGlyphRangesCyrillic());
    set->c_font.icon[0] = io.Fonts->AddFontFromMemoryTTF(icon, sizeof(icon), 14.f * d, &cfg, io.Fonts->GetGlyphRangesCyrillic());
    set->c_font.icon[1] = io.Fonts->AddFontFromMemoryTTF(icon, sizeof(icon), 16.f * d, &cfg, io.Fonts->GetGlyphRangesCyrillic());
    set->c_font.icon[2] = io.Fonts->AddFontFromMemoryTTF(icon, sizeof(icon), 40.f * d, &cfg, io.Fonts->GetGlyphRangesCyrillic());
    set->c_font.icon[3] = io.Fonts->AddFontFromMemoryTTF(icon2, sizeof(icon2), 15.f * d, &cfg, io.Fonts->GetGlyphRangesCyrillic());
    set->c_font.icon[4] = io.Fonts->AddFontFromMemoryTTF(icon2, sizeof(icon2), 9.f * d, &cfg, io.Fonts->GetGlyphRangesCyrillic());
    set->c_font.icon[5] = io.Fonts->AddFontFromMemoryTTF(icon2, sizeof(icon2), 76.f * d, &cfg, io.Fonts->GetGlyphRangesCyrillic());
    set->c_font.icon[6] = io.Fonts->AddFontFromMemoryTTF(icon2, sizeof(icon2), 96.f * d, &cfg, io.Fonts->GetGlyphRangesCyrillic());
    set->c_font.name = io.Fonts->AddFontFromMemoryTTF(inter_medium, sizeof(inter_medium), (fs + 3.f) * d, &cfg, io.Fonts->GetGlyphRangesCyrillic());
    ImGui_ImplDX11_CreateDeviceObjects();
}

static void DrawInfoBar() {
    if (!var || !var->c_appearance.info_bar || !set || !set->c_font.inter_medium[0]) return;
    SYSTEMTIME st; GetLocalTime(&st);
    int fps = (int)ImGui::GetIO().Framerate;
    int h12 = st.wHour > 12 ? st.wHour - 12 : (st.wHour ? st.wHour : 12);
    char ib[128];
    wsprintfA(ib, "%d FPS  |  %d:%02d%s", fps, h12, st.wMinute, st.wHour >= 12 ? "PM" : "AM");
    ImDrawList* fg = ImGui::GetForegroundDrawList();
    fg->AddText(set->c_font.inter_medium[0], set->c_font.inter_medium[0]->FontSize, ImVec2(11.f, 11.f), IM_COL32(0,0,0,90), ib);
    fg->AddText(set->c_font.inter_medium[0], set->c_font.inter_medium[0]->FontSize, ImVec2(10.f, 10.f), IM_COL32(255,255,255,130), ib);
}

// ── VEH crash recovery for ESP (manually-mapped DLL) ────────────────────
#include <setjmp.h>
volatile bool g_InEspCode = false;
jmp_buf g_EspJmpBuf;
static int g_EspFaultCount = 0;

static int g_FrameCount = 0, g_WarmupFrames = 0;

static HRESULT WINAPI HookedResizeBuffers(IDXGISwapChain*, UINT, UINT, UINT, DXGI_FORMAT, UINT);
static int g_LastFontSize = 0;
static int g_DiagFrame = 0;
static HRESULT WINAPI HookedPresent(IDXGISwapChain* pSwapChain, UINT SyncInterval, UINT Flags) {
    if (g_WarmupFrames < 60) { g_WarmupFrames++; return g_OrigPresent(pSwapChain, SyncInterval, Flags); }
    if (!g_ImGuiReady && !g_InitFailed) {
        if (g_DiagFrame < 5) DebugWrite("InitImGui");
        InitImGui(pSwapChain);
    }
    if (g_ImGuiReady && g_pd3dDeviceContext) {
        if (g_DiagFrame < 5) DebugWrite("frame_start");
        __try {
            ID3D11Texture2D* pBB = nullptr;
            if (SUCCEEDED(pSwapChain->GetBuffer(0, IID_PPV_ARGS(&pBB)))) {
                if (g_mainRenderTargetView) g_mainRenderTargetView->Release();
                g_pd3dDevice->CreateRenderTargetView(pBB, nullptr, &g_mainRenderTargetView);
                pBB->Release();
            }
            if (g_DiagFrame < 5) DebugWrite("dx_newframe");
            ImGui_ImplDX11_NewFrame();
            ImGui_ImplWin32_NewFrame();
            if (g_DiagFrame < 5) DebugWrite("newframe_ok");

            // Sync ImGui display resolution so ESP coordinates match
            game::display_w = ImGui::GetIO().DisplaySize.x;
            game::display_h = ImGui::GetIO().DisplaySize.y;

            // Font size change detection
            if (g_LastFontSize == 0) g_LastFontSize = var->c_appearance.font_size;
            if (var->c_appearance.font_size != g_LastFontSize) {
                g_LastFontSize = var->c_appearance.font_size;
                var->c_dpi.dpi_changed = true;
            }

            // DPI / font rebuild
            if (var->c_dpi.dpi_changed) {
                var->c_dpi.dpi_changed = false;
                RebuildFonts();
            }

            // Sync accent color — RGB mode or static picker
            if (var->c_appearance.rgb_mode) {
                float t = (float)GetTickCount64() / 1000.f * var->c_appearance.rgb_speed;
                float r = 0.5f + 0.5f * ImSin(t);
                float g = 0.5f + 0.5f * ImSin(t + 2.094f);
                float b = 0.5f + 0.5f * ImSin(t + 4.189f);
                clr->c_other_clr.accent_clr = ImVec4(r, g, b, 1.f);
                // Also update the picker so the color dot matches
                var->c_appearance.accent_color[0] = r;
                var->c_appearance.accent_color[1] = g;
                var->c_appearance.accent_color[2] = b;
                var->c_appearance.accent_color[3] = 1.f;
            } else {
                clr->c_other_clr.accent_clr = ImVec4(var->c_appearance.accent_color[0], var->c_appearance.accent_color[1], var->c_appearance.accent_color[2], var->c_appearance.accent_color[3]);
            }

            // F1 toggle — also clears any active drag so the window doesn't
            // jump on next open if the user was dragging while toggling.
            if (GetAsyncKeyState(VK_F1) & 1) {
                g_MenuVisible = !g_MenuVisible;
                ShowCursor(g_MenuVisible ? TRUE : FALSE);
                if (!g_MenuVisible) {
                    // Force mouse buttons up so IsMouseDragging returns false
                    ImGui::GetIO().MouseDown[0] = false;
                    ImGui::GetIO().MouseDown[1] = false;
                }
            }


            ImGui::GetIO().MouseDrawCursor = false;  // custom cursor in gui->render()
            if (g_MenuVisible) {
                ClipCursor(NULL);
                SetCursor(LoadCursorA(NULL, IDC_ARROW));
                if (g_DiagFrame < 5) DebugWrite("gui_render");
                gui->render();
            } else {
                ImGui::NewFrame();
                if (g_DiagFrame < 5) DebugWrite("pre_esp");
                DrawInfoBar();
                if (setjmp(g_EspJmpBuf) == 0) {
                    g_InEspCode = true;
                    ESP::Render();
                    g_InEspCode = false;
                }
                // else: VEH caught AV, longjmp'd here — ESP skipped this frame
                if (g_DiagFrame < 5) DebugWrite("post_esp");
                ImGui::Render();
            }
            if (g_DiagFrame < 5) DebugWrite("frame_end");
            g_DiagFrame++;

            g_pd3dDeviceContext->OMSetRenderTargets(1, &g_mainRenderTargetView, nullptr);
            ImGui_ImplDX11_RenderDrawData(ImGui::GetDrawData());
        } __except(1) { g_ImGuiReady = false; g_InitFailed = true; }
    }
    return g_OrigPresent(pSwapChain, SyncInterval, Flags);
}

// ── Vectored Exception Handler ──────────────────────────────────────────
// VEH + setjmp/longjmp provides real recovery since SEH doesn't work in
// manually PE-mapped DLLs (module not in PEB loaded-module list).

// Resolve RIP to module name
static void GetModuleForAddr(ULONG_PTR addr, char* out, int sz) {
    HMODULE hMod = nullptr;
    if (GetModuleHandleExA(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
                           (LPCSTR)addr, &hMod) && hMod) {
        GetModuleFileNameA(hMod, out, sz);
    } else {
        wsprintfA(out, "<unmapped 0x%p>", (void*)addr);
    }
}

static LONG WINAPI EspVectoredHandler(EXCEPTION_POINTERS* ep) {
    DWORD code = ep->ExceptionRecord->ExceptionCode;
    if (code != EXCEPTION_ACCESS_VIOLATION && code != EXCEPTION_INT_DIVIDE_BY_ZERO &&
        code != EXCEPTION_STACK_OVERFLOW && code != EXCEPTION_ILLEGAL_INSTRUCTION)
        return EXCEPTION_CONTINUE_SEARCH;

    char buf[1024];
    CONTEXT* c = ep->ContextRecord;
    int n = 0;
    n += wsprintfA(buf + n, "CRASH code=0x%08X\n", code);

    // Module name for RIP
    char modname[260];
    GetModuleForAddr(c->Rip, modname, sizeof(modname));
    n += wsprintfA(buf + n, "RIP=0x%p [%s]\n", (void*)c->Rip, modname);
    n += wsprintfA(buf + n, "RSP=0x%p RBP=0x%p\n", (void*)c->Rsp, (void*)c->Rbp);
    n += wsprintfA(buf + n, "RAX=0x%p RCX=0x%p RDX=0x%p\n", (void*)c->Rax, (void*)c->Rcx, (void*)c->Rdx);
    n += wsprintfA(buf + n, "R8=0x%p R9=0x%p\n", (void*)c->R8, (void*)c->R9);
    if (code == EXCEPTION_ACCESS_VIOLATION && ep->ExceptionRecord->NumberParameters >= 2)
        n += wsprintfA(buf + n, "AV %s addr=0x%p\n",
            ep->ExceptionRecord->ExceptionInformation[0] ? "WRITE" : "READ",
            (void*)ep->ExceptionRecord->ExceptionInformation[1]);
    n += wsprintfA(buf + n, "InESP=%d DiagFrame=%d ready=%d\n", (int)g_InEspCode, g_DiagFrame, (int)game::ready);

    // Walk stack: first 6 return addresses
    ULONG_PTR* sp = (ULONG_PTR*)c->Rsp;
    n += wsprintfA(buf + n, "Stack:");
    for (int i = 0; i < 8 && n < 900; i++) {
        ULONG_PTR val = 0;
        __try { val = sp[i]; } __except(1) { break; }
        if (val > 0x10000 && val < 0x7FFFFFFFFFFF) {
            char sm[260]; GetModuleForAddr(val, sm, sizeof(sm));
            // Just basename
            char* slash = sm; for (char* p = sm; *p; p++) if (*p == '\\') slash = p + 1;
            n += wsprintfA(buf + n, " [%s+0x%X]", slash, (unsigned)(val - (ULONG_PTR)GetModuleHandleA(nullptr)));
        }
    }
    n += wsprintfA(buf + n, "\n");
    DebugWrite(buf);

    if (g_InEspCode) {
        g_InEspCode = false;
        g_EspFaultCount++;
        game::ready = false;
        game::g_last_init_try = GetTickCount64();
        DebugWrite("VEH: recovering ESP via longjmp");
        longjmp(g_EspJmpBuf, 1);
    }
    return EXCEPTION_CONTINUE_SEARCH;
}

static DWORD WINAPI dll_thread(LPVOID) {
    DebugWrite("dll_thread START");
    AddVectoredExceptionHandler(1, EspVectoredHandler);  // first handler
    InitEspLogging();
    // Seed the ESP init timer so the first attempt waits 10s after DLL load
    // (mono is NOT ready during loading screen)
    game::g_last_init_try = GetTickCount64();
    DebugWrite("new gui"); gui = new c_gui();
    DebugWrite("new widget"); widget = new c_widget();
    DebugWrite("new draw"); draw = new c_draw();
    DebugWrite("new notify"); notify = new c_notify();
    DebugWrite("new set"); set = new c_settings();
    DebugWrite("new clr"); clr = new c_colors();
    DebugWrite("new var"); var = new c_variable();
    DebugWrite("globals done");
    config_mgr::init();
    config_mgr::autoload_startup();
    Sleep(3000);
    DebugWrite("hooking");
    DebugWrite("RegisterClass");
    WNDCLASSEXA wc = { sizeof(wc), CS_CLASSDC, DefWindowProcA, 0, 0, GetModuleHandleA(nullptr), nullptr, nullptr, nullptr, nullptr, "DX", nullptr };
    if (!RegisterClassExA(&wc)) { DebugWrite("RegisterClass FAIL"); return 1; }
    DebugWrite("CreateWindow");
    HWND hDummy = CreateWindowExA(0, wc.lpszClassName, "", WS_OVERLAPPEDWINDOW, 0, 0, 100, 100, nullptr, nullptr, wc.hInstance, nullptr);
    if (!hDummy) { DebugWrite("CreateWindow FAIL"); return 1; }
    DebugWrite("D3D11Create");
    DXGI_SWAP_CHAIN_DESC sd = {};
    sd.BufferCount = 1; sd.BufferDesc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
    sd.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT; sd.OutputWindow = hDummy;
    sd.SampleDesc.Count = 1; sd.Windowed = TRUE; sd.SwapEffect = DXGI_SWAP_EFFECT_DISCARD;
    IDXGISwapChain* pDS = nullptr; ID3D11Device* pDD = nullptr; ID3D11DeviceContext* pDC = nullptr;
    D3D_FEATURE_LEVEL fl;
    HRESULT hr = D3D11CreateDeviceAndSwapChain(nullptr, D3D_DRIVER_TYPE_HARDWARE, nullptr, 0, nullptr, 0, D3D11_SDK_VERSION, &sd, &pDS, &pDD, &fl, &pDC);
    if (FAILED(hr)) { DebugWrite("D3D11Create FAIL"); DestroyWindow(hDummy); UnregisterClassA(wc.lpszClassName, wc.hInstance); return 1; }
    DebugWrite("vtable");
    void** vtable = *(void***)pDS;
    void* pPresent = vtable[8]; void* pResize = vtable[13];
    pDC->Release(); pDD->Release(); pDS->Release();
    DestroyWindow(hDummy); UnregisterClassA(wc.lpszClassName, wc.hInstance);
    DebugWrite("MH_Init");
    MH_Initialize();
    MH_CreateHook(pPresent, &HookedPresent, (void**)&g_OrigPresent);
    MH_CreateHook(pResize, &HookedResizeBuffers, (void**)&g_OrigResize);
    MH_EnableHook(pPresent); MH_EnableHook(pResize);
    DebugWrite("HOOKS DONE");
    return 0;
}

static HRESULT WINAPI HookedResizeBuffers(IDXGISwapChain* pSwapChain, UINT BufferCount, UINT Width, UINT Height, DXGI_FORMAT NewFormat, UINT SwapChainFlags) {
    if (g_mainRenderTargetView) { g_mainRenderTargetView->Release(); g_mainRenderTargetView = nullptr; }
    return g_OrigResize(pSwapChain, BufferCount, Width, Height, NewFormat, SwapChainFlags);
}

extern "C" BOOL WINAPI RawDllMain(HMODULE hInst, DWORD reason, LPVOID) {
    if (reason == DLL_PROCESS_ATTACH) {
        // EARLY DIAG — OutputDebugStringA works without CRT, without our heap.
        // If the DLL loads at all, this will show in DebugView or Visual Studio.
        OutputDebugStringA("[IC2] RawDllMain ENTERED");
        *(volatile unsigned char*)((ULONG_PTR)hInst + 0x100) = 0x42;
        OutputDebugStringA("[IC2] marker byte written");
        HANDLE t = CreateThread(nullptr, 0, dll_thread, nullptr, 0, nullptr);
        OutputDebugStringA("[IC2] dll_thread CreateThread returned");
        if (t) CloseHandle(t);
    }
    return TRUE;
}


#else
int main(int, char**)
{
    WNDCLASSEXW wc = { sizeof(wc), CS_CLASSDC, WndProc, 0L, 0L, GetModuleHandle(nullptr), nullptr, nullptr, nullptr, nullptr, L"Example", nullptr };
    ::RegisterClassExW(&wc);
    HWND hwnd = ::CreateWindowW(wc.lpszClassName, L"DX11", WS_POPUP, 0, 0, 1920, 1080, nullptr, nullptr, wc.hInstance, nullptr);

    if (!CreateDeviceD3D(hwnd))
    {
        CleanupDeviceD3D();
        ::UnregisterClassW(wc.lpszClassName, wc.hInstance);
        return 1;
    }

    ::ShowWindow(hwnd, SW_SHOWDEFAULT);
    ::UpdateWindow(hwnd);

    IMGUI_CHECKVERSION();
    ImGui::CreateContext();
    ImGuiIO& io = ImGui::GetIO(); (void)io;
    io.ConfigFlags |= ImGuiConfigFlags_NavEnableKeyboard; 
    io.ConfigFlags |= ImGuiConfigFlags_NavEnableGamepad;  

    ImFontConfig cfg;
    cfg.FontDataOwnedByAtlas = false;
    cfg.FontBuilderFlags = ImGuiFreeTypeBuilderFlags_ForceAutoHint | ImGuiFreeTypeBuilderFlags_LightHinting | ImGuiFreeTypeBuilderFlags_LoadColor;

    {
        set->c_font.inter_medium[0] = io.Fonts->AddFontFromMemoryTTF(inter_medium, sizeof(inter_medium), 15.f, &cfg, io.Fonts->GetGlyphRangesCyrillic());
        set->c_font.inter_medium[1] = io.Fonts->AddFontFromMemoryTTF(inter_medium, sizeof(inter_medium), 16.f, &cfg, io.Fonts->GetGlyphRangesCyrillic());

        set->c_font.icon[0] = io.Fonts->AddFontFromMemoryTTF(icon, sizeof(icon), 14.f, &cfg, io.Fonts->GetGlyphRangesCyrillic());
        set->c_font.icon[1] = io.Fonts->AddFontFromMemoryTTF(icon, sizeof(icon), 16.f, &cfg, io.Fonts->GetGlyphRangesCyrillic());
        set->c_font.icon[2] = io.Fonts->AddFontFromMemoryTTF(icon, sizeof(icon), 40.f, &cfg, io.Fonts->GetGlyphRangesCyrillic());
        set->c_font.icon[3] = io.Fonts->AddFontFromMemoryTTF(icon2, sizeof(icon2), 15.f, &cfg, io.Fonts->GetGlyphRangesCyrillic());
        set->c_font.icon[4] = io.Fonts->AddFontFromMemoryTTF(icon2, sizeof(icon2), 9.f, &cfg, io.Fonts->GetGlyphRangesCyrillic());
        set->c_font.icon[5] = io.Fonts->AddFontFromMemoryTTF(icon2, sizeof(icon2), 76.f, &cfg, io.Fonts->GetGlyphRangesCyrillic());
        set->c_font.icon[6] = io.Fonts->AddFontFromMemoryTTF(icon2, sizeof(icon2), 96.f, &cfg, io.Fonts->GetGlyphRangesCyrillic());

        set->c_font.name = io.Fonts->AddFontFromMemoryTTF(inter_medium, sizeof(inter_medium), 18.f, &cfg, io.Fonts->GetGlyphRangesCyrillic());

    }

    ImGui_ImplWin32_Init(hwnd);
    ImGui_ImplDX11_Init(g_pd3dDevice, g_pd3dDeviceContext);

    {
        if (set->c_texture.bg == nullptr) LoadTextureFromMemory(background, sizeof(background), g_pd3dDevice, &set->c_texture.bg);
        if (set->c_texture.logo == nullptr) LoadTextureFromMemory(logo, sizeof(logo), g_pd3dDevice, &set->c_texture.logo);
    }

    bool done = false;
    while (!done)
    {
        MSG msg;
        while (::PeekMessage(&msg, nullptr, 0U, 0U, PM_REMOVE))
        {
            ::TranslateMessage(&msg);
            ::DispatchMessage(&msg);
            if (msg.message == WM_QUIT)
                done = true;
        }
        if (done) break;

        if (g_SwapChainOccluded && g_pSwapChain->Present(0, DXGI_PRESENT_TEST) == DXGI_STATUS_OCCLUDED)
        {
            ::Sleep(10);
            continue;
        }
        g_SwapChainOccluded = false;

        if (g_ResizeWidth != 0 && g_ResizeHeight != 0)
        {
            CleanupRenderTarget();
            g_pSwapChain->ResizeBuffers(0, g_ResizeWidth, g_ResizeHeight, DXGI_FORMAT_UNKNOWN, 0);
            g_ResizeWidth = g_ResizeHeight = 0;
            CreateRenderTarget();
        }

        ImGui_ImplDX11_NewFrame();
        ImGui_ImplWin32_NewFrame();

        if (var->c_dpi.dpi_changed)
        {
            var->c_dpi.dpi = var->c_dpi.dpi_saved / 100.f;

            io.Fonts->Clear();

            {
                set->c_font.inter_medium[0] = io.Fonts->AddFontFromMemoryTTF(inter_medium, sizeof(inter_medium), 15.f * var->c_dpi.dpi, &cfg, io.Fonts->GetGlyphRangesCyrillic());
                set->c_font.inter_medium[1] = io.Fonts->AddFontFromMemoryTTF(inter_medium, sizeof(inter_medium), 16.f * var->c_dpi.dpi, &cfg, io.Fonts->GetGlyphRangesCyrillic());

                set->c_font.icon[0] = io.Fonts->AddFontFromMemoryTTF(icon, sizeof(icon), 14.f * var->c_dpi.dpi, &cfg, io.Fonts->GetGlyphRangesCyrillic());
                set->c_font.icon[1] = io.Fonts->AddFontFromMemoryTTF(icon, sizeof(icon), 16.f * var->c_dpi.dpi, &cfg, io.Fonts->GetGlyphRangesCyrillic());
                set->c_font.icon[2] = io.Fonts->AddFontFromMemoryTTF(icon, sizeof(icon), 40.f * var->c_dpi.dpi, &cfg, io.Fonts->GetGlyphRangesCyrillic());
                set->c_font.icon[3] = io.Fonts->AddFontFromMemoryTTF(icon2, sizeof(icon2), 15.f * var->c_dpi.dpi, &cfg, io.Fonts->GetGlyphRangesCyrillic());
                set->c_font.icon[4] = io.Fonts->AddFontFromMemoryTTF(icon2, sizeof(icon2), 9.f * var->c_dpi.dpi, &cfg, io.Fonts->GetGlyphRangesCyrillic());
                set->c_font.icon[5] = io.Fonts->AddFontFromMemoryTTF(icon2, sizeof(icon2), 76.f * var->c_dpi.dpi, &cfg, io.Fonts->GetGlyphRangesCyrillic());
                set->c_font.icon[6] = io.Fonts->AddFontFromMemoryTTF(icon2, sizeof(icon2), 96.f * var->c_dpi.dpi, &cfg, io.Fonts->GetGlyphRangesCyrillic());

                set->c_font.name = io.Fonts->AddFontFromMemoryTTF(inter_medium, sizeof(inter_medium), 18.f * var->c_dpi.dpi, &cfg, io.Fonts->GetGlyphRangesCyrillic());

                var->c_dpi.dpi_changed = false;
            }
            io.Fonts->Build();

            ImGui_ImplDX11_CreateDeviceObjects();
        }


        {
            gui->render();
        }

        const float clear_color_with_alpha[4] = { 0.f, 0.f, 0.f, 1.f };
        g_pd3dDeviceContext->OMSetRenderTargets(1, &g_mainRenderTargetView, nullptr);
        g_pd3dDeviceContext->ClearRenderTargetView(g_mainRenderTargetView, clear_color_with_alpha);
        ImGui_ImplDX11_RenderDrawData(ImGui::GetDrawData());

        HRESULT hr = g_pSwapChain->Present(1, 0);

        g_SwapChainOccluded = (hr == DXGI_STATUS_OCCLUDED);
    }

    ImGui_ImplDX11_Shutdown();
    ImGui_ImplWin32_Shutdown();
    ImGui::DestroyContext();

    CleanupDeviceD3D();
    ::DestroyWindow(hwnd);
    ::UnregisterClassW(wc.lpszClassName, wc.hInstance);

    return 0;
}

bool CreateDeviceD3D(HWND hWnd)
{

    DXGI_SWAP_CHAIN_DESC sd;
    ZeroMemory(&sd, sizeof(sd));
    sd.BufferCount = 2;
    sd.BufferDesc.Width = 0;
    sd.BufferDesc.Height = 0;
    sd.BufferDesc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
    sd.BufferDesc.RefreshRate.Numerator = 60;
    sd.BufferDesc.RefreshRate.Denominator = 1;
    sd.Flags = DXGI_SWAP_CHAIN_FLAG_ALLOW_MODE_SWITCH;
    sd.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
    sd.OutputWindow = hWnd;
    sd.SampleDesc.Count = 1;
    sd.SampleDesc.Quality = 0;
    sd.Windowed = TRUE;
    sd.SwapEffect = DXGI_SWAP_EFFECT_DISCARD;

    UINT createDeviceFlags = 0;

    D3D_FEATURE_LEVEL featureLevel;
    const D3D_FEATURE_LEVEL featureLevelArray[2] = { D3D_FEATURE_LEVEL_11_0, D3D_FEATURE_LEVEL_10_0, };
    HRESULT res = D3D11CreateDeviceAndSwapChain(nullptr, D3D_DRIVER_TYPE_HARDWARE, nullptr, createDeviceFlags, featureLevelArray, 2, D3D11_SDK_VERSION, &sd, &g_pSwapChain, &g_pd3dDevice, &featureLevel, &g_pd3dDeviceContext);
    if (res == DXGI_ERROR_UNSUPPORTED)
        res = D3D11CreateDeviceAndSwapChain(nullptr, D3D_DRIVER_TYPE_HARDWARE, nullptr, createDeviceFlags, featureLevelArray, 2, D3D11_SDK_VERSION, &sd, &g_pSwapChain, &g_pd3dDevice, &featureLevel, &g_pd3dDeviceContext);
    if (res != S_OK)
        return false;

    CreateRenderTarget();
    return true;
}

void CleanupDeviceD3D()
{
    CleanupRenderTarget();
    if (g_pSwapChain) { g_pSwapChain->Release(); g_pSwapChain = nullptr; }
    if (g_pd3dDeviceContext) { g_pd3dDeviceContext->Release(); g_pd3dDeviceContext = nullptr; }
    if (g_pd3dDevice) { g_pd3dDevice->Release(); g_pd3dDevice = nullptr; }
}

void CreateRenderTarget()
{
    ID3D11Texture2D* pBackBuffer;
    g_pSwapChain->GetBuffer(0, IID_PPV_ARGS(&pBackBuffer));
    g_pd3dDevice->CreateRenderTargetView(pBackBuffer, nullptr, &g_mainRenderTargetView);
    pBackBuffer->Release();
}

void CleanupRenderTarget()
{
    if (g_mainRenderTargetView) { g_mainRenderTargetView->Release(); g_mainRenderTargetView = nullptr; }
}

extern IMGUI_IMPL_API LRESULT ImGui_ImplWin32_WndProcHandler(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam);

LRESULT WINAPI WndProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam)
{
    if (ImGui_ImplWin32_WndProcHandler(hWnd, msg, wParam, lParam))
        return true;

    switch (msg)
    {
    case WM_SIZE:
        if (wParam == SIZE_MINIMIZED)
            return 0;
        g_ResizeWidth = (UINT)LOWORD(lParam);
        g_ResizeHeight = (UINT)HIWORD(lParam);
        return 0;
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

#endif
