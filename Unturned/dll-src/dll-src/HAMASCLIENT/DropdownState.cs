using System;
public class DropdownState
{
	public DropdownState(string[] strings, string selectedString)
	{
		this.Options = strings;
		this.Selected = selectedString;
	}
	public override string ToString()
	{
		return this.Selected.ToString();
	}
	public float ScrollPosition = 0f;
	public string[] Options;
	public string Selected;
}
