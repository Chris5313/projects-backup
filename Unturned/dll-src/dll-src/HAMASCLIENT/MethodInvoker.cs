using System;
using System.Reflection;
public class MethodInvoker<T>
{
	public MethodInvoker(Type _class, string name)
	{
		this.mi = _class.GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
	}
	public MethodInvoker(Type _class, string name, BindingFlags reflectionFlags)
	{
		this.mi = _class.GetMethod(name, reflectionFlags);
	}
	public MethodInvoker(string _class, string name)
	{
		this.mi = ReflectionUtil.FindTypeNoCache(_class).GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
	}
	public MethodInvoker(string _class, string name, BindingFlags reflectionFlags)
	{
		this.mi = ReflectionUtil.FindTypeNoCache(_class).GetMethod(name, reflectionFlags);
	}
	public void Instance(object instance)
	{
		this.instance = instance;
	}
	public T Invoke(params object[] objs)
	{
		return (T)((object)this.mi.Invoke(this.instance, objs));
	}
	public T InvokeI(object instance, params object[] objs)
	{
		return (T)((object)this.mi.Invoke(instance, objs));
	}
	public static implicit operator T(MethodInvoker<T> method)
	{
		return method.Invoke(new object[] { method.instance });
	}
	public MethodInfo mi;
	public object instance = null;
}
