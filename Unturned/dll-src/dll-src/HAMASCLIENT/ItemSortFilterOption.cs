using System;
using System.Collections.Generic;
using UnityEngine;
public class ItemSortFilterOption : OptionBase
{
	public ItemSortFilterOption()
	{
		this.SortSettings = new ItemSortConfig();
		ItemSortConfig dx0mI3VF7tws4aOTlUQV9RnyL = this.SortSettings as ItemSortConfig;
		dx0mI3VF7tws4aOTlUQV9RnyL.SortItems = false;
		dx0mI3VF7tws4aOTlUQV9RnyL.IsBlacklist = false;
		dx0mI3VF7tws4aOTlUQV9RnyL.UseCategoryFiltering = false;
		dx0mI3VF7tws4aOTlUQV9RnyL.Items = new Dictionary<ushort, ItemInfo>();
		dx0mI3VF7tws4aOTlUQV9RnyL.Categories = new List<Type>();
	}
	public override void Serialize(PacketWriter writer)
	{
	}
	public override void Deserialize(ByteReader reader)
	{
	}
	public override void DisplayOption()
	{
		ItemSortConfig dx0mI3VF7tws4aOTlUQV9RnyL = this.SortSettings as ItemSortConfig;
		dx0mI3VF7tws4aOTlUQV9RnyL.SortItems = MenuGuiHelper.DrawCheckbox(dx0mI3VF7tws4aOTlUQV9RnyL.SortItems, "Sort items", Array.Empty<GUILayoutOption>());
		bool dkcopsW2kYlTkNiHyikzClfPi = dx0mI3VF7tws4aOTlUQV9RnyL.SortItems;
		bool flag = dkcopsW2kYlTkNiHyikzClfPi;
		if (flag)
		{
			dx0mI3VF7tws4aOTlUQV9RnyL.IsBlacklist = MenuGuiHelper.DrawCheckbox(dx0mI3VF7tws4aOTlUQV9RnyL.IsBlacklist, "Is blacklist", Array.Empty<GUILayoutOption>());
			try
			{
				foreach (ItemInfo ddb8pIlWKKbHkw2jCuyAPcvL in dx0mI3VF7tws4aOTlUQV9RnyL.Items.Values)
				{
					bool flag2 = MenuGuiHelper.Button(ddb8pIlWKKbHkw2jCuyAPcvL.name, -1, true, null);
					bool flag3 = flag2;
					if (flag3)
					{
						AutomationBot.RemoveItemFromESP(ddb8pIlWKKbHkw2jCuyAPcvL.id);
					}
				}
			}
			catch
			{
			}
		}
		bool flag4 = MenuGuiHelper.Button("Manage sort items", -1, true, null);
		bool flag5 = flag4;
		if (flag5)
		{
			AddItemWindow.isActive = !AddItemWindow.isActive;
		}
	}
}
