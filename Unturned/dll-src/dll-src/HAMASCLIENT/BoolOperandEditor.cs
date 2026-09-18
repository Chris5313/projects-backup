using System;
public class BoolOperandEditor : OperandBase
{
	public BoolOperandEditor(string varName, ReflectedStaticMember valueProvider)
		: base(varName, valueProvider)
	{
	}
	public override void DrawConfigureTab()
	{
		bool flag = GuiAreaState.Button(this.variableName + ".Reverse()");
		bool flag2 = flag;
		if (flag2)
		{
			this.selectedMode = TriStateOverride.Reverse;
		}
		bool flag3 = GuiAreaState.Button(this.variableName + "->True");
		bool flag4 = flag3;
		if (flag4)
		{
			this.selectedMode = TriStateOverride.True;
		}
		bool flag5 = GuiAreaState.Button(this.variableName + "->False");
		bool flag6 = flag5;
		if (flag6)
		{
			this.selectedMode = TriStateOverride.False;
		}
	}
	public override string GetOperandValue()
	{
		bool flag = this.selectedMode == TriStateOverride.Reverse;
		bool flag2 = flag;
		string text;
		if (flag2)
		{
			text = this.variableName + ".Reverse()";
		}
		else
		{
			text = this.variableName + "->" + this.selectedMode.ToString();
		}
		return text;
	}
	public override void Proceed()
	{
		bool flag = this.selectedMode == TriStateOverride.True;
		bool flag2 = flag;
		if (flag2)
		{
			this.valueProvider.SetValue(true);
		}
		else
		{
			bool flag3 = this.selectedMode == TriStateOverride.False;
			bool flag4 = flag3;
			if (flag4)
			{
				this.valueProvider.SetValue(false);
			}
			else
			{
				this.valueProvider.SetValue(!(bool)this.valueProvider.GetValue());
			}
		}
	}
	public TriStateOverride selectedMode = TriStateOverride.Reverse;
}
