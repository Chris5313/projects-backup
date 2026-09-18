using System;
using System.Collections.Generic;
using SDG.Unturned;
using UnityEngine;

/// <summary>
/// Network Tab - online Hamas users + account status.
/// Cloud configs live in the draggable "Cloud" window (top-bar Cloud button).
/// </summary>
public class NetworkTab : FeatureTabBase
{
	public override string GetName() => "Network";
	public override int SortId() => 3; // must be < total tab count: 11 wrote past the end of tabs[] -> invisible tab
	public override TabCount GetTabCounts() => TabCount.One;

	public static NetworkTab Instance { get; private set; }
	public NetworkTab() { Instance = this; }

	private Vector2 _usersScroll = Vector2.zero;
	private string _userSearch = "";

	public override void DoTab(TabCount tc)
	{
		DrawUsersAndStatus();
	}

	// ═══════════════════════════════════════════════════════════════════
	// ONLINE USERS + IDENTITY
	// ═══════════════════════════════════════════════════════════════════
	private void DrawUsersAndStatus()
	{
		// ── Your username (compact status; editing happens in the Cloud window) ──
		base.DrawSectionHeader("Your Username");
		GUILayout.BeginHorizontal();
		if (!HamasNetwork.IsAuthenticated)
		{
			GUI.color = Color.red;
			GUILayout.Label("Not connected - restart loader");
			GUI.color = Color.white;
		}
		else
		{
			string cur = HamasNetwork.DisplayName;
			if (string.IsNullOrEmpty(cur))
			{
				GUI.color = Color.yellow;
				GUILayout.Label("No username set");
				GUI.color = Color.white;
			}
			else
			{
				GUI.color = Color.green;
				GUILayout.Label("Your name: " + cur);
				GUI.color = Color.white;
			}
			if (MenuGuiHelper.Button(string.IsNullOrEmpty(cur) ? "Choose Name" : "Change", 90, true, "Opens the Cloud window"))
			{
				MenuState.showCloudConfigs = true;
				HamasNetwork.RefreshCloudConfigs();
			}
		}
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();

		GUILayout.Space(8);

		// ── Connection status ──
		base.DrawSectionHeader("Network Status");
		GUILayout.BeginHorizontal();
		bool connected = HamasNetwork.IsAuthenticated;
		GUI.color = connected ? Color.green : Color.red;
		GUILayout.Label(connected ? "● Connected" : "● Not Connected", GUILayout.Width(120));
		GUI.color = Color.white;

		if (HamasNetwork.GhostMode)
		{
			GUI.color = Color.cyan;
			GUILayout.Label("[GHOST]", GUILayout.Width(60));
			GUI.color = Color.white;
		}

		GUILayout.Label("Users: " + HamasNetwork.OnlineCount, GUILayout.Width(80));
		GUI.color = new Color(0.9f, 0.7f, 0.1f);
		GUILayout.Label("In-game: " + HamasNetwork.InGameCount, GUILayout.Width(85));
		GUI.color = Color.white;
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();

		GUILayout.Space(8);

		// ── Online users ──
		base.DrawSectionHeader("Online Hamas Users");

		GUILayout.BeginHorizontal();
		GUILayout.Label("Search:", GUILayout.Width(50));
		_userSearch = GUILayout.TextField(_userSearch, GUILayout.Width(150));
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();

		GUILayout.Space(5);

		_usersScroll = GUILayout.BeginScrollView(_usersScroll, GUILayout.ExpandHeight(true));

		var onlineUsers = HamasNetwork.GetOnlineUsers();
		if (onlineUsers.Count == 0)
		{
			GUI.color = new Color(0.6f, 0.6f, 0.6f);
			GUILayout.Label("  No Hamas users online");
			GUI.color = Color.white;
		}
		else
		{
			foreach (ulong steamId in onlineUsers)
			{
				string charName = GetPlayerName(steamId);
				string chosen = HamasNetwork.GetOnlineName(steamId);

				if (!string.IsNullOrEmpty(_userSearch))
				{
					string primary = chosen ?? charName;
					if (!primary.ToLower().Contains(_userSearch.ToLower()) &&
					    !charName.ToLower().Contains(_userSearch.ToLower()) &&
					    !steamId.ToString().Contains(_userSearch))
						continue;
				}

				GUILayout.BeginHorizontal();

				// Online dot
				GUI.color = Color.green;
				GUILayout.Label("●", GUILayout.Width(15));
				GUI.color = Color.white;

				// Chosen username (primary) — falls back to in-game character name
				GUI.color = string.IsNullOrEmpty(chosen) ? new Color(0.6f, 0.6f, 0.6f) : Color.white;
				GUILayout.Label(string.IsNullOrEmpty(chosen) ? charName : chosen, GUILayout.Width(140));
				GUI.color = Color.white;

				// In server indicator
				if (IsPlayerInServer(steamId))
				{
					GUI.color = Color.yellow;
					GUILayout.Label("[IN SERVER]", GUILayout.Width(80));
					GUI.color = Color.white;
				}

				// DLL injected right now (heartbeat from their client)
				if (HamasNetwork.IsInGame(steamId))
				{
					GUI.color = new Color(0.9f, 0.4f, 1f);
					GUILayout.Label("[INJECT]", GUILayout.Width(70));
					GUI.color = Color.white;
				}

				// Steam ID (secondary info)
				GUI.color = new Color(0.6f, 0.6f, 0.6f);
				GUILayout.Label(steamId.ToString(), GUILayout.Width(150));
				GUI.color = Color.white;

				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
			}
		}

		GUILayout.EndScrollView();
	}

	// ═══════════════════════════════════════════════════════════════════
	// Helpers
	// ═══════════════════════════════════════════════════════════════════
	private string GetPlayerName(ulong steamId)
	{
		if (Provider.isConnected)
		{
			foreach (var client in Provider.clients)
			{
				if (client.playerID.steamID.m_SteamID == steamId)
					return client.playerID.characterName;
			}
		}
		string s = steamId.ToString();
		return "User " + s.Substring(Math.Max(0, s.Length - 4));
	}

	private bool IsPlayerInServer(ulong steamId)
	{
		if (!Provider.isConnected) return false;
		foreach (var client in Provider.clients)
		{
			if (client.playerID.steamID.m_SteamID == steamId)
				return true;
		}
		return false;
	}
}
