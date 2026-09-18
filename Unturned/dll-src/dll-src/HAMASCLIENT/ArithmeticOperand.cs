using System;
public class ArithmeticOperand : OperandBase
{
	public ArithmeticOperand(string varName, ReflectedStaticMember valueProvider)
		: base(varName, valueProvider)
	{
	}
	public override void DrawConfigureTab()
	{
		GuiAreaState.Label("Value to operand:");
		this.Value = int.Parse(GuiAreaState.TextField(this.Value.ToString()));
		bool flag = GuiAreaState.Button(string.Format("{0}->{1}", this.variableName, this.Value));
		bool flag2 = flag;
		if (flag2)
		{
			this.Operation = OperandOperation.Set;
		}
		bool flag3 = GuiAreaState.Button(string.Format("{0}.Sum({1})", this.variableName, this.Value));
		bool flag4 = flag3;
		if (flag4)
		{
			this.Operation = OperandOperation.Sum;
		}
		bool flag5 = GuiAreaState.Button(string.Format("{0}.Reduce({1})", this.variableName, this.Value));
		bool flag6 = flag5;
		if (flag6)
		{
			this.Operation = OperandOperation.Reduce;
		}
		bool flag7 = GuiAreaState.Button(string.Format("{0}.Divide({1})", this.variableName, this.Value));
		bool flag8 = flag7;
		if (flag8)
		{
			this.Operation = OperandOperation.Divide;
		}
		bool flag9 = GuiAreaState.Button(string.Format("{0}.Multiple({1})", this.variableName, this.Value));
		bool flag10 = flag9;
		if (flag10)
		{
			this.Operation = OperandOperation.Multiplie;
		}
	}
	public override string GetOperandValue()
	{
		bool flag = this.Operation == OperandOperation.Set;
		bool flag2 = flag;
		string text;
		if (flag2)
		{
			text = string.Format("{0}->{1}", this.variableName, this.Value);
		}
		else
		{
			text = string.Format("{0}.{1}({2})", this.variableName, this.Operation, this.Value);
		}
		return text;
	}
	public override void Proceed()
	{
		bool flag = this.Operation == OperandOperation.Set;
		bool flag2 = flag;
		if (flag2)
		{
			this.valueProvider.SetValue(this.Value);
		}
		else
		{
			bool flag3 = this.Operation == OperandOperation.Sum;
			bool flag4 = flag3;
			if (flag4)
			{
				this.valueProvider.SetValue((int)this.valueProvider.GetValue() + this.Value);
			}
			else
			{
				bool flag5 = this.Operation == OperandOperation.Reduce;
				bool flag6 = flag5;
				if (flag6)
				{
					this.valueProvider.SetValue((int)this.valueProvider.GetValue() - this.Value);
				}
				else
				{
					bool flag7 = this.Operation == OperandOperation.Divide;
					bool flag8 = flag7;
					if (flag8)
					{
						this.valueProvider.SetValue((int)this.valueProvider.GetValue() / this.Value);
					}
					else
					{
						bool flag9 = this.Operation == OperandOperation.Multiplie;
						bool flag10 = flag9;
						if (flag10)
						{
							this.valueProvider.SetValue((int)this.valueProvider.GetValue() * this.Value);
						}
					}
				}
			}
		}
	}
	public OperandOperation Operation = OperandOperation.Set;
	public int Value = 0;
}
