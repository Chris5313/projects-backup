using System;
using SDG.Unturned;
using Steamworks;
using UnityEngine;
public class MenuUpdateDriver : MonoBehaviour
{
	[InitializeAttribute]
	private static void SubscribeConnectionEvents()
	{
		Provider.onClientConnected = (Provider.ClientConnected)Delegate.Combine(Provider.onClientConnected, new Provider.ClientConnected(MenuUpdateDriver.OnServerConnected));
		Provider.onServerConnected = (Provider.ServerConnected)Delegate.Combine(Provider.onServerConnected, new Provider.ServerConnected(delegate(CSteamID steamid)
		{
			MenuUpdateDriver.OnServerConnected();
		}));
		Provider.onClientDisconnected = (Provider.ClientDisconnected)Delegate.Combine(Provider.onClientDisconnected, new Provider.ClientDisconnected(MenuUpdateDriver.OnServerDisconnected));
		bool flag = Bootstrapper.InitFinished && Provider.isConnected;
		bool flag2 = flag;
		if (flag2)
		{
			MenuUpdateDriver.OnServerConnected();
		}
	}
	private static void OnServerConnected()
	{
		MenuUpdateDriver.Instance = Bootstrapper.HostGameObject.AddComponent<MenuUpdateDriver>();
	}
	private static void OnServerDisconnected()
	{
		UnityEngine.Object.Destroy(MenuUpdateDriver.Instance);
	}
	private void Update()
	{
		TracerRenderer.RecordWalkingTracers();
	}
	private static MenuUpdateDriver Instance;
}
