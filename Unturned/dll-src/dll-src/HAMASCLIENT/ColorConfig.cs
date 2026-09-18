using System;
using System.Collections.Generic;
using UnityEngine;
public static class ColorConfig
{
	[InitializeAttribute]
	private static void Init()
	{
		for (int i = 0; i < ColorConfig.Colors.Length; i++)
		{
			ColorConfig.ColorNameIndexMap.Add(ColorConfig.Colors[i].ColorName, i);
		}
		ColorConfig.AddColorChangedCallback("Cham Visible Color", new ColorChangedHandler(ColorConfig.OnChamColorChanged));
		ColorConfig.AddColorChangedCallback("Cham Invisible Color", new ColorChangedHandler(ColorConfig.OnChamColorChanged));
		ColorConfig.AddColorChangedCallback("Cham Wireframe Color", new ColorChangedHandler(ColorConfig.OnChamColorChanged));
	}
	private static void OnChamColorChanged(Color32 color)
	{
		EspCategories.ChamVisibleColor = ColorConfig.GetColor("Cham Visible Color");
		EspCategories.ChamInvisibleColor = ColorConfig.GetColor("Cham Invisible Color");
		EspCategories.ChamWireframeColor = ColorConfig.GetColor("Cham Wireframe Color");
		EspManager.ReapplyAllChams();
	}
	public static void Update()
	{
		try
		{
			ColorConfig.AnimationClock += Time.deltaTime;
			bool flag = ColorConfig.AnimationClock > 1f;
			bool flag2 = flag;
			if (flag2)
			{
				ColorConfig.AnimationClock -= 1f;
			}
			for (int i = 0; i < ColorSetting.GradientColors.Count; i++)
			{
				ColorConfig.AnimateGradientColor(ColorSetting.GradientColors[i]);
			}
		}
		catch
		{
		}
	}
	public static Color GetColor(string name)
	{
		return ColorConfig.Colors[ColorConfig.ColorNameIndexMap[name]].Color;
	}
	public static ColorSetting GetColorEntry(string name)
	{
		return ColorConfig.Colors[ColorConfig.ColorNameIndexMap[name]];
	}
	public static void SetColorChangedCallback(string name, ColorChangedHandler dlg)
	{
		bool flag = ColorConfig.ColorNameIndexMap.Count > 0;
		bool flag2 = flag;
		if (flag2)
		{
			ColorConfig.Colors[ColorConfig.ColorNameIndexMap[name]].OnColorChanged = dlg;
		}
		else
		{
			for (int i = 0; i < ColorConfig.Colors.Length; i++)
			{
				bool flag3 = ColorConfig.Colors[i].ColorName == name;
				bool flag4 = flag3;
				if (flag4)
				{
					ColorConfig.Colors[i].OnColorChanged = dlg;
				}
			}
		}
	}
	public static void AddColorChangedCallback(string name, ColorChangedHandler dlg)
	{
		ColorConfig.Colors[ColorConfig.ColorNameIndexMap[name]].OnColorChanged = (ColorChangedHandler)Delegate.Combine(ColorConfig.Colors[ColorConfig.ColorNameIndexMap[name]].OnColorChanged, dlg);
	}
	public static void RemoveColorChangedCallback(string name, ColorChangedHandler dlg)
	{
		ColorConfig.Colors[ColorConfig.ColorNameIndexMap[name]].OnColorChanged = (ColorChangedHandler)Delegate.Remove(ColorConfig.Colors[ColorConfig.ColorNameIndexMap[name]].OnColorChanged, dlg);
	}
	private static void AnimateGradientColor(ColorSetting color)
	{
		color.GradientOffset += Time.deltaTime * color.GradientSpeed;
		bool flag = color.GradientOffset > 1f;
		bool flag2 = flag;
		if (flag2)
		{
			color.GradientOffset -= 1f;
		}
		color.settedColor = ColorConfig.RainbowColor(color.GradientOffset, color.settedColor.a);
	}
	public static Color32 RainbowColor(float t, byte a = 255)
	{
		ColorConfig.RainbowResult = Color.red;
		t = Mathf.Clamp01(t);
		bool flag = t < 0.16666667f;
		bool flag2 = flag;
		if (flag2)
		{
			ColorConfig.RainbowResult = Color.Lerp(ColorConfig.Colors[0].Color, ColorConfig.Colors[1].Color, t * 6f);
		}
		else
		{
			bool flag3 = t < 0.33333334f;
			bool flag4 = flag3;
			if (flag4)
			{
				ColorConfig.RainbowResult = Color.Lerp(ColorConfig.Colors[1].Color, ColorConfig.Colors[2].Color, (t - 0.16666667f) * 6f);
			}
			else
			{
				bool flag5 = t < 0.5f;
				bool flag6 = flag5;
				if (flag6)
				{
					ColorConfig.RainbowResult = Color.Lerp(ColorConfig.Colors[2].Color, ColorConfig.Colors[3].Color, (t - 0.33333334f) * 6f);
				}
				else
				{
					bool flag7 = t < 0.6666667f;
					bool flag8 = flag7;
					if (flag8)
					{
						ColorConfig.RainbowResult = Color.Lerp(ColorConfig.Colors[3].Color, ColorConfig.Colors[4].Color, (t - 0.5f) * 6f);
					}
					else
					{
						bool flag9 = t < 0.8333333f;
						bool flag10 = flag9;
						if (flag10)
						{
							ColorConfig.RainbowResult = Color.Lerp(ColorConfig.Colors[4].Color, ColorConfig.Colors[5].Color, (t - 0.6666667f) * 6f);
						}
						else
						{
							ColorConfig.RainbowResult = Color.Lerp(ColorConfig.Colors[5].Color, ColorConfig.Colors[0].Color, (t - 0.8333333f) * 6f);
						}
					}
				}
			}
		}
		ColorConfig.RainbowResult.a = a;
		return ColorConfig.RainbowResult;
	}
	public static ColorSetting[] Colors = new ColorSetting[]
	{
		new ColorSetting(Color.red, "Fading first color", false),
		new ColorSetting(Color.yellow, "Fading second color", false),
		new ColorSetting(Color.green, "Fading third color", false),
		new ColorSetting(Color.cyan, "Fading four color", false),
		new ColorSetting(Color.blue, "Fading five color", false),
		new ColorSetting(Color.magenta, "Fading six color", false),
		new ColorSetting(Color.red, "Freecamera line color", false),
		new ColorSetting(Color.red, "Aimbot FOV color", false),
		new ColorSetting(Color.red, "Aimhacks target line", false),
		new ColorSetting(Color.red, "Grab items through walls FOV color", false),
		new ColorSetting(Color.cyan, "Tracers color", false),
		new ColorSetting(Color.red, "Damage hitmarkers color", false),
		new ColorSetting(new Color(0f, 1f, 0f, 0f), "Custom nightvision color", false),
		new ColorSetting(new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue), "Custom Clouds", false),
		new ColorSetting(new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue), "Custom Clouds Rim", false),
		new ColorSetting(new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue), "Custom Sun Color", false),
		new ColorSetting(new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue), "Custom Sky Color", false),
		new ColorSetting(Color.red, "Independent player info targeting color", false),
		new ColorSetting(Color.white, "Aim hit mark color", false),
		new ColorSetting(new Color32(40, 40, 40, 100), "Info panel color", false),
		new ColorSetting(new Color32(144, 98, 38, 160), "Info panel padding color", false),
		new ColorSetting(new Color32(230, 230, 230, byte.MaxValue), "Info panel text color", false),
		new ColorSetting(new Color32(230, 230, 230, byte.MaxValue), "Crosshair color", false),
		new ColorSetting(Color.red, "Sphere preview point color", false),
		new ColorSetting(Color.red, "Sphere preview line color", false),
		new ColorSetting(Color.white, "Player step color", false),
		new ColorSetting(Color.cyan, "Walking tracers color", false),
		new ColorSetting(new Color32(16, 20, 16, byte.MaxValue), "Menu background color", false),
		new ColorSetting(new Color32(25, 35, 25, byte.MaxValue), "Menu line color", false),
		new ColorSetting(new Color32(0, 120, 40, byte.MaxValue), "Menu outlines color", false),
		new ColorSetting(new Color32(20, 30, 20, byte.MaxValue), "Menu tab color", false),
		new ColorSetting(new Color32(0, 151, 54, byte.MaxValue), "Menu elements color", false),
		new ColorSetting(new Color32(byte.MaxValue, 0, 0, 85), "Hands color", false),
		new ColorSetting(new Color32(byte.MaxValue, 0, 0, 85), "Skin color", false),
		new ColorSetting(Color.red, "Logger color", false),
		new ColorSetting(new Color32(0, byte.MaxValue, 0, byte.MaxValue), "Cham Visible Color", false),
		new ColorSetting(new Color32(byte.MaxValue, 0, 0, byte.MaxValue), "Cham Invisible Color", false),
		new ColorSetting(new Color32(0, byte.MaxValue, byte.MaxValue, byte.MaxValue), "Cham Wireframe Color", false),
		new ColorSetting(new Color32(0, 151, 54, byte.MaxValue), "Menu accent color", false)
	};
	private static Dictionary<string, int> ColorNameIndexMap = new Dictionary<string, int>();
	private static float AnimationClock = 0f;
	private static Color32 RainbowResult;
}
