using System;
using System.Collections.Generic;
using System.Reflection;
using SDG.Unturned;
using UnityEngine;

namespace gatyware
{
	public static class StorageViewer
	{
		public static string StatusText = "";
		private static List<StorageEntry> _items = new List<StorageEntry>();
		private static string _ownerName = "";
		private static string _groupName = "";
		private static string _storageName = "";
		private static float _showTime;
		private static bool _visible;
		private static Vector2 _scroll;

		private struct StorageEntry
		{
			public string name;
			public byte amount;
			public ushort id;
		}

		public static void Update()
		{
			if (!State.StorageViewerOn) return;
			if (Player.player == null) return;

			// Fade out after 15 seconds
			if (_visible && Time.time - _showTime > 15f)
			{
				_visible = false;
				_items.Clear();
			}

			// Detect punch input (left click with fists equipped)
			if (!Input.GetMouseButtonDown(0)) return;

			try
			{
				PlayerEquipment eq = Player.player.equipment;
				if (eq == null) return;

				// Check if player has no weapon equipped (fists) or melee
				bool isMelee = false;
				if (eq.asset == null)
					isMelee = true;
				else if (eq.asset is ItemMeleeAsset)
					isMelee = true;

				if (!isMelee) return;

				Camera cam = (FreeCam.FreeCamCamera != null) ? FreeCam.FreeCamCamera : MainCamera.instance;
				if (cam == null) cam = Camera.main;
				if (cam == null) return;

				RaycastHit hit;
				if (!Physics.Raycast(new Ray(cam.transform.position, cam.transform.forward), out hit, 8f, RayMasks.BARRICADE_INTERACT, QueryTriggerInteraction.Collide))
					return;

				InteractableStorage storage = hit.transform.GetComponent<InteractableStorage>();
				if (storage == null)
					storage = hit.transform.GetComponentInParent<InteractableStorage>();
				if (storage == null) return;

				ScanStorage(storage, hit.transform);
			}
			catch (Exception ex)
			{
				Runtime.Trace("storeviewer err: " + ex.Message);
			}
		}

		private static void ScanStorage(InteractableStorage storage, Transform t)
		{
			_items.Clear();
			_ownerName = "?";
			_groupName = "?";
			_storageName = "Storage";

			try
			{
				// Get name from asset
				BarricadeDrop drop = FindDrop(t);
				if (drop != null && drop.asset != null)
					_storageName = drop.asset.itemName;

				// Get owner/group
				BarricadeData data = GetData(drop);
				if (data != null)
				{
					_ownerName = ResolveOwner(data.owner);
					_groupName = data.group != 0 ? data.group.ToString() : "None";
				}

				// Read items via reflection
				Items items = GetStorageItems(storage);
				if (items != null)
				{
					byte pageCount = items.getItemCount();
					for (byte i = 0; i < pageCount; i++)
					{
						ItemJar jar = items.getItem(i);
						if (jar == null || jar.item == null) continue;
						ItemAsset asset = Assets.find(EAssetType.ITEM, jar.item.id) as ItemAsset;
						_items.Add(new StorageEntry
						{
							name = asset != null ? asset.itemName : "ID:" + jar.item.id.ToString(),
							amount = jar.item.amount,
							id = jar.item.id
						});
					}
				}
			}
			catch (Exception ex)
			{
				Runtime.Trace("storeviewer scan: " + ex.Message);
			}

			_showTime = Time.time;
			_visible = true;
			StatusText = _storageName + " - " + _items.Count + " items";
		}

		private static BarricadeDrop FindDrop(Transform t)
		{
			if (BarricadeManager.regions == null) return null;
			for (int i = 0; i < BarricadeManager.regions.GetLength(0); i++)
			{
				for (int j = 0; j < BarricadeManager.regions.GetLength(1); j++)
				{
					BarricadeRegion region = BarricadeManager.regions[i, j];
					if (region == null || region.drops == null) continue;
					for (int k = 0; k < region.drops.Count; k++)
					{
						BarricadeDrop d = region.drops[k];
						if (d != null && d.model != null && d.model == t) return d;
						if (d != null && d.model != null && t.IsChildOf(d.model)) return d;
					}
				}
			}
			return null;
		}

		private static BarricadeData GetData(BarricadeDrop drop)
		{
			try
			{
				FieldInfo fi = typeof(BarricadeDrop).GetField("serversideData", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (fi != null) return fi.GetValue(drop) as BarricadeData;
				PropertyInfo pi = typeof(BarricadeDrop).GetProperty("serversideData", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (pi != null) return pi.GetValue(drop, null) as BarricadeData;
				fi = typeof(BarricadeDrop).GetField("_data", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (fi != null) return fi.GetValue(drop) as BarricadeData;
			}
			catch { }
			return null;
		}

		private static Items GetStorageItems(InteractableStorage storage)
		{
			try
			{
				FieldInfo fi = typeof(InteractableStorage).GetField("items", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (fi != null) return fi.GetValue(storage) as Items;
			}
			catch { }
			return null;
		}

		private static string ResolveOwner(ulong steamId)
		{
			if (steamId == 0UL) return "None";
			try
			{
				if (Provider.clients != null)
				{
					for (int i = 0; i < Provider.clients.Count; i++)
					{
						if (Provider.clients[i].playerID.steamID.m_SteamID == steamId)
							return Provider.clients[i].playerID.characterName + " (" + steamId + ")";
					}
				}
			}
			catch { }
			return steamId.ToString();
		}
	}
}
