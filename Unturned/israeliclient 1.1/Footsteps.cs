using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SDG.Unturned;
using UnityEngine;

namespace gatyware
{
	public static class Footsteps
	{
		[DllImport("kernel32.dll")]
		private static extern bool VirtualProtect(IntPtr a, int s, uint p, out uint o);

		private static void Jmp(IntPtr from, IntPtr to)
		{
			uint p;
			Footsteps.VirtualProtect(from, 14, 64U, out p);
			byte[] array = new byte[14];
			array[0] = byte.MaxValue;
			array[1] = 37;
			BitConverter.GetBytes(to.ToInt64()).CopyTo(array, 6);
			Marshal.Copy(array, 0, from, 14);
			Footsteps.VirtualProtect(from, 14, p, out p);
		}

		private static void DoHook(Type t, string method, string hook, byte[] sv, ref IntPtr ptr, ref MethodInfo orig)
		{
			orig = t.GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (orig == null)
			{
				Runtime.Trace("footsteps: " + method + " not found");
				return;
			}
			RuntimeHelpers.PrepareMethod(orig.MethodHandle);
			ptr = orig.MethodHandle.GetFunctionPointer();
			MethodInfo method2 = typeof(Footsteps).GetMethod(hook, BindingFlags.Static | BindingFlags.NonPublic);
			RuntimeHelpers.PrepareMethod(method2.MethodHandle);
			Marshal.Copy(ptr, sv, 0, 14);
			Footsteps.Jmp(ptr, method2.MethodHandle.GetFunctionPointer());
		}

		private static void Unhook(byte[] sv, IntPtr ptr)
		{
			uint p;
			Footsteps.VirtualProtect(ptr, 14, 64U, out p);
			Marshal.Copy(sv, 0, ptr, 14);
			Footsteps.VirtualProtect(ptr, 14, p, out p);
		}

		private static void Rehook(IntPtr ptr, string name)
		{
			Footsteps.Jmp(ptr, typeof(Footsteps).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic).MethodHandle.GetFunctionPointer());
		}

		private static void Init()
		{
			if (Footsteps._inited)
			{
				return;
			}
			Footsteps._inited = true;
			Footsteps._circle = Footsteps.MakeCircle();
			Footsteps._star = Footsteps.MakeStar();
			Footsteps._block = new MaterialPropertyBlock();
			Shader shader = Shader.Find("Hidden/Internal-Colored");
			if (shader == null)
			{
				return;
			}
			Footsteps._mat = new Material(shader)
			{
				hideFlags = (HideFlags)61
			};
			Footsteps._mat.SetInt("_SrcBlend", 5);
			Footsteps._mat.SetInt("_DstBlend", 10);
			Footsteps._mat.SetInt("_Cull", 0);
			Footsteps._mat.SetInt("_ZWrite", 0);
			Footsteps._mat.SetInt("_ZTest", 8);
		}

		private static void InstallHooks()
		{
			if (Footsteps._hooked)
			{
				return;
			}
			Footsteps._hooked = true;
			try
			{
				Type type = typeof(PlayerUI).Assembly.GetType("SDG.Unturned.PlayerMovement");
				if (type == null)
				{
					Runtime.Trace("footsteps: PlayerMovement not found");
				}
				else
				{
					Footsteps.DoHook(type, "PlayFootstepAudioClip", "HkStep", Footsteps._stepSv, ref Footsteps._stepPtr, ref Footsteps._stepOrig);
					Footsteps.DoHook(type, "PlayLandAudioClip", "HkLand", Footsteps._landSv, ref Footsteps._landPtr, ref Footsteps._landOrig);
					Runtime.Trace("footsteps: hooks OK");
				}
			}
			catch (Exception ex)
			{
				Runtime.Trace("footsteps err: " + ex.Message);
			}
		}

		private static void HkStep(object self)
		{
			if (Footsteps._stepLog < 5)
			{
				Runtime.Trace("HkStep fired ft=" + State.Footsteps.ToString());
				Footsteps._stepLog++;
			}
			if (State.Footsteps && !State.IsSpying)
			{
				Player component = ((Component)self).GetComponent<Player>();
				bool flag;
				if (component == null)
				{
					flag = false;
				}
				else
				{
					SteamChannel channel = component.channel;
					flag = ((channel != null) ? new bool?(channel.IsLocalPlayer) : null).GetValueOrDefault();
				}
				if (flag)
				{
					try
					{
						Footsteps.Spawn(((Component)self).transform.position, Footsteps.IsSprinting(self) ? 1.5f : 1f);
					}
					catch (Exception ex)
					{
						if (Footsteps._stepLog < 10)
						{
							Runtime.Trace("step err: " + ex.Message);
							Footsteps._stepLog++;
						}
					}
				}
			}
			Footsteps.Unhook(Footsteps._stepSv, Footsteps._stepPtr);
			try
			{
				Footsteps._stepOrig.Invoke(self, null);
			}
			finally
			{
				Footsteps.Rehook(Footsteps._stepPtr, "HkStep");
			}
		}

		private static void HkLand(object self)
		{
			if (State.Footsteps && !State.IsSpying)
			{
				Player component = ((Component)self).GetComponent<Player>();
				bool flag;
				if (component == null)
				{
					flag = false;
				}
				else
				{
					SteamChannel channel = component.channel;
					flag = ((channel != null) ? new bool?(channel.IsLocalPlayer) : null).GetValueOrDefault();
				}
				if (flag)
				{
					Footsteps.Spawn(((Component)self).transform.position, 1.8f);
				}
			}
			Footsteps.Unhook(Footsteps._landSv, Footsteps._landPtr);
			try
			{
				Footsteps._landOrig.Invoke(self, null);
			}
			finally
			{
				Footsteps.Rehook(Footsteps._landPtr, "HkLand");
			}
		}

		private static bool IsSprinting(object self)
		{
			try
			{
				FieldInfo field = self.GetType().GetField("player", BindingFlags.Instance | BindingFlags.Public);
				if (field == null)
				{
					return false;
				}
				object value = field.GetValue(self);
				if (value == null)
				{
					return false;
				}
				FieldInfo field2 = value.GetType().GetField("stance", BindingFlags.Instance | BindingFlags.Public);
				if (field2 == null)
				{
					return false;
				}
				object value2 = field2.GetValue(value);
				if (value2 == null)
				{
					return false;
				}
				PropertyInfo property = value2.GetType().GetProperty("stance", BindingFlags.Instance | BindingFlags.Public);
				if (property != null)
				{
					return (int)property.GetValue(value2, null) == 6;
				}
			}
			catch
			{
			}
			return false;
		}

		private static void Spawn(Vector3 pos, float mult)
		{
			if (Footsteps._mat == null)
			{
				return;
			}
			GameObject gameObject = new GameObject("mc_step");
			gameObject.layer = 16;
			gameObject.transform.position = pos + Vector3.up * 0.05f;
			gameObject.transform.eulerAngles = new Vector3(90f, 0f, 0f);
			gameObject.transform.localScale = Vector3.zero;
			gameObject.AddComponent<MeshFilter>().mesh = ((State.FootstepShape == 0) ? Footsteps._circle : Footsteps._star);
			MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
			meshRenderer.sharedMaterial = new Material(Footsteps._mat)
			{
				color = State.FootstepColor
			};
			Footsteps._active.Add(new Footsteps.StepFX
			{
				go = gameObject,
				rend = meshRenderer,
				birth = Time.time,
				mult = mult
			});
		}

		public static void Update()
		{
			Footsteps.Init();
			Footsteps.InstallHooks();
			for (int i = Footsteps._active.Count - 1; i >= 0; i--)
			{
				Footsteps.StepFX stepFX = Footsteps._active[i];
				if (!stepFX.go)
				{
					Footsteps._active.RemoveAt(i);
				}
				else
				{
					float num = (Time.time - stepFX.birth) / State.FootstepLifetime;
					if (num > 1f)
					{
						UnityEngine.Object.Destroy(stepFX.go);
						Footsteps._active.RemoveAt(i);
					}
					else if (State.IsSpying)
					{
						if (stepFX.go.activeSelf)
						{
							stepFX.go.SetActive(false);
						}
					}
					else
					{
						if (!stepFX.go.activeSelf)
						{
							stepFX.go.SetActive(true);
						}
						float num2 = num * State.FootstepSize * stepFX.mult;
						stepFX.go.transform.localScale = new Vector3(num2, num2, num2);
						float num3 = State.FootstepSpin ? (State.FootstepSpinSpd * Time.time) : 0f;
						stepFX.go.transform.eulerAngles = new Vector3(90f, num3, 0f);
						Color footstepColor = State.FootstepColor;
						footstepColor.a *= 1f - num;
						stepFX.rend.material.color = footstepColor;
					}
				}
			}
		}

		private static Mesh MakeCircle()
		{
			Mesh mesh = new Mesh();
			float num = 0.09817477f;
			List<Vector3> list = new List<Vector3>();
			List<Color> list2 = new List<Color>();
			List<int> list3 = new List<int>();
			for (int i = 0; i <= 64; i++)
			{
				float num2 = (float)i * num;
				list.Add(new Vector3(Mathf.Cos(num2), Mathf.Sin(num2), 0f));
				list2.Add(Color.white);
				if (i > 0)
				{
					list3.Add(i - 1);
					list3.Add(i);
				}
			}
			list3.Add(64);
			list3.Add(0);
			mesh.vertices = list.ToArray();
			mesh.colors = list2.ToArray();
			mesh.SetIndices(list3.ToArray(), (MeshTopology)3, 0);
			return mesh;
		}

		private static Mesh MakeStar()
		{
			Mesh mesh = new Mesh();
			Vector3[] array = new Vector3[6];
			for (int i = 0; i < 3; i++)
			{
				float num = -1.5707964f + (float)i * 3.1415927f * 2f / 3f;
				array[i] = new Vector3(Mathf.Cos(num), Mathf.Sin(num), 0f);
			}
			for (int j = 0; j < 3; j++)
			{
				float num2 = 1.5707964f + (float)j * 3.1415927f * 2f / 3f;
				array[3 + j] = new Vector3(Mathf.Cos(num2), Mathf.Sin(num2), 0f);
			}
			mesh.vertices = array;
			mesh.colors = new Color[]
			{
				Color.white,
				Color.white,
				Color.white,
				Color.white,
				Color.white,
				Color.white
			};
			mesh.SetIndices(new int[]
			{
				0,
				1,
				1,
				2,
				2,
				0,
				3,
				4,
				4,
				5,
				5,
				3
			}, (MeshTopology)3, 0);
			return mesh;
		}

		private static readonly List<Footsteps.StepFX> _active = new List<Footsteps.StepFX>();

		private static Mesh _circle;

		private static Mesh _star;

		private static Material _mat;

		private static MaterialPropertyBlock _block;

		private static bool _inited;

		private static byte[] _stepSv = new byte[14];

		private static byte[] _landSv = new byte[14];

		private static IntPtr _stepPtr;

		private static IntPtr _landPtr;

		private static MethodInfo _stepOrig;

		private static MethodInfo _landOrig;

		private static bool _hooked;

		private static int _stepLog;

		private struct StepFX
		{
			public GameObject go;

			public MeshRenderer rend;

			public float birth;

			public float mult;
		}
	}
}
