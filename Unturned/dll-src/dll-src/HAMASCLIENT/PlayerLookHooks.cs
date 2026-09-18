using System;
using System.Reflection;
using SDG.Unturned;
using UnityEngine;
public class PlayerLookHooks
{
	[InitializeAttribute]
	public static void InitReflection()
	{
		PlayerLookHooks.SweepHitsField = typeof(PlayerLook).GetField("sweepHits", BindingFlags.Static | BindingFlags.NonPublic);
		PlayerLookHooks.FlinchLocalRotationField = typeof(PlayerLook).GetField("flinchLocalRotation", BindingFlags.Instance | BindingFlags.NonPublic);
		PlayerLookHooks.FlinchFromExplosionMethod = typeof(PlayerAnimator).GetMethod("FlinchFromExplosion", BindingFlags.Instance | BindingFlags.NonPublic);
	}
	[HookMethodAttribute(typeof(PlayerLook), "get_isCam", new Type[] { })]
	public bool IsCam()
	{
		return MiscConfig.freeCamera || OverrideManager.CallOriginalInstance<bool>(this, Array.Empty<object>());
	}
	[HookMethodAttribute(typeof(PlayerLook), "FlinchFromDamage", new Type[] { })]
	internal void FlinchFromDamage(byte damageAmount, Vector3 worldDirection)
	{
		bool noFlinch = MiscConfig.noFlinch;
		if (!noFlinch)
		{
			Camera instance = MainCamera.instance;
			bool flag = instance == null;
			bool flag2 = !flag;
			if (flag2)
			{
				Vector3 normalized = Vector3.Cross(Vector3.up, worldDirection).normalized;
				Vector3 vector = instance.transform.InverseTransformDirection(normalized);
				float num = (float)Mathf.Min((int)damageAmount, 25) * 0.5f;
				float num2 = 1f - Player.player.skills.mastery(1, 3) * 0.75f;
				Quaternion quaternion = Quaternion.AngleAxis(num * num2 * MiscConfig.damagePunchMultiplier, vector);
				PlayerLookHooks.FlinchLocalRotationField.SetValue(Player.player.look, (Quaternion)PlayerLookHooks.FlinchLocalRotationField.GetValue(Player.player.look) * quaternion);
			}
		}
	}
	// Recoil adds its kick straight into _pitch/_yaw the moment a shot fires.
	// The camera transform is built from those fields during PlayerLook.Update —
	// BEFORE the aimbot's LateUpdate write erases them — so every shot rendered
	// one kicked frame, then snapped back = the "flash when shooting". While the
	// aimbot actively owns the aim, skip recoil entirely (the kick would be
	// invisible anyway). Israeliclient hooks this same method safely.
	[HookMethodAttribute(typeof(PlayerLook), "recoil", new Type[] { typeof(float), typeof(float), typeof(float), typeof(float) })]
	private void RecoilOverride(float x, float y, float h, float v)
	{
		bool aimbotOwnsAim = AimbotConfig.enableAim
			&& AimbotConfig.enableAimbot
			&& (AimbotConfig.alwaysAim || AimbotConfig.IsMemoryAimbotKeyActive())
			&& AimbotUtil.currentAimObjective.Target != null
			&& !ScreenshotManager.IsSpying;
		if (!aimbotOwnsAim)
		{
			OverrideManager.CallOriginal(this, new object[] { x, y, h, v });
		}
	}
	[HookMethodAttribute(typeof(PlayerLook), "clampPitch", new Type[] { })]
	private void ClampPitch()
	{
		bool flag = ScreenshotManager.IsSpying || !MiscConfig.unclampCameraRotation;
		bool flag2 = flag;
		if (flag2)
		{
			OverrideManager.CallOriginal(this, Array.Empty<object>());
		}
	}
	[HookMethodAttribute(typeof(PlayerLook), "clampYaw", new Type[] { })]
	private void ClampYaw()
	{
		bool flag = ScreenshotManager.IsSpying || !MiscConfig.unclampCameraRotation;
		bool flag2 = flag;
		if (flag2)
		{
			OverrideManager.CallOriginal(this, Array.Empty<object>());
		}
	}
	[HookMethodAttribute(typeof(PlayerLook), "Update", BindingFlags.Instance | BindingFlags.NonPublic, new Type[] { })]
	public static void Update(PlayerLook instance)
	{
		bool flag = !instance.channel.IsLocalPlayer || (!MiscConfig.freeCamera && (!MiscConfig.vehicleMouseMove || !PlayerMovementHook.IsDrivingVehicle));
		bool flag2 = flag;
		if (flag2)
		{
			OverrideManager.CallOriginal(instance, Array.Empty<object>());
		}
	}
	[HookMethodAttribute(typeof(PlayerLook), "sphereCastCamera", new Type[] { })]
	private Vector3 SphereCastCamera(Vector3 origin, Vector3 direction, float length, int layerMask)
	{
		bool dbjv74arVJtUMAqsSN0cWr9w = ScreenshotManager.IsSpying;
		bool flag = dbjv74arVJtUMAqsSN0cWr9w;
		Vector3 vector;
		if (flag)
		{
			vector = OverrideManager.CallOriginalInstance<Vector3>(this, new object[] { origin, direction, length, layerMask });
		}
		else
		{
			int num = Physics.SphereCastNonAlloc(new Ray(origin, direction), 0.39f, PlayerLookHooks.SweepHitsField.GetValue(null) as RaycastHit[], length, MiscConfig.thirdCameraIgnoreObstacles ? 1 : layerMask, QueryTriggerInteraction.Ignore);
			float num2 = length + MiscConfig.thirdCameraDistance;
			for (int i = 0; i < num; i++)
			{
				num2 = Mathf.Min(num2, (PlayerLookHooks.SweepHitsField.GetValue(null) as RaycastHit[])[i].distance);
			}
			vector = origin + direction * num2;
		}
		return vector;
	}
	[HookMethodAttribute(typeof(PlayerLook), "setActivePerspective", new Type[] { })]
	private void SetActivePerspective(EPlayerPerspective newPerspective)
	{
		bool dbjv74arVJtUMAqsSN0cWr9w = ScreenshotManager.IsSpying;
		bool flag = dbjv74arVJtUMAqsSN0cWr9w;
		if (flag)
		{
			bool flag2;
			if (newPerspective == EPlayerPerspective.THIRD)
			{
				ECameraMode d66xkrBp6Z1GTuSAAF2uDhRmK = MiscConfig.ForcedCameraMode;
				flag2 = MiscConfig.ForcedCameraMode == ECameraMode.VEHICLE && Player.player.movement.getVehicle() == null;
			}
			else
			{
				flag2 = false;
			}
			bool flag3 = flag2;
			bool flag4 = flag3;
			if (flag4)
			{
				OverrideManager.CallOriginal(this, new object[] { 0 });
				return;
			}
			bool flag5 = false;
			bool flag6 = flag5;
			if (flag6)
			{
				OverrideManager.CallOriginal(this, new object[] { 1 });
				return;
			}
		}
		OverrideManager.CallOriginal(this, new object[] { newPerspective });
	}
	[HookMethodAttribute(typeof(PlayerLook), "InitializePlayer", BindingFlags.Instance | BindingFlags.NonPublic, new Type[] { })]
	public static void InitializePlayer(Player player)
	{
		OverrideManager.CallOriginal(player, Array.Empty<object>());
		try
		{
			bool flag = player.channel.IsLocalPlayer && MiscConfig.imitNightvision && !ScreenshotManager.IsSpying;
			bool flag2 = flag;
			if (flag2)
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
		}
		catch
		{
		}
	}
	[HookMethodAttribute(typeof(PlayerLook), "FlinchFromExplosion", new Type[] { })]
	internal void FlinchFromExplosion(Vector3 position, float radius, float magnitudeDegrees)
	{
		bool noFlinch = MiscConfig.noFlinch;
		if (!noFlinch)
		{
			Camera instance = MainCamera.instance;
			bool flag = instance == null;
			bool flag2 = !flag;
			if (flag2)
			{
				Vector3 vector = instance.transform.position - position;
				float magnitude = vector.magnitude;
				bool flag3 = magnitude <= 0f || magnitude >= radius;
				bool flag4 = !flag3;
				if (flag4)
				{
					Vector3 vector2 = vector / magnitude;
					Vector3 normalized = Vector3.Cross(Vector3.up, vector2).normalized;
					Vector3 vector3 = instance.transform.InverseTransformDirection(normalized);
					float num = 1f - Player.player.skills.mastery(1, 3) * 0.5f;
					float num2 = 1f - MathfEx.Square(magnitude / radius);
					magnitudeDegrees *= num * num2 * MiscConfig.damagePunchMultiplier;
					Player.player.look.targetExplosionLocalRotation.currentRotation = Player.player.look.targetExplosionLocalRotation.currentRotation * Quaternion.AngleAxis(magnitudeDegrees, vector3);
					PlayerLookHooks.FlinchFromExplosionMethod.Invoke(Player.player.animator, new object[] { vector2, magnitudeDegrees });
				}
			}
		}
	}
	private static FieldInfo SweepHitsField;
	private static FieldInfo FlinchLocalRotationField;
	private static MethodInfo FlinchFromExplosionMethod;
}
