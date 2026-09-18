using System;
using UnityEngine;
[Serializable]
public class DrawEntry
{
	public DrawEntry(string drawText, ConfigCategoryGroup category = null)
	{
		this.Text = drawText;
		this.Category = category;
	}
	public DrawEntry(string drawText, Texture2D texture, ConfigCategoryGroup category = null)
	{
		this.Text = drawText;
		this.Category = category;
		this.Texture = texture;
	}
	public const float FadeInTime = 0.4f;
	public const float FadeOutTime = 0.3f;
	public float Timer = 0f;
	public float Alpha = 0f;
	public string Text;
	public ConfigCategoryGroup Category;
	public Texture2D Texture;
}
