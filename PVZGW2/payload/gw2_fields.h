// ---------------------------------------------------------------------------
// GW2 struct field offsets — extracted from Frostbite reflection descriptors.
// Source: gw2_fieldmap.json (996 classes w/ fields), see C:\Users\Public\
//         gw2_classes.txt for the browsable list.
// Method: TypeInfo tables @ 0x1431e52d8/0x1432077d8/0x14321a190 ->
//         FieldInfo arrays {name*, offset<<16, type*, flags} @ 0x18 stride.
// Valid:  dump 0 (GW2 retail, 2026-08-30). Offsets are INSTANCE-relative:
//         value = (uint8_t*)instance + offset.
// ---------------------------------------------------------------------------
#pragma once

namespace gw2 {
namespace fields {

namespace types {
    // TypeInfo tables (permanent RVAs): {name*, kind:u16|size:u16, parent*,
    // count:u16, fields*:FieldInfo[]}. Entry stride in comment.
    constexpr uintptr_t typeinfo_a = 0x31E52D8;  // stride 0x30 (7850 entries)
    constexpr uintptr_t typeinfo_b = 0x32077D8;  // stride 0x38 (6823 entries)
    constexpr uintptr_t typeinfo_c = 0x321A190;  // stride 0x38 (2326 entries)
    constexpr uintptr_t fieldinfo_stride = 0x18; // FieldInfo entry size
}

// ---- AIM ------------------------------------------------------------------
namespace AimingPoseData {                     // size 0x18, all float
    constexpr uintptr_t MinimumPitch    = 0x00; // pitch clamp min (rad)
    constexpr uintptr_t MaximumPitch    = 0x04; // pitch clamp max (rad)
    constexpr uintptr_t TargetingFov    = 0x08; // ADS FOV
    constexpr uintptr_t AimSteadiness   = 0x0C; // sway scalar
    constexpr uintptr_t SpeedMultiplier = 0x10; // aim speed mult (speedhack aim)
    constexpr uintptr_t RecoilMultiplier= 0x14; // recoil mult (0.0 = no recoil)
}
namespace AimAssistCollisionBoneData {         // size 0x38
    constexpr uintptr_t SnapAim          = 0x14; // game's own snap-aim cfg
}
namespace PhysicsDrivenAnimationEntityBinding {
    constexpr uintptr_t PhysicsMotionTarget = 0x00;
    constexpr uintptr_t AimLeftRight        = 0x14; // yaw axis
    constexpr uintptr_t AimUpDown           = 0x28; // pitch axis
}

// ---- WEAPONS / FIRE ---------------------------------------------------------
namespace WeaponDispersion {                   // size 0x68
    constexpr uintptr_t StandDispersion              = 0x00; // vec (0x1c)
    constexpr uintptr_t CrouchDispersion             = 0x1C;
    constexpr uintptr_t ProneDispersion              = 0x38;
    constexpr uintptr_t JumpDispersionAngle          = 0x54; // float
    constexpr uintptr_t ProneTransitionDispAngle     = 0x58;
    constexpr uintptr_t MoveDispersionAngle          = 0x5C;
    constexpr uintptr_t MoveZoomedDispersionAngle    = 0x60;
    constexpr uintptr_t DecreasePerSecond            = 0x64; // 0 = permanent
}
namespace PVZWeaponsBinding {                  // size 0x8c
    constexpr uintptr_t WeaponPrimingStarted  = 0x00; // event (0x14)
    constexpr uintptr_t WeaponFireStarted     = 0x14; // event
    constexpr uintptr_t TimeSinceFireStarted  = 0x28;
    constexpr uintptr_t WeaponIsFiring        = 0x3C;
    constexpr uintptr_t SpawnedProjectile     = 0x50; // proj spawn event
    constexpr uintptr_t ReloadTimeMultiplier  = 0x64; // 0 = instant reload
    constexpr uintptr_t ShootSpaceIndex       = 0x78;
}

// ---- HEALTH / DAMAGE --------------------------------------------------------
namespace DifficultyData {                     // size 0xc0
    constexpr uintptr_t HumanHealthModifier   = 0x50; // float mult
    constexpr uintptr_t FriendsHealthModifier = 0x54;
    constexpr uintptr_t EnemiesHealthModifier = 0x58; // <1 = weaker zombies
    constexpr uintptr_t FriendlyDamageModifier= 0x5C;
}
namespace VehicleHealthZoneData {              // size 0x20
    constexpr uintptr_t MaxHealth       = 0x00; // float
    constexpr uintptr_t MaxShieldHealth = 0x04; // float
    constexpr uintptr_t UseDamageAngleCalculation = 0x1C;
}
namespace HealthScaleThreshold {               // size 0x10
    constexpr uintptr_t ConsecutiveDeaths = 0x00; // int
    constexpr uintptr_t HealthMultiplier  = 0x04; // float
    constexpr uintptr_t UIDisplaySid      = 0x08;
}
namespace PlayerScore {                        // size 0x18
    constexpr uintptr_t Kills = 0x00;            // int
}
namespace KillStreakInfo {                     // size 0x8
    constexpr uintptr_t KillThreshold = 0x00;
}
namespace PointSystemParamsAsset {             // size 0x60
    constexpr uintptr_t MultiKillTimeLimit     = 0x18;
    constexpr uintptr_t TimedKillStreakMinTime = 0x1C;
    constexpr uintptr_t TimedKillStreakMaxTime = 0x20;
    constexpr uintptr_t KillStreakX            = 0x28;
}

// ---- MOVEMENT / SPEED --------------------------------------------------------
namespace MoverTune {                          // size 0xd8
    constexpr uintptr_t speed           = 0x18; // float
    constexpr uintptr_t maxSpeedFraction= 0x1C; // float
}
namespace SpawnSpeedData {                     // size 0x38
    constexpr uintptr_t Speed = 0x30;            // float
}
namespace TeleportEntityData {                 // size 0xf0
    constexpr uintptr_t ForceTeleport              = 0x19; // bool
    constexpr uintptr_t TeleportCharacterIfInVehicle = 0x1A; // bool
}

// ---- CAMERA ------------------------------------------------------------------
namespace PlayerCameraEntityData {             // size 0xc0 (own fields from 0xA0)
    constexpr uintptr_t SoldierTargetMode  = 0xA0;
    constexpr uintptr_t SoldierCameraIndex = 0xA4;
    constexpr uintptr_t VehicleTargetMode  = 0xA8;
    constexpr uintptr_t VehicleCameraIndex = 0xAC;
    constexpr uintptr_t ReleaseControlIfTargetLost = 0xB4;
    constexpr uintptr_t ShouldTargetControllable   = 0xB5;
}
namespace CameraLensPreset {                   // size 0x30
    constexpr uintptr_t DefaultFocalLength = 0x18; // FOV chain
    constexpr uintptr_t SensorWidth        = 0x1C;
    constexpr uintptr_t SensorHeight       = 0x20;
}
namespace CameraCommonBinding {                // size 0x28
    constexpr uintptr_t FirstPersonCameraHeight = 0x00;
    constexpr uintptr_t AnimatedCameraBlendTime = 0x14;
}
namespace VehicleCameraData {                  // size 0x130
    constexpr uintptr_t MoveToPosition          = 0xD0; // vec3
    constexpr uintptr_t MoveToPositionSlopeFactor = 0xE0;
    constexpr uintptr_t TargetOffset            = 0xF0; // vec3
    constexpr uintptr_t TargetOffsetSlopeFactor = 0x100;
    constexpr uintptr_t RotationFactor          = 0x110;
    constexpr uintptr_t PositionFactor          = 0x120;
    constexpr uintptr_t ResetDistance           = 0x124;
    constexpr uintptr_t FixedPosition           = 0x128; // bool
}

// ---- ECONOMY -------------------------------------------------------------------
namespace SunDropProjectile {                  // size 0x10
    // fields: see gw2_fieldmap.json
}
namespace RewardInfo {                         // size 0x10
    constexpr uintptr_t RewardData = 0x00;
}

} // namespace fields
} // namespace gw2
