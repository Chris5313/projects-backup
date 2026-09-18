using System;
using System.Reflection;
using SDG.Unturned;
public class ProviderHooks
{
	[HookMethodAttribute(typeof(Provider), "awake", new Type[] { })]
	public void OnProviderAwake()
	{
		OverrideManager.CallOriginal(this, Array.Empty<object>());
		SkinsManager.Initialize();
	}
	[HookMethodAttribute(typeof(Provider), "receiveWorkshopResponse", new Type[] { })]
	internal static void OnReceiveWorkshopResponse(object response)
	{
		MiscConfig.ForcedCameraMode = (ECameraMode)response.GetType().GetField("cameraMode", BindingFlags.Instance | BindingFlags.Public).GetValue(response);
		bool modifyPlayerPerspective = MiscConfig.modifyPlayerPerspective;
		if (modifyPlayerPerspective)
		{
			ReflectionUtil.SetFieldValue(typeof(SteamServerAdvertisement), "_cameraMode", ReflectionUtil.GetFieldValue(typeof(Provider), "_currentServerAdvertisement", null), MiscConfig.playerPerspective);
			response.GetType().GetField("cameraMode", BindingFlags.Instance | BindingFlags.Public).SetValue(response, MiscConfig.playerPerspective);
		}
		OverrideManager.CallOriginal(null, new object[] { response });
	}
	[HookMethodAttribute(typeof(Provider), "removePlayer", new Type[] { })]
	internal static void OnRemovePlayer(byte index)
	{
		bool flag = index < 0 || (int)index >= Provider.clients.Count;
		if (flag)
		{
			UnturnedLog.error("Failed to find player: " + index.ToString());
		}
		else
		{
			try
			{
				PlayerBones.UnregisterPlayer(Provider.clients[(int)index].GetNetId().id, null);
			}
			catch
			{
			}
			OverrideManager.CallOriginal(null, new object[] { index });
		}
	}
}
