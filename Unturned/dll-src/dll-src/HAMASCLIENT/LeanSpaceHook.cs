using System;
using SDG.Unturned;
using UnityEngine;
public class LeanSpaceHook
{
	[HookMethodAttribute(typeof(PlayerAnimator), "isLeanSpaceEmpty", new Type[] { })]
	private bool HookedIsLeanSpaceEmpty(Vector3 direction)
	{
		bool freeLeans = MiscConfig.freeLeans;
		bool flag = freeLeans;
		bool flag2;
		if (flag)
		{
			flag2 = true;
		}
		else
		{
			Vector3 vector = Player.player.transform.position + Player.player.transform.up * Player.player.look.heightLook;
			float radius = PlayerStance.RADIUS;
			float num = 1.2f - radius;
			Vector3 vector2 = vector + direction * num;
			flag2 = Physics.OverlapCapsuleNonAlloc(vector, vector2, radius, LeanSpaceHook.LeanOverlapColliders, RayMasks.BLOCK_LEAN) == 0;
		}
		return flag2;
	}
	private static Collider[] LeanOverlapColliders = new Collider[1];
}
