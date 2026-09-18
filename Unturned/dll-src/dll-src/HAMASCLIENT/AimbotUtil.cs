using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using SDG.Unturned;
using UnityEngine;
public static class AimbotUtil
{
	// (get) Token: 0x06000128 RID: 296 RVA: 0x0000E078 File Offset: 0x0000C278
	public static AimObjective aimObjective
	{
		get
		{
			bool flag = !AimbotConfig.enableAim;
			bool flag2 = flag;
			if (flag2)
			{
				AimbotUtil.currentAimObjective = AimbotUtil.FindAimObjective();
			}
			return AimbotUtil.currentAimObjective;
		}
	}
	[InitializeAttribute]
	private static void ClearStateOnDisconnect()
	{
		Provider.onClientDisconnected = delegate
		{
			AimbotUtil.bulletEventHistoryPerPlayer.Clear();
			AimbotUtil.DbacktrackHistory.Clear();
		};
	}
	public static void RecordBacktrackPositions()
	{
		bool flag = !AimbotConfig.enableBacktrack;
		if (!flag)
		{
			float time = Time.time;
			int backtrackMaxHistory = AimbotConfig.backtrackMaxHistory;
			float num = AimbotConfig.backtrackTimeMs / 1000f;
			HashSet<ulong> hashSet = new HashSet<ulong>();
			foreach (SteamPlayer steamPlayer in Provider.clients)
			{
				bool flag2 = steamPlayer == null || steamPlayer.player == null;
				if (!flag2)
				{
					bool flag3 = steamPlayer.player.channel.IsLocalPlayer || steamPlayer.player.life.isDead;
					if (!flag3)
					{
						ulong steamID = steamPlayer.player.channel.owner.playerID.steamID.m_SteamID;
						hashSet.Add(steamID);
						List<AimbotUtil.BacktrackSnapshot> list;
						bool flag4 = !AimbotUtil.DbacktrackHistory.TryGetValue(steamID, out list);
						if (flag4)
						{
							list = new List<AimbotUtil.BacktrackSnapshot>();
							AimbotUtil.DbacktrackHistory[steamID] = list;
						}
						list.Add(new AimbotUtil.BacktrackSnapshot
						{
							position = steamPlayer.player.transform.position,
							time = time
						});
						int num2 = list.Count - backtrackMaxHistory;
						bool flag5 = num2 > 0;
						if (flag5)
						{
							list.RemoveRange(0, num2);
						}
						int num3 = 0;
						while (num3 < list.Count && time - list[num3].time > num)
						{
							num3++;
						}
						bool flag6 = num3 > 0;
						if (flag6)
						{
							list.RemoveRange(0, num3);
						}
					}
				}
			}
			List<ulong> list2 = null;
			foreach (KeyValuePair<ulong, List<AimbotUtil.BacktrackSnapshot>> keyValuePair in AimbotUtil.DbacktrackHistory)
			{
				bool flag7 = !hashSet.Contains(keyValuePair.Key);
				if (flag7)
				{
					bool flag8 = list2 == null;
					if (flag8)
					{
						list2 = new List<ulong>();
					}
					list2.Add(keyValuePair.Key);
				}
			}
			bool flag9 = list2 != null;
			if (flag9)
			{
				foreach (ulong num4 in list2)
				{
					AimbotUtil.DbacktrackHistory.Remove(num4);
				}
			}
		}
	}
	public static Vector3 GetBacktrackPosition(Player player)
	{
		bool flag = !AimbotConfig.enableBacktrack || player == null;
		Vector3 vector;
		if (flag)
		{
			vector = ((player != null) ? player.transform.position : Vector3.zero);
		}
		else
		{
			ulong steamID = player.channel.owner.playerID.steamID.m_SteamID;
			List<AimbotUtil.BacktrackSnapshot> list;
			bool flag2 = !AimbotUtil.DbacktrackHistory.TryGetValue(steamID, out list) || list.Count == 0;
			if (flag2)
			{
				vector = player.transform.position;
			}
			else
			{
				float num = Time.time - AimbotConfig.backtrackTimeMs / 1000f;
				vector = AimbotUtil.GetBacktrackPositionAtTime(list, num);
			}
		}
		return vector;
	}
	public static Vector3 GetBacktrackPosition(Player player, float timeMs)
	{
		bool flag = !AimbotConfig.enableBacktrack || player == null;
		Vector3 vector;
		if (flag)
		{
			vector = ((player != null) ? player.transform.position : Vector3.zero);
		}
		else
		{
			ulong steamID = player.channel.owner.playerID.steamID.m_SteamID;
			List<AimbotUtil.BacktrackSnapshot> list;
			bool flag2 = !AimbotUtil.DbacktrackHistory.TryGetValue(steamID, out list) || list.Count == 0;
			if (flag2)
			{
				vector = player.transform.position;
			}
			else
			{
				float num = Time.time - timeMs / 1000f;
				vector = AimbotUtil.GetBacktrackPositionAtTime(list, num);
			}
		}
		return vector;
	}
	public static Vector3 GetBacktrackAimPosition(Player player)
	{
		bool flag = !AimbotConfig.enableBacktrack || player == null;
		Vector3 vector;
		if (flag)
		{
			vector = ((player != null) ? player.GetAimPoint() : Vector3.zero);
		}
		else
		{
			Vector3 vector2 = player.GetAimPoint();
			Vector3 position = player.transform.position;
			Vector3 backtrackPosition = AimbotUtil.GetBacktrackPosition(player);
			vector = vector2 + (backtrackPosition - position);
		}
		return vector;
	}
	public static Vector3 GetBacktrackPositionAtTime(Player player, float absoluteTime)
	{
		bool flag = !AimbotConfig.enableBacktrack || player == null;
		Vector3 vector;
		if (flag)
		{
			vector = ((player != null) ? player.transform.position : Vector3.zero);
		}
		else
		{
			ulong steamID = player.channel.owner.playerID.steamID.m_SteamID;
			List<AimbotUtil.BacktrackSnapshot> list;
			bool flag2 = !AimbotUtil.DbacktrackHistory.TryGetValue(steamID, out list) || list.Count == 0;
			if (flag2)
			{
				vector = player.transform.position;
			}
			else
			{
				float num = absoluteTime - AimbotConfig.backtrackTimeMs / 1000f;
				vector = AimbotUtil.GetBacktrackPositionAtTime(list, num);
			}
		}
		return vector;
	}
	private static Vector3 GetBacktrackPositionAtTime(List<AimbotUtil.BacktrackSnapshot> history, float targetTime)
	{
		bool flag = history.Count == 0;
		Vector3 vector;
		if (flag)
		{
			vector = Vector3.zero;
		}
		else
		{
			bool flag2 = history.Count == 1;
			if (flag2)
			{
				vector = history[0].position;
			}
			else
			{
				int i = 0;
				int num = history.Count - 1;
				while (i <= num)
				{
					int num2 = i + num >> 1;
					bool flag3 = history[num2].time < targetTime;
					if (flag3)
					{
						i = num2 + 1;
					}
					else
					{
						num = num2 - 1;
					}
				}
				int num3 = i;
				bool flag4 = num3 >= history.Count;
				if (flag4)
				{
					num3 = history.Count - 1;
				}
				bool flag5 = num3 > 0;
				if (flag5)
				{
					float num4 = Mathf.Abs(history[num3].time - targetTime);
					float num5 = Mathf.Abs(history[num3 - 1].time - targetTime);
					bool flag6 = num5 < num4;
					if (flag6)
					{
						num3--;
					}
				}
				bool flag7 = num3 < 0;
				if (flag7)
				{
					num3 = 0;
				}
				bool flag8 = num3 + 1 < history.Count;
				if (flag8)
				{
					float time = history[num3].time;
					float time2 = history[num3 + 1].time;
					bool flag9 = time2 > time && targetTime >= time && targetTime <= time2;
					if (flag9)
					{
						float num6 = (targetTime - time) / (time2 - time);
						return Vector3.Lerp(history[num3].position, history[num3 + 1].position, num6);
					}
				}
				vector = history[num3].position;
			}
		}
		return vector;
	}
	public static void RefreshAimObjective()
	{
		bool enableAim = AimbotConfig.enableAim;
		bool flag = enableAim;
		if (flag)
		{
			AimbotUtil.currentAimObjective = AimbotUtil.FindAimObjective();
		}
	}
	public static void Update()
	{
		AimbotUtil.RecordBacktrackPositions();
		bool enableAim = AimbotConfig.enableAim;
		bool flag = enableAim;
		if (flag)
		{
			// Target scan only — the actual angle write happens once per frame
			// in UpdateThrottler.LateUpdate, after the game's own PlayerLook.Update
			// applied mouse/recoil. Writing here too raced with it and made the
			// camera flash between two positions.
			AimbotUtil.UpdateStickyTarget();
			AimbotUtil.currentAimObjective = AimbotUtil.FindAimObjective();
		}
		bool flag3 = AimbotConfig.autoBestSphereSize && AimbotConfig.enableSilentAim && AimbotConfig.silentAimType == SilentAimType.Sphere;
		bool flag4 = flag3;
		if (flag4)
		{
			try
			{
				Transform transform = AimbotUtil.GetAimBoneTransform(AimbotUtil.currentAimObjective.Target);
				bool flag5 = transform != null;
				if (flag5)
				{
					Vector3 vector = transform.position;
					bool flag6 = AimbotConfig.enableBacktrack && AimbotUtil.currentAimObjective.Target is Player;
					if (flag6)
					{
						vector = AimbotUtil.GetBacktrackAimPosition((Player)AimbotUtil.currentAimObjective.Target);
					}
					float num = Vector3.Distance(Player.player.look.aim.position, vector);
					float num2 = Mathf.Clamp(2f - num / 100f, 0.5f, 2f);
					foreach (AimSphereOptions daqXfD9Fjc9IG7oF0OAH4hEOR in AimbotConfig.SphereSizes)
					{
						daqXfD9Fjc9IG7oF0OAH4hEOR.sphereSize = num2;
					}
				}
			}
			catch
			{
			}
		}
	}
	public static void AimAtObjective(bool research = false)
	{
		bool flag = AimbotUtil.currentAimObjective.Target == null || !AimbotUtil.DIsTargetValid(AimbotUtil.currentAimObjective.Target);
		bool flag2 = !flag;
		if (flag2)
		{
			if (research)
			{
				bool flag3 = AimbotUtil.aimObjective.TargetType == TargetType.Player;
				bool flag4 = flag3;
				if (flag4)
				{
					Vector3 vector = (AimbotUtil.aimObjective.Target as Player).GetAimPoint();
					bool enableBacktrack = AimbotConfig.enableBacktrack;
					if (enableBacktrack)
					{
						vector = AimbotUtil.GetBacktrackAimPosition((Player)AimbotUtil.aimObjective.Target);
					}
					AimbotUtil.RotateLookToPoint(vector);
				}
				else
				{
					Transform transform = AimbotUtil.GetTargetTransform(AimbotUtil.aimObjective.TargetType, AimbotUtil.aimObjective.Target);
					bool flag5 = transform != null;
					bool flag6 = flag5;
					if (flag6)
					{
						AimbotUtil.RotateLookToPoint(transform.position);
					}
				}
			}
			else
			{
				bool flag7 = AimbotUtil.currentAimObjective.TargetType == TargetType.Player;
				bool flag8 = flag7;
				if (flag8)
				{
					Vector3 vector2 = (AimbotUtil.currentAimObjective.Target as Player).GetAimPoint();
					bool enableBacktrack2 = AimbotConfig.enableBacktrack;
					if (enableBacktrack2)
					{
						vector2 = AimbotUtil.GetBacktrackAimPosition((Player)AimbotUtil.currentAimObjective.Target);
					}
					AimbotUtil.RotateLookToPoint(vector2);
				}
				else
				{
					Transform transform2 = AimbotUtil.GetTargetTransform(AimbotUtil.currentAimObjective.TargetType, AimbotUtil.currentAimObjective.Target);
					bool flag9 = transform2 != null;
					bool flag10 = flag9;
					if (flag10)
					{
						AimbotUtil.RotateLookToPoint(transform2.position);
					}
				}
			}
		}
	}
	public static void RotateLookToPoint(Vector3 pos)
	{
		bool flag = Player.player.movement.getVehicle() != null;
		bool flag2 = flag;
		if (flag2)
		{
			Player.player.movement.getVehicle().transform.eulerAngles = MathUtil.LookRotationEulerTo(MainCamera.instance.transform.position, pos);
		}
		else
		{
			float curYaw = AimbotUtil.playerLookYawField.Get(Player.player.look);
			float curPitch = AimbotUtil.playerLookPitchField.Get(Player.player.look);
			float num;
			float num2;
			Transform aimT = Player.player.look.aim;
			Vector3 pivot = ((aimT != null) ? aimT.position : Vector3.zero);
			bool flag4 = aimT != null && (pos - pivot).sqrMagnitude > 0.04f;
			if (flag4)
			{
				// Ground truth from UseableGun.fire IL (third person): the game raycasts
				// from MainCamera along its forward, RE-POINTS look.aim at whatever that
				// ray hits (or C + D*512 if it hits nothing), then fires the bullet from
				// the lean-shifted pivot through that hit point. So the bullet lands
				// wherever the CAMERA ray actually hits — the lean miss is just the
				// camera ray not passing through the target.
				// Fix: aim the CAMERA at the target. The ray then hits the target, the
				// game re-points the pivot at that hit, and the bullet is on target by
				// construction — any lean, any distance, independent of hit depth.
				// In first person the camera sits at the pivot, so this reduces to the
				// same analytic aim as before (pixel-perfect FP unchanged).
				Vector3 desired = pos - pivot;
				bool flag5 = MainCamera.instance != null;
				if (flag5)
				{
					Vector3 fromCam = pos - MainCamera.instance.transform.position;
					bool flag6 = fromCam.sqrMagnitude > 0.001f;
					if (flag6)
					{
						desired = fromCam;
					}
				}
				float mag = desired.magnitude;
				bool flag7 = mag > 0.001f;
				if (flag7)
				{
					desired /= mag;
				}
				else
				{
					desired = (pos - pivot).normalized;
				}
				num = Mathf.Atan2(desired.x, desired.z) * 57.29578f;
				float horiz = Mathf.Sqrt(desired.x * desired.x + desired.z * desired.z);
				num2 = Mathf.Clamp(90f - Mathf.Atan2(desired.y, horiz) * 57.29578f, 0f, 180f);
			}
			else
			{
				Vector3 position = Player.player.transform.position + Vector3.up * Player.player.look.heightLook;
				Vector3 normalized = (pos - position).normalized;
				num = Mathf.Atan2(normalized.x, normalized.z) * 57.29578f;
				float num9 = Mathf.Sqrt(normalized.x * normalized.x + normalized.z * normalized.z);
				float num10 = Mathf.Atan2(normalized.y, num9) * 57.29578f;
				num2 = Mathf.Clamp(90f - num10, 0f, 180f);
				while (num - curYaw > 180f)
				{
					num -= 360f;
				}
				while (num - curYaw < -180f)
				{
					num += 360f;
				}
			}
			bool smoothAimbot = AimbotConfig.smoothAimbot;
			bool flag3 = smoothAimbot;
			if (flag3)
			{
				float num11 = Mathf.Clamp01(Time.deltaTime * AimbotConfig.smoothAimbotSpeed);
				float num4 = Mathf.LerpAngle(curYaw, num, num11);
				float num5 = Mathf.LerpAngle(curPitch, num2, num11);
				AimbotUtil.playerLookPitchField.Set(Player.player.look, num5);
				AimbotUtil.playerLookYawField.Set(Player.player.look, num4);
			}
			else
			{
				AimbotUtil.playerLookPitchField.Set(Player.player.look, num2);
				AimbotUtil.playerLookYawField.Set(Player.player.look, num);
			}
		}
	}
	// ── Sticky aim ─────────────────────────────────────────
	// While the aim key is held and sticky aim is on, the first target picked
	// is LOCKED — no target switching until it dies/goes invalid or the key is
	// released. Kills the flicker/flip-flop when several players or a pack of
	// zombies cluster inside the FOV.
	public static object stickyTarget;

	private static void UpdateStickyTarget()
	{
		bool active = AimbotConfig.enableAimbot && AimbotConfig.stickyAim && (AimbotConfig.alwaysAim || AimbotConfig.IsMemoryAimbotKeyActive());
		if (!active)
		{
			AimbotUtil.stickyTarget = null;
			return;
		}
		if (AimbotUtil.stickyTarget != null && !AimbotUtil.DIsTargetValid(AimbotUtil.stickyTarget))
		{
			AimbotUtil.stickyTarget = null;
		}
	}

	public static AimObjective FindAimObjective()
	{
		// Sticky: if a target is locked, keep returning it (skip scanning entirely)
		bool sticky = AimbotUtil.stickyTarget != null;
		if (sticky)
		{
			object locked = AimbotUtil.stickyTarget;
			Vector3 point;
			if (locked is Player)
			{
				point = ((Player)locked).GetAimPoint();
				if (AimbotConfig.enableBacktrack)
				{
					point = AimbotUtil.GetBacktrackAimPosition((Player)locked);
				}
			}
			else
			{
				Transform bone = AimbotUtil.GetAimBoneTransform(locked);
				point = ((bone != null) ? bone.position : Vector3.zero);
			}
			TargetType lockedType = TargetType.Player;
			if (locked is Zombie) lockedType = TargetType.Zombies;
			else if (locked is Animal) lockedType = TargetType.Animals;
			else if (locked is InteractableVehicle) lockedType = TargetType.Vehicles;
			else if (locked is InteractableBed) lockedType = TargetType.Beds;
			else if (locked is InteractableClaim) lockedType = TargetType.ClaimFlags;
			else if (locked is InteractableStorage) lockedType = TargetType.Storages;
			return new AimObjective(lockedType, locked, point);
		}
		AimObjective result = AimbotUtil.FindBestTargetInternal();
		// Lock onto the freshly picked target (sticky aim only grabs when a key is held —
		// UpdateStickyTarget already verified that)
		if (AimbotConfig.stickyAim && result.Target != null)
		{
			AimbotUtil.stickyTarget = result.Target;
		}
		return result;
	}

	private static AimObjective FindBestTargetInternal()
	{
		int num = (AimbotConfig.enableAimbot ? AimbotConfig.memoryAimbotFOV : FovCircleRenderer.GetFovRadius("Aimbot FOV"));
		foreach (TargetType dr5qliNNQh3jZolh9fn7SFNyi in AimbotConfig.TargetTypes)
		{
			try
			{
				bool flag = (AimbotConfig.enableAimbot ? AimbotConfig.memoryAimbotCheckWalls : (AimbotConfig.checkWithLinecast && !AimbotConfig.enableSilentAim));
				AimObjective du4XicP1hVrJzXjQ70aniDyvk = AimbotUtil.FindBestTarget(MathUtil.GetAimTargetDistance(), num, dr5qliNNQh3jZolh9fn7SFNyi, AimbotConfig.aimSorting, flag, AimbotConfig.enableSilentAim, false);
				bool flag2 = du4XicP1hVrJzXjQ70aniDyvk.Target != null;
				bool flag3 = flag2;
				if (flag3)
				{
					return du4XicP1hVrJzXjQ70aniDyvk;
				}
			}
			catch
			{
				return new AimObjective(TargetType.Player, null, Vector3.zero);
			}
		}
		return new AimObjective(TargetType.Player, null, Vector3.zero);
	}
	public static AimObjective FindBestTarget(int distance, int fov, TargetType goal, AimSorting sorting, bool checkWalls = true, bool checkBySilent = true, bool tryExpandSphere = false)
	{
		int num;
		switch (goal)
		{
		case TargetType.Player:
		{
			num = Provider.clients.Count;
			bool flag = num == 1;
			bool flag2 = flag;
			if (flag2)
			{
				return new AimObjective(goal, null, Vector3.zero);
			}
			break;
		}
		case TargetType.ClaimFlags:
			num = InteractableTracker.ClaimFlags.Count;
			break;
		case TargetType.Beds:
			num = InteractableTracker.Beds.Count;
			break;
		case TargetType.Storages:
			num = InteractableTracker.Storages.Count;
			break;
		case TargetType.Zombies:
			num = ZombiePatches.TrackedZombies.Count;
			break;
		case TargetType.Animals:
			num = AnimalManager.animals.Count;
			break;
		case TargetType.Vehicles:
			num = VehicleManager.vehicles.Count;
			break;
		default:
			return new AimObjective(goal, null, Vector3.zero);
		}
		int num2 = distance + 1;
		int num3 = fov + 1;
		int num4 = -1;
		int num5 = -1;
		Vector3 vector = Vector3.zero;
		Vector3 vector2 = Vector3.zero;
		Vector3 vector3 = Vector3.zero;
		Transform transform = ((Player.player.look.aim.transform != null) ? Player.player.look.aim.transform : ((MainCamera.instance != null) ? MainCamera.instance.transform : Camera.current.transform));
		int i = 0;
		while (i < num)
		{
			Vector3 vector4;
			switch (goal)
			{
			case TargetType.Player:
			{
				bool flag3 = Provider.clients[i] == null || Provider.clients[i].player == null;
				bool flag4 = !flag3;
				if (flag4)
				{
					bool flag5 = Provider.clients[i].player.channel.IsLocalPlayer || Provider.clients[i].player.life.isDead;
					bool flag6 = !flag5;
					if (flag6)
					{
						bool flag7 = PlayerPriorityManager.IsFriendOrGroupMateSteamPlayer(Provider.clients[i]);
						bool flag8 = !flag7;
						if (flag8)
						{
							bool flag9 = AimbotConfig.dontShootPlayersOnSafezone && LevelNodes.isPointInsideSafezone(Provider.clients[i].player.transform.position, out Provider.clients[i].player.movement.isSafeInfo);
							bool flag10 = !flag9;
							if (flag10)
							{
								vector4 = Provider.clients[i].player.GetAimPoint();
								bool enableBacktrack = AimbotConfig.enableBacktrack;
								if (enableBacktrack)
								{
									vector4 = AimbotUtil.GetBacktrackAimPosition(Provider.clients[i].player);
								}
								goto IL_048F;
							}
						}
					}
				}
				break;
			}
			case TargetType.ClaimFlags:
			{
				bool flag11 = InteractableTracker.ClaimFlags[i] == null;
				bool flag12 = !flag11;
				if (flag12)
				{
					vector4 = InteractableTracker.ClaimFlags[i].transform.position;
					goto IL_048F;
				}
				break;
			}
			case TargetType.Beds:
			{
				bool flag13 = InteractableTracker.Beds[i] == null;
				bool flag14 = !flag13;
				if (flag14)
				{
					vector4 = InteractableTracker.Beds[i].transform.position;
					goto IL_048F;
				}
				break;
			}
			case TargetType.Storages:
			{
				bool flag15 = InteractableTracker.Storages[i] == null;
				bool flag16 = !flag15;
				if (flag16)
				{
					vector4 = InteractableTracker.Storages[i].transform.position;
					goto IL_048F;
				}
				break;
			}
			case TargetType.Zombies:
			{
				bool flag17 = ZombiePatches.TrackedZombies[i] == null || ZombiePatches.TrackedZombies[i].isDead;
				bool flag18 = !flag17;
				if (flag18)
				{
					vector4 = ZombiePatches.TrackedZombies[i].transform.position;
					goto IL_048F;
				}
				break;
			}
			case TargetType.Animals:
			{
				bool flag19 = AnimalManager.animals[i] == null || AnimalManager.animals[i].isDead;
				bool flag20 = !flag19;
				if (flag20)
				{
					vector4 = AnimalManager.animals[i].transform.position;
					goto IL_048F;
				}
				break;
			}
			case TargetType.Vehicles:
			{
				bool flag21 = VehicleManager.vehicles[i] == null || VehicleManager.vehicles[i].isDead;
				bool flag22 = !flag21;
				if (flag22)
				{
					vector4 = VehicleManager.vehicles[i].transform.position;
					goto IL_048F;
				}
				break;
			}
			default:
				vector4 = Vector3.zero;
				goto IL_048F;
			}
			IL_0483:
			i++;
			continue;
			IL_048F:
			bool flag23 = vector4 == Vector3.zero;
			bool flag24 = flag23;
			if (flag24)
			{
				goto IL_0483;
			}
			bool flag25 = AimbotConfig.enableVehicleHitboxExploit && goal == TargetType.Player && i < Provider.clients.Count && Provider.clients[i] != null && Provider.clients[i].player != null && Provider.clients[i].player.movement.getVehicle() != null;
			bool flag26 = Vector3.Distance(vector4, Player.player.transform.position) > (float)distance;
			bool flag27 = flag26;
			if (flag27)
			{
				goto IL_0483;
			}
			bool flag28 = !flag25 && (!checkBySilent || AimbotConfig.restrictAimByFov) && !vector4.IsOnScreen();
			bool flag29 = flag28;
			if (flag29)
			{
				goto IL_0483;
			}
			Vector3 vector5 = vector4.WorldToScreenPoint();
			bool flag30 = !flag25 && AimbotConfig.restrictAimByFov && Vector2.Distance(new Vector2((float)(Screen.width / 2), (float)(Screen.height / 2)), new Vector2(vector5.x, vector5.y)) > (float)fov;
			bool flag31 = flag30;
			if (flag31)
			{
				goto IL_0483;
			}
			bool flag32 = !flag25 && checkWalls;
			bool flag33 = flag32;
			if (flag33)
			{
				RaycastHit raycastHit;
				bool flag34 = Physics.Linecast(transform.position, vector4, out raycastHit, RayMasks.DAMAGE_CLIENT, QueryTriggerInteraction.Ignore);
				if (flag34)
				{
					Transform transform2 = null;
					switch (goal)
					{
					case TargetType.Player:
					{
						bool flag35 = i < Provider.clients.Count && Provider.clients[i] != null && Provider.clients[i].player != null;
						if (flag35)
						{
							transform2 = Provider.clients[i].player.transform;
						}
						break;
					}
					case TargetType.Zombies:
					{
						bool flag36 = i < ZombiePatches.TrackedZombies.Count && ZombiePatches.TrackedZombies[i] != null;
						if (flag36)
						{
							transform2 = ZombiePatches.TrackedZombies[i].transform;
						}
						break;
					}
					case TargetType.Animals:
					{
						bool flag37 = i < AnimalManager.animals.Count && AnimalManager.animals[i] != null;
						if (flag37)
						{
							transform2 = AnimalManager.animals[i].transform;
						}
						break;
					}
					case TargetType.Vehicles:
					{
						bool flag38 = i < VehicleManager.vehicles.Count && VehicleManager.vehicles[i] != null;
						if (flag38)
						{
							transform2 = VehicleManager.vehicles[i].transform;
						}
						break;
					}
					}
					bool flag39 = transform2 == null || (raycastHit.transform != transform2 && !raycastHit.transform.IsChildOf(transform2));
					flag32 = flag39;
				}
				else
				{
					flag32 = false;
				}
			}
			bool flag40 = flag32;
			if (flag40)
			{
				goto IL_0483;
			}
			vector3 = vector4;
			if (checkBySilent)
			{
				bool flag41 = !flag25 && AimbotConfig.silentAimType == SilentAimType.Distance && AimbotConfig.verifyForwardHitPointAviablity && Physics.Linecast(transform.position, vector4 + MathUtil.DirectionTo(vector4, transform.position) * Mathf.Clamp(Vector3.Distance(vector4, transform.position), 0f, (float)AimbotConfig.distanceToHit), RayMasks.DAMAGE_CLIENT, QueryTriggerInteraction.Ignore);
				bool flag42 = flag41;
				if (flag42)
				{
					goto IL_0483;
				}
				bool flag43 = !flag25 && AimbotConfig.silentAimType == SilentAimType.Sphere && !checkWalls;
				bool flag44 = flag43;
				if (flag44)
				{
					Vector3 vector6 = vector4;
					bool flag45 = !vector6.TryGetVisiblePointFromCamera(out vector3, Player.player.equipment.asset != null && !(Player.player.equipment.asset is ItemMeleeAsset));
					if (flag45)
					{
						goto IL_0483;
					}
				}
			}
			bool flag46 = sorting == AimSorting.Unmanaged;
			bool flag47 = flag46;
			if (flag47)
			{
				return AimbotUtil.BuildAimObjective(goal, i, vector3);
			}
			bool flag48 = Vector3.Distance(vector4, Player.player.transform.position) < (float)num2;
			bool flag49 = flag48;
			if (flag49)
			{
				vector = vector3;
				num4 = i;
				num2 = (int)Vector3.Distance(vector4, Player.player.transform.position);
			}
			bool flag50 = Vector2.Distance(new Vector2((float)(Screen.width / 2), (float)(Screen.height / 2)), new Vector2(vector5.x, vector5.y)) < (float)num3;
			bool flag51 = flag50;
			if (flag51)
			{
				vector2 = vector3;
				num5 = i;
				num3 = (int)Vector2.Distance(new Vector2((float)(Screen.width / 2), (float)(Screen.height / 2)), new Vector2(vector5.x, vector5.y));
			}
			goto IL_0483;
		}
		bool flag52 = num4 == -1;
		bool flag53 = flag52;
		AimObjective du4XicP1hVrJzXjQ70aniDyvk;
		if (flag53)
		{
			du4XicP1hVrJzXjQ70aniDyvk = new AimObjective(goal, null, Vector3.zero);
		}
		else
		{
			bool flag54 = sorting == AimSorting.Distance;
			bool flag55 = flag54;
			if (flag55)
			{
				du4XicP1hVrJzXjQ70aniDyvk = AimbotUtil.BuildAimObjective(goal, num4, vector);
			}
			else
			{
				bool flag56 = sorting == AimSorting.FOV;
				bool flag57 = flag56;
				if (flag57)
				{
					bool flag58 = num5 != -1;
					if (flag58)
					{
						du4XicP1hVrJzXjQ70aniDyvk = AimbotUtil.BuildAimObjective(goal, num5, vector2);
					}
					else
					{
						du4XicP1hVrJzXjQ70aniDyvk = AimbotUtil.BuildAimObjective(goal, num4, vector);
					}
				}
				else
				{
					du4XicP1hVrJzXjQ70aniDyvk = new AimObjective(goal, null, Vector3.zero);
				}
			}
		}
		return du4XicP1hVrJzXjQ70aniDyvk;
	}
	public static Transform GetTargetTransform(TargetType ag, object o)
	{
		bool flag = o == null;
		bool flag2 = flag;
		Transform transform;
		if (flag2)
		{
			transform = null;
		}
		else
		{
			switch (ag)
			{
			case TargetType.Player:
				transform = ((Player)o).transform;
				break;
			case TargetType.ClaimFlags:
				transform = ((InteractableClaim)o).transform;
				break;
			case TargetType.Beds:
				transform = ((InteractableBed)o).transform;
				break;
			case TargetType.Storages:
				transform = ((InteractableStorage)o).transform;
				break;
			case TargetType.Zombies:
				transform = ((Zombie)o).transform;
				break;
			case TargetType.Animals:
				transform = ((Animal)o).transform;
				break;
			case TargetType.Vehicles:
				transform = ((InteractableVehicle)o).transform;
				break;
			default:
				transform = null;
				break;
			}
		}
		return transform;
	}
	public static Transform GetAimBoneTransform(object o)
	{
		bool flag = o is Player;
		bool flag2 = flag;
		Transform transform;
		if (flag2)
		{
			transform = ((Player)o).GetAimTransform();
		}
		else
		{
			bool flag3 = o is InteractableBed;
			bool flag4 = flag3;
			if (flag4)
			{
				transform = ((InteractableBed)o).transform;
			}
			else
			{
				bool flag5 = o is InteractableClaim;
				bool flag6 = flag5;
				if (flag6)
				{
					transform = ((InteractableClaim)o).transform;
				}
				else
				{
					bool flag7 = o is InteractableStorage;
					bool flag8 = flag7;
					if (flag8)
					{
						transform = ((InteractableStorage)o).transform;
					}
					else
					{
						bool flag9 = o is InteractableVehicle;
						bool flag10 = flag9;
						if (flag10)
						{
							transform = ((InteractableVehicle)o).transform;
						}
						else
						{
							bool flag11 = o is Zombie;
							bool flag12 = flag11;
							if (flag12)
							{
								transform = ((Zombie)o).transform;
							}
							else
							{
								bool flag13 = o is Animal;
								bool flag14 = flag13;
								if (flag14)
								{
									transform = ((Animal)o).transform;
								}
								else
								{
									transform = null;
								}
							}
						}
					}
				}
			}
		}
		return transform;
	}
	public static bool ShouldAimByChance()
	{
		bool flag = AimbotConfig.aimingChance == 100;
		bool flag2 = flag;
		bool flag3;
		if (flag2)
		{
			flag3 = true;
		}
		else
		{
			bool flag4 = AimbotConfig.aimingChance != AimbotUtil.cachedAimingChance;
			bool flag5 = flag4;
			if (flag5)
			{
				AimbotUtil.aimingChanceAccumulator = 0;
				AimbotUtil.cachedAimingChance = AimbotConfig.aimingChance;
				flag3 = true;
			}
			else
			{
				AimbotUtil.aimingChanceAccumulator += AimbotUtil.cachedAimingChance;
				bool flag6 = AimbotUtil.aimingChanceAccumulator >= 100;
				bool flag7 = flag6;
				if (flag7)
				{
					AimbotUtil.aimingChanceAccumulator -= 100;
					flag3 = true;
				}
				else
				{
					flag3 = false;
				}
			}
		}
		return flag3;
	}
	private static AimObjective BuildAimObjective(TargetType ag, int index, Vector3 point)
	{
		AimObjective du4XicP1hVrJzXjQ70aniDyvk;
		switch (ag)
		{
		case TargetType.Player:
			du4XicP1hVrJzXjQ70aniDyvk = new AimObjective(ag, Provider.clients[index].player, point);
			break;
		case TargetType.ClaimFlags:
			du4XicP1hVrJzXjQ70aniDyvk = new AimObjective(ag, InteractableTracker.ClaimFlags[index], point);
			break;
		case TargetType.Beds:
			du4XicP1hVrJzXjQ70aniDyvk = new AimObjective(ag, InteractableTracker.Beds[index], point);
			break;
		case TargetType.Storages:
			du4XicP1hVrJzXjQ70aniDyvk = new AimObjective(ag, InteractableTracker.Storages[index], point);
			break;
		case TargetType.Zombies:
			du4XicP1hVrJzXjQ70aniDyvk = new AimObjective(ag, ZombiePatches.TrackedZombies[index], point);
			break;
		case TargetType.Animals:
			du4XicP1hVrJzXjQ70aniDyvk = new AimObjective(ag, AnimalManager.animals[index], point);
			break;
		case TargetType.Vehicles:
			du4XicP1hVrJzXjQ70aniDyvk = new AimObjective(ag, VehicleManager.vehicles[index], point);
			break;
		default:
			du4XicP1hVrJzXjQ70aniDyvk = new AimObjective(ag, null, point);
			break;
		}
		return du4XicP1hVrJzXjQ70aniDyvk;
	}
	public static void ForgeRaycastInfo(GameObject obj, object targetObject, Vector3 point, ref RaycastInfo info)
	{
		bool flag = info == null;
		if (flag)
		{
			info = (RaycastInfo)FormatterServices.GetUninitializedObject(typeof(RaycastInfo));
		}
		bool flag2 = targetObject is InteractableClaim || targetObject is InteractableBed || targetObject is InteractableStorage;
		bool flag3 = targetObject is Player && AimbotConfig.enableVehicleHitboxExploit && ((Player)targetObject).movement.getVehicle() != null;
		info.point = point;
		info.collider = ((obj.GetComponent<BoxCollider>() != null) ? obj.GetComponent<BoxCollider>() : obj.GetComponent<Collider>());
		info.transform = (flag2 ? DamageTool.getBarricadeRootTransform(obj.transform) : obj.transform);
		info.limb = AimbotConfig.silentAimLimb.ToLimb();
		AimbotUtil.AssignRaycastInfoTarget(ref info, targetObject);
		bool flag4 = flag2;
		bool flag5 = flag4;
		if (flag5)
		{
			info.materialName = PhysicsTool.GetMaterialName(info.point, info.transform, info.collider);
			info.material = PhysicsTool.GetLegacyMaterialByName(info.materialName);
		}
		else
		{
			info.material = EPhysicsMaterial.NONE;
		}
	}
	public static RaycastInfo SilentAimRaycast(Ray ray, float range, int mask, Player ignorePlayer, ref DelayedBullet ebi)
	{
		RaycastInfo raycastInfo = null;
		bool flag = AimbotConfig.enableSilentAim && AimbotUtil.TrySilentRaycastForTarget(ray, range, mask, ignorePlayer, ref ebi, out raycastInfo);
		bool flag2 = flag;
		RaycastInfo raycastInfo2;
		if (flag2)
		{
			raycastInfo2 = raycastInfo;
		}
		else
		{
			raycastInfo2 = DamageTool.raycast(ray, range, mask, ignorePlayer);
		}
		return raycastInfo2;
	}
	public static RaycastInfo MeleeSilentAimRaycast(Ray ray, float range, int mask, Player ignorePlayer)
	{
		RaycastInfo raycastInfo = null;
		// Primary: melee-specific locked target (players/zombies/animals, scanned
		// every frame by SilentAim.UpdateTarget — works in single-player too).
		if (AimbotConfig.enableMeleeSilentAim
			&& SilentAim.TryForgeMeleeHit(ray, range, mask, ignorePlayer, out raycastInfo))
		{
			return raycastInfo;
		}
		// Secondary: classic aimbot target selection
		bool flag = AimbotConfig.enableMeleeSilentAim && AimbotUtil.aimObjective.Target != null && AimbotUtil.DIsTargetValid(AimbotUtil.aimObjective.Target) && AimbotUtil.TryMeleeRaycastForTarget(ray, range, mask, ignorePlayer, AimbotUtil.aimObjective.Target, out raycastInfo);
		bool flag2 = flag;
		RaycastInfo raycastInfo2;
		if (flag2)
		{
			raycastInfo2 = raycastInfo;
		}
		else
		{
			bool extendMeleeRange = MiscConfig.extendMeleeRange;
			bool flag3 = extendMeleeRange;
			if (flag3)
			{
				RaycastInfo raycastInfo3 = DamageTool.raycast(ray, 21f, mask, ignorePlayer);
				bool flag4 = raycastInfo3.collider != null;
				bool flag5 = flag4;
				if (flag5)
				{
					raycastInfo3.point += MathUtil.DirectionTo(raycastInfo3.point, Player.player.look.aim.position) * Mathf.Max(0f, Vector3.Distance(raycastInfo3.collider.transform.position, Player.player.look.aim.position) - 6f);
				}
				raycastInfo2 = raycastInfo3;
			}
			else
			{
				raycastInfo2 = DamageTool.raycast(ray, range, mask, ignorePlayer);
			}
		}
		return raycastInfo2;
	}
	private static void AssignRaycastInfoTarget(ref RaycastInfo info, object targetObject)
	{
		bool flag = targetObject is Player;
		bool flag2 = flag;
		if (flag2)
		{
			info.player = (Player)targetObject;
			bool flag3 = AimbotConfig.enableVehicleHitboxExploit && ((Player)targetObject).movement.getVehicle() != null;
			bool flag4 = flag3;
			if (flag4)
			{
				info.vehicle = ((Player)targetObject).movement.getVehicle();
			}
		}
		else
		{
			bool flag5 = targetObject is Zombie;
			bool flag6 = flag5;
			if (flag6)
			{
				info.zombie = (Zombie)targetObject;
			}
			else
			{
				bool flag7 = targetObject is Animal;
				bool flag8 = flag7;
				if (flag8)
				{
					info.animal = (Animal)targetObject;
				}
				else
				{
					bool flag9 = targetObject is InteractableVehicle;
					bool flag10 = flag9;
					if (flag10)
					{
						info.vehicle = (InteractableVehicle)targetObject;
					}
				}
			}
		}
	}
	private static bool TryMeleeRaycastForTarget(Ray ray, float range, int mask, Player ignorePlayer, object targetObject, out RaycastInfo info)
	{
		info = null;
		bool flag = targetObject == null;
		bool flag2;
		if (flag)
		{
			flag2 = false;
		}
		else
		{
			Transform transform = AimbotUtil.GetAimBoneTransform(targetObject);
			bool flag3 = transform == null;
			if (flag3)
			{
				flag2 = false;
			}
			else
			{
				bool flag4 = !AimbotUtil.DIsTargetValid(targetObject);
				if (flag4)
				{
					flag2 = false;
				}
				else
				{
					bool flag5 = targetObject is Player;
					if (flag5)
					{
						Player player = (Player)targetObject;
						bool flag6 = PlayerPriorityManager.IsFriendOrGroupMatePlayer(player);
						if (flag6)
						{
							return false;
						}
						bool flag7 = AimbotConfig.dontShootPlayersOnSafezone && LevelNodes.isPointInsideSafezone(player.transform.position, out player.movement.isSafeInfo);
						if (flag7)
						{
							return false;
						}
					}
					bool flag8 = AimbotConfig.enableVehicleHitboxExploit && targetObject is Player && ((Player)targetObject).movement.getVehicle() != null;
					float num = (flag8 ? AimbotConfig.vehicleHitboxExploitDistance : 0f);
					Vector3 vector = ((AimbotConfig.enableBacktrack && targetObject is Player && !flag8) ? AimbotUtil.GetBacktrackPosition((Player)targetObject) : transform.position);
					float num2 = Vector3.Distance(vector, ray.origin);
					float num3 = (flag8 ? (range + num) : range);
					bool flag9 = num2 > num3;
					if (flag9)
					{
						flag2 = false;
					}
					else
					{
						// Wall check — exclude ENEMY/VEHICLE so the target's own
						// collider doesn't count as a wall blocking the hit.
						int meleeWallMask = RayMasks.DAMAGE_CLIENT & ~RayMasks.ENEMY & ~RayMasks.VEHICLE;
						bool flag10 = !flag8 && Physics.Linecast(ray.origin, vector, meleeWallMask, QueryTriggerInteraction.Ignore);
						if (flag10)
						{
							flag2 = false;
						}
						else
						{
							Vector3 vector2 = MathUtil.DirectionTo(ray.origin, vector);
							RaycastInfo raycastInfo = DamageTool.raycast(new Ray(ray.origin, vector2), num3, mask, ignorePlayer);
							bool flag11 = !flag8 && raycastInfo != null && raycastInfo.point != Vector3.zero;
							if (flag11)
							{
								float num4 = Vector3.Distance(ray.origin, raycastInfo.point);
								bool flag12 = num4 < num2 - 1f;
								if (flag12)
								{
									return false;
								}
							}
							Vector3 vector3 = vector;
							AimbotUtil.ForgeRaycastInfo(transform.gameObject, targetObject, vector3, ref raycastInfo);
							bool flag13 = flag8;
							if (flag13)
							{
								float num5 = AimbotConfig.vehicleHitboxExploitDistance;
								Vector3 vector4 = MathUtil.DirectionTo(ray.origin, transform.position);
								float num6 = Vector3.Distance(transform.position, ray.origin);
								raycastInfo.point = transform.position - vector4 * (num5 * (num6 / 21f));
								bool flag14 = Physics.Linecast(raycastInfo.point, Player.player.look.aim.transform.position, RayMasks.DAMAGE_CLIENT);
								if (flag14)
								{
									bool flag15 = num6 > num5;
									if (flag15)
									{
										raycastInfo.point = transform.position - vector4 * num5;
									}
									else
									{
										raycastInfo.point = transform.position - vector4 * num6;
									}
								}
							}
							info = raycastInfo;
							flag2 = true;
						}
					}
				}
			}
		}
		return flag2;
	}
	private static bool TrySilentRaycastForTarget(Ray ray, float range, int mask, Player ignorePlayer, ref DelayedBullet ebi, out RaycastInfo info)
	{
		info = null;
		bool flag = ebi.Target == null;
		bool flag2;
		if (flag)
		{
			flag2 = false;
		}
		else
		{
			Transform transform = AimbotUtil.GetAimBoneTransform(ebi.Target);
			bool flag3 = transform == null;
			if (flag3)
			{
				flag2 = false;
			}
			else
			{
				bool flag4 = !AimbotUtil.DIsTargetValid(ebi.Target);
				if (flag4)
				{
					flag2 = false;
				}
				else
				{
					bool flag5 = ebi.Target is Player;
					if (flag5)
					{
						Player player = (Player)ebi.Target;
						bool flag6 = PlayerPriorityManager.IsFriendOrGroupMatePlayer(player);
						if (flag6)
						{
							return false;
						}
						bool flag7 = AimbotConfig.dontShootPlayersOnSafezone && LevelNodes.isPointInsideSafezone(player.transform.position, out player.movement.isSafeInfo);
						if (flag7)
						{
							return false;
						}
					}
					bool flag8 = AimbotConfig.enableVehicleHitboxExploit && ebi.Target is Player && ((Player)ebi.Target).movement.getVehicle() != null;
					float num = (flag8 ? AimbotConfig.vehicleHitboxExploitDistance : ((float)AimbotConfig.distanceToHit));
					Vector3 vector = ((AimbotConfig.enableBacktrack && ebi.Target is Player && !flag8) ? ((ebi.fireTime > 0f) ? AimbotUtil.GetBacktrackPositionAtTime((Player)ebi.Target, ebi.fireTime) : AimbotUtil.GetBacktrackPosition((Player)ebi.Target)) : transform.position);
					bool flag9 = Vector3.Distance(ray.origin, vector) > range;
					if (flag9)
					{
						flag2 = false;
					}
					else
					{
						bool flag11;
						switch (AimbotConfig.silentAimType)
						{
						case SilentAimType.Aim:
						{
							bool flag10 = !flag8 && Physics.Linecast(ray.origin, vector, RayMasks.DAMAGE_CLIENT, QueryTriggerInteraction.Ignore);
							if (flag10)
							{
								flag11 = false;
							}
							else
							{
								Vector3 vector2 = MathUtil.DirectionTo(ray.origin, vector);
								info = DamageTool.raycast(new Ray(ray.origin, vector2), range, mask, ignorePlayer);
								bool flag12 = info != null && info.transform != null;
								if (flag12)
								{
									float num2 = ((info.point != Vector3.zero) ? Vector3.Distance(ray.origin, info.point) : range);
									float num3 = Vector3.Distance(ray.origin, vector);
									bool flag13 = num2 < num3 - num && !flag8;
									if (flag13)
									{
										flag11 = false;
									}
									else
									{
										AimbotUtil.ForgeRaycastInfo(transform.gameObject, ebi.Target, vector, ref info);
										flag11 = true;
									}
								}
								else
								{
									flag11 = false;
								}
							}
							break;
						}
						case SilentAimType.Distance:
						{
							info = (AimbotConfig.straightRaycasting ? DamageTool.raycast(new Ray(ray.origin, ray.direction), range, mask, ignorePlayer) : DamageTool.raycast(new Ray(ray.origin, MathUtil.DirectionTo(ray.origin, vector)), range, mask, ignorePlayer));
							bool flag14 = AimbotConfig.traceFromOrigin && Vector3.Distance(vector, ray.origin) <= num + range && !Physics.Linecast(ray.origin, MathUtil.DirectionTo(vector, ray.origin) * Mathf.Clamp(Vector3.Distance(vector, ray.origin), 0f, num), RayMasks.DAMAGE_CLIENT, QueryTriggerInteraction.Ignore);
							bool flag15 = info != null && Vector3.Distance(vector, (info.point != Vector3.zero) ? info.point : ray.origin) <= num;
							bool flag16 = flag14;
							if (flag16)
							{
								AimbotUtil.ForgeRaycastInfo(transform.gameObject, ebi.Target, vector + MathUtil.DirectionTo(vector, ray.origin) * Mathf.Clamp(Vector3.Distance(vector, ray.origin), 0f, num), ref info);
								flag11 = true;
							}
							else
							{
								bool flag17 = flag15;
								if (flag17)
								{
									AimbotUtil.ForgeRaycastInfo(transform.gameObject, ebi.Target, (info.point != Vector3.zero) ? info.point : ray.origin, ref info);
									flag11 = true;
								}
								else
								{
									flag11 = false;
								}
							}
							break;
						}
						case SilentAimType.Sphere:
						{
							Vector3 vector3 = Vector3.zero;
							bool flag18 = false;
							bool hookSpherePointToBullet = AimbotConfig.hookSpherePointToBullet;
							bool flag19 = hookSpherePointToBullet && ebi.SphereOffset != Vector3.zero;
							if (flag19)
							{
								Vector3 vector4 = vector + ebi.SphereOffset;
								bool flag20 = Vector3.Distance(ray.origin, vector4) < range && !Physics.Linecast(ray.origin, vector4, RayMasks.DAMAGE_CLIENT, QueryTriggerInteraction.Ignore) && ebi.SphereOffset.magnitude <= AimbotConfig.MaxSphereSize + 0.5f;
								bool flag21 = flag20;
								if (flag21)
								{
									vector3 = vector4;
									flag18 = true;
								}
							}
							bool flag22 = !flag18;
							if (flag22)
							{
								Vector3 vector5;
								bool flag23 = vector.TryGetVisiblePoint(ray.origin, out vector5, true) && Vector3.Distance(ray.origin, vector5) < range;
								bool flag24 = flag23;
								if (flag24)
								{
									bool flag25 = hookSpherePointToBullet;
									if (flag25)
									{
										ebi.SphereOffset = vector5 - vector;
									}
									vector3 = vector5;
									flag18 = true;
								}
								else
								{
									vector3 = vector;
									flag18 = Vector3.Distance(ray.origin, vector3) < range;
								}
							}
							bool flag26 = flag18;
							if (flag26)
							{
								info = (AimbotConfig.straightRaycasting ? DamageTool.raycast(new Ray(ray.origin, ray.direction), range, mask, ignorePlayer) : DamageTool.raycast(new Ray(ray.origin, MathUtil.DirectionTo(ray.origin, vector3)), range, mask, ignorePlayer));
								bool flag27 = Vector3.Distance(ray.origin, vector3) < range;
								if (flag27)
								{
									bool flag28 = !flag8 && info != null && info.point != Vector3.zero;
									if (flag28)
									{
										float num4 = Vector3.Distance(ray.origin, info.point);
										float num5 = Vector3.Distance(ray.origin, vector3);
										bool flag29 = num4 < num5 - num - AimbotConfig.MaxSphereSize;
										if (flag29)
										{
											flag11 = false;
											break;
										}
									}
									AimbotUtil.ForgeRaycastInfo(transform.gameObject, ebi.Target, vector3, ref info);
									flag11 = true;
								}
								else
								{
									flag11 = false;
								}
							}
							else
							{
								flag11 = false;
							}
							break;
						}
						default:
							flag11 = false;
							break;
						}
						bool flag30 = flag11 && AimbotConfig.enableVehicleHitboxExploit && info != null;
						if (flag30)
						{
							float num6 = AimbotConfig.vehicleHitboxExploitDistance;
							Vector3 vector6 = MathUtil.DirectionTo(ray.origin, transform.position);
							float num7 = Vector3.Distance(transform.position, ray.origin);
							info.point = transform.position - vector6 * (num6 * (num7 / 21f));
							bool flag31 = Physics.Linecast(info.point, Player.player.look.aim.transform.position, RayMasks.DAMAGE_CLIENT);
							if (flag31)
							{
								bool flag32 = num7 > num6;
								if (flag32)
								{
									info.point = transform.position - vector6 * num6;
								}
								else
								{
									info.point = transform.position - vector6 * num7;
								}
							}
						}
						flag2 = flag11;
					}
				}
			}
		}
		return flag2;
	}
	public static bool DIsTargetValid(object target)
	{
		bool flag = target is Player;
		bool flag2;
		if (flag)
		{
			Player player = (Player)target;
			flag2 = player != null && player.life != null && !player.life.isDead;
		}
		else
		{
			bool flag3 = target is Zombie;
			if (flag3)
			{
				Zombie zombie = (Zombie)target;
				flag2 = zombie != null && !zombie.isDead;
			}
			else
			{
				bool flag4 = target is Animal;
				if (flag4)
				{
					Animal animal = (Animal)target;
					flag2 = animal != null && !animal.isDead;
				}
				else
				{
					bool flag5 = target is InteractableVehicle;
					if (flag5)
					{
						InteractableVehicle interactableVehicle = (InteractableVehicle)target;
						flag2 = interactableVehicle != null && !interactableVehicle.isDead;
					}
					else
					{
						flag2 = target != null;
					}
				}
			}
		}
		return flag2;
	}
	public static Vector3 GetBezierPoint(float t, List<Vector3> points)
	{
		List<Vector3> list = new List<Vector3>(points);
		while (list.Count > 1)
		{
			List<Vector3> list2 = new List<Vector3>();
			for (int i = 0; i < list.Count - 1; i++)
			{
				list2.Add(Vector3.Lerp(list[i], list[i + 1], t));
			}
			list = list2;
		}
		return list[0];
	}
	public static Dictionary<ulong, List<AimbotUtil.BacktrackSnapshot>> DbacktrackHistory = new Dictionary<ulong, List<AimbotUtil.BacktrackSnapshot>>();
	public static AimObjective currentAimObjective = default(AimObjective);
	private static int cachedAimingChance = 100;
	private static int aimingChanceAccumulator = 0;
	private static ReflectedField<float> playerLookPitchField = new ReflectedField<float>(typeof(PlayerLook), "_pitch", BindingFlags.Instance | BindingFlags.NonPublic);
	private static ReflectedField<float> playerLookYawField = new ReflectedField<float>(typeof(PlayerLook), "_yaw", BindingFlags.Instance | BindingFlags.NonPublic);
	public static Dictionary<ulong, List<DelayedBullet>> bulletEventHistoryPerPlayer = new Dictionary<ulong, List<DelayedBullet>>();
	public struct BacktrackSnapshot
	{
		public Vector3 position;
		public float time;
	}
}
