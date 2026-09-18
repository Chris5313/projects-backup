using System;
using SDG.Unturned;
using UnityEngine;
public static class VehicleRecoverPatch
{
	[HookMethodAttribute(typeof(InteractableVehicle), "tellRecov", new Type[] { })]
	public static void OnTellRecovOverride(InteractableVehicle instance, Vector3 newPosition, int newRecov)
	{
		Passenger passenger = instance.passengers[0];
		bool flag = passenger != null;
		bool flag2 = flag;
		Player player2;
		if (flag2)
		{
			SteamPlayer player = passenger.player;
			player2 = ((player == null) ? null : player.player);
		}
		else
		{
			player2 = null;
		}
		bool flag3 = player2 == Player.player && MiscConfig.customVehicleBehaviourBacking && MiscConfig.vehicleNoclipBacking;
		bool flag4 = flag3;
		if (flag4)
		{
			VehicleBehaviour.instance.transform.position = newPosition;
		}
		OverrideManager.CallOriginal(instance, new object[] { newPosition, newRecov });
	}
}
