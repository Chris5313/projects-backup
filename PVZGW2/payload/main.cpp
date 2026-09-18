// ---------------------------------------------------------------------------
// GW2 internal payload — entry point.
//
// Pipeline: driver manual-maps this DLL into GW2.Main_Win64_Retail.exe,
//           calls DllMain (DLL_PROCESS_ATTACH) via thread hijack.
//           DllMain spawns a worker that hooks every DXGI swapchain creation
//           path (D3D11CreateDeviceAndSwapChain + IDXGIFactory::CreateSwapChain
//           + CreateSwapChainForHwnd) BEFORE Frostbite's renderer init, then
//           hooks IDXGISwapChain::Present to render an ImGui overlay.
// ---------------------------------------------------------------------------
#include <windows.h>
#include <d3d11.h>
#include <dxgi.h>
#include <dxgi1_2.h>

#include "MinHook.h"
#include "config.h"
#include "gw2_offsets.h"
#include "gw2_offset_data.h"
#include "gw2_entities.h"
#include "gw2_trigger.h"
#include "gw2_viewproj.h"
#include "gw2_dumper.h"
#include "pe_dumper.h"
#include "gw2_probe.h"
#include "gw2_entfinder.h"
#include "fb_dumper.h"
bool Overlay_Init(void* swapchain, HWND hwnd);
bool Overlay_RenderFrame(void* swapchain);
bool Overlay_IsMenuOpen();
void Overlay_ToggleMenu();
namespace scanner { void RunScan(); void RunLightScan(); }

static HMODULE g_Module = nullptr;
static void*  g_swapchain = nullptr;
static HWND   g_hwnd = nullptr;

// --- logging ---------------------------------------------------------------
// One persistent append handle, one WriteFile per line. FILE_APPEND_DATA
// writes are atomic at EOF, so concurrent VEH invocations on different
// threads can interleave whole lines but never corrupt them (the old
// open/write/close twice-per-line version lost lines under load).
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
        if (prev != INVALID_HANDLE_VALUE) { CloseHandle(h); } // race loser
    }
    char buf[640];
    size_t n = 0;
    while (msg[n] && n < sizeof buf - 3) { buf[n] = msg[n]; n++; }
    buf[n++] = '\r'; buf[n++] = '\n';
    DWORD w = 0;
    WriteFile(hLog, buf, (DWORD)n, &w, nullptr);
#endif
}

// --- Vectored Exception Handler --------------------------------------------
// Manual-mapped DLLs have no PE exception directory, so __try/__except does
// NOT work. A VEH is registered via API and works regardless.
// This version also resolves the module name + offset for the faulting RIP
// so we know whether the crash is in EAAC, dxgi, or the game itself.
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
        LogHex(tag, rip);
    }
}

// VEH state: reentry guard + total log cap (first-chance exceptions are
// common while the game loads; unbounded logging slows it into the ground)
static volatile LONG g_vehBusy = 0;
static volatile LONG g_vehLogged = 0;
static LONG WINAPI VectoredHandler(PEXCEPTION_POINTERS ex)
{
    DWORD code = ex->ExceptionRecord->ExceptionCode;
    if (code == 0x406D1388) return EXCEPTION_CONTINUE_SEARCH; // thread name — ignore
    if (code == 0xC00000FF) return EXCEPTION_CONTINUE_SEARCH; // buffer overflow — game's own
    // Guard-page (0x80000001) = normal stack growth — hundreds during load.
    // Stack overflow (0xC00000FD) = the thread's stack is exhausted; logging
    // it needs stack we don't have → nested fault → VEH death spiral. Both
    // are passed through untouched, never logged.
    if (code == 0x80000001 || code == 0xC00000FD) return EXCEPTION_CONTINUE_SEARCH;
    // Reentry guard: if an exception fires while we're already inside this
    // handler (fault inside the logging code), return immediately — never
    // recurse.
    if (InterlockedCompareExchange(&g_vehBusy, 1, 0) != 0) return EXCEPTION_CONTINUE_SEARCH;
    // Rate cap: first-chance exceptions are common during loading; after 48
    // lines we stay silent so a storm can't slow the game to death.
    if (InterlockedIncrement(&g_vehLogged) <= 48) {
        LogHex("VEH code:", code);
        LogModuleOf("VEH rip", (ULONG_PTR)ex->ContextRecord->Rip);
    }
    g_vehBusy = 0;
    return EXCEPTION_CONTINUE_SEARCH;
}
// --- Present hook ----------------------------------------------------------
// --- F2 scan trampoline: clears the busy flag when the scan finishes ------
static volatile LONG g_scanBusy = 0;   // 1 while RunScan is executing
static volatile LONG g_scanBusyStamp = 0; // GetTickCount when g_scanBusy was set
static volatile LONG g_scanQueued = 0; // F2 pressed during a running scan

// ScanThread re-checks g_scanQueued: a queued press chains the next scan
// immediately after the current one completes (no Present-hook timing
// dependency for the follow-up).
static DWORD WINAPI ScanThread(void*)
{
    do {
        InterlockedExchange(&g_scanQueued, 0);
        scanner::RunScan();
    } while (InterlockedCompareExchange(&g_scanQueued, 1, 0) == 1);
    InterlockedExchange(&g_scanBusy, 0);
    InterlockedExchange(&g_scanBusyStamp, 0);
    return 0;
}

// Light-scan thread (auto-refresh): entity discovery without the 5 GB dump.
// If an F2 lands while it runs (g_scanQueued), follow up with a FULL scan.
static DWORD WINAPI LightScanThread(void*)
{
    // ALL-exits busy-clear: if RunLightScan faults (outside any SEH), the
    // old thread died with g_scanBusy still 1 and every future auto-scan
    // was blocked forever — the 9/9 session shows exactly 2 LSCAN lines and
    // then silence while the match went on (only the first few entities
    // ever rendered). SEH + the timestamp watchdog below make a stuck flag
    // impossible.
    __try {
        InterlockedExchange(&g_scanQueued, 0);
        scanner::RunLightScan();
        // ONE line per scan (rare, no per-frame I/O): proves discovery ran
        // and what the table looks like.
        {
            char b[64]; size_t k = 0;
            const char* t = "LSCAN done ent=";
            while (t[k]) { b[k] = t[k]; k++; }
            int n = ent::EntityCount();
            char nb[12]; int nl = 0;
            if (n <= 0) nb[nl++] = '0';
            while (n > 0 && nl < 11) { nb[nl++] = (char)('0' + n % 10); n /= 10; }
            for (int i = nl - 1; i >= 0; i--) b[k++] = nb[i];
            b[k] = 0;
            Log(b);
        }
        if (InterlockedCompareExchange(&g_scanQueued, 1, 0) == 1) {
            do {
                InterlockedExchange(&g_scanQueued, 0);
                scanner::RunScan();
            } while (InterlockedCompareExchange(&g_scanQueued, 1, 0) == 1);
        }
    } __except (EXCEPTION_EXECUTE_HANDLER) {
        Log("LSCAN: EXCEPTION — scan thread survived, flag cleared");
    }
    InterlockedExchange(&g_scanBusy, 0);
    InterlockedExchange(&g_scanBusyStamp, 0);
    return 0;
}
// --- F3 auto-dump pair: scan 1 -> 15s window (HP changes) -> scan 2 -> DONE --
// One keypress produces the two-dump pair the offline HP diff needs. The
// status line at the top-left (drawn by overlay.cpp) walks through the phases
// so the user knows exactly when it is safe to close the game.
static volatile LONG g_autoBusy = 0;
static char g_autoStatus[128] = "AUTO: idle (F3 = HP dump pair)";

namespace autodump {
const char* StatusText() { return g_autoStatus; }
}

static void AutoSet(const char* msg)
{
    size_t k = 0;
    while (msg[k] && k < sizeof g_autoStatus - 1) { g_autoStatus[k] = msg[k]; k++; }
    g_autoStatus[k] = 0;
}

static void AutoCountdown(int secs)
{
    char b[96]; int n = 0;
    const char* m = "AUTO: dump 2 in ..s -- TAKE HITS NOW";
    while (m[n]) { b[n] = m[n]; n++; }
    b[22] = (char)('0' + (secs / 10));
    b[23] = (char)('0' + (secs % 10));
    b[n] = 0;
    AutoSet(b);
}

static DWORD WINAPI AutoDumpThread(void*)
{
    // a manual F2 scan may be mid-flight; RunScan's file handles are globals,
    // so two concurrent RunScan calls would corrupt each other. Wait it out,
    // then hold g_scanBusy ourselves so F2 stays locked out for the pair.
    if (InterlockedCompareExchange(&g_scanBusy, 1, 0) != 0) {
        AutoSet("AUTO: waiting for running scan...");
        while (InterlockedCompareExchange(&g_scanBusy, 1, 0) != 0) Sleep(500);
    }

    Log("AUTO — dump pair started (F3)");
    AutoSet("AUTO: dump 1 running...");
    scanner::RunScan();                       // dump 1 (numbering skips
    Log("AUTO — dump 1 done");                // existing files on disk)

    for (int s = 15; s > 0; s--) {            // 15s window: HP must change
        AutoCountdown(s);
        Sleep(1000);
    }

    AutoSet("AUTO: dump 2 running...");
    scanner::RunScan();                       // dump 2
    Log("AUTO — dump 2 done");
    InterlockedExchange(&g_scanBusy, 0);      // release before the banner:

    // marker file: the offline side can poll for it instead of guessing
    HANDLE h = CreateFileA("C:\\Users\\Public\\gw2_hp_pair_done.txt",
        GENERIC_WRITE, FILE_SHARE_READ, nullptr, CREATE_ALWAYS,
        FILE_ATTRIBUTE_NORMAL, nullptr);
    if (h != INVALID_HANDLE_VALUE) {
        DWORD w = 0;
        WriteFile(h, "auto pair complete\r\n", 20, &w, nullptr);
        CloseHandle(h);
    }
    AutoSet("HP DUMPS DONE -- safe to close game");
    Log("AUTO — pair complete, marker written");
    InterlockedExchange(&g_autoBusy, 0);
    return 0;
}

typedef HRESULT(__stdcall* PresentFn)(IDXGISwapChain*, UINT, UINT);
static PresentFn g_origPresent = nullptr;

static HRESULT __stdcall PresentHook(IDXGISwapChain* sc, UINT sync, UINT flags)
{
    static bool initialized = false;
    static LONG presentCount = 0;

    LONG n = InterlockedIncrement(&presentCount);

    if (n == 1) Log("Present hooked — first frame");
    if (!initialized && presentCount >= 30) {
        __try {
            // v4 logic: take the hwnd from the REAL game swapchain, never
            // trust g_hwnd — it may hold the destroyed 1x1 dummy window
            // (our own dummy D3D11CreateDeviceAndSwapChain call goes through
            // our MinHook and sets g_hwnd = desc->OutputWindow = dummy).
            DXGI_SWAP_CHAIN_DESC scd;
            if (SUCCEEDED(sc->GetDesc(&scd)) && scd.OutputWindow)
                g_hwnd = scd.OutputWindow;
            if (g_hwnd) {
                if (Overlay_Init(sc, g_hwnd)) initialized = true;
                else Log("Overlay_Init returned false");
            }
        } __except (EXCEPTION_EXECUTE_HANDLER) {
            Log("EXCEPTION inside Overlay_Init");
        }
    }

    // F1 — menu toggle (edge-trigger on HELD state, not the 0x1 transition
    // bit — the game's own GetAsyncKeyState polling resets it)
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
    // F2 — entity scan + heap dump. Each press writes its own numbered file
    // set (gw2_scan_N.txt / gw2_heapdump_N.bin / gw2_heapmap_N.txt).
    // A press while a scan is RUNNING is remembered (g_scanQueued) and fires
    // a fresh scan the moment the current one finishes — the freeze made
    // mid-scan presses look dead (run B: 3 presses, 1 scan).
    {
        static bool f2Down = false;
        SHORT k = GetAsyncKeyState(0x71);
        if (k & 0x8000) {
            if (!f2Down) {
                f2Down = true;
                if (g_autoBusy) {
                    Log("F2 — ignored (auto pair running)");
                } else if (InterlockedCompareExchange(&g_scanBusy, 1, 0) == 0) {
                    Log("F2 — scan thread launched");
                    CreateThread(nullptr, 0, ScanThread, nullptr, 0, nullptr);
                } else {
                    InterlockedExchange(&g_scanQueued, 1);
                    Log("F2 — queued (scan running)");
                }
            }
        } else {
            f2Down = false;
        }
    }

    // F3 — BACKPOINTER SCANNER: Find what points TO our entities
    // NO GOING BACK TO HEAP SCAN - THIS FINDS THE REAL ACCESSOR CHAIN
    {
        static bool f3Down = false;
        SHORT k = GetAsyncKeyState(0x72);
        if (k & 0x8000) {
            if (!f3Down) {
                f3Down = true;
                Log("F3 - BACKPOINTER SCAN - finding real entity accessor chain...");
                ent::RunBackpointerScan();
            }
        } else {
            f3Down = false;
        }
    }

    // F4 - FORWARD TRACE from GameContext (the right way)
    {
        static bool f4Down = false;
        SHORT k = GetAsyncKeyState(0x73);
        if (k & 0x8000) {
            if (!f4Down) {
                f4Down = true;
                Log("F4 - FORWARD TRACE from GameContext...");
                ent::TraceFromGameContext();
            }
        } else {
            f4Down = false;
        }
    }

#ifndef GW2_RELEASE
    // F5 — local-entity dump (debug only): full hex of the detected local
    // player (charObj / state / components) + all weapon containers, to
    // C:\Users\Public\gw2_local_dump.txt.
    {
        static bool f5Down = false;
        SHORT k = GetAsyncKeyState(0x74);
        if (k & 0x8000) {
            if (!f5Down) {
                f5Down = true;
                ent::DumpLocal();
                Log("F5 — local entity dump written");
            }
        } else {
            f5Down = false;
        }
    }

    // F6 — triggerbot toggle (debug only; release uses the Combat checkbox).
    {
        static bool f6Down = false;
        SHORT k = GetAsyncKeyState(0x75);
        if (k & 0x8000) {
            if (!f6Down) {
                f6Down = true;
                trigger::SetEnabled(!trigger::IsEnabled());
                Log(trigger::IsEnabled() ? "F6: triggerbot ON" : "F6: triggerbot OFF");
            }
        } else {
            f6Down = false;
        }
    }
#endif

    // F7 — dump game PE for IDA analysis
    {
        static bool f7Down = false;
        SHORT k = GetAsyncKeyState(0x76);
        if (k & 0x8000) {
            if (!f7Down) {
                f7Down = true;
                const char* path = "C:\\Users\\Public\\gw2_game.exe";
                if (DumpGamePEFixed(path)) {
                    Log("F7 — PE dumped to C:\\Users\\Public\\gw2_game.exe");
                } else {
                    Log("F7 — PE dump FAILED");
                }
            }
        } else {
            f7Down = false;
        }
    }

    // F8 — probe GameContext structure (runtime discovery)
    {
        static bool f8Down = false;
        SHORT k = GetAsyncKeyState(0x77); // VK_F8
        if (k & 0x8000) {
            if (!f8Down) {
                f8Down = true;
                probe::ProbeGameContext();
                Log("F8 — Probe complete, check C:\\Users\\Public\\gw2_probe.log");
            }
        } else {
            f8Down = false;
        }
    }

    // auto-rescan: keep the entity table alive with no key presses. Light
    // pass (no disk I/O) — first run ~8 s after the overlay is up, then
    // every 12 s (9/9 session: 20 s cadence + a scan-thread death left the
    // busy flag stuck = only the first 2-4 entities EVER rendered). A scan
    // also fires the moment the camera locks (slot lock = match loaded =
    // entities exist, don't wait for the interval).
    // WATCHDOG: if g_scanBusy has been held > 60 s the thread is gone —
    // force-clear so discovery continues. (RunLightScan faults were caught
    // by nothing before; the thread wrapper now has SEH, but belt+suspenders.)
    if (initialized) {
        static DWORD nextAuto = 0;
        static bool  first = true;
        DWORD now = GetTickCount();
        LONG busyStamp = g_scanBusyStamp;
        if (g_scanBusy && busyStamp && now - busyStamp > 60000) {
            g_scanBusy = 0; g_scanBusyStamp = 0;
            Log("SCAN WATCHDOG: stuck busy flag force-cleared");
        }
        // DISABLED: Background scanning causes entity list racing
        // Entities are now read synchronously in OnFrame (Present hook)
        // if (vproj::HasViewProj() && g_scanBusy == 0 && !g_autoBusy &&
        //     nextAuto == 0) {
        //     if (InterlockedCompareExchange(&g_scanBusy, 1, 0) == 0) {
        //         g_scanBusyStamp = (LONG)now;
        //         nextAuto = now + 12000;
        //         CreateThread(nullptr, 0, LightScanThread, nullptr, 0, nullptr);
        //     }
        // }
        // if (first) { first = false; if (!nextAuto) nextAuto = now + 8000; }
        // if (nextAuto && now >= nextAuto) {
        //     if (!g_autoBusy &&
        //         InterlockedCompareExchange(&g_scanBusy, 1, 0) == 0) {
        //         g_scanBusyStamp = (LONG)now;
        //         CreateThread(nullptr, 0, LightScanThread, nullptr, 0, nullptr);
        //     }
        //     nextAuto = now + 12000;
        // }
    }

        // F9: trigger memory dump
        static bool dumpTriggered = false;
        if (!dumpTriggered && (GetAsyncKeyState(VK_F9) & 0x8000)) {
            dumpTriggered = true;
            Log("F9 pressed - starting dump...");
            dumper::DumpAll();
        }

    // F10: run entity list finder (scans heap for CharObj vtables)
    {
        static bool f10Down = false;
        SHORT k = GetAsyncKeyState(0x79); // VK_F10
        if (k & 0x8000) {
            if (!f10Down) {
                f10Down = true;
                Log("F10 - Running entity list finder...");
                entfinder::FindEntityList();
                Log("F10 - Done. Check C:\\Users\\Public\\gw2_entfinder.log");
            }
        } else {
            f10Down = false;
        }
    }

    // F11: Frostbite SDK dump (TypeInfo walker)
    {
        static bool f11Down = false;
        SHORT k = GetAsyncKeyState(0x7A); // VK_F11
        if (k & 0x8000) {
            if (!f11Down) {
                f11Down = true;
                Log("F11 - Running Frostbite SDK dump...");
                fb::DumpAllClasses("C:\\Users\\Public\\gw2_sdk_dump.txt");
                // Also search for key classes
                char buf[4096];
                fb::SearchClasses("RenderView", buf, sizeof(buf));
                Log(buf);
                fb::SearchClasses("CameraManager", buf, sizeof(buf));
                Log(buf);
                fb::SearchClasses("ViewProj", buf, sizeof(buf));
                Log(buf);
                Log("F11 - Done. Check C:\\Users\\Public\\gw2_sdk_dump.txt");
            }
        } else {
            f11Down = false;
        }
    }

    if (initialized) {
        static bool loggedRender = false;
        if (!loggedRender) { loggedRender = true; Log("PH: calling Overlay_RenderFrame"); }
        __try {
            Overlay_RenderFrame(sc);
        } __except (EXCEPTION_EXECUTE_HANDLER) {
            static bool loggedExc = false;
            if (!loggedExc) { loggedExc = true; Log("EXCEPTION inside RenderFrame"); }
        }
    }
    return g_origPresent(sc, sync, flags);
}

// --- swapchain capture: swap the Present vtable slot directly ---------------
// Present = IDXGISwapChain VTable index 8.
//
// MinHook refused the factory vtable entries (no hook logs, no menu) — its
// trampoline machinery needs RWX which EAAC's DynamicCodePolicy blocks for
// those paths. Direct vtable pointer swap needs only a data-page write.

static bool PatchPointer(void** slot, void* value)
{
    DWORD old = 0;
    if (!VirtualProtect(slot, sizeof(void*), PAGE_READWRITE, &old)) return false;
    *slot = value;
    VirtualProtect(slot, sizeof(void*), old, &old);
    return true;
}

static void CaptureSwapchain(IDXGISwapChain* sc)
{
    if (!sc || g_swapchain) return;
    g_swapchain = sc;

    void** vtable = *(void***)sc;
    g_origPresent = (PresentFn)vtable[8];
    if (PatchPointer(&vtable[8], (void*)&PresentHook))
        Log("Present vtable swapped");
    else
        Log("Present vtable swap FAILED");
}

// --- D3D11CreateDeviceAndSwapChain hook -----------------------------------
typedef HRESULT (WINAPI *CreateDeviceSwapChainFn)(
    IDXGIAdapter*, D3D_DRIVER_TYPE, HMODULE, UINT,
    const D3D_FEATURE_LEVEL*, UINT, UINT,
    const DXGI_SWAP_CHAIN_DESC*, IDXGISwapChain**, ID3D11Device**,
    D3D_FEATURE_LEVEL*, ID3D11DeviceContext**);

static CreateDeviceSwapChainFn g_origCreateDeviceSwapChain = nullptr;

static HRESULT WINAPI CreateDeviceSwapChainHook(
    IDXGIAdapter* adapter, D3D_DRIVER_TYPE driverType, HMODULE software,
    UINT flags, const D3D_FEATURE_LEVEL* featureLevels, UINT nFeatureLevels,
    UINT sdkVersion, const DXGI_SWAP_CHAIN_DESC* desc,
    IDXGISwapChain** outSwapChain, ID3D11Device** outDevice,
    D3D_FEATURE_LEVEL* outFeatureLevel, ID3D11DeviceContext** outContext)
{
    Log("D3D11CreateDeviceAndSwapChain captured");
    HRESULT hr = g_origCreateDeviceSwapChain(
        adapter, driverType, software, flags, featureLevels, nFeatureLevels,
        sdkVersion, desc, outSwapChain, outDevice, outFeatureLevel, outContext);

    if (SUCCEEDED(hr) && outSwapChain && *outSwapChain) {
        if (desc && desc->OutputWindow) g_hwnd = desc->OutputWindow;
        CaptureSwapchain(*outSwapChain);
    }
    return hr;
}
// --- IDXGIFactory::CreateSwapChain hook (vtable index 10) ------------------
// The factory vtable is shared across instances, so we create our own factory
// and patch its vtable — that patches the global one for every consumer.
typedef HRESULT(STDMETHODCALLTYPE* FactoryCreateSwapChainFn)(
    IDXGIFactory*, IUnknown*, DXGI_SWAP_CHAIN_DESC*, IDXGISwapChain**);

static FactoryCreateSwapChainFn g_origFactoryCreateSwapChain = nullptr;

static HRESULT STDMETHODCALLTYPE FactoryCreateSwapChainHook(
    IDXGIFactory* self, IUnknown* device, DXGI_SWAP_CHAIN_DESC* desc,
    IDXGISwapChain** out)
{
    HRESULT hr = g_origFactoryCreateSwapChain(self, device, desc, out);
    if (SUCCEEDED(hr) && out && *out) {
        if (desc && desc->OutputWindow) g_hwnd = desc->OutputWindow;
        Log("IDXGIFactory::CreateSwapChain captured");
        CaptureSwapchain(*out);
    }
    return hr;
}

// --- IDXGIFactory::CreateSwapChainForHwnd hook (vtable index 15) -----------
typedef HRESULT(STDMETHODCALLTYPE* FactoryCreateSwapChainForHwndFn)(
    IDXGIFactory*, IUnknown*, HWND, const DXGI_SWAP_CHAIN_DESC1*,
    const DXGI_SWAP_CHAIN_FULLSCREEN_DESC*, IDXGIOutput*, IDXGISwapChain1**);

static FactoryCreateSwapChainForHwndFn g_origCreateSwapChainForHwnd = nullptr;

static HRESULT STDMETHODCALLTYPE CreateSwapChainForHwndHook(
    IDXGIFactory* self, IUnknown* device, HWND hwnd,
    const DXGI_SWAP_CHAIN_DESC1* desc, const DXGI_SWAP_CHAIN_FULLSCREEN_DESC* fs,
    IDXGIOutput* restrictToOutput, IDXGISwapChain1** out)
{
    HRESULT hr = g_origCreateSwapChainForHwnd(
        self, device, hwnd, desc, fs, restrictToOutput, out);
    if (SUCCEEDED(hr) && out && *out) {
        if (hwnd) g_hwnd = hwnd;
        Log("CreateSwapChainForHwnd captured");
        CaptureSwapchain((IDXGISwapChain*)*out);
    }
    return hr;
}

typedef HRESULT(WINAPI* CreateDXGIFactory1Fn)(REFIID, void**);

static void HookFactory()
{
    HMODULE dxgi = GetModuleHandleA("dxgi.dll");
    if (!dxgi) dxgi = LoadLibraryA("dxgi.dll");
    if (!dxgi) { Log("dxgi.dll not found"); return; }

    auto createFactory = (CreateDXGIFactory1Fn)
        GetProcAddress(dxgi, "CreateDXGIFactory1");
    if (!createFactory) { Log("CreateDXGIFactory1 not found"); return; }

    IDXGIFactory* factory = nullptr;
    if (FAILED(createFactory(__uuidof(IDXGIFactory), (void**)&factory)) || !factory) {
        Log("CreateDXGIFactory1 failed");
        return;
    }

    // Patch the shared factory vtable — direct pointer swap, no MinHook.
    void** vtable = *(void***)factory;
    g_origFactoryCreateSwapChain = (FactoryCreateSwapChainFn)vtable[10];
    if (PatchPointer(&vtable[10], (void*)&FactoryCreateSwapChainHook))
        Log("IDXGIFactory::CreateSwapChain vtable swapped");
    else
        Log("IDXGIFactory::CreateSwapChain swap FAILED");

    g_origCreateSwapChainForHwnd = (FactoryCreateSwapChainForHwndFn)vtable[15];
    if (PatchPointer(&vtable[15], (void*)&CreateSwapChainForHwndHook))
        Log("CreateSwapChainForHwnd vtable swapped");
    else
        Log("CreateSwapChainForHwnd swap FAILED");

    factory->Release();
}


// --- register unwind info ---------------------------------------------------
// Manually-mapped images are not in the loader's module list, so SEH
// unwinding can't find our RUNTIME_FUNCTIONs. RtlAddFunctionTable registers
// .pdata so __try/__except inside the payload actually works. Without this,
// any exception in payload code becomes an unhandled crash in ntdll.
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

static void InitHooks()
{
    Log("worker started (inline)");

    if (MH_Initialize() != MH_OK) { Log("MH_Initialize failed"); return; }
    HookFactory();

    HMODULE d3d11 = GetModuleHandleA("d3d11.dll");
    if (!d3d11) d3d11 = LoadLibraryA("d3d11.dll");
    if (!d3d11) { Log("d3d11.dll not found"); return; }

    auto create = (CreateDeviceSwapChainFn)
        GetProcAddress(d3d11, "D3D11CreateDeviceAndSwapChain");
    if (!create) { Log("D3D11CreateDeviceAndSwapChain not found"); return; }

    MH_CreateHook(create, &CreateDeviceSwapChainHook, (void**)&g_origCreateDeviceSwapChain);
    MH_EnableHook(create);
    Log("D3D11CreateDeviceAndSwapChain hooked (MinHook)");
    
    // v4 approach: Create dummy swapchain to patch Present vtable
    __try {
        Log("v4: step 1 - registering window class");
        WNDCLASSA wc = {};
        wc.lpfnWndProc = DefWindowProcA;
        wc.lpszClassName = "gw2d";
        RegisterClassA(&wc);
        
        Log("v4: step 2 - creating window");
        HWND hwnd = CreateWindowExA(0, "gw2d", "", WS_POPUP, 0, 0, 1, 1, nullptr, nullptr, nullptr, nullptr);
        
        if (hwnd) {
            Log("v4: step 3 - window created, preparing desc");
            DXGI_SWAP_CHAIN_DESC desc = {};
            desc.BufferCount = 1;
            desc.BufferDesc.Format = DXGI_FORMAT_B8G8R8A8_UNORM;
            desc.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
            desc.OutputWindow = hwnd;
            desc.SampleDesc.Count = 1;
            desc.Windowed = TRUE;
            desc.SwapEffect = DXGI_SWAP_EFFECT_DISCARD;
            
            Log("v4: step 4 - calling D3D11CreateDeviceAndSwapChain");
            IDXGISwapChain* sc = nullptr;
            ID3D11Device* dev = nullptr;
            D3D_FEATURE_LEVEL fl = D3D_FEATURE_LEVEL_11_0;
            
            HRESULT hr = D3D11CreateDeviceAndSwapChain(
                nullptr, D3D_DRIVER_TYPE_HARDWARE, nullptr, 0,
                &fl, 1, D3D11_SDK_VERSION, &desc, &sc, &dev, nullptr, nullptr);
            
            Log("v4: step 5 - D3D11CreateDeviceAndSwapChain returned");
            if (SUCCEEDED(hr) && sc) {
                Log("v4: step 6 - success! Patching Present...");
                CaptureSwapchain(sc);
                if (dev) dev->Release();
                sc->Release();
                Log("v4: step 7 - Present vtable patched!");
            } else {
                Log("v4: step 8 - D3D11CreateDeviceAndSwapChain FAILED");
            }
            Log("v4: step 9 - cleanup window");
            DestroyWindow(hwnd);

            UnregisterClassA("gw2d", nullptr);
            Log("v4: step 10 - COMPLETE");
        } else {
            Log("v4: CreateWindow FAILED");
        }
    } __except(EXCEPTION_EXECUTE_HANDLER) {
        Log("v4: EXCEPTION during vtable patch");
    }
}
// --- DllMain ---------------------------------------------------------------
BOOL WINAPI DllMain(HINSTANCE hInst, DWORD reason, LPVOID)
{
    if (reason != DLL_PROCESS_ATTACH) return TRUE;

    g_Module = hInst;
    AddVectoredExceptionHandler(1, VectoredHandler);

    // Singleton — but ONLY for the overlay/renderer side. The launcher-stub
    // pattern (2026-09-05: stub lives <1s, real game replaces it) means a
    // second manual-map of this payload is usually the REAL process; a bare
    // return TRUE here left the real game with NO hooks at all (the AC:S
    // lines proved it: hook LIVE from the abandoned injection, zero calls in
    // the live one). Second injection now installs the serializer slots +
    // harvester (idempotent: SwapSlot verifies the slot still holds the
    // expected ORIGINAL pointer, so a double-map either re-installs cleanly
    // or logs MISMATCH and leaves it), while skipping only the duplicate
    // overlay/vtable work below.
    HANDLE mutex = CreateMutexA(nullptr, TRUE, "Global\\GW2_Payload_Singleton");
    DWORD merr = GetLastError();   // capture BEFORE any other API call
    bool secondInjection = (mutex && merr == ERROR_ALREADY_EXISTS);
    // Diagnostic line (2026-09-08 double-DllMain mystery): image base + boot
    // tick + pid. Two entries with the SAME base = same image entered twice;
    // different bases = two images (stale-frames or double inject); a big
    // tick gap = two separate loader sessions (the payload log appends
    // across sessions, and kmap_status.txt is wiped per session, so only
    // this line can tell them apart). MutexCreateErr is logged so a failed
    // CreateMutexA (which fakes "first injection") is visible.
    {
        char b[160]; int k = 0;
        const char* t = secondInjection
            ? "DllMain: second injection img=" : "DllMain entered img=";
        while (t[k]) { b[k] = t[k]; k++; }
        ULONG_PTR v = (ULONG_PTR)hInst; int sh = 60;
        for (; sh >= 0; sh -= 4) b[k++] = "0123456789ABCDEF"[(v >> sh) & 0xF];
        const char* t2 = " tick=";
        for (int j = 0; t2[j]; j++) b[k++] = t2[j];
        ULONGLONG tk = GetTickCount64(); sh = 60;
        for (; sh >= 0; sh -= 4) b[k++] = "0123456789ABCDEF"[(tk >> sh) & 0xF];
        const char* t3 = " pid=";
        for (int j = 0; t3[j]; j++) b[k++] = t3[j];
        v = (ULONG_PTR)GetCurrentProcessId(); sh = 28;
        for (; sh >= 0; sh -= 4) b[k++] = "0123456789ABCDEF"[(v >> sh) & 0xF];
        const char* t4 = " mutexErr=";
        for (int j = 0; t4[j]; j++) b[k++] = t4[j];
        sh = 28;
        for (; sh >= 0; sh -= 4) b[k++] = "0123456789ABCDEF"[((ULONG_PTR)merr >> sh) & 0xF];
        b[k] = 0;
        Log(b);
    }
    ResolveDiag();

    DisableThreadLibraryCalls(hInst);

    // MUST run before any __try/__except in this image works (manual map).
    RegisterFunctionTable(hInst);

    // Manual-map marker (driver reads this byte to confirm DllMain ran).
    *(volatile unsigned char*)((ULONG_PTR)hInst + 0x100) = 0x42;

    if (secondInjection) {
        // Second image in the SAME process: the first image owns the hooks,
        // the overlay and the Present capture. Running InitHooks again would
        // double-MinHook the same functions (trampoline chains) and re-patch
        // the swapchain vtable — freeze/AV territory. The mutex only exists
        // if the first image's DllMain ran, so if we get here it is alive.
        Log("DllMain: second injection — first image owns the process, no-op");
        return TRUE;
    }

    // All hook setup inline — no CreateThread (EAAC thread-create hooks).
    InitHooks();

    Log("DllMain complete");
    return TRUE;
}
