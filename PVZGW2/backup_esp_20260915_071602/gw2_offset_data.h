// ---------------------------------------------------------------------------
// GW2 dump offsets — GENERATED TABLES (from gen_gw2_offsets.js).
// Source: gw2_image_dump_0.bin @ 0x140000000, Ghidra analysis (93,884 funcs).
// Regenerate: node C:\Tools\gen_gw2_offsets.js
// ---------------------------------------------------------------------------
#pragma once

namespace gw2 {
namespace gen {

// ---- named function RVAs (verified vs gw2_dump_functions.txt) --------------
namespace fn {
    constexpr uintptr_t bi_windup           = 0x0201FD0; // zlib inflate support
    constexpr uintptr_t __chkstk            = 0x154A8F0;
    constexpr uintptr_t __delayLoadHelper2  = 0x18272E4;
    constexpr uintptr_t _ValidateImageBase  = 0x154B0F0;
    constexpr uintptr_t atexit              = 0x154A524;
    // 6,567 import thunks + 87k unnamed functions:
    //   C:\Users\Public\gw2_dump_functions.txt (address name pairs)
} // namespace fn

// ---- Frostbite reflection strings (PERMANENT RVAs, data section) ----------
// The game's own property/type names. Each is referenced from a class
// descriptor table; xref the address in Ghidra to find the owning structure.
// Full xref map: C:\Users\Public\gw2_leads.txt (833 categorized leads).
// Full 6,975-entry table: C:\Users\Public\gw2_gen_offsets.inc
namespace strings {

#define GW2_STR(name, rva, tag) \
    constexpr uintptr_t str_##name = rva; // [tag]

    // ---- AIM / WEAPONS ----
    GW2_STR(SnapAim,         0x2274590, AIM)    // "SnapAim"
    GW2_STR(AutoAim,         0x2348618, AIM)    // "AutoAim"
    GW2_STR(accuracy,        0x20A9DA0, AIM)    // "accuracy"
    GW2_STR(MotionFilterAim, 0x20D72C0, AIM)    // "MotionFilterAim"
    GW2_STR(RateOfFire,      0x22EC0F0, FIRE)   // "RateOfFire"
    GW2_STR(SoldierFire,     0x22E6450, FIRE)   // "SoldierFire"
    GW2_STR(CheckTimeToFire, 0x231DE58, FIRE)   // "CheckTimeToFire"
    GW2_STR(fltSingleFire,   0x22EC0B8, FIRE)   // "fltSingleFire"
    GW2_STR(fltBurstFire,    0x22EC0C8, FIRE)   // "fltBurstFire"
    GW2_STR(fltAutomaticFire,0x22EC128, FIRE)   // "fltAutomaticFire"
    GW2_STR(Trigger,         0x21F3028, FIRE)   // "Trigger"
    GW2_STR(EnableFriendlyFire, 0x2264DF8, FIRE) // "EnableFriendlyFire"

    // ---- HEALTH ----
    GW2_STR(Health,            0x223A7E8, HEALTH)   // "Health"
    GW2_STR(MaxHealth,         0x2262C90, HEALTH)   // "MaxHealth"
    GW2_STR(MaxShieldHealth,   0x226D138, HEALTH)   // "MaxShieldHealth"
    GW2_STR(Damage,            0x25E1E4,  HEALTH)   // "Damage"
    GW2_STR(DisableRegenHealth,0x264D98,  HEALTH)   // "DisableRegenerateHealth"

    // ---- ENTITY TYPES (class descriptors; instances live on heap) ----
    GW2_STR(HumanPlayerEntity, 0x25E1F0, ENTITY)    // "HumanPlayerEntityData"
    GW2_STR(HumanPlayerProxy,  0x25E198, ENTITY)    // "HumanPlayerProxyEntityData"
    GW2_STR(PlayerIterator,    0x25F4B0, ENTITY)    // "PlayerIteratorEntityData"
    GW2_STR(LocalPlayerEvent,  0x25F4E8, ENTITY)    // "LocalPlayerEventEntityData"
    GW2_STR(TargetPos,         0x0D7688, ENTITY)    // "TargetPos"
    GW2_STR(TargetOrientation, 0x0E6388, ENTITY)    // "TargetOrientation"
    GW2_STR(posPlayer,         0x0CAC08, ENTITY)    // "posPlayer"

    // ---- CAMERA / VIEW ----
    GW2_STR(hkxCamera,       0x207E338, CAMERA) // "hkxCamera" (Havok cam)
    GW2_STR(FovIsHorizontal, 0x20CE118, CAMERA) // "FovIsHorizontal"
    GW2_STR(FovInDegrees,    0x20CE128, CAMERA) // "FovInDegrees"

    // ---- MOVEMENT ----
    GW2_STR(transformSet,    0x2082060, MOVEMENT) // "transformSet" (Havok)
    GW2_STR(transforms,      0x099F00, MOVEMENT) // "transforms"

#undef GW2_STR
} // namespace strings

// ---- Usage ---------------------------------------------------------------
// Reflection strings are the entry into Frostbite's type system:
//   1. xref(str_SnapAim) in Ghidra -> data table referencing it = class desc
//   2. class desc gives field layout of the owning entity component
//   3. component instances live on the heap -> runtime scan by vtable
// Payload runtime usage:
//   const char* s = (const char*)gw2::addr(gw2::gen::strings::str_AutoAim);

} // namespace gen
} // namespace gw2
