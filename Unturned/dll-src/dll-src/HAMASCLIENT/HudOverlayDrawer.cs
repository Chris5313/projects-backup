using System;
using SDG.Unturned;
using UnityEngine;
public static class HudOverlayDrawer
{
	public static void Draw()
	{
		bool drawHorizontalInfoPanel = Settings.drawHorizontalInfoPanel;
		if (drawHorizontalInfoPanel)
		{
			HudOverlayDrawer.DrawInfoPanel();
		}
		bool flag = Settings.useCustomCrosshair && Player.player != null;
		if (flag)
		{
			HudOverlayDrawer.DrawCustomCrosshair();
		}
	}
	private static Texture2D s_HamasCrosshairTex;
	private static Texture2D GetHamasCrosshairTex()
	{
		if (s_HamasCrosshairTex != null) return s_HamasCrosshairTex;
		int S = 64;
		s_HamasCrosshairTex = new Texture2D(S, S, TextureFormat.RGBA32, false);
		s_HamasCrosshairTex.filterMode = FilterMode.Bilinear;
		Color32[] px = new Color32[S * S];
		Color32 clear = new Color32(0, 0, 0, 0);
		Color32 fill = new Color32(255, 255, 255, 255);
		// crescent: outer R=24 center (26,32); bite Rb=19 center (38,32)
		// star: center (50,32), outer 9, inner 3.6
		float[] starX = new float[10], starY = new float[10];
		for (int i = 0; i < 10; i++)
		{
			float a = Mathf.PI / 2f + (float)i * Mathf.PI / 5f;
			float rr = (i % 2 == 0) ? 9f : 3.6f;
			starX[i] = 50f + Mathf.Cos(a) * rr;
			starY[i] = 32f + Mathf.Sin(a) * rr;
		}
		for (int y = 0; y < S; y++)
		for (int x = 0; x < S; x++)
		{
			float dx = x - 26f, dy = y - 32f;
			float dOut = Mathf.Sqrt(dx * dx + dy * dy);
			float bxc = x - 38f, byc = y - 32f;
			float dBite = Mathf.Sqrt(bxc * bxc + byc * byc);
			bool inCrescent = dOut <= 24f && dBite >= 19f;
			// point-in-star polygon (even-odd ray cast)
			bool inStar = false;
			for (int i = 0, j = 9; i < 10; j = i++)
			{
				if (((starY[i] > y) != (starY[j] > y)) &&
					(x < (starX[j] - starX[i]) * (y - starY[i]) / (starY[j] - starY[i]) + starX[i]))
					inStar = !inStar;
			}
			px[y * S + x] = (inCrescent || inStar) ? fill : clear;
		}
		s_HamasCrosshairTex.SetPixels32(px);
		s_HamasCrosshairTex.Apply();
		return s_HamasCrosshairTex;
	}
	private static void DrawCustomCrosshair()
	{
		int num = Settings.crosshairHeight * 2;
		bool flag = Settings.crosshairWidth % 2 == 0;
		CrosshairStyle crosshairType = Settings.crosshairType;
		if (crosshairType == CrosshairStyle.Hamas)
		{
			Texture2D tex = GetHamasCrosshairTex();
			float sz = 16f + Settings.crosshairHeight * 4f;
			Rect r = new Rect(Screen.width / 2f - sz / 2f, Screen.height / 2f - sz / 2f, sz, sz);
			Color prev = GUI.color;
			GUI.color = ColorConfig.GetColorEntry("Crosshair color").Color;
			GUI.DrawTexture(r, tex, ScaleMode.StretchToFill, true);
			GUI.color = prev;
		}
		else if (crosshairType == CrosshairStyle.Gap)
		{
			Rect rect = new Rect((float)(Screen.width / 2 - Settings.crosshairWidth / 2), (float)(Screen.height / 2 - Settings.crosshairWidth / 2), (float)(Settings.crosshairWidth + (flag ? 1 : 0)), (float)(Settings.crosshairWidth + (flag ? 1 : 0)));
			Rect rect2 = new Rect(rect.x, rect.y - (float)num, rect.width, (float)(num - Settings.crosshairGap));
			Rect rect3 = new Rect(rect.x, rect.y + rect.height + (float)Settings.crosshairGap, rect.width, (float)(num - Settings.crosshairGap));
			Rect rect4 = new Rect(rect.x - (float)num, rect.y, (float)(num - Settings.crosshairGap), rect.height);
			Rect rect5 = new Rect(rect.x + rect.width + (float)Settings.crosshairGap, rect.y, (float)(num - Settings.crosshairGap), rect.height);
			MenuGuiHelper.DrawTextureRect(rect2, GuiStyles.WhiteTexture, ColorConfig.GetColorEntry("Crosshair color").Color, false, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawTextureRect(rect3, GuiStyles.WhiteTexture, ColorConfig.GetColorEntry("Crosshair color").Color, false, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawTextureRect(rect4, GuiStyles.WhiteTexture, ColorConfig.GetColorEntry("Crosshair color").Color, false, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawTextureRect(rect5, GuiStyles.WhiteTexture, ColorConfig.GetColorEntry("Crosshair color").Color, false, ScaleMode.StretchToFill);
		}
		else
		{
			Rect rect6 = new Rect((float)(Screen.width / 2 - Settings.crosshairHeight), (float)(Screen.height / 2 - Settings.crosshairWidth / 2), (float)(num + 1), (float)(Settings.crosshairWidth + (flag ? 1 : 0)));
			Rect rect7 = new Rect((float)(Screen.width / 2 - Settings.crosshairWidth / 2), (float)(Screen.height / 2 - Settings.crosshairHeight), (float)(Settings.crosshairWidth + (flag ? 1 : 0)), (float)(num + 1));
			MenuGuiHelper.DrawTextureRect(rect6, GuiStyles.WhiteTexture, ColorConfig.GetColorEntry("Crosshair color").Color, false, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawTextureRect(rect7, GuiStyles.WhiteTexture, ColorConfig.GetColorEntry("Crosshair color").Color, false, ScaleMode.StretchToFill);
		}
	}
	private static void DrawInfoPanel()
	{
		GuiStyles.GrayLabelStyle.fontSize = Settings.infoPanelSize;
		GuiStyles.GrayLabelStyle.normal.textColor = ColorConfig.GetColor("Info panel text color");
		string text;
		try
		{
			text = string.Concat(new string[]
			{
				"HAMASCLIENT Unturned Public",
				" | ",
				MathUtil.FormatSeconds(DateTime.Now.Minute * 60 + DateTime.Now.Hour * 3600 + DateTime.Now.Second),
				" | ",
				Provider.isConnected ? string.Concat(new string[]
				{
					Provider.serverName,
					" | ",
					Parser.getIPFromUInt32(Provider.CurrentServerAdvertisement.ip) + ":" + Provider.CurrentServerAdvertisement.queryPort.ToString(),
					" | ",
					((int)(Provider.ping * 1000f)).ToString() + "ms"
				}) : "Not connected",
				" | ",
				FpsCounter.Fps.ToString() + " FPS"
			});
		}
		catch
		{
			text = string.Concat(new string[]
			{
				"HAMASCLIENT Unturned Public",
				" | ",
				MathUtil.FormatSeconds(DateTime.Now.Minute * 60 + DateTime.Now.Hour * 3600 + DateTime.Now.Second),
				" | ",
				"Single Player",
				" | ",
				FpsCounter.Fps.ToString() + " FPS"
			});
		}
		Vector2 vector = GuiStyles.GrayLabelStyle.CalcSize(new GUIContent(text));
		Color color = ColorConfig.GetColor("Menu background color");
		Color32 color2 = ColorConfig.GetColor("Menu line color");
		float num = 4f;
		float num2 = 8f;
		Rect rect = new Rect((float)Settings.infoPanelPaddingFromScreen, (float)Settings.infoPanelPaddingFromScreen, vector.x + num2, vector.y + num * 2f);
		bool flag = Settings.infoPanelPaddingPlacement == ScreenEdge.Top;
		if (flag)
		{
			rect.y += (float)Settings.infoPanelPadding;
		}
		else
		{
			bool flag2 = Settings.infoPanelPaddingPlacement == ScreenEdge.Left;
			if (flag2)
			{
				rect.x += (float)Settings.infoPanelPadding;
			}
		}
		MenuGuiHelper.DrawRect(rect, color, false, ScaleMode.StretchToFill);
		Texture2D watermarkBgTexture = MenuGuiHelper.GetWatermarkBgTexture();
		float num3 = Time.time * 8f % 24f;
		float num4 = num3 / 24f;
		Color color3 = GUI.color;
		GUI.color = new Color(1f, 1f, 1f, MenuState.GetMenuOpenProgress());
		GUI.DrawTextureWithTexCoords(rect, watermarkBgTexture, new Rect(-num4, 0f, rect.width / 24f, rect.height / 24f), true);
		GUI.color = color3;
		MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y, rect.width, 2f), color2, false, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y + rect.height - 2f, rect.width, 2f), color2, false, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y, 2f, rect.height), color2, false, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 2f, rect.y, 2f, rect.height), color2, false, ScaleMode.StretchToFill);
		Rect rect2 = new Rect(rect.x + num2 / 2f, rect.y + num, vector.x, vector.y);
		GUI.Label(rect2, text, GuiStyles.GrayLabelStyle);
		GuiStyles.GrayLabelStyle.normal.textColor = new Color32(100, 100, 100, byte.MaxValue);
		GuiStyles.GrayLabelStyle.fontSize = 20;
	}
}
