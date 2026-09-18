using System;
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field)]
public class ConfigBindAttribute : Attribute
{
	public ConfigBindAttribute(string categoryName, string variableName)
	{
		this.CategoryName = categoryName;
		this.VariableName = variableName;
	}
	public string CategoryName;
	public string VariableName;
}
