using System;
using System.Collections.Generic;
using System.Reflection;
using SDG.Unturned;
using UnityEngine;
public static class OverlayDrawer
{
	[InitializeAttribute]
	private static void InitializeHooks()
	{
		List<WindowBase> list = new List<WindowBase>();
		foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
		{
			bool flag = type.BaseType == typeof(WindowBase);
			bool flag2 = flag;
			if (flag2)
			{
				list.Add((WindowBase)Activator.CreateInstance(type));
			}
		}
		OverlayDrawer.Hooks = list.ToArray();
	}
	public static void OnUpdate()
	{
		bool flag = !(Player.player == null);
		if (flag)
		{
			bool flag2 = AimbotConfig.bulletDelaying && !AimbotConfig.unholdDelayByMouse && Input.GetKey(AimbotConfig.bulletDelayKeybind);
			if (flag2)
			{
				AimbotUtil.bulletEventHistoryPerPlayer.Clear();
			}
			FovCircleRenderer.DrawFovCircles();
			bool flag3 = AimbotConfig.enableSilentAim && AimbotConfig.silentAimType == SilentAimType.Sphere && AimbotConfig.debugSpherePoints;
			if (flag3)
			{
				bool flag4 = AimbotUtil.currentAimObjective.Target == null;
				if (flag4)
				{
					int num = (AimbotConfig.enableAimbot ? AimbotConfig.memoryAimbotFOV : FovCircleRenderer.GetFovRadius("Aimbot FOV"));
					int num2 = MathUtil.GetAimTargetDistance();
					bool flag5 = (AimbotConfig.enableAimbot ? AimbotConfig.memoryAimbotCheckWalls : (AimbotConfig.checkWithLinecast && !AimbotConfig.enableSilentAim));
					foreach (TargetType dr5qliNNQh3jZolh9fn7SFNyi in AimbotConfig.TargetTypes)
					{
						AimObjective du4XicP1hVrJzXjQ70aniDyvk = AimbotUtil.FindBestTarget(num2, int.MaxValue, dr5qliNNQh3jZolh9fn7SFNyi, AimbotConfig.aimSorting, flag5, false, false);
						bool flag6 = du4XicP1hVrJzXjQ70aniDyvk.Target != null;
						if (flag6)
						{
							AimbotUtil.currentAimObjective = du4XicP1hVrJzXjQ70aniDyvk;
							break;
						}
					}
				}
				bool flag7 = AimbotUtil.currentAimObjective.Target != null;
				if (flag7)
				{
					OverlayDrawer.DrawSphereDebugPoints();
				}
			}
		}
	}
	public static void OnGUI()
	{
		GUI.skin = GuiStyles.CustomSkin;
		OverlayDrawer.DrawHooks();
		bool enableUserLogger = Settings.enableUserLogger;
		bool flag = enableUserLogger;
		if (flag)
		{
			OverlayDrawer.DrawLoggerWindow();
		}
		bool flag2 = Event.current.type != EventType.Repaint;
		bool flag3 = !flag2;
		if (flag3)
		{
			try
			{
				List<DelayedBullet> list = null;
				bool flag4 = AimbotConfig.bulletDelaying && AimbotConfig.showBulletDelayingTimer && Provider.modeConfigData.Gameplay.Ballistics && AimbotUtil.currentAimObjective.Target != null && AimbotUtil.currentAimObjective.TargetType == TargetType.Player && AimbotUtil.bulletEventHistoryPerPlayer.TryGetValue(((Player)AimbotUtil.currentAimObjective.Target).channel.owner.playerID.steamID.m_SteamID, out list) && list != null && list.Count > 0;
				bool flag5 = flag4;
				if (flag5)
				{
					OverlayDrawer.DrawBulletDelayTimer(list);
				}
			}
			catch
			{
			}
		}
	}
	public static void DrawBulletDelayTimer(List<DelayedBullet> cbis)
	{
		Rect rect = new Rect((float)(Screen.width / 2 - 15), (float)(Screen.height / 2 + 30), 30f, 10f);
		MenuGuiHelper.DrawRect(rect, new Color32(20, 20, 20, byte.MaxValue), false, ScaleMode.StretchToFill);
		rect.x += 2f;
		rect.y += 2f;
		rect.width -= 4f;
		rect.height -= 4f;
		try
		{
			GUIStyle guistyle = new GUIStyle();
			guistyle.normal.textColor = Color.white;
			guistyle.fontSize = 11;
			guistyle.alignment = TextAnchor.MiddleCenter;
			GUI.Label(new Rect(rect.x - 50f, rect.y + 15f, rect.width + 100f, 20f), "Delayed: " + cbis.Count.ToString() + " / " + AimbotConfig.bulletDelayAmount.ToString(), guistyle);
			bool flag = AimbotConfig.unholdDelayByMouse && AimbotConfig.momentalyUnhold;
			if (flag)
			{
				GUI.Label(new Rect(rect.x - 50f, rect.y + 30f, rect.width + 100f, 20f), "Unhold: Mouse", guistyle);
			}
			else
			{
				GUI.Label(new Rect(rect.x - 50f, rect.y + 30f, rect.width + 100f, 20f), "Release Key: " + AimbotConfig.bulletDelayKeybind.ToString(), guistyle);
			}
			int num = 0;
			foreach (DelayedBullet drxteGz0evnNDVy6poswGHA5b in cbis)
			{
				num = Mathf.Max((int)drxteGz0evnNDVy6poswGHA5b.Step, num);
			}
			ItemGunAsset itemGunAsset = Player.player.equipment.asset as ItemGunAsset;
			bool flag2 = itemGunAsset != null && itemGunAsset.ballisticSteps > 0;
			if (flag2)
			{
				rect.width *= (float)num / (float)itemGunAsset.ballisticSteps;
			}
			MenuGuiHelper.DrawRect(rect, new Color32(20, byte.MaxValue, 20, byte.MaxValue), false, ScaleMode.StretchToFill);
		}
		catch
		{
		}
	}
	public static void DrawLoggerWindow()
	{
		Rect rect = new Rect(OverlayDrawer.LoggerWindowRect.x, OverlayDrawer.LoggerWindowRect.y, 64f, 64f);
		WindowDragger.DragWindow(ref rect);
		OverlayDrawer.LoggerWindowRect = rect;
		GUILayout.BeginArea(new Rect(OverlayDrawer.LoggerWindowRect.x, OverlayDrawer.LoggerWindowRect.y, 600f, 400f));
		bool dtKlbZSZSYlN9ldxLbDo4VuvI = MenuState.menuOpenedBacking;
		bool flag = dtKlbZSZSYlN9ldxLbDo4VuvI;
		if (flag)
		{
			OverlayDrawer.DrawLoggerLine("In menu", 0f);
		}
		foreach (ValueTuple<string, float> valueTuple in Logger.UserLogs)
		{
			string item = valueTuple.Item1;
			float item2 = valueTuple.Item2;
			OverlayDrawer.DrawLoggerLine(item, item2);
		}
		GUILayout.EndArea();
	}
	private static void DrawLoggerLine(string s, float f)
	{
		bool flag = Settings.loggerTextCase == TextCase.UpperCase;
		bool flag2 = flag;
		if (flag2)
		{
			s = s.ToUpper();
		}
		else
		{
			bool flag3 = Settings.loggerTextCase == TextCase.LowerCase;
			bool flag4 = flag3;
			if (flag4)
			{
				s = s.ToLower();
			}
		}
		float num = 1f - Mathf.Clamp01(f / 5f - 0.75f) * 4f;
		Rect rect = GUILayoutUtility.GetRect(600f, 20f);
		GuiStyles.DarkLabelStyle.normal.textColor = new Color(0f, 0f, 0f, num);
		switch (Settings.loggerTextOutline)
		{
		case TextOutlineStyle.RightDownSided:
			GUI.Label(new Rect(rect.x + 1f, rect.y, rect.width, rect.height), s, GuiStyles.DarkLabelStyle);
			GUI.Label(new Rect(rect.x, rect.y + 1f, rect.width, rect.height), s, GuiStyles.DarkLabelStyle);
			break;
		case TextOutlineStyle.RightTopSided:
			GUI.Label(new Rect(rect.x + 1f, rect.y, rect.width, rect.height), s, GuiStyles.DarkLabelStyle);
			GUI.Label(new Rect(rect.x, rect.y - 1f, rect.width, rect.height), s, GuiStyles.DarkLabelStyle);
			break;
		case TextOutlineStyle.LeftTopSided:
			GUI.Label(new Rect(rect.x - 1f, rect.y, rect.width, rect.height), s, GuiStyles.DarkLabelStyle);
			GUI.Label(new Rect(rect.x, rect.y - 1f, rect.width, rect.height), s, GuiStyles.DarkLabelStyle);
			break;
		case TextOutlineStyle.LeftDownSided:
			GUI.Label(new Rect(rect.x - 1f, rect.y, rect.width, rect.height), s, GuiStyles.DarkLabelStyle);
			GUI.Label(new Rect(rect.x, rect.y + 1f, rect.width, rect.height), s, GuiStyles.DarkLabelStyle);
			break;
		case TextOutlineStyle.FourSided:
			GUI.Label(new Rect(rect.x + 1f, rect.y, rect.width, rect.height), s, GuiStyles.DarkLabelStyle);
			GUI.Label(new Rect(rect.x, rect.y - 1f, rect.width, rect.height), s, GuiStyles.DarkLabelStyle);
			GUI.Label(new Rect(rect.x - 1f, rect.y, rect.width, rect.height), s, GuiStyles.DarkLabelStyle);
			GUI.Label(new Rect(rect.x, rect.y + 1f, rect.width, rect.height), s, GuiStyles.DarkLabelStyle);
			break;
		}
		Color color = ColorConfig.GetColorEntry("Logger color").Color;
		GuiStyles.BaseLabelStyle.normal.textColor = new Color(color.r, color.g, color.b, num);
		GUI.Label(rect, s, GuiStyles.BaseLabelStyle);
	}
	public static void DrawAimTargets()
	{
		try
		{
			bool flag = Player.player != null && Player.player.look.perspective == EPlayerPerspective.FIRST;
			AimObjective daseuOZmKRI1v3DQRcsmyTKsC = AimbotUtil.currentAimObjective;
			bool flag2 = !flag && AimbotConfig.enableAim && AimbotConfig.drawTarget && daseuOZmKRI1v3DQRcsmyTKsC.Target != null && MainCamera.instance != null;
			bool flag3 = flag2;
			if (flag3)
			{
				OverlayDrawer.DrawTargetLine();
			}
			AimObjective du4XicP1hVrJzXjQ70aniDyvk = daseuOZmKRI1v3DQRcsmyTKsC;
			bool flag4 = AimbotConfig.restrictAimByFov && (daseuOZmKRI1v3DQRcsmyTKsC.Target == null || daseuOZmKRI1v3DQRcsmyTKsC.PointOffset == Vector3.zero);
			if (flag4)
			{
				int num = MathUtil.GetAimTargetDistance();
				bool flag5 = (AimbotConfig.enableAimbot ? AimbotConfig.memoryAimbotCheckWalls : (AimbotConfig.checkWithLinecast && !AimbotConfig.enableSilentAim));
				foreach (TargetType dr5qliNNQh3jZolh9fn7SFNyi in AimbotConfig.TargetTypes)
				{
					AimObjective du4XicP1hVrJzXjQ70aniDyvk2 = AimbotUtil.FindBestTarget(num, int.MaxValue, dr5qliNNQh3jZolh9fn7SFNyi, AimbotConfig.aimSorting, flag5, false, false);
					bool flag6 = du4XicP1hVrJzXjQ70aniDyvk2.Target != null;
					if (flag6)
					{
						du4XicP1hVrJzXjQ70aniDyvk = du4XicP1hVrJzXjQ70aniDyvk2;
						break;
					}
				}
			}
			bool flag7 = !flag && AimbotConfig.enableAim && AimbotConfig.drawTarget && AimbotConfig.previewHitPoint && du4XicP1hVrJzXjQ70aniDyvk.PointOffset != Vector3.zero && (Player.player.look.aim.transform.position - du4XicP1hVrJzXjQ70aniDyvk.PointOffset).magnitude > 2f;
			bool flag8 = flag7;
			if (flag8)
			{
				OverlayDrawer.DrawHitPointMarker(du4XicP1hVrJzXjQ70aniDyvk.PointOffset);
				bool drawLineFromHitPoint = AimbotConfig.drawLineFromHitPoint;
				bool flag9 = drawLineFromHitPoint;
				if (flag9)
				{
					OverlayDrawer.DrawLine(AimbotUtil.GetAimBoneTransform(du4XicP1hVrJzXjQ70aniDyvk.Target).position, du4XicP1hVrJzXjQ70aniDyvk.PointOffset);
				}
				bool drawLineFromPlayerHead = AimbotConfig.drawLineFromPlayerHead;
				bool flag10 = drawLineFromPlayerHead;
				if (flag10)
				{
					Transform transform = AimbotUtil.GetAimBoneTransform(du4XicP1hVrJzXjQ70aniDyvk.Target);
					bool flag11 = transform != null;
					if (flag11)
					{
						OverlayDrawer.DrawLine(Player.player.look.aim.position, transform.position);
					}
				}
			}
		}
		catch
		{
		}
	}
	public static void DrawSphereDebugPoints()
	{
		Transform transform = AimbotUtil.GetAimBoneTransform(AimbotUtil.currentAimObjective.Target);
		Vector3 vector2;
		foreach (Vector3 vector in SpherePointGenerator.SpherePoints)
		{
			bool flag = !(transform.position + vector).IsOnScreen();
			bool flag2 = !flag;
			if (flag2)
			{
				vector2 = (transform.position + vector).WorldToScreenPoint();
				MenuGuiHelper.DrawTextureRect(new Rect(vector2.x - 2f, vector2.y - 2f, 4f, 4f), GuiStyles.WhiteTexture, Physics.Linecast(Player.player.look.aim.position, transform.position + vector, RayMasks.DAMAGE_CLIENT, QueryTriggerInteraction.Ignore) ? Color.red : Color.yellow, false, ScaleMode.StretchToFill);
			}
		}
		vector2 = AimbotUtil.currentAimObjective.PointOffset.WorldToScreenPoint();
		MenuGuiHelper.DrawTextureRect(new Rect(vector2.x - 2f, vector2.y - 2f, 4f, 4f), GuiStyles.WhiteTexture, Color.green, false, ScaleMode.StretchToFill);
	}
	private static void DrawLine(Vector3 start, Vector3 end)
	{
		EspDrawer.DrawLine(start.WorldToScreenPoint(), end.WorldToScreenPoint(), ColorConfig.GetColor("Sphere preview line color"), 1f);
	}
	private static void DrawHitPointMarker(Vector3 point)
	{
		EspDrawer.BoundsCornerPoints[0] = new Vector3(point.x + 0.15f, point.y + 0.15f, point.z + 0.15f).WorldToScreenPoint();
		EspDrawer.BoundsCornerPoints[1] = new Vector3(point.x - 0.15f, point.y - 0.15f, point.z + 0.15f).WorldToScreenPoint();
		EspDrawer.BoundsCornerPoints[2] = new Vector3(point.x + 0.15f, point.y + 0.15f, point.z - 0.15f).WorldToScreenPoint();
		EspDrawer.BoundsCornerPoints[3] = new Vector3(point.x - 0.15f, point.y - 0.15f, point.z - 0.15f).WorldToScreenPoint();
		EspDrawer.BoundsCornerPoints[4] = new Vector3(point.x + 0.15f, point.y - 0.15f, point.z + 0.15f).WorldToScreenPoint();
		EspDrawer.BoundsCornerPoints[5] = new Vector3(point.x - 0.15f, point.y + 0.15f, point.z + 0.15f).WorldToScreenPoint();
		EspDrawer.BoundsCornerPoints[6] = new Vector3(point.x + 0.15f, point.y - 0.15f, point.z - 0.15f).WorldToScreenPoint();
		EspDrawer.BoundsCornerPoints[7] = new Vector3(point.x - 0.15f, point.y + 0.15f, point.z - 0.15f).WorldToScreenPoint();
		Color32 color = ColorConfig.GetColor("Sphere preview point color");
		EspDrawer.DrawLine(EspDrawer.BoundsCornerPoints[0], EspDrawer.BoundsCornerPoints[4], color, 1f);
		EspDrawer.DrawLine(EspDrawer.BoundsCornerPoints[1], EspDrawer.BoundsCornerPoints[5], color, 1f);
		EspDrawer.DrawLine(EspDrawer.BoundsCornerPoints[0], EspDrawer.BoundsCornerPoints[5], color, 1f);
		EspDrawer.DrawLine(EspDrawer.BoundsCornerPoints[4], EspDrawer.BoundsCornerPoints[1], color, 1f);
		EspDrawer.DrawLine(EspDrawer.BoundsCornerPoints[2], EspDrawer.BoundsCornerPoints[6], color, 1f);
		EspDrawer.DrawLine(EspDrawer.BoundsCornerPoints[3], EspDrawer.BoundsCornerPoints[7], color, 1f);
		EspDrawer.DrawLine(EspDrawer.BoundsCornerPoints[2], EspDrawer.BoundsCornerPoints[7], color, 1f);
		EspDrawer.DrawLine(EspDrawer.BoundsCornerPoints[6], EspDrawer.BoundsCornerPoints[3], color, 1f);
		EspDrawer.DrawLine(EspDrawer.BoundsCornerPoints[0], EspDrawer.BoundsCornerPoints[2], color, 1f);
		EspDrawer.DrawLine(EspDrawer.BoundsCornerPoints[1], EspDrawer.BoundsCornerPoints[3], color, 1f);
		EspDrawer.DrawLine(EspDrawer.BoundsCornerPoints[4], EspDrawer.BoundsCornerPoints[6], color, 1f);
		EspDrawer.DrawLine(EspDrawer.BoundsCornerPoints[5], EspDrawer.BoundsCornerPoints[7], color, 1f);
	}
	private static void DrawTargetLine()
	{
		Transform transform = AimbotUtil.GetAimBoneTransform(AimbotUtil.currentAimObjective.Target);
		bool flag = transform != null;
		bool flag2 = flag;
		if (flag2)
		{
			EspDrawer.DrawLine(new Vector3(AimbotConfig.targetLineStartX * (float)Screen.width, AimbotConfig.targetLineStartY * (float)Screen.height, 0f), transform.position.WorldToScreenPoint(), ColorConfig.GetColor("Aimhacks target line"), 1f);
		}
	}
	private static void DrawHooks()
	{
		for (int i = 0; i < OverlayDrawer.Hooks.Length; i++)
		{
			bool aviablity = OverlayDrawer.Hooks[i].GetAviablity();
			bool flag = aviablity;
			if (flag)
			{
				bool flag2 = OverlayDrawer.Hooks[i].IsShowOnMenu();
				bool flag3 = flag2;
				if (flag3)
				{
					GUI.color = new Color(1f, 1f, 1f, MenuState.GetMenuOpenProgress());
				}
				else
				{
					GUI.color = new Color(1f, 1f, 1f, MenuState.GetMenuCloseProgress());
				}
				bool flag4 = GUI.color.a > 0f;
				bool flag5 = flag4;
				if (flag5)
				{
					OverlayDrawer.Hooks[i].DrawWindowFrame();
					bool flag6 = !OverlayDrawer.Hooks[i].UseStaticRect();
					bool flag7 = flag6;
					if (flag7)
					{
						Rect rect = new Rect(OverlayDrawer.Hooks[i].WindowPosition.x, OverlayDrawer.Hooks[i].WindowPosition.y, OverlayDrawer.Hooks[i].GetSize().x, 16f);
						WindowDragger.DragWindow(ref rect);
						OverlayDrawer.Hooks[i].WindowPosition = new Vector2(rect.x, rect.y);
					}
				}
			}
		}
	}
	public static WindowBase[] Hooks;
	public static Rect LoggerWindowRect = new Rect(100f, 100f, 600f, 400f);
}
