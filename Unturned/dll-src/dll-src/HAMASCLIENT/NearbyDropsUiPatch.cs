using System;
using System.Collections.Generic;
using System.Reflection;
using SDG.Unturned;
using UnityEngine;
public static class NearbyDropsUiPatch
{
	[HookMethodAttribute(typeof(PlayerDashboardInventoryUI), "updateNearbyDrops", new Type[] { })]
	public static void UpdateNearbyDropsOverride()
	{
		bool flag = !PlayerDashboardInventoryUI.active || NearbyDropsUiPatch.pendingItemsInRadiusField.value.Count < 1;
		bool flag2 = !flag;
		if (flag2)
		{
			int height = (int)NearbyDropsUiPatch.areaItemsField.value.height;
			Vector3 eyesPositionWithoutLeaning = Player.player.look.GetEyesPositionWithoutLeaning();
			int num = Mathf.Max(0, NearbyDropsUiPatch.pendingItemsInRadiusField.value.Count - 20);
			for (int i = NearbyDropsUiPatch.pendingItemsInRadiusField.value.Count - 1; i >= num; i--)
			{
				InteractableItem interactableItem = NearbyDropsUiPatch.pendingItemsInRadiusField.value[i];
				NearbyDropsUiPatch.pendingItemsInRadiusField.value.RemoveAt(i);
				bool flag3 = !(interactableItem == null) && interactableItem.item != null;
				bool flag4 = flag3;
				if (flag4)
				{
					Renderer componentInChildren = interactableItem.transform.GetComponentInChildren<Renderer>();
					bool flag5 = !(componentInChildren == null);
					bool flag6 = flag5;
					if (flag6)
					{
						Vector3 center = componentInChildren.bounds.center;
						RaycastHit raycastHit = default(RaycastHit);
						bool flag7 = MiscConfig.extendRegionInteractThroughWalls || !Physics.Linecast(eyesPositionWithoutLeaning, center, out raycastHit, RayMasks.BLOCK_PICKUP, QueryTriggerInteraction.Ignore);
						bool flag8 = flag7;
						if (flag8)
						{
							NearbyDropsUiPatch.createElementForNearbyDropMethod.Invoke(new object[] { interactableItem });
						}
					}
				}
			}
			bool flag9 = (int)NearbyDropsUiPatch.areaItemsField.value.height > height;
			bool flag10 = flag9;
			if (flag10)
			{
				NearbyDropsUiPatch.updateBoxAreasMethod.Invoke(Array.Empty<object>());
			}
		}
	}
	public static ReflectedField<List<InteractableItem>> pendingItemsInRadiusField = new ReflectedField<List<InteractableItem>>(typeof(PlayerDashboardInventoryUI), "pendingItemsInRadius", BindingFlags.Static | BindingFlags.NonPublic);
	public static ReflectedField<Items> areaItemsField = new ReflectedField<Items>(typeof(PlayerDashboardInventoryUI), "areaItems", BindingFlags.Static | BindingFlags.NonPublic);
	public static ReflectedMethod createElementForNearbyDropMethod = new ReflectedMethod(typeof(PlayerDashboardInventoryUI), "createElementForNearbyDrop", BindingFlags.Static | BindingFlags.NonPublic);
	public static ReflectedMethod updateBoxAreasMethod = new ReflectedMethod(typeof(PlayerDashboardInventoryUI), "updateBoxAreas", BindingFlags.Static | BindingFlags.NonPublic);
}
