using System;
using System.Collections.Generic;
using System.Reflection;
using SDG.Unturned;
using UnityEngine;
public static class MiscConfig
{
	// (get) Token: 0x06000070 RID: 112 RVA: 0x00006558 File Offset: 0x00004758
	// (set) Token: 0x06000071 RID: 113 RVA: 0x00006570 File Offset: 0x00004770
	[ConfigBindAttribute("Misc options", "Modify player perspective")]
	[SaveableNameAttribute]
	public static bool modifyPlayerPerspective
	{
		get
		{
			return MiscConfig.modifyPlayerPerspectiveBacking;
		}
		set
		{
			bool flag = value != MiscConfig.modifyPlayerPerspectiveBacking && !value && Player.player != null;
			if (flag)
			{
				bool flag2;
				if (Player.player.look.perspective == EPlayerPerspective.THIRD)
				{
					ECameraMode d66xkrBp6Z1GTuSAAF2uDhRmK = MiscConfig.ForcedCameraMode;
					flag2 = MiscConfig.ForcedCameraMode == ECameraMode.VEHICLE && Player.player.movement.getVehicle() == null;
				}
				else
				{
					flag2 = false;
				}
				bool flag3 = flag2;
				if (flag3)
				{
					MiscConfig.SetActivePerspectiveMethod.InvokeOn(Player.player.look, new object[] { 0 });
				}
				else
				{
					bool flag4 = Player.player.look.perspective == EPlayerPerspective.FIRST && MiscConfig.ForcedCameraMode == ECameraMode.THIRD;
					if (flag4)
					{
						MiscConfig.SetActivePerspectiveMethod.InvokeOn(Player.player.look, new object[] { 1 });
					}
				}
				Provider.cameraMode = MiscConfig.ForcedCameraMode;
			}
			else
			{
				bool flag5 = value != MiscConfig.modifyPlayerPerspectiveBacking && value && Provider.isConnected;
				if (flag5)
				{
					Provider.cameraMode = MiscConfig.playerPerspective;
				}
			}
			MiscConfig.modifyPlayerPerspectiveBacking = value;
		}
	}
	// (get) Token: 0x06000072 RID: 114 RVA: 0x00006688 File Offset: 0x00004888
	// (set) Token: 0x06000073 RID: 115 RVA: 0x000066A0 File Offset: 0x000048A0
	[ConfigBindAttribute("Misc options", "Enable custom local player model")]
	[SaveableNameAttribute]
	public static bool replaceLocalPlayerModel
	{
		get
		{
			return MiscConfig.replaceLocalPlayerModelBacking;
		}
		set
		{
			bool flag = MiscConfig.replaceLocalPlayerModelBacking != value;
			if (flag)
			{
				bool flag2 = !value;
				if (flag2)
				{
					DTungTungSahurModel.CleanupModel();
				}
				else
				{
					DTungTungSahurModel.FullCleanup();
				}
			}
			MiscConfig.replaceLocalPlayerModelBacking = value;
		}
	}
	// (get) Token: 0x06000074 RID: 116 RVA: 0x000066E0 File Offset: 0x000048E0
	// (set) Token: 0x06000075 RID: 117 RVA: 0x000066F8 File Offset: 0x000048F8
	[ConfigBindAttribute("Misc options", "Custom model type")]
	[SaveableNameAttribute]
	public static DCustomModelType customModelType
	{
		get
		{
			return MiscConfig.customModelTypeBacking;
		}
		set
		{
			bool flag = MiscConfig.customModelTypeBacking != value;
			if (flag)
			{
				MiscConfig.customModelTypeBacking = value;
				bool dtungTungSahurModelEnabled = MiscConfig.replaceLocalPlayerModelBacking;
				if (dtungTungSahurModelEnabled)
				{
					DTungTungSahurModel.FullCleanup();
				}
			}
		}
	}
	// (get) Token: 0x06000076 RID: 118 RVA: 0x00006730 File Offset: 0x00004930
	// (set) Token: 0x06000077 RID: 119 RVA: 0x00006748 File Offset: 0x00004948
	[ConfigBindAttribute("Misc options", "Freecamera")]
	[SaveableNameAttribute]
	public static bool freeCamera
	{
		get
		{
			return MiscConfig.freeCameraBacking;
		}
		set
		{
			bool flag = MiscConfig.freeCameraBacking != value;
			if (flag)
			{
				if (value)
				{
					FreeCamera.Instance = new GameObject("RTSCamera").AddComponent<FreeCamera>();
				}
				else
				{
					bool flag2 = FreeCamera.Instance != null;
					if (flag2)
					{
						UnityEngine.Object.Destroy(FreeCamera.Instance.gameObject);
					}
				}
			}
			MiscConfig.freeCameraBacking = value;
		}
	}
	// (get) Token: 0x06000078 RID: 120 RVA: 0x000067AC File Offset: 0x000049AC
	// (set) Token: 0x06000079 RID: 121 RVA: 0x000067C4 File Offset: 0x000049C4
	[ConfigBindAttribute("Misc options", "Custom vehicle behaviour")]
	[SaveableNameAttribute]
	public static bool customVehicleBehaviour
	{
		get
		{
			return MiscConfig.customVehicleBehaviourBacking;
		}
		set
		{
			bool flag = value != MiscConfig.customVehicleBehaviourBacking && !value && VehicleBehaviour.instance != null && VehicleBehaviour.instance.vehicleRigidbody != null;
			if (flag)
			{
				VehicleBehaviour.RestoreVehicleCollision(VehicleBehaviour.DvehCollisionVehicle);
				VehicleBehaviour.instance.vehicleRigidbody.useGravity = true;
				VehicleBehaviour.instance.vehicleRigidbody.isKinematic = false;
			}
			MiscConfig.customVehicleBehaviourBacking = value;
		}
	}
	// (get) Token: 0x0600007A RID: 122 RVA: 0x00006838 File Offset: 0x00004A38
	// (set) Token: 0x0600007B RID: 123 RVA: 0x00006850 File Offset: 0x00004A50
	[ConfigBindAttribute("Misc options", "Imit nightvision")]
	[SaveableNameAttribute]
	public static bool imitNightvision
	{
		get
		{
			return MiscConfig.imitNightvisionBacking;
		}
		set
		{
			bool flag = MiscConfig.imitNightvisionBacking != value && !ScreenshotManager.IsSpying;
			if (flag)
			{
				try
				{
					if (value)
					{
						switch (MiscConfig.nightVisionType)
						{
						case NightVisionType.Military:
							LevelLighting.nightvisionColor = new Color32(20, 120, 80, 0);
							LevelLighting.nightvisionFogIntensity = 0.2f;
							break;
						case NightVisionType.Civilian:
							LevelLighting.nightvisionColor = new Color(0.4f, 0.4f, 0.4f, 0f);
							LevelLighting.nightvisionFogIntensity = 0.2f;
							break;
						case NightVisionType.Custom:
						{
							Color color = ColorConfig.GetColor("Custom nightvision color");
							LevelLighting.nightvisionColor = new Color(color.r, color.g, color.b);
							LevelLighting.nightvisionFogIntensity = color.a;
							break;
						}
						}
						MiscConfig.SavedLightingVision = LevelLighting.vision;
						LevelLighting.vision = MiscConfig.nightVisionType.ToLightingVision();
					}
					else
					{
						LevelLighting.vision = MiscConfig.SavedLightingVision;
					}
					LevelLighting.updateLighting();
					try
					{
						LevelLighting.updateLocal();
					}
					catch (Exception ex)
					{
						Debug.LogWarning("[imitNightvision] updateLocal error: " + ex.Message);
					}
					try
					{
						PlayerLifeUiHooks.UpdateGrayscaleHook();
					}
					catch (Exception ex2)
					{
						Debug.LogWarning("[imitNightvision] UpdateGrayscaleHook error: " + ex2.Message);
					}
				}
				catch (Exception ex3)
				{
					Debug.LogWarning("[imitNightvision] Error toggling nightvision: " + ex3.Message);
				}
			}
			MiscConfig.imitNightvisionBacking = value;
		}
	}
	// (get) Token: 0x0600007C RID: 124 RVA: 0x00006A1C File Offset: 0x00004C1C
	// (set) Token: 0x0600007D RID: 125 RVA: 0x00006A34 File Offset: 0x00004C34
	[ConfigBindAttribute("Misc options", "Use custom FOV")]
	[SaveableNameAttribute]
	public static bool useCustomFOV
	{
		get
		{
			return MiscConfig.useCustomFOVBacking;
		}
		set
		{
			bool flag = MiscConfig.useCustomFOVBacking != value;
			if (flag)
			{
				if (value)
				{
					MiscConfig.OriginalFov = OptionsSettings.fov;
				}
				else
				{
					OptionsSettings.fov = MiscConfig.OriginalFov;
				}
			}
			MiscConfig.useCustomFOVBacking = value;
		}
	}
	// (get) Token: 0x0600007E RID: 126 RVA: 0x00006A7C File Offset: 0x00004C7C
	// (set) Token: 0x0600007F RID: 127 RVA: 0x00006A94 File Offset: 0x00004C94
	[ConfigBindAttribute("Misc options", "Vehicle noclip")]
	[SaveableNameAttribute]
	public static bool vehicleNoclip
	{
		get
		{
			return MiscConfig.vehicleNoclipBacking;
		}
		set
		{
			bool flag = MiscConfig.vehicleNoclipBacking != value && !value && Player.player != null && Player.player.movement.getVehicle() != null && VehicleBehaviour.originalPhysicsProfile != null;
			if (flag)
			{
				VehicleBehaviour.RestoreVehicleCollision(Player.player.movement.getVehicle());
				VehicleBehaviour.originalPhysicsProfile.applyTo(Player.player.movement.getVehicle());
				bool flag2 = VehicleBehaviour.instance != null;
				if (flag2)
				{
					VehicleBehaviour.instance.vehicleRigidbody.useGravity = true;
					VehicleBehaviour.instance.vehicleRigidbody.isKinematic = false;
				}
			}
			MiscConfig.vehicleNoclipBacking = value;
		}
	}
	// (get) Token: 0x06000080 RID: 128 RVA: 0x00006B4C File Offset: 0x00004D4C
	// (set) Token: 0x06000081 RID: 129 RVA: 0x00006B64 File Offset: 0x00004D64
	[ConfigBindAttribute("Misc options", "Chat spamming")]
	[SaveableNameAttribute]
	public static bool chatSpamming
	{
		get
		{
			return MiscConfig.m_chatSpamming;
		}
		set
		{
			bool flag = MiscConfig.m_chatSpamming != value;
			if (flag)
			{
				ChatSpammer.SetSpamEnabled(value);
			}
			MiscConfig.m_chatSpamming = value;
		}
	}
	// (get) Token: 0x06000082 RID: 130 RVA: 0x00006B90 File Offset: 0x00004D90
	// (set) Token: 0x06000083 RID: 131 RVA: 0x00006BA8 File Offset: 0x00004DA8
	[ConfigBindAttribute("Misc options", "Custom FOV")]
	[SaveableNameAttribute]
	public static float customFOV
	{
		get
		{
			return MiscConfig.customFOVBacking;
		}
		set
		{
			bool flag = !(value <= 1000f);
			if (flag)
			{
				value = 100f;
			}
			bool useCustomFOV = MiscConfig.useCustomFOV;
			if (useCustomFOV)
			{
				OptionsSettings.fov = value;
			}
			MiscConfig.customFOVBacking = value;
		}
	}
	// (get) Token: 0x06000084 RID: 132 RVA: 0x00006BD0 File Offset: 0x00004DD0
	// (set) Token: 0x06000085 RID: 133 RVA: 0x00006BE8 File Offset: 0x00004DE8
	[ConfigBindAttribute("Misc options", "Night vision type")]
	[SaveableNameAttribute]
	public static NightVisionType nightVisionType
	{
		get
		{
			return MiscConfig.nightVisionTypeBacking;
		}
		set
		{
			bool flag = MiscConfig.nightVisionTypeBacking != value && MiscConfig.imitNightvision && !ScreenshotManager.IsSpying;
			if (flag)
			{
				switch (value)
				{
				case NightVisionType.Military:
					LevelLighting.nightvisionColor = new Color32(20, 120, 80, 0);
					LevelLighting.nightvisionFogIntensity = 0.2f;
					break;
				case NightVisionType.Civilian:
					LevelLighting.nightvisionColor = new Color(0.4f, 0.4f, 0.4f, 0f);
					LevelLighting.nightvisionFogIntensity = 0.2f;
					break;
				case NightVisionType.Custom:
				{
					Color color = ColorConfig.GetColor("Custom nightvision color");
					LevelLighting.nightvisionColor = new Color(color.r, color.g, color.b);
					LevelLighting.nightvisionFogIntensity = color.a;
					break;
				}
				}
				LevelLighting.vision = value.ToLightingVision();
				LevelLighting.updateLighting();
				LevelLighting.updateLocal();
				PlayerLifeUiHooks.UpdateGrayscaleHook();
			}
			MiscConfig.nightVisionTypeBacking = value;
		}
	}
	// (get) Token: 0x06000086 RID: 134 RVA: 0x00006CDC File Offset: 0x00004EDC
	// (set) Token: 0x06000087 RID: 135 RVA: 0x00006CF4 File Offset: 0x00004EF4
	[ConfigBindAttribute("Misc options", "Modified player perspective")]
	[SaveableNameAttribute]
	public static ECameraMode playerPerspective
	{
		get
		{
			return MiscConfig.playerPerspectiveBacking;
		}
		set
		{
			bool flag = MiscConfig.playerPerspectiveBacking != value;
			if (flag)
			{
				bool isConnected = Provider.isConnected;
				if (isConnected)
				{
					Provider.cameraMode = value;
				}
				bool flag2 = Player.player != null && !ScreenshotManager.IsSpying;
				if (flag2)
				{
					bool flag3 = value == ECameraMode.FIRST && Player.player.look.perspective == EPlayerPerspective.THIRD;
					if (flag3)
					{
						MiscConfig.SetActivePerspectiveMethod.InvokeOn(Player.player.look, new object[] { 0 });
					}
					else
					{
						bool flag4 = value == ECameraMode.THIRD && Player.player.look.perspective == EPlayerPerspective.FIRST;
						if (flag4)
						{
							MiscConfig.SetActivePerspectiveMethod.InvokeOn(Player.player.look, new object[] { 1 });
						}
						else
						{
							bool flag5 = value == ECameraMode.VEHICLE && Player.player.look.perspective == EPlayerPerspective.THIRD && Player.player.movement.getVehicle() == null;
							if (flag5)
							{
								MiscConfig.SetActivePerspectiveMethod.InvokeOn(Player.player.look, new object[] { 0 });
							}
						}
					}
				}
			}
			MiscConfig.playerPerspectiveBacking = value;
		}
	}
	// (get) Token: 0x06000088 RID: 136 RVA: 0x00006E38 File Offset: 0x00005038
	// (set) Token: 0x06000089 RID: 137 RVA: 0x00006E50 File Offset: 0x00005050
	[ConfigBindAttribute("Misc options", "Use custom aspect ratio")]
	[SaveableNameAttribute]
	public static bool useCustomAspectRatio
	{
		get
		{
			return MiscConfig.useCustomAspectRatioBacking;
		}
		set
		{
			bool flag = MiscConfig.useCustomAspectRatioBacking != value;
			if (flag)
			{
				if (value)
				{
					Camera main = Camera.main;
					bool flag2 = main != null;
					if (flag2)
					{
						MiscConfig.OriginalAspectRatio = main.aspect;
					}
					bool flag3 = MainCamera.instance != null;
					if (flag3)
					{
						MiscConfig.OriginalAspectRatio = MainCamera.instance.aspect;
					}
					bool flag4 = MiscConfig.customAspectRatioBacking > 0f;
					if (flag4)
					{
						Camera main2 = Camera.main;
						bool flag5 = main2 != null;
						if (flag5)
						{
							main2.aspect = MiscConfig.customAspectRatioBacking;
						}
						bool flag6 = MainCamera.instance != null;
						if (flag6)
						{
							MainCamera.instance.aspect = MiscConfig.customAspectRatioBacking;
						}
					}
				}
				else
				{
					Camera main3 = Camera.main;
					bool flag7 = main3 != null;
					if (flag7)
					{
						main3.aspect = MiscConfig.OriginalAspectRatio;
					}
					bool flag8 = MainCamera.instance != null;
					if (flag8)
					{
						MainCamera.instance.aspect = MiscConfig.OriginalAspectRatio;
					}
				}
			}
			MiscConfig.useCustomAspectRatioBacking = value;
		}
	}
	// (get) Token: 0x0600008A RID: 138 RVA: 0x00006F68 File Offset: 0x00005168
	// (set) Token: 0x0600008B RID: 139 RVA: 0x00006F80 File Offset: 0x00005180
	[ConfigBindAttribute("Misc options", "Custom aspect ratio")]
	[SaveableNameAttribute]
	public static float customAspectRatio
	{
		get
		{
			return MiscConfig.customAspectRatioBacking;
		}
		set
		{
			MiscConfig.customAspectRatioBacking = value;
			bool useCustomAspectRatio = MiscConfig.useCustomAspectRatio;
			if (useCustomAspectRatio)
			{
				bool flag = value > 0f;
				if (flag)
				{
					Camera main = Camera.main;
					bool flag2 = main != null;
					if (flag2)
					{
						main.aspect = value;
					}
					bool flag3 = MainCamera.instance != null;
					if (flag3)
					{
						MainCamera.instance.aspect = value;
					}
				}
				else
				{
					Camera main2 = Camera.main;
					bool flag4 = main2 != null;
					if (flag4)
					{
						main2.aspect = MiscConfig.OriginalAspectRatio;
					}
					bool flag5 = MainCamera.instance != null;
					if (flag5)
					{
						MainCamera.instance.aspect = MiscConfig.OriginalAspectRatio;
					}
				}
			}
		}
	}
	private static ReflectedMethod SetActivePerspectiveMethod = new ReflectedMethod(typeof(PlayerLook), "setActivePerspective", BindingFlags.Instance | BindingFlags.NonPublic);
	[ConfigBindAttribute("Misc options", "Correct firerate to work")]
	[SaveableNameAttribute]
	public static bool correctFirerateToWork = true;
	[ConfigBindAttribute("Misc options", "No flash")]
	[SaveableNameAttribute]
	public static bool noFlash = true;
	[ConfigBindAttribute("Misc options", "No grayscale")]
	[SaveableNameAttribute]
	public static bool noGrayscale = true;
	[ConfigBindAttribute("Misc options", "No pain")]
	[SaveableNameAttribute]
	public static bool noPain = true;
	[ConfigBindAttribute("Misc options", "No blur")]
	[SaveableNameAttribute]
	public static bool noBlur = true;
	[ConfigBindAttribute("Misc options", "No flinch")]
	[SaveableNameAttribute]
	public static bool noFlinch = true;
	[ConfigBindAttribute("Misc options", "No hallucinations")]
	[SaveableNameAttribute]
	public static bool noHallucinations = true;
	[ConfigBindAttribute("Misc options", "Local leans")]
	[SaveableNameAttribute]
	public static bool localLeans = false;
	[ConfigBindAttribute("Misc options", "Free leans")]
	[SaveableNameAttribute]
	public static bool freeLeans = false;
	[ConfigBindAttribute("Misc options", "Skip asset verifying")]
	[SaveableNameAttribute]
	public static bool skipAssetVerifying = false;
	[ConfigBindAttribute("Misc options", "Fake lag")]
	[SaveableNameAttribute]
	public static bool fakeLag = false;
	[ConfigBindAttribute("Misc options", "Ignore barricade placement errors")]
	[SaveableNameAttribute]
	public static bool ignoreBarricadePlacementErrors = false;
	[ConfigBindAttribute("Misc options", "Ignore structure placement errors")]
	[SaveableNameAttribute]
	public static bool ignoreStructurePlacementErrors = false;
	[ConfigBindAttribute("Misc options", "Imit compass in inventory")]
	[SaveableNameAttribute]
	public static bool imitCompassInInventory = false;
	[ConfigBindAttribute("Misc options", "Imit map in inventory")]
	[SaveableNameAttribute]
	public static bool imitMapInInventory = false;
	public static bool modifyPlayerPerspectiveBacking = false;
	public static bool freeCameraBacking = false;
	[ConfigBindAttribute("Misc options", "Instant aiming")]
	[SaveableNameAttribute]
	public static bool instantAiming = true;
	[ConfigBindAttribute("Misc options", "Custom day time")]
	[SaveableNameAttribute]
	public static bool customDayTime = false;
	[ConfigBindAttribute("Misc options", "Display all player marks on map")]
	[SaveableNameAttribute]
	public static bool displayAllPlayerMarksOnMap = false;
	[ConfigBindAttribute("Misc options", "Display all players on map")]
	[SaveableNameAttribute]
	public static bool displayAllPlayersOnMap = true;
	[ConfigBindAttribute("Misc options", "Disable scope overlay")]
	[SaveableNameAttribute]
	public static bool disableScopeOverlayH = false;
	[ConfigBindAttribute("Misc options", "Disable binocularus overlay")]
	[SaveableNameAttribute]
	public static bool disableBinocularOverlay = false;
	public static bool customVehicleBehaviourBacking = false;
	[ConfigBindAttribute("Misc options", "Chat on kill")]
	[SaveableNameAttribute]
	public static bool chatOnKill = false;
	[ConfigBindAttribute("Misc options", "Ignore leave timer")]
	[SaveableNameAttribute]
	public static bool ignoreLeaveTimer = false;
	[ConfigBindAttribute("Misc options", "No ballistics")]
	[SaveableNameAttribute]
	public static bool noBallistics = false;
	[ConfigBindAttribute("Misc options", "Pickup items through walls")]
	[SaveableNameAttribute]
	public static bool pickupItemsThroughWalls = false;
	[ConfigBindAttribute("Misc options", "Extended melee range")]
	[SaveableNameAttribute]
	public static bool extendMeleeRange = false;
	[ConfigBindAttribute("Misc options", "Modify move behaviour")]
	[SaveableNameAttribute]
	public static bool modifyMoveBehaviour = false;
	[ConfigBindAttribute("Misc options", "Show move modifying")]
	[SaveableNameAttribute]
	public static bool showMoveModifying = false;
	[ConfigBindAttribute("Misc options", "Player spinbot draw direction")]
	[SaveableNameAttribute]
	public static bool playerSpinbotDrawDirection = true;
	[ConfigBindAttribute("Misc options", "Player spinbot speed")]
	[SaveableNameAttribute]
	public static float playerSpinbotSpeed = 360f;
	[ConfigBindAttribute("Misc options", "Player spinbot random pitch")]
	[SaveableNameAttribute]
	public static bool playerSpinbotRandomPitch = false;
	[ConfigBindAttribute("Misc options", "Player spinbot desync")]
	[SaveableNameAttribute]
	public static bool playerSpinbotDesync = false;
	[ConfigBindAttribute("Misc options", "Player spinbot desync amount")]
	[SaveableNameAttribute]
	public static float playerSpinbotDesyncAmount = 90f;
	[ConfigBindAttribute("Misc options", "Player spinbot anti-aim max distance")]
	[SaveableNameAttribute]
	public static float playerSpinbotAntiAimDistance = 200f;
	[ConfigBindAttribute("Misc options", "Replace hit limb to custom")]
	[SaveableNameAttribute]
	public static bool replaceHitLimbToCustom = false;
	[ConfigBindAttribute("Misc options", "Show weapon info")]
	[SaveableNameAttribute]
	public static bool showWeaponInfo = false;
	[ConfigBindAttribute("Misc options", "Use static rect for weapon info")]
	[SaveableNameAttribute]
	public static bool useStaticRectForWeaponInfo = false;
	[ConfigBindAttribute("Misc options", "Extend player region")]
	[SaveableNameAttribute]
	public static bool extendPlayerRegion = false;
	[ConfigBindAttribute("Misc options", "Extend region interact through walls")]
	[SaveableNameAttribute]
	public static bool extendRegionInteractThroughWalls = false;
	[ConfigBindAttribute("Misc options", "Change vehicle leave velocity")]
	[SaveableNameAttribute]
	public static bool changeVehicleLeaveVelocity = false;
	[ConfigBindAttribute("Misc options", "Use forward velocity")]
	[SaveableNameAttribute]
	public static bool useForwardVelocity = false;
	[ConfigBindAttribute("Misc options", "Random swap face")]
	[SaveableNameAttribute]
	public static bool randomSwapingFace = false;
	[ConfigBindAttribute("Misc options", "Notify on admin join")]
	[SaveableNameAttribute]
	public static bool notifyOnAdminJoin = false;
	[ConfigBindAttribute("Misc options", "Independent player info targeting")]
	[SaveableNameAttribute]
	public static bool independentPlayerInfoTargeting = false;
	public static bool imitNightvisionBacking = false;
	public static bool useCustomFOVBacking = false;
	public static bool vehicleNoclipBacking = false;
	[ConfigBindAttribute("Misc options", "Automatic semi-bust")]
	[SaveableNameAttribute]
	public static bool automaticSemiBurst = false;
	[ConfigBindAttribute("Misc options", "Auto item pickup")]
	[SaveableNameAttribute]
	public static bool autoItemPickup = false;
	[ConfigBindAttribute("Misc options", "Chat spamming")]
	[SaveableNameAttribute]
	public static bool m_chatSpamming = false;
	[ConfigBindAttribute("Misc options", "Auto fishing")]
	[SaveableNameAttribute]
	public static bool autoFishing = false;
	[ConfigBindAttribute("Misc options", "Auto fishing max wait")]
	[SaveableNameAttribute]
	public static int autoFishingMaxWait = 30;
	[ConfigBindAttribute("Misc options", "Auto fishing show stats")]
	[SaveableNameAttribute]
	public static bool autoFishingShowStats = true;
	[ConfigBindAttribute("Misc options", "Auto fishing catch delay")]
	[SaveableNameAttribute]
	public static float autoFishingCatchDelay = 0.3f;
	[ConfigBindAttribute("Misc options", "Auto fishing auto equip")]
	[SaveableNameAttribute]
	public static bool autoFishingAutoEquip = false;
	[ConfigBindAttribute("Misc options", "Auto farm harvest")]
	[SaveableNameAttribute]
	public static bool autoFarmHarvest = false;
	[ConfigBindAttribute("Misc options", "Auto farm fertilize")]
	[SaveableNameAttribute]
	public static bool autoFarmFertilize = false;
	[ConfigBindAttribute("Misc options", "Auto farm plant")]
	[SaveableNameAttribute]
	public static bool autoFarmPlant = false;
	[ConfigBindAttribute("Misc options", "Auto farm plant seed index")]
	[SaveableNameAttribute]
	public static int autoFarmPlantSeedIndex = -1;
	[ConfigBindAttribute("Misc options", "Auto place plant")]
	[SaveableNameAttribute]
	public static bool autoPlacePlant = false;
	[ConfigBindAttribute("Misc options", "Auto place plant seed index")]
	[SaveableNameAttribute]
	public static int autoPlacePlantSeedIndex = -1;
	[ConfigBindAttribute("Misc options", "Show farm grid")]
	[SaveableNameAttribute]
	public static bool showFarmGrid = false;
	[ConfigBindAttribute("Misc options", "Show farm grid placement")]
	[SaveableNameAttribute]
	public static bool showFarmGridPlacement = false;
	[ConfigBindAttribute("Misc options", "Auto farm show stats")]
	[SaveableNameAttribute]
	public static bool autoFarmShowStats = true;
	[ConfigBindAttribute("Misc options", "Show farm stat planted")]
	[SaveableNameAttribute]
	public static bool showFarmStatPlanted = true;
	[ConfigBindAttribute("Misc options", "Show farm stat harvested")]
	[SaveableNameAttribute]
	public static bool showFarmStatHarvested = true;
	[ConfigBindAttribute("Misc options", "Show farm stat fertilized")]
	[SaveableNameAttribute]
	public static bool showFarmStatFertilized = false;
	[ConfigBindAttribute("Misc options", "Show farm stat growing")]
	[SaveableNameAttribute]
	public static bool showFarmStatGrowing = true;
	[ConfigBindAttribute("Misc options", "Show farm stat grown")]
	[SaveableNameAttribute]
	public static bool showFarmStatGrown = true;
	[ConfigBindAttribute("Misc options", "Show farm stat empty")]
	[SaveableNameAttribute]
	public static bool showFarmStatEmpty = false;
	[ConfigBindAttribute("Misc options", "Auto farm craft")]
	[SaveableNameAttribute]
	public static bool autoFarmCraft = false;
	[ConfigBindAttribute("Misc options", "Auto farm store")]
	[SaveableNameAttribute]
	public static bool autoFarmStore = false;
	[ConfigBindAttribute("Misc options", "Auto farm store radius")]
	[SaveableNameAttribute]
	public static int autoFarmStoreRadius = 10;
	[ConfigBindAttribute("Misc options", "Show farm stat crafted")]
	[SaveableNameAttribute]
	public static bool showFarmStatCrafted = false;
	[ConfigBindAttribute("Misc options", "Show farm stat stored")]
	[SaveableNameAttribute]
	public static bool showFarmStatStored = false;
	[ConfigBindAttribute("Misc options", "Kill sound")]
	[SaveableNameAttribute]
	public static bool killSound = true;
	[ConfigBindAttribute("Misc options", "Hit sound")]
	[SaveableNameAttribute]
	public static bool hitSound = true;
	[ConfigBindAttribute("Misc options", "Custom hit sound (.wav)")]
	[SaveableNameAttribute]
	public static bool customHitSound = false;

	[ConfigBindAttribute("Misc options", "Hit sound volume")]
	[SaveableNameAttribute]
	public static float hitSoundVolume = 1f;
	[ConfigBindAttribute("Misc options", "Silent aim target box")]
	[SaveableNameAttribute]
	public static bool silentAimTargetBox = false;
	[ConfigBindAttribute("Misc options", "Randomize nickname by server players")]
	[SaveableNameAttribute]
	public static bool randomizeNicknameByServerPlayers = false;
	[ConfigBindAttribute("Misc options", "Spam chat zone")]
	[SaveableNameAttribute]
	public static EChatMode spamChatZone = EChatMode.GLOBAL;
	[ConfigBindAttribute("Misc options", "Change move rotation")]
	[SaveableNameAttribute]
	public static bool changeMoveRotation = true;
	[ConfigBindAttribute("Misc options", "Display player info")]
	[SaveableNameAttribute]
	public static bool displayPlayerInfo = false;
	[ConfigBindAttribute("Misc options", "Display player group members")]
	[SaveableNameAttribute]
	public static bool displayPlayerGroupMembers = true;
	[ConfigBindAttribute("Misc options", "Display player info always")]
	[SaveableNameAttribute]
	public static bool displayPlayerInfoAlways = false;
	[ConfigBindAttribute("Misc options", "Notify about spy")]
	[SaveableNameAttribute]
	public static bool notifyAboutSpy = true;
	[ConfigBindAttribute("Misc options", "Third camera ignore obstacles")]
	[SaveableNameAttribute]
	public static bool thirdCameraIgnoreObstacles = false;
	[ConfigBindAttribute("Misc options", "Extend ballistic range")]
	[SaveableNameAttribute]
	public static bool extendBallisticRange = false;
	[ConfigBindAttribute("Misc options", "Unclamp camera rotation")]
	[SaveableNameAttribute]
	public static bool unclampCameraRotation = false;
	[ConfigBindAttribute("Misc options", "Custom build offset")]
	[SaveableNameAttribute]
	public static bool customBuildOffset = false;
	[ConfigBindAttribute("Misc options", "Override sun color")]
	[SaveableNameAttribute]
	public static bool overrideSunColor = false;
	[ConfigBindAttribute("Misc options", "Override sky color")]
	[SaveableNameAttribute]
	public static bool overrideSkyColor = false;
	[ConfigBindAttribute("Misc options", "Override clouds color")]
	[SaveableNameAttribute]
	public static bool overrideCloudsColor = false;
	[ConfigBindAttribute("Misc options", "Override clouds rim color")]
	[SaveableNameAttribute]
	public static bool overrideCloudsRimColor = false;
	[ConfigBindAttribute("Misc options", "Display vanish players window")]
	[SaveableNameAttribute]
	public static bool displayVanishPlayersWindow = false;
	[ConfigBindAttribute("Misc options", "Raw walk")]
	[SaveableNameAttribute]
	public static bool rawWalk = false;
	[ConfigBindAttribute("Misc options", "Third camera distance")]
	[SaveableNameAttribute]
	public static float thirdCameraDistance = 0f;
	[ConfigBindAttribute("Misc options", "Spy delay timer")]
	[SaveableNameAttribute]
	public static float spyDelayTimer = 0.6f;
	[ConfigBindAttribute("Misc options", "Face swap delay")]
	[SaveableNameAttribute]
	public static float faceSwapDelay = 1f;
	[ConfigBindAttribute("Misc options", "Chat spam delay")]
	[SaveableNameAttribute]
	public static float chatSpamDelay = 1f;
	[ConfigBindAttribute("Misc options", "Vehicle speed")]
	[SaveableNameAttribute]
	public static float vehicleSpeed = 1f;
	[ConfigBindAttribute("Misc options", "Vehicle mouse move")]
	[SaveableNameAttribute]
	public static bool vehicleMouseMove = false;
	[ConfigBindAttribute("Misc options", "Vehicle camera distance")]
	[SaveableNameAttribute]
	public static float vehicleCameraDistance = 12f;
	[ConfigBindAttribute("Misc options", "Vehicle camera height")]
	[SaveableNameAttribute]
	public static float vehicleCameraHeight = 6f;
	[ConfigBindAttribute("Misc options", "Vehicle camera smoothing")]
	[SaveableNameAttribute]
	public static float vehicleCameraSmooth = 8f;
	[ConfigBindAttribute("Misc options", "Vehicle lock rotation")]
	[SaveableNameAttribute]
	public static bool vehicleLockRotationBacking = false;
	public static bool vehicleLockRotation
	{
		get
		{
			return MiscConfig.vehicleLockRotationBacking;
		}
		set
		{
			bool flag = MiscConfig.vehicleLockRotationBacking != value && !value && Player.player != null && Player.player.movement.getVehicle() != null && VehicleBehaviour.instance != null && VehicleBehaviour.instance.vehicleRigidbody != null;
			if (flag)
			{
				VehicleBehaviour.instance.vehicleRigidbody.useGravity = true;
				VehicleBehaviour.instance.vehicleRigidbody.isKinematic = false;
			}
			MiscConfig.vehicleLockRotationBacking = value;
		}
	}
	[ConfigBindAttribute("Misc options", "Vehicle lock rotation turn speed")]
	[SaveableNameAttribute]
	public static float vehicleLockRotationTurnSpeed = 90f;
	[ConfigBindAttribute("Misc options", "Vehicle spinbot")]
	[SaveableNameAttribute]
	public static bool vehicleSpinbot = false;
	[ConfigBindAttribute("Misc options", "Vehicle spinbot type")]
	[SaveableNameAttribute]
	public static DxVehSpinType vehicleSpinbotType = DxVehSpinType.Flip180;
	[ConfigBindAttribute("Misc options", "Vehicle spinbot speed")]
	[SaveableNameAttribute]
	public static float vehicleSpinbotSpeed = 360f;
	[ConfigBindAttribute("Misc options", "Vehicle spinbot draw direction")]
	[SaveableNameAttribute]
	public static bool vehicleSpinbotDrawDirection = true;
	[ConfigBindAttribute("Misc options", "Set vehicle position to ground on spy")]
	[SaveableNameAttribute]
	public static bool vehicleGroundOnSpy = false;
	[ConfigBindAttribute("Misc options", "Vehicle player collision")]
	[SaveableNameAttribute]
	public static bool useVehiclePhysics = false;
	[ConfigBindAttribute("Misc options", "Sway multiplier")]
	[SaveableNameAttribute]
	public static float swayMultiplier = 0.3f;
	[ConfigBindAttribute("Misc options", "Spread multiplier")]
	[SaveableNameAttribute]
	public static float spreadMultiplier = 0.4f;
	[ConfigBindAttribute("Misc options", "Recoil multiplier")]
	[SaveableNameAttribute]
	public static float recoilMultiplier = 0.3f;
	[ConfigBindAttribute("Misc options", "Recoil impact multiplier")]
	[SaveableNameAttribute]
	public static float recoilImpactMultiplier = 0.3f;
	[ConfigBindAttribute("Misc options", "Damage punch multiplier")]
	[SaveableNameAttribute]
	public static float damagePunchMultiplier = 1f;
	[ConfigBindAttribute("Misc options", "Salvage time multiplier")]
	[SaveableNameAttribute]
	public static float salvageTimeMultiplier = 1f;
	[ConfigBindAttribute("Misc options", "Build forward offset")]
	[SaveableNameAttribute]
	public static float buildForwardOffset = 2f;
	[ConfigBindAttribute("Misc options", "Build Y offset")]
	[SaveableNameAttribute]
	public static float buildYOffset = 1f;
	[ConfigBindAttribute("Misc options", "Custom time")]
	[SaveableNameAttribute]
	public static float customTime = 0.5f;
	public static float customFOVBacking = 60f;
	public static float OriginalFov = 60f;
	[ConfigBindAttribute("Misc options", "Firerate decrease")]
	[SaveableNameAttribute]
	public static int firerateDecrease = 0;
	[ConfigBindAttribute("Misc options", "Additional ballistic steps")]
	[SaveableNameAttribute]
	public static int additionalBallisticSteps = 4;
	[ConfigBindAttribute("Misc options", "Spy window size")]
	[SaveableNameAttribute]
	public static int spyWindowSize = 200;
	[ConfigBindAttribute("Misc options", "Player info window size")]
	[SaveableNameAttribute]
	public static int playerInfoWindowSize = 50;
	[ConfigBindAttribute("Misc options", "Extend region range")]
	[SaveableNameAttribute]
	public static int extendRegionRange = 20;
	[ConfigBindAttribute("Misc options", "Auto item pickup distance")]
	[SaveableNameAttribute]
	public static int autoItemPickupDistance = 20;
	[ConfigBindAttribute("Misc options", "Auto item pickup delay")]
	[SaveableNameAttribute]
	public static float autoItemPickupDelay = 0.1f;
	[ConfigBindAttribute("Misc options", "Auto farm distance")]
	[SaveableNameAttribute]
	public static int autoFarmDistance = 20;
	[ConfigBindAttribute("Misc options", "Freeze farm grid keybind")]
	[SaveableNameAttribute]
	public static KeyCode farmGridFreezeKey = KeyCode.None;
	public static bool waitingForFarmGridFreezeKey = false;
	[ConfigBindAttribute("Misc options", "Zoom exploit")]
	[SaveableNameAttribute]
	public static bool zoomExploit = false;
	[ConfigBindAttribute("Misc options", "Zoom toggle mode")]
	[SaveableNameAttribute]
	public static bool zoomToggle = false;
	[ConfigBindAttribute("Misc options", "Zoom amount")]
	[SaveableNameAttribute]
	public static float zoomAmount = 2f;
	[ConfigBindAttribute("Misc options", "Zoom keybind")]
	[SaveableNameAttribute]
	public static KeyCode zoomKeybind = KeyCode.None;
	public static bool waitingForZoomKey = false;
	public static bool zoomActive = false;
	public static float zoomOriginalFOV = 0f;
	public static float zoomOriginalOptionsFOV = 0f;
	[ConfigBindAttribute("Misc options", "Pickup items through walls distance")]
	[SaveableNameAttribute]
	public static int pickupItemsThroughWallsDistance = 20;
	[ConfigBindAttribute("Misc options", "Independent player info targeting distance")]
	[SaveableNameAttribute]
	public static int independetPlayerInfoTargetingDistance = 600;
	[ConfigBindAttribute("Misc options", "Vehicle velocity X")]
	[SaveableNameAttribute]
	public static int vehilceVelocityX = 15;
	[ConfigBindAttribute("Misc options", "Vehicle velocity Y")]
	[SaveableNameAttribute]
	public static int vehilceVelocityY = 15;
	[ConfigBindAttribute("Misc options", "Vehicle velocity Z")]
	[SaveableNameAttribute]
	public static int vehilceVelocityZ = 15;
	[ConfigBindAttribute("Misc options", "Vehicle velocity forward")]
	[SaveableNameAttribute]
	public static int vehilceVelocityForward = 15;
	[ConfigBindAttribute("Misc options", "Freecamera speed")]
	[SaveableNameAttribute]
	public static int freeCameraSpeed = 5;
	public static int UnusedIntSetting1 = 4;
	public static int UnusedIntSetting2 = 1;
	public static byte[] SpoofedHwid1 = new byte[0];
	public static byte[] SpoofedHwid2 = new byte[0];
	public static byte[] SpoofedHwid3 = new byte[0];
	[ConfigBindAttribute("Misc options", "Replaced hit limb")]
	[SaveableNameAttribute]
	public static Limb replacedHitLimb = Limb.Head;
	[ConfigBindAttribute("Misc options", "Spy type")]
	[SaveableNameAttribute]
	public static AvatarSpyMode spyType = AvatarSpyMode.SpyInFourFrames;
	[ConfigBindAttribute("Misc options", "HWID send type")]
	[SaveableNameAttribute]
	public static HwidMode hwidType = HwidMode.SendRandomHWID;
	[ConfigBindAttribute("Misc options", "Move type")]
	[SaveableNameAttribute]
	public static MoveType moveType = MoveType.FourTactSpin;
	public static ELightingVision SavedLightingVision = ELightingVision.NONE;
	public static NightVisionType nightVisionTypeBacking = NightVisionType.Military;
	public static ECameraMode ForcedCameraMode;
	public static ECameraMode playerPerspectiveBacking = ECameraMode.BOTH;
	[SaveableNameAttribute("spamText")]
	public static string spamText = "Hamas On top Best free Hack at discord.g g/hamasclient";
	[SaveableNameAttribute("adminNotifyText")]
	public static string adminNotifyText = "{1} ({0}) admin joined on the server!";
	[SaveableNameAttribute("killText")]
	public static string killText = "ez";
	[SaveableNameAttribute("killAudioName")]
	public static string killAudioName = "Cheat hit";
	[SaveableNameAttribute("hitAudioName")]
	public static string hitAudioName = "Cheat hit";
	public static Dictionary<ushort, ItemInfo> AutoPickupWhitelist = new Dictionary<ushort, ItemInfo>();
	public static bool useCustomAspectRatioBacking = false;
	public static float OriginalAspectRatio = 0f;
	public static float customAspectRatioBacking = 0f;
	[ConfigBindAttribute("Misc options", "Custom model scale")]
	[SaveableNameAttribute]
	public static float tungTungSahurModelScale = 1.8f;
	[ConfigBindAttribute("Misc options", "Show custom model on spy")]
	[SaveableNameAttribute]
	public static bool showCustomModelOnSpy = false;
	private static bool replaceLocalPlayerModelBacking = false;
	private static DCustomModelType customModelTypeBacking = DCustomModelType.TungTungSahur;
	[ConfigBindAttribute("Misc options", "Toggle vehicle player collision")]
	public static void ToggleVehiclePhysics()
	{
		useVehiclePhysics = !useVehiclePhysics;
		if (useVehiclePhysics && Player.player != null && Player.player.movement.getVehicle() != null)
		{
			VehicleBehaviour.ApplyVehiclePlayerOnlyCollision(Player.player.movement.getVehicle());
		}
		else if (!useVehiclePhysics)
		{
			VehicleBehaviour.RestoreVehicleCollision(VehicleBehaviour.DvehCollisionVehicle);
		}
	}
}
