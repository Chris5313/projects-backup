using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using SDG.Unturned;
using UnityEngine;
public class MenuState : MonoBehaviour
{
	// (get) Token: 0x0600028A RID: 650 RVA: 0x0002AAF4 File Offset: 0x00028CF4
	// (set) Token: 0x0600028B RID: 651 RVA: 0x0002AB0C File Offset: 0x00028D0C
	[ConfigBindAttribute("Misc options", "Open menu")]
	public static bool menuOpened
	{
		get
		{
			return MenuState.menuOpenedBacking;
		}
		set
		{
			bool flag = MenuState.menuOpenedBacking != value && value;
			if (flag)
			{
				Cursor.lockState = CursorLockMode.None;
				bool flag2 = PlayerUI.window != null;
				if (flag2)
				{
					PlayerUI.window.showCursor = true;
				}
				ConfigManager.RefreshConfigList();
				UnturnedSettingsConfig.RefreshConfigList();
				MenuState.SetMenuScrollBlock(true);
				bool flag3 = !Settings.smoothMenuOpen;
				if (flag3)
				{
					MenuState.menuSmoothOpenTime = Settings.smoothOpenTime;
				}
			}
			else
			{
				bool flag4 = MenuState.menuOpenedBacking != value && !value;
				if (flag4)
				{
					MenuState.SetMenuScrollBlock(false);
					bool flag5 = !Settings.smoothMenuOpen;
					if (flag5)
					{
						MenuState.menuSmoothOpenTime = 0f;
					}
					MenuState.activePopupOwner = null;
				}
			}
			MenuState.menuOpenedBacking = value;
		}
	}
	[InitializeAttribute]
	private static void InitializeTabs()
	{
		List<FeatureTabBase> list = new List<FeatureTabBase>();
		foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
		{
			bool flag = type.BaseType == typeof(FeatureTabBase) && !(type == typeof(OverridesTab) && !MenuState.overridesTabEnabled);
			if (flag)
			{
				list.Add((FeatureTabBase)Activator.CreateInstance(type));
			}
		}
		MenuState.tabs = new FeatureTabBase[list.Count];
		int num = 0;
		foreach (FeatureTabBase doekZ1zoBEuvfUmOs9QSpfX0X in list)
		{
			bool flag2 = doekZ1zoBEuvfUmOs9QSpfX0X.SortId() != -1;
			if (flag2)
			{
				int sortId = doekZ1zoBEuvfUmOs9QSpfX0X.SortId();
				// Out-of-range or duplicate SortId used to clobber/land past the array
				// (NetworkTab at 11 left a blank gap). Append instead — tabs never vanish.
				bool flag2b = sortId < num && MenuState.tabs[sortId] == null;
				if (flag2b)
				{
					MenuState.tabs[sortId] = doekZ1zoBEuvfUmOs9QSpfX0X;
				}
				else
				{
					MenuState.tabs[num] = doekZ1zoBEuvfUmOs9QSpfX0X;
				}
				num++;
			}
		}
		foreach (FeatureTabBase doekZ1zoBEuvfUmOs9QSpfX0X2 in list)
		{
			bool flag3 = doekZ1zoBEuvfUmOs9QSpfX0X2.SortId() == -1;
			if (flag3)
			{
				MenuState.tabs[num] = doekZ1zoBEuvfUmOs9QSpfX0X2;
				num++;
			}
		}
		MenuState.blackoutTexture = TextureUtil.CreateBlackoutTexture();
	}
	public static void SetMenuScrollBlock(bool activity)
	{
		bool flag = !activity;
		if (flag)
		{
			foreach (object obj in MenuState.scrollRectExs)
			{
				bool flag2 = obj != null && obj.GetType() == ReflectionUtil.FindTypeInUnturned("ScrollRectEx");
				if (flag2)
				{
					obj.GetType().GetProperty("scrollSensitivity").SetValue(obj, 40);
				}
			}
			MenuState.scrollRectExs = new object[0];
		}
		else
		{
			MenuState.scrollRectExs = (from scroll in UnityEngine.Object.FindObjectsOfType(ReflectionUtil.FindTypeInUnturned("ScrollRectEx"))
				select (scroll)).ToArray<object>();
			foreach (object obj2 in MenuState.scrollRectExs)
			{
				obj2.GetType().GetProperty("scrollSensitivity").SetValue(obj2, 0);
			}
		}
	}
	public static Color GetMenuFadeColor()
	{
		return new Color(1f, 1f, 1f, (MenuState.menuSmoothOpenTime == 0f) ? 0f : (MenuState.menuSmoothOpenTime / Settings.smoothOpenTime));
	}
	public static Color GetWorldDimColor()
	{
		return new Color(1f, 1f, 1f, (MenuState.menuSmoothOpenTime == 0f) ? 1f : (1f - MenuState.menuSmoothOpenTime / Settings.smoothOpenTime));
	}
	public static float GetMenuCloseProgress()
	{
		bool flag = MenuState.menuSmoothOpenTime != 0f;
		float num;
		if (flag)
		{
			num = 1f - MenuState.menuSmoothOpenTime / Settings.smoothOpenTime;
		}
		else
		{
			num = 1f;
		}
		return num;
	}
	public static float GetMenuOpenProgress()
	{
		bool flag = MenuState.menuSmoothOpenTime != 0f;
		float num;
		if (flag)
		{
			num = MenuState.menuSmoothOpenTime / Settings.smoothOpenTime;
		}
		else
		{
			num = 0f;
		}
		return num;
	}
	public void Update()
	{
		bool flag = Provider.preferenceData != null && Provider.preferenceData.Viewmodel != null;
		if (flag)
		{
			Provider.preferenceData.Viewmodel.Field_Of_View_Aim = WorldTab.fovAim;
			Provider.preferenceData.Viewmodel.Field_Of_View_Hip = WorldTab.fovHip;
			Provider.preferenceData.Viewmodel.Offset_Depth = WorldTab.offsetDepth;
			Provider.preferenceData.Viewmodel.Offset_Horizontal = WorldTab.offsetHorizontal;
			Provider.preferenceData.Viewmodel.Offset_Vertical = WorldTab.offsetVertical;
		}
		bool flag2 = MenuState.DwelcomeTimer > 0f;
		if (flag2)
		{
			MenuState.DwelcomeTimer -= Time.unscaledDeltaTime;
		}
		bool flag3 = !MenuState.DopenDelayedTriggered;
		if (flag3)
		{
			MenuState.DopenDelayTimer -= Time.unscaledDeltaTime;
			bool flag4 = MenuState.DopenDelayTimer <= 0f;
			if (flag4)
			{
				MenuState.DopenDelayedTriggered = true;
				MenuState.menuOpened = true;
			}
		}
		bool flag5 = MenuState.menuOpened && MenuState.menuSmoothOpenTime < Settings.smoothOpenTime;
		if (flag5)
		{
			MenuState.menuSmoothOpenTime += Time.unscaledDeltaTime;
			bool flag6 = MenuState.menuSmoothOpenTime > Settings.smoothOpenTime;
			if (flag6)
			{
				MenuState.menuSmoothOpenTime = Settings.smoothOpenTime;
			}
		}
		else
		{
			bool flag7 = !MenuState.menuOpened && MenuState.menuSmoothOpenTime != 0f;
			if (flag7)
			{
				MenuState.menuSmoothOpenTime -= Time.unscaledDeltaTime;
				bool flag8 = MenuState.menuSmoothOpenTime < 0f;
				if (flag8)
				{
					MenuState.menuSmoothOpenTime = 0f;
				}
			}
		}
		bool dbjv74arVJtUMAqsSN0cWr9w = ScreenshotManager.IsSpying;
		if (dbjv74arVJtUMAqsSN0cWr9w)
		{
			float num = 1.3333334f;
			Camera main = Camera.main;
			bool flag9 = main != null && main.aspect != num;
			if (flag9)
			{
				main.aspect = num;
			}
			bool flag10 = MainCamera.instance != null && MainCamera.instance.aspect != num;
			if (flag10)
			{
				MainCamera.instance.aspect = num;
			}
		}
		else
		{
			bool flag11 = MiscConfig.useCustomAspectRatio && MiscConfig.customAspectRatio > 0f;
			if (flag11)
			{
				Camera main2 = Camera.main;
				bool flag12 = main2 != null && main2.aspect != MiscConfig.customAspectRatio;
				if (flag12)
				{
					main2.aspect = MiscConfig.customAspectRatio;
				}
				bool flag13 = MainCamera.instance != null && MainCamera.instance.aspect != MiscConfig.customAspectRatio;
				if (flag13)
				{
					MainCamera.instance.aspect = MiscConfig.customAspectRatio;
				}
			}
			else
			{
				float num2 = (float)Screen.width / (float)Screen.height;
				Camera main3 = Camera.main;
				bool flag14 = main3 != null && main3.aspect != num2;
				if (flag14)
				{
					main3.aspect = num2;
				}
				bool flag15 = MainCamera.instance != null && MainCamera.instance.aspect != num2;
				if (flag15)
				{
					MainCamera.instance.aspect = num2;
				}
			}
		}
		bool de30FqjVHC03X81IY6Y3eTNN = MenuState.clearPopupPending;
		if (de30FqjVHC03X81IY6Y3eTNN)
		{
			MenuState.popupDrawAction = null;
			MenuState.clearPopupPending = false;
		}
		bool flag16 = MenuState.pendingTabSwitch != null;
		if (flag16)
		{
			MenuState.currentTab = MenuState.pendingTabSwitch;
			MenuState.pendingTabSwitch = null;
		}
	}
	public void LateUpdate()
	{
		bool flag = MiscConfig.zoomExploit && MiscConfig.zoomKeybind != KeyCode.None && MiscConfig.zoomAmount > 0f;
		if (flag)
		{
			bool keyDown = Input.GetKeyDown(MiscConfig.zoomKeybind);
			bool key = Input.GetKey(MiscConfig.zoomKeybind);
			bool zoomToggle = MiscConfig.zoomToggle;
			if (zoomToggle)
			{
				bool flag2 = keyDown;
				if (flag2)
				{
					MiscConfig.zoomActive = !MiscConfig.zoomActive;
				}
			}
			else
			{
				MiscConfig.zoomActive = key;
			}
			bool zoomActive = MiscConfig.zoomActive;
			if (zoomActive)
			{
				bool flag3 = MainCamera.instance != null;
				if (flag3)
				{
					bool flag4 = MiscConfig.zoomOriginalFOV == 0f;
					if (flag4)
					{
						float captureFov = MainCamera.instance.fieldOfView;
						MiscConfig.zoomOriginalFOV = (!(captureFov <= 1000f)) ? 100f : captureFov;
					}
					float num = MiscConfig.zoomOriginalFOV / MiscConfig.zoomAmount;
					MainCamera.instance.fieldOfView = num;
				}
			}
			else
			{
				bool flag5 = MiscConfig.zoomOriginalFOV != 0f && MainCamera.instance != null;
				if (flag5)
				{
					MainCamera.instance.fieldOfView = MiscConfig.zoomOriginalFOV;
					MiscConfig.zoomOriginalFOV = 0f;
				}
			}
		}
		else
		{
			bool flag6 = MiscConfig.zoomActive && MiscConfig.zoomOriginalFOV != 0f && MainCamera.instance != null;
			if (flag6)
			{
				MainCamera.instance.fieldOfView = MiscConfig.zoomOriginalFOV;
			}
			MiscConfig.zoomActive = false;
			MiscConfig.zoomOriginalFOV = 0f;
		}
		bool flag7 = !MiscConfig.zoomActive && MiscConfig.useCustomFOV && MiscConfig.customFOV > 0f && MainCamera.instance != null;
		if (flag7)
		{
			MainCamera.instance.fieldOfView = MiscConfig.customFOV;
		}
		bool flag8 = !MiscConfig.zoomActive && MainCamera.instance != null && !(MainCamera.instance.fieldOfView <= 1000f);
		if (flag8)
		{
			MainCamera.instance.fieldOfView = 100f;
		}
		bool flag9 = !(OptionsSettings.fov <= 1000f);
		if (flag9)
		{
			OptionsSettings.fov = 100f;
		}
	}
	public static void DrawMenu()
	{
		MenuState.DrawWelcomeNotification();
		Color color = GUI.color;
		GUI.color = MenuState.GetMenuFadeColor();
		bool flag = GUI.color.a != 0f;
		if (flag)
		{
			bool flag2 = PlayerUI.window != null;
			if (flag2)
			{
				PlayerUI.window.showCursor = true;
			}
			bool drawBackgroundBlackout = Settings.drawBackgroundBlackout;
			if (drawBackgroundBlackout)
			{
				GUI.DrawTexture(new Rect(0f, 0f, (float)Screen.width, (float)Screen.height), MenuState.blackoutTexture);
			}
			// v2 cloud window: if the cursor is over the cloud window's title bar or
			// resize corner, it owns the drag this frame — don't also drag/resize the menu.
			bool cloudDragBlock = MenuState.showCloudConfigs
				&& MenuState.cloudWindowRect.width > 1f
				&& (new Rect(MenuState.cloudWindowRect.x, MenuState.cloudWindowRect.y, MenuState.cloudWindowRect.width, 28f).Contains(WindowDragger.LastMousePosition)
					|| new Rect(MenuState.cloudWindowRect.x + MenuState.cloudWindowRect.width - 15f, MenuState.cloudWindowRect.y + MenuState.cloudWindowRect.height - 15f, 15f, 15f).Contains(WindowDragger.LastMousePosition));
			if (!cloudDragBlock)
			{
				Rect rect = new Rect(MenuState.menuRect.x, MenuState.menuRect.y, MenuState.menuRect.width, 15f);
				WindowDragger.DragWindow(ref rect);
				rect.height = MenuState.menuRect.height;
				MenuState.menuRect = rect;
				Rect rect2 = new Rect(MenuState.menuRect.x + MenuState.menuRect.width - 15f, MenuState.menuRect.y + MenuState.menuRect.height - 15f, 15f, 15f);
				WindowDragger.DragWindow(ref rect2);
				MenuState.menuRect.width = MenuState.menuRect.width - (MenuState.menuRect.x + MenuState.menuRect.width - 15f - rect2.x);
				MenuState.menuRect.height = MenuState.menuRect.height - (MenuState.menuRect.y + MenuState.menuRect.height - 15f - rect2.y);
				MenuState.menuRect.width = Mathf.Max(MenuState.menuRect.width, 630f);
				MenuState.menuRect.height = Mathf.Max(MenuState.menuRect.height, 470f);
			}
			MenuState.DrawMenuPanels();
		}
		GUI.color = color;
	}
	private static void DrawMenuPanels()
	{
		GuiStyles.DcheckAccentUpdate();
		Color color = ColorConfig.GetColor("Menu background color");
		Color32 color2 = ColorConfig.GetColor("Menu line color");
		MenuGuiHelper.DrawRect(MenuState.menuRect, color, true, ScaleMode.StretchToFill);
		Texture2D animatedBgTexture = MenuGuiHelper.GetAnimatedBgTexture();
		float num = Time.time * 15f % 24f;
		float num2 = num / 24f;
		GUI.DrawTextureWithTexCoords(MenuState.menuRect, animatedBgTexture, new Rect(-num2, 0f, MenuState.menuRect.width / 24f, MenuState.menuRect.height / 24f), true);
		MenuGuiHelper.DrawRect(new Rect(MenuState.menuRect.x, MenuState.menuRect.y, MenuState.menuRect.width, 80f), new Color32(18, 18, 20, byte.MaxValue), true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(MenuState.menuRect.x, MenuState.menuRect.y + 80f, MenuState.menuRect.width, 1f), color2, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(MenuState.menuRect.x + 140f, MenuState.menuRect.y + 80f, 1f, MenuState.menuRect.height - 80f), color2, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(MenuState.menuRect.x, MenuState.menuRect.y, MenuState.menuRect.width, 1f), color2, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(MenuState.menuRect.x, MenuState.menuRect.y + MenuState.menuRect.height - 1f, MenuState.menuRect.width, 1f), color2, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(MenuState.menuRect.x, MenuState.menuRect.y, 1f, MenuState.menuRect.height), color2, true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(MenuState.menuRect.x + MenuState.menuRect.width - 1f, MenuState.menuRect.y, 1f, MenuState.menuRect.height), color2, true, ScaleMode.StretchToFill);
		new Rect(MenuState.menuRect.x + 10f, MenuState.menuRect.y + 80f, 120f, MenuState.menuRect.height);
		int num3 = 0;
		foreach (FeatureTabBase doekZ1zoBEuvfUmOs9QSpfX0X in MenuState.tabs)
		{
			try
			{
				doekZ1zoBEuvfUmOs9QSpfX0X.DrawTabHeader(num3);
			}
			catch
			{
			}
			num3++;
		}
		Rect rect = new Rect(MenuState.menuRect.x + 140f, MenuState.menuRect.y + 80f, MenuState.menuRect.width - 140f, MenuState.menuRect.height - 80f);
		bool flag = MenuState.currentTab != null;
		if (flag)
		{
			switch (MenuState.currentTab.GetTabCounts())
			{
			case TabCount.One:
				try
				{
					MenuGuiHelper.Panel(new Rect(rect.x + 15f, rect.y + 15f, rect.width - 30f, rect.height - 30f), delegate
					{
						MenuState.currentTab.DoTab(TabCount.One);
					}, 15);
				}
				catch
				{
				}
				break;
			case TabCount.Two:
				try
				{
					MenuGuiHelper.Panel(new Rect(rect.x + 15f, rect.y + 15f, rect.width / 2f - 45f, rect.height - 30f), delegate
					{
						MenuState.currentTab.DoTab(TabCount.One);
					}, 15);
				}
				catch
				{
				}
				try
				{
					MenuGuiHelper.Panel(new Rect(rect.x + rect.width / 2f - 13f, rect.y + 15f, rect.width / 2f - 15f, rect.height - 30f), delegate
					{
						MenuState.currentTab.DoTab(TabCount.Two);
					}, 15);
				}
				catch
				{
				}
				break;
			case TabCount.Three:
				try
				{
					MenuGuiHelper.Panel(new Rect(rect.x + 15f, rect.y + 15f, rect.width / 2f - 45f, rect.height - 30f), delegate
					{
						MenuState.currentTab.DoTab(TabCount.One);
					}, 15);
				}
				catch
				{
				}
				try
				{
					MenuGuiHelper.Panel(new Rect(rect.x + rect.width / 2f - 13f, rect.y + 15f, rect.width / 2f - 15f, rect.height / 2f - 30f), delegate
					{
						MenuState.currentTab.DoTab(TabCount.Two);
					}, 15);
				}
				catch
				{
				}
				try
				{
					MenuGuiHelper.Panel(new Rect(rect.x + rect.width / 2f - 13f, rect.y + rect.height / 2f, rect.width / 2f - 15f, rect.height / 2f - 15f), delegate
					{
						MenuState.currentTab.DoTab(TabCount.Three);
					}, 15);
				}
				catch
				{
				}
				break;
			}
		}
		try
		{
			bool flag2 = MenuState.popupDrawAction != null;
			if (flag2)
			{
				try
				{
					MenuState.popupDrawAction();
				}
				catch
				{
				}
				bool flag3 = MenuState.activePopupOwner == null;
				if (flag3)
				{
					MenuState.popupDrawAction = null;
				}
			}
		}
		catch
		{
		}
		try
		{
			MenuGuiHelper.LastPanelContentRect = new Rect(MenuState.menuRect.x + 160f, MenuState.menuRect.y + 15f, MenuState.menuRect.width - 170f, 60f);
			GUILayout.BeginArea(MenuGuiHelper.LastPanelContentRect);
			GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
			GUILayout.FlexibleSpace();
			MenuGuiHelper.StringPopupRow("Selected configuration: ", ConfigManager.ConfigNameSelector, 200, "");
			GUILayout.Space(15f);
			GUILayout.BeginVertical(Array.Empty<GUILayoutOption>());
			GUILayout.Space(15f);
			GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
			bool flag4 = MenuGuiHelper.ButtonIcon("Save", 0, 30, true);
			if (flag4)
			{
				ConfigManager.SaveConfig(ConfigManager.ConfigNameSelector.Selected);
				ConfigManager.RefreshConfigList();
			}
			GUILayout.Space(8f);
			bool flag5 = MenuGuiHelper.ButtonIcon("Load", 1, 30, true);
			if (flag5)
			{
				ConfigManager.LoadConfig(ConfigManager.ConfigNameSelector.Selected + ".conf");
			}
			GUILayout.Space(8f);
			bool flag6 = MenuGuiHelper.ButtonIcon("CFGs", 2, 30, true);
			if (flag6)
			{
				string text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Unturned_Data", "configs");
				Directory.CreateDirectory(text);
				Process.Start("explorer.exe", text);
			}
			GUILayout.Space(8f);
			bool flag7 = MenuGuiHelper.ButtonIcon("Cloud", 3, 30, true);
			if (flag7)
			{
				MenuState.showCloudConfigs = !MenuState.showCloudConfigs;
				if (MenuState.showCloudConfigs)
				{
					HamasNetwork.RefreshCloudConfigs();
				}
			}
			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();
			GUILayout.EndArea();
		}
		catch
		{
		}
		// ── Logo emblem — sized to the title text band (60px tall, centered) ──
		if (MenuState.s_LogoTex == null) MenuState.LoadLogoTexture();
		const float logoH = 60f;               // matches title label rect height
		const float logoAspect = 108f / 110f;  // exact emblem content aspect
		float logoW = logoH * logoAspect;      // ≈58.9px
		float logoX = MenuState.menuRect.x + 12f;
		float logoY = MenuState.menuRect.y + 10f; // same band as title text (y+10, h=60)
		if (MenuState.s_LogoTex != null)
		{
			GUI.DrawTexture(new Rect(logoX, logoY, logoW, logoH), MenuState.s_LogoTex, ScaleMode.ScaleToFit, true);
		}
		float titleX = MenuState.menuRect.x + ((MenuState.s_LogoTex != null) ? (12f + logoW + 12f) : 15f);
		// ── Title shadows + gradient text (same 60px band, center y+40) ──
		GUI.Label(new Rect(titleX + 1f, MenuState.menuRect.y + 11f, 240f, 60f), "<color=#141414>HAMASCLIENT</color>", GuiStyles.BigBoldLabelShadowStyle);
		GUI.Label(new Rect(titleX, MenuState.menuRect.y + 12f, 240f, 60f), "<color=#141414>HAMASCLIENT</color>", GuiStyles.BigBoldLabelShadowStyle);
		GUI.Label(new Rect(titleX, MenuState.menuRect.y + 10f, 240f, 60f), MenuState.GetHAMASGradientText() + "<color=#ffffff>CLIENT</color>", GuiStyles.BigBoldLabelStyle);
		// Cloud configs popup
		if (MenuState.showCloudConfigs)
		{
			DrawCloudConfigsPopup();
		}
		MenuState.DrawAimTargetESP();
		// Draw cursor last so it's always on top
		GUI.DrawTexture(new Rect(Input.mousePosition.x, (float)Screen.height - Input.mousePosition.y, 20f * GraphicsSettings.userInterfaceScale, 20f * GraphicsSettings.userInterfaceScale), GuiStyles.CursorTextureRef);
	}
	private static void DrawCloudConfigsPopup()
	{
		// ── v2: draggable window with Load / DL / Delete, search, local-file sync ──
		if (MenuState.cloudWindowRect.width < 1f)
		{
			float cw = 460f;
			float ch = 430f;
			float cx = MenuState.menuRect.x + MenuState.menuRect.width / 2f - cw / 2f;
			float cy = MenuState.menuRect.y + 90f;
			MenuState.cloudWindowRect = new Rect(cx, cy, cw, ch);
		}
		Rect win = MenuState.cloudWindowRect;

		// Drag: title bar only (cloudWinDrag claim + menu guard).
		Rect titleRect = new Rect(win.x, win.y, win.width - 30f, 26f);
		bool wasDragging = WindowDragger.IsDragging;
		if ((Input.GetMouseButtonDown(0) && titleRect.Contains(WindowDragger.LastMousePosition)) || (wasDragging && MenuState.cloudWinDrag))
		{
			MenuState.cloudWinDrag = true;
			if (Input.GetMouseButton(0) && !Input.GetMouseButtonUp(0))
			{
				win.x += Input.mousePosition.x - WindowDragger.LastMousePosition.x;
				win.y += (Screen.height - Input.mousePosition.y) - WindowDragger.LastMousePosition.y;
			}
			else
			{
				MenuState.cloudWinDrag = false;
			}
			WindowDragger.IsDragging = MenuState.cloudWinDrag;
		}

		// Resize: bottom-right corner
		Rect grip = new Rect(win.x + win.width - 15f, win.y + win.height - 15f, 15f, 15f);
		if ((Input.GetMouseButtonDown(0) && grip.Contains(WindowDragger.LastMousePosition)) || (wasDragging && MenuState.cloudWinResize))
		{
			MenuState.cloudWinResize = true;
			if (Input.GetMouseButton(0) && !Input.GetMouseButtonUp(0))
			{
				win.width = Mathf.Max(380f, win.width + Input.mousePosition.x - WindowDragger.LastMousePosition.x);
				win.height = Mathf.Max(300f, win.height + (Screen.height - Input.mousePosition.y) - WindowDragger.LastMousePosition.y);
			}
			else
			{
				MenuState.cloudWinResize = false;
			}
			WindowDragger.IsDragging = MenuState.cloudWinResize;
		}

		win.x = Mathf.Clamp(win.x, -win.width + 60f, Screen.width - 60f);
		win.y = Mathf.Clamp(win.y, 0f, Screen.height - 40f);
		MenuState.cloudWindowRect = win;

		// Window chrome
		MenuGuiHelper.DrawRect(win, new Color32(30, 30, 32, 250), true, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(win.x, win.y, win.width, 2f), MenuGuiHelper.GetAccentColor(255), true, ScaleMode.StretchToFill);
		GUI.Label(new Rect(win.x + 12f, win.y + 4f, win.width - 50f, 24f), "Cloud Configs", GuiStyles.SmallBoldWhiteLabelStyle);
		if (GUI.Button(new Rect(win.x + win.width - 30f, win.y + 3f, 24f, 22f), "X"))
		{
			MenuState.showCloudConfigs = false;
		}

		GUILayout.BeginArea(new Rect(win.x + 10f, win.y + 32f, win.width - 20f, win.height - 42f));

		// ── Username row: your chosen cloud identity ──
		GUILayout.BeginHorizontal();
		if (!HamasNetwork.IsAuthenticated)
		{
			GUI.color = Color.red;
			GUILayout.Label("Not connected", GuiStyles.SmallGrayLabelStyle);
			GUI.color = Color.white;
		}
		else if (MenuState.cloudNameEdit)
		{
			GUILayout.Label("Name:", GUILayout.Width(38f));
			MenuState.cloudNameInput = GUILayout.TextField(MenuState.cloudNameInput, 16, GUILayout.Width(130f));
			GUI.enabled = (MenuState.cloudNameInput.Trim().Length >= 3) && !MenuState.cloudBusy;
			if (GUILayout.Button("Save", GUILayout.Width(50f)))
			{
				string picked = MenuState.cloudNameInput.Trim();
				MenuState.cloudBusy = true;
				MenuState.cloudUploadStatus = "Setting name...";
				HamasNetwork.SetUsername(picked, delegate(bool ok, string msg)
				{
					MenuState.cloudBusy = false;
					if (ok)
					{
						MenuState.cloudNameEdit = false;
						MenuState.cloudUploadStatus = "Name set: " + picked;
						HamasNetwork.RefreshCloudConfigs();
					}
					else
					{
						MenuState.cloudUploadStatus = msg;
					}
				});
			}
			GUI.enabled = true;
			if (GUILayout.Button("Cancel", GUILayout.Width(55f)))
			{
				MenuState.cloudNameEdit = false;
			}
		}
		else
		{
			string curName = HamasNetwork.DisplayName;
			if (string.IsNullOrEmpty(curName))
			{
				GUI.color = Color.yellow;
				GUILayout.Label("Pick a username (3-16 chars) - used for configs & online list", GuiStyles.SmallGrayLabelStyle);
				GUI.color = Color.white;
			}
			else
			{
				GUI.color = Color.green;
				GUILayout.Label("Your name: " + curName, GuiStyles.SmallGrayLabelStyle);
				GUI.color = Color.white;
			}
			if (GUILayout.Button(string.IsNullOrEmpty(curName) ? "Choose" : "Change", GUILayout.Width(60f)) && !MenuState.cloudBusy)
			{
				MenuState.cloudNameInput = curName ?? "";
				MenuState.cloudNameEdit = true;
			}
		}
		GUILayout.EndHorizontal();

		GUILayout.Space(4f);

		// Status line
		string status = HamasNetwork.IsAuthenticated ? "Connected" : "Not connected - restart loader";
		if (MenuState.cloudBusy) status = "Working...";
		else if (!string.IsNullOrEmpty(MenuState.cloudDeleteStatus)) status = MenuState.cloudDeleteStatus;
		else if (!string.IsNullOrEmpty(MenuState.cloudUploadStatus)) status = MenuState.cloudUploadStatus;
		else if (HamasNetwork.IsFetchingConfigs) status = "Loading list...";
		else if (!string.IsNullOrEmpty(HamasNetwork.LastError)) status = "Error: " + HamasNetwork.LastError;
		GUILayout.Label(status, GuiStyles.SmallGrayLabelStyle);

		GUILayout.Space(4f);

		// Search + refresh
		GUILayout.BeginHorizontal();
		GUILayout.Label("Search:", GUILayout.Width(48f));
		MenuState.cloudSearch = GUILayout.TextField(MenuState.cloudSearch, 32, GUILayout.Width(140f));
		if (GUILayout.Button("Refresh", GUILayout.Width(60f)) && !MenuState.cloudBusy)
		{
			HamasNetwork.RefreshCloudConfigs();
		}
		GUILayout.FlexibleSpace();
		GUILayout.Label(HamasNetwork.CloudConfigs.Count + " configs", GuiStyles.SmallGrayLabelStyle);
		GUILayout.EndHorizontal();

		GUILayout.Space(4f);

		// Upload current config
		GUILayout.BeginHorizontal();
		GUILayout.Label("Name:", GUILayout.Width(38f));
		MenuState.cloudUploadName = GUILayout.TextField(MenuState.cloudUploadName, 32, GUILayout.Width(110f));
		GUILayout.Label("Desc:", GUILayout.Width(34f));
		MenuState.cloudUploadDesc = GUILayout.TextField(MenuState.cloudUploadDesc, 100, GUILayout.Width(120f));
		if (GUILayout.Button("Upload current", GUILayout.Width(95f)) && !MenuState.cloudBusy)
		{
			TryUploadCurrentConfig();
		}
		GUILayout.EndHorizontal();

		GUILayout.Space(6f);

		// Config list
		MenuState.cloudConfigScroll = GUILayout.BeginScrollView(MenuState.cloudConfigScroll);
		string search = (MenuState.cloudSearch ?? "").Trim().ToLower();
		List<HamasNetwork.CloudConfigInfo> snapshot;
		lock (HamasNetwork.ConfigListLock)
		{
			snapshot = new List<HamasNetwork.CloudConfigInfo>(HamasNetwork.CloudConfigs);
		}
		foreach (HamasNetwork.CloudConfigInfo cfg in snapshot)
		{
			if (search.Length > 0
				&& !(cfg.Name ?? "").ToLower().Contains(search)
				&& !(cfg.Author ?? "").ToLower().Contains(search)
				&& !(cfg.Description ?? "").ToLower().Contains(search))
			{
				continue;
			}

			bool haveLocal = System.IO.File.Exists(UnityEngine.Application.dataPath + "/configs/" + cfg.Name + ".conf");
			bool isCurrent = ConfigManager.CurrentConfigName == cfg.Name;

			GUILayout.BeginHorizontal();
			string title = cfg.Name + (isCurrent ? "  [LOADED]" : (haveLocal ? "  [saved]" : ""));
			GUILayout.Label(title, GuiStyles.SmallBoldWhiteLabelStyle, GUILayout.Width(170f));
			GUILayout.Label((cfg.Author ?? "?") + "  " + (cfg.Size / 1024) + "KB", GuiStyles.SmallGrayLabelStyle, GUILayout.Width(110f));

			if (isCurrent)
			{
				GUILayout.Label("Loaded", GuiStyles.SmallGrayLabelStyle, GUILayout.Width(58f));
			}
			else if (GUILayout.Button("Load", GUILayout.Width(58f)) && !MenuState.cloudBusy)
			{
				MenuState.cloudBusy = true;
				MenuState.cloudDeleteStatus = "";
				string fileName = cfg.Name + ".conf";
				if (haveLocal)
				{
					ConfigManager.LoadConfig(fileName);
					MenuState.cloudUploadStatus = "Loaded " + cfg.Name;
					MenuState.cloudBusy = false;
				}
				else
				{
					MenuState.cloudUploadStatus = "Downloading " + cfg.Name + "...";
					HamasNetwork.DownloadConfig(cfg.Name, delegate(bool ok, string msg)
					{
						if (ok)
						{
							ConfigManager.RefreshConfigList();
							ConfigManager.LoadConfig(fileName);
							MenuState.cloudUploadStatus = "Loaded " + cfg.Name;
						}
						else
						{
							MenuState.cloudUploadStatus = "DL failed: " + msg;
						}
						MenuState.cloudBusy = false;
					});
				}
			}

			if (!haveLocal)
			{
				if (GUILayout.Button("DL", GUILayout.Width(40f)) && !MenuState.cloudBusy)
				{
					MenuState.cloudBusy = true;
					MenuState.cloudDeleteStatus = "";
					MenuState.cloudUploadStatus = "Downloading " + cfg.Name + "...";
					HamasNetwork.DownloadConfig(cfg.Name, delegate(bool ok, string msg)
					{
						MenuState.cloudUploadStatus = ok ? ("Saved " + cfg.Name) : ("DL failed: " + msg);
						if (ok) ConfigManager.RefreshConfigList();
						MenuState.cloudBusy = false;
					});
				}
			}
			else
			{
				GUILayout.Space(44f);
			}

			if (cfg.Own == 1)
			{
				if (GUILayout.Button("Delete", GUILayout.Width(56f)) && !MenuState.cloudBusy)
				{
					MenuState.cloudDeleteTarget = cfg.Name;
					MenuState.cloudDeleteStatus = "";
				}
			}
			else
			{
				GUILayout.Space(60f);
			}
			GUILayout.EndHorizontal();

			if (!string.IsNullOrEmpty(cfg.Description))
			{
				GUILayout.Label("   " + cfg.Description, GuiStyles.SmallGrayLabelStyle);
			}
		}
		GUILayout.EndScrollView();
		GUILayout.EndArea();

		// Delete confirmation popup (drawn on top of everything)
		if (MenuState.cloudDeleteTarget != null)
		{
			Rect d = new Rect(win.x + win.width / 2f - 130f, win.y + win.height / 2f - 55f, 260f, 110f);
			MenuGuiHelper.DrawRect(d, new Color32(20, 20, 22, 252), true, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(d.x, d.y, d.width, 2f), MenuGuiHelper.GetAccentColor(255), true, ScaleMode.StretchToFill);
			GUILayout.BeginArea(new Rect(d.x + 10f, d.y + 10f, d.width - 20f, d.height - 20f));
			GUILayout.Label("Delete \"" + MenuState.cloudDeleteTarget + "\" from the cloud?", GuiStyles.SmallBoldWhiteLabelStyle);
			GUILayout.FlexibleSpace();
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Confirm", GUILayout.Width(80f)) && !MenuState.cloudBusy)
			{
				string target = MenuState.cloudDeleteTarget;
				MenuState.cloudBusy = true;
				HamasNetwork.DeleteCloudConfig(target, delegate(bool ok, string msg)
				{
					MenuState.cloudBusy = false;
					MenuState.cloudDeleteStatus = ok ? ("Deleted " + target) : ("Delete failed: " + msg);
					if (ok)
					{
						MenuState.cloudDeleteTarget = null;
						HamasNetwork.RefreshCloudConfigs();
					}
				});
			}
			GUILayout.Space(10f);
			if (GUILayout.Button("Cancel", GUILayout.Width(80f)))
			{
				MenuState.cloudDeleteTarget = null;
			}
			GUILayout.EndHorizontal();
			GUILayout.EndArea();
		}
	}
	private static void TryUploadCurrentConfig()
	{
		if (!HamasNetwork.IsAuthenticated)
		{
			MenuState.cloudUploadStatus = "Not connected - restart loader";
			return;
		}
		if (string.IsNullOrEmpty(MenuState.cloudUploadName))
		{
			MenuState.cloudUploadStatus = "Enter a name first";
			return;
		}
		if (string.IsNullOrEmpty(ConfigManager.CurrentConfigName) || ConfigManager.CurrentConfigName == "config save name here")
		{
			MenuState.cloudUploadStatus = "Save a config first!";
			return;
		}
		string localPath = UnityEngine.Application.dataPath + "/configs/" + ConfigManager.CurrentConfigName + ".conf";
		if (!System.IO.File.Exists(localPath))
		{
			MenuState.cloudUploadStatus = "Config file not found";
			return;
		}
		MenuState.cloudBusy = true;
		MenuState.cloudUploadStatus = "Uploading...";
		byte[] data = System.IO.File.ReadAllBytes(localPath);
		HamasNetwork.UploadConfig(MenuState.cloudUploadName, MenuState.cloudUploadDesc, data, delegate(bool success, string msg)
		{
			MenuState.cloudBusy = false;
			MenuState.cloudUploadStatus = success ? "Uploaded!" : msg;
			if (success)
			{
				MenuState.cloudUploadName = "";
				MenuState.cloudUploadDesc = "";
				HamasNetwork.RefreshCloudConfigs();
			}
		});
	}
	private static void DrawWelcomeNotification()
	{
		bool flag = MenuState.DwelcomeTimer > 0f;
		if (flag)
		{
			float num = 1f;
			bool flag2 = MenuState.DwelcomeTimer < 2f;
			if (flag2)
			{
				num = MenuState.DwelcomeTimer / 2f;
			}
			Color color = GUI.color;
			GUI.color = new Color(1f, 1f, 1f, num);
			bool flag3 = MenuState.DnotificationTitleStyle == null;
			if (flag3)
			{
				MenuState.DnotificationTitleStyle = new GUIStyle(GUI.skin.label);
				MenuState.DnotificationTitleStyle.alignment = TextAnchor.MiddleCenter;
				MenuState.DnotificationTitleStyle.fontSize = 42;
				MenuState.DnotificationTitleStyle.fontStyle = FontStyle.Bold;
				MenuState.DnotificationTitleStyle.richText = true;
				GUIStyle dnotificationTitleStyle = MenuState.DnotificationTitleStyle;
				Font font;
				if ((font = Font.CreateDynamicFontFromOSFont("Trebuchet MS", 42)) == null)
				{
					font = Font.CreateDynamicFontFromOSFont("Verdana", 42) ?? GUI.skin.font;
				}
				dnotificationTitleStyle.font = font;
			}
			bool flag4 = MenuState.DnotificationSubtitleStyle == null;
			if (flag4)
			{
				MenuState.DnotificationSubtitleStyle = new GUIStyle(GUI.skin.label);
				MenuState.DnotificationSubtitleStyle.alignment = TextAnchor.MiddleCenter;
				MenuState.DnotificationSubtitleStyle.fontSize = 18;
				MenuState.DnotificationSubtitleStyle.richText = true;
				GUIStyle dnotificationSubtitleStyle = MenuState.DnotificationSubtitleStyle;
				Font font2;
				if ((font2 = Font.CreateDynamicFontFromOSFont("Trebuchet MS", 18)) == null)
				{
					font2 = Font.CreateDynamicFontFromOSFont("Verdana", 18) ?? GUI.skin.font;
				}
				dnotificationSubtitleStyle.font = font2;
			}
			float num2 = 460f;
			float num3 = 140f;
			float num4 = (float)(Screen.width / 2) - num2 / 2f;
			float num5 = (float)(Screen.height / 2) - num3 / 2f - 50f;
			Rect rect = new Rect(num4, num5, num2, num3);
			MenuGuiHelper.DrawRect(rect, new Color32(20, 20, 22, 240), false, ScaleMode.StretchToFill);
			Color32 accentColor = MenuGuiHelper.GetAccentColor(byte.MaxValue);
			MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y, rect.width, 2f), accentColor, false, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y + rect.height - 2f, rect.width, 2f), accentColor, false, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y, 2f, rect.height), accentColor, false, ScaleMode.StretchToFill);
			MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 2f, rect.y, 2f, rect.height), accentColor, false, ScaleMode.StretchToFill);
			Rect rect2 = new Rect(rect.x, rect.y + 25f, rect.width, 50f);
			string text = MenuState.GetHAMASGradientText() + "<color=#ffffff>CLIENT</color>";
			Color contentColor = GUI.contentColor;
			GUI.contentColor = new Color(0f, 0f, 0f, num * 0.6f);
			GUI.Label(new Rect(rect2.x + 2f, rect2.y + 2f, rect2.width, rect2.height), "HAMASCLIENT", MenuState.DnotificationTitleStyle);
			GUI.contentColor = Color.white;
			GUI.Label(rect2, text, MenuState.DnotificationTitleStyle);
			Rect rect3 = new Rect(rect.x, rect.y + 80f, rect.width, 35f);
			Color32 accentColor2 = MenuGuiHelper.GetAccentColor(byte.MaxValue);
			string text2 = string.Format("#{0:X2}{1:X2}{2:X2}", accentColor2.r, accentColor2.g, accentColor2.b);
			string text3 = string.Concat(new string[] { "<color=#888888>Successfully Loaded!</color>  <color=", text2, ">Press </color><color=#ffffff><b>F1</b></color><color=", text2, "> to open the menu</color>" });
			GUI.contentColor = new Color(0f, 0f, 0f, num * 0.6f);
			GUI.Label(new Rect(rect3.x + 1f, rect3.y + 1f, rect3.width, rect3.height), "Successfully Loaded!  Press F1 to open the menu", MenuState.DnotificationSubtitleStyle);
			GUI.contentColor = Color.white;
			GUI.Label(rect3, text3, MenuState.DnotificationSubtitleStyle);
			GUI.contentColor = contentColor;
			GUI.color = color;
		}
	}
	private static string GetHAMASGradientText()
	{
		Color32 accentColor = MenuGuiHelper.GetAccentColor(byte.MaxValue);
		Color32 color = new Color32((byte)Mathf.Min(255, (int)(accentColor.r + 80)), (byte)Mathf.Min(255, (int)(accentColor.g + 80)), (byte)Mathf.Min(255, (int)(accentColor.b + 80)), byte.MaxValue);
		Color32 color2 = new Color32((byte)Mathf.Max(0, (int)(accentColor.r - 40)), (byte)Mathf.Max(0, (int)(accentColor.g - 40)), (byte)Mathf.Max(0, (int)(accentColor.b - 40)), byte.MaxValue);
		Color32 color3 = accentColor;
		Color32[] array = new Color32[] { color, color3, color2, color3 };
		string text = "HAMAS";
		string text2 = "";
		float num = 1.5f;
		float num2 = Time.time * num % (float)array.Length;
		for (int i = 0; i < text.Length; i++)
		{
			float num3 = ((float)i + num2) % (float)array.Length;
			int num4 = Mathf.FloorToInt(num3) % array.Length;
			int num5 = (num4 + 1) % array.Length;
			float num6 = num3 - Mathf.Floor(num3);
			Color32 color4 = Color32.Lerp(array[num4], array[num5], num6);
			string text3 = string.Format("#{0:X2}{1:X2}{2:X2}", color4.r, color4.g, color4.b);
			text2 = string.Concat(new string[]
			{
				text2,
				"<color=",
				text3,
				">",
				text[i].ToString(),
				"</color>"
			});
		}
		return text2;
	}
	private static void DrawAimTargetESP()
	{
		bool flag = Player.player == null || Player.player.look == null || Camera.main == null;
		if (!flag)
		{
			Color color = GUI.color;
			Color contentColor = GUI.contentColor;
			try
			{
				bool flag2 = AimbotConfig.drawTarget && (!(Player.player != null) || Player.player.look.perspective > EPlayerPerspective.FIRST);
				if (flag2)
				{
					AimObjective daseuOZmKRI1v3DQRcsmyTKsC = AimbotUtil.currentAimObjective;
					bool flag3 = daseuOZmKRI1v3DQRcsmyTKsC.Target != null;
					if (flag3)
					{
						Vector3 vector = (Vector3)daseuOZmKRI1v3DQRcsmyTKsC.Target;
						Vector3 vector2 = Camera.main.WorldToScreenPoint(vector);
						bool flag4 = vector2.z > 0f && vector2.x > 0f && vector2.x < (float)Screen.width && vector2.y > 0f && vector2.y < (float)Screen.height;
						if (flag4)
						{
							float num = 20f;
							GUI.color = new Color(1f, 0f, 0f, 1f);
							Rect rect = new Rect(vector2.x - num / 2f, (float)Screen.height - vector2.y - num / 2f, num, num);
							MenuGuiHelper.DrawRect(rect, new Color(1f, 0f, 0f, 0.5f), false, ScaleMode.StretchToFill);
							bool flag5 = AimbotConfig.targetLineStartX != 0f || AimbotConfig.targetLineStartY != 0f;
							if (flag5)
							{
								Vector2 vector3 = new Vector2(AimbotConfig.targetLineStartX, AimbotConfig.targetLineStartY);
								Vector2 vector4 = new Vector2(vector2.x, (float)Screen.height - vector2.y);
								MenuGuiHelper.DrawLine(vector3, vector4, new Color(1f, 0f, 0f, 0.8f), 2f);
							}
						}
					}
				}
				bool previewHitLimb = AimbotConfig.previewHitLimb;
				if (previewHitLimb)
				{
					AimObjective daseuOZmKRI1v3DQRcsmyTKsC2 = AimbotUtil.currentAimObjective;
					bool flag6 = daseuOZmKRI1v3DQRcsmyTKsC2.Target != null;
					if (flag6)
					{
						Vector3 vector5 = (Vector3)daseuOZmKRI1v3DQRcsmyTKsC2.Target;
						bool replaceHitLimbToCustom = MiscConfig.replaceHitLimbToCustom;
						if (replaceHitLimbToCustom)
						{
							Transform transform = AimbotUtil.GetAimBoneTransform(daseuOZmKRI1v3DQRcsmyTKsC2.Target);
							bool flag7 = transform != null;
							if (flag7)
							{
								PlayerBones d7ElSFH0pY0XmMbO1Ij5Yf3Tp;
								bool flag8 = PlayerBones.playersByPlayer.TryGetValue(Player.player, out d7ElSFH0pY0XmMbO1Ij5Yf3Tp);
								if (flag8)
								{
									Limb replacedHitLimb = MiscConfig.replacedHitLimb;
									vector5 = d7ElSFH0pY0XmMbO1Ij5Yf3Tp.GetLimbPosition(replacedHitLimb);
								}
							}
						}
						Vector3 vector6 = Camera.main.WorldToScreenPoint(vector5);
						bool flag9 = vector6.z > 0f && vector6.x > 0f && vector6.x < (float)Screen.width && vector6.y > 0f && vector6.y < (float)Screen.height;
						if (flag9)
						{
							float num2 = (float)AimbotConfig.hitMarkSize;
							GUI.color = new Color(0f, 1f, 0f, 1f);
							Rect rect2 = new Rect(vector6.x - num2 / 2f, (float)Screen.height - vector6.y - num2 / 2f, num2, num2);
							MenuGuiHelper.DrawLine(new Vector2(rect2.x, rect2.y), new Vector2(rect2.x + rect2.width, rect2.y + rect2.height), new Color(0f, 1f, 0f, 0.9f), 2f);
							MenuGuiHelper.DrawLine(new Vector2(rect2.x + rect2.width, rect2.y), new Vector2(rect2.x, rect2.y + rect2.height), new Color(0f, 1f, 0f, 0.9f), 2f);
						}
					}
				}
			}
			catch
			{
			}
			GUI.color = color;
			GUI.contentColor = contentColor;
		}
	}
	// Set to true to re-enable the Overrides tab
	public static bool overridesTabEnabled = false;
	// Set to true to re-enable the vanished players window option
	public static bool vanishedPlayersWindowEnabled = false;
	// Set to true to re-enable the GoldTomPearl custom model
	public static bool goldTomPearlModelEnabled = false;
	public static float menuSmoothOpenTime = 0f;
	public static bool menuOpenedBacking = false;
	public static bool showCloudConfigs = false;
	public static Vector2 cloudConfigScroll = Vector2.zero;
	// ── v2 cloud window: draggable, Load/DL/Delete, search ──
	public static Rect cloudWindowRect = new Rect(0f, 0f, 0f, 0f);
	public static bool cloudWinDrag = false;
	public static bool cloudWinResize = false;
	public static string cloudSearch = "";
	public static string cloudDeleteTarget = null;
	public static string cloudDeleteStatus = "";
	public static bool cloudBusy = false;	public static string cloudUploadName = "";
		public static string cloudUploadDesc = "";
		public static string cloudUploadStatus = "";
		public static bool cloudNameEdit = false;
		public static string cloudNameInput = "";
	public static object[] scrollRectExs = new object[0];
	private static FeatureTabBase[] tabs;
	public static FeatureTabBase currentTab;
	public static object activePopupOwner;
	public static Rect activePopupRect;
	public static global::System.Action popupDrawAction;
	public static Rect menuRect = new Rect((float)(Screen.width / 2 - 320), (float)(Screen.height / 2 - 240), 840f, 570f);
	public static Texture2D blackoutTexture;
	private static int lastTabIndex = 0;
	private static float unusedTimer = 0f;
	private static float unusedTimer2 = 0f;
	public static bool clearPopupPending = false;
	public static FeatureTabBase pendingTabSwitch;
	private static bool unusedFlag = false;
	private static float unusedDelay = 0.4f;
	private static float DopenDelayTimer = 1.5f;
	private static bool DopenDelayedTriggered = false;
	private static float DwelcomeTimer = 5f;
	private static GUIStyle DnotificationTitleStyle;
	private static GUIStyle DnotificationSubtitleStyle;
	// ── Hamas logo icon (embedded resource) ──
	private static Texture2D s_LogoTex;
	private static bool s_LogoLoadAttempted;
	private static void LoadLogoTexture()
	{
		if (s_LogoLoadAttempted) return;
		s_LogoLoadAttempted = true;
		try
		{
			var asm = System.Reflection.Assembly.GetExecutingAssembly();
			using (var stream = asm.GetManifestResourceStream("HAMASCLIENT.hamaslogo.png"))
			{
				if (stream == null) { Logger.LogClient("logo: resource not found"); return; }
				byte[] data = new byte[stream.Length];
				stream.Read(data, 0, data.Length);
				s_LogoTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
				s_LogoTex.filterMode = FilterMode.Bilinear;
				ImageConversion.LoadImage(s_LogoTex, data);
			}
		}
		catch (System.Exception ex) { Logger.LogClient("logo: " + ex.Message); }
	}
}
