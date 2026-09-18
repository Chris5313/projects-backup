using System;
public class GlazierButtonClickGuard
{
	[HookMethodAttribute("GlazierButton_uGUI", "OnUnityButtonClicked", new Type[] { })]
	private void HookedOnUnityButtonClicked()
	{
		bool flag = !MenuState.menuOpened;
		bool flag2 = flag;
		if (flag2)
		{
			OverrideManager.CallOriginal(this, Array.Empty<object>());
		}
	}
	[HookMethodAttribute("GlazierButton_uGUI", "OnUnityButtonRightClicked", new Type[] { })]
	private void HookedOnUnityButtonRightClicked()
	{
		bool flag = !MenuState.menuOpened;
		bool flag2 = flag;
		if (flag2)
		{
			OverrideManager.CallOriginal(this, Array.Empty<object>());
		}
	}
}
