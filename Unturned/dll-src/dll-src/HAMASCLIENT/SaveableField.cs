using System;
using System.Reflection;
public struct SaveableField
{
	public SaveableField(SaveValueType varType, Type originalType, string originalVarName, string varName, bool isProperty)
	{
		this.ValueType = varType;
		this.DeclaringType = originalType;
		this.OriginalMemberName = originalVarName;
		this.SaveName = varName;
		this.IsProperty = isProperty;
	}
	public object GetValueObject()
	{
		bool drxWLpNmkNm6CNZBgQJwRBebB = this.IsProperty;
		bool flag = drxWLpNmkNm6CNZBgQJwRBebB;
		object obj;
		if (flag)
		{
			obj = this.DeclaringType.GetProperty(this.OriginalMemberName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
		}
		else
		{
			obj = this.DeclaringType.GetField(this.OriginalMemberName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
		}
		return obj;
	}
	public T GetValueTyped<T>()
	{
		bool drxWLpNmkNm6CNZBgQJwRBebB = this.IsProperty;
		bool flag = drxWLpNmkNm6CNZBgQJwRBebB;
		T t;
		if (flag)
		{
			t = (T)((object)this.DeclaringType.GetProperty(this.OriginalMemberName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null));
		}
		else
		{
			t = (T)((object)this.DeclaringType.GetField(this.OriginalMemberName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null));
		}
		return t;
	}
	public Type GetValueType()
	{
		bool drxWLpNmkNm6CNZBgQJwRBebB = this.IsProperty;
		bool flag = drxWLpNmkNm6CNZBgQJwRBebB;
		Type type;
		if (flag)
		{
			type = this.DeclaringType.GetProperty(this.OriginalMemberName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).PropertyType;
		}
		else
		{
			type = this.DeclaringType.GetField(this.OriginalMemberName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).FieldType;
		}
		return type;
	}
	public void SetValue(object o)
	{
		try
		{
			bool drxWLpNmkNm6CNZBgQJwRBebB = this.IsProperty;
			bool flag = drxWLpNmkNm6CNZBgQJwRBebB;
			if (flag)
			{
				this.DeclaringType.GetProperty(this.OriginalMemberName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).SetValue(null, o);
			}
			else
			{
				this.DeclaringType.GetField(this.OriginalMemberName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).SetValue(null, o);
			}
		}
		catch
		{
		}
	}
	public SaveValueType ValueType;
	public Type DeclaringType;
	public string SaveName;
	public string OriginalMemberName;
	public bool IsProperty;
}
