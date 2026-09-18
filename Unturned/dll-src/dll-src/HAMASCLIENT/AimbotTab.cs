using System;
using UnityEngine;
public class AimbotTab : FeatureTabBase
{
	public override int SortId()
	{
		return 1;
	}
	public override string GetName()
	{
		return "Aimbot";
	}
	public override TabCount GetTabCounts()
	{
		return TabCount.Three;
	}
	public override void DoTab(TabCount tc)
	{
		bool flag = tc == TabCount.One;
		if (flag)
		{
			base.DrawSectionHeader("Silent Aimbot");
			this.SilentAimScroll = GUILayout.BeginScrollView(this.SilentAimScroll, Array.Empty<GUILayoutOption>());
			AimbotConfig.enableSilentAim = MenuGuiHelper.DrawCheckbox(AimbotConfig.enableSilentAim, "Enable Silent Aimbot", Array.Empty<GUILayoutOption>());
			bool enableSilentAim = AimbotConfig.enableSilentAim;
			if (enableSilentAim)
			{
				AimbotConfig.straightRaycasting = MenuGuiHelper.DrawCheckbox(AimbotConfig.straightRaycasting, "Straight Raycasting", Array.Empty<GUILayoutOption>());
				AimbotConfig.enableAutoShoot = MenuGuiHelper.DrawCheckbox(AimbotConfig.enableAutoShoot, "Auto Shoot", Array.Empty<GUILayoutOption>());
			}
			AimbotConfig.enableMeleeSilentAim = MenuGuiHelper.DrawCheckbox(AimbotConfig.enableMeleeSilentAim, "Enable Melee Silent Aimbot", Array.Empty<GUILayoutOption>());
			bool flag2 = AimbotConfig.enableSilentAim || AimbotConfig.enableMeleeSilentAim;
			if (flag2)
			{
				AimbotConfig.hitPointToTransform = MenuGuiHelper.DrawCheckbox(AimbotConfig.hitPointToTransform, "Target Root Bone", Array.Empty<GUILayoutOption>());
				AimbotConfig.enableVehicleHitboxExploit = MenuGuiHelper.DrawCheckbox(AimbotConfig.enableVehicleHitboxExploit, "Vehicle Hitbox Exploit", Array.Empty<GUILayoutOption>());
				bool enableVehicleHitboxExploit = AimbotConfig.enableVehicleHitboxExploit;
				if (enableVehicleHitboxExploit)
				{
					AimbotConfig.vehicleHitboxExploitDistance = MenuGuiHelper.LabeledFloatSlider("Hitbox Expander Distance: ", AimbotConfig.vehicleHitboxExploitDistance, 5f, 100f, -1);
				}
				bool flag3 = !AimbotConfig.hitPointToTransform;
				if (flag3)
				{
					AimbotConfig.bestSilentAimPartPreselective = MenuGuiHelper.DrawCheckbox(AimbotConfig.bestSilentAimPartPreselective, "Target Best Hitbox", Array.Empty<GUILayoutOption>());
				}
				bool flag4 = !AimbotConfig.hitPointToTransform && !AimbotConfig.bestSilentAimPartPreselective && MenuGuiHelper.Button("Silent Aim Hitpoint: " + AimbotConfig.silentAimHitPointLimb.ToDisplayNameLimb(), -1, true, null);
				if (flag4)
				{
					AimbotConfig.silentAimHitPointLimb = AimbotConfig.silentAimHitPointLimb.Next<Limb>();
				}
				AimbotConfig.silentAimLimb = AimbotConfig.silentAimLimb.EnumPopup("Silent Aim Hitbox: ", AimbotConfig.silentAimLimb.ToDisplayNameLimb());
				bool flag5 = AimbotConfig.silentAimLimb == Limb.Random;
				if (flag5)
				{
					AimbotConfig.randomLimbHeadHitChance = MenuGuiHelper.LabeledIntSlider("Random Head Hit Chance: ", AimbotConfig.randomLimbHeadHitChance, 0, 100, -1);
				}
				AimbotConfig.silentAimType = AimbotConfig.silentAimType.EnumPopup("Silent Aim Method: ", AimbotConfig.silentAimType.ToDisplayNameSilentAimType());
				AimbotConfig.aimingChance = MenuGuiHelper.LabeledIntSlider("Silent Aim Hit Chance: ", AimbotConfig.aimingChance, 0, 100, -1);
				SilentAimType silentAimType = AimbotConfig.silentAimType;
				bool flag6 = silentAimType != SilentAimType.Distance;
				if (flag6)
				{
					bool flag7 = silentAimType == SilentAimType.Sphere;
					if (flag7)
					{
						AimbotConfig.hookSpherePointToBullet = MenuGuiHelper.DrawCheckbox(AimbotConfig.hookSpherePointToBullet, "Hook Sphere Point to Bullet", Array.Empty<GUILayoutOption>());
						AimbotConfig.autoBestSphereSize = MenuGuiHelper.DrawCheckbox(AimbotConfig.autoBestSphereSize, "Auto Best Sphere Size", Array.Empty<GUILayoutOption>());
						GUILayout.Label("Spheres: ", Array.Empty<GUILayoutOption>());
						GUILayout.Space(4f);
						for (int i = 0; i < AimbotConfig.SphereSizes.Count; i++)
						{
							AimbotConfig.SphereSizes[i].sphereSize = MenuGuiHelper.LabeledFloatSlider("Sphere Size: ", AimbotConfig.SphereSizes[i].sphereSize, 1f, 2f, -1);
							AimbotConfig.SphereSizes[i].sphereSegments = MenuGuiHelper.LabeledIntSlider("Sphere Segments: ", AimbotConfig.SphereSizes[i].sphereSegments, 2, 32, -1);
							bool flag8 = MenuGuiHelper.Button("Remove Sphere", -1, true, null);
							if (flag8)
							{
								AimbotConfig.SphereSizes.RemoveAt(i);
								SpherePointGenerator.BuildSpherePoints();
							}
						}
						bool flag9 = MenuGuiHelper.Button("Sort & Optimize", -1, true, null);
						if (flag9)
						{
							SpherePointGenerator.RebuildSpherePoints();
						}
						bool flag10 = MenuGuiHelper.Button("Add Sphere", -1, true, null);
						if (flag10)
						{
							AimbotConfig.SphereSizes.Add(new AimSphereOptions(1f, 8));
							AimbotConfig.MaxSphereSize = Mathf.Max(AimbotConfig.MaxSphereSize, 1f);
						}
						AimbotConfig.debugSpherePoints = MenuGuiHelper.DrawCheckbox(AimbotConfig.debugSpherePoints, "Debug Sphere Points", Array.Empty<GUILayoutOption>());
						AimbotConfig.setHitPointToCameraIfAviable = MenuGuiHelper.DrawCheckbox(AimbotConfig.setHitPointToCameraIfAviable, "Prefer Camera Hit Point", Array.Empty<GUILayoutOption>());
						AimbotConfig.additiveRangeBySphereSize = MenuGuiHelper.DrawCheckbox(AimbotConfig.additiveRangeBySphereSize, "Extend Range by Sphere Size", Array.Empty<GUILayoutOption>());
						AimbotConfig.previewHitPoint = MenuGuiHelper.DrawCheckbox(AimbotConfig.previewHitPoint, "Preview Sphere Point", Array.Empty<GUILayoutOption>());
						bool previewHitPoint = AimbotConfig.previewHitPoint;
						if (previewHitPoint)
						{
							AimbotConfig.drawLineFromHitPoint = MenuGuiHelper.DrawCheckbox(AimbotConfig.drawLineFromHitPoint, "Draw Line from Sphere Point", Array.Empty<GUILayoutOption>());
							AimbotConfig.drawLineFromPlayerHead = MenuGuiHelper.DrawCheckbox(AimbotConfig.drawLineFromPlayerHead, "Draw Line from Player Head", Array.Empty<GUILayoutOption>());
						}
						AimbotConfig.verifySphereToPlayerPointByLinecast = MenuGuiHelper.DrawCheckbox(AimbotConfig.verifySphereToPlayerPointByLinecast, "Verify Sphere Point by Linecast", Array.Empty<GUILayoutOption>());
					}
				}
				else
				{
					AimbotConfig.distanceToHit = MenuGuiHelper.LabeledIntSlider("Hit Distance: ", AimbotConfig.distanceToHit, 1, 15, -1);
					AimbotConfig.verifyForwardHitPointAviablity = MenuGuiHelper.DrawCheckbox(AimbotConfig.verifyForwardHitPointAviablity, "Verify Forward Hit Point", Array.Empty<GUILayoutOption>());
					AimbotConfig.traceFromOrigin = MenuGuiHelper.DrawCheckbox(AimbotConfig.traceFromOrigin, "Trace from Origin", Array.Empty<GUILayoutOption>());
					bool traceFromOrigin = AimbotConfig.traceFromOrigin;
					if (traceFromOrigin)
					{
						AimbotConfig.verifyTraceByLinecast = MenuGuiHelper.DrawCheckbox(AimbotConfig.verifyTraceByLinecast, "Verify Trace by Linecast", Array.Empty<GUILayoutOption>());
					}
				}
				GUILayout.Space(10f);
				string text = "";
				foreach (TargetType dr5qliNNQh3jZolh9fn7SFNyi in AimbotConfig.TargetTypes)
				{
					bool flag11 = !string.IsNullOrEmpty(text);
					if (flag11)
					{
						text += string.Format("->{0}", dr5qliNNQh3jZolh9fn7SFNyi);
					}
					else
					{
						text += dr5qliNNQh3jZolh9fn7SFNyi.ToString();
					}
				}
				bool flag12 = !string.IsNullOrEmpty(text);
				if (flag12)
				{
					GUILayout.Label("Sorting Type: " + text, Array.Empty<GUILayoutOption>());
				}
				else
				{
					GUILayout.Label("Sorting Type: None", Array.Empty<GUILayoutOption>());
				}
				byte b = 0;
				while ((int)b < Enum.GetValues(typeof(TargetType)).Length)
				{
					TargetType dr5qliNNQh3jZolh9fn7SFNyi2 = (TargetType)b;
					bool flag13 = AimbotConfig.TargetTypes.Contains(dr5qliNNQh3jZolh9fn7SFNyi2);
					flag13 = MenuGuiHelper.DrawCheckbox(flag13, "Target " + dr5qliNNQh3jZolh9fn7SFNyi2.ToString(), Array.Empty<GUILayoutOption>());
					bool flag14 = !AimbotConfig.TargetTypes.Contains(dr5qliNNQh3jZolh9fn7SFNyi2) && flag13;
					if (flag14)
					{
						AimbotConfig.TargetTypes.Add(dr5qliNNQh3jZolh9fn7SFNyi2);
					}
					else
					{
						bool flag15 = AimbotConfig.TargetTypes.Contains(dr5qliNNQh3jZolh9fn7SFNyi2) && !flag13;
						if (flag15)
						{
							AimbotConfig.TargetTypes.Remove(dr5qliNNQh3jZolh9fn7SFNyi2);
						}
					}
					b += 1;
				}
			}
			GUILayout.EndScrollView();
		}
		else
		{
			bool flag16 = tc == TabCount.Two;
			if (flag16)
			{
				base.DrawSectionHeader("Memory Aimbot");
				this.MemoryAimScroll = GUILayout.BeginScrollView(this.MemoryAimScroll, Array.Empty<GUILayoutOption>());
				AimbotConfig.enableAimbot = MenuGuiHelper.DrawCheckbox(AimbotConfig.enableAimbot, "Enable Memory Aimbot", Array.Empty<GUILayoutOption>());
				bool enableAimbot = AimbotConfig.enableAimbot;
				if (enableAimbot)
				{
					AimbotConfig.memoryAimbotFOV = MenuGuiHelper.LabeledIntSlider("Aimbot FOV: ", AimbotConfig.memoryAimbotFOV, 10, 500, -1);
					bool flag17 = MenuGuiHelper.Button("Aim Key: " + (AimbotConfig.waitingForMemoryAimKey ? "Press any key..." : ((AimbotConfig.memoryAimbotKeybind == KeyCode.None) ? "None (Always)" : AimbotConfig.memoryAimbotKeybind.ToString())), -1, true, null);
					if (flag17)
					{
						AimbotConfig.waitingForMemoryAimKey = !AimbotConfig.waitingForMemoryAimKey;
					}
					else
					{
						bool waitingForMemoryAimKey = AimbotConfig.waitingForMemoryAimKey;
						if (waitingForMemoryAimKey)
						{
							bool flag18 = Event.current.isKey && Event.current.keyCode > KeyCode.None;
							if (flag18)
							{
								bool flag19 = Event.current.keyCode == KeyCode.Escape;
								if (flag19)
								{
									AimbotConfig.waitingForMemoryAimKey = false;
								}
								else
								{
									AimbotConfig.memoryAimbotKeybind = Event.current.keyCode;
									AimbotConfig.waitingForMemoryAimKey = false;
								}
							}
							else
							{
								bool flag20 = Event.current.type == EventType.MouseDown;
								if (flag20)
								{
									AimbotConfig.memoryAimbotKeybind = KeyCode.Mouse0 + Event.current.button;
									AimbotConfig.waitingForMemoryAimKey = false;
								}
							}
						}
					}
					AimbotConfig.bestAimbotPartPreselective = MenuGuiHelper.DrawCheckbox(AimbotConfig.bestAimbotPartPreselective, "Target Best Hitbox", Array.Empty<GUILayoutOption>());
					AimbotConfig.aimbotLimb = AimbotConfig.aimbotLimb.EnumPopup("Aimbot Hitbox: ", AimbotConfig.aimbotLimb.ToDisplayNameLimb());
					bool flag22 = AimbotConfig.aimbotLimb == Limb.Random;
					if (flag22)
					{
						AimbotConfig.randomLimbHeadHitChance = MenuGuiHelper.LabeledIntSlider("Random Head Hit Chance: ", AimbotConfig.randomLimbHeadHitChance, 0, 100, -1);
					}
					AimbotConfig.smoothAimbot = MenuGuiHelper.DrawCheckbox(AimbotConfig.smoothAimbot, "Aimbot Smoothing", Array.Empty<GUILayoutOption>());
					bool smoothAimbot = AimbotConfig.smoothAimbot;
					if (smoothAimbot)
					{
						AimbotConfig.smoothAimbotSpeed = MenuGuiHelper.LabeledFloatSlider("Smoothing Speed: ", AimbotConfig.smoothAimbotSpeed, 1f, 25f, -1);
					}
					AimbotConfig.stickyAim = MenuGuiHelper.DrawCheckbox(AimbotConfig.stickyAim, "Sticky Aim", Array.Empty<GUILayoutOption>());
					AimbotConfig.alwaysAim = MenuGuiHelper.DrawCheckbox(AimbotConfig.alwaysAim, "Always Aim", Array.Empty<GUILayoutOption>());
					AimbotConfig.memoryAimbotCheckWalls = MenuGuiHelper.DrawCheckbox(AimbotConfig.memoryAimbotCheckWalls, "Check Walls (Linecast)", Array.Empty<GUILayoutOption>());
					AimbotConfig.aimingChance = MenuGuiHelper.LabeledIntSlider("Aimbot Hit Chance: ", AimbotConfig.aimingChance, 0, 100, -1);
					GUILayout.Space(10f);
					GUILayout.Label("Target Types:", Array.Empty<GUILayoutOption>());
					byte b2 = 0;
					while ((int)b2 < Enum.GetValues(typeof(TargetType)).Length)
					{
						TargetType dr5qliNNQh3jZolh9fn7SFNyi3 = (TargetType)b2;
						bool flag23 = AimbotConfig.TargetTypes.Contains(dr5qliNNQh3jZolh9fn7SFNyi3);
						flag23 = MenuGuiHelper.DrawCheckbox(flag23, "Target " + dr5qliNNQh3jZolh9fn7SFNyi3.ToString(), Array.Empty<GUILayoutOption>());
						bool flag24 = !AimbotConfig.TargetTypes.Contains(dr5qliNNQh3jZolh9fn7SFNyi3) && flag23;
						if (flag24)
						{
							AimbotConfig.TargetTypes.Add(dr5qliNNQh3jZolh9fn7SFNyi3);
						}
						else
						{
							bool flag25 = AimbotConfig.TargetTypes.Contains(dr5qliNNQh3jZolh9fn7SFNyi3) && !flag23;
							if (flag25)
							{
								AimbotConfig.TargetTypes.Remove(dr5qliNNQh3jZolh9fn7SFNyi3);
							}
						}
						b2 += 1;
					}
				}
				GUILayout.EndScrollView();
			}
			else
			{
				base.DrawSectionHeader("Shared Options");
				this.SharedOptionsScroll = GUILayout.BeginScrollView(this.SharedOptionsScroll, Array.Empty<GUILayoutOption>());
				AimbotConfig.setDistanceByGunRange = MenuGuiHelper.DrawCheckbox(AimbotConfig.setDistanceByGunRange, "Auto Distance by Gun Range", Array.Empty<GUILayoutOption>());
				bool flag26 = !AimbotConfig.setDistanceByGunRange;
				if (flag26)
				{
					AimbotConfig.aimTargetDistance = MenuGuiHelper.LabeledIntSlider("Aim Distance: ", AimbotConfig.aimTargetDistance, 0, 500, -1);
				}
				AimbotConfig.restrictAimByFov = MenuGuiHelper.DrawCheckbox(AimbotConfig.restrictAimByFov, "Restrict Aim by FOV", Array.Empty<GUILayoutOption>());
				AimbotConfig.drawTarget = MenuGuiHelper.DrawCheckbox(AimbotConfig.drawTarget, "Draw Aim Target", Array.Empty<GUILayoutOption>());
				AimbotConfig.checkWithLinecast = MenuGuiHelper.DrawCheckbox(AimbotConfig.checkWithLinecast, "Check Target with Linecast", Array.Empty<GUILayoutOption>());
				AimbotConfig.dontShootPlayersOnSafezone = MenuGuiHelper.DrawCheckbox(AimbotConfig.dontShootPlayersOnSafezone, "Skip Players in Safezone", Array.Empty<GUILayoutOption>());
				AimbotConfig.manuallyCalculateBallisticDistance = MenuGuiHelper.DrawCheckbox(AimbotConfig.manuallyCalculateBallisticDistance, "Manual Ballistic Distance", Array.Empty<GUILayoutOption>());
				AimbotConfig.aimSorting = AimbotConfig.aimSorting.EnumPopup("Player Sorting: ", "");
				bool flag27 = MenuGuiHelper.Button("Player Sorting: " + AimbotConfig.aimSorting.ToString(), -1, true, null);
				if (flag27)
				{
					AimbotConfig.aimSorting = AimbotConfig.aimSorting.Next<AimSorting>();
				}
				AimbotConfig.previewHitLimb = MenuGuiHelper.DrawCheckbox(AimbotConfig.previewHitLimb, "Preview Hit Limb", Array.Empty<GUILayoutOption>());
				bool previewHitLimb = AimbotConfig.previewHitLimb;
				if (previewHitLimb)
				{
					AimbotConfig.hitMarkSize = MenuGuiHelper.LabeledIntSlider("Hitmark Size: ", AimbotConfig.hitMarkSize, 4, 20, -1);
				}
				GUILayout.Label("Aim Target Line Position: ", Array.Empty<GUILayoutOption>());
				GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
				GUILayout.Label("X:", Array.Empty<GUILayoutOption>());
				AimbotConfig.targetLineStartX = MenuGuiHelper.FloatTextField(AimbotConfig.targetLineStartX);
				GUILayout.Label("Y:", Array.Empty<GUILayoutOption>());
				AimbotConfig.targetLineStartY = MenuGuiHelper.FloatTextField(AimbotConfig.targetLineStartY);
				GUILayout.EndHorizontal();
				GUILayout.Space(10f);
				AimbotConfig.enableBacktrack = MenuGuiHelper.DrawCheckbox(AimbotConfig.enableBacktrack, "Enable Backtrack", Array.Empty<GUILayoutOption>());
				bool enableBacktrack = AimbotConfig.enableBacktrack;
				if (enableBacktrack)
				{
					AimbotConfig.showBacktrackVisualizer = MenuGuiHelper.DrawCheckbox(AimbotConfig.showBacktrackVisualizer, "Show Backtrack Visualizer", Array.Empty<GUILayoutOption>());
					AimbotConfig.backtrackTimeMs = MenuGuiHelper.LabeledFloatSlider("Backtrack Time (ms): ", AimbotConfig.backtrackTimeMs, 0f, 500f, -1);
					AimbotConfig.backtrackMaxHistory = MenuGuiHelper.LabeledIntSlider("Max History Entries: ", AimbotConfig.backtrackMaxHistory, 8, 512, -1);
				}
				GUILayout.Space(10f);
				AimbotConfig.bulletDelaying = MenuGuiHelper.DrawCheckbox(AimbotConfig.bulletDelaying, "Bullet Delay", Array.Empty<GUILayoutOption>());
				bool bulletDelaying = AimbotConfig.bulletDelaying;
				if (bulletDelaying)
				{
					AimbotConfig.showBulletDelayingTimer = MenuGuiHelper.DrawCheckbox(AimbotConfig.showBulletDelayingTimer, "Show Delay Timer", Array.Empty<GUILayoutOption>());
					AimbotConfig.bulletDelayAmount = MenuGuiHelper.LabeledIntSlider("Max Delay Amount: ", AimbotConfig.bulletDelayAmount, 1, 100, -1);
					AimbotConfig.bulletDelaySeconds = MenuGuiHelper.LabeledFloatSlider("Delay Seconds: ", AimbotConfig.bulletDelaySeconds, 0f, 10f, -1);
					bool flag28 = MenuGuiHelper.Button("Release Key: " + (AimbotConfig.waitingForDelayKey ? "Press any key..." : AimbotConfig.bulletDelayKeybind.ToString()), -1, true, null);
					if (flag28)
					{
						AimbotConfig.waitingForDelayKey = !AimbotConfig.waitingForDelayKey;
					}
					else
					{
						bool waitingForDelayKey = AimbotConfig.waitingForDelayKey;
						if (waitingForDelayKey)
						{
							bool flag29 = Event.current.isKey && Event.current.keyCode > KeyCode.None;
							if (flag29)
							{
								bool flag30 = Event.current.keyCode == KeyCode.Escape;
								if (flag30)
								{
									AimbotConfig.waitingForDelayKey = false;
								}
								else
								{
									AimbotConfig.bulletDelayKeybind = Event.current.keyCode;
									AimbotConfig.waitingForDelayKey = false;
								}
							}
							else
							{
								bool flag31 = Event.current.type == EventType.MouseDown;
								if (flag31)
								{
									AimbotConfig.bulletDelayKeybind = KeyCode.Mouse0 + Event.current.button;
									AimbotConfig.waitingForDelayKey = false;
								}
							}
						}
					}
					AimbotConfig.unholdDelayByMouse = MenuGuiHelper.DrawCheckbox(AimbotConfig.unholdDelayByMouse, "Release Delay on Mouse Up", Array.Empty<GUILayoutOption>());
					bool unholdDelayByMouse = AimbotConfig.unholdDelayByMouse;
					if (unholdDelayByMouse)
					{
						AimbotConfig.momentalyUnhold = MenuGuiHelper.DrawCheckbox(AimbotConfig.momentalyUnhold, "Instant Release on Mouse Up", Array.Empty<GUILayoutOption>());
					}
				}
				GUILayout.EndScrollView();
			}
		}
	}
	public Vector2 SharedOptionsScroll = Vector2.zero;
	public Vector2 SilentAimScroll = Vector2.zero;
	public Vector2 MemoryAimScroll = Vector2.zero;
}
