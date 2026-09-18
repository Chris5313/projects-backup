using System;
using SDG.Unturned;
using Steamworks;
using UnityEngine;
public class GameLoopDriver : MonoBehaviour
{
	[InitializeAttribute]
	private static void OnGameInitialized()
	{
		Provider.onClientConnected = (Provider.ClientConnected)Delegate.Combine(Provider.onClientConnected, new Provider.ClientConnected(GameLoopDriver.EnsureComponent));
		Provider.onServerConnected = (Provider.ServerConnected)Delegate.Combine(Provider.onServerConnected, new Provider.ServerConnected(delegate(CSteamID steamid)
		{
			GameLoopDriver.EnsureComponent();
		}));
		Provider.onClientDisconnected = (Provider.ClientDisconnected)Delegate.Combine(Provider.onClientDisconnected, new Provider.ClientDisconnected(GameLoopDriver.DestroyComponent));
		bool flag = Bootstrapper.InitFinished && Provider.isConnected;
		bool flag2 = flag;
		if (flag2)
		{
			GameLoopDriver.EnsureComponent();
		}
	}
	private static void EnsureComponent()
	{
		GameLoopDriver.Instance = Bootstrapper.HostGameObject.AddComponent<GameLoopDriver>();
	}
	private static void DestroyComponent()
	{
		UnityEngine.Object.Destroy(GameLoopDriver.Instance);
	}
	private void Update()
	{
		LocalPlayerChams.Update();
			SilentAim.UpdateTarget();
		DTungTungSahurModel.Update();
		this.AccumulatedTime += Time.unscaledDeltaTime;
		bool flag = this.AccumulatedTime > 0.75f;
		bool flag2 = flag;
		if (flag2)
		{
			this.AccumulatedTime -= 0.75f;
			EspManager.DrawAllCategories();
		}
	}
	private void LateUpdate()
	{
		DTungTungSahurModel.LateUpdate();
	}
	private const float UpdateInterval = 0.75f;
	private static GameLoopDriver Instance;
	private float AccumulatedTime = 0f;
}
