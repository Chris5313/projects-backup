using System;
using SDG.Unturned;
using UnityEngine;
public class PlayerFinderTab : FeatureTabBase
{
	public override string GetName()
	{
		return "Player Finder";
	}
	public override TabCount GetTabCounts()
	{
		return TabCount.Three;
	}
	public override void DoTab(TabCount tc)
	{
		bool flag = tc == TabCount.One;
		bool flag2 = flag;
		if (flag2)
		{
			bool flag3 = PlayTimeScanner.scanResults.Count == 0;
			bool flag4 = flag3;
			if (flag4)
			{
				GUILayout.Label("No players found", Array.Empty<GUILayoutOption>());
			}
			else
			{
				GUILayout.Label("Found:", Array.Empty<GUILayoutOption>());
				foreach (ConnectionLogEntry d5bADMxbfCFvG3WY9Gu4DXxc in PlayTimeScanner.scanResults)
				{
					GUILayout.Label(string.Format("[{0}:{1}] {2} ({3})", new object[]
					{
						Parser.getIPFromUInt32(d5bADMxbfCFvG3WY9Gu4DXxc.Ip),
						d5bADMxbfCFvG3WY9Gu4DXxc.Port,
						d5bADMxbfCFvG3WY9Gu4DXxc.Nickname,
						d5bADMxbfCFvG3WY9Gu4DXxc.Time
					}), Array.Empty<GUILayoutOption>());
				}
			}
		}
		else
		{
			bool flag5 = tc == TabCount.Two;
			bool flag6 = flag5;
			if (flag6)
			{
				bool flag7 = !PlayTimeScanner.scanFinished && !PlayTimeScanner.scanning;
				bool flag8 = flag7;
				if (flag8)
				{
					GUILayout.Label("No data saved currently", Array.Empty<GUILayoutOption>());
				}
				else
				{
					bool du63rJdwVDiFSuAn8z1xmL2WA = PlayTimeScanner.scanFinished;
					bool flag9 = du63rJdwVDiFSuAn8z1xmL2WA;
					if (flag9)
					{
						GUILayout.Label(string.Format("Scanned all servers ({0})", Provider.provider.matchmakingService.serverList.Count), Array.Empty<GUILayoutOption>());
					}
					else
					{
						bool flag10 = PlayTimeScanner.scanning && !PlayTimeScanner.searchCancelled;
						bool flag11 = flag10;
						if (flag11)
						{
							GUILayout.Label(string.Format("Fetching servers... (currently fetched {0})", Provider.provider.matchmakingService.serverList.Count), Array.Empty<GUILayoutOption>());
						}
						else
						{
							bool dcsepIcVHGAxnfTORA2ru3dMD = PlayTimeScanner.scanning;
							bool flag12 = dcsepIcVHGAxnfTORA2ru3dMD;
							if (flag12)
							{
								GUILayout.Label(string.Format("Servers fetched ({0})", Provider.provider.matchmakingService.serverList.Count), Array.Empty<GUILayoutOption>());
								GUILayout.Label(string.Format("Searching players ({0}/{1})", PlayTimeScanner.nextServerIndex, Provider.provider.matchmakingService.serverList.Count), Array.Empty<GUILayoutOption>());
							}
						}
					}
				}
			}
			else
			{
				bool flag13 = !PlayTimeScanner.scanning;
				bool flag14 = flag13;
				if (flag14)
				{
					GUILayout.Label("Enter username to search on unturned servers", Array.Empty<GUILayoutOption>());
					this.SearchUsername = GUILayout.TextField(this.SearchUsername, Array.Empty<GUILayoutOption>());
					bool flag15 = MenuGuiHelper.Button("Start Searching", -1, true, null);
					bool flag16 = flag15;
					if (flag16)
					{
						PlayTimeScanner.instance.StartScan(this.SearchUsername);
					}
				}
				else
				{
					bool flag17 = MenuGuiHelper.Button("Stop searching", -1, true, null);
					bool flag18 = flag17;
					if (flag18)
					{
						PlayTimeScanner.instance.StopScan();
					}
				}
			}
		}
	}
	public string SearchUsername = "";
}
