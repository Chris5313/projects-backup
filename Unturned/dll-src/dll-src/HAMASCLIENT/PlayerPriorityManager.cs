using System;
using System.Collections.Generic;
using System.Threading;
using SDG.Unturned;

/// <summary>
/// Per-player relation tracking with manual overrides.
///
/// Relations: Default -> Friend -> Enemy -> GroupMate -> HamasUser (Next<> cycles).
///
/// Auto logic (3s loop + HamasNetwork poll):
///  - Hamas users (DLL authenticated, from /online) are auto-marked HamasUser
///  - Same-group players are auto-marked GroupMate
///  - Offline Hamas users drop back to Default
/// Manual Friend/Enemy (PlayersTab buttons, middle-click aim) always wins:
/// auto logic never overwrites a manual override until the user resets it.
/// </summary>
public static class PlayerPriorityManager
{
	/// <summary>Steam IDs the user manually set (any relation). Blocks auto re-marking.</summary>
	public static readonly HashSet<ulong> ManualOverrides = new HashSet<ulong>();

	public static bool IsManuallySet(ulong steamId)
	{
		return ManualOverrides.Contains(steamId);
	}

	/// <summary>Manual set from a ulong id — records the override (blocks auto until ResetAuto).</summary>
	public static void SetManual(ulong steamId, PlayerRelation priority)
	{
		PlayerPriorityManager.SetPriority(steamId, priority);
		ManualOverrides.Add(steamId);
	}

	public static void SetManualPlayer(Player p, PlayerRelation priority)
	{
		PlayerPriorityManager.SetManual(p.channel.owner.playerID.steamID.m_SteamID, priority);
	}

	public static void SetManualSteamPlayer(SteamPlayer sp, PlayerRelation priority)
	{
		PlayerPriorityManager.SetManual(sp.playerID.steamID.m_SteamID, priority);
	}

	/// <summary>Clear the manual override and return to Default (auto logic resumes).</summary>
	public static void ResetAuto(ulong steamId)
	{
		ManualOverrides.Remove(steamId);
		PlayerPriorityManager.SetPriority(steamId, PlayerRelation.Default);
	}

	public static void ResetAutoPlayer(Player p)
	{
		PlayerPriorityManager.ResetAuto(p.channel.owner.playerID.steamID.m_SteamID);
	}

	public static void ResetAutoSteamPlayer(SteamPlayer sp)
	{
		PlayerPriorityManager.ResetAuto(sp.playerID.steamID.m_SteamID);
	}

	[InitializeAttribute]
	private static void AutoDetectGroupMatesLoop()
	{
		new Thread(new ThreadStart(delegate
		{
			for (;;)
			{
				bool flag = Provider.isConnected && (bool)EspCategories.categories[0].Options[1].SortSettings;
				bool flag2 = flag;
				if (flag2)
				{
					try
					{
						foreach (SteamPlayer steamPlayer in Provider.clients)
						{
							ulong steamId = steamPlayer.playerID.steamID.m_SteamID;
							PlayerRelation currentPriority = PlayerPriorityManager.GetPrioritySteamPlayer(steamPlayer);

							// Manual Friend/Enemy wins over everything — never touch it
							if (PlayerPriorityManager.IsManuallySet(steamId))
							{
								continue;
							}
							// Auto-mark Hamas users (highest auto priority)
							else if (HamasNetwork.IsHamasUser(steamId))
							{
								if (currentPriority != PlayerRelation.HamasUser)
								{
									PlayerPriorityManager.SetPrioritySteamPlayer(steamPlayer, PlayerRelation.HamasUser);
								}
							}
							// Auto-detect group mates (only from Default)
							else if (steamPlayer.player.quests.isMemberOfSameGroupAs(Player.player))
							{
								if (currentPriority == PlayerRelation.Default)
								{
									PlayerPriorityManager.SetPrioritySteamPlayer(steamPlayer, PlayerRelation.GroupMate);
								}
							}
							// Remove GroupMate if no longer in group
							else if (currentPriority == PlayerRelation.GroupMate)
							{
								PlayerPriorityManager.SetPrioritySteamPlayer(steamPlayer, PlayerRelation.Default);
							}
						}
					}
					catch
					{
					}
				}
				Thread.Sleep(3000);
			}
		})).Start();
	}
	public static bool IsFriendOrGroupMatePlayer(Player p)
	{
		PlayerRelation rel = PlayerPriorityManager.GetPriority(p.channel.owner.playerID.steamID.m_SteamID);
		return rel == PlayerRelation.Friend || rel == PlayerRelation.GroupMate || rel == PlayerRelation.HamasUser;
	}
	public static bool IsFriendOrGroupMateSteamPlayer(SteamPlayer sp)
	{
		PlayerRelation rel = PlayerPriorityManager.GetPriority(sp.playerID.steamID.m_SteamID);
		return rel == PlayerRelation.Friend || rel == PlayerRelation.GroupMate || rel == PlayerRelation.HamasUser;
	}
	public static bool IsHamasUserPlayer(Player p)
	{
		return PlayerPriorityManager.GetPriority(p.channel.owner.playerID.steamID.m_SteamID) == PlayerRelation.HamasUser;
	}
	public static bool IsHamasUserSteamPlayer(SteamPlayer sp)
	{
		return PlayerPriorityManager.GetPriority(sp.playerID.steamID.m_SteamID) == PlayerRelation.HamasUser;
	}
	public static bool IsEnemyPlayer(Player p)
	{
		return PlayerPriorityManager.GetPriority(p.channel.owner.playerID.steamID.m_SteamID) == PlayerRelation.Enemy;
	}
	public static bool IsEnemySteamPlayer(SteamPlayer sp)
	{
		return PlayerPriorityManager.GetPriority(sp.playerID.steamID.m_SteamID) == PlayerRelation.Enemy;
	}
	public static PlayerRelation GetPriorityPlayer(Player p)
	{
		return PlayerPriorityManager.GetPriority(p.channel.owner.playerID.steamID.m_SteamID);
	}
	public static PlayerRelation GetPrioritySteamPlayer(SteamPlayer sp)
	{
		return PlayerPriorityManager.GetPriority(sp.playerID.steamID.m_SteamID);
	}
	public static PlayerRelation GetPriority(ulong steamId)
	{
		PlayerRelation dbcfkWLnn9d8dtQQBa0x7Cya;
		bool flag = PlayerPriorityManager.Priorities.TryGetValue(steamId, out dbcfkWLnn9d8dtQQBa0x7Cya);
		bool flag2 = flag;
		PlayerRelation dbcfkWLnn9d8dtQQBa0x7Cya2;
		if (flag2)
		{
			dbcfkWLnn9d8dtQQBa0x7Cya2 = dbcfkWLnn9d8dtQQBa0x7Cya;
		}
		else
		{
			PlayerPriorityManager.Priorities.Add(steamId, PlayerRelation.Default);
			dbcfkWLnn9d8dtQQBa0x7Cya2 = PlayerRelation.Default;
		}
		return dbcfkWLnn9d8dtQQBa0x7Cya2;
	}
	public static void SetPriorityPlayer(Player p, PlayerRelation priority)
	{
		PlayerPriorityManager.SetPriority(p.channel.owner.playerID.steamID.m_SteamID, priority);
	}
	public static void SetPrioritySteamPlayer(SteamPlayer sp, PlayerRelation priority)
	{
		PlayerPriorityManager.SetPriority(sp.playerID.steamID.m_SteamID, priority);
	}
	public static void SetPriority(ulong steamId, PlayerRelation priority)
	{
		bool flag = PlayerPriorityManager.Priorities.ContainsKey(steamId);
		bool flag2 = flag;
		if (flag2)
		{
			PlayerPriorityManager.Priorities[steamId] = priority;
		}
		else
		{
			PlayerPriorityManager.Priorities.Add(steamId, priority);
		}
	}
	public static Dictionary<ulong, PlayerRelation> Priorities = new Dictionary<ulong, PlayerRelation>();
}
