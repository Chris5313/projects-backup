using System;
using System.Collections.Generic;
using UnityEngine;
public static class AimbotConfig
{
	// (get) Token: 0x06000112 RID: 274 RVA: 0x0000C2B0 File Offset: 0x0000A4B0
	public static bool enableAim
	{
		get
		{
			return AimbotConfig.enableSilentAim || AimbotConfig.enableMeleeSilentAim || AimbotConfig.enableAimbot || AimbotConfig.enableAutoShoot;
		}
	}
	// (get) Token: 0x06000113 RID: 275 RVA: 0x0000C2E0 File Offset: 0x0000A4E0
	public static bool expandRangeBySphere
	{
		get
		{
			return AimbotConfig.enableSilentAim && AimbotConfig.silentAimType == SilentAimType.Sphere && AimbotConfig.additiveRangeBySphereSize;
		}
	}
	[ConfigBindAttribute("Aim options", "Toggle Memory Aimbot")]
	public static void ToggleMemoryAimbot()
	{
		AimbotConfig.enableAimbot = !AimbotConfig.enableAimbot;
	}
	[ConfigBindAttribute("Aim options", "Toggle Silent Aimbot")]
	public static void ToggleSilentAim()
	{
		AimbotConfig.enableSilentAim = !AimbotConfig.enableSilentAim;
	}
	// (get) Token: 0x06000116 RID: 278 RVA: 0x0000C32C File Offset: 0x0000A52C
	public static float bulletDelaySecondsSafe
	{
		get
		{
			return (AimbotConfig.bulletDelaySeconds > 0f) ? AimbotConfig.bulletDelaySeconds : 0.0001f;
		}
	}
	public static bool IsMemoryAimbotKeyActive()
	{
		bool flag = AimbotConfig.memoryAimbotKeybind == KeyCode.None;
		return flag || Input.GetKey(AimbotConfig.memoryAimbotKeybind);
	}
	[ConfigBindAttribute("Aim options", "Enable aimbot")]
	[SaveableNameAttribute]
	public static bool enableAimbot = false;
	[ConfigBindAttribute("Aim options", "Enable silent aim")]
	[SaveableNameAttribute]
	public static bool enableSilentAim = true;
	[ConfigBindAttribute("Aim options", "Enable auto shoot")]
	[SaveableNameAttribute]
	public static bool enableAutoShoot = false;
	[ConfigBindAttribute("Aim options", "Enable melee silent aim")]
	[SaveableNameAttribute]
	public static bool enableMeleeSilentAim = true;
	public static List<AimSphereOptions> SphereSizes = new List<AimSphereOptions>
	{
		new AimSphereOptions(1f, 8)
	};
	public static float MaxSphereSize = 1f;
	[ConfigBindAttribute("Aim options", "Distance to hit")]
	[SaveableNameAttribute]
	public static int distanceToHit = 15;
	[ConfigBindAttribute("Aim options", "Aim target distance")]
	[SaveableNameAttribute]
	public static int aimTargetDistance = 200;
	[ConfigBindAttribute("Aim options", "Aiming chance")]
	[SaveableNameAttribute]
	public static int aimingChance = 75;
	[ConfigBindAttribute("Aim options", "Hit mark size")]
	[SaveableNameAttribute]
	public static int hitMarkSize = 10;
	[ConfigBindAttribute("Aim options", "Preview hit point")]
	[SaveableNameAttribute]
	public static bool previewHitPoint = true;
	[ConfigBindAttribute("Aim options", "Draw line from hit point")]
	[SaveableNameAttribute]
	public static bool drawLineFromHitPoint = false;
	[ConfigBindAttribute("Aim options", "Draw line from player head")]
	[SaveableNameAttribute]
	public static bool drawLineFromPlayerHead = false;
	[ConfigBindAttribute("Aim options", "Smooth aimbot")]
	[SaveableNameAttribute]
	public static bool smoothAimbot = true;
	[ConfigBindAttribute("Aim options", "Smooth aimbot speed")]
	[SaveableNameAttribute]
	public static float smoothAimbotSpeed = 3f;
	[ConfigBindAttribute("Aim options", "Restrict aim by fov")]
	[SaveableNameAttribute]
	public static bool restrictAimByFov = true;
	[ConfigBindAttribute("Aim options", "Check aim with linecast")]
	[SaveableNameAttribute]
	public static bool checkWithLinecast = true;
	[ConfigBindAttribute("Aim options", "Always aim")]
	[SaveableNameAttribute]
	public static bool alwaysAim = false;
	[ConfigBindAttribute("Aim options", "Draw target")]
	[SaveableNameAttribute]
	public static bool drawTarget = true;
	[ConfigBindAttribute("Aim options", "Set distance by gun range")]
	[SaveableNameAttribute]
	public static bool setDistanceByGunRange = true;
	[ConfigBindAttribute("Aim options", "Straight raycasting")]
	[SaveableNameAttribute]
	public static bool straightRaycasting = true;
	[ConfigBindAttribute("Aim options", "Trace from origin")]
	[SaveableNameAttribute]
	public static bool traceFromOrigin = false;
	[ConfigBindAttribute("Aim options", "Verify forward hit point availability")]
	[SaveableNameAttribute]
	public static bool verifyForwardHitPointAviablity = true;
	[ConfigBindAttribute("Aim options", "Verify trace by linecast")]
	[SaveableNameAttribute]
	public static bool verifyTraceByLinecast = false;
	[ConfigBindAttribute("Aim options", "Verify sphere to player point by linecast")]
	[SaveableNameAttribute]
	public static bool verifySphereToPlayerPointByLinecast = false;
	[ConfigBindAttribute("Aim options", "Set hit point to transform")]
	[SaveableNameAttribute]
	public static bool hitPointToTransform = true;
	[ConfigBindAttribute("Aim options", "Manually calculate ballistic distance")]
	[SaveableNameAttribute]
	public static bool manuallyCalculateBallisticDistance = true;
	[ConfigBindAttribute("Aim options", "Hook sphere point to bullet")]
	[SaveableNameAttribute]
	public static bool hookSpherePointToBullet = true;
	[ConfigBindAttribute("Aim options", "Don't shoot players on safezone")]
	[SaveableNameAttribute]
	public static bool dontShootPlayersOnSafezone = true;
	[ConfigBindAttribute("Aim options", "Additive range by sphere size")]
	[SaveableNameAttribute]
	public static bool additiveRangeBySphereSize = true;
	[ConfigBindAttribute("Aim options", "Prefer camera hit point")]
	[SaveableNameAttribute]
	public static bool setHitPointToCameraIfAviable = true;
	[ConfigBindAttribute("Aim options", "Debug sphere points")]
	[SaveableNameAttribute]
	public static bool debugSpherePoints = false;
	[ConfigBindAttribute("Aim options", "Target best silent aim hitbox")]
	[SaveableNameAttribute]
	public static bool bestSilentAimPartPreselective = true;
	[ConfigBindAttribute("Aim options", "Target best aimbot hitbox")]
	[SaveableNameAttribute]
	public static bool bestAimbotPartPreselective = true;
	[ConfigBindAttribute("Aim options", "Preview hit limb")]
	[SaveableNameAttribute]
	public static bool previewHitLimb = true;
	[ConfigBindAttribute("Aim options", "Bullet delaying")]
	[SaveableNameAttribute]
	public static bool bulletDelaying = false;
	[ConfigBindAttribute("Aim options", "Show bullet delaying timer")]
	[SaveableNameAttribute]
	public static bool showBulletDelayingTimer = true;
	[ConfigBindAttribute("Aim options", "Release delay on mouse up")]
	[SaveableNameAttribute]
	public static bool unholdDelayByMouse = true;
	[ConfigBindAttribute("Aim options", "Instant release on mouse up")]
	[SaveableNameAttribute]
	public static bool momentalyUnhold = false;
	[ConfigBindAttribute("Aim options", "Bullet delay amount")]
	[SaveableNameAttribute]
	public static int bulletDelayAmount = 15;
	[ConfigBindAttribute("Aim options", "Bullet delay seconds")]
	[SaveableNameAttribute]
	public static float bulletDelaySeconds = 0f;
	[ConfigBindAttribute("Aim options", "Bullet delay keybind")]
	[SaveableNameAttribute]
	public static KeyCode bulletDelayKeybind = KeyCode.F;
	public static bool waitingForDelayKey = false;
	[ConfigBindAttribute("Aim options", "Target line start X")]
	[SaveableNameAttribute]
	public static float targetLineStartX = 0.5f;
	[ConfigBindAttribute("Aim options", "Target line start Y")]
	[SaveableNameAttribute]
	public static float targetLineStartY = 0.5f;
	[ConfigBindAttribute("Aim options", "Aim sorting")]
	[SaveableNameAttribute]
	public static AimSorting aimSorting = AimSorting.FOV;
	[ConfigBindAttribute("Aim options", "Silent aim type")]
	[SaveableNameAttribute]
	public static SilentAimType silentAimType = SilentAimType.Sphere;
	[ConfigBindAttribute("Aim options", "Aimbot limb")]
	[SaveableNameAttribute]
	public static Limb aimbotLimb = Limb.Head;
	[ConfigBindAttribute("Aim options", "Silent aim limb")]
	[SaveableNameAttribute]
	public static Limb silentAimLimb = Limb.Head;
	[ConfigBindAttribute("Aim options", "Silent aim hit point limb")]
	[SaveableNameAttribute]
	public static Limb silentAimHitPointLimb = Limb.Head;
	[ConfigBindAttribute("Aim options", "Random limb head hit chance")]
	[SaveableNameAttribute]
	public static int randomLimbHeadHitChance = 50;
	[ConfigBindAttribute("Aim options", "Sticky aim")]
	[SaveableNameAttribute]
	public static bool stickyAim = false;
	public static List<TargetType> TargetTypes = new List<TargetType> { TargetType.Player };
	[ConfigBindAttribute("Aim options", "Enable vehicle hitbox exploit")]
	[SaveableNameAttribute]
	public static bool enableVehicleHitboxExploit = false;
	[ConfigBindAttribute("Aim options", "Vehicle hitbox exploit distance")]
	[SaveableNameAttribute]
	public static float vehicleHitboxExploitDistance = 40f;
	[ConfigBindAttribute("Aim options", "Auto best sphere size")]
	[SaveableNameAttribute]
	public static bool autoBestSphereSize = false;
	[ConfigBindAttribute("Aim options", "Enable backtrack")]
	[SaveableNameAttribute]
	public static bool enableBacktrack = false;
	[ConfigBindAttribute("Aim options", "Show backtrack visualizer")]
	[SaveableNameAttribute]
	public static bool showBacktrackVisualizer = false;
	[ConfigBindAttribute("Aim options", "Backtrack time (ms)")]
	[SaveableNameAttribute]
	public static float backtrackTimeMs = 200f;
	[ConfigBindAttribute("Aim options", "Backtrack max history")]
	[SaveableNameAttribute]
	public static int backtrackMaxHistory = 128;
	public static bool BacktrackFlag = false;
	[ConfigBindAttribute("Aim options", "Memory aimbot FOV")]
	[SaveableNameAttribute]
	public static int memoryAimbotFOV = 200;
	[ConfigBindAttribute("Aim options", "Memory aimbot check walls")]
	[SaveableNameAttribute]
	public static bool memoryAimbotCheckWalls = true;
	[ConfigBindAttribute("Aim options", "Memory aimbot keybind")]
	[SaveableNameAttribute]
	public static KeyCode memoryAimbotKeybind = KeyCode.None;
	public static bool waitingForMemoryAimKey = false;
}
