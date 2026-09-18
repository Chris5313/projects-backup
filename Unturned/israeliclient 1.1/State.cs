using System;
using UnityEngine;
using System.Reflection;

namespace gatyware
{
	[Obfuscation(Exclude = true)]
	public static class State
	{
		public static Color Rainbow(float speed)
		{
			float num = Time.unscaledTime * speed;
			return new Color(Mathf.Sin(num * 2f) * 0.5f + 0.5f, Mathf.Sin(num * 2f + 2.094f) * 0.5f + 0.5f, Mathf.Sin(num * 2f + 4.189f) * 0.5f + 0.5f, 1f);
		}

		private static void Rb(int i, ref bool on, ref float spd, ref Color col)
		{
			if (on)
			{
				if (!State._rbInit[i])
				{
					State._bases[i] = col;
					State._rbInit[i] = true;
				}
				float a = col.a;
				col = State.Rainbow(spd);
				col.a = a;
				return;
			}
			if (State._rbInit[i])
			{
				col = State._bases[i];
				State._rbInit[i] = false;
			}
		}

		public static void UpdateRainbows()
		{
			State.Rb(0, ref State.PlayerBoxRb, ref State.PlayerBoxRbSpd, ref State.PlayerBoxColor);
			State.Rb(1, ref State.PlayerSnapRb, ref State.PlayerSnapRbSpd, ref State.PlayerSnapColor);
			State.Rb(2, ref State.PlayerSkelRb, ref State.PlayerSkelRbSpd, ref State.PlayerSkelColor);
			State.Rb(3, ref State.ZombieBoxRb, ref State.ZombieBoxRbSpd, ref State.ZombieBoxColor);
			State.Rb(4, ref State.ZombieSnapRb, ref State.ZombieSnapRbSpd, ref State.ZombieSnapColor);
			State.Rb(5, ref State.VehicleBoxRb, ref State.VehicleBoxRbSpd, ref State.VehicleBoxColor);
			State.Rb(6, ref State.VehicleSnapRb, ref State.VehicleSnapRbSpd, ref State.VehicleSnapColor);
			State.Rb(7, ref State.AnimalBoxRb, ref State.AnimalBoxRbSpd, ref State.AnimalBoxColor);
			State.Rb(8, ref State.PlayerNameRb, ref State.PlayerNameRbSpd, ref State.PlayerTextColor);
			State.Rb(9, ref State.PlayerWeaponRb, ref State.PlayerWeaponRbSpd, ref State.PlayerWeaponColor);
			State.Rb(10, ref State.PlayerFillRb, ref State.PlayerFillRbSpd, ref State.PlayerFillColor);
			State.Rb(11, ref State.ZombieSkelRb, ref State.ZombieSkelRbSpd, ref State.ZombieSkelColor);
			State.Rb(12, ref State.ZombieFillRb, ref State.ZombieFillRbSpd, ref State.ZombieFillColor);
			State.Rb(13, ref State.ZombieTextRb, ref State.ZombieTextRbSpd, ref State.ZombieTextColor);
			State.Rb(14, ref State.VehicleFillRb, ref State.VehicleFillRbSpd, ref State.VehicleFillColor);
			State.Rb(15, ref State.VehicleTextRb, ref State.VehicleTextRbSpd, ref State.VehicleTextColor);
			State.Rb(16, ref State.VehicleLockedRb, ref State.VehicleLockedRbSpd, ref State.VehicleLockedColor);
			State.Rb(17, ref State.AnimalFillRb, ref State.AnimalFillRbSpd, ref State.AnimalFillColor);
			State.Rb(18, ref State.AnimalSnapRb, ref State.AnimalSnapRbSpd, ref State.AnimalSnapColor);
			State.Rb(19, ref State.AnimalTextRb, ref State.AnimalTextRbSpd, ref State.AnimalTextColor);
			State.Rb(20, ref State.ItemSnapRb, ref State.ItemSnapRbSpd, ref State.ItemSnapColor);
			State.Rb(21, ref State.ItemTextRb, ref State.ItemTextRbSpd, ref State.ItemTextColor);
			State.Rb(26, ref State.CrosshairRb, ref State.CrosshairRbSpd, ref State.CrosshairColor);
			State.Rb(27, ref State.PlayerChamsVisRb, ref State.PlayerChamsVisRbSpd, ref State.PlayerChamsVis);
			State.Rb(28, ref State.PlayerChamsNonVisRb, ref State.PlayerChamsNonVisRbSpd, ref State.PlayerChamsNonVis);
			State.Rb(32, ref State.ZombieChamsVisRb, ref State.ZombieChamsVisRbSpd, ref State.ZombieChamsVis);
			State.Rb(33, ref State.ZombieChamsNonVisRb, ref State.ZombieChamsNonVisRbSpd, ref State.ZombieChamsNonVis);
			State.Rb(37, ref State.VehicleChamsVisRb, ref State.VehicleChamsVisRbSpd, ref State.VehicleChamsVis);
			State.Rb(38, ref State.VehicleChamsNonVisRb, ref State.VehicleChamsNonVisRbSpd, ref State.VehicleChamsNonVis);
			State.Rb(29, ref State.SelfChamsRb, ref State.SelfChamsRbSpd, ref State.SelfChamsColor);
			State.Rb(30, ref State.FootstepRb, ref State.FootstepRbSpd, ref State.FootstepColor);
			State.Rb(39, ref State.StorageBoxRb, ref State.StorageBoxRbSpd, ref State.StorageBoxColor);
			State.Rb(40, ref State.StorageFillRb, ref State.StorageFillRbSpd, ref State.StorageFillColor);
			State.Rb(41, ref State.StorageSnapRb, ref State.StorageSnapRbSpd, ref State.StorageSnapColor);
			State.Rb(42, ref State.StorageTextRb, ref State.StorageTextRbSpd, ref State.StorageTextColor);
			State.Rb(43, ref State.AirdropBoxRb, ref State.AirdropBoxRbSpd, ref State.AirdropBoxColor);
			State.Rb(44, ref State.AirdropFillRb, ref State.AirdropFillRbSpd, ref State.AirdropFillColor);
			State.Rb(45, ref State.AirdropSnapRb, ref State.AirdropSnapRbSpd, ref State.AirdropSnapColor);
			State.Rb(46, ref State.AirdropTextRb, ref State.AirdropTextRbSpd, ref State.AirdropTextColor);
			State.Rb(47, ref State.BedBoxRb, ref State.BedBoxRbSpd, ref State.BedBoxColor);
			State.Rb(48, ref State.BedFillRb, ref State.BedFillRbSpd, ref State.BedFillColor);
			State.Rb(49, ref State.BedSnapRb, ref State.BedSnapRbSpd, ref State.BedSnapColor);
			State.Rb(34, ref State.GenBoxRb, ref State.GenBoxRbSpd, ref State.GenBoxColor);
			State.Rb(35, ref State.GenFillRb, ref State.GenFillRbSpd, ref State.GenFillColor);
			State.Rb(36, ref State.GenSnapRb, ref State.GenSnapRbSpd, ref State.GenSnapColor);
			State.Rb(31, ref State.TurretBoxRb, ref State.TurretBoxRbSpd, ref State.TurretBoxColor);
			State.Rb(50, ref State.TurretFillRb, ref State.TurretFillRbSpd, ref State.TurretFillColor);
			State.Rb(51, ref State.TurretSnapRb, ref State.TurretSnapRbSpd, ref State.TurretSnapColor);
			State.Rb(52, ref State.BulletSelfRb, ref State.BulletSelfRbSpd, ref State.BulletSelfColor);
			State.Rb(53, ref State.BulletOtherRb, ref State.BulletOtherRbSpd, ref State.BulletOtherColor);
			State.Rb(54, ref State.BulletSnapRb, ref State.BulletSnapRbSpd, ref State.BulletSnapColor);
			State.Rb(55, ref State.DmgNumColorRb, ref State.DmgNumColorRbSpd, ref State.DmgNumColor);
			State.Rb(56, ref State.MeleeTracerRb, ref State.MeleeTracerRbSpd, ref State.MeleeTracerColor);
			State.Rb(57, ref State.SkyColorRb, ref State.SkyColorRbSpd, ref State.SkyColor);
			State.Rb(58, ref State.SunColorRb, ref State.SunColorRbSpd, ref State.SunColor);
			State.Rb(59, ref State.CloudColorRb, ref State.CloudColorRbSpd, ref State.CloudColor);
			State.Rb(60, ref State.CloudRimColorRb, ref State.CloudRimColorRbSpd, ref State.CloudRimColor);
			State.Rb(61, ref State.WeaponChamsRb, ref State.WeaponChamsRbSpd, ref State.WeaponChamsColor);
			State.PlayerFillColor.a = State.PlayerFillAlpha;
			State.ZombieFillColor.a = State.ZombieFillAlpha;
			State.VehicleFillColor.a = State.VehicleFillAlpha;
			State.AnimalFillColor.a = State.AnimalFillAlpha;
			State.StorageFillColor.a = State.StorageFillAlpha;
			State.AirdropFillColor.a = State.AirdropFillAlpha;
			State.BedFillColor.a = State.BedFillAlpha;
			State.GenFillColor.a = State.GenFillAlpha;
			State.TurretFillColor.a = State.TurretFillAlpha;

		}

		public static bool Open;

		public static bool CornerBoxMode = true;

		public static bool Box3D;

		// Default menu key is `Comma` (KeyCode 188) instead of F1 because some users have
	// F1 captured as a global hotkey by Discord / Steam overlay / Razer Synapse /
	// Logitech G Hub, which prevents Unity's Input.GetKeyDown from ever seeing the press.
	public static KeyCode MenuKey = KeyCode.Comma;

		public static int EspFontIndex;

		public static int Language;

		public static bool EspTextBackground = true;

		public static bool CrosshairOn;

		public static int CrosshairType;

		public static float CrosshairSize = 12f;

		public static float CrosshairThick = 2f;

		public static Color CrosshairColor = new Color(1f, 1f, 1f, 0.9f);

		public static bool CrosshairRb;

		public static float CrosshairRbSpd = 1f;

		public static bool CrosshairSpin;

		public static float CrosshairSpinSpd = 90f;

		public static int SpyMode;

		public static bool StreamProofOn;

		public static bool IsSpying;

		public static float LastSpyTime;

		public static bool SpyToastEnabled = true;

		public static bool QuestMaxML;

		public static bool QuestAlcohol;

		public static bool QuestLumberjack;

		public static bool QuestVoucher;

		public static bool QuestXmas2024;

		public static bool QuestXmas2025Factory;

		public static bool QuestXmas2025Penguins;

		public static bool EntityInspectorOn;

		public static bool StorageViewerOn;

		public static bool ItemSpawnerOpen;

		public static string ItemSpawnerSearch = "";

		public static bool NoFlash;

		public static bool NoPain;

		public static bool NoHallucination;

		public static bool NoGrayscale;

		public static bool NoFog;

		public static int NightVision;

		public static bool CustomTime;

		public static float CustomTimeValue = 0.5f;

		public static bool OverrideSky;

		public static Color SkyColor = new Color(0.3f, 0.5f, 0.9f);

		public static bool SkyColorRb;

		public static float SkyColorRbSpd = 1f;

		public static bool OverrideSun;

		public static Color SunColor = new Color(1f, 0.95f, 0.8f);

		public static bool SunColorRb;

		public static float SunColorRbSpd = 1f;

		public static bool OverrideCloud;

		public static Color CloudColor = new Color(0.9f, 0.9f, 0.95f);

		public static bool CloudColorRb;

		public static float CloudColorRbSpd = 1f;

		public static bool OverrideCloudRim;

		public static Color CloudRimColor = new Color(1f, 0.8f, 0.5f);

		public static bool CloudRimColorRb;

		public static float CloudRimColorRbSpd = 1f;

		public static bool CustomFov;

		public static float CustomFovDeg = 90f;

		public static bool UnlockPerspective;

		public static bool ForceCompass;

		public static bool ForceMap;

		public static bool Watermark;

		public static bool InfoBar;

		public static bool MapShowAllPlayers;

		public static bool MapShowAllMarkers;

		public static bool FarReach;

		public static float FarReachDist = 20f;

		public static bool PickupThroughWalls;

		public static float PickupDistance = 20f;

		public static bool AutoForge;

		public static float AutoForgeRadius = 25f;

		public static float AutoForgeDelay = 0.1f;

		public static bool ExtendNearbyRadius;

		public static float NearbyRadius = 20f;

		public static bool NearbyThroughWalls;

		public static bool IgnoreBarricadeErrors;

		public static bool IgnoreStructureErrors;

		public static bool PlaceAnywhere;

		public static bool CustomBuildOffset;

		public static float BuildOffsetX;

		public static float BuildOffsetY;

		public static float BuildOffsetZ;

		public static float SalvageMultiplier = 1f;

		public static bool ChatSpamming;

		public static string SpamText = ":3";

		public static float ChatSpamDelay = 1f;

		public static bool PlayerEsp;

		public static bool PlayerName = true;

		public static bool PlayerDistance = true;

		public static bool PlayerBox = true;

		public static bool PlayerWeapon = true;

		public static bool PlayerSkeleton;

		public static bool PlayerClothing;

		public static float ClothingIconSize = 18f;

		public static bool PlayerSnapline;

		public static bool PlayerFill;

		public static Color PlayerBoxColor = new Color(0.25f, 0.6f, 1f);

		public static bool PlayerBoxRb;

		public static float PlayerBoxRbSpd = 1f;

		public static Color PlayerFillColor = new Color(0.25f, 0.6f, 1f, 0.12f);

		public static bool PlayerFillRb;

		public static float PlayerFillRbSpd = 1f;

		public static float PlayerFillAlpha = 0.12f;

		public static Color PlayerSkelColor = new Color(0.4f, 0.7f, 1f);

		public static bool PlayerSkelRb;

		public static float PlayerSkelRbSpd = 1f;

		public static Color PlayerSnapColor = new Color(0.55f, 0.4f, 0.85f);

		public static bool PlayerSnapRb;

		public static float PlayerSnapRbSpd = 1f;

		public static Color PlayerTextColor = new Color(1f, 1f, 1f, 0.95f);

		public static bool PlayerNameRb;

		public static float PlayerNameRbSpd = 1f;

		public static Color PlayerWeaponColor = new Color(0.85f, 0.75f, 1f, 0.85f);

		public static bool PlayerWeaponRb;

		public static float PlayerWeaponRbSpd = 1f;

		public static float PlayerMaxDist = 500f;

		public static float PlayerBoxThick = 1.5f;

		public static float PlayerSnapThick = 1f;

		public static float PlayerSkelThick = 1.2f;

		public static int PlayerFontSize = 12;

		public static bool PlayerChams;

		public static Color PlayerChamsVis = new Color(0.1f, 1f, 0.2f, 0.85f);

		public static bool PlayerChamsVisRb;

		public static float PlayerChamsVisRbSpd = 1f;

		public static Color PlayerChamsNonVis = new Color(1f, 0.1f, 0.1f, 0.85f);

		public static bool PlayerChamsNonVisRb;

		public static float PlayerChamsNonVisRbSpd = 1f;

        public static bool AimAssistOn = false;
        public static bool AimAssistRequireADS = true;      // Only when right-click aiming
        public static bool AimAssistTargetPlayers = true;
        public static bool AimAssistTargetZombies = false;
        public static bool AimAssistFriendly = true;          // Skip friends
        public static bool AimAssistVisCheck = true;          // Wall check
        public static float AimAssistSmoothness = 0.5f;       // 0 = fast, 1 = slow
        public static bool AimAssistIndicator = false;        // Show where it's aiming

        public static bool ChamsWireframe;

		public static bool AutoPickupOn;

		public static float AutoPickupDist = 20f;

		public static float AutoPickupSpeed = 0.05f;

		public static bool AutoPickupSkipEmpty;

		public static bool AutoFarmOn;
		public static float AutoFarmHarvestRadius = 30f;
		public static float AutoFarmActionDelay = 0.5f;
		public static bool AutoFarmAutoReplant = true;
		public static bool AutoFarmAutoStore = true;
		public static bool AutoFarmAutoCraft;
		public static bool AutoFarmAutoEquipSeed = true;	public static bool AutoFarmSelectingCraft = false;
	public static ushort AutoFarmCraftItemID = 0;

	// Auto water (closest vanilla analog to "fertilizer": equipped water item plants/uses
	// fill plant waterGrowth; on farms built with a server-plugin fertilizer asset instead,
	// set this item ID to that specific item).
	public static bool AutoFarmWaterOn;
	public static ushort AutoFarmWaterItemID = 0;

        public static bool AutoFishOn;
		public static float AutoFishReelDelay = 0.15f;

		public static bool SoundsEnabled = true;

		// Auto Join
		public static bool AutoJoinOn;
		public static string AutoJoinIP = "";
		public static int AutoJoinPort = 27015;
		public static string AutoJoinPassword = "";
		public static bool AutoJoinPlayerLimit;
		public static int AutoJoinMaxPlayers = 10;
		public static float AutoJoinRetryDelay = 10f;

		public static bool HwidChangerOn = true;

		public static Color FriendColor = new Color(0.2f, 0.9f, 0.3f, 1f);

		public static bool FriendColorRb;

		public static float FriendColorRbSpd = 1f;

		public static Color EnemyColor = new Color(1f, 0.2f, 0.2f, 1f);

		public static bool EnemyColorRb;

		public static float EnemyColorRbSpd = 1f;

		public static bool SelfChams;

		public static Color SelfChamsColor = new Color(0.5f, 0.3f, 0.9f, 0.7f);

		public static bool SelfChamsRb;

		public static float SelfChamsRbSpd = 1f;

		public static bool AimbotOn;

		public static KeyCode AimbotKey = (KeyCode)324;

		public static bool AimbotSmooth = true;

		public static float AimbotSmoothness = 0.5f;

		public static bool AimbotFovRestrict = true;

		public static float AimbotFov = 120f;

		public static float AimbotMaxDist = 200f;

		public static bool AimbotVisCheck = true;

		public static int AimbotLimb;

		public static bool AimbotDrawFov = true;

		public static Color AimbotFovColor = Color.white;

		public static bool AimbotFovColorRb;

		public static float AimbotFovColorRbSpd = 1f;

		public static bool AimbotFriendly;

		public static bool AimbotTargetPlayers = true;

		public static bool AimbotTargetZombies = true;

		public static bool TriggerBot;

		public static KeyCode TriggerBotKey = (KeyCode)0;

		public static float TriggerBotDelay = 0.05f;

		public static bool TriggerBotHold;

		public static bool SilentAimOn;

		public static int SilentAimHitChance = 100;

		public static bool SilentAimFovRestrict = true;

		public static float SilentAimFov = 200f;

		public static float SilentAimMaxDist = 200f;

		public static bool SilentAimVisCheck = true;

		public static int SilentAimHitLimb;

		public static int SilentAimPredLimb;

		public static bool SilentAimDrawFov = true;

		public static Color SilentAimFovColor = new Color(1f, 0.3f, 0.3f, 0.4f);

		public static bool SilentAimFovColorRb;

		public static float SilentAimFovColorRbSpd = 1f;

		public static int SilentAimTargetPoint;

		public static Color SilentAimTargetColor = new Color(1f, 0.2f, 0.2f, 0.5f);

		public static bool SilentAimTargetColorRb;

		public static float SilentAimTargetColorRbSpd = 1f;

		public static bool SilentAimTargetStar = true;

		public static bool SilentAimTargetText = true;

		public static bool SilentAimTargetPlayers = true;

		public static bool SilentAimTargetZombies = true;

		public static bool SilentAimFriendly;

		// MoonClient-style sphere targeting
		public static int SilentAimStyle = 0; // 0 = Original, 1 = MoonClient Sphere

		public static bool SilentAimSphereEnabled;
		public static float SilentAimSphereSize = 1f;
		public static int SilentAimSphereSegments = 8;
		public static bool SilentAimSphereExtendRange;
		public static bool SilentAimSphereDebug;

		// Advanced hitbox selection
		public static bool SilentAimBestHitbox;
		public static bool SilentAimRandomLimb;
		public static float SilentAimRandomHeadChance = 50f;
		public static bool SilentAimHitboxToRoot;

		// Enhanced raycasting
		public static bool SilentAimStraightRaycast;
		public static bool SilentAimTraceFromOrigin;
		public static bool SilentAimVerifyForward;

		// Visual debugging
		public static bool SilentAimPreviewHitPoint;
		public static bool SilentAimDrawLineFromHitPoint;
		public static bool SilentAimDrawLineFromPlayerHead;

		// Smart distance
		public static bool SilentAimAutoGunRange;
		public static float SilentAimDistanceToHit = 15f;
		public static float SilentAimAimTargetDistance = 200f;

		// VehicleFly style
		public static int VehicleFlyStyle = 0; // 0 = Original, 1 = MoonClient

		// Cosmetic features
		public static bool HitMarker;
		public static float HitMarkerSize = 15f;
		public static Color HitMarkerColor = new Color(1f, 0f, 0f, 1f);
		public static bool HitMarkerColorRb;
		public static float HitMarkerColorRbSpd = 1f;
		public static float HitMarkerDuration = 0.3f;

		public static bool KillEffect;
		public static int KillEffectStyle = 0; // 0 = Screen flash, 1 = Text
		public static Color KillEffectColor = new Color(1f, 0.5f, 0f, 0.3f);
		public static bool KillEffectColorRb;
		public static float KillEffectColorRbSpd = 1f;

		public static bool FreeCamOn;

		public static float FreeCamSpeed = 10f;

		public static KeyCode FreeCamKey = (KeyCode)283;

		public static bool VehicleFlyOn;

		public static float VehicleFlySpeed = 1f;

		public static KeyCode VehicleFlyKey = (KeyCode)284;

		public static bool VehicleDamageOff;

		public static KeyCode VehicleDamageKey = (KeyCode)285;

		public static bool Footsteps;

		public static int FootstepShape;

		public static Color FootstepColor = new Color(0.5f, 0.3f, 0.9f, 0.6f);

		public static bool FootstepRb;

		public static float FootstepRbSpd = 1f;

		public static bool FootstepSpin;

		public static float FootstepSpinSpd = 90f;

		public static float FootstepLifetime = 1.5f;

		public static float FootstepSize = 1.5f;

		public static bool ZombieEsp;

		public static bool ZombieName = true;

		public static bool ZombieBox = true;

		public static bool ZombieSkeleton;

		public static bool ZombieSnapline;

		public static bool ZombieDistance = true;

		public static bool ZombieFill;

		public static bool ZombieHealth = true;

		public static Color ZombieBoxColor = new Color(0.7f, 0.7f, 0.1f);

		public static bool ZombieBoxRb;

		public static float ZombieBoxRbSpd = 1f;

		public static Color ZombieFillColor = new Color(0.7f, 0.7f, 0.1f, 0.1f);

		public static bool ZombieFillRb;

		public static float ZombieFillRbSpd = 1f;

		public static float ZombieFillAlpha = 0.1f;

		public static Color ZombieSkelColor = new Color(0.8f, 0.8f, 0.2f);

		public static bool ZombieSkelRb;

		public static float ZombieSkelRbSpd = 1f;

		public static Color ZombieSnapColor = new Color(0.7f, 0.7f, 0.1f);

		public static bool ZombieSnapRb;

		public static float ZombieSnapRbSpd = 1f;

		public static Color ZombieTextColor = new Color(1f, 0.95f, 0.5f, 0.9f);

		public static bool ZombieTextRb;

		public static float ZombieTextRbSpd = 1f;

		public static float ZombieMaxDist = 300f;

		public static float ZombieBoxThick = 1.5f;

		public static float ZombieSnapThick = 1f;

		public static int ZombieFontSize = 11;

		public static bool ZombieChams;

		public static Color ZombieChamsVis = new Color(1f, 1f, 0.1f, 0.85f);

		public static bool ZombieChamsVisRb;

		public static float ZombieChamsVisRbSpd = 1f;

		public static Color ZombieChamsNonVis = new Color(1f, 0.5f, 0f, 0.85f);

		public static bool ZombieChamsNonVisRb;

		public static float ZombieChamsNonVisRbSpd = 1f;

		public static bool ItemEsp;

		public static bool ItemSnapline;

		public static Color ItemTextColor = new Color(0.7f, 0.85f, 1f, 0.9f);

		public static bool ItemTextRb;

		public static float ItemTextRbSpd = 1f;

		public static Color ItemSnapColor = new Color(0.7f, 0.85f, 1f, 0.5f);

		public static bool ItemSnapRb;

		public static float ItemSnapRbSpd = 1f;

		public static float ItemMaxDist = 200f;

		public static int ItemFontSize = 10;

		public static bool ItemClump = true;

		public static bool ItemIcons;

		public static bool VehicleEsp;

		public static bool VehicleBox = true;

		public static bool VehicleName = true;

		public static bool VehicleLocked = true;

		public static bool VehicleHealth = true;

		public static bool VehicleSnapline;

		public static bool VehicleUnlockedOnly;

		public static bool VehicleFill;

		public static Color VehicleBoxColor = new Color(0.3f, 0.85f, 0.4f);

		public static bool VehicleBoxRb;

		public static float VehicleBoxRbSpd = 1f;

		public static Color VehicleFillColor = new Color(0.3f, 0.85f, 0.4f, 0.1f);

		public static bool VehicleFillRb;

		public static float VehicleFillRbSpd = 1f;

		public static float VehicleFillAlpha = 0.1f;

		public static Color VehicleSnapColor = new Color(0.3f, 0.85f, 0.4f);

		public static bool VehicleSnapRb;

		public static float VehicleSnapRbSpd = 1f;

		public static Color VehicleTextColor = new Color(0.85f, 1f, 0.85f, 0.9f);

		public static bool VehicleTextRb;

		public static float VehicleTextRbSpd = 1f;

		public static Color VehicleLockedColor = new Color(1f, 0.4f, 0.3f, 0.9f);

		public static bool VehicleLockedRb;

		public static float VehicleLockedRbSpd = 1f;

		public static float VehicleMaxDist = 500f;

		public static float VehicleBoxThick = 1.5f;

		public static float VehicleSnapThick = 1f;

		public static int VehicleFontSize = 11;

		public static bool VehicleChams;

		public static Color VehicleChamsVis = new Color(0.1f, 1f, 0.4f, 0.85f);

		public static bool VehicleChamsVisRb;

		public static float VehicleChamsVisRbSpd = 1f;

		public static Color VehicleChamsNonVis = new Color(1f, 0.4f, 0.1f, 0.85f);

		public static bool VehicleChamsNonVisRb;

		public static float VehicleChamsNonVisRbSpd = 1f;

		public static bool AnimalEsp;

		public static bool AnimalBox = true;

		public static bool AnimalName = true;

		public static bool AnimalSnapline;

		public static bool AnimalFill;

		public static Color AnimalBoxColor = new Color(0.9f, 0.6f, 0.2f);

		public static bool AnimalBoxRb;

		public static float AnimalBoxRbSpd = 1f;

		public static Color AnimalFillColor = new Color(0.9f, 0.6f, 0.2f, 0.1f);

		public static bool AnimalFillRb;

		public static float AnimalFillRbSpd = 1f;

		public static float AnimalFillAlpha = 0.1f;

		public static Color AnimalSnapColor = new Color(0.9f, 0.6f, 0.2f);

		public static bool AnimalSnapRb;

		public static float AnimalSnapRbSpd = 1f;

		public static Color AnimalTextColor = new Color(1f, 0.85f, 0.6f, 0.9f);

		public static bool AnimalTextRb;

		public static float AnimalTextRbSpd = 1f;

		public static float AnimalMaxDist = 300f;

		public static int AnimalFontSize = 10;

		public static bool StorageEsp;

		public static bool StorageBox = true;

		public static bool StorageName = true;

		public static bool StorageSnapline;

		public static bool StorageFill;

		public static Color StorageBoxColor = new Color(0.85f, 0.25f, 0.25f);

		public static bool StorageBoxRb;

		public static float StorageBoxRbSpd = 1f;

		public static Color StorageFillColor = new Color(0.85f, 0.25f, 0.25f, 0.1f);

		public static bool StorageFillRb;

		public static float StorageFillRbSpd = 1f;

		public static float StorageFillAlpha = 0.1f;

		public static Color StorageSnapColor = new Color(0.85f, 0.25f, 0.25f);

		public static bool StorageSnapRb;

		public static float StorageSnapRbSpd = 1f;

		public static Color StorageTextColor = new Color(1f, 0.7f, 0.7f, 0.9f);

		public static bool StorageTextRb;

		public static float StorageTextRbSpd = 1f;

		public static float StorageMaxDist = 300f;

		public static float StorageBoxThick = 1.5f;

		public static float StorageSnapThick = 1f;

		public static int StorageFontSize = 10;

		public static bool AirdropEsp;

		public static bool AirdropBox = true;

		public static bool AirdropName = true;

		public static bool AirdropSnapline;

		public static bool AirdropFill;

		public static Color AirdropBoxColor = new Color(1f, 0.85f, 0.15f);

		public static bool AirdropBoxRb;

		public static float AirdropBoxRbSpd = 1f;

		public static Color AirdropFillColor = new Color(1f, 0.85f, 0.15f, 0.1f);

		public static bool AirdropFillRb;

		public static float AirdropFillRbSpd = 1f;

		public static float AirdropFillAlpha = 0.1f;

		public static Color AirdropSnapColor = new Color(1f, 0.85f, 0.15f);

		public static bool AirdropSnapRb;

		public static float AirdropSnapRbSpd = 1f;

		public static Color AirdropTextColor = new Color(1f, 1f, 0.7f, 0.9f);

		public static bool AirdropTextRb;

		public static float AirdropTextRbSpd = 1f;

		public static float AirdropMaxDist = 2000f;

		public static float AirdropBoxThick = 2f;

		public static float AirdropSnapThick = 1.5f;

		public static int AirdropFontSize = 12;

		public static bool BedEsp;

		public static bool BedBox = true;

		public static bool BedName = true;

		public static bool BedSnapline;

		public static bool BedFill;

		public static bool BedClaimedOnly;

		public static bool BedShowClaimed = true;

		public static Color BedBoxColor = new Color(0.65f, 0.35f, 0.85f);

		public static bool BedBoxRb;

		public static float BedBoxRbSpd = 1f;

		public static Color BedFillColor = new Color(0.65f, 0.35f, 0.85f, 0.1f);

		public static bool BedFillRb;

		public static float BedFillRbSpd = 1f;

		public static float BedFillAlpha = 0.1f;

		public static Color BedSnapColor = new Color(0.65f, 0.35f, 0.85f);

		public static bool BedSnapRb;

		public static float BedSnapRbSpd = 1f;

		public static Color BedTextColor = new Color(0.85f, 0.7f, 1f, 0.9f);

		public static bool BedTextRb;

		public static float BedTextRbSpd = 1f;

		public static float BedMaxDist = 500f;

		public static float BedBoxThick = 1.5f;

		public static float BedSnapThick = 1f;

		public static int BedFontSize = 10;

		public static bool GenEsp;

		public static bool GenBox = true;

		public static bool GenName = true;

		public static bool GenSnapline;

		public static bool GenFill;

		public static Color GenBoxColor = new Color(0.95f, 0.65f, 0.15f);

		public static bool GenBoxRb;

		public static float GenBoxRbSpd = 1f;

		public static Color GenFillColor = new Color(0.95f, 0.65f, 0.15f, 0.1f);

		public static bool GenFillRb;

		public static float GenFillRbSpd = 1f;

		public static float GenFillAlpha = 0.1f;

		public static Color GenSnapColor = new Color(0.95f, 0.65f, 0.15f);

		public static bool GenSnapRb;

		public static float GenSnapRbSpd = 1f;

		public static Color GenTextColor = new Color(1f, 0.9f, 0.6f, 0.9f);

		public static bool GenTextRb;

		public static float GenTextRbSpd = 1f;

		public static float GenMaxDist = 300f;

		public static float GenBoxThick = 1.5f;

		public static float GenSnapThick = 1f;

		public static int GenFontSize = 10;

		public static bool TurretEsp;

		public static bool TurretBox = true;

		public static bool TurretName = true;

		public static bool TurretSnapline;

		public static bool TurretFill;

		public static Color TurretBoxColor = new Color(1f, 0.3f, 0.3f);

		public static bool TurretBoxRb;

		public static float TurretBoxRbSpd = 1f;

		public static Color TurretFillColor = new Color(1f, 0.3f, 0.3f, 0.1f);

		public static bool TurretFillRb;

		public static float TurretFillRbSpd = 1f;

		public static float TurretFillAlpha = 0.1f;

		public static Color TurretSnapColor = new Color(1f, 0.3f, 0.3f);

		public static bool TurretSnapRb;

		public static float TurretSnapRbSpd = 1f;

		public static Color TurretTextColor = new Color(1f, 0.7f, 0.7f, 0.9f);

		public static bool TurretTextRb;

		public static float TurretTextRbSpd = 1f;

		public static float TurretMaxDist = 500f;

		public static float TurretBoxThick = 1.5f;

		public static float TurretSnapThick = 1f;

		public static int TurretFontSize = 10;

		public static bool GrenadeEsp;

		public static bool GrenadeBox = true;

		public static bool GrenadeName = true;

		public static bool GrenadeSnapline;

		public static bool GrenadeRadius = true;

		public static Color GrenadeBoxColor = new Color(1f, 0.2f, 0.2f);

		public static bool GrenadeBoxRb;

		public static float GrenadeBoxRbSpd = 1f;

		public static Color GrenadeSnapColor = new Color(1f, 0.2f, 0.2f);

		public static bool GrenadeSnapRb;

		public static float GrenadeSnapRbSpd = 1f;

		public static Color GrenadeTextColor = new Color(1f, 0.6f, 0.6f, 0.9f);

		public static bool GrenadeTextRb;

		public static float GrenadeTextRbSpd = 1f;

		public static Color GrenadeRadColor = new Color(1f, 0.2f, 0.2f, 0.4f);

		public static bool GrenadeRadRb;

		public static float GrenadeRadRbSpd = 1f;

		public static float GrenadeMaxDist = 300f;

		public static bool BulletEsp;

		public static bool BulletSelf = true;

		public static bool BulletOthers = true;

		public static bool BulletSnapline;

		public static bool BulletTrail = true;

		public static Color BulletSelfColor = new Color(0.3f, 0.9f, 1f, 0.9f);

		public static bool BulletSelfRb;

		public static float BulletSelfRbSpd = 1f;

		public static Color BulletOtherColor = new Color(1f, 0.3f, 0.3f, 0.9f);

		public static bool BulletOtherRb;

		public static float BulletOtherRbSpd = 1f;

		public static Color BulletSnapColor = new Color(1f, 0.5f, 0.2f);

		public static bool BulletSnapRb;

		public static float BulletSnapRbSpd = 1f;

		public static float BulletMaxDist = 500f;

		public static float BulletLifetime = 3f;

		public static int BulletFontSize = 10;

		public static bool MeleeTracers;

		public static Color MeleeTracerColor = new Color(1f, 0.3f, 0.3f, 0.8f);

		public static bool MeleeTracerRb;

		public static float MeleeTracerRbSpd = 1f;

		public static int BulletPattern;

		public static int GrenadeFontSize = 11;

		public static bool WeaponInfo;

		public static bool WeaponChams;

		public static bool WeaponWireframe;

		public static Color WeaponChamsColor = new Color(0.3f, 0.7f, 1f, 0.8f);

		public static bool WeaponChamsRb;

		public static float WeaponChamsRbSpd = 1f;

		public static bool DisableScope;

		public static bool DisableBino;

		public static bool InstantAim;

		public static float SpreadMult = 1f;

		public static float RecoilMult = 1f;

		public static float SwayMult = 1f;

		public static bool MeleeReach;

		public static float MeleeReachExtra = 4f;

		public static bool ForceHeadshot;

		public static bool NoBallistics;

		public static bool ExtendBallisticRange;

		public static int ExtraBallisticSteps = 4;

		public static bool AutoSemiBurst;

		public static float DamageFlinchMult = 1f;

		public static bool Spinbot;

		public static int SpinType;

		public static bool SpinShow;

		public static bool SpinStar;

		public static bool IgnoreLeaveTimer;

		public static bool DamageNumbers;

        public static float SpyBlockUntil = 0f;

        public static float DmgNumLifetime = 2f;

		public static int DmgNumFontSize = 14;

		public static bool DmgNumCustomColor;

		public static Color DmgNumColor = new Color(1f, 0.3f, 0.3f, 1f);

		public static bool DmgNumColorRb;

		public static float DmgNumColorRbSpd = 1f;

		private static Color[] _bases = new Color[64];

		private static bool[] _rbInit = new bool[64];
	}
}
