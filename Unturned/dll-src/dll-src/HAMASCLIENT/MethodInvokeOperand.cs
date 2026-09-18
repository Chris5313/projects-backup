using System;
using System.Reflection;
public class MethodInvokeOperand : OperandBase
{
	public MethodInvokeOperand(string varName, ReflectedStaticMember valueProvider)
		: base(varName, valueProvider)
	{
	}
	public override void DrawConfigureTab()
	{
		throw new NotImplementedException();
	}
	public override string GetOperandValue()
	{
		return this.variableName + ".Invoke()";
	}
	public override void Proceed()
	{
		this.Method.Invoke(null, new object[0]);
	}
	public MethodInfo Method;
}
