using System;
using System.Reflection;
using SDG.Unturned;
using UnityEngine;
public class LevelLightingPatches
{
	[HookMethodAttribute(typeof(LevelLighting), "updateLighting", new Type[] { })]
	public static void UpdateLightingPatch()
	{
		LevelLightingPatches.SavedLightingTime = LevelLighting.time;
		bool flag = MiscConfig.customDayTime && !ScreenshotManager.IsSpying;
		bool flag2 = flag;
		if (flag2)
		{
			LevelLightingPatches.LightingTimeField.Set(MiscConfig.customTime);
		}
		OverrideManager.CallOriginal(null, Array.Empty<object>());
		bool flag3 = MiscConfig.customDayTime && !ScreenshotManager.IsSpying;
		bool flag4 = flag3;
		if (flag4)
		{
			LevelLightingPatches.LightingTimeField.Set(LevelLightingPatches.SavedLightingTime);
		}
		LevelLightingPatches.SkyboxMaterialField.RefereshFieldValue();
		bool dbjv74arVJtUMAqsSN0cWr9w = ScreenshotManager.IsSpying;
		bool flag5 = dbjv74arVJtUMAqsSN0cWr9w;
		if (flag5)
		{
			LevelLightingPatches.UpdateSkyboxColorsMethod.Invoke(Array.Empty<object>());
		}
		else
		{
			bool overrideSkyColor = MiscConfig.overrideSkyColor;
			bool flag6 = overrideSkyColor;
			if (flag6)
			{
				LevelLightingPatches.SkyboxMaterialField.fldValue.SetColor("_SkyColor", ColorConfig.GetColor("Custom Sky Color"));
			}
			else
			{
				LevelLightingPatches.UpdateSkyboxColorsMethod.Invoke(Array.Empty<object>());
			}
			bool overrideSunColor = MiscConfig.overrideSunColor;
			bool flag7 = overrideSunColor;
			if (flag7)
			{
				LevelLightingPatches.SkyboxMaterialField.fldValue.SetColor("_SunColor", ColorConfig.GetColor("Custom Sun Color"));
			}
			bool overrideCloudsColor = MiscConfig.overrideCloudsColor;
			bool flag8 = overrideCloudsColor;
			if (flag8)
			{
				LevelLightingPatches.SkyboxMaterialField.fldValue.SetColor("_CloudColor", ColorConfig.GetColor("Custom Clouds"));
			}
			bool overrideCloudsRimColor = MiscConfig.overrideCloudsRimColor;
			bool flag9 = overrideCloudsRimColor;
			if (flag9)
			{
				LevelLightingPatches.SkyboxMaterialField.fldValue.SetColor("_CloudRimColor", ColorConfig.GetColor("Custom Clouds Rim"));
			}
		}
	}
	public static float SavedLightingTime = 1200f;
	public static ReflectedField<float> LightingTimeField = new ReflectedField<float>(typeof(LevelLighting), "_time", BindingFlags.Static | BindingFlags.NonPublic);
	public static ReflectedField<Material> SkyboxMaterialField = new ReflectedField<Material>(typeof(LevelLighting), "skybox", BindingFlags.Static | BindingFlags.NonPublic);
	public static ReflectedMethod UpdateSkyboxColorsMethod = new ReflectedMethod(typeof(LevelLighting), "updateSkyboxColors", BindingFlags.Static | BindingFlags.NonPublic);
}
