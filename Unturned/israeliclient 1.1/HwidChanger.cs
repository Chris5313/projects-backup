using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using SDG.Unturned;

namespace gatyware
{
	public static class HwidChanger
	{
		[DllImport("kernel32.dll")]
		private static extern bool VirtualProtect(IntPtr addr, int size, uint prot, out uint old);

		public static string DisplayHash
		{
			get
			{
				return HwidChanger._displayHash;
			}
		}

		public static bool IsActive
		{
			get
			{
				return State.HwidChangerOn && HwidChanger._hooked;
			}
		}

		public static void Init()
		{
			if (HwidChanger._hooked)
			{
				return;
			}
			try
			{
				HwidChanger._orig = typeof(LocalHwid).GetMethod("GetHwids", BindingFlags.Static | BindingFlags.Public);
				if (HwidChanger._orig == null)
				{
					Runtime.Trace("hwid: GetHwids not found");
				}
				else
				{
					RuntimeHelpers.PrepareMethod(HwidChanger._orig.MethodHandle);
					HwidChanger._ptr = HwidChanger._orig.MethodHandle.GetFunctionPointer();
					MethodInfo method = typeof(HwidChanger).GetMethod("HookGetHwids", BindingFlags.Static | BindingFlags.NonPublic);
					RuntimeHelpers.PrepareMethod(method.MethodHandle);
					Marshal.Copy(HwidChanger._ptr, HwidChanger._saved, 0, 14);
					HwidChanger.WriteJmp(HwidChanger._ptr, method.MethodHandle.GetFunctionPointer());
					HwidChanger._hooked = true;
					HwidChanger.Regenerate();
					Runtime.Trace("hwid: hook OK — " + HwidChanger._displayHash);
				}
			}
			catch (Exception ex)
			{
				Runtime.Trace("hwid: err " + ex.Message);
			}
		}

		private static byte[][] HookGetHwids()
		{
			if (!State.HwidChangerOn)
			{
				uint prot;
				HwidChanger.VirtualProtect(HwidChanger._ptr, 14, 64U, out prot);
				Marshal.Copy(HwidChanger._saved, 0, HwidChanger._ptr, 14);
				HwidChanger.VirtualProtect(HwidChanger._ptr, 14, prot, out prot);
				byte[][] result;
				try
				{
					result = (byte[][])HwidChanger._orig.Invoke(null, null);
				}
				finally
				{
					HwidChanger.WriteJmp(HwidChanger._ptr, typeof(HwidChanger).GetMethod("HookGetHwids", BindingFlags.Static | BindingFlags.NonPublic).MethodHandle.GetFunctionPointer());
				}
				return result;
			}
			if (HwidChanger._currentHwids == null)
			{
				HwidChanger.Regenerate();
			}
			return HwidChanger._currentHwids;
		}

		public static void Regenerate()
		{
			using (SHA1 sha = SHA1.Create())
			{
				HwidChanger._currentHwids = new byte[3][];
				HwidChanger._currentHwids[0] = sha.ComputeHash(Encoding.UTF8.GetBytes("ic_" + Guid.NewGuid().ToString("N")));
				HwidChanger._currentHwids[1] = sha.ComputeHash(Encoding.UTF8.GetBytes("ic_" + Guid.NewGuid().ToString("N")));
				HwidChanger._currentHwids[2] = sha.ComputeHash(Encoding.UTF8.GetBytes("ic_" + Guid.NewGuid().ToString("N") + DateTime.UtcNow.Ticks.ToString()));
			}
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < Math.Min(8, HwidChanger._currentHwids[0].Length); i++)
			{
				stringBuilder.Append(HwidChanger._currentHwids[0][i].ToString("X2"));
			}
			HwidChanger._displayHash = stringBuilder.ToString();
		}

		public static string GetRealHwid()
		{
			if (HwidChanger._orig == null || !HwidChanger._hooked)
			{
				return "Unknown";
			}
			string result;
			try
			{
				uint prot;
				HwidChanger.VirtualProtect(HwidChanger._ptr, 14, 64U, out prot);
				Marshal.Copy(HwidChanger._saved, 0, HwidChanger._ptr, 14);
				HwidChanger.VirtualProtect(HwidChanger._ptr, 14, prot, out prot);
				byte[][] array;
				try
				{
					array = (byte[][])HwidChanger._orig.Invoke(null, null);
				}
				finally
				{
					HwidChanger.WriteJmp(HwidChanger._ptr, typeof(HwidChanger).GetMethod("HookGetHwids", BindingFlags.Static | BindingFlags.NonPublic).MethodHandle.GetFunctionPointer());
				}
				if (array == null || array.Length == 0 || array[0] == null)
				{
					result = "None";
				}
				else
				{
					StringBuilder stringBuilder = new StringBuilder();
					for (int i = 0; i < Math.Min(8, array[0].Length); i++)
					{
						stringBuilder.Append(array[0][i].ToString("X2"));
					}
					result = stringBuilder.ToString();
				}
			}
			catch
			{
				result = "Error";
			}
			return result;
		}

		private static void WriteJmp(IntPtr from, IntPtr to)
		{
			uint prot;
			HwidChanger.VirtualProtect(from, 14, 64U, out prot);
			byte[] array = new byte[14];
			array[0] = byte.MaxValue;
			array[1] = 37;
			BitConverter.GetBytes(to.ToInt64()).CopyTo(array, 6);
			Marshal.Copy(array, 0, from, 14);
			HwidChanger.VirtualProtect(from, 14, prot, out prot);
		}

		private static byte[] _saved = new byte[14];

		private static IntPtr _ptr;

		private static MethodInfo _orig;

		private static bool _hooked;

		private static byte[][] _currentHwids;

		private static string _displayHash = "Not generated";
	}
}
