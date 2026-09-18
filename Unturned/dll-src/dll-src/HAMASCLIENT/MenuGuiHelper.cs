using System;
using System.Collections.Generic;
using SDG.Unturned;
using UnityEngine;
public static class MenuGuiHelper
{
	public static Color32 GetAccentColor(byte alpha = 255)
	{
		Color32 color = ColorConfig.GetColor("Menu accent color");
		color.a = alpha;
		return color;
	}
	public static float DraggableValueSlider(string userText, float value, float min, float max, string end = "")
	{
		bool useLegacySliders = Settings.useLegacySliders;
		bool flag = useLegacySliders;
		float num;
		if (flag)
		{
			GuiAreaState.Label(userText + value.ToString() + end);
			GuiAreaState.Space(-4);
			num = GuiAreaState.Slider(value, min, max);
		}
		else
		{
			GuiAreaState.currentArea.offset += 14;
			bool flag2 = !GuiAreaState.IsInsideScrollViewport();
			bool flag3 = flag2;
			if (flag3)
			{
				GuiAreaState.currentArea.height -= 14;
				num = value;
			}
			else
			{
				string text = (Mathf.Round(value * 1000f) * 0.001f).ToString();
				text = ((text.Length > 5) ? text.Substring(0, 5) : text);
				string text2 = userText + text + end;
				int num2 = (int)GUI.skin.label.CalcSize(new GUIContent(userText + ((int)max).ToString() + ",000" + end)).x + 8;
				GUI.Label(new Rect((float)GuiAreaState.rectX, GuiAreaState.currentArea.rect.y + (float)GuiAreaState.currentArea.offset - 14f, (float)num2, 14f), text2);
				float num3 = GUI.HorizontalSlider(new Rect((float)(GuiAreaState.rectX + num2), GuiAreaState.currentArea.rect.y + (float)GuiAreaState.currentArea.offset - 18f, (float)(GuiAreaState.width - GuiAreaState.padding - num2), 14f), value, min, max);
				num = num3;
			}
		}
		return num;
	}
	public static int IntSlider(string userText, int value, int min, int max, string end = "")
	{
		bool useLegacySliders = Settings.useLegacySliders;
		bool flag = useLegacySliders;
		int num;
		if (flag)
		{
			GuiAreaState.Label(userText + value.ToString() + end);
			GuiAreaState.Space(-4);
			num = (int)GuiAreaState.Slider((float)value, (float)min, (float)max);
		}
		else
		{
			GuiAreaState.currentArea.offset += 14;
			bool flag2 = !GuiAreaState.IsInsideScrollViewport();
			bool flag3 = flag2;
			if (flag3)
			{
				GuiAreaState.currentArea.height -= 14;
				num = value;
			}
			else
			{
				string text = userText + value.ToString() + end;
				int num2 = (int)GUI.skin.label.CalcSize(new GUIContent(userText + max.ToString() + end)).x + 8;
				GUI.Label(new Rect((float)GuiAreaState.rectX, GuiAreaState.currentArea.rect.y + (float)GuiAreaState.currentArea.offset - 14f, (float)num2, 14f), text);
				int num3 = (int)GUI.HorizontalSlider(new Rect((float)(GuiAreaState.rectX + num2), GuiAreaState.currentArea.rect.y + (float)GuiAreaState.currentArea.offset - 18f, (float)(GuiAreaState.width - GuiAreaState.padding - num2), 14f), (float)value, (float)min, (float)max);
				num = num3;
			}
		}
		return num;
	}
	public static void DrawEnumSlider<T>(this EnumOption<T> es, string selectiveName, string enumName = "") where T : struct
	{
		MenuGuiHelper.EnumSliderRow<T>(selectiveName, es, -1, enumName);
	}
	public static T EnumPopup<T>(this T src, string selectiveName, string enumName = "") where T : struct
	{
		EnumSelector d4WIZXAOkc83nheh8yO4dQDGh;
		bool flag = !MenuGuiHelper.EnumStorageCache.TryGetValue(src.GetType(), out d4WIZXAOkc83nheh8yO4dQDGh);
		bool flag2 = flag;
		if (flag2)
		{
			d4WIZXAOkc83nheh8yO4dQDGh = new EnumSelector(src);
			MenuGuiHelper.EnumStorageCache.Add(src.GetType(), d4WIZXAOkc83nheh8yO4dQDGh);
		}
		bool flag3 = !d4WIZXAOkc83nheh8yO4dQDGh.ValueJustChanged;
		if (flag3)
		{
			d4WIZXAOkc83nheh8yO4dQDGh.CurrentValue = src;
		}
		else
		{
			d4WIZXAOkc83nheh8yO4dQDGh.ValueJustChanged = false;
		}
		MenuGuiHelper.EnumStoragePopup(selectiveName, d4WIZXAOkc83nheh8yO4dQDGh, -1, enumName);
		return (T)((object)d4WIZXAOkc83nheh8yO4dQDGh.CurrentValue);
	}
	public static void DrawTextureRect(Rect rect, Texture2D tex, Color32 c, bool appendAlpha = true, ScaleMode sm = ScaleMode.StretchToFill)
	{
		rect = new Rect(Mathf.Round(rect.x), Mathf.Round(rect.y), Mathf.Round(rect.width), Mathf.Round(rect.height));
		GUI.DrawTexture(rect, tex, sm, true, 0f, c, 0f, 0f);
	}
	public static void DrawRect(Rect rect, Color32 c, bool appendAlpha = true, ScaleMode sm = ScaleMode.StretchToFill)
	{
		rect = new Rect(Mathf.Round(rect.x), Mathf.Round(rect.y), Mathf.Round(rect.width), Mathf.Round(rect.height));
		GUI.DrawTexture(rect, GuiStyles.WhiteTexture, sm, false, 0f, c, 0f, 0f);
	}
	public static void Panel(Rect rect, global::System.Action action = null, int padding = 15)
	{
		MenuGuiHelper.PanelCustom(rect, new Color32(20, 20, 20, byte.MaxValue), new Color32(30, 30, 30, byte.MaxValue), new Color32(24, 24, 24, byte.MaxValue), action, padding);
	}
	public static Texture2D GetAnimatedBgTexture()
	{
		bool flag = MenuGuiHelper.m_AnimatedBgTexture == null;
		if (flag)
		{
			MenuGuiHelper.m_AnimatedBgTexture = new Texture2D(24, 24);
			MenuGuiHelper.m_AnimatedBgTexture.wrapMode = TextureWrapMode.Repeat;
			Color32[] array = new Color32[576];
			for (int i = 0; i < 24; i++)
			{
				for (int j = 0; j < 24; j++)
				{
					int num = (j - i) % 24;
					bool flag2 = num < 0;
					if (flag2)
					{
						num += 24;
					}
					bool flag3 = num <= 1 || num >= 23;
					if (flag3)
					{
						array[i * 24 + j] = new Color32(0, 0, 0, 51);
					}
					else
					{
						array[i * 24 + j] = new Color32(0, 0, 0, 0);
					}
				}
			}
			MenuGuiHelper.m_AnimatedBgTexture.SetPixels32(array);
			MenuGuiHelper.m_AnimatedBgTexture.Apply();
		}
		return MenuGuiHelper.m_AnimatedBgTexture;
	}
	public static Texture2D GetWatermarkBgTexture()
	{
		bool flag = MenuGuiHelper.m_WatermarkBgTexture == null;
		if (flag)
		{
			MenuGuiHelper.m_WatermarkBgTexture = new Texture2D(24, 24);
			MenuGuiHelper.m_WatermarkBgTexture.wrapMode = TextureWrapMode.Repeat;
			Color32[] array = new Color32[576];
			for (int i = 0; i < 24; i++)
			{
				for (int j = 0; j < 24; j++)
				{
					int num = (j - i) % 24;
					bool flag2 = num < 0;
					if (flag2)
					{
						num += 24;
					}
					bool flag3 = num <= 2 || num >= 22;
					if (flag3)
					{
						array[i * 24 + j] = new Color32(0, 0, 0, 85);
					}
					else
					{
						array[i * 24 + j] = new Color32(0, 0, 0, 0);
					}
				}
			}
			MenuGuiHelper.m_WatermarkBgTexture.SetPixels32(array);
			MenuGuiHelper.m_WatermarkBgTexture.Apply();
		}
		return MenuGuiHelper.m_WatermarkBgTexture;
	}
	public static void PanelCustom(Rect rect, Color32 background, Color32 outline, Color32 content, global::System.Action action = null, int padding = 15)
	{
		MenuGuiHelper.DrawRect(new Rect(rect.x - 1f, rect.y - 1f, rect.width + 2f, rect.height + 2f), outline, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y, rect.width, rect.height), content, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y + 3f, 1f, 2f), outline, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x + 1f, rect.y + 2f, 1f, 1f), outline, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x + 2f, rect.y + 1f, 1f, 1f), outline, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x + 3f, rect.y, 2f, 1f), outline, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x - 1f, rect.y - 1f, 6f, 1f), background, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x - 1f, rect.y - 1f, 1f, 6f), background, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y, 1f, 3f), background, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y, 3f, 1f), background, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x + 1f, rect.y + 1f, 1f, 1f), background, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 1f, rect.y + 3f, 1f, 2f), outline, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 2f, rect.y + 2f, 1f, 1f), outline, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 3f, rect.y + 1f, 1f, 1f), outline, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 5f, rect.y, 2f, 1f), outline, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 5f, rect.y - 1f, 6f, 1f), background, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width, rect.y - 1f, 1f, 6f), background, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 1f, rect.y, 1f, 3f), background, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 3f, rect.y, 3f, 1f), background, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 2f, rect.y + 1f, 1f, 1f), background, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y + rect.height - 5f, 1f, 2f), outline, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x + 1f, rect.y + rect.height - 3f, 1f, 1f), outline, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x + 2f, rect.y + rect.height - 2f, 1f, 1f), outline, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x + 3f, rect.y + rect.height - 1f, 2f, 1f), outline, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x - 1f, rect.y + rect.height, 6f, 1f), background, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x - 1f, rect.y + rect.height - 5f, 1f, 5f), background, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y + rect.height - 1f, 3f, 1f), background, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y + rect.height - 3f, 1f, 3f), background, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x + 1f, rect.y + rect.height - 2f, 1f, 1f), background, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 1f, rect.y + rect.height - 5f, 1f, 2f), outline, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 2f, rect.y + rect.height - 3f, 1f, 1f), outline, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 3f, rect.y + rect.height - 2f, 1f, 1f), outline, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 5f, rect.y + rect.height - 1f, 2f, 1f), outline, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 5f, rect.y + rect.height, 6f, 1f), background, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width, rect.y + rect.height - 5f, 1f, 6f), background, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 3f, rect.y + rect.height - 1f, 3f, 1f), background, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 1f, rect.y + rect.height - 3f, 1f, 3f), background, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 2f, rect.y + rect.height - 2f, 1f, 3f), background, true, ScaleMode.StretchToFill);
		bool flag = action == null;
		bool flag2 = !flag;
		if (flag2)
		{
			Rect rect2 = new Rect(rect.x + (float)padding, rect.y + (float)padding, rect.width - (float)(padding * 2), rect.height - (float)(padding * 2));
			MenuGuiHelper.LastPanelContentRect = rect2;
			GUILayout.BeginArea(rect2);
			try
			{
				action();
			}
			catch (Exception ex)
			{
				Logger.LogClient("error while draw tab");
				Logger.LogClient(ex.Message);
				Logger.LogClient(ex.StackTrace);
			}
			finally
			{
				GUILayout.EndArea();
			}
		}
	}
	public static float LabeledFloatSlider(string name, float value, float min, float max, int width = -1)
	{
		bool flag = !string.IsNullOrEmpty(MenuGuiHelper.featureSearchText) && !name.ToLower().Contains(MenuGuiHelper.featureSearchText.ToLower());
		float num;
		if (flag)
		{
			num = value;
		}
		else
		{
			Rect rect = GUILayoutUtility.GetRect((float)width, 24f);
			width = (int)rect.width;
			string text = name + "_sld_hover";
			string text2 = name + "_sld_fill";
			string text3 = name + "_sld_thumb";
			string text4 = name + "_sld_value";
			float num2;
			bool flag2 = !MenuGuiHelper.AnimStateCache.TryGetValue(text4, out num2);
			if (flag2)
			{
				num2 = value;
				MenuGuiHelper.AnimStateCache.Add(text4, value);
			}
			GUI.Label(new Rect(rect.x, rect.y, rect.width - 50f, 18f), name);
			Rect rect2 = new Rect(rect.x, rect.y + 17f, rect.width, 6f);
			int controlID = GUIUtility.GetControlID(FocusType.Passive);
			bool flag3 = MenuState.popupDrawAction == null;
			if (flag3)
			{
				switch (Event.current.GetTypeForControl(controlID))
				{
				case EventType.MouseDown:
				{
					bool flag4 = rect2.Contains(Event.current.mousePosition) && Event.current.button == 0;
					if (flag4)
					{
						GUIUtility.hotControl = controlID;
						float num3 = (Event.current.mousePosition.x - rect2.x) / rect2.width;
						value = min + num3 * (max - min);
						value = Mathf.Clamp(value, min, max);
						Event.current.Use();
					}
					break;
				}
				case EventType.MouseUp:
				{
					bool flag5 = GUIUtility.hotControl == controlID && Event.current.button == 0;
					if (flag5)
					{
						GUIUtility.hotControl = 0;
						Event.current.Use();
					}
					break;
				}
				case EventType.MouseDrag:
				{
					bool flag6 = GUIUtility.hotControl == controlID;
					if (flag6)
					{
						float num4 = (Event.current.mousePosition.x - rect2.x) / rect2.width;
						value = min + num4 * (max - min);
						value = Mathf.Clamp(value, min, max);
						Event.current.Use();
					}
					break;
				}
				}
			}
			float num5;
			bool flag7 = !MenuGuiHelper.AnimStateCache.TryGetValue(text, out num5);
			if (flag7)
			{
				num5 = 0f;
				MenuGuiHelper.AnimStateCache.Add(text, 0f);
			}
			bool flag8 = rect2.Contains(Event.current.mousePosition);
			bool flag9 = Event.current.type == EventType.Repaint;
			if (flag9)
			{
				bool flag10 = flag8 || GUIUtility.hotControl == controlID;
				if (flag10)
				{
					num5 += Time.deltaTime * 5f;
				}
				else
				{
					num5 -= Time.deltaTime * 5f;
				}
				num5 = Mathf.Clamp01(num5);
				MenuGuiHelper.AnimStateCache[text] = num5;
			}
			MenuGuiHelper.DrawRect(rect2, new Color32(20, 20, 22, byte.MaxValue), true, ScaleMode.StretchToFill);
			bool flag11 = GUIUtility.hotControl == controlID;
			Color32 accentColor;
			if (flag11)
			{
				accentColor = MenuGuiHelper.GetAccentColor(byte.MaxValue);
			}
			else
			{
				bool flag12 = num5 > 0f;
				if (flag12)
				{
					float num6 = Mathf.Sin(Time.time * 3f) * 0.1f + 0.9f;
					Color32 accentColor2 = MenuGuiHelper.GetAccentColor(byte.MaxValue);
					accentColor = new Color32((byte)((float)accentColor2.r * num6), (byte)((float)accentColor2.g * num6), (byte)((float)accentColor2.b * num6), (byte)(num5 * 150f));
				}
				else
				{
					accentColor = new Color32(65, 65, 70, byte.MaxValue);
				}
			}
			MenuGuiHelper.DrawRect(new Rect(rect2.x, rect2.y, rect2.width, 1f), accentColor, true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect2.x, rect2.y + rect2.height - 1f, rect2.width, 1f), accentColor, true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect2.x, rect2.y, 1f, rect2.height), accentColor, true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect2.x + rect2.width - 1f, rect2.y, 1f, rect2.height), accentColor, true, ScaleMode.StretchToFill);
			float num7 = (value - min) / (max - min);
			float num8 = (num2 - min) / (max - min);
			float num9 = Mathf.Lerp(num8, num7, Mathf.Min(Time.deltaTime * 15f, 1f));
			MenuGuiHelper.AnimStateCache[text4] = min + num9 * (max - min);
			num2 = min + num9 * (max - min);
			float num10 = rect2.x + num9 * rect2.width;
			Rect rect3 = new Rect(rect2.x + 1f, rect2.y + 1f, Mathf.Max(0f, num10 - rect2.x - 1f), rect2.height - 2f);
			Color32 accentColor3 = MenuGuiHelper.GetAccentColor(byte.MaxValue);
			MenuGuiHelper.DrawRect(rect3, accentColor3, true, ScaleMode.StretchToFill);
			float num11 = 1f;
			bool flag13 = GUIUtility.hotControl == controlID;
			if (flag13)
			{
				num11 = 0.9f;
			}
			else
			{
				bool flag14 = num5 > 0f;
				if (flag14)
				{
					num11 = 1f + num5 * 0.2f;
				}
			}
			float num12 = 6f * num11;
			float num13 = 12f * num11;
			Rect rect4 = new Rect(num10 - num12 / 2f, rect2.y + rect2.height / 2f - num13 / 2f, num12, num13);
			MenuGuiHelper.DrawRect(rect4, accentColor3, true, ScaleMode.StretchToFill);
			float num14 = Mathf.Lerp(num2, value, Time.deltaTime * 10f);
			float num15 = Mathf.Round(num14 * 10f) * 0.1f;
			Rect rect5 = new Rect(rect.x + (float)width - 46f, rect.y, 45f, 14f);
			Color32 accentColor4 = new Color32(65, 65, 70, byte.MaxValue);
			bool flag15 = Mathf.Abs(value - num2) > 0.01f;
			if (flag15)
			{
				accentColor4 = MenuGuiHelper.GetAccentColor(byte.MaxValue);
			}
			MenuGuiHelper.DrawRect(rect5, new Color32(20, 20, 22, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect5.x, rect5.y, rect5.width, 1f), accentColor4, true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect5.x, rect5.y + rect5.height - 1f, rect5.width, 1f), accentColor4, true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect5.x, rect5.y, 1f, rect5.height), accentColor4, true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect5.x + rect5.width - 1f, rect5.y, 1f, rect5.height), accentColor4, true, ScaleMode.StretchToFill);
			GUIStyle guistyle = new GUIStyle(GUI.skin.label);
			guistyle.alignment = TextAnchor.MiddleCenter;
			guistyle.fontSize = 10;
			guistyle.normal.textColor = new Color32(230, 230, 230, byte.MaxValue);
			GUI.Label(rect5, num15.ToString(), guistyle);
			GUILayout.Space(4f);
			num = value;
		}
		return num;
	}
	public static int LabeledIntSlider(string name, int value, int min, int max, int width = -1)
	{
		bool flag = !string.IsNullOrEmpty(MenuGuiHelper.featureSearchText) && !name.ToLower().Contains(MenuGuiHelper.featureSearchText.ToLower());
		int num;
		if (flag)
		{
			num = value;
		}
		else
		{
			Rect rect = GUILayoutUtility.GetRect((float)width, 24f);
			width = (int)rect.width;
			GUI.Label(new Rect(rect.x, rect.y, rect.width - 50f, 18f), name);
			Rect rect2 = new Rect(rect.x, rect.y + 17f, rect.width, 6f);
			int controlID = GUIUtility.GetControlID(FocusType.Passive);
			bool flag2 = MenuState.popupDrawAction == null;
			if (flag2)
			{
				switch (Event.current.GetTypeForControl(controlID))
				{
				case EventType.MouseDown:
				{
					bool flag3 = rect2.Contains(Event.current.mousePosition) && Event.current.button == 0;
					if (flag3)
					{
						GUIUtility.hotControl = controlID;
						float num2 = (Event.current.mousePosition.x - rect2.x) / rect2.width;
						value = (int)((float)min + num2 * (float)(max - min));
						value = Mathf.Clamp(value, min, max);
						Event.current.Use();
					}
					break;
				}
				case EventType.MouseUp:
				{
					bool flag4 = GUIUtility.hotControl == controlID && Event.current.button == 0;
					if (flag4)
					{
						GUIUtility.hotControl = 0;
						Event.current.Use();
					}
					break;
				}
				case EventType.MouseDrag:
				{
					bool flag5 = GUIUtility.hotControl == controlID;
					if (flag5)
					{
						float num3 = (Event.current.mousePosition.x - rect2.x) / rect2.width;
						value = (int)((float)min + num3 * (float)(max - min));
						value = Mathf.Clamp(value, min, max);
						Event.current.Use();
					}
					break;
				}
				}
			}
			string text = name + "_sld";
			float num4;
			bool flag6 = !MenuGuiHelper.AnimStateCache.TryGetValue(text, out num4);
			if (flag6)
			{
				num4 = 0f;
				MenuGuiHelper.AnimStateCache.Add(text, 0f);
			}
			bool flag7 = rect2.Contains(Event.current.mousePosition);
			bool flag8 = Event.current.type == EventType.Repaint;
			if (flag8)
			{
				bool flag9 = flag7 || GUIUtility.hotControl == controlID;
				if (flag9)
				{
					num4 += Time.deltaTime * 5f;
				}
				else
				{
					num4 -= Time.deltaTime * 5f;
				}
				num4 = Mathf.Clamp01(num4);
				MenuGuiHelper.AnimStateCache[text] = num4;
			}
			MenuGuiHelper.DrawRect(rect2, new Color32(20, 20, 22, byte.MaxValue), true, ScaleMode.StretchToFill);
			bool flag10 = GUIUtility.hotControl == controlID;
			Color32 color;
			if (flag10)
			{
				color = MenuGuiHelper.GetAccentColor(byte.MaxValue);
			}
			else
			{
				bool flag11 = num4 > 0f;
				if (flag11)
				{
					color = MenuGuiHelper.GetAccentColor((byte)(num4 * 150f));
				}
				else
				{
					color = new Color32(65, 65, 70, byte.MaxValue);
				}
			}
			MenuGuiHelper.DrawRect(new Rect(rect2.x, rect2.y, rect2.width, 1f), color, true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect2.x, rect2.y + rect2.height - 1f, rect2.width, 1f), color, true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect2.x, rect2.y, 1f, rect2.height), color, true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect2.x + rect2.width - 1f, rect2.y, 1f, rect2.height), color, true, ScaleMode.StretchToFill);
			float num5 = (float)(value - min) / (float)(max - min);
			float num6 = rect2.x + num5 * rect2.width;
			Rect rect3 = new Rect(rect2.x + 1f, rect2.y + 1f, Mathf.Max(0f, num6 - rect2.x - 1f), rect2.height - 2f);
			MenuGuiHelper.DrawRect(rect3, MenuGuiHelper.GetAccentColor(byte.MaxValue), true, ScaleMode.StretchToFill);
			float num7 = Mathf.Lerp(1f, 1.3f, num4);
			float num8 = 6f * num7;
			float num9 = 12f * num7;
			Rect rect4 = new Rect(num6 - num8 / 2f, rect2.y + rect2.height / 2f - num9 / 2f, num8, num9);
			MenuGuiHelper.DrawRect(rect4, MenuGuiHelper.GetAccentColor(byte.MaxValue), true, ScaleMode.StretchToFill);
			Rect rect5 = new Rect(rect.x + (float)width - 46f, rect.y, 45f, 14f);
			Color32 color2 = new Color32(65, 65, 70, byte.MaxValue);
			MenuGuiHelper.DrawRect(rect5, new Color32(20, 20, 22, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect5.x, rect5.y, rect5.width, 1f), color2, true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect5.x, rect5.y + rect5.height - 1f, rect5.width, 1f), color2, true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect5.x, rect5.y, 1f, rect5.height), color2, true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect5.x + rect5.width - 1f, rect5.y, 1f, rect5.height), color2, true, ScaleMode.StretchToFill);
			GUIStyle guistyle = new GUIStyle(GUI.skin.label);
			guistyle.alignment = TextAnchor.MiddleCenter;
			guistyle.fontSize = 10;
			guistyle.normal.textColor = new Color32(230, 230, 230, byte.MaxValue);
			GUI.Label(rect5, value.ToString(), guistyle);
			GUILayout.Space(4f);
			num = value;
		}
		return num;
	}
	public static float RawSlider(float value, float min, float max, int width = -1)
	{
		Rect rect = GUILayoutUtility.GetRect((float)width, 8f);
		width = (int)rect.width;
		int controlID = GUIUtility.GetControlID(FocusType.Passive);
		bool flag = MenuState.popupDrawAction == null;
		if (flag)
		{
			switch (Event.current.GetTypeForControl(controlID))
			{
			case EventType.MouseDown:
			{
				bool flag2 = rect.Contains(Event.current.mousePosition) && Event.current.button == 0;
				if (flag2)
				{
					GUIUtility.hotControl = controlID;
					float num = (Event.current.mousePosition.x - rect.x) / rect.width;
					value = min + num * (max - min);
					value = Mathf.Clamp(value, min, max);
					Event.current.Use();
				}
				break;
			}
			case EventType.MouseUp:
			{
				bool flag3 = GUIUtility.hotControl == controlID && Event.current.button == 0;
				if (flag3)
				{
					GUIUtility.hotControl = 0;
					Event.current.Use();
				}
				break;
			}
			case EventType.MouseDrag:
			{
				bool flag4 = GUIUtility.hotControl == controlID;
				if (flag4)
				{
					float num2 = (Event.current.mousePosition.x - rect.x) / rect.width;
					value = min + num2 * (max - min);
					value = Mathf.Clamp(value, min, max);
					Event.current.Use();
				}
				break;
			}
			}
		}
		string text = "raw_sld_" + rect.x.ToString() + "_" + rect.y.ToString();
		float num3;
		bool flag5 = !MenuGuiHelper.AnimStateCache.TryGetValue(text, out num3);
		if (flag5)
		{
			num3 = 0f;
			MenuGuiHelper.AnimStateCache.Add(text, 0f);
		}
		bool flag6 = rect.Contains(Event.current.mousePosition);
		bool flag7 = Event.current.type == EventType.Repaint;
		if (flag7)
		{
			bool flag8 = flag6 || GUIUtility.hotControl == controlID;
			if (flag8)
			{
				num3 += Time.deltaTime * 5f;
			}
			else
			{
				num3 -= Time.deltaTime * 5f;
			}
			num3 = Mathf.Clamp01(num3);
			MenuGuiHelper.AnimStateCache[text] = num3;
		}
		Rect rect2 = new Rect(rect.x, rect.y + 1f, rect.width, 6f);
		MenuGuiHelper.DrawRect(rect2, new Color32(20, 20, 22, byte.MaxValue), true, ScaleMode.StretchToFill);
		bool flag9 = GUIUtility.hotControl == controlID;
		Color32 color;
		if (flag9)
		{
			color = MenuGuiHelper.GetAccentColor(byte.MaxValue);
		}
		else
		{
			bool flag10 = num3 > 0f;
			if (flag10)
			{
				color = MenuGuiHelper.GetAccentColor((byte)(num3 * 150f));
			}
			else
			{
				color = new Color32(65, 65, 70, byte.MaxValue);
			}
		}
		MenuGuiHelper.DrawRect(new Rect(rect2.x, rect2.y, rect2.width, 1f), color, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect2.x, rect2.y + rect2.height - 1f, rect2.width, 1f), color, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect2.x, rect2.y, 1f, rect2.height), color, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect2.x + rect2.width - 1f, rect2.y, 1f, rect2.height), color, true, ScaleMode.StretchToFill);
		float num4 = (value - min) / (max - min);
		float num5 = rect2.x + num4 * rect2.width;
		Rect rect3 = new Rect(rect2.x + 1f, rect2.y + 1f, Mathf.Max(0f, num5 - rect2.x - 1f), rect2.height - 2f);
		MenuGuiHelper.DrawRect(rect3, MenuGuiHelper.GetAccentColor(byte.MaxValue), true, ScaleMode.StretchToFill);
		Rect rect4 = new Rect(num5 - 3f, rect2.y + rect2.height / 2f - 5f, 6f, 10f);
		MenuGuiHelper.DrawRect(rect4, MenuGuiHelper.GetAccentColor(byte.MaxValue), true, ScaleMode.StretchToFill);
		return value;
	}
	public static bool DrawCheckbox(bool state, string label, params GUILayoutOption[] options)
	{
		return MenuGuiHelper.DrawCheckbox(state, label, null, options);
	}
	public static bool DrawCheckbox(bool state, string label, GUIStyle style, params GUILayoutOption[] options)
	{
		bool flag = !string.IsNullOrEmpty(MenuGuiHelper.featureSearchText) && !label.ToLower().Contains(MenuGuiHelper.featureSearchText.ToLower());
		bool flag2;
		if (flag)
		{
			flag2 = state;
		}
		else
		{
			Rect rect = GUILayoutUtility.GetRect(-1f, 20f);
			string text = label + "_chk_hover";
			string text2 = label + "_chk_check";
			float num;
			bool flag3 = !MenuGuiHelper.AnimStateCache.TryGetValue(text, out num);
			if (flag3)
			{
				num = 0f;
				MenuGuiHelper.AnimStateCache.Add(text, 0f);
			}
			float num2;
			bool flag4 = !MenuGuiHelper.AnimStateCache.TryGetValue(text2, out num2);
			if (flag4)
			{
				num2 = (state ? 1f : 0f);
				MenuGuiHelper.AnimStateCache.Add(text2, num2);
			}
			bool flag5 = rect.Contains(Event.current.mousePosition);
			bool flag6 = Event.current.type == EventType.Repaint;
			if (flag6)
			{
				bool flag7 = flag5;
				if (flag7)
				{
					num += Time.deltaTime * 5f;
				}
				else
				{
					num -= Time.deltaTime * 5f;
				}
				num = Mathf.Clamp01(num);
				MenuGuiHelper.AnimStateCache[text] = num;
				float num3 = (state ? 1f : 0f);
				num2 = Mathf.MoveTowards(num2, num3, Time.deltaTime * 8f);
				MenuGuiHelper.AnimStateCache[text2] = num2;
				float num4 = num * num * (3f - 2f * num);
				float num5 = Mathf.Round(rect.x + 2f);
				float num6 = Mathf.Round(rect.y + 4f);
				float num7 = 12f;
				Rect rect2 = new Rect(num5, num6, num7, num7);
				Color32 color = new Color32(20, 20, 22, byte.MaxValue);
				MenuGuiHelper.DrawRect(rect2, color, true, ScaleMode.StretchToFill);
				Color32 color2 = new Color32(65, 65, 70, byte.MaxValue);
				Color32 accentColor = MenuGuiHelper.GetAccentColor(byte.MaxValue);
				float num8 = Mathf.Max(num2, num4 * 0.6f);
				Color32 color3 = Color32.Lerp(color2, accentColor, num8);
				MenuGuiHelper.DrawRect(new Rect(rect2.x, rect2.y, rect2.width, 1f), color3, true, ScaleMode.StretchToFill);
				MenuGuiHelper.DrawRect(new Rect(rect2.x, rect2.y + rect2.height - 1f, rect2.width, 1f), color3, true, ScaleMode.StretchToFill);
				MenuGuiHelper.DrawRect(new Rect(rect2.x, rect2.y, 1f, rect2.height), color3, true, ScaleMode.StretchToFill);
				MenuGuiHelper.DrawRect(new Rect(rect2.x + rect2.width - 1f, rect2.y, 1f, rect2.height), color3, true, ScaleMode.StretchToFill);
				bool flag8 = num2 > 0.01f;
				if (flag8)
				{
					float num9 = num2 * num2 * (3f - 2f * num2);
					float num10 = Mathf.Round(6f * num9);
					bool flag9 = num10 < 1f;
					if (flag9)
					{
						num10 = 1f;
					}
					float num11 = rect2.x + Mathf.Round((rect2.width - num10) / 2f);
					float num12 = rect2.y + Mathf.Round((rect2.height - num10) / 2f);
					Rect rect3 = new Rect(num11, num12, num10, num10);
					Color32 accentColor2 = MenuGuiHelper.GetAccentColor((byte)(255f * num9));
					MenuGuiHelper.DrawRect(rect3, accentColor2, true, ScaleMode.StretchToFill);
				}
				Rect rect4 = new Rect(Mathf.Round(rect.x + 22f), rect.y, rect.width - 22f, rect.height);
				GUIStyle guistyle = style ?? GuiStyles.SmallBoldGrayLabelStyle;
				Color32 color4 = guistyle.normal.textColor;
				Color32 color5 = new Color32(150, 150, 150, byte.MaxValue);
				Color32 color6 = new Color32(180, 180, 180, byte.MaxValue);
				Color32 color7 = new Color32(230, 230, 230, byte.MaxValue);
				bool flag10 = num2 > 0.01f;
				Color32 color9;
				if (flag10)
				{
					Color32 color8 = Color32.Lerp(color5, color6, num4);
					color9 = Color32.Lerp(color8, color7, num2);
				}
				else
				{
					color9 = Color32.Lerp(color5, color6, num4);
				}
				guistyle.normal.textColor = color9;
				GUI.Label(rect4, label, guistyle);
				guistyle.normal.textColor = color4;
			}
			bool flag11 = flag5 && Input.GetMouseButtonDown(0) && Event.current.type == EventType.MouseDown && (MenuState.popupDrawAction == null || !MenuState.activePopupRect.Contains(new Vector2(Input.mousePosition.x, (float)Screen.height - Input.mousePosition.y)));
			bool flag12 = flag11;
			if (flag12)
			{
				state = !state;
				Event.current.Use();
			}
			flag2 = state;
		}
		return flag2;
	}
	public static bool ButtonIcon(string label, int iconType, int width = -1, bool checkEnum = true)
	{
		Rect rect = GUILayoutUtility.GetRect((float)width, 20f);
		float num;
		bool flag = !MenuGuiHelper.AnimStateCache.TryGetValue(label, out num);
		bool flag2 = flag;
		if (flag2)
		{
			num = 0f;
			MenuGuiHelper.AnimStateCache.Add(label, 0f);
		}
		bool flag3 = Event.current.type != EventType.Repaint;
		bool flag4 = !flag3;
		if (flag4)
		{
			byte b = (byte)(25f + 35f * num);
			Color32 color = new Color32(b, b, (byte)((float)b + 5f * num), byte.MaxValue);
			Color32 color2 = ((num > 0f) ? MenuGuiHelper.GetAccentColor((byte)(100f + 155f * num)) : new Color32(50, 50, 52, byte.MaxValue));
			MenuGuiHelper.DrawRect(new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, rect.height - 2f), color, true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), color2, true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y + rect.height - 1f, rect.width, 1f), color2, true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), color2, true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 1f, rect.y, 1f, rect.height), color2, true, ScaleMode.StretchToFill);
			Color32 color3 = new Color32(180, 180, 180, byte.MaxValue);
			bool flag5 = iconType == 0;
			if (flag5)
			{
				MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width / 2f - 6f, rect.y + 4f, 12f, 12f), color3, true, ScaleMode.StretchToFill);
				MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width / 2f - 4f, rect.y + 10f, 8f, 5f), color, true, ScaleMode.StretchToFill);
				MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width / 2f - 3f, rect.y + 4f, 6f, 4f), color, true, ScaleMode.StretchToFill);
			}
			else
			{
				bool flag6 = iconType == 1;
				if (flag6)
				{
					MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width / 2f - 7f, rect.y + 6f, 14f, 9f), color3, true, ScaleMode.StretchToFill);
					MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width / 2f - 7f, rect.y + 4f, 6f, 2f), color3, true, ScaleMode.StretchToFill);
					MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width / 2f - 5f, rect.y + 8f, 10f, 5f), color, true, ScaleMode.StretchToFill);
				}
				else
				{
					bool flag7 = iconType == 2;
					if (flag7)
					{
						MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width / 2f - 6f, rect.y + 5f, 12f, 2f), color3, true, ScaleMode.StretchToFill);
						MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width / 2f - 6f, rect.y + 9f, 12f, 2f), color3, true, ScaleMode.StretchToFill);
						MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width / 2f - 6f, rect.y + 13f, 12f, 2f), color3, true, ScaleMode.StretchToFill);
					}
					else
					{
						bool flag7b = iconType == 3;
						if (flag7b)
						{
							// Cloud icon - simple cloud shape
							MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width / 2f - 5f, rect.y + 8f, 10f, 6f), color3, true, ScaleMode.StretchToFill);
							MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width / 2f - 7f, rect.y + 10f, 4f, 4f), color3, true, ScaleMode.StretchToFill);
							MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width / 2f + 3f, rect.y + 9f, 4f, 5f), color3, true, ScaleMode.StretchToFill);
							MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width / 2f - 2f, rect.y + 5f, 5f, 4f), color3, true, ScaleMode.StretchToFill);
						}
					}
				}
			}
			bool flag8 = rect.Contains(Event.current.mousePosition);
			if (flag8)
			{
				string text = "";
				bool flag9 = iconType == 0;
				if (flag9)
				{
					text = "Save configuration";
				}
				else
				{
					bool flag10 = iconType == 1;
					if (flag10)
					{
						text = "Load configuration";
					}
					else
					{
						bool flag11 = iconType == 2;
						if (flag11)
						{
							text = "Configuration folder";
						}
						else
						{
							bool flag11b = iconType == 3;
							if (flag11b)
							{
								text = "Cloud configurations";
							}
						}
					}
				}
				bool flag12 = !string.IsNullOrEmpty(text);
				if (flag12)
				{
					GuiAreaState.tooltips.Add(new ValueTuple<Rect, string>(new Rect(Input.mousePosition.x, (float)Screen.height - Input.mousePosition.y + 20f * GraphicsSettings.userInterfaceScale, 600f, 30f), text));
				}
			}
			bool flag13 = rect.Contains(Event.current.mousePosition) && num <= 0.5f;
			bool flag14 = flag13;
			if (flag14)
			{
				num += Mathf.Max(Mathf.Min(Time.deltaTime * 3.8f, 0.5f - (num + Time.deltaTime * 3.8f)), 0f);
			}
			else
			{
				num -= Time.deltaTime * 4.5f;
			}
			num = Mathf.Clamp(num, 0f, (num > 0.5f) ? 1f : 0.5f);
		}
		Vector2 vector = GUIUtility.GUIToScreenPoint(new Vector2(rect.x, rect.y));
		bool flag15 = rect.Contains(Event.current.mousePosition) && Input.GetMouseButtonDown(0) && Event.current.type == EventType.MouseDown && (!checkEnum || MenuState.popupDrawAction == null || !MenuState.activePopupRect.Contains(new Vector2(Input.mousePosition.x, (float)Screen.height - Input.mousePosition.y)));
		bool flag16 = flag15 && MenuState.menuRect.x + 140f < vector.x && MenuState.menuRect.y + 80f < (float)Screen.height - vector.y;
		bool flag17 = flag16;
		if (flag17)
		{
			MenuState.activePopupOwner = null;
			num = 1f;
		}
		MenuGuiHelper.AnimStateCache[label] = num;
		return flag15;
	}
	public static bool Button(string label, int width = -1, bool checkEnum = true, string tooltip = null)
	{
		bool flag = !string.IsNullOrEmpty(MenuGuiHelper.featureSearchText) && !label.ToLower().Contains(MenuGuiHelper.featureSearchText.ToLower());
		bool flag2;
		if (flag)
		{
			flag2 = false;
		}
		else
		{
			Rect rect = GUILayoutUtility.GetRect((float)width, 20f);
			float num;
			bool flag3 = !MenuGuiHelper.AnimStateCache.TryGetValue(label, out num);
			bool flag4 = flag3;
			if (flag4)
			{
				num = 0f;
				MenuGuiHelper.AnimStateCache.Add(label, 0f);
			}
			bool flag5 = Event.current.type != EventType.Repaint;
			bool flag6 = !flag5;
			if (flag6)
			{
				byte b = (byte)(25f + 35f * num);
				Color32 color = new Color32(b, b, (byte)((float)b + 5f * num), byte.MaxValue);
				Color32 color2 = ((num > 0f) ? MenuGuiHelper.GetAccentColor((byte)(100f + 155f * num)) : new Color32(50, 50, 52, byte.MaxValue));
				MenuGuiHelper.DrawRect(new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, rect.height - 2f), color, true, ScaleMode.StretchToFill);
				MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), color2, true, ScaleMode.StretchToFill);
				MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y + rect.height - 1f, rect.width, 1f), color2, true, ScaleMode.StretchToFill);
				MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), color2, true, ScaleMode.StretchToFill);
				MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 1f, rect.y, 1f, rect.height), color2, true, ScaleMode.StretchToFill);
				GUI.Label(new Rect(rect.x + 20f, rect.y, rect.width - 40f, rect.height), label, GuiStyles.SmallBoldGrayLabelStyle);
				bool flag7 = !string.IsNullOrEmpty(tooltip) && rect.Contains(Event.current.mousePosition);
				if (flag7)
				{
					GuiAreaState.tooltips.Add(new ValueTuple<Rect, string>(new Rect(Input.mousePosition.x, (float)Screen.height - Input.mousePosition.y + 20f * GraphicsSettings.userInterfaceScale, 600f, 30f), tooltip));
				}
				bool flag8 = rect.Contains(Event.current.mousePosition) && num <= 0.5f;
				bool flag9 = flag8;
				if (flag9)
				{
					num += Mathf.Max(Mathf.Min(Time.deltaTime * 3.8f, 0.5f - (num + Time.deltaTime * 3.8f)), 0f);
				}
				else
				{
					num -= Time.deltaTime * 4.5f;
				}
				num = Mathf.Clamp(num, 0f, (num > 0.5f) ? 1f : 0.5f);
			}
			Vector2 vector = GUIUtility.GUIToScreenPoint(new Vector2(rect.x, rect.y));
			bool flag10 = rect.Contains(Event.current.mousePosition) && Input.GetMouseButtonDown(0) && Event.current.type == EventType.MouseDown && (!checkEnum || MenuState.popupDrawAction == null || !MenuState.activePopupRect.Contains(new Vector2(Input.mousePosition.x, (float)Screen.height - Input.mousePosition.y)));
			bool flag11 = flag10 && MenuState.menuRect.x + 140f < vector.x && MenuState.menuRect.y + 80f < (float)Screen.height - vector.y;
			bool flag12 = flag11;
			if (flag12)
			{
				MenuState.activePopupOwner = null;
				num = 1f;
			}
			MenuGuiHelper.AnimStateCache[label] = num;
			flag2 = flag10;
		}
		return flag2;
	}
	public static bool RectButton(Rect rect, string label, int width = -1, bool checkEnum = true)
	{
		float num;
		bool flag = !MenuGuiHelper.AnimStateCache.TryGetValue(label, out num);
		bool flag2 = flag;
		if (flag2)
		{
			num = 0f;
			MenuGuiHelper.AnimStateCache.Add(label, 0f);
		}
		bool flag3 = Event.current.type != EventType.Repaint;
		bool flag4 = !flag3;
		if (flag4)
		{
			byte b = (byte)(25f + 35f * num);
			Color32 color = new Color32(b, b, (byte)((float)b + 5f * num), byte.MaxValue);
			Color32 color2 = ((num > 0f) ? MenuGuiHelper.GetAccentColor((byte)(100f + 155f * num)) : new Color32(50, 50, 52, byte.MaxValue));
			MenuGuiHelper.DrawRect(new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, rect.height - 2f), color, true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), color2, true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y + rect.height - 1f, rect.width, 1f), color2, true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), color2, true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 1f, rect.y, 1f, rect.height), color2, true, ScaleMode.StretchToFill);
			GUI.Label(new Rect(rect.x + 20f, rect.y, rect.width - 40f, rect.height), label, GuiStyles.SmallBoldGrayLabelStyle);
			bool flag5 = rect.Contains(Event.current.mousePosition) && num <= 0.5f;
			bool flag6 = flag5;
			if (flag6)
			{
				num += Mathf.Max(Mathf.Min(Time.deltaTime * 3.8f, 0.5f - (num + Time.deltaTime * 3.8f)), 0f);
			}
			else
			{
				num -= Time.deltaTime * 4.5f;
			}
			num = Mathf.Clamp(num, 0f, (num > 0.5f) ? 1f : 0.5f);
		}
		Vector2 vector = GUIUtility.GUIToScreenPoint(new Vector2(rect.x, rect.y));
		bool flag7 = rect.Contains(Event.current.mousePosition) && Input.GetMouseButtonDown(0) && Event.current.type == EventType.MouseDown && (!checkEnum || MenuState.popupDrawAction == null || !MenuState.activePopupRect.Contains(new Vector2(Input.mousePosition.x, (float)Screen.height - Input.mousePosition.y)));
		bool flag8 = flag7 && MenuState.menuRect.x + 140f < vector.x && MenuState.menuRect.y + 80f < (float)Screen.height - vector.y;
		bool flag9 = flag8;
		if (flag9)
		{
			MenuState.activePopupOwner = null;
			num = 1f;
		}
		MenuGuiHelper.AnimStateCache[label] = num;
		return flag7;
	}
	public static bool ColorSwatchButton(string label, Color32 color, int width = -1, bool checkEnum = true)
	{
		Rect rect = GUILayoutUtility.GetRect((float)width, 24f);
		float num;
		bool flag = !MenuGuiHelper.AnimStateCache.TryGetValue(label, out num);
		bool flag2 = flag;
		if (flag2)
		{
			num = 0f;
			MenuGuiHelper.AnimStateCache.Add(label, 0f);
		}
		bool flag3 = Event.current.type != EventType.Repaint;
		bool flag4 = !flag3;
		if (flag4)
		{
			byte b = (byte)(25f + 35f * num);
			Color32 color2 = new Color32(b, b, (byte)((float)b + 5f * num), byte.MaxValue);
			Color32 color3 = ((num > 0f) ? MenuGuiHelper.GetAccentColor((byte)(100f + 155f * num)) : new Color32(50, 50, 52, byte.MaxValue));
			MenuGuiHelper.DrawRect(new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, rect.height - 2f), color2, true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), color3, true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y + rect.height - 1f, rect.width, 1f), color3, true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), color3, true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 1f, rect.y, 1f, rect.height), color3, true, ScaleMode.StretchToFill);
			GUI.Label(new Rect(rect.x + 20f, rect.y + 2f, rect.width - 60f, rect.height), label, GuiStyles.SmallBoldGrayLabelStyle);
			bool flag5 = rect.Contains(Event.current.mousePosition) && num <= 0.5f;
			bool flag6 = flag5;
			if (flag6)
			{
				num += Time.deltaTime * 3.8f;
			}
			else
			{
				num -= Time.deltaTime * 4.5f;
			}
			num = Mathf.Clamp(num, 0f, (num > 0.5f) ? 1f : 0.5f);
		}
		Vector2 vector = GUIUtility.GUIToScreenPoint(new Vector2(rect.x, rect.y));
		bool flag7 = rect.Contains(Event.current.mousePosition) && Input.GetMouseButtonDown(0) && Event.current.type == EventType.MouseDown && (!checkEnum || MenuState.popupDrawAction == null || !MenuState.activePopupRect.Contains(new Vector2(Input.mousePosition.x, (float)Screen.height - Input.mousePosition.y)));
		bool flag8 = flag7 && MenuState.menuRect.x + 140f < vector.x && MenuState.menuRect.y + 80f < (float)Screen.height - vector.y;
		bool flag9 = flag8;
		if (flag9)
		{
			MenuState.activePopupOwner = null;
			num = 1f;
		}
		MenuGuiHelper.AnimStateCache[label] = num;
		Rect rect2 = new Rect(rect.x + rect.width - 32f, rect.y + 4f, 16f, 16f);
		MenuGuiHelper.DrawRect(new Rect(rect2.x - 1f, rect2.y - 1f, rect2.width + 2f, rect2.height + 2f), new Color32(40, 40, 40, byte.MaxValue), true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(rect2, color, true, ScaleMode.StretchToFill);
		Color32 color4 = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)(80f + num * 100f));
		MenuGuiHelper.DrawRect(new Rect(rect2.x, rect2.y, rect2.width, 1f), color4, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect2.x, rect2.y, 1f, rect2.height), color4, true, ScaleMode.StretchToFill);
		Color32 color5 = new Color32(0, 0, 0, (byte)(100f + num * 50f));
		MenuGuiHelper.DrawRect(new Rect(rect2.x, rect2.y + rect2.height - 1f, rect2.width, 1f), color5, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect2.x + rect2.width - 1f, rect2.y, 1f, rect2.height), color5, true, ScaleMode.StretchToFill);
		return flag7;
	}
	public static void EnumSliderRow<T>(string name, EnumOption<T> storage, int width = -1, string valueFormat = "")
	{
		Rect rect = GUILayoutUtility.GetRect((float)width, 40f);
		width = ((width == -1) ? ((int)MenuGuiHelper.LastPanelContentRect.width) : width);
		GUI.Label(rect, name);
		Color32 color = ((MenuState.activePopupOwner == storage || rect.Contains(Event.current.mousePosition)) ? MenuGuiHelper.GetAccentColor(byte.MaxValue) : new Color32(50, 50, 52, byte.MaxValue));
		MenuGuiHelper.DrawRect(new Rect(rect.x + 1f, rect.y + 15f, rect.width - 2f, 22f), new Color32((byte)(28f + 14f * storage.holdTime), (byte)(28f + 14f * storage.holdTime), (byte)(28f + 14f * storage.holdTime), byte.MaxValue), true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x + 1f, rect.y + 14f, rect.width - 2f, 1f), color, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x + 1f, rect.y + 37f, rect.width - 2f, 1f), color, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y + 15f, 1f, 22f), color, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 1f, rect.y + 15f, 1f, 22f), color, true, ScaleMode.StretchToFill);
		GUI.Label(new Rect(rect.x + 20f, rect.y + 15f, rect.width - 20f, 22f), string.IsNullOrEmpty(valueFormat) ? storage._enum.ToString() : valueFormat, GuiStyles.SmallBoldGrayLabelStyle);
		bool flag = Event.current.type == EventType.Repaint;
		bool flag2 = flag;
		if (flag2)
		{
			bool flag3 = MenuState.activePopupOwner == storage;
			bool flag4 = flag3;
			if (flag4)
			{
				storage.holdTime = 1f;
			}
			else
			{
				bool flag5 = rect.Contains(Event.current.mousePosition);
				bool flag6 = flag5;
				if (flag6)
				{
					storage.holdTime += Time.deltaTime * 4f;
				}
				else
				{
					storage.holdTime -= Time.deltaTime * 5.2f;
				}
			}
			storage.holdTime = Mathf.Clamp01(storage.holdTime);
		}
		bool flag7 = Input.GetMouseButtonDown(0) && Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition) && (MenuState.popupDrawAction == null || !MenuState.activePopupRect.Contains(new Vector2(Input.mousePosition.x, (float)Screen.height - Input.mousePosition.y)));
		bool flag8 = flag7;
		if (flag8)
		{
			bool flag9 = MenuState.activePopupOwner == storage;
			bool flag10 = flag9;
			if (flag10)
			{
				MenuState.activePopupOwner = null;
				MenuState.clearPopupPending = true;
			}
			else
			{
				MenuState.activePopupOwner = storage;
			}
		}
		bool flag11 = MenuState.activePopupOwner == storage;
		bool flag12 = flag11;
		if (flag12)
		{
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 24f, rect.y + 27f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 23f, rect.y + 26f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 22f, rect.y + 25f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 21f, rect.y + 24f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 20f, rect.y + 23f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 19f, rect.y + 24f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 18f, rect.y + 25f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 17f, rect.y + 26f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 16f, rect.y + 27f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.EnumDropdownHeight = storage.enumValues.Length * 20;
			bool flag13 = Event.current.type == EventType.Repaint;
			bool flag14 = flag13;
			if (flag14)
			{
				MenuState.activePopupRect = new Rect(GUIUtility.GUIToScreenPoint(new Vector2(rect.x, rect.y)).x, GUIUtility.GUIToScreenPoint(new Vector2(rect.x, rect.y)).y + 40f, (float)width, (float)MenuGuiHelper.EnumDropdownHeight);
			}
			MenuState.popupDrawAction = delegate
			{
				Rect rect2 = new Rect(MenuState.activePopupRect.x + 2f, MenuState.activePopupRect.y + 2f, MenuState.activePopupRect.width - 4f, MenuState.activePopupRect.height - 4f);
				MenuGuiHelper.DrawRect(rect2, new Color32(20, 20, 22, byte.MaxValue), true, ScaleMode.StretchToFill);
				Color32 accentColor = MenuGuiHelper.GetAccentColor(byte.MaxValue);
				MenuGuiHelper.DrawRect(new Rect(rect2.x, rect2.y, rect2.width, 1f), accentColor, true, ScaleMode.StretchToFill);
				MenuGuiHelper.DrawRect(new Rect(rect2.x, rect2.y + rect2.height - 1f, rect2.width, 1f), accentColor, true, ScaleMode.StretchToFill);
				MenuGuiHelper.DrawRect(new Rect(rect2.x, rect2.y, 1f, rect2.height), accentColor, true, ScaleMode.StretchToFill);
				MenuGuiHelper.DrawRect(new Rect(rect2.x + rect2.width - 1f, rect2.y, 1f, rect2.height), accentColor, true, ScaleMode.StretchToFill);
				int num = 0;
				foreach (object obj in storage.enumValues)
				{
					bool flag15 = MenuGuiHelper.RectButton(new Rect(MenuState.activePopupRect.x + 2f, MenuState.activePopupRect.y + 2f + (float)(num * 20), MenuState.activePopupRect.width - 4f, 20f), obj.ToString(), -1, false);
					bool flag16 = flag15;
					if (flag16)
					{
						storage._enum = (T)((object)obj);
						MenuState.activePopupOwner = null;
					}
					num++;
				}
			};
		}
		else
		{
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 24f, rect.y + 23f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 23f, rect.y + 24f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 22f, rect.y + 25f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 21f, rect.y + 26f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 20f, rect.y + 27f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 19f, rect.y + 26f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 18f, rect.y + 25f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 17f, rect.y + 24f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 16f, rect.y + 23f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
		}
	}
	public static void EnumStoragePopup(string name, EnumSelector storage, int width = -1, string valueFormat = "")
	{
		Rect rect = GUILayoutUtility.GetRect((float)width, 40f);
		width = ((width == -1) ? ((int)MenuGuiHelper.LastPanelContentRect.width) : width);
		GUI.Label(rect, name);
		Color32 color = ((MenuState.activePopupOwner == storage || rect.Contains(Event.current.mousePosition)) ? MenuGuiHelper.GetAccentColor(byte.MaxValue) : new Color32(50, 50, 52, byte.MaxValue));
		MenuGuiHelper.DrawRect(new Rect(rect.x + 1f, rect.y + 15f, rect.width - 2f, 22f), new Color32((byte)(28f + 14f * storage.ValueChangeTimer), (byte)(28f + 14f * storage.ValueChangeTimer), (byte)(28f + 14f * storage.ValueChangeTimer), byte.MaxValue), true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y + 14f, rect.width, 1f), color, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y + 37f, rect.width, 1f), color, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y + 15f, 1f, 22f), color, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 1f, rect.y + 15f, 1f, 22f), color, true, ScaleMode.StretchToFill);
		GUI.Label(new Rect(rect.x + 20f, rect.y + 15f, rect.width - 20f, 22f), string.IsNullOrEmpty(valueFormat) ? storage.CurrentValue.ToString() : valueFormat, GuiStyles.SmallBoldGrayLabelStyle);
		bool flag = Event.current.type == EventType.Repaint;
		bool flag2 = flag;
		if (flag2)
		{
			bool flag3 = MenuState.activePopupOwner == storage;
			bool flag4 = flag3;
			if (flag4)
			{
				storage.ValueChangeTimer = 1f;
			}
			else
			{
				bool flag5 = rect.Contains(Event.current.mousePosition);
				bool flag6 = flag5;
				if (flag6)
				{
					storage.ValueChangeTimer += Time.deltaTime * 4f;
				}
				else
				{
					storage.ValueChangeTimer -= Time.deltaTime * 5.2f;
				}
			}
			storage.ValueChangeTimer = Mathf.Clamp01(storage.ValueChangeTimer);
		}
		bool flag7 = Input.GetMouseButtonDown(0) && Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition) && (MenuState.popupDrawAction == null || !MenuState.activePopupRect.Contains(new Vector2(Input.mousePosition.x, (float)Screen.height - Input.mousePosition.y)));
		bool flag8 = flag7;
		if (flag8)
		{
			bool flag9 = MenuState.activePopupOwner == storage;
			bool flag10 = flag9;
			if (flag10)
			{
				MenuState.activePopupOwner = null;
				MenuState.clearPopupPending = true;
			}
			else
			{
				MenuState.activePopupOwner = storage;
			}
		}
		bool flag11 = MenuState.activePopupOwner == storage;
		bool flag12 = flag11;
		if (flag12)
		{
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 24f, rect.y + 27f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 23f, rect.y + 26f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 22f, rect.y + 25f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 21f, rect.y + 24f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 20f, rect.y + 23f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 19f, rect.y + 24f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 18f, rect.y + 25f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 17f, rect.y + 26f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 16f, rect.y + 27f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.EnumDropdownHeight = storage.Values.Length * 20;
			bool flag13 = Event.current.type == EventType.Repaint;
			bool flag14 = flag13;
			if (flag14)
			{
				MenuState.activePopupRect = new Rect(GUIUtility.GUIToScreenPoint(new Vector2(rect.x, rect.y)).x, GUIUtility.GUIToScreenPoint(new Vector2(rect.x, rect.y)).y + 40f, (float)width, (float)MenuGuiHelper.EnumDropdownHeight);
			}
			MenuState.popupDrawAction = delegate
			{
				Rect rect2 = new Rect(MenuState.activePopupRect.x + 2f, MenuState.activePopupRect.y + 2f, MenuState.activePopupRect.width - 4f, MenuState.activePopupRect.height - 4f);
				MenuGuiHelper.DrawRect(rect2, new Color32(20, 20, 22, byte.MaxValue), true, ScaleMode.StretchToFill);
				Color32 accentColor = MenuGuiHelper.GetAccentColor(byte.MaxValue);
				MenuGuiHelper.DrawRect(new Rect(rect2.x, rect2.y, rect2.width, 1f), accentColor, true, ScaleMode.StretchToFill);
				MenuGuiHelper.DrawRect(new Rect(rect2.x, rect2.y + rect2.height - 1f, rect2.width, 1f), accentColor, true, ScaleMode.StretchToFill);
				MenuGuiHelper.DrawRect(new Rect(rect2.x, rect2.y, 1f, rect2.height), accentColor, true, ScaleMode.StretchToFill);
				MenuGuiHelper.DrawRect(new Rect(rect2.x + rect2.width - 1f, rect2.y, 1f, rect2.height), accentColor, true, ScaleMode.StretchToFill);
				int num = 0;
				foreach (object obj in storage.Values)
				{
					bool flag15 = MenuGuiHelper.RectButton(new Rect(MenuState.activePopupRect.x + 2f, MenuState.activePopupRect.y + 2f + (float)(num * 20), MenuState.activePopupRect.width - 4f, 20f), obj.ToString(), -1, false);
					bool flag16 = flag15;
					if (flag16)
					{
						storage.CurrentValue = obj;
						storage.ValueJustChanged = true;
						MenuState.activePopupOwner = null;
					}
					num++;
				}
			};
		}
		else
		{
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 24f, rect.y + 23f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 23f, rect.y + 24f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 22f, rect.y + 25f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 21f, rect.y + 26f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 20f, rect.y + 27f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 19f, rect.y + 26f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 18f, rect.y + 25f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 17f, rect.y + 24f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 16f, rect.y + 23f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
		}
	}
	public static void StringPopupRow(string name, DropdownState storage, int width = -1, string valueFormat = "")
	{
		Rect rect = GUILayoutUtility.GetRect((float)width, 40f);
		width = ((width == -1) ? ((int)MenuGuiHelper.LastPanelContentRect.width) : width);
		GUI.Label(rect, name);
		Color32 color = ((MenuState.activePopupOwner == storage || rect.Contains(Event.current.mousePosition)) ? MenuGuiHelper.GetAccentColor(byte.MaxValue) : new Color32(50, 50, 52, byte.MaxValue));
		MenuGuiHelper.DrawRect(new Rect(rect.x + 1f, rect.y + 15f, rect.width - 2f, 22f), new Color32((byte)(28f + 14f * storage.ScrollPosition), (byte)(28f + 14f * storage.ScrollPosition), (byte)(28f + 14f * storage.ScrollPosition), byte.MaxValue), true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y + 14f, rect.width, 1f), color, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y + 37f, rect.width, 1f), color, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y + 15f, 1f, 22f), color, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 1f, rect.y + 15f, 1f, 22f), color, true, ScaleMode.StretchToFill);
		GUI.Label(new Rect(rect.x + 20f, rect.y + 15f, rect.width - 20f, 22f), string.IsNullOrEmpty(valueFormat) ? storage.ToString() : valueFormat, GuiStyles.SmallBoldGrayLabelStyle);
		bool flag = Event.current.type == EventType.Repaint;
		bool flag2 = flag;
		if (flag2)
		{
			bool flag3 = MenuState.activePopupOwner == storage;
			bool flag4 = flag3;
			if (flag4)
			{
				storage.ScrollPosition = 1f;
			}
			else
			{
				bool flag5 = rect.Contains(Event.current.mousePosition);
				bool flag6 = flag5;
				if (flag6)
				{
					storage.ScrollPosition += Time.deltaTime * 4f;
				}
				else
				{
					storage.ScrollPosition -= Time.deltaTime * 5.2f;
				}
			}
			storage.ScrollPosition = Mathf.Clamp01(storage.ScrollPosition);
		}
		bool flag7 = Input.GetMouseButtonDown(0) && Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition) && (MenuState.popupDrawAction == null || !MenuState.activePopupRect.Contains(new Vector2(Input.mousePosition.x, (float)Screen.height - Input.mousePosition.y)));
		bool flag8 = flag7;
		if (flag8)
		{
			bool flag9 = MenuState.activePopupOwner == storage;
			bool flag10 = flag9;
			if (flag10)
			{
				MenuState.activePopupOwner = null;
			}
			else
			{
				MenuState.activePopupOwner = storage;
			}
		}
		bool flag11 = MenuState.activePopupOwner == storage;
		bool flag12 = flag11;
		if (flag12)
		{
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 24f, rect.y + 27f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 23f, rect.y + 26f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 22f, rect.y + 25f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 21f, rect.y + 24f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 20f, rect.y + 23f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 19f, rect.y + 24f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 18f, rect.y + 25f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 17f, rect.y + 26f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 16f, rect.y + 27f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.EnumDropdownHeight = storage.Options.Length * 20;
			bool flag13 = Event.current.type == EventType.Repaint;
			bool flag14 = flag13;
			if (flag14)
			{
				MenuState.activePopupRect = new Rect(GUIUtility.GUIToScreenPoint(new Vector2(rect.x, rect.y)).x, GUIUtility.GUIToScreenPoint(new Vector2(rect.x, rect.y)).y + 40f, (float)width, (float)MenuGuiHelper.EnumDropdownHeight);
			}
			MenuState.popupDrawAction = delegate
			{
				Rect rect2 = new Rect(MenuState.activePopupRect.x + 2f, MenuState.activePopupRect.y + 2f, MenuState.activePopupRect.width - 4f, MenuState.activePopupRect.height - 4f);
				MenuGuiHelper.DrawRect(rect2, new Color32(20, 20, 22, byte.MaxValue), true, ScaleMode.StretchToFill);
				Color32 accentColor = MenuGuiHelper.GetAccentColor(byte.MaxValue);
				MenuGuiHelper.DrawRect(new Rect(rect2.x, rect2.y, rect2.width, 1f), accentColor, true, ScaleMode.StretchToFill);
				MenuGuiHelper.DrawRect(new Rect(rect2.x, rect2.y + rect2.height - 1f, rect2.width, 1f), accentColor, true, ScaleMode.StretchToFill);
				MenuGuiHelper.DrawRect(new Rect(rect2.x, rect2.y, 1f, rect2.height), accentColor, true, ScaleMode.StretchToFill);
				MenuGuiHelper.DrawRect(new Rect(rect2.x + rect2.width - 1f, rect2.y, 1f, rect2.height), accentColor, true, ScaleMode.StretchToFill);
				int num = 0;
				foreach (string text in storage.Options)
				{
					bool flag15 = MenuGuiHelper.RectButton(new Rect(MenuState.activePopupRect.x + 2f, MenuState.activePopupRect.y + 2f + (float)(num * 20), MenuState.activePopupRect.width - 4f, 20f), text.ToString(), -1, false);
					bool flag16 = flag15;
					if (flag16)
					{
						storage.Selected = text;
						MenuState.activePopupOwner = null;
					}
					num++;
				}
			};
		}
		else
		{
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 24f, rect.y + 23f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 23f, rect.y + 24f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 22f, rect.y + 25f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 21f, rect.y + 26f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 20f, rect.y + 27f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 19f, rect.y + 26f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 18f, rect.y + 25f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 17f, rect.y + 24f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 16f, rect.y + 23f, 1f, 1f), new Color32(91, 91, 91, byte.MaxValue), true, ScaleMode.StretchToFill);
		}
	}
	public static float FloatTextField(float val)
	{
		bool flag = val % 1f != 0f;
		float num;
		try
		{
			num = float.Parse(GUILayout.TextField(val.ToString() + (flag ? "" : ",0"), Array.Empty<GUILayoutOption>()));
		}
		catch
		{
			num = 0f;
		}
		return num;
	}
	public static int IntTextField(int val)
	{
		int num;
		try
		{
			num = int.Parse(GUILayout.TextField(val.ToString(), Array.Empty<GUILayoutOption>()));
		}
		catch
		{
			num = 0;
		}
		return num;
	}
	public static void DrawSolidRect(Rect rect, Color32 color, int radius)
	{
		MenuGuiHelper.DrawRect(rect, color, true, ScaleMode.StretchToFill);
	}
	public static void DrawOutlinedRect(Rect rect, Color32 color, Color32 solidColor, int radius)
	{
		MenuGuiHelper.DrawRect(rect, solidColor, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), color, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y + rect.height - 1f, rect.width, 1f), color, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), color, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 1f, rect.y, 1f, rect.height), color, true, ScaleMode.StretchToFill);
	}
	[ConfigBindAttribute("Misc options", "ClearUIEffects")]
	public static void ClearUiEffects()
	{
		GameObjectPoolDictionary gameObjectPoolDictionary = ReflectionUtil.GetFieldValueTyped<GameObjectPoolDictionary>(typeof(EffectManager), "pool", null);
		EffectManager effectManager = ReflectionUtil.GetFieldValueTyped<EffectManager>(typeof(EffectManager), "manager", null);
		gameObjectPoolDictionary.DestroyAll();
		ReflectionUtil.InvokeMethod(typeof(EffectManager), "destroyAllDebris", effectManager, Array.Empty<object>());
		ReflectionUtil.InvokeMethod(typeof(EffectManager), "destroyAllUI", effectManager, Array.Empty<object>());
	}
	public static void DrawLine(Vector2 start, Vector2 end, Color color, float width)
	{
		Vector2 vector = end - start;
		float magnitude = vector.magnitude;
		bool flag = magnitude < 0.001f;
		if (!flag)
		{
			vector.Normalize();
			Vector2 vector2 = new Vector2(-vector.y, vector.x) * width * 0.5f;
			Vector3[] array = new Vector3[]
			{
				start + vector2,
				end + vector2,
				end - vector2,
				start - vector2
			};
			Color32 color2 = new Color32((byte)(color.r * 255f), (byte)(color.g * 255f), (byte)(color.b * 255f), (byte)(color.a * 255f));
			MenuGuiHelper.DrawRect(new Rect(array[0].x, array[0].y, array[1].x - array[0].x, array[1].y - array[0].y), color2, false, ScaleMode.StretchToFill);
		}
	}
	public static string featureSearchText = "";
	private static Texture2D m_AnimatedBgTexture;
	private static Texture2D m_WatermarkBgTexture;
	public static Rect LastPanelContentRect = Rect.zero;
	public static Dictionary<string, float> AnimStateCache = new Dictionary<string, float>();
	public static Dictionary<Type, EnumSelector> EnumStorageCache = new Dictionary<Type, EnumSelector>();
	private static int UnusedCounter = 0;
	private static int EnumDropdownHeight = 0;
}
