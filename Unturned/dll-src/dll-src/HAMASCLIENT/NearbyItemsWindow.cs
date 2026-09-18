using System;
using System.Collections.Generic;
using SDG.Unturned;
using UnityEngine;
public class NearbyItemsWindow : WindowBase
{
	public override bool GetAviablity()
	{
		return NearbyItemsWindow.opened;
	}
	public override bool IsShowOnMenu()
	{
		return true;
	}
	public override Vector2 GetSize()
	{
		return new Vector2(320f, 400f);
	}
	public override void DrawWindow()
	{
		base.DrawSectionHeader("Nearby items");
		NearbyItemsWindow.findText = GuiAreaState.TextField(NearbyItemsWindow.findText);
		GuiAreaState.BeginScrollView(this.ScrollPosition);
		foreach (List<InteractableItem> list in AutomationBot.nearbyItemsById.Values)
		{
			bool flag = list.Count == 1 && list[0] == null;
			bool flag2 = !flag;
			if (flag2)
			{
				int num = 0;
				string text = "";
				bool flag3 = list.Count == 1;
				bool flag4 = flag3;
				if (flag4)
				{
					num = 1;
					text = list[0].asset.itemName;
					bool flag5 = Vector3.Distance(Player.player.transform.position, list[0].transform.position) > 20f;
					bool flag6 = flag5;
					if (flag6)
					{
						continue;
					}
				}
				else
				{
					foreach (InteractableItem interactableItem in list)
					{
						bool flag7 = interactableItem != null && Vector3.Distance(Player.player.transform.position, interactableItem.transform.position) <= 20f;
						bool flag8 = flag7;
						if (flag8)
						{
							text = list[0].asset.itemName;
							num++;
						}
					}
					bool flag9 = num == 0;
					bool flag10 = flag9;
					if (flag10)
					{
						continue;
					}
				}
				bool flag11 = (string.IsNullOrEmpty(NearbyItemsWindow.findText) || text.ToLower().Contains(NearbyItemsWindow.findText.ToLower())) && GuiAreaState.Button(text + ((num > 1) ? string.Format(" ({0})", num) : ""));
				bool flag12 = flag11;
				if (flag12)
				{
					list[0].use();
				}
			}
		}
		this.ScrollPosition = GuiAreaState.EndScrollView();
		bool flag13 = GuiAreaState.Button("Close");
		bool flag14 = flag13;
		if (flag14)
		{
			NearbyItemsWindow.opened = false;
		}
	}
	[SaveableNameAttribute("NearbyItemsWindow.Opened")]
	public static bool opened = false;
	[SaveableNameAttribute("NearbyItemsWindow.FindText")]
	public static string findText = "";
	public Vector2 ScrollPosition = Vector2.zero;
}
