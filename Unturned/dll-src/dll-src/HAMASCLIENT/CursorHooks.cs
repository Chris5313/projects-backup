using System;
using UnityEngine;
public class CursorHooks
{
	[HookMethodAttribute(typeof(Cursor), "set_lockState", new Type[] { })]
	public static void SetCursorLockState(CursorLockMode lockState)
	{
		bool flag = MenuState.menuOpened && (lockState == CursorLockMode.Confined || lockState == CursorLockMode.Locked);
		bool flag2 = !flag;
		if (flag2)
		{
			OverrideManager.CallOriginal(null, new object[] { lockState });
		}
	}
}
