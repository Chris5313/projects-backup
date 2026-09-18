// ---------------------------------------------------------------------------
// In-process dumper — dumps game memory from INSIDE EAC
//
// Output (C:\Users\Public\):
//   gw2_dump_image.bin     — main module, decrypted, offset=RVA
//   gw2_dump_heap.bin      — all committed private RW regions
//   gw2_dump_structs.bin   — key game structures (cameras, etc.)
//   gw2_dump_manifest.txt  — addresses, sizes, offsets
// ---------------------------------------------------------------------------
#include "gw2_dumper.h"
#include <windows.h>
#include <psapi.h>
#include <cstdio>
#include <cstdint>
#include <cmath>

#pragma comment(lib, "psapi.lib")

namespace dumper {

static const char* OUT_DIR = "C:\\Users\\Public\\";
static const char* TARGET_MODULE = "GW2.Main_Win64_Retail.exe";

// Known structure addresses (from gw2_viewproj.cpp)
static constexpr uint64_t CAM_STATIC = 0x142CEE730ull;

static void LogDump(const char* msg)
{
    char path[MAX_PATH];
    snprintf(path, MAX_PATH, "%sgw2_dump_log.txt", OUT_DIR);
    HANDLE h = CreateFileA(path, FILE_APPEND_DATA,
        FILE_SHARE_READ | FILE_SHARE_WRITE, nullptr, OPEN_ALWAYS,
        FILE_ATTRIBUTE_NORMAL, nullptr);
    if (h == INVALID_HANDLE_VALUE) return;
    DWORD w;
    WriteFile(h, msg, (DWORD)strlen(msg), &w, nullptr);
    WriteFile(h, "\r\n", 2, &w, nullptr);
    CloseHandle(h);
}

static void LogDumpF(const char* fmt, ...)
{
    char buf[512];
    va_list args;
    va_start(args, fmt);
    vsnprintf(buf, sizeof(buf), fmt, args);
    va_end(args);
    LogDump(buf);
}

// Get main module info
static bool GetMainModule(uint64_t* baseOut, uint64_t* sizeOut)
{
    HMODULE mods[512];
    DWORD needed = 0;
    if (!EnumProcessModules(GetCurrentProcess(), mods, sizeof(mods), &needed))
        return false;
    
    int count = needed / sizeof(HMODULE);
    for (int i = 0; i < count; i++) {
        char name[MAX_PATH] = {0};
        GetModuleBaseNameA(GetCurrentProcess(), mods[i], name, MAX_PATH);
        if (strstr(name, "GW2") || strstr(name, "Main_Win64")) {
            MODULEINFO mi = {0};
            if (GetModuleInformation(GetCurrentProcess(), mods[i], &mi, sizeof(mi))) {
                *baseOut = (uint64_t)mi.lpBaseOfDll;
                *sizeOut = (uint64_t)mi.SizeOfImage;
                return true;
            }
        }
    }
    return false;
}

// Dump main module image (offset = RVA for IDA)
static bool DumpImage(uint64_t base, uint64_t size)
{
    char path[MAX_PATH];
    snprintf(path, MAX_PATH, "%sgw2_dump_image.bin", OUT_DIR);
    
    HANDLE hFile = CreateFileA(path, GENERIC_WRITE, 0, nullptr,
        CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, nullptr);
    if (hFile == INVALID_HANDLE_VALUE) {
        LogDumpF("ERROR: Cannot create %s", path);
        return false;
    }
    
    LogDumpF("Dumping image: base=0x%llX size=0x%llX", base, size);
    
    // Query each page and dump if readable
    uint8_t* buf = (uint8_t*)VirtualAlloc(nullptr, 0x10000, MEM_COMMIT, PAGE_READWRITE);
    if (!buf) { CloseHandle(hFile); return false; }
    
    uint64_t written = 0;
    for (uint64_t off = 0; off < size; off += 0x1000) {
        MEMORY_BASIC_INFORMATION mbi = {0};
        uint8_t* addr = (uint8_t*)(base + off);
        
        if (VirtualQuery(addr, &mbi, sizeof(mbi)) &&
            mbi.State == MEM_COMMIT &&
            (mbi.Protect & (PAGE_READONLY | PAGE_READWRITE | PAGE_EXECUTE_READ | 
                           PAGE_EXECUTE_READWRITE | PAGE_EXECUTE_WRITECOPY))) {
            // Readable page - copy it
            __try {
                memcpy(buf, addr, 0x1000);
            } __except(EXCEPTION_EXECUTE_HANDLER) {
                memset(buf, 0, 0x1000);
            }
        } else {
            // Not readable - zero fill
            memset(buf, 0, 0x1000);
        }
        
        DWORD w;
        WriteFile(hFile, buf, 0x1000, &w, nullptr);
        written += w;
    }
    
    VirtualFree(buf, 0, MEM_RELEASE);
    CloseHandle(hFile);
    
    LogDumpF("Image dump complete: %llu bytes", written);
    return true;
}

// Dump heap regions
static bool DumpHeap()
{
    char pathBin[MAX_PATH], pathManifest[MAX_PATH];
    snprintf(pathBin, MAX_PATH, "%sgw2_dump_heap.bin", OUT_DIR);
    snprintf(pathManifest, MAX_PATH, "%sgw2_dump_heap_manifest.txt", OUT_DIR);
    
    HANDLE hBin = CreateFileA(pathBin, GENERIC_WRITE, 0, nullptr,
        CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, nullptr);
    HANDLE hMan = CreateFileA(pathManifest, GENERIC_WRITE, 0, nullptr,
        CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, nullptr);
    if (hBin == INVALID_HANDLE_VALUE || hMan == INVALID_HANDLE_VALUE) {
        if (hBin != INVALID_HANDLE_VALUE) CloseHandle(hBin);
        if (hMan != INVALID_HANDLE_VALUE) CloseHandle(hMan);
        return false;
    }
    
    LogDump("Dumping heap regions...");
    
    uint8_t* buf = (uint8_t*)VirtualAlloc(nullptr, 0x100000, MEM_COMMIT, PAGE_READWRITE);
    if (!buf) { CloseHandle(hBin); CloseHandle(hMan); return false; }
    
    uint64_t fileOff = 0;
    int regions = 0;
    char line[256];
    
    // Header
    snprintf(line, sizeof(line), "# file_offset,va,size\r\n");
    DWORD w; WriteFile(hMan, line, (DWORD)strlen(line), &w, nullptr);
    
    MEMORY_BASIC_INFORMATION mbi;
    uint8_t* addr = nullptr;
    
    while (VirtualQuery(addr, &mbi, sizeof(mbi))) {
        // Private committed RW regions (heap/globals)
        if (mbi.State == MEM_COMMIT && 
            mbi.Type == MEM_PRIVATE &&
            (mbi.Protect == PAGE_READWRITE || mbi.Protect == PAGE_EXECUTE_READWRITE)) {
            
            uint64_t regionSize = mbi.RegionSize;
            uint64_t regionVA = (uint64_t)mbi.BaseAddress;
            
            // Write manifest entry
            snprintf(line, sizeof(line), "0x%llX,0x%llX,0x%llX\r\n", 
                     fileOff, regionVA, regionSize);
            WriteFile(hMan, line, (DWORD)strlen(line), &w, nullptr);
            
            // Dump region in chunks
            for (uint64_t off = 0; off < regionSize; off += 0x100000) {
                uint64_t chunk = min(0x100000ull, regionSize - off);
                __try {
                    memcpy(buf, (uint8_t*)regionVA + off, (size_t)chunk);
                } __except(EXCEPTION_EXECUTE_HANDLER) {
                    memset(buf, 0, (size_t)chunk);
                }
                WriteFile(hBin, buf, (DWORD)chunk, &w, nullptr);
                fileOff += chunk;
            }
            regions++;
        }
        
        addr = (uint8_t*)mbi.BaseAddress + mbi.RegionSize;
        if ((uint64_t)addr < (uint64_t)mbi.BaseAddress) break; // overflow
    }
    
    VirtualFree(buf, 0, MEM_RELEASE);
    CloseHandle(hBin);
    CloseHandle(hMan);
    
    LogDumpF("Heap dump complete: %d regions, %llu bytes", regions, fileOff);
    return true;
}

// Dump key game structures
static bool DumpStructures(uint64_t imageBase)
{
    char path[MAX_PATH];
    snprintf(path, MAX_PATH, "%sgw2_dump_structs.txt", OUT_DIR);
    
    HANDLE h = CreateFileA(path, GENERIC_WRITE, 0, nullptr,
        CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, nullptr);
    if (h == INVALID_HANDLE_VALUE) return false;
    
    char line[1024];
    DWORD w;
    
    // Header
    snprintf(line, sizeof(line), 
        "# GW2 Structure Dump\r\n"
        "# Image Base: 0x%llX\r\n"
        "#\r\n", imageBase);
    WriteFile(h, line, (DWORD)strlen(line), &w, nullptr);
    
    // Read CameraManager chain
    uint64_t camMgr = 0, views = 0;
    if (ReadProcessMemory(GetCurrentProcess(), (void*)CAM_STATIC, &camMgr, 8, nullptr) && camMgr) {
        snprintf(line, sizeof(line), "CAM_STATIC (0x%llX) -> CameraManager = 0x%llX\r\n", 
                 CAM_STATIC, camMgr);
        WriteFile(h, line, (DWORD)strlen(line), &w, nullptr);
        
        if (ReadProcessMemory(GetCurrentProcess(), (void*)(camMgr + 0x68), &views, 8, nullptr) && views) {
            snprintf(line, sizeof(line), "CameraManager+0x68 -> RenderViews = 0x%llX\r\n", views);
            WriteFile(h, line, (DWORD)strlen(line), &w, nullptr);
            
            // Dump RenderView[0] structure (0x520 bytes)
            snprintf(line, sizeof(line), "\r\n# RenderView[0] at 0x%llX (0x520 bytes):\r\n", views);
            WriteFile(h, line, (DWORD)strlen(line), &w, nullptr);
            
            uint8_t rvBuf[0x520];
            if (ReadProcessMemory(GetCurrentProcess(), (void*)views, rvBuf, 0x520, nullptr)) {
                // Dump as hex + float interpretation
                for (int off = 0; off < 0x520; off += 64) {
                    float* f = (float*)(rvBuf + off);
                    snprintf(line, sizeof(line), 
                        "  +0x%03X: [%9.4f %9.4f %9.4f %9.4f] [%9.4f %9.4f %9.4f %9.4f]\r\n"
                        "          [%9.4f %9.4f %9.4f %9.4f] [%9.4f %9.4f %9.4f %9.4f]\r\n",
                        off,
                        f[0], f[1], f[2], f[3], f[4], f[5], f[6], f[7],
                        f[8], f[9], f[10], f[11], f[12], f[13], f[14], f[15]);
                    WriteFile(h, line, (DWORD)strlen(line), &w, nullptr);
                }
                
                // Identify likely matrices
                snprintf(line, sizeof(line), "\r\n# Matrix analysis:\r\n");
                WriteFile(h, line, (DWORD)strlen(line), &w, nullptr);
                
                for (int off = 0; off < 0x520 - 64; off += 16) {
                    float* m = (float*)(rvBuf + off);
                    // Check if looks like a 4x4 matrix
                    bool hasNonZero = false;
                    bool allFinite = true;
                    for (int i = 0; i < 16; i++) {
                        if (m[i] != 0.f) hasNonZero = true;
                        if (m[i] != m[i] || m[i] > 1e10f || m[i] < -1e10f) allFinite = false;
                    }
                    if (!hasNonZero || !allFinite) continue;
                    
                    // Check for VIEW matrix pattern (orthonormal rows, translation in col 3)
                    float r0len = sqrtf(m[0]*m[0] + m[1]*m[1] + m[2]*m[2]);
                    float r1len = sqrtf(m[4]*m[4] + m[5]*m[5] + m[6]*m[6]);
                    float r2len = sqrtf(m[8]*m[8] + m[9]*m[9] + m[10]*m[10]);
                    bool isView = (r0len > 0.9f && r0len < 1.1f &&
                                   r1len > 0.9f && r1len < 1.1f &&
                                   r2len > 0.9f && r2len < 1.1f);
                    
                    // Check for PROJECTION matrix pattern (m[14] = -1 or 1, m[11] != 0)
                    bool isProj = (fabsf(fabsf(m[14]) - 1.f) < 0.01f && 
                                   fabsf(m[11]) > 0.01f &&
                                   fabsf(m[3]) < 0.01f && fabsf(m[7]) < 0.01f);
                    
                    // Check for VP matrix (product of view and proj)
                    bool isVP = (fabsf(m[15]) < 0.01f && fabsf(m[14]) > 0.5f);
                    
                    if (isView || isProj || isVP) {
                        snprintf(line, sizeof(line), 
                            "  +0x%03X: %s\r\n"
                            "    [%9.4f %9.4f %9.4f %9.4f]\r\n"
                            "    [%9.4f %9.4f %9.4f %9.4f]\r\n"
                            "    [%9.4f %9.4f %9.4f %9.4f]\r\n"
                            "    [%9.4f %9.4f %9.4f %9.4f]\r\n",
                            off, isView ? "VIEW" : (isProj ? "PROJECTION" : "VIEWPROJ"),
                            m[0], m[1], m[2], m[3],
                            m[4], m[5], m[6], m[7],
                            m[8], m[9], m[10], m[11],
                            m[12], m[13], m[14], m[15]);
                        WriteFile(h, line, (DWORD)strlen(line), &w, nullptr);
                    }
                }
            }
        }
    }
    
    CloseHandle(h);
    LogDump("Structure dump complete");
    return true;
}

// Write master manifest
static void WriteManifest(uint64_t imageBase, uint64_t imageSize)
{
    char path[MAX_PATH];
    snprintf(path, MAX_PATH, "%sgw2_dump_manifest.txt", OUT_DIR);
    
    HANDLE h = CreateFileA(path, GENERIC_WRITE, 0, nullptr,
        CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, nullptr);
    if (h == INVALID_HANDLE_VALUE) return;
    
    char buf[1024];
    DWORD w;
    
    snprintf(buf, sizeof(buf),
        "# GW2 Memory Dump Manifest\r\n"
        "# Generated from inside EAC\r\n"
        "#\r\n"
        "IMAGE_BASE=0x%llX\r\n"
        "IMAGE_SIZE=0x%llX\r\n"
        "#\r\n"
        "# Files:\r\n"
        "#   gw2_dump_image.bin      - main module (load at IMAGE_BASE)\r\n"
        "#   gw2_dump_heap.bin       - heap regions (see heap_manifest.txt)\r\n"
        "#   gw2_dump_structs.txt    - camera/view structures\r\n"
        "#   gw2_dump_log.txt        - dump log\r\n"
        "#\r\n"
        "# IDA: File > Load file > Additional binary file\r\n"
        "#      Loading segment: 0x%llX\r\n"
        "#      Create RAM section, 64-bit mode\r\n",
        imageBase, imageSize, imageBase);
    
    WriteFile(h, buf, (DWORD)strlen(buf), &w, nullptr);
    CloseHandle(h);
}

bool DumpExists()
{
    char path[MAX_PATH];
    snprintf(path, MAX_PATH, "%sgw2_dump_manifest.txt", OUT_DIR);
    DWORD attr = GetFileAttributesA(path);
    return (attr != INVALID_FILE_ATTRIBUTES);
}

void DumpAll()
{
    LogDump("========================================");
    LogDump("GW2 In-Process Dumper starting...");
    LogDump("========================================");
    
    uint64_t base = 0, size = 0;
    if (!GetMainModule(&base, &size)) {
        LogDump("ERROR: Cannot find main module");
        return;
    }
    
    LogDumpF("Main module: base=0x%llX size=0x%llX (%llu MB)", 
             base, size, size / (1024*1024));
    
    // Dump everything
    DumpImage(base, size);
    DumpHeap();
    DumpStructures(base);
    WriteManifest(base, size);
    
    LogDump("========================================");
    LogDump("Dump complete! Files in C:\\Users\\Public\\");
    LogDump("========================================");
}

} // namespace dumper
