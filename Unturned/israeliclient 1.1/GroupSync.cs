using System.Collections.Generic;
using SDG.Unturned;
using UnityEngine;

namespace gatyware
{
	/// <summary>
	/// Automatically marks players in the local player's group as Friends,
	/// and removes the Friend tag when they leave the group.
	/// </summary>
	public static class GroupSync
	{
		// Keys of players we auto-friended, so we can clean up when they leave
		private static readonly HashSet<string> _autoFriended = new HashSet<string>();
		private static float _nextScan;
		private const float SCAN_INTERVAL = 1.5f;

		private static string Key(SteamPlayer sp)
		{
			return sp.playerID.steamID.ToString();
		}

		public static void Update()
		{
			if (Time.realtimeSinceStartup < _nextScan) return;
			_nextScan = Time.realtimeSinceStartup + SCAN_INTERVAL;

			Player local = Player.LocalPlayer;
			if (local == null || local.quests == null) return;
			if (!local.quests.isMemberOfAGroup)
			{
				ClearAutoFriends();
				return;
			}

			List<SteamPlayer> clients = Provider.clients;
			if (clients == null) return;

			// Build set of current group member keys
			HashSet<string> inGroup = new HashSet<string>();
			for (int i = 0; i < clients.Count; i++)
			{
				SteamPlayer sp = clients[i];
				if (sp?.player == null) continue;
				if (sp.player == local) continue;
				if (!local.quests.isMemberOfSameGroupAs(sp.player)) continue;
				inGroup.Add(Key(sp));
			}

			// Friend anyone newly in the group
			for (int i = 0; i < clients.Count; i++)
			{
				SteamPlayer sp = clients[i];
				if (sp?.player == null) continue;
				if (sp.player == local) continue;
				string k = Key(sp);
				if (!inGroup.Contains(k)) continue;
				if (_autoFriended.Contains(k)) continue;
				_autoFriended.Add(k);
				PlayerRelation.Set(sp, RelationType.Friend);
			}

			// Un-friend anyone who left the group
			List<string> toRemove = null;
			foreach (string k in _autoFriended)
			{
				if (!inGroup.Contains(k))
				{
					if (toRemove == null) toRemove = new List<string>();
					toRemove.Add(k);
				}
			}
			if (toRemove == null) return;
			foreach (string k in toRemove)
			{
				_autoFriended.Remove(k);
				SteamPlayer sp = FindByKey(k, clients);
				if (sp != null && PlayerRelation.IsFriend(sp))
					PlayerRelation.Set(sp, RelationType.Default);
			}
		}

		private static void ClearAutoFriends()
		{
			if (_autoFriended.Count == 0) return;
			List<SteamPlayer> clients = Provider.clients;
			foreach (string k in _autoFriended)
			{
				if (clients == null) break;
				SteamPlayer sp = FindByKey(k, clients);
				if (sp != null && PlayerRelation.IsFriend(sp))
					PlayerRelation.Set(sp, RelationType.Default);
			}
			_autoFriended.Clear();
		}

		private static SteamPlayer FindByKey(string key, List<SteamPlayer> clients)
		{
			for (int i = 0; i < clients.Count; i++)
			{
				if (clients[i] != null && clients[i].playerID.steamID.ToString() == key)
					return clients[i];
			}
			return null;
		}
	}
}
