using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SDG.Unturned;
using UnityEngine;

namespace gatyware
{
	public static class MapHook
	{
		[DllImport("kernel32.dll")]
		private static extern bool VirtualProtect(IntPtr addr, int size, uint prot, out uint old);

		public static void Init()
		{
			if (MapHook._hooked)
			{
				return;
			}
			MapHook._hooked = true;
			try
			{
				BindingFlags bindingAttr = BindingFlags.Static | BindingFlags.NonPublic;
				Type typeFromHandle = typeof(PlayerDashboardInformationUI);
				MapHook._remoteImagesF = typeFromHandle.GetField("remotePlayerImages", bindingAttr);
				MapHook._markerImagesF = typeFromHandle.GetField("markerImages", bindingAttr);
				MapHook._remoteContainerF = typeFromHandle.GetField("mapRemotePlayersContainer", bindingAttr);
				MapHook._markerContainerF = typeFromHandle.GetField("mapMarkersContainer", bindingAttr);
				MapHook._showAvatarsF = typeFromHandle.GetField("showPlayerAvatarsToggle", bindingAttr);
				MapHook._showNamesF = typeFromHandle.GetField("showPlayerNamesToggle", bindingAttr);
				MapHook._projectM = typeFromHandle.GetMethod("ProjectWorldPositionToMap", bindingAttr);
				MapHook._h1m = typeFromHandle.GetMethod("updateRemotePlayerAvatars", bindingAttr);
				if (MapHook._h1m != null)
				{
					RuntimeHelpers.PrepareMethod(MapHook._h1m.MethodHandle);
					MapHook._h1p = MapHook._h1m.MethodHandle.GetFunctionPointer();
					MethodInfo method = typeof(MapHook).GetMethod("H1", bindingAttr);
					RuntimeHelpers.PrepareMethod(method.MethodHandle);
					Marshal.Copy(MapHook._h1p, MapHook._h1s, 0, 14);
					MapHook.WriteJmp(MapHook._h1p, method.MethodHandle.GetFunctionPointer());
					Runtime.Trace("map: avatar hook OK");
				}
				MapHook._h2m = typeFromHandle.GetMethod("updateMarkers", bindingAttr);
				if (MapHook._h2m != null)
				{
					RuntimeHelpers.PrepareMethod(MapHook._h2m.MethodHandle);
					MapHook._h2p = MapHook._h2m.MethodHandle.GetFunctionPointer();
					MethodInfo method2 = typeof(MapHook).GetMethod("H2", bindingAttr);
					RuntimeHelpers.PrepareMethod(method2.MethodHandle);
					Marshal.Copy(MapHook._h2p, MapHook._h2s, 0, 14);
					MapHook.WriteJmp(MapHook._h2p, method2.MethodHandle.GetFunctionPointer());
					Runtime.Trace("map: marker hook OK");
				}
			}
			catch (Exception ex)
			{
				Runtime.Trace("map: err " + ex.Message);
			}
		}

		private static void H1()
		{
			uint prot;
			MapHook.VirtualProtect(MapHook._h1p, 14, 64U, out prot);
			Marshal.Copy(MapHook._h1s, 0, MapHook._h1p, 14);
			MapHook.VirtualProtect(MapHook._h1p, 14, prot, out prot);
			MapHook._h1m.Invoke(null, null);
			MapHook.WriteJmp(MapHook._h1p, typeof(MapHook).GetMethod("H1", BindingFlags.Static | BindingFlags.NonPublic).MethodHandle.GetFunctionPointer());
			if (!State.MapShowAllPlayers || State.IsSpying)
			{
				return;
			}
			Player localPlayer = Player.LocalPlayer;
			if (localPlayer == null)
			{
				return;
			}
			try
			{
				MapHook.DoShowPlayers(localPlayer);
			}
			catch
			{
			}
		}

		private static void DoShowPlayers(Player lp)
		{
			FieldInfo remoteImagesF = MapHook._remoteImagesF;
			List<ISleekImage> list = ((remoteImagesF != null) ? remoteImagesF.GetValue(null) : null) as List<ISleekImage>;
			FieldInfo remoteContainerF = MapHook._remoteContainerF;
			ISleekElement sleekElement = ((remoteContainerF != null) ? remoteContainerF.GetValue(null) : null) as ISleekElement;
			if (list == null || sleekElement == null || MapHook._projectM == null)
			{
				return;
			}
			int num = 0;
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].IsVisible)
				{
					num++;
				}
			}
			bool flag = false;
			bool flag2 = false;
			try
			{
				FieldInfo showAvatarsF = MapHook._showAvatarsF;
				ISleekToggle sleekToggle = ((showAvatarsF != null) ? showAvatarsF.GetValue(null) : null) as ISleekToggle;
				FieldInfo showNamesF = MapHook._showNamesF;
				ISleekToggle sleekToggle2 = ((showNamesF != null) ? showNamesF.GetValue(null) : null) as ISleekToggle;
				flag = (sleekToggle != null && sleekToggle.Value);
				flag2 = (sleekToggle2 != null && sleekToggle2.Value);
			}
			catch
			{
			}
			List<SteamPlayer> clients = Provider.clients;
			if (clients == null)
			{
				return;
			}
			for (int j = 0; j < clients.Count; j++)
			{
				SteamPlayer steamPlayer = clients[j];
				if (!(((steamPlayer != null) ? steamPlayer.player : null) == null) && !(steamPlayer.model == null) && !(steamPlayer.player == lp) && !steamPlayer.player.quests.isMemberOfSameGroupAs(lp) && !lp.look.areSpecStatsVisible)
				{
					ISleekImage sleekImage;
					if (num < list.Count)
					{
						sleekImage = list[num];
						sleekImage.IsVisible = true;
					}
					else
					{
						sleekImage = Glazier.Get().CreateImage();
						sleekImage.PositionOffset_X = -10f;
						sleekImage.PositionOffset_Y = -10f;
						sleekImage.SizeOffset_X = 20f;
						sleekImage.SizeOffset_Y = 20f;
						RelationType relationType = PlayerRelation.Get(steamPlayer);
						Color color = (relationType == RelationType.Friend) ? State.FriendColor : ((relationType == RelationType.Enemy) ? State.EnemyColor : Color.white);
						sleekImage.AddLabel(string.Empty, color, (ESleekSide)1);
						sleekElement.AddChild(sleekImage);
						list.Add(sleekImage);
					}
					num++;
					Vector2 vector = (Vector2)MapHook._projectM.Invoke(null, new object[]
					{
						steamPlayer.player.transform.position
					});
					sleekImage.PositionScale_X = vector.x;
					sleekImage.PositionScale_Y = vector.y;
					if (!OptionsSettings.streamer && flag)
					{
						sleekImage.Texture = Provider.provider.communityService.getIcon(steamPlayer.playerID.steamID, true);
					}
					else
					{
						sleekImage.Texture = null;
					}
					if (flag2)
					{
						RelationType relationType2 = PlayerRelation.Get(steamPlayer);
						string text = steamPlayer.playerID.characterName;
						if (relationType2 == RelationType.Friend && !string.IsNullOrEmpty(steamPlayer.playerID.nickName))
						{
							text = steamPlayer.playerID.nickName;
						}
						sleekImage.UpdateLabel(text);
						ISleekLabel sideLabel = sleekImage.SideLabel;
						if (sideLabel != null)
						{
							Color color2 = (relationType2 == RelationType.Friend) ? State.FriendColor : ((relationType2 == RelationType.Enemy) ? State.EnemyColor : Color.white);
							sideLabel.TextColor = new SleekColor(color2);
						}
					}
					else
					{
						sleekImage.UpdateLabel(string.Empty);
					}
				}
			}
			for (int k = list.Count - 1; k >= num; k--)
			{
				list[k].IsVisible = false;
			}
		}

		private static void H2()
		{
			uint prot;
			MapHook.VirtualProtect(MapHook._h2p, 14, 64U, out prot);
			Marshal.Copy(MapHook._h2s, 0, MapHook._h2p, 14);
			MapHook.VirtualProtect(MapHook._h2p, 14, prot, out prot);
			MapHook._h2m.Invoke(null, null);
			MapHook.WriteJmp(MapHook._h2p, typeof(MapHook).GetMethod("H2", BindingFlags.Static | BindingFlags.NonPublic).MethodHandle.GetFunctionPointer());
			if (!State.MapShowAllMarkers || State.IsSpying)
			{
				return;
			}
			Player localPlayer = Player.LocalPlayer;
			if (localPlayer == null)
			{
				return;
			}
			try
			{
				MapHook.DoShowMarkers(localPlayer);
			}
			catch
			{
			}
		}

		private static void DoShowMarkers(Player lp)
		{
			FieldInfo markerImagesF = MapHook._markerImagesF;
			List<ISleekImage> list = ((markerImagesF != null) ? markerImagesF.GetValue(null) : null) as List<ISleekImage>;
			FieldInfo markerContainerF = MapHook._markerContainerF;
			ISleekElement sleekElement = ((markerContainerF != null) ? markerContainerF.GetValue(null) : null) as ISleekElement;
			if (list == null || sleekElement == null || MapHook._projectM == null)
			{
				return;
			}
			int num = 0;
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].IsVisible)
				{
					num++;
				}
			}
			List<SteamPlayer> clients = Provider.clients;
			if (clients == null)
			{
				return;
			}
			for (int j = 0; j < clients.Count; j++)
			{
				SteamPlayer steamPlayer = clients[j];
				if (!(((steamPlayer != null) ? steamPlayer.player : null) == null) && !(steamPlayer.model == null) && !(steamPlayer.player == lp) && !steamPlayer.player.quests.isMemberOfSameGroupAs(lp) && steamPlayer.player.quests.isMarkerPlaced)
				{
					ISleekImage sleekImage;
					if (num < list.Count)
					{
						sleekImage = list[num];
						sleekImage.IsVisible = true;
					}
					else
					{
						sleekImage = Glazier.Get().CreateImage(PlayerDashboardInformationUI.icons.load<Texture2D>("Marker"));
						sleekImage.PositionOffset_X = -10f;
						sleekImage.PositionOffset_Y = -10f;
						sleekImage.SizeOffset_X = 20f;
						sleekImage.SizeOffset_Y = 20f;
						sleekImage.AddLabel(string.Empty, (ESleekSide)1);
						sleekElement.AddChild(sleekImage);
						list.Add(sleekImage);
					}
					num++;
					Vector2 vector = (Vector2)MapHook._projectM.Invoke(null, new object[]
					{
						steamPlayer.player.quests.markerPosition
					});
					sleekImage.PositionScale_X = vector.x;
					sleekImage.PositionScale_Y = vector.y;
					sleekImage.TintColor = steamPlayer.markerColor;
					string text = steamPlayer.player.quests.markerTextOverride;
					if (string.IsNullOrEmpty(text))
					{
						text = (string.IsNullOrEmpty(steamPlayer.playerID.nickName) ? steamPlayer.playerID.characterName : steamPlayer.playerID.nickName);
					}
					sleekImage.UpdateLabel(text);
				}
			}
			for (int k = list.Count - 1; k >= num; k--)
			{
				list[k].IsVisible = false;
			}
		}

		private static void WriteJmp(IntPtr from, IntPtr to)
		{
			uint prot;
			MapHook.VirtualProtect(from, 14, 64U, out prot);
			byte[] array = new byte[14];
			array[0] = byte.MaxValue;
			array[1] = 37;
			BitConverter.GetBytes(to.ToInt64()).CopyTo(array, 6);
			Marshal.Copy(array, 0, from, 14);
			MapHook.VirtualProtect(from, 14, prot, out prot);
		}

		private static byte[] _h1s = new byte[14];

		private static byte[] _h2s = new byte[14];

		private static IntPtr _h1p;

		private static IntPtr _h2p;

		private static MethodInfo _h1m;

		private static MethodInfo _h2m;

		private static bool _hooked;

		private static FieldInfo _remoteImagesF;

		private static FieldInfo _markerImagesF;

		private static FieldInfo _remoteContainerF;

		private static FieldInfo _markerContainerF;

		private static FieldInfo _showAvatarsF;

		private static FieldInfo _showNamesF;

		private static MethodInfo _projectM;
	}
}
