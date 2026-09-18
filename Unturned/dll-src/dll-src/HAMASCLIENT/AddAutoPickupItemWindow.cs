using System;
using UnityEngine;
public class AddAutoPickupItemWindow : WindowBase
{
	public override bool GetAviablity()
	{
		return AddAutoPickupItemWindow.isActive;
	}
	public override Vector2 GetSize()
	{
		return new Vector2(320f, 400f);
	}
	public override bool IsShowOnMenu()
	{
		return true;
	}
	public override void DrawWindow()
	{
		base.DrawSectionHeader("Add item to auto pickup");
		AddAutoPickupItemWindow.findText = GuiAreaState.TextField(AddAutoPickupItemWindow.findText);
		bool flag = string.IsNullOrEmpty(AddAutoPickupItemWindow.findText);
		bool flag2 = flag;
		if (flag2)
		{
			GuiAreaState.BeginScrollView(AddAutoPickupItemWindow.scrollPosition);
			for (int i = 0; i < AutomationBot.allItems.Count; i++)
			{
				bool flag3 = GuiAreaState.Button(AutomationBot.allItems[i].name) && !MiscConfig.AutoPickupWhitelist.ContainsKey(AutomationBot.allItems[i].id);
				bool flag4 = flag3;
				if (flag4)
				{
					MiscConfig.AutoPickupWhitelist.Add(AutomationBot.allItems[i].id, AutomationBot.allItems[i]);
				}
			}
			AddAutoPickupItemWindow.scrollPosition = GuiAreaState.EndScrollView();
		}
		else
		{
			GuiAreaState.BeginScrollView(AddAutoPickupItemWindow.scrollPosition);
			foreach (ItemInfo ddb8pIlWKKbHkw2jCuyAPcvL in AutomationBot.allItems)
			{
				bool flag5 = ddb8pIlWKKbHkw2jCuyAPcvL.name.ToLower().Contains(AddAutoPickupItemWindow.findText.ToLower()) && GuiAreaState.Button(ddb8pIlWKKbHkw2jCuyAPcvL.name) && !MiscConfig.AutoPickupWhitelist.ContainsKey(ddb8pIlWKKbHkw2jCuyAPcvL.id);
				bool flag6 = flag5;
				if (flag6)
				{
					MiscConfig.AutoPickupWhitelist.Add(ddb8pIlWKKbHkw2jCuyAPcvL.id, ddb8pIlWKKbHkw2jCuyAPcvL);
				}
			}
			AddAutoPickupItemWindow.scrollPosition = GuiAreaState.EndScrollView();
		}
		bool flag7 = GuiAreaState.Button("Close");
		bool flag8 = flag7;
		if (flag8)
		{
			AddAutoPickupItemWindow.isActive = false;
		}
	}
	[SaveableNameAttribute("AutoPickupItemWindow.Opened")]
	public static bool isActive = false;
	[SaveableNameAttribute("AutoPickupItemWindow.FindText")]
	public static string findText = "";
	public static Vector2 scrollPosition = Vector2.zero;
}
