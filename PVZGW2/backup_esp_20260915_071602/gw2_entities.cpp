// ---------------------------------------------------------------------------
// gw2_entities.cpp — implementation of the live entity layer.
// Threads: BeginCollect/Add* run on the F2 scan thread; OnFrame/DebugList/
// DumpLocal run on the Present thread (always the same, sequential) — the
// entity table itself is guarded by one CRITICAL_SECTION against the scan
// thread. All game-memory reads go through NtReadVirtualMemory (kernel-side
// copy: a freed/unmapped page returns an error instead of raising).
// File I/O is strictly NOT per-frame: the container watcher samples ~1.3 Hz
// and only writes when bytes actually change (buffered, flushed per sample).
// ---------------------------------------------------------------------------
#include "gw2_entities.h"
#include "gw2_viewproj.h"
#include <windows.h>

namespace ent {

// ---- verified RVAs (see header) ---------------------------------------------
static constexpr uint64_t RVA_CHAROBJ_VT   = 0x228B380;
static constexpr uint64_t RVA_CHAROBJ_TI   = 0x228B658;
static constexpr uint32_t CHAROBJ_MARKER   = 0x100F;
static constexpr uint64_t RVA_TEAMREG_VT   = 0x228BC58;
static constexpr uint64_t RVA_CONTAINER_VT = 0x236B638;

static constexpr uintptr_t CO_MARKER    = 0x18;    // u32
static constexpr uintptr_t CO_E1        = 0x20;    // qword -> entity (E1)
static constexpr uintptr_t CO_CLASSBASE = 0x28;    // qword -> property/classbase node
static constexpr uintptr_t CO_TI        = 0x48;    // qword -> TypeInfo
static constexpr uintptr_t CO_PROXY     = 0x50;    // qword -> hknpCharacterProxy
static constexpr uintptr_t CO_STATE     = 0x58;    // qword -> state
static constexpr uintptr_t CO_COMPS     = 0x68;    // qword -> component array
static constexpr uintptr_t CO_MAXHP     = 0x8c;    // float  MaxHealth (100/155/180/1000)
static constexpr uintptr_t CB_NODE      = 0x30;    // classbase+0x30 -> node
static constexpr uintptr_t NODE_STR     = 0x10;    // node+0x10     -> class string ptr
// class string: "Gameplay/Soldiers/PhysicsData/<Class>Physics", ASCII.
// [30] = 'Z' (zombie side) / 'P' (plant side); name containing "_AI" or
// "Turret" = NPC (verified dump7: Zombie_AIBrowncoat_Physics,
// Plant_AIWeed_Physics vs player Zombie_AssaultPhysics etc).
static constexpr int  FAC_SIDE_CHAR = 30;
static constexpr int  FAC_STR_LEN   = 56;
static constexpr uintptr_t ST_OBJ120 = 0x120;   // qword -> per-char object (F5 dump)
static constexpr uintptr_t PX_POS    = 0x70;    // float[3] in the proxy
// torso lift for the body-column hit test (triggerbot): the proxy origin
// sits at the FEET while the game reticle aims at the body. GW2 characters
// are ~1.2-1.8 m tall; 1.2 m covers chest-to-head for most classes.
static constexpr float    ENT_LIFT  = 1.2f;


static constexpr int    MAX_ENT    = 160;
static constexpr int    MAX_TEAM   = 4;
static constexpr int    MAX_CONT   = 256;
static constexpr size_t CONT_SNAP  = 0x150;     // bytes watched per container
struct Entity {
    uint64_t charObj, proxy, state;
    float    x, y, z;
    int      team;          // 0 plants, 1 zombies, -1 unknown
    bool     ai;            // NPC (class name carries _AI / Turret)
    uint32_t fails;         // consecutive read failures (dead slot)
    uint32_t lastMove;      // frame tick of the last position change (liveness)
};
// Heap arenas can land above 4 GiB depending on machine VA layout
// (cec(2) dump7: entity arena at 0x10c-0x10f..; dev machine below 4 GiB).
// The strong gates are the exact vtable/TypeInfo match and the marker —
// plausibility only rejects garbage.
static bool Plausible(uint64_t va)
{
    return va >= 0x10000ull && va < 0x0000800000000000ull;
}

struct Container {
    uint64_t va;
    bool     hasSnap;
    uint8_t  snap[CONT_SNAP];
    uint32_t changes;
};


static Entity    g_ent[MAX_ENT];
static int       g_nEnt = 0;
static uint64_t  g_team[MAX_TEAM];
static int       g_nTeam = 0;
static Container g_cont[MAX_CONT];
static int       g_nCont = 0;

static uint64_t g_vaCharObj = 0, g_vaTeamReg = 0, g_vaContainer = 0, g_vaTI = 0;

static CRITICAL_SECTION g_cs;
static bool             g_csInit = false;

// Present-thread-only state (OnFrame/DebugList/StatusText run sequentially
// on the Present hook thread — no lock needed among them)
static int  g_localIdx = -1;
static char g_status[160];

// NtReadVirtualMemory resolved from ntdll at BeginCollect (kernel-side read:
// a freed/unmapped page returns an error instead of raising).
typedef LONG NTSTATUS;
typedef NTSTATUS(NTAPI* pfn_NtRVM)(HANDLE, PVOID, PVOID, SIZE_T, PSIZE_T);
static pfn_NtRVM g_NtRVM = nullptr;

static bool Rd(uint64_t va, void* dst, size_t n)
{
    if (!g_NtRVM) return false;
    SIZE_T got = 0;
    return g_NtRVM((HANDLE)-1, (PVOID)va, dst, (SIZE_T)n, &got) == 0 && got == (SIZE_T)n;
}

// Side + NPC flag from the class string. Returns false if the chain breaks
// (caller leaves team -1 = yellow, retries on the round-robin).
static bool ReadFaction(uint64_t charObj, int* teamOut, bool* aiOut)
{
    uint64_t cb = 0, node = 0, strp = 0;
    if (!Rd(charObj + CO_CLASSBASE, &cb, 8) || !Plausible(cb)) return false;
    if (!Rd(cb + CB_NODE, &node, 8) || !Plausible(node)) return false;
    if (!Rd(node + NODE_STR, &strp, 8) || !Plausible(strp)) return false;
    char s[FAC_STR_LEN];
    if (!Rd(strp, s, FAC_STR_LEN)) return false;
    s[FAC_STR_LEN - 1] = 0;
    char c = s[FAC_SIDE_CHAR];
    int team = (c == 'Z') ? 1 : (c == 'P') ? 0 : -1;
    if (team < 0) return false;
    bool ai = false;
    for (int i = 0; i + 3 < FAC_STR_LEN; i++) {
        if (s[i] == '_' && s[i+1] == 'A' && s[i+2] == 'I') { ai = true; break; }
        if (s[i] == 'T' && s[i+1] == 'u' && s[i+2] == 'r') { ai = true; break; }
    }
    *teamOut = team; *aiOut = ai;
    return true;
}

static size_t WHex(char* o, uint64_t v)
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
static size_t WHex32(char* o, uint32_t v)
{
    static const char d[] = "0123456789ABCDEF";
    o[0] = '0'; o[1] = 'x';
    size_t k = 2;
    for (int sh = 28; sh >= 0; sh -= 4) o[k++] = d[(v >> sh) & 0xF];
    return k;
}
static size_t WStr(char* o, const char* s)
{
    size_t k = 0; while (s[k]) { o[k] = s[k]; k++; } return k;
}
static size_t WU(char* o, unsigned v)
{
    char t[12]; int tn = 0;
    if (v == 0) t[tn++] = '0';
    while (v && tn < 11) { t[tn++] = (char)('0' + v % 10); v /= 10; }
    size_t k = 0;
    while (tn) o[k++] = t[--tn];
    return k;
}

// ---- scan-thread collection --------------------------------------------------
// reset=false (auto-refresh light scans): keep the live table — AddCharObj
// dedups by VA, joiners append, dead rows blank out via the fails>600 path.
void BeginCollect(uint64_t imgBase, bool reset)
{
    if (!g_csInit) { InitializeCriticalSectionAndSpinCount(&g_cs, 4000); g_csInit = true; }
    HMODULE nt = GetModuleHandleA("ntdll.dll");
    if (nt) g_NtRVM = (pfn_NtRVM)GetProcAddress(nt, "NtReadVirtualMemory");

    g_vaCharObj   = imgBase + RVA_CHAROBJ_VT;
    g_vaTeamReg   = imgBase + RVA_TEAMREG_VT;
    g_vaContainer = imgBase + RVA_CONTAINER_VT;
    g_vaTI        = imgBase + RVA_CHAROBJ_TI;

    if (!reset) return;
    EnterCriticalSection(&g_cs);
    g_nEnt = g_nTeam = g_nCont = 0;
    for (int i = 0; i < MAX_CONT; i++) { g_cont[i].va = 0; g_cont[i].hasSnap = false; g_cont[i].changes = 0; }
    LeaveCriticalSection(&g_cs);
}

void AddCharObj(uint64_t va)
{
    // shape validation: marker + TypeInfo + both links plausible.
    // NOTE: the low byte of the +0x18 marker VARIES PER SESSION (0x100F one
    // run, 0x100E the next) - only the high nibble is stable. The strong
    // discriminator is the TypeInfo pointer at +0x48.
    uint32_t marker = 0; uint64_t ti = 0, proxy = 0, state = 0;
    if (!Rd(va + CO_MARKER, &marker, 4) || (marker & 0xFF00) != 0x1000) return;
    if (!Rd(va + CO_TI, &ti, 8) || ti != g_vaTI) return;
    if (!Rd(va + CO_PROXY, &proxy, 8) || !Plausible(proxy)) return;
    if (!Rd(va + CO_STATE, &state, 8) || !Plausible(state)) return;

    EnterCriticalSection(&g_cs);
    for (int i = 0; i < g_nEnt; i++)
        if (g_ent[i].charObj == va) { LeaveCriticalSection(&g_cs); return; }
    if (g_nEnt < MAX_ENT) {
        g_ent[g_nEnt].charObj = va;
        g_ent[g_nEnt].proxy = proxy;
        g_ent[g_nEnt].state = state;
        g_ent[g_nEnt].x = g_ent[g_nEnt].y = g_ent[g_nEnt].z = 0.f;
        g_ent[g_nEnt].team = -1;
        g_ent[g_nEnt].ai = false;
        g_ent[g_nEnt].fails = 0;
        g_ent[g_nEnt].lastMove = 0;   // liveness unknown until first move
        g_nEnt++;
    }
    LeaveCriticalSection(&g_cs);
}

void AddTeamReg(uint64_t va)
{
    EnterCriticalSection(&g_cs);
    for (int i = 0; i < g_nTeam; i++)
        if (g_team[i] == va) { LeaveCriticalSection(&g_cs); return; }
    if (g_nTeam < MAX_TEAM) g_team[g_nTeam++] = va;
    LeaveCriticalSection(&g_cs);
}

void AddContainer(uint64_t va)
{
    EnterCriticalSection(&g_cs);
    for (int i = 0; i < g_nCont; i++)
        if (g_cont[i].va == va) { LeaveCriticalSection(&g_cs); return; }
    if (g_nCont < MAX_CONT) {
        g_cont[g_nCont].va = va;
        g_cont[g_nCont].hasSnap = false;
        g_cont[g_nCont].changes = 0;
        g_nCont++;
    }
    LeaveCriticalSection(&g_cs);
}

void EndCollect() { /* tables stay live for the Present thread */ }

uint64_t CharObjVT()   { return g_vaCharObj; }
uint64_t TeamRegVT()   { return g_vaTeamReg; }
uint64_t ContainerVT() { return g_vaContainer; }

// ---- container watcher --------------------------------------------------------
// Identifies the local player's weapon at runtime: sample every ~0.75 s,
// diff against the last snapshot, log changed dwords. Whichever container
// changes while the local player fires is HIS weapon's runtime state —
// its FireLogic sub-blocks (+0x70/+0x190/+0x220/+0x2B0/+0x340) then become
// the rapid-fire / fire-mode edit targets.
static HANDLE g_wF = INVALID_HANDLE_VALUE;
static uint8_t g_wBuf[16 * 1024];
static size_t  g_wUsed = 0;

static void WatchSample()
{
    if (g_nCont == 0) return;
    if (g_wF == INVALID_HANDLE_VALUE) {
        g_wF = CreateFileA("C:\\Users\\Public\\gw2_wpwatch.txt",
            GENERIC_WRITE, FILE_SHARE_READ, nullptr, CREATE_ALWAYS,
            FILE_ATTRIBUTE_NORMAL, nullptr);
        if (g_wF == INVALID_HANDLE_VALUE) return;
    }
    LONG cap = SetFilePointer(g_wF, 0, nullptr, FILE_CURRENT);
    if (cap < 0 || (uint64_t)cap > 8ull * 1024 * 1024) return;   // size cap

    uint8_t cur[CONT_SNAP];
    EnterCriticalSection(&g_cs);          // scan thread may append meanwhile
    for (int i = 0; i < g_nCont; i++) {
        if (!Rd(g_cont[i].va, cur, CONT_SNAP)) continue;
        if (!g_cont[i].hasSnap) {
            memcpy(g_cont[i].snap, cur, CONT_SNAP);
            g_cont[i].hasSnap = true;
            continue;
        }
        char line[256]; size_t k = WStr(line, "CHG ");
        k += WHex(line + k, g_cont[i].va);
        int diffs = 0;
        for (size_t off = 0; off + 4 <= CONT_SNAP && diffs < 10; off += 4) {
            if (k > sizeof line - 40) break;    // check BEFORE appending
            uint32_t a, b;
            memcpy(&a, g_cont[i].snap + off, 4);
            memcpy(&b, cur + off, 4);
            if (a == b) continue;
            if (diffs == 0) g_cont[i].changes++;
            line[k++] = ' '; k += WHex32(line + k, (uint32_t)off);
            line[k++] = ':'; k += WHex32(line + k, a);
            line[k++] = '>'; k += WHex32(line + k, b);
            diffs++;
        }
        if (diffs > 0) {
            memcpy(g_cont[i].snap, cur, CONT_SNAP);
            if (g_wUsed + k + 2 > sizeof g_wBuf) {
                DWORD w = 0; WriteFile(g_wF, g_wBuf, (DWORD)g_wUsed, &w, nullptr);
                g_wUsed = 0;
            }
            if (g_wUsed + k + 2 <= sizeof g_wBuf) {
                memcpy(g_wBuf + g_wUsed, line, k); g_wUsed += k;
                g_wBuf[g_wUsed++] = '\r'; g_wBuf[g_wUsed++] = '\n';
            }
        }
    }
    LeaveCriticalSection(&g_cs);
    if (g_wUsed) {
        DWORD w = 0; WriteFile(g_wF, g_wBuf, (DWORD)g_wUsed, &w, nullptr);
        g_wUsed = 0;
    }
}

// ---- Present-thread live layer -------------------------------------------------
// (team/ai come from the faction string — see ReadFaction. The teamReg
// registry link at state+0x130 is DEAD: Garden Ops spawns 4 registries and
// player teams landed on index 2/3, past the colored 0/1 — dump7, all
// circles yellow. The faction chain works in Garden Ops AND PvP.)

// present-thread frame counter mirror (OnFrame bumps; DebugList reads for
// the freshness window — both run on the same thread, no lock needed)
static uint32_t g_tickNow = 0;

void OnFrame()
{
    if (!g_csInit) return;

    // snapshot the table (scan thread may append meanwhile)
    Entity snap[MAX_ENT];   // 160*48 = 7.5 KB — Present thread stack is fine
    int n;
    EnterCriticalSection(&g_cs);
    n = g_nEnt;
    for (int i = 0; i < n; i++) snap[i] = g_ent[i];
    LeaveCriticalSection(&g_cs);

    static uint32_t tick = 0;
    tick++;
    g_tickNow = tick;

    for (int i = 0; i < n; i++) {
        float p[3];
        if (Rd(snap[i].proxy + PX_POS, p, 12)) {
            // liveness: mark the frame tick whenever the entity moved
            // (>= 1 cm — live proxies micro-jitter at physics rate; dead
            // slots are bit-frozen). The triggerbot must not shoot at
            // corpses (dump15: frozen rows kept projecting and the bot
            // "fired at nothing").
            float dx = p[0] - snap[i].x, dy = p[1] - snap[i].y, dz = p[2] - snap[i].z;
            if (dx > 0.01f || dx < -0.01f || dy > 0.01f || dy < -0.01f || dz > 0.01f || dz < -0.01f) {
                // SLOT-WAKE (2026-09-09 spawn-color fix): motion starting
                // again after a frozen gap = the slot was recycled (old
                // entity died, new one spawned). The class-string chain
                // still shows the PREVIOUS occupant right now — inheriting
                // it paints the new body in the dead guy's color (plants
                // spawning orange). Reset to UNKNOWN: yellow until the
                // first post-wake faction read lands (1-2 frames), never
                // the wrong color.
                if (snap[i].lastMove && snap[i].lastMove + 12 <= tick &&
                    snap[i].team >= 0) {
                    snap[i].team = -1;
                }
                snap[i].lastMove = tick;
            }
            snap[i].x = p[0]; snap[i].y = p[1]; snap[i].z = p[2];
            snap[i].fails = 0;
        } else if (++snap[i].fails > 600) {
            snap[i].x = snap[i].y = snap[i].z = 0.f;   // dead slot: blank, keep row
        }
        // team/AI refresh: unknown-team entities retry EVERY FRAME until
        // resolved (a fresh spawn resolves within 1-2 frames this way).
        // Known teams re-validate on the round-robin as before.
        if (snap[i].team < 0 || (uint32_t)i == (tick % 128) ||
            (snap[i].team >= 0 && (uint32_t)i == (tick % 32))) {
            int t = -1; bool a = false;
            if (ReadFaction(snap[i].charObj, &t, &a)) {
                snap[i].team = t;
                snap[i].ai = a;
            }
        }
    }

    // local player: nearest PLAYER entity to the captured camera. AI can
    // never be the local player — in Garden Ops browncoats crowd the camera
    // and won the old nearest-any-entity pick (dump16: local=AIBrowncoat,
    // team logic inverted -> triggerbot fired at anything). The 3rd-person
    // camera orbits YOU, so the nearest non-AI entity is you.
    float cam[3] = { 0, 0, 0 };
    g_localIdx = -1;
    if (vproj::GetCamera(cam)) {
        float bestD = 1e30f; int best = -1;
        for (int i = 0; i < n; i++) {
            if (snap[i].ai) continue;                          // never AI
            if (snap[i].team < 0) continue;                    // faction unknown yet
            if (snap[i].x == 0.f && snap[i].y == 0.f && snap[i].z == 0.f) continue;
            float d[3] = { snap[i].x - cam[0], snap[i].y - cam[1], snap[i].z - cam[2] };
            float dd = d[0]*d[0] + d[1]*d[1] + d[2]*d[2];
            if (dd < bestD) { bestD = dd; best = i; }
        }
        g_localIdx = best;
    }


    // watcher sample every ~45 frames (0.75 s at 60 fps)
    static uint32_t wtick = 0;
    if (++wtick >= 45) { wtick = 0; WatchSample(); }

    // publish back (rows 0..n-1 only — appends by the scan thread survive)
    EnterCriticalSection(&g_cs);
    for (int i = 0; i < n; i++) g_ent[i] = snap[i];
    LeaveCriticalSection(&g_cs);
}

int EntityCount()
{
    if (!g_csInit) return 0;
    EnterCriticalSection(&g_cs);
    int n = g_nEnt;
    LeaveCriticalSection(&g_cs);
    return n;
}


int DebugList(float* wx, float* wy, float* wz, float* dist,
              int* team, int* isLocal, int* ai, int* fresh, int maxN)
{
    if (!g_csInit) return 0;
    float cam[3] = { 0, 0, 0 };
    bool haveCam = vproj::GetCamera(cam);

    // world positions out — callers project what they need (ESP box head/
    // feet, triggerbot body column, future skeleton bones). The proxy origin
    // is at the entity's FEET.
    int out = 0;
    EnterCriticalSection(&g_cs);
    for (int i = 0; i < g_nEnt && out < maxN; i++) {
        if (g_ent[i].x == 0.f && g_ent[i].y == 0.f && g_ent[i].z == 0.f) continue;
        if (haveCam) {
            float d[3] = { g_ent[i].x - cam[0], g_ent[i].y - cam[1], g_ent[i].z - cam[2] };
            float g = d[0]*d[0] + d[1]*d[1] + d[2]*d[2];
            float r = g;                          // Newton sqrt (CRT-free)
            for (int it = 0; it < 20 && r > 0.f; it++) r = 0.5f * (r + g / r);
            dist[out] = r;
        } else dist[out] = 0.f;
        wx[out] = g_ent[i].x; wy[out] = g_ent[i].y; wz[out] = g_ent[i].z;
        team[out] = g_ent[i].team;
        isLocal[out] = (i == g_localIdx);
        ai[out] = g_ent[i].ai ? 1 : 0;
        fresh[out] = (g_tickNow - g_ent[i].lastMove <= 480) ? 1 : 0;   // moved < ~8 s
        out++;
    }
    LeaveCriticalSection(&g_cs);
    return out;
}

const char* StatusText()
{
    if (!g_csInit) return "ENT: press F2 in a match";
    int hot = -1; uint32_t hotN = 0, t0 = 0, t1 = 0;
    EnterCriticalSection(&g_cs);
    for (int i = 0; i < g_nCont; i++)
        if (g_cont[i].changes > hotN) { hotN = g_cont[i].changes; hot = i; }
    for (int i = 0; i < g_nEnt; i++) {
        if (g_ent[i].team == 0) t0++;
        else if (g_ent[i].team == 1) t1++;
    }
    int nE = g_nEnt, nC = g_nCont, loc = g_localIdx;
    uint64_t hotVA = (hot >= 0) ? g_cont[hot].va : 0;
    LeaveCriticalSection(&g_cs);

    char* o = g_status; size_t k = WStr(o, "ENT ");
    k += WU(o + k, (unsigned)nE);
    k += WStr(o + k, " t0="); k += WU(o + k, t0);
    k += WStr(o + k, " t1="); k += WU(o + k, t1);
    k += WStr(o + k, " cont="); k += WU(o + k, (unsigned)nC);
    if (hotVA) {
        k += WStr(o + k, " hot=");
        k += WHex(o + k, hotVA);
        o[k++] = '('; k += WU(o + k, hotN); o[k++] = ')';
    }
    if (loc >= 0) k += WStr(o + k, " YOU");
    o[k] = 0;
    return g_status;
}

// ---- F5: full local-entity dump ------------------------------------------------
static void DumpRange(HANDLE f, uint64_t va, size_t len)
{
    uint8_t buf[0x300];
    if (len > sizeof buf) len = sizeof buf;
    if (!Rd(va, buf, len)) {
        DWORD w = 0; WriteFile(f, "UNMAPPED\r\n", 10, &w, nullptr);
        return;
    }
    for (size_t o = 0; o < len; o += 16) {
        char line[96]; size_t k = WHex(line, va + o);
        line[k++] = ' ';
        uint64_t q0, q1;
        memcpy(&q0, buf + o, 8);
        memcpy(&q1, buf + o + 8, 8);
        k += WHex(line + k, q0);
        line[k++] = ' ';
        k += WHex(line + k, q1);
        line[k++] = '\r'; line[k++] = '\n';
        DWORD w = 0; WriteFile(f, line, (DWORD)k, &w, nullptr);
    }
}
static void DumpLine(HANDLE f, const char* a, uint64_t v)
{
    char line[128]; size_t k = WStr(line, a);
    k += WHex(line + k, v);
    line[k++] = '\r'; line[k++] = '\n';
    DWORD w = 0; WriteFile(f, line, (DWORD)k, &w, nullptr);
}

void DumpLocal()
{
    if (!g_csInit) return;
    EnterCriticalSection(&g_cs);
    int li = g_localIdx;
    Entity local = { 0, 0, 0, 0.f, 0.f, 0.f, -1, false, 0, 0 };
    if (li >= 0 && li < g_nEnt) local = g_ent[li];
    int nC = g_nCont;
    uint64_t contVA[MAX_CONT];
    for (int i = 0; i < nC; i++) contVA[i] = g_cont[i].va;
    LeaveCriticalSection(&g_cs);

    HANDLE f = CreateFileA("C:\\Users\\Public\\gw2_local_dump.txt",
        GENERIC_WRITE, FILE_SHARE_READ, nullptr, CREATE_ALWAYS,
        FILE_ATTRIBUTE_NORMAL, nullptr);
    if (f == INVALID_HANDLE_VALUE) return;

    if (local.charObj) {
        DumpLine(f, "LOCAL charObj ", local.charObj);
        DumpRange(f, local.charObj, 0x300);
        if (local.state) {
            DumpLine(f, "LOCAL state ", local.state);
            DumpRange(f, local.state, 0x180);
            uint64_t obj120 = 0;
            if (Rd(local.state + ST_OBJ120, &obj120, 8) && obj120) {
                DumpLine(f, "LOCAL state+0x120 ", obj120);
                DumpRange(f, obj120, 0x100);
            }
        }
        uint64_t comps = 0;
        if (Rd(local.charObj + CO_COMPS, &comps, 8) && comps) {
            DumpLine(f, "LOCAL components ", comps);
            DumpRange(f, comps, 0x300);
        }
    } else {
        DumpLine(f, "NO LOCAL — no entities yet. charObjs=0x", 0);
    }
    for (int i = 0; i < nC; i++) {
        DumpLine(f, "CONTAINER ", contVA[i]);
        DumpRange(f, contVA[i], CONT_SNAP);
    }
    CloseHandle(f);
}

} // namespace ent
