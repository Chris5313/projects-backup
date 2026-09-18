using System;
using System.Globalization;
using SDG.Unturned;
using UnityEngine;
public class SettingsTab : FeatureTabBase
{
	public override string GetName()
	{
		return "Settings";
	}
	public override TabCount GetTabCounts()
	{
		return TabCount.Three;
	}
	public override void DoTab(TabCount tc)
	{
		bool flag = tc == TabCount.One;
		if (flag)
		{
			base.DrawSectionHeader("Customizing colors");
			GUILayout.Label("Search colors:", Array.Empty<GUILayoutOption>());
			this.ColorSearchText = GUILayout.TextField(this.ColorSearchText, Array.Empty<GUILayoutOption>());
			this.ColorSearchScroll = GUILayout.BeginScrollView(this.ColorSearchScroll, Array.Empty<GUILayoutOption>());
			for (int i = 0; i < EspCategories.categories.Length; i++)
			{
				for (int j = 0; j < EspCategories.categories[i].LineColors.Length; j++)
				{
					bool flag2 = string.IsNullOrEmpty(this.ColorSearchText) || EspCategories.categories[i].LineColors[j].ColorName.IndexOf(this.ColorSearchText, StringComparison.OrdinalIgnoreCase) >= 0;
					if (flag2)
					{
						bool flag3 = MenuGuiHelper.ColorSwatchButton(EspCategories.categories[i].LineColors[j].ColorName, EspCategories.categories[i].LineColors[j].Color, -1, true);
						if (flag3)
						{
							this.IsCategoryColorSelected = true;
							this.SelectedColorGroupIndex = i;
							this.SelectedColorIndex = j;
						}
					}
				}
			}
			for (int k = 0; k < ColorConfig.Colors.Length; k++)
			{
				bool flag4 = string.IsNullOrEmpty(this.ColorSearchText) || ColorConfig.Colors[k].ColorName.IndexOf(this.ColorSearchText, StringComparison.OrdinalIgnoreCase) >= 0;
				if (flag4)
				{
					bool flag5 = MenuGuiHelper.ColorSwatchButton(ColorConfig.Colors[k].ColorName, ColorConfig.Colors[k].settedColor, -1, true);
					if (flag5)
					{
						this.IsCategoryColorSelected = false;
						this.SelectedColorGroupIndex = k;
					}
				}
			}
			GUILayout.EndScrollView();
		}
		else
		{
			bool flag6 = tc != TabCount.Two;
			if (flag6)
			{
				ConfigManager.CurrentConfigName = GUILayout.TextField(ConfigManager.CurrentConfigName, Array.Empty<GUILayoutOption>());
				bool flag7 = MenuGuiHelper.Button("Save config", -1, true, "Save configuration");
				if (flag7)
				{
					ConfigManager.SaveConfig(ConfigManager.CurrentConfigName);
					ConfigManager.RefreshConfigList();
				}
				GUILayout.Space(15f);
				this.ConfigListScroll = GUILayout.BeginScrollView(this.ConfigListScroll, GUILayout.Height(140f));
				string configToDelete = null;
				foreach (string text in ConfigManager.ConfigFileNames)
				{
					GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
					bool flag8 = MenuGuiHelper.Button(text.Replace(".conf", ""), 150, true, "Load configuration");
					if (flag8)
					{
						ConfigManager.LoadConfig(text);
					}
					// Delete button
					GUI.color = new Color(1f, 0.5f, 0.5f, 1f);
					if (MenuGuiHelper.Button("X", 22, true, "Delete this config"))
					{
						configToDelete = text.Replace(".conf", "");
					}
					GUI.color = Color.white;
					GUILayout.FlexibleSpace();
					GUILayout.EndHorizontal();
				}
				GUILayout.EndScrollView();
				// Process delete outside loop to avoid collection modification
				if (configToDelete != null)
				{
					ConfigManager.DeleteConfig(configToDelete);
				}
				GUILayout.Space(5f);
				try
				{
					bool flag9 = Characters.active != null;
					if (flag9)
					{
						GUILayout.Label(("Set nickname: " + Characters.active.nick == Characters.active.name) ? Characters.active.nick : (Characters.active.nick + ":" + Characters.active.name), Array.Empty<GUILayoutOption>());
					}
				}
				catch
				{
				}
				bool flag10 = MenuGuiHelper.Button(Provider.isConnected ? "Fast reconnect with nickname change" : "Change nickname", -1, true, null);
				if (flag10)
				{
					string[] array = new string[]
					{
						"Shadow", "Nova", "Blaze", "Ghost", "Viper", "Sniper", "Rogue", "Venom", "Frost", "Zero",
						"Reaper", "Drift", "Phantom", "Raze", "Knight", "Clutch", "Zynx", "Glitch", "Nexus", "Crypt"
					};
					string[] array2 = new string[]
					{
						"", "", "", "", "YT", "HD", "LOL", "420", "xX", "Xx",
						"_x", "_tv", "_pro", "Z", "1", "69", "_exe", "_bot"
					};
					string text2 = array[UnityEngine.Random.Range(0, array.Length)];
					string text3 = array2[UnityEngine.Random.Range(0, array2.Length)];
					string text4 = ((UnityEngine.Random.value < 0.3f) ? UnityEngine.Random.Range(1, 999).ToString() : "");
					string text5 = text2 + text3 + text4;
					text5 = text5.Substring(0, Mathf.Clamp(text5.Length, 0, 16));
					Characters.rename(text5);
					Characters.renick(text5);
					Logger.LogClient("Set nickname: " + text5);
					Logger.LogUser(string.Format("[+] New username {0}", text5));
					bool isConnected = Provider.isConnected;
					if (isConnected)
					{
						Provider.disconnect();
					}
				}
				
				// ═══════════ CLOUD CONFIGS - Link to Network Tab ═══════════
				GUILayout.Space(15f);
				base.DrawSectionHeader("Cloud Configs");
				
				GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
				GUILayout.Label(HamasNetwork.IsAuthenticated ? "Connected" : "Not connected", 
					HamasNetwork.IsAuthenticated ? GuiStyles.SmallBoldWhiteLabelStyle : GuiStyles.SmallGrayLabelStyle, GUILayout.Width(100f));
				GUILayout.Label(HamasNetwork.CloudConfigs.Count + " configs", GuiStyles.SmallGrayLabelStyle, GUILayout.Width(80f));
				GUILayout.FlexibleSpace();
				if (MenuGuiHelper.Button("Open Network Tab", 120, true, "Full cloud config management, online users, upload/download"))
				{
					// Switch to Network tab using pending tab switch mechanism
					if (NetworkTab.Instance != null)
					{
						MenuState.pendingTabSwitch = NetworkTab.Instance;
					}
				}
				GUILayout.EndHorizontal();
			}
			else
			{
				base.DrawSectionHeader("Change colors");
				bool d3K4rzUWBCl5cYJpxNv9aqlcg = this.IsCategoryColorSelected;
				if (d3K4rzUWBCl5cYJpxNv9aqlcg)
				{
					SettingsTab.DrawColorPicker(ref EspCategories.categories[this.SelectedColorGroupIndex].LineColors[this.SelectedColorIndex]);
				}
				else
				{
					SettingsTab.DrawColorPicker(ref ColorConfig.Colors[this.SelectedColorGroupIndex]);
				}
			}
		}
	}
	public static void DrawColorPicker(ref ColorSetting cheatColor)
	{
		Color32 color = cheatColor.settedColor;
		bool flag = !cheatColor.isGradient;
		if (flag)
		{
			Rect rect = GUILayoutUtility.GetRect(-1f, 30f);
			Rect rect2 = new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, rect.height - 2f);
			MenuGuiHelper.DrawRect(rect, new Color32(50, 50, 55, byte.MaxValue), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(rect2, color, true, ScaleMode.StretchToFill);
			Rect rect3 = new Rect(rect.x + rect.width - 90f, rect.y + (rect.height - 14f) / 2f, 60f, 14f);
			MenuGuiHelper.DrawRect(rect3, new Color32(20, 20, 22, 200), true, ScaleMode.StretchToFill);
			GUIStyle guistyle = new GUIStyle(GUI.skin.label);
			guistyle.fontSize = 9;
			guistyle.alignment = TextAnchor.MiddleCenter;
			guistyle.normal.textColor = new Color32(200, 200, 200, byte.MaxValue);
			string text = string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", new object[] { color.r, color.g, color.b, color.a });
			GUI.Label(rect3, text, guistyle);
			GUIStyle guistyle2 = new GUIStyle(GUI.skin.label);
			guistyle2.fontSize = 8;
			guistyle2.alignment = TextAnchor.MiddleCenter;
			guistyle2.normal.textColor = new Color32(180, 180, 180, byte.MaxValue);
			Rect rect4 = new Rect(rect.x + rect.width - 25f, rect.y + (rect.height - 14f) / 2f, 20f, 14f);
			MenuGuiHelper.DrawRect(rect4, new Color32(30, 30, 35, byte.MaxValue), true, ScaleMode.StretchToFill);
			GUI.Label(rect4, "C", guistyle2);
			bool flag2 = Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect4.Contains(Event.current.mousePosition);
			if (flag2)
			{
				GUIUtility.systemCopyBuffer = text;
				Event.current.Use();
			}
			Rect rect5 = new Rect(rect.x + rect.width - 48f, rect.y + (rect.height - 14f) / 2f, 20f, 14f);
			MenuGuiHelper.DrawRect(rect5, new Color32(30, 30, 35, byte.MaxValue), true, ScaleMode.StretchToFill);
			GUI.Label(rect5, "P", guistyle2);
			bool flag3 = Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect5.Contains(Event.current.mousePosition);
			if (flag3)
			{
				string text2 = GUIUtility.systemCopyBuffer;
				bool flag4 = !string.IsNullOrEmpty(text2);
				if (flag4)
				{
					text2 = text2.Trim();
					bool flag5 = text2.StartsWith("#");
					if (flag5)
					{
						text2 = text2.Substring(1);
					}
					bool flag6 = text2.StartsWith("0x", StringComparison.OrdinalIgnoreCase);
					if (flag6)
					{
						text2 = text2.Substring(2);
					}
					uint num;
					bool flag7 = uint.TryParse(text2, NumberStyles.HexNumber, null, out num);
					if (flag7)
					{
						bool flag8 = text2.Length >= 8;
						if (flag8)
						{
							color.r = (byte)((num >> 24) & 255U);
							color.g = (byte)((num >> 16) & 255U);
							color.b = (byte)((num >> 8) & 255U);
							color.a = (byte)(num & 255U);
						}
						else
						{
							bool flag9 = text2.Length >= 6;
							if (flag9)
							{
								color.r = (byte)((num >> 16) & 255U);
								color.g = (byte)((num >> 8) & 255U);
								color.b = (byte)(num & 255U);
							}
						}
					}
				}
				Event.current.Use();
			}
			Texture2D texture2D;
			bool flag10 = GuiStyles.TextureRegistry.TryGetValue("colorpicker", out texture2D) && texture2D != null;
			if (flag10)
			{
				Rect rect6 = GUILayoutUtility.GetRect(200f, 80f);
				MenuGuiHelper.DrawRect(new Rect(rect6.x - 1f, rect6.y - 1f, rect6.width + 2f, rect6.height + 2f), new Color32(50, 50, 55, byte.MaxValue), true, ScaleMode.StretchToFill);
				GUI.DrawTexture(rect6, texture2D, ScaleMode.StretchToFill, true, 0f, Color.white, 0f, 0f);
				bool flag11 = Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect6.Contains(Event.current.mousePosition);
				if (flag11)
				{
					Vector2 vector = Event.current.mousePosition - rect6.position;
					float num2 = vector.x / rect6.width;
					float num3 = 1f;
					float num4 = 1f;
					float num5 = vector.y / rect6.height;
					bool flag12 = num5 > 0.75f;
					if (flag12)
					{
						float num6 = (num5 - 0.75f) / 0.25f;
						Color color2 = Color.HSVToRGB(num2, num3, num4);
						color = Color32.Lerp(color2, Color.white, num6);
					}
					else
					{
						bool flag13 = num5 < 0.25f;
						if (flag13)
						{
							float num7 = (0.25f - num5) / 0.25f;
							Color color3 = Color.HSVToRGB(num2, num3, num4);
							color = Color32.Lerp(color3, Color.black, num7);
						}
						else
						{
							color = Color.HSVToRGB(num2, num3, num4);
						}
					}
					Event.current.Use();
				}
			}
			Rect rect7 = GUILayoutUtility.GetRect(-1f, 16f);
			GUI.Label(new Rect(rect7.x, rect7.y, 35f, 16f), "R", new GUIStyle(GUI.skin.label)
			{
				fontSize = 10,
				normal = 
				{
					textColor = new Color32(byte.MaxValue, 100, 100, byte.MaxValue)
				}
			});
			color.r = (byte)SettingsTab.DraggableValueSlider("", (float)color.r, 0f, 255f, new Rect(rect7.x + 20f, rect7.y, rect7.width - 20f, 16f));
			Rect rect8 = GUILayoutUtility.GetRect(-1f, 16f);
			GUI.Label(new Rect(rect8.x, rect8.y, 35f, 16f), "G", new GUIStyle(GUI.skin.label)
			{
				fontSize = 10,
				normal = 
				{
					textColor = new Color32(100, byte.MaxValue, 100, byte.MaxValue)
				}
			});
			color.g = (byte)SettingsTab.DraggableValueSlider("", (float)color.g, 0f, 255f, new Rect(rect8.x + 20f, rect8.y, rect8.width - 20f, 16f));
			Rect rect9 = GUILayoutUtility.GetRect(-1f, 16f);
			GUI.Label(new Rect(rect9.x, rect9.y, 35f, 16f), "B", new GUIStyle(GUI.skin.label)
			{
				fontSize = 10,
				normal = 
				{
					textColor = new Color32(100, 150, byte.MaxValue, byte.MaxValue)
				}
			});
			color.b = (byte)SettingsTab.DraggableValueSlider("", (float)color.b, 0f, 255f, new Rect(rect9.x + 20f, rect9.y, rect9.width - 20f, 16f));
			Rect rect10 = GUILayoutUtility.GetRect(-1f, 16f);
			GUI.Label(new Rect(rect10.x, rect10.y, 35f, 16f), "A", new GUIStyle(GUI.skin.label)
			{
				fontSize = 10,
				normal = 
				{
					textColor = Color.white
				}
			});
			color.a = (byte)SettingsTab.DraggableValueSlider("", (float)color.a, 0f, 255f, new Rect(rect10.x + 20f, rect10.y, rect10.width - 20f, 16f));
		}
		else
		{
			Rect rect11 = GUILayoutUtility.GetRect(-1f, 20f);
			SettingsTab.DrawRainbowGradient(rect11);
		}
		cheatColor.isGradient = MenuGuiHelper.DrawCheckbox(cheatColor.isGradient, "Rainbow Mode", Array.Empty<GUILayoutOption>());
		bool isGradient = cheatColor.isGradient;
		if (isGradient)
		{
			GUILayout.Space(4f);
			Rect rect12 = GUILayoutUtility.GetRect(-1f, 16f);
			GUI.Label(new Rect(rect12.x, rect12.y, 70f, 16f), "Speed:", new GUIStyle(GUI.skin.label)
			{
				fontSize = 10,
				normal = 
				{
					textColor = new Color32(180, 180, 180, byte.MaxValue)
				}
			});
			cheatColor.GradientSpeed = SettingsTab.DraggableValueSlider("", cheatColor.GradientSpeed, 0.05f, 1f, new Rect(rect12.x + 50f, rect12.y, rect12.width - 50f, 16f));
		}
		cheatColor.settedColor = color;
	}
	private static void DrawRainbowGradient(Rect rect)
	{
		for (int i = 0; i < (int)rect.width; i++)
		{
			float num = (float)i / rect.width;
			Color color = Color.HSVToRGB(num, 1f, 1f);
			MenuGuiHelper.DrawRect(new Rect(rect.x + (float)i, rect.y, 1f, rect.height), color, true, ScaleMode.StretchToFill);
		}
		MenuGuiHelper.DrawRect(new Rect(rect.x - 1f, rect.y - 1f, rect.width + 2f, 1f), new Color32(50, 50, 55, byte.MaxValue), true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x - 1f, rect.y + rect.height, rect.width + 2f, 1f), new Color32(50, 50, 55, byte.MaxValue), true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x - 1f, rect.y, 1f, rect.height), new Color32(50, 50, 55, byte.MaxValue), true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width, rect.y, 1f, rect.height), new Color32(50, 50, 55, byte.MaxValue), true, ScaleMode.StretchToFill);
	}
	private static float DraggableValueSlider(string userText, float value, float min, float max, Rect rect)
	{
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
		string text = "cp_sld_" + controlID.ToString();
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
		Rect rect2 = new Rect(rect.x, rect.y + 5f, rect.width, 6f);
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
		float num5 = num4 * (rect2.width - 2f);
		bool flag11 = num5 > 0f;
		if (flag11)
		{
			Rect rect3 = new Rect(rect2.x + 1f, rect2.y + 1f, num5, rect2.height - 2f);
			MenuGuiHelper.DrawRect(rect3, MenuGuiHelper.GetAccentColor(byte.MaxValue), true, ScaleMode.StretchToFill);
		}
		float num6 = rect2.x + num4 * rect2.width;
		Rect rect4 = new Rect(num6 - 3f, rect2.y - 2f, 6f, 10f);
		MenuGuiHelper.DrawRect(rect4, new Color32(230, 230, 230, byte.MaxValue), true, ScaleMode.StretchToFill);
		GUIStyle guistyle = new GUIStyle(GUI.skin.label);
		guistyle.fontSize = 9;
		guistyle.alignment = TextAnchor.MiddleRight;
		guistyle.normal.textColor = new Color32(180, 180, 180, byte.MaxValue);
		GUI.Label(new Rect(rect.x + rect.width - 35f, rect.y - 2f, 30f, 14f), ((int)value).ToString(), guistyle);
		return value;
	}
	public Vector2 ColorSearchScroll = Vector2.zero;
	public Vector2 ConfigListScroll = Vector2.zero;
	public bool IsCategoryColorSelected = true;
	public int SelectedColorGroupIndex;
	public int SelectedColorIndex;
	public string ColorSearchText = "";
	// Cloud configs
	public Vector2 CloudConfigScroll = Vector2.zero;
	public string UploadConfigName = "";
	public string UploadConfigDesc = "";
	public bool ShowCloudConfigs = false;
	public string GitHubTokenInput = "";
}
