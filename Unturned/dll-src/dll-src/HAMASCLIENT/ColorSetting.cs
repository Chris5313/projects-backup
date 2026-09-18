using System;
using System.Collections.Generic;
using UnityEngine;
public class ColorSetting
{
	// (get) Token: 0x060003BF RID: 959 RVA: 0x00040CF0 File Offset: 0x0003EEF0
	// (set) Token: 0x060003C0 RID: 960 RVA: 0x00040D08 File Offset: 0x0003EF08
	public Color32 settedColor
	{
		get
		{
			return this.Color;
		}
		set
		{
			bool flag = (value.r != this.Color.r || value.g != this.Color.g || value.b != this.Color.b || value.a != this.Color.a) && this.OnColorChanged != null;
			bool flag2 = flag;
			if (flag2)
			{
				this.OnColorChanged(value);
			}
			this.Color = value;
		}
	}
	// (get) Token: 0x060003C1 RID: 961 RVA: 0x00040D8C File Offset: 0x0003EF8C
	// (set) Token: 0x060003C2 RID: 962 RVA: 0x00040DA4 File Offset: 0x0003EFA4
	public bool isGradient
	{
		get
		{
			return this.IsGradientBacking;
		}
		set
		{
			bool flag = this.IsGradientBacking != value;
			bool flag2 = flag;
			if (flag2)
			{
				if (value)
				{
					this.SavedColor = this.Color;
					ColorSetting.GradientColors.Add(this);
				}
				else
				{
					ColorSetting.GradientColors.Remove(this);
					this.Color = this.SavedColor;
				}
			}
			this.IsGradientBacking = value;
		}
	}
	public ColorSetting(Color32 color, string colorName, bool isGradient = false)
	{
		this.ColorName = colorName;
		this.Color = color;
		this.GradientOffset = 0f;
		this.GradientSpeed = 0.4f;
		this.IsGradientBacking = isGradient;
		this.OnColorChanged = null;
	}
	public ColorSetting(Color32 color, string colorName, ColorChangedHandler onColorChanged, bool isGradient = false)
	{
		this.ColorName = colorName;
		this.Color = color;
		this.GradientOffset = 0f;
		this.GradientSpeed = 0.4f;
		this.IsGradientBacking = isGradient;
		this.OnColorChanged = onColorChanged;
	}
	public static List<ColorSetting> GradientColors = new List<ColorSetting>();
	public Color32 Color;
	public string ColorName;
	public bool IsGradientBacking;
	public float GradientOffset;
	public float GradientSpeed;
	public ColorChangedHandler OnColorChanged;
	public Color32 SavedColor;
}
