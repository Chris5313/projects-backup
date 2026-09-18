using System;
using System.Reflection;
public class FieldRef
{
	// (get) Token: 0x06000031 RID: 49 RVA: 0x00004170 File Offset: 0x00002370
	// (set) Token: 0x06000032 RID: 50 RVA: 0x00004193 File Offset: 0x00002393
	public object value
	{
		get
		{
			return this.FieldInfo.GetValue(this.BoundInstance);
		}
		set
		{
			this.FieldInfo.SetValue(this.BoundInstance, value);
		}
	}
	public FieldRef(Type _class, string name)
	{
		this.FieldInfo = _class.GetField(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
	}
	public FieldRef(Type _class, string name, BindingFlags reflectionFlags)
	{
		this.FieldInfo = _class.GetField(name, reflectionFlags);
	}
	public FieldRef(string _class, string name)
	{
		this.FieldInfo = ReflectionUtil.FindTypeNoCache(_class).GetField(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
	}
	public FieldRef(string _class, string name, BindingFlags reflectionFlags)
	{
		this.FieldInfo = ReflectionUtil.FindTypeNoCache(_class).GetField(name, reflectionFlags);
	}
	public void CacheValue()
	{
		this.CachedValue = this.value;
	}
	public void BindInstance(object instance)
	{
		this.BoundInstance = instance;
	}
	public void SetValue(object value)
	{
		this.FieldInfo.SetValue(this.BoundInstance, value);
	}
	public void SetValueOn(object instance, object value)
	{
		this.FieldInfo.SetValue(instance, value);
	}
	public object GetValue()
	{
		return this.FieldInfo.GetValue(this.BoundInstance);
	}
	public object GetValueFrom(object instance)
	{
		return this.FieldInfo.GetValue(instance);
	}
	public FieldInfo FieldInfo;
	public object CachedValue;
	public object BoundInstance = null;
}
