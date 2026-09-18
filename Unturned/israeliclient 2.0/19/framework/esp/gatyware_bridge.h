#pragma once
// gatyware (1.1 Mono) loader bridge.
//
// Writes the embedded gatywareclient.dll to %TEMP%, loads it into the root
// Mono domain, resolves gatyware.Runtime::Init, and invokes it ONCE on the
// Unity main thread (via the Provider.Update detour in esp_renderer.h).
//
// The render thread is already mono-attached by game::init(); the main thread
// is Unity's own managed thread, so no extra thread_attach is needed.

#include "mono_bridge.h"
#include "gatyware_data.h"
#include "game_data.h"      // game::log
#include <Windows.h>

namespace gatyware {

inline void* g_init_method   = nullptr;
inline bool  g_loaded        = false;   // one-shot: try_load() has run
inline bool  g_init_invoked  = false;   // one-shot: Init() has been invoked
inline void* g_image         = nullptr;  // gatywareclient assembly image
inline void* g_runtime_class = nullptr;  // gatyware.Runtime class

// Append one line to both the framework ESP log (payload_debug.txt via
// game::log) and a dedicated bridge log. No printf-family — hand-formed.
inline void log(const char* msg) {
    game::log(msg);
    HANDLE h = CreateFileA("C:\\ProgramData\\Microsoft\\DeviceSync\\gatyware_bridge.txt",
        FILE_APPEND_DATA, FILE_SHARE_READ | FILE_SHARE_WRITE, nullptr, OPEN_ALWAYS,
        FILE_ATTRIBUTE_NORMAL, nullptr);
    if (h == INVALID_HANDLE_VALUE) return;
    DWORD bw = 0, len = 0;
    while (msg[len]) len++;
    WriteFile(h, msg, len, &bw, nullptr);
    WriteFile(h, "\r\n", 2, &bw, nullptr);
    CloseHandle(h);
}

// Called ONCE from the render thread after mono::init() succeeded and
// game::ready is true. Returns true when Runtime.Init has been resolved.
inline bool try_load() {
    if (g_loaded) return g_init_method != nullptr;
    g_loaded = true;

    if (!mono::g_domain) { log("gatyware: no mono domain"); return false; }

    // 1. Write embedded assembly to %TEMP%\gatywareclient.dll
    char path[512] = {};
    if (GetTempPathA(sizeof(path), path) == 0) { log("gatyware: GetTempPathA failed"); return false; }
    size_t i = 0;
    while (path[i]) i++;
    const char fn[] = "gatywareclient.dll";
    if (i + sizeof(fn) > sizeof(path)) { log("gatyware: temp path too long"); return false; }
    for (size_t j = 0; j < sizeof(fn); j++) path[i + j] = fn[j];  // includes NUL

    HANDLE h = CreateFileA(path, GENERIC_WRITE, 0, nullptr, CREATE_ALWAYS,
        FILE_ATTRIBUTE_NORMAL, nullptr);
    if (h == INVALID_HANDLE_VALUE) { log("gatyware: temp create failed"); return false; }
    DWORD written = 0;
    BOOL ok = WriteFile(h, kGatywareDll, kGatywareDllSize, &written, nullptr);
    CloseHandle(h);
    if (!ok || written != kGatywareDllSize) {
        log("gatyware: temp write failed");
        DeleteFileA(path);
        return false;
    }
    log("gatyware: temp assembly written");

    // 2. Load into root domain, then delete the temp file immediately.
    void* assembly = mono::domain_assembly_open(mono::g_domain, path);
    DeleteFileA(path);
    if (!assembly) { log("gatyware: domain_assembly_open failed"); return false; }
    void* img = mono::assembly_get_image(assembly);
    if (!img) { log("gatyware: assembly_get_image failed"); return false; }
    g_image = img;

    void* rt = mono::class_from_name(img, "gatyware", "Runtime");
    if (!rt) { log("gatyware: class gatyware.Runtime not found"); return false; }
    g_runtime_class = rt;

    g_init_method = mono::class_get_method_from_name(rt, "Init", 0);
    if (!g_init_method) { log("gatyware: Runtime.Init not found"); return false; }

    log("gatyware: Runtime.Init resolved");
    return true;
}

// One-shot: invoke gatyware.Runtime.Init() on the Unity main thread.
// Called from hk_provider_update (Provider.Update detour).
inline void invoke_init_on_main_thread() {
    if (g_init_invoked || !g_init_method) return;   // retry each frame until resolved
    g_init_invoked = true;

    void* exc = nullptr;
    mono::runtime_invoke(g_init_method, nullptr, nullptr, &exc);
    if (exc) { log("gatyware: Runtime.Init threw"); return; }
    log("gatyware: Runtime.Init invoked on main thread");
}

} // namespace gatyware
