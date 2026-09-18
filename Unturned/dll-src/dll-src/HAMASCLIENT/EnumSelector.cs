using System;
public class EnumSelector
{
	public EnumSelector()
	{
		this.CurrentValue = Enum.GetValues(this.CurrentValue.GetType()).GetValue(0);
		this.Values = Enum.GetValues(this.CurrentValue.GetType());
	}
	public EnumSelector(object _enum)
	{
		this.CurrentValue = _enum;
		this.Values = Enum.GetValues(_enum.GetType());
	}
	public override string ToString()
	{
		return this.CurrentValue.ToString();
	}
	public float ValueChangeTimer = 0f;
	public Array Values;
	public object CurrentValue;
	public bool ValueJustChanged = false;
}
