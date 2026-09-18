using System;
using System.Reflection;
[AttributeUsage(AttributeTargets.Method)]
public class HookMethodAttribute : Attribute
{
	public HookMethodAttribute(Type t, string methodName, params Type[] memberIndentifiers)
	{
		this.TargetType = t;
		this.SearchFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
		this.TargetMethodName = methodName;
		this.MethodParameterTypes = memberIndentifiers;
	}
	public HookMethodAttribute(Type t, string methodName, BindingFlags flags, params Type[] memberIndentifiers)
	{
		this.TargetType = t;
		this.TargetMethodName = methodName;
		this.SearchFlags = flags;
		this.MethodParameterTypes = memberIndentifiers;
	}
	public HookMethodAttribute(string t, string methodName, params Type[] memberIndentifiers)
	{
		this.TargetType = ReflectionUtil.FindTypeNoCache(t);
		this.SearchFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
		this.TargetMethodName = methodName;
		this.MethodParameterTypes = memberIndentifiers;
	}
	public HookMethodAttribute(string t, string methodName, BindingFlags flags, params Type[] memberIndentifiers)
	{
		this.TargetType = ReflectionUtil.FindTypeNoCache(t);
		this.TargetMethodName = methodName;
		this.SearchFlags = flags;
		this.MethodParameterTypes = memberIndentifiers;
	}
	public Type TargetType;
	public string TargetMethodName;
	public BindingFlags SearchFlags;
	public Type[] MethodParameterTypes;
}
