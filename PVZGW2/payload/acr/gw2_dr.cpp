// ---------------------------------------------------------------------------
// gw2_dr.cpp — see gw2_dr.h for design notes.
// ---------------------------------------------------------------------------
#include <windows.h>
#include <tlhelp32.h>
#include <cstdio>
#include <cstdint>
#include "gw2_dr.h"

namespace acr {

// --- config -----------------------------------------------------------------
static const char* kDrLogPath =
    "C:\\ProgramData\\Microsoft\\DeviceSync\\ac_dr.txt";

// v15.2: LIVE discovery instead of guessed RVAs. The v15.1 run proved the
// four IDA-derived targets wrong (or uncalled): 117s alive, login-build
// confirmed (T6 codec object at t=1578), zero hits. Worse, three of the
// four were ALREADY disproven by the v3/v4 slot-swap runs ("zero calls" —
// those functions are not on the login path).
//
// New scheme:
//   DR0: execution BP on 0xB5A460 (last remaining IDA guess, login builder)
//   DR1-3: dynamic 8-byte WRITE watchpoints on the heap codec object that
//          the passive sweep discovers (T6/T1 target hits). Its content
//          demonstrably changes during login (sig changes force re-dumps),
 //         so a write watchpoint is guaranteed fire material.
// On every hit: registers + object window + RAW STACK DUMP. The stack
// contains return addresses of the live call chain — the REAL encode path,
// verified at runtime, replacing every IDA guess. Also validates the DR
// mechanism itself: if these known-written addresses never fire, EAAC is
// wiping debug registers and usermode hardware BPs are dead (pivot signal).
static const uint64_t kRvaExecBp0 = 0xB5A460; // DR0 login builder (unverified)

static const int kRearmEveryMs   = 10000;  // re-arm pass cadence while young
static const int kArmAtMs        = 20000;  // v15.1: first arm — after EAAC init, before login
static const int kRearmUntilMs   = 120000; // late-spawned threads coverage
static const int kDisarmAtMs     = 300000; // hard disarm (login long over)
static const int kMaxHits        = 100;    // total hit cap -> disarm

// --- state -------------------------------------------------------------------
static uint64_t g_base = 0;
static uint64_t g_bpExec = 0;              // DR0 target
static uint64_t g_objAddr = 0;             // discovered codec object (watched)
static volatile LONG g_objAddrSeq = 0;     // bump on new object -> re-arm soon
static volatile LONG g_hits = 0;
static volatile LONG g_writeHits = 0;
static volatile LONG g_captured = 0;
static volatile LONG g_disarmed = 0;
static volatile LONG g_vehBusy = 0;
static uint64_t g_initTick = 0;
static uint64_t g_lastArmPass = 0;
static uint64_t g_initMs = 0;

// watch list — pointers pulled from hit contexts; re-dumped on content change
struct WatchEnt { uint64_t addr; uint64_t lastSig; int dumps; };
static WatchEnt g_watch[24];
static volatile LONG g_watchCount = 0;

// --- logging -----------------------------------------------------------------
static void DrLog(const char* msg)
{
    FILE* f = nullptr;
    fopen_s(&f, kDrLogPath, "a");
    if (!f) return;
    fputs(msg, f);
    fputc('\n', f);
    fclose(f);
}

static void DrLogLine(const char* tag, uint64_t a, uint64_t b, uint64_t c)
{
    char line[160];
    int p = 0;
    while (tag[p] && p < 100) { line[p] = tag[p]; p++; }
    line[p++] = ' ';
    _snprintf_s(line + p, sizeof(line) - p, _TRUNCATE, "%llX %llX %llX",
                (unsigned long long)a, (unsigned long long)b,
                (unsigned long long)c);
    DrLog(line);
}

static const char kHexD[17] = "0123456789ABCDEF";

static void FmtAddr(char* out, uint64_t v)
{
    out[0] = '0'; out[1] = 'x';
    for (int i = 0; i < 12; i++)
        out[2 + i] = kHexD[(v >> ((11 - i) * 4)) & 0xF];
    out[14] = 0;
}

// dump a memory window to the log (SEH-guarded). Returns bytes dumped.
static int DumpWin(const char* tag, uint64_t addr, size_t len)
{
    if (addr <= 0x10000 || addr >= 0x7FFFFFFFFFFFull) return 0;
    unsigned char buf[0x60];
    size_t n = len < sizeof(buf) ? len : sizeof(buf);
    __try {
        volatile unsigned char t = *(volatile unsigned char*)addr; (void)t;
        for (size_t i = 0; i < n; i++) buf[i] = ((unsigned char*)addr)[i];
    } __except (EXCEPTION_EXECUTE_HANDLER) { return 0; }

    // capture detection: the AntiCheatData TDF framing
    for (size_t i = 0; i + 2 < n; i++) {
        if (buf[i] == 0x90 && buf[i + 1] == 0x10) {
            for (size_t j = i + 2; j + 1 < n && j < i + 0x120; j++) {
                if (buf[j] == 0x90 && buf[j + 1] == 0x40) {
                    InterlockedExchange(&g_captured, 1);
                    break;
                }
            }
        }
        if (buf[i] == 0xB0 && buf[i + 1] == 0x60) {
            for (size_t j = i + 2; j + 1 < n && j < i + 0x40; j++) {
                if (buf[j] == 0x90 && buf[j + 1] == 0x10) {
                    InterlockedExchange(&g_captured, 1);
                    break;
                }
            }
        }
    }

    char line[928]; int p = 0;
    while (tag[p]) { line[p] = tag[p]; p++; }
    char ab[16]; FmtAddr(ab, addr);
    for (int i = 0; ab[i]; i++) line[p++] = ab[i];
    line[p++] = ' '; line[p++] = 't'; line[p++] = '=';
    if (g_initTick) {
        unsigned long long ms = GetTickCount64() - g_initTick;
        char nb[24]; int nl = 0;
        if (ms == 0) nb[nl++] = '0';
        while (ms > 0 && nl < 20) { nb[nl++] = (char)('0' + ms % 10); ms /= 10; }
        for (int i = nl - 1; i >= 0; i--) line[p++] = nb[i];
    }
    const char* h = " hex=";
    for (int i = 0; h[i]; i++) line[p++] = h[i];
    for (size_t i = 0; i < n && p < 900; i++) {
        line[p++] = kHexD[buf[i] >> 4];
        line[p++] = kHexD[buf[i] & 0xF];
    }
    line[p] = 0;
    DrLog(line);
    return (int)n;
}

// --- VEH: the breakpoint handler ---------------------------------------------
// Data-write breakpoints trap AFTER the writing instruction: rip = next
// instruction, and Dr6 B-bits name WHICH bp fired. Identify by Dr6, never
// by rip — and NEVER leak our #DBs to other handlers.
static LONG WINAPI DrVeh(EXCEPTION_POINTERS* ep)
{
    if (!ep || !ep->ExceptionRecord || !ep->ContextRecord) return EXCEPTION_CONTINUE_SEARCH;
    if (ep->ExceptionRecord->ExceptionCode != (DWORD)0x80000004 /*STATUS_SINGLE_STEP*/)
        return EXCEPTION_CONTINUE_SEARCH;

    CONTEXT* ctx = ep->ContextRecord;
    uint64_t dr6 = ctx->Dr6;
    uint64_t bbits = dr6 & 0xF;
    if (!bbits) return EXCEPTION_CONTINUE_SEARCH; // not a DR hit (TF etc.)

    bool isExec = (dr6 & 1) && ctx->Rip == g_bpExec;  // B0 + rip match
    bool isWrite = (dr6 & 0xE) != 0 && g_objAddr != 0; // B1/B2/B3
    if (!isExec && !isWrite) {
        // our DR0 fired but rip mismatched, or stale bits: swallow quietly
        ctx->Dr6 = 0;
        ctx->EFlags |= 0x10000;
        return EXCEPTION_CONTINUE_EXECUTION;
    }

    ctx->Dr6 = 0;
    ctx->EFlags |= 0x10000; // RF: no re-trigger on this instruction

    if (InterlockedIncrement(&g_hits) > kMaxHits) {
        InterlockedExchange(&g_disarmed, 1);
    }
    if (g_disarmed || InterlockedCompareExchange(&g_vehBusy, 1, 0) != 0)
        return EXCEPTION_CONTINUE_EXECUTION;

    __try {
        char line[320]; int p = 0;
        if (isExec) {
            const char* s = "DR: EXEC HIT bp0";
            while (s[p]) { line[p] = s[p]; p++; }
        } else {
            const char* s = "DR: WRITE HIT bp";
            while (s[p]) { line[p] = s[p]; p++; }
            int bp = (dr6 & 2) ? 1 : ((dr6 & 4) ? 2 : 3);
            line[p++] = (char)('0' + bp);
            InterlockedIncrement(&g_writeHits);
        }
        line[p++] = ' ';
        _snprintf_s(line + p, sizeof(line) - p, _TRUNCATE,
            "rip=%llX RVA=%llX rcx=%llX rdx=%llX r8=%llX r9=%llX rsp=%llX hits=%d",
            (unsigned long long)ctx->Rip,
            (unsigned long long)(ctx->Rip - g_base),
            (unsigned long long)ctx->Rcx, (unsigned long long)ctx->Rdx,
            (unsigned long long)ctx->R8, (unsigned long long)ctx->R9,
            (unsigned long long)ctx->Rsp, g_hits);
        DrLog(line);

        if (isExec) {
            // execution hit on the login builder: dump its arg buffers
            DumpWin("DR:A rcx ", ctx->Rcx, 0x60);
            DumpWin("DR:A rdx ", ctx->Rdx, 0x60);
            DumpWin("DR:A r8  ", ctx->R8, 0x60);
            DumpWin("DR:A r9  ", ctx->R9, 0x60);
        } else {
            // write hit on the codec object: dump the whole object window
            DumpWin("DR:O obj ", g_objAddr - 0x40, 0xC0);
        }

        // RAW STACK DUMP — return addresses of the live call chain.
        // Offline: filter qwords in [g_base, g_base+0x4000000) = the REAL
        // runtime call path (replaces every IDA-guessed target).
        DumpWin("DR:S stack ", ctx->Rsp, 0x180);

        // seed the per-frame watch list from registers + stack args
        const uint64_t seeds[4] = { ctx->Rcx, ctx->Rdx, ctx->R8, ctx->R9 };
        for (int i = 0; i < 4; i++) {
            uint64_t s2 = seeds[i];
            if (s2 <= 0x10000 || s2 >= 0x7FFFFFFFFFFFull) continue;
            LONG n = g_watchCount;
            if (n >= 24) break;
            bool known = false;
            for (LONG j = 0; j < n; j++)
                if (g_watch[j].addr == s2) { known = true; break; }
            if (known) continue;
            g_watch[n].addr = s2; g_watch[n].lastSig = 0; g_watch[n].dumps = 0;
            g_watchCount = n + 1;
        }
    } __except (EXCEPTION_EXECUTE_HANDLER) { }

    InterlockedExchange(&g_vehBusy, 0);

    if (g_captured) {
        DrLogLine("DR: CAPTURE CONFIRMED (90 10 / 90 40 pair seen) — disarming",
                  g_captured, 0, 0);
        InterlockedExchange(&g_disarmed, 1);
    }
    return EXCEPTION_CONTINUE_EXECUTION;
}

// --- thread arming -------------------------------------------------------------
// DR0: execution BP (RW=00 LEN=00). DR1-3: 8-byte WRITE watchpoints on the
// discovered codec object (RW=01 LEN=10 -> x64 8-byte). DR7 fields per BP:
// RW bits (16+4n), LEN bits (18+4n); local enables 0x55.
//   exec-only:  0x55
//   +3 writes:  0x55 | 0x900000 | 0x9000000 | 0x90000000 = 0x99900055
static void SetDrsOnThread(DWORD tid, bool arm)
{
    HANDLE h = OpenThread(THREAD_SUSPEND_RESUME | THREAD_GET_CONTEXT |
                          THREAD_SET_CONTEXT | THREAD_QUERY_INFORMATION,
                          FALSE, tid);
    if (!h) return;
    if (SuspendThread(h) != (DWORD)-1) {
        CONTEXT ctx = {};
        ctx.ContextFlags = CONTEXT_DEBUG_REGISTERS;
        if (GetThreadContext(h, &ctx)) {
            if (arm) {
                uint64_t obj = g_objAddr;
                ctx.Dr0 = g_bpExec;
                ctx.Dr1 = obj ? obj - 0x40 : 0;
                ctx.Dr2 = obj ? obj : 0;
                ctx.Dr3 = obj ? obj + 0x40 : 0;
                ctx.Dr6 = 0;
                ctx.Dr7 = obj ? (uint64_t)0x99900055 : (uint64_t)0x55;
            } else {
                ctx.Dr0 = 0; ctx.Dr1 = 0; ctx.Dr2 = 0; ctx.Dr3 = 0;
                ctx.Dr6 = 0; ctx.Dr7 = 0;
            }
            SetThreadContext(h, &ctx);
        }
        ResumeThread(h);
    }
    CloseHandle(h);
}

static int ArmPass(bool arm)
{
    int n = 0;
    HANDLE snap = CreateToolhelp32Snapshot(TH32CS_SNAPTHREAD, 0);
    if (snap == INVALID_HANDLE_VALUE) return 0;
    THREADENTRY32 te = {};
    te.dwSize = sizeof(te);
    DWORD pid = GetCurrentProcessId();
    DWORD self = GetCurrentThreadId();
    if (Thread32First(snap, &te)) {
        do {
            if (te.th32OwnerProcessID != pid) continue;
            if (te.th32ThreadID == self) continue;
            SetDrsOnThread(te.th32ThreadID, arm);
            n++;
        } while (Thread32Next(snap, &te));
    }
    CloseHandle(snap);
    return n;
}

// --- public API -----------------------------------------------------------------
void Init()
{
    g_initTick = GetTickCount64();
    g_initMs = g_initTick;
    g_base = (uint64_t)GetModuleHandleA(nullptr);
    g_bpExec = g_base + kRvaExecBp0;

    char line[128]; int p = 0;
    const char* s = "DR: init base=";
    while (s[p]) { line[p] = s[p]; p++; }
    char ab[16]; FmtAddr(ab, g_base);
    for (int i = 0; ab[i]; i++) line[p++] = ab[i];
    line[p] = 0;
    DrLog(line);
    DrLogLine("DR: exec bp0", g_bpExec, 0, 0);

    // v15.2 CRITICAL: register the #DB handler BEFORE anything arms a
    // breakpoint. (A v15.2 build accidentally shipped without this — the
    // linker stripped the orphaned DrVeh, and the first watchpoint hit
    // would have crashed the game with no handler. Verified via binary
    // string audit: every string inside DrVeh was missing from the DLL.)
    AddVectoredExceptionHandler(1, DrVeh);
    DrLogLine("DR: VEH registered", (uint64_t)&DrVeh, 0, 0);
    DrLogLine("DR: v15.2 watchpoints armed on codec-object discovery", 0, 0, 0);

    // v15.1: defer first arm to t=20s (post-EAAC-init, pre-login).
    g_lastArmPass = GetTickCount64();
}

// v15.2: called by the passive sweep when it finds a codec object in the
// heap (T6/T1 target hits). Sets the write-watchpoint target and forces a
// re-arm pass within the next tick — no 10s wait.
void WatchObject(uint64_t heapAddr)
{
    if (!g_initMs || g_disarmed) return;
    if (heapAddr <= 0x10000 || heapAddr >= 0x100000000ull) return;
    if (g_objAddr == heapAddr) return;
    g_objAddr = heapAddr;
    InterlockedIncrement(&g_objAddrSeq);
    DrLogLine("DR: watch object set", heapAddr, 0, 0);
    // invalidate the re-arm throttle so OnTick arms on the next tick
    g_lastArmPass = 0;
}

void OnTick()
{
    if (!g_initMs) return;
    uint64_t now = GetTickCount64();
    uint64_t t = now - g_initMs;

    // hard disarm timer
    if (!g_disarmed && t > (uint64_t)kDisarmAtMs) {
        InterlockedExchange(&g_disarmed, 1);
        DrLogLine("DR: auto-disarm at t=300s", 0, 0, 0);
    }
    if (g_disarmed) {
        // one final clear pass, then idle forever
        static bool cleared = false;
        if (!cleared) {
            cleared = true;
            ArmPass(false);
            DrLogLine("DR: all DRs cleared — module idle", 0, 0, 0);
        }
        return;
    }

    // v15.1: heartbeat — timestamps the exact death moment in the log
    static uint64_t lastBeat = 0;
    if (now - lastBeat >= 5000) {
        lastBeat = now;
        DrLogLine("DR: alive t(ms)=", t, (uint64_t)(LONG)g_hits, 0);
    }


    // v15.1: first arm at t>=20s, re-arm every 10s until 120s (late
    // threads). BEGIN/END markers: log stopping BETWEEN them = death
    // during the pass (suspend collision); stopping seconds after a
    // completed pass = EAAC polls debug registers. WatchObject forces
    // immediate re-arm (g_lastArmPass=0) but never before t=20s.
    if (t >= (uint64_t)kArmAtMs && t < (uint64_t)kRearmUntilMs &&
        (g_lastArmPass == 0 || now - g_lastArmPass >= (uint64_t)kRearmEveryMs)) {
        DrLogLine("DR: arm pass BEGIN t(ms)=", t, g_objAddr, 0);
        int n = ArmPass(true);
        DrLogLine("DR: arm pass threads=", (uint64_t)n, 0, 0);
        g_lastArmPass = now;
    }

    // watch re-dump: every frame, dump watched pointers whose content changed
    for (LONG i = 0; i < g_watchCount; i++) {
        WatchEnt* e = &g_watch[i];
        if (!e->addr || e->dumps >= 24) continue;
        uint64_t sig = 0;
        __try {
            volatile unsigned char t2 = *(volatile unsigned char*)e->addr; (void)t2;
            uint64_t h = 1469598103934665603ull;
            for (int b = 0; b < 0x60; b++) {
                h ^= ((unsigned char*)e->addr)[b];
                h *= 1099511628211ull;
            }
            sig = h;
        } __except (EXCEPTION_EXECUTE_HANDLER) { e->addr = 0; continue; }
        if (e->lastSig == sig) continue;
        e->lastSig = sig;
        e->dumps++;
        DumpWin("DR:W ", e->addr, 0x60);
    }

    if (g_captured && !g_disarmed) {
        InterlockedExchange(&g_disarmed, 1);
        DrLogLine("DR: CAPTURE — disarming", g_captured, 0, 0);
    }
}

} // namespace acr
