using System;
using SDG.Unturned;
using UnityEngine;
public static class RandomConeUtil
{
	[HookMethodAttribute(typeof(RandomEx), "GetRandomForwardVectorInCone", new Type[] { })]
	public static Vector3 GetRandomForwardVectorInCone(float halfAngleRadians)
	{
		halfAngleRadians *= MiscConfig.spreadMultiplier;
		halfAngleRadians = Mathf.Min(halfAngleRadians, 1.5697963f);
		float num = Mathf.Sin(halfAngleRadians * Mathf.Sqrt(UnityEngine.Random.value));
		float num2 = 6.2831855f * UnityEngine.Random.value;
		float num3 = Mathf.Cos(num2);
		float num4 = Mathf.Sin(num2);
		float num5 = num3 * num;
		float num6 = num4 * num;
		float num7 = Mathf.Sqrt(1f - num5 * num5 - num6 * num6);
		return new Vector3(num5, num6, num7);
	}
}
