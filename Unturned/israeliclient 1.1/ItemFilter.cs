using System;
using System.Collections.Generic;
using SDG.Unturned;
using UnityEngine;

namespace gatyware
{
	public static class ItemFilter
	{
		public static Rect WinRect
		{
			get
			{
				return ItemFilter._winRect;
			}
		}

		public static void ScanItems()
		{
			if (ItemFilter._scanned)
			{
				return;
			}
			ItemFilter._scanned = true;
			ItemFilter._all.Clear();
			try
			{
				for (ushort num = 0; num < 65535; num += 1)
				{
					try
					{
						ItemAsset itemAsset = Assets.find((EAssetType)1, num) as ItemAsset;
						if (itemAsset != null)
						{
							string itemName = itemAsset.itemName;
							if (!string.IsNullOrEmpty(itemName) && !(itemName.ToLower() == "name"))
							{
								ItemFilter._all.Add(new ItemFilter.Entry
								{
									id = itemAsset.id,
									name = itemName,
									type = itemAsset.type.ToString(),
									rarity = itemAsset.rarity.ToString()
								});
							}
						}
					}
					catch
					{
					}
				}
			}
			catch
			{
			}
			ItemFilter.ApplyFilter();
		}

		private static void ApplyFilter()
		{
			ItemFilter._filtered.Clear();
			string text = (ItemFilter._typeFilter > 0) ? ItemFilter._typeNames[ItemFilter._typeFilter] : null;
			string text2 = ItemFilter._search.ToLower();
			for (int i = 0; i < ItemFilter._all.Count; i++)
			{
				ItemFilter.Entry entry = ItemFilter._all[i];
				if ((text == null || !(entry.type != text)) && (text2.Length <= 0 || entry.name.ToLower().Contains(text2) || entry.id.ToString().Contains(text2)))
				{
					ItemFilter._filtered.Add(entry);
				}
			}
		}

		public static bool Passes(ushort id)
		{
			return !ItemFilter.FilterEnabled || ItemFilter.Selected.Count == 0 || ItemFilter.Selected.ContainsKey(id);
		}

		private static Vector2 LocalMouse(Event ev)
		{
			return ev.mousePosition;
		}


		private static void AddInventoryItems()
		{
			try
			{
				Player localPlayer = Player.LocalPlayer;
				if (!(((localPlayer != null) ? localPlayer.inventory : null) == null))
				{
					for (byte b = 0; b < PlayerInventory.PAGES - 2; b += 1)
					{
						Items items = localPlayer.inventory.items[(int)b];
						if (items != null)
						{
							for (byte b2 = 0; b2 < items.getItemCount(); b2 += 1)
							{
								ItemJar item = items.getItem(b2);
								if (((item != null) ? item.item : null) != null)
								{
									ItemAsset itemAsset = Assets.find((EAssetType)1, item.item.id) as ItemAsset;
									if (itemAsset != null && !ItemFilter.Selected.ContainsKey(itemAsset.id))
									{
										ItemFilter.Selected[itemAsset.id] = itemAsset.itemName;
									}
								}
							}
						}
					}
				}
			}
			catch
			{
			}
		}

		public static void Reset()
		{
			ItemFilter._scanned = false;
			ItemFilter._all.Clear();
			ItemFilter._filtered.Clear();
		}

		public static bool WindowOpen;

		public static bool FilterEnabled;

		public static Dictionary<ushort, string> Selected = new Dictionary<ushort, string>();

		private static List<ItemFilter.Entry> _all = new List<ItemFilter.Entry>();

		private static List<ItemFilter.Entry> _filtered = new List<ItemFilter.Entry>();

		private static bool _scanned;

		private static string _search = "";

		private static Vector2 _scroll;

		private static Rect _winRect = new Rect(200f, 80f, 500f, 460f);

		private static bool _dragging;

		private static Vector2 _dragOff;

		private static string[] _typeNames = new string[]
		{
			"All",
			"GUN",
			"MELEE",
			"MEDICAL",
			"FOOD",
			"WATER",
			"BACKPACK",
			"VEST",
			"SHIRT",
			"PANTS",
			"HAT",
			"MASK",
			"GLASSES",
			"MAGAZINE",
			"SIGHT",
			"BARREL",
			"GRIP",
			"TACTICAL",
			"THROWABLE",
			"SUPPLY",
			"FUEL",
			"TOOL",
			"BARRICADE",
			"STRUCTURE",
			"STORAGE"
		};

		private static int _typeFilter;

		private struct Entry
		{
			public ushort id;

			public string name;

			public string type;

			public string rarity;
		}
	}
}
