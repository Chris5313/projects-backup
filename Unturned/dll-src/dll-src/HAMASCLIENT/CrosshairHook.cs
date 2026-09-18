using System;
using System.Reflection;
using SDG.Unturned;
using UnityEngine;
public class CrosshairHook
{
	[HookMethodAttribute(typeof(Crosshair), "SetDirectionalArrowsVisible", BindingFlags.Instance | BindingFlags.Public, new Type[] { })]
	public static void HookedSetDirectionalArrowsVisible(Crosshair c, bool isVisible)
	{
		CrosshairHook.IsGunCrosshairVisibleField.Set(c, isVisible);
		bool flag = Settings.useCustomCrosshair && Settings.forceDisableDefaultCrosshair;
		bool flag2 = flag;
		if (flag2)
		{
			CrosshairHook.CrosshairLeftImageField.Get(c).IsVisible = false;
			CrosshairHook.CrosshairRightImageField.Get(c).IsVisible = false;
			CrosshairHook.CrosshairUpImageField.Get(c).IsVisible = false;
			CrosshairHook.CrosshairDownImageField.Get(c).IsVisible = false;
		}
		else
		{
			CrosshairHook.CrosshairLeftImageField.Get(c).IsVisible = isVisible;
			CrosshairHook.CrosshairRightImageField.Get(c).IsVisible = isVisible;
			CrosshairHook.CrosshairUpImageField.Get(c).IsVisible = isVisible;
			CrosshairHook.CrosshairDownImageField.Get(c).IsVisible = isVisible;
			CrosshairHook.IsInterpolatedSpreadValidField.Set(c, CrosshairHook.IsInterpolatedSpreadValidField.Get(c) && isVisible);
		}
	}
	[HookMethodAttribute(typeof(Crosshair), "SetGameWantsCenterDotVisible", BindingFlags.Instance | BindingFlags.Public, new Type[] { })]
	public static void HookedSetGameWantsCenterDotVisible(Crosshair c, bool isVisible)
	{
		CrosshairHook.GameWantsCenterDotVisibleField.Set(c, isVisible);
		bool flag = !Settings.forceDisableDefaultCrosshair || !Settings.useCustomCrosshair;
		bool flag2 = flag;
		if (flag2)
		{
			CrosshairHook.CenterDotImageField.Get(c).IsVisible = isVisible && CrosshairHook.PluginAllowsCenterDotVisibleField.Get(c);
		}
	}
	[HookMethodAttribute(typeof(Crosshair), "SetPluginAllowsCenterDotVisible", BindingFlags.Instance | BindingFlags.Public, new Type[] { })]
	public static void HookedSetPluginAllowsCenterDotVisible(Crosshair c, bool isVisible)
	{
		CrosshairHook.PluginAllowsCenterDotVisibleField.Set(c, isVisible);
		bool flag = !Settings.forceDisableDefaultCrosshair || !Settings.useCustomCrosshair;
		bool flag2 = flag;
		if (flag2)
		{
			CrosshairHook.CenterDotImageField.Get(c).IsVisible = CrosshairHook.GameWantsCenterDotVisibleField.Get(c) && isVisible;
		}
		else
		{
			CrosshairHook.CenterDotImageField.Get(c).IsVisible = false;
		}
	}
	[HookMethodAttribute(typeof(Crosshair), "OnUpdate", BindingFlags.Instance | BindingFlags.Public, new Type[] { })]
	public static void HookedOnUpdate(Crosshair c)
	{
		bool flag = Settings.useCustomCrosshair && Settings.forceDisableDefaultCrosshair;
		bool flag2 = flag;
		if (flag2)
		{
			bool flag3 = !CrosshairHook.CrosshairHiddenState;
			bool flag4 = flag3;
			if (flag4)
			{
				bool flag5 = CrosshairHook.IsGunCrosshairVisibleField.Get(c);
				bool flag6 = flag5;
				if (flag6)
				{
					CrosshairHook.CrosshairLeftImageField.Get(c).IsVisible = false;
					CrosshairHook.CrosshairRightImageField.Get(c).IsVisible = false;
					CrosshairHook.CrosshairUpImageField.Get(c).IsVisible = false;
					CrosshairHook.CrosshairDownImageField.Get(c).IsVisible = false;
				}
				CrosshairHook.CenterDotImageField.Get(c).IsVisible = false;
				CrosshairHook.CrosshairHiddenState = true;
			}
		}
		else
		{
			bool flag7 = !Settings.forceDisableDefaultCrosshair && CrosshairHook.CrosshairHiddenState;
			bool flag8 = flag7;
			if (flag8)
			{
				bool flag9 = CrosshairHook.IsGunCrosshairVisibleField.Get(c);
				bool flag10 = flag9;
				if (flag10)
				{
					CrosshairHook.CrosshairLeftImageField.Get(c).IsVisible = true;
					CrosshairHook.CrosshairRightImageField.Get(c).IsVisible = true;
					CrosshairHook.CrosshairUpImageField.Get(c).IsVisible = true;
					CrosshairHook.CrosshairDownImageField.Get(c).IsVisible = true;
				}
				CrosshairHook.CenterDotImageField.Get(c).IsVisible = CrosshairHook.GameWantsCenterDotVisibleField.Get(c) && CrosshairHook.PluginAllowsCenterDotVisibleField.Get(c);
				CrosshairHook.CrosshairHiddenState = false;
			}
			bool flag11 = !CrosshairHook.IsGunCrosshairVisibleField.Get(c);
			bool flag12 = flag11;
			if (flag12)
			{
				CrosshairHook.IsInterpolatedSpreadValidField.Set(c, false);
			}
			else
			{
				UseableGun useableGun = Player.player.equipment.useable as UseableGun;
				bool flag13 = useableGun == null;
				bool flag14 = flag13;
				if (flag14)
				{
					CrosshairHook.IsInterpolatedSpreadValidField.Set(c, false);
				}
				else
				{
					Camera instance = MainCamera.instance;
					bool flag15 = instance == null;
					bool flag16 = flag15;
					if (flag16)
					{
						CrosshairHook.IsInterpolatedSpreadValidField.Set(c, false);
					}
					else
					{
						float fieldOfView = instance.fieldOfView;
						float num = 0.017453292f * fieldOfView * 0.5f;
						bool flag17 = num < 0.001f;
						bool flag18 = flag17;
						if (flag18)
						{
							CrosshairHook.IsInterpolatedSpreadValidField.Set(c, false);
						}
						else
						{
							bool flag19 = Player.player.look.perspective == EPlayerPerspective.FIRST;
							bool flag20 = flag19;
							Vector2 vector3;
							if (flag20)
							{
								Quaternion rotation = Player.player.look.aim.rotation;
								Quaternion quaternion = Quaternion.Euler(Player.player.animator.recoilViewmodelCameraRotation.currentPosition);
								Vector3 vector = rotation * quaternion * Vector3.forward;
								Vector2 vector2 = instance.WorldToViewportPoint(instance.transform.position + vector);
								vector3 = c.ViewportToNormalizedPosition(vector2);
								vector3.x += c.Parent.PositionScale_X;
								vector3.y += c.Parent.PositionScale_Y;
							}
							else
							{
								vector3 = new Vector2(0.5f, 0.5f);
							}
							float num2 = UseableGunHooks.GetCurrentSpread() * (ScreenshotManager.IsSpying ? 1f : MiscConfig.spreadMultiplier);
							CrosshairHook.InterpolatedSpreadField.instance = c;
							bool flag21 = CrosshairHook.IsInterpolatedSpreadValidField.Get(c) && !ScreenshotManager.IsSpying;
							bool flag22 = flag21;
							if (flag22)
							{
								CrosshairHook.InterpolatedSpreadField.value = Mathf.Lerp(CrosshairHook.InterpolatedSpreadField.Get(), num2, Time.deltaTime * 16f);
							}
							else
							{
								CrosshairHook.InterpolatedSpreadField.value = num2;
								CrosshairHook.IsInterpolatedSpreadValidField.Set(c, true);
							}
							float num3 = Mathf.Tan(num);
							float num4 = num3 * instance.aspect;
							float num5 = Mathf.Tan(CrosshairHook.InterpolatedSpreadField.Get());
							float num6 = num5 / num4 * 0.5f;
							float num7 = num5 / num3 * 0.5f;
							bool useStaticCrosshair = OptionsSettings.useStaticCrosshair;
							bool flag23 = useStaticCrosshair;
							if (flag23)
							{
								num6 = Mathf.Lerp(0.0025f, 0.05f, OptionsSettings.staticCrosshairSize);
								num7 = num6 * instance.aspect;
							}
							CrosshairHook.CrosshairLeftImageField.instance = c;
							CrosshairHook.CrosshairRightImageField.instance = c;
							CrosshairHook.CrosshairUpImageField.instance = c;
							CrosshairHook.CrosshairDownImageField.instance = c;
							CrosshairHook.CrosshairLeftImageField.value.PositionScale_X = vector3.x - num6;
							CrosshairHook.CrosshairLeftImageField.value.PositionScale_Y = vector3.y;
							CrosshairHook.CrosshairRightImageField.value.PositionScale_X = vector3.x + num6;
							CrosshairHook.CrosshairRightImageField.value.PositionScale_Y = vector3.y;
							CrosshairHook.CrosshairUpImageField.value.PositionScale_X = vector3.x;
							CrosshairHook.CrosshairUpImageField.value.PositionScale_Y = vector3.y - num7;
							CrosshairHook.CrosshairDownImageField.value.PositionScale_X = vector3.x;
							CrosshairHook.CrosshairDownImageField.value.PositionScale_Y = vector3.y + num7;
						}
					}
				}
			}
		}
	}
	public static ReflectedField<bool> GameWantsCenterDotVisibleField = new ReflectedField<bool>(typeof(Crosshair), "gameWantsCenterDotVisible", BindingFlags.Instance | BindingFlags.NonPublic);
	public static ReflectedField<bool> PluginAllowsCenterDotVisibleField = new ReflectedField<bool>(typeof(Crosshair), "pluginAllowsCenterDotVisible", BindingFlags.Instance | BindingFlags.NonPublic);
	public static ReflectedField<bool> IsGunCrosshairVisibleField = new ReflectedField<bool>(typeof(Crosshair), "isGunCrosshairVisible", BindingFlags.Instance | BindingFlags.NonPublic);
	public static ReflectedField<bool> IsInterpolatedSpreadValidField = new ReflectedField<bool>(typeof(Crosshair), "isInterpolatedSpreadValid", BindingFlags.Instance | BindingFlags.NonPublic);
	public static ReflectedField<float> InterpolatedSpreadField = new ReflectedField<float>(typeof(Crosshair), "interpolatedSpread", BindingFlags.Instance | BindingFlags.NonPublic);
	public static ReflectedField<ISleekImage> CenterDotImageField = new ReflectedField<ISleekImage>(typeof(Crosshair), "centerDotImage", BindingFlags.Instance | BindingFlags.NonPublic);
	public static ReflectedField<ISleekImage> CrosshairLeftImageField = new ReflectedField<ISleekImage>(typeof(Crosshair), "crosshairLeftImage", BindingFlags.Instance | BindingFlags.NonPublic);
	public static ReflectedField<ISleekImage> CrosshairRightImageField = new ReflectedField<ISleekImage>(typeof(Crosshair), "crosshairRightImage", BindingFlags.Instance | BindingFlags.NonPublic);
	public static ReflectedField<ISleekImage> CrosshairUpImageField = new ReflectedField<ISleekImage>(typeof(Crosshair), "crosshairUpImage", BindingFlags.Instance | BindingFlags.NonPublic);
	public static ReflectedField<ISleekImage> CrosshairDownImageField = new ReflectedField<ISleekImage>(typeof(Crosshair), "crosshairDownImage", BindingFlags.Instance | BindingFlags.NonPublic);
	public static bool CrosshairHiddenState = false;
}
