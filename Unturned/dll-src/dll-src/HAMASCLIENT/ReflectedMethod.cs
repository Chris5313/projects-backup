using System;
using System.Reflection;
public class ReflectedMethod
{
	public ReflectedMethod(Type _class, string name)
	{
		this.Method = _class.GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
	}
	public ReflectedMethod(Type _class, string name, BindingFlags reflectionFlags)
	{
		this.Method = _class.GetMethod(name, reflectionFlags);
	}
	public ReflectedMethod(string _class, string name)
	{
		this.Method = ReflectionUtil.FindTypeNoCache(_class).GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
	}
	public ReflectedMethod(string _class, string name, BindingFlags reflectionFlags)
	{
		this.Method = ReflectionUtil.FindTypeNoCache(_class).GetMethod(name, reflectionFlags);
	}
	public void SetInstance(object instance)
	{
		this.Instance = instance;
	}
	public void Invoke(params object[] objs)
	{
		this.Method.Invoke(this.Instance, objs);
	}
	public void InvokeOn(object instance, params object[] objs)
	{
		this.Method.Invoke(instance, objs);
	}
	public MethodInfo Method;
	public object Instance = null;
}
