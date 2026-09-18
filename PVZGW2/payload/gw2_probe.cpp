// ---------------------------------------------------------------------------
// gw2_probe.cpp — Runtime probe to discover entity pointer chains
// Output goes to the main payload log (GW2_LOG_PATH)
// ---------------------------------------------------------------------------
#include "gw2_offsets.h"
#include "config.h"
#include <windows.h>
#include <cstdint>

namespace probe {

static HANDLE g_log = INVALID_HANDLE_VALUE;
static int g_probeCount = 0;

static void WriteLog(const char* s) {
    if (g_log == INVALID_HANDLE_VALUE) {
        // Use same log as main payload
        g_log = CreateFileA(GW2_LOG_PATH,
            FILE_APPEND_DATA, FILE_SHARE_READ | FILE_SHARE_WRITE,
            nullptr, OPEN_ALWAYS, FILE_ATTRIBUTE_NORMAL, nullptr);
        if (g_log == INVALID_HANDLE_VALUE) return;
    }
    DWORD written;
    int len = 0;
    while (s[len]) len++;
    WriteFile(g_log, s, len, &written, nullptr);
}

static void LogHex64(const char* prefix, uint64_t val) {
    char buf[128];
    char* p = buf;
    while (*prefix) *p++ = *prefix++;
    *p++ = '0'; *p++ = 'x';
    for (int i = 60; i >= 0; i -= 4) {
        int nib = (int)((val >> i) & 0xF);
        *p++ = "0123456789ABCDEF"[nib];
    }
    *p++ = '\r'; *p++ = '\n'; *p = 0;
    WriteLog(buf);
}

static void LogStr(const char* s) {
    WriteLog(s);
    WriteLog("\r\n");
}

static void LogInt(const char* prefix, int val) {
    char buf[64];
    char* p = buf;
    while (*prefix) *p++ = *prefix++;
    if (val < 0) { *p++ = '-'; val = -val; }
    char tmp[16]; int ti = 0;
    do { tmp[ti++] = '0' + (val % 10); val /= 10; } while (val);
    while (ti > 0) *p++ = tmp[--ti];
    *p++ = '\r'; *p++ = '\n'; *p = 0;
    WriteLog(buf);
}

static bool IsValid(uint64_t ptr) {
    return ptr >= 0x10000 && ptr < 0x7FFFFFFFFFFF;
}

static bool CanRead(void* addr) {
    MEMORY_BASIC_INFORMATION mbi;
    if (!VirtualQuery(addr, &mbi, sizeof(mbi))) return false;
    if (mbi.State != MEM_COMMIT) return false;
    if (mbi.Protect & (PAGE_NOACCESS | PAGE_GUARD)) return false;
    return true;
}

static uint64_t ReadPtr(uint64_t addr) {
    if (!IsValid(addr) || !CanRead((void*)addr)) return 0;
    return *(uint64_t*)addr;
}

static uint32_t ReadU32(uint64_t addr) {
    if (!IsValid(addr) || !CanRead((void*)addr)) return 0;
    return *(uint32_t*)addr;
}

static float ReadFloat(uint64_t addr) {
    if (!IsValid(addr) || !CanRead((void*)addr)) return 0.0f;
    return *(float*)addr;
}

// Main probe function
void ProbeGameContext() {
    g_probeCount++;
    
    uint64_t base = gw2::base();
    
    LogStr("=== PROBE START ===");
    LogHex64("Base: ", base);
    LogInt("Probe #", g_probeCount);
    
    if (!base) {
        LogStr("ERROR: base is null");
        return;
    }
    
    // 1. GameContext
    uint64_t pCtx = base + gw2::statics::GameContext;
    uint64_t ctx = ReadPtr(pCtx);
    LogHex64("GameContext ptr: ", pCtx);
    LogHex64("GameContext val: ", ctx);
    
    if (IsValid(ctx)) {
        // Dump first 32 qwords
        LogStr("--- GameContext dump ---");
        for (int off = 0; off <= 0x180; off += 8) {
            uint64_t val = ReadPtr(ctx + off);
            if (IsValid(val)) {
                char buf[80];
                char* p = buf;
                *p++ = '+'; *p++ = '0'; *p++ = 'x';
                for (int i = 8; i >= 0; i -= 4) {
                    *p++ = "0123456789ABCDEF"[(off >> i) & 0xF];
                }
                *p++ = ':'; *p++ = ' '; *p = 0;
                WriteLog(buf);
                LogHex64("", val);
            }
        }
        
        // Check +0x68 (player base)
        uint64_t v68 = ReadPtr(ctx + 0x68);
        if (IsValid(v68)) {
            LogHex64("+0x68 val: ", v68);
            
            // Scan for CharObj vtable
            uint64_t charVT = base + gw2::vtables::CharObj;
            LogHex64("CharObj VT: ", charVT);
            
            for (int off = 0; off <= 0x100; off += 8) {
                uint64_t ptr = ReadPtr(v68 + off);
                if (IsValid(ptr)) {
                    uint64_t vt = ReadPtr(ptr);
                    if (vt == charVT) {
                        LogStr("FOUND CharObj!");
                        LogHex64("  at +0x68+", (uint64_t)off);
                        LogHex64("  ptr: ", ptr);
                    }
                }
            }
        }
    }
    
    // 2. CameraManager
    uint64_t pCam = base + gw2::statics::CameraManager;
    uint64_t cam = ReadPtr(pCam);
    LogHex64("CameraManager: ", cam);
    
    // 3. Entity managers
    uint64_t pGate = base + gw2::statics::ClientPlayerGateEM;
    uint64_t gate = ReadPtr(pGate);
    LogHex64("ClientPlayerGateEM: ", gate);
    
    uint64_t pDyn = base + gw2::statics::DynamicModelEM;
    uint64_t dyn = ReadPtr(pDyn);
    LogHex64("DynamicModelEM: ", dyn);
    
    LogStr("=== PROBE END ===");
}

void CloseProbe() {
    if (g_log != INVALID_HANDLE_VALUE) {
        CloseHandle(g_log);
        g_log = INVALID_HANDLE_VALUE;
    }
}

} // namespace probe
