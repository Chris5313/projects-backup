using System;
using System.Collections.Generic;
using System.Reflection;
using SDG.Unturned;
public class InteractableTracker
{
	public static void Clear()
	{
		InteractableTracker.Storages.Clear();
		InteractableTracker.Beds.Clear();
		InteractableTracker.ClaimFlags.Clear();
		InteractableTracker.Generators.Clear();
		InteractableTracker.Sentries.Clear();
		InteractableTracker.Forages.Clear();
		InteractableTracker.Mannequins.Clear();
	}
	[InitializeAttribute]
	public static void Initialize()
	{
		InteractableTracker.NetIdField = typeof(Interactable).GetField("_netId", BindingFlags.Instance | BindingFlags.NonPublic);
		Provider.onClientDisconnected = (Provider.ClientDisconnected)Delegate.Combine(Provider.onClientDisconnected, new Provider.ClientDisconnected(InteractableTracker.Clear));
	}
	[HookMethodAttribute(typeof(Interactable), "AssignNetId", BindingFlags.Instance | BindingFlags.NonPublic, new Type[] { })]
	public static void OnAssignNetId(Interactable instance, NetId netId)
	{
		InteractableTracker.NetIdField.SetValue(instance, netId);
		NetIdRegistry.Assign(netId, instance);
		InteractableTracker.TrackInteractable(instance);
	}
	[HookMethodAttribute(typeof(Interactable), "ReleaseNetId", BindingFlags.Instance | BindingFlags.NonPublic, new Type[] { })]
	public static void OnReleaseNetId(Interactable instance, NetId netId)
	{
		NetIdRegistry.Release(netId);
		((NetId)InteractableTracker.NetIdField.GetValue(instance)).Clear();
		InteractableTracker.UntrackInteractable(instance);
	}
	public static void TrackInteractable(Interactable i)
	{
		bool flag = i is InteractableStorage;
		bool flag2 = flag;
		if (flag2)
		{
			InteractableTracker.Storages.Add(i as InteractableStorage);
		}
		else
		{
			bool flag3 = i is InteractableBed;
			bool flag4 = flag3;
			if (flag4)
			{
				InteractableTracker.Beds.Add(i as InteractableBed);
			}
			else
			{
				bool flag5 = i is InteractableClaim;
				bool flag6 = flag5;
				if (flag6)
				{
					InteractableTracker.ClaimFlags.Add(i as InteractableClaim);
				}
				else
				{
					bool flag7 = i is InteractableGenerator;
					bool flag8 = flag7;
					if (flag8)
					{
						InteractableTracker.Generators.Add(i as InteractableGenerator);
					}
					else
					{
						bool flag9 = i is InteractableSentry;
						bool flag10 = flag9;
						if (flag10)
						{
							InteractableTracker.Sentries.Add(i as InteractableSentry);
						}
						else
						{
							bool flag11 = i is InteractableForage;
							bool flag12 = flag11;
							if (flag12)
							{
								InteractableTracker.Forages.Add(i as InteractableForage);
							}
							else
							{
								bool flag13 = i is InteractableMannequin;
								bool flag14 = flag13;
								if (flag14)
								{
									InteractableTracker.Mannequins.Add(i as InteractableMannequin);
								}
							}
						}
					}
				}
			}
		}
	}
	public static void UntrackInteractable(Interactable i)
	{
		bool flag = i is InteractableStorage;
		bool flag2 = flag;
		if (flag2)
		{
			InteractableTracker.Storages.Remove(i as InteractableStorage);
		}
		else
		{
			bool flag3 = i is InteractableBed;
			bool flag4 = flag3;
			if (flag4)
			{
				InteractableTracker.Beds.Remove(i as InteractableBed);
			}
			else
			{
				bool flag5 = i is InteractableClaim;
				bool flag6 = flag5;
				if (flag6)
				{
					InteractableTracker.ClaimFlags.Remove(i as InteractableClaim);
				}
				else
				{
					bool flag7 = i is InteractableGenerator;
					bool flag8 = flag7;
					if (flag8)
					{
						InteractableTracker.Generators.Remove(i as InteractableGenerator);
					}
					else
					{
						bool flag9 = i is InteractableSentry;
						bool flag10 = flag9;
						if (flag10)
						{
							InteractableTracker.Sentries.Remove(i as InteractableSentry);
						}
						else
						{
							bool flag11 = i is InteractableForage;
							bool flag12 = flag11;
							if (flag12)
							{
								InteractableTracker.Forages.Remove(i as InteractableForage);
							}
							else
							{
								bool flag13 = i is InteractableMannequin;
								bool flag14 = flag13;
								if (flag14)
								{
									InteractableTracker.Mannequins.Remove(i as InteractableMannequin);
								}
							}
						}
					}
				}
			}
		}
	}
	public static FieldInfo NetIdField;
	public static List<InteractableStorage> Storages = new List<InteractableStorage>();
	public static List<InteractableBed> Beds = new List<InteractableBed>();
	public static List<InteractableClaim> ClaimFlags = new List<InteractableClaim>();
	public static List<InteractableGenerator> Generators = new List<InteractableGenerator>();
	public static List<InteractableSentry> Sentries = new List<InteractableSentry>();
	public static List<InteractableForage> Forages = new List<InteractableForage>();
	public static List<InteractableMannequin> Mannequins = new List<InteractableMannequin>();
}
