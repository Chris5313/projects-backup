using System;
using System.Reflection;
using SDG.Unturned;
public class LogPatches
{
	[HookMethodAttribute(typeof(UnturnedLog), "info", new Type[] { typeof(string) })]
	public static void InfoPatch(string message)
	{
		bool flag = LogPatches.InsideLogField;
		bool flag2 = !flag;
		if (flag2)
		{
			try
			{
				LogPatches.InsideLogField.value = true;
				Logs.printLine(message);
				Logger.LogUnturned(message);
			}
			finally
			{
				LogPatches.InsideLogField.value = false;
			}
		}
	}
	[HookMethodAttribute(typeof(UnturnedLog), "warn", new Type[] { typeof(string) })]
	public static void WarnPatch(string message)
	{
		bool flag = LogPatches.InsideLogField;
		bool flag2 = !flag;
		if (flag2)
		{
			try
			{
				LogPatches.InsideLogField.value = true;
				Logs.printLine(message);
				Logger.LogUnturned(message);
			}
			finally
			{
				LogPatches.InsideLogField.value = false;
			}
		}
	}
	[HookMethodAttribute(typeof(UnturnedLog), "error", new Type[] { typeof(string) })]
	public static void ErrorPatch(string message)
	{
		bool flag = LogPatches.InsideLogField;
		bool flag2 = !flag;
		if (flag2)
		{
			try
			{
				LogPatches.InsideLogField.value = true;
				Logs.printLine(message);
				CommandWindow.LogError(message);
				Logger.LogUnturned(message);
			}
			finally
			{
				LogPatches.InsideLogField.value = false;
			}
		}
	}
	public static ReflectedField<bool> InsideLogField = new ReflectedField<bool>(typeof(UnturnedLog), "insideLog", BindingFlags.Static | BindingFlags.NonPublic);
}
