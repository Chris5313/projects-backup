using System;
using System.Collections.Generic;
using System.Reflection;
using SDG.Unturned;
using UnityEngine;

namespace gatyware
{
	public static class AutoFarm
	{
		public class StorageEntry
		{
			public InteractableStorage storage;
			public Vector3 position;
			public string name;
		}

		public static List<StorageEntry> Storages = new List<StorageEntry>();
		public static bool AddingStorage;
		public static bool WindowOpen;
		public static string StatusText = "";

		public static Rect WinRect
		{
			get { return AutoFarm._winRect; }
		}

		private static float _lastAction;
		private static bool _reflected;

		private static MethodInfo _askFarm;
		private static MethodInfo _sendHarvest;
		private static MethodInfo _clientHarvest;
		private static MethodInfo _sendInteract;
		private static MethodInfo _askStoreStorage;
		private static MethodInfo _askCraft;
		private static MethodInfo _sendCraft;
		private static PropertyInfo _recipesProp;
		private static PropertyInfo _isFullyGrownProp;
		private static PropertyInfo _plantedProp;
		private static PropertyInfo _growthProp;
		private static PropertyInfo _farmAssetProp;
		private static PropertyInfo _farmAssetFruitProp;
		private static FieldInfo _piHit;
		private static FieldInfo _piLastInteract;
		private static FieldInfo _piInteractable;
		private static bool _interactableResolved;

		private static void Reflect()
		{
			if (AutoFarm._reflected) return;
			AutoFarm._reflected = true;
			BindingFlags bf = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			BindingFlags sbf = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
			try { AutoFarm._askFarm = typeof(InteractableFarm).GetMethod("askFarm", bf); } catch { }
			try { AutoFarm._sendHarvest = typeof(InteractableFarm).GetMethod("SendHarvestRequest", sbf); } catch { }
			try { AutoFarm._clientHarvest = typeof(InteractableFarm).GetMethod("ClientHarvest", bf | sbf); } catch { }
			if (AutoFarm._clientHarvest == null)
				try { AutoFarm._clientHarvest = typeof(InteractableFarm).GetMethod("ClientInteract", bf); } catch { }
			if (AutoFarm._clientHarvest == null)
				try { AutoFarm._clientHarvest = typeof(InteractableFarm).GetMethod("ClientInteract", sbf); } catch { }
			try { AutoFarm._sendInteract = typeof(PlayerInteract).GetMethod("sendInteract", bf | sbf); } catch { }
			if (AutoFarm._sendInteract == null)
				try { AutoFarm._sendInteract = typeof(PlayerInteract).GetMethod("SendInteractRequest", bf | sbf); } catch { }
			if (AutoFarm._sendInteract == null)
				try { AutoFarm._sendInteract = typeof(PlayerInteract).GetMethod("ClientInteract", bf); } catch { }
			try { AutoFarm._askStoreStorage = typeof(InteractableStorage).GetMethod("askStoreStorage", bf); } catch { }
			if (AutoFarm._askStoreStorage == null)
				try { AutoFarm._askStoreStorage = typeof(InteractableStorage).GetMethod("ClientInteract", bf | sbf); } catch { }
			try { AutoFarm._askCraft = typeof(PlayerCrafting).GetMethod("askCraft", bf); } catch { }
			try { AutoFarm._sendCraft = typeof(PlayerCrafting).GetMethod("sendCraft", bf); } catch { }
			if (AutoFarm._sendCraft == null)
				try { AutoFarm._sendCraft = typeof(PlayerCrafting).GetMethod("SendCraftRequest", bf); } catch { }
			if (AutoFarm._sendCraft == null)
				try { AutoFarm._sendCraft = typeof(PlayerCrafting).GetMethod("ServerCraftRequest", bf); } catch { }
			try { AutoFarm._recipesProp = typeof(PlayerCrafting).GetProperty("recipes", bf); } catch { }
			try { AutoFarm._isFullyGrownProp = typeof(InteractableFarm).GetProperty("IsFullyGrown", bf); } catch { }
			try { AutoFarm._plantedProp = typeof(InteractableFarm).GetProperty("planted", bf); } catch { }
			try { AutoFarm._growthProp = typeof(InteractableFarm).GetProperty("growth", bf); } catch { }
			try { AutoFarm._farmAssetProp = typeof(InteractableFarm).GetProperty("farmAsset", bf); } catch { }
			if (AutoFarm._farmAssetProp != null)
			{
				try { AutoFarm._farmAssetFruitProp = AutoFarm._farmAssetProp.PropertyType.GetProperty("fruitID", bf); } catch { }
				if (AutoFarm._farmAssetFruitProp == null)
					try { AutoFarm._farmAssetFruitProp = AutoFarm._farmAssetProp.PropertyType.GetProperty("fruit", bf); } catch { }
			}
			try { AutoFarm._piHit = typeof(PlayerInteract).GetField("hit", BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); } catch { }
			try { AutoFarm._piLastInteract = typeof(PlayerInteract).GetField("lastInteract", BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); } catch { }
			if (AutoFarm._piLastInteract == null)
				try { AutoFarm._piLastInteract = typeof(PlayerInteract).GetField("<lastInteract>k__BackingField", BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); } catch { }
			if (!AutoFarm._interactableResolved)
			{
				AutoFarm._interactableResolved = true;
				BindingFlags anyBf = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
				try { AutoFarm._piInteractable = typeof(PlayerInteract).GetField("interactable", anyBf); } catch { }
				if (AutoFarm._piInteractable == null)
					try { AutoFarm._piInteractable = typeof(PlayerInteract).GetField("<interactable>k__BackingField", anyBf); } catch { }
			}
		}

		private static ushort _lastCraftItemID;

		public static void Update()
		{
			if (!State.AutoFarmOn || State.IsSpying)
			{
				if (!State.AutoFarmOn) AutoFarm.StatusText = "AutoFarm disabled";
				else if (State.IsSpying) AutoFarm.StatusText = "Spying - paused";
				return;
			}
			Player player = Player.LocalPlayer;
			if (player == null)
			{
				AutoFarm.StatusText = "No player";
				return;
			}
			AutoFarm.Reflect();

			// Invalidate craft cache when seed ID changes
			if (State.AutoFarmCraftItemID != AutoFarm._lastCraftItemID)
			{
				AutoFarm._lastCraftItemID = State.AutoFarmCraftItemID;
				AutoFarm._craftCacheBuilt = false;
			}

			if (AutoFarm.AddingStorage)
				AutoFarm.TryAddStorageFromLook();

			if (Time.realtimeSinceStartup - AutoFarm._lastAction < State.AutoFarmActionDelay) return;

			if (AutoFarm.TryHarvestNearbyFarm(player))
			{
				AutoFarm._lastAction = Time.realtimeSinceStartup;
				return;
			}
			if (State.AutoFarmAutoCraft && State.AutoFarmCraftItemID != 0)
			{
				if (AutoFarm.TryCraftItem(player))
				{
					AutoFarm._lastAction = Time.realtimeSinceStartup;
					return;
				}
				AutoFarm.StatusText = "Craft failed - check seed ID";
			}
			else if (State.AutoFarmAutoCraft && State.AutoFarmCraftItemID == 0)
			{
				AutoFarm.StatusText = "Set seed item ID in menu";
			}
			if (State.AutoFarmAutoReplant)
			{
				if (AutoFarm.TryReplantNearbyFarm(player))
				{
					AutoFarm._lastAction = Time.realtimeSinceStartup;
					return;
				}
			}
			if (State.AutoFarmWaterOn && State.AutoFarmWaterItemID != 0)
			{
				if (AutoFarm.TryWaterNearbyFarm(player))
				{
					AutoFarm._lastAction = Time.realtimeSinceStartup;
					return;
				}
				AutoFarm.StatusText = "Water failed - check water item ID";
			}
			else if (State.AutoFarmWaterOn && State.AutoFarmWaterItemID == 0)
			{
				AutoFarm.StatusText = "Set water item ID in menu";
			}
			if (State.AutoFarmAutoStore && AutoFarm.Storages.Count > 0)
			{
				if (AutoFarm.TryStoreItems(player))
				{
					AutoFarm._lastAction = Time.realtimeSinceStartup;
					return;
				}
				AutoFarm.StatusText = "Store failed - add storage";
			}
			else if (State.AutoFarmAutoStore && AutoFarm.Storages.Count == 0)
			{
				AutoFarm.StatusText = "Add storage in menu";
			}
			if (AutoFarm.StatusText == "" || AutoFarm.StatusText == "Idle")
				AutoFarm.StatusText = "No farms nearby";
		}

		// Draw status text in StreamProof mode
		private static void TryAddStorageFromLook()
		{
			Camera cam = (FreeCam.FreeCamCamera != null) ? FreeCam.FreeCamCamera : MainCamera.instance;
			if (cam == null) return;
			RaycastHit hit;
			if (!Physics.Raycast(new Ray(cam.transform.position, cam.transform.forward), out hit, 12f, RayMasks.BARRICADE_INTERACT, (QueryTriggerInteraction)1))
				return;
			InteractableStorage storage = hit.transform.GetComponent<InteractableStorage>();
			if (storage == null) return;
			for (int i = 0; i < AutoFarm.Storages.Count; i++)
			{
				if (AutoFarm.Storages[i].storage == storage)
				{
					AutoFarm.AddingStorage = false;
					return;
				}
			}
			string name = "Storage";
			try
			{
				if (BarricadeManager.regions != null)
				{
					for (int i = 0; i < BarricadeManager.regions.GetLength(0); i++)
					{
						for (int j = 0; j < BarricadeManager.regions.GetLength(1); j++)
						{
							BarricadeRegion reg = BarricadeManager.regions[i, j];
							if (reg?.drops == null) continue;
							for (int k = 0; k < reg.drops.Count; k++)
							{
								if (reg.drops[k]?.model == hit.transform)
								{
									if (reg.drops[k].asset != null)
										name = reg.drops[k].asset.itemName;
									goto found;
								}
							}
						}
					}
				}
				found:;
			}
			catch { }
			AutoFarm.Storages.Add(new AutoFarm.StorageEntry
			{
				storage = storage,
				position = hit.transform.position,
				name = name
			});
			AutoFarm.AddingStorage = false;
		}

		public static void RemoveStorage(int index)
		{
			if (index >= 0 && index < AutoFarm.Storages.Count)
				AutoFarm.Storages.RemoveAt(index);
		}

		public static void ClearStorages()
		{
			AutoFarm.Storages.Clear();
		}

		// ── Farm Scanning ──

		private static bool GetIsFullyGrown(InteractableFarm farm)
		{
			try
			{
				if (AutoFarm._isFullyGrownProp != null)
					return (bool)AutoFarm._isFullyGrownProp.GetValue(farm, null);
				return farm.IsFullyGrown;
			}
			catch { return false; }
		}

		private static bool GetPlanted(InteractableFarm farm)
		{
			try
			{
				if (AutoFarm._plantedProp != null)
					return (bool)AutoFarm._plantedProp.GetValue(farm, null);
				return false;
			}
			catch { return true; }
		}

		private static float GetGrowth(InteractableFarm farm)
		{
			try
			{
				if (AutoFarm._growthProp != null)
					return (float)AutoFarm._growthProp.GetValue(farm, null);
				return 0f;
			}
			catch { return 0f; }
		}

		private static ushort GetFruitID(InteractableFarm farm)
		{
			try
			{
				if (AutoFarm._farmAssetProp != null && AutoFarm._farmAssetFruitProp != null)
				{
					object farmAsset = AutoFarm._farmAssetProp.GetValue(farm, null);
					if (farmAsset != null)
						return (ushort)AutoFarm._farmAssetFruitProp.GetValue(farmAsset, null);
				}
			}
			catch { }
			return 0;
		}

		private static void ScanFarms(Vector3 center, float radius, List<InteractableFarm> grown, List<InteractableFarm> empty)
		{
			float sqr = radius * radius;
			if (BarricadeManager.regions == null) return;
			for (int i = 0; i < BarricadeManager.regions.GetLength(0); i++)
			{
				for (int j = 0; j < BarricadeManager.regions.GetLength(1); j++)
				{
					BarricadeRegion region = BarricadeManager.regions[i, j];
					if (region?.drops == null) continue;
					for (int k = 0; k < region.drops.Count; k++)
					{
						BarricadeDrop drop = region.drops[k];
						if (drop?.model == null) continue;
						if ((drop.model.position - center).sqrMagnitude > sqr) continue;
						InteractableFarm farm = drop.model.GetComponent<InteractableFarm>();
						if (farm == null) continue;
						if (AutoFarm.GetIsFullyGrown(farm))
						{
							if (grown != null) grown.Add(farm);
						}
						else if (!AutoFarm.GetPlanted(farm))
						{
							if (empty != null) empty.Add(farm);
						}
					}
				}
			}
		}

		// ── Harvesting ──

		private static bool TryHarvestNearbyFarm(Player player)
		{
			Vector3 pos = player.transform.position;
			List<InteractableFarm> grown = new List<InteractableFarm>();
			AutoFarm.ScanFarms(pos, State.AutoFarmHarvestRadius, grown, null);
			for (int i = 0; i < grown.Count; i++)
			{
				if (grown[i] == null) continue;
				AutoFarm.InteractFarm(grown[i], player);
				AutoFarm.StatusText = "Harvesting farm...";
				return true;
			}
			return false;
		}

		private static void InteractFarm(InteractableFarm farm, Player player)
		{
			try
			{
				// Use the proper ClientHarvest() RPC method from U3SDK
				// This calls SendHarvestRequest.Invoke(GetNetId(), NetTransport.ENetReliability.Unreliable)
				MethodInfo clientHarvestMethod = typeof(InteractableFarm).GetMethod("ClientHarvest", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (clientHarvestMethod != null)
				{
					clientHarvestMethod.Invoke(farm, null);
					AutoFarm.StatusText = "Harvesting (RPC)";
					return;
				}
				
				// Fallback to reflection-based methods for older versions
				if (AutoFarm._clientHarvest != null)
				{
					try
					{
						ParameterInfo[] ps = AutoFarm._clientHarvest.GetParameters();
						if (ps.Length == 0) { AutoFarm._clientHarvest.Invoke(farm, null); return; }
					}
					catch { }
				}
				
				// Direct sendInteract path as fallback
				if (AutoFarm.TryDirectSendInteract(farm, player))
				{
					AutoFarm.StatusText = "Interacted (direct)";
					return;
				}
				
				// Last resort: fake the raycast hit
				AutoFarm.FakeHitFarm(farm, player);
			}
			catch (Exception ex)
			{
				Runtime.Trace("AutoFarm interact err: " + ex.Message);
			}
		}

		private static void FakeHitFarm(InteractableFarm farm, Player player)
		{
			try
			{
				if (AutoFarm._piHit == null) return;
				Collider col = farm.GetComponent<Collider>();
				if (col == null) col = farm.GetComponentInChildren<Collider>();

				RaycastHit hit = default(RaycastHit);
				bool gotHit = false;
				int interactMask = RayMasks.BARRICADE_INTERACT | RayMasks.PLAYER_INTERACT;

				// Primary path: forward raycast from the active camera aimed at the closest point on the farm's collider.
				// This is the same line of sight the player would normally use to press F on the farm.
				Camera cam = MainCamera.instance;
				if (cam == null) cam = Camera.main;
				if (cam != null && col != null)
				{
					try
					{
						Vector3 camPos = cam.transform.position;
						Vector3 closest = col.ClosestPoint(camPos);
						Vector3 toTarget = closest - camPos;
						float total = toTarget.magnitude;
						if (total > 0.01f)
						{
							Vector3 dir = toTarget / total;
							if (Physics.Raycast(new Ray(camPos, dir), out hit, total + 2f, interactMask, (QueryTriggerInteraction)1))
								gotHit = true;
						}
					}
					catch { }
				}

				// Fallback path: dead-reckon straight down from above the collider (original behavior,
				// kept because some map setups place farms under terrain).
				if (!gotHit && col != null)
				{
					Vector3 start = col.bounds.center + Vector3.up * 4f;
					if (Physics.Raycast(new Ray(start, Vector3.down), out hit, 12f, RayMasks.BARRICADE_INTERACT, (QueryTriggerInteraction)1))
						gotHit = true;
					else if (Physics.Raycast(new Ray(start, Vector3.down), out hit, 12f, RayMasks.PLAYER_INTERACT, (QueryTriggerInteraction)1))
						gotHit = true;
					if (gotHit) Runtime.Trace("AutoFarm fakeHit: forward path failed, used dead-reckon fallback");
				}

				if (!gotHit)
				{
					Runtime.Trace("AutoFarm fakeHit: no valid raycast hit for farm");
					return;
				}

				PlayerInteract interact = player.GetComponent<PlayerInteract>();

				// Try static first, then instance — handles both field layouts
				try { AutoFarm._piHit.SetValue(null, hit); } catch { }
				if (interact != null) { try { AutoFarm._piHit.SetValue(interact, hit); } catch { } }

				// Reset lastInteract to 0 so cooldown is bypassed
				if (AutoFarm._piLastInteract != null)
				{
					try { AutoFarm._piLastInteract.SetValue(null, 0f); } catch { }
					if (interact != null) { try { AutoFarm._piLastInteract.SetValue(interact, 0f); } catch { } }
				}

				if (AutoFarm._sendInteract != null && interact != null)
				{
					ParameterInfo[] ps = AutoFarm._sendInteract.GetParameters();
					if (ps.Length == 0)
					{
						AutoFarm._sendInteract.Invoke(interact, null);
					}
					else if (ps.Length >= 1 && (ps[0].ParameterType == typeof(Transform) || ps[0].ParameterType == typeof(GameObject)))
					{
						object arg = (ps[0].ParameterType == typeof(GameObject)) ? (object)farm.gameObject : (object)farm.transform;
						object[] invokeArgs = new object[ps.Length];
						invokeArgs[0] = arg;
						AutoFarm._sendInteract.Invoke(interact, invokeArgs);
					}
				}
			}
			catch (Exception ex)
			{
				Runtime.Trace("AutoFarm fakeHit err: " + ex.Message);
			}
		}

		// ── Replanting ──

		// Reflect PlayerEquipment.equip for auto-equip seed
		private static MethodInfo _equipMethod;
		private static bool _equipReflected;

		private static void ReflectEquip()
		{
			if (_equipReflected) return;
			_equipReflected = true;
			BindingFlags bf = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			// Prefer the canonical equip(byte, byte, byte, byte) instance method — that
			// signature is stable across all Unturned versions. Fall through to name-based
			// lookups only for unusual renamed builds.
			foreach (MethodInfo m in typeof(PlayerEquipment).GetMethods(bf))
			{
				if (m.Name != "equip") continue;
				ParameterInfo[] ps = m.GetParameters();
				if (ps.Length == 4
					&& ps[0].ParameterType == typeof(byte)
					&& ps[1].ParameterType == typeof(byte)
					&& ps[2].ParameterType == typeof(byte)
					&& ps[3].ParameterType == typeof(byte))
				{
					_equipMethod = m;
					break;
				}
			}
			if (_equipMethod == null)
				_equipMethod = typeof(PlayerEquipment).GetMethod("equip", bf);
			if (_equipMethod == null)
				_equipMethod = typeof(PlayerEquipment).GetMethod("ServerEquip", bf);
			if (_equipMethod == null)
				_equipMethod = typeof(PlayerEquipment).GetMethod("ClientEquip", bf);
			if (_equipMethod == null)
				_equipMethod = typeof(PlayerEquipment).GetMethod("SendEquipRequest", bf);
			Runtime.Trace("AutoFarm equip reflect: method=" + (_equipMethod != null ? _equipMethod.Name : "null"));
		}

		// Shared inventory-search + equip helper. Searches the player's inventory for an
		// item with the given id, equips the first match using whatever PlayerEquipment.equip
		// signature was reflectively resolved, and returns true if the item ended up equipped
		// (or was already equipped). Returns false if itemID==0, the reflection failed, or
		// the item isn't in the inventory.
		private static bool EnsureItemEquipped(Player player, ushort itemID, string logTag)
		{
			if (itemID == 0) return false;
			if (player.equipment != null && player.equipment.asset != null
				&& player.equipment.asset.id == itemID) return true;
			AutoFarm.ReflectEquip();
			if (AutoFarm._equipMethod == null) return false;
			for (byte page = 0; page < PlayerInventory.PAGES - 2; page++)
			{
				Items items = player.inventory.items[(int)page];
				if (items == null) continue;
				for (byte idx = 0; idx < items.getItemCount(); idx++)
				{
					ItemJar jar = items.getItem(idx);
					if (jar?.item == null) continue;
					if (jar.item.id != itemID) continue;
					try
					{
						ParameterInfo[] ps = AutoFarm._equipMethod.GetParameters();
						if (ps.Length == 4)
							AutoFarm._equipMethod.Invoke(player.equipment, new object[] { page, jar.x, jar.y, (byte)0 });
						else if (ps.Length == 3)
							AutoFarm._equipMethod.Invoke(player.equipment, new object[] { page, jar.x, jar.y });
						else if (ps.Length == 2)
							AutoFarm._equipMethod.Invoke(player.equipment, new object[] { page, idx });
						else continue;
						return true;
					}
					catch (Exception ex)
					{
						Runtime.Trace("AutoFarm " + logTag + " equip err: " + ex.Message);
					}
				}
			}
			return false;
		}

		// Seed-equip is "best effort": the toggle gates user intent, and a missing seed id
		// means the user will equip manually. Returning true in those cases keeps replant
		// from being blocked on a missing config.
		private static bool EnsureSeedEquipped(Player player)
		{
			if (!State.AutoFarmAutoEquipSeed) return true;
			if (State.AutoFarmCraftItemID == 0) return true;
			return AutoFarm.EnsureItemEquipped(player, State.AutoFarmCraftItemID, "seed");
		}

		private static bool TryReplantNearbyFarm(Player player)
		{
			Vector3 pos = player.transform.position;
			List<InteractableFarm> empty = new List<InteractableFarm>();
			AutoFarm.ScanFarms(pos, State.AutoFarmHarvestRadius, null, empty);
			for (int i = 0; i < empty.Count; i++)
			{
				if (empty[i] == null) continue;
				// Auto-equip seed before planting
				if (State.AutoFarmAutoEquipSeed && State.AutoFarmCraftItemID != 0)
				{
					if (!AutoFarm.EnsureSeedEquipped(player))
					{
						AutoFarm.StatusText = "No seed in inventory!";
						return false;
					}
				}
				AutoFarm.InteractFarm(empty[i], player);
				AutoFarm.StatusText = "Planting seed...";
				return true;
			}
			return false;
		}

		// ── Watering (fertilizer analog) ──

		// Watering requires a configured item — if the user enabled AutoFarmWaterOn but
		// hasn't set an item yet, return false so the status reports "No water item!".
		private static bool EnsureWaterItemEquipped(Player player)
		{
			return AutoFarm.EnsureItemEquipped(player, State.AutoFarmWaterItemID, "water");
		}

		// Scan planted-but-not-grown farms within radius and water the closest one.
		// This is the closest vanilla-Unturned analog to "fertilizer": equipping a
		// water item and pressing F on a planted farm grows it faster. If the user
		// sets AutoFarmWaterItemID to a server-plugin fertilizer item, same interaction.
		private static bool TryWaterNearbyFarm(Player player)
		{
			Vector3 pos = player.transform.position;
			List<InteractableFarm> planted = new List<InteractableFarm>();
			float sqr = State.AutoFarmHarvestRadius * State.AutoFarmHarvestRadius;
			if (BarricadeManager.regions == null) return false;
			for (int i = 0; i < BarricadeManager.regions.GetLength(0); i++)
			{
				for (int j = 0; j < BarricadeManager.regions.GetLength(1); j++)
				{
					BarricadeRegion region = BarricadeManager.regions[i, j];
					if (region?.drops == null) continue;
					for (int k = 0; k < region.drops.Count; k++)
					{
						BarricadeDrop drop = region.drops[k];
						if (drop?.model == null) continue;
						if ((drop.model.position - pos).sqrMagnitude > sqr) continue;
						InteractableFarm farm = drop.model.GetComponent<InteractableFarm>();
						if (farm == null) continue;
						bool grown = AutoFarm.GetIsFullyGrown(farm);
						bool pl = AutoFarm.GetPlanted(farm);
						if (!grown && pl) planted.Add(farm);
					}
				}
			}
			for (int i = 0; i < planted.Count; i++)
			{
				if (planted[i] == null) continue;
				if (!AutoFarm.EnsureWaterItemEquipped(player))
				{
					AutoFarm.StatusText = "No water item!";
					return false;
				}
				AutoFarm.InteractFarm(planted[i], player);
				AutoFarm.StatusText = "Watering farm...";
				return true;
			}
			return false;
		}

		// Direct sendInteract: set PlayerInteract.interactable transform to the farm's
		// transform, reset cooldown, then enumerate ALL sendInteract / SendInteractRequest
		// overloads on PlayerInteract and invoke the first that accepts (Transform|GameObject)
		// or no args. This is what pressing F does client-side.
		private static bool TryDirectSendInteract(InteractableFarm farm, Player player)
		{
			try
			{
				if (AutoFarm._sendInteract == null) return false;
				PlayerInteract interact = player?.GetComponent<PlayerInteract>();
				if (interact == null) return false;

				// Reset cooldown so the server doesn't rate-limit us
				if (AutoFarm._piLastInteract != null)
				{
					try { AutoFarm._piLastInteract.SetValue(null, 0f); } catch { }
					try { AutoFarm._piLastInteract.SetValue(interact, 0f); } catch { }
				}

				// Point PlayerInteract.interactable at the farm's transform
				if (AutoFarm._piInteractable != null)
				{
					try { AutoFarm._piInteractable.SetValue(interact, farm.transform); } catch { }
				}

				BindingFlags bf = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
				MethodInfo[] candidates;
				try { candidates = typeof(PlayerInteract).GetMethods(bf); } catch { candidates = null; }
				if (candidates == null) return false;
				foreach (MethodInfo mi in candidates)
				{
					if (mi.Name != "sendInteract" && mi.Name != "SendInteractRequest") continue;
					ParameterInfo[] ps = mi.GetParameters();
					try
					{
						if (ps.Length == 0)
						{
							mi.Invoke(interact, null);
							return true;
						}
						if (ps.Length == 2 && (ps[0].ParameterType == typeof(Transform) || ps[0].ParameterType == typeof(GameObject)) && ps[1].ParameterType == typeof(bool))
						{
							// Common older-Unturned signature: sendInteract(Transform, bool isClickRequest)
							object firstArg = (ps[0].ParameterType == typeof(GameObject)) ? (object)farm.gameObject : (object)farm.transform;
							mi.Invoke(interact, new object[] { firstArg, true });
							return true;
						}
						if (ps.Length >= 1 && (ps[0].ParameterType == typeof(Transform) || ps[0].ParameterType == typeof(GameObject)))
						{
							object firstArg = (ps[0].ParameterType == typeof(GameObject)) ? (object)farm.gameObject : (object)farm.transform;
							object[] invokeArgs = new object[ps.Length];
							invokeArgs[0] = firstArg;
							for (int i = 1; i < ps.Length; i++)
							{
								Type pt = ps[i].ParameterType;
								if (pt.IsPrimitive) invokeArgs[i] = pt == typeof(bool) ? (object)false : (object)0;
								else if (pt.IsValueType) invokeArgs[i] = Activator.CreateInstance(pt);
								else invokeArgs[i] = null;
							}
							mi.Invoke(interact, invokeArgs);
							return true;
						}
					}
					catch { /* try next overload */ }
				}
			}
			catch (Exception ex)
			{
				Runtime.Trace("AutoFarm direct interact err: " + ex.Message);
			}
			return false;
		}

		// ── Crafting ──

		// Cache: list of (blueprintIndex, itemAssetID) for the craft item
		private static bool _craftCacheBuilt;
		private static int _craftCacheForID;
		private static int _craftBlueprintIdx = -1;
		private static ItemAsset _craftBlueprintAsset;

		private static void RebuildCraftCache()
		{
			_craftBlueprintIdx = -1;
			_craftBlueprintAsset = null;
			_craftCacheBuilt = false;

			ushort targetID = State.AutoFarmCraftItemID;
			if (targetID == 0) return;

			try
			{
				List<ItemAsset> allItems = new List<ItemAsset>();
				Assets.find(allItems);
				foreach (ItemAsset asset in allItems)
				{
					if (asset?.blueprints == null) continue;
					for (int b = 0; b < asset.blueprints.Count; b++)
					{
						Blueprint bp = asset.blueprints[b];
						if (bp == null) continue;
						if (AutoFarm.BlueprintOutputsTargetID(bp, targetID))
						{
							_craftBlueprintIdx = b;
							_craftBlueprintAsset = asset;
							_craftCacheBuilt = true;
							_craftCacheForID = targetID;
							Runtime.Trace("AutoFarm craft cache: asset=" + asset.id + " bp=" + b + " -> " + targetID);
							return;
						}
					}
				}
				_craftCacheBuilt = true;
				_craftCacheForID = targetID;
				Runtime.Trace("AutoFarm craft cache: no blueprint found for id=" + targetID);
			}
			catch (Exception ex)
			{
				Runtime.Trace("AutoFarm craft cache err: " + ex.Message);
			}
		}

		private static FieldInfo _bpOutputsField;
		private static FieldInfo _bpOutputIdField;
		private static bool _bpReflected;

		private static bool BlueprintOutputsTargetID(Blueprint bp, ushort targetID)
		{
			try
			{
				if (!_bpReflected)
				{
					_bpReflected = true;
					BindingFlags bf = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
					// outputs is typically a BlueprintOutput[] or BlueprintSupply[]
					_bpOutputsField = typeof(Blueprint).GetField("outputs", bf);
				if (_bpOutputsField == null) _bpOutputsField = typeof(Blueprint).GetField("_outputs", bf);
				if (_bpOutputsField != null)
				{
					// Figure out the id field on the element type
					System.Array testArr = _bpOutputsField.GetValue(bp) as System.Array;
					if (testArr != null && testArr.Length > 0)
					{
						Type elemType = testArr.GetType().GetElementType();
						_bpOutputIdField = elemType?.GetField("id", bf);
						if (_bpOutputIdField == null)
							_bpOutputIdField = elemType?.GetField("itemID", bf);
						// The original code had `? null : null` here which was always-null dead code.
						// The per-element loop below handles PropertyInfo fallback dynamically,
						// so we just leave _bpOutputIdField null if no field was found.
					}
				}
				}
				if (_bpOutputsField == null) return false;
				System.Array arr = _bpOutputsField.GetValue(bp) as System.Array;
				if (arr == null) return false;
				foreach (object elem in arr)
				{
					if (elem == null) continue;
					ushort id = 0;
					if (_bpOutputIdField != null)
					{
						id = (ushort)Convert.ChangeType(_bpOutputIdField.GetValue(elem), typeof(ushort));
					}
					else
					{
						// Fallback: try to get 'id' property via reflection on element type
						PropertyInfo idProp = elem.GetType().GetProperty("id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
						if (idProp == null) idProp = elem.GetType().GetProperty("itemID", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
						if (idProp != null)
							id = (ushort)Convert.ChangeType(idProp.GetValue(elem, null), typeof(ushort));
						else
						{
							FieldInfo idF = elem.GetType().GetField("id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
							if (idF == null) idF = elem.GetType().GetField("itemID", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
							if (idF != null)
								id = (ushort)Convert.ChangeType(idF.GetValue(elem), typeof(ushort));
						}
					}
					if (id == targetID) return true;
				}
			}
			catch { }
			return false;
		}

		private static bool TryCraftItem(Player player)
		{
			try
			{
				PlayerCrafting crafting = player.crafting;
				if (crafting == null) return false;
				ushort targetID = State.AutoFarmCraftItemID;
				if (targetID == 0) return false;

				// Rebuild cache if stale
				if (!_craftCacheBuilt || _craftCacheForID != targetID)
					RebuildCraftCache();

				if (_craftBlueprintAsset == null || _craftBlueprintIdx < 0)
					return false;

				// Try askCraft / sendCraft with the found blueprint index
				return AutoFarm.CallCraftMethod(crafting, player, _craftBlueprintAsset, _craftBlueprintIdx);
			}
			catch (Exception ex)
			{
				Runtime.Trace("AutoFarm craft err: " + ex.Message);
			}
			return false;
		}

		private static bool CallCraftMethod(PlayerCrafting crafting, Player player, ItemAsset asset, int blueprintIdx)
		{
			try
			{
				BindingFlags bf = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

				// Use the proper SendCraft RPC method from U3SDK
				// Signature: SendCraft.Invoke(GetNetId(), ENetReliability.Reliable, assetGuid, index, asManyAsPossible)
				MethodInfo sendCraftMethod = typeof(PlayerCrafting).GetMethod("SendCraft", bf);
				if (sendCraftMethod != null)
				{
					try
					{
						ParameterInfo[] ps = sendCraftMethod.GetParameters();
						if (ps.Length == 4 && ps[0].ParameterType == typeof(SDG.NetTransport.ENetReliability))
						{
							// New U3SDK signature: SendCraft(ENetReliability, Guid, byte, bool)
							sendCraftMethod.Invoke(crafting, new object[] { 
								SDG.NetTransport.ENetReliability.Reliable, 
								asset.GUID, 
								(byte)blueprintIdx, 
								false 
							});
							AutoFarm.StatusText = "Crafting (RPC)";
							return true;
						}
					}
					catch (Exception ex) { Runtime.Trace("SendCraft RPC fail: " + ex.Message); }
				}

				// Fallback to older methods for compatibility
				MethodInfo craftDirect = typeof(PlayerCrafting).GetMethod("craft", bf);
				if (craftDirect == null) craftDirect = typeof(PlayerCrafting).GetMethod("Craft", bf);
				if (craftDirect == null) craftDirect = typeof(PlayerCrafting).GetMethod("ClientCraft", bf);
				if (craftDirect == null) craftDirect = typeof(PlayerCrafting).GetMethod("SendCraftRequest", bf);
				if (craftDirect == null) craftDirect = typeof(PlayerCrafting).GetMethod("ServerCraftRequest", bf);
				if (craftDirect != null)
				{
					try
					{
						ParameterInfo[] ps = craftDirect.GetParameters();
						if (ps.Length == 2)
						{
							craftDirect.Invoke(crafting, new object[] { asset, blueprintIdx });
							AutoFarm.StatusText = "Crafting seed...";
							return true;
						}
						if (ps.Length == 1)
						{
							// Might be craft(Blueprint bp)
							if (asset.blueprints != null && blueprintIdx < asset.blueprints.Count)
								craftDirect.Invoke(crafting, new object[] { asset.blueprints[blueprintIdx] });
							AutoFarm.StatusText = "Crafting seed...";
							return true;
						}
					}
					catch (Exception ex) { Runtime.Trace("craft direct fail: " + ex.Message); }
				}

				// askCraft — find the item in the player's crafting recipe list to get the right index
				if (AutoFarm._askCraft != null)
				{
					ParameterInfo[] ps = AutoFarm._askCraft.GetParameters();
					try
					{
						// Try to find craftIndex by searching crafting.recipes
						int craftIdx = AutoFarm.FindCraftingListIndex(crafting, asset, blueprintIdx);

						if (ps.Length == 3 && ps[0].ParameterType == typeof(Player) && craftIdx >= 0)
						{
							AutoFarm._askCraft.Invoke(crafting, new object[] {
								player,
								Convert.ChangeType(craftIdx, ps[1].ParameterType),
								Convert.ChangeType(blueprintIdx, ps[2].ParameterType)
							});
							AutoFarm.StatusText = "Crafting seed...";
							return true;
						}
						if (ps.Length == 2 && ps[0].ParameterType == typeof(Player) && craftIdx >= 0)
						{
							AutoFarm._askCraft.Invoke(crafting, new object[] {
								player,
								Convert.ChangeType(craftIdx, ps[1].ParameterType)
							});
							AutoFarm.StatusText = "Crafting seed...";
							return true;
						}
						if (ps.Length == 1 && craftIdx >= 0)
						{
							AutoFarm._askCraft.Invoke(crafting, new object[] { Convert.ChangeType(craftIdx, ps[0].ParameterType) });
							AutoFarm.StatusText = "Crafting seed...";
							return true;
						}
					}
					catch (Exception ex) { Runtime.Trace("askCraft fail: " + ex.Message); }
				}

				// sendCraft fallback
				if (AutoFarm._sendCraft != null)
				{
					try
					{
						ParameterInfo[] ps = AutoFarm._sendCraft.GetParameters();
						int craftIdx = AutoFarm.FindCraftingListIndex(crafting, asset, blueprintIdx);
						if (craftIdx >= 0 && ps.Length == 2 && ps[0].ParameterType == typeof(Player))
						{
							AutoFarm._sendCraft.Invoke(null, new object[] { player, Convert.ChangeType(craftIdx, ps[1].ParameterType) });
							AutoFarm.StatusText = "Crafting seed...";
							return true;
						}
					}
					catch (Exception ex) { Runtime.Trace("sendCraft fail: " + ex.Message); }
				}
			}
			catch (Exception ex)
			{
				Runtime.Trace("AutoFarm craft call err: " + ex.Message);
			}
			return false;
		}

		/// Find the index of an asset+blueprint in PlayerCrafting.recipes list
		private static int FindCraftingListIndex(PlayerCrafting crafting, ItemAsset asset, int blueprintIdx)
		{
			try
			{
				if (AutoFarm._recipesProp == null) return -1;
				object recipesObj = AutoFarm._recipesProp.GetValue(crafting, null);
				System.Collections.IList list = recipesObj as System.Collections.IList;
				if (list == null) return -1;
				BindingFlags bf = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
				for (int i = 0; i < list.Count; i++)
				{
					object recipe = list[i];
					if (recipe == null) continue;
					// Check if this recipe entry matches our asset+blueprint
					// The entry might have an asset reference or outputs matching our target
					FieldInfo assetField = recipe.GetType().GetField("asset", bf);
					if (assetField != null && assetField.GetValue(recipe) == asset) return i;
					PropertyInfo assetProp = recipe.GetType().GetProperty("asset", bf);
					if (assetProp != null && assetProp.GetValue(recipe, null) == asset) return i;
				}
			}
			catch { }
			return 0; // default to 0 if we can't find it — better than -1 causing no craft
		}

		// ── Storing ──

		private static bool TryStoreItems(Player player)
		{
			AutoFarm.StorageEntry nearest = null;
			float nearestSqr = float.MaxValue;
			Vector3 pos = player.transform.position;
			for (int i = 0; i < AutoFarm.Storages.Count; i++)
			{
				if (AutoFarm.Storages[i].storage == null)
				{
					AutoFarm.Storages.RemoveAt(i);
					i--;
					continue;
				}
				float sqr = (AutoFarm.Storages[i].position - pos).sqrMagnitude;
				if (sqr < nearestSqr)
				{
					nearestSqr = sqr;
					nearest = AutoFarm.Storages[i];
				}
			}
			if (nearest == null) return false;
			if (nearestSqr > 100f * 100f) return false;
			try
			{
				if (AutoFarm._askStoreStorage == null) return false;
				ParameterInfo[] ps = AutoFarm._askStoreStorage.GetParameters();
				for (byte page = 0; page < PlayerInventory.PAGES - 2; page++)
				{
					Items items = player.inventory.items[(int)page];
					if (items == null) continue;
					for (byte idx = 0; idx < items.getItemCount(); idx++)
					{
						ItemJar jar = items.getItem(idx);
						if (jar?.item == null) continue;
						ItemAsset asset = Assets.find(EAssetType.ITEM, jar.item.id) as ItemAsset;
						if (asset == null) continue;
						string t = asset.type.ToString();
						if (t != "FOOD" && t != "FARM") continue;
						if (State.AutoFarmAutoCraft && jar.item.id == State.AutoFarmCraftItemID)
							continue;
						if (ps.Length == 3 && ps[0].ParameterType == typeof(Player))
						{
							AutoFarm._askStoreStorage.Invoke(nearest.storage, new object[] { player, page, idx });
							AutoFarm.StatusText = "Storing items...";
							return true;
						}
					}
				}
			}
			catch (Exception ex)
			{
				Runtime.Trace("AutoFarm store err: " + ex.Message);
			}
			return false;
		}

		// ── UI Window ──

		private static Rect _winRect = new Rect(280f, 120f, 380f, 380f);
		private static bool _dragging;
		private static Vector2 _dragOff;
		private static Vector2 _scroll;

	}
}

