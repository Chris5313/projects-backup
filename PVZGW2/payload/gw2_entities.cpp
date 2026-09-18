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
#include <cstdio>
#include "../vendor/minhook/include/MinHook.h"

extern void OILog(const char* msg);

namespace ent {

// ---- verified RVAs (see header) ---------------------------------------------
static constexpr uint64_t RVA_CHAROBJ_VT   = 0x228B380;
static constexpr uint64_t RVA_CHAROBJ_TI   = 0x228B658;
static constexpr uint32_t CHAROBJ_MARKER   = 0x100F;
static constexpr uint64_t RVA_TEAMREG_VT   = 0x228BC58;
static constexpr uint64_t RVA_CONTAINER_VT = 0x236B638;
// GameContext offsets for direct entity list reading
static constexpr uintptr_t RVA_GAME_CONTEXT = 0x2CDBED8;
static constexpr uintptr_t CTX_ENT_START    = 0x148;  // -> entity list begin
static constexpr uintptr_t CTX_ENT_END      = 0x150;  // -> entity list end


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
static constexpr uintptr_t BACKYARD_ENT_POS = 0x30;  // float[3] in backyard entities
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

// Hook system state (forward declarations)
static bool g_hookInstalled = false;
static void TryInstallHook();

// Entity heap range tracking - for periodic rescan to find new spawns
static uint64_t g_heapLo = 0xFFFFFFFFFFFFFFFFull;
static uint64_t g_heapHi = 0;
static uint32_t g_rescanTick = 0;
static uint64_t g_imgBase = 0;


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
// =============================================================================
// DIRECT ENTITY READING - Multiple sources for complete coverage
// =============================================================================

// Known manager RVAs (verified in IDA)
static constexpr uint64_t RVA_CLIENTPLAYERGATE = 0x2CDA8B0;  // ClientPlayerGateEntityManager
static constexpr uint64_t RVA_GAMECONTEXT      = 0x2CDBED8;  // GameContext

// Manager structure offsets (from memory analysis)
static constexpr uint64_t MGR_LIST_START = 0x10;
static constexpr uint64_t MGR_LIST_END   = 0x18;

// Entity pointer stride
static constexpr int ENT_PTR_STRIDE = 8;

// Add entity to cache if valid (helper)
static bool TryAddEntity(uint64_t entPtr)
{
    if (!Plausible(entPtr)) return false;
    
    // Check vtable matches CharObj
    uint64_t vtable = 0;
    if (!Rd(entPtr, &vtable, 8) || vtable != g_vaCharObj)
        return false;
    
    // v75: NO marker gate here. The +0x18 marker is a spawn counter
    // (dump: 0x100E/0x100F on live objects) that climbs ALL SESSION — the
    // 0x1000-mask silently rejected every add once ids passed 0x10FF
    // (log: ENTSCAN hits=39 added=0). vtable + TypeInfo + proxy already
    // prove identity (dump: 36/36 unanimous).
    // Validate TypeInfo
    uint64_t ti = 0;
    if (!Rd(entPtr + CO_TI, &ti, 8) || ti != g_vaTI)
        return false;
    
    // Read proxy
    uint64_t proxy = 0;
    if (!Rd(entPtr + CO_PROXY, &proxy, 8) || !Plausible(proxy))
        return false;
    
    // Add to cache (dedup)
    EnterCriticalSection(&g_cs);
    bool exists = false;
    for (int j = 0; j < g_nEnt; j++) {
        if (g_ent[j].charObj == entPtr) { exists = true; break; }
    }
    bool added = false;
    if (!exists && g_nEnt < MAX_ENT) {
        uint64_t state = 0;
        Rd(entPtr + CO_STATE, &state, 8);
        g_ent[g_nEnt].charObj = entPtr;
        g_ent[g_nEnt].proxy = proxy;
        g_ent[g_nEnt].state = state;
        g_ent[g_nEnt].x = g_ent[g_nEnt].y = g_ent[g_nEnt].z = 0.f;
        g_ent[g_nEnt].team = -1;
        g_ent[g_nEnt].ai = false;
        g_ent[g_nEnt].fails = 0;
        g_ent[g_nEnt].lastMove = 0;
        g_nEnt++;
        vproj::RecordTarget(proxy);  // Register for VP calibration
        
        // Track heap bounds
        if (entPtr < g_heapLo) g_heapLo = entPtr;
        if (entPtr > g_heapHi) g_heapHi = entPtr;
    }
    LeaveCriticalSection(&g_cs);
    return added;
}

// Read entities from GameContext entity list (most stable)
static int ReadFromGameContext()
{
    static int logCount = 0;
    if (!g_imgBase || !g_NtRVM) {
        if (logCount++ < 3) OILog("READ: no imgBase or NtRVM");
        return 0;
    }
    
    // Read GameContext pointer
    uint64_t ctx = 0;
    if (!Rd(g_imgBase + RVA_GAMECONTEXT, &ctx, 8) || !Plausible(ctx)) {
        if (logCount++ < 5) {
            char buf[80];
            sprintf_s(buf, "READ: GameContext fail at 0x%llX ctx=0x%llX", 
                g_imgBase + RVA_GAMECONTEXT, ctx);
            OILog(buf);
        }
        return 0;
    }
    
    // Read entity list bounds
    uint64_t listStart = 0, listEnd = 0;
    if (!Rd(ctx + CTX_ENT_START, &listStart, 8) || !Plausible(listStart)) {
        if (logCount++ < 5) {
            char buf[80];
            sprintf_s(buf, "READ: listStart fail ctx=0x%llX start=0x%llX", ctx, listStart);
            OILog(buf);
        }
        return 0;
    }
    if (!Rd(ctx + CTX_ENT_END, &listEnd, 8) || listEnd <= listStart) {
        if (logCount++ < 5) {
            char buf[80];
            sprintf_s(buf, "READ: listEnd fail start=0x%llX end=0x%llX", listStart, listEnd);
            OILog(buf);
        }
        return 0;
    }
    
    // Sanity cap
    if (listEnd - listStart > 200 * 8) {
        if (logCount++ < 5) OILog("READ: list too big");
        return 0;
    }
    
    int added = 0;
    for (uint64_t addr = listStart; addr < listEnd; addr += 8) {
        uint64_t entPtr = 0;
        if (Rd(addr, &entPtr, 8) && TryAddEntity(entPtr))
            added++;
    }
    
    if (logCount++ < 5) {
        char buf[64];
        sprintf_s(buf, "READ: ctx=0x%llX list=%lld->%lld added=%d",
            ctx, listStart, listEnd, added);
        OILog(buf);
    }
    return added;
}

// Read entities from ClientPlayerGateEntityManager
static int ReadFromManager()
{
    if (!g_imgBase || !g_NtRVM) return 0;
    
    // Read manager pointer
    uint64_t mgrPtr = 0;
    if (!Rd(g_imgBase + RVA_CLIENTPLAYERGATE, &mgrPtr, 8) || !Plausible(mgrPtr))
        return 0;
    
    // Try multiple list offsets
    static const int offsets[] = {0x10, 0x28, 0x48, 0x68, 0x88, 0xA8, 0xC8, 0xE8};
    int added = 0;
    
    for (int oi = 0; oi < sizeof(offsets)/sizeof(offsets[0]); oi++) {
        uint64_t listStart = 0, listEnd = 0;
        if (!Rd(mgrPtr + offsets[oi], &listStart, 8) || !Plausible(listStart))
            continue;
        if (!Rd(mgrPtr + offsets[oi] + 8, &listEnd, 8) || listEnd <= listStart)
            continue;
        if (listEnd - listStart > 200 * 8) continue;
        
        for (uint64_t addr = listStart; addr < listEnd; addr += 8) {
            uint64_t entPtr = 0;
            if (Rd(addr, &entPtr, 8) && TryAddEntity(entPtr))
                added++;
        }
    }
    return added;
}

// Scan heap range for CharObj instances (fallback)
static int ScanHeapRange()
{
    if (!g_imgBase || !g_NtRVM) return 0;
    if (g_heapLo >= g_heapHi) return 0;
    
    // Only scan if we have valid heap bounds from previous finds
    uint64_t scanLo = g_heapLo - 0x100000;  // Expand range slightly
    uint64_t scanHi = g_heapHi + 0x100000;
    if (scanHi - scanLo > 0x10000000) return 0;  // Don't scan > 256MB
    
    int added = 0;
    uint8_t buf[4096];
    
    for (uint64_t page = scanLo & ~0xFFFULL; page < scanHi; page += 4096) {
        if (!Rd(page, buf, 4096)) continue;
        
        for (int off = 0; off < 4096; off += 8) {
            uint64_t val = *(uint64_t*)(buf + off);
            if (val == g_vaCharObj) {
                // Found CharObj vtable - this might be an entity
                if (TryAddEntity(page + off))
                    added++;
            }
        }
    }
    return added;
}

// Forward declaration
static int ReadBackyardEntities();   // v75 identity scanner

// Master discovery function - tries all sources
static int ReadEntitiesFromAllSources()
{
    int total = 0;
    
    // Try GameContext first (online matches)
    total += ReadFromGameContext();
    
    // Try Backyard manager (practice area with NPCs)
    total += ReadBackyardEntities();
    
    return total;
}

// Validate cached entities - LENIENT: only remove if memory completely invalid
static void ValidateCachedEntities()
{
    if (!g_imgBase || !g_NtRVM) return;
    
    EnterCriticalSection(&g_cs);
    
    int writeIdx = 0;
    for (int i = 0; i < g_nEnt; i++) {
        // MINIMAL check: just see if memory is readable at all
        uint64_t vtable = 0;
        bool readable = Rd(g_ent[i].charObj, &vtable, 8);
        
        if (readable && vtable != 0) {
            // Memory still accessible - keep it, reset fails
            g_ent[i].fails = 0;
            if (writeIdx != i) g_ent[writeIdx] = g_ent[i];
            writeIdx++;
        } else {
            // Memory unreadable/zeroed - but keep for a VERY long time
            g_ent[i].fails++;
            if (g_ent[i].fails < 600) {  // 10 seconds before removal
                if (writeIdx != i) g_ent[writeIdx] = g_ent[i];
                writeIdx++;
            }
        }
    }
    g_nEnt = writeIdx;
    
    LeaveCriticalSection(&g_cs);
}

// Try multiple manager offsets to find entity list
static int TryManagerOffsets(uint64_t mgrPtr)
{
    if (!mgrPtr || !Plausible(mgrPtr)) return 0;
    
    // Common offsets for entity lists in Frostbite managers
    static const int offsets[] = {0x10, 0x18, 0x28, 0x30, 0x48, 0x50};
    int best = 0;
    
    for (int o = 0; o < sizeof(offsets)/sizeof(offsets[0]); o++) {
        uint64_t listStart = 0, listEnd = 0;
        if (!Rd(mgrPtr + offsets[o], &listStart, 8) || !Plausible(listStart))
            continue;
        if (!Rd(mgrPtr + offsets[o] + 8, &listEnd, 8) || listEnd <= listStart)
            continue;
        
        uint64_t size = listEnd - listStart;
        if (size < 8 || size > 200 * 8) continue;
        
        // Count valid CharObj pointers at this offset
        int count = 0;
        for (uint64_t addr = listStart; addr < listEnd && count < 50; addr += 8) {
            uint64_t ptr = 0;
            if (!Rd(addr, &ptr, 8) || !Plausible(ptr)) continue;
            uint64_t vt = 0;
            if (!Rd(ptr, &vt, 8) || vt != g_vaCharObj) continue;
            count++;
        }
        if (count > best) best = count;
    }
    return best;
}


// Side + NPC flag from the class string. Returns false if the chain breaks
// (caller leaves team -1 = yellow, retries on the round-robin).
static bool ReadFaction(uint64_t charObj, int* teamOut, bool* aiOut)
{
    static DWORD lastLog = 0, lastBadLog = 0;
    static int failCB = 0, failNode = 0, failStr = 0, failRead = 0, failChar = 0, okCount = 0;
    bool ok = false;
    
    uint64_t cb = 0, node = 0, strp = 0;
    if (!Rd(charObj + CO_CLASSBASE, &cb, 8) || !Plausible(cb)) { failCB++; }
    else if (!Rd(cb + CB_NODE, &node, 8) || !Plausible(node)) { failNode++; }
    else if (!Rd(node + NODE_STR, &strp, 8) || !Plausible(strp)) { failStr++; }
    else {
        char s[FAC_STR_LEN];
        if (!Rd(strp, s, FAC_STR_LEN)) { failRead++; }
        else {
            s[FAC_STR_LEN - 1] = 0;
            char c = s[FAC_SIDE_CHAR];
            if (c == 'S' && s[FAC_SIDE_CHAR+1] == 'c') c = s[FAC_SIDE_CHAR+7];
            int team = (c == 'Z') ? 1 : (c == 'P') ? 0 : -1;
            if (team < 0) { 
                if (GetTickCount() - lastBadLog > 500) {
                    lastBadLog = GetTickCount();
                    char buf[100]; int k = 0;
                    const char* h = "FAC_BAD c='"; while(*h) buf[k++] = *h++;
                    buf[k++] = (c >= 32 && c < 127) ? c : '?';
                    h = "' s="; while(*h) buf[k++] = *h++;
                    for (int i = 0; i < 50 && s[i] >= 32 && s[i] < 127; i++) buf[k++] = s[i];
                    buf[k] = 0; OILog(buf);
                }
                failChar++;
            } else {
                bool ai = false;
                for (int i = 0; i + 3 < FAC_STR_LEN; i++) {
                    if (s[i] == '_' && s[i+1] == 'A' && s[i+2] == 'I') { ai = true; break; }
                    if (s[i] == 'T' && s[i+1] == 'u' && s[i+2] == 'r') { ai = true; break; }
                }
                *teamOut = team; *aiOut = ai;
                ok = true; okCount++;
            }
        }
    }
    if (GetTickCount() - lastLog > 2000) {
        lastLog = GetTickCount();
        char buf[100]; int k = 0;
        auto addStr = [&](const char* s) { while(*s) buf[k++] = *s++; };
        auto addInt = [&](int v) { 
            if (v == 0) { buf[k++] = '0'; return; }
            char t[12]; int n = 0;
            while (v > 0) { t[n++] = '0' + (v % 10); v /= 10; }
            while (n > 0) buf[k++] = t[--n];
        };
        addStr("FAC: ok="); addInt(okCount);
        addStr(" cb="); addInt(failCB);
        addStr(" node="); addInt(failNode);
        addStr(" str="); addInt(failStr);
        addStr(" rd="); addInt(failRead);
        addStr(" chr="); addInt(failChar);
        buf[k] = 0; OILog(buf);
        failCB = failNode = failStr = failRead = failChar = okCount = 0;
    }
    return ok;
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

    g_imgBase     = imgBase;  // save for direct GameContext reading
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
        // track entity heap range for periodic rescan
        if (va < g_heapLo) g_heapLo = va;
        if (va > g_heapHi) g_heapHi = va;
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


// ---- periodic entity update ---------------------------------------------------
// Read entities from game managers - no heap scan needed
static void RescanEntities()
{
    // Use all sources for comprehensive scanning
    int added = ReadEntitiesFromAllSources();
    
    // Log if we found new entities
    static DWORD lastLog = 0;
    if (added > 0 && GetTickCount() - lastLog > 2000) {
        lastLog = GetTickCount();
        char buf[80];
        sprintf_s(buf, "RESCAN: +%d ents (total=%d, heap=0x%llX-0x%llX)", 
            added, g_nEnt, g_heapLo, g_heapHi);
        OILog(buf);
    }
}
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

// Forward declaration - defined in FORWARD TRACE section below
static int ReadBackyardEntities();

void OnFrame()
{
    // Initialize critical section on first call
    if (!g_csInit) {
        InitializeCriticalSectionAndSpinCount(&g_cs, 4000);
        g_csInit = true;
        OILog("ONF: CS initialized");
    }

    // Initialize base address once (needed for entity reading)
    static bool initLogged = false;
    if (!g_imgBase) {
        HMODULE exe = GetModuleHandleA(nullptr);
        if (exe) {
            g_imgBase = (uint64_t)exe;
            g_vaCharObj = g_imgBase + RVA_CHAROBJ_VT;
            g_vaTI = g_imgBase + RVA_CHAROBJ_TI;
            
            // Also init NtRVM
            HMODULE nt = GetModuleHandleA("ntdll.dll");
            if (nt) g_NtRVM = (pfn_NtRVM)GetProcAddress(nt, "NtReadVirtualMemory");
            
            char buf[128];
            sprintf_s(buf, "INIT: imgBase=0x%llX NtRVM=%p vaCharObj=0x%llX", 
                g_imgBase, g_NtRVM, g_vaCharObj);
            OILog(buf);
            initLogged = true;
        }
    }

    // snapshot the table
    Entity snap[MAX_ENT];
    int n;
    EnterCriticalSection(&g_cs);
    n = g_nEnt;
    for (int i = 0; i < n; i++) snap[i] = g_ent[i];
    LeaveCriticalSection(&g_cs);

    static uint32_t tick = 0;
    tick++;
    g_tickNow = tick;

    // Entity management: validate cache every frame
    if (g_imgBase) {
        // READ entities synchronously - no background thread racing
        static DWORD lastRead = 0;
        static int readCount = 0;
        DWORD now = GetTickCount();
        if (now - lastRead > 500) {
            lastRead = now;
            int added = ReadEntitiesFromAllSources();
            readCount++;
            if (readCount <= 5 || added > 0) {
                char buf[64];
                sprintf_s(buf, "ENT READ: added=%d total=%d", added, g_nEnt);
                OILog(buf);
            }
        }
        
        // Validate existing cached entities
        ValidateCachedEntities();
        
        // Re-snapshot
        EnterCriticalSection(&g_cs);
        n = g_nEnt;
        for (int i = 0; i < n; i++) snap[i] = g_ent[i];
        LeaveCriticalSection(&g_cs);
    }

    for (int i = 0; i < n; i++) {
        float p[3];
        // Backyard entities: state is small offset (< 0x1000)
        // CharObj entities: state is a heap pointer (large)
        uint64_t posOff = (snap[i].state > 0 && snap[i].state < 0x1000) ? snap[i].state : PX_POS;
        // v36: garbage guard — a read can SUCCEED and return NaN/zeros/huge
        // values (dead or recycled slot). NaN != 0 slipped every check and
        // was STORED; 1e35-scale floats (log: ent0=(45,1e35,1)) passed the
        // NaN check too and clipped every W2S. Bounds: this map is <10 km
        // across, Y is height.
        bool rdOk = Rd(snap[i].proxy + posOff, p, 12);
        bool inBounds = p[0] > -10000.f && p[0] < 10000.f &&
                        p[1] > -1000.f && p[1] < 10000.f &&
                        p[2] > -10000.f && p[2] < 10000.f;
        // v36b: backyard rows live in a known map volume (50..400, 20..120,
        // -250..-50). Their junk reads (log: (-0.0,0.0,43.8) — a HEIGHT, not
        // a position) pass generic bounds and drew tiny boxes at nonsense
        // screen spots. Yard rows must stay yard-shaped every frame.
        // v75: yard-bounds gate REMOVED — it froze boxes at the yard volume
        // edge (tracking loss) and the entity's identity is already proven by
        // vtable+TI; coordinate gating added nothing but stuck boxes.
        bool isBk = (snap[i].state > 0 && snap[i].state < 0x1000);
        bool goodPos = rdOk && p[0] == p[0] && p[1] == p[1] && p[2] == p[2] &&
                       inBounds &&
                       (p[0] != 0.f || p[1] != 0.f || p[2] != 0.f);
        if (goodPos) {
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
                    // Slot recycled - try immediate team resolve to avoid yellow flash
                    int t = -1; bool a = false;
                    if (!ReadFaction(snap[i].charObj, &t, &a)) t = -1;
                    snap[i].team = t;
                    snap[i].ai = a;
                }
                snap[i].lastMove = tick;
            }
            snap[i].x = p[0]; snap[i].y = p[1]; snap[i].z = p[2];
            snap[i].fails = 0;
        } else if (!rdOk) {
            // only hard read failures age the slot; garbage-but-readable
            // keeps the last good position
            if (++snap[i].fails > 1800) {
                // 30 sec of failed reads = truly dead slot (was 600 = 10s)
                snap[i].x = snap[i].y = snap[i].z = 0.f;
            }
        }
        // team/AI refresh: unknown-team entities retry EVERY FRAME until
        // resolved (a fresh spawn resolves within 1-2 frames this way).
        // Known teams re-validate on the round-robin as before.
        // Skip marker check for backyard entities (they don't have CharObj markers)
        bool isBackyard = (snap[i].state > 0 && snap[i].state < 0x1000);
        if (!isBackyard) {
            // v75: liveness via vtable (the +0x18 marker is a spawn counter
            // that drifts past 0x1000 late-session — it flagged LIVE entities
            // as dead). Recycled heap rewrites the vtable qword at +0.
            uint64_t vt = 0;
            if (!Rd(snap[i].charObj, &vt, 8) || vt != g_vaCharObj) {
                // charObj is dead/recycled - mark entity for removal
                snap[i].team = -2; // special "dead" marker
                continue;
            }
            if (snap[i].team < 0 || (uint32_t)i == (tick % 128) ||
                (snap[i].team >= 0 && (uint32_t)i == (tick % 32))) {
                int t = -1; bool a = false;
                if (ReadFaction(snap[i].charObj, &t, &a)) {
                    snap[i].team = t;
                    snap[i].ai = a;
                }
            }
        }
    }

    // Purge dead entities from snap[] (marker invalid = recycled memory)
    // Keep backyard entities (they don't have CharObj markers)
    int purgedCount = 0;
    int writeIdx = 0;
    for (int i = 0; i < n; i++) {
        bool keep = false;
        // Backyard entity: state is small offset (< 0x1000)
        // CharObj entity: state is 0 or a heap pointer (large)
        bool isBackyard = (snap[i].state > 0 && snap[i].state < 0x1000);
        if (isBackyard) {
            // Backyard entity - keep if position is still valid
            float p[3];
            uint64_t posOff = snap[i].state;
            if (Rd(snap[i].proxy + posOff, p, 12) &&
                (p[0] != 0.f || p[1] != 0.f || p[2] != 0.f)) {
                keep = true;
            }
        } else {
            // v75: liveness = vtable STILL the CharObj vtable (recycled heap
            // rewrites the vtable qword at +0). The +0x18 marker is a spawn
            // counter that drifts past the old 0x1000 mask late-session and
            // purged live rows (table drain 37->14 in the log).
            uint64_t vt = 0;
            if (Rd(snap[i].charObj, &vt, 8) && vt == g_vaCharObj) {
                keep = true;
            }
        }
        if (keep) {
            if (writeIdx != i) snap[writeIdx] = snap[i];
            writeIdx++;
        } else {
            purgedCount++;
        }
    }
    n = writeIdx;  // Update n to reflect purged count
    
    // Log entity stats every 2 sec
    static DWORD lastEntLog = 0;
    if (GetTickCount() - lastEntLog > 2000) {
        lastEntLog = GetTickCount();
        char buf[80]; int k = 0;
        auto addStr = [&](const char* s) { while(*s) buf[k++] = *s++; };
        auto addInt = [&](int v) { 
            if (v == 0) { buf[k++] = '0'; return; }
            char t[12]; int tn = 0;
            while (v > 0) { t[tn++] = '0' + (v % 10); v /= 10; }
            while (tn > 0) buf[k++] = t[--tn];
        };
        addStr("ENT: total="); addInt(n);
        addStr(" purged="); addInt(purgedCount);
        // count how many have valid team
        int validTeam = 0;
        for (int i = 0; i < n; i++) if (snap[i].team >= 0) validTeam++;
        addStr(" team_ok="); addInt(validTeam);
        buf[k] = 0; OILog(buf);
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

    // publish back - write purged snap to g_ent
    EnterCriticalSection(&g_cs);
    g_nEnt = n;
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
        // v36: NaN rows are not zero but are not positions either
        if (g_ent[i].x != g_ent[i].x || g_ent[i].y != g_ent[i].y || g_ent[i].z != g_ent[i].z) continue;
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

int GetEntityAddrs(uint64_t* out, int maxN)
{
    EnterCriticalSection(&g_cs);
    int n = (g_nEnt < maxN) ? g_nEnt : maxN;
    for (int i = 0; i < n; i++) out[i] = g_ent[i].charObj;
    LeaveCriticalSection(&g_cs);
    return n;
}

// =============================================================================
// BACKPOINTER SCANNER - Find what points TO our entities
// NO GOING BACK TO HEAP SCAN - WE NEED THE RIGHT METHOD
// This finds the proper pointer chain: StaticGlobal -> Manager -> EntityList -> Entity
// =============================================================================

struct BackpointerHit {
    uint64_t location;      // where the pointer lives
    uint64_t target;        // what it points to
    uint64_t containerBase; // if we can identify the containing structure
    char     regionType[16];// "STATIC", "HEAP", "STACK", "OTHER"
};

static BackpointerHit g_bpHits[1024];
static int g_bpCount = 0;

// Categorize memory region
static void GetRegionType(uint64_t addr, uint64_t imgBase, char* out)
{
    // Static data sections: imgBase + 0x1000000 to imgBase + 0x3000000 (rough)
    if (addr >= imgBase && addr < imgBase + 0x3000000) {
        // Check specific sections
        uint64_t rva = addr - imgBase;
        if (rva >= 0x2000000 && rva < 0x2D00000) {
            // .data / .rdata sections - THESE ARE THE GOLD
            const char* s = "STATIC"; while (*s) *out++ = *s++; *out = 0;
            return;
        }
        const char* s = "IMAGE"; while (*s) *out++ = *s++; *out = 0;
        return;
    }
    // Known entity heap region
    if (g_heapLo != 0xFFFFFFFFFFFFFFFFull && addr >= g_heapLo && addr <= g_heapHi + 0x1000000) {
        const char* s = "ENTHEAP"; while (*s) *out++ = *s++; *out = 0;
        return;
    }
    // Stack region (high addresses, typically 0x7FF...)
    if ((addr >> 44) == 0x7FF) {
        const char* s = "STACK"; while (*s) *out++ = *s++; *out = 0;
        return;
    }
    // General heap
    const char* s = "HEAP"; while (*s) *out++ = *s++; *out = 0;
}

// Scan a memory range for pointers to target addresses
static int ScanRangeForPointers(uint64_t scanStart, uint64_t scanEnd, 
                                 uint64_t* targets, int nTargets,
                                 uint64_t imgBase)
{
    if (!g_NtRVM) return 0;
    
    static uint8_t buf[64 * 1024];
    int found = 0;
    
    for (uint64_t addr = scanStart; addr < scanEnd && g_bpCount < 1000; addr += 64 * 1024) {
        SIZE_T got = 0;
        if (g_NtRVM((HANDLE)-1, (PVOID)addr, buf, 64 * 1024, &got) != 0 || got < 8)
            continue;
        
        size_t nq = got / 8;
        const uint64_t* qv = (const uint64_t*)buf;
        
        for (size_t i = 0; i < nq && g_bpCount < 1000; i++) {
            uint64_t v = qv[i];
            if (v < 0x10000 || v > 0x7FFFFFFFFFFF) continue;
            
            // Check if this value matches any of our targets
            for (int t = 0; t < nTargets; t++) {
                if (v == targets[t]) {
                    g_bpHits[g_bpCount].location = addr + i * 8;
                    g_bpHits[g_bpCount].target = v;
                    g_bpHits[g_bpCount].containerBase = 0;
                    GetRegionType(addr + i * 8, imgBase, g_bpHits[g_bpCount].regionType);
                    g_bpCount++;
                    found++;
                    break;
                }
            }
        }
    }
    return found;
}

// =============================================================================
// FORWARD TRACE - Start from known static GameContext and trace TO entities
// This is diagnostic code - actual entity reading is in ReadEntitiesFromManager()
// =============================================================================

// Additional RVAs for trace (not duplicating the ones at top of file)
static constexpr uint64_t RVA_CAMERAMANAGER = 0x02CEE730;
static constexpr uint64_t RVA_DYNAMICMODEL = 0x02CDB540;
static constexpr uint64_t RVA_BACKYARD_MANAGER = 0x02CDAB40;
static constexpr uint64_t BACKYARD_LIST_START = 0x10;
static constexpr uint64_t BACKYARD_LIST_END = 0x58;
// Backyard coordinate bounds (from camera analysis)
// Camera was at (206.8, 65.4, -177.0) - entities should be similar
// v37: scan window — the dump shows CharObjs living at 0x8000000-0xA0000000.
// Widen freely; the scan is resumable and budgeted.
static constexpr uint64_t HEAP_SCAN_LO = 0x1000000ull;
static constexpr uint64_t HEAP_SCAN_HI = 0xB0000000ull;

// v75: IsBackyardCoord DELETED — yard coordinate gating froze boxes at the
// yard volume edge; identity comes from vtable+TI validation instead.

// (declared above; was forward-declared here too late for the update loop)

// Read entities directly from the backyard manager
// Position may be at:
//  - entity+0x30 directly
//  - entity+offset -> nested object+0x70 (like CharObj proxy)
// ---------------------------------------------------------------------------
// v76: BF4-style PlayerManager enumeration — dump-verified end to end:
//   manager object (vtable image RVA 0x21E3850) holds a FIXED entity array
//   at +0xD0, 17 slots of CharObj pointers (dump: 16 live + 1 gap).
//   BF4/spankerfield walks GameContext -> PlayerManager -> player array.
//   GW2's context statics are dead, so the manager is found ONCE by its
//   unique vtable (candidate validated by its array actually containing
//   CharObjs), then the fixed array is read directly every frame. No
//   per-entity heap scanning, no coordinate gates, nothing to lose.
static constexpr uint64_t RVA_ENTMGR_VT    = 0x21E3850;
static constexpr uint64_t ENTMGR_ARRAY_OFF = 0xD0;
static constexpr int      ENTMGR_MAX_SLOTS = 17;
static uint64_t g_entMgr = 0;   // resolved manager instance

static int ReadBackyardEntities()
{
    if (!g_imgBase || !g_NtRVM) return 0;

    static DWORD s_lastLog = 0;
    static int   s_mgrFails = 0, s_zeroStreak = 0;
    int added = 0;

    // ---- stage 1: locate the manager (ONE full sweep, resumable, cached) ----
    if (!g_entMgr) {
        static uint64_t s_cursor = 0;
        static uint64_t s_cand[8];
        static int      s_candCO[8];   // CharObj-valid slots per candidate
        static int      s_nCand = 0;
        DWORD t0 = GetTickCount();
        uint8_t buf[0x8000];
        const uint64_t vtMgr = g_imgBase + RVA_ENTMGR_VT;
        auto SlotPlausible = [](uint64_t v) {
            return v == 0 || (v > 0x1000000ull && v < 0xB0000000ull && !(v & 3));
        };
        while (s_cursor < HEAP_SCAN_HI) {
            uint64_t page = s_cursor & ~0xFFFULL;
            if (!Rd(page, buf, sizeof(buf))) {
                s_cursor = (page + 0x100000) & ~0xFFFFFULL;
                if (GetTickCount() - t0 > 6) return 0;
                continue;
            }
            for (int off = 0; off + 8 <= (int)sizeof(buf); off += 8) {
                uint64_t val;
                memcpy(&val, buf + off, 8);
                if (val != vtMgr) continue;
                uint64_t cand = page + off;
                uint64_t slots[ENTMGR_MAX_SLOTS];
                if (!Rd(cand + ENTMGR_ARRAY_OFF, slots, sizeof(slots))) continue;
                bool allOk = true;
                for (int k = 0; k < ENTMGR_MAX_SLOTS && allOk; k++)
                    if (!SlotPlausible(slots[k])) allOk = false;
                if (!allOk) continue;
                // validate the candidate by counting CharObj vtables in slots
                int co = 0;
                for (int k = 0; k < ENTMGR_MAX_SLOTS; k++) {
                    if (!slots[k]) continue;
                    uint64_t vt = 0;
                    if (Rd(slots[k], &vt, 8) && vt == g_vaCharObj) co++;
                }
                if (co < 1) continue;   // empty/foreign instance
                int worst = 0;
                for (int k = 1; k < 8; k++) if (s_candCO[k] < s_candCO[worst]) worst = k;
                if (s_nCand < 8) { s_cand[s_nCand] = cand; s_candCO[s_nCand] = co; s_nCand++; }
                else if (co > s_candCO[worst]) { s_cand[worst] = cand; s_candCO[worst] = co; }
            }
            s_cursor = page + sizeof(buf);
            if (GetTickCount() - t0 > 6) return 0;   // resume next frame
        }
        // sweep complete: pick the candidate with the most CharObj slots
        int best = -1, bestCO = 0;
        for (int k = 0; k < s_nCand; k++)
            if (s_candCO[k] > bestCO) { bestCO = s_candCO[k]; best = k; }
        if (best >= 0) {
            g_entMgr = s_cand[best];
            char b[96];
            snprintf(b, sizeof(b), "ENTMGR: locked mgr=%llX coSlots=%d",
                (unsigned long long)g_entMgr, bestCO);
            OILog(b);
        } else {
            DWORD now = GetTickCount();
            if (now - s_lastLog > 5000) {
                s_lastLog = now;
                OILog("ENTMGR: no manager yet - sweep restarts");
            }
        }
        s_nCand = 0;
        s_cursor = HEAP_SCAN_LO;
        if (!g_entMgr) return 0;
    }

    // ---- stage 2: read the FIXED array every frame (the BF4 walk) ----------
    uint64_t slots[ENTMGR_MAX_SLOTS];
    if (!Rd(g_entMgr + ENTMGR_ARRAY_OFF, slots, sizeof(slots))) {
        if (++s_mgrFails > 30) {   // ~0.5 s of dead memory: map changed?
            OILog("ENTMGR: array unreadable - re-locking manager");
            g_entMgr = 0; s_mgrFails = 0;
        }
        return 0;
    }
    s_mgrFails = 0;

    int live = 0;
    for (int k = 0; k < ENTMGR_MAX_SLOTS; k++) {
        uint64_t ent = slots[k];
        if (!ent || ent < 0x1000000ull || ent > 0xB0000000ull || (ent & 3)) continue;
        live++;
        if (TryAddEntity(ent)) added++;
    }

    // reused-memory guard: slots look populated but NEVER validate as
    // entities for ~5 s -> the manager object was recycled, re-lock
    if (live > 0 && g_nEnt == 0) {
        if (++s_zeroStreak > 300) {
            OILog("ENTMGR: slots never validate - re-locking manager");
            g_entMgr = 0; s_zeroStreak = 0;
        }
    } else s_zeroStreak = 0;

    DWORD now = GetTickCount();
    if (now - s_lastLog > 5000) {
        s_lastLog = now;
        char b[128];
        snprintf(b, sizeof(b), "ENTMGR: mgr=%llX live=%d total=%d",
            (unsigned long long)g_entMgr, live, g_nEnt);
        OILog(b);
    }
    return added;
}


// Helper: try to read entity list from object at given offsets
static void TryEntityList(HANDLE f, const char* name, uint64_t obj, uint64_t startOff, uint64_t endOff)
{
    char line[512];
    DWORD written;
    int len;
    
    uint64_t listStart = 0, listEnd = 0;
    g_NtRVM((HANDLE)-1, (PVOID)(obj + startOff), &listStart, 8, nullptr);
    g_NtRVM((HANDLE)-1, (PVOID)(obj + endOff), &listEnd, 8, nullptr);
    
    len = sprintf_s(line, "  %s+0x%llX/0x%llX: start=0x%llX end=0x%llX",
        name, startOff, endOff, listStart, listEnd);
    WriteFile(f, line, len, &written, nullptr);
    
    if (listStart && listEnd && listEnd > listStart && 
        listStart > 0x10000 && listEnd < 0x7FFFFFFFFFFF) {
        uint64_t count = (listEnd - listStart) / 8;
        len = sprintf_s(line, " (%llu entries)\r\n", count);
        WriteFile(f, line, len, &written, nullptr);
        
        // Check first few entries for CharObj
        int toCheck = (count > 8) ? 8 : (int)count;
        int charObjCount = 0;
        for (int i = 0; i < toCheck; i++) {
            uint64_t entPtr = 0;
            g_NtRVM((HANDLE)-1, (PVOID)(listStart + i * 8), &entPtr, 8, nullptr);
            if (entPtr > 0x10000 && entPtr < 0x7FFFFFFFFFFF) {
                // Read multiple offsets to find vtable and understand structure
                uint64_t off0 = 0, off8 = 0, off10 = 0, off18 = 0;
                g_NtRVM((HANDLE)-1, (PVOID)entPtr, &off0, 8, nullptr);
                g_NtRVM((HANDLE)-1, (PVOID)(entPtr + 8), &off8, 8, nullptr);
                g_NtRVM((HANDLE)-1, (PVOID)(entPtr + 0x10), &off10, 8, nullptr);
                g_NtRVM((HANDLE)-1, (PVOID)(entPtr + 0x18), &off18, 8, nullptr);
                
                // Check which offset might be vtable (in image range)
                uint64_t vtRva0 = (off0 > g_imgBase && off0 < g_imgBase + 0x4000000) ? (off0 - g_imgBase) : 0;
                uint64_t vtRva8 = (off8 > g_imgBase && off8 < g_imgBase + 0x4000000) ? (off8 - g_imgBase) : 0;
                
                // Try to read type name string if offset 0 points to image
                char typeName[64] = {0};
                if (vtRva0 != 0) {
                    g_NtRVM((HANDLE)-1, (PVOID)off0, typeName, 48, nullptr);
                    // Check if it looks like a string (printable ASCII)
                    bool isString = true;
                    for (int j = 0; j < 8 && typeName[j]; j++) {
                        if (typeName[j] < 0x20 || typeName[j] > 0x7E) {
                            isString = false;
                            break;
                        }
                    }
                    if (!isString) typeName[0] = 0;
                }
                
                const char* charObjTag = "";
                if (vtRva0 == RVA_CHAROBJ_VT || vtRva8 == RVA_CHAROBJ_VT) {
                    charObjTag = " <-- CHAROBJ!";
                    charObjCount++;
                }
                
                if (typeName[0]) {
                    len = sprintf_s(line, "    [%d] 0x%llX: type=\"%.40s\" +8=0x%llX (vt@0=0x%llX)%s\r\n",
                        i, entPtr, typeName, off8, vtRva0, charObjTag);
                } else {
                    len = sprintf_s(line, "    [%d] 0x%llX: +0=0x%llX +8=0x%llX +10=0x%llX +18=0x%llX (vt@0=0x%llX vt@8=0x%llX)%s\r\n",
                        i, entPtr, off0, off8, off10, off18, vtRva0, vtRva8, charObjTag);
                }
                WriteFile(f, line, len, &written, nullptr);
                
                // Try to read position data from common transform offsets
                float pos[4] = {0};
                // Check various common offsets for position data
                int posOffsets[] = {0x30, 0x40, 0x50, 0x60, 0x70, 0x80, 0x90, 0xA0, 0xB0, 0xC0, 0xD0, 0xE0, 0xF0, 0x100, 0x110, 0x120};
                for (int po = 0; po < sizeof(posOffsets)/sizeof(posOffsets[0]); po++) {
                    g_NtRVM((HANDLE)-1, (PVOID)(entPtr + posOffsets[po]), pos, 16, nullptr);
                    // Check if looks like valid 3D coordinates (reasonable range)
                    if (pos[0] > -50000.0f && pos[0] < 50000.0f &&
                        pos[1] > -50000.0f && pos[1] < 50000.0f &&
                        pos[2] > -50000.0f && pos[2] < 50000.0f &&
                        pos[0] != 0.0f && pos[1] != 0.0f && (pos[2] != 0.0f || pos[3] != 0.0f)) {
                        len = sprintf_s(line, "      +0x%X pos: %.2f, %.2f, %.2f\r\n",
                            posOffsets[po], pos[0], pos[1], pos[2]);
                        WriteFile(f, line, len, &written, nullptr);
                    }
                }
            }
        }
        if (charObjCount > 0) {
            len = sprintf_s(line, "  *** FOUND %d CHAROBJ ENTITIES! ***\r\n", charObjCount);
            WriteFile(f, line, len, &written, nullptr);
        }
    } else {
        WriteFile(f, " (invalid)\r\n", 12, &written, nullptr);
    }
}

void TraceFromGameContext()
{
    if (!g_imgBase || !g_NtRVM) {
        OILog("TRACE: No imgBase or NtRVM\n");
        return;
    }
    
    OILog("TRACE: Scanning multiple static pointers...\n");
    
    HANDLE f = CreateFileA("C:\\Users\\Public\\gw2_trace.txt",
        GENERIC_WRITE, FILE_SHARE_READ, nullptr, CREATE_ALWAYS,
        FILE_ATTRIBUTE_NORMAL, nullptr);
    if (f == INVALID_HANDLE_VALUE) {
        OILog("TRACE: Failed to create output file\n");
        return;
    }
    
    char line[512];
    DWORD written;
    int len;
    
    WriteFile(f, "=== MULTI-STATIC POINTER TRACE ===\r\n", 37, &written, nullptr);
    len = sprintf_s(line, "Image base: 0x%llX\r\n\r\n", g_imgBase);
    WriteFile(f, line, len, &written, nullptr);
    
    // List of known static pointers to check
    struct StaticPtr {
        const char* name;
        uint64_t rva;
    } statics[] = {
        {"GameContext", RVA_GAMECONTEXT},
        {"ClientPlayerGateEM", RVA_CLIENTPLAYERGATE},
        {"CameraManager", RVA_CAMERAMANAGER},
        {"DynamicModelEM", RVA_DYNAMICMODEL},
        // Additional globals found from IDA analysis
        {"Global_2CD7EB0", 0x2CD7EB0},
        {"Global_2CD8260", 0x2CD8260},
        {"Global_2CD82D8", 0x2CD82D8},
        {"Global_2CD9288", 0x2CD9288},  // vtable 0x221A868
        {"Global_2CDA248", 0x2CDA248},  // vtable 0x22080F8
        {"Global_2CDA258", 0x2CDA258},  // vtable 0x2208088
        {"Global_2CDAB40", 0x2CDAB40},
        {"Global_2CDAE20", 0x2CDAE20},  // vtable 0x221AB48
        {"Global_2CDAE30", 0x2CDAE30},  // vtable 0x223D4C0
        {"AIWaveManager", 0x30020F0},   // ClientAIWaveManagerEntity - manages AI spawns!
    };
    
    for (int s = 0; s < sizeof(statics)/sizeof(statics[0]); s++) {
        uint64_t pStatic = g_imgBase + statics[s].rva;
        uint64_t value = 0;
        
        len = sprintf_s(line, "=== %s (RVA 0x%llX) ===\r\n", statics[s].name, statics[s].rva);
        WriteFile(f, line, len, &written, nullptr);
        
        if (g_NtRVM((HANDLE)-1, (PVOID)pStatic, &value, 8, nullptr) != 0) {
            WriteFile(f, "  Read failed\r\n\r\n", 17, &written, nullptr);
            continue;
        }
        
        len = sprintf_s(line, "  Value: 0x%llX\r\n", value);
        WriteFile(f, line, len, &written, nullptr);
        
        // Check if value looks like a valid pointer
        if (value == 0 || value == 0xFFFFFFFFFFFFFFFF || value < 0x10000 || value > 0x7FFFFFFFFFFF) {
            WriteFile(f, "  (invalid/sentinel)\r\n\r\n", 24, &written, nullptr);
            continue;
        }
        
        // Dump structure
        WriteFile(f, "  Structure dump (pointers only):\r\n", 35, &written, nullptr);
        uint8_t dump[0x200];
        if (g_NtRVM((HANDLE)-1, (PVOID)value, dump, sizeof(dump), nullptr) == 0) {
            for (int off = 0; off < 0x200; off += 8) {
                uint64_t v = *(uint64_t*)(dump + off);
                if (v > 0x10000 && v < 0x7FFFFFFFFFFF) {
                    len = sprintf_s(line, "    +0x%03X: 0x%llX\r\n", off, v);
                    WriteFile(f, line, len, &written, nullptr);
                }
            }
        }
        
        // Try common entity list offsets
        WriteFile(f, "  Trying entity list offsets:\r\n", 31, &written, nullptr);
        // Known offsets from disassembly
        TryEntityList(f, statics[s].name, value, 0x28, 0x30);  // Primary entity list
        TryEntityList(f, statics[s].name, value, 0x48, 0x50);  // Secondary list
        TryEntityList(f, statics[s].name, value, 0x148, 0x150);
        TryEntityList(f, statics[s].name, value, 0x10, 0x18);
        TryEntityList(f, statics[s].name, value, 0x20, 0x28);
        TryEntityList(f, statics[s].name, value, 0x30, 0x38);
        TryEntityList(f, statics[s].name, value, 0x40, 0x48);
        TryEntityList(f, statics[s].name, value, 0x58, 0x60);
        TryEntityList(f, statics[s].name, value, 0x68, 0x70);
        TryEntityList(f, statics[s].name, value, 0x80, 0x88);
        TryEntityList(f, statics[s].name, value, 0x100, 0x108);
        
        WriteFile(f, "\r\n", 2, &written, nullptr);
    }
    
    // Scan data sections for pointers to entity heap region
    // The heap scanner found entities in ~0x80000000 - 0xA0000000 range
    WriteFile(f, "=== SCANNING FOR POINTERS TO ENTITY HEAP REGION ===\r\n", 54, &written, nullptr);
    WriteFile(f, "(Looking for static pointers to 0x80000000-0xA0000000)\r\n\r\n", 58, &written, nullptr);
    
    // Scan multiple data sections
    struct Section { uint64_t start; uint64_t end; const char* name; } sections[] = {
        {0x02001000, 0x02982000, "udata"},
        {0x02982000, 0x03005000, "sxdata"},
        {0x02CD0000, 0x02D50000, "globals"},  // approximate globals area
    };
    
    int totalFound = 0;
    for (int sec = 0; sec < 3; sec++) {
        uint64_t secStart = g_imgBase + sections[sec].start;
        uint64_t secEnd = g_imgBase + sections[sec].end;
        int secFound = 0;
        
        len = sprintf_s(line, "Section %s (0x%llX - 0x%llX):\r\n", 
            sections[sec].name, sections[sec].start, sections[sec].end);
        WriteFile(f, line, len, &written, nullptr);
        
        for (uint64_t addr = secStart; addr < secEnd && secFound < 30; addr += 8) {
            uint64_t ptr = 0;
            if (g_NtRVM((HANDLE)-1, (PVOID)addr, &ptr, 8, nullptr) != 0) continue;
            
            // Check if pointer is in entity heap region
            if (ptr >= 0x80000000 && ptr < 0xA0000000) {
                // Read what it points to
                uint64_t val = 0;
                g_NtRVM((HANDLE)-1, (PVOID)ptr, &val, 8, nullptr);
                
                uint64_t vtableRva = (val > g_imgBase && val < g_imgBase + 0x4000000) ? 
                                     (val - g_imgBase) : 0;
                
                len = sprintf_s(line, "  RVA 0x%llX -> heap 0x%llX (first qword: 0x%llX, vtable? 0x%llX)\r\n",
                    addr - g_imgBase, ptr, val, vtableRva);
                WriteFile(f, line, len, &written, nullptr);
                secFound++;
                totalFound++;
            }
        }
        
        if (secFound == 0) {
            WriteFile(f, "  (none found)\r\n", 16, &written, nullptr);
        }
        WriteFile(f, "\r\n", 2, &written, nullptr);
    }
    
    len = sprintf_s(line, "Total heap pointers found: %d\r\n\r\n", totalFound);
    WriteFile(f, line, len, &written, nullptr);
    
    // Check TLS - the game uses gs:58h for context
    WriteFile(f, "=== TLS SCAN (Thread Local Storage) ===\r\n", 41, &written, nullptr);
    
    // Read TEB (Thread Environment Block) to get TLS array
    uint64_t teb = __readgsqword(0x30);  // gs:30h = TEB
    uint64_t tlsArray = 0;
    
    // TLS slots are at TEB+0x58 (ThreadLocalStoragePointer)
    if (g_NtRVM((HANDLE)-1, (PVOID)(teb + 0x58), &tlsArray, 8, nullptr) == 0 && tlsArray) {
        len = sprintf_s(line, "TEB: 0x%llX, TLS Array: 0x%llX\r\n", teb, tlsArray);
        WriteFile(f, line, len, &written, nullptr);
        
        // Scan first 64 TLS slots
        WriteFile(f, "TLS slots with heap pointers:\r\n", 31, &written, nullptr);
        int tlsFound = 0;
        for (int slot = 0; slot < 64; slot++) {
            uint64_t slotVal = 0;
            if (g_NtRVM((HANDLE)-1, (PVOID)(tlsArray + slot * 8), &slotVal, 8, nullptr) != 0) continue;
            
            // Check if slot points to heap region
            if (slotVal >= 0x10000 && slotVal < 0x7FFFFFFFFFFF) {
                uint64_t first = 0;
                g_NtRVM((HANDLE)-1, (PVOID)slotVal, &first, 8, nullptr);
                
                uint64_t vtableRva = (first > g_imgBase && first < g_imgBase + 0x4000000) ?
                                     (first - g_imgBase) : 0;
                
                len = sprintf_s(line, "  TLS[%d]: 0x%llX -> first: 0x%llX (vtable? 0x%llX)\r\n",
                    slot, slotVal, first, vtableRva);
                WriteFile(f, line, len, &written, nullptr);
                tlsFound++;
                
                // If this looks like an object, dump its structure
                if (vtableRva > 0x1000 && vtableRva < 0x3000000) {
                    uint8_t objDump[0x200];
                    if (g_NtRVM((HANDLE)-1, (PVOID)slotVal, objDump, sizeof(objDump), nullptr) == 0) {
                        WriteFile(f, "    Structure pointers:\r\n", 25, &written, nullptr);
                        for (int off = 0; off < 0x180; off += 8) {
                            uint64_t v = *(uint64_t*)(objDump + off);
                            if (v > 0x10000 && v < 0x7FFFFFFFFFFF) {
                                // Check for entity list pattern (start < end, reasonable size)
                                if (off < 0x170) {
                                    uint64_t next = *(uint64_t*)(objDump + off + 8);
                                    if (next > v && (next - v) < 0x100000 && ((next - v) % 8) == 0) {
                                        uint64_t count = (next - v) / 8;
                                        len = sprintf_s(line, "    +0x%03X: 0x%llX (LIST? %llu items to +0x%03X)\r\n",
                                            off, v, count, off + 8);
                                        WriteFile(f, line, len, &written, nullptr);
                                        
                                        // Check first entry
                                        uint64_t ent = 0;
                                        g_NtRVM((HANDLE)-1, (PVOID)v, &ent, 8, nullptr);
                                        if (ent > 0x10000) {
                                            uint64_t entVt = 0;
                                            g_NtRVM((HANDLE)-1, (PVOID)ent, &entVt, 8, nullptr);
                                            len = sprintf_s(line, "      first: 0x%llX vtable=0x%llX%s\r\n",
                                                ent, entVt - g_imgBase,
                                                ((entVt - g_imgBase) == RVA_CHAROBJ_VT) ? " CHAROBJ!" : "");
                                            WriteFile(f, line, len, &written, nullptr);
                                        }
                                        continue;
                                    }
                                }
                                len = sprintf_s(line, "    +0x%03X: 0x%llX\r\n", off, v);
                                WriteFile(f, line, len, &written, nullptr);
                            }
                        }
                    }
                }
            }
        }
        if (tlsFound == 0) {
            WriteFile(f, "  (no heap pointers in TLS)\r\n", 29, &written, nullptr);
        }
    } else {
        WriteFile(f, "  Could not read TLS array\r\n", 28, &written, nullptr);
    }
    WriteFile(f, "\r\n=== TRACE COMPLETE ===\r\n", 26, &written, nullptr);
    CloseHandle(f);
    
    OILog("TRACE: Done! Check C:\\Users\\Public\\gw2_trace.txt\n");
}

// Main backpointer scan - F3 triggers this
void RunBackpointerScan()
{
    if (!g_imgBase || !g_NtRVM) {
        OILog("BPSCAN: No imgBase or NtRVM - run F2 first\n");
        return;
    }
    
    EnterCriticalSection(&g_cs);
    if (g_nEnt == 0) {
        LeaveCriticalSection(&g_cs);
        OILog("BPSCAN: No entities - run F2 heap scan first\n");
        return;
    }
    
    // Collect first 8 entity addresses as targets
    uint64_t targets[8];
    int nTargets = (g_nEnt > 8) ? 8 : g_nEnt;
    for (int i = 0; i < nTargets; i++) {
        targets[i] = g_ent[i].charObj;
    }
    uint64_t heapLo = g_heapLo;
    uint64_t heapHi = g_heapHi;
    LeaveCriticalSection(&g_cs);
    
    g_bpCount = 0;
    OILog("BPSCAN: Starting backpointer scan...\n");
    
    char logBuf[256];
    int k = 0;
    auto add = [&](const char* s) { while (*s && k < 250) logBuf[k++] = *s++; };
    auto addHex = [&](uint64_t v) {
        if (k > 230) return;
        logBuf[k++] = '0'; logBuf[k++] = 'x';
        for (int sh = 60; sh >= 0; sh -= 4) {
            int d = (v >> sh) & 0xF;
            if (d || sh < 16 || k > 2 + (60-sh)/4) // skip leading zeros but keep last 4 digits
                logBuf[k++] = (d < 10) ? ('0' + d) : ('A' + d - 10);
        }
    };
    
    // Log targets
    k = 0;
    add("BPSCAN: Searching for "); 
    logBuf[k++] = '0' + nTargets;
    add(" entity pointers...\n");
    logBuf[k] = 0;
    OILog(logBuf);
    
    // === PHASE 1: Scan STATIC sections (most important) ===
    // .data section: roughly imgBase + 0x2C00000 to 0x2E00000
    // .rdata: roughly imgBase + 0x1F00000 to 0x2200000
    OILog("BPSCAN: Phase 1 - Scanning static sections...\n");
    
    int staticHits = ScanRangeForPointers(
        g_imgBase + 0x2C00000,  // .data start
        g_imgBase + 0x3000000,  // .data end (generous)
        targets, nTargets, g_imgBase);
    
    k = 0;
    add("BPSCAN: Static section hits: ");
    logBuf[k++] = '0' + (staticHits / 100) % 10;
    logBuf[k++] = '0' + (staticHits / 10) % 10;
    logBuf[k++] = '0' + staticHits % 10;
    add("\n");
    logBuf[k] = 0;
    OILog(logBuf);
    
    // === PHASE 2: Scan entity heap region ===
    OILog("BPSCAN: Phase 2 - Scanning entity heap region...\n");
    
    if (heapLo != 0xFFFFFFFFFFFFFFFFull) {
        uint64_t heapScanLo = (heapLo > 0x10000000) ? heapLo - 0x10000000 : 0x10000;
        uint64_t heapScanHi = heapHi + 0x10000000;
        
        int heapHits = ScanRangeForPointers(
            heapScanLo, heapScanHi,
            targets, nTargets, g_imgBase);
        
        k = 0;
        add("BPSCAN: Heap region hits: ");
        logBuf[k++] = '0' + (heapHits / 100) % 10;
        logBuf[k++] = '0' + (heapHits / 10) % 10;
        logBuf[k++] = '0' + heapHits % 10;
        add("\n");
        logBuf[k] = 0;
        OILog(logBuf);
    }
    
    // === PHASE 2.5: SECOND-LEVEL SCAN ===
    // Find the container by looking at ENTHEAP hits
    // Those hits are INSIDE a container - find what points to the container itself
    OILog("BPSCAN: Phase 2.5 - Finding container base from ENTHEAP hits...\n");
    
    uint64_t containerLo = 0xFFFFFFFFFFFFFFFFull;
    uint64_t containerHi = 0;
    int entHeapHitCount = 0;
    
    for (int i = 0; i < g_bpCount; i++) {
        if (g_bpHits[i].regionType[0] == 'E') { // ENTHEAP
            if (g_bpHits[i].location < containerLo) containerLo = g_bpHits[i].location;
            if (g_bpHits[i].location > containerHi) containerHi = g_bpHits[i].location;
            entHeapHitCount++;
        }
    }
    
    // Store first-level hits count before second-level scan
    int firstLevelCount = g_bpCount;
    
    if (entHeapHitCount > 0 && containerLo != 0xFFFFFFFFFFFFFFFFull) {
        // Align container bounds (likely structure starts at 0x...000 or 0x...00)
        uint64_t containerBase = containerLo & ~0xFFFull; // align to 4KB page
        uint64_t containerEnd = (containerHi + 0x1000) & ~0xFFFull;
        
        k = 0;
        add("BPSCAN: Container region: ");
        addHex(containerBase);
        add(" - ");
        addHex(containerEnd);
        add("\n");
        logBuf[k] = 0;
        OILog(logBuf);
        
        // Create targets for second-level scan - pointers INTO the container
        uint64_t level2Targets[16];
        int nLevel2 = 0;
        
        // Add the container base and key offsets
        level2Targets[nLevel2++] = containerBase;
        level2Targets[nLevel2++] = containerBase + 0x8;
        level2Targets[nLevel2++] = containerBase + 0x10;
        level2Targets[nLevel2++] = containerBase + 0x18;
        level2Targets[nLevel2++] = containerBase + 0x20;
        
        // Also add the actual hit locations (these are inside the container)
        for (int i = 0; i < g_bpCount && nLevel2 < 16; i++) {
            if (g_bpHits[i].regionType[0] == 'E') {
                // Check if this location is new
                bool exists = false;
                for (int j = 0; j < nLevel2; j++) {
                    if (level2Targets[j] == g_bpHits[i].location) { exists = true; break; }
                }
                if (!exists) level2Targets[nLevel2++] = g_bpHits[i].location;
            }
        }
        
        OILog("BPSCAN: Doing LEVEL 2 scan - finding what points to the container...\n");
        
        // Scan static sections for pointers to container region
        int level2Static = ScanRangeForPointers(
            g_imgBase + 0x2C00000, g_imgBase + 0x3000000,
            level2Targets, nLevel2, g_imgBase);
        
        k = 0;
        add("BPSCAN: Level 2 static hits: ");
        logBuf[k++] = '0' + (level2Static / 100) % 10;
        logBuf[k++] = '0' + (level2Static / 10) % 10;
        logBuf[k++] = '0' + level2Static % 10;
        add("\n");
        logBuf[k] = 0;
        OILog(logBuf);
        
        // Also scan the broader heap for EntityManager-like structures
        if (heapLo != 0xFFFFFFFFFFFFFFFFull) {
            int level2Heap = ScanRangeForPointers(
                heapLo > 0x10000000 ? heapLo - 0x10000000 : 0x10000,
                heapHi + 0x10000000,
                level2Targets, nLevel2, g_imgBase);
            
            k = 0;
            add("BPSCAN: Level 2 heap hits: ");
            logBuf[k++] = '0' + (level2Heap / 100) % 10;
            logBuf[k++] = '0' + (level2Heap / 10) % 10;
            logBuf[k++] = '0' + level2Heap % 10;
            add("\n");
            logBuf[k] = 0;
            OILog(logBuf);
        }
    }
    
    // === PHASE 3: Dump results to file ===
    HANDLE f = CreateFileA("C:\\Users\\Public\\gw2_backpointers.txt",
        GENERIC_WRITE, FILE_SHARE_READ, nullptr, CREATE_ALWAYS,
        FILE_ATTRIBUTE_NORMAL, nullptr);
    
    if (f != INVALID_HANDLE_VALUE) {
        char line[512];
        DWORD written;
        
        // Header
        int len = 0;
        const char* hdr = "=== BACKPOINTER SCAN RESULTS ===\r\n"
                          "NO GOING BACK TO HEAP SCAN - FIND THE RIGHT METHOD\r\n"
                          "Format: [REGION] location -> target\r\n"
                          "STATIC hits are GOLD - these are the accessor chain\r\n\r\n";
        WriteFile(f, hdr, (DWORD)strlen(hdr), &written, nullptr);
        
        // Targets
        len = sprintf_s(line, "=== TARGETS (first %d entities) ===\r\n", nTargets);
        WriteFile(f, line, len, &written, nullptr);
        for (int i = 0; i < nTargets; i++) {
            len = sprintf_s(line, "  Entity[%d]: 0x%llX\r\n", i, targets[i]);
            WriteFile(f, line, len, &written, nullptr);
        }
        
        // Heap bounds
        len = sprintf_s(line, "\r\n=== ENTITY HEAP BOUNDS ===\r\n  Lo: 0x%llX\r\n  Hi: 0x%llX\r\n",
                        heapLo, heapHi);
        WriteFile(f, line, len, &written, nullptr);
        
        // Container info (from Level 2 scan)
        if (containerLo != 0xFFFFFFFFFFFFFFFFull) {
            uint64_t containerBase = containerLo & ~0xFFFull;
            len = sprintf_s(line, "\r\n=== DETECTED CONTAINER REGION ===\r\n"
                                  "  Base (page-aligned): 0x%llX\r\n"
                                  "  First pointer at: 0x%llX\r\n"
                                  "  Last pointer at: 0x%llX\r\n"
                                  "  Level 1 hits: %d, Level 2 hits: %d\r\n\r\n",
                            containerBase, containerLo, containerHi,
                            firstLevelCount, g_bpCount - firstLevelCount);
            WriteFile(f, line, len, &written, nullptr);
        } else {
            WriteFile(f, "\r\n", 2, &written, nullptr);
        }
        // Group results by region type
        WriteFile(f, "=== STATIC HITS (MOST IMPORTANT) ===\r\n", 39, &written, nullptr);
        for (int i = 0; i < g_bpCount; i++) {
            if (g_bpHits[i].regionType[0] == 'S') { // STATIC
                uint64_t rva = g_bpHits[i].location - g_imgBase;
                len = sprintf_s(line, "  [STATIC RVA 0x%llX] 0x%llX -> 0x%llX\r\n",
                                rva, g_bpHits[i].location, g_bpHits[i].target);
                WriteFile(f, line, len, &written, nullptr);
            }
        }
        
        WriteFile(f, "\r\n=== IMAGE HITS ===\r\n", 22, &written, nullptr);
        for (int i = 0; i < g_bpCount; i++) {
            if (g_bpHits[i].regionType[0] == 'I') { // IMAGE
                uint64_t rva = g_bpHits[i].location - g_imgBase;
                len = sprintf_s(line, "  [IMAGE RVA 0x%llX] 0x%llX -> 0x%llX\r\n",
                                rva, g_bpHits[i].location, g_bpHits[i].target);
                WriteFile(f, line, len, &written, nullptr);
            }
        }
        
        WriteFile(f, "\r\n=== ENTITY HEAP HITS ===\r\n", 28, &written, nullptr);
        for (int i = 0; i < g_bpCount; i++) {
            if (g_bpHits[i].regionType[0] == 'E') { // ENTHEAP
                len = sprintf_s(line, "  [ENTHEAP] 0x%llX -> 0x%llX\r\n",
                                g_bpHits[i].location, g_bpHits[i].target);
                WriteFile(f, line, len, &written, nullptr);
            }
        }
        
        WriteFile(f, "\r\n=== OTHER HEAP HITS ===\r\n", 27, &written, nullptr);
        for (int i = 0; i < g_bpCount; i++) {
            if (g_bpHits[i].regionType[0] == 'H') { // HEAP
                len = sprintf_s(line, "  [HEAP] 0x%llX -> 0x%llX\r\n",
                                g_bpHits[i].location, g_bpHits[i].target);
                WriteFile(f, line, len, &written, nullptr);
            }
        }
        
        // Summary
        len = sprintf_s(line, "\r\n=== SUMMARY ===\r\nTotal hits: %d\r\n", g_bpCount);
        WriteFile(f, line, len, &written, nullptr);
        
        int nStatic = 0, nImage = 0, nEntHeap = 0, nHeap = 0;
        for (int i = 0; i < g_bpCount; i++) {
            switch (g_bpHits[i].regionType[0]) {
                case 'S': nStatic++; break;
                case 'I': nImage++; break;
                case 'E': nEntHeap++; break;
                case 'H': nHeap++; break;
            }
        }
        len = sprintf_s(line, "  STATIC: %d (THESE ARE THE ACCESSOR CHAIN)\r\n"
                              "  IMAGE: %d\r\n  ENTHEAP: %d\r\n  HEAP: %d\r\n",
                        nStatic, nImage, nEntHeap, nHeap);
        WriteFile(f, line, len, &written, nullptr);
        
        // Next steps
        const char* next = "\r\n=== NEXT STEPS ===\r\n"
            "1. Look at STATIC hits - these are pointers in global data\r\n"
            "2. For each STATIC hit, find what structure it's in (check IDA at that RVA)\r\n"
            "3. Trace back: what points to THAT structure?\r\n"
            "4. Build the chain: GlobalPtr -> Manager -> List -> Entity\r\n"
            "5. Hardcode the offsets - NO MORE HEAP SCANNING\r\n";
        WriteFile(f, next, (DWORD)strlen(next), &written, nullptr);
        
        CloseHandle(f);
    }
    
    k = 0;
    add("BPSCAN: Done! ");
    logBuf[k++] = '0' + (g_bpCount / 100) % 10;
    logBuf[k++] = '0' + (g_bpCount / 10) % 10;
    logBuf[k++] = '0' + g_bpCount % 10;
    add(" hits. Results in C:\\Users\\Public\\gw2_backpointers.txt\n");
    logBuf[k] = 0;
    OILog(logBuf);
}

// =============================================================================
// VTABLE PATCH SYSTEM - The TRUE pro approach  
// Patch CharObj vtable entry to capture ALL entities as game processes them
// =============================================================================

// Hook state (g_hookInstalled declared above)
static void* g_origCharObjFunc = nullptr;
static int g_hookedSlot = -1;

// Vtable patch helper (same approach as Present hook)
static bool PatchVtableSlot(void** slot, void* newFunc, void** origFunc)
{
    DWORD oldProtect = 0;
    if (!VirtualProtect(slot, sizeof(void*), PAGE_READWRITE, &oldProtect)) {
        OILog("HOOK: VirtualProtect failed");
        return false;
    }
    *origFunc = *slot;
    *slot = newFunc;
    VirtualProtect(slot, sizeof(void*), oldProtect, &oldProtect);
    return true;
}

// Hook callback - called when game invokes CharObj virtual function
// 'this' pointer (rcx on x64) = entity pointer
static void __fastcall CharObjVtableHook(void* thisPtr)
{
    static uint32_t hookCalls = 0;
    static DWORD lastLog = 0;
    
    hookCalls++;
    
    // Add this entity to our list
    __try {
        uint64_t entPtr = (uint64_t)thisPtr;
        if (entPtr) {
            TryAddEntity(entPtr);
        }
    }
    __except(EXCEPTION_EXECUTE_HANDLER) {
        // Entity may have been deallocated - ignore
    }
    
    // Log hook stats periodically
    DWORD now = GetTickCount();
    if (now - lastLog > 5000) {
        lastLog = now;
        char buf[80];
        sprintf_s(buf, "VHOOK: calls=%u total_ent=%d", hookCalls, g_nEnt);
        OILog(buf);
    }
    
    // Call original function
    if (g_origCharObjFunc) {
        ((void(__fastcall*)(void*))g_origCharObjFunc)(thisPtr);
    }
}

// Install vtable patch on CharObj
bool InstallEntityHook()
{
    if (g_hookInstalled) return true;
    
    // Initialize addresses if not set (hook can work without F2 scan)
    if (!g_imgBase) {
        HMODULE exe = GetModuleHandleA(NULL);
        if (!exe) {
            OILog("VHOOK: GetModuleHandle failed");
            return false;
        }
        g_imgBase = (uint64_t)exe;
        g_vaCharObj = g_imgBase + RVA_CHAROBJ_VT;
        g_vaTI = g_imgBase + RVA_CHAROBJ_TI;
        
        char buf[80];
        sprintf_s(buf, "VHOOK: Init imgBase=0x%llX vaCharObj=0x%llX", g_imgBase, g_vaCharObj);
        OILog(buf);
    }
    
    if (!g_vaCharObj) return false;
    
    // The vtable is at g_vaCharObj (already runtime address)
    void** vtable = (void**)g_vaCharObj;
    
    char logbuf[128];
    sprintf_s(logbuf, "VHOOK: vtable=0x%llX slot[1]=0x%p slot[2]=0x%p", 
        g_vaCharObj, vtable[1], vtable[2]);
    OILog(logbuf);
    
    // Try to patch slot 1 (usually a frequently-called virtual method)
    // Slot 0 is destructor - skip it
    for (int slot = 1; slot <= 4; slot++) {
        void* funcPtr = vtable[slot];
        if (!funcPtr) continue;
        
        // Validate it's a reasonable function pointer
        uint64_t addr = (uint64_t)funcPtr;
        if (addr < g_imgBase || addr > g_imgBase + 0x10000000) continue;
        
        sprintf_s(logbuf, "VHOOK: Trying slot[%d]=0x%p", slot, funcPtr);
        OILog(logbuf);
        
        if (PatchVtableSlot(&vtable[slot], (void*)&CharObjVtableHook, &g_origCharObjFunc)) {
            g_hookInstalled = true;
            g_hookedSlot = slot;
            sprintf_s(logbuf, "VHOOK: SUCCESS! Hooked slot[%d], orig=0x%p", slot, g_origCharObjFunc);
            OILog(logbuf);
            return true;
        }
    }
    
    OILog("VHOOK: Failed to patch any vtable slot");
    return false;
}

// Try to install hook (called periodically until successful)
void TryInstallHook()
{
    static DWORD lastTry = 0;
    
    if (g_hookInstalled) return;
    
    DWORD now = GetTickCount();
    if (now - lastTry < 5000) return;
    lastTry = now;
    
    InstallEntityHook();
}


} // namespace ent
