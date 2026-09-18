// kmap payload — Phase 3.2 (Mono handshake + managed client loader)
//
// Architecture: driver injects when mono-2.0-bdwgc.dll is in the PEB.
// Payload runs a worker thread that:
//   1. Mono handshake — resolve exports, get root domain, attach thread
//   2. Poll for Assembly-CSharp image (up to 60 s)
//   3. Poll for SDG.Unturned.Provider class (game code ready)
//   4. Load Daou.dll, find Daou.Daou1.Daou2, invoke it
//      Daou2() hops to Unity's main thread via logMessageReceived.
//
// DllMain returns in microseconds — all work on the worker thread.

#include <windows.h>

// ---------------------------------------------------------------------------
// Mono function pointers — signatures verified against Unturned's own
// mono-2.0-bdwgc.dll exports.
// ---------------------------------------------------------------------------
using fn_get_root_domain      = void*       (*)();
using fn_thread_attach        = void*       (*)(void* domain);
using fn_image_loaded         = void*       (*)(const char* name);
using fn_image_get_name       = const char* (*)(void* image);
using fn_assembly_get_image   = void*       (*)(void* assembly);
using fn_class_from_name      = void*       (*)(void* image, const char* ns, const char* name);
using fn_asm_iter_cb          = void        (*)(void* assembly, void* user);
using fn_assembly_foreach     = void        (*)(fn_asm_iter_cb, void* user);
using fn_domain_assembly_open  = void*       (*)(void* domain, const char* path, void* status);
using fn_class_get_method_from_name = void*  (*)(void* klass, const char* name, int paramCount);
using fn_runtime_invoke        = void*       (*)(void* method, void* obj, void** params, void** exc);
using fn_compile_method        = void*       (*)(void* method);
using fn_object_new            = void*       (*)(void* domain, void* klass);
using fn_string_new            = void*       (*)(void* domain, const char* text);
using fn_class_get_type        = void*       (*)(void* klass);
using fn_type_get_object       = void*       (*)(void* domain, void* type);
// ---------------------------------------------------------------------------
// File logging — hand-formed strings only, no printf-family (BE sig scan).
// ---------------------------------------------------------------------------
static const wchar_t kLogPath[] = L"C:\\ProgramData\\Microsoft\\DeviceSync\\kmap_mono.txt";

static void LogAppend(const char* line)
{
    HANDLE h = CreateFileW(kLogPath,
        FILE_APPEND_DATA, FILE_SHARE_READ | FILE_SHARE_WRITE,
        nullptr, OPEN_ALWAYS, FILE_ATTRIBUTE_NORMAL, nullptr);
    if (h == INVALID_HANDLE_VALUE) return;
    SetFilePointer(h, 0, nullptr, FILE_END);

    size_t n = 0;
    while (line[n]) n++;

    DWORD written = 0;
    WriteFile(h, line, (DWORD)n, &written, nullptr);
    WriteFile(h, "\r\n", 2, &written, nullptr);
    CloseHandle(h);
}

static void HexPtr(ULONG_PTR val, char* out)
{
    static const char kHex[] = "0123456789ABCDEF";
    out[0] = '0';
    out[1] = 'x';
    for (int i = 0; i < 16; i++) {
        out[2 + i] = kHex[(val >> ((15 - i) * 4)) & 0xF];
    }
    out[18] = '\0';
}

static void LogHex(const char* prefix, ULONG_PTR val)
{
    char buf[19];
    HexPtr(val, buf);
    LogAppend(prefix);
    LogAppend(buf);
}

// ---------------------------------------------------------------------------
// Diagnostic enumeration — dump every currently-loaded assembly's image name.
// Used on timeout to explain why a poll gave up.
// ---------------------------------------------------------------------------
struct MonoAPI {
    fn_assembly_get_image assembly_get_image;
    fn_image_get_name     image_get_name;
};
static MonoAPI g_api;

static void AsmForeachCallback(void* assembly, void* /*user*/)
{
    if (!assembly) return;
    void* img = g_api.assembly_get_image(assembly);
    if (!img) return;
    const char* nm = g_api.image_get_name(img);
    LogAppend(nm ? nm : "(unnamed)");
}

static void DumpLoadedAssemblies(fn_assembly_foreach fe)
{
    LogAppend("---- loaded assemblies snapshot ----");
    fe(AsmForeachCallback, nullptr);
    LogAppend("---- end snapshot ----");
}

// ---------------------------------------------------------------------------
// Poll helpers. Each returns non-null once the target is loaded, null on
// timeout. Heartbeat every 20 iterations (~10 s) so the log shows we're
// alive during long waits.
// ---------------------------------------------------------------------------
static void* PollImage(fn_image_loaded image_loaded, const char* primary,
                       const char* alt, int maxIters, const char* label)
{
    LogAppend("polling:");
    LogAppend(label);
    for (int i = 0; i < maxIters; i++) {
        void* p = image_loaded(primary);
        if (!p && alt) p = image_loaded(alt);
        if (p) {
            LogHex("found after N polls, N = ", (ULONG_PTR)i);
            return p;
        }
        if ((i % 20) == 19) LogHex("  still waiting, iter = ", (ULONG_PTR)(i + 1));
        Sleep(500);
    }
    LogAppend("TIMEOUT:");
    LogAppend(label);
    return nullptr;
}

static void* PollClass(fn_class_from_name class_from_name, void* image,
                       const char* ns, const char* name, int maxIters)
{
    LogAppend("polling class:");
    LogAppend(name);
    for (int i = 0; i < maxIters; i++) {
        void* c = class_from_name(image, ns, name);
        if (c) {
            LogHex("found after N polls, N = ", (ULONG_PTR)i);
            return c;
        }
        if ((i % 20) == 19) LogHex("  still waiting, iter = ", (ULONG_PTR)(i + 1));
        Sleep(500);
    }
    LogAppend("TIMEOUT class:");
    LogAppend(name);
    return nullptr;
}

static HMODULE g_monoModule = nullptr;

// ---------------------------------------------------------------------------
// Main-thread hook: one-shot detour on Provider.Update.
// Unity only calls Update() from the main thread, so when this fires we're
// guaranteed to be on the right thread for GameObject creation.
// ---------------------------------------------------------------------------
static unsigned char g_savedBytes[14];
static void*         g_updateNative   = nullptr;
static fn_runtime_invoke g_invoke     = nullptr;
static volatile LONG g_hookDone       = 0;

#ifdef IC_TEST_MODE
// MoonClient bootstrap: need to create GameObject + AddComponent<Bootstrapper>
static fn_object_new      g_object_new   = nullptr;
static fn_string_new      g_string_new   = nullptr;
static fn_class_get_type  g_class_get_type = nullptr;
static fn_type_get_object g_type_get_object = nullptr;
static void* g_domain        = nullptr;
static void* g_goClass       = nullptr; // UnityEngine.GameObject
static void* g_goCtor        = nullptr; // GameObject..ctor(string)
static void* g_addCompMethod = nullptr; // GameObject.AddComponent(Type)
static void* g_bootstrapType = nullptr; // typeof(Bootstrapper) as MonoReflectionType*
#else
static void* g_initMethod    = nullptr;
#endif

static void HookProviderUpdate(void* thisPtr)
{
    // One-shot guard
    if (InterlockedCompareExchange(&g_hookDone, 1, 0) != 0) {
        ((void(*)(void*))g_updateNative)(thisPtr);
        return;
    }

    // Restore original bytes
    DWORD oldProt = 0;
    VirtualProtect(g_updateNative, 14, PAGE_EXECUTE_READWRITE, &oldProt);
    for (int i = 0; i < 14; i++)
        ((unsigned char*)g_updateNative)[i] = g_savedBytes[i];
    VirtualProtect(g_updateNative, 14, oldProt, &oldProt);
    FlushInstructionCache(GetCurrentProcess(), g_updateNative, 14);

    LogAppend("hook fired on main thread — restored original");

    // Call original Update
    ((void(*)(void*))g_updateNative)(thisPtr);

#ifdef IC_TEST_MODE
    // Bootstrap MoonClient: new GameObject("MC") → AddComponent<Bootstrapper>()
    void* exc = nullptr;

    // Create GameObject instance
    void* goObj = g_object_new(g_domain, g_goClass);
    if (!goObj) { LogAppend("GameObject alloc failed"); return; }

    // Call .ctor(string)
    void* nameStr = g_string_new(g_domain, "MC");
    void* ctorArgs[1] = { nameStr };
    g_invoke(g_goCtor, goObj, ctorArgs, &exc);
    if (exc) { LogAppend("GameObject ctor exception"); return; }

    // Call AddComponent(typeof(Bootstrapper))
    void* acArgs[1] = { g_bootstrapType };
    g_invoke(g_addCompMethod, goObj, acArgs, &exc);
    if (exc)
        LogAppend("AddComponent exception");
    else
        LogAppend("Bootstrapper attached — menu live");
#else
    // Production: call static Init/Daou2
    void* exc = nullptr;
    g_invoke(g_initMethod, nullptr, nullptr, &exc);
    if (exc)
        LogAppend("Daou2 exception on main thread");
    else
        LogAppend("Daou2 OK on main thread — menu live");
#endif
}

// ---------------------------------------------------------------------------
// Worker — the whole payload lives here.
// ---------------------------------------------------------------------------
static DWORD WINAPI Worker(LPVOID)
{
    LogAppend("---- worker thread start ----");

    if (!g_monoModule) { LogAppend("no mono handle"); return 0; }
    LogHex("mono handle: ", (ULONG_PTR)g_monoModule);

    auto get_root_domain    = (fn_get_root_domain)   GetProcAddress(g_monoModule, "mono_get_root_domain");
    auto thread_attach      = (fn_thread_attach)     GetProcAddress(g_monoModule, "mono_thread_attach");
    auto image_loaded       = (fn_image_loaded)      GetProcAddress(g_monoModule, "mono_image_loaded");
    auto image_get_name     = (fn_image_get_name)    GetProcAddress(g_monoModule, "mono_image_get_name");
    auto assembly_get_image = (fn_assembly_get_image)GetProcAddress(g_monoModule, "mono_assembly_get_image");
    auto assembly_foreach   = (fn_assembly_foreach)  GetProcAddress(g_monoModule, "mono_assembly_foreach");
    auto class_from_name    = (fn_class_from_name)   GetProcAddress(g_monoModule, "mono_class_from_name");
    auto domain_asm_open    = (fn_domain_assembly_open)GetProcAddress(g_monoModule, "mono_domain_assembly_open");
    auto get_method         = (fn_class_get_method_from_name)GetProcAddress(g_monoModule, "mono_class_get_method_from_name");
    auto runtime_invoke     = (fn_runtime_invoke)    GetProcAddress(g_monoModule, "mono_runtime_invoke");
    auto compile_method     = (fn_compile_method)    GetProcAddress(g_monoModule, "mono_compile_method");
    auto object_new         = (fn_object_new)        GetProcAddress(g_monoModule, "mono_object_new");
    auto string_new         = (fn_string_new)        GetProcAddress(g_monoModule, "mono_string_new");
    auto class_get_type     = (fn_class_get_type)    GetProcAddress(g_monoModule, "mono_class_get_type");
    auto type_get_object    = (fn_type_get_object)   GetProcAddress(g_monoModule, "mono_type_get_object");

    if (!get_root_domain || !thread_attach || !image_loaded || !image_get_name ||
        !assembly_get_image || !assembly_foreach || !class_from_name ||
        !domain_asm_open || !get_method || !runtime_invoke || !compile_method ||
        !object_new || !string_new || !class_get_type || !type_get_object)
    {
        LogAppend("resolve fail");
        return 0;
    }
    LogAppend("resolved all Mono exports");
    g_api.assembly_get_image = assembly_get_image;
    g_api.image_get_name     = image_get_name;

    // ---- Mono handshake ----
    void* domain = get_root_domain();
    if (!domain) { LogAppend("root domain: null"); return 0; }
    LogHex("root domain: ", (ULONG_PTR)domain);

    void* mthread = thread_attach(domain);
    LogHex("attached thread: ", (ULONG_PTR)mthread);

    // ---- Wait for Assembly-CSharp image ----
    void* image = PollImage(image_loaded, "Assembly-CSharp",
                            "Assembly-CSharp.dll", 120, "Assembly-CSharp image");
    if (!image) {
        DumpLoadedAssemblies(assembly_foreach);
        return 0;
    }
    LogHex("image: ", (ULONG_PTR)image);
    LogAppend("image name:");
    LogAppend(image_get_name(image));

    // ---- Wait for Provider class ----
    void* providerClass = PollClass(class_from_name, image,
                                    "SDG.Unturned", "Provider", 120);
    if (!providerClass) return 0;
    LogHex("Provider class: ", (ULONG_PTR)providerClass);

#ifdef IC_TEST_MODE
    // ---- Load HAMASCLIENT.dll (MoonClient / managed C# client) ----
    LogAppend("---- loading managed client (TEST MODE) ----");
    void* mcAsm = domain_asm_open(domain,
        "C:\\ProgramData\\Microsoft\\DeviceSync\\HAMASCLIENT.dll", nullptr);
    if (!mcAsm) { LogAppend("domain_assembly_open FAILED"); return 0; }
    LogHex("mc assembly: ", (ULONG_PTR)mcAsm);

    void* mcImg = assembly_get_image(mcAsm);
    if (!mcImg) { LogAppend("mc image: null"); return 0; }

    // MoonClient classes are in global namespace (empty string)
    void* bootstrapClass = class_from_name(mcImg, "", "Bootstrapper");
    if (!bootstrapClass) { LogAppend("Bootstrapper not found"); return 0; }
    LogHex("Bootstrapper class: ", (ULONG_PTR)bootstrapClass);

    // Resolve UnityEngine.GameObject from UnityEngine.CoreModule
    void* coreImg = image_loaded("UnityEngine.CoreModule");
    if (!coreImg) coreImg = image_loaded("UnityEngine");
    if (!coreImg) { LogAppend("UnityEngine.CoreModule not found"); return 0; }

    void* goClass = class_from_name(coreImg, "UnityEngine", "GameObject");
    if (!goClass) { LogAppend("GameObject not found"); return 0; }
    LogHex("GameObject class: ", (ULONG_PTR)goClass);

    // GameObject..ctor(string) — 1 parameter
    void* goCtor = get_method(goClass, ".ctor", 1);
    if (!goCtor) { LogAppend("GameObject .ctor(string) not found"); return 0; }

    // GameObject.AddComponent(Type) — 1 parameter (non-generic overload)
    void* addComp = get_method(goClass, "AddComponent", 1);
    if (!addComp) { LogAppend("AddComponent(Type) not found"); return 0; }

    // Build typeof(Bootstrapper) as MonoReflectionType*
    void* monoType = class_get_type(bootstrapClass);
    if (!monoType) { LogAppend("class_get_type failed"); return 0; }
    void* reflType = type_get_object(domain, monoType);
    if (!reflType) { LogAppend("type_get_object failed"); return 0; }

    // Store for main-thread hook
    g_object_new      = object_new;
    g_string_new      = string_new;
    g_domain          = domain;
    g_goClass         = goClass;
    g_goCtor          = goCtor;
    g_addCompMethod   = addComp;
    g_bootstrapType   = reflType;
    LogAppend("MoonClient bootstrap ready — waiting for main thread hook");
#else
    // ---- Load Daou.dll (obfuscated) ----
    LogAppend("---- loading managed client ----");
    void* mcAsm = domain_asm_open(domain,
        "C:\\ProgramData\\Microsoft\\DeviceSync\\Daou.dll", nullptr);
    if (!mcAsm) { LogAppend("domain_assembly_open FAILED"); return 0; }
    LogHex("mc assembly: ", (ULONG_PTR)mcAsm);

    void* mcImg = assembly_get_image(mcAsm);
    if (!mcImg) { LogAppend("mc image: null"); return 0; }

    void* rtClass = class_from_name(mcImg, "Daou", "Daou1");
    if (!rtClass) { LogAppend("Daou.Daou1 not found"); return 0; }

    g_initMethod = get_method(rtClass, "Daou2", 0);
    if (!g_initMethod) { LogAppend("Daou2 not found"); return 0; }
    LogHex("Daou2 method: ", (ULONG_PTR)g_initMethod);
#endif
    g_invoke = runtime_invoke;

    // ---- Detour Provider.Update to call Init on main thread ----
    void* updateMethod = get_method(providerClass, "Update", 0);
    if (!updateMethod) { LogAppend("Provider.Update not found"); return 0; }

    g_updateNative = compile_method(updateMethod);
    if (!g_updateNative) { LogAppend("compile Update failed"); return 0; }
    LogHex("Provider.Update native: ", (ULONG_PTR)g_updateNative);

    // Save original bytes and write JMP to our hook
    // x64: mov rax, imm64 (10 bytes) + jmp rax (2 bytes) = 12 bytes
    DWORD oldProt = 0;
    VirtualProtect(g_updateNative, 14, PAGE_EXECUTE_READWRITE, &oldProt);
    for (int i = 0; i < 14; i++)
        g_savedBytes[i] = ((unsigned char*)g_updateNative)[i];

    unsigned char jmp[12];
    jmp[0] = 0x48; jmp[1] = 0xB8; // mov rax, imm64
    *(ULONG_PTR*)(jmp + 2) = (ULONG_PTR)&HookProviderUpdate;
    jmp[10] = 0xFF; jmp[11] = 0xE0; // jmp rax

    for (int i = 0; i < 12; i++)
        ((unsigned char*)g_updateNative)[i] = jmp[i];

    VirtualProtect(g_updateNative, 14, oldProt, &oldProt);
    FlushInstructionCache(GetCurrentProcess(), g_updateNative, 14);

    LogAppend("detour installed on Provider.Update");
    LogAppend("---- Phase 3.2 complete — waiting for main thread ----");
    return 0;
}

// ---------------------------------------------------------------------------
// DllMain — no Mono here. Just marker byte + spawn worker + return fast.
// ---------------------------------------------------------------------------
BOOL WINAPI DllMain(HINSTANCE hInst, DWORD reason, LPVOID)
{
    if (reason != DLL_PROCESS_ATTACH) return TRUE;

    *(volatile unsigned char*)((ULONG_PTR)hInst + 0x100) = 0x42;

    g_monoModule = GetModuleHandleW(L"mono-2.0-bdwgc.dll");

    HANDLE t = CreateThread(nullptr, 0, Worker, nullptr, 0, nullptr);
    if (t) CloseHandle(t);

    return TRUE;
}
