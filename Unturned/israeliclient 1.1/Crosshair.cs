using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SDG.Unturned;
using UnityEngine;

namespace gatyware
{
	public static class Crosshair
	{
		public static string[] Names
		{
			get
			{
				return Crosshair._names;
			}
		}

		[DllImport("kernel32.dll")]
		private static extern bool VirtualProtect(IntPtr a, int s, uint p, out uint o);

		private static void Jmp(IntPtr from, IntPtr to)
		{
			uint p;
			Crosshair.VirtualProtect(from, 14, 64U, out p);
			byte[] array = new byte[14];
			array[0] = byte.MaxValue;
			array[1] = 37;
			BitConverter.GetBytes(to.ToInt64()).CopyTo(array, 6);
			Marshal.Copy(array, 0, from, 14);
			Crosshair.VirtualProtect(from, 14, p, out p);
		}

		private static void DoHook(Type t, string method, string hook, byte[] sv, ref IntPtr ptr, ref MethodInfo orig)
		{
			orig = t.GetMethod(method, BindingFlags.Instance | BindingFlags.Public);
			if (orig == null)
			{
				return;
			}
			RuntimeHelpers.PrepareMethod(orig.MethodHandle);
			ptr = orig.MethodHandle.GetFunctionPointer();
			MethodInfo method2 = typeof(Crosshair).GetMethod(hook, BindingFlags.Static | BindingFlags.NonPublic);
			RuntimeHelpers.PrepareMethod(method2.MethodHandle);
			Marshal.Copy(ptr, sv, 0, 14);
			Crosshair.Jmp(ptr, method2.MethodHandle.GetFunctionPointer());
		}

		private static void Install()
		{
			if (Crosshair._hooked)
			{
				return;
			}
			Crosshair._hooked = true;
			try
			{
				Type type = typeof(PlayerUI).Assembly.GetType("SDG.Unturned.Crosshair");
				if (!(type == null))
				{
					Crosshair.DoHook(type, "SetDirectionalArrowsVisible", "HkArr", Crosshair._arrSv, ref Crosshair._arrPtr, ref Crosshair._arrOrig);
					Crosshair.DoHook(type, "SetGameWantsCenterDotVisible", "HkDot", Crosshair._dotSv, ref Crosshair._dotPtr, ref Crosshair._dotOrig);
					Crosshair.DoHook(type, "SetPluginAllowsCenterDotVisible", "HkPlg", Crosshair._plgSv, ref Crosshair._plgPtr, ref Crosshair._plgOrig);
					Crosshair.DoHook(type, "OnUpdate", "HkUpd", Crosshair._updSv, ref Crosshair._updPtr, ref Crosshair._updOrig);
					BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.NonPublic;
					Crosshair._fIsGunVis = type.GetField("isGunCrosshairVisible", bindingAttr);
					Crosshair._fGameWantsDot = type.GetField("gameWantsCenterDotVisible", bindingAttr);
					Crosshair._fPluginAllowsDot = type.GetField("pluginAllowsCenterDotVisible", bindingAttr);
					Crosshair._fCenter = type.GetField("centerDotImage", bindingAttr);
					Crosshair._fLeft = type.GetField("crosshairLeftImage", bindingAttr);
					Crosshair._fRight = type.GetField("crosshairRightImage", bindingAttr);
					Crosshair._fUp = type.GetField("crosshairUpImage", bindingAttr);
					Crosshair._fDown = type.GetField("crosshairDownImage", bindingAttr);
					Runtime.Trace("crosshair: 4 hooks OK");
				}
			}
			catch (Exception ex)
			{
				Runtime.Trace("crosshair err: " + ex.Message);
			}
		}

		private static bool Blocking()
		{
			return State.CrosshairOn && !State.IsSpying;
		}

		private static void HkArr(object self, bool isVisible)
		{
			if (Crosshair._fIsGunVis != null)
			{
				Crosshair._fIsGunVis.SetValue(self, isVisible);
			}
			if (Crosshair.Blocking())
			{
				Crosshair.SetVis(Crosshair._fLeft, self, false);
				Crosshair.SetVis(Crosshair._fRight, self, false);
				Crosshair.SetVis(Crosshair._fUp, self, false);
				Crosshair.SetVis(Crosshair._fDown, self, false);
				return;
			}
			Crosshair.SetVis(Crosshair._fLeft, self, isVisible);
			Crosshair.SetVis(Crosshair._fRight, self, isVisible);
			Crosshair.SetVis(Crosshair._fUp, self, isVisible);
			Crosshair.SetVis(Crosshair._fDown, self, isVisible);
		}

		private static void HkDot(object self, bool isVisible)
		{
			if (Crosshair._fGameWantsDot != null)
			{
				Crosshair._fGameWantsDot.SetValue(self, isVisible);
			}
			if (Crosshair.Blocking())
			{
				Crosshair.SetVis(Crosshair._fCenter, self, false);
				return;
			}
			bool flag = !(Crosshair._fPluginAllowsDot != null) || (bool)Crosshair._fPluginAllowsDot.GetValue(self);
			Crosshair.SetVis(Crosshair._fCenter, self, isVisible && flag);
		}

		private static void HkPlg(object self, bool isVisible)
		{
			if (Crosshair._fPluginAllowsDot != null)
			{
				Crosshair._fPluginAllowsDot.SetValue(self, isVisible);
			}
			if (Crosshair.Blocking())
			{
				Crosshair.SetVis(Crosshair._fCenter, self, false);
				return;
			}
			bool flag = !(Crosshair._fGameWantsDot != null) || (bool)Crosshair._fGameWantsDot.GetValue(self);
			Crosshair.SetVis(Crosshair._fCenter, self, flag && isVisible);
		}

		private static void HkUpd(object self)
		{
			if (Crosshair.Blocking())
			{
				if (!Crosshair._wasHidden)
				{
					if (Crosshair._fIsGunVis != null && (bool)Crosshair._fIsGunVis.GetValue(self))
					{
						Crosshair.SetVis(Crosshair._fLeft, self, false);
						Crosshair.SetVis(Crosshair._fRight, self, false);
						Crosshair.SetVis(Crosshair._fUp, self, false);
						Crosshair.SetVis(Crosshair._fDown, self, false);
					}
					Crosshair.SetVis(Crosshair._fCenter, self, false);
					Crosshair._wasHidden = true;
				}
				return;
			}
			if (Crosshair._wasHidden)
			{
				if (Crosshair._fIsGunVis != null && (bool)Crosshair._fIsGunVis.GetValue(self))
				{
					Crosshair.SetVis(Crosshair._fLeft, self, true);
					Crosshair.SetVis(Crosshair._fRight, self, true);
					Crosshair.SetVis(Crosshair._fUp, self, true);
					Crosshair.SetVis(Crosshair._fDown, self, true);
				}
				bool vis = Crosshair._fGameWantsDot != null && (bool)Crosshair._fGameWantsDot.GetValue(self) && (!(Crosshair._fPluginAllowsDot != null) || (bool)Crosshair._fPluginAllowsDot.GetValue(self));
				Crosshair.SetVis(Crosshair._fCenter, self, vis);
				Crosshair._wasHidden = false;
			}
			Crosshair.Unhook(Crosshair._updSv, Crosshair._updPtr);
			try
			{
				Crosshair._updOrig.Invoke(self, null);
			}
			finally
			{
				Crosshair.Rehook(Crosshair._updPtr, "HkUpd");
			}
		}

		private static void SetVis(FieldInfo fi, object inst, bool vis)
		{
			if (fi == null)
			{
				return;
			}
			object value = fi.GetValue(inst);
			if (value == null)
			{
				return;
			}
			PropertyInfo property = value.GetType().GetProperty("IsVisible", BindingFlags.Instance | BindingFlags.Public);
			if (property != null)
			{
				property.SetValue(value, vis, null);
			}
		}

		private static void Unhook(byte[] sv, IntPtr ptr)
		{
			uint p;
			Crosshair.VirtualProtect(ptr, 14, 64U, out p);
			Marshal.Copy(sv, 0, ptr, 14);
			Crosshair.VirtualProtect(ptr, 14, p, out p);
		}

		private static void Rehook(IntPtr ptr, string name)
		{
			Crosshair.Jmp(ptr, typeof(Crosshair).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic).MethodHandle.GetFunctionPointer());
		}

        public static void Render()
        {
            Crosshair.Install();
            if (State.IsSpying || !State.CrosshairOn || Cursor.lockState != (CursorLockMode)1)
            {
                return;
            }
            float cx = (float)Screen.width * 0.5f;
            float cy = (float)Screen.height * 0.5f;
            float crosshairSize = State.CrosshairSize;
            float crosshairThick = State.CrosshairThick;
            Color crosshairColor = State.CrosshairColor;
            float a = State.CrosshairSpin ? (Time.unscaledTime * State.CrosshairSpinSpd) : 0f;
            switch (State.CrosshairType)
            {
                case 0:
                    Crosshair.DrawCross(cx, cy, crosshairSize, crosshairThick, crosshairColor, a);
                    return;
                case 1:
                    Crosshair.DrawDot(cx, cy, crosshairSize * 0.3f, crosshairColor);
                    return;
                case 2:
                    Crosshair.DrawCircle(cx, cy, crosshairSize, crosshairThick, crosshairColor, a);
                    return;
                case 3:
                    Crosshair.DrawDiamond(cx, cy, crosshairSize, crosshairThick, crosshairColor, a);
                    return;
                case 4:
                    Crosshair.DrawStar(cx, cy, crosshairSize, crosshairThick, crosshairColor, a);
                    return;
                case 5:  // New case for Nazi
                    Crosshair.DrawNazi(cx, cy, crosshairSize, crosshairThick, crosshairColor, a);
                    return;
                default:
                    return;
            }
        }

        // Render the actual swastika (卍 Manji) shape: 4 arms going OUT from center, each
        // bending 90° in the same rotational direction. The bend uses (dirY, -dirX) which,
        // in Unity's screen coords (+Y down), produces the standard Manji rotation —
        // i=0 (right arm) bends up, i=1 (down arm) bends right, i=2 (left arm) bends down,
        // i=3 (up arm) bends left — all four tips curving the same way around the center.
        private static void DrawNazi(float cx, float cy, float sz, float th, Color c, float a)
        {
            float deg = a * 0.017453292f;
            float inner = sz * 0.18f;
            float outer = sz * 0.55f;
            float bend = sz * 0.32f;
            float armTh = th * 0.95f;

            for (int i = 0; i < 4; i++)
            {
                float armAngle = deg + (float)i * 1.5707963f;
                float dirX = Mathf.Cos(armAngle);
                float dirY = Mathf.Sin(armAngle);

                // Perpendicular vector (rotated 90° clockwise)
                float perpX = dirY;
                float perpY = -dirX;

                // Main arm from inner to outer
                Vector2 p0 = new Vector2(cx + dirX * inner, cy + dirY * inner);
                Vector2 p1 = new Vector2(cx + dirX * outer, cy + dirY * outer);

               
               
                Vector2 p2 = new Vector2(p1.x - perpX * bend, p1.y - perpY * bend);

                Draw.Line(p0, p1, c, armTh);
                Draw.Line(p1, p2, c, armTh);
            }

            if (sz > 10f)
            {
                float dotSize = sz * 0.04f;
                Draw.Line(new Vector2(cx - dotSize, cy), new Vector2(cx + dotSize, cy), c, th * 0.5f);
                Draw.Line(new Vector2(cx, cy - dotSize), new Vector2(cx, cy + dotSize), c, th * 0.5f);
            }
        }

        private static void DrawCross(float cx, float cy, float sz, float th, Color c, float a)
		{
			float num = sz * 0.25f;
			float num2 = Mathf.Cos(a * 0.017453292f);
			float num3 = Mathf.Sin(a * 0.017453292f);
			for (int i = 0; i < 4; i++)
			{
				float num4 = (float)((i < 2) ? 0 : ((i == 2) ? -1 : 1));
				float num5 = (float)((i == 0) ? -1 : ((i == 1) ? 1 : 0));
				float num6 = num4 * num;
				float num7 = num5 * num;
				float num8 = num4 * sz;
				float num9 = num5 * sz;
				Draw.Line(new Vector2(cx + num6 * num2 - num7 * num3, cy + num6 * num3 + num7 * num2), new Vector2(cx + num8 * num2 - num9 * num3, cy + num8 * num3 + num9 * num2), c, th);
			}
		}

		private static void DrawDot(float cx, float cy, float r, Color c)
		{
			if (r < 1f)
			{
				r = 1f;
			}
			Skin.Tint(new Rect(cx - r, cy - r, r * 2f, r * 2f), c);
		}

		private static void DrawCircle(float cx, float cy, float sz, float th, Color c, float a)
		{
			float num = a * 0.017453292f;
			int num2 = 32;
			float num3 = 360f / (float)num2;
			for (int i = 0; i < num2; i++)
			{
				float num4 = (float)i * num3 * 0.017453292f + num;
				float num5 = (float)(i + 1) * num3 * 0.017453292f + num;
				Draw.Line(new Vector2(cx + Mathf.Cos(num4) * sz, cy + Mathf.Sin(num4) * sz), new Vector2(cx + Mathf.Cos(num5) * sz, cy + Mathf.Sin(num5) * sz), c, th);
			}
		}

		private static void DrawDiamond(float cx, float cy, float sz, float th, Color c, float a)
		{
			float num = a * 0.017453292f;
			float[] array = new float[4];
			float[] array2 = new float[4];
			for (int i = 0; i < 4; i++)
			{
				float num2 = (float)i * 90f * 0.017453292f + num;
				array[i] = cx + Mathf.Cos(num2) * sz;
				array2[i] = cy + Mathf.Sin(num2) * sz;
			}
			for (int j = 0; j < 4; j++)
			{
				int num3 = (j + 1) % 4;
				Draw.Line(new Vector2(array[j], array2[j]), new Vector2(array[num3], array2[num3]), c, th);
			}
		}

		private static void DrawStar(float cx, float cy, float sz, float th, Color c, float a)
		{
			float num = a * 0.017453292f;
			Crosshair.Tri(cx, cy, sz, th, c, num - 1.5707964f);
			Crosshair.Tri(cx, cy, sz, th, c, num + 1.5707964f);
		}

		private static void Tri(float cx, float cy, float r, float th, Color c, float o)
		{
			float[] array = new float[3];
			float[] array2 = new float[3];
			for (int i = 0; i < 3; i++)
			{
				float num = o + (float)i * 2.0943952f;
				array[i] = cx + Mathf.Cos(num) * r;
				array2[i] = cy + Mathf.Sin(num) * r;
			}
			for (int j = 0; j < 3; j++)
			{
				int num2 = (j + 1) % 3;
				Draw.Line(new Vector2(array[j], array2[j]), new Vector2(array[num2], array2[num2]), c, th);
			}
		}

		private static readonly string[] _names = new string[]
		{
			"Cross",
			"Dot",
			"Circle",
			"Diamond",
			"Star of David",
			"Nazi"
		};

		private static byte[] _arrSv = new byte[14];

		private static byte[] _dotSv = new byte[14];

		private static byte[] _plgSv = new byte[14];

		private static byte[] _updSv = new byte[14];

		private static IntPtr _arrPtr;

		private static IntPtr _dotPtr;

		private static IntPtr _plgPtr;

		private static IntPtr _updPtr;

		private static MethodInfo _arrOrig;

		private static MethodInfo _dotOrig;

		private static MethodInfo _plgOrig;

		private static MethodInfo _updOrig;

		private static FieldInfo _fIsGunVis;

		private static FieldInfo _fGameWantsDot;

		private static FieldInfo _fPluginAllowsDot;

		private static FieldInfo _fCenter;

		private static FieldInfo _fLeft;

		private static FieldInfo _fRight;

		private static FieldInfo _fUp;

		private static FieldInfo _fDown;

		private static bool _hooked;

		private static bool _wasHidden;
	}
}
