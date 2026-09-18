using System;
using UnityEngine;
public class WorldTab : FeatureTabBase
{
	public override string GetName()
	{
		return "World";
	}
	public override TabCount GetTabCounts()
	{
		return TabCount.Three;
	}
	public override void DoTab(TabCount tc)
	{
		bool flag = tc == TabCount.One;
		if (flag)
		{
			base.DrawSectionHeader("Optimization");
			MenuGuiHelper.Button("Master Texture Limit: " + QualitySettings.masterTextureLimit.ToString(), -1, true, null);
			GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
			for (int i = 0; i < 6; i++)
			{
				bool flag2 = MenuGuiHelper.Button(i.ToString(), -1, true, null);
				if (flag2)
				{
					QualitySettings.masterTextureLimit = i;
				}
			}
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
			for (int j = 6; j < 11; j++)
			{
				bool flag3 = MenuGuiHelper.Button(j.ToString(), -1, true, null);
				if (flag3)
				{
					QualitySettings.masterTextureLimit = j;
				}
			}
			GUILayout.EndHorizontal();
			QualitySettings.lodBias = MenuGuiHelper.LabeledFloatSlider("LOD Scaling: ", QualitySettings.lodBias, 0.05f, 20f, -1);
			Settings.disallowParticles = MenuGuiHelper.DrawCheckbox(Settings.disallowParticles, "Disable Game Particles", Array.Empty<GUILayoutOption>());
			Settings.disallowWeaponTraces = MenuGuiHelper.DrawCheckbox(Settings.disallowWeaponTraces, "Disable Weapon Tracers", Array.Empty<GUILayoutOption>());
			bool flag4 = MenuGuiHelper.Button("Remove Game Skybox", -1, true, null);
			if (flag4)
			{
				RenderSettings.skybox = null;
			}
			bool flag5 = MenuGuiHelper.Button("Remove All Game UI", -1, true, null);
			if (flag5)
			{
				MenuGuiHelper.ClearUiEffects();
			}
			bool flag6 = MenuGuiHelper.Button("Remove Game Camera Scripts", -1, true, null);
			if (flag6)
			{
				MiscActions.ClearCameraScripts();
			}
		}
		else
		{
			bool flag7 = tc == TabCount.Two;
			if (flag7)
			{
				WorldTab.PreferencesScroll = GUILayout.BeginScrollView(WorldTab.PreferencesScroll, Array.Empty<GUILayoutOption>());
				base.DrawSectionHeader("Preferences");
				Settings.drawTracers = MenuGuiHelper.DrawCheckbox(Settings.drawTracers, "Draw Weapon Tracers", Array.Empty<GUILayoutOption>());
				bool drawTracers = Settings.drawTracers;
				if (drawTracers)
				{
					Settings.useGLTracers = MenuGuiHelper.DrawCheckbox(Settings.useGLTracers, "Use GL Tracers", Array.Empty<GUILayoutOption>());
					Settings.tracerType = Settings.tracerType.EnumPopup("Tracer Type: ", Settings.tracerType.ToDisplayNameBallisticStepType());
					Settings.tracersLifetime = MenuGuiHelper.LabeledFloatSlider("Tracer Lifetime: ", Settings.tracersLifetime, 0f, 6f, -1);
					Settings.tracersWidth = MenuGuiHelper.LabeledFloatSlider("Tracer Width: ", Settings.tracersWidth, 0f, 1f, -1);
				}
				Settings.drawDamageHitmark = MenuGuiHelper.DrawCheckbox(Settings.drawDamageHitmark, "Draw Damage Hitmarkers", Array.Empty<GUILayoutOption>());
				bool drawDamageHitmark = Settings.drawDamageHitmark;
				if (drawDamageHitmark)
				{
					Settings.damageHitmarksLifetime = MenuGuiHelper.LabeledFloatSlider("Hitmarker Lifetime: ", Settings.damageHitmarksLifetime, 0f, 10f, -1);
					Settings.isDamageHitmarkersCombined = MenuGuiHelper.DrawCheckbox(Settings.isDamageHitmarkersCombined, "Use Combined Hitmarkers", Array.Empty<GUILayoutOption>());
					bool isDamageHitmarkersCombined = Settings.isDamageHitmarkersCombined;
					if (isDamageHitmarkersCombined)
					{
						Settings.damageHitmarksCombineDistance = MenuGuiHelper.LabeledFloatSlider("Hitmarker Combine Distance: ", Settings.damageHitmarksCombineDistance, 0f, 5f, -1);
					}
				}
				Settings.walkingTracers = MenuGuiHelper.DrawCheckbox(Settings.walkingTracers, "Show Walking Tracers", Array.Empty<GUILayoutOption>());
				bool walkingTracers = Settings.walkingTracers;
				if (walkingTracers)
				{
					Settings.seeOwnWalkingTracers = MenuGuiHelper.DrawCheckbox(Settings.seeOwnWalkingTracers, "Show Own Walking Tracers", Array.Empty<GUILayoutOption>());
					Settings.walkingTracersWidth = MenuGuiHelper.LabeledFloatSlider("Walking Tracer Width: ", Settings.walkingTracersWidth, 0.01f, 1f, -1);
					Settings.walkingTracersLifetime = MenuGuiHelper.LabeledFloatSlider("Walking Tracer Lifetime: ", Settings.walkingTracersLifetime, 0f, 6f, -1);
					Settings.walkingTracersDrawDistance = MenuGuiHelper.LabeledIntSlider("Walking Tracer Draw Distance: ", Settings.walkingTracersDrawDistance, 10, 250, -1);
				}
				Settings.useCustomCrosshair = MenuGuiHelper.DrawCheckbox(Settings.useCustomCrosshair, "Use Custom Crosshair", Array.Empty<GUILayoutOption>());
				bool useCustomCrosshair = Settings.useCustomCrosshair;
				if (useCustomCrosshair)
				{
					Settings.forceDisableDefaultCrosshair = MenuGuiHelper.DrawCheckbox(Settings.forceDisableDefaultCrosshair, "Force Disable Default Crosshair", Array.Empty<GUILayoutOption>());
					Settings.crosshairWidth = MenuGuiHelper.LabeledIntSlider("Crosshair Width: ", Settings.crosshairWidth, 1, 15, -1);
					Settings.crosshairHeight = MenuGuiHelper.LabeledIntSlider("Crosshair Height: ", Settings.crosshairHeight, 2, 40, -1);
					bool flag8 = Settings.crosshairType == CrosshairStyle.Gap;
					if (flag8)
					{
						Settings.crosshairGap = MenuGuiHelper.LabeledIntSlider("Crosshair Gap: ", Settings.crosshairGap, 0, 20, -1);
					}
					Settings.crosshairType = Settings.crosshairType.EnumPopup("Crosshair Type: ", Settings.crosshairType.ToString());
				}
				Settings.drawHorizontalInfoPanel = MenuGuiHelper.DrawCheckbox(Settings.drawHorizontalInfoPanel, "Draw Horizontal Info Panel", Array.Empty<GUILayoutOption>());
				bool drawHorizontalInfoPanel = Settings.drawHorizontalInfoPanel;
				if (drawHorizontalInfoPanel)
				{
					Settings.infoPanelPaddingFromScreen = MenuGuiHelper.LabeledIntSlider("Info Panel Screen Padding: ", Settings.infoPanelPaddingFromScreen, 0, 50, -1);
					Settings.infoPanelSize = MenuGuiHelper.LabeledIntSlider("Info Panel Size: ", Settings.infoPanelSize, 6, 35, -1);
					Settings.drawInfoPanelPadding = MenuGuiHelper.DrawCheckbox(Settings.drawInfoPanelPadding, "Draw Info Panel Padding", Array.Empty<GUILayoutOption>());
					bool drawInfoPanelPadding = Settings.drawInfoPanelPadding;
					if (drawInfoPanelPadding)
					{
						Settings.infoPanelPadding = MenuGuiHelper.LabeledIntSlider("Info Panel Padding: ", Settings.infoPanelPadding, 0, 5, -1);
						Settings.infoPanelPaddingPlacement = Settings.infoPanelPaddingPlacement.EnumPopup("Padding Placement: ", Settings.infoPanelPaddingPlacement.ToString());
					}
				}
				Settings.drawInfo = MenuGuiHelper.DrawCheckbox(Settings.drawInfo, "Draw Info", Array.Empty<GUILayoutOption>());
				Settings.drawBackgroundBlackout = MenuGuiHelper.DrawCheckbox(Settings.drawBackgroundBlackout, "Draw Background Blackout", Array.Empty<GUILayoutOption>());
				Settings.smoothMenuOpen = MenuGuiHelper.DrawCheckbox(Settings.smoothMenuOpen, "Smooth Menu Open", Array.Empty<GUILayoutOption>());
				bool smoothMenuOpen = Settings.smoothMenuOpen;
				if (smoothMenuOpen)
				{
					Settings.smoothOpenTime = MenuGuiHelper.LabeledFloatSlider("Smooth Open Time: ", Settings.smoothOpenTime, 0.05f, 0.5f, -1);
				}
				Settings.menuCirclingRadius = MenuGuiHelper.LabeledIntSlider("Menu Circling Radius: ", Settings.menuCirclingRadius, 0, 20, -1);
				Settings.enableUserLogger = MenuGuiHelper.DrawCheckbox(Settings.enableUserLogger, "Enable User Logger", Array.Empty<GUILayoutOption>());
				bool enableUserLogger = Settings.enableUserLogger;
				if (enableUserLogger)
				{
					Settings.loggerTextOutline = Settings.loggerTextOutline.EnumPopup("Logger Text Outline: ", Settings.loggerTextOutline.ToDisplayNameOutlineMode());
					Settings.loggerTextCase = Settings.loggerTextCase.EnumPopup("Logger Text Case: ", Settings.loggerTextCase.ToDisplayNameTextCase());
				}
				Settings.playerStepsCircle = MenuGuiHelper.DrawCheckbox(Settings.playerStepsCircle, "Show Player Steps", Array.Empty<GUILayoutOption>());
				bool playerStepsCircle = Settings.playerStepsCircle;
				if (playerStepsCircle)
				{
					Settings.seeOwnSteps = MenuGuiHelper.DrawCheckbox(Settings.seeOwnSteps, "Show Own Steps", Array.Empty<GUILayoutOption>());
					Settings.stepsDrawDistance = MenuGuiHelper.LabeledIntSlider("Steps Draw Distance: ", Settings.stepsDrawDistance, 5, 500, -1);
					Settings.stepsLifetime = MenuGuiHelper.LabeledFloatSlider("Steps Lifetime: ", Settings.stepsLifetime, 0.25f, 5f, -1);
					Settings.stepsSpreadingDistance = MenuGuiHelper.LabeledFloatSlider("Steps Spreading Distance: ", Settings.stepsSpreadingDistance, 0f, 5f, -1);
					Settings.stepsRunDistanceMultiplier = MenuGuiHelper.LabeledFloatSlider("Steps Run Distance Multiplier: ", Settings.stepsRunDistanceMultiplier, 1f, 4f, -1);
					Settings.stepsDropDistanceMultiplier = MenuGuiHelper.LabeledFloatSlider("Steps Drop Distance Multiplier: ", Settings.stepsDropDistanceMultiplier, 1f, 4f, -1);
					Settings.stepStyle = Settings.stepStyle.EnumPopup("Step Style: ", Settings.stepStyle.ToString());
				}
				Settings.chamsedRepaintOwnSkin = MenuGuiHelper.DrawCheckbox(Settings.chamsedRepaintOwnSkin, "Enable Self Chams", Array.Empty<GUILayoutOption>());
				Settings.localPlayerWireframe = MenuGuiHelper.DrawCheckbox(Settings.localPlayerWireframe, "Enable Self Wireframe", Array.Empty<GUILayoutOption>());
				GUILayout.EndScrollView();
			}
			else
			{
				base.DrawSectionHeader("Viewmodel");
				WorldTab.fovAim = MenuGuiHelper.LabeledFloatSlider("Aim FOV: ", WorldTab.fovAim, 15f, 180f, -1);
				WorldTab.fovHip = MenuGuiHelper.LabeledFloatSlider("Hip FOV: ", WorldTab.fovHip, 15f, 180f, -1);
				WorldTab.offsetDepth = MenuGuiHelper.LabeledFloatSlider("Depth Offset: ", WorldTab.offsetDepth, -5f, 5f, -1);
				WorldTab.offsetHorizontal = MenuGuiHelper.LabeledFloatSlider("Horizontal Offset: ", WorldTab.offsetHorizontal, -5f, 5f, -1);
				WorldTab.offsetVertical = MenuGuiHelper.LabeledFloatSlider("Vertical Offset: ", WorldTab.offsetVertical, -5f, 5f, -1);
			}
		}
	}
	public static Vector2 PreferencesScroll = Vector2.zero;
	[ConfigBindAttribute("Viewmodel", "Aim FOV")]
	[SaveableNameAttribute]
	public static float fovAim = 90f;
	[ConfigBindAttribute("Viewmodel", "Hip FOV")]
	[SaveableNameAttribute]
	public static float fovHip = 90f;
	[ConfigBindAttribute("Viewmodel", "Depth Offset")]
	[SaveableNameAttribute]
	public static float offsetDepth = 0f;
	[ConfigBindAttribute("Viewmodel", "Horizontal Offset")]
	[SaveableNameAttribute]
	public static float offsetHorizontal = 0f;
	[ConfigBindAttribute("Viewmodel", "Vertical Offset")]
	[SaveableNameAttribute]
	public static float offsetVertical = 0f;
}
