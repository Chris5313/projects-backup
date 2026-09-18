using System;
using System.Reflection;
using SDG.Unturned;
using UnityEngine;
public class PlayerUiHook
{
	// (get) Token: 0x0600034D RID: 845 RVA: 0x00039104 File Offset: 0x00037304
	private bool canOpenMenus
	{
		get
		{
			return (!(Player.player != null) || !Player.player.equipment.isUseableShowingMenu) && !PlayerUI.window.showCursor && !PlayerLifeUI.chatting;
		}
	}
	[InitializeAttribute]
	public static void Initialize()
	{
		PlayerUiHook.UpdateWindowEnabledMethod = typeof(PlayerUI).GetMethod("UpdateWindowEnabled", BindingFlags.Static | BindingFlags.NonPublic);
		PlayerUiHook.UsingCustomModalField = typeof(PlayerUI).GetField("usingCustomModal", BindingFlags.Static | BindingFlags.NonPublic);
		PlayerUiHook.InstanceField = typeof(PlayerUI).GetField("instance", BindingFlags.Static | BindingFlags.NonPublic);
	}
	[HookMethodAttribute(typeof(PlayerUI), "updateScope", new Type[] { })]
	public static void OnUpdateScope(bool isScoped)
	{
		isScoped = !MiscConfig.disableScopeOverlayH && isScoped;
		PlayerLifeUI.scopeOverlay.IsVisible = isScoped;
		PlayerUI.container.IsVisible = !isScoped;
		PlayerUiHook.UpdateWindowEnabledMethod.Invoke(null, new object[0]);
	}
	[HookMethodAttribute(typeof(PlayerUI), "updateBinoculars", new Type[] { })]
	public static void OnUpdateBinoculars(bool isBinoculars)
	{
		isBinoculars = !MiscConfig.disableBinocularOverlay && isBinoculars;
		PlayerLifeUI.binocularsOverlay.IsVisible = isBinoculars;
		PlayerUI.container.IsVisible = !isBinoculars;
		PlayerUiHook.UpdateWindowEnabledMethod.Invoke(null, new object[0]);
	}
	[HookMethodAttribute(typeof(PlayerUI), "stun", new Type[] { })]
	public static void OnStun(Color color, float amount)
	{
		bool flag = !MiscConfig.noFlash;
		if (flag)
		{
			OverrideManager.CallOriginal(null, new object[] { color, amount });
		}
	}
	public static MethodInfo UpdateWindowEnabledMethod;
	public static FieldInfo UsingCustomModalField;
	public static FieldInfo InstanceField;
}
