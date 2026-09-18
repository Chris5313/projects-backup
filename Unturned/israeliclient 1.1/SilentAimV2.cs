using System;
using System.Reflection;
using SDG.Unturned;
using UnityEngine;

namespace gatyware
{
	public static class SilentAimV2
	{
		public static bool HasTarget
		{
			get
			{
				return SilentAim.LockedTarget != null;
			}
		}

		public static object LockedTarget
		{
			get
			{
				return SilentAim.LockedTarget;
			}
		}

		public static Vector3 LockedTargetPos
		{
			get
			{
				return SilentAim.LockedTargetPos;
			}
		}

		private static void Cache()
		{
			if (SilentAimV2._cached)
			{
				return;
			}
			SilentAimV2._cached = true;
			BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.NonPublic;
			SilentAimV2._pendingInputField = typeof(PlayerInput).GetField("clientPendingInput", bindingAttr);
			Type type = typeof(Provider).Assembly.GetType("SDG.Unturned.PlayerInputPacket");
			if (type != null)
			{
				SilentAimV2._pktYaw = type.GetField("yaw", BindingFlags.Instance | BindingFlags.Public);
				SilentAimV2._pktPitch = type.GetField("pitch", BindingFlags.Instance | BindingFlags.Public);
			}
		}

		public static void AfterFixedUpdate(PlayerInput input)
		{
			if (!State.SilentAimOn || State.IsSpying || State.Open)
			{
				return;
			}
			if (State.Spinbot)
			{
				return;
			}
			Player player = input.player;
			if (player == null)
			{
				return;
			}
			if (player.equipment == null || !(player.equipment.useable is UseableGun))
			{
				return;
			}
			if (!Input.GetKey((KeyCode)323))
			{
				return;
			}
			if (SilentAim.LastSpoofedHitPoint == Vector3.zero)
			{
				return;
			}
			SilentAimV2.Cache();
			if (SilentAimV2._pendingInputField == null || SilentAimV2._pktYaw == null)
			{
				return;
			}
			object value = SilentAimV2._pendingInputField.GetValue(input);
			if (value == null)
			{
				return;
			}
			if (value.GetType().Name != "WalkingPlayerInputPacket")
			{
				return;
			}
			Vector3 position = player.look.aim.position;
			Vector3 normalized = (SilentAim.LastSpoofedHitPoint - position).normalized;
			float num = Mathf.Atan2(normalized.x, normalized.z) * 57.29578f;
			float num2 = -Mathf.Asin(normalized.y) * 57.29578f + 90f;
			num2 = Mathf.Clamp(num2, 0f, 180f);
			while (num < 0f)
			{
				num += 360f;
			}
			while (num >= 360f)
			{
				num -= 360f;
			}
			SilentAimV2._pktYaw.SetValue(value, num);
			SilentAimV2._pktPitch.SetValue(value, num2);
		}

		private static FieldInfo _pendingInputField;

		private static FieldInfo _pktYaw;

		private static FieldInfo _pktPitch;

		private static bool _cached;
	}
}
