using System;
using System.Reflection;
public class ReflectedStaticMember
{
	public ReflectedStaticMember(FieldInfo fieldInfo)
	{
		this.TargetField = fieldInfo;
		this.TargetProperty = null;
	}
	public ReflectedStaticMember(PropertyInfo propertyInfo)
	{
		this.TargetField = null;
		this.TargetProperty = propertyInfo;
	}
	public void SetValue(object value)
	{
		bool flag = this.TargetField != null;
		bool flag2 = flag;
		if (flag2)
		{
			this.TargetField.SetValue(null, value);
		}
		else
		{
			this.TargetProperty.SetValue(null, value);
		}
	}
	public object GetValue()
	{
		bool flag = this.TargetField != null;
		bool flag2 = flag;
		object obj;
		if (flag2)
		{
			obj = this.TargetField.GetValue(null);
		}
		else
		{
			obj = this.TargetProperty.GetValue(null);
		}
		return obj;
	}
	public FieldInfo TargetField;
	public PropertyInfo TargetProperty;
}
