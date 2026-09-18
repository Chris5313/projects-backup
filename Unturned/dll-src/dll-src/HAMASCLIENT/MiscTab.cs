using System;
using SDG.Unturned;
using UnityEngine;
public class MiscTab : FeatureTabBase
{
	public override string GetName()
	{
		return "Misc";
	}
	public override int SortId()
	{
		return 2;
	}
	public override void DoTab(TabCount tc)
	{
		bool flag = tc == TabCount.One;
		if (flag)
		{
			base.DrawSectionHeader("Misc");
			this.PlayerEffectsScroll = GUILayout.BeginScrollView(this.PlayerEffectsScroll, Array.Empty<GUILayoutOption>());
			MiscConfig.noFlash = MenuGuiHelper.DrawCheckbox(MiscConfig.noFlash, "No flash", Array.Empty<GUILayoutOption>());
			MiscConfig.noGrayscale = MenuGuiHelper.DrawCheckbox(MiscConfig.noGrayscale, "No grayscale", Array.Empty<GUILayoutOption>());
			MiscConfig.noPain = MenuGuiHelper.DrawCheckbox(MiscConfig.noPain, "No pain", Array.Empty<GUILayoutOption>());
			MiscConfig.noBlur = MenuGuiHelper.DrawCheckbox(MiscConfig.noBlur, "No blur", Array.Empty<GUILayoutOption>());
			MiscConfig.noFlinch = MenuGuiHelper.DrawCheckbox(MiscConfig.noFlinch, "No flinch", Array.Empty<GUILayoutOption>());
			MiscConfig.noHallucinations = MenuGuiHelper.DrawCheckbox(MiscConfig.noHallucinations, "No hallucinations", Array.Empty<GUILayoutOption>());
			MiscConfig.rawWalk = MenuGuiHelper.DrawCheckbox(MiscConfig.rawWalk, "Raw walk", Array.Empty<GUILayoutOption>());
			MiscConfig.instantAiming = MenuGuiHelper.DrawCheckbox(MiscConfig.instantAiming, "Instant aiming", Array.Empty<GUILayoutOption>());
			MiscConfig.imitCompassInInventory = MenuGuiHelper.DrawCheckbox(MiscConfig.imitCompassInInventory, "Show compass in inventory", Array.Empty<GUILayoutOption>());
			MiscConfig.imitMapInInventory = MenuGuiHelper.DrawCheckbox(MiscConfig.imitMapInInventory, "Show map in inventory", Array.Empty<GUILayoutOption>());
			MiscConfig.displayAllPlayersOnMap = MenuGuiHelper.DrawCheckbox(MiscConfig.displayAllPlayersOnMap, "Display all players on map", Array.Empty<GUILayoutOption>());
			MiscConfig.displayAllPlayerMarksOnMap = MenuGuiHelper.DrawCheckbox(MiscConfig.displayAllPlayerMarksOnMap, "Display all player marks on map", Array.Empty<GUILayoutOption>());
			MiscConfig.disableBinocularOverlay = MenuGuiHelper.DrawCheckbox(MiscConfig.disableBinocularOverlay, "Disable binocular overlay blackout", Array.Empty<GUILayoutOption>());
			MiscConfig.disableScopeOverlayH = MenuGuiHelper.DrawCheckbox(MiscConfig.disableScopeOverlayH, "Disable scope overlay blackout", Array.Empty<GUILayoutOption>());
			MiscConfig.ignoreBarricadePlacementErrors = MenuGuiHelper.DrawCheckbox(MiscConfig.ignoreBarricadePlacementErrors, "Ignore barricade placement errors", Array.Empty<GUILayoutOption>());
			MiscConfig.ignoreStructurePlacementErrors = MenuGuiHelper.DrawCheckbox(MiscConfig.ignoreStructurePlacementErrors, "Ignore structure placement errors", Array.Empty<GUILayoutOption>());
			MiscConfig.ignoreLeaveTimer = MenuGuiHelper.DrawCheckbox(MiscConfig.ignoreLeaveTimer, "Ignore leave timer", Array.Empty<GUILayoutOption>());
			
			MiscConfig.silentAimTargetBox = MenuGuiHelper.DrawCheckbox(MiscConfig.silentAimTargetBox, "Melee target box (3D red)", Array.Empty<GUILayoutOption>());
			MiscConfig.extendMeleeRange = MenuGuiHelper.DrawCheckbox(MiscConfig.extendMeleeRange, "Extended melee range", Array.Empty<GUILayoutOption>());
			MiscConfig.noBallistics = MenuGuiHelper.DrawCheckbox(MiscConfig.noBallistics, "Remove ballistics", Array.Empty<GUILayoutOption>());
			MiscConfig.thirdCameraIgnoreObstacles = MenuGuiHelper.DrawCheckbox(MiscConfig.thirdCameraIgnoreObstacles, "Ignore obstacles in third person", Array.Empty<GUILayoutOption>());
			MiscConfig.overrideSkyColor = MenuGuiHelper.DrawCheckbox(MiscConfig.overrideSkyColor, "Override sky color", Array.Empty<GUILayoutOption>());
			MiscConfig.overrideSunColor = MenuGuiHelper.DrawCheckbox(MiscConfig.overrideSunColor, "Override sun color", Array.Empty<GUILayoutOption>());
			MiscConfig.overrideCloudsColor = MenuGuiHelper.DrawCheckbox(MiscConfig.overrideCloudsColor, "Override clouds color", Array.Empty<GUILayoutOption>());
			MiscConfig.overrideCloudsRimColor = MenuGuiHelper.DrawCheckbox(MiscConfig.overrideCloudsRimColor, "Override clouds rim color", Array.Empty<GUILayoutOption>());
			MiscConfig.unclampCameraRotation = MenuGuiHelper.DrawCheckbox(MiscConfig.unclampCameraRotation, "Unclamp camera rotation", Array.Empty<GUILayoutOption>());
			if (MenuState.vanishedPlayersWindowEnabled)
				MiscConfig.displayVanishPlayersWindow = MenuGuiHelper.DrawCheckbox(MiscConfig.displayVanishPlayersWindow, "Display player vanish window", Array.Empty<GUILayoutOption>());
			MiscConfig.skipAssetVerifying = MenuGuiHelper.DrawCheckbox(MiscConfig.skipAssetVerifying, "Skip asset verifying", Array.Empty<GUILayoutOption>());
			MiscConfig.automaticSemiBurst = MenuGuiHelper.DrawCheckbox(MiscConfig.automaticSemiBurst, "Automatic semi-burst", Array.Empty<GUILayoutOption>());
			MiscConfig.hitSound = MenuGuiHelper.DrawCheckbox(MiscConfig.hitSound, "Hit sound", Array.Empty<GUILayoutOption>());
			bool hitSound = MiscConfig.hitSound;
			if (hitSound)
			{
				MiscConfig.customHitSound = MenuGuiHelper.DrawCheckbox(MiscConfig.customHitSound, "Custom hit sound (hitsound.wav)", Array.Empty<GUILayoutOption>());
				if (!MiscConfig.customHitSound)
				{
					int hitIdx = System.Array.IndexOf(GuiStyles.SoundNames, MiscConfig.hitAudioName);
					if (hitIdx < 0) hitIdx = 0;
					if (MenuGuiHelper.Button("Hit sound: " + MiscConfig.hitAudioName, -1, true, null))
					{
						hitIdx = (hitIdx + 1) % GuiStyles.SoundNames.Length;
						MiscConfig.hitAudioName = GuiStyles.SoundNames[hitIdx];
					}
				}
				MiscConfig.hitSoundVolume = MenuGuiHelper.LabeledFloatSlider("Hit volume: ", MiscConfig.hitSoundVolume, 0f, 5f, -1);
			}
			MiscConfig.replaceLocalPlayerModel = MenuGuiHelper.DrawCheckbox(MiscConfig.replaceLocalPlayerModel, "Replace local player model", Array.Empty<GUILayoutOption>());
			bool replaceLocalPlayerModel = MiscConfig.replaceLocalPlayerModel;
			if (replaceLocalPlayerModel)
			{
				MiscConfig.customModelType = MiscConfig.customModelType.EnumPopup("Custom Model: ", "");
				MiscConfig.tungTungSahurModelScale = MenuGuiHelper.LabeledFloatSlider("Model size: ", MiscConfig.tungTungSahurModelScale, 0.1f, 5f, -1);
				MiscConfig.showCustomModelOnSpy = MenuGuiHelper.DrawCheckbox(MiscConfig.showCustomModelOnSpy, "Show custom model on spy", Array.Empty<GUILayoutOption>());
			}
			MiscConfig.showWeaponInfo = MenuGuiHelper.DrawCheckbox(MiscConfig.showWeaponInfo, "Show weapon information", Array.Empty<GUILayoutOption>());
			bool showWeaponInfo = MiscConfig.showWeaponInfo;
			if (showWeaponInfo)
			{
				MiscConfig.useStaticRectForWeaponInfo = MenuGuiHelper.DrawCheckbox(MiscConfig.useStaticRectForWeaponInfo, "Static target information window", Array.Empty<GUILayoutOption>());
			}
			MiscConfig.imitNightvision = MenuGuiHelper.DrawCheckbox(MiscConfig.imitNightvision, "Force nightvision", Array.Empty<GUILayoutOption>());
			bool flag2 = MiscConfig.imitNightvision && MenuGuiHelper.Button(string.Format("Nightvision type: {0}", MiscConfig.nightVisionType), -1, true, null);
			if (flag2)
			{
				MiscConfig.nightVisionType = MiscConfig.nightVisionType.Next<NightVisionType>();
			}
			MiscConfig.notifyOnAdminJoin = MenuGuiHelper.DrawCheckbox(MiscConfig.notifyOnAdminJoin, "Notify on admin join", Array.Empty<GUILayoutOption>());
			bool notifyOnAdminJoin = MiscConfig.notifyOnAdminJoin;
			if (notifyOnAdminJoin)
			{
				GUILayout.Label("Notify text:", Array.Empty<GUILayoutOption>());
				MiscConfig.adminNotifyText = GUILayout.TextField(MiscConfig.adminNotifyText, Array.Empty<GUILayoutOption>());
			}
			MiscConfig.chatSpamming = MenuGuiHelper.DrawCheckbox(MiscConfig.chatSpamming, "Spam in chat", Array.Empty<GUILayoutOption>());
			MiscConfig.chatSpamDelay = MenuGuiHelper.LabeledFloatSlider("Spam delay: ", MiscConfig.chatSpamDelay, 0.05f, 2f, -1);
			GUILayout.Label("Spam text:", Array.Empty<GUILayoutOption>());
			MiscConfig.spamText = GUILayout.TextField(MiscConfig.spamText, Array.Empty<GUILayoutOption>());
			MiscConfig.spamChatZone = MiscConfig.spamChatZone.EnumPopup("Spam chat zone: ", "");
			MiscConfig.chatOnKill = MenuGuiHelper.DrawCheckbox(MiscConfig.chatOnKill, "Chat on kill", Array.Empty<GUILayoutOption>());
			bool chatOnKill = MiscConfig.chatOnKill;
			if (chatOnKill)
			{
				GUILayout.Label("Kill text:", Array.Empty<GUILayoutOption>());
				MiscConfig.killText = GUILayout.TextField(MiscConfig.killText, Array.Empty<GUILayoutOption>());
			}
			MiscConfig.killSound = MenuGuiHelper.DrawCheckbox(MiscConfig.killSound, "Kill sound", Array.Empty<GUILayoutOption>());
			bool killSoundEnabled = MiscConfig.killSound;
			if (killSoundEnabled)
			{
				int killIdx = System.Array.IndexOf(GuiStyles.SoundNames, MiscConfig.killAudioName);
				if (killIdx < 0) killIdx = 0;
				if (MenuGuiHelper.Button("Kill sound: " + MiscConfig.killAudioName, -1, true, null))
				{
					killIdx = (killIdx + 1) % GuiStyles.SoundNames.Length;
					MiscConfig.killAudioName = GuiStyles.SoundNames[killIdx];
				}
			}
			MiscConfig.customBuildOffset = MenuGuiHelper.DrawCheckbox(MiscConfig.customBuildOffset, "Custom build offset", Array.Empty<GUILayoutOption>());
			bool customBuildOffset = MiscConfig.customBuildOffset;
			if (customBuildOffset)
			{
				MiscConfig.buildForwardOffset = MenuGuiHelper.LabeledFloatSlider("Forward offset: ", MiscConfig.buildForwardOffset, 0f, 8f, -1);
				MiscConfig.buildYOffset = MenuGuiHelper.LabeledFloatSlider("Vertical offset: ", MiscConfig.buildYOffset, -4f, 4f, -1);
			}
			MiscConfig.customDayTime = MenuGuiHelper.DrawCheckbox(MiscConfig.customDayTime, "Custom day time", Array.Empty<GUILayoutOption>());
			bool customDayTime = MiscConfig.customDayTime;
			if (customDayTime)
			{
				MiscConfig.customTime = MenuGuiHelper.LabeledFloatSlider("Time: ", MiscConfig.customTime, 0f, 1f, -1);
			}
			MiscConfig.randomSwapingFace = MenuGuiHelper.DrawCheckbox(MiscConfig.randomSwapingFace, "Random face swapping", Array.Empty<GUILayoutOption>());
			bool randomSwapingFace = MiscConfig.randomSwapingFace;
			if (randomSwapingFace)
			{
				MiscConfig.faceSwapDelay = MenuGuiHelper.LabeledFloatSlider("Face swap delay: ", MiscConfig.faceSwapDelay, 0.1f, 3f, -1);
			}
			GUILayout.EndScrollView();
		}
		else
		{
			base.DrawSectionHeader("Misc Options");
			this.MiscOptionsScroll = GUILayout.BeginScrollView(this.MiscOptionsScroll, Array.Empty<GUILayoutOption>());
			MiscConfig.freeCamera = MenuGuiHelper.DrawCheckbox(MiscConfig.freeCamera, "Free camera", Array.Empty<GUILayoutOption>());
			bool freeCamera = MiscConfig.freeCamera;
			if (freeCamera)
			{
				GUILayout.Label("Camera speed: " + MiscConfig.freeCameraSpeed.ToString(), Array.Empty<GUILayoutOption>());
				MiscConfig.freeCameraSpeed = (int)MenuGuiHelper.RawSlider((float)MiscConfig.freeCameraSpeed, 3f, 99f, -1);
			}
			MiscConfig.spreadMultiplier = MenuGuiHelper.LabeledFloatSlider("Gun spread: ", MiscConfig.spreadMultiplier, 0f, 1f, -1);
			MiscConfig.recoilMultiplier = MenuGuiHelper.LabeledFloatSlider("Gun recoil: ", MiscConfig.recoilMultiplier, 0f, 1f, -1);
			MiscConfig.recoilImpactMultiplier = MenuGuiHelper.LabeledFloatSlider("Gun recoil impact: ", MiscConfig.recoilImpactMultiplier, 0f, 1f, -1);
			MiscConfig.swayMultiplier = MenuGuiHelper.LabeledFloatSlider("Gun sway: ", MiscConfig.swayMultiplier, 0f, 1f, -1);
			MiscConfig.damagePunchMultiplier = MenuGuiHelper.LabeledFloatSlider("Punch damage: ", MiscConfig.damagePunchMultiplier, 0f, 1f, -1);
			MiscConfig.salvageTimeMultiplier = MenuGuiHelper.LabeledFloatSlider("Salvage time: ", MiscConfig.salvageTimeMultiplier, 0.05f, 1f, -1);
			MiscConfig.thirdCameraDistance = MenuGuiHelper.LabeledFloatSlider("Third-person camera distance: ", MiscConfig.thirdCameraDistance, 0f, 10f, -1);
			MiscConfig.firerateDecrease = MenuGuiHelper.LabeledIntSlider("Fire rate multiplier: ", MiscConfig.firerateDecrease, 0, 3, -1);
			MiscConfig.correctFirerateToWork = MenuGuiHelper.DrawCheckbox(MiscConfig.correctFirerateToWork, "Correct fire rate to work", Array.Empty<GUILayoutOption>());
			MiscConfig.useCustomFOV = MenuGuiHelper.DrawCheckbox(MiscConfig.useCustomFOV, "Change player FOV", Array.Empty<GUILayoutOption>());
			bool useCustomFOV = MiscConfig.useCustomFOV;
			if (useCustomFOV)
			{
				int num = (int)MiscConfig.customFOV;
				num = MenuGuiHelper.LabeledIntSlider("FOV: ", num, 60, 120, -1);
				MiscConfig.customFOV = (float)num;
			}
			MiscConfig.zoomExploit = MenuGuiHelper.DrawCheckbox(MiscConfig.zoomExploit, "Zoom exploit", Array.Empty<GUILayoutOption>());
			bool zoomExploit = MiscConfig.zoomExploit;
			if (zoomExploit)
			{
				bool flag3 = MenuGuiHelper.Button("Zoom key: " + (MiscConfig.waitingForZoomKey ? "Press any key..." : ((MiscConfig.zoomKeybind == KeyCode.None) ? "None" : MiscConfig.zoomKeybind.ToString())), -1, true, null);
				if (flag3)
				{
					MiscConfig.waitingForZoomKey = !MiscConfig.waitingForZoomKey;
				}
				else
				{
					bool waitingForZoomKey = MiscConfig.waitingForZoomKey;
					if (waitingForZoomKey)
					{
						bool flag4 = Event.current.isKey && Event.current.keyCode > KeyCode.None;
						if (flag4)
						{
							bool flag5 = Event.current.keyCode == KeyCode.Escape;
							if (flag5)
							{
								MiscConfig.waitingForZoomKey = false;
							}
							else
							{
								MiscConfig.zoomKeybind = Event.current.keyCode;
								MiscConfig.waitingForZoomKey = false;
							}
						}
					}
				}
				MiscConfig.zoomToggle = MenuGuiHelper.DrawCheckbox(MiscConfig.zoomToggle, "Toggle zoom key", Array.Empty<GUILayoutOption>());
				MiscConfig.zoomAmount = MenuGuiHelper.LabeledFloatSlider("Zoom: ", MiscConfig.zoomAmount, 0.1f, 5f, -1);
			}
			MiscConfig.useCustomAspectRatio = MenuGuiHelper.DrawCheckbox(MiscConfig.useCustomAspectRatio, "Use custom aspect ratio", Array.Empty<GUILayoutOption>());
			bool useCustomAspectRatio = MiscConfig.useCustomAspectRatio;
			if (useCustomAspectRatio)
			{
				MiscConfig.customAspectRatio = MenuGuiHelper.LabeledFloatSlider("Aspect ratio: ", MiscConfig.customAspectRatio, 0f, 3f, -1);
			}
			MiscConfig.replaceHitLimbToCustom = MenuGuiHelper.DrawCheckbox(MiscConfig.replaceHitLimbToCustom, "Force hitbox", Array.Empty<GUILayoutOption>());
			bool replaceHitLimbToCustom = MiscConfig.replaceHitLimbToCustom;
			if (replaceHitLimbToCustom)
			{
				MiscConfig.replacedHitLimb = MiscConfig.replacedHitLimb.EnumPopup("Current hitbox: ", MiscConfig.replacedHitLimb.ToDisplayNameLimb());
			}
			MiscConfig.modifyPlayerPerspective = MenuGuiHelper.DrawCheckbox(MiscConfig.modifyPlayerPerspective, "Modify player perspective", Array.Empty<GUILayoutOption>());
			bool flag6 = MiscConfig.modifyPlayerPerspective && MenuGuiHelper.Button("Current perspective: " + MiscConfig.playerPerspective.ToString(), -1, true, null);
			if (flag6)
			{
				MiscConfig.playerPerspective = MiscConfig.playerPerspective.Next<ECameraMode>();
				bool flag7 = MiscConfig.playerPerspective == ECameraMode.ANY;
				if (flag7)
				{
					MiscConfig.playerPerspective = ECameraMode.FIRST;
				}
			}
			MiscConfig.displayPlayerInfo = MenuGuiHelper.DrawCheckbox(MiscConfig.displayPlayerInfo, "Display target information", Array.Empty<GUILayoutOption>());
			bool displayPlayerInfo = MiscConfig.displayPlayerInfo;
			if (displayPlayerInfo)
			{
				MiscConfig.displayPlayerInfoAlways = MenuGuiHelper.DrawCheckbox(MiscConfig.displayPlayerInfoAlways, "Always display player info", Array.Empty<GUILayoutOption>());
				MiscConfig.displayPlayerGroupMembers = MenuGuiHelper.DrawCheckbox(MiscConfig.displayPlayerGroupMembers, "Display player group members", Array.Empty<GUILayoutOption>());
				MiscConfig.playerInfoWindowSize = MenuGuiHelper.LabeledIntSlider("Player info size: ", MiscConfig.playerInfoWindowSize, 10, 100, -1);
				MiscConfig.independentPlayerInfoTargeting = MenuGuiHelper.DrawCheckbox(MiscConfig.independentPlayerInfoTargeting, "Independent player info targeting", Array.Empty<GUILayoutOption>());
				bool independentPlayerInfoTargeting = MiscConfig.independentPlayerInfoTargeting;
				if (independentPlayerInfoTargeting)
				{
					MiscConfig.independetPlayerInfoTargetingDistance = MenuGuiHelper.LabeledIntSlider("Independent player info targeting distance: ", MiscConfig.independetPlayerInfoTargetingDistance, 0, 2000, -1);
				}
			}
			MiscConfig.extendBallisticRange = MenuGuiHelper.DrawCheckbox(MiscConfig.extendBallisticRange, "Extend ballistic range", Array.Empty<GUILayoutOption>());
			bool extendBallisticRange = MiscConfig.extendBallisticRange;
			if (extendBallisticRange)
			{
				MiscConfig.additionalBallisticSteps = MenuGuiHelper.LabeledIntSlider("Extended ballistic steps: ", MiscConfig.additionalBallisticSteps, 1, 4, -1);
			}
			MiscConfig.extendPlayerRegion = MenuGuiHelper.DrawCheckbox(MiscConfig.extendPlayerRegion, "Extend pickup region", Array.Empty<GUILayoutOption>());
			bool extendPlayerRegion = MiscConfig.extendPlayerRegion;
			if (extendPlayerRegion)
			{
				MiscConfig.extendRegionRange = MenuGuiHelper.LabeledIntSlider("Extended region distance: ", MiscConfig.extendRegionRange, 2, 20, -1);
				MiscConfig.extendRegionInteractThroughWalls = MenuGuiHelper.DrawCheckbox(MiscConfig.extendRegionInteractThroughWalls, "Allow extended pickup through walls", Array.Empty<GUILayoutOption>());
			}
			MiscConfig.pickupItemsThroughWalls = MenuGuiHelper.DrawCheckbox(MiscConfig.pickupItemsThroughWalls, "Pickup items through walls", Array.Empty<GUILayoutOption>());
			bool pickupItemsThroughWalls = MiscConfig.pickupItemsThroughWalls;
			if (pickupItemsThroughWalls)
			{
				MiscConfig.pickupItemsThroughWallsDistance = MenuGuiHelper.LabeledIntSlider("Pickup items through walls distance: ", MiscConfig.pickupItemsThroughWallsDistance, 3, 20, -1);
			}
			MiscConfig.autoItemPickup = MenuGuiHelper.DrawCheckbox(MiscConfig.autoItemPickup, "Auto item pickup", Array.Empty<GUILayoutOption>());
			bool autoItemPickup = MiscConfig.autoItemPickup;
			if (autoItemPickup)
			{
				MiscConfig.autoItemPickupDistance = MenuGuiHelper.LabeledIntSlider("Auto item pickup distance: ", MiscConfig.autoItemPickupDistance, 2, 20, -1);
				MiscConfig.autoItemPickupDelay = MenuGuiHelper.LabeledFloatSlider("Pickup delay: ", MiscConfig.autoItemPickupDelay, 0f, 2f, -1);
				this.autoPickupScroll = GUILayout.BeginScrollView(this.autoPickupScroll, new GUILayoutOption[] { GUILayout.Height(150f) });
				foreach (ItemInfo ddb8pIlWKKbHkw2jCuyAPcvL in MiscConfig.AutoPickupWhitelist.Values)
				{
					bool flag8 = MenuGuiHelper.Button(ddb8pIlWKKbHkw2jCuyAPcvL.name, -1, true, null);
					if (flag8)
					{
						MiscConfig.AutoPickupWhitelist.Remove(ddb8pIlWKKbHkw2jCuyAPcvL.id);
					}
				}
				GUILayout.EndScrollView();
				bool flag9 = MenuGuiHelper.Button("Configure auto pickup items", -1, true, null);
				if (flag9)
				{
					AddAutoPickupItemWindow.isActive = !AddAutoPickupItemWindow.isActive;
				}
			}
			MiscConfig.autoFishing = MenuGuiHelper.DrawCheckbox(MiscConfig.autoFishing, "Auto fishing", Array.Empty<GUILayoutOption>());
			bool autoFishing = MiscConfig.autoFishing;
			if (autoFishing)
			{
				MiscConfig.autoFishingMaxWait = MenuGuiHelper.LabeledIntSlider("Fishing max wait: ", MiscConfig.autoFishingMaxWait, 5, 60, -1);
				MiscConfig.autoFishingCatchDelay = MenuGuiHelper.LabeledFloatSlider("Fishing catch delay: ", MiscConfig.autoFishingCatchDelay, 0.1f, 1.4f, -1);
				MiscConfig.autoFishingShowStats = MenuGuiHelper.DrawCheckbox(MiscConfig.autoFishingShowStats, "Show fishing stats", Array.Empty<GUILayoutOption>());
				MiscConfig.autoFishingAutoEquip = MenuGuiHelper.DrawCheckbox(MiscConfig.autoFishingAutoEquip, "Auto equip fishing rod", Array.Empty<GUILayoutOption>());
			}
			MiscConfig.autoFarmHarvest = MenuGuiHelper.DrawCheckbox(MiscConfig.autoFarmHarvest, "Auto farm harvest", Array.Empty<GUILayoutOption>());
			MiscConfig.autoFarmFertilize = MenuGuiHelper.DrawCheckbox(MiscConfig.autoFarmFertilize, "Auto farm fertilize", Array.Empty<GUILayoutOption>());
			MiscConfig.autoFarmPlant = MenuGuiHelper.DrawCheckbox(MiscConfig.autoFarmPlant, "Auto farm plant", Array.Empty<GUILayoutOption>());
			MiscConfig.autoPlacePlant = MenuGuiHelper.DrawCheckbox(MiscConfig.autoPlacePlant, "Auto place plant", Array.Empty<GUILayoutOption>());
			MiscConfig.autoFarmCraft = MenuGuiHelper.DrawCheckbox(MiscConfig.autoFarmCraft, "Auto farm craft seeds", Array.Empty<GUILayoutOption>());
			MiscConfig.autoFarmStore = MenuGuiHelper.DrawCheckbox(MiscConfig.autoFarmStore, "Auto farm store harvest", Array.Empty<GUILayoutOption>());
			bool autoFarmStore = MiscConfig.autoFarmStore;
			if (autoFarmStore)
			{
				MiscConfig.autoFarmStoreRadius = MenuGuiHelper.LabeledIntSlider("Store radius: ", MiscConfig.autoFarmStoreRadius, 3, 50, -1);
			}
			bool flag10 = MiscConfig.autoFarmPlant || MiscConfig.autoPlacePlant;
			if (flag10)
			{
				bool flag11 = AutomationBot.inventorySeedIds.Count == 0;
				if (flag11)
				{
					GUILayout.Label("No seeds in inventory", Array.Empty<GUILayoutOption>());
				}
				else
				{
					bool autoFarmPlant = MiscConfig.autoFarmPlant;
					if (autoFarmPlant)
					{
						string text = "First seed";
						bool flag12 = MiscConfig.autoFarmPlantSeedIndex >= 0 && MiscConfig.autoFarmPlantSeedIndex < AutomationBot.inventoryPlantNames.Count;
						if (flag12)
						{
							text = AutomationBot.inventoryPlantNames[MiscConfig.autoFarmPlantSeedIndex];
						}
						bool flag13 = MenuGuiHelper.Button("Plant: " + text, -1, true, null);
						if (flag13)
						{
							MiscConfig.autoFarmPlantSeedIndex++;
							bool flag14 = MiscConfig.autoFarmPlantSeedIndex >= AutomationBot.inventoryPlantNames.Count;
							if (flag14)
							{
								MiscConfig.autoFarmPlantSeedIndex = -1;
							}
						}
					}
					bool autoPlacePlant = MiscConfig.autoPlacePlant;
					if (autoPlacePlant)
					{
						string text2 = "First seed";
						bool flag15 = MiscConfig.autoPlacePlantSeedIndex >= 0 && MiscConfig.autoPlacePlantSeedIndex < AutomationBot.inventoryPlantNames.Count;
						if (flag15)
						{
							text2 = AutomationBot.inventoryPlantNames[MiscConfig.autoPlacePlantSeedIndex];
						}
						bool flag16 = MenuGuiHelper.Button("Place: " + text2, -1, true, null);
						if (flag16)
						{
							MiscConfig.autoPlacePlantSeedIndex++;
							bool flag17 = MiscConfig.autoPlacePlantSeedIndex >= AutomationBot.inventoryPlantNames.Count;
							if (flag17)
							{
								MiscConfig.autoPlacePlantSeedIndex = -1;
							}
						}
					}
				}
			}
			MiscConfig.showFarmGrid = MenuGuiHelper.DrawCheckbox(MiscConfig.showFarmGrid, "Show farm grid", Array.Empty<GUILayoutOption>());
			bool showFarmGrid = MiscConfig.showFarmGrid;
			if (showFarmGrid)
			{
				MiscConfig.showFarmGridPlacement = MenuGuiHelper.DrawCheckbox(MiscConfig.showFarmGridPlacement, "Show placement spots", Array.Empty<GUILayoutOption>());
			}
			bool flag18 = MenuGuiHelper.Button("Freeze grid key: " + (MiscConfig.waitingForFarmGridFreezeKey ? "Press any key..." : ((MiscConfig.farmGridFreezeKey == KeyCode.None) ? "None" : MiscConfig.farmGridFreezeKey.ToString())), -1, true, null);
			if (flag18)
			{
				MiscConfig.waitingForFarmGridFreezeKey = !MiscConfig.waitingForFarmGridFreezeKey;
			}
			else
			{
				bool waitingForFarmGridFreezeKey = MiscConfig.waitingForFarmGridFreezeKey;
				if (waitingForFarmGridFreezeKey)
				{
					bool flag19 = Event.current.isKey && Event.current.keyCode > KeyCode.None;
					if (flag19)
					{
						bool flag20 = Event.current.keyCode == KeyCode.Escape;
						if (flag20)
						{
							MiscConfig.waitingForFarmGridFreezeKey = false;
						}
						else
						{
							MiscConfig.farmGridFreezeKey = Event.current.keyCode;
							MiscConfig.waitingForFarmGridFreezeKey = false;
						}
					}
				}
			}
			bool gridFrozen = AutomationBot.gridFrozen;
			if (gridFrozen)
			{
				GUILayout.Label("Grid FROZEN (" + AutomationBot.frozenGridPoints.Count.ToString() + " spots)", Array.Empty<GUILayoutOption>());
			}
			bool flag21 = MiscConfig.autoFarmHarvest || MiscConfig.autoFarmFertilize || MiscConfig.autoFarmPlant || MiscConfig.autoPlacePlant || MiscConfig.autoFarmCraft || MiscConfig.autoFarmStore;
			if (flag21)
			{
				MiscConfig.autoFarmDistance = MenuGuiHelper.LabeledIntSlider("Auto farm distance: ", MiscConfig.autoFarmDistance, 3, 50, -1);
				MiscConfig.autoFarmShowStats = MenuGuiHelper.DrawCheckbox(MiscConfig.autoFarmShowStats, "Show farm stats", Array.Empty<GUILayoutOption>());
				bool autoFarmShowStats = MiscConfig.autoFarmShowStats;
				if (autoFarmShowStats)
				{
					MiscConfig.showFarmStatPlanted = MenuGuiHelper.DrawCheckbox(MiscConfig.showFarmStatPlanted, "  Planted", Array.Empty<GUILayoutOption>());
					MiscConfig.showFarmStatHarvested = MenuGuiHelper.DrawCheckbox(MiscConfig.showFarmStatHarvested, "  Harvested", Array.Empty<GUILayoutOption>());
					MiscConfig.showFarmStatFertilized = MenuGuiHelper.DrawCheckbox(MiscConfig.showFarmStatFertilized, "  Fertilized", Array.Empty<GUILayoutOption>());
					MiscConfig.showFarmStatGrowing = MenuGuiHelper.DrawCheckbox(MiscConfig.showFarmStatGrowing, "  Growing", Array.Empty<GUILayoutOption>());
					MiscConfig.showFarmStatGrown = MenuGuiHelper.DrawCheckbox(MiscConfig.showFarmStatGrown, "  Grown", Array.Empty<GUILayoutOption>());
					MiscConfig.showFarmStatEmpty = MenuGuiHelper.DrawCheckbox(MiscConfig.showFarmStatEmpty, "  Empty", Array.Empty<GUILayoutOption>());
					MiscConfig.showFarmStatCrafted = MenuGuiHelper.DrawCheckbox(MiscConfig.showFarmStatCrafted, "  Crafted", Array.Empty<GUILayoutOption>());
					MiscConfig.showFarmStatStored = MenuGuiHelper.DrawCheckbox(MiscConfig.showFarmStatStored, "  Stored", Array.Empty<GUILayoutOption>());
				}
			}
			MiscConfig.customVehicleBehaviour = MenuGuiHelper.DrawCheckbox(MiscConfig.customVehicleBehaviour, "Custom vehicle behaviour", Array.Empty<GUILayoutOption>());
			bool customVehicleBehaviour = MiscConfig.customVehicleBehaviour;
			if (customVehicleBehaviour)
			{
				MiscConfig.vehicleMouseMove = MenuGuiHelper.DrawCheckbox(MiscConfig.vehicleMouseMove, "Steer vehicle with mouse", Array.Empty<GUILayoutOption>());
				MiscConfig.vehicleLockRotation = MenuGuiHelper.DrawCheckbox(MiscConfig.vehicleLockRotation, "Lock vehicle rotation (move with mouse, rotate with arrows)", Array.Empty<GUILayoutOption>());
				bool vehicleLockRotation = MiscConfig.vehicleLockRotation;
				if (vehicleLockRotation)
				{
					MiscConfig.vehicleLockRotationTurnSpeed = MenuGuiHelper.LabeledFloatSlider("Arrow turn speed: ", MiscConfig.vehicleLockRotationTurnSpeed, 10f, 360f, -1);
				}
				MiscConfig.vehicleCameraDistance = MenuGuiHelper.LabeledFloatSlider("Camera distance: ", MiscConfig.vehicleCameraDistance, 3f, 30f, -1);
				MiscConfig.vehicleCameraHeight = MenuGuiHelper.LabeledFloatSlider("Camera height: ", MiscConfig.vehicleCameraHeight, 1f, 20f, -1);
				MiscConfig.vehicleCameraSmooth = MenuGuiHelper.LabeledFloatSlider("Camera smoothing: ", MiscConfig.vehicleCameraSmooth, 1f, 30f, -1);
				MiscConfig.vehicleNoclip = MenuGuiHelper.DrawCheckbox(MiscConfig.vehicleNoclip, "Vehicle flight", Array.Empty<GUILayoutOption>());
				bool flag22 = !MiscConfig.vehicleNoclip;
				if (flag22)
				{
					try
					{
						bool flag23 = MenuGuiHelper.Button("Reset values to default", -1, true, null);
						if (flag23)
						{
							VehicleBehaviour.capturedPhysicsProfile = VehicleBehaviour.ClonePhysicsProfile(VehicleBehaviour.originalPhysicsProfile);
							VehicleBehaviour.steerMaxOverride = VehicleBehaviour.savedSteerMax;
							VehicleBehaviour.steerMinOverride = VehicleBehaviour.savedSteerMin;
							ReflectionUtil.SetFieldValue(typeof(VehicleAsset), "_steerMax", VehicleBehaviour.instance.currentVehicle.asset, VehicleBehaviour.steerMaxOverride);
							ReflectionUtil.SetFieldValue(typeof(VehicleAsset), "_steerMin", VehicleBehaviour.instance.currentVehicle.asset, VehicleBehaviour.steerMinOverride);
						}
						ValueTuple<string, float>[] array = new ValueTuple<string, float>[]
						{
							new ValueTuple<string, float>("rootMassOverride", 10f),
							new ValueTuple<string, float>("rootMassMultiplier", 5f),
							new ValueTuple<string, float>("rootDragMultiplier", 10f),
							new ValueTuple<string, float>("rootAngularDragMultiplier", 6f),
							new ValueTuple<string, float>("wheelStiffnessTractionMultiplier", 4f),
							new ValueTuple<string, float>("wheelDampingRate", 4f),
							new ValueTuple<string, float>("wheelSuspensionForce", 100f),
							new ValueTuple<string, float>("wheelSuspensionDamper", 10f),
							new ValueTuple<string, float>("wheelMassOverride", 15f),
							new ValueTuple<string, float>("wheelMassMultiplier", 5f),
							new ValueTuple<string, float>("motorTorqueMultiplier", 10f),
							new ValueTuple<string, float>("motorTorqueClampMultiplier", 8f),
							new ValueTuple<string, float>("brakeTorqueMultiplier", 10f),
							new ValueTuple<string, float>("brakeTorqueTractionMultiplier", 8f),
							new ValueTuple<string, float>("carjackForceMultiplier", 10f)
						};
						string[] array2 = new string[] { "forwardFriction", "sidewaysFriction" };
						Type typeFromHandle = typeof(VehiclePhysicsProfileAsset);
						VehicleBehaviour.steerMinOverride = MenuGuiHelper.LabeledFloatSlider("Steer min", VehicleBehaviour.steerMinOverride, 5f, 90f, -1);
						VehicleBehaviour.steerMaxOverride = MenuGuiHelper.LabeledFloatSlider("Steer max", VehicleBehaviour.steerMaxOverride, 5f, 90f, -1);
						foreach (ValueTuple<string, float> valueTuple in array)
						{
							string item = valueTuple.Item1;
							float item2 = valueTuple.Item2;
							bool flag24 = typeFromHandle.GetProperty(item).GetValue(VehicleBehaviour.capturedPhysicsProfile) != null;
							if (flag24)
							{
								typeFromHandle.GetProperty(item).GetValue(VehicleBehaviour.capturedPhysicsProfile).ToString();
							}
							bool flag25 = typeFromHandle.GetProperty(item).GetValue(VehicleBehaviour.capturedPhysicsProfile) == null;
							if (flag25)
							{
								bool flag26 = MenuGuiHelper.Button("Create value", -1, true, null);
								if (flag26)
								{
									typeFromHandle.GetProperty(item).SetValue(VehicleBehaviour.capturedPhysicsProfile, 0f);
								}
							}
							else
							{
								typeFromHandle.GetProperty(item).SetValue(VehicleBehaviour.capturedPhysicsProfile, MenuGuiHelper.LabeledFloatSlider(item + ": ", (float)typeFromHandle.GetProperty(item).GetValue(VehicleBehaviour.capturedPhysicsProfile), 0f, item2, -1));
							}
						}
						foreach (string text3 in array2)
						{
							GUILayout.Label(text3, Array.Empty<GUILayoutOption>());
							bool flag27 = typeFromHandle.GetProperty(text3).GetValue(VehicleBehaviour.capturedPhysicsProfile) == null;
							if (flag27)
							{
								bool flag28 = MenuGuiHelper.Button("Create value", -1, true, null);
								if (flag28)
								{
									typeFromHandle.GetProperty(text3).SetValue(VehicleBehaviour.capturedPhysicsProfile, default(VehiclePhysicsProfileAsset.Friction));
								}
							}
							else
							{
								VehiclePhysicsProfileAsset.Friction friction = (VehiclePhysicsProfileAsset.Friction)typeFromHandle.GetProperty(text3).GetValue(VehicleBehaviour.capturedPhysicsProfile);
								friction.stiffness = MenuGuiHelper.LabeledFloatSlider("Stiffness: ", friction.stiffness, 0f, 20f, -1);
								friction.extremumValue = MenuGuiHelper.LabeledFloatSlider("Extremum value: ", friction.extremumValue, 0f, 10f, -1);
								friction.extremumSlip = MenuGuiHelper.LabeledFloatSlider("Extremum slip: ", friction.extremumSlip, 0f, 10f, -1);
								friction.asymptoteValue = MenuGuiHelper.LabeledFloatSlider("Asymptote value: ", friction.asymptoteValue, 0f, 10f, -1);
								friction.asymptoteSlip = MenuGuiHelper.LabeledFloatSlider("Asymptote slip: ", friction.asymptoteSlip, 0f, 10f, -1);
								typeFromHandle.GetProperty(text3).SetValue(VehicleBehaviour.capturedPhysicsProfile, friction);
							}
						}
						bool flag29 = VehicleBehaviour.instance != null;
						if (flag29)
						{
							VehicleBehaviour.capturedPhysicsProfile.applyTo(VehicleBehaviour.instance.currentVehicle);
							ReflectionUtil.SetFieldValue(typeof(VehicleAsset), "_steerMax", VehicleBehaviour.instance.currentVehicle.asset, VehicleBehaviour.steerMaxOverride);
							ReflectionUtil.SetFieldValue(typeof(VehicleAsset), "_steerMin", VehicleBehaviour.instance.currentVehicle.asset, VehicleBehaviour.steerMinOverride);
						}
						goto IL_18DA;
					}
					catch (Exception ex)
					{
						GUILayout.Label(ex.Message, Array.Empty<GUILayoutOption>());
						goto IL_18DA;
					}
				}
				bool vehicleNoclip = MiscConfig.vehicleNoclip;
				if (vehicleNoclip)
				{
					MiscConfig.useVehiclePhysics = MenuGuiHelper.DrawCheckbox(MiscConfig.useVehiclePhysics, "Enable vehicle player collision", Array.Empty<GUILayoutOption>());
				}
				MiscConfig.vehicleSpeed = MenuGuiHelper.LabeledFloatSlider("Vehicle speed: ", MiscConfig.vehicleSpeed, 0f, 100f, -1);
				MiscConfig.vehicleSpinbot = MenuGuiHelper.DrawCheckbox(MiscConfig.vehicleSpinbot, "Vehicle spinbot", Array.Empty<GUILayoutOption>());
				bool vehicleSpinbot = MiscConfig.vehicleSpinbot;
				if (vehicleSpinbot)
				{
					bool flag30 = MenuGuiHelper.Button("Spinbot type: " + MiscConfig.vehicleSpinbotType.DvehSpinTypeName(), -1, true, null);
					if (flag30)
					{
						MiscConfig.vehicleSpinbotType = MiscConfig.vehicleSpinbotType.Next<DxVehSpinType>();
					}
					bool flag31 = MiscConfig.vehicleSpinbotType == DxVehSpinType.Continuous;
					if (flag31)
					{
						MiscConfig.vehicleSpinbotSpeed = MenuGuiHelper.LabeledFloatSlider("Spinbot speed: ", MiscConfig.vehicleSpinbotSpeed, 10f, 3600f, -1);
					}
					MiscConfig.vehicleSpinbotDrawDirection = MenuGuiHelper.DrawCheckbox(MiscConfig.vehicleSpinbotDrawDirection, "Draw server direction line", Array.Empty<GUILayoutOption>());
				}
			}
			MiscConfig.vehicleGroundOnSpy = MenuGuiHelper.DrawCheckbox(MiscConfig.vehicleGroundOnSpy, "Set vehicle position to ground on spy", Array.Empty<GUILayoutOption>());
			IL_18DA:
			MiscConfig.changeVehicleLeaveVelocity = MenuGuiHelper.DrawCheckbox(MiscConfig.changeVehicleLeaveVelocity, "Change vehicle leave velocity", Array.Empty<GUILayoutOption>());
			bool changeVehicleLeaveVelocity = MiscConfig.changeVehicleLeaveVelocity;
			if (changeVehicleLeaveVelocity)
			{
				MiscConfig.useForwardVelocity = MenuGuiHelper.DrawCheckbox(MiscConfig.useForwardVelocity, "Use forward velocity", Array.Empty<GUILayoutOption>());
				bool useForwardVelocity = MiscConfig.useForwardVelocity;
				if (useForwardVelocity)
				{
					GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
					GUILayout.Label("Forward multiplier:", Array.Empty<GUILayoutOption>());
					try
					{
						MiscConfig.vehilceVelocityForward = MenuGuiHelper.IntTextField(MiscConfig.vehilceVelocityForward);
					}
					catch
					{
					}
					GUILayout.EndHorizontal();
				}
				else
				{
					try
					{
						GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
						GUILayout.Label("X:", Array.Empty<GUILayoutOption>());
						MiscConfig.vehilceVelocityX = MenuGuiHelper.IntTextField(MiscConfig.vehilceVelocityX);
						GUILayout.Label("Y:", Array.Empty<GUILayoutOption>());
						MiscConfig.vehilceVelocityY = MenuGuiHelper.IntTextField(MiscConfig.vehilceVelocityY);
						GUILayout.Label("Z:", Array.Empty<GUILayoutOption>());
						MiscConfig.vehilceVelocityZ = MenuGuiHelper.IntTextField(MiscConfig.vehilceVelocityZ);
						GUILayout.EndHorizontal();
					}
					catch
					{
					}
				}
			}
			MiscConfig.modifyMoveBehaviour = MenuGuiHelper.DrawCheckbox(MiscConfig.modifyMoveBehaviour, "Enable player spinbot", Array.Empty<GUILayoutOption>());
			bool modifyMoveBehaviour = MiscConfig.modifyMoveBehaviour;
			if (modifyMoveBehaviour)
			{
				MiscConfig.moveType = MiscConfig.moveType.EnumPopup("Rotation type: ", MiscConfig.moveType.ToDisplayNameMoveType());
				MiscConfig.showMoveModifying = MenuGuiHelper.DrawCheckbox(MiscConfig.showMoveModifying, "Show spinbot rotation", Array.Empty<GUILayoutOption>());
				MiscConfig.playerSpinbotDrawDirection = MenuGuiHelper.DrawCheckbox(MiscConfig.playerSpinbotDrawDirection, "Draw server direction line", Array.Empty<GUILayoutOption>());
				bool flag32 = MiscConfig.moveType == MoveType.Continuous;
				if (flag32)
				{
					MiscConfig.playerSpinbotSpeed = MenuGuiHelper.LabeledFloatSlider("Spinbot speed: ", MiscConfig.playerSpinbotSpeed, 10f, 3600f, -1);
				}
				bool flag33 = MiscConfig.moveType != MoveType.Jitter && MiscConfig.moveType != MoveType.RandomPitch && MiscConfig.moveType != MoveType.TwoTactSpin && MiscConfig.moveType != MoveType.FourTactSpin;
				if (flag33)
				{
					MiscConfig.playerSpinbotRandomPitch = MenuGuiHelper.DrawCheckbox(MiscConfig.playerSpinbotRandomPitch, "Random pitch override", Array.Empty<GUILayoutOption>());
				}
				MiscConfig.playerSpinbotDesync = MenuGuiHelper.DrawCheckbox(MiscConfig.playerSpinbotDesync, "Body desync", Array.Empty<GUILayoutOption>());
				bool playerSpinbotDesync = MiscConfig.playerSpinbotDesync;
				if (playerSpinbotDesync)
				{
					MiscConfig.playerSpinbotDesyncAmount = MenuGuiHelper.LabeledFloatSlider("Desync amount: ", MiscConfig.playerSpinbotDesyncAmount, -180f, 180f, -1);
				}
				bool flag34 = MiscConfig.moveType == MoveType.AntiAim;
				if (flag34)
				{
					MiscConfig.playerSpinbotAntiAimDistance = MenuGuiHelper.LabeledFloatSlider("Anti-aim max distance: ", MiscConfig.playerSpinbotAntiAimDistance, 10f, 1000f, -1);
				}
			}
			bool flag35 = MenuGuiHelper.Button("Open nearby items window", -1, true, null);
			if (flag35)
			{
				NearbyItemsWindow.opened = true;
			}
			MiscConfig.spyType = MiscConfig.spyType.EnumPopup("Anti-spy type: ", MiscConfig.spyType.ToDisplayNameSpyType());
			MiscConfig.notifyAboutSpy = MenuGuiHelper.DrawCheckbox(MiscConfig.notifyAboutSpy, "Notify about spy", Array.Empty<GUILayoutOption>());
			bool flag36 = MiscConfig.spyType == AvatarSpyMode.SpyWithDelay;
			if (flag36)
			{
				MiscConfig.spyDelayTimer = MenuGuiHelper.LabeledFloatSlider("Spy delay: ", MiscConfig.spyDelayTimer, 0.15f, 2f, -1);
			}
			else
			{
				bool flag37 = MiscConfig.spyType == AvatarSpyMode.SendCustomImage;
				if (flag37)
				{
					GUILayout.Label("Custom image is sent from " + Application.dataPath + "/spyimage.png. If the image doesn't exist, the spy request is declined.", Array.Empty<GUILayoutOption>());
				}
			}
			bool notifyAboutSpy = MiscConfig.notifyAboutSpy;
			if (notifyAboutSpy)
			{
				MiscConfig.spyWindowSize = MenuGuiHelper.LabeledIntSlider("Window size: ", MiscConfig.spyWindowSize, 25, 200, -1);
			}
			MiscConfig.hwidType = MiscConfig.hwidType.EnumPopup("HWID type: ", MiscConfig.hwidType.ToDisplayNameHwidType());
			bool flag38 = MiscConfig.hwidType == HwidMode.SendRealHWID;
			if (flag38)
			{
				bool flag39 = MenuGuiHelper.Button("Change real HWID", -1, true, null);
				if (flag39)
				{
					HwidSpoofer.ChangeRealHwid();
				}
			}
			else
			{
				bool flag40 = MiscConfig.hwidType == HwidMode.UsePseudoHWID || MiscConfig.hwidType == HwidMode.SendLinuxPseudoHWID;
				if (flag40)
				{
					ushort num2 = 0;
					ushort num3 = 0;
					ushort num4 = 0;
					foreach (byte b in MiscConfig.SpoofedHwid1)
					{
						num2 += (ushort)b;
					}
					foreach (byte b2 in MiscConfig.SpoofedHwid2)
					{
						num3 += (ushort)b2;
					}
					foreach (byte b3 in MiscConfig.SpoofedHwid3)
					{
						num4 += (ushort)b3;
					}
					GUILayout.Label("Pseudo player prefs HWID hash: " + num2.ToString(), Array.Empty<GUILayoutOption>());
					GUILayout.Label("Pseudo convenient savedata HWID hash: " + num3.ToString(), Array.Empty<GUILayoutOption>());
					bool flag41 = MiscConfig.hwidType == HwidMode.UsePseudoHWID;
					if (flag41)
					{
						GUILayout.Label("Pseudo Windows HWID hash: " + num4.ToString(), Array.Empty<GUILayoutOption>());
					}
					bool flag42 = MenuGuiHelper.Button("Change pseudo HWID", -1, true, null);
					if (flag42)
					{
						HwidSpoofer.ChangePseudoHwid();
					}
					GUILayout.Label("Information about HWIDs: The game uses 3 types of hashes obtained from different sources. You can change and save all 3 HWIDs.", Array.Empty<GUILayoutOption>());
				}
			}
			bool flag43 = MenuGuiHelper.Button("Unlock all achievements", -1, true, null);
			if (flag43)
			{
				AchievementsUnlocker.UnlockAllAchievements();
			}
			GUILayout.EndScrollView();
		}
	}
	public Vector2 PlayerEffectsScroll = Vector2.zero;
	public Vector2 MiscOptionsScroll = Vector2.zero;
	public Vector2 autoPickupScroll = Vector2.zero;
}
