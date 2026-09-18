using System;
using SDG.Unturned;
using Steamworks;
using UnityEngine;
public class PlayerMovementHook : PlayerMovement
{
	[InitializeAttribute]
	private static void SubscribeEvents()
	{
		Provider.onClientConnected = (Provider.ClientConnected)Delegate.Combine(Provider.onClientConnected, new Provider.ClientConnected(PlayerMovementHook.ResetDrivingFlag));
		Provider.onServerConnected = (Provider.ServerConnected)Delegate.Combine(Provider.onServerConnected, new Provider.ServerConnected(delegate(CSteamID steamid)
		{
			PlayerMovementHook.ResetDrivingFlag();
		}));
	}
	public static void ResetDrivingFlag()
	{
		PlayerMovementHook.IsDrivingVehicle = false;
	}
	[HookMethodAttribute(typeof(PlayerMovement), "setVehicle", new Type[] { })]
	public void HookedSetVehicle(InteractableVehicle newVehicle, byte newSeat, Transform newSeatingTransform, Vector3 newSeatingPosition, byte newSeatingAngle, bool forceUpdate)
	{
		try
		{
			bool flag = base.channel.IsLocalPlayer || Provider.isServer;
			bool flag2 = flag;
			if (flag2)
			{
				bool flag3 = newVehicle != null && newSeat == 0 && !PlayerMovementHook.IsDrivingVehicle;
				bool flag4 = flag3;
				if (flag4)
				{
					PlayerMovementHook.IsDrivingVehicle = true;
					VehicleBehaviour.ApplyVehicleBehaviour(newVehicle);
				}
				else
				{
					bool flag5 = (newVehicle == null || newSeat != 0) && PlayerMovementHook.IsDrivingVehicle;
					bool flag6 = flag5;
					if (flag6)
					{
						PlayerMovementHook.IsDrivingVehicle = false;
						VehicleBehaviour.RestoreVehicleState();
					}
				}
			}
		}
		catch
		{
		}
		OverrideManager.CallOriginal(this, new object[] { newVehicle, newSeat, newSeatingTransform, newSeatingPosition, newSeatingAngle, forceUpdate });
	}
	[HookMethodAttribute(typeof(PlayerMovement), "PlayFootstepAudioClip", new Type[] { })]
	private static void HookedPlayFootstepAudioClip(PlayerMovement movement)
	{
		string text = ((movement.player.stance.stance == EPlayerStance.SPRINT) ? "FootstepRun" : "FootstepWalk");
		OneShotAudioDefinition oneShotAudioDefinition = PlayerMovementHook.AudioDefProvider.Invoke(new object[]
		{
			PlayerMovementHook.MaterialNameField.Get(movement),
			text
		});
		bool flag = oneShotAudioDefinition == null;
		bool flag2 = !flag;
		if (flag2)
		{
			AudioClip randomClip = oneShotAudioDefinition.GetRandomClip();
			bool flag3 = randomClip == null;
			bool flag4 = !flag3;
			if (flag4)
			{
				float num = 1f - movement.player.skills.mastery(1, 0) * 0.75f;
				bool flag5 = movement.player.stance.stance == EPlayerStance.CROUCH;
				bool flag6 = flag5;
				if (flag6)
				{
					num *= 0.5f;
				}
				bool flag7 = Settings.playerStepsCircle && (Settings.seeOwnSteps || !movement.channel.IsLocalPlayer) && movement.player.stance.stance != EPlayerStance.PRONE && movement.player.stance.stance != EPlayerStance.SWIM;
				bool flag8 = flag7;
				if (flag8)
				{
					TracerRenderer.SpawnStepMarker(movement, false);
				}
				num *= 0.125f;
				OneShotAudioParameters oneShotAudioParameters = new OneShotAudioParameters(movement.transform, randomClip);
				oneShotAudioParameters.volume = num * oneShotAudioDefinition.volumeMultiplier;
				oneShotAudioParameters.RandomizePitch(oneShotAudioDefinition.minPitch, oneShotAudioDefinition.maxPitch);
				oneShotAudioParameters.SetLinearRolloff(1f, 32f);
				oneShotAudioParameters.Play();
			}
		}
	}
	[HookMethodAttribute(typeof(PlayerMovement), "PlayLandAudioClip", new Type[] { })]
	private static bool HookedPlayLandAudioClip(PlayerMovement movement)
	{
		bool flag = movement.player.stance.stance == EPlayerStance.PRONE || string.IsNullOrEmpty(PlayerMovementHook.MaterialNameField.Get(movement));
		bool flag2 = flag;
		bool flag3;
		if (flag2)
		{
			flag3 = false;
		}
		else
		{
			OneShotAudioDefinition oneShotAudioDefinition = PlayerMovementHook.AudioDefProvider.Invoke(new object[]
			{
				PlayerMovementHook.MaterialNameField.Get(movement),
				"BipedLand"
			});
			bool flag4 = oneShotAudioDefinition == null;
			bool flag5 = flag4;
			if (flag5)
			{
				flag3 = false;
			}
			else
			{
				AudioClip randomClip = oneShotAudioDefinition.GetRandomClip();
				bool flag6 = randomClip == null;
				bool flag7 = flag6;
				if (flag7)
				{
					flag3 = false;
				}
				else
				{
					float num = 1f - movement.player.skills.mastery(1, 0) * 0.75f;
					bool flag8 = movement.player.stance.stance == EPlayerStance.CROUCH;
					bool flag9 = flag8;
					if (flag9)
					{
						num *= 0.5f;
					}
					bool flag10 = Settings.playerStepsCircle && (Settings.seeOwnSteps || !movement.channel.IsLocalPlayer) && movement.player.stance.stance != EPlayerStance.PRONE && movement.player.stance.stance != EPlayerStance.SWIM;
					bool flag11 = flag10;
					if (flag11)
					{
						TracerRenderer.SpawnStepMarker(movement, true);
					}
					num *= 0.15f;
					OneShotAudioParameters oneShotAudioParameters = new OneShotAudioParameters(movement.transform, randomClip);
					oneShotAudioParameters.volume = num * oneShotAudioDefinition.volumeMultiplier;
					oneShotAudioParameters.RandomizePitch(oneShotAudioDefinition.minPitch, oneShotAudioDefinition.maxPitch);
					oneShotAudioParameters.SetLinearRolloff(1f, 24f);
					oneShotAudioParameters.Play();
					PlayerMovementHook.LastFootstepField.Set(movement, Time.time);
					flag3 = true;
				}
			}
		}
		return flag3;
	}
	private static ReflectedField<string> MaterialNameField = new ReflectedField<string>(typeof(PlayerMovement), "materialName");
	private static ReflectedField<float> LastFootstepField = new ReflectedField<float>(typeof(PlayerMovement), "lastFootstep");
	private static MethodInvoker<OneShotAudioDefinition> AudioDefProvider = new MethodInvoker<OneShotAudioDefinition>("PhysicMaterialCustomData", "GetAudioDef");
	public static bool IsDrivingVehicle = false;
}
