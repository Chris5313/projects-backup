using System;
using System.Collections.Generic;
using SDG.Unturned;
using UnityEngine;
public static class ItemManagerHooks
{
	[HookMethodAttribute(typeof(ItemManager), "findSimulatedItemsInRadius", new Type[] { })]
	public static void FindSimulatedItemsInRadius(Vector3 center, float sqrRadius, List<InteractableItem> result)
	{
		sqrRadius = (MiscConfig.extendPlayerRegion ? ((float)(MiscConfig.extendRegionRange * MiscConfig.extendRegionRange)) : sqrRadius);
		bool flag = ItemManager.clampedItems == null;
		bool flag2 = !flag;
		if (flag2)
		{
			foreach (InteractableItem interactableItem in ItemManager.clampedItems)
			{
				bool flag3 = interactableItem != null && (interactableItem.transform.position - center).sqrMagnitude <= sqrRadius;
				bool flag4 = flag3;
				if (flag4)
				{
					result.Add(interactableItem);
				}
			}
		}
	}
}
