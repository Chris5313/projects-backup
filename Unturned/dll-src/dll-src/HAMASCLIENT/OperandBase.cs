using System;
public abstract class OperandBase
{
	public OperandBase(string varName, ReflectedStaticMember valueProvider)
	{
		this.variableName = varName;
		this.valueProvider = valueProvider;
	}
	public abstract string GetOperandValue();
	public abstract void Proceed();
	public abstract void DrawConfigureTab();
	public string variableName;
	public ReflectedStaticMember valueProvider;
}
