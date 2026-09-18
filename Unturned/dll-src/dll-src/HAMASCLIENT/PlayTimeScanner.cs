using System;
using System.Collections;
using System.Collections.Generic;
using SDG.Unturned;
using UnityEngine;
public class PlayTimeScanner : MonoBehaviour
{
	public void Awake()
	{
		PlayTimeScanner.instance = this;
		PlayTimeScanner.serverListFilters = new ServerListFilters();
		PlayTimeScanner.serverListFilters.attendance = EAttendance.HasPlayers;
		PlayTimeScanner.serverListFilters.vacProtection = EVACProtectionFilter.Any;
		PlayTimeScanner.serverListFilters.workshop = EWorkshop.ANY;
		PlayTimeScanner.serverListFilters.plugins = EPlugins.ANY;
		PlayTimeScanner.serverListFilters.password = EPassword.NO;
		PlayTimeScanner.serverListFilters.camera = ECameraMode.ANY;
		PlayTimeScanner.serverListFilters.thirdpartyAntiCheatProtection = EThirdpartyAntiCheatProtectionFilter.Any;
		PlayTimeScanner.serverListFilters.cheats = ECheats.ANY;
		PlayTimeScanner.serverListFilters.notFull = true;
		PlayTimeScanner.serverListFilters.gold = EServerListGoldFilter.Any;
		PlayTimeScanner.serverListFilters.listSource = ESteamServerList.INTERNET;
		PlayTimeScanner.serverListFilters.combat = ECombat.ANY;
		PlayTimeScanner.serverListFilters.monetization = EServerMonetizationTag.Any;
	}
	public void StartScan(string nickName)
	{
		bool flag = !string.IsNullOrEmpty(nickName);
		if (flag)
		{
			PlayTimeScanner.searchNickname = nickName;
			PlayTimeScanner.pendingQueries.Clear();
			PlayTimeScanner.scanResults.Clear();
			PlayTimeScanner.nextServerIndex = 0;
			PlayTimeScanner.searchCancelled = false;
			PlayTimeScanner.scanFinished = false;
			PlayTimeScanner.scanning = true;
			this.RefreshMasterServer();
		}
	}
	public void RefreshMasterServer()
	{
		this.scanCoroutine = base.StartCoroutine(this.ScanCoroutine());
		Provider.provider.matchmakingService.refreshMasterServer(PlayTimeScanner.serverListFilters);
	}
	public IEnumerator ScanCoroutine()
	{
		PlayTimeScanner._DTeMCNVvpp0I11wjHJIN0qgtI_d__3 _DTeMCNVvpp0I11wjHJIN0qgtI_d__ = new PlayTimeScanner._DTeMCNVvpp0I11wjHJIN0qgtI_d__3(0);
		_DTeMCNVvpp0I11wjHJIN0qgtI_d__._4__this = this;
		return _DTeMCNVvpp0I11wjHJIN0qgtI_d__;
	}
	public void StopScan()
	{
		for (int i = 0; i < PlayTimeScanner.pendingQueries.Count; i++)
		{
			PlayTimeScanner.pendingQueries[i].Cancel();
		}
		bool flag = this.scanCoroutine != null;
		if (flag)
		{
			base.StopCoroutine(this.scanCoroutine);
			this.scanCoroutine = null;
		}
		PlayTimeScanner.pendingQueries.Clear();
		PlayTimeScanner.scanning = false;
	}
	public void OnPlayerTimeReceived(string nickname, float playTime, int index)
	{
		bool dcsepIcVHGAxnfTORA2ru3dMD = PlayTimeScanner.scanning;
		if (dcsepIcVHGAxnfTORA2ru3dMD)
		{
			for (int i = 0; i < PlayTimeScanner.pendingQueries.Count; i++)
			{
				bool flag = PlayTimeScanner.pendingQueries[i].ServerIndex == index;
				if (flag)
				{
					bool flag2 = !string.IsNullOrEmpty(nickname);
					if (flag2)
					{
						TimeSpan timeSpan = TimeSpan.FromSeconds((double)playTime);
						string text = string.Empty;
						bool flag3 = timeSpan.Days > 0;
						if (flag3)
						{
							text = text + " " + timeSpan.Days.ToString() + "d";
						}
						bool flag4 = timeSpan.Hours > 0;
						if (flag4)
						{
							text = text + " " + timeSpan.Hours.ToString() + "h";
						}
						bool flag5 = timeSpan.Minutes > 0;
						if (flag5)
						{
							text = text + " " + timeSpan.Minutes.ToString() + "m";
						}
						bool flag6 = timeSpan.Seconds > 0;
						if (flag6)
						{
							text = text + " " + timeSpan.Seconds.ToString() + "s";
						}
						ConnectionLogEntry d5bADMxbfCFvG3WY9Gu4DXxc = new ConnectionLogEntry(PlayTimeScanner.pendingQueries[i].ServerIp, PlayTimeScanner.pendingQueries[i].ServerPort, nickname.ToLower(), text);
						PlayTimeScanner.scanResults.Add(d5bADMxbfCFvG3WY9Gu4DXxc);
					}
					PlayTimeScanner.pendingQueries.RemoveAt(i);
					break;
				}
			}
			bool flag7 = Provider.provider.matchmakingService.serverList.Count == PlayTimeScanner.nextServerIndex && PlayTimeScanner.pendingQueries.Count == 0;
			if (flag7)
			{
				PlayTimeScanner.scanFinished = true;
				PlayTimeScanner.scanning = false;
			}
			else
			{
				bool flag8 = Provider.provider.matchmakingService.serverList.Count != PlayTimeScanner.nextServerIndex;
				if (flag8)
				{
					SteamServerAdvertisement steamServerAdvertisement = Provider.provider.matchmakingService.serverList[PlayTimeScanner.nextServerIndex];
					ServerPlayerQuery d3jHiaQeLVJhKeFOEEaBExLtT = new ServerPlayerQuery(steamServerAdvertisement.ip, steamServerAdvertisement.queryPort, steamServerAdvertisement.connectionPort, PlayTimeScanner.nextServerIndex, new Action<string, float, int>(this.OnPlayerTimeReceived));
					PlayTimeScanner.pendingQueries.Add(d3jHiaQeLVJhKeFOEEaBExLtT);
					d3jHiaQeLVJhKeFOEEaBExLtT.StartQuery();
					PlayTimeScanner.nextServerIndex++;
				}
			}
		}
	}
	public static ServerListFilters serverListFilters = null;
	public const int maxConcurrentQueries = 5;
	public static string searchNickname = "";
	public static List<ServerPlayerQuery> pendingQueries = new List<ServerPlayerQuery>();
	public static List<ConnectionLogEntry> scanResults = new List<ConnectionLogEntry>();
	public static int nextServerIndex = 0;
	public static bool searchCancelled = false;
	public static bool scanFinished = false;
	public static bool scanning = false;
	public static PlayTimeScanner instance;
	public Coroutine scanCoroutine;
	private sealed class _DTeMCNVvpp0I11wjHJIN0qgtI_d__3 : IEnumerator
	{
		private int state;
		private object current;
		public PlayTimeScanner _4__this;

		public _DTeMCNVvpp0I11wjHJIN0qgtI_d__3(int state)
		{
			this.state = state;
		}

		public object Current
		{
			get { return current; }
		}

		public bool MoveNext()
		{
			return false;
		}

		public void Reset()
		{
		}
	}
}
