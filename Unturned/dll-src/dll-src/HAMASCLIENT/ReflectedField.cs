using System;
using System.Reflection;
public class ReflectedField<T>
{
	// (get) Token: 0x060001F6 RID: 502 RVA: 0x0001E1AC File Offset: 0x0001C3AC
	// (set) Token: 0x060001F7 RID: 503 RVA: 0x0001E1D4 File Offset: 0x0001C3D4
	public T value
	{
		get
		{
			return (T)((object)this.fi.GetValue(this.instance));
		}
		set
		{
			this.fi.SetValue(this.instance, value);
		}
	}
	public ReflectedField(Type _class, string name)
	{
		this.fi = _class.GetField(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
	}
	public ReflectedField(Type _class, string name, BindingFlags reflectionFlags)
	{
		this.fi = _class.GetField(name, reflectionFlags);
	}
	public ReflectedField(string _class, string name)
	{
		this.fi = ReflectionUtil.FindTypeNoCache(_class).GetField(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
	}
	public ReflectedField(string _class, string name, BindingFlags reflectionFlags)
	{
		this.fi = ReflectionUtil.FindTypeNoCache(_class).GetField(name, reflectionFlags);
	}
	public void RefereshFieldValue()
	{
		this.fldValue = this.Get();
	}
	public void RefereshFieldValue(object instance)
	{
		this.fldValue = this.Get(instance);
	}
	public void Instance(object instance)
	{
		this.instance = instance;
	}
	public void Set(object value)
	{
		this.fi.SetValue(this.instance, value);
	}
	public void Set(object instance, object value)
	{
		this.fi.SetValue(instance, value);
	}
	public T Get()
	{
		return (T)((object)this.fi.GetValue(this.instance));
	}
	public T Get(object instance)
	{
		return (T)((object)this.fi.GetValue(instance));
	}
	public static implicit operator T(ReflectedField<T> field)
	{
		return (T)((object)field.fi.GetValue(field.instance));
	}
	public FieldInfo fi;
	public T fldValue;
	public object instance = null;
}
