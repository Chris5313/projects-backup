using System;
using SDG.Unturned;
using Steamworks;
using UnityEngine;
public class InGameHooks : MonoBehaviour
{
	[InitializeAttribute]
	private static void Init()
	{
		Provider.onClientConnected = (Provider.ClientConnected)Delegate.Combine(Provider.onClientConnected, new Provider.ClientConnected(InGameHooks.OnConnect));
		Provider.onServerConnected = (Provider.ServerConnected)Delegate.Combine(Provider.onServerConnected, new Provider.ServerConnected(delegate(CSteamID steamid)
		{
			InGameHooks.OnConnect();
		}));
		Provider.onClientDisconnected = (Provider.ClientDisconnected)Delegate.Combine(Provider.onClientDisconnected, new Provider.ClientDisconnected(InGameHooks.OnDisconnect));
		bool flag = Bootstrapper.InitFinished && Provider.isConnected;
		bool flag2 = flag;
		if (flag2)
		{
			InGameHooks.OnConnect();
		}
	}
	private static void OnConnect()
	{
		InGameHooks.Instance = Bootstrapper.HostGameObject.AddComponent<InGameHooks>();
	}
	private static void OnDisconnect()
	{
		UnityEngine.Object.Destroy(InGameHooks.Instance);
	}
	private void Update()
	{
		bool mouseButtonDown = Input.GetMouseButtonDown(2);
		bool flag = mouseButtonDown;
		if (flag)
		{
			RaycastInfo raycastInfo = DamageTool.raycast(new Ray(Player.player.look.aim.position, Player.player.look.aim.forward), 15.5f, RayMasks.DAMAGE_CLIENT, Player.player);
			bool flag2 = raycastInfo.player != null && PlayerPriorityManager.IsFriendOrGroupMatePlayer(raycastInfo.player);
			bool flag3 = flag2;
			if (flag3)
			{
				// middle-click toggle: unset = back to AUTO (auto-detect resumes)
				PlayerPriorityManager.ResetAutoPlayer(raycastInfo.player);
			}
			else
			{
				bool flag4 = raycastInfo.player != null && !PlayerPriorityManager.IsFriendOrGroupMatePlayer(raycastInfo.player);
				bool flag5 = flag4;
				if (flag5)
				{
					// middle-click: manual friend-mark (survives auto-detect until AUTO reset)
					PlayerPriorityManager.SetManualPlayer(raycastInfo.player, PlayerRelation.Friend);
				}
			}
		}
		bool randomSwapingFace = MiscConfig.randomSwapingFace;
		bool flag6 = randomSwapingFace;
		if (flag6)
		{
			InGameHooks.FaceSwapTimer += Time.deltaTime;
			bool flag7 = InGameHooks.FaceSwapTimer > MiscConfig.faceSwapDelay;
			bool flag8 = flag7;
			if (flag8)
			{
				InGameHooks.FaceSwapTimer = 0f;
				Player.player.clothing.sendSwapFace((byte)UnityEngine.Random.Range(0, (int)(Customization.FACES_FREE + 1)));
			}
		}
		Player.player.animator.scopeSway = Player.player.animator.scopeSway * MiscConfig.swayMultiplier;
	}
	private static InGameHooks Instance;
	private static bool UnusedFlag;
	private static float FaceSwapTimer;
}
