using System;
using System.Reflection;
using SDG.Unturned;
using UnityEngine;
public class PlayerLifeUiHooks
{
	[HookMethodAttribute(typeof(PlayerLifeUI), "hasCompassInInventory", new Type[] { })]
	protected static bool HasCompassInInventoryHook()
	{
		return MiscConfig.imitCompassInInventory || OverrideManager.CallOriginalStatic<bool>();
	}
	[HookMethodAttribute(typeof(PlayerLifeUI), "updateGrayscale", new Type[] { })]
	public static void UpdateGrayscaleHook()
	{
		bool flag = !Provider.isConnected;
		bool flag2 = !flag;
		if (flag2)
		{
			bool noGrayscale = MiscConfig.noGrayscale;
			bool flag3 = noGrayscale;
			if (flag3)
			{
				Component component = Player.player.animator.viewmodelCameraTransform.GetComponent("GrayscaleEffect");
				Component component2 = MainCamera.instance.GetComponent("GrayscaleEffect");
				Component component3 = Player.player.look.characterCamera.GetComponent("GrayscaleEffect");
				bool flag4 = component != null;
				if (flag4)
				{
					FieldInfo field = component.GetType().GetField("blend");
					if (field != null)
					{
						field.SetValue(component, 0f);
					}
				}
				bool flag5 = component2 != null;
				bool flag6 = flag5;
				if (flag6)
				{
					FieldInfo field2 = component2.GetType().GetField("blend");
					if (field2 != null)
					{
						field2.SetValue(component2, 0f);
					}
				}
				bool flag7 = component3 != null;
				if (flag7)
				{
					FieldInfo field3 = component3.GetType().GetField("blend");
					if (field3 != null)
					{
						field3.SetValue(component3, 0f);
					}
				}
			}
			else
			{
				try
				{
					OverrideManager.CallOriginalObject();
				}
				catch
				{
				}
			}
		}
	}
	[HookMethodAttribute(typeof(PlayerLifeUI), "onDamaged", new Type[] { })]
	private static void OnDamagedHook(byte damage)
	{
		bool flag = damage > 5 && !MiscConfig.noPain;
		bool flag2 = flag;
		if (flag2)
		{
			PlayerUI.pain(Mathf.Clamp((float)damage / 40f, 0f, 1f));
		}
	}
}
