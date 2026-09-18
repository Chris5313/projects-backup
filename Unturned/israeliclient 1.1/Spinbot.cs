using System;
using System.Reflection;
using SDG.Unturned;
using UnityEngine;

namespace gatyware
{
	public static class Spinbot
	{
		private static void Cache()
		{
			if (Spinbot._cached)
			{
				return;
			}
			Spinbot._cached = true;
			BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.NonPublic;
			Spinbot._pendingInputField = typeof(PlayerInput).GetField("clientPendingInput", bindingAttr);
			Spinbot._thirdAnimField = typeof(PlayerAnimator).GetField("thirdAnimator", bindingAttr);
			Type type = typeof(Provider).Assembly.GetType("SDG.Unturned.PlayerInputPacket");
			if (type != null)
			{
				Spinbot._pktYaw = type.GetField("yaw", BindingFlags.Instance | BindingFlags.Public);
				Spinbot._pktPitch = type.GetField("pitch", BindingFlags.Instance | BindingFlags.Public);
			}
		}

		public static void AfterFixedUpdate(PlayerInput input)
		{
			if (!State.Spinbot || State.IsSpying)
			{
				return;
			}
			Player player = input.player;
			if (player == null)
			{
				return;
			}
			Spinbot.Cache();
			if (Spinbot._pendingInputField == null)
			{
				return;
			}
			object value = Spinbot._pendingInputField.GetValue(input);
			if (value == null)
			{
				return;
			}
			if (value.GetType().Name != "WalkingPlayerInputPacket")
			{
				return;
			}
			float yaw = player.look.yaw;
			float pitch = player.look.pitch;
			float num = yaw;
			float num2 = pitch;
			Spinbot._tick++;
			switch (State.SpinType)
			{
			case 0:
				switch (Spinbot._tick % 4 + 1)
				{
				case 1:
					num = yaw;
					num2 = 0f;
					break;
				case 2:
					num = yaw - 90f;
					num2 = 0f;
					break;
				case 3:
					num = yaw - 180f;
					num2 = 180f;
					break;
				case 4:
					num = yaw - 270f;
					num2 = 180f;
					break;
				}
				break;
			case 1:
				if (Spinbot._tick % 2 == 0)
				{
					num = yaw;
					num2 = UnityEngine.Random.Range(0f, 90f);
				}
				else
				{
					num = yaw - 180f;
					num2 = UnityEngine.Random.Range(90f, 180f);
				}
				break;
			case 2:
				num = yaw - 90f;
				break;
			case 3:
				num = yaw + 90f;
				break;
			case 4:
				num = yaw - 180f;
				break;
			}
			Spinbot.LastFakeYaw = num;
			if (Spinbot._pktYaw != null)
			{
				Spinbot._pktYaw.SetValue(value, num);
			}
			if (Spinbot._pktPitch != null)
			{
				Spinbot._pktPitch.SetValue(value, num2);
			}
			if (State.SpinShow && Spinbot._thirdAnimField != null)
			{
				object value2 = Spinbot._thirdAnimField.GetValue(player.animator);
				if (value2 != null)
				{
					Component component = value2 as Component;
					Transform transform = (component != null) ? component.transform : null;
					if (transform != null)
					{
						transform.localEulerAngles = new Vector3(90f, num - yaw, 0f);
					}
				}
			}
		}

		public static void ResetVisual(Player lp)
		{
			if (((lp != null) ? lp.animator : null) == null || Spinbot._thirdAnimField == null)
			{
				return;
			}
			object value = Spinbot._thirdAnimField.GetValue(lp.animator);
			if (value != null)
			{
				Component component = value as Component;
				Transform transform = (component != null) ? component.transform : null;
				if (transform != null)
				{
					transform.localEulerAngles = new Vector3(90f, 0f, 0f);
				}
			}
		}

		private static FieldInfo _pendingInputField;

		private static FieldInfo _thirdAnimField;

		private static FieldInfo _pktYaw;

		private static FieldInfo _pktPitch;

		private static bool _cached;

		private static int _tick;

		public static float LastFakeYaw;

		public static string[] TypeNames = new string[]
		{
			"Four Tact",
			"Two Tact",
			"Walk Left",
			"Walk Right",
			"Walk Back"
		};
	}
}
