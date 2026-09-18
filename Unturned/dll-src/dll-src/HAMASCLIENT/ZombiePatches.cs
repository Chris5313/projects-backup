using System;
using System.Collections.Generic;
using System.Reflection;
using SDG.Unturned;
public class ZombiePatches
{
	[InitializeAttribute]
	private static void Initialize()
	{
		Provider.onClientDisconnected = (Provider.ClientDisconnected)Delegate.Combine(Provider.onClientDisconnected, new Provider.ClientDisconnected(ZombiePatches.OnClientDisconnected));
	}
	public static void OnClientDisconnected()
	{
		ZombiePatches.TrackedZombies.Clear();
	}
	[HookMethodAttribute(typeof(Zombie), "init", BindingFlags.Instance | BindingFlags.Public, new Type[] { })]
	public static void ZombieInitPatch(Zombie instance)
	{
		ZombiePatches.TrackedZombies.Add(instance);
		OverrideManager.CallOriginal(instance, Array.Empty<object>());
	}
	public static List<Zombie> TrackedZombies = new List<Zombie>();
}
