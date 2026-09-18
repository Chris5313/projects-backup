// ---------------------------------------------------------------------------
// gw2_entfinder.cpp - Runtime entity list finder
// Scans live heap memory to find the entity manager and list - no static offsets
// This is how real cheats find entity lists at runtime.
// ---------------------------------------------------------------------------
#include "gw2_offsets.h"
#include "config.h"
#include <windows.h>
#include <psapi.h>
#include <cstdint>

namespace entfinder {

static HANDLE g_log = INVALID_HANDLE_VALUE;
static uint64_t g_base = 0;
static uint64_t g_charObjVT = 0;

// Found CharObj pointers
static uint64_t g_charObjs[4096];
static int g_nCharObjs = 0;

// Found entity list candidates (pointers that point to CharObj arrays)
struct ListCandidate {
    uint64_t listAddr;      // Address of the list/array
    uint64_t containerAddr; // Address that holds the list pointer
    int count;              // How many CharObjs found
    int stride;             // Spacing between entries
};
static ListCandidate g_lists[64];
static int g_nLists = 0;

// Best result
static uint64_t g_entityManager = 0;
static uint64_t g_entityListBase = 0;
static int g_entityListCount = 0;
static int g_entityListStride = 8;

static void WriteLog(const char* s) {
    if (g_log == INVALID_HANDLE_VALUE) {
        g_log = CreateFileA("C:\\Users\\Public\\gw2_entfinder.log",
            GENERIC_WRITE, FILE_SHARE_READ,
            nullptr, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, nullptr);
        if (g_log == INVALID_HANDLE_VALUE) return;
    }
    DWORD written;
    int len = 0;
    while (s[len]) len++;
    WriteFile(g_log, s, len, &written, nullptr);
}

static void LogLine(const char* s) {
    WriteLog(s);
    WriteLog("\r\n");
}

static void LogHex(const char* prefix, uint64_t val) {
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

static bool IsHeapPtr(uint64_t p) {
    // Valid heap pointer range (not module, not stack)
    return p >= 0x10000 && p < 0x7FFFFFFFFFFF && (p & 0xFFFFFFFFF0000000ull) != 0x140000000ull;
}

static bool CanRead(uint64_t addr) {
    if (addr < 0x10000 || addr > 0x7FFFFFFFFFFF) return false;
    MEMORY_BASIC_INFORMATION mbi;
    if (!VirtualQuery((void*)addr, &mbi, sizeof(mbi))) return false;
    if (mbi.State != MEM_COMMIT) return false;
    if (mbi.Protect & (PAGE_NOACCESS | PAGE_GUARD)) return false;
    return true;
}

static uint64_t ReadPtr(uint64_t addr) {
    if (!CanRead(addr)) return 0;
    __try {
        return *(volatile uint64_t*)addr;
    } __except(EXCEPTION_EXECUTE_HANDLER) {
        return 0;
    }
}

static uint32_t ReadU32(uint64_t addr) {
    if (!CanRead(addr)) return 0;
    __try {
        return *(volatile uint32_t*)addr;
    } __except(EXCEPTION_EXECUTE_HANDLER) {
        return 0;
    }
}

// Check if pointer points to a CharObj (by vtable)
static bool IsCharObj(uint64_t ptr) {
    if (!IsHeapPtr(ptr)) return false;
    uint64_t vt = ReadPtr(ptr);
    return vt == g_charObjVT;
}

// Pass 1: Scan heap for all CharObj instances
static void ScanForCharObjs() {
    LogLine("=== Pass 1: Scanning heap for CharObj vtables ===");
    
    g_nCharObjs = 0;
    MEMORY_BASIC_INFORMATION mbi;
    uint64_t addr = 0x10000;
    
    while (addr < 0x7FFFFFFFFFFF && g_nCharObjs < 4096) {
        if (!VirtualQuery((void*)addr, &mbi, sizeof(mbi))) {
            addr += 0x10000;
            continue;
        }
        
        if (mbi.State == MEM_COMMIT && 
            !(mbi.Protect & (PAGE_NOACCESS | PAGE_GUARD)) &&
            (mbi.Protect & (PAGE_READWRITE | PAGE_READONLY | PAGE_EXECUTE_READ | PAGE_EXECUTE_READWRITE))) {
            
            // Skip module regions
            uint64_t regionBase = (uint64_t)mbi.BaseAddress;
            if (regionBase >= g_base && regionBase < g_base + 0x10000000) {
                addr = regionBase + mbi.RegionSize;
                continue;
            }
            
            // Scan this region for CharObj vtables
            uint64_t regionEnd = regionBase + mbi.RegionSize;
            for (uint64_t scan = regionBase; scan + 8 <= regionEnd && g_nCharObjs < 4096; scan += 8) {
                __try {
                    uint64_t val = *(volatile uint64_t*)scan;
                    if (val == g_charObjVT) {
                        // Found a CharObj! Record its address
                        g_charObjs[g_nCharObjs++] = scan;
                    }
                } __except(EXCEPTION_EXECUTE_HANDLER) {
                    break; // Region became invalid, move on
                }
            }
        }
        
        addr = (uint64_t)mbi.BaseAddress + mbi.RegionSize;
    }
    
    LogInt("Found CharObj instances: ", g_nCharObjs);
    
    // Log first 10
    for (int i = 0; i < g_nCharObjs && i < 10; i++) {
        LogHex("  CharObj: ", g_charObjs[i]);
    }
}

// Pass 2: Find pointers TO these CharObjs (the entity list entries)
static void ScanForEntityList() {
    LogLine("=== Pass 2: Scanning for entity list (pointers to CharObjs) ===");
    
    if (g_nCharObjs < 3) {
        LogLine("Not enough CharObjs found to identify list pattern");
        return;
    }
    
    g_nLists = 0;
    MEMORY_BASIC_INFORMATION mbi;
    uint64_t addr = 0x10000;
    
    // We're looking for memory that contains pointers to our CharObjs
    // A valid entity list will have multiple consecutive pointers to CharObjs
    
    while (addr < 0x7FFFFFFFFFFF && g_nLists < 64) {
        if (!VirtualQuery((void*)addr, &mbi, sizeof(mbi))) {
            addr += 0x10000;
            continue;
        }
        
        if (mbi.State == MEM_COMMIT && 
            !(mbi.Protect & (PAGE_NOACCESS | PAGE_GUARD)) &&
            (mbi.Protect & (PAGE_READWRITE | PAGE_READONLY))) {
            
            uint64_t regionBase = (uint64_t)mbi.BaseAddress;
            uint64_t regionEnd = regionBase + mbi.RegionSize;
            
            // Scan for CharObj pointers
            for (uint64_t scan = regionBase; scan + 8 <= regionEnd; scan += 8) {
                __try {
                    uint64_t val = *(volatile uint64_t*)scan;
                    
                    // Check if this points to any of our CharObjs
                    bool match = false;
                    for (int i = 0; i < g_nCharObjs; i++) {
                        if (val == g_charObjs[i]) {
                            match = true;
                            break;
                        }
                    }
                    
                    if (match) {
                        // Found a pointer to CharObj! Check if it's part of a list
                        int count = 1;
                        
                        // Check next entries (stride = 8 bytes for pointer array)
                        for (int s = 1; s < 200; s++) {
                            uint64_t nextVal = ReadPtr(scan + s * 8);
                            bool nextMatch = false;
                            for (int i = 0; i < g_nCharObjs; i++) {
                                if (nextVal == g_charObjs[i]) {
                                    nextMatch = true;
                                    break;
                                }
                            }
                            if (nextMatch) count++;
                            else if (count >= 3) break; // Gap, stop counting
                        }
                        
                        if (count >= 3 && g_nLists < 64) {
                            // This looks like an entity list!
                            g_lists[g_nLists].listAddr = scan;
                            g_lists[g_nLists].count = count;
                            g_lists[g_nLists].stride = 8;
                            g_lists[g_nLists].containerAddr = 0;
                            g_nLists++;
                            
                            // Skip past this list
                            scan += count * 8;
                        }
                    }
                } __except(EXCEPTION_EXECUTE_HANDLER) {
                    break;
                }
            }
        }
        
        addr = (uint64_t)mbi.BaseAddress + mbi.RegionSize;
    }
    
    LogInt("Found list candidates: ", g_nLists);
    
    // Log all candidates
    for (int i = 0; i < g_nLists; i++) {
        LogHex("  List at: ", g_lists[i].listAddr);
        LogInt("    count: ", g_lists[i].count);
    }
}

// Pass 3: Find what POINTS to the entity list (the manager)
static void ScanForManager() {
    LogLine("=== Pass 3: Scanning for entity manager (pointer to list) ===");
    
    if (g_nLists == 0) {
        LogLine("No entity lists found");
        return;
    }
    
    // Find the best list (most entries)
    int bestIdx = 0;
    for (int i = 1; i < g_nLists; i++) {
        if (g_lists[i].count > g_lists[bestIdx].count) {
            bestIdx = i;
        }
    }
    
    uint64_t targetList = g_lists[bestIdx].listAddr;
    LogHex("Looking for pointer to list: ", targetList);
    
    MEMORY_BASIC_INFORMATION mbi;
    uint64_t addr = 0x10000;
    
    while (addr < 0x7FFFFFFFFFFF) {
        if (!VirtualQuery((void*)addr, &mbi, sizeof(mbi))) {
            addr += 0x10000;
            continue;
        }
        
        if (mbi.State == MEM_COMMIT && 
            !(mbi.Protect & (PAGE_NOACCESS | PAGE_GUARD)) &&
            (mbi.Protect & (PAGE_READWRITE | PAGE_READONLY))) {
            
            uint64_t regionBase = (uint64_t)mbi.BaseAddress;
            uint64_t regionEnd = regionBase + mbi.RegionSize;
            
            for (uint64_t scan = regionBase; scan + 8 <= regionEnd; scan += 8) {
                __try {
                    uint64_t val = *(volatile uint64_t*)scan;
                    if (val == targetList) {
                        LogHex("Found manager candidate at: ", scan);
                        
                        // Check if this is part of a struct (has other valid pointers nearby)
                        uint64_t structBase = scan - (scan % 0x100); // Align to 256
                        LogHex("  Possible struct base: ", structBase);
                        
                        // Calculate offset
                        uint64_t offset = scan - structBase;
                        LogHex("  List at struct offset: ", offset);
                        
                        // Save the best result
                        g_lists[bestIdx].containerAddr = scan;
                        g_entityManager = structBase;
                        g_entityListBase = targetList;
                        g_entityListCount = g_lists[bestIdx].count;
                    }
                } __except(EXCEPTION_EXECUTE_HANDLER) {
                    break;
                }
            }
        }
        
        addr = (uint64_t)mbi.BaseAddress + mbi.RegionSize;
    }
}

// Pass 4: Find static pointer to the manager (RVA we need)
static void FindStaticPointer() {
    LogLine("=== Pass 4: Finding static pointer to manager ===");
    
    if (g_entityManager == 0) {
        LogLine("No entity manager found");
        return;
    }
    
    // Scan the game's static data section for pointer to our manager
    uint64_t dataStart = g_base + gw2::sections::sxdata;
    uint64_t dataEnd = dataStart + 0x683000;
    
    LogHex("Scanning static data: ", dataStart);
    LogHex("                  to: ", dataEnd);
    LogHex("Looking for manager: ", g_entityManager);
    
    // Also look for pointer directly to the entity list
    LogHex("Or list base: ", g_entityListBase);
    
    for (uint64_t scan = dataStart; scan + 8 <= dataEnd; scan += 8) {
        __try {
            uint64_t val = *(volatile uint64_t*)scan;
            
            // Check if pointing to manager struct (within 0x200 of our guess)
            if (val >= g_entityManager && val < g_entityManager + 0x200) {
                uint64_t rva = scan - g_base;
                LogHex("FOUND static ptr to manager! RVA: ", rva);
                LogHex("  Points to: ", val);
            }
            
            // Check if pointing to entity list directly
            if (val == g_entityListBase) {
                uint64_t rva = scan - g_base;
                LogHex("FOUND static ptr to list! RVA: ", rva);
            }
        } __except(EXCEPTION_EXECUTE_HANDLER) {
            continue;
        }
    }
}

// Main entry point - run the full scan
void FindEntityList() {
    g_base = gw2::base();
    if (!g_base) {
        LogLine("ERROR: Game base not found");
        return;
    }
    
    g_charObjVT = g_base + gw2::vtables::CharObj;
    
    LogLine("=== GW2 Entity List Finder ===");
    LogHex("Game base: ", g_base);
    LogHex("CharObj vtable: ", g_charObjVT);
    
    DWORD start = GetTickCount();
    
    ScanForCharObjs();
    ScanForEntityList();
    ScanForManager();
    FindStaticPointer();
    
    DWORD elapsed = GetTickCount() - start;
    
    LogLine("");
    LogLine("=== RESULTS ===");
    LogHex("Entity Manager (guess): ", g_entityManager);
    LogHex("Entity List Base: ", g_entityListBase);
    LogInt("Entity Count: ", g_entityListCount);
    LogInt("Scan time (ms): ", (int)elapsed);
    
    if (g_entityListBase && g_entityListCount > 0) {
        LogLine("");
        LogLine("=== HOW TO USE ===");
        LogLine("Read entity pointers from list base:");
        LogLine("  for (int i = 0; i < count; i++) {");
        LogLine("    uint64_t charObj = ReadPtr(listBase + i * 8);");
        LogLine("    uint64_t proxy = ReadPtr(charObj + 0x50);");
        LogLine("    float pos[3]; ReadMem(proxy + 0x70, pos, 12);");
        LogLine("  }");
    }
    
    // Close log
    if (g_log != INVALID_HANDLE_VALUE) {
        CloseHandle(g_log);
        g_log = INVALID_HANDLE_VALUE;
    }
}

// Getters for results
uint64_t GetEntityListBase() { return g_entityListBase; }
int GetEntityCount() { return g_entityListCount; }
uint64_t* GetCharObjs() { return g_charObjs; }
int GetCharObjCount() { return g_nCharObjs; }

} // namespace entfinder
