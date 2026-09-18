using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SDG.Unturned;
using UnityEngine;
public static class UnturnedSettingsConfig
{
	public static void EnsureConfigDirectory()
	{
		bool flag = !Directory.Exists(Application.dataPath + "/configs/");
		if (flag)
		{
			Directory.CreateDirectory(Application.dataPath + "/configs/");
		}
	}
	public static void RefreshConfigList()
	{
		UnturnedSettingsConfig.configFileNames.Clear();
		foreach (FileInfo fileInfo in new DirectoryInfo(Application.dataPath + "/configs/").GetFiles())
		{
			bool flag = fileInfo.Name.EndsWith(".conf");
			if (flag)
			{
				UnturnedSettingsConfig.configFileNames.Add(fileInfo.Name);
			}
		}
	}
	public static void Initialize()
	{
		UnturnedSettingsConfig.EnsureConfigDirectory();
		UnturnedSettingsConfig.RefreshConfigList();
		UnturnedSettingsConfig.graphicsUiUpdateAllMethod = typeof(MenuConfigurationGraphicsUI).GetMethod("updateAll", BindingFlags.Static | BindingFlags.NonPublic);
		UnturnedSettingsConfig.optionsUiUpdateAllMethod = typeof(MenuConfigurationOptionsUI).GetMethod("updateAll", BindingFlags.Static | BindingFlags.NonPublic);
		UnturnedSettingsConfig.controlsUiUpdateAllMethod = typeof(MenuConfigurationControlsUI).GetMethod("updateAll", BindingFlags.Static | BindingFlags.NonPublic);
	}
	public static void LoadConfig(string fileName)
	{
		UnturnedSettingsConfig.EnsureConfigDirectory();
		try
		{
			ByteReader dut0a6FCClF9uncpHt4baoWwu = new ByteReader(File.ReadAllBytes(Application.dataPath + "/configs/" + fileName));
			byte b = dut0a6FCClF9uncpHt4baoWwu.ReadByte();
			GraphicsSettings.anisotropicFilteringMode = (EAnisotropicFilteringMode)dut0a6FCClF9uncpHt4baoWwu.ReadByte();
			GraphicsSettings.antiAliasingType = (EAntiAliasingType)dut0a6FCClF9uncpHt4baoWwu.ReadByte();
			GraphicsSettings.blast = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			GraphicsSettings.blend = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			GraphicsSettings.bloom = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			GraphicsSettings.buffer = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			GraphicsSettings.chromaticAberration = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			GraphicsSettings.debris = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			GraphicsSettings.effectQuality = (EGraphicQuality)dut0a6FCClF9uncpHt4baoWwu.ReadByte();
			GraphicsSettings.filmGrain = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			GraphicsSettings.foliageFocus = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			GraphicsSettings.foliageQuality = (EGraphicQuality)dut0a6FCClF9uncpHt4baoWwu.ReadByte();
			GraphicsSettings.fullscreenMode = (FullScreenMode)dut0a6FCClF9uncpHt4baoWwu.ReadByte();
			GraphicsSettings.glitter = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			GraphicsSettings.grassDisplacement = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			GraphicsSettings.isAmbientOcclusionEnabled = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			GraphicsSettings.IsItemIconAntiAliasingEnabled = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			GraphicsSettings.landmarkQuality = (EGraphicQuality)dut0a6FCClF9uncpHt4baoWwu.ReadByte();
			GraphicsSettings.lightingQuality = (EGraphicQuality)dut0a6FCClF9uncpHt4baoWwu.ReadByte();
			GraphicsSettings.normalizedDrawDistance = dut0a6FCClF9uncpHt4baoWwu.ReadSingle();
			GraphicsSettings.NormalizedFarClipDistance = dut0a6FCClF9uncpHt4baoWwu.ReadSingle();
			GraphicsSettings.normalizedLandmarkDrawDistance = dut0a6FCClF9uncpHt4baoWwu.ReadSingle();
			GraphicsSettings.outlineQuality = (EGraphicQuality)dut0a6FCClF9uncpHt4baoWwu.ReadByte();
			GraphicsSettings.planarReflectionQuality = (EGraphicQuality)dut0a6FCClF9uncpHt4baoWwu.ReadByte();
			GraphicsSettings.puddle = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			GraphicsSettings.ragdolls = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			GraphicsSettings.reflectionQuality = (EGraphicQuality)dut0a6FCClF9uncpHt4baoWwu.ReadByte();
			GraphicsSettings.renderMode = (ERenderMode)dut0a6FCClF9uncpHt4baoWwu.ReadByte();
			GraphicsSettings.scopeQuality = (EGraphicQuality)dut0a6FCClF9uncpHt4baoWwu.ReadByte();
			GraphicsSettings.skyboxReflection = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			GraphicsSettings.sunShaftsQuality = (EGraphicQuality)dut0a6FCClF9uncpHt4baoWwu.ReadByte();
			GraphicsSettings.TargetFrameRate = dut0a6FCClF9uncpHt4baoWwu.ReadInt32();
			GraphicsSettings.terrainQuality = (EGraphicQuality)dut0a6FCClF9uncpHt4baoWwu.ReadByte();
			bool flag = b <= 1;
			if (flag)
			{
				dut0a6FCClF9uncpHt4baoWwu.Position++;
			}
			GraphicsSettings.triplanar = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			GraphicsSettings.uncapLandmarks = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			GraphicsSettings.UnfocusedTargetFrameRate = dut0a6FCClF9uncpHt4baoWwu.ReadInt32();
			GraphicsSettings.userInterfaceScale = dut0a6FCClF9uncpHt4baoWwu.ReadSingle();
			GraphicsSettings.UseTargetFrameRate = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			GraphicsSettings.UseUnfocusedTargetFrameRate = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			GraphicsSettings.waterQuality = (EGraphicQuality)dut0a6FCClF9uncpHt4baoWwu.ReadByte();
			GraphicsSettings.IsWindEnabled = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			try
			{
				GraphicsSettings.save();
			}
			catch
			{
			}
			UnturnedSettingsConfig.graphicsUiUpdateAllMethod.Invoke(null, new object[0]);
			OptionsSettings.backgroundColor = dut0a6FCClF9uncpHt4baoWwu.ReadColor();
			OptionsSettings.badColor = dut0a6FCClF9uncpHt4baoWwu.ReadColor();
			OptionsSettings.cursorColor = dut0a6FCClF9uncpHt4baoWwu.ReadColor();
			OptionsSettings.fontColor = dut0a6FCClF9uncpHt4baoWwu.ReadColor();
			OptionsSettings.foregroundColor = dut0a6FCClF9uncpHt4baoWwu.ReadColor();
			OptionsSettings.fov = dut0a6FCClF9uncpHt4baoWwu.ReadSingle();
			bool fovFlag = !(OptionsSettings.fov <= 1000f);
			if (fovFlag)
			{
				OptionsSettings.fov = 100f;
			}
			OptionsSettings.gameVolume = dut0a6FCClF9uncpHt4baoWwu.ReadSingle();
			OptionsSettings.metric = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			OptionsSettings.proUI = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			OptionsSettings.shadowColor = dut0a6FCClF9uncpHt4baoWwu.ReadColor();
			OptionsSettings.ShouldHitmarkersFollowWorldPosition = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			OptionsSettings.UnfocusedVolume = dut0a6FCClF9uncpHt4baoWwu.ReadSingle();
			OptionsSettings.VoiceAlwaysRecording = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			OptionsSettings.voiceVolume = dut0a6FCClF9uncpHt4baoWwu.ReadSingle();
			OptionsSettings.ambience = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			OptionsSettings.chatText = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			OptionsSettings.chatVoiceIn = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			OptionsSettings.chatVoiceOut = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			OptionsSettings.criticalHitmarkerColor = dut0a6FCClF9uncpHt4baoWwu.ReadColor();
			OptionsSettings.crosshairColor = dut0a6FCClF9uncpHt4baoWwu.ReadColor();
			OptionsSettings.crosshairShape = (ECrosshairShape)dut0a6FCClF9uncpHt4baoWwu.ReadByte();
			OptionsSettings.debug = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			OptionsSettings.enableScreenshotsOnLoadingScreen = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			OptionsSettings.enableScreenshotSupersampling = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			OptionsSettings.featuredWorkshop = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			OptionsSettings.filter = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			OptionsSettings.gore = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			OptionsSettings.hints = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			OptionsSettings.hitmarkerColor = dut0a6FCClF9uncpHt4baoWwu.ReadColor();
			OptionsSettings.hitmarkerStyle = (EHitmarkerStyle)dut0a6FCClF9uncpHt4baoWwu.ReadByte();
			OptionsSettings.loadingScreenMusicVolume = dut0a6FCClF9uncpHt4baoWwu.ReadSingle();
			bool flag2 = b == 0;
			if (flag2)
			{
				dut0a6FCClF9uncpHt4baoWwu.Position++;
			}
			else
			{
				OptionsSettings.MusicMasterVolume = dut0a6FCClF9uncpHt4baoWwu.ReadSingle();
				OptionsSettings.MainMenuMusicVolume = dut0a6FCClF9uncpHt4baoWwu.ReadSingle();
				OptionsSettings.ambientMusicVolume = dut0a6FCClF9uncpHt4baoWwu.ReadSingle();
				OptionsSettings.deathMusicVolume = dut0a6FCClF9uncpHt4baoWwu.ReadSingle();
				OptionsSettings.loadingScreenMusicVolume = dut0a6FCClF9uncpHt4baoWwu.ReadSingle();
			}
			OptionsSettings.pauseWhenUnfocused = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			OptionsSettings.screenshotSizeMultiplier = dut0a6FCClF9uncpHt4baoWwu.ReadInt32();
			OptionsSettings.shouldNametagFadeOut = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			OptionsSettings.showHotbar = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			OptionsSettings.splashscreen = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			OptionsSettings.staticCrosshairSize = dut0a6FCClF9uncpHt4baoWwu.ReadSingle();
			OptionsSettings.streamer = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			OptionsSettings.talk = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			OptionsSettings.timer = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			OptionsSettings.useStaticCrosshair = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			OptionsSettings.vehicleThirdPersonCameraMode = (EVehicleThirdPersonCameraMode)dut0a6FCClF9uncpHt4baoWwu.ReadByte();
			OptionsSettings.volume = dut0a6FCClF9uncpHt4baoWwu.ReadSingle();
			try
			{
				OptionsSettings.save();
			}
			catch
			{
			}
			UnturnedSettingsConfig.optionsUiUpdateAllMethod.Invoke(null, new object[0]);
			ControlsSettings.aiming = (EControlMode)dut0a6FCClF9uncpHt4baoWwu.ReadByte();
			ControlsSettings.crouching = (EControlMode)dut0a6FCClF9uncpHt4baoWwu.ReadByte();
			ControlsSettings.invert = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			ControlsSettings.invertFlight = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
			ControlsSettings.leaning = (EControlMode)dut0a6FCClF9uncpHt4baoWwu.ReadByte();
			ControlsSettings.mouseAimSensitivity = dut0a6FCClF9uncpHt4baoWwu.ReadSingle();
			ControlsSettings.projectionRatioCoefficient = dut0a6FCClF9uncpHt4baoWwu.ReadSingle();
			ControlsSettings.proning = (EControlMode)dut0a6FCClF9uncpHt4baoWwu.ReadByte();
			ControlsSettings.sensitivityScalingMode = (ESensitivityScalingMode)dut0a6FCClF9uncpHt4baoWwu.ReadByte();
			ControlsSettings.sprinting = (EControlMode)dut0a6FCClF9uncpHt4baoWwu.ReadByte();
			ushort num = dut0a6FCClF9uncpHt4baoWwu.ReadUInt16();
			bool flag3 = ControlsSettings.bindings.Length == (int)num;
			if (flag3)
			{
				for (ushort num2 = 0; num2 < num; num2 += 1)
				{
					ControlsSettings.bindings[(int)num2].key = (KeyCode)dut0a6FCClF9uncpHt4baoWwu.ReadUInt16();
				}
			}
			try
			{
				ControlsSettings.save();
			}
			catch
			{
			}
			UnturnedSettingsConfig.controlsUiUpdateAllMethod.Invoke(null, new object[0]);
			UnturnedSettingsConfig.lastLoadedConfigName = fileName.Substring(0, fileName.Length - ".conf".Length);
		}
		catch (Exception ex)
		{
			Debug.Log(ex.Message);
			Debug.Log(ex.StackTrace);
		}
	}
	public static void SaveConfig(string name)
	{
		UnturnedSettingsConfig.EnsureConfigDirectory();
		PacketWriter dtqtgBvjhehJ9nKOGQCJPsaGO = new PacketWriter();
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteByte(2);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteByte((byte)GraphicsSettings.anisotropicFilteringMode);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteByte((byte)GraphicsSettings.antiAliasingType);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(GraphicsSettings.blast);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(GraphicsSettings.blend);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(GraphicsSettings.bloom);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(GraphicsSettings.buffer);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(GraphicsSettings.chromaticAberration);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(GraphicsSettings.debris);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteByte((byte)GraphicsSettings.effectQuality);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(GraphicsSettings.filmGrain);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(GraphicsSettings.foliageFocus);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteByte((byte)GraphicsSettings.foliageQuality);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteByte((byte)GraphicsSettings.fullscreenMode);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(GraphicsSettings.glitter);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(GraphicsSettings.grassDisplacement);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(GraphicsSettings.isAmbientOcclusionEnabled);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(GraphicsSettings.IsItemIconAntiAliasingEnabled);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteByte((byte)GraphicsSettings.landmarkQuality);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteByte((byte)GraphicsSettings.lightingQuality);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteSingle(GraphicsSettings.normalizedDrawDistance);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteSingle(GraphicsSettings.NormalizedFarClipDistance);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteSingle(GraphicsSettings.normalizedLandmarkDrawDistance);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteByte((byte)GraphicsSettings.outlineQuality);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteByte((byte)GraphicsSettings.planarReflectionQuality);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(GraphicsSettings.puddle);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(GraphicsSettings.ragdolls);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteByte((byte)GraphicsSettings.reflectionQuality);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteByte((byte)GraphicsSettings.renderMode);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteByte((byte)GraphicsSettings.scopeQuality);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(GraphicsSettings.skyboxReflection);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteByte((byte)GraphicsSettings.sunShaftsQuality);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteInt32(GraphicsSettings.TargetFrameRate);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteByte((byte)GraphicsSettings.terrainQuality);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(GraphicsSettings.triplanar);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(GraphicsSettings.uncapLandmarks);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteInt32(GraphicsSettings.UnfocusedTargetFrameRate);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteSingle(GraphicsSettings.userInterfaceScale);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(GraphicsSettings.UseTargetFrameRate);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(GraphicsSettings.UseUnfocusedTargetFrameRate);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteByte((byte)GraphicsSettings.waterQuality);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(GraphicsSettings.IsWindEnabled);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteColor(OptionsSettings.backgroundColor);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteColor(OptionsSettings.badColor);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteColor(OptionsSettings.cursorColor);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteColor(OptionsSettings.fontColor);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteColor(OptionsSettings.foregroundColor);			bool fovFlag2 = !(OptionsSettings.fov <= 1000f);
			if (fovFlag2)
			{
				OptionsSettings.fov = 100f;
			}
			dtqtgBvjhehJ9nKOGQCJPsaGO.WriteSingle(OptionsSettings.fov);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteSingle(OptionsSettings.gameVolume);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(OptionsSettings.metric);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(OptionsSettings.proUI);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteColor(OptionsSettings.shadowColor);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(OptionsSettings.ShouldHitmarkersFollowWorldPosition);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteSingle(OptionsSettings.UnfocusedVolume);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(OptionsSettings.VoiceAlwaysRecording);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteSingle(OptionsSettings.voiceVolume);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(OptionsSettings.ambience);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(OptionsSettings.chatText);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(OptionsSettings.chatVoiceIn);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(OptionsSettings.chatVoiceOut);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteColor(OptionsSettings.criticalHitmarkerColor);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteColor(OptionsSettings.crosshairColor);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteByte((byte)OptionsSettings.crosshairShape);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(OptionsSettings.debug);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(OptionsSettings.enableScreenshotsOnLoadingScreen);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(OptionsSettings.enableScreenshotSupersampling);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(OptionsSettings.featuredWorkshop);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(OptionsSettings.filter);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(OptionsSettings.gore);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(OptionsSettings.hints);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteColor(OptionsSettings.hitmarkerColor);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteByte((byte)OptionsSettings.hitmarkerStyle);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteSingle(OptionsSettings.loadingScreenMusicVolume);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteSingle(OptionsSettings.MusicMasterVolume);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteSingle(OptionsSettings.MainMenuMusicVolume);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteSingle(OptionsSettings.ambientMusicVolume);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteSingle(OptionsSettings.deathMusicVolume);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteSingle(OptionsSettings.loadingScreenMusicVolume);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(OptionsSettings.pauseWhenUnfocused);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteInt32(OptionsSettings.screenshotSizeMultiplier);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(OptionsSettings.shouldNametagFadeOut);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(OptionsSettings.showHotbar);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(OptionsSettings.splashscreen);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteSingle(OptionsSettings.staticCrosshairSize);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(OptionsSettings.streamer);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(OptionsSettings.talk);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(OptionsSettings.timer);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(OptionsSettings.useStaticCrosshair);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteByte((byte)OptionsSettings.vehicleThirdPersonCameraMode);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteSingle(OptionsSettings.volume);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteByte((byte)ControlsSettings.aiming);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteByte((byte)ControlsSettings.crouching);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(ControlsSettings.invert);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(ControlsSettings.invertFlight);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteByte((byte)ControlsSettings.leaning);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteSingle(ControlsSettings.mouseAimSensitivity);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteSingle(ControlsSettings.projectionRatioCoefficient);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteByte((byte)ControlsSettings.proning);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteByte((byte)ControlsSettings.sensitivityScalingMode);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteByte((byte)ControlsSettings.sprinting);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteUInt16((ushort)ControlsSettings.bindings.Length);
		foreach (ControlBinding controlBinding in ControlsSettings.bindings)
		{
			dtqtgBvjhehJ9nKOGQCJPsaGO.WriteUInt16((ushort)controlBinding.key);
		}
		File.WriteAllBytes(Application.dataPath + "/configs/" + name + ".conf", dtqtgBvjhehJ9nKOGQCJPsaGO.Buffer.ToArray());
	}
	public static List<string> configFileNames = new List<string>();
	public static string lastLoadedConfigName = "config save name here";
	private const byte configVersion = 2;
	private const string configExtension = ".conf";
	private static MethodInfo optionsUiUpdateAllMethod;
	private static MethodInfo graphicsUiUpdateAllMethod;
	private static MethodInfo controlsUiUpdateAllMethod;
}
