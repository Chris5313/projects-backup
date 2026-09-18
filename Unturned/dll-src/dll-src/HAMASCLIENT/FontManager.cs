using System;
using System.Collections.Generic;
using UnityEngine;
public static class FontManager
{
	[InitializeAttribute]
	private static void LoadInstalledFonts()
	{
		List<ValueTuple<string, Font>> list = new List<ValueTuple<string, Font>>();
		foreach (string text in Font.GetOSInstalledFontNames())
		{
			Font font = Font.CreateDynamicFontFromOSFont(text, 14);
			bool flag = font != null;
			if (flag)
			{
				list.Add(new ValueTuple<string, Font>(text, font));
			}
		}
		FontManager.installedFonts = list.ToArray();
		foreach (ValueTuple<string, Font> valueTuple in FontManager.installedFonts)
		{
			string item = valueTuple.Item1;
			Font item2 = valueTuple.Item2;
			FontManager.fontList.Add(item2);
			FontManager.fontsByName.Add(item, item2);
		}
	}
	public static ValueTuple<string, Font>[] installedFonts = new ValueTuple<string, Font>[0];
	public static Dictionary<string, Font> fontsByName = new Dictionary<string, Font>();
	public static List<Font> fontList = new List<Font>();
}
