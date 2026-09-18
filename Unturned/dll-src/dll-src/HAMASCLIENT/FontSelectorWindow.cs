using System;
using UnityEngine;
internal class FontSelectorWindow : WindowBase
{
	public override bool GetAviablity()
	{
		return FontSelectorWindow.Opened;
	}
	public override Vector2 GetSize()
	{
		return new Vector2(320f, 400f);
	}
	public override bool IsShowOnMenu()
	{
		return true;
	}
	public override void DrawWindow()
	{
		base.DrawSectionHeader("Change Font");
		GUILayout.BeginArea(new Rect(base.windowRect.x, base.windowRect.y, 320f, 400f));
		GUILayout.Space(20f);
		bool flag = GUILayout.Button("Close", Array.Empty<GUILayoutOption>());
		if (flag)
		{
			FontSelectorWindow.Opened = false;
		}
		try
		{
			bool flag2 = GUILayout.Button("Reset font", Array.Empty<GUILayoutOption>());
			if (flag2)
			{
				EspCategories.categories[FontSelectorWindow.TargetStyleIndex].TextStyle.font = GuiStyles.CenterLabelStyle.font;
				EspCategories.categories[FontSelectorWindow.TargetStyleIndex].TextShadowStyle.font = GuiStyles.DarkCenterLabelStyle.font;
				EspCategories.categories[FontSelectorWindow.TargetStyleIndex].FontName = "";
			}
			bool flag3 = FontSelectorWindow.FontButtonStyle == null;
			if (flag3)
			{
				FontSelectorWindow.FontButtonStyle = new GUIStyle(GUI.skin.button);
			}
		}
		catch
		{
		}
		FontSelectorWindow.ScrollPosition = GUILayout.BeginScrollView(FontSelectorWindow.ScrollPosition, Array.Empty<GUILayoutOption>());
		try
		{
			for (int i = 0; i < FontManager.installedFonts.Length; i++)
			{
				FontSelectorWindow.FontButtonStyle.font = FontManager.installedFonts[i].Item2;
				bool flag4 = GUILayout.Button(FontManager.installedFonts[i].Item1, FontSelectorWindow.FontButtonStyle, Array.Empty<GUILayoutOption>());
				if (flag4)
				{
					EspCategories.categories[FontSelectorWindow.TargetStyleIndex].TextStyle.font = FontManager.installedFonts[i].Item2;
					EspCategories.categories[FontSelectorWindow.TargetStyleIndex].TextShadowStyle.font = FontManager.installedFonts[i].Item2;
					EspCategories.categories[FontSelectorWindow.TargetStyleIndex].FontName = FontManager.installedFonts[i].Item1;
				}
			}
		}
		catch
		{
		}
		GUILayout.EndScrollView();
		GUILayout.EndArea();
	}
	public static bool Opened = false;
	public static Vector2 ScrollPosition = Vector2.zero;
	public static int TargetStyleIndex = 0;
	public static GUIStyle FontButtonStyle;
	public static int FontSize = 14;
}
