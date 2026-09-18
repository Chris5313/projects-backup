using System.Collections.Generic;
using UnityEngine;

namespace gatyware
{
	public static class ESP
	{
		// Tracer-ghost data entry API — consumed by DamageNumbers / WeaponMods.
		// Native ESP (ImGui) does all the actual rendering; these just record
		// melee/bullet hit points so the (stripped) C# ghost list stays populated.

		private struct BulletGhost
		{
			public Vector3 origin;
			public Vector3 pos;
			public float birth;
			public bool isSelf;
			public Color col;
		}

		private static List<BulletGhost> _ghosts = new List<BulletGhost>();

		public static void AddMeleeGhost(Vector3 origin, Vector3 hit)
		{
			if (!State.MeleeTracers)
			{
				return;
			}
			_ghosts.Add(new BulletGhost
			{
				origin = origin,
				pos = hit,
				birth = Time.unscaledTime,
				isSelf = true,
				col = State.MeleeTracerColor
			});
		}

		public static void AddBulletGhost(Vector3 origin, Vector3 hit, Color col)
		{
			if (!State.BulletEsp)
			{
				return;
			}
			_ghosts.Add(new BulletGhost
			{
				origin = origin,
				pos = hit,
				birth = Time.unscaledTime,
				isSelf = true,
				col = col
			});
		}
	}
}
