using System;
using SDG.Unturned;
using Steamworks;
using UnityEngine;
public class PlayersTab : FeatureTabBase
{
	public override string GetName()
	{
		return "Players";
	}
	public override void DoTab(TabCount tc)
	{
		bool flag = tc == TabCount.One;
		bool flag2 = flag;
		if (flag2)
		{
			base.DrawSectionHeader("Player List");
			GUILayout.Label("Search by name:", Array.Empty<GUILayoutOption>());
			this.PlayerSearchText = GUILayout.TextField(this.PlayerSearchText, new GUILayoutOption[] { GUILayout.Height(20f) });
			GUILayout.Space(6f);
			int num = 0;
			for (int i = 0; i < Provider.clients.Count; i++)
			{
				SteamPlayer steamPlayer = Provider.clients[i];
				bool flag3 = steamPlayer == null || steamPlayer.player == null || steamPlayer.player.channel.IsLocalPlayer;
				if (!flag3)
				{
					num++;
				}
			}
			GUILayout.Label(num.ToString() + " player" + ((num != 1) ? "s" : "") + " online", Array.Empty<GUILayoutOption>());
			GUILayout.Space(4f);
			this.PlayerListScroll = GUILayout.BeginScrollView(this.PlayerListScroll, Array.Empty<GUILayoutOption>());
			bool flag4 = false;
			for (int j = 0; j < Provider.clients.Count; j++)
			{
				SteamPlayer steamPlayer2 = Provider.clients[j];
				bool flag5 = steamPlayer2 == null || steamPlayer2.player == null || steamPlayer2.player.channel.IsLocalPlayer;
				if (!flag5)
				{
					bool flag6 = string.IsNullOrEmpty(this.PlayerSearchText) || steamPlayer2.playerID.characterName.ToLower().Contains(this.PlayerSearchText.ToLower()) || steamPlayer2.playerID.nickName.ToLower().Contains(this.PlayerSearchText.ToLower()) || steamPlayer2.playerID.playerName.ToLower().Contains(this.PlayerSearchText.ToLower());
					bool flag7 = !flag6;
					if (!flag7)
					{
						flag4 = true;
						string text = steamPlayer2.playerID.characterName;
						bool flag8 = string.IsNullOrEmpty(text);
						if (flag8)
						{
							text = steamPlayer2.playerID.playerName;
						}
						string text2 = ((steamPlayer2.player.equipment.asset != null) ? steamPlayer2.player.equipment.asset.itemName : "Unarmed");
						float num2 = 0f;
						bool flag9 = Player.player != null && steamPlayer2.player != null;
						if (flag9)
						{
							num2 = Vector3.Distance(Player.player.transform.position, steamPlayer2.player.transform.position);
						}
						GUILayout.BeginHorizontal(new GUILayoutOption[] { GUILayout.Height(30f) });
						Texture2D texture2D = GuiStyles.GetSteamAvatarIcon(steamPlayer2.playerID.steamID);
						bool flag10 = texture2D != null;
						if (flag10)
						{
							GUILayout.Label(new GUIContent(texture2D), new GUILayoutOption[]
							{
								GUILayout.Width(26f),
								GUILayout.Height(26f)
							});
						}
						else
						{
							GUILayout.Box("", new GUILayoutOption[]
							{
								GUILayout.Width(26f),
								GUILayout.Height(26f)
							});
						}
						GUILayout.Space(4f);
						string text3 = string.Format("{0}  [{1}m]  {2}", text, num2.ToString("F0"), text2);
						bool flag11 = text3.Length > 36;
						if (flag11)
						{
							text3 = text3.Substring(0, 33) + "...";
						}
						// [HAMAS] badge — user runs this client (auto-marked via network)
						bool isHamas = PlayerPriorityManager.GetPrioritySteamPlayer(steamPlayer2) == PlayerRelation.HamasUser;
						GUI.color = isHamas ? new Color(0.55f, 1f, 0.55f) : Color.white;
						bool flag12 = MenuGuiHelper.Button(text3, -1, true, null);
						GUI.color = Color.white;
						if (flag12)
						{
							PlayersTab.SelectedPlayerIndex = j;
						}
						if (isHamas)
						{
							GUI.color = new Color(0.55f, 1f, 0.55f);
							GUILayout.Label("[HAMAS]", GUILayout.Width(66f));
							GUI.color = Color.white;
						}
						GUILayout.Space(1f);
					}
				}
			}
			bool flag13 = !flag4;
			if (flag13)
			{
				GUILayout.Space(4f);
				GUILayout.Label("No players found", Array.Empty<GUILayoutOption>());
			}
			GUILayout.EndScrollView();
		}
		else
		{
			base.DrawSectionHeader("Player Details");
			bool flag14 = !Provider.isConnected;
			bool flag15 = flag14;
			if (flag15)
			{
				GUILayout.Label("Not connected to a server", Array.Empty<GUILayoutOption>());
			}
			else
			{
				bool flag16 = Provider.clients.Count <= PlayersTab.SelectedPlayerIndex;
				bool flag17 = flag16;
				if (flag17)
				{
					PlayersTab.SelectedPlayerIndex = -1;
				}
				else
				{
					bool flag18 = PlayersTab.SelectedPlayerIndex == -1;
					bool flag19 = flag18;
					if (flag19)
					{
						GUILayout.Label("Select a player from the list", Array.Empty<GUILayoutOption>());
					}
					else
					{
						SteamPlayer steamPlayer3 = Provider.clients[PlayersTab.SelectedPlayerIndex];
						Player player = steamPlayer3.player;
						GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
						Texture2D texture2D2 = GuiStyles.GetSteamAvatarIcon(steamPlayer3.playerID.steamID);
						bool flag20 = texture2D2 != null;
						if (flag20)
						{
							GUILayout.Label(new GUIContent(texture2D2), new GUILayoutOption[]
							{
								GUILayout.Width(48f),
								GUILayout.Height(48f)
							});
						}
						else
						{
							GUILayout.Box("", new GUILayoutOption[]
							{
								GUILayout.Width(48f),
								GUILayout.Height(48f)
							});
						}
						GUILayout.Space(8f);
						GUILayout.BeginVertical(Array.Empty<GUILayoutOption>());
						GUILayout.Label("<b>" + steamPlayer3.playerID.playerName + "</b>", Array.Empty<GUILayoutOption>());
						bool flag21 = MenuGuiHelper.Button("Open Steam Profile", -1, true, null);
						CSteamID csteamID;
						if (flag21)
						{
							string text4 = "https://steamcommunity.com/profiles/";
							csteamID = steamPlayer3.playerID.steamID;
							SteamFriends.ActivateGameOverlayToWebPage(text4 + csteamID.m_SteamID.ToString(), EActivateGameOverlayToWebPageMode.k_EActivateGameOverlayToWebPageMode_Default);
						}
						GUILayout.EndVertical();
						GUILayout.EndHorizontal();
						GUILayout.Space(10f);
						base.DrawSectionHeader("Identity");
						GUILayout.Label("Character Name: " + steamPlayer3.playerID.characterName, Array.Empty<GUILayoutOption>());
						GUILayout.Label("Nickname: " + steamPlayer3.playerID.nickName, Array.Empty<GUILayoutOption>());
						GUILayout.Space(8f);
						base.DrawSectionHeader("Status");
						bool flag22 = Player.player != null && player != null;
						if (flag22)
						{
							GUILayout.Label("Distance: " + Vector3.Distance(Player.player.transform.position, player.transform.position).ToString("F1") + "m", Array.Empty<GUILayoutOption>());
						}
						GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
						GUILayout.Label("Weapon:", new GUILayoutOption[] { GUILayout.Width(55f) });
						string text5 = ((player.equipment.asset != null) ? player.equipment.asset.itemName : "Unarmed");
						GUILayout.Label(text5, new GUILayoutOption[] { GUILayout.ExpandWidth(true) });
						bool flag23 = player.equipment.asset != null;
						if (flag23)
						{
							Texture2D texture2D3 = GuiStyles.GetPlayerEquippedItemIcon(player);
							bool flag24 = texture2D3 != null;
							if (flag24)
							{
								GUILayout.Label(new GUIContent(texture2D3), new GUILayoutOption[]
								{
									GUILayout.Width(28f),
									GUILayout.Height(28f)
								});
							}
						}
						GUILayout.EndHorizontal();
						GUILayout.Space(8f);
						base.DrawSectionHeader("Group Members");
						string text6 = "";
						foreach (SteamPlayer steamPlayer4 in Provider.clients)
						{
							bool flag25 = player.quests.isMemberOfSameGroupAs(steamPlayer4.player);
							bool flag26 = flag25;
							if (flag26)
							{
								bool flag27 = steamPlayer4.playerID.playerName != steamPlayer3.playerID.playerName;
								if (flag27)
								{
									text6 = text6 + steamPlayer4.playerID.playerName + ", ";
								}
							}
						}
						bool flag28 = !string.IsNullOrEmpty(text6);
						if (flag28)
						{
							text6 = text6.Substring(0, text6.Length - 2);
							GUILayout.Label(text6, Array.Empty<GUILayoutOption>());
						}
						else
						{
							GUILayout.Label("Not in a group", Array.Empty<GUILayoutOption>());
						}
						GUILayout.Space(8f);
						base.DrawSectionHeader("Equipment");
						bool flag29 = player.clothing != null;
						if (flag29)
						{
							GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
							GUILayout.BeginVertical(new GUILayoutOption[] { GUILayout.Width(120f) });
							bool flag30 = player.clothing.hatAsset != null;
							if (flag30)
							{
								GUILayout.Label("Hat: " + player.clothing.hatAsset.itemName, Array.Empty<GUILayoutOption>());
							}
							bool flag31 = player.clothing.maskAsset != null;
							if (flag31)
							{
								GUILayout.Label("Mask: " + player.clothing.maskAsset.itemName, Array.Empty<GUILayoutOption>());
							}
							bool flag32 = player.clothing.glassesAsset != null;
							if (flag32)
							{
								GUILayout.Label("Glasses: " + player.clothing.glassesAsset.itemName, Array.Empty<GUILayoutOption>());
							}
							GUILayout.EndVertical();
							GUILayout.BeginVertical(Array.Empty<GUILayoutOption>());
							bool flag33 = player.clothing.shirtAsset != null;
							if (flag33)
							{
								GUILayout.Label("Shirt: " + player.clothing.shirtAsset.itemName, Array.Empty<GUILayoutOption>());
							}
							bool flag34 = player.clothing.pantsAsset != null;
							if (flag34)
							{
								GUILayout.Label("Pants: " + player.clothing.pantsAsset.itemName, Array.Empty<GUILayoutOption>());
							}
							bool flag35 = player.clothing.vestAsset != null;
							if (flag35)
							{
								GUILayout.Label("Vest: " + player.clothing.vestAsset.itemName, Array.Empty<GUILayoutOption>());
							}
							bool flag36 = player.clothing.backpackAsset != null;
							if (flag36)
							{
								GUILayout.Label("Backpack: " + player.clothing.backpackAsset.itemName, Array.Empty<GUILayoutOption>());
							}
							GUILayout.EndVertical();
							GUILayout.EndHorizontal();
						}
						else
						{
							GUILayout.Label("No equipment data available", Array.Empty<GUILayoutOption>());
						}
						GUILayout.Space(10f);
						GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
						string text7 = "<b>Steam ID:</b> ";
						csteamID = steamPlayer3.playerID.steamID;
						GUILayout.Label(text7 + csteamID.m_SteamID.ToString(), new GUILayoutOption[] { GUILayout.ExpandWidth(true) });
						bool flag37 = MenuGuiHelper.Button("Copy ID", -1, true, null);
						if (flag37)
						{
							csteamID = steamPlayer3.playerID.steamID;
							GUIUtility.systemCopyBuffer = csteamID.m_SteamID.ToString();
						}
						GUILayout.EndHorizontal();
						GUILayout.Space(8f);
						base.DrawSectionHeader("Priority");
						PlayerRelation curRel = PlayerPriorityManager.GetPrioritySteamPlayer(steamPlayer3);
						if (PlayerPriorityManager.IsManuallySet(steamPlayer3.playerID.steamID.m_SteamID))
						{
							GUI.color = Color.yellow;
							GUILayout.Label("[MANUAL] - auto-detect paused for this player");
							GUI.color = Color.white;
						}
						GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
						if (MenuGuiHelper.Button("Friend", 55, true, null) && curRel != PlayerRelation.Friend)
						{
							PlayerPriorityManager.SetManualSteamPlayer(steamPlayer3, PlayerRelation.Friend);
						}
						if (MenuGuiHelper.Button("Enemy", 55, true, null) && curRel != PlayerRelation.Enemy)
						{
							PlayerPriorityManager.SetManualSteamPlayer(steamPlayer3, PlayerRelation.Enemy);
						}
						GUILayout.EndHorizontal();
						GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
						if (MenuGuiHelper.Button("Group", 55, true, null) && curRel != PlayerRelation.GroupMate)
						{
							PlayerPriorityManager.SetManualSteamPlayer(steamPlayer3, PlayerRelation.GroupMate);
						}
						if (MenuGuiHelper.Button("Default", 60, true, null) && curRel != PlayerRelation.Default)
						{
							PlayerPriorityManager.SetManualSteamPlayer(steamPlayer3, PlayerRelation.Default);
						}
						if (curRel != PlayerRelation.Default && GUILayout.Button("AUTO", GUILayout.Width(50f)))
						{
							// wipe manual override + let auto-detect (Hamas/group) take over again
							PlayerPriorityManager.ResetAutoSteamPlayer(steamPlayer3);
						}
						GUILayout.EndHorizontal();
						GUILayout.Space(4f);
						GUILayout.Label("Current: " + curRel + (HamasNetwork.IsHamasUser(steamPlayer3.playerID.steamID.m_SteamID) ? "  (Hamas network user)" : ""));
					}
				}
			}
		}
	}
	public static int SelectedPlayerIndex = -1;
	public Vector2 PlayerListScroll = Vector2.zero;
	public string PlayerSearchText = "";
}
