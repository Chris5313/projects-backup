using System;
using Steamworks;
public class ServerPlayerQuery
{
	public ServerPlayerQuery(uint serverIP, ushort serverPort, ushort connectionPort, int index, Action<string, float, int> onPlayersRefreshed)
	{
		ServerPlayerQuery._c__DisplayClass0_0 CS_8__locals1 = new ServerPlayerQuery._c__DisplayClass0_0();
		CS_8__locals1.onPlayersRefreshed = onPlayersRefreshed;
		CS_8__locals1.index = index;
		this.ServerIp = serverIP;
		this.ServerPort = serverPort;
		this.ConnectionPort = connectionPort;
		this.OnPlayersRefreshed = CS_8__locals1.onPlayersRefreshed;
		this.ServerIndex = CS_8__locals1.index;
		this.QueryHandle = HServerQuery.Invalid;
		this.FoundPlayerName = "";
		this.FoundPlayerPlaytime = 0f;
		this.PlayersResponseCallback = new ISteamMatchmakingPlayersResponse(delegate(string playerName, int score, float playTime)
		{
			bool flag = playerName.ToLower().Contains(PlayTimeScanner.searchNickname.ToLower());
			bool flag2 = flag;
			if (flag2)
			{
				this.FoundPlayerName = playerName;
				this.FoundPlayerPlaytime = playTime;
			}
		}, delegate
		{
			this.StartQuery();
		}, delegate
		{
			this.QueryHandle = HServerQuery.Invalid;
			CS_8__locals1.onPlayersRefreshed(this.FoundPlayerName, this.FoundPlayerPlaytime, CS_8__locals1.index);
		});
	}
	public void StartQuery()
	{
		bool flag = this.RetryCount > 4;
		bool flag2 = flag;
		if (flag2)
		{
			this.QueryHandle = HServerQuery.Invalid;
			this.OnPlayersRefreshed("", 0f, this.ServerIndex);
		}
		else
		{
			this.RetryCount++;
			this.QueryHandle = SteamMatchmakingServers.PlayerDetails(this.ServerIp, this.ServerPort, this.PlayersResponseCallback);
		}
	}
	public void Cancel()
	{
		bool flag = this.QueryHandle != HServerQuery.Invalid;
		bool flag2 = flag;
		if (flag2)
		{
			SteamMatchmakingServers.CancelServerQuery(this.QueryHandle);
			this.QueryHandle = HServerQuery.Invalid;
		}
	}
	public uint ServerIp;
	public ushort ServerPort;
	public ushort ConnectionPort;
	public int ServerIndex;
	public Action<string, float, int> OnPlayersRefreshed;
	public string FoundPlayerName = "";
	public float FoundPlayerPlaytime = 0f;
	public HServerQuery QueryHandle;
	public ISteamMatchmakingPlayersResponse PlayersResponseCallback;
	public int RetryCount = 0;
	private sealed class _c__DisplayClass0_0
	{
		public Action<string, float, int> onPlayersRefreshed;
		public int index;
	}
}
