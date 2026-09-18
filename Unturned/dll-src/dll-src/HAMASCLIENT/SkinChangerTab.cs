using System;
using System.Collections.Generic;
using SDG.Unturned;
using UnityEngine;
public class SkinChangerTab : FeatureTabBase
{
	public override string GetName()
	{
		return "Skin changer";
	}
	public override void DoTab(TabCount tc)
	{
		bool flag = tc == TabCount.One;
		if (flag)
		{
			foreach (ClothingCategory dnFbkaUD6mnA1BajCnfh8ztdO in SkinsManager.SkinCatalog.Keys)
			{
				bool flag2 = MenuGuiHelper.Button(dnFbkaUD6mnA1BajCnfh8ztdO.ToString(), -1, true, null);
				if (flag2)
				{
					this.selectedCategory = dnFbkaUD6mnA1BajCnfh8ztdO;
				}
			}
			bool flag3 = MenuGuiHelper.Button("Clear skins", -1, true, null);
			if (flag3)
			{
				SkinsManager.SelectedSkins.Clear();
				bool flag4 = Player.player != null;
				if (flag4)
				{
					Player.player.clothing.firstClothes.visualHat = 0;
					Player.player.clothing.firstClothes.visualMask = 0;
					Player.player.clothing.firstClothes.visualGlasses = 0;
					Player.player.clothing.firstClothes.visualVest = 0;
					Player.player.clothing.firstClothes.visualBackpack = 0;
					Player.player.clothing.firstClothes.visualShirt = 0;
					Player.player.clothing.firstClothes.visualPants = 0;
					Player.player.clothing.thirdClothes.visualHat = 0;
					Player.player.clothing.thirdClothes.visualMask = 0;
					Player.player.clothing.thirdClothes.visualGlasses = 0;
					Player.player.clothing.thirdClothes.visualVest = 0;
					Player.player.clothing.thirdClothes.visualBackpack = 0;
					Player.player.clothing.thirdClothes.visualShirt = 0;
					Player.player.clothing.thirdClothes.visualPants = 0;
					Player.player.clothing.characterClothes.visualHat = 0;
					Player.player.clothing.characterClothes.visualMask = 0;
					Player.player.clothing.characterClothes.visualGlasses = 0;
					Player.player.clothing.characterClothes.visualVest = 0;
					Player.player.clothing.characterClothes.visualBackpack = 0;
					Player.player.clothing.characterClothes.visualShirt = 0;
					Player.player.clothing.characterClothes.visualPants = 0;
					Player.player.clothing.firstClothes.apply();
					Player.player.clothing.thirdClothes.apply();
					Player.player.clothing.characterClothes.apply();
				}
			}
		}
		else
		{
			bool flag5 = SkinsManager.SkinCatalog.ContainsKey(this.selectedCategory);
			if (flag5)
			{
				GuiAreaState.PushArea(MenuGuiHelper.LastPanelContentRect);
				try
				{
					GUILayout.Space(20f);
					SkinChangerTab.SearchText = GUILayout.TextField(SkinChangerTab.SearchText, Array.Empty<GUILayoutOption>());
					GuiAreaState.currentArea.offset += 20 + (int)GUI.skin.textField.fixedHeight + 4;
					SkinInfo[] array = SkinsManager.SkinCatalog[this.selectedCategory];
					bool flag6 = !string.IsNullOrEmpty(SkinChangerTab.SearchText);
					string text = (flag6 ? SkinChangerTab.SearchText.ToLower() : null);
					string[] array2 = SkinChangerTab.GetOrBuildLowercaseNames(this.selectedCategory, array);
					bool flag7 = this.DlastSearchText != SkinChangerTab.SearchText || this.DlastCategory != this.selectedCategory || this.DlastAllSkins != array;
					if (flag7)
					{
						this.DfilteredIndices.Clear();
						for (int i = 0; i < array.Length; i++)
						{
							bool flag8 = !flag6 || array2[i].Contains(text);
							if (flag8)
							{
								this.DfilteredIndices.Add(i);
							}
						}
						this.DlastSearchText = SkinChangerTab.SearchText;
						this.DlastCategory = this.selectedCategory;
						this.DlastAllSkins = array;
					}
					int count = this.DfilteredIndices.Count;
					float num = 0f;
					float width = GuiAreaState.currentArea.rect.width;
					float num2 = (float)GuiAreaState.currentArea.offset;
					float num3 = GuiAreaState.currentArea.rect.height - num2 - 10f;
					num3 = Mathf.Max(num3, 100f);
					float num4 = 20f;
					float num5 = (float)count * num4;
					Rect rect = new Rect(num, num2, width, num3);
					Rect rect2 = new Rect(0f, 0f, width - 16f, num5);
					bool flag9 = flag6;
					Vector2 vector;
					if (flag9)
					{
						vector = this.searchScrollPosition;
					}
					else
					{
						bool flag10 = !this.categoryScrollPositions.ContainsKey(this.selectedCategory);
						if (flag10)
						{
							this.categoryScrollPositions[this.selectedCategory] = Vector2.zero;
						}
						vector = this.categoryScrollPositions[this.selectedCategory];
					}
					vector = GUI.BeginScrollView(rect, vector, rect2);
					int num6 = Mathf.Max(0, Mathf.FloorToInt(vector.y / num4) - 1);
					int num7 = Mathf.Min(count, Mathf.CeilToInt((vector.y + num3) / num4) + 1);
					for (int j = num6; j < num7; j++)
					{
						int num8 = this.DfilteredIndices[j];
						Rect rect3 = new Rect(0f, (float)j * num4, width - 16f, num4);
						bool flag11 = MenuGuiHelper.RectButton(rect3, array[num8].ItemName, -1, true);
						if (flag11)
						{
							SkinsManager.SetSelectedSkin(array[num8]);
						}
					}
					GUI.EndScrollView();
					bool flag12 = flag6;
					if (flag12)
					{
						this.searchScrollPosition = vector;
					}
					else
					{
						this.categoryScrollPositions[this.selectedCategory] = vector;
					}
					GuiAreaState.currentArea.offset += (int)num3;
				}
				finally
				{
					GuiAreaState.PopArea();
				}
			}
			else
			{
				GUILayout.Label("Select a category from the left tab", GuiStyles.SmallBoldGrayLabelStyle, Array.Empty<GUILayoutOption>());
			}
		}
	}
	private static string[] GetOrBuildLowercaseNames(ClothingCategory category, SkinInfo[] skins)
	{
		string[] array;
		bool flag = !SkinChangerTab.DlowerCaseNamesCache.TryGetValue(category, out array) || array.Length != skins.Length;
		if (flag)
		{
			array = new string[skins.Length];
			for (int i = 0; i < skins.Length; i++)
			{
				array[i] = skins[i].ItemName.ToLower();
			}
			SkinChangerTab.DlowerCaseNamesCache[category] = array;
		}
		return array;
	}
	public Dictionary<ClothingCategory, Vector2> categoryScrollPositions = new Dictionary<ClothingCategory, Vector2>();
	public Vector2 searchScrollPosition = Vector2.zero;
	public ClothingCategory selectedCategory;
	public static string SearchText = "";
	private List<int> DfilteredIndices = new List<int>();
	private string DlastSearchText = null;
	private ClothingCategory DlastCategory;
	private SkinInfo[] DlastAllSkins;
	private static Dictionary<ClothingCategory, string[]> DlowerCaseNamesCache = new Dictionary<ClothingCategory, string[]>();
}
