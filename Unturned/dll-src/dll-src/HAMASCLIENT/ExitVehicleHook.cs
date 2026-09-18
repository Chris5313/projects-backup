using System;
using SDG.NetTransport;
using SDG.Unturned;
using UnityEngine;
public class ExitVehicleHook
{
	[HookMethodAttribute(typeof(VehicleManager), "exitVehicle", new Type[] { })]
	public static void OnExitVehicle()
	{
		bool flag = Player.player.movement.getVehicle() != null;
		bool flag2 = flag;
		if (flag2)
		{
			ExitVehicleHook.SendExitVehicleRequest.Invoke(ENetReliability.Unreliable, MiscConfig.changeVehicleLeaveVelocity ? (MiscConfig.useForwardVelocity ? (Player.player.movement.getVehicle().transform.forward * (float)MiscConfig.vehilceVelocityForward) : new Vector3((float)MiscConfig.vehilceVelocityX, (float)MiscConfig.vehilceVelocityY, (float)MiscConfig.vehilceVelocityZ)) : Player.player.movement.getVehicle().GetComponent<Rigidbody>().velocity);
		}
	}
	public static readonly ServerStaticMethod<Vector3> SendExitVehicleRequest = ServerStaticMethod<Vector3>.Get(new ServerStaticMethod<Vector3>.ReceiveDelegateWithContext(VehicleManager.ReceiveExitVehicleRequest));
}
