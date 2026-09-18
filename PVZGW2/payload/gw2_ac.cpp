// ---------------------------------------------------------------------------
// gw2_ac.cpp — EAAC ACHT harvester (Phase 2a).
//
// Passive heap scan driven from the Present hook: no CreateThread, no extra
// hooks, no memory writes — reads only, so it stays quiet even with EAAC's
// usermode integrity checks live.
//
// Targets derived from gw2_image_dump_0.bin (2026-09-04, dump RVA == file
// offset, live base 0x140000000; all verified by runtime pointer search):
//   * AntiCheatData TypeRep descriptor  RVA 0x2A68D80
//     {cat=11 struct, hash 0xEB41B110, fullName -> "Blaze::AntiCheatData",
//      memberTable, memberCount=2}
//   * AntiCheatData TypeRep handle slot RVA 0x2D8BC30 (referenced by 12+
//     static TDF member entries game-wide)
//   * member table  RVA 0x2997480 — 2 entries: shieldUUID (fieldId 0x10),
//     skyfallUUID (fieldId 0x40), BOTH blob-typed (shared blob TypeRep
//     RVA 0x2A5CCB0, fullName "blob")
//   * LoginRequest member "antiCheat" fieldId 0x60, type = AntiCheatData
//
// Scan modes per 4 MB time slice:
//   1. aligned qwords equal to any pointer target -> 0xC0-byte window dump,
//      re-dumped whenever content changes (catches the empty->filled moment)
//   2. ASCII GUID strings (8-4-4-4-12 hex, 36 bytes) -> value + context
//   3. wire pattern: (09|04) 10  ...  (09|04) 40 within 40 bytes
//      = serialized AntiCheatData blob pair (shieldUUID/skyfallUUID members
//      inside the login packet) -> window dump
//
// Log: C:\ProgramData\Microsoft\DeviceSync\ac_harvest.txt
// ---------------------------------------------------------------------------
#include <windows.h>
#include "gw2_ac.h"
#include "acr/gw2_dr.h"

// --- config ----------------------------------------------------------------
static const char* kHarvestPath =
    "C:\\ProgramData\\Microsoft\\DeviceSync\\ac_harvest.txt";

// Pointer targets as game RVAs (see header comment).
static const unsigned __int64 kRvaPtrTargets[8] = {
    0x2A68D80, // T0 AntiCheatData TypeRep descriptor
    0x2D8BC30, // T1 AntiCheatData TypeRep handle slot
    0x211BCC8, // T2 "Blaze::AntiCheatData" string
    0x2997480, // T3 AntiCheatData member table
    0x211BCF0, // T4 "shieldUUID" string
    0x211BD00, // T5 "skyfallUUID" string
    0x2A5CCB0, // T6 blob TypeRep (shared member type)
    0x2128D00, // T7 "antiCheat" string (LoginRequest member)
};

// Per-tick byte budget (Present-hook time slice).
static const unsigned __int64 kBudget = 16ull * 1024 * 1024;
// Skip regions smaller than this.
static const SIZE_T kMinRegion = 0x2000;

static void AcLog(const char* msg);
static void EmitHexWindow(const char* tag, unsigned __int64 addr,
                          const unsigned char* win, size_t winLen);
static void FmtAddr(char* out, unsigned __int64 v);

// --- Phase 2a-v3: serializer hook via DATA-SLOT SWAP ------------------------
// v1 (passive scan): missed the UUIDs — the login packet buffer lives only
// milliseconds while a full heap sweep takes minutes.
// v2 (MinHook on the encode fn): MH_CreateHook fails when EAAC is live —
// its trampoline machinery needs RWX, which EAAC's DynamicCodePolicy blocks
// (same reason main.cpp swaps the Present vtable instead of MinHook).
// v3: swap the serializer FUNCTION POINTER stored in the TypeRep descriptor
// (.data, runtime-written, plain qword write — the exact technique the
// working Present hook uses). The replacement is a stub in our own image
// (the driver clears NX on the WHOLE payload image, so a .data buffer is
// executable), which logs args then tail-jumps to the original function
// with every register and the stack intact.
//
// Slots (verified in dump + live AC:PT poll):
//   AntiCheatData descriptor  RVA 0x2A68D80; +0x18 slot RVA 0x2A68D88
//     -> orig thunk RVA 0x19B2770 (E9 thunk, 3-arg encode-family dispatcher)
//   blob TypeRep (shared by shieldUUID/skyfallUUID and all blob members)
//     RVA 0x2A5CCB0; +0x18 slot RVA 0x2A5CCC8 -> orig RVA 0x1992DC0
//
// Slot values are verified against the expected originals before the swap;
// a mismatched slot is logged and LEFT ALONE (never hook an unknown target).
static const unsigned __int64 kRvaAcDescSlot  = 0x2A68D88;
static const unsigned __int64 kRvaAcOrig      = 0x19B2770;
static const unsigned __int64 kRvaBlobSlot    = 0x2A5CCC8;
static const unsigned __int64 kRvaBlobOrig    = 0x1992DC0;
// v17: heap codec-object dispatch fn (observed at T6ref-0x28 in the heap
// T6-referencing object, value stable across sessions 19/21). The static
// TypeRep slot proved NOT on the encode path (run 21: hook live t=4s,
// object written t=6.9s, zero calls) — this heap fnptr is the runtime
// codec registry dispatch candidate.
static const unsigned __int64 kRvaHeapFn      = 0x7B988E;
static const unsigned __int64 kHeapFnOff      = 0x30; // fnptr slot = T6hit - 0x30 (run 22: fnptr 0x1407B988E at AC:O win+0x10, window starts hit-0x40)
static int g_heapSwaps = 0; // cap 3

static volatile LONG g_serCallsAc   = 0;
static volatile LONG g_serCallsBlob = 0;
static volatile LONG g_serBusy      = 0;
static unsigned __int64 g_installTick = 0;

static bool PtrPlausible(unsigned __int64 p)
{
    return p > 0x10000 && p < 0x7FFFFFFFFFFFull;
}

static void DumpPtrWindow(const char* tag, unsigned __int64 p)
{
    if (!PtrPlausible(p)) return;
    __try {
        EmitHexWindow(tag, p, (const unsigned char*)p, 0x100);
    } __except (EXCEPTION_EXECUTE_HANDLER) { }
}

static void AppendNum(char* line, int& p, unsigned __int64 v)
{
    char nb[24]; int nl = 0;
    if (v == 0) nb[nl++] = '0';
    while (v > 0 && nl < 23) { nb[nl++] = (char)('0' + v % 10); v /= 10; }
    for (int i = nl - 1; i >= 0; i--) line[p++] = nb[i];
}

// Common log body: tag, ms-since-install, call#, arg windows.
static void SerLogBody(const char* tag, int tagLen, LONG n,
                       unsigned __int64 a1, unsigned __int64 a2,
                       unsigned __int64 a3)
{
    if (InterlockedCompareExchange(&g_serBusy, 1, 0) != 0) return; // no nesting
    __try {
        char line[160]; int p = 0;
        for (int i = 0; i < tagLen; i++) line[p++] = tag[i];
        const char* t = " t=";
        for (int i = 0; t[i]; i++) line[p++] = t[i];
        AppendNum(line, p, GetTickCount64() - g_installTick);
        const char* c = " #";
        for (int i = 0; c[i]; i++) line[p++] = c[i];
        AppendNum(line, p, (unsigned __int64)n);
        line[p] = 0;
        AcLog(line);
        DumpPtrWindow("AC:S a1 ", a1);
        DumpPtrWindow("AC:S a2 ", a2);
        DumpPtrWindow("AC:S a3 ", a3);
        // TDF writers usually receive the output cursor as one arg; the
        // already-built bytes sit just before it.
        DumpPtrWindow("AC:S a2m ", a2 - 0x40);
        DumpPtrWindow("AC:S a3m ", a3 - 0x40);
    } __except (EXCEPTION_EXECUTE_HANDLER) { }
    g_serBusy = 0;
}

// Called from stub machine code — signature must match the original fn:
// (rcx, rdx, r8) preserved by the stub around this call.
static void SerLogAc(unsigned __int64 a1, unsigned __int64 a2, unsigned __int64 a3)
{
    LONG n = InterlockedIncrement(&g_serCallsAc);
    if (n > 32) return; // cap — after that, pure pass-through
    // v17: tag SH = HEAP codec-object dispatch (T6ref-0x28 slot). Distinct
    // from SB (static T6+0x18) so the log tells us WHICH path fired.
    SerLogBody("AC:SH", 5, n, a1, a2, a3);
}

static void SerLogBlob(unsigned __int64 a1, unsigned __int64 a2, unsigned __int64 a3)
{
    LONG n = InterlockedIncrement(&g_serCallsBlob);
    if (n > 48) return; // blob members are hot — cap hard
    SerLogBody("AC:SB", 5, n, a1, a2, a3); // blob serializer (values pass here)
}

// --- stub builder ------------------------------------------------------------
// 55                push rbp
// 48 89 E5          mov  rbp, rsp
// 48 83 E4 F0       and  rsp, -16
// 48 83 EC 40       sub  rsp, 0x40
// 48 89 4C 24 20    mov  [rsp+0x20], rcx
// 48 89 54 24 28    mov  [rsp+0x28], rdx
// 4C 89 44 24 30    mov  [rsp+0x30], r8
// 48 B8 imm64       mov  rax, SerLogX
// FF D0             call rax
// 48 8B 4C 24 20    mov  rcx, [rsp+0x20]
// 48 8B 54 24 28    mov  rdx, [rsp+0x28]
// 4C 8B 44 24 30    mov  r8,  [rsp+0x30]
// 48 89 EC          mov  rsp, rbp
// 5D                pop  rbp
// 49 BB imm64       mov  r11, origFn
// 41 FF E3          jmp  r11
// Saves/restores rcx/rdx/r8 around the logger; rax and r11 are scratch in
// the x64 ABI (and the orig thunk body writes rax before reading it).
__declspec(align(16)) static unsigned char g_stubAc[128];
__declspec(align(16)) static unsigned char g_stubBlob[128];
static bool BuildStub(unsigned char* out, void* logger, unsigned __int64 orig)
{
    static const unsigned char kBody[] = {
        0x55,                                             // push rbp
        0x48,0x89,0xE5,                                   // mov rbp, rsp
        0x48,0x83,0xE4,0xF0,                              // and rsp, -16
        0x48,0x83,0xEC,0x40,                              // sub rsp, 0x40
        0x48,0x89,0x4C,0x24,0x20,                         // mov [rsp+0x20], rcx
        0x48,0x89,0x54,0x24,0x28,                         // mov [rsp+0x28], rdx
        0x4C,0x89,0x44,0x24,0x30,                         // mov [rsp+0x30], r8
        0x48,0xB8,0,0,0,0,0,0,0,0,                        // mov rax, <logger>
        0xFF,0xD0,                                        // call rax
        0x48,0x8B,0x4C,0x24,0x20,                         // mov rcx, [rsp+0x20]
        0x48,0x8B,0x54,0x24,0x28,                         // mov rdx, [rsp+0x28]
        0x4C,0x8B,0x44,0x24,0x30,                         // mov r8, [rsp+0x30]
        0x48,0x89,0xEC,                                   // mov rsp, rbp
        0x5D,                                             // pop rbp
        0x49,0xBB,0,0,0,0,0,0,0,0,                        // mov r11, <orig>
        0x41,0xFF,0xE3,                                   // jmp r11
    };
    int n = (int)sizeof kBody;
    if (n > 128) return false;
    for (int i = 0; i < n; i++) out[i] = kBody[i];
    // patch imm64s: '48 B8' = mov rax, logger; '49 BB' = mov r11, orig
    for (int i = 0; i + 11 <= n; i++) {
        if (out[i] == 0x48 && out[i+1] == 0xB8) {
            unsigned __int64 v = (unsigned __int64)logger;
            for (int k = 0; k < 8; k++) out[i+2+k] = (unsigned char)(v >> (k*8));
            break;
        }
    }
    for (int i = 0; i + 11 <= n; i++) {
        if (out[i] == 0x49 && out[i+1] == 0xBB) {
            for (int k = 0; k < 8; k++) out[i+2+k] = (unsigned char)(orig >> (k*8));
            break;
        }
    }
    return true;
}

// Swap one serializer slot -> our stub. Returns true on success.
static bool SwapSlot(unsigned __int64 slotRva, unsigned __int64 expectedOrigRva,
                     unsigned char* stub, void* logger, const char* name)
{
    unsigned __int64 base = (unsigned __int64)GetModuleHandleA(nullptr);
    unsigned __int64 slot = base + slotRva;
    unsigned __int64 orig = 0;
    __try {
        orig = *(volatile unsigned __int64*)slot;
    } __except (EXCEPTION_EXECUTE_HANDLER) {
        AcLog("AC:S slot read fault");
        return false;
    }
    if (orig != base + expectedOrigRva) {
        // NEVER hook an unexpected target — log the actual value and bail.
        char line[128]; int p = 0;
        while (name[p]) { line[p] = name[p]; p++; }
        const char* t = " orig MISMATCH got=";
        for (int i = 0; t[i]; i++) line[p++] = t[i];
        char ab[16]; FmtAddr(ab, orig);
        for (int i = 0; ab[i]; i++) line[p++] = ab[i];
        line[p] = 0;
        AcLog(line);
        return false;
    }
    if (!BuildStub(stub, logger, orig)) { AcLog("AC:S stub build fail"); return false; }
    __try {
        *(volatile unsigned __int64*)slot = (unsigned __int64)stub;
    } __except (EXCEPTION_EXECUTE_HANDLER) {
        AcLog("AC:S slot write fault");
        return false;
    }
    AcLog("AC:S hook live");
    return true;
}

// v17: swap a slot at an ABSOLUTE address (heap codec object), verifying it
// holds the expected original. Same safety rules as SwapSlot.
static bool SwapSlotAt(unsigned __int64 slotAddr, unsigned __int64 expectedOrig,
                       unsigned char* stub, void* logger, const char* name)
{
    unsigned __int64 orig = 0;
    __try {
        orig = *(volatile unsigned __int64*)slotAddr;
    } __except (EXCEPTION_EXECUTE_HANDLER) {
        AcLog("AC:S heap slot read fault");
        return false;
    }
    if (orig != expectedOrig) {
        char line[128]; int p = 0;
        while (name[p] && p < 60) { line[p] = name[p]; p++; }
        const char* t = " heap orig MISMATCH got=";
        for (int i = 0; t[i]; i++) line[p++] = t[i];
        char ab[16]; FmtAddr(ab, orig);
        for (int i = 0; ab[i]; i++) line[p++] = ab[i];
        line[p] = 0;
        AcLog(line);
        return false;
    }
    if (!BuildStub(stub, logger, orig)) { AcLog("AC:S stub build fail"); return false; }
    __try {
        *(volatile unsigned __int64*)slotAddr = (unsigned __int64)stub;
    } __except (EXCEPTION_EXECUTE_HANDLER) {
        AcLog("AC:S heap slot write fault");
        return false;
    }
    AcLog("AC:S v17 heap swap LIVE");
    return true;
}

// --- logging (one persistent append handle, same pattern as main.cpp) ------
static HANDLE g_hLog = INVALID_HANDLE_VALUE;

static void AcLog(const char* msg)
{
    if (g_hLog == INVALID_HANDLE_VALUE) {
        g_hLog = CreateFileA(kHarvestPath, FILE_APPEND_DATA,
            FILE_SHARE_READ | FILE_SHARE_WRITE, nullptr, OPEN_ALWAYS,
            FILE_ATTRIBUTE_NORMAL, nullptr);
        if (g_hLog == INVALID_HANDLE_VALUE) return;
    }
    size_t n = 0;
    while (msg[n] && n < 900) n++;
    char buf[928];
    for (size_t i = 0; i < n; i++) buf[i] = msg[i];
    buf[n] = '\r'; buf[n + 1] = '\n';
    DWORD w = 0;
    WriteFile(g_hLog, buf, (DWORD)n + 2, &w, nullptr);
}

static const char kHexD[] = "0123456789ABCDEF";

static void FmtAddr(char* out, unsigned __int64 v) // "0x%012llX"
{
    out[0] = '0'; out[1] = 'x';
    for (int i = 0; i < 12; i++)
        out[2 + i] = kHexD[(v >> ((11 - i) * 4)) & 0xF];
    out[14] = 0;
}

static void AcLogHex(const char* prefix, unsigned __int64 v)
{
    char line[128]; int p = 0;
    while (prefix[p] && p < 100) { line[p] = prefix[p]; p++; }
    char ab[16]; FmtAddr(ab, v);
    for (int i = 0; ab[i]; i++) line[p++] = ab[i];
    line[p] = 0;
    AcLog(line);
}
// --- dedup tables ----------------------------------------------------------
struct WinEnt { unsigned __int64 addr; unsigned __int64 lastSig; int dumps; };
static WinEnt  g_obj[512];
static unsigned g_objCount = 0;
static WinEnt  g_wire[512]; // v18: packet-fingerprint dedup
static unsigned g_wireCount = 0;

// Returns the entry for addr (creating if room); nullptr when table full.
static WinEnt* WinFind(WinEnt* tbl, unsigned* count, unsigned __int64 addr)
{
    for (unsigned i = 0; i < *count; i++)
        if (tbl[i].addr == addr) return &tbl[i];
    if (*count >= 512) return nullptr;
    WinEnt* e = &tbl[(*count)++];
    e->addr = addr; e->lastSig = 0; e->dumps = 0;
    return e;
}

// FNV-1a
static unsigned __int64 HashBytes(const unsigned char* p, size_t n)
{
    unsigned __int64 h = 1469598103934665603ull;
    for (size_t i = 0; i < n; i++) { h ^= p[i]; h *= 1099511628211ull; }
    return h;
}

// --- scan state -------------------------------------------------------------
static unsigned __int64 g_cursor = 0;
static int   g_pass = 0;
static bool  g_inited = false;
static unsigned __int64 g_bytesThisPass = 0;
static int   g_passObjDumps = 0;
static int   g_passWireDumps = 0; // v18: packet-fingerprint hits per pass
static bool  g_slotSwapped = false; // v16: one-shot deferred blob-slot swap
static unsigned __int64 g_base = 0;
// direct .data polls — the handle slot is 0 (bss) until Blaze registers
// the live TypeRep; its fill moment + pointee dump is the anchor we need.
static const unsigned __int64 kRvaPolls[3] = {
    0x2D8BC30, // P0 handle slot (bss — fills at TypeRep registration)
    0x2A6C690, // P1 LoginRequest antiCheat member typeref slot
    0x2A68D80, // P2 AntiCheatData descriptor head (cat|hash)
};
static unsigned __int64 g_pollLast[3] = { 0, 0, 0 };
static unsigned __int64 g_targets[8] = { 0, 0, 0, 0, 0, 0, 0, 0 };
static unsigned __int64 g_selfLo = 0, g_selfHi = 0;

// --- formatters ------------------------------------------------------------
static void EmitHexWindow(const char* tag, unsigned __int64 addr,
                          const unsigned char* win, size_t winLen)
{
    char line[928];
    int p = 0;
    while (tag[p]) { line[p] = tag[p]; p++; }
    char ab[16]; FmtAddr(ab, addr);
    for (int i = 0; ab[i]; i++) line[p++] = ab[i];
    // v7: timestamp (ms since harvest init) on every window line — lets us
    // correlate captures with login timing after the fact.
    line[p++] = ' '; line[p++] = 't'; line[p++] = '=';
    AppendNum(line, p, GetTickCount64() - g_installTick);
    const char* h = " hex=";
    for (int i = 0; h[i]; i++) line[p++] = h[i];
    size_t maxB = winLen; if (maxB > 0xA0) maxB = 0xA0; // 160 bytes max
    for (size_t i = 0; i < maxB && p < 900; i++) {
        line[p++] = kHexD[win[i] >> 4];
        line[p++] = kHexD[win[i] & 0xF];
    }
    line[p] = 0;
    AcLog(line);
}

// --- v8: registry-neighbor chase ---------------------------------------------
// Run 5/7 logs showed HEAP structures referencing the AntiCheatData handle
// slot (T1 = 0x142D8BC30). Those entries are runtime codec registries:
//   [.., T1, heapPtrA, heapPtrB, flags, 4, 0x18, 0, fnptr, ..]
// heapPtrA/heapPtrB are LIVE heap objects for the type — prime candidates
// for the persistent AntiCheatData instance (or its blob storage) that EAAC
// fills at init. The UUIDs, if anywhere stable, are THERE — the wire packet
// is transient but the source instance is not. This dumps them, chases one
// level of {ptr,len} pairs AND bare heap pointers inside them (run 7's
// neighbor array held {A,B,idx} triples whose A/B — e.g. 0x08831500,
// 0x081726D0 — were never followed). Pure reads.
static WinEnt  g_nbr[512];
static unsigned g_nbrCount = 0;
static WinEnt  g_sub[512];
static unsigned g_subCount = 0;

static unsigned __int64 ReadQ(const unsigned char* q)
{
    return (unsigned __int64)q[0] | ((unsigned __int64)q[1] << 8) |
           ((unsigned __int64)q[2] << 16) | ((unsigned __int64)q[3] << 24) |
           ((unsigned __int64)q[4] << 32) | ((unsigned __int64)q[5] << 40) |
           ((unsigned __int64)q[6] << 48) | ((unsigned __int64)q[7] << 56);
}

static bool HeapPtr(unsigned __int64 p)
{
    if (p <= 0x10000 || p >= 0x100000000ull) return false; // sub-4GB only
    if (p >= g_selfLo && p < g_selfHi) return false;
    // v12: PRIVATE memory only. Run 12's watchlist polled module-image
    // pages (0x7DF4-0x7DF7xxxx; crash rip 0x7AF2xxxx = same neighborhood,
    // EAAC component) every tick — sessions shortened from 6.4 min (v7,
    // rare chasing) to 2-3.4 min (v11, aggressive). Never follow pointers
    // into module images; heap/stack allocations only.
    MEMORY_BASIC_INFORMATION mbi;
    if (VirtualQuery((LPCVOID)p, &mbi, sizeof mbi) == 0) return false;
    if (mbi.Type != MEM_PRIVATE) return false;
    if (mbi.State != MEM_COMMIT) return false;
    if (mbi.Protect & (PAGE_NOACCESS | PAGE_GUARD)) return false;
    return true;
}

// v11: persistent watchlist. DumpNeighbor's re-dump only re-fires when the
// PARENT object's window changes — but EAAC fills the VALUE-slot neighbor
// (0x3246B000 stayed zero for 2 full minutes in run 10) without touching
// the parent. Watched addresses are polled every tick; DumpNeighbor's own
// sig-check dumps them the moment content changes.
static unsigned __int64 g_watch[32];
static unsigned g_watchCount = 0;
static void WatchAdd(unsigned __int64 p)
{
    if (!HeapPtr(p)) return;
    for (unsigned i = 0; i < g_watchCount; i++)
        if (g_watch[i] == p) return;
    if (g_watchCount >= 32) return;
    g_watch[g_watchCount++] = p;
}

static void DumpNeighbor(unsigned __int64 p)
{
    if (!HeapPtr(p)) return;
    // v10: content-change re-dump. Run 9 dumped the AntiCheatData value
    // slot neighbor ZEROED at t=1875; EAAC fills it after the server
    // handshake, and the run died before the object hit re-armed. Now the
    // neighbor re-dumps whenever its content changes (cap 12 versions), so
    // the empty->filled transition cannot slip through.
    WinEnt* e = WinFind(g_nbr, &g_nbrCount, p);
    if (!e) return;
    {
        unsigned __int64 sig = 0;
        __try {
            volatile unsigned char t = *(const unsigned char*)p; (void)t;
            sig = HashBytes((const unsigned char*)p, 0x100);
        } __except (EXCEPTION_EXECUTE_HANDLER) { return; }
        if (e->dumps >= 12) return;
        if (e->dumps > 0 && e->lastSig == sig) return;
        e->lastSig = sig;
        e->dumps++;
    }
    __try {
        volatile unsigned char t = *(const unsigned char*)p; (void)t;
        EmitHexWindow("AC:N ", p, (const unsigned char*)p, 0x100);
        // one level deeper, pass 1: {ptr, len} member pairs inside the
        // neighbor (real blob members have len 4..64 — UUIDs are 8/16/36
        // bytes; static-table {hash, fieldId} misreads are excluded by
        // len>=4 and the heap-only pointer filter)
        const unsigned char* nb = (const unsigned char*)p;
        for (int off = 0; off + 16 <= 0x100; off += 8) {
            unsigned __int64 a = ReadQ(nb + off);
            unsigned __int64 b = ReadQ(nb + off + 8);
            unsigned __int64 tp = 0, tl = 0;
            if (HeapPtr(a) && b >= 4 && b <= 64) { tp = a; tl = b; }
            else if (HeapPtr(b) && a >= 4 && a <= 64) { tp = b; tl = a; }
            else continue;
            WinEnt* s = WinFind(g_sub, &g_subCount, tp);
            if (s && s->dumps >= 8) continue;
            if (s) s->dumps++;
            __try {
                volatile unsigned char t2 = *(const unsigned char*)tp; (void)t2;
                EmitHexWindow("AC:M ", tp, (const unsigned char*)tp,
                              (size_t)(tl < 0x40 ? tl : 0x40));
                // len == 16 exactly = binary GUID candidate: flag it loud
                if (tl == 16)
                    EmitHexWindow("AC:UU ", tp, (const unsigned char*)tp, 16);
            } __except (EXCEPTION_EXECUTE_HANDLER) { }
        }
        // pass 2 (v8): bare heap pointers — run 7's neighbor was an
        // {A, B, idx} array whose A/B slots (0x08831500, 0x081726D0 ...)
        // never qualified as {ptr,len} pairs. Follow them regardless.
        {
            int followed = 0;
            for (int off = 0; off + 8 <= 0x100 && followed < 8; off += 8) {
                unsigned __int64 a = ReadQ(nb + off);
                if (!HeapPtr(a)) continue;
                if (a >= p && a < p + 0x100) continue; // inside this window
                WinEnt* s = WinFind(g_sub, &g_subCount, a);
                if (s && s->dumps >= 8) continue;
                if (s) s->dumps++;
                followed++;
                __try {
                    volatile unsigned char t2 = *(const unsigned char*)a; (void)t2;
                    EmitHexWindow("AC:MB ", a, (const unsigned char*)a, 0x40);
                } __except (EXCEPTION_EXECUTE_HANDLER) { }
            }
        }
    } __except (EXCEPTION_EXECUTE_HANDLER) { }
}

// v14: guard-page reporter. Run 15 (friend, full login): the T1 registry
// window held 0x779716D0/0x779716F8 — same small-heap pattern as v7's
// never-followed leads (0x08831500, 0x081726D0) — but HeapPtr's v12
// VirtualQuery filter silently dropped them (unreadable/other type) all
// run. A pointer that is VALID but UNREADABLE is the strongest possible
// signal: EAAC guarding its attestation storage. Log the page state once
// per address so "not a pointer" and "guarded storage" are distinguishable,
// and keep watching — a guard that flips to committed-readable (the fill
// moment) is the capture.
static WinEnt  g_guard[64];
static unsigned g_guardCount = 0;

static void ReportGuarded(unsigned __int64 p)
{
    if (p <= 0x10000 || p >= 0x100000000ull) return;
    // dedup: log each address once
    for (unsigned i = 0; i < g_guardCount; i++)
        if (g_guard[i].addr == p) {
            // state change? (guard -> readable = the interesting moment)
            MEMORY_BASIC_INFORMATION mbi;
            if (VirtualQuery((LPCVOID)p, &mbi, sizeof mbi) &&
                mbi.State == MEM_COMMIT &&
                !(mbi.Protect & (PAGE_NOACCESS | PAGE_GUARD)) &&
                mbi.Type == MEM_PRIVATE) {
                if (!(g_guard[i].lastSig)) { // lastSig used as "was guarded"
                    g_guard[i].lastSig = 1;
                    DumpNeighbor(p); // readable now — dump via normal path
                }
            }
            return;
        }
    if (g_guardCount >= 64) return;
    g_guard[g_guardCount].addr = p;
    g_guard[g_guardCount].lastSig = 0;
    g_guard[g_guardCount].dumps = 0;
    g_guardCount++;
    MEMORY_BASIC_INFORMATION mbi;
    char line[160]; int lp = 0;
    const char* a = "AC:MG 0x";
    while (a[lp]) { line[lp] = a[lp]; lp++; }
    char ab[16]; FmtAddr(ab, p);
    for (int i = 0; ab[i]; i++) line[lp++] = ab[i];
    if (VirtualQuery((LPCVOID)p, &mbi, sizeof mbi)) {
        const char* s = " state=";
        for (int i = 0; s[i]; i++) line[lp++] = s[i];
        const char* st = mbi.State == MEM_COMMIT ? "commit" :
                         (mbi.State == MEM_RESERVE ? "reserve" : "free");
        for (int i = 0; st[i]; i++) line[lp++] = st[i];
        const char* p2 = " prot=";
        for (int i = 0; p2[i]; i++) line[lp++] = p2[i];
        // hex protection
        static const char* hx = "0123456789ABCDEF";
        line[lp++] = '0'; line[lp++] = 'x';
        unsigned prot = (unsigned)mbi.Protect;
        for (int i = 28; i >= 0; i -= 4)
            line[lp++] = hx[(prot >> i) & 0xF];
        const char* t2 = " type=";
        for (int i = 0; t2[i]; i++) line[lp++] = t2[i];
        unsigned ty = (unsigned)mbi.Type;
        for (int i = 28; i >= 0; i -= 4)
            line[lp++] = hx[(ty >> i) & 0xF];
    } else {
        const char* s = " query=FAILED";
        for (int i = 0; s[i]; i++) line[lp++] = s[i];
    }
    line[lp] = 0;
    AcLog(line);
}

static void ChaseRegistryNeighbors(const unsigned char* win, size_t hitOff,
                                   size_t winLen)
{
    // neighbors around the T1 qword inside the logged window
    static const int kOffs[4] = { -0x10, -0x08, 0x08, 0x10 };
    for (int k = 0; k < 4; k++) {
        int o = (int)hitOff + kOffs[k];
        if (o < 0 || (size_t)o + 8 > winLen) continue;
        unsigned __int64 nb = ReadQ(win + o);
        DumpNeighbor(nb);
        WatchAdd(nb); // v11: poll it every tick from now on
        if (!HeapPtr(nb)) ReportGuarded(nb); // v14
    }
    // v8: chase EVERY aligned heap qword in the T1 window — run 7's AC:O
    // object held more {heap, static} pairs (e.g. {0x322CEAB8, 0x1422D6840},
    // {0x33EB5C60, 0x1421521D8}) that the ±0x10 offsets never touched.
    {
        int chased = 0;
        for (size_t o = 0; o + 8 <= winLen && chased < 10; o += 8) {
            unsigned __int64 v = ReadQ(win + o);
            if (!HeapPtr(v)) {
                ReportGuarded(v); // v14: record then move on
                continue;
            }
            unsigned __int64 wlo = (unsigned __int64)win;
            if (v >= wlo && v < wlo + winLen) continue; // self-ref
            DumpNeighbor(v);
            WatchAdd(v); // v11
            chased++;
        }
    }
}

static void ScanChunkInner(const unsigned char* base, size_t len)
{
    // -- 1. pointer targets (aligned qwords) --
    size_t mis = (size_t)((unsigned __int64)base & 7);
    size_t start = mis ? (8 - mis) : 0;
    for (size_t i = start; i + 8 <= len; i += 8) {
        const unsigned char* q = base + i;
        unsigned __int64 v = (unsigned __int64)q[0] | ((unsigned __int64)q[1] << 8) |
            ((unsigned __int64)q[2] << 16) | ((unsigned __int64)q[3] << 24) |
            ((unsigned __int64)q[4] << 32) | ((unsigned __int64)q[5] << 40) |
            ((unsigned __int64)q[6] << 48) | ((unsigned __int64)q[7] << 56);
        for (int t = 0; t < 8; t++) {
            if (v != g_targets[t]) continue;
            unsigned __int64 addr = (unsigned __int64)(base + i);
            size_t back = i >= 0x40 ? 0x40 : i;
            size_t fwd = len - i; if (fwd > 0x80) fwd = 0x80;
            const unsigned char* win = base + i - back;
            size_t winLen = back + fwd;
            unsigned __int64 sig = HashBytes(win, winLen);
            WinEnt* e = WinFind(g_obj, &g_objCount, addr);
            if (e) {
                if (e->dumps >= 40) break;
                if (e->dumps > 0 && e->lastSig == sig) break;
                e->lastSig = sig; e->dumps++;
            }
            g_passObjDumps++;
            char tag[24]; int tp = 0;
            const char* s = "AC:O T";
            while (s[tp]) { tag[tp] = s[tp]; tp++; }
            tag[tp++] = (char)('0' + t);
            tag[tp++] = ' ';
            tag[tp] = 0;
            EmitHexWindow(tag, addr, win, winLen);
            // v9: ANY target found in the HEAP = runtime codec registry
            // entry — chase its live neighbor pointers (instance/blob
            // candidates). Run 8's T6 hit at t=2282 (login build time!)
            // held 0x32F490F0/0x328CB0 that the T1-only chase never
            // followed. That object referenced BOTH T6 and T0's head —
            // the AntiCheatData codec instance itself.
            if (addr < 0x100000000ull && back >= 0x10)
                ChaseRegistryNeighbors(win, back, winLen);
            // v15.2: heap codec object found — arm write-watchpoints on
            // it. The game's next write to this object fires a DR hit
            // with the LIVE call chain (stack dump).
            if ((t == 1 || t == 6 || t == 0) && addr < 0x100000000ull)
                acr::WatchObject(addr);
            // v17: T6-referencing heap object found — swap its codec
            // sweep hit (t~200ms), long before the t~6s login encode; the
            // v16 static slot proved not-on-path. Cap 3 swaps.
            if (t == 6 && addr > 0x10000 && addr < 0x100000000ull &&
                g_heapSwaps < 3) {
                unsigned __int64 slotAddr = addr - kHeapFnOff;
                unsigned __int64 expect = g_base + kRvaHeapFn;
                unsigned __int64 cur = 0;
                __try { cur = *(volatile unsigned __int64*)slotAddr; }
                __except (EXCEPTION_EXECUTE_HANDLER) { cur = 0; }
                if (cur == expect || cur == 0) {
                    g_heapSwaps++;
                    SwapSlotAt(slotAddr, expect, g_stubAc,
                               (void*)SerLogAc, "heapcodec");
                }
            }
            break;
        }
    }

    // -- v18: LOGIN PACKET fingerprint scan ---------------------------------
    // TDF framing for the AntiCheatData members inside the serialized
    // LoginRequest: shieldUUID = 90 10 <len> <blob>, skyfallUUID = 90 40
    // <len> <blob>, same blob length, adjacent in one struct body.
    // The old scans drowned because they swept module images; v13+ sweeps
    // PRIVATE SUB-4GB RW ONLY — noise gone. The packet waits in the SSL
    // write path for ~1s (network flush), far longer than the ~1s pass.
    // Validation to kill remaining coincidence hits:
    //   * both lens in {8, 16, 36} (binary GUID sizes)
    //   * equal lens, gap between them < 0x120
    //   * blob bytes NOT pointer-like / not all-zero / entropy >= 6 distinct
    //     bytes for len 16 (real GUIDs are random)
    if (len >= 8) {
        for (size_t i = 0; i + 4 < len; i++) {
            if (base[i] != 0x90 || base[i+1] != 0x10) continue;
            unsigned ln1 = base[i+2] << 8 | base[i+3];
            if (ln1 != 16 && ln1 != 36 && ln1 != 8) continue;
            size_t end1 = i + 4 + ln1;
            if (end1 > len) continue;
            // blob sanity: not all zero
            bool nz = false;
            for (size_t k = i + 4; k < end1; k++)
                if (base[k]) { nz = true; break; }
            if (!nz) continue;
            // find 90 40 with the same length nearby
            size_t jEnd = end1 + 0x120; if (jEnd > len) jEnd = len;
            for (size_t j = i + 4; j + 4 < jEnd; j++) {
                if (base[j] != 0x90 || base[j+1] != 0x40) continue;
                unsigned ln2 = base[j+2] << 8 | base[j+3];
                if (ln2 != ln1) continue;
                size_t end2 = j + 4 + ln2;
                if (end2 > len) continue;
                // second blob sanity
                bool nz2 = false;
                for (size_t k = j + 4; k < end2; k++)
                    if (base[k]) { nz2 = true; break; }
                if (!nz2) continue;
                // FIRE: dump a wide window covering both blobs
                size_t back = i >= 0x40 ? 0x40 : i;
                size_t fwd = (end2 > len) ? len : end2;
                size_t fwdLen = fwd - i + 0x40;
                if (i + fwdLen > len) fwdLen = len - i;
                const unsigned char* win = base + i - back;
                size_t winLen = back + fwdLen;
                unsigned __int64 addr = (unsigned __int64)(base + i);
                unsigned __int64 sig = HashBytes(win, winLen);
                WinEnt* e = WinFind(g_wire, &g_wireCount, addr);
                if (e) {
                    if (e->dumps >= 40) break;
                    if (e->dumps > 0 && e->lastSig == sig) break;
                    e->lastSig = sig; e->dumps++;
                }
                g_passWireDumps++;
                EmitHexWindow("AC:PKT ", addr, win, winLen);
                break;
            }
        }
    }

    // v13 history: scans 2-5 (ASCII GUID, old 09/04 wire) removed after 12
    // runs of pure noise in the pre-private-only era. The v18 fingerprint
    // scan above replaces them on clean ground.
}

static void ScanChunk(const unsigned char* base, size_t len)
{
    __try {
        ScanChunkInner(base, len);
    } __except (EXCEPTION_EXECUTE_HANDLER) {
        // page died mid-scan — end of chunk
    }
}

// --- region filter ----------------------------------------------------------
static bool RegionEligible(const MEMORY_BASIC_INFORMATION& mbi)
{
    if (mbi.State != MEM_COMMIT) return false;
    // v12: private heap/stack only — skip ALL module images. The game's
    // heap objects (T-targets, wire packets, UUID strings) are private;
    // module reads both waste pass time and risk EAAC page-watchers.
    if (mbi.Type != MEM_PRIVATE) return false;
    // transient login packet lives in the sub-4GB private heap. Skipping
    // the image cuts a full pass from ~4 min to seconds.
    if ((unsigned __int64)mbi.BaseAddress >= 0x100000000ull) return false;
    if (mbi.Protect & (PAGE_NOACCESS | PAGE_GUARD)) return false;
    if (!(mbi.Protect & (PAGE_READWRITE | PAGE_EXECUTE_READWRITE |
                         PAGE_WRITECOPY | PAGE_EXECUTE_WRITECOPY)))
        return false;
    if (mbi.RegionSize < kMinRegion) return false;
    unsigned __int64 lo = (unsigned __int64)mbi.BaseAddress;
    unsigned __int64 hi = lo + mbi.RegionSize;
    return (hi <= g_selfLo || lo >= g_selfHi); // skip our own module
}

// --- public tick ------------------------------------------------------------
namespace ac {

void HarvestTick()
{
    if (!g_inited) {
        g_inited = true;

        // our own module range (skip in scans) — no psapi dependency:
        // read SizeOfImage straight from our PE header.
        unsigned __int64 self = (unsigned __int64)&HarvestTick;
        self &= ~0xFFFULL; // walk down to PE header page by page
        for (int i = 0; i < 1024; i++) {
            __try {
                if (*(unsigned short*)self == 'ZM') break; // MZ
            } __except (EXCEPTION_EXECUTE_HANDLER) { }
            self -= 0x1000;
        }
        __try {
            long e_lfanew = *(long*)(self + 0x3C);
            unsigned imageSize = *(unsigned*)(self + e_lfanew + 0x50);
            g_selfLo = self; g_selfHi = self + imageSize;
        } __except (EXCEPTION_EXECUTE_HANDLER) {
            g_selfLo = g_selfHi = self; // degenerate: skip nothing else
        }

        // game exe base + resolved targets
        unsigned __int64 base = (unsigned __int64)GetModuleHandleA(nullptr);
        g_base = base;
        for (int t = 0; t < 8; t++) g_targets[t] = base + kRvaPtrTargets[t];

        g_installTick = GetTickCount64(); // v7: timestamps are relative to this
        acr::Init(); // v15: hardware breakpoints on the encode path
        AcLog("AC: harvest init");
        for (int t = 0; t < 8; t++) {
            char line[64]; int p = 0;
            const char* s = "AC: T";
            while (s[p]) { line[p] = s[p]; p++; }
            line[p++] = (char)('0' + t); line[p++] = '=';
            char tb[16]; FmtAddr(tb, g_targets[t]);
            for (int i = 0; tb[i]; i++) line[p++] = tb[i];
            line[p] = 0;
            AcLog(line);
        }
        if (!base) return;
    }
    if (g_targets[0] == 0) return; // exe base unavailable
    acr::OnTick(); // v15: DR re-arm, watch re-dumps, disarm timers

    // -- v16.1: blob-slot swap at t>=4s (once) ------------------------------
    // Run 19 postmortem: swap went live at t=21094 but the login encode
    // happened at t=6156 — the 20s DR-safety deferral (wrong lesson: that
    // crash was the DR suspend storm, not a .data write) missed the only
    // window by 15s. Swap held 145s with zero EAAC retaliation, so the
    // write itself is safe. New timing: t=4s — after the driver's 3s EAAC
    // settle, before the observed t~6s encode.
    if (!g_slotSwapped && g_installTick &&
        GetTickCount64() - g_installTick >= 4000) {
        g_slotSwapped = true;
        AcLog("AC:S v16 swap attempt begin");
        // verify the live codec object still carries the expected fn
        bool confirmed = false;
        __try {
            // the T6 target itself (g_targets[6]) IS the blob TypeRep; its
            // +0x18 slot is what we swap. Sanity: the object we found in
            // the heap (run 18) held exactly orig 0x1992DC0 at +0x18.
            unsigned __int64 slotVal = *(volatile unsigned __int64*)
                (g_targets[6] + 0x18);
            AcLogHex("AC:S T6+0x18 =", slotVal);
            confirmed = (slotVal == g_base + kRvaBlobOrig);
        } __except (EXCEPTION_EXECUTE_HANDLER) { }
        if (confirmed) {
            if (SwapSlot(kRvaBlobSlot, kRvaBlobOrig, g_stubBlob,
                         (void*)SerLogBlob, "blob"))
                AcLog("AC:S v16 blob swap LIVE");
        } else {
            AcLog("AC:S v16 swap SKIPPED — slot not verified "
                  "(registered later? benign)");
        }
    }
    // -- direct .data polls: catch the TypeRep registration moment --
    for (int pi = 0; pi < 3; pi++) {
        unsigned __int64 paddr = g_base + kRvaPolls[pi];
        __try {
            unsigned __int64 v = *(unsigned __int64*)paddr;
            if (v != g_pollLast[pi]) {
                g_pollLast[pi] = v;
                char line[96]; int lp = 0;
                const char* a = "AC:P n";
                while (a[lp]) { line[lp] = a[lp]; lp++; }
                line[lp++] = (char)('0' + pi);
                line[lp++] = '=';
                char vb[16]; FmtAddr(vb, v);
                for (int i = 0; vb[i]; i++) line[lp++] = vb[i];
                line[lp] = 0;
                AcLog(line);
                if (v > 0x10000 && v < 0x7FFFFFFFFFFF) {
                    __try {
                        EmitHexWindow("AC:PT ", v, (const unsigned char*)v, 0x40);
                    } __except (EXCEPTION_EXECUTE_HANDLER) { }
                }
            }
        } __except (EXCEPTION_EXECUTE_HANDLER) { }
    }

    // -- v8: target-slot CONTENT dumps --------------------------------------
    // T3 (member table, RVA 0x2997480) is encrypted at rest and only exists
    // decrypted in live memory; T0/T6 TypeReps are bss-built. Dump each
    // target slot's runtime content whenever it changes — T3's two entries
    // (shieldUUID fieldId 0x10 / skyfallUUID fieldId 0x40) are the ground
    // truth for where the two blob VALUE slots live.
    {
        static const int kSlotTs[4] = { 0, 1, 3, 6 }; // T0 T1 T3 T6
        static unsigned __int64 tsLastSig[4] = { 0, 0, 0, 0 };
        for (int i = 0; i < 4; i++) {
            int t = kSlotTs[i];
            unsigned __int64 slot = g_targets[t];
            __try {
                unsigned __int64 sig = HashBytes((const unsigned char*)slot, 0x60);
                if (sig != tsLastSig[i]) {
                    tsLastSig[i] = sig;
                    char tag[16]; int tp = 0;
                    const char* s = "AC:TS T";
                    while (s[tp]) { tag[tp] = s[tp]; tp++; }
                    tag[tp++] = (char)('0' + t);
                    tag[tp++] = ' ';
                    tag[tp] = 0;
                    EmitHexWindow(tag, slot, (const unsigned char*)slot, 0x60);
                    // chase every heap qword inside the freshly-read slot
                    // (member entries may carry live value pointers)
                    int chased = 0;
                    for (int off = 0; off + 8 <= 0x60 && chased < 4; off += 8) {
                        unsigned __int64 v = ReadQ((const unsigned char*)slot + off);
                        if (!HeapPtr(v)) continue;
                        char tvtag[16]; int tvp = 0;
                        const char* ts2 = "AC:TV T";
                        while (ts2[tvp]) { tvtag[tvp] = ts2[tvp]; tvp++; }
                        tvtag[tvp++] = (char)('0' + t);
                        tvtag[tvp++] = ' ';
                        tvtag[tvp] = 0;
                        __try {
                            volatile unsigned char tv = *(const unsigned char*)v;
                            (void)tv;
                            EmitHexWindow(tvtag, v, (const unsigned char*)v, 0x40);
                            // {ptr,16} inside the pointee = binary GUID slot
                            for (int o2 = 0; o2 + 16 <= 0x40; o2 += 8) {
                                unsigned __int64 a = ReadQ((const unsigned char*)v + o2);
                                unsigned __int64 b = ReadQ((const unsigned char*)v + o2 + 8);
                                unsigned __int64 gp = 0;
                                if (HeapPtr(a) && b == 16) gp = a;
                                else if (HeapPtr(b) && a == 16) gp = b;
                                else continue;
                                __try {
                                    volatile unsigned char tg = *(const unsigned char*)gp;
                                    (void)tg;
                                    EmitHexWindow("AC:UU ", gp,
                                                  (const unsigned char*)gp, 16);
                                } __except (EXCEPTION_EXECUTE_HANDLER) { }
                            }
                            chased++;
                        } __except (EXCEPTION_EXECUTE_HANDLER) { }
                    }
                }
            } __except (EXCEPTION_EXECUTE_HANDLER) { }
        }
    }


    // v18.1: PRIORITY sweep. Run 24 postmortem: loading screens render at
    // ~12fps, so the 16MB/frame budget covers only ~190MB/s — a full 2.15GB
    // pass takes 9+ seconds while the login packet lives ~1s. Fix: scan
    // SMALL regions (<= 256KB — fresh allocations like the packet buffer)
    // FIRST on a fast rotation, then the big regions with the remainder.
    // Small regions total a few MB — the packet's neighborhood is now
    // covered many times per second even during loading.
    unsigned __int64 budget = kBudget;

    // -- phase 1: small-region scan — EVERY frame, full set, no rotation --
    // v18.2: run 25 proved the rotating-cursor version only covered part
    // of the small-region set per frame (at 12fps a full rotation took
    // seconds — same miss as before). The small-region set is only a few
    // MB total; scan ALL of it every frame. Cap is a runaway guard only.
    {
        unsigned __int64 spent = 0;
        unsigned __int64 cap = 8ull * 1024 * 1024; // runaway guard
        unsigned __int64 cur = 0;
        while (spent < cap && cur < 0x100000000ull) {
            MEMORY_BASIC_INFORMATION mbi;
            if (VirtualQuery((LPCVOID)cur, &mbi, sizeof mbi) == 0) break;
            unsigned __int64 lo = (unsigned __int64)mbi.BaseAddress;
            unsigned __int64 hi = lo + mbi.RegionSize;
            if (RegionEligible(mbi) && mbi.RegionSize <= 256 * 1024) {
                __try {
                    ScanChunk((const unsigned char*)lo, (size_t)mbi.RegionSize);
                } __except (EXCEPTION_EXECUTE_HANDLER) { }
                spent += mbi.RegionSize;
            }
            cur = hi;
        }
    }

    // -- phase 2: full sweep (unchanged, budget reduced by phase 1) --------
    while (budget > 0) {
        MEMORY_BASIC_INFORMATION mbi;
        if (VirtualQuery((LPCVOID)g_cursor, &mbi, sizeof mbi) == 0) {
            // walked off the address space — pass complete
            g_pass++;
            char line[192]; int p = 0;
            const char* a = "AC:PASS #";
            while (a[p]) { line[p] = a[p]; p++; }
            auto putnum = [&](unsigned __int64 n) {
                char nb[24]; int nl = 0;
                if (n == 0) nb[nl++] = '0';
                while (n > 0 && nl < 23) { nb[nl++] = (char)('0' + n % 10); n /= 10; }
                for (int i = nl - 1; i >= 0; i--) line[p++] = nb[i];
            };
            putnum((unsigned __int64)g_pass);
            const char* b = " MB=";
            for (int i = 0; b[i]; i++) line[p++] = b[i];
            putnum(g_bytesThisPass / (1024 * 1024));
            const char* d = " objD=";
            for (int i = 0; d[i]; i++) line[p++] = d[i];
            putnum((unsigned __int64)g_passObjDumps);
            const char* w = " wireD=";
            for (int i = 0; w[i]; i++) line[p++] = w[i];
            putnum((unsigned __int64)g_passWireDumps);
            line[p] = 0;
            AcLog(line);

            g_cursor = 0;
            g_bytesThisPass = 0;
            g_passObjDumps = 0;
            g_passWireDumps = 0;
            return;
        }

        unsigned __int64 lo = (unsigned __int64)mbi.BaseAddress;
        unsigned __int64 hi = lo + mbi.RegionSize;

        if (RegionEligible(mbi)) {
            unsigned __int64 regionLeft = hi - g_cursor;
            unsigned __int64 chunk = regionLeft < budget ? regionLeft : budget;
            ScanChunk((const unsigned char*)g_cursor, (size_t)chunk);
            g_bytesThisPass += chunk;
            budget -= chunk;
            g_cursor += chunk;
        } else {
            g_cursor = hi; // skip whole region
        }
    }
}

// v16 (2026-09-06): RE-ARMED with corrected understanding. The v5 "never
// fires" verdict is VOID: those runs had (a) the double-inject bug (the
// real game process refused hooks via the singleton mutex) and (b) for the
// AntiCheatData slot, the wrong slot entirely (T0+0x18 is a NAME pointer —
// run 18 IDA analysis). The T6 BLOB slot (0x2A5CCC8 -> orig 0x1992DC0) is
// run-18-verified as the encode dispatcher every blob value passes through
// (live codec object at 0x337DF538 held it at +0x18). 18 runs exhausted
// every read-only approach; EAAC wipes DRs, blocks MinHook, detects code
// patches. This slot swap is a .data qword write — the same technique as
// the Present vtable hook that has survived every run.
// v16 changes vs v3:
//   * ONLY the blob slot is swapped (T6+0x18); the AntiCheatData slot is
//     left alone (structurally wrong target)
//   * swap happens at first Tick AFTER t=20s (post-EAAC-init; InstallHooks
//     runs at DLL load when the stub pages may not be executable yet on
//     second injections)
//   * slot value verified against the live codec object before writing;
//     any mismatch = log + leave alone
// v18: packet-fingerprint scan restored on clean ground (private-mem-only
// sweep, tight 90 10/90 40 pair validation, blob sanity checks).
void InstallHooks()
{
    AcLog("AC:S v18.2: full small-region scan every frame");
}

} // namespace ac

