using System;
using UnityEngine;
using SDG.Unturned;

public static class SilentAim
{
	public static object LockedTarget;
	public static Vector3 LockedTargetPos;

	// Debug trace — logs melee silent aim decisions to logs.txt (Unturned_Data)
	public static bool DebugTrace = true;
	private static object s_LastLoggedTarget = null;

	// -----------------------------------------------------------------------
	// TryForgeMeleeHit — core of melee silent aim.
	// Returns true and fills `ri` when a valid, visible locked target exists.
	// Returns false otherwise (caller falls back to a normal raycast).
	// Used by BOTH melee weapons (UseableMelee.fire) and fists (punch).
	// -----------------------------------------------------------------------
	public static bool TryForgeMeleeHit(Ray ray, float range, int mask, Player ignorePlayer, out RaycastInfo ri)
	{
		ri = null;
		if (LockedTarget == null) return false;

		Vector3 origin = ray.origin;
		Vector3 targetCenter;
		Transform targetTransform;
		Collider targetCollider = null;

		if (LockedTarget is Player tp)
		{
			if (tp == null || tp.life == null || tp.life.isDead) return false;
			targetTransform = tp.transform;
			targetCenter = (tp.movement != null && tp.movement.controller != null)
				? tp.transform.position + tp.movement.controller.center
				: (tp.look != null ? tp.look.aim.position : tp.transform.position + Vector3.up * 1.6f);
			var cols = tp.GetComponentsInChildren<Collider>();
			if (cols != null && cols.Length > 0) targetCollider = cols[0];
		}
		else if (LockedTarget is Zombie tz)
		{
			if (tz == null || tz.isDead) return false;
			targetTransform = tz.transform;
			targetCenter = tz.transform.position + Vector3.up * 1f;
			var cols = tz.GetComponentsInChildren<Collider>();
			if (cols != null && cols.Length > 0) targetCollider = cols[0];
		}
		else if (LockedTarget is Animal ta)
		{
			if (ta == null || ta.isDead) return false;
			targetTransform = ta.transform;
			targetCenter = ta.transform.position + Vector3.up * 0.5f;
			var cols = ta.GetComponentsInChildren<Collider>();
			if (cols != null && cols.Length > 0) targetCollider = cols[0];
		}
		else
		{
			return false;
		}

		float dist = Vector3.Distance(origin, targetCenter);
		if (dist > range + 2f)
		{
			if (DebugTrace) Logger.LogClient("[meleeSA] target too far: " + dist.ToString("F1") + " > " + (range + 2f).ToString("F1"));
			return false;
		}

		// Wall/visibility check — scan multiple body points for a visible one.
		// ENEMY and VEHICLE excluded so the target's own collider doesn't block the line.
		int wallMask = RayMasks.DAMAGE_CLIENT & ~RayMasks.ENEMY & ~RayMasks.VEHICLE;
		Vector3[] scanPts;
		if (LockedTarget is Player sp)
		{
			Vector3 pos = sp.transform.position;
			Vector3 aim = sp.look != null ? sp.look.aim.position : pos + Vector3.up * 1.6f;
			Vector3 r = sp.look != null ? sp.look.aim.right : sp.transform.right;
			scanPts = new[] { aim, aim + r * 0.4f, aim - r * 0.4f, pos + Vector3.up * 1.5f, pos + Vector3.up * 1f, pos + Vector3.up * 0.5f };
		}
		else if (LockedTarget is Zombie)
		{
			Vector3 pos = ((Zombie)LockedTarget).transform.position;
			scanPts = new[] { pos + Vector3.up * 1.5f, pos + Vector3.up * 1f, pos + Vector3.up * 0.5f };
		}
		else
		{
			Vector3 pos = ((Animal)LockedTarget).transform.position;
			scanPts = new[] { pos + Vector3.up * 1f, pos + Vector3.up * 0.5f, pos + Vector3.up * 0.2f };
		}

		Vector3 hitPoint = scanPts[0];
		bool foundVisible = false;
		for (int i = 0; i < scanPts.Length; i++)
		{
			if (!Physics.Linecast(origin, scanPts[i], wallMask, QueryTriggerInteraction.Ignore))
			{
				hitPoint = scanPts[i];
				foundVisible = true;
				break;
			}
		}

		if (!foundVisible)
		{
			if (DebugTrace) Logger.LogClient("[meleeSA] no visible point on locked target");
			return false;
		}

		// Build RaycastInfo directly — no physics raycast needed (misses at
		// short melee range). Same pattern the Israeli Client uses.
		RaycastInfo ri2 = new RaycastInfo(targetTransform);
		ri2.point = hitPoint;
		ri2.direction = (hitPoint - origin).normalized;
		ri2.normal = -ri2.direction;
		ri2.limb = MiscConfig.replaceHitLimbToCustom ? MiscConfig.replacedHitLimb.ToLimb() : ELimb.SKULL;
		ri2.materialName = "Flesh_Dynamic";
		if (targetCollider != null) ri2.collider = targetCollider;

		if (LockedTarget is Player hitP)
		{
			ri2.player = hitP;
			ri2.transform = hitP.transform;
		}
		else if (LockedTarget is Zombie hitZ)
		{
			ri2.zombie = hitZ;
			ri2.transform = hitZ.transform;
		}
		else if (LockedTarget is Animal hitA)
		{
			ri2.animal = hitA;
			ri2.transform = hitA.transform;
		}

		ri = ri2;
		if (DebugTrace) Logger.LogClient("[meleeSA] forged hit on " + LockedTarget.GetType().Name + " @ " + hitPoint.ToString("F1"));
		return true;
	}

	// Melee-weapon entry point (UseableMeleeHooks.OnFire)
	public static RaycastInfo DoMeleeRaycast(Ray ray, float range, int mask, Player ignorePlayer)
	{
		RaycastInfo forged;
		if (TryForgeMeleeHit(ray, range, mask, ignorePlayer, out forged))
			return forged;
		return DamageTool.raycast(ray, range, mask, ignorePlayer);
	}

	// Every-frame target scan (GameLoopDriver.Update).
	// Players (respecting friends/safezone), zombies, and animals.
	public static void UpdateTarget()
	{
		var lp = Player.player;
		if (lp == null) { LockedTarget = null; return; }
		if (!AimbotConfig.enableMeleeSilentAim) { LockedTarget = null; return; }
		float maxRange = 10f;
		if (lp.equipment != null && lp.equipment.asset is ItemMeleeAsset ma)
			maxRange = ma.range + 4f;
		object best = null;
		float bestDist = float.MaxValue;
		Vector3 origin = lp.look.aim.position;
		Vector3 forward = lp.look.aim.forward;

		foreach (SteamPlayer sp in Provider.clients)
		{
			try
			{
				if (sp == null || sp.player == null || sp.player == lp || sp.player.life == null || sp.player.life.isDead) continue;
				if (PlayerPriorityManager.IsFriendOrGroupMateSteamPlayer(sp)) continue;
				if (AimbotConfig.dontShootPlayersOnSafezone && LevelNodes.isPointInsideSafezone(sp.player.transform.position, out sp.player.movement.isSafeInfo)) continue;
				Vector3 pos = sp.player.look != null ? sp.player.look.aim.position : sp.player.transform.position + Vector3.up * 1.6f;
				float d = Vector3.Distance(origin, pos);
				if (d > maxRange) continue;
				if (Vector3.Angle(forward, (pos - origin).normalized) > 150f) continue;
				if (d < bestDist) { bestDist = d; best = sp.player; }
			}
			catch { }
		}
		foreach (Zombie z in ZombiePatches.TrackedZombies)
		{
			try
			{
				if (z == null || z.isDead) continue;
				Vector3 pos = z.transform.position + Vector3.up * 1f;
				float d = Vector3.Distance(origin, pos);
				if (d > maxRange) continue;
				if (Vector3.Angle(forward, (pos - origin).normalized) > 150f) continue;
				if (d < bestDist) { bestDist = d; best = z; }
			}
			catch { }
		}
		foreach (Animal a in AnimalManager.animals)
		{
			try
			{
				if (a == null || a.isDead) continue;
				Vector3 pos = a.transform.position + Vector3.up * 0.5f;
				float d = Vector3.Distance(origin, pos);
				if (d > maxRange) continue;
				if (Vector3.Angle(forward, (pos - origin).normalized) > 150f) continue;
				if (d < bestDist) { bestDist = d; best = a; }
			}
			catch { }
		}

		LockedTarget = best;
		if (LockedTarget != null)
		{
			if (LockedTarget is Player tp)
				LockedTargetPos = tp.look != null ? tp.look.aim.position : tp.transform.position + Vector3.up * 1.6f;
			else if (LockedTarget is Zombie tz)
				LockedTargetPos = tz.transform.position + Vector3.up * 1f;
			else if (LockedTarget is Animal ta)
				LockedTargetPos = ta.transform.position + Vector3.up * 0.5f;
		}

		if (DebugTrace && LockedTarget != s_LastLoggedTarget)
		{
			if (LockedTarget == null)
				Logger.LogClient("[meleeSA] target cleared");
			else
				Logger.LogClient("[meleeSA] locked " + LockedTarget.GetType().Name + " dist=" + bestDist.ToString("F1"));
			s_LastLoggedTarget = LockedTarget;
		}
	}

	static readonly Vector3[] _corners = new Vector3[8];

	public static void DrawTargetBox()
	{
		if (!MiscConfig.silentAimTargetBox || LockedTarget == null) return;
		if (ScreenshotManager.IsSpying) return;
		if (Event.current == null || Event.current.type != EventType.Repaint) return;
		Color edgeCol = new Color(1f, 0.2f, 0.2f, 0.85f);
		Color fillCol = new Color(1f, 0.1f, 0.1f, 0.25f);
		Bounds b;
		if (LockedTarget is Player p)
		{
			float h = 2f;
			if (p.movement != null && p.movement.controller != null) h = p.movement.controller.height;
			b = new Bounds(p.transform.position + Vector3.up * (h * 0.5f), new Vector3(0.8f, h, 0.8f));
		}
		else if (LockedTarget is Zombie z)
			b = new Bounds(z.transform.position + Vector3.up * 1f, new Vector3(0.8f, 2f, 0.8f));
		else if (LockedTarget is Animal an)
			b = new Bounds(an.transform.position + Vector3.up * 0.5f, new Vector3(0.8f, 1f, 0.8f));
		else return;
		Vector3 mn = b.min, mx = b.max;
		_corners[0] = new Vector3(mn.x, mn.y, mn.z); _corners[1] = new Vector3(mx.x, mn.y, mn.z);
		_corners[2] = new Vector3(mx.x, mn.y, mx.z); _corners[3] = new Vector3(mn.x, mn.y, mx.z);
		_corners[4] = new Vector3(mn.x, mx.y, mn.z); _corners[5] = new Vector3(mx.x, mx.y, mn.z);
		_corners[6] = new Vector3(mx.x, mx.y, mx.z); _corners[7] = new Vector3(mn.x, mx.y, mx.z);
		Vector2[] sc = new Vector2[8];
		bool any = false;
		for (int i = 0; i < 8; i++)
		{
			Vector3 v = _corners[i].WorldToScreenPoint();
			if (float.IsNaN(v.x)) { sc[i] = Vector2.zero; continue; }
			sc[i] = new Vector2(v.x, Screen.height - v.y);
			any = true;
		}
		if (!any) return;
		Color origCol = GUI.color;
		void FillFace(int a, int b2, int c2, int d2)
		{
			if (a >= 8 || b2 >= 8 || c2 >= 8 || d2 >= 8) return;
			GUI.color = fillCol;
			Vector2 ca = sc[a], cb = sc[b2], cc = sc[c2], cd = sc[d2];
			float minX = Mathf.Min(ca.x, cb.x, cc.x, cd.x);
			float minY = Mathf.Min(ca.y, cb.y, cc.y, cd.y);
			float maxX = Mathf.Max(ca.x, cb.x, cc.x, cd.x);
			float maxY = Mathf.Max(ca.y, cb.y, cc.y, cd.y);
			GUI.DrawTexture(new Rect(minX, minY, maxX - minX, maxY - minY), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, fillCol, 0f, 0f);
		}
		void Edge(int a, int b2) { if (a < 8 && b2 < 8) EspDrawer.DrawLine(sc[a], sc[b2], edgeCol, 1.5f); }
		FillFace(0,1,5,4); FillFace(3,2,6,7); FillFace(0,3,7,4);
		FillFace(1,2,6,5); FillFace(4,5,6,7); FillFace(0,1,2,3);
		Edge(0,1); Edge(1,2); Edge(2,3); Edge(3,0); Edge(4,5); Edge(5,6); Edge(6,7); Edge(7,4);
		Edge(0,4); Edge(1,5); Edge(2,6); Edge(3,7);
		GUI.color = origCol;
	}
}
