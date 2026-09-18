using System;
using System.Collections.Generic;
using System.Reflection;
using SDG.Unturned;
using UnityEngine;
public class MapUiPatch
{
	// (get) Token: 0x060000CC RID: 204 RVA: 0x00009A88 File Offset: 0x00007C88
	public static Type targetType
	{
		get
		{
			return typeof(PlayerDashboardInformationUI);
		}
	}
	// (get) Token: 0x060000CD RID: 205 RVA: 0x00009AA4 File Offset: 0x00007CA4
	public static List<ISleekImage> markerImages
	{
		get
		{
			return MapUiPatch.markerImagesField.GetValue(null) as List<ISleekImage>;
		}
	}
	// (get) Token: 0x060000CE RID: 206 RVA: 0x00009AC8 File Offset: 0x00007CC8
	public static List<ISleekImage> remotePlayerImages
	{
		get
		{
			return MapUiPatch.remotePlayerImagesField.GetValue(null) as List<ISleekImage>;
		}
	}
	[InitializeAttribute]
	public static void Init()
	{
		MapUiPatch.remotePlayerImagesField = typeof(PlayerDashboardInformationUI).GetField("remotePlayerImages", BindingFlags.Static | BindingFlags.NonPublic);
		MapUiPatch.markerImagesField = typeof(PlayerDashboardInformationUI).GetField("markerImages", BindingFlags.Static | BindingFlags.NonPublic);
		MapUiPatch.mapMarkersContainerField = typeof(PlayerDashboardInformationUI).GetField("mapMarkersContainer", BindingFlags.Static | BindingFlags.NonPublic);
		MapUiPatch.mapRemotePlayersContainerField = typeof(PlayerDashboardInformationUI).GetField("mapRemotePlayersContainer", BindingFlags.Static | BindingFlags.NonPublic);
		MapUiPatch.showPlayerAvatarsToggleField = typeof(PlayerDashboardInformationUI).GetField("showPlayerAvatarsToggle", BindingFlags.Static | BindingFlags.NonPublic);
		MapUiPatch.showPlayerNamesToggleField = typeof(PlayerDashboardInformationUI).GetField("showPlayerNamesToggle", BindingFlags.Static | BindingFlags.NonPublic);
		MapUiPatch.projectWorldPositionToMapMethod = typeof(PlayerDashboardInformationUI).GetMethod("ProjectWorldPositionToMap", BindingFlags.Static | BindingFlags.NonPublic);
		try
		{
			MapUiPatch.iconsField = typeof(PlayerDashboardInformationUI).GetField("icons", BindingFlags.Static | BindingFlags.NonPublic);
		}
		catch
		{
			Logger.LogClient("PlayerDashboardInformationUI.icons field not found - marker icons will not be loaded");
		}
		ScreenshotManager.PreDrawEvent = (SimpleDelegate)Delegate.Combine(ScreenshotManager.PreDrawEvent, new SimpleDelegate(MapUiPatch.OnSpyStartHideMarkers));
	}
	public static void OnSpyStartHideMarkers()
	{
		for (int i = 0; i < MapUiPatch.remotePlayerImages.Count; i++)
		{
			MapUiPatch.remotePlayerImages[i].IsVisible = false;
		}
		for (int j = 0; j < MapUiPatch.markerImages.Count; j++)
		{
			MapUiPatch.markerImages[j].IsVisible = false;
		}
	}
	[HookMethodAttribute(typeof(PlayerDashboardInformationUI), "searchForMapsInInventory", new Type[] { })]
	protected static void SearchForMapsInInventoryOverride(ref bool enableChart, ref bool enableMap)
	{
		bool imitMapInInventory = MiscConfig.imitMapInInventory;
		bool flag = imitMapInInventory;
		if (flag)
		{
			enableMap = true;
			enableChart = true;
		}
		else
		{
			bool flag2 = enableChart & enableMap;
			bool flag3 = !flag2;
			if (flag3)
			{
				for (byte b = 0; b < PlayerInventory.PAGES - 2; b += 1)
				{
					Items items = Player.player.inventory.items[(int)b];
					bool flag4 = items != null;
					bool flag5 = flag4;
					if (flag5)
					{
						foreach (ItemJar itemJar in items.items)
						{
							bool flag6 = itemJar != null;
							bool flag7 = flag6;
							if (flag7)
							{
								ItemMapAsset asset = itemJar.GetAsset<ItemMapAsset>();
								bool flag8 = asset != null;
								bool flag9 = flag8;
								if (flag9)
								{
									enableChart |= asset.enablesChart;
									enableMap |= asset.enablesMap;
								}
								bool flag10 = enableChart & enableMap;
								bool flag11 = flag10;
								if (flag11)
								{
									return;
								}
							}
						}
					}
				}
			}
		}
	}
	[HookMethodAttribute(typeof(PlayerDashboardInformationUI), "updateMarkers", new Type[] { })]
	private static void UpdateMarkersOverride()
	{
		int num = 0;
		foreach (SteamPlayer steamPlayer in Provider.clients)
		{
			bool flag = !(steamPlayer.model == null);
			bool flag2 = flag;
			if (flag2)
			{
				PlayerQuests quests = steamPlayer.player.quests;
				bool flag3 = (!(steamPlayer.playerID.steamID != Provider.client) || quests.isMemberOfSameGroupAs(Player.player) || (MiscConfig.displayAllPlayerMarksOnMap && !ScreenshotManager.IsSpying)) && quests.isMarkerPlaced;
				bool flag4 = flag3;
				if (flag4)
				{
					bool flag5 = num < MapUiPatch.markerImages.Count;
					bool flag6 = flag5;
					ISleekImage sleekImage;
					if (flag6)
					{
						sleekImage = MapUiPatch.markerImages[num];
						sleekImage.IsVisible = true;
					}
					else
					{
						try
						{
							bool flag7 = MapUiPatch.iconsField != null;
							if (flag7)
							{
								sleekImage = Glazier.Get().CreateImage((MapUiPatch.iconsField.GetValue(null) as Bundle).load<Texture2D>("Marker"));
							}
							else
							{
								sleekImage = Glazier.Get().CreateImage();
							}
						}
						catch
						{
							sleekImage = Glazier.Get().CreateImage();
						}
						sleekImage.PositionOffset_X = -10f;
						sleekImage.PositionOffset_Y = -10f;
						sleekImage.SizeOffset_X = 20f;
						sleekImage.SizeOffset_Y = 20f;
						sleekImage.AddLabel(string.Empty, ESleekSide.RIGHT);
						(MapUiPatch.mapMarkersContainerField.GetValue(num) as ISleekElement).AddChild(sleekImage);
						MapUiPatch.markerImages.Add(sleekImage);
					}
					num++;
					Vector2 vector = (Vector2)MapUiPatch.projectWorldPositionToMapMethod.Invoke(null, new object[] { quests.markerPosition });
					sleekImage.PositionScale_X = vector.x;
					sleekImage.PositionScale_Y = vector.y;
					sleekImage.TintColor = steamPlayer.markerColor;
					string text = quests.markerTextOverride;
					bool flag8 = string.IsNullOrEmpty(text);
					bool flag9 = flag8;
					if (flag9)
					{
						bool flag10 = string.IsNullOrEmpty(steamPlayer.playerID.nickName);
						bool flag11 = flag10;
						if (flag11)
						{
							text = steamPlayer.playerID.characterName;
						}
						else
						{
							text = steamPlayer.playerID.nickName;
						}
					}
					sleekImage.UpdateLabel(text);
				}
			}
		}
		for (int i = MapUiPatch.markerImages.Count - 1; i >= num; i--)
		{
			MapUiPatch.markerImages[i].IsVisible = false;
		}
	}
	[HookMethodAttribute(typeof(PlayerDashboardInformationUI), "updateRemotePlayerAvatars", new Type[] { })]
	private static void UpdateRemotePlayerAvatarsOverride()
	{
		int num = 0;
		bool areSpecStatsVisible = Player.player.look.areSpecStatsVisible;
		foreach (SteamPlayer steamPlayer in Provider.clients)
		{
			bool flag = !(steamPlayer.model == null) && !(steamPlayer.playerID.steamID == Provider.client);
			bool flag2 = flag;
			if (flag2)
			{
				bool flag3 = steamPlayer.player.quests.isMemberOfSameGroupAs(Player.player);
				bool flag4 = areSpecStatsVisible || flag3 || (MiscConfig.displayAllPlayersOnMap && !ScreenshotManager.IsSpying);
				bool flag5 = flag4;
				if (flag5)
				{
					bool flag6 = num < MapUiPatch.remotePlayerImages.Count;
					bool flag7 = flag6;
					ISleekImage sleekImage;
					if (flag7)
					{
						sleekImage = MapUiPatch.remotePlayerImages[num];
						sleekImage.IsVisible = true;
					}
					else
					{
						sleekImage = Glazier.Get().CreateImage();
						sleekImage.PositionOffset_X = -10f;
						sleekImage.PositionOffset_Y = -10f;
						sleekImage.SizeOffset_X = 20f;
						sleekImage.SizeOffset_Y = 20f;
						sleekImage.AddLabel(string.Empty, ESleekSide.RIGHT);
						(MapUiPatch.mapRemotePlayersContainerField.GetValue(null) as ISleekElement).AddChild(sleekImage);
						MapUiPatch.remotePlayerImages.Add(sleekImage);
					}
					num++;
					Vector2 vector = (Vector2)MapUiPatch.projectWorldPositionToMapMethod.Invoke(null, new object[] { steamPlayer.player.transform.position });
					sleekImage.PositionScale_X = vector.x;
					sleekImage.PositionScale_Y = vector.y;
					bool flag8 = OptionsSettings.streamer || !(MapUiPatch.showPlayerAvatarsToggleField.GetValue(null) as ISleekToggle).Value;
					bool flag9 = flag8;
					if (flag9)
					{
						sleekImage.Texture = null;
					}
					else
					{
						sleekImage.Texture = Provider.provider.communityService.getIcon(steamPlayer.playerID.steamID, true);
					}
					bool value = (MapUiPatch.showPlayerNamesToggleField.GetValue(null) as ISleekToggle).Value;
					bool flag10 = value;
					if (flag10)
					{
						bool flag11 = flag3 && !string.IsNullOrEmpty(steamPlayer.playerID.nickName);
						bool flag12 = flag11;
						if (flag12)
						{
							sleekImage.UpdateLabel(steamPlayer.playerID.nickName);
						}
						else
						{
							sleekImage.UpdateLabel(steamPlayer.playerID.characterName);
						}
					}
					else
					{
						sleekImage.UpdateLabel(string.Empty);
					}
				}
			}
		}
		for (int i = MapUiPatch.remotePlayerImages.Count - 1; i >= num; i--)
		{
			MapUiPatch.remotePlayerImages[i].IsVisible = false;
		}
	}
	public static FieldInfo remotePlayerImagesField;
	public static FieldInfo markerImagesField;
	public static FieldInfo mapMarkersContainerField;
	public static FieldInfo mapRemotePlayersContainerField;
	public static FieldInfo showPlayerAvatarsToggleField;
	public static FieldInfo showPlayerNamesToggleField;
	public static MethodInfo projectWorldPositionToMapMethod;
	public static FieldInfo iconsField;
}
