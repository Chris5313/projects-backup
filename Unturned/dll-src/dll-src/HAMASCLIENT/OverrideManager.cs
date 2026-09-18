using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
public static class OverrideManager
{
	public static void ApplyAllOverrides()
	{
		foreach (MethodInfo methodInfo in AttributeRegistry.MethodsByAttribute[typeof(HookMethodAttribute)])
		{
			try
			{
				HookMethodAttribute dk25cIW1nkfhHjlmZqnMOqUJq = (HookMethodAttribute)methodInfo.GetCustomAttribute(typeof(HookMethodAttribute));
				Logger.LogClient("[ov_section] Starting Override -> " + dk25cIW1nkfhHjlmZqnMOqUJq.TargetType.Name + "." + dk25cIW1nkfhHjlmZqnMOqUJq.TargetMethodName);
				OverrideManager.CreateOverrideCore(dk25cIW1nkfhHjlmZqnMOqUJq.TargetType, dk25cIW1nkfhHjlmZqnMOqUJq.TargetMethodName, dk25cIW1nkfhHjlmZqnMOqUJq.SearchFlags, methodInfo.DeclaringType, methodInfo.Name, (methodInfo.IsPublic ? BindingFlags.Public : BindingFlags.NonPublic) | (methodInfo.IsStatic ? BindingFlags.Static : BindingFlags.Instance), dk25cIW1nkfhHjlmZqnMOqUJq.MethodParameterTypes);
			}
			catch (Exception ex)
			{
				Logger.LogClient("[ov_section] failed!!! contact support");
				Logger.LogClient(ex.Message);
				Logger.LogClient(ex.StackTrace);
			}
		}
	}
	public static DetourHook CreateOverride(Type originalClass, string originalMethodName, Type modifiedClass, string hookMethodName, params Type[] memberIndentifiers)
	{
		return OverrideManager.CreateOverrideCore(originalClass, originalMethodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, modifiedClass, hookMethodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, memberIndentifiers);
	}
	public static DetourHook CreateOverrideExplicit(Type originalClass, string originalMethodName, bool isOriginalPublic, bool isOriginalStatic, Type modifiedClass, string hookMethodName, bool isHookPublic, bool isHookStatic, params Type[] memberIndentifiers)
	{
		return OverrideManager.CreateOverrideCore(originalClass, originalMethodName, (isOriginalPublic ? BindingFlags.Public : BindingFlags.NonPublic) | (isOriginalStatic ? BindingFlags.Static : BindingFlags.Instance), modifiedClass, hookMethodName, (isHookPublic ? BindingFlags.Public : BindingFlags.NonPublic) | (isHookStatic ? BindingFlags.Static : BindingFlags.Instance), memberIndentifiers);
	}
	public static DetourHook CreateOverridePublic(Type originalClass, string originalMethodName, bool isOriginalPublic, bool isOriginalStatic, Type modifiedClass, string hookMethodName, params Type[] memberIndentifiers)
	{
		return OverrideManager.CreateOverrideCore(originalClass, originalMethodName, (isOriginalPublic ? BindingFlags.Public : BindingFlags.NonPublic) | (isOriginalStatic ? BindingFlags.Static : BindingFlags.Instance), modifiedClass, hookMethodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, memberIndentifiers);
	}
	public static DetourHook CreateOverrideCore(Type originalClass, string originalMethodName, BindingFlags originalFlags, Type modifiedClass, string hookMethodName, BindingFlags hookFlags, params Type[] memberIndentifiers)
	{
		MemberInfo[] member = originalClass.GetMember(originalMethodName, originalFlags);
		MethodInfo methodInfo = null;
		bool flag = member.Length == 0;
		DetourHook d9Tzj1SkWAoyZl0owsCmbQM3M;
		if (flag)
		{
			UnityEngine.Debug.LogError("[ov_section] member/function could not be found");
			d9Tzj1SkWAoyZl0owsCmbQM3M = null;
		}
		else
		{
			bool flag2 = member.Length == 1;
			if (flag2)
			{
				methodInfo = member[0] as MethodInfo;
			}
			else
			{
				foreach (MemberInfo memberInfo in member)
				{
					bool flag3 = memberIndentifiers.Length == 0 && ((MethodInfo)memberInfo).GetParameters().Length == 0;
					if (flag3)
					{
						methodInfo = memberInfo as MethodInfo;
						break;
					}
					ParameterInfo[] parameters = ((MethodInfo)memberInfo).GetParameters();
					bool flag4 = memberIndentifiers.Length != parameters.Length;
					if (flag4)
					{
						break;
					}
					bool flag5 = true;
					for (int j = 0; j < parameters.Length; j++)
					{
						bool flag6 = parameters[j].ParameterType != memberIndentifiers[j];
						if (flag6)
						{
							flag5 = false;
							break;
						}
					}
					bool flag7 = flag5;
					if (flag7)
					{
						methodInfo = memberInfo as MethodInfo;
						break;
					}
				}
			}
			bool flag8 = methodInfo == null;
			if (flag8)
			{
				UnityEngine.Debug.LogError("original info is not found!");
				d9Tzj1SkWAoyZl0owsCmbQM3M = null;
			}
			else
			{
				MethodInfo method = modifiedClass.GetMethod(hookMethodName, hookFlags);
				bool flag9 = method == null;
				if (flag9)
				{
					UnityEngine.Debug.LogError("hook info is not found!");
					d9Tzj1SkWAoyZl0owsCmbQM3M = null;
				}
				else
				{
					DetourHook d9Tzj1SkWAoyZl0owsCmbQM3M2 = new DetourHook(methodInfo, method);
					d9Tzj1SkWAoyZl0owsCmbQM3M2.Apply();
					OverrideManager.OverridesByToken.Add(method.MetadataToken, d9Tzj1SkWAoyZl0owsCmbQM3M2);
					Logger.LogClient(string.Concat(new string[]
					{
						"hooked function ",
						methodInfo.DeclaringType.Name,
						".",
						methodInfo.Name,
						" successfully"
					}));
					d9Tzj1SkWAoyZl0owsCmbQM3M = d9Tzj1SkWAoyZl0owsCmbQM3M2;
				}
			}
		}
		return d9Tzj1SkWAoyZl0owsCmbQM3M;
	}
	public static T CallOriginalStatic<T>()
	{
		return (T)((object)OverrideManager.CallOriginalCore(new StackTrace(false).GetFrame(1).GetMethod(), null, new object[0]));
	}
	public static T CallOriginalInstance<T>(object instance, params object[] args)
	{
		return (T)((object)OverrideManager.CallOriginalCore(new StackTrace(false).GetFrame(1).GetMethod(), instance, args));
	}
	public static object CallOriginalObject()
	{
		return OverrideManager.CallOriginalCore(new StackTrace(false).GetFrame(1).GetMethod(), null, new object[0]);
	}
	public static object CallOriginal(object instance, params object[] args)
	{
		return OverrideManager.CallOriginalCore(new StackTrace(false).GetFrame(1).GetMethod(), instance, args);
	}
	public static object CallOriginalCore(MethodBase hookMethod, object instance, params object[] args)
	{
		DetourHook d9Tzj1SkWAoyZl0owsCmbQM3M;
		bool flag = OverrideManager.OverridesByToken.TryGetValue(hookMethod.MetadataToken, out d9Tzj1SkWAoyZl0owsCmbQM3M);
		object obj2;
		if (flag)
		{
			d9Tzj1SkWAoyZl0owsCmbQM3M.Revert();
			object obj = d9Tzj1SkWAoyZl0owsCmbQM3M.originalMethod.Invoke(instance, args);
			d9Tzj1SkWAoyZl0owsCmbQM3M.Apply();
			obj2 = obj;
		}
		else
		{
			UnityEngine.Debug.LogError("hook info is not found!");
			obj2 = null;
		}
		return obj2;
	}
	public static void RestoreAllOverrides()
	{
		foreach (DetourHook d9Tzj1SkWAoyZl0owsCmbQM3M in OverrideManager.OverridesByToken.Values)
		{
			d9Tzj1SkWAoyZl0owsCmbQM3M.Revert();
		}
	}
	public static void RestoreOverrideByName(string name)
	{
		foreach (DetourHook d9Tzj1SkWAoyZl0owsCmbQM3M in OverrideManager.OverridesByToken.Values)
		{
			bool flag = d9Tzj1SkWAoyZl0owsCmbQM3M.originalMethod.DeclaringType.Name + "." + d9Tzj1SkWAoyZl0owsCmbQM3M.originalMethod.Name == name;
			if (flag)
			{
				d9Tzj1SkWAoyZl0owsCmbQM3M.Revert();
				break;
			}
		}
	}
	public static Dictionary<int, DetourHook> OverridesByToken = new Dictionary<int, DetourHook>();
}
