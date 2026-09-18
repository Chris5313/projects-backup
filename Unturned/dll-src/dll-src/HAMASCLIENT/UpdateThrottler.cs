using System;
using SDG.Unturned;
using Steamworks;
using UnityEngine;
public class UpdateThrottler : MonoBehaviour
{
	[InitializeAttribute]
	private static void Initialize()
	{
		Provider.onClientConnected = (Provider.ClientConnected)Delegate.Combine(Provider.onClientConnected, new Provider.ClientConnected(UpdateThrottler.OnConnected));
		Provider.onServerConnected = (Provider.ServerConnected)Delegate.Combine(Provider.onServerConnected, new Provider.ServerConnected(delegate(CSteamID steamid)
		{
			UpdateThrottler.OnConnected();
		}));
		Provider.onClientDisconnected = (Provider.ClientDisconnected)Delegate.Combine(Provider.onClientDisconnected, new Provider.ClientDisconnected(UpdateThrottler.OnDisconnected));
		bool flag = Bootstrapper.InitFinished && Provider.isConnected;
		bool flag2 = flag;
		if (flag2)
		{
			UpdateThrottler.OnConnected();
		}
	}
	private static void OnConnected()
	{
		UpdateThrottler.instance = Bootstrapper.HostGameObject.AddComponent<UpdateThrottler>();
	}
	private static void OnDisconnected()
	{
		UnityEngine.Object.Destroy(UpdateThrottler.instance);
	}
	private void Update()
	{
		AimbotUtil.Update();
	}

	// Aim angles are written in LateUpdate — the LAST write of the frame, after
	// the game's PlayerLook.Update has applied mouse deltas + recoil. Writing
	// during Update raced with the game's own writer (script order is unstable),
	// so two positions fought every frame = camera flashing.
	private void LateUpdate()
	{
		bool flag = AimbotConfig.enableAim && AimbotConfig.enableAimbot && (AimbotConfig.alwaysAim || AimbotConfig.IsMemoryAimbotKeyActive()) && AimbotUtil.currentAimObjective.Target != null && !ScreenshotManager.IsSpying;
		if (flag)
		{
			AimbotUtil.AimAtObjective(false);
		}
	}
	private static UpdateThrottler instance;
	private static bool tickToggle;
}
