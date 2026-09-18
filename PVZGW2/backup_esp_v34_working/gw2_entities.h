// ---------------------------------------------------------------------------
// gw2_entities.h — live entity layer (dump-verified structure map).
//
// Verified across two full-heap dumps (2026-08-31, run1/run2 same-session
// cross-checks; see C:\Tools\gw2_chain*.js analysis):
//
//   charObj   vtable RVA 0x228B380 — 37/38 live per match, 1:1 with the
//             hknpCharacterProxy set. Frostbite character entity.
//               +0x18 u32 marker 0x100F          +0x48 TypeInfo RVA 0x228B658
//               +0x50 -> hknpCharacterProxy      +0x58 -> state
//               +0x68 -> component array         +0x8C float 100.0 (NOT hp)
//   state     vtable RVA 0x228B948 — 1:1 with charObjs, bidirectional link.
//               +0x18 -> hknpCharacterStateManager
//               +0x120 -> per-char object (two classes: RVA 0x2327CC0 /
//                         0x222DF920 — per-character, unnamed so far)
//               +0x128 -> back to charObj        +0x130 -> team registry
//   teamReg   vtable RVA 0x228BC58 — exactly 2 live (the two teams).
//   container vtable RVA 0x236B638 — 215 live "WeaponFiring" runtime objects.
//               FireLogic sub-blocks at +0x70/+0x190/+0x220/+0x2B0/+0x340
//               (sub vtables RVA 0x226E218 / 0x2270188 / 0x226E0F0).
//               No FFD pointer inside — FFD link goes via ebx GUID tables.
//
// Collection happens during the F2 scan (scanner pass A); the live layer
// refreshes positions/teams per frame, detects the local player as the
// entity nearest the captured camera (3rd-person cam orbits the player),
// and watches every container for runtime changes — the local player's
// weapon container is the one whose FireLogic state changes while HE fires.
// ---------------------------------------------------------------------------
#pragma once
#include <cstdint>

namespace ent {

// scan-thread collection (called from gw2_scanner.cpp)
void BeginCollect(uint64_t imgBase, bool reset = true); // resolves VAs; reset=false = incremental
void AddCharObj(uint64_t va);             // vtable hit — shape-validated here
void AddTeamReg(uint64_t va);
void AddContainer(uint64_t va);
void EndCollect();

// resolved vtable VAs for the scanner's per-qword compare
uint64_t CharObjVT();
uint64_t TeamRegVT();
uint64_t ContainerVT();

// Present-thread live layer
void OnFrame();                           // positions, factions, local, watcher
int  DebugList(float* wx, float* wy, float* wz, float* dist,
               int* team, int* isLocal, int* ai, int* fresh, int maxN);
// Live entity rows. wx/wy/wz = world position of the entity's FEET (proxy
// origin); callers project anchors themselves (head +1.7 m for boxes,
// torso +1.2 m for the triggerbot column) via vproj::WorldToScreen.
// fresh[i]: 1 = position moved within the last ~4 s (live entity); 0 =
// frozen (dead/despawned slot — do NOT treat as a target).
// team[i]: 0 plants / 1 zombies, from the class string
// ("Gameplay/Soldiers/PhysicsData/<Class>Physics", [30]=='Z'/'P'; the
// state+0x130 teamReg link is PvP-only and WRONG in Garden Ops — dump7).
// ai[i]: 1 = NPC ("_AI"/"Turret" in the class name).
// (tom-pearl dump: E1+0xb8 zeros/denormals, state+0x120 flags).
int  EntityCount();                       // live entity rows (diagnostics)
const char* StatusText();
void DumpLocal();                         // F5: full local-entity hex dump

} // namespace ent
