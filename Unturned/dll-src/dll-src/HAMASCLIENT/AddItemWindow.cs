using System;
using System.Collections.Generic;
using UnityEngine;
public class AddItemWindow : WindowBase
{
	public override bool GetAviablity()
	{
		return AddItemWindow.isActive;
	}
	public override Vector2 GetSize()
	{
		return new Vector2(404f, 404f);
	}
	public override bool IsShowOnMenu()
	{
		return true;
	}
	public static string FormatAssetTypeName(string text)
	{
		string text2 = text.Replace("Asset", "");
		for (int i = 0; i < text2.Length - 1; i++)
		{
			bool flag = i > 0 && text2[i - 1] != ' ' && text2[i] != ' ' && text2[i + 1] != ' ' && char.IsLower(text2[i]) && char.IsUpper(text2[i + 1]);
			bool flag2 = flag;
			if (flag2)
			{
				text2 = text2.Insert(i + 1, " ");
			}
		}
		return text2.ToLower();
	}
	public override void DrawWindow()
	{
		base.DrawSectionHeader("Add item");
		try
		{
			AddItemWindow.findText = GuiAreaState.TextField(AddItemWindow.findText);
			GuiArea currentArea = GuiAreaState.currentArea;
			currentArea.rect.x = currentArea.rect.x + 5f;
			GuiArea currentArea2 = GuiAreaState.currentArea;
			currentArea2.rect.width = currentArea2.rect.width - 10f;
			(EspCategories.categories[1].Options[0].SortSettings as ItemSortConfig).UseCategoryFiltering = GuiAreaState.Toggle((EspCategories.categories[1].Options[0].SortSettings as ItemSortConfig).UseCategoryFiltering, "Is use category filtering");
			int offset = GuiAreaState.currentArea.offset;
			bool drmsWPTdgvBqovH0bGUwaAaU = (EspCategories.categories[1].Options[0].SortSettings as ItemSortConfig).UseCategoryFiltering;
			bool flag = drmsWPTdgvBqovH0bGUwaAaU;
			if (flag)
			{
				GuiAreaState.currentArea.rect.width = 220f;
			}
			bool flag2 = string.IsNullOrEmpty(AddItemWindow.findText);
			bool flag3 = flag2;
			if (flag3)
			{
				GuiAreaState.BeginScrollView(AddItemWindow.itemsScrollPosition);
				for (int i = 0; i < AutomationBot.allItems.Count; i++)
				{
					bool flag4 = GuiAreaState.Button(AutomationBot.allItems[i].name);
					bool flag5 = flag4;
					if (flag5)
					{
						AutomationBot.AddItemToESP(AutomationBot.allItems[i]);
					}
				}
				AddItemWindow.itemsScrollPosition = GuiAreaState.EndScrollView();
			}
			else
			{
				GuiAreaState.BeginScrollView(AddItemWindow.itemsScrollPosition);
				foreach (ItemInfo ddb8pIlWKKbHkw2jCuyAPcvL in AutomationBot.allItems)
				{
					bool flag6 = ddb8pIlWKKbHkw2jCuyAPcvL.name.ToLower().Contains(AddItemWindow.findText.ToLower()) && GuiAreaState.Button(ddb8pIlWKKbHkw2jCuyAPcvL.name);
					bool flag7 = flag6;
					if (flag7)
					{
						AutomationBot.AddItemToESP(ddb8pIlWKKbHkw2jCuyAPcvL);
					}
				}
				AddItemWindow.itemsScrollPosition = GuiAreaState.EndScrollView();
			}
			bool drmsWPTdgvBqovH0bGUwaAaU2 = (EspCategories.categories[1].Options[0].SortSettings as ItemSortConfig).UseCategoryFiltering;
			bool flag8 = drmsWPTdgvBqovH0bGUwaAaU2;
			if (flag8)
			{
				GuiArea currentArea3 = GuiAreaState.currentArea;
				currentArea3.rect.x = currentArea3.rect.x + 230f;
				GuiAreaState.currentArea.offset = offset;
				GuiAreaState.currentArea.rect.width = 160f;
				byte b = 0;
				while ((int)b < AutomationBot.trackedTypes.Count)
				{
					try
					{
						List<Type> dziJVLx1YOES6umDzig25wx8J = (EspCategories.categories[1].Options[0].SortSettings as ItemSortConfig).Categories;
						Type type = AutomationBot.trackedTypes[(int)b];
						bool flag9 = dziJVLx1YOES6umDzig25wx8J.Contains(type);
						flag9 = GuiAreaState.Toggle(flag9, "Filter " + AddItemWindow.FormatAssetTypeName(type.Name));
						bool flag10 = !dziJVLx1YOES6umDzig25wx8J.Contains(type) && flag9;
						bool flag11 = flag10;
						if (flag11)
						{
							dziJVLx1YOES6umDzig25wx8J.Add(type);
						}
						else
						{
							bool flag12 = dziJVLx1YOES6umDzig25wx8J.Contains(type) && !flag9;
							bool flag13 = flag12;
							if (flag13)
							{
								dziJVLx1YOES6umDzig25wx8J.Remove(type);
							}
						}
					}
					catch
					{
					}
					b += 1;
				}
			}
		}
		catch
		{
		}
		bool flag14 = GuiAreaState.Button("Close");
		bool flag15 = flag14;
		if (flag15)
		{
			AddItemWindow.isActive = false;
		}
	}
	[SaveableNameAttribute("AddItemWindow.Opened")]
	public static bool isActive = false;
	[SaveableNameAttribute("AddItemWindow.IsUseFiltering")]
	public static bool isUseFiltering = false;
	[SaveableNameAttribute("AddItemWindow.FindText")]
	public static string findText = "";
	public static Vector2 itemsScrollPosition = Vector2.zero;
}
