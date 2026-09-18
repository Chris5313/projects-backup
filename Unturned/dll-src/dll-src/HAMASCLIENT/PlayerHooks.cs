using System;
using System.Reflection;
using SDG.Unturned;
using UnityEngine;
public class PlayerHooks : Player
{
	[HookMethodAttribute(typeof(Player), "ReceiveStat", new Type[] { })]
	public void ReceiveStatOverride(EPlayerStat stat)
	{
		bool flag = MiscConfig.chatOnKill && stat == EPlayerStat.KILLS_PLAYERS;
		if (flag)
		{
			ChatManager.sendChat(EChatMode.GLOBAL, MiscConfig.killText);
		}
		bool killSoundFlag = MiscConfig.killSound && stat == EPlayerStat.KILLS_PLAYERS;
		if (killSoundFlag)
		{
			try
			{
				string audioName = MiscConfig.killAudioName;
				AudioClip killClip = null;
				if (audioName == "Random")
				{
					string randomName = GuiStyles.SoundNames[UnityEngine.Random.Range(0, GuiStyles.SoundNames.Length - 1)];
					if (GuiStyles.LoadedAssetCache.ContainsKey(randomName))
						killClip = GuiStyles.LoadedAssetCache[randomName].Asset as AudioClip;
				}
				else if (GuiStyles.LoadedAssetCache.ContainsKey(audioName))
				{
					killClip = GuiStyles.LoadedAssetCache[audioName].Asset as AudioClip;
				}
				if (killClip != null)
				{
					OneShotAudioParameters osp = new OneShotAudioParameters(Player.player.transform.position, killClip);
					osp.minDistance = 0f;
					osp.maxDistance = 15f;
					osp.Play();
				}
				else
				{
					Logger.LogClient("[KillSound] clip is null for: " + audioName);
				}
			}
			catch (Exception ex)
			{
				Logger.LogClient("[KillSound] error: " + ex.Message);
			}
		}
		OverrideManager.CallOriginal(this, new object[] { stat });
	}
	[HookMethodAttribute(typeof(Player), "ReceiveTakeScreenshot", BindingFlags.Instance | BindingFlags.Public, new Type[] { })]
	public static void ReceiveTakeScreenshotOverride(Player player)
	{
		ScreenshotManager.TakeScreenshot(player);
	}
}
