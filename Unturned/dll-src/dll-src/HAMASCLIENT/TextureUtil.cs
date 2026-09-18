using System;
using System.Collections.Generic;
using UnityEngine;
public static class TextureUtil
{
	public static Texture2D CreateSolidTexture(int width, int height, Color color)
	{
		Texture2D texture2D = new Texture2D(width, height);
		texture2D.anisoLevel = 0;
		texture2D.filterMode = FilterMode.Point;
		texture2D.wrapMode = TextureWrapMode.Repeat;
		for (int i = 0; i < width; i++)
		{
			for (int j = 0; j < height; j++)
			{
				texture2D.SetPixel(i, j, color);
			}
		}
		texture2D.Apply();
		return texture2D;
	}
	public static void FillRectPixels(Rect rect, Color color, ref Texture2D texture)
	{
		int num = 0;
		while ((float)num < rect.width)
		{
			int num2 = 0;
			while ((float)num2 < rect.height)
			{
				texture.SetPixel((int)(rect.x + (float)num), texture.height - (int)(rect.y + (float)num2), color);
				num2++;
			}
			num++;
		}
		texture.Apply();
	}
	public static Texture2D CreatePixelTexture(Color color)
	{
		Texture2D texture2D = new Texture2D(1, 1);
		texture2D.anisoLevel = 0;
		texture2D.filterMode = FilterMode.Point;
		texture2D.wrapMode = TextureWrapMode.Repeat;
		texture2D.SetPixel(0, 0, color);
		texture2D.Apply();
		return texture2D;
	}
	public static Texture2D CreateBlackoutTexture()
	{
		Texture2D texture2D = new Texture2D(Screen.width, Screen.height);
		texture2D.filterMode = FilterMode.Point;
		texture2D.wrapMode = TextureWrapMode.Repeat;
		texture2D.anisoLevel = 16;
		int num = 150;
		int num2 = 40;
		float num3 = 200f;
		for (int i = 0; i < Screen.width; i++)
		{
			for (int j = 0; j < Screen.height; j++)
			{
				int num4 = i;
				int num5 = j;
				bool flag = (float)Screen.width - num3 < (float)i;
				bool flag2 = flag;
				if (flag2)
				{
					num4 = Screen.width - i;
				}
				bool flag3 = (float)Screen.height - num3 < (float)j;
				bool flag4 = flag3;
				if (flag4)
				{
					num5 = Screen.height - j;
				}
				float num6 = Mathf.Clamp(num3 - (float)Mathf.Min(num4, num5), 0f, num3);
				float num7 = ((num6 == 0f) ? ((float)num2) : ((float)num2 + (float)(num - num2) * (num6 / num3)));
				texture2D.SetPixel(i, j, new Color(0f, 0f, 0f, num7 / 255f));
			}
		}
		texture2D.Apply();
		return texture2D;
	}
	public static Texture2D CreateHsvGradient(int width, int height)
	{
		Texture2D texture2D = new Texture2D(width, height, TextureFormat.RGBA32, false);
		for (int i = 0; i < height; i++)
		{
			for (int j = 0; j < width; j++)
			{
				float num = (float)j / (float)(width - 1);
				float num2 = (float)i / (float)(height - 1);
				Color color = Color.HSVToRGB(num, 1f, 1f);
				bool flag = num2 > 0.75f;
				bool flag2 = flag;
				if (flag2)
				{
					float num3 = (num2 - 0.75f) / 0.25f;
					color = Color.Lerp(color, Color.white, num3);
				}
				else
				{
					bool flag3 = num2 < 0.25f;
					bool flag4 = flag3;
					if (flag4)
					{
						float num4 = Mathf.Abs(num2 - 0.25f) / 0.25f;
						color = Color.Lerp(color, Color.black, num4);
					}
				}
				texture2D.SetPixel(j, i, color);
			}
		}
		texture2D.Apply();
		return texture2D;
	}
	public static void GetRoundedCornerTextures(out Texture2D leftTopCorner, out Texture2D rightTopCorner, out Texture2D leftDownCorner, out Texture2D rightDownCorner, int radius)
	{
		bool flag = TextureUtil.CachedRadii.Contains(radius);
		bool flag2 = flag;
		if (flag2)
		{
			leftTopCorner = TextureUtil.LeftTopCornerCache[radius];
			leftDownCorner = TextureUtil.LeftBottomCornerCache[radius];
			rightTopCorner = TextureUtil.RightTopCornerCache[radius];
			rightDownCorner = TextureUtil.RightBottomCornerCache[radius];
		}
		else
		{
			TextureUtil.CachedRadii.Add(radius);
			leftTopCorner = new Texture2D(radius + 1, radius + 1);
			for (int i = 0; i < leftTopCorner.width; i++)
			{
				for (int j = 0; j < leftTopCorner.height + 1; j++)
				{
					Vector2Int vector2Int = new Vector2Int(i, leftTopCorner.height - j);
					bool flag3 = Vector2Int.Distance(new Vector2Int(leftTopCorner.width, leftTopCorner.height), new Vector2Int(i, j)) > (float)radius;
					bool flag4 = flag3;
					if (flag4)
					{
						leftTopCorner.SetPixel(vector2Int.x, vector2Int.y, new Color32(0, 0, 0, 0));
					}
					else
					{
						leftTopCorner.SetPixel(vector2Int.x, vector2Int.y, Color.white);
					}
				}
			}
			leftTopCorner.Apply();
			TextureUtil.LeftTopCornerCache.Add(radius, leftTopCorner);
			rightTopCorner = new Texture2D(radius + 1, radius + 1);
			for (int k = 0; k < rightTopCorner.width; k++)
			{
				for (int l = 0; l < rightTopCorner.height + 1; l++)
				{
					Vector2Int vector2Int2 = new Vector2Int(k, rightTopCorner.height - l);
					bool flag5 = Vector2Int.Distance(new Vector2Int(0, rightTopCorner.height), new Vector2Int(k, l)) > (float)radius;
					bool flag6 = flag5;
					if (flag6)
					{
						rightTopCorner.SetPixel(vector2Int2.x, vector2Int2.y, new Color32(0, 0, 0, 0));
					}
					else
					{
						rightTopCorner.SetPixel(vector2Int2.x, vector2Int2.y, Color.white);
					}
				}
			}
			rightTopCorner.Apply();
			TextureUtil.RightTopCornerCache.Add(radius, rightTopCorner);
			leftDownCorner = new Texture2D(radius + 1, radius + 1);
			for (int m = 0; m < leftDownCorner.width; m++)
			{
				for (int n = 0; n < leftDownCorner.height + 1; n++)
				{
					Vector2Int vector2Int3 = new Vector2Int(m, leftDownCorner.height - n);
					bool flag7 = Vector2Int.Distance(new Vector2Int(leftDownCorner.width, 0), new Vector2Int(m, n)) > (float)radius;
					bool flag8 = flag7;
					if (flag8)
					{
						leftDownCorner.SetPixel(vector2Int3.x, vector2Int3.y, new Color32(0, 0, 0, 0));
					}
					else
					{
						leftDownCorner.SetPixel(vector2Int3.x, vector2Int3.y, Color.white);
					}
				}
			}
			leftDownCorner.Apply();
			TextureUtil.LeftBottomCornerCache.Add(radius, leftDownCorner);
			rightDownCorner = new Texture2D(radius + 1, radius + 1);
			for (int num = 0; num < rightDownCorner.width; num++)
			{
				for (int num2 = 0; num2 < rightDownCorner.height + 1; num2++)
				{
					Vector2Int vector2Int4 = new Vector2Int(num, rightDownCorner.height - num2);
					bool flag9 = Vector2Int.Distance(new Vector2Int(0, 0), new Vector2Int(num, num2)) > (float)radius;
					bool flag10 = flag9;
					if (flag10)
					{
						rightDownCorner.SetPixel(vector2Int4.x, vector2Int4.y, new Color32(0, 0, 0, 0));
					}
					else
					{
						rightDownCorner.SetPixel(vector2Int4.x, vector2Int4.y, Color.white);
					}
				}
			}
			rightDownCorner.Apply();
			TextureUtil.RightBottomCornerCache.Add(radius, rightDownCorner);
		}
	}
	public static List<int> CachedRadii = new List<int>();
	public static Dictionary<int, Texture2D> LeftTopCornerCache = new Dictionary<int, Texture2D>();
	public static Dictionary<int, Texture2D> LeftBottomCornerCache = new Dictionary<int, Texture2D>();
	public static Dictionary<int, Texture2D> RightTopCornerCache = new Dictionary<int, Texture2D>();
	public static Dictionary<int, Texture2D> RightBottomCornerCache = new Dictionary<int, Texture2D>();
}
