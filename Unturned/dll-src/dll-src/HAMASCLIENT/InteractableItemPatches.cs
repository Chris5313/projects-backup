using System;
using System.Collections.Generic;
using System.Reflection;
using SDG.Unturned;
using UnityEngine;
public class InteractableItemPatches
{
	[InitializeAttribute]
	public static void Initialize()
	{
		InteractableItemPatches.WasResetField = typeof(InteractableItem).GetField("wasReset", BindingFlags.Instance | BindingFlags.NonPublic);
		Provider.onClientDisconnected = (Provider.ClientDisconnected)Delegate.Combine(Provider.onClientDisconnected, new Provider.ClientDisconnected(InteractableItemPatches.OnClientDisconnected));
	}
	public static void OnClientDisconnected()
	{
		InteractableItemPatches.TrackedItems.Clear();
	}
	[HookMethodAttribute(typeof(InteractableItem), "OnEnable", BindingFlags.Instance | BindingFlags.NonPublic, new Type[] { })]
	public static void OnEnablePatch(InteractableItem instance)
	{
		ItemManager.clampedItems.Add(instance);
		InteractableItemPatches.TrackedItems.Add(instance);
	}
	[HookMethodAttribute(typeof(InteractableItem), "OnDisable", BindingFlags.Instance | BindingFlags.NonPublic, new Type[] { })]
	public static void OnDisablePatch(InteractableItem instance)
	{
		bool flag = Time.realtimeSinceStartup - InteractableItemPatches.LastCleanupTime > 2.5f;
		if (flag)
		{
			InteractableItemPatches.LastCleanupTime = Time.realtimeSinceStartup;
			for (int i = 0; i < InteractableItemPatches.TrackedItems.Count; i++)
			{
				bool flag2 = InteractableItemPatches.TrackedItems[i] == null;
				if (flag2)
				{
					InteractableItemPatches.TrackedItems.RemoveAt(i);
				}
			}
		}
		bool flag3 = (bool)InteractableItemPatches.WasResetField.GetValue(instance);
		if (flag3)
		{
			ItemManager.clampedItems.RemoveFast(instance);
		}
	}
	[HookMethodAttribute(typeof(InteractableItem), "use", new Type[] { })]
	private static void ItemUsePatch(InteractableItem instance)
	{
		Logger.LogUser(string.Concat(new object[]
		{
			"[+] Picked up ",
			instance.asset.name,
			" ID ",
			instance.asset.id
		}));
		ItemManager.takeItem(instance.transform.parent, byte.MaxValue, byte.MaxValue, 0, byte.MaxValue);
	}
	public static FieldInfo WasResetField;
	public static List<InteractableItem> TrackedItems = new List<InteractableItem>();
	[SerializeField]
	private static MonoBehaviour monoBehaviour;
	public static float LastCleanupTime = 0f;
}
