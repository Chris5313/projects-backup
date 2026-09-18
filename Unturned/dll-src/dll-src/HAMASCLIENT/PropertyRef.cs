using System;
using System.Reflection;
public class PropertyRef<T>
{
	// (get) Token: 0x06000018 RID: 24 RVA: 0x0000371C File Offset: 0x0000191C
	// (set) Token: 0x06000019 RID: 25 RVA: 0x00003744 File Offset: 0x00001944
	public T value
	{
		get
		{
			return (T)((object)this.pi.GetValue(this.instance));
		}
		set
		{
			this.pi.SetValue(this.instance, value);
		}
	}
	public PropertyRef(Type _class, string name)
	{
		this.pi = _class.GetProperty(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
	}
	public PropertyRef(Type _class, string name, BindingFlags reflectionFlags)
	{
		this.pi = _class.GetProperty(name, reflectionFlags);
	}
	public PropertyRef(string _class, string name)
	{
		this.pi = ReflectionUtil.FindTypeNoCache(_class).GetProperty(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
	}
	public PropertyRef(string _class, string name, BindingFlags reflectionFlags)
	{
		this.pi = ReflectionUtil.FindTypeNoCache(_class).GetProperty(name, reflectionFlags);
	}
	public void Instance(object instance)
	{
		this.instance = instance;
	}
	public void Set(object value)
	{
		this.pi.SetValue(this.instance, value);
	}
	public void Set(object instance, object value)
	{
		this.pi.SetValue(instance, value);
	}
	public T Get()
	{
		return (T)((object)this.pi.GetValue(this.instance));
	}
	public T Get(object instance)
	{
		return (T)((object)this.pi.GetValue(instance));
	}
	public static implicit operator T(PropertyRef<T> field)
	{
		return (T)((object)field.pi.GetValue(field.instance));
	}
	public PropertyInfo pi;
	public object instance = null;
}
