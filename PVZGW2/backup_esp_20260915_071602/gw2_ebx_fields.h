#pragma once
#include <cstdint>

// GW2 ebx field offsets — Frosty 1.0.6.3 schema + runtime descriptor mining
// Generated: offsets validated against live heap descriptors where marked.
// conf: VERIFIED = reproduced from mined runtime anchors; SPAN = inside verified span;
//       TAIL = beyond last anchor (schema-derived, verify at runtime once).
// Layout rules (x64 Frostbite DataContainer): ptr/CString/List=8b align8, Vec3/Vec4=16b align16,
//       bool=1b, enum/float/int=4b, embedded struct=fmap runtime size, natural alignment.

// ---- WeaponFiringData (runtime size ?, anchors 0/0)
namespace gw2::ebx::WeaponFiringData {
    constexpr uint32_t PrimaryFire                              = 0x000; // PointerRef  [TAIL]
    constexpr uint32_t DeployTime                               = 0x008; // Single  [TAIL]
    constexpr uint32_t ReactivateCooldownTime                   = 0x00c; // Single  [TAIL]
    constexpr uint32_t DisableZoomOnDeployTime                  = 0x010; // Single  [TAIL]
    constexpr uint32_t AltDeployTime                            = 0x014; // Single  [TAIL]
    constexpr uint32_t AltDeployId                              = 0x018; // Int32  [TAIL]
    constexpr uint32_t WeaponSway                               = 0x020; // PointerRef  [TAIL]
    constexpr uint32_t Rumble                                   = 0x028; // RumbleFiringData  [TAIL]
    constexpr uint32_t SupportDelayStand                        = 0x034; // Single  [TAIL]
    constexpr uint32_t SupportDelayProne                        = 0x038; // Single  [TAIL]
    constexpr uint32_t IgnoreWeaponSwitchInputs                 = 0x040; // List`1[WeaponSwitchingOverride]  [TAIL]
    constexpr uint32_t IncrementShootIndexStart                 = 0x048; // Boolean  [TAIL]
    constexpr uint32_t UseAutoAiming                            = 0x049; // Boolean  [TAIL]
    constexpr uint32_t ShowEnemyNametagOnAim                    = 0x04a; // Boolean  [TAIL]
    constexpr uint32_t ReloadWholeMags                          = 0x04b; // Boolean  [TAIL]
    constexpr uint32_t DisableReloadWhileSprinting              = 0x04c; // Boolean  [TAIL]
    constexpr uint32_t AbortReloadOnSprint                      = 0x04d; // Boolean  [TAIL]
    constexpr uint32_t DoNotResetWeaponStateOnDeactivate        = 0x04e; // Boolean  [TAIL]
    constexpr uint32_t ExcludePassengersFromRaycasts            = 0x04f; // Boolean  [TAIL]
    constexpr uint32_t _id                                      = 0x050; // CString  [TAIL]
    constexpr uint32_t _Guid                                    = 0x058; // AssetClassGuid  [TAIL]
}

// ---- FiringFunctionData (runtime size 864, anchors 0/0)
namespace gw2::ebx::FiringFunctionData {
    constexpr uint32_t WeaponDispersion                         = 0x000; // WeaponDispersion  [TAIL]
    constexpr uint32_t Shot                                     = 0x070; // ShotConfigData  [TAIL]
    constexpr uint32_t Reload                                   = 0x120; // ReloadData  [TAIL]
    constexpr uint32_t OverHeat                                 = 0x150; // OverHeatData  [TAIL]
    constexpr uint32_t FireEffects1p                            = 0x1d0; // List`1[FireEffectData]  [TAIL]
    constexpr uint32_t FireEffects3p                            = 0x1d8; // List`1[FireEffectData]  [TAIL]
    constexpr uint32_t PredictionLineEffect                     = 0x1e0; // PointerRef  [TAIL]
    constexpr uint32_t PredictionEndEffect                      = 0x1e8; // PointerRef  [TAIL]
    constexpr uint32_t Sound                                    = 0x1f0; // PointerRef  [TAIL]
    constexpr uint32_t Sound1p                                  = 0x1f8; // PointerRef  [TAIL]
    constexpr uint32_t FireLogic                                = 0x200; // FireLogicData  [TAIL]
    constexpr uint32_t Ammo                                     = 0x2e0; // AmmoConfigData  [TAIL]
    constexpr uint32_t ChargeShots                              = 0x300; // List`1[ChargeShotConfigData]  [TAIL]
    constexpr uint32_t ChargingRumbleWhenFull                   = 0x308; // RumbleLoopData  [TAIL]
    constexpr uint32_t ChargingEffectDelay                      = 0x318; // Single  [TAIL]
    constexpr uint32_t SelfHealTimeWhenDeployed                 = 0x31c; // Single  [TAIL]
    constexpr uint32_t Projectiles                              = 0x320; // List`1[ProjectileConfigData]  [TAIL]
    constexpr uint32_t ProjectileSpawnBone                      = 0x328; // GameplayBones  [TAIL]
    constexpr uint32_t ProjectileSpawnBone_Second               = 0x32c; // GameplayBones  [TAIL]
    constexpr uint32_t ProjectileSpawnBone_Third                = 0x330; // GameplayBones  [TAIL]
    constexpr uint32_t ProjectileSpawnBoneAlt                   = 0x334; // GameplayBones  [TAIL]
    constexpr uint32_t ProjectileSpawnBoneAlt_Second            = 0x338; // GameplayBones  [TAIL]
    constexpr uint32_t ProjectileSpawnBoneAlt_Third             = 0x33c; // GameplayBones  [TAIL]
    constexpr uint32_t OverrideFireEffectSpawnBone              = 0x340; // GameplayBones  [TAIL]
    constexpr uint32_t OverrideFireEffectSpawnBone_Second       = 0x344; // GameplayBones  [TAIL]
    constexpr uint32_t OverrideFireEffectSpawnBone_Third        = 0x348; // GameplayBones  [TAIL]
    constexpr uint32_t ApplyCharacterScale                      = 0x34c; // Boolean  [TAIL]
    constexpr uint32_t UsePrimaryAmmo                           = 0x34d; // Boolean  [TAIL]
    constexpr uint32_t UnlimitedAmmoForAI                       = 0x34e; // Boolean  [TAIL]
    constexpr uint32_t _id                                      = 0x350; // CString  [TAIL]
    constexpr uint32_t _Guid                                    = 0x358; // AssetClassGuid  [TAIL]
}

// ---- WeaponDispersion (runtime size 104, anchors 8/8)
namespace gw2::ebx::WeaponDispersion {
    constexpr uint32_t StandDispersion                          = 0x000; // FiringDispersionData  [VERIFIED]
    constexpr uint32_t CrouchDispersion                         = 0x01c; // FiringDispersionData  [VERIFIED]
    constexpr uint32_t ProneDispersion                          = 0x038; // FiringDispersionData  [VERIFIED]
    constexpr uint32_t JumpDispersionAngle                      = 0x054; // Single  [VERIFIED]
    constexpr uint32_t ProneTransitionDispersionAngle           = 0x058; // Single  [VERIFIED]
    constexpr uint32_t MoveDispersionAngle                      = 0x05c; // Single  [VERIFIED]
    constexpr uint32_t MoveZoomedDispersionAngle                = 0x060; // Single  [VERIFIED]
    constexpr uint32_t DecreasePerSecond                        = 0x064; // Single  [VERIFIED]
}

// ---- FiringDispersionData (runtime size 28, anchors 7/7)
namespace gw2::ebx::FiringDispersionData {
    constexpr uint32_t DispersionMode                           = 0x000; // DispersionMode  [VERIFIED]
    constexpr uint32_t MinAngle                                 = 0x004; // Single  [VERIFIED]
    constexpr uint32_t MaxAngle                                 = 0x008; // Single  [VERIFIED]
    constexpr uint32_t IncreasePerShot                          = 0x00c; // Single  [VERIFIED]
    constexpr uint32_t DecreasePerSecond                        = 0x010; // Single  [VERIFIED]
    constexpr uint32_t YawMultiplier                            = 0x014; // Single  [VERIFIED]
    constexpr uint32_t PitchMultiplier                          = 0x018; // Single  [VERIFIED]
}

// ---- ShotConfigData (runtime size 176, anchors 8/8)
namespace gw2::ebx::ShotConfigData {
    constexpr uint32_t InitialPosition                          = 0x000; // Vec3  [VERIFIED]
    constexpr uint32_t InitialPositionNoBone                    = 0x010; // Vec3  [VERIFIED]
    constexpr uint32_t InitialPositionNoBone_Second             = 0x020; // Vec3  [VERIFIED]
    constexpr uint32_t InitialPositionNoBone_Third              = 0x030; // Vec3  [VERIFIED]
    constexpr uint32_t InitialSpeed                             = 0x040; // Vec3  [VERIFIED]
    constexpr uint32_t RaycastDistance                          = 0x050; // Single  [VERIFIED]
    constexpr uint32_t InheritWeaponSpeedAmount                 = 0x054; // Single  [SPAN]
    constexpr uint32_t MuzzleExplosion                          = 0x058; // PointerRef  [SPAN]
    constexpr uint32_t SpawnDelay                               = 0x060; // Single  [SPAN]
    constexpr uint32_t NumberOfBulletsPerShell                  = 0x064; // UInt32  [SPAN]
    constexpr uint32_t NumberOfBulletsPerShot                   = 0x068; // UInt32  [SPAN]
    constexpr uint32_t NumberOfBulletsPerBurst                  = 0x06c; // UInt32  [SPAN]
    constexpr uint32_t UnloadBulletsPerShot                     = 0x070; // UInt32  [SPAN]
    constexpr uint32_t RemainingBulletModifiers                 = 0x078; // List`1[RemainingBulletModifierData]  [SPAN]
    constexpr uint32_t ForceSpawnToBoneAutoAimAngle             = 0x080; // Single  [SPAN]
    constexpr uint32_t ForceSpawnToBoneAutoAimDistance          = 0x084; // Single  [SPAN]
    constexpr uint32_t WeaponHitDistanceMinimum                 = 0x088; // Single  [SPAN]
    constexpr uint32_t WeaponDotTestMax                         = 0x08c; // Single  [SPAN]
    constexpr uint32_t WeaponRaycastVerticalOffset              = 0x090; // Single  [SPAN]
    constexpr uint32_t WeaponWallTestDist                       = 0x094; // Single  [SPAN]
    constexpr uint32_t CloseCharacterTestDist                   = 0x098; // Single  [SPAN]
    constexpr uint32_t AllowAIClientShootBoneLookup             = 0x09c; // Boolean  [VERIFIED]
    constexpr uint32_t AllowAIServerShootBoneLookup             = 0x09d; // Boolean  [VERIFIED]
    constexpr uint32_t ForceSpawnToBone                         = 0x09e; // Boolean  [TAIL]
    constexpr uint32_t ForceSpawnToCamera                       = 0x09f; // Boolean  [TAIL]
    constexpr uint32_t DualShootspace                           = 0x0a0; // Boolean  [TAIL]
    constexpr uint32_t TripleShootspace                         = 0x0a1; // Boolean  [TAIL]
}

// ---- FireLogicData (runtime size 224, anchors 8/8)
namespace gw2::ebx::FireLogicData {
    constexpr uint32_t PunchComboBehavior                       = 0x000; // PointerRef  [VERIFIED]
    constexpr uint32_t HoldAndRelease                           = 0x008; // HoldAndReleaseData  [VERIFIED]
    constexpr uint32_t BoltAction                               = 0x028; // BoltActionData  [VERIFIED]
    constexpr uint32_t Recoil                                   = 0x038; // RecoilData  [VERIFIED]
    constexpr uint32_t FireInputAction                          = 0x060; // Int32  [VERIFIED]
    constexpr uint32_t ReloadInputAction                        = 0x064; // Int32  [VERIFIED]
    constexpr uint32_t CycleFireModeInputAction                 = 0x068; // Int32  [VERIFIED]
    constexpr uint32_t TriggerDetonateRumble                    = 0x06c; // RumbleTriggerData  [SPAN]
    constexpr uint32_t TriggerPullWeight                        = 0x078; // Single  [SPAN]
    constexpr uint32_t RateOfFire                               = 0x07c; // Single  [SPAN]
    constexpr uint32_t RateOfFireForBurst                       = 0x080; // Single  [SPAN]
    constexpr uint32_t RateOfFireIncreasePerAutomaticShot       = 0x084; // Single  [SPAN]
    constexpr uint32_t RateOfFireIncreaseMaximum                = 0x088; // Single  [SPAN]
    constexpr uint32_t ClientFireRateMultiplier                 = 0x08c; // Single  [SPAN]
    constexpr uint32_t ReloadDelay                              = 0x090; // Single  [SPAN]
    constexpr uint32_t ReloadTime                               = 0x094; // Single  [SPAN]
    constexpr uint32_t ReloadTimeMultiplier                     = 0x098; // Single  [SPAN]
    constexpr uint32_t FirstReloadTimeOverride                  = 0x09c; // Single  [SPAN]
    constexpr uint32_t ReloadTimerArray                         = 0x0a0; // List`1[Single]  [SPAN]
    constexpr uint32_t ReloadTimeBulletsLeft                    = 0x0a8; // Single  [SPAN]
    constexpr uint32_t ReloadThreshold                          = 0x0ac; // Single  [SPAN]
    constexpr uint32_t PreFireDelay                             = 0x0b0; // Single  [SPAN]
    constexpr uint32_t AutomaticDelay                           = 0x0b4; // Single  [SPAN]
    constexpr uint32_t ReloadLogic                              = 0x0b8; // ReloadLogic  [SPAN]
    constexpr uint32_t ReloadType                               = 0x0bc; // ReloadType  [SPAN]
    constexpr uint32_t FireLogicType                            = 0x0c0; // FireLogicType  [SPAN]
    constexpr uint32_t FireLogicTypeArray                       = 0x0c8; // List`1[FireLogicType]  [SPAN]
    constexpr uint32_t AutomaticFirePrimingTime                 = 0x0d0; // Single  [SPAN]
    constexpr uint32_t PrimingFireInputAction                   = 0x0d4; // Int32  [SPAN]
    constexpr uint32_t AlternateFireAndDetonate                 = 0x0d8; // Boolean  [VERIFIED]
    constexpr uint32_t HoldOffReloadUntilFireRelease            = 0x0d9; // Boolean  [TAIL]
    constexpr uint32_t HoldOffReloadUntilZoomRelease            = 0x0da; // Boolean  [TAIL]
    constexpr uint32_t ForceReloadActionOnFireTrigger           = 0x0db; // Boolean  [TAIL]
    constexpr uint32_t AlwaysAutoReload                         = 0x0dc; // Boolean  [TAIL]
}

// ---- RecoilData (runtime size 40, anchors 10/10)
namespace gw2::ebx::RecoilData {
    constexpr uint32_t MaxRecoilAngleX                          = 0x000; // Single  [VERIFIED]
    constexpr uint32_t MinRecoilAngleX                          = 0x004; // Single  [VERIFIED]
    constexpr uint32_t MaxRecoilAngleY                          = 0x008; // Single  [VERIFIED]
    constexpr uint32_t MinRecoilAngleY                          = 0x00c; // Single  [VERIFIED]
    constexpr uint32_t MaxRecoilAngleZ                          = 0x010; // Single  [VERIFIED]
    constexpr uint32_t MinRecoilAngleZ                          = 0x014; // Single  [VERIFIED]
    constexpr uint32_t MaxRecoilFov                             = 0x018; // Single  [VERIFIED]
    constexpr uint32_t MinRecoilFov                             = 0x01c; // Single  [VERIFIED]
    constexpr uint32_t RecoilRecoveryTime                       = 0x020; // Single  [VERIFIED]
    constexpr uint32_t RecoilFollowsDispersion                  = 0x024; // Boolean  [VERIFIED]
}

// ---- ReloadData (runtime size 48, anchors 0/0)
namespace gw2::ebx::ReloadData {
    constexpr uint32_t Offset                                   = 0x000; // Vec3  [TAIL]
    constexpr uint32_t Rotation                                 = 0x010; // Vec3  [TAIL]
    constexpr uint32_t Bone                                     = 0x020; // GameplayBones  [TAIL]
    constexpr uint32_t Effect                                   = 0x028; // PointerRef  [TAIL]
}

// ---- OverHeatData (runtime size ?, anchors 0/0)
namespace gw2::ebx::OverHeatData {
    constexpr uint32_t OverHeatEffect                           = 0x000; // FireEffectData  [TAIL]
    constexpr uint32_t HeatPerBullet                            = 0x060; // Single  [TAIL]
    constexpr uint32_t HeatIncPerSecond                         = 0x064; // Single  [TAIL]
    constexpr uint32_t HeatDropPerSecond                        = 0x068; // Single  [TAIL]
    constexpr uint32_t HeatRateOfFireIncreaseMultiplier         = 0x06c; // Single  [TAIL]
    constexpr uint32_t OverHeatPenaltyTime                      = 0x070; // Single  [TAIL]
    constexpr uint32_t OverHeatThreshold                        = 0x074; // Single  [TAIL]
    constexpr uint32_t OverHeatEffectPersists                   = 0x078; // Boolean  [TAIL]
}

// ---- AmmoConfigData (runtime size 28, anchors 7/7)
namespace gw2::ebx::AmmoConfigData {
    constexpr uint32_t MagazineCapacity                         = 0x000; // Int32  [VERIFIED]
    constexpr uint32_t InitialNumberOfMagazines                 = 0x004; // Int32  [VERIFIED]
    constexpr uint32_t NumberOfMagazines                        = 0x008; // Int32  [VERIFIED]
    constexpr uint32_t TraceFrequency                           = 0x00c; // UInt32  [VERIFIED]
    constexpr uint32_t AutoReplenishDelay                       = 0x010; // Single  [SPAN]
    constexpr uint32_t AmmoBagPickupAmount                      = 0x014; // Int32  [SPAN]
    constexpr uint32_t AutoReplenishMagazine                    = 0x018; // Boolean  [VERIFIED]
    constexpr uint32_t ReplenishAtProjectileLimit               = 0x019; // Boolean  [VERIFIED]
    constexpr uint32_t ReplenishToInitialNumberOfMagazines      = 0x01a; // Boolean  [VERIFIED]
    constexpr uint32_t ReplenishOnDeactivate                    = 0x01b; // Boolean  [TAIL]
}

// ---- GunSwayData (runtime size 896, anchors 0/0)
namespace gw2::ebx::GunSwayData {
    constexpr uint32_t Stand                                    = 0x000; // GunSwayStandData  [TAIL]
    constexpr uint32_t Crouch                                   = 0x118; // GunSwayCrouchProneData  [TAIL]
    constexpr uint32_t Prone                                    = 0x1b0; // GunSwayCrouchProneData  [TAIL]
    constexpr uint32_t ProneToCrouch                            = 0x248; // GunSwayStanceTransition  [TAIL]
    constexpr uint32_t ProneToStand                             = 0x25c; // GunSwayStanceTransition  [TAIL]
    constexpr uint32_t CrouchToProne                            = 0x270; // GunSwayStanceTransition  [TAIL]
    constexpr uint32_t CrouchToStand                            = 0x284; // GunSwayStanceTransition  [TAIL]
    constexpr uint32_t StandToProne                             = 0x298; // GunSwayStanceTransition  [TAIL]
    constexpr uint32_t StandToCrouch                            = 0x2ac; // GunSwayStanceTransition  [TAIL]
    constexpr uint32_t SuppressionModifierUnzoomed              = 0x2c0; // GunSwayStanceZoomModifierData  [TAIL]
    constexpr uint32_t SuppressionModifierZoomed                = 0x304; // GunSwayStanceZoomModifierData  [TAIL]
    constexpr uint32_t ModifierUnlocks                          = 0x348; // List`1[GunSwayModifierUnlock]  [TAIL]
    constexpr uint32_t DeviationScaleFactorZoom                 = 0x350; // Single  [TAIL]
    constexpr uint32_t GameplayDeviationScaleFactorZoom         = 0x354; // Single  [TAIL]
    constexpr uint32_t DeviationScaleFactorNoZoom               = 0x358; // Single  [TAIL]
    constexpr uint32_t GameplayDeviationScaleFactorNoZoom       = 0x35c; // Single  [TAIL]
    constexpr uint32_t ShootingRecoilDecreaseScale              = 0x360; // Single  [TAIL]
    constexpr uint32_t FirstShotRecoilMultiplier                = 0x364; // Single  [TAIL]
    constexpr uint32_t CameraRecoil                             = 0x368; // PointerRef  [TAIL]
    constexpr uint32_t _id                                      = 0x370; // CString  [TAIL]
    constexpr uint32_t _Guid                                    = 0x378; // AssetClassGuid  [TAIL]
}

// ---- GunSwayStandData (runtime size 280, anchors 1/1)
namespace gw2::ebx::GunSwayStandData {
    constexpr uint32_t NoZoom                                   = 0x000; // GunSwayBaseMoveJumpData  [VERIFIED]
    constexpr uint32_t Zoom                                     = 0x08c; // GunSwayBaseMoveJumpData  [TAIL]
}

// ---- GunSwayBaseMoveJumpData (runtime size 140, anchors 8/8)
namespace gw2::ebx::GunSwayBaseMoveJumpData {
    constexpr uint32_t BaseValue                                = 0x000; // GunSwayDispersionData  [VERIFIED]
    constexpr uint32_t Moving                                   = 0x010; // GunSwayDispersionData  [VERIFIED]
    constexpr uint32_t Jumping                                  = 0x020; // GunSwayDispersionData  [VERIFIED]
    constexpr uint32_t Sprinting                                = 0x030; // GunSwayDispersionData  [VERIFIED]
    constexpr uint32_t VaultingSmallObject                      = 0x040; // GunSwayDispersionData  [VERIFIED]
    constexpr uint32_t VaultingMediumObject                     = 0x050; // GunSwayDispersionData  [VERIFIED]
    constexpr uint32_t Recoil                                   = 0x060; // GunSwayRecoilData  [VERIFIED]
    constexpr uint32_t GunSwayLag                               = 0x078; // GunSwayLagData  [VERIFIED]
}

// ---- GunSwayCrouchProneData (runtime size 152, anchors 1/1)
namespace gw2::ebx::GunSwayCrouchProneData {
    constexpr uint32_t NoZoom                                   = 0x000; // GunSwayBaseMoveData  [VERIFIED]
    constexpr uint32_t Zoom                                     = 0x04c; // GunSwayBaseMoveData  [TAIL]
}

// ---- GunSwayDispersionData (runtime size 16, anchors 4/4)
namespace gw2::ebx::GunSwayDispersionData {
    constexpr uint32_t MinAngle                                 = 0x000; // Single  [VERIFIED]
    constexpr uint32_t MaxAngle                                 = 0x004; // Single  [VERIFIED]
    constexpr uint32_t IncreasePerShot                          = 0x008; // Single  [VERIFIED]
    constexpr uint32_t DecreasePerSecond                        = 0x00c; // Single  [VERIFIED]
}

// ---- GunSwayRecoilData (runtime size 24, anchors 6/6)
namespace gw2::ebx::GunSwayRecoilData {
    constexpr uint32_t RecoilAmplitudeMax                       = 0x000; // Single  [VERIFIED]
    constexpr uint32_t RecoilAmplitudeIncPerShot                = 0x004; // Single  [VERIFIED]
    constexpr uint32_t HorizontalRecoilAmplitudeIncPerShotMin   = 0x008; // Single  [VERIFIED]
    constexpr uint32_t HorizontalRecoilAmplitudeIncPerShotMax   = 0x00c; // Single  [VERIFIED]
    constexpr uint32_t HorizontalRecoilAmplitudeMax             = 0x010; // Single  [VERIFIED]
    constexpr uint32_t RecoilAmplitudeDecreaseFactor            = 0x014; // Single  [VERIFIED]
}

// ---- GunSwayStanceZoomModifierData (runtime size 68, anchors 8/8)
namespace gw2::ebx::GunSwayStanceZoomModifierData {
    constexpr uint32_t DispersionMod                            = 0x000; // GunSwayDispersionModData  [VERIFIED]
    constexpr uint32_t MovingDispersionMod                      = 0x010; // GunSwayDispersionModData  [VERIFIED]
    constexpr uint32_t SprintingDispersionMod                   = 0x020; // GunSwayDispersionModData  [VERIFIED]
    constexpr uint32_t RecoilMagnitudeMod                       = 0x030; // Single  [VERIFIED]
    constexpr uint32_t RecoilAngleMod                           = 0x034; // Single  [VERIFIED]
    constexpr uint32_t FirstShotRecoilMod                       = 0x038; // Single  [VERIFIED]
    constexpr uint32_t LagYawMod                                = 0x03c; // Single  [VERIFIED]
    constexpr uint32_t LagPitchMod                              = 0x040; // Single  [VERIFIED]
}

// ---- GunSwayStanceTransition (runtime size 20, anchors 2/2)
namespace gw2::ebx::GunSwayStanceTransition {
    constexpr uint32_t MaxPenaltyValue                          = 0x000; // GunSwayDispersionData  [VERIFIED]
    constexpr uint32_t CoolDown                                 = 0x010; // Single  [VERIFIED]
}

// ---- HoldAndReleaseData (runtime size 32, anchors 4/4)
namespace gw2::ebx::HoldAndReleaseData {
    constexpr uint32_t MaxHoldTime                              = 0x000; // Single  [VERIFIED]
    constexpr uint32_t MinPowerModifier                         = 0x004; // Single  [VERIFIED]
    constexpr uint32_t MaxPowerModifier                         = 0x008; // Single  [VERIFIED]
    constexpr uint32_t PowerIncreasePerSecond                   = 0x00c; // Single  [VERIFIED]
    constexpr uint32_t Delay                                    = 0x010; // Single  [TAIL]
    constexpr uint32_t KilledHoldingPowerModifier               = 0x014; // Single  [TAIL]
    constexpr uint32_t ChargeDelay                              = 0x018; // Single  [TAIL]
    constexpr uint32_t ForceFireWhenKilledHolding               = 0x01c; // Boolean  [TAIL]
}

// ---- BoltActionData (runtime size 16, anchors 7/7)
namespace gw2::ebx::BoltActionData {
    constexpr uint32_t BoltActionDelay                          = 0x000; // Single  [VERIFIED]
    constexpr uint32_t BoltActionTime                           = 0x004; // Single  [VERIFIED]
    constexpr uint32_t HoldBoltActionUntilFireRelease           = 0x008; // Boolean  [VERIFIED]
    constexpr uint32_t HoldBoltActionUntilZoomRelease           = 0x009; // Boolean  [VERIFIED]
    constexpr uint32_t ForceBoltActionOnFireTrigger             = 0x00a; // Boolean  [VERIFIED]
    constexpr uint32_t UnZoomOnBoltAction                       = 0x00b; // Boolean  [VERIFIED]
    constexpr uint32_t ReturnToZoomAfterBoltAction              = 0x00c; // Boolean  [VERIFIED]
}

// ---- RumbleTriggerData (runtime size 12, anchors 2/2)
namespace gw2::ebx::RumbleTriggerData {
    constexpr uint32_t LowRumble                                = 0x000; // Single  [VERIFIED]
    constexpr uint32_t HighRumble                               = 0x004; // Single  [VERIFIED]
    constexpr uint32_t Duration                                 = 0x008; // Single  [TAIL]
}

