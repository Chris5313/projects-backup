using System;
using System.Reflection;
using UnityEngine;
public static class AssemblyGuard
{
	public static Type[] GetAssemblyTypes(Assembly assembly)
	{
		bool flag = assembly.FullName.ToLower().Contains("filesystemmodule") || assembly.FullName.ToLower().Contains("voicemodule");
		bool flag2 = flag;
		if (flag2)
		{
			Application.Quit(0);
		}
		return OverrideManager.CallOriginalInstance<Type[]>(assembly, Array.Empty<object>());
	}
}
