using System;
using System.Collections.Generic;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace gatyware
{
	public static class AutoForge
	{
		private static float _lastPickup;

		public static void Update()
		{
			if (!State.AutoForge || State.IsSpying)
			{
				return;
			}
			Player localPlayer = Player.LocalPlayer;
			if (localPlayer == null)
			{
				return;
			}
			if (Time.realtimeSinceStartup - AutoForge._lastPickup < State.AutoForgeDelay)
			{
				return;
			}
			AutoForge._lastPickup = Time.realtimeSinceStartup;
			
			// Get position - works whether in vehicle or on foot
			Vector3 position = localPlayer.transform.position;
			float radius = State.AutoForgeRadius;
			float sqrRadius = radius * radius;
			
			// Harvest resource nodes
			AutoForge.HarvestResourceNodes(position, sqrRadius);
			
			// Pick up dropped resource items
			AutoForge.PickupResourceItems(position, sqrRadius);
		}

		private static void HarvestResourceNodes(Vector3 position, float sqrRadius)
		{
			try
			{
				// Find InteractableForage objects (forage resources like berries, mushrooms)
				InteractableForage[] forageNodes = UnityEngine.Object.FindObjectsOfType<InteractableForage>();
				if (forageNodes != null)
				{
					for (int i = 0; i < forageNodes.Length; i++)
					{
						InteractableForage forage = forageNodes[i];
						if (forage == null)
						{
							continue;
						}
						
						Transform resourceTransform = forage.transform.parent;
						if (resourceTransform == null)
						{
							continue;
						}
						
						// Check if within radius
						if ((resourceTransform.position - position).sqrMagnitude > sqrRadius)
						{
							continue;
						}
						
						// Harvest the forage resource
						try
						{
							ResourceManager.forage(resourceTransform);
						}
						catch { }
					}
				}
				
				// Find regular resource nodes (trees, rocks, metal ore)
				// Use ResourceManager to get resources in radius
				List<RegionCoordinate> searchRegions = new List<RegionCoordinate>();
				List<Transform> resourceTransforms = new List<Transform>();
				
				// Get nearby regions - expand search area
				byte playerX, playerY;
				if (Regions.tryGetCoordinate(position, out playerX, out playerY))
				{
					int searchRadius = Mathf.CeilToInt(Mathf.Sqrt(sqrRadius) / 512f) + 2; // Approximate region coverage
					for (byte x = (byte)Mathf.Max(0, playerX - searchRadius); x <= Mathf.Min(Regions.WORLD_SIZE - 1, playerX + searchRadius); x++)
					{
						for (byte y = (byte)Mathf.Max(0, playerY - searchRadius); y <= Mathf.Min(Regions.WORLD_SIZE - 1, playerY + searchRadius); y++)
						{
							searchRegions.Add(new RegionCoordinate(x, y));
						}
					}
					
					ResourceManager.getResourcesInRadius(position, sqrRadius, searchRegions, resourceTransforms);
					
					for (int i = 0; i < resourceTransforms.Count; i++)
					{
						Transform resource = resourceTransforms[i];
						if (resource == null)
						{
							continue;
						}
						
						// Harvest the resource
						try
						{
							Player player = Player.player;
							CSteamID steamID = (player != null && player.channel != null && player.channel.owner != null) ? player.channel.owner.playerID.steamID : default(CSteamID);
							ResourceManager.damage(resource, Vector3.down, 1000f, 1f, 1f, out _, out _, steamID, EDamageOrigin.Unknown, false);
						}
						catch { }
					}
				}
			}
			catch { }
		}

		private static void PickupResourceItems(Vector3 position, float sqrRadius)
		{
			List<InteractableItem> clampedItems = ItemManager.clampedItems;
			if (clampedItems == null)
			{
				return;
			}
			
			for (int i = 0; i < clampedItems.Count; i++)
			{
				InteractableItem interactableItem = clampedItems[i];
				if (interactableItem == null || interactableItem.asset == null)
				{
					continue;
				}
				
				// Check if within radius
				if ((interactableItem.transform.position - position).sqrMagnitude > sqrRadius)
				{
					continue;
				}
				
				// Check if it's a resource item (metal, stone, etc.)
				if (AutoForge.IsResourceItem(interactableItem.asset))
				{
					try
					{
						interactableItem.use();
					}
					catch { }
				}
			}
		}

		private static bool IsResourceItem(ItemAsset asset)
		{
			if (asset == null)
			{
				return false;
			}
			
			// Check item name for resource keywords
			string name = asset.itemName.ToLower();
			
			// Common resource items in Unturned - expanded list
			if (name.Contains("metal") || name.Contains("ore"))
			{
				return true;
			}
			if (name.Contains("stone") || name.Contains("rock"))
			{
				return true;
			}
			if (name.Contains("fiber") || name.Contains("cloth"))
			{
				return true;
			}
			if (name.Contains("wood") || name.Contains("log") || name.Contains("plank"))
			{
				return true;
			}
			if (name.Contains("coal"))
			{
				return true;
			}
			if (name.Contains("sulfur"))
			{
				return true;
			}
			if (name.Contains("crystal"))
			{
				return true;
			}
			if (name.Contains("iron") || name.Contains("copper") || name.Contains("silver") || name.Contains("gold"))
			{
				return true;
			}
			if (name.Contains("lime") || name.Contains("magnesium"))
			{
				return true;
			}
			if (name.Contains("potassium") || name.Contains("nitrate"))
			{
				return true;
			}
			
			// Check item type - RESOURCE type items
			string type = asset.type.ToString();
			if (type == "RESOURCE" || type == "FARM" || type == "BUILDING")
			{
				return true;
			}
			
			return false;
		}
	}
}
