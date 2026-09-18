using System;
public class EnumOperandEditor : OperandBase
{
	public EnumOperandEditor(string varName, ReflectedStaticMember valueProvider)
		: base(varName, valueProvider)
	{
	}
	public override void DrawConfigureTab()
	{
		Array values = Enum.GetValues(this.valueProvider.GetValue().GetType());
		bool flag = this.selectedMode == CycleDirection.Set && GuiAreaState.Button("Operand to set: " + values.GetValue((int)this.selectedEnumIndex).ToString());
		bool flag2 = flag;
		if (flag2)
		{
			this.selectedEnumIndex = (byte)(((int)(this.selectedEnumIndex + 1) == values.Length) ? 0 : (this.selectedEnumIndex + 1));
		}
		bool flag3 = GuiAreaState.Button(this.variableName + "->" + values.GetValue((int)this.selectedEnumIndex).ToString());
		bool flag4 = flag3;
		if (flag4)
		{
			this.selectedMode = CycleDirection.Set;
		}
		bool flag5 = GuiAreaState.Button(this.variableName + ".Next()");
		bool flag6 = flag5;
		if (flag6)
		{
			this.selectedMode = CycleDirection.Next;
		}
		bool flag7 = GuiAreaState.Button(this.variableName + ".Back()");
		bool flag8 = flag7;
		if (flag8)
		{
			this.selectedMode = CycleDirection.Back;
		}
	}
	public override string GetOperandValue()
	{
		bool flag = this.selectedMode == CycleDirection.Next;
		bool flag2 = flag;
		string text;
		if (flag2)
		{
			text = EnumUtil.NextValue(this.valueProvider.GetValue()).ToString() + ".Next()";
		}
		else
		{
			bool flag3 = this.selectedMode == CycleDirection.Back;
			bool flag4 = flag3;
			if (flag4)
			{
				text = EnumUtil.PreviousValue(this.valueProvider.GetValue()).ToString() + ".Back()";
			}
			else
			{
				Array values = Enum.GetValues(this.valueProvider.GetValue().GetType());
				text = this.variableName + "->" + values.GetValue((int)this.selectedEnumIndex).ToString();
			}
		}
		return text;
	}
	public override void Proceed()
	{
		bool flag = this.selectedMode == CycleDirection.Next;
		bool flag2 = flag;
		if (flag2)
		{
			this.valueProvider.SetValue(EnumUtil.NextValue(this.valueProvider.GetValue()));
		}
		else
		{
			bool flag3 = this.selectedMode == CycleDirection.Back;
			bool flag4 = flag3;
			if (flag4)
			{
				this.valueProvider.SetValue(EnumUtil.PreviousValue(this.valueProvider.GetValue()));
			}
			else
			{
				Array values = Enum.GetValues(this.valueProvider.GetValue().GetType());
				this.valueProvider.SetValue(values.GetValue((int)this.selectedEnumIndex));
			}
		}
	}
	public CycleDirection selectedMode = CycleDirection.Set;
	public byte selectedEnumIndex = 0;
}
