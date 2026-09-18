// ---------------------------------------------------------------------------
// GW2 internal payload — entry point.
//
// Pipeline: driver maps this DLL into GW2.Main_Win64_Retail.exe as a proper
//           SEC_IMAGE file-backed section (kernel applies per-section
//           protections + relocations — VAD/PTE state is fully consistent,
//           .text RX / .data RW like a loader-mapped DLL). Execution starts
//           via thread hijack into kmap_thunk (see kmap_thunk.asm), which
//           calls the PE entry and restores the thread.
//
// Hook strategy (v4): NO MinHook (its RWX trampolines are an EAAC artifact).
// A throwaway D3D11 device + swapchain is created inside DllMain to grab the
// SHARED IDXGISwapChain class vtables; Present (slot 8) is swapped on both
// the blt-model and flip-model vtables. Every swapchain in the process —
// existing or created later, by ANY creation path — dispatches Present
// through the patched slot. The menu overlay is rendered from PresentHook.
// ---------------------------------------------------------------------------
#include <windows.h>
#include <d3d11.h>
#include <dxgi.h>

#include "config.h"

// The IAT lands in .rdata (read-only by default). The driver resolves
// imports by writing the IAT directly inside the mapped image — no loader,
// no NtProtectVirtualMemory (image-section views can't raise protection
// above the section characteristics, and EAAC's DynamicCodePolicy blocks
// adding execute anyway). Make .rdata writable at build time instead.
#pragma comment(linker, "/section:.rdata,RW")

// --- driver contract (kmap_thunk.asm / driver mapper.cpp) ------------------
// kmap_ctx is filled by the driver: {origRip, entry, dllBase}. kmap_marker
// is the success byte the driver polls after hijacking a thread into
// kmap_thunk.
extern "C" {
    extern unsigned long long kmap_ctx[3];   // defined in kmap_thunk.asm
    __declspec(dllexport) volatile unsigned char kmap_marker = 0;
}

// Overlay API (overlay.cpp)
bool Overlay_Init(void* swapchain, HWND hwnd);
bool Overlay_RenderFrame(void* swapchain);
bool Overlay_IsMenuOpen();
void Overlay_ToggleMenu();

static HMODULE g_Module = nullptr;
static HWND    g_hwnd = nullptr;

// --- logging ---------------------------------------------------------------
static void Log(const char* msg)
{
#ifdef GW2_LOG_PATH
    HANDLE h = CreateFileA(GW2_LOG_PATH, FILE_APPEND_DATA,
        FILE_SHARE_READ | FILE_SHARE_WRITE, nullptr, OPEN_ALWAYS,
        FILE_ATTRIBUTE_NORMAL, nullptr);
    if (h != INVALID_HANDLE_VALUE) {
        SetFilePointer(h, 0, nullptr, FILE_END);
        DWORD w = 0;
        WriteFile(h, msg, (DWORD)strlen(msg), &w, nullptr);
        WriteFile(h, "\r\n", 2, &w, nullptr);
        CloseHandle(h);
    }
#endif
}

// --- Vectored Exception Handler --------------------------------------------
// Manual-mapped DLLs are not in the loader's module tables, so SEH unwinding
// can't find our handlers by default. A VEH is registered via API and works
// regardless; RegisterFunctionTable (DllMain) additionally wires up
// __try/__except. The VEH also resolves the faulting module for diagnostics.
typedef BOOL (WINAPI *pfn_GetModuleHandleExA)(DWORD, LPCSTR, HMODULE*);
typedef DWORD (WINAPI *pfn_GetModuleFileNameA)(HMODULE, LPSTR, DWORD);
static pfn_GetModuleHandleExA  g_GetModuleHandleExA = nullptr;
static pfn_GetModuleFileNameA  g_GetModuleFileNameA = nullptr;

static void ResolveDiag()
{
    HMODULE k32 = GetModuleHandleA("kernel32.dll");
    if (!k32) return;
    g_GetModuleHandleExA = (pfn_GetModuleHandleExA)GetProcAddress(k32, "GetModuleHandleExA");
    g_GetModuleFileNameA = (pfn_GetModuleFileNameA)GetProcAddress(k32, "GetModuleFileNameA");
}

static void LogHex(const char* prefix, ULONG_PTR v)
{
    char b[96];
    int pos = 0;
    while (prefix[pos]) { b[pos] = prefix[pos]; pos++; }
    b[pos++] = '0'; b[pos++] = 'x';
    for (int shift = 60; shift >= 0; shift -= 4) {
        int nib = (int)((v >> shift) & 0xF);
        b[pos++] = "0123456789ABCDEF"[nib];
    }
    b[pos] = '\0';
    Log(b);
}

static void LogModuleOf(const char* tag, ULONG_PTR rip)
{
    HMODULE h = nullptr;
    char path[MAX_PATH];
    if (g_GetModuleHandleExA && g_GetModuleFileNameA &&
        g_GetModuleHandleExA(0x6 /*FROM_ADDRESS|UNCHANGED*/, (LPCSTR)rip, &h) && h &&
        g_GetModuleFileNameA(h, path, MAX_PATH)) {
        const char* name = path;
        for (const char* p = path; *p; p++) if (*p == '\\') name = p + 1;
        char b[384];
        int pos = 0;
        while (tag[pos]) b[pos] = tag[pos], pos++;
        b[pos++] = ':'; b[pos++] = ' ';
        for (int i = 0; name[i]; i++) b[pos++] = name[i];
        b[pos++] = '+'; b[pos++] = '0'; b[pos++] = 'x';
        ULONG_PTR off = rip - (ULONG_PTR)h;
        for (int shift = 60; shift >= 0; shift -= 4) b[pos++] = "0123456789ABCDEF"[(off >> shift) & 0xF];
        b[pos] = 0;
        Log(b);
    } else {
        char b[96];
        LogHex(tag, rip);
    }
}

static LONG WINAPI VectoredHandler(PEXCEPTION_POINTERS ex)
{
    DWORD code = ex->ExceptionRecord->ExceptionCode;
    if (code == 0x406D1388) return EXCEPTION_CONTINUE_SEARCH; // thread name — ignore
    if (code == 0xC00000FF) return EXCEPTION_CONTINUE_SEARCH; // buffer overflow — game's own
    LogHex("VEH code:", code);
    LogModuleOf("VEH rip", (ULONG_PTR)ex->ContextRecord->Rip);
    return EXCEPTION_CONTINUE_SEARCH;
}

// --- Present hook ----------------------------------------------------------
// Installed on the shared IDXGISwapChain class vtables (blt + flip model).
typedef HRESULT(__stdcall* PresentFn)(IDXGISwapChain*, UINT, UINT);

static void**    g_scVtableBlt     = nullptr;  // CDXGISwapChain (DISCARD/SEQUENTIAL)
static void**    g_scVtableFlip    = nullptr;  // flip-model class (FLIP_*)
static PresentFn g_origPresentBlt  = nullptr;
static PresentFn g_origPresentFlip = nullptr;

static PresentFn OrigPresentOf(IDXGISwapChain* sc)
{
    void** vt = *(void***)sc;
    if (vt == g_scVtableBlt && g_origPresentBlt)   return g_origPresentBlt;
    if (vt == g_scVtableFlip && g_origPresentFlip) return g_origPresentFlip;
    return g_origPresentFlip ? g_origPresentFlip : g_origPresentBlt;
}

static HRESULT __stdcall PresentHook(IDXGISwapChain* sc, UINT sync, UINT flags)
{
    static bool initialized = false;
    static LONG presentCount = 0;

    LONG n = InterlockedIncrement(&presentCount);
    if (n <= 5) {
        char b[64] = "Present entered #";
        int pos = 16;
        int v = n;
        char tmp[8]; int tp = 0;
        if (v == 0) tmp[tp++] = '0';
        while (v > 0) { tmp[tp++] = '0' + (v % 10); v /= 10; }
        while (tp > 0) b[pos++] = tmp[--tp];
        b[pos] = '\0';
        Log(b);
    }
    if (!initialized && presentCount >= 30) {
        __try {
            if (!g_hwnd) {
                DXGI_SWAP_CHAIN_DESC scd;
                if (SUCCEEDED(sc->GetDesc(&scd))) g_hwnd = scd.OutputWindow;
            }
            if (g_hwnd) {
                if (Overlay_Init(sc, g_hwnd)) initialized = true;
                else Log("Overlay_Init returned false");
            }
        } __except (EXCEPTION_EXECUTE_HANDLER) {
            Log("EXCEPTION inside Overlay_Init");
        }
    }

    // Edge-trigger on HELD state (not the 0x1 transition bit — the game's own
    // input polling consumes that). Latch so we toggle exactly once per press.
    {
        static bool f1Down = false;
        SHORT k = GetAsyncKeyState(GW2_MENU_KEY);
        if (k & 0x8000) {
            if (!f1Down) {
                f1Down = true;
                Overlay_ToggleMenu();
                Log(Overlay_IsMenuOpen() ? "F1: menu OPEN" : "F1: menu CLOSED");
            }
        } else {
            f1Down = false;
        }
    }

    if (initialized) {
        __try {
            Overlay_RenderFrame(sc);
        } __except (EXCEPTION_EXECUTE_HANDLER) {
            static bool loggedExc = false;
            if (!loggedExc) { loggedExc = true; Log("EXCEPTION inside RenderFrame"); }
        }
    }

    PresentFn orig = OrigPresentOf(sc);
    if (!orig) return S_OK;
    return orig(sc, sync, flags);
}

// --- vtable slot patch (data-page write, no code patch, no RWX) -----------
static bool PatchPointer(void** slot, void* value)
{
    DWORD old = 0;
    if (!VirtualProtect(slot, sizeof(void*), PAGE_READWRITE, &old)) return false;
    *slot = value;
    VirtualProtect(slot, sizeof(void*), old, &old);
    return true;
}

// --- swapchain Present pre-patch via dummy device -------------------------
// Creates a throwaway D3D11 device + swapchain (twice: blt model and flip
// model — they are distinct dxgi classes with distinct vtables), patches
// Present (slot 8) on each class vtable, then releases everything. The
// vtables live in dxgi.dll's .rdata and are shared by ALL swapchain
// instances in the process — existing ones and any created later via any
// API path (D3D11CreateDeviceAndSwapChain, IDXGIFactory::CreateSwapChain,
// CreateSwapChainForHwnd, CreateSwapChainForCoreWindow).
static void PatchPresentVtable()
{
    WNDCLASSA wc = {};
    wc.lpfnWndProc = DefWindowProcA;
    wc.lpszClassName = "gw2d";
    RegisterClassA(&wc);
    HWND hwnd = CreateWindowExA(0, "gw2d", "", WS_POPUP, 0, 0, 1, 1,
        nullptr, nullptr, nullptr, nullptr);
    if (!hwnd) { Log("dummy window failed"); return; }

    const DXGI_SWAP_EFFECT effects[2] = {
        DXGI_SWAP_EFFECT_DISCARD,      // blt-model class
        DXGI_SWAP_EFFECT_FLIP_DISCARD  // flip-model class
    };
    int patched = 0;

    for (int i = 0; i < 2; i++) {
        DXGI_SWAP_CHAIN_DESC scd = {};
        scd.BufferCount = (effects[i] == DXGI_SWAP_EFFECT_FLIP_DISCARD) ? 2 : 1;
        scd.BufferDesc.Format = DXGI_FORMAT_B8G8R8A8_UNORM;
        scd.BufferDesc.ScanlineOrdering = DXGI_MODE_SCANLINE_ORDER_UNSPECIFIED;
        scd.BufferDesc.Scaling = DXGI_MODE_SCALING_UNSPECIFIED;
        scd.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
        scd.OutputWindow = hwnd;
        scd.SampleDesc.Count = 1;
        scd.Windowed = TRUE;
        scd.SwapEffect = effects[i];

        IDXGISwapChain*      sc     = nullptr;
        ID3D11Device*        dev    = nullptr;
        ID3D11DeviceContext* devCtx = nullptr;
        D3D_FEATURE_LEVEL    fl     = {};

        HRESULT hr = D3D11CreateDeviceAndSwapChain(
            nullptr, D3D_DRIVER_TYPE_HARDWARE, nullptr, 0,
            nullptr, 0, D3D11_SDK_VERSION, &scd, &sc, &dev, &fl, &devCtx);
        if (FAILED(hr) || !sc) {
            Log("dummy device create failed");
            continue;
        }

        void** vt = *(void***)sc;
        if (vt == g_scVtableBlt || vt == g_scVtableFlip) {
            // same class as an earlier dummy — already patched
            if (devCtx) devCtx->Release();
            if (dev) dev->Release();
            sc->Release();
            continue;
        }

        PresentFn orig = (PresentFn)vt[8];
        if (PatchPointer(&vt[8], (void*)&PresentHook)) {
            if (!g_scVtableBlt) {
                g_scVtableBlt    = vt;
                g_origPresentBlt = orig;
            } else {
                g_scVtableFlip    = vt;
                g_origPresentFlip = orig;
            }
            patched++;
        }

        if (devCtx) devCtx->Release();
        if (dev) dev->Release();
        sc->Release();
    }

    DestroyWindow(hwnd);

    if (patched > 0) {
        Log("Present vtable patched (dummy device)");
        LogHex("  blt vtable:  ", (ULONG_PTR)g_scVtableBlt);
        LogHex("  flip vtable: ", (ULONG_PTR)g_scVtableFlip);
    } else {
        Log("Present vtable patch FAILED");
    }
}

// --- stage marker files -----------------------------------------------------
// One file per init milestone. The driver log can get wiped by the loader on
// failure — these files survive and show EXACTLY how far DllMain got.
static void WriteStage(const char* path)
{
    HANDLE h = CreateFileA(path, GENERIC_WRITE, 0, nullptr, CREATE_ALWAYS,
        FILE_ATTRIBUTE_NORMAL, nullptr);
    if (h != INVALID_HANDLE_VALUE) CloseHandle(h);
}

// --- register unwind info ---------------------------------------------------
// Raw-mapped images are not in the loader's module list, so SEH unwinding
// can't find our RUNTIME_FUNCTIONs. RtlAddFunctionTable registers .pdata so
// __try/__except inside the payload actually works.
static void RegisterFunctionTable(HMODULE h)
{
    __try {
        auto dos = (IMAGE_DOS_HEADER*)h;
        auto nt  = (IMAGE_NT_HEADERS64*)((BYTE*)h + dos->e_lfanew);
        auto& d  = nt->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_EXCEPTION];
        if (!d.VirtualAddress || d.Size < sizeof(RUNTIME_FUNCTION)) return;
        ULONG count = d.Size / sizeof(RUNTIME_FUNCTION);
        RtlAddFunctionTable((PRUNTIME_FUNCTION)((BYTE*)h + d.VirtualAddress),
                            count, (DWORD64)h);
    } __except (EXCEPTION_EXECUTE_HANDLER) {
        Log("RegisterFunctionTable faulted");
    }
}

// --- inline init (no worker thread) ----------------------------------------
// EAAC hooks BaseThreadInitThunk / RtlUserThreadStart / RtlCreateUserThread
// and flags new threads created from non-backed code. All hook setup is
// fast and runs inline in DllMain.
static void InitHooks()
{
    PatchPresentVtable();
}

// --- DllMain ---------------------------------------------------------------
BOOL WINAPI DllMain(HINSTANCE hInst, DWORD reason, LPVOID)
{
    if (reason != DLL_PROCESS_ATTACH) return TRUE;

    g_Module = hInst;
    AddVectoredExceptionHandler(1, VectoredHandler);
    WriteStage("C:\\Users\\Public\\gw2_stage_1_dllmain.txt");
    Log("DllMain entered");
    ResolveDiag();

    RegisterFunctionTable(hInst);
    WriteStage("C:\\Users\\Public\\gw2_stage_2_functable.txt");

    // Success marker — the driver polls this byte (exported as kmap_marker).
    kmap_marker = 0x42;
    WriteStage("C:\\Users\\Public\\gw2_stage_3_marker.txt");

    // All hook setup inline — no CreateThread (EAAC thread-create hooks).
    InitHooks();

    WriteStage("C:\\Users\\Public\\gw2_stage_4_done.txt");
    Log("DllMain complete");
    return TRUE;
}
