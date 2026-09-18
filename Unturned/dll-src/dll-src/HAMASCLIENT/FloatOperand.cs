using System;
public class FloatOperand : OperandBase
{
	public FloatOperand(string varName, ReflectedStaticMember valueProvider)
		: base(varName, valueProvider)
	{
	}
	public override void DrawConfigureTab()
	{
		GuiAreaState.Label("Value to operand:");
		this.operandValue = float.Parse(GuiAreaState.TextField(this.operandValue.ToString()));
		bool flag = GuiAreaState.Button(string.Format("{0}->{1}", this.variableName, this.operandValue));
		bool flag2 = flag;
		if (flag2)
		{
			this.operation = FloatOperandOperation.Set;
		}
		bool flag3 = GuiAreaState.Button(string.Format("{0}.Sum({1})", this.variableName, this.operandValue));
		bool flag4 = flag3;
		if (flag4)
		{
			this.operation = FloatOperandOperation.Sum;
		}
		bool flag5 = GuiAreaState.Button(string.Format("{0}.Reduce({1})", this.variableName, this.operandValue));
		bool flag6 = flag5;
		if (flag6)
		{
			this.operation = FloatOperandOperation.Reduce;
		}
		bool flag7 = GuiAreaState.Button(string.Format("{0}.Divide({1})", this.variableName, this.operandValue));
		bool flag8 = flag7;
		if (flag8)
		{
			this.operation = FloatOperandOperation.Divide;
		}
		bool flag9 = GuiAreaState.Button(string.Format("{0}.Multiple({1})", this.variableName, this.operandValue));
		bool flag10 = flag9;
		if (flag10)
		{
			this.operation = FloatOperandOperation.Multiplie;
		}
	}
	public override string GetOperandValue()
	{
		bool flag = this.operation == FloatOperandOperation.Set;
		bool flag2 = flag;
		string text;
		if (flag2)
		{
			text = string.Format("{0}->{1}", this.variableName, this.operandValue);
		}
		else
		{
			text = string.Format("{0}.{1}({2})", this.variableName, this.operation, this.operandValue);
		}
		return text;
	}
	public override void Proceed()
	{
		bool flag = this.operation == FloatOperandOperation.Set;
		bool flag2 = flag;
		if (flag2)
		{
			this.valueProvider.SetValue(this.operandValue);
		}
		else
		{
			bool flag3 = this.operation == FloatOperandOperation.Sum;
			bool flag4 = flag3;
			if (flag4)
			{
				this.valueProvider.SetValue((float)this.valueProvider.GetValue() + this.operandValue);
			}
			else
			{
				bool flag5 = this.operation == FloatOperandOperation.Reduce;
				bool flag6 = flag5;
				if (flag6)
				{
					this.valueProvider.SetValue((float)this.valueProvider.GetValue() - this.operandValue);
				}
				else
				{
					bool flag7 = this.operation == FloatOperandOperation.Divide;
					bool flag8 = flag7;
					if (flag8)
					{
						this.valueProvider.SetValue((float)this.valueProvider.GetValue() / this.operandValue);
					}
					else
					{
						bool flag9 = this.operation == FloatOperandOperation.Multiplie;
						bool flag10 = flag9;
						if (flag10)
						{
							this.valueProvider.SetValue((float)this.valueProvider.GetValue() * this.operandValue);
						}
					}
				}
			}
		}
	}
	public FloatOperandOperation operation = FloatOperandOperation.Set;
	public float operandValue = 0f;
}
