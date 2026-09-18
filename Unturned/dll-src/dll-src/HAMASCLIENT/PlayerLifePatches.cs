using System;
using System.Reflection;
using SDG.Unturned;
public class PlayerLifePatches
{
	[HookMethodAttribute(typeof(PlayerLife), "askView", BindingFlags.Instance | BindingFlags.Public, new Type[] { })]
	public static void AskViewPatch(PlayerLife instance, byte amount)
	{
		bool flag = !MiscConfig.noHallucinations;
		bool flag2 = flag;
		if (flag2)
		{
			OverrideManager.CallOriginal(instance, new object[] { amount });
		}
	}
}
