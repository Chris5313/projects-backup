using System;
using SDG.Unturned;
using UnityEngine;
public class EffectManagerHooks
{
	[HookMethodAttribute(typeof(EffectManager), "internalSpawnEffect", new Type[]
	{
		typeof(EffectAsset),
		typeof(Vector3),
		typeof(Quaternion),
		typeof(Vector3),
		typeof(bool),
		typeof(Transform)
	})]
	internal static Transform InternalSpawnEffectHook(EffectAsset asset, Vector3 point, Quaternion rotation, Vector3 scaleMultiplier, bool wasInstigatedByPlayer, Transform parent)
	{
		bool disallowParticles = Settings.disallowParticles;
		bool flag = disallowParticles;
		Transform transform;
		if (flag)
		{
			transform = null;
		}
		else
		{
			transform = OverrideManager.CallOriginalInstance<Transform>(null, new object[] { asset, point, rotation, scaleMultiplier, wasInstigatedByPlayer, parent });
		}
		return transform;
	}
}
