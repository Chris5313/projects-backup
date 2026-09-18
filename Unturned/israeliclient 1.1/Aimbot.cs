using System;
using System.Collections.Generic;
using System.Reflection;
using SDG.Unturned;
using UnityEngine;

namespace gatyware
{
	public static class Aimbot
	{
		private static void Cache()
		{
			if (Aimbot._cached)
			{
				return;
			}
			Aimbot._cached = true;
			BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.NonPublic;
			Aimbot._yawField = typeof(PlayerLook).GetField("_yaw", bindingAttr);
			Aimbot._pitchField = typeof(PlayerLook).GetField("_pitch", bindingAttr);
		}

		public static void Tick()
		{
			if (!State.AimbotOn || State.IsSpying)
			{
				Aimbot._lockedTarget = null;
				return;
			}
			Player player = Player.player;
			if (player == null || player.life.isDead)
			{
				Aimbot._lockedTarget = null;
				return;
			}
			if (player.movement.getVehicle() != null)
			{
				Aimbot._lockedTarget = null;
				return;
			}
			if (player.equipment == null || player.equipment.asset == null || !player.equipment.isEquipped)
			{
				Aimbot._lockedTarget = null;
				return;
			}
			if (!(player.equipment.asset is ItemGunAsset) && !(player.equipment.asset is ItemMeleeAsset))
			{
				Aimbot._lockedTarget = null;
				return;
			}
			if (State.AimbotKey == KeyCode.None || !Input.GetKey(State.AimbotKey))
			{
				Aimbot._lockedTarget = null;
				return;
			}
			Aimbot.Cache();
			if (Aimbot._yawField == null || Aimbot._pitchField == null)
			{
				return;
			}
			Vector3 vector;
			if (!Aimbot.GetTargetPos(player, out vector))
			{
				Aimbot._lockedTarget = null;
				return;
			}
			Vector3 vector2 = player.transform.position + Vector3.up * player.look.heightLook;
			Vector3 vector3 = vector - vector2;
			if (vector3.sqrMagnitude < 0.25f)
			{
				return;
			}
			float num = Mathf.Atan2(vector3.x, vector3.z) * 57.29578f;
			float num2 = Mathf.Sqrt(vector3.x * vector3.x + vector3.z * vector3.z);
			float num3 = Mathf.Atan2(vector3.y, num2) * 57.29578f;
			num3 += Aimbot.GetBulletDropAngle(player, num2, vector3.y);
			float num4 = Mathf.Clamp(90f - num3, 0f, 180f);
			float num5 = (float)Aimbot._yawField.GetValue(player.look);
			float num6 = (float)Aimbot._pitchField.GetValue(player.look);
			while (num - num5 > 180f)
			{
				num -= 360f;
			}
			while (num - num5 < -180f)
			{
				num += 360f;
			}
			float num8;
			float num9;
			if (State.AimbotSmooth)
			{
				float num7 = Mathf.Clamp01(Mathf.Lerp(25f, 2f, State.AimbotSmoothness) * Time.deltaTime);
				num8 = num5 + (num - num5) * num7;
				num9 = num6 + (num4 - num6) * num7;
			}
			else
			{
				num8 = num;
				num9 = num4;
			}
			Aimbot._yawField.SetValue(player.look, num8);
			Aimbot._pitchField.SetValue(player.look, num9);
		}

		private static void CacheTrigger()
		{
			if (Aimbot._triggerCached)
			{
				return;
			}
			Aimbot._triggerCached = true;
			BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.NonPublic;
			Aimbot._primaryPressedField = typeof(PlayerEquipment).GetField("localWasPrimaryPressedBetweenSimulationFrames", bindingAttr);
			Aimbot._primaryHeldField = typeof(PlayerEquipment).GetField("localWasPrimaryHeldLastFrame", bindingAttr);
		}

		public static void TriggerTick()
		{
			if (!State.TriggerBot || State.IsSpying || State.Open)
			{
				return;
			}
			Player player = Player.player;
			if (player == null || player.life.isDead)
			{
				return;
			}
			if (player.movement.getVehicle() != null)
			{
				return;
			}
			if (player.equipment == null || player.equipment.asset == null || !player.equipment.isEquipped)
			{
				return;
			}
			if (!(player.equipment.asset is ItemGunAsset))
			{
				return;
			}
			if (player.equipment.isBusy && !State.TriggerBotHold)
			{
				return;
			}
			if (State.TriggerBotKey != KeyCode.None && !Input.GetKey(State.TriggerBotKey))
			{
				return;
			}
			if (!State.TriggerBotHold && Time.realtimeSinceStartup - Aimbot._lastTriggerShot < State.TriggerBotDelay)
			{
				return;
			}
			Aimbot.CacheTrigger();
			if (Aimbot._primaryPressedField == null)
			{
				return;
			}
			bool flag = false;
			if (State.SilentAimOn && (SilentAim.LockedTarget != null || SilentAimV2.HasTarget))
			{
				flag = true;
			}
			else
			{
				Transform aim = player.look.aim;
				float range = ((ItemGunAsset)player.equipment.asset).range;
				int num = RayMasks.DAMAGE_CLIENT | 16777216;
				RaycastHit raycastHit = default;
				if (Physics.Raycast(new Ray(aim.position, aim.forward), out raycastHit, range, num, (QueryTriggerInteraction)1))
				{
					string tag = raycastHit.transform.tag;
					if (tag == "Player")
					{
						Player player2 = DamageTool.getPlayer(raycastHit.transform);
						if (player2 != null && !player2.channel.IsLocalPlayer && !player2.life.isDead && !PlayerRelation.IsFriend(player2))
						{
							flag = true;
						}
					}
					else if (tag == "Agent" || tag == "Enemy")
					{
						Zombie zombie = DamageTool.getZombie(raycastHit.transform);
						if (zombie != null && !zombie.isDead)
						{
							flag = true;
						}
					}
				}
			}
			if (flag)
			{
				Aimbot._primaryPressedField.SetValue(player.equipment, true);
				if (Aimbot._primaryHeldField != null)
				{
					Aimbot._primaryHeldField.SetValue(player.equipment, true);
				}
				Aimbot._lastTriggerShot = Time.realtimeSinceStartup;
				return;
			}
			if (State.TriggerBotHold && Aimbot._primaryHeldField != null)
			{
				Aimbot._primaryHeldField.SetValue(player.equipment, false);
			}
		}

		private static bool GetTargetPos(Player lp, out Vector3 pos)
		{
			pos = Vector3.zero;
			Vector3 position = lp.look.aim.position;
			Vector2 screenCenter;
			screenCenter = new Vector2((float)Screen.width * 0.5f, (float)Screen.height * 0.5f);
			if (Aimbot._lockedTarget != null)
			{
				Vector3 vector;
				if (Aimbot.IsTargetValid(Aimbot._lockedTarget, position, screenCenter, out vector))
				{
					pos = vector;
					return true;
				}
				Aimbot._lockedTarget = null;
			}
			float num = float.MaxValue;
			object obj = null;
			if (State.AimbotTargetPlayers)
			{
				for (int i = 0; i < Provider.clients.Count; i++)
				{
					SteamPlayer steamPlayer = Provider.clients[i];
					if (steamPlayer != null && !(steamPlayer.player == null) && !steamPlayer.player.channel.IsLocalPlayer && !steamPlayer.player.life.isDead && (!State.AimbotFriendly || !PlayerRelation.IsFriend(steamPlayer)))
					{
						Vector3 playerLimb = Aimbot.GetPlayerLimb(steamPlayer.player);
						float num2 = Aimbot.EvalTarget(playerLimb, position, screenCenter);
						if (num2 < num)
						{
							num = num2;
							pos = playerLimb;
							obj = steamPlayer.player;
						}
					}
				}
			}
			if (State.AimbotTargetZombies)
			{
				for (int j = 0; j < ZombieManager.regions.Length; j++)
				{
					List<Zombie> zombies = ZombieManager.regions[j].zombies;
					for (int k = 0; k < zombies.Count; k++)
					{
						Zombie zombie = zombies[k];
						if (!(zombie == null) && !zombie.isDead)
						{
							Vector3 zombieHead = Aimbot.GetZombieHead(zombie);
							float num3 = Aimbot.EvalTarget(zombieHead, position, screenCenter);
							if (num3 < num)
							{
								num = num3;
								pos = zombieHead;
								obj = zombie;
							}
						}
					}
				}
			}
			if (obj != null)
			{
				Aimbot._lockedTarget = obj;
			}
			return obj != null;
		}

		private static bool IsTargetValid(object target, Vector3 camPos, Vector2 screenCenter, out Vector3 pos)
		{
			pos = Vector3.zero;
			if (target is Player)
			{
				Player player = (Player)target;
				if (player.life.isDead)
				{
					return false;
				}
				pos = Aimbot.GetPlayerLimb(player);
			}
			else
			{
				if (!(target is Zombie))
				{
					return false;
				}
				Zombie zombie = (Zombie)target;
				if (zombie.isDead)
				{
					return false;
				}
				pos = Aimbot.GetZombieHead(zombie);
			}
			if (Vector3.Distance(camPos, pos) > State.AimbotMaxDist)
			{
				return false;
			}
			if (State.AimbotFovRestrict)
			{
				Vector3 vector = MainCamera.instance.WorldToScreenPoint(pos);
				if (vector.z <= 0f)
				{
					return false;
				}
				if (Vector2.Distance(screenCenter, new Vector2(vector.x, (float)Screen.height - vector.y)) > State.AimbotFov * 1.5f)
				{
					return false;
				}
			}
			return !State.AimbotVisCheck || !Physics.Linecast(camPos, pos, RayMasks.DAMAGE_CLIENT, (QueryTriggerInteraction)1);
		}

		private static float EvalTarget(Vector3 pos, Vector3 camPos, Vector2 screenCenter)
		{
			if (Vector3.Distance(camPos, pos) > State.AimbotMaxDist)
			{
				return float.MaxValue;
			}
			Vector3 vector = MainCamera.instance.WorldToScreenPoint(pos);
			if (vector.z <= 0f)
			{
				return float.MaxValue;
			}
			float num = Vector2.Distance(screenCenter, new Vector2(vector.x, (float)Screen.height - vector.y));
			if (State.AimbotFovRestrict && num > State.AimbotFov)
			{
				return float.MaxValue;
			}
			if (State.AimbotVisCheck && Physics.Linecast(camPos, pos, RayMasks.DAMAGE_CLIENT, (QueryTriggerInteraction)1))
			{
				return float.MaxValue;
			}
			return num;
		}

		private static Vector3 GetPlayerLimb(Player p)
		{
			string b;
			switch (State.AimbotLimb)
			{
			case 0:
				b = "Skull";
				break;
			case 1:
				b = "Spine";
				break;
			case 2:
				return Aimbot.GetBestVisiblePlayerLimb(p);
			default:
				b = "Skull";
				break;
			}
			Collider[] componentsInChildren = p.GetComponentsInChildren<Collider>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				if (componentsInChildren[i].name == b)
				{
					return componentsInChildren[i].bounds.center;
				}
			}
			return p.look.aim.position;
		}

		private static Vector3 GetBestVisiblePlayerLimb(Player p)
		{
			Vector3 position = MainCamera.instance.transform.position;
			Collider[] componentsInChildren = p.GetComponentsInChildren<Collider>();
			for (int i = 0; i < Aimbot._limbPri.Length; i++)
			{
				int j = 0;
				while (j < componentsInChildren.Length)
				{
					if (componentsInChildren[j].name == Aimbot._limbPri[i])
					{
						Vector3 center = componentsInChildren[j].bounds.center;
						if (!Physics.Linecast(position, center, RayMasks.DAMAGE_CLIENT, (QueryTriggerInteraction)1))
						{
							return center;
						}
						break;
					}
					else
					{
						j++;
					}
				}
			}
			return p.look.aim.position;
		}

		private static Vector3 GetZombieHead(Zombie z)
		{
			Transform transform = Aimbot.FindChildRecursive(z.transform, "Skull");
			if (transform != null)
			{
				return transform.position;
			}
			Transform transform2 = Aimbot.FindChildRecursive(z.transform, "Spine");
			if (transform2 != null)
			{
				return transform2.position + Vector3.up * 0.3f;
			}
			return z.transform.position + Vector3.up * 1.75f;
		}

		private static Transform FindChildRecursive(Transform parent, string name)
		{
			for (int i = 0; i < parent.childCount; i++)
			{
				Transform child = parent.GetChild(i);
				if (child.name == name)
				{
					return child;
				}
				Transform transform = Aimbot.FindChildRecursive(child, name);
				if (transform != null)
				{
					return transform;
				}
			}
			return null;
		}


		private static float GetBulletDropAngle(Player lp, float horizontalDist, float verticalDist)
		{
			if (!Aimbot.IsBallistics())
			{
				return 0f;
			}
			ItemGunAsset gunAsset = lp.equipment.asset as ItemGunAsset;
			if (gunAsset == null)
			{
				return 0f;
			}
			float velocity = gunAsset.ballisticForce;
			if (velocity < 1f)
			{
				return 0f;
			}
			float gravity = Mathf.Abs(Physics.gravity.y);
			if (gravity < 0.01f)
			{
				return 0f;
			}
			float time = horizontalDist / velocity;
			float drop = 0.5f * gravity * time * time;
			float compensationAngle = Mathf.Atan2(drop, horizontalDist) * 57.29578f;
			return compensationAngle;
		}

		private static bool IsBallistics()
		{
			try
			{
				if (Provider.modeConfigData != null && Provider.modeConfigData.Gameplay != null)
				{
					return Provider.modeConfigData.Gameplay.Ballistics;
				}
			}
			catch
			{
			}
			return false;
		}

		private static FieldInfo _yawField;

		private static FieldInfo _pitchField;

		private static bool _cached;

		private static object _lockedTarget;

		private static bool _hadTarget;

		private static float _lastTriggerShot;

		private static FieldInfo _primaryPressedField;

		private static FieldInfo _primaryHeldField;

		private static bool _triggerCached;

		private static readonly string[] _limbPri = new string[]
		{
			"Skull",
			"Spine",
			"Left_Hand",
			"Right_Hand"
		};
	}
}
