// ---------------------------------------------------------------------------
// GW2Dumper — standalone game dumper for PvZ GW2 (GW2.Main_Win64_Retail.exe).
//
// Re-creates the lost C:\Tools dump artifacts:
//   gw2_image_dump_0.bin   — the game's main module image AS DECRYPTED AT
//                            RUNTIME (VMProtect sections decrypted, IAT
//                            resolved). File offset == RVA. Load in IDA as
//                            "binary file" at the base recorded in the
//                            manifest (runtime base; usually 0x140000000).
//   gw2_image_manifest.txt — region map (RVA/VA/size/state/protect) + base.
//
//   --heap  additionally dumps committed MEM_PRIVATE RW regions (heaps +
//                            module statics are NOT included — those live in
//                            the image dump) to gw2_heap_dump_0.bin with a
//                            gw2_heap_manifest.txt mapping file-offset ->
//                            runtime VA. This is the artifact that gave the
//                            ebx/descriptor mining (live reflection
//                            descriptors, weapon containers, health comps).
//
// Usage:
//   GW2Dumper.exe            dump the image
//   GW2Dumper.exe --heap     image + heap (heap can be several GB)
//
// Run the game BARE (EAAC blocked — the standard local test setup) and the
// dumper from any admin/user shell; it only needs PROCESS_VM_READ.
// If OpenProcess fails, EAAC's callback stripped the handle — re-run bare.
//
// Output dir: C:\Users\Public\
// ---------------------------------------------------------------------------
#include <windows.h>
#include <tlhelp32.h>
#include <psapi.h>
#include <stdio.h>
#include <stdint.h>

#pragma comment(lib, "psapi.lib")

static const char* TARGET = "GW2.Main_Win64_Retail.exe";
static const char* OUT_DIR = "C:\\Users\\Public\\";

// ---------------------------------------------------------------------------
static DWORD FindTargetPid()
{
    HANDLE snap = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
    if (snap == INVALID_HANDLE_VALUE) return 0;
    PROCESSENTRY32W pe = { sizeof pe };
    DWORD pid = 0;
    if (Process32FirstW(snap, &pe)) {
        do {
            char name[64];
            int i = 0;
            for (; i < 63 && pe.szExeFile[i]; i++)
                name[i] = (char)(pe.szExeFile[i] < 128 ? pe.szExeFile[i] : '?');
            name[i] = 0;
            for (int j = 0; name[j]; j++)
                if (name[j] >= 'A' && name[j] <= 'Z') name[j] += 32;
            if (_stricmp(name, TARGET) == 0) { pid = pe.th32ProcessID; break; }
        } while (Process32NextW(snap, &pe));
    }
    CloseHandle(snap);
    return pid;
}

// main module base + size of the target (64-bit module list)
static bool GetMainModule(HANDLE h, uint64_t* baseOut, uint64_t* sizeOut)
{
    HMODULE mods[2048];
    DWORD needed = 0;
    if (!EnumProcessModulesEx(h, mods, sizeof mods, &needed, LIST_MODULES_64BIT))
        return false;
    int n = (int)(needed / sizeof(HMODULE));
    if (n < 1) return false;
    MODULEINFO mi;
    for (int i = 0; i < n; i++) {
        char path[MAX_PATH] = { 0 };
        GetModuleFileNameExA(h, mods[i], path, MAX_PATH);
        const char* fn = path;
        for (const char* p = path; *p; p++) if (*p == '\\') fn = p + 1;
        if (_stricmp(fn, TARGET) == 0) {
            if (GetModuleInformation(h, mods[i], &mi, sizeof mi)) {
                *baseOut = (uint64_t)(uintptr_t)mi.lpBaseOfDll;
                *sizeOut = (uint64_t)mi.SizeOfImage;
                return true;
            }
        }
    }
    // fall back to module[0] if the name walk failed
    if (GetModuleInformation(h, mods[0], &mi, sizeof mi)) {
        *baseOut = (uint64_t)(uintptr_t)mi.lpBaseOfDll;
        *sizeOut = (uint64_t)mi.SizeOfImage;
        return true;
    }
    return false;
}

static const char* ProtStr(DWORD p)
{
    static char b[32];
    const char* rw = "";
    if (p & PAGE_READONLY) rw = "R";
    else if (p & PAGE_READWRITE) rw = "RW";
    else if (p & PAGE_EXECUTE_READ) rw = "RX";
    else if (p & PAGE_EXECUTE_READWRITE) rw = "RWX";
    else if (p & PAGE_WRITECOPY) rw = "WC";
    else if (p & PAGE_NOACCESS) rw = "NOACCESS";
    else if (p & PAGE_EXECUTE_WRITECOPY) rw = "XWC";
    if (p & PAGE_GUARD) { snprintf(b, sizeof b, "%s+GUARD", rw); return b; }
    snprintf(b, sizeof b, "%s", rw);
    return b;
}

// ---------------------------------------------------------------------------
// IMAGE dump: offset == RVA, gaps (unmapped/hollow) zero-filled so the file
// is SizeOfImage long and any RVA can be read at file[RVA].
static bool DumpImage(HANDLE h, uint64_t base, uint64_t size)
{
    char path[256], mpath[256];
    snprintf(path, sizeof path, "%sgw2_image_dump_0.bin", OUT_DIR);
    snprintf(mpath, sizeof mpath, "%sgw2_image_manifest.txt", OUT_DIR);

    FILE* f = fopen(path, "wb");
    if (!f) { printf("ERROR: cannot create %s\n", path); return false; }
    FILE* m = fopen(mpath, "w");

    // pre-size the file (sparse-ish: we zero-fill holes while writing linearly)
    if (_fseeki64(f, 0, SEEK_SET) != 0) { fclose(f); fclose(m); return false; }

    uint64_t rva = 0;
    uint64_t totalRead = 0;
    uint32_t regions = 0;

    printf("[image] base 0x%llX size 0x%llX (%.1f MB)\n",
        (unsigned long long)base, (unsigned long long)size, size / 1048576.0);

    // walk regions linearly; holes are filled with a zero run
    static unsigned char zeros[0x10000];
    static unsigned char buf[0x10000];

    while (rva < size) {
        MEMORY_BASIC_INFORMATION mbi;
        if (VirtualQueryEx(h, (LPCVOID)(base + rva), &mbi, sizeof mbi) == 0) {
            // end of usable info — zero-fill the rest
            uint64_t left = size - rva;
            while (left) {
                uint64_t c = left > sizeof zeros ? sizeof zeros : left;
                fwrite(zeros, 1, (size_t)c, f);
                rva += c; left -= c;
            }
            break;
        }
        uint64_t rvaHi = (uint64_t)mbi.BaseAddress + mbi.RegionSize - base;
        if (rvaHi > size) rvaHi = size;
        uint64_t span = rvaHi - rva;

        if (mbi.State == MEM_COMMIT && !(mbi.Protect & (PAGE_NOACCESS | PAGE_GUARD))) {
            if (m) fprintf(m, "RVA 0x%08llX  VA 0x%llX  size 0x%llX  %s\n",
                (unsigned long long)rva, (unsigned long long)(base + rva),
                (unsigned long long)span, ProtStr(mbi.Protect));
            regions++;
            // read in chunks
            uint64_t off = 0;
            while (off < span) {
                uint64_t c = span - off > sizeof buf ? sizeof buf : span - off;
                SIZE_T got = 0;
                SIZE_T want = (SIZE_T)c;
                // ReadProcessMemory with a chunk that never crosses a page
                // boundary fails atomically; page-align the chunk size
                uint64_t vaNow = base + rva + off;
                uint64_t pageLeft = 0x1000 - (vaNow & 0xFFF);
                if (c > pageLeft) c = pageLeft;
                if (ReadProcessMemory(h, (LPCVOID)vaNow, buf, (SIZE_T)c, &got) && got == c) {
                    fwrite(buf, 1, (size_t)c, f);
                    totalRead += c;
                } else {
                    // partial fail: zero-fill this chunk (hollow page)
                    fwrite(zeros, 1, (size_t)c, f);
                }
                off += c;
            }
        } else {
            // uncommitted / guard / noaccess hole -> zeros (offset==RVA kept)
            uint64_t left = span;
            while (left) {
                uint64_t c = left > sizeof zeros ? sizeof zeros : left;
                fwrite(zeros, 1, (size_t)c, f);
                left -= c;
            }
            if (m && mbi.State == MEM_RESERVE)
                fprintf(m, "RVA 0x%08llX  VA 0x%llX  size 0x%llX  RESERVED %s\n",
                    (unsigned long long)rva, (unsigned long long)(base + rva),
                    (unsigned long long)span, ProtStr(mbi.Protect));
        }
        rva = rvaHi;
    }

    fflush(f);
    if (_fseeki64(f, 0, SEEK_END) != 0) {}
    long long fsz = _ftelli64(f);
    fclose(f);

    if (m) {
        fprintf(m, "\n# base      0x%llX\n", (unsigned long long)base);
        fprintf(m, "# imagesize 0x%llX\n", (unsigned long long)size);
        fprintf(m, "# file      %s (%lld bytes)\n", path, fsz);
        fprintf(m, "# rule      file offset == RVA (VA - base)\n");
        fprintf(m, "# readable  %llu bytes across %u regions\n",
            (unsigned long long)totalRead, regions);
        fprintf(m, "# ida       load as 64-bit binary at 0x%llX\n", (unsigned long long)base);
        fclose(m);
    }

    printf("[image] wrote %lld bytes (%.1f MB), readable %llu, regions %u\n",
        fsz, fsz / 1048576.0, (unsigned long long)totalRead, regions);
    printf("[image] manifest: %s\n", mpath);
    (void)regions;
    return true;
}

// ---------------------------------------------------------------------------
// HEAP dump: committed private RW regions, packed back-to-back with a
// manifest mapping file offset -> runtime VA (old F2-style artifact).
static bool DumpHeap(HANDLE h)
{
    char path[256], mpath[256];
    snprintf(path, sizeof path, "%sgw2_heap_dump_0.bin", OUT_DIR);
    snprintf(mpath, sizeof mpath, "%sgw2_heap_manifest.txt", OUT_DIR);

    FILE* f = fopen(path, "wb");
    FILE* m = fopen(mpath, "w");
    if (!f || !m) { if (f) fclose(f); if (m) fclose(m); printf("ERROR: create heap files\n"); return false; }

    static unsigned char buf[0x10000];
    uint64_t off = 0;
    uint32_t regions = 0;
    uint64_t totalRead = 0;

    uint64_t va = 0x10000;
    while (va < 0x00007FFFFFFF0000ull) {
        MEMORY_BASIC_INFORMATION mbi;
        if (VirtualQueryEx(h, (LPCVOID)va, &mbi, sizeof mbi) == 0) break;
        uint64_t hi = (uint64_t)mbi.BaseAddress + mbi.RegionSize;
        if (mbi.State == MEM_COMMIT &&
            (mbi.Type == MEM_PRIVATE) &&
            (mbi.Protect & (PAGE_READWRITE | PAGE_EXECUTE_READWRITE | PAGE_WRITECOPY | PAGE_EXECUTE_WRITECOPY)) &&
            !(mbi.Protect & PAGE_GUARD) &&
            mbi.RegionSize >= 0x1000)
        {
            uint64_t len = mbi.RegionSize;
            uint64_t o = 0;
            bool any = false;
            while (o < len) {
                uint64_t c = len - o > sizeof buf ? sizeof buf : len - o;
                uint64_t pageLeft = 0x1000 - ((va + o) & 0xFFF);
                if (c > pageLeft) c = pageLeft;
                SIZE_T got = 0;
                if (ReadProcessMemory(h, (LPCVOID)(va + o), buf, (SIZE_T)c, &got) && got == c) {
                    fwrite(buf, 1, (size_t)c, f);
                    totalRead += c; off += c; o += c; any = true;
                } else {
                    // skip the whole region on first failure — keep mapping
                    // honest (only fully-readable regions are recorded)
                    break;
                }
            }
            if (any) {
                fprintf(m, "fileoff 0x%09llX  VA 0x%llX  size 0x%llX  %s\n",
                    (unsigned long long)(off - o), (unsigned long long)va,
                    (unsigned long long)o, ProtStr(mbi.Protect));
                regions++;
            }
        }
        va = hi;
    }
    fflush(f);
    long long fsz = _ftelli64(f);
    fclose(f);
    fprintf(m, "\n# file     %s (%lld bytes)\n", path, fsz);
    fprintf(m, "# readable %llu bytes across %u regions\n",
        (unsigned long long)totalRead, regions);
    fprintf(m, "# rule     fileoff column maps to runtime VA (manifest rows)\n");
    fclose(m);

    printf("[heap]  wrote %lld bytes (%.1f MB), regions %u\n",
        fsz, fsz / 1048576.0, regions);
    printf("[heap]  manifest: %s\n", mpath);
    return true;
}

// ---------------------------------------------------------------------------
int main(int argc, char** argv)
{
    bool heap = (argc > 1 && _stricmp(argv[1], "--heap") == 0);

    printf("GW2Dumper — PvZ GW2 runtime dumper\n");
    printf("target: %s\n\n", TARGET);

    DWORD pid = FindTargetPid();
    if (!pid) { printf("ERROR: %s is not running\n", TARGET); return 1; }
    printf("found pid %lu\n", pid);

    HANDLE h = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, FALSE, pid);
    if (!h) {
        h = OpenProcess(PROCESS_VM_READ, FALSE, pid); // minimal
        if (!h) {
            printf("ERROR: OpenProcess failed (err %lu).\n", GetLastError());
            printf("EAAC is likely live and stripped the handle. Run the game BARE\n");
            printf("(EAAC launcher blocked / service disabled) and retry.\n");
            return 1;
        }
    }

    uint64_t base = 0, size = 0;
    if (!GetMainModule(h, &base, &size) || !base || !size) {
        printf("ERROR: could not resolve the main module (err %lu)\n", GetLastError());
        CloseHandle(h);
        return 1;
    }

    bool ok = DumpImage(h, base, size);
    if (ok && heap) DumpHeap(h);

    CloseHandle(h);
    printf("\n%s\n", ok ? "DONE — load the .bin in IDA (64-bit binary, base from manifest)" : "FAILED");
    return ok ? 0 : 1;
}
