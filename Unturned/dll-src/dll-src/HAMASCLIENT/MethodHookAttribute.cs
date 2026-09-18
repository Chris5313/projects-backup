using System;
using System.Reflection;
[AttributeUsage(AttributeTargets.Method)]
public class MethodHookAttribute : Attribute
{
	public MethodHookAttribute(Type t, string methodName, params Type[] memberIndentifiers)
	{
		this.TargetType = t;
		this.BindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
		this.TargetMethodName = methodName;
		this.ParameterTypes = memberIndentifiers;
	}
	public MethodHookAttribute(Type t, string methodName, BindingFlags flags, params Type[] memberIndentifiers)
	{
		this.TargetType = t;
		this.TargetMethodName = methodName;
		this.BindingFlags = flags;
		this.ParameterTypes = memberIndentifiers;
	}
	public MethodHookAttribute(string t, string methodName, params Type[] memberIndentifiers)
	{
		this.TargetType = ReflectionUtil.FindTypeNoCache(t);
		this.BindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
		this.TargetMethodName = methodName;
		this.ParameterTypes = memberIndentifiers;
	}
	public MethodHookAttribute(string t, string methodName, BindingFlags flags, params Type[] memberIndentifiers)
	{
		this.TargetType = ReflectionUtil.FindTypeNoCache(t);
		this.TargetMethodName = methodName;
		this.BindingFlags = flags;
		this.ParameterTypes = memberIndentifiers;
	}
	public Type TargetType;
	public string TargetMethodName;
	public BindingFlags BindingFlags;
	public Type[] ParameterTypes;
}
