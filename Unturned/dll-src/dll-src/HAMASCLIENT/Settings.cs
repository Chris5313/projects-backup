using System;
public static class Settings
{
	// (get) Token: 0x060001C0 RID: 448 RVA: 0x0001ADC8 File Offset: 0x00018FC8
	// (set) Token: 0x060001C1 RID: 449 RVA: 0x0001ADE0 File Offset: 0x00018FE0
	[ConfigBindAttribute("Visual options", "Smooth open time")]
	[SaveableNameAttribute]
	public static float smoothOpenTime
	{
		get
		{
			return Settings.smoothOpenTimeBacking;
		}
		set
		{
			bool flag = Settings.smoothOpenTimeBacking != value;
			if (flag)
			{
				bool menuOpened = MenuState.menuOpened;
				if (menuOpened)
				{
					MenuState.menuSmoothOpenTime = value;
				}
			}
			Settings.smoothOpenTimeBacking = value;
		}
	}
	// (get) Token: 0x060001C2 RID: 450 RVA: 0x0001AE18 File Offset: 0x00019018
	// (set) Token: 0x060001C3 RID: 451 RVA: 0x0001AE30 File Offset: 0x00019030
	[ConfigBindAttribute("Visual options", "Chamsed repaint own skin")]
	[SaveableNameAttribute]
	public static bool chamsedRepaintOwnSkin
	{
		get
		{
			return Settings.chamsedRepaintOwnSkinBacking;
		}
		set
		{
			bool flag = false;
			bool flag2 = value != Settings.chamsedRepaintOwnSkinBacking;
			if (flag2)
			{
				flag = true;
			}
			Settings.chamsedRepaintOwnSkinBacking = value;
			bool flag3 = flag;
			if (flag3)
			{
				try
				{
					LocalPlayerChams.ApplyOwnSkinChams();
				}
				catch
				{
				}
			}
		}
	}
	// (get) Token: 0x060001C4 RID: 452 RVA: 0x0001AE80 File Offset: 0x00019080
	// (set) Token: 0x060001C5 RID: 453 RVA: 0x0001AE97 File Offset: 0x00019097
	[ConfigBindAttribute("Visual options", "Blur on menu")]
	[SaveableNameAttribute]
	public static bool blurOnMenu
	{
		get
		{
			return Settings.blurOnMenuBacking;
		}
		set
		{
			Settings.blurOnMenuBacking = value;
		}
	}
	public static bool UnusedFlag = false;
	[ConfigBindAttribute("Visual options", "Menu width")]
	[SaveableNameAttribute]
	public static int menuWidth = 640;
	[ConfigBindAttribute("Visual options", "Menu height")]
	[SaveableNameAttribute]
	public static int menuHeight = 480;
	[ConfigBindAttribute("Visual options", "Draw tracers")]
	[SaveableNameAttribute]
	public static bool drawTracers = false;
	[ConfigBindAttribute("Visual options", "Use GL tracers")]
	[SaveableNameAttribute]
	public static bool useGLTracers = false;
	[ConfigBindAttribute("Visual options", "Tracer type")]
	[SaveableNameAttribute]
	public static ProjectileType tracerType = ProjectileType.BallisticMoved;
	[ConfigBindAttribute("Visual options", "Tracers width")]
	[SaveableNameAttribute]
	public static float tracersWidth = 0.15f;
	[ConfigBindAttribute("Visual options", "Tracers lifetime")]
	[SaveableNameAttribute]
	public static float tracersLifetime = 3f;
	[ConfigBindAttribute("Visual options", "Walking tracers")]
	[SaveableNameAttribute]
	public static bool walkingTracers;
	[ConfigBindAttribute("Visual options", "See own walking tracers")]
	[SaveableNameAttribute]
	public static bool seeOwnWalkingTracers = true;
	[ConfigBindAttribute("Visual options", "Walking tracers width")]
	[SaveableNameAttribute]
	public static float walkingTracersWidth = 0.3f;
	[ConfigBindAttribute("Visual options", "Walking tracers lifetime")]
	[SaveableNameAttribute]
	public static float walkingTracersLifetime = 2f;
	[ConfigBindAttribute("Visual options", "Walking tracers draw distance")]
	[SaveableNameAttribute]
	public static int walkingTracersDrawDistance = 75;
	[ConfigBindAttribute("Visual options", "Draw horizontal info-panel")]
	[SaveableNameAttribute]
	public static bool drawHorizontalInfoPanel = false;
	[ConfigBindAttribute("Visual options", "Info-panel padding from screen")]
	[SaveableNameAttribute]
	public static int infoPanelPaddingFromScreen = 15;
	[ConfigBindAttribute("Visual options", "Info-panel size")]
	[SaveableNameAttribute]
	public static int infoPanelSize = 15;
	[ConfigBindAttribute("Visual options", "Draw info-panel padding")]
	[SaveableNameAttribute]
	public static bool drawInfoPanelPadding = true;
	[ConfigBindAttribute("Visual options", "Info-panel padding")]
	[SaveableNameAttribute]
	public static int infoPanelPadding = 3;
	[ConfigBindAttribute("Visual options", "Info-panel padding placement")]
	[SaveableNameAttribute]
	public static ScreenEdge infoPanelPaddingPlacement = ScreenEdge.Bottom;
	[ConfigBindAttribute("Visual options", "Use custom crosshair")]
	[SaveableNameAttribute]
	public static bool useCustomCrosshair = false;
	[ConfigBindAttribute("Visual options", "Force disable default crosshair")]
	[SaveableNameAttribute]
	public static bool forceDisableDefaultCrosshair = true;
	[ConfigBindAttribute("Visual options", "Crosshair type")]
	[SaveableNameAttribute]
	public static CrosshairStyle crosshairType = CrosshairStyle.Solid;
	[ConfigBindAttribute("Visual options", "Crosshair height")]
	[SaveableNameAttribute]
	public static int crosshairHeight = 6;
	[ConfigBindAttribute("Visual options", "Crosshair width")]
	[SaveableNameAttribute]
	public static int crosshairWidth = 2;
	[ConfigBindAttribute("Visual options", "Crosshair gap")]
	[SaveableNameAttribute]
	public static int crosshairGap = 2;
	[ConfigBindAttribute("Visual options", "Player tracers working distance")]
	[SaveableNameAttribute]
	public static int playerTracersWorkingDistance = 100;
	[ConfigBindAttribute("Visual options", "Max player tracer points")]
	[SaveableNameAttribute]
	public static int maxPlayerTracerPoints = 60;
	[ConfigBindAttribute("Visual options", "Draw damage hitmarks")]
	[SaveableNameAttribute]
	public static bool drawDamageHitmark = true;
	[ConfigBindAttribute("Visual options", "Damage hitmarks lifetime")]
	[SaveableNameAttribute]
	public static float damageHitmarksLifetime = 5f;
	[ConfigBindAttribute("Visual options", "Is damage hitmarkers combined")]
	[SaveableNameAttribute]
	public static bool isDamageHitmarkersCombined = false;
	[ConfigBindAttribute("Visual options", "Scale combined hitmarkers")]
	[SaveableNameAttribute]
	public static bool scaleCombinedHitmarkers = true;
	[ConfigBindAttribute("Visual options", "Damage hitmarks combine distance")]
	[SaveableNameAttribute]
	public static float damageHitmarksCombineDistance = 1.5f;
	[ConfigBindAttribute("Visual options", "Disallow particles")]
	[SaveableNameAttribute]
	public static bool disallowParticles = false;
	[ConfigBindAttribute("Visual options", "Disallow weapon traces")]
	[SaveableNameAttribute]
	public static bool disallowWeaponTraces = false;
	[ConfigBindAttribute("Visual options", "Draw info")]
	[SaveableNameAttribute]
	public static bool drawInfo = true;
	[ConfigBindAttribute("Visual options", "Draw background blackout")]
	[SaveableNameAttribute]
	public static bool drawBackgroundBlackout = true;
	public static bool blurOnMenuBacking = true;
	[ConfigBindAttribute("Visual options", "Menu circling radius")]
	[SaveableNameAttribute]
	public static int menuCirclingRadius = 5;
	[ConfigBindAttribute("Visual options", "Smooth menu open")]
	[SaveableNameAttribute]
	public static bool smoothMenuOpen = true;
	public static float smoothOpenTimeBacking = 0.25f;
	[ConfigBindAttribute("Visual options", "Use legacy sliders")]
	[SaveableNameAttribute]
	public static bool useLegacySliders = false;
	[ConfigBindAttribute("Visual options", "Rainbow fading on menu header")]
	[SaveableNameAttribute]
	public static bool rainbowFadingOnMenuHeader = false;
	[ConfigBindAttribute("Visual options", "Enable user logger")]
	[SaveableNameAttribute]
	public static bool enableUserLogger = false;
	[ConfigBindAttribute("Visual options", "Logger text outline")]
	[SaveableNameAttribute]
	public static TextOutlineStyle loggerTextOutline = TextOutlineStyle.None;
	[ConfigBindAttribute("Visual options", "Logger text case")]
	[SaveableNameAttribute]
	public static TextCase loggerTextCase = TextCase.Default;
	[ConfigBindAttribute("Visual options", "Player steps circle")]
	[SaveableNameAttribute]
	public static bool playerStepsCircle = false;
	[ConfigBindAttribute("Visual options", "See own steps")]
	[SaveableNameAttribute]
	public static bool seeOwnSteps = false;
	[ConfigBindAttribute("Visual options", "Steps draw distance")]
	[SaveableNameAttribute]
	public static int stepsDrawDistance = 100;
	[ConfigBindAttribute("Visual options", "Steps spreading distance")]
	[SaveableNameAttribute]
	public static float stepsSpreadingDistance = 1f;
	[ConfigBindAttribute("Visual options", "Steps run distance multiplier")]
	[SaveableNameAttribute]
	public static float stepsRunDistanceMultiplier = 1.5f;
	[ConfigBindAttribute("Visual options", "Steps drop distance multiplier")]
	[SaveableNameAttribute]
	public static float stepsDropDistanceMultiplier = 1.8f;
	[ConfigBindAttribute("Visual options", "Steps lifetime")]
	[SaveableNameAttribute]
	public static float stepsLifetime = 1f;
	[ConfigBindAttribute("Visual options", "Step style")]
	[SaveableNameAttribute]
	public static StepStyle stepStyle = StepStyle.Circle;
	public static bool chamsedRepaintOwnSkinBacking = false;
	[ConfigBindAttribute("Visual options", "Wireframe")]
	[SaveableNameAttribute]
	public static bool wireframeChams = false;
	[ConfigBindAttribute("Visual options", "Local player wireframe")]
	[SaveableNameAttribute]
	public static bool localPlayerWireframe = false;
	[ConfigBindAttribute("Visual options", "Wireframe line density")]
	[SaveableNameAttribute]
	public static int wireframeLineDensity = 50;
}
