#pragma once
// gatyware (1.1 Mono) settings bridge.
//
// Resolves `gatyware.State` (static class) and caches MonoClassField* pointers,
// then pushes the native `var` (c_variable) values into C# State every frame.
//
// bool / int / float  -> mono_field_static_set_value (value copied from a local
//                        of the matching C type).
// UnityEngine.Color   -> mono_vtable_get_static_field_data (raw address of the
//                        16-byte struct) + memcpy of 4 floats (ApplyCustomFov
//                        pattern, mono_bridge.h lines 171-174).
// KeyCode / string     -> DEFERRED (Phase 2b). Not synced here.

#include "gatyware_bridge.h"   // g_image, g_runtime_class, log
#include "../settings/variables.h"
#include <cstring>             // memcpy

namespace gatyware {

inline void* st_class   = nullptr;
inline void* st_vtable  = nullptr;
inline bool  st_resolved = false;

// ── Field cache (MonoClassField*) ────────────────────────────────────────
inline void* f_AimbotOn            = nullptr;
inline void* f_AimbotFov           = nullptr;
inline void* f_AimbotSmoothness    = nullptr;
inline void* f_AimbotVisCheck      = nullptr;
inline void* f_AimbotFriendly      = nullptr;
inline void* f_AimbotTargetPlayers = nullptr;
inline void* f_AimbotTargetZombies = nullptr;
inline void* f_AimbotDrawFov       = nullptr;
inline void* f_AimbotLimb          = nullptr;

inline void* f_SilentAimOn         = nullptr;
inline void* f_SilentAimHitChance  = nullptr;
inline void* f_SilentAimFov        = nullptr;
inline void* f_SilentAimVisCheck   = nullptr;
inline void* f_SilentAimFriendly   = nullptr;
inline void* f_SilentAimTargetPlayers = nullptr;
inline void* f_SilentAimTargetZombies = nullptr;
inline void* f_SilentAimHitLimb    = nullptr;
inline void* f_SilentAimDrawFov    = nullptr;

inline void* f_TriggerBot          = nullptr;
inline void* f_TriggerBotDelay     = nullptr;

inline void* f_RecoilMult          = nullptr;
inline void* f_SpreadMult          = nullptr;
inline void* f_SwayMult            = nullptr;
inline void* f_MeleeReach          = nullptr;
inline void* f_MeleeReachExtra     = nullptr;

inline void* f_PlayerChams         = nullptr;
inline void* f_ZombieChams         = nullptr;
inline void* f_SelfChams           = nullptr;
inline void* f_WeaponChams         = nullptr;

inline void* f_PlayerEsp           = nullptr;
inline void* f_ZombieEsp           = nullptr;
inline void* f_ItemEsp             = nullptr;
inline void* f_VehicleEsp          = nullptr;
inline void* f_AnimalEsp           = nullptr;
inline void* f_StorageEsp          = nullptr;
inline void* f_AirdropEsp          = nullptr;
inline void* f_BedEsp              = nullptr;
inline void* f_GenEsp              = nullptr;
inline void* f_TurretEsp           = nullptr;
inline void* f_GrenadeEsp          = nullptr;
inline void* f_BulletEsp           = nullptr;

inline void* f_CustomFov           = nullptr;
inline void* f_CustomFovDeg        = nullptr;
inline void* f_NoFlash             = nullptr;
inline void* f_NightVision         = nullptr;
inline void* f_NoFog               = nullptr;
inline void* f_NoGrayscale         = nullptr;
inline void* f_CustomTime          = nullptr;
inline void* f_CustomTimeValue     = nullptr;

// ── Color field cache + raw addresses (mono_vtable_get_static_field_data) ──
inline void*  f_PlayerChamsVis     = nullptr;
inline float* a_PlayerChamsVis     = nullptr;
inline void*  f_PlayerChamsNonVis  = nullptr;
inline float* a_PlayerChamsNonVis  = nullptr;
inline void*  f_ZombieChamsVis     = nullptr;
inline float* a_ZombieChamsVis     = nullptr;
inline void*  f_ZombieChamsNonVis  = nullptr;
inline float* a_ZombieChamsNonVis  = nullptr;
inline void*  f_SelfChamsColor     = nullptr;
inline float* a_SelfChamsColor     = nullptr;

// ── 1.1-only feature field cache ──────────────────────────────────────────
// Automation
inline void* f_AutoFarmOn          = nullptr;
inline void* f_AutoFarmHarvestRadius = nullptr;
inline void* f_AutoFarmActionDelay = nullptr;
inline void* f_AutoFarmAutoReplant = nullptr;
inline void* f_AutoFarmAutoStore   = nullptr;
inline void* f_AutoFarmAutoCraft   = nullptr;
inline void* f_AutoFarmAutoEquipSeed = nullptr;
inline void* f_AutoFarmWaterOn     = nullptr;
inline void* f_AutoFishOn          = nullptr;
inline void* f_AutoFishReelDelay   = nullptr;
inline void* f_AutoForge           = nullptr;
inline void* f_AutoForgeRadius     = nullptr;
inline void* f_AutoForgeDelay      = nullptr;
inline void* f_AutoJoinOn          = nullptr;
inline void* f_AutoJoinPort        = nullptr;
inline void* f_AutoJoinPlayerLimit = nullptr;
inline void* f_AutoJoinMaxPlayers  = nullptr;
inline void* f_AutoJoinRetryDelay  = nullptr;
inline void* f_AutoPickupOn        = nullptr;
inline void* f_AutoPickupDist      = nullptr;
inline void* f_AutoPickupSpeed     = nullptr;
inline void* f_AutoPickupSkipEmpty = nullptr;
// Misc tools
inline void* f_ItemSpawnerOpen     = nullptr;
inline void* f_EntityInspectorOn   = nullptr;
inline void* f_StorageViewerOn     = nullptr;
inline void* f_HwidChangerOn       = nullptr;
// Spinbot
inline void* f_Spinbot             = nullptr;
inline void* f_SpinType            = nullptr;
inline void* f_SpinShow            = nullptr;
// Vehicle fly / freecam
inline void* f_VehicleFlyOn        = nullptr;
inline void* f_VehicleFlySpeed     = nullptr;
inline void* f_VehicleFlyStyle     = nullptr;
inline void* f_VehicleDamageOff    = nullptr;
inline void* f_FreeCamOn           = nullptr;
inline void* f_FreeCamSpeed        = nullptr;
// Footsteps
inline void* f_Footsteps           = nullptr;
inline void* f_FootstepShape       = nullptr;
inline void* f_FootstepColor       = nullptr;
inline float* a_FootstepColor      = nullptr;
inline void* f_FootstepSpin        = nullptr;
inline void* f_FootstepSpinSpd     = nullptr;
inline void* f_FootstepLifetime    = nullptr;
inline void* f_FootstepSize        = nullptr;
// Damage numbers
inline void* f_DamageNumbers       = nullptr;
inline void* f_DmgNumLifetime      = nullptr;
inline void* f_DmgNumFontSize      = nullptr;
inline void* f_DmgNumCustomColor   = nullptr;
inline void* f_DmgNumColor         = nullptr;
inline float* a_DmgNumColor        = nullptr;
// Crosshair
inline void* f_CrosshairOn         = nullptr;
inline void* f_CrosshairType       = nullptr;
inline void* f_CrosshairSize       = nullptr;
inline void* f_CrosshairThick      = nullptr;
inline void* f_CrosshairColor      = nullptr;
inline float* a_CrosshairColor     = nullptr;
inline void* f_CrosshairSpin       = nullptr;
inline void* f_CrosshairSpinSpd    = nullptr;
// Misc visuals / world
inline void* f_NoPain              = nullptr;
inline void* f_NoHallucination     = nullptr;
inline void* f_UnlockPerspective   = nullptr;
inline void* f_Watermark           = nullptr;
inline void* f_InfoBar             = nullptr;
inline void* f_ForceCompass        = nullptr;
inline void* f_ForceMap            = nullptr;
inline void* f_OverrideSky         = nullptr;
inline void* f_SkyColor            = nullptr;
inline float* a_SkyColor           = nullptr;
inline void* f_OverrideSun         = nullptr;
inline void* f_SunColor            = nullptr;
inline float* a_SunColor           = nullptr;
inline void* f_OverrideCloud       = nullptr;
inline void* f_CloudColor          = nullptr;
inline float* a_CloudColor         = nullptr;
inline void* f_OverrideCloudRim    = nullptr;
inline void* f_CloudRimColor       = nullptr;
inline float* a_CloudRimColor      = nullptr;
// Map / player relation
inline void* f_FriendColor         = nullptr;
inline float* a_FriendColor        = nullptr;
inline void* f_EnemyColor          = nullptr;
inline float* a_EnemyColor         = nullptr;
inline void* f_MapShowAllPlayers   = nullptr;
inline void* f_MapShowAllMarkers   = nullptr;
// Reach / placement
inline void* f_FarReach            = nullptr;
inline void* f_FarReachDist        = nullptr;
inline void* f_PickupThroughWalls  = nullptr;
inline void* f_PickupDistance      = nullptr;
inline void* f_ExtendNearbyRadius  = nullptr;
inline void* f_NearbyRadius        = nullptr;
inline void* f_NearbyThroughWalls  = nullptr;
inline void* f_IgnoreBarricadeErrors = nullptr;
inline void* f_IgnoreStructureErrors = nullptr;
inline void* f_PlaceAnywhere       = nullptr;
inline void* f_CustomBuildOffset   = nullptr;
inline void* f_BuildOffsetX        = nullptr;
inline void* f_BuildOffsetY        = nullptr;
inline void* f_BuildOffsetZ        = nullptr;
inline void* f_SalvageMultiplier   = nullptr;
// Weapon extras
inline void* f_AutoSemiBurst       = nullptr;
inline void* f_DamageFlinchMult    = nullptr;
inline void* f_DisableBino         = nullptr;
inline void* f_DisableScope        = nullptr;
inline void* f_ExtendBallisticRange = nullptr;
inline void* f_ExtraBallisticSteps = nullptr;
inline void* f_IgnoreLeaveTimer    = nullptr;
inline void* f_InstantAim          = nullptr;
inline void* f_NoBallistics        = nullptr;
inline void* f_ForceHeadshot       = nullptr;
inline void* f_BulletSelf          = nullptr;
inline void* f_BulletOthers        = nullptr;
inline void* f_BulletSelfColor     = nullptr;
inline float* a_BulletSelfColor    = nullptr;
inline void* f_BulletOtherColor    = nullptr;
inline float* a_BulletOtherColor   = nullptr;
inline void* f_BulletTrail         = nullptr;
// Quest exploits
inline void* f_QuestMaxML          = nullptr;
inline void* f_QuestAlcohol        = nullptr;
inline void* f_QuestLumberjack     = nullptr;
inline void* f_QuestVoucher        = nullptr;
inline void* f_QuestXmas2024       = nullptr;
inline void* f_QuestXmas2025Factory = nullptr;
inline void* f_QuestXmas2025Penguins = nullptr;
// Anti-spy
inline void* f_SpyMode             = nullptr;
inline void* f_SpyToastEnabled     = nullptr;

// ── Low-level writers ─────────────────────────────────────────────────────
inline void set_bool(void* f, bool v)  { if (f && st_vtable) mono::field_static_set_value(st_vtable, f, &v); }
inline void set_int(void* f, int v)    { if (f && st_vtable) mono::field_static_set_value(st_vtable, f, &v); }
inline void set_float(void* f, float v){ if (f && st_vtable) mono::field_static_set_value(st_vtable, f, &v); }
inline void set_color(float* addr, void* f, const float* rgba) {
    if (addr) { memcpy(addr, rgba, 16); }
    else if (f && st_vtable) { float c[4] = { rgba[0], rgba[1], rgba[2], rgba[3] }; mono::field_static_set_value(st_vtable, f, c); }
}

// Resolve a plain (bool/int/float) field.
inline void* F(const char* name) {
    return st_class ? mono::class_get_field_from_name(st_class, name) : nullptr;
}
// Resolve a Color field; force static-init, then cache the raw 16-byte address.
inline void* FC(const char* name, float** addr) {
    void* f = F(name);
    if (addr) *addr = nullptr;
    if (!f) return nullptr;
    float buf[4] = { 0.f, 0.f, 0.f, 0.f };
    mono::field_static_get_value(st_vtable, f, buf);   // runtime-init the static class
    float* p = nullptr;
    if (mono::p_mono_vtable_get_static_field_data)
        p = (float*)mono::p_mono_vtable_get_static_field_data(st_vtable, f);
    if (addr) *addr = p;
    return f;
}

// Resolve State class + vtable + all CORE field pointers. One-shot.
inline bool resolve_state() {
    if (st_resolved) return st_class != nullptr;
    st_resolved = true;

    if (!g_image) { log("gatyware.settings: no assembly image"); return false; }
    st_class = mono::class_from_name(g_image, "gatyware", "State");
    if (!st_class) { log("gatyware.settings: State class not found"); return false; }
    st_vtable = mono::class_vtable(mono::g_domain, st_class);
    if (!st_vtable) { log("gatyware.settings: State vtable null"); return false; }

    f_AimbotOn            = F("AimbotOn");
    f_AimbotFov           = F("AimbotFov");
    f_AimbotSmoothness    = F("AimbotSmoothness");
    f_AimbotVisCheck      = F("AimbotVisCheck");
    f_AimbotFriendly      = F("AimbotFriendly");
    f_AimbotTargetPlayers = F("AimbotTargetPlayers");
    f_AimbotTargetZombies = F("AimbotTargetZombies");
    f_AimbotDrawFov       = F("AimbotDrawFov");
    f_AimbotLimb          = F("AimbotLimb");

    f_SilentAimOn         = F("SilentAimOn");
    f_SilentAimHitChance  = F("SilentAimHitChance");
    f_SilentAimFov        = F("SilentAimFov");
    f_SilentAimVisCheck   = F("SilentAimVisCheck");
    f_SilentAimFriendly   = F("SilentAimFriendly");
    f_SilentAimTargetPlayers = F("SilentAimTargetPlayers");
    f_SilentAimTargetZombies = F("SilentAimTargetZombies");
    f_SilentAimHitLimb    = F("SilentAimHitLimb");
    f_SilentAimDrawFov    = F("SilentAimDrawFov");

    f_TriggerBot          = F("TriggerBot");
    f_TriggerBotDelay     = F("TriggerBotDelay");

    f_RecoilMult          = F("RecoilMult");
    f_SpreadMult          = F("SpreadMult");
    f_SwayMult            = F("SwayMult");
    f_MeleeReach          = F("MeleeReach");
    f_MeleeReachExtra     = F("MeleeReachExtra");

    f_PlayerChams         = F("PlayerChams");
    f_ZombieChams         = F("ZombieChams");
    f_SelfChams           = F("SelfChams");
    f_WeaponChams         = F("WeaponChams");

    f_PlayerChamsVis      = FC("PlayerChamsVis",    &a_PlayerChamsVis);
    f_PlayerChamsNonVis   = FC("PlayerChamsNonVis", &a_PlayerChamsNonVis);
    f_ZombieChamsVis      = FC("ZombieChamsVis",    &a_ZombieChamsVis);
    f_ZombieChamsNonVis   = FC("ZombieChamsNonVis", &a_ZombieChamsNonVis);
    f_SelfChamsColor      = FC("SelfChamsColor",    &a_SelfChamsColor);

    f_PlayerEsp           = F("PlayerEsp");
    f_ZombieEsp           = F("ZombieEsp");
    f_ItemEsp             = F("ItemEsp");
    f_VehicleEsp          = F("VehicleEsp");
    f_AnimalEsp           = F("AnimalEsp");
    f_StorageEsp          = F("StorageEsp");
    f_AirdropEsp          = F("AirdropEsp");
    f_BedEsp              = F("BedEsp");
    f_GenEsp              = F("GenEsp");
    f_TurretEsp           = F("TurretEsp");
    f_GrenadeEsp          = F("GrenadeEsp");
    f_BulletEsp           = F("BulletEsp");

    f_CustomFov           = F("CustomFov");
    f_CustomFovDeg        = F("CustomFovDeg");
    f_NoFlash             = F("NoFlash");
    f_NightVision         = F("NightVision");
    f_NoFog               = F("NoFog");
    f_NoGrayscale         = F("NoGrayscale");
    f_CustomTime          = F("CustomTime");
    f_CustomTimeValue     = F("CustomTimeValue");

    f_AutoFarmOn          = F("AutoFarmOn");
    f_AutoFarmHarvestRadius = F("AutoFarmHarvestRadius");
    f_AutoFarmActionDelay = F("AutoFarmActionDelay");
    f_AutoFarmAutoReplant = F("AutoFarmAutoReplant");
    f_AutoFarmAutoStore   = F("AutoFarmAutoStore");
    f_AutoFarmAutoCraft   = F("AutoFarmAutoCraft");
    f_AutoFarmAutoEquipSeed = F("AutoFarmAutoEquipSeed");
    f_AutoFarmWaterOn     = F("AutoFarmWaterOn");
    f_AutoFishOn          = F("AutoFishOn");
    f_AutoFishReelDelay   = F("AutoFishReelDelay");
    f_AutoForge           = F("AutoForge");
    f_AutoForgeRadius     = F("AutoForgeRadius");
    f_AutoForgeDelay      = F("AutoForgeDelay");
    f_AutoJoinOn          = F("AutoJoinOn");
    f_AutoJoinPort        = F("AutoJoinPort");
    f_AutoJoinPlayerLimit = F("AutoJoinPlayerLimit");
    f_AutoJoinMaxPlayers  = F("AutoJoinMaxPlayers");
    f_AutoJoinRetryDelay  = F("AutoJoinRetryDelay");
    f_AutoPickupOn        = F("AutoPickupOn");
    f_AutoPickupDist      = F("AutoPickupDist");
    f_AutoPickupSpeed     = F("AutoPickupSpeed");
    f_AutoPickupSkipEmpty = F("AutoPickupSkipEmpty");
    f_ItemSpawnerOpen     = F("ItemSpawnerOpen");
    f_EntityInspectorOn   = F("EntityInspectorOn");
    f_StorageViewerOn     = F("StorageViewerOn");
    f_HwidChangerOn       = F("HwidChangerOn");
    f_Spinbot             = F("Spinbot");
    f_SpinType            = F("SpinType");
    f_SpinShow            = F("SpinShow");
    f_VehicleFlyOn        = F("VehicleFlyOn");
    f_VehicleFlySpeed     = F("VehicleFlySpeed");
    f_VehicleFlyStyle     = F("VehicleFlyStyle");
    f_VehicleDamageOff    = F("VehicleDamageOff");
    f_FreeCamOn           = F("FreeCamOn");
    f_FreeCamSpeed        = F("FreeCamSpeed");
    f_Footsteps           = F("Footsteps");
    f_FootstepShape       = F("FootstepShape");
    f_FootstepColor       = FC("FootstepColor", &a_FootstepColor);
    f_FootstepSpin        = F("FootstepSpin");
    f_FootstepSpinSpd     = F("FootstepSpinSpd");
    f_FootstepLifetime    = F("FootstepLifetime");
    f_FootstepSize        = F("FootstepSize");
    f_DamageNumbers       = F("DamageNumbers");
    f_DmgNumLifetime      = F("DmgNumLifetime");
    f_DmgNumFontSize      = F("DmgNumFontSize");
    f_DmgNumCustomColor   = F("DmgNumCustomColor");
    f_DmgNumColor         = FC("DmgNumColor", &a_DmgNumColor);
    f_CrosshairOn         = F("CrosshairOn");
    f_CrosshairType       = F("CrosshairType");
    f_CrosshairSize       = F("CrosshairSize");
    f_CrosshairThick      = F("CrosshairThick");
    f_CrosshairColor      = FC("CrosshairColor", &a_CrosshairColor);
    f_CrosshairSpin       = F("CrosshairSpin");
    f_CrosshairSpinSpd    = F("CrosshairSpinSpd");
    f_NoPain              = F("NoPain");
    f_NoHallucination     = F("NoHallucination");
    f_UnlockPerspective   = F("UnlockPerspective");
    f_Watermark           = F("Watermark");
    f_InfoBar             = F("InfoBar");
    f_ForceCompass        = F("ForceCompass");
    f_ForceMap            = F("ForceMap");
    f_OverrideSky         = F("OverrideSky");
    f_SkyColor            = FC("SkyColor", &a_SkyColor);
    f_OverrideSun         = F("OverrideSun");
    f_SunColor            = FC("SunColor", &a_SunColor);
    f_OverrideCloud       = F("OverrideCloud");
    f_CloudColor          = FC("CloudColor", &a_CloudColor);
    f_OverrideCloudRim    = F("OverrideCloudRim");
    f_CloudRimColor       = FC("CloudRimColor", &a_CloudRimColor);
    f_FriendColor         = FC("FriendColor", &a_FriendColor);
    f_EnemyColor          = FC("EnemyColor", &a_EnemyColor);
    f_MapShowAllPlayers   = F("MapShowAllPlayers");
    f_MapShowAllMarkers   = F("MapShowAllMarkers");
    f_FarReach            = F("FarReach");
    f_FarReachDist        = F("FarReachDist");
    f_PickupThroughWalls  = F("PickupThroughWalls");
    f_PickupDistance      = F("PickupDistance");
    f_ExtendNearbyRadius  = F("ExtendNearbyRadius");
    f_NearbyRadius        = F("NearbyRadius");
    f_NearbyThroughWalls  = F("NearbyThroughWalls");
    f_IgnoreBarricadeErrors = F("IgnoreBarricadeErrors");
    f_IgnoreStructureErrors = F("IgnoreStructureErrors");
    f_PlaceAnywhere       = F("PlaceAnywhere");
    f_CustomBuildOffset   = F("CustomBuildOffset");
    f_BuildOffsetX        = F("BuildOffsetX");
    f_BuildOffsetY        = F("BuildOffsetY");
    f_BuildOffsetZ        = F("BuildOffsetZ");
    f_SalvageMultiplier   = F("SalvageMultiplier");
    f_AutoSemiBurst       = F("AutoSemiBurst");
    f_DamageFlinchMult    = F("DamageFlinchMult");
    f_DisableBino         = F("DisableBino");
    f_DisableScope        = F("DisableScope");
    f_ExtendBallisticRange = F("ExtendBallisticRange");
    f_ExtraBallisticSteps = F("ExtraBallisticSteps");
    f_IgnoreLeaveTimer    = F("IgnoreLeaveTimer");
    f_InstantAim          = F("InstantAim");
    f_NoBallistics        = F("NoBallistics");
    f_ForceHeadshot       = F("ForceHeadshot");
    f_BulletSelf          = F("BulletSelf");
    f_BulletOthers        = F("BulletOthers");
    f_BulletSelfColor     = FC("BulletSelfColor", &a_BulletSelfColor);
    f_BulletOtherColor    = FC("BulletOtherColor", &a_BulletOtherColor);
    f_BulletTrail         = F("BulletTrail");
    f_QuestMaxML          = F("QuestMaxML");
    f_QuestAlcohol        = F("QuestAlcohol");
    f_QuestLumberjack     = F("QuestLumberjack");
    f_QuestVoucher        = F("QuestVoucher");
    f_QuestXmas2024       = F("QuestXmas2024");
    f_QuestXmas2025Factory = F("QuestXmas2025Factory");
    f_QuestXmas2025Penguins = F("QuestXmas2025Penguins");
    f_SpyMode             = F("SpyMode");
    f_SpyToastEnabled     = F("SpyToastEnabled");
    log("gatyware.settings: State resolved");
    return true;
}

inline bool g_rt_proved = false;

// One-shot round-trip proof: read back AimbotOn (write path) and invoke
// Runtime.Trace(string) (C#->native marker, writes C:\Users\Public\mc_debug.txt).
inline void prove_roundtrip() {
    if (g_rt_proved) return;
    g_rt_proved = true;

    if (f_AimbotOn && st_vtable) {
        bool b = false;
        mono::field_static_get_value(st_vtable, f_AimbotOn, &b);
        char msg[48];
        int i = 0;
        const char* pre = "gatyware: rt AimbotOn=";
        for (const char* p = pre; *p; p++) msg[i++] = *p;
        msg[i++] = b ? '1' : '0';
        msg[i] = 0;
        log(msg);
    } else {
        log("gatyware: rt AimbotOn unresolved");
    }

    if (g_runtime_class) {
        void* trace_m = mono::class_get_method_from_name(g_runtime_class, "Trace", 1);
        void* s = mono::string_new(mono::g_domain, "settings bridge OK");
        if (trace_m && s) {
            void* args[1] = { s };
            void* exc = nullptr;
            mono::runtime_invoke(trace_m, nullptr, args, &exc);
            if (exc) log("gatyware: rt Trace threw");
            else      log("gatyware: rt Trace invoked (see mc_debug.txt)");
        } else {
            log("gatyware: rt Trace/string_new unavailable");
        }
    } else {
        log("gatyware: rt no Runtime class");
    }
}

// Push native `var` -> C# State. No-ops until State is resolved.
inline void sync_settings() {
    if (!st_resolved) { if (!resolve_state()) return; }
    if (!st_class || !st_vtable || !var) return;

    // Aimbot
    set_bool (f_AimbotOn,            var->c_aimbot.aimbot);
    set_float(f_AimbotFov,           (float)var->c_aimbot.fov);
    set_float(f_AimbotSmoothness,    var->c_aimbot.smoothing / 100.0f);
    set_bool (f_AimbotVisCheck,      var->c_aimbot.vis_check);
    set_bool (f_AimbotFriendly,      var->c_aimbot.friendly);
    set_bool (f_AimbotTargetPlayers, var->c_aimbot.target_players);
    set_bool (f_AimbotTargetZombies, var->c_aimbot.target_zombies);
    set_bool (f_AimbotDrawFov,       var->c_aimbot.draw_fov);
    set_int  (f_AimbotLimb,          var->c_aimbot.limb_selection);

    // Silent aim
    set_bool (f_SilentAimOn,          var->c_silent.silent);
    set_int  (f_SilentAimHitChance,   var->c_silent.chance);
    set_float(f_SilentAimFov,         var->c_silent.silent_fov);
    set_bool (f_SilentAimVisCheck,    var->c_silent.vis_check);
    set_bool (f_SilentAimFriendly,    var->c_silent.skip_friendly);
    set_bool (f_SilentAimTargetPlayers, var->c_silent.target_players);
    set_bool (f_SilentAimTargetZombies, var->c_silent.target_zombies);
    set_int  (f_SilentAimHitLimb,     var->c_silent.limb_selection);
    set_bool (f_SilentAimDrawFov,     var->c_silent.draw_fov);

    // Triggerbot
    set_bool (f_TriggerBot,           var->c_trigger.enable_trigger);
    set_float(f_TriggerBotDelay,      var->c_trigger.delay);

    // Weapon (bool toggles -> C# float multipliers; 0 = off, 1 = stock)
    set_float(f_RecoilMult, var->c_weapon.no_recoil ? 0.0f : 1.0f);
    set_float(f_SpreadMult, var->c_weapon.no_spread ? 0.0f : 1.0f);
    set_float(f_SwayMult,   var->c_weapon.no_sway   ? 0.0f : 1.0f);
    set_bool (f_MeleeReach,      var->c_weapon.extended_melee);
    set_float(f_MeleeReachExtra, var->c_weapon.melee_range);

    // Chams master toggles
    set_bool (f_PlayerChams, var->c_self.player_chams);
    set_bool (f_ZombieChams, var->c_self.zombie_chams);
    set_bool (f_SelfChams,   var->c_self.self_chams);
    set_bool (f_WeaponChams, var->c_weapon.weapon_chams);

    // Chams colors (native has ONE vis/nonvis pair shared by player+zombie)
    set_color(a_PlayerChamsVis,    f_PlayerChamsVis,    var->c_self.chams_vis_color);
    set_color(a_PlayerChamsNonVis, f_PlayerChamsNonVis, var->c_self.chams_nonvis_color);
    set_color(a_ZombieChamsVis,    f_ZombieChamsVis,    var->c_self.chams_vis_color);
    set_color(a_ZombieChamsNonVis, f_ZombieChamsNonVis, var->c_self.chams_nonvis_color);
    set_color(a_SelfChamsColor,    f_SelfChamsColor,    var->c_self.self_chams_color);

    // ESP master toggles
    set_bool(f_PlayerEsp,  var->c_pesp.esp);
    set_bool(f_ZombieEsp,  var->c_zesp.esp);
    set_bool(f_ItemEsp,    var->c_iesp.esp);
    set_bool(f_VehicleEsp, var->c_vesp.esp);
    set_bool(f_AnimalEsp,  var->c_aesp.esp);
    set_bool(f_StorageEsp, var->c_sesp.esp);
    set_bool(f_AirdropEsp, var->c_desp.esp);
    set_bool(f_BedEsp,     var->c_besp.esp);
    set_bool(f_GenEsp,     var->c_gesp.esp);
    set_bool(f_TurretEsp,  var->c_tesp.esp);
    set_bool(f_GrenadeEsp, var->c_resp.esp);
    set_bool(f_BulletEsp,  var->c_fesp.esp);

    // Self / World
    set_bool (f_CustomFov,       var->c_self.custom_fov);
    set_float(f_CustomFovDeg,    var->c_self.fov_deg);
    set_bool (f_NoFlash,         var->c_self.no_flash);
    set_int  (f_NightVision,     var->c_self.night_vision);
    set_bool (f_NoFog,           var->c_self.no_fog);
    set_bool (f_NoGrayscale,     var->c_self.no_grayscale);
    set_bool (f_CustomTime,      var->c_world.custom_time);
    set_float(f_CustomTimeValue, var->c_world.time_value);

    // ── 1.1-only features ────────────────────────────────────────────────
    // AutoFarm
    set_bool (f_AutoFarmOn,          var->c_autofarm.auto_farm_on);
    set_float(f_AutoFarmHarvestRadius, var->c_autofarm.auto_farm_radius);
    set_float(f_AutoFarmActionDelay, var->c_autofarm.auto_farm_delay);
    set_bool (f_AutoFarmAutoReplant, var->c_autofarm.auto_farm_replant);
    set_bool (f_AutoFarmAutoStore,   var->c_autofarm.auto_farm_store);
    set_bool (f_AutoFarmAutoCraft,   var->c_autofarm.auto_farm_craft);
    set_bool (f_AutoFarmAutoEquipSeed, var->c_autofarm.auto_farm_equip_seed);
    set_bool (f_AutoFarmWaterOn,     var->c_autofarm.auto_farm_water);
    // AutoFish
    set_bool (f_AutoFishOn,          var->c_automation.auto_fish);
    set_float(f_AutoFishReelDelay,   var->c_automation.auto_fish_delay);
    // AutoForge
    set_bool (f_AutoForge,           var->c_automation.auto_forge);
    set_float(f_AutoForgeRadius,     var->c_automation.auto_forge_radius);
    set_float(f_AutoForgeDelay,      var->c_automation.auto_forge_delay);
    // AutoJoin
    set_bool (f_AutoJoinOn,          var->c_automation.auto_joiner);
    set_int  (f_AutoJoinPort,        var->c_automation.server_port);
    set_bool (f_AutoJoinPlayerLimit, var->c_automation.auto_join_limit);
    set_int  (f_AutoJoinMaxPlayers,  var->c_automation.auto_join_max);
    set_float(f_AutoJoinRetryDelay,  var->c_automation.auto_join_retry);
    // AutoPickup
    set_bool (f_AutoPickupOn,        var->c_automation.auto_pickup);
    set_float(f_AutoPickupDist,      var->c_automation.pickup_radius);
    set_float(f_AutoPickupSpeed,     var->c_automation.auto_pickup_speed);
    set_bool (f_AutoPickupSkipEmpty, var->c_automation.auto_pickup_skip_empty);
    // Misc tools
    set_bool (f_ItemSpawnerOpen,     var->c_misc.item_spawner);
    set_bool (f_EntityInspectorOn,   var->c_misc.entity_inspector);
    set_bool (f_StorageViewerOn,     var->c_misc.storage_viewer);
    set_bool (f_HwidChangerOn,       var->c_misc.hwid_changer);
    // Spinbot
    set_bool (f_Spinbot,             var->c_aimbot.spinbot);
    set_int  (f_SpinType,            var->c_aimbot.spin_type);
    set_bool (f_SpinShow,            var->c_aimbot.spin_show);
    // Vehicle fly / freecam
    set_bool (f_VehicleFlyOn,        var->c_movement.vehicle_fly);
    set_float(f_VehicleFlySpeed,     var->c_movement.fly_speed);
    set_int  (f_VehicleFlyStyle,     var->c_movement.vehicle_fly_style);
    set_bool (f_VehicleDamageOff,    var->c_movement.no_vehicle_dmg);
    set_bool (f_FreeCamOn,           var->c_fun.freecam);
    set_float(f_FreeCamSpeed,        var->c_fun.freecam_speed);
    // Footsteps
    set_bool (f_Footsteps,           var->c_self.footsteps);
    set_int  (f_FootstepShape,       var->c_self.footstep_shape);
    set_color(a_FootstepColor,       f_FootstepColor, var->c_self.footstep_color);
    set_bool (f_FootstepSpin,        var->c_self.footstep_spin);
    set_float(f_FootstepSpinSpd,     var->c_self.footstep_spin_spd);
    set_float(f_FootstepLifetime,    var->c_self.footstep_lifetime);
    set_float(f_FootstepSize,        var->c_self.footstep_size);
    // Damage numbers
    set_bool (f_DamageNumbers,       var->c_self.damage_numbers);
    set_float(f_DmgNumLifetime,      var->c_self.dmg_lifetime);
    set_int  (f_DmgNumFontSize,      var->c_self.dmg_font_size);
    set_bool (f_DmgNumCustomColor,   var->c_self.dmg_custom_color);
    set_color(a_DmgNumColor,         f_DmgNumColor, var->c_self.dmg_color);
    // Crosshair
    set_bool (f_CrosshairOn,         var->c_crosshair.enable);
    set_int  (f_CrosshairType,       var->c_crosshair.type_selection);
    set_float(f_CrosshairSize,       var->c_crosshair.size);
    set_float(f_CrosshairThick,      var->c_crosshair.thick);
    set_color(a_CrosshairColor,      f_CrosshairColor, var->c_crosshair.color);
    set_bool (f_CrosshairSpin,       var->c_crosshair.spin);
    set_float(f_CrosshairSpinSpd,    var->c_crosshair.spin_speed);
    // Misc visuals / world
    set_bool (f_NoPain,              var->c_self.no_pain);
    set_bool (f_NoHallucination,     var->c_self.no_hallucination);
    set_bool (f_UnlockPerspective,   var->c_self.unlock_perspective);
    set_bool (f_Watermark,           var->c_appearance.watermark);
    set_bool (f_InfoBar,             var->c_appearance.info_bar);
    set_bool (f_ForceCompass,        var->c_world.force_compass);
    set_bool (f_ForceMap,            var->c_world.force_map);
    set_bool (f_OverrideSky,         var->c_world.override_sky);
    set_color(a_SkyColor,            f_SkyColor, var->c_world.sky_color);
    set_bool (f_OverrideSun,         var->c_world.override_sun);
    set_color(a_SunColor,            f_SunColor, var->c_world.sun_color);
    set_bool (f_OverrideCloud,       var->c_world.override_cloud);
    set_color(a_CloudColor,          f_CloudColor, var->c_world.cloud_color);
    set_bool (f_OverrideCloudRim,    var->c_world.override_cloud_rim);
    set_color(a_CloudRimColor,       f_CloudRimColor, var->c_world.cloud_rim_color);
    // Map / player relation
    set_color(a_FriendColor,         f_FriendColor, var->c_misc.friend_color);
    set_color(a_EnemyColor,          f_EnemyColor, var->c_misc.enemy_color);
    set_bool (f_MapShowAllPlayers,   var->c_world.map_show_players);
    set_bool (f_MapShowAllMarkers,   var->c_world.map_show_markers);
    // Reach / placement
    set_bool (f_FarReach,            var->c_movement.far_reach);
    set_float(f_FarReachDist,        var->c_movement.reach_dist);
    set_bool (f_PickupThroughWalls,  var->c_movement.pickup_walls);
    set_float(f_PickupDistance,      var->c_movement.pickup_dist);
    set_bool (f_ExtendNearbyRadius,  var->c_movement.extend_nearby);
    set_float(f_NearbyRadius,        var->c_movement.nearby_radius);
    set_bool (f_NearbyThroughWalls,  var->c_movement.nearby_through_walls);
    set_bool (f_IgnoreBarricadeErrors, var->c_placement.ignore_barricade);
    set_bool (f_IgnoreStructureErrors, var->c_placement.ignore_structure);
    set_bool (f_PlaceAnywhere,       var->c_placement.place_anywhere);
    set_bool (f_CustomBuildOffset,   var->c_placement.custom_offset);
    set_float(f_BuildOffsetX,        var->c_placement.offset_x);
    set_float(f_BuildOffsetY,        var->c_placement.offset_y);
    set_float(f_BuildOffsetZ,        var->c_placement.offset_z);
    set_float(f_SalvageMultiplier,   var->c_placement.salvage_multiplier);
    // Weapon extras
    set_bool (f_AutoSemiBurst,       var->c_weapon.auto_semi_burst);
    set_float(f_DamageFlinchMult,    var->c_weapon.damage_flinch_mult);
    set_bool (f_DisableBino,         var->c_weapon.disable_bino);
    set_bool (f_DisableScope,        var->c_weapon.disable_scope);
    set_bool (f_ExtendBallisticRange, var->c_weapon.extend_ballistic_range);
    set_int  (f_ExtraBallisticSteps, var->c_weapon.extra_ballistic_steps);
    set_bool (f_IgnoreLeaveTimer,    var->c_weapon.ignore_leave_timer);
    set_bool (f_InstantAim,          var->c_weapon.instant_aim);
    set_bool (f_NoBallistics,        var->c_weapon.no_ballistics);
    set_bool (f_ForceHeadshot,       var->c_weapon.force_headshot);
    set_bool (f_BulletSelf,          var->c_fesp.self);
    set_bool (f_BulletOthers,        var->c_fesp.others);
    set_color(a_BulletSelfColor,     f_BulletSelfColor, var->c_fesp.self_color);
    set_color(a_BulletOtherColor,    f_BulletOtherColor, var->c_fesp.other_color);
    set_bool (f_BulletTrail,         var->c_fesp.trail);
    // Quest exploits
    set_bool (f_QuestMaxML,          var->c_quest.max_ml);
    set_bool (f_QuestAlcohol,        var->c_quest.alcohol);
    set_bool (f_QuestLumberjack,     var->c_quest.lumberjack);
    set_bool (f_QuestVoucher,        var->c_quest.voucher);
    set_bool (f_QuestXmas2024,       var->c_quest.xmas2024);
    set_bool (f_QuestXmas2025Factory, var->c_quest.xmas2025_factory);
    set_bool (f_QuestXmas2025Penguins, var->c_quest.xmas2025_penguins);
    // Anti-spy
    set_int  (f_SpyMode,             var->c_antispy.spy_mode);
    set_bool (f_SpyToastEnabled,     var->c_antispy.spy_toast);
    prove_roundtrip();
}

} // namespace gatyware
