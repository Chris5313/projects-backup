using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SDG.Unturned;
using UnityEngine;

namespace gatyware
{
	public static class MiscVisuals
	{
		[DllImport("kernel32.dll")]
		private static extern bool VirtualProtect(IntPtr addr, int size, uint prot, out uint old);

		private static void WriteJmp(IntPtr from, IntPtr to)
		{
			uint prot;
			MiscVisuals.VirtualProtect(from, 14, 64U, out prot);
			byte[] array = new byte[14];
			array[0] = byte.MaxValue;
			array[1] = 37;
			BitConverter.GetBytes(to.ToInt64()).CopyTo(array, 6);
			Marshal.Copy(array, 0, from, 14);
			MiscVisuals.VirtualProtect(from, 14, prot, out prot);
		}

		private static void InitHooks()
		{
			if (MiscVisuals._hooksInit)
			{
				return;
			}
			MiscVisuals._hooksInit = true;
			Assembly assembly = typeof(Provider).Assembly;
			try
			{
				Type type = assembly.GetType("SDG.Unturned.PlayerLifeUI");
				if (type != null)
				{
					MiscVisuals._compassOrigMethod = type.GetMethod("hasCompassInInventory", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
					if (MiscVisuals._compassOrigMethod != null)
					{
						RuntimeHelpers.PrepareMethod(MiscVisuals._compassOrigMethod.MethodHandle);
						MiscVisuals._compassOrigPtr = MiscVisuals._compassOrigMethod.MethodHandle.GetFunctionPointer();
						MethodInfo method = typeof(MiscVisuals).GetMethod("HookCompass", BindingFlags.Static | BindingFlags.NonPublic);
						RuntimeHelpers.PrepareMethod(method.MethodHandle);
						MiscVisuals._compassHookPtr = method.MethodHandle.GetFunctionPointer();
						Marshal.Copy(MiscVisuals._compassOrigPtr, MiscVisuals._compassSaved, 0, 14);
						MiscVisuals.WriteJmp(MiscVisuals._compassOrigPtr, MiscVisuals._compassHookPtr);
						MiscVisuals._compassHooked = true;
						Runtime.Trace("misc: compass hook OK");
					}
				}
			}
			catch (Exception ex)
			{
				Runtime.Trace("misc: compass err: " + ex.Message);
			}
			try
			{
				Type type2 = assembly.GetType("SDG.Unturned.PlayerDashboardInformationUI");
				if (type2 != null)
				{
					MiscVisuals._mapOrigMethod = type2.GetMethod("searchForMapsInInventory", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
					if (MiscVisuals._mapOrigMethod != null)
					{
						RuntimeHelpers.PrepareMethod(MiscVisuals._mapOrigMethod.MethodHandle);
						MiscVisuals._mapOrigPtr = MiscVisuals._mapOrigMethod.MethodHandle.GetFunctionPointer();
						MethodInfo method2 = typeof(MiscVisuals).GetMethod("HookSearchMaps", BindingFlags.Static | BindingFlags.NonPublic);
						RuntimeHelpers.PrepareMethod(method2.MethodHandle);
						MiscVisuals._mapHookPtr = method2.MethodHandle.GetFunctionPointer();
						Marshal.Copy(MiscVisuals._mapOrigPtr, MiscVisuals._mapSaved, 0, 14);
						MiscVisuals.WriteJmp(MiscVisuals._mapOrigPtr, MiscVisuals._mapHookPtr);
						MiscVisuals._mapHooked = true;
						Runtime.Trace("misc: map hook OK");
					}
				}
			}
			catch (Exception ex2)
			{
				Runtime.Trace("misc: map err: " + ex2.Message);
			}
			Assembly assembly2 = typeof(Provider).Assembly;
			MiscVisuals.InstallHook(typeof(PlayerUI), "stun", "HookStun", MiscVisuals._stunSaved, ref MiscVisuals._stunOrigPtr, ref MiscVisuals._stunOrig, "flash");
			MiscVisuals.InstallHook(typeof(PlayerLife), "askView", "HookView", MiscVisuals._viewSaved, ref MiscVisuals._viewOrigPtr, ref MiscVisuals._viewOrig, "halluc");
			Type type3 = assembly2.GetType("SDG.Unturned.PlayerLifeUI");
			if (type3 != null)
			{
				MiscVisuals.InstallHook(type3, "onDamaged", "HookDamaged", MiscVisuals._dmgSaved, ref MiscVisuals._dmgOrigPtr, ref MiscVisuals._dmgOrig, "pain");
				MiscVisuals.InstallHook(type3, "updateGrayscale", "HookGrayscale", MiscVisuals._graySaved, ref MiscVisuals._grayOrigPtr, ref MiscVisuals._grayOrig, "gray");
			}
		}

		private static void InstallHook(Type targetType, string methodName, string hookName, byte[] saved, ref IntPtr origPtr, ref MethodInfo origMethod, string label)
		{
			try
			{
				origMethod = targetType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				if (origMethod == null)
				{
					Runtime.Trace("misc: " + label + " method not found");
				}
				else
				{
					RuntimeHelpers.PrepareMethod(origMethod.MethodHandle);
					origPtr = origMethod.MethodHandle.GetFunctionPointer();
					MethodInfo method = typeof(MiscVisuals).GetMethod(hookName, BindingFlags.Static | BindingFlags.NonPublic);
					RuntimeHelpers.PrepareMethod(method.MethodHandle);
					Marshal.Copy(origPtr, saved, 0, 14);
					MiscVisuals.WriteJmp(origPtr, method.MethodHandle.GetFunctionPointer());
					Runtime.Trace("misc: " + label + " hook OK");
				}
			}
			catch (Exception ex)
			{
				Runtime.Trace("misc: " + label + " err: " + ex.Message);
			}
		}

		private static void HookStun(Color color, float amount)
		{
			if (State.NoFlash)
			{
				return;
			}
			MiscVisuals.CallOrigVoid(MiscVisuals._stunSaved, ref MiscVisuals._stunOrigPtr, MiscVisuals._stunOrig, null, new object[]
			{
				color,
				amount
			});
		}

		private static void HookView(PlayerLife instance, byte amount)
		{
			if (State.NoHallucination)
			{
				return;
			}
			MiscVisuals.CallOrigVoid(MiscVisuals._viewSaved, ref MiscVisuals._viewOrigPtr, MiscVisuals._viewOrig, instance, new object[]
			{
				amount
			});
		}

		private static void HookDamaged(byte damage)
		{
			if (State.NoPain)
			{
				return;
			}
			MiscVisuals.CallOrigVoid(MiscVisuals._dmgSaved, ref MiscVisuals._dmgOrigPtr, MiscVisuals._dmgOrig, null, new object[]
			{
				damage
			});
		}

		private static void HookGrayscale()
		{
			if (State.NoGrayscale)
			{
				return;
			}
			MiscVisuals.CallOrigVoid(MiscVisuals._graySaved, ref MiscVisuals._grayOrigPtr, MiscVisuals._grayOrig, null, null);
		}

		private static void CallOrigVoid(byte[] saved, ref IntPtr origPtr, MethodInfo orig, object instance, object[] args)
		{
			uint prot;
			MiscVisuals.VirtualProtect(origPtr, 14, 64U, out prot);
			Marshal.Copy(saved, 0, origPtr, 14);
			MiscVisuals.VirtualProtect(origPtr, 14, prot, out prot);
			try
			{
				orig.Invoke(instance, args);
			}
			finally
			{
				MiscVisuals.WriteJmp(origPtr, typeof(MiscVisuals).GetMethod((orig == MiscVisuals._stunOrig) ? "HookStun" : ((orig == MiscVisuals._viewOrig) ? "HookView" : ((orig == MiscVisuals._dmgOrig) ? "HookDamaged" : "HookGrayscale")), BindingFlags.Static | BindingFlags.NonPublic).MethodHandle.GetFunctionPointer());
			}
		}

		private static bool HookCompass()
		{
			if (State.ForceCompass && !State.IsSpying)
			{
				return true;
			}
			uint prot;
			MiscVisuals.VirtualProtect(MiscVisuals._compassOrigPtr, 14, 64U, out prot);
			Marshal.Copy(MiscVisuals._compassSaved, 0, MiscVisuals._compassOrigPtr, 14);
			MiscVisuals.VirtualProtect(MiscVisuals._compassOrigPtr, 14, prot, out prot);
			bool result;
			try
			{
				result = (bool)MiscVisuals._compassOrigMethod.Invoke(null, null);
			}
			finally
			{
				MiscVisuals.WriteJmp(MiscVisuals._compassOrigPtr, MiscVisuals._compassHookPtr);
			}
			return result;
		}

		private static void HookSearchMaps(ref bool enableChart, ref bool enableMap)
		{
			if (State.ForceMap && !State.IsSpying)
			{
				enableChart = true;
				enableMap = true;
				return;
			}
			uint prot;
			MiscVisuals.VirtualProtect(MiscVisuals._mapOrigPtr, 14, 64U, out prot);
			Marshal.Copy(MiscVisuals._mapSaved, 0, MiscVisuals._mapOrigPtr, 14);
			MiscVisuals.VirtualProtect(MiscVisuals._mapOrigPtr, 14, prot, out prot);
			object[] array = new object[]
			{
				enableChart,
				enableMap
			};
			try
			{
				MiscVisuals._mapOrigMethod.Invoke(null, array);
				enableChart = (bool)array[0];
				enableMap = (bool)array[1];
			}
			finally
			{
				MiscVisuals.WriteJmp(MiscVisuals._mapOrigPtr, MiscVisuals._mapHookPtr);
			}
		}

		public static void Update()
		{
			MiscVisuals.InitHooks();
			bool isSpying = State.IsSpying;
			Player localPlayer = Player.LocalPlayer;
			if (!localPlayer)
			{
				return;
			}
			try
			{
				if (State.NoFog && !isSpying)
				{
					if (!MiscVisuals._fogOverride)
					{
						MiscVisuals._origFogState = RenderSettings.fog;
						MiscVisuals._origFogDensity = RenderSettings.fogDensity;
						MiscVisuals._fogOverride = true;
					}
					RenderSettings.fog = false;
					RenderSettings.fogDensity = 0f;
					Shader.SetGlobalFloat("_AtmosphericFog", 0f);
					try
					{
						UnturnedPostProcess instance = UnturnedPostProcess.instance;
						if (instance != null)
						{
							if (MiscVisuals._ppBaseLayerField == null)
							{
								MiscVisuals._ppBaseLayerField = typeof(UnturnedPostProcess).GetField("basePostProcessLayer", BindingFlags.Instance | BindingFlags.NonPublic);
							}
							if (MiscVisuals._ppBaseLayerField != null)
							{
								object value = MiscVisuals._ppBaseLayerField.GetValue(instance);
								if (value != null)
								{
									PropertyInfo property = value.GetType().GetProperty("fog");
									if (property != null)
									{
										object value2 = property.GetValue(value);
										if (value2 != null)
										{
											PropertyInfo property2 = value2.GetType().GetProperty("enabled");
											if (property2 != null)
											{
												property2.SetValue(value2, false);
											}
										}
									}
								}
							}
						}
						goto IL_13D;
					}
					catch
					{
						goto IL_13D;
					}
				}
				if (MiscVisuals._fogOverride)
				{
					RenderSettings.fog = MiscVisuals._origFogState;
					RenderSettings.fogDensity = MiscVisuals._origFogDensity;
					MiscVisuals._fogOverride = false;
				}
				IL_13D:;
			}
			catch
			{
			}
			try
			{
				if (State.UnlockPerspective && !isSpying)
				{
					if (!MiscVisuals._cameraModeOverridden)
					{
						MiscVisuals._savedCameraMode = Provider.cameraMode;
						MiscVisuals._cameraModeOverridden = true;
					}
					Provider.cameraMode = (ECameraMode)2;
				}
				else if (MiscVisuals._cameraModeOverridden)
				{
					Provider.cameraMode = MiscVisuals._savedCameraMode;
					MiscVisuals._cameraModeOverridden = false;
					if (localPlayer.look != null && localPlayer.look.perspective != null)
					{
						if (!MiscVisuals._perspCached)
						{
							MiscVisuals._setPerspective = typeof(PlayerLook).GetMethod("setActivePerspective", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
							MiscVisuals._perspCached = true;
						}
						if (MiscVisuals._setPerspective != null)
						{
							MiscVisuals._setPerspective.Invoke(localPlayer.look, new object[]
							{
								(EPlayerPerspective)0
							});
						}
					}
				}
			}
			catch
			{
			}
			try
			{
				int num = isSpying ? 0 : State.NightVision;
				if (num != MiscVisuals._prevNV)
				{
					MiscVisuals._prevNV = num;
					if (num == 0)
					{
						LevelLighting.vision = (ELightingVision)0;
					}
					else if (num == 1)
					{
						LevelLighting.vision = (ELightingVision)1;
						LevelLighting.nightvisionColor = new Color(0.2f, 0.8f, 0.2f);
						LevelLighting.nightvisionFogIntensity = 0.2f;
					}
					else if (num == 2)
					{
						LevelLighting.vision = (ELightingVision)2;
						LevelLighting.nightvisionColor = new Color(0.6f, 0.6f, 0.6f);
						LevelLighting.nightvisionFogIntensity = 0.2f;
					}
					else
					{
						LevelLighting.vision = (ELightingVision)1;
						LevelLighting.nightvisionColor = new Color(1f, 1f, 1f);
						LevelLighting.nightvisionFogIntensity = 0f;
					}
					LevelLighting.updateLighting();
					LevelLighting.ForceRefreshForLatestViewer();
				}
			}
			catch
			{
			}
			try
			{
				if (!MiscVisuals._timeHooked)
				{
					MiscVisuals._timeField = typeof(LevelLighting).GetField("_time", BindingFlags.Static | BindingFlags.NonPublic);
					MiscVisuals._timeOrig = typeof(LevelLighting).GetMethod("updateLighting", BindingFlags.Static | BindingFlags.Public);
					if (MiscVisuals._timeField != null && MiscVisuals._timeOrig != null)
					{
						RuntimeHelpers.PrepareMethod(MiscVisuals._timeOrig.MethodHandle);
						MiscVisuals._timeOrigPtr = MiscVisuals._timeOrig.MethodHandle.GetFunctionPointer();
						MethodInfo method = typeof(MiscVisuals).GetMethod("HookUpdateLighting", BindingFlags.Static | BindingFlags.NonPublic);
						RuntimeHelpers.PrepareMethod(method.MethodHandle);
						Marshal.Copy(MiscVisuals._timeOrigPtr, MiscVisuals._timeSaved, 0, 14);
						MiscVisuals.WriteJmp(MiscVisuals._timeOrigPtr, method.MethodHandle.GetFunctionPointer());
						MiscVisuals._timeHooked = true;
						Runtime.Trace("misc: custom time hook OK");
					}
				}
			}
			catch
			{
			}
			try
			{
				if (State.NoGrayscale && !isSpying)
				{
					if (MiscVisuals._grayEffectType == null)
					{
						foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
						{
							try
							{
								Type type = assembly.GetType("GrayscaleEffect");
								if (type != null)
								{
									MiscVisuals._grayEffectType = type;
									MiscVisuals._blendField = type.GetField("blend", BindingFlags.Instance | BindingFlags.Public);
									Runtime.Trace("grayscale: found in " + assembly.GetName().Name + " blend=" + (MiscVisuals._blendField != null).ToString());
									break;
								}
							}
							catch
							{
							}
						}
					}
					if (MiscVisuals._grayEffectType != null)
					{
						try
						{
							Transform viewmodelCameraTransform = localPlayer.animator.viewmodelCameraTransform;
							MiscVisuals.KillGray((viewmodelCameraTransform != null) ? viewmodelCameraTransform.GetComponent(MiscVisuals._grayEffectType) : null);
						}
						catch
						{
						}
						try
						{
							Camera instance2 = MainCamera.instance;
							MiscVisuals.KillGray((instance2 != null) ? instance2.GetComponent(MiscVisuals._grayEffectType) : null);
						}
						catch
						{
						}
						try
						{
							Camera characterCamera = localPlayer.look.characterCamera;
							MiscVisuals.KillGray((characterCamera != null) ? characterCamera.GetComponent(MiscVisuals._grayEffectType) : null);
						}
						catch
						{
						}
					}
				}
			}
			catch
			{
			}
			if (State.CustomFov && !isSpying)
			{
				if (!MiscVisuals._fovWasCustom)
				{
					MiscVisuals._origFov = OptionsSettings.fov;
				}
				if (MiscVisuals._cachedFovField == null)
				{
					MiscVisuals._cachedFovField = typeof(OptionsSettings).GetField("_cachedVerticalFOV", BindingFlags.Static | BindingFlags.NonPublic);
				}
				if (MiscVisuals._cachedFovField != null)
				{
					MiscVisuals._cachedFovField.SetValue(null, State.CustomFovDeg);
				}
				MiscVisuals._fovWasCustom = true;
			}
			else if (MiscVisuals._fovWasCustom)
			{
				OptionsSettings.fov = MiscVisuals._origFov;
				MiscVisuals._fovWasCustom = false;
			}
			if (!isSpying)
			{
				try
				{
					if (MiscVisuals._skyboxMat == null && MiscVisuals._skyboxField == null)
					{
						MiscVisuals._skyboxField = typeof(LevelLighting).GetField("skybox", BindingFlags.Static | BindingFlags.NonPublic);
					}
					if (MiscVisuals._skyboxMat == null && MiscVisuals._skyboxField != null)
					{
						MiscVisuals._skyboxMat = (MiscVisuals._skyboxField.GetValue(null) as Material);
					}
					if (MiscVisuals._skyboxMat != null)
					{
						if (State.OverrideSky)
						{
							MiscVisuals._skyboxMat.SetColor("_SkyColor", State.SkyColor);
						}
						if (State.OverrideSun)
						{
							MiscVisuals._skyboxMat.SetColor("_SunColor", State.SunColor);
						}
						if (State.OverrideCloud)
						{
							MiscVisuals._skyboxMat.SetColor("_CloudColor", State.CloudColor);
						}
						if (State.OverrideCloudRim)
						{
							MiscVisuals._skyboxMat.SetColor("_CloudRimColor", State.CloudRimColor);
						}
					}
				}
				catch
				{
				}
			}
		}

		private static void KillGray(Component c)
		{
			if (c == null)
			{
				return;
			}
			if (MiscVisuals._blendField != null)
			{
				MiscVisuals._blendField.SetValue(c, 0f);
			}
			Behaviour behaviour = c as Behaviour;
			if (behaviour != null)
			{
				behaviour.enabled = false;
			}
		}

		private static void HookUpdateLighting()
		{
			float num = 0f;
			bool flag = false;
			if (State.CustomTime && !State.IsSpying && MiscVisuals._timeField != null)
			{
				num = (float)MiscVisuals._timeField.GetValue(null);
				MiscVisuals._timeField.SetValue(null, State.CustomTimeValue);
				flag = true;
			}
			uint prot;
			MiscVisuals.VirtualProtect(MiscVisuals._timeOrigPtr, 14, 64U, out prot);
			Marshal.Copy(MiscVisuals._timeSaved, 0, MiscVisuals._timeOrigPtr, 14);
			MiscVisuals.VirtualProtect(MiscVisuals._timeOrigPtr, 14, prot, out prot);
			try
			{
				MiscVisuals._timeOrig.Invoke(null, null);
			}
			finally
			{
				MiscVisuals.WriteJmp(MiscVisuals._timeOrigPtr, typeof(MiscVisuals).GetMethod("HookUpdateLighting", BindingFlags.Static | BindingFlags.NonPublic).MethodHandle.GetFunctionPointer());
			}
			if (flag)
			{
				MiscVisuals._timeField.SetValue(null, num);
			}
			if (State.NoFog && !State.IsSpying)
			{
				RenderSettings.fog = false;
				RenderSettings.fogDensity = 0f;
				Shader.SetGlobalFloat("_AtmosphericFog", 0f);
			}
			if (MiscVisuals._skyboxMat == null)
			{
				MiscVisuals._skyboxField = typeof(LevelLighting).GetField("skybox", BindingFlags.Static | BindingFlags.NonPublic);
				if (MiscVisuals._skyboxField != null)
				{
					MiscVisuals._skyboxMat = (MiscVisuals._skyboxField.GetValue(null) as Material);
				}
			}
			if (MiscVisuals._skyboxMat != null)
			{
				if (!MiscVisuals._skyColorsSaved)
				{
					MiscVisuals._origSky = MiscVisuals._skyboxMat.GetColor("_SkyColor");
					MiscVisuals._origSun = MiscVisuals._skyboxMat.GetColor("_SunColor");
					MiscVisuals._origCloud = MiscVisuals._skyboxMat.GetColor("_CloudColor");
					MiscVisuals._origCloudRim = MiscVisuals._skyboxMat.GetColor("_CloudRimColor");
					MiscVisuals._skyColorsSaved = true;
				}
				if (!State.IsSpying)
				{
					if (State.OverrideSky)
					{
						MiscVisuals._skyboxMat.SetColor("_SkyColor", State.SkyColor);
					}
					if (State.OverrideSun)
					{
						MiscVisuals._skyboxMat.SetColor("_SunColor", State.SunColor);
					}
					if (State.OverrideCloud)
					{
						MiscVisuals._skyboxMat.SetColor("_CloudColor", State.CloudColor);
					}
					if (State.OverrideCloudRim)
					{
						MiscVisuals._skyboxMat.SetColor("_CloudRimColor", State.CloudRimColor);
						return;
					}
				}
				else
				{
					MiscVisuals._skyboxMat.SetColor("_SkyColor", MiscVisuals._origSky);
					MiscVisuals._skyboxMat.SetColor("_SunColor", MiscVisuals._origSun);
					MiscVisuals._skyboxMat.SetColor("_CloudColor", MiscVisuals._origCloud);
					MiscVisuals._skyboxMat.SetColor("_CloudRimColor", MiscVisuals._origCloudRim);
				}
			}
		}


		private static byte[] _stunSaved = new byte[14];

		private static IntPtr _stunOrigPtr;

		private static MethodInfo _stunOrig;

		private static byte[] _viewSaved = new byte[14];

		private static IntPtr _viewOrigPtr;

		private static MethodInfo _viewOrig;

		private static byte[] _dmgSaved = new byte[14];

		private static IntPtr _dmgOrigPtr;

		private static MethodInfo _dmgOrig;

		private static byte[] _graySaved = new byte[14];

		private static IntPtr _grayOrigPtr;

		private static MethodInfo _grayOrig;

		private static bool _fovWasCustom;

		private static float _origFov;

		private static FieldInfo _cachedFovField;

		private static Type _grayEffectType;

		private static FieldInfo _blendField;

		private static bool _grayTypeCached;

		private static bool _fogOverride;

		private static FieldInfo _ppBaseLayerField;

		private static bool _origFogState;

		private static float _origFogDensity;

		private static int _prevNV;

		private static FieldInfo _timeField;

		private static byte[] _timeSaved = new byte[14];

		private static IntPtr _timeOrigPtr;

		private static MethodInfo _timeOrig;

		private static bool _timeHooked;

		private static FieldInfo _skyboxField;

		private static Material _skyboxMat;

		private static ECameraMode _savedCameraMode;

		private static bool _cameraModeOverridden;

		private static MethodInfo _setPerspective;

		private static bool _perspCached;

		private static bool _hooksInit;

		private static byte[] _compassSaved = new byte[14];

		private static IntPtr _compassOrigPtr;

		private static IntPtr _compassHookPtr;

		private static MethodInfo _compassOrigMethod;

		private static bool _compassHooked;

		private static byte[] _mapSaved = new byte[14];

		private static IntPtr _mapOrigPtr;

		private static IntPtr _mapHookPtr;

		private static MethodInfo _mapOrigMethod;

		private static bool _mapHooked;

		private static GUIStyle _wmStyle;

		private static GUIStyle _wmShadow;

		private static Color _origSky;

		private static Color _origSun;

		private static Color _origCloud;

		private static Color _origCloudRim;

		private static bool _skyColorsSaved;

		private const uint PAGE_EXECUTE_READWRITE = 64U;

		private static GUIStyle _ibStyle;

		private static GUIStyle _ibShadow;

		private static float _fps;

		private static float _fpsTimer;

		private static int _fpsCount;
	}
}
