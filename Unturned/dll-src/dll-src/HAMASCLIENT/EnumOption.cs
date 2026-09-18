using System;
public class EnumOption<T>
{
	public EnumOption()
	{
		this._enum = (T)((object)Enum.GetValues(this._enum.GetType()).GetValue(0));
		this.enumValues = Enum.GetValues(this._enum.GetType());
	}
	public EnumOption(T _enum)
	{
		this._enum = _enum;
		this.enumValues = Enum.GetValues(_enum.GetType());
	}
	public static implicit operator T(EnumOption<T> _enum)
	{
		return _enum._enum;
	}
	public static implicit operator EnumOption<T>(T _enum)
	{
		return new EnumOption<T>(_enum);
	}
	public override string ToString()
	{
		return this._enum.ToString();
	}
	public float holdTime = 0f;
	public Array enumValues;
	public T _enum;
}
