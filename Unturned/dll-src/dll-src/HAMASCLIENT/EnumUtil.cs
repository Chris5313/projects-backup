using System;
public static class EnumUtil
{
	public static object NextValue(object src)
	{
		Array values = Enum.GetValues(src.GetType());
		int num = Array.IndexOf(values, src) + 1;
		return (values.Length == num) ? values.GetValue(0) : values.GetValue(num);
	}
	public static object PreviousValue(object src)
	{
		Array values = Enum.GetValues(src.GetType());
		int num = Array.IndexOf(values, src) - 1;
		return (0 > num) ? values.GetValue(values.Length - 1) : values.GetValue(num);
	}
}
