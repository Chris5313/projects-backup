using System;
using System.Collections.Generic;
using SDG.Unturned;
using UnityEngine;

namespace gatyware
{
    public static class ItemSpawner
    {
        public static bool WindowOpen { get { return State.ItemSpawnerOpen; } }
        public static Rect WinRect = new Rect(200f, 100f, 420f, 500f);

        private static List<Entry> _all = new List<Entry>();
        private static List<Entry> _filtered = new List<Entry>();
        private static Vector2 _scroll;
        private static bool _scanned;
        private static int _selectedIdx = -1;
        private static byte _spawnAmount = 1;
        private static string _amountText = "1";

        private struct Entry
        {
            public ushort id;
            public string name;
            public string type;
        }

        private static void ScanItems()
        {
            _scanned = true;
            _all.Clear();
            Asset[] array = Assets.find(EAssetType.ITEM);
            for (int i = 0; i < array.Length; i++)
            {
                ItemAsset itemAsset = array[i] as ItemAsset;
                if (itemAsset != null && !string.IsNullOrEmpty(itemAsset.itemName))
                {
                    _all.Add(new Entry
                    {
                        id = itemAsset.id,
                        name = itemAsset.itemName,
                        type = itemAsset.type.ToString()
                    });
                }
            }
            _all.Sort((Entry a, Entry b) => string.Compare(a.name, b.name));
            ApplyFilter();
        }

        private static void ApplyFilter()
        {
            _filtered.Clear();
            string q = (State.ItemSpawnerSearch ?? "").Trim().ToLower();
            for (int i = 0; i < _all.Count; i++)
            {
                if (string.IsNullOrEmpty(q)
                    || _all[i].name.ToLower().Contains(q)
                    || _all[i].id.ToString().Contains(q)
                    || _all[i].type.ToLower().Contains(q))
                {
                    _filtered.Add(_all[i]);
                }
            }
        }

        private static bool _dragging;
        private static Vector2 _dragOff;

        private static void SpawnToInventory(ushort itemId, byte amount)
        {
            try
            {
                Player lp = Player.player;
                if (lp == null || lp.inventory == null) return;

                for (byte n = 0; n < amount; n++)
                {
                    Item item = new Item(itemId, true);
                    bool added = lp.inventory.tryAddItem(item, auto: true, playEffect: true);
                    if (!added)
                    {
                        ItemManager.dropItem(item, lp.transform.position, false, true, true);
                    }
                }
            }
            catch (Exception ex)
            {
                Runtime.Trace("itemspawn inv: " + ex.Message);
            }
        }

        private static void SpawnToGround(ushort itemId, byte amount)
        {
            try
            {
                Player lp = Player.player;
                if (lp == null) return;

                for (byte n = 0; n < amount; n++)
                {
                    Item item = new Item(itemId, true);
                    ItemManager.dropItem(item, lp.transform.position + lp.transform.forward * 2f, false, true, true);
                }
            }
            catch (Exception ex)
            {
                Runtime.Trace("itemspawn gnd: " + ex.Message);
            }
        }
    }
}