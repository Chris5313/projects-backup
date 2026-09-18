using System;
using UnityEngine;
public class EspTextEntry
{
	public EspTextEntry(string formattedText)
	{
		this.formatText = formattedText;
	}
	public bool drawEnabled = true;
	public bool showDistanceTag = true;
	public string formatText = "";
	public bool formatNewlines = true;
	public bool useGlobalColor = true;
	public ColorSetting textColor = new ColorSetting(Color.red, "lol esp color", false);
	public Vector2 screenOffset = Vector2.zero;
	public Vector3 worldOffset = Vector3.zero;
	public EnumOption<TextOutlineStyle> outlineMode = TextOutlineStyle.None;
	public EnumOption<TextCase> textCase = TextCase.Default;
	public int outlineThickness = 1;
	public int fontSize = 10;
	public bool scaleByDistance = false;
	public int minScaleDistance = 50;
	public int maxScaleDistance = 200;
	public int minFontSize = 4;
	public int maxFontSize = 10;
}
