using System;
using System.Collections.Generic;
using SDG.Unturned;
using UnityEngine;

namespace gatyware
{
	public static class AutoPickup
	{
		public static Rect WinRect
		{
			get
			{
				return AutoPickup._winRect;
			}
		}

		private static void ScanItems()
		{
			AutoPickup._scanned = true;
			AutoPickup._all.Clear();
			Asset[] array = Assets.find((EAssetType)1);
			for (int i = 0; i < array.Length; i++)
			{
				ItemAsset itemAsset = array[i] as ItemAsset;
				if (itemAsset != null && !string.IsNullOrEmpty(itemAsset.itemName))
				{
					AutoPickup._all.Add(new AutoPickup.Entry
					{
						id = itemAsset.id,
						name = itemAsset.itemName,
						type = itemAsset.type.ToString()
					});
				}
			}
			AutoPickup._all.Sort((AutoPickup.Entry a, AutoPickup.Entry b) => string.Compare(a.name, b.name));
			AutoPickup.ApplyFilter();
		}

		private static void ApplyFilter()
		{
			AutoPickup._filtered.Clear();
			string text = AutoPickup._search.ToLower();
			for (int i = 0; i < AutoPickup._all.Count; i++)
			{
				AutoPickup.Entry entry = AutoPickup._all[i];
				if (text.Length <= 0 || entry.name.ToLower().Contains(text) || entry.id.ToString().Contains(text))
				{
					AutoPickup._filtered.Add(entry);
				}
			}
			int dir = AutoPickup._sortAsc ? 1 : -1;
			switch (AutoPickup._sortCol)
			{
			case 0:
				AutoPickup._filtered.Sort((AutoPickup.Entry a, AutoPickup.Entry b) => dir * string.Compare(a.name, b.name));
				return;
			case 1:
				AutoPickup._filtered.Sort((AutoPickup.Entry a, AutoPickup.Entry b) => dir * string.Compare(a.type, b.type));
				return;
			case 2:
				AutoPickup._filtered.Sort((AutoPickup.Entry a, AutoPickup.Entry b) => dir * a.id.CompareTo(b.id));
				return;
			default:
				return;
			}
		}

		public static void Update()
		{
			if (!State.AutoPickupOn || State.IsSpying)
			{
				return;
			}
			if (AutoPickup.Selected.Count == 0)
			{
				return;
			}
			Player localPlayer = Player.LocalPlayer;
			if (localPlayer == null)
			{
				return;
			}
			if (Time.realtimeSinceStartup - AutoPickup._lastPickup < State.AutoPickupSpeed)
			{
				return;
			}
			AutoPickup._lastPickup = Time.realtimeSinceStartup;
			List<InteractableItem> clampedItems = ItemManager.clampedItems;
			if (clampedItems == null)
			{
				return;
			}
			Vector3 position = localPlayer.transform.position;
			float num = State.AutoPickupDist * State.AutoPickupDist;
			for (int i = 0; i < clampedItems.Count; i++)
			{
				InteractableItem interactableItem = clampedItems[i];
				if (!(interactableItem == null) && interactableItem.asset != null && AutoPickup.Selected.ContainsKey(interactableItem.asset.id) && (interactableItem.transform.position - position).sqrMagnitude <= num && (!State.AutoPickupSkipEmpty || !(interactableItem.asset is ItemMagazineAsset) || interactableItem.item == null || interactableItem.item.amount != 0))
				{
					interactableItem.use();
				}
			}
		}


		private static void AddInventoryItems()
		{
			Player localPlayer = Player.LocalPlayer;
			if (((localPlayer != null) ? localPlayer.inventory : null) == null)
			{
				return;
			}
			for (byte b = 0; b < PlayerInventory.PAGES - 2; b += 1)
			{
				Items items = localPlayer.inventory.items[(int)b];
				if (items != null)
				{
					foreach (ItemJar itemJar in items.items)
					{
						if (itemJar != null)
						{
							ItemAsset itemAsset = Assets.find((EAssetType)1, itemJar.item.id) as ItemAsset;
							if (itemAsset != null && !AutoPickup.Selected.ContainsKey(itemAsset.id))
							{
								AutoPickup.Selected[itemAsset.id] = itemAsset.itemName;
							}
						}
					}
				}
			}
		}

		public static void Reset()
		{
			AutoPickup._scanned = false;
			AutoPickup._all.Clear();
			AutoPickup._filtered.Clear();
		}

		public static bool WindowOpen;

		public static Dictionary<ushort, string> Selected = new Dictionary<ushort, string>();

		private static List<AutoPickup.Entry> _all = new List<AutoPickup.Entry>();

		private static List<AutoPickup.Entry> _filtered = new List<AutoPickup.Entry>();

		private static bool _scanned;

		private static string _search = "";

		private static Vector2 _scroll;

		private static Rect _winRect = new Rect(220f, 100f, 500f, 540f);

		private static bool _dragging;

		private static Vector2 _dragOff;

		private static int _sortCol;

		private static bool _sortAsc = true;

		private static float _lastPickup;

		private struct Entry
		{
			public ushort id;

			public string name;

			public string type;
		}
	}
}
