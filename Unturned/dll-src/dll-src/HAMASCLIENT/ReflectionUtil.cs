using System;
using System.Collections.Generic;
using System.Reflection;
using SDG.Unturned;
public static class ReflectionUtil
{
	public static Type FindTypeInUnturned(string type)
	{
		return ReflectionUtil.FindTypeInAssembly(type, typeof(Setup));
	}
	public static Type FindTypeInAssembly(string type, Type assemblyRefType)
	{
		bool flag = ReflectionUtil.typeCache.ContainsKey(type);
		bool flag2 = flag;
		Type type2;
		if (flag2)
		{
			type2 = ReflectionUtil.typeCache[type];
		}
		else
		{
			foreach (Type type3 in Assembly.GetAssembly(assemblyRefType).GetTypes())
			{
				bool flag3 = type3.Name == type;
				bool flag4 = flag3;
				if (flag4)
				{
					ReflectionUtil.typeCache.Add(type3.Name, type3);
					return type3;
				}
			}
			type2 = null;
		}
		return type2;
	}
	public static Type FindTypeNoCache(string type)
	{
		foreach (Type type2 in Assembly.GetAssembly(typeof(Setup)).GetTypes())
		{
			bool flag = type2.Name == type;
			bool flag2 = flag;
			if (flag2)
			{
				return type2;
			}
		}
		return null;
	}
	public static T GetFieldValueTyped<T>(Type script, string varName, object obj)
	{
		return (T)((object)ReflectionUtil.GetFieldValue(script, varName, obj));
	}
	public static object GetFieldValue(Type script, string varName, object obj)
	{
		BindingFlags bindingFlags = ((obj == null) ? BindingFlags.Static : BindingFlags.Instance);
		string text = string.Concat(new string[]
		{
			script.FullName,
			":",
			varName,
			":",
			(obj == null) ? "static" : "instance"
		});
		FieldInfo fieldInfo;
		bool flag = ReflectionUtil.fieldCache.TryGetValue(text, out fieldInfo);
		bool flag2 = flag;
		FieldInfo fieldInfo2;
		if (flag2)
		{
			fieldInfo2 = fieldInfo;
		}
		else
		{
			fieldInfo2 = script.GetField(varName, BindingFlags.NonPublic | bindingFlags);
			ReflectionUtil.fieldCache.Add(text, fieldInfo2);
		}
		bool flag3 = fieldInfo2 == null;
		object obj2;
		if (flag3)
		{
			obj2 = null;
		}
		else
		{
			obj2 = fieldInfo2.GetValue(obj);
		}
		return obj2;
	}
	public static void SetFieldValue(Type script, string varName, object obj, object value)
	{
		try
		{
			BindingFlags bindingFlags = ((obj == null) ? BindingFlags.Static : BindingFlags.Instance);
			string text = string.Concat(new string[]
			{
				script.FullName,
				":",
				varName,
				":",
				(obj == null) ? "static" : "instance"
			});
			FieldInfo fieldInfo;
			bool flag = ReflectionUtil.fieldCache.TryGetValue(text, out fieldInfo);
			bool flag2 = flag;
			FieldInfo fieldInfo2;
			if (flag2)
			{
				fieldInfo2 = fieldInfo;
			}
			else
			{
				fieldInfo2 = script.GetField(varName, BindingFlags.NonPublic | bindingFlags);
				ReflectionUtil.fieldCache.Add(text, fieldInfo2);
			}
			bool flag3 = fieldInfo2 != null;
			if (flag3)
			{
				fieldInfo2.SetValue(obj, value);
			}
		}
		catch (Exception ex)
		{
			Logger.LogClient(string.Concat(new string[] { "Cannot set a ", varName, " value in ", script.Name, " class: ", ex.Message, " ||||| ", ex.StackTrace }));
		}
	}
	public static T InvokeMethodTyped<T>(Type script, string methodName, object obj, params object[] arguments)
	{
		return (T)((object)ReflectionUtil.InvokeMethod(script, methodName, obj, arguments));
	}
	public static object InvokeMethod(Type script, string methodName, object obj, params object[] arguments)
	{
		BindingFlags bindingFlags = ((obj == null) ? BindingFlags.Static : BindingFlags.Instance);
		string text = string.Concat(new string[]
		{
			script.FullName,
			":",
			methodName,
			":",
			(obj == null) ? "static" : "instance"
		});
		MethodInfo methodInfo;
		bool flag = ReflectionUtil.methodCache.TryGetValue(text, out methodInfo);
		bool flag2 = flag;
		MethodInfo methodInfo2;
		if (flag2)
		{
			methodInfo2 = methodInfo;
		}
		else
		{
			methodInfo2 = script.GetMethod(methodName, BindingFlags.NonPublic | bindingFlags);
			ReflectionUtil.methodCache.Add(text, methodInfo2);
		}
		bool flag3 = methodInfo2 == null;
		object obj2;
		if (flag3)
		{
			obj2 = null;
		}
		else
		{
			obj2 = methodInfo2.Invoke(obj, arguments);
		}
		return obj2;
	}
	public static Dictionary<string, Type> typeCache = new Dictionary<string, Type>();
	public static Dictionary<string, FieldInfo> fieldCache = new Dictionary<string, FieldInfo>();
	public static Dictionary<string, MethodInfo> methodCache = new Dictionary<string, MethodInfo>();
}
