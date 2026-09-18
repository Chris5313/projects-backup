using System;
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class SaveableNameAttribute : Attribute
{
	public SaveableNameAttribute(string saveableName)
	{
		this.SaveableName = saveableName;
	}
	public SaveableNameAttribute()
	{
		this.SaveableName = null;
	}
	public string SaveableName = null;
}
