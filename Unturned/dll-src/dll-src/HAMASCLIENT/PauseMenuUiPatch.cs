using System;
using SDG.Unturned;
using UnityEngine;
public class PauseMenuUiPatch
{
	[HookMethodAttribute(typeof(PlayerPauseUI), "onClickedExitButton", new Type[] { })]
	private static void OnClickedExitButtonOverride(SleekButtonIconConfirm button)
	{
		bool flag = PlayerPauseUI.shouldExitButtonRespectTimer && Time.realtimeSinceStartup - PlayerPauseUI.lastLeave < Provider.modeConfigData.Gameplay.Timer_Exit && !MiscConfig.ignoreLeaveTimer;
		bool flag2 = !flag;
		if (flag2)
		{
			Provider.RequestDisconnect("clicked exit button from in-game pause menu");
		}
	}
	[HookMethodAttribute(typeof(PlayerPauseUI), "onClickedQuitButton", new Type[] { })]
	private static void OnClickedQuitButtonOverride(SleekButtonIconConfirm button)
	{
		bool flag = PlayerPauseUI.shouldExitButtonRespectTimer && Time.realtimeSinceStartup - PlayerPauseUI.lastLeave < Provider.modeConfigData.Gameplay.Timer_Exit && !MiscConfig.ignoreLeaveTimer;
		bool flag2 = !flag;
		if (flag2)
		{
			Provider.QuitGame("clicked quit from in-game pause menu");
		}
	}
}
