using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using SDG.Unturned;
using UnityEngine;
public static class MiscUpdateHooks
{
	private static void OnReloadStarted(UseableGun gun)
	{
		float num = 1f;
		num += gun.player.skills.mastery(0, 2) * 0.5f;
		bool flag = MiscUpdateHooks.ThirdAttachmentsField.Get(gun).magazineAsset != null;
		if (flag)
		{
			num *= MiscUpdateHooks.ThirdAttachmentsField.Get(gun).magazineAsset.speed;
		}
		float num2 = gun.equippedGunAsset.reload.length * num;
		MiscUpdateHooks.ReloadTimers.Add(gun.player.channel.owner.playerID.steamID.m_SteamID, new ValueTuple<float, float>(Time.realtimeSinceStartup, num2));
		CoroutineHost.StartHostCoroutine(MiscUpdateHooks.ReloadTimerCoroutine(num2, gun.player.channel.owner.playerID.steamID.m_SteamID));
	}
	private static IEnumerator ReloadTimerCoroutine(float length, ulong steamID)
	{
		MiscUpdateHooks._DOZcQBYlTBfUYnRtVsmsQGsPF_d__1 _DOZcQBYlTBfUYnRtVsmsQGsPF_d__ = new MiscUpdateHooks._DOZcQBYlTBfUYnRtVsmsQGsPF_d__1(0);
		_DOZcQBYlTBfUYnRtVsmsQGsPF_d__.length = length;
		_DOZcQBYlTBfUYnRtVsmsQGsPF_d__.steamID = steamID;
		return _DOZcQBYlTBfUYnRtVsmsQGsPF_d__;
	}
	private static void ApplyVisualModifiers()
	{
		bool modifyMoveBehaviour = MiscConfig.modifyMoveBehaviour;
		if (modifyMoveBehaviour)
		{
			bool flag = !ScreenshotManager.IsSpying;
			if (flag)
			{
				float num = (MiscConfig.playerSpinbotDesync ? MiscConfig.playerSpinbotDesyncAmount : 0f);
				float num2 = (MiscConfig.showMoveModifying ? (PlayerInputOverride.PlayerServerYaw - Player.player.look.yaw + num) : num);
				ReflectionUtil.GetFieldValueTyped<CharacterAnimator>(typeof(PlayerAnimator), "thirdAnimator", Player.player.animator).transform.localEulerAngles = new Vector3(90f, num2, 0f);
			}
			else
			{
				ReflectionUtil.GetFieldValueTyped<CharacterAnimator>(typeof(PlayerAnimator), "thirdAnimator", Player.player.animator).transform.localEulerAngles = new Vector3(90f, 0f, 0f);
			}
		}
		bool flag2 = Player.player != null;
		if (flag2)
		{
			MiscUpdateHooks.LastPerspective = Player.player.look.perspective;
			bool flag3;
			if (MiscUpdateHooks.LastPerspective == EPlayerPerspective.THIRD)
			{
				ECameraMode d66xkrBp6Z1GTuSAAF2uDhRmK = MiscConfig.ForcedCameraMode;
				flag3 = MiscConfig.ForcedCameraMode == ECameraMode.VEHICLE && Player.player.movement.getVehicle() == null;
			}
			else
			{
				flag3 = false;
			}
			bool flag4 = flag3;
			if (flag4)
			{
				MiscUpdateHooks.SetActivePerspectiveField.InvokeOn(Player.player.look, new object[] { 0 });
			}
			else
			{
				EPlayerPerspective djTSjYtAiAx9SR0uXvJEpxJak = MiscUpdateHooks.LastPerspective;
				bool flag5 = false;
				if (flag5)
				{
					MiscUpdateHooks.SetActivePerspectiveField.InvokeOn(Player.player.look, new object[] { 1 });
				}
			}
		}
		bool imitNightvision = MiscConfig.imitNightvision;
		if (imitNightvision)
		{
			LevelLighting.vision = MiscConfig.SavedLightingVision;
			PlayerLifeUiHooks.UpdateGrayscaleHook();
		}
		bool flag6 = MiscConfig.imitNightvision || MiscConfig.customDayTime;
		if (flag6)
		{
			LevelLighting.updateLighting();
			LevelLighting.updateLocal();
		}
	}
	private static void ApplyNightVisionAndPerspective()
	{
		bool imitNightvision = MiscConfig.imitNightvision;
		if (imitNightvision)
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
			LevelLighting.vision = MiscConfig.nightVisionType.ToLightingVision();
			LevelLighting.updateLighting();
			LevelLighting.updateLocal();
			PlayerLifeUiHooks.UpdateGrayscaleHook();
		}
		bool flag = Player.player != null && Player.player.look.perspective != MiscUpdateHooks.LastPerspective;
		if (flag)
		{
			MiscUpdateHooks.SetActivePerspectiveField.InvokeOn(Player.player.look, new object[] { MiscUpdateHooks.LastPerspective });
		}
	}
	private static EPlayerPerspective LastPerspective;
	private static ReflectedMethod SetActivePerspectiveField = new ReflectedMethod(typeof(PlayerLook), "setActivePerspective", BindingFlags.Instance | BindingFlags.NonPublic);
	private static ReflectedField<Attachments> ThirdAttachmentsField = new ReflectedField<Attachments>(typeof(UseableGun), "thirdAttachments", BindingFlags.Instance | BindingFlags.NonPublic);
	public static Dictionary<ulong, ValueTuple<float, float>> ReloadTimers = new Dictionary<ulong, ValueTuple<float, float>>();
	private sealed class _DOZcQBYlTBfUYnRtVsmsQGsPF_d__1 : IEnumerator
	{
		private int state;
		private object current;
		public float length;
		public ulong steamID;

		public _DOZcQBYlTBfUYnRtVsmsQGsPF_d__1(int state)
		{
			this.state = state;
		}

		public object Current
		{
			get { return current; }
		}

		public bool MoveNext()
		{
			return false;
		}

		public void Reset()
		{
		}
	}
}
