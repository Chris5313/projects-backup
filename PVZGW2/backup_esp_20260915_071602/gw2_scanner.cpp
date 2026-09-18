// ---------------------------------------------------------------------------
// gw2_scanner.cpp — runtime entity scanner (feature foundation).
// One-shot diagnostic triggered by F2. Dumps to C:\Users\Public\gw2_scan.txt:
//
//   RTTI  <addr> <Havok class>       — physics objects (proxies, projectiles)
//   DATA  <addr> <blueprint class>   — Frostbite DataContainer instances
//   ENT   <addr> -> <data addr> <blueprint class> — live entities (3rd pass)
//   COUNT <class> <n>                — per-class summary at the end
//
// Pass A: heap qword V (image ptr); *(V-8) is a known COL -> RTTI object.
// Pass B: heap qword V in TypeInfo zone -> blueprint instance header.
// Pass C: heap qword pointing at a pass-B instance -> live entity holder.
//
// Crash-proofing (v2 — first run took the game down):
//   * regions with PAGE_GUARD are never touched: reading a guard page clears
//     its guard bit and breaks the owning thread's stack growth -> AV in
//     ntdll -> process death. That is exactly what killed run 1.
//   * every foreign read goes through SafeCopy (SEH __try/__except): pages
//     freed between VirtualQuery and the read are skipped, not fatal.
//   * pages are snapshotted in 64 KB chunks (page-granular fallback) and
//     scanned from the snapshot.
//   * output flushes every 8 KB with pass markers — a partial run still
//     tells us how far it got.
// ---------------------------------------------------------------------------
#include <cstdint>
#include "gw2_offsets.h"
#include "gw2_scan_data.h"
#include "gw2_viewproj.h"
#include "gw2_entities.h"

// CRT-free substring search (names look like ".?AVhknpCharacterProxy@@")
static bool NameHas(const char* s, const char* sub)
{
    for (; *s; s++) {
        const char* a = s, * b = sub;
        while (*b && *a == *b) { a++; b++; }
        if (!*b) return true;
    }
    return false;
}
#include <windows.h>
#include <psapi.h>

namespace scanner {

static HANDLE g_f = INVALID_HANDLE_VALUE;
static char   g_buf[64 * 1024];
static size_t g_used = 0;

static void Flush()
{
    if (g_used) {
        DWORD w = 0;
        WriteFile(g_f, g_buf, (DWORD)g_used, &w, nullptr);
        g_used = 0;
    }
}

static void L(const char* s, size_t n)
{
    if (g_used + n + 2 > sizeof g_buf) Flush();
    memcpy(g_buf + g_used, s, n); g_used += n;
    g_buf[g_used++] = '\r'; g_buf[g_used++] = '\n';
    if (g_used >= 8192) Flush();          // incremental: survive crashes
}

static void Ls(const char* s) { size_t n = 0; while (s[n]) n++; L(s, n); }

static size_t Hex64(char* o, uint64_t v)
{
    static const char d[] = "0123456789ABCDEF";
    o[0] = '0'; o[1] = 'x';
    int sh = 60; bool seen = false; size_t k = 2;
    while (sh >= 0) {
        char c = d[(v >> sh) & 0xF];
        if (c != '0' || seen || sh == 0) { o[k++] = c; seen = true; }
        sh -= 4;
    }
    return k;
}

static void Line2(const char* tag, uint64_t a, const char* name)
{
    char b[160]; size_t k = 0;
    while (tag[k]) { b[k] = tag[k]; k++; }
    b[k++] = ' ';
    k += Hex64(b + k, a);
    b[k++] = ' ';
    for (size_t i = 0; name[i] && k < sizeof b - 2; i++) b[k++] = name[i];
    L(b, k);
}

static void LineEnt(uint64_t a, uint64_t d, const char* name)
{
    char b[200]; size_t k = 3;
    b[0] = 'E'; b[1] = 'N'; b[2] = 'T';
    b[k++] = ' ';
    k += Hex64(b + k, a);
    b[k++] = ' '; b[k++] = '-'; b[k++] = '>'; b[k++] = ' ';
    k += Hex64(b + k, d);
    b[k++] = ' ';
    for (size_t i = 0; name[i] && k < sizeof b - 2; i++) b[k++] = name[i];
    L(b, k);
}

// run counter: every F2 writes its own numbered file set so multiple scans
// (e.g. one in Ops, one in a MP match) never overwrite each other.
static LONG g_runNo = 0;

// build "C:\Users\Public\<prefix><n>.<ext>" without CRT string helpers
static void MakePath(char* out, size_t cap, const char* prefix, const char* ext, LONG n)
{
    size_t k = 0;
    const char* dir = "C:\\Users\\Public\\";
    while (dir[k] && k < cap - 1) { out[k] = dir[k]; k++; }
    for (size_t i = 0; prefix[i] && k < cap - 1; i++) out[k++] = prefix[i];
    char d[12]; int dl = 0;
    if (n <= 0) d[dl++] = '0';
    while (n > 0 && dl < 11) { d[dl++] = '0' + (char)(n % 10); n /= 10; }
    for (int i = dl - 1; i >= 0 && k < cap - 1; i--) out[k++] = d[i];
    if (k < cap - 1) out[k++] = '.';
    for (size_t i = 0; ext[i] && k < cap - 1; i++) out[k++] = ext[i];
    out[k] = 0;
}

struct CountEnt { uint64_t key; uint32_t n; const char* name; };
static CountEnt g_counts[8192];
static size_t   g_ncounts = 0;

static void Count(uint64_t key, const char* name)
{
    for (size_t i = 0; i < g_ncounts; i++)
        if (g_counts[i].key == key) { g_counts[i].n++; return; }
    if (g_ncounts < 8192) {
        g_counts[g_ncounts].key = key;
        g_counts[g_ncounts].n = 1;
        g_counts[g_ncounts].name = name;
        g_ncounts++;
    }
}

// Fault-proof copy primitive for manual-mapped code: __try/__except needs the
// PE exception directory, which the driver's mapper never registers — the
// first fault inside a __try block is UNHANDLED and kills the process (run 2
// crash). NtReadVirtualMemory on our own process copies in the kernel: a bad
// page comes back as an error NTSTATUS, no user-mode exception at all.
typedef LONG (WINAPI *pfn_NtRVM)(HANDLE, PVOID, PVOID, ULONG, PULONG);
static pfn_NtRVM g_NtRVM = nullptr;

static bool SafeCopy(void* dst, const void* src, size_t n)
{
    if (!g_NtRVM) return false;
    ULONG got = 0;
    LONG st = g_NtRVM((HANDLE)-1, (PVOID)src, dst, (ULONG)n, &got);
    return st == 0;            // STATUS_SUCCESS — partial copies are failures
}

// Region filter: committed, plain RW/ERW base protection. PAGE_GUARD and
// PAGE_NOACCESS are excluded — a guard page touched by us loses its guard bit
// and the owning thread dies later on stack growth (run-1 crash).
static bool GoodRegion(const MEMORY_BASIC_INFORMATION& mbi)
{
    if (mbi.State != MEM_COMMIT) return false;
    if (mbi.Protect & (PAGE_GUARD | PAGE_NOACCESS)) return false;
    DWORD base = mbi.Protect & 0xFF;
    return base == PAGE_READWRITE || base == PAGE_EXECUTE_READWRITE;
}

// pass-B results for pass C: blueprint instance addresses (capped)
static uint64_t g_dataAddrs[262144];
static uint32_t g_dataName[262144];
static size_t   g_nData = 0;

static bool InDataList(uint64_t v, uint32_t* ni)
{
    size_t lo = 0, hi = g_nData;
    while (lo < hi) {
        size_t m = (lo + hi) >> 1;
        if (g_dataAddrs[m] == v) { *ni = g_dataName[m]; return true; }
        if (g_dataAddrs[m] < v) lo = m + 1; else hi = m;
    }
    return false;
}

static void SortData()
{
    for (size_t gap = g_nData / 2; gap; gap /= 2)
        for (size_t i = gap; i < g_nData; i++) {
            uint64_t a = g_dataAddrs[i]; uint32_t n = g_dataName[i];
            size_t j = i;
            while (j >= gap && g_dataAddrs[j - gap] > a) {
                g_dataAddrs[j] = g_dataAddrs[j - gap];
                g_dataName[j] = g_dataName[j - gap];
                j -= gap;
            }
            g_dataAddrs[j] = a; g_dataName[j] = n;
        }
}

// ranges to skip: our own module + the game image statics (pass B would hit
// the type registry itself; pass A statics are vtable refs, not objects)
static uint64_t g_skipLo[8], g_skipHi[8];
static int      g_nSkip = 0;

static bool Skip(uint64_t a)
{
    for (int i = 0; i < g_nSkip; i++)
        if (a >= g_skipLo[i] && a < g_skipHi[i]) return true;
    return false;
}

// snapshot buffer: one shared staging area
static uint8_t g_snap[64 * 1024];

// entity-layer vtable VAs (resolved per scan by ent::BeginCollect)
static uint64_t g_vaCharObj = 0, g_vaTeamReg = 0, g_vaContainer = 0;

// ---- pass A + B over one snapshotted chunk ----
static void ScanAB(uint64_t base, size_t len, uint64_t imgLo, uint64_t imgHi)
{
    size_t nq = len >> 3;
    const uint64_t* qv = (const uint64_t*)g_snap;
    for (size_t i = 0; i < nq; i++) {
        uint64_t v = qv[i];
        if (v < imgLo + 0x1000 || v >= imgHi) continue;      // game ptr only
        uint64_t q = base + (uint64_t)(i << 3);              // original address

        // pass A: v is a vtable; COL at v-8 (inside image, readable)
        if (v < imgLo + 0x31A2000ull) {
            uint64_t col = 0;
            if (SafeCopy(&col, (const void*)(v - 8), 8)) {
                int ci = gw2scan::ColFind(col);
                if (ci >= 0) {
                    if (!Skip(q))
                        Line2("RTTI", q, gw2scan::Ni(gw2scan::kCols[ci].ni));
                    Count(gw2scan::kCols[ci].va, gw2scan::Ni(gw2scan::kCols[ci].ni));
                    // hknpCharacterProxy = any character container — record
                    // for live viewProj discovery (positions read at runtime
                    // from proxy+0x70, see gw2_live.h)
                    if (NameHas(gw2scan::Ni(gw2scan::kCols[ci].ni), "hknpCharacterProxy"))
                        vproj::RecordTarget(q);
                    continue;
                }
            }
        }
        // entity layer (gw2_entities.h): charObj / team-registry /
        // weapon-container vtable hits. These .udata vtables carry no RTTI,
        // so a plain VA compare is their only discovery path.
        if (v == g_vaCharObj)         ent::AddCharObj(q);
        else if (v == g_vaTeamReg)    ent::AddTeamReg(q);
        else if (v == g_vaContainer)  ent::AddContainer(q);

        // pass B: v points into TypeInfo zone -> DataContainer header
        int ti2 = gw2scan::TiFind(v);
        if (ti2 >= 0) {
            if (g_nData < 262144) {
                g_dataAddrs[g_nData] = q;   // instance start = q (TypeInfo* first)
                g_dataName[g_nData] = gw2scan::kTiSites[ti2].ni;
                g_nData++;
            }
            if (!Skip(q))
                Line2("DATA", q, gw2scan::Ni(gw2scan::kTiSites[ti2].ni));
            Count(v, gw2scan::Ni(gw2scan::kTiSites[ti2].ni));
        }
    }
}

// ---- pass C over one snapshotted chunk: qwords pointing at pass-B
//      instances = live entity holders ----
// v3: no imgHi filter. This game's heap (0x8-0x9xxxxxxx, 0xCxxxxxxx) sits
// BELOW the image at 0x140000000 — the old "heap ptr only" check discarded
// every real holder (run 3: ENT=0).
static void ScanC(uint64_t base, size_t len, uint64_t imgHi)
{
    (void)imgHi;
    size_t nq = len >> 3;
    const uint64_t* qv = (const uint64_t*)g_snap;
    for (size_t i = 0; i < nq; i++) {
        uint64_t v = qv[i];
        uint32_t ni = 0;
        if (InDataList(v, &ni)) {
            LineEnt(base + (uint64_t)(i << 3), v, gw2scan::Ni(ni));
            Count(0x9000000000000000ull + ni, gw2scan::Ni(ni)); // ENT counts
        }
    }
}

// callback context (WalkRegion has no user-param slot)
static uint64_t g_ctxImgLo = 0, g_ctxImgHi = 0;

// ---- pass D: full RW heap dump -----------------------------------------
// One F2 writes EVERY committed writable region to disk (plus an address
// map). All feature analysis — ESP positions, camera matrices, weapon
// DataContainers, entity graphs, network structs — then happens offline
// against gw2_heapdump.bin; no more in-game scan rounds.
static HANDLE   g_dumpF = INVALID_HANDLE_VALUE;
static HANDLE   g_mapF  = INVALID_HANDLE_VALUE;
static uint64_t g_dumpBytes = 0;

static void DumpCb(uint64_t base, size_t len)
{
    DWORD wrote = 0;
    if (!WriteFile(g_dumpF, g_snap, (DWORD)len, &wrote, nullptr) || wrote != len)
        return;                       // disk full / IO error: skip piece
    uint64_t off = g_dumpBytes;
    g_dumpBytes += len;
    // map line: "DUMP 0x<va> 0x<len> 0x<fileoff>\r\n"
    char m[96]; size_t k = 0;
    const char* t = "DUMP ";
    while (t[k]) { m[k] = t[k]; k++; }
    k += Hex64(m + k, base);
    m[k++] = ' ';
    k += Hex64(m + k, (uint64_t)len);
    m[k++] = ' ';
    k += Hex64(m + k, off);
    m[k++] = '\r'; m[k++] = '\n';
    DWORD wm = 0;
    WriteFile(g_mapF, m, (DWORD)k, &wm, nullptr);
}

static void ScanABCb(uint64_t base, size_t len) { ScanAB(base, len, g_ctxImgLo, g_ctxImgHi); }
static void ScanCCb (uint64_t base, size_t len) { ScanC(base, len, g_ctxImgHi); }

// Walk [p,end) in 64 KB chunks; on fault fall back to page granularity.
typedef void (*ChunkScan)(uint64_t base, size_t len);
static void WalkRegion(uint64_t p, uint64_t end, uint64_t page, ChunkScan cs)
{
    while (p < end) {
        uint64_t left = end - p;
        size_t n = (left < sizeof g_snap) ? (size_t)left : sizeof g_snap;
        if (n & 7) n &= ~(size_t)7;                 // keep qword aligned
        if (n == 0) break;
        if (SafeCopy(g_snap, (const void*)p, n)) {
            cs(p, n);
            p += n;
        } else {
            // chunk faulted: salvage page by page
            for (uint64_t a = p; a < p + n; a += page) {
                size_t pn = (size_t)((p + n - a < page) ? (p + n - a) : page);
                if (SafeCopy(g_snap, (const void*)a, pn)) cs(a, pn);
            }
            p += n;
        }
    }
}

// pass A+B with dump: snapshot goes to disk AND through the scanners
static void ScanABDumpCb(uint64_t base, size_t len)
{
    DumpCb(base, len);   // chunk already staged in g_snap
    ScanAB(base, len, g_ctxImgLo, g_ctxImgHi);
}
void RunScan()
{
    // Resolve the kernel copy primitive FIRST — without it the scan can't
    // read anything safely.
    HMODULE nt = GetModuleHandleA("ntdll.dll");
    g_NtRVM = nt ? (pfn_NtRVM)GetProcAddress(nt, "NtReadVirtualMemory") : nullptr;

    LONG run = InterlockedIncrement(&g_runNo);
    char path[96];
    // session counter restarts at 1 with every game restart — skip past
    // dump numbers already on disk so a new session never overwrites a
    // previous session's files (today's pair must survive a relaunch).
    MakePath(path, sizeof path, "gw2_scan_", "txt", run);
    while (GetFileAttributesA(path) != INVALID_FILE_ATTRIBUTES) {
        run = InterlockedIncrement(&g_runNo);
        MakePath(path, sizeof path, "gw2_scan_", "txt", run);
    }
    g_f = CreateFileA(path, GENERIC_WRITE,
        FILE_SHARE_READ | FILE_SHARE_WRITE, nullptr, CREATE_ALWAYS,
        FILE_ATTRIBUTE_NORMAL, nullptr);
    if (g_f == INVALID_HANDLE_VALUE) return;

    const uint64_t IMG_LO = gw2::base();
    const uint64_t IMG_HI = IMG_LO + 0x3A00000ull;
    // entity layer: resolve vtable VAs + reset tables for this scan
    ent::BeginCollect(IMG_LO);
    g_vaCharObj   = ent::CharObjVT();
    g_vaTeamReg   = ent::TeamRegVT();
    g_vaContainer = ent::ContainerVT();

    // skip our own payload module range
    HMODULE self = nullptr;
    if (GetModuleHandleExA(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS |
                           GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
                           (LPCSTR)&RunScan, &self) && self) {
        MODULEINFO mi = {};
        if (GetModuleInformation(GetCurrentProcess(), self, &mi, sizeof mi)) {
            g_skipLo[g_nSkip] = (uint64_t)mi.lpBaseOfDll;
            g_skipHi[g_nSkip] = (uint64_t)mi.lpBaseOfDll + mi.SizeOfImage;
            g_nSkip++;
        }
    }
    // skip only the TypeInfo registry zone (self-referencing registry noise).
    // Game-image RW statics are KEPT — global singletons live there.
    g_skipLo[g_nSkip] = IMG_LO + 0x31E0000ull;
    g_skipHi[g_nSkip] = IMG_LO + 0x3280000ull;
    g_nSkip++;

    g_nData = 0; g_ncounts = 0; g_dumpBytes = 0;

    // Dump files open FIRST: pass A+B now dumps each chunk as it scans it.
    // The old dump-after-scan order lost everything above 0x87a80000 when
    // the game was closed mid-run (run B: proxies at 0x90-0x92M never
    // reached the file even though the scan pass had already seen them).
    MakePath(path, sizeof path, "gw2_heapdump_", "bin", run);
    g_dumpF = CreateFileA(path, GENERIC_WRITE,
        FILE_SHARE_READ, nullptr, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, nullptr);
    MakePath(path, sizeof path, "gw2_heapmap_", "txt", run);
    g_mapF = CreateFileA(path, GENERIC_WRITE,
        FILE_SHARE_READ, nullptr, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, nullptr);
    Ls("==== DUMP begin (dump-while-scanning) ====");

    {   // header with image base — needed to interpret absolute addresses.
        // Flushed at once: a non-empty file proves the scan thread started,
        // even if it dies right after.
        char hdr[128]; size_t k = 0;
        const char* m = "==== SCAN base=";
        while (m[k]) { hdr[k] = m[k]; k++; }
        k += Hex64(hdr + k, IMG_LO);
        hdr[k++] = ' '; hdr[k++] = '='; hdr[k++] = '=';
        hdr[k++] = '='; hdr[k++] = '=';
        L(hdr, k);
        Flush();
    }

    // ---- pass A + B over committed RW heap in low 8 GB ----
    const uint64_t SCAN_HI = 0x000200000000ull;
    SYSTEM_INFO si; GetSystemInfo(&si);
    const uint64_t PAGE = si.dwPageSize;

    uint64_t p = (uint64_t)si.lpMinimumApplicationAddress;
    uint64_t nextMark = 0x100000000ull;   // progress marker every 4 GB scanned
    while (p < SCAN_HI) {
        MEMORY_BASIC_INFORMATION mbi;
        if (!VirtualQuery((void*)p, &mbi, sizeof mbi)) { p += PAGE; continue; }
        uint64_t end = (uint64_t)mbi.BaseAddress + mbi.RegionSize;
        if (!GoodRegion(mbi)) { p = end; continue; }
        g_ctxImgLo = IMG_LO; g_ctxImgHi = IMG_HI;
        WalkRegion(p, end, PAGE, ScanABDumpCb);
        p = end;
        if (p >= nextMark) {   // crash later → markers show how far we got
            char b[96]; size_t k = 0;
            const char* m = "==== AB progress=";
            while (m[k]) { b[k] = m[k]; k++; }
            k += Hex64(b + k, p);
            b[k++] = ' '; b[k++] = '='; b[k++] = '=';
            b[k++] = '='; b[k++] = '=';
            L(b, k);
            Flush();
            nextMark += 0x100000000ull;
        }
    }

    Flush();
    {   // pass AB progress marker
        char b[96]; size_t k = 0;
        const char* m = "==== PASS AB DONE nData=";
        while (m[k]) { b[k] = m[k]; k++; }
        char nb[24]; size_t nl = 0; uint64_t x = g_nData;
        if (!x) nb[nl++] = '0';
        while (x && nl < 24) { nb[nl++] = '0' + (char)(x % 10); x /= 10; }
        for (size_t z = 0; z < nl; z++) b[k++] = nb[nl - 1 - z];
        b[k++] = ' '; b[k++] = '='; b[k++] = '=';
        b[k++] = '='; b[k++] = '=';
        L(b, k);
    }

    // ---- pass C: live entities = heap qwords pointing at pass-B instances ----
    SortData();
    p = (uint64_t)si.lpMinimumApplicationAddress;
    while (p < SCAN_HI) {
        MEMORY_BASIC_INFORMATION mbi;
        if (!VirtualQuery((void*)p, &mbi, sizeof mbi)) { p += PAGE; continue; }
        uint64_t end = (uint64_t)mbi.BaseAddress + mbi.RegionSize;
        if (!GoodRegion(mbi)) { p = end; continue; }
        g_ctxImgHi = IMG_HI;
        WalkRegion(p, end, PAGE, ScanCCb);
        p = end;
    }

    Flush();
    Ls("==== PASS C DONE ====");

    // ---- dump finalize (data was written during pass A+B) ----
    if (g_dumpF != INVALID_HANDLE_VALUE && g_mapF != INVALID_HANDLE_VALUE) {
        FlushFileBuffers(g_dumpF);
        FlushFileBuffers(g_mapF);
    }
    CloseHandle(g_dumpF);  g_dumpF = INVALID_HANDLE_VALUE;
    CloseHandle(g_mapF);   g_mapF  = INVALID_HANDLE_VALUE;
    {
        char s[128]; size_t k = 0;
        const char* t = "==== DUMP DONE bytes=";
        while (t[k]) { s[k] = t[k]; k++; }
        char nb[24]; size_t nl = 0; uint64_t x = g_dumpBytes;
        if (!x) nb[nl++] = '0';
        while (x && nl < 24) { nb[nl++] = '0' + (char)(x % 10); x /= 10; }
        for (size_t z = 0; z < nl; z++) s[k++] = nb[nl - 1 - z];
        L(s, k);
    }

    // ---- summary ----
    {
        Ls("==== COUNTS ====");
        for (size_t i = 0; i < g_ncounts; i++) {
            char c[192]; size_t n = 5;
            c[0] = 'C'; c[1] = 'O'; c[2] = 'U'; c[3] = 'N'; c[4] = 'T';
            c[n++] = ' ';
            n += Hex64(c + n, g_counts[i].key);
            c[n++] = ' ';
            char nb[24]; size_t nl = 0;
            uint32_t x = g_counts[i].n;
            if (!x) nb[nl++] = '0';
            while (x && nl < 24) { nb[nl++] = '0' + (char)(x % 10); x /= 10; }
            for (size_t z = 0; z < nl; z++) c[n++] = nb[nl - 1 - z];
            c[n++] = ' ';
            for (size_t z = 0; g_counts[i].name[z] && n < sizeof c - 2; z++)
                c[n++] = g_counts[i].name[z];
            L(c, n);
        }
        Ls("==== DONE ====");

        // ---- VMAP: where does writable memory actually live? ----
        // VirtualQuery-ONLY (no reads, cannot fault). If the game's heap sits
        // above SCAN_HI the RTTI/DATA/ENT lists come back empty and this
        // section is the only way to tell that apart from "scanner broken".
        {
            Ls("==== VMAP (writable regions >=1MB, low->high) ====");
            uint64_t q = SCAN_HI;
            const uint64_t TOP = (uint64_t)si.lpMaximumApplicationAddress;
            uint32_t lines = 0, nreg = 0;
            while (q < TOP && lines < 4096) {
                MEMORY_BASIC_INFORMATION mbi;
                if (!VirtualQuery((void*)q, &mbi, sizeof mbi)) break;
                uint64_t end = (uint64_t)mbi.BaseAddress + mbi.RegionSize;
                if (end <= q) break;   // paranoia: never loop forever
                if (mbi.State == MEM_COMMIT && mbi.RegionSize >= 0x100000) {
                    uint32_t base = mbi.Protect & 0xFF;
                    if (base == PAGE_READWRITE || base == PAGE_EXECUTE_READWRITE) {
                        nreg++;
                        if (lines < 4000) {
                            char v[160]; size_t n = 0;
                            const char* tag = "VMAP ";
                            for (size_t z = 0; tag[z]; z++) v[n++] = tag[z];
                            n += Hex64(v + n, (uint64_t)mbi.BaseAddress);
                            v[n++] = ' ';
                            n += Hex64(v + n, (uint64_t)mbi.RegionSize);
                            v[n++] = ' ';
                            n += Hex64(v + n, (uint64_t)mbi.Protect);
                            L(v, n);
                            lines++;
                        }
                    }
                }
                q = end;
            }
            char t[96]; size_t n = 0;
            const char* tag = "==== VMAP regions=";
            for (size_t z = 0; tag[z]; z++) t[n++] = tag[z];
            char nb[24]; size_t nl = 0; uint32_t x = nreg;
            if (!x) nb[nl++] = '0';
            while (x && nl < 24) { nb[nl++] = '0' + (char)(x % 10); x /= 10; }
            for (size_t z = 0; z < nl; z++) t[n++] = nb[nl - 1 - z];
            L(t, n);
            Ls("==== VMAP DONE ====");
        }
    }
    ent::EndCollect();
    FlushFileBuffers(g_f);
    CloseHandle(g_f);
    g_f = INVALID_HANDLE_VALUE;
}

// ---- light scan: entity discovery only (auto-refresh) ----------------------
// Pass-A vtable compare with NO file I/O, no RTTI lines, no heap dump: walk
// the low-8GB heap, feed the three entity vtable hits to ent::Add* (which
// shape-validates). Powers the auto-rescan loop so ESP works without a
// manual F2; keeps the live table (BeginCollect reset=false) — joiners
// append, dead rows blank out on the Present thread.
static void ScanLeanCb(uint64_t base, size_t len)
{
    const uint64_t IMG_LO = gw2::base();
    size_t nq = len >> 3;
    const uint64_t* qv = (const uint64_t*)g_snap;
    for (size_t i = 0; i < nq; i++) {
        uint64_t v = qv[i];
        uint64_t q = base + (uint64_t)(i << 3);
        // entity layer: the three vtable compares (Add* shape-validate)
        if (v == g_vaCharObj)        ent::AddCharObj(q);
        else if (v == g_vaTeamReg)   ent::AddTeamReg(q);
        else if (v == g_vaContainer) ent::AddContainer(q);

        // RTTI pass A — hknpCharacterProxy feeds the viewProj DISCOVERY
        // (vproj::RecordTarget). Without this the camera never locks in
        // release builds (light-scan-only) and nothing can project — the
        // 9/3 release postmortem: entities present, ESP blank.
        if (v < IMG_LO + 0x31A2000ull && v >= IMG_LO + 0x1000) {
            uint64_t col = 0;
            if (SafeCopy(&col, (const void*)(v - 8), 8)) {
                int ci = gw2scan::ColFind(col);
                if (ci >= 0 &&
                    NameHas(gw2scan::Ni(gw2scan::kCols[ci].ni), "hknpCharacterProxy"))
                    vproj::RecordTarget(q);
            }
        }
    }
}

void RunLightScan()
{
    HMODULE nt = GetModuleHandleA("ntdll.dll");
    g_NtRVM = nt ? (pfn_NtRVM)GetProcAddress(nt, "NtReadVirtualMemory") : nullptr;
    if (!g_NtRVM) return;

    const uint64_t IMG_LO = gw2::base();
    ent::BeginCollect(IMG_LO, false);      // incremental — never blank the ESP
    g_vaCharObj   = ent::CharObjVT();
    g_vaTeamReg   = ent::TeamRegVT();
    g_vaContainer = ent::ContainerVT();

    // skip ranges: our own module + the TypeInfo registry (same as RunScan)
    g_nSkip = 0;
    HMODULE self = nullptr;
    if (GetModuleHandleExA(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS |
                           GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
                           (LPCSTR)&RunLightScan, &self) && self) {
        MODULEINFO mi = {};
        if (GetModuleInformation(GetCurrentProcess(), self, &mi, sizeof mi)) {
            g_skipLo[g_nSkip] = (uint64_t)mi.lpBaseOfDll;
            g_skipHi[g_nSkip] = (uint64_t)mi.lpBaseOfDll + mi.SizeOfImage;
            g_nSkip++;
        }
    }
    g_skipLo[g_nSkip] = IMG_LO + 0x31E0000ull;
    g_skipHi[g_nSkip] = IMG_LO + 0x3280000ull;
    g_nSkip++;

    SYSTEM_INFO si; GetSystemInfo(&si);
    const uint64_t PAGE = si.dwPageSize;
    const uint64_t SCAN_HI = 0x000200000000ull;
    uint64_t p = (uint64_t)si.lpMinimumApplicationAddress;
    while (p < SCAN_HI) {
        MEMORY_BASIC_INFORMATION mbi;
        if (!VirtualQuery((void*)p, &mbi, sizeof mbi)) { p += PAGE; continue; }
        uint64_t end = (uint64_t)mbi.BaseAddress + mbi.RegionSize;
        if (!GoodRegion(mbi)) { p = end; continue; }
        WalkRegion(p, end, PAGE, ScanLeanCb);
        p = end;
    }
    ent::EndCollect();
}

} // namespace scanner
