using System;
using System.Collections.Generic;
using UnityEngine;
public class VisualsTab : FeatureTabBase
{
	public override string GetName()
	{
		return "Visuals";
	}
	public override int SortId()
	{
		return 0;
	}
	public override void DoTab(TabCount tc)
	{
		bool flag = tc == TabCount.One;
		bool flag2 = flag;
		if (flag2)
		{
			for (int i = 0; i < EspCategories.categories.Length; i++)
			{
				bool flag3 = MenuGuiHelper.Button(EspCategories.categories[i].CategoryName, -1, true, null);
				bool flag4 = flag3;
				if (flag4)
				{
					this.SelectedVisualIndex = i;
				}
			}
			EspManager.EspDirtyFlag = MenuGuiHelper.DrawCheckbox(EspManager.EspDirtyFlag, "Accurate bounds calculation (slower)", Array.Empty<GUILayoutOption>());
			bool flag5 = MenuGuiHelper.Button("Reset settings", -1, true, null);
			bool flag6 = flag5;
			if (flag6)
			{
				for (int j = 0; j < EspCategories.categories.Length; j++)
				{
					EspCategories.categories[j].enabled = false;
					EspCategories.categories[j].SettingFlag1 = false;
					EspCategories.categories[j].SettingFlag2 = false;
					EspCategories.categories[j].SettingFlag3 = false;
					EspCategories.categories[j].SettingFlag4 = false;
					EspCategories.categories[j].SettingFlag5 = true;
					EspCategories.categories[j].SettingFlag6 = false;
					EspCategories.categories[j].SettingFlag7 = false;
					EspCategories.categories[j].SettingFlag8 = false;
					EspCategories.categories[j].SettingFlag14 = true;
					EspCategories.categories[j].SettingFlag15 = false;
					EspCategories.categories[j].AnchorOffset = new Vector2(0.5f, 1f);
					EspCategories.categories[j].SettingFlag11 = false;
					EspCategories.categories[j].SettingFlag12 = true;
					EspCategories.categories[j].MaxLineCount = 360;
					EspCategories.categories[j].SettingFlag13 = true;
					EspCategories.categories[j].LineEntries = new List<EspTextEntry>
					{
						new EspTextEntry(EspCategories.categories[j].FormattedText)
					};
				}
			}
		}
		else
		{
			EspCategory dwIoOmLGoIyIvOm9z0SHgcarr = EspCategories.categories[this.SelectedVisualIndex];
			base.DrawSectionHeader(dwIoOmLGoIyIvOm9z0SHgcarr.CategoryName);
			dwIoOmLGoIyIvOm9z0SHgcarr.PositionOffset = GUILayout.BeginScrollView(dwIoOmLGoIyIvOm9z0SHgcarr.PositionOffset, Array.Empty<GUILayoutOption>());
			dwIoOmLGoIyIvOm9z0SHgcarr.enabled = MenuGuiHelper.DrawCheckbox(dwIoOmLGoIyIvOm9z0SHgcarr.enabled, "Enabled", Array.Empty<GUILayoutOption>());
			dwIoOmLGoIyIvOm9z0SHgcarr.SettingFlag1 = MenuGuiHelper.DrawCheckbox(dwIoOmLGoIyIvOm9z0SHgcarr.SettingFlag1, "Enable 2D box", Array.Empty<GUILayoutOption>());
			bool dmk5zVuQk0jGUFJWxk9EnD7G = dwIoOmLGoIyIvOm9z0SHgcarr.SettingFlag1;
			bool flag7 = dmk5zVuQk0jGUFJWxk9EnD7G;
			if (flag7)
			{
				dwIoOmLGoIyIvOm9z0SHgcarr.SettingFlag2 = MenuGuiHelper.DrawCheckbox(dwIoOmLGoIyIvOm9z0SHgcarr.SettingFlag2, "Enable internal 2D box outline", Array.Empty<GUILayoutOption>());
				dwIoOmLGoIyIvOm9z0SHgcarr.SettingFlag3 = MenuGuiHelper.DrawCheckbox(dwIoOmLGoIyIvOm9z0SHgcarr.SettingFlag3, "Enable external 2D box outline", Array.Empty<GUILayoutOption>());
				dwIoOmLGoIyIvOm9z0SHgcarr.SettingFlag4 = MenuGuiHelper.DrawCheckbox(dwIoOmLGoIyIvOm9z0SHgcarr.SettingFlag4, "Enable filled 2D box", Array.Empty<GUILayoutOption>());
			}
			dwIoOmLGoIyIvOm9z0SHgcarr.SettingFlag5 = MenuGuiHelper.DrawCheckbox(dwIoOmLGoIyIvOm9z0SHgcarr.SettingFlag5, "Enable 2D box plane", Array.Empty<GUILayoutOption>());
			bool d30iUXR8sxNNzAAzBtRq0xi0n = dwIoOmLGoIyIvOm9z0SHgcarr.SettingFlag5;
			bool flag8 = d30iUXR8sxNNzAAzBtRq0xi0n;
			if (flag8)
			{
				dwIoOmLGoIyIvOm9z0SHgcarr.SettingFlag6 = MenuGuiHelper.DrawCheckbox(dwIoOmLGoIyIvOm9z0SHgcarr.SettingFlag6, "Enable internal plane outline", Array.Empty<GUILayoutOption>());
				dwIoOmLGoIyIvOm9z0SHgcarr.SettingFlag7 = MenuGuiHelper.DrawCheckbox(dwIoOmLGoIyIvOm9z0SHgcarr.SettingFlag7, "Enable external plane outline", Array.Empty<GUILayoutOption>());
			}
			dwIoOmLGoIyIvOm9z0SHgcarr.SettingFlag8 = MenuGuiHelper.DrawCheckbox(dwIoOmLGoIyIvOm9z0SHgcarr.SettingFlag8, "Enable 3D box", Array.Empty<GUILayoutOption>());
			dwIoOmLGoIyIvOm9z0SHgcarr.SettingFlag14 = MenuGuiHelper.DrawCheckbox(dwIoOmLGoIyIvOm9z0SHgcarr.SettingFlag14, "Enable snaplines", Array.Empty<GUILayoutOption>());
			bool dsy8h4Odqh4GzyIBRSA0YZwCx = dwIoOmLGoIyIvOm9z0SHgcarr.SettingFlag14;
			bool flag9 = dsy8h4Odqh4GzyIBRSA0YZwCx;
			if (flag9)
			{
				dwIoOmLGoIyIvOm9z0SHgcarr.SettingFlag15 = MenuGuiHelper.DrawCheckbox(dwIoOmLGoIyIvOm9z0SHgcarr.SettingFlag15, "Enable snapline outlines", Array.Empty<GUILayoutOption>());
				GUILayout.Label("Line center: ", Array.Empty<GUILayoutOption>());
				GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
				GUILayout.Label("X:", Array.Empty<GUILayoutOption>());
				dwIoOmLGoIyIvOm9z0SHgcarr.AnchorOffset.x = MenuGuiHelper.FloatTextField(dwIoOmLGoIyIvOm9z0SHgcarr.AnchorOffset.x);
				GUILayout.Label("Y:", Array.Empty<GUILayoutOption>());
				dwIoOmLGoIyIvOm9z0SHgcarr.AnchorOffset.y = MenuGuiHelper.FloatTextField(dwIoOmLGoIyIvOm9z0SHgcarr.AnchorOffset.y);
				GUILayout.EndHorizontal();
			}
			dwIoOmLGoIyIvOm9z0SHgcarr.SettingFlag11 = MenuGuiHelper.DrawCheckbox(dwIoOmLGoIyIvOm9z0SHgcarr.SettingFlag11, "Enable chams", Array.Empty<GUILayoutOption>());
			bool d2JTP060dwZSwDD85nvmWwK0A = dwIoOmLGoIyIvOm9z0SHgcarr.SettingFlag11;
			if (d2JTP060dwZSwDD85nvmWwK0A)
			{
				bool chamVisibilityCheck = VisualsTab.ChamVisibilityCheck;
				VisualsTab.ChamVisibilityCheck = MenuGuiHelper.DrawCheckbox(VisualsTab.ChamVisibilityCheck, "Enable visibility check", Array.Empty<GUILayoutOption>());
				bool flag10 = chamVisibilityCheck != VisualsTab.ChamVisibilityCheck;
				if (flag10)
				{
					EspManager.ReapplyAllChams();
				}
			}
			dwIoOmLGoIyIvOm9z0SHgcarr.WireframeEnabled = MenuGuiHelper.DrawCheckbox(dwIoOmLGoIyIvOm9z0SHgcarr.WireframeEnabled, "Enable wireframe", Array.Empty<GUILayoutOption>());
			bool dwireframeEnabled = dwIoOmLGoIyIvOm9z0SHgcarr.WireframeEnabled;
			if (dwireframeEnabled)
			{
				int wireframeLineDensity = Settings.wireframeLineDensity;
				Settings.wireframeLineDensity = MenuGuiHelper.LabeledIntSlider("Wireframe line density: ", Settings.wireframeLineDensity, 1, 100, -1);
				bool flag11 = wireframeLineDensity != Settings.wireframeLineDensity;
				if (flag11)
				{
					ChamWireframeComponent.DInvalidateAllLod();
				}
			}
			dwIoOmLGoIyIvOm9z0SHgcarr.SettingFlag12 = MenuGuiHelper.DrawCheckbox(dwIoOmLGoIyIvOm9z0SHgcarr.SettingFlag12, "Enable render distance limit", Array.Empty<GUILayoutOption>());
			bool davrKLDCvmAu7NRTbchmgnI2B = dwIoOmLGoIyIvOm9z0SHgcarr.SettingFlag12;
			bool flag12 = davrKLDCvmAu7NRTbchmgnI2B;
			if (flag12)
			{
				dwIoOmLGoIyIvOm9z0SHgcarr.MaxLineCount = MenuGuiHelper.LabeledIntSlider("Render distance: ", dwIoOmLGoIyIvOm9z0SHgcarr.MaxLineCount, 0, 2000, -1);
			}
			for (int k = 0; k < dwIoOmLGoIyIvOm9z0SHgcarr.Options.Length; k++)
			{
				dwIoOmLGoIyIvOm9z0SHgcarr.Options[k].DisplayOption();
			}
			bool flag13 = this.SelectedVisualIndex == 12;
			if (flag13)
			{
				GUILayout.Label("Ore name filter:", Array.Empty<GUILayoutOption>());
				EspManager.DoreNameFilter = GUILayout.TextField(EspManager.DoreNameFilter, Array.Empty<GUILayoutOption>());
			}
			dwIoOmLGoIyIvOm9z0SHgcarr.SettingFlag13 = MenuGuiHelper.DrawCheckbox(dwIoOmLGoIyIvOm9z0SHgcarr.SettingFlag13, "Draw text", Array.Empty<GUILayoutOption>());
			bool darar3adCBuubvdP0AmlvE03C = dwIoOmLGoIyIvOm9z0SHgcarr.SettingFlag13;
			bool flag14 = darar3adCBuubvdP0AmlvE03C;
			if (flag14)
			{
				for (int l = 0; l < dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries.Count; l++)
				{
					try
					{
						dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].drawEnabled = MenuGuiHelper.DrawCheckbox(dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].drawEnabled, "Draw this text", Array.Empty<GUILayoutOption>());
						GUILayout.Label("Format text:", Array.Empty<GUILayoutOption>());
						dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].formatText = GUILayout.TextField(dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].formatText, Array.Empty<GUILayoutOption>());
						dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].scaleByDistance = MenuGuiHelper.DrawCheckbox(dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].scaleByDistance, "Scale text by distance", Array.Empty<GUILayoutOption>());
						bool d2Dz7Uaa1ngijd9u4MUUh6Ewj = dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].scaleByDistance;
						bool flag15 = d2Dz7Uaa1ngijd9u4MUUh6Ewj;
						if (flag15)
						{
							dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].minScaleDistance = MenuGuiHelper.LabeledIntSlider("Min scale distance: ", dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].minScaleDistance, 50, 1000, -1);
							dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].maxScaleDistance = MenuGuiHelper.LabeledIntSlider("Max scale distance: ", dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].maxScaleDistance, 50, 1000, -1);
							dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].minFontSize = MenuGuiHelper.LabeledIntSlider("Min font size: ", dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].minFontSize, 5, 20, -1);
							dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].maxFontSize = MenuGuiHelper.LabeledIntSlider("Max font size: ", dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].maxFontSize, 5, 20, -1);
						}
						else
						{
							dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].fontSize = MenuGuiHelper.LabeledIntSlider("Font size: ", dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].fontSize, 5, 20, -1);
						}
						dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].formatNewlines = MenuGuiHelper.DrawCheckbox(dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].formatNewlines, "Format newlines", Array.Empty<GUILayoutOption>());
						GUILayout.Label("3D offset: ", Array.Empty<GUILayoutOption>());
						GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
						GUILayout.Label("X:", Array.Empty<GUILayoutOption>());
						dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].worldOffset.x = MenuGuiHelper.FloatTextField(dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].worldOffset.x);
						GUILayout.Label("Y:", Array.Empty<GUILayoutOption>());
						dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].worldOffset.y = MenuGuiHelper.FloatTextField(dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].worldOffset.y);
						GUILayout.Label("Z:", Array.Empty<GUILayoutOption>());
						dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].worldOffset.z = MenuGuiHelper.FloatTextField(dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].worldOffset.z);
						GUILayout.EndHorizontal();
						GUILayout.Label("2D offset: ", Array.Empty<GUILayoutOption>());
						GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
						GUILayout.Label("X:", Array.Empty<GUILayoutOption>());
						dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].screenOffset.x = (float)MenuGuiHelper.IntTextField((int)dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].screenOffset.x);
						GUILayout.Label("Y:", Array.Empty<GUILayoutOption>());
						dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].screenOffset.y = (float)MenuGuiHelper.IntTextField((int)dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].screenOffset.y);
						GUILayout.EndHorizontal();
						dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].outlineThickness = MenuGuiHelper.LabeledIntSlider("Outline thickness: ", dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].outlineThickness, 1, 5, -1);
						dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].outlineMode.DrawEnumSlider<TextOutlineStyle>("Outline mode: ", dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].outlineMode._enum.ToDisplayNameOutlineMode());
						dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].textCase.DrawEnumSlider<TextCase>("Text case: ", dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].textCase._enum.ToDisplayNameTextCase());
						GuiStyles.CustomToggleStyle.normal.textColor = (dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].useGlobalColor ? Color.white : (dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].textColor.isGradient ? Color.white : dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].textColor.Color));
						dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].useGlobalColor = MenuGuiHelper.DrawCheckbox(dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].useGlobalColor, "Use global color", GuiStyles.CustomToggleStyle, Array.Empty<GUILayoutOption>());
						bool flag16 = !dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].useGlobalColor;
						bool flag17 = flag16;
						if (flag17)
						{
							Color32 dkJGdJpvFP4j4uWN4CyFixyQ = dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].textColor.Color;
							bool flag18 = !dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].textColor.isGradient;
							bool flag19 = flag18;
							if (flag19)
							{
								GUILayout.Label(string.Format("Red: {0}, Green: {1}, Blue: {2}", dkJGdJpvFP4j4uWN4CyFixyQ.r, dkJGdJpvFP4j4uWN4CyFixyQ.g, dkJGdJpvFP4j4uWN4CyFixyQ.b), Array.Empty<GUILayoutOption>());
								GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
								dkJGdJpvFP4j4uWN4CyFixyQ.r = (byte)MenuGuiHelper.RawSlider((float)dkJGdJpvFP4j4uWN4CyFixyQ.r, 0f, 255f, -1);
								dkJGdJpvFP4j4uWN4CyFixyQ.g = (byte)MenuGuiHelper.RawSlider((float)dkJGdJpvFP4j4uWN4CyFixyQ.g, 0f, 255f, -1);
								dkJGdJpvFP4j4uWN4CyFixyQ.b = (byte)MenuGuiHelper.RawSlider((float)dkJGdJpvFP4j4uWN4CyFixyQ.b, 0f, 255f, -1);
								GUILayout.EndHorizontal();
							}
							dkJGdJpvFP4j4uWN4CyFixyQ.a = (byte)MenuGuiHelper.LabeledIntSlider("Alpha: ", (int)dkJGdJpvFP4j4uWN4CyFixyQ.a, 0, 255, -1);
							dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].textColor.isGradient = MenuGuiHelper.DrawCheckbox(dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].textColor.isGradient, "Enable rainbow", Array.Empty<GUILayoutOption>());
							bool isGradient = dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].textColor.isGradient;
							bool flag20 = isGradient;
							if (flag20)
							{
								dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].textColor.GradientSpeed = MenuGuiHelper.LabeledFloatSlider("Rainbow color speed: ", dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].textColor.GradientSpeed, 0.05f, 1f, -1);
							}
							dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries[l].textColor.settedColor = dkJGdJpvFP4j4uWN4CyFixyQ;
						}
						bool flag21 = MenuGuiHelper.Button("Remove this text", -1, true, null);
						bool flag22 = flag21;
						if (flag22)
						{
							dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries.RemoveAt(l);
						}
					}
					catch
					{
					}
				}
				bool flag23 = MenuGuiHelper.Button("Add text", -1, true, null);
				bool flag24 = flag23;
				if (flag24)
				{
					dwIoOmLGoIyIvOm9z0SHgcarr.LineEntries.Add(new EspTextEntry(""));
				}
				bool flag25 = MenuGuiHelper.Button("Change font", -1, true, null);
				bool flag26 = flag25;
				if (flag26)
				{
					bool flag27 = FontSelectorWindow.TargetStyleIndex == this.SelectedVisualIndex && FontSelectorWindow.Opened;
					bool flag28 = flag27;
					if (flag28)
					{
						FontSelectorWindow.Opened = false;
					}
					else
					{
						FontSelectorWindow.TargetStyleIndex = this.SelectedVisualIndex;
						FontSelectorWindow.Opened = true;
					}
				}
			}
			GUILayout.EndScrollView();
			EspCategories.categories[this.SelectedVisualIndex] = dwIoOmLGoIyIvOm9z0SHgcarr;
		}
	}
	public int SelectedVisualIndex = 0;
	public static bool ChamVisibilityCheck;
}
