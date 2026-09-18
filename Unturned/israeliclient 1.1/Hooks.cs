using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

namespace gatyware
{
	public static class Hooks
	{
		public static void Install()
		{
			try
			{
				PropertyInfo property = typeof(Cursor).GetProperty("lockState", BindingFlags.Static | BindingFlags.Public);
				if (property == null)
				{
					Runtime.Trace("hook: lockState prop not found");
				}
				else
				{
					Hooks._origMethod = property.GetSetMethod();
					if (Hooks._origMethod == null)
					{
						Runtime.Trace("hook: setter not found");
					}
					else
					{
						RuntimeHelpers.PrepareMethod(Hooks._origMethod.MethodHandle);
						Hooks._origPtr = Hooks._origMethod.MethodHandle.GetFunctionPointer();
						MethodInfo method = typeof(Hooks).GetMethod("OnSetLockState", BindingFlags.Static | BindingFlags.Public);
						RuntimeHelpers.PrepareMethod(method.MethodHandle);
						Hooks._hookPtr = method.MethodHandle.GetFunctionPointer();
						Hooks._saved = new byte[13];
						Marshal.Copy(Hooks._origPtr, Hooks._saved, 0, 13);
						Hooks.WriteJmp(Hooks._origPtr, Hooks._hookPtr);
						Hooks._active = true;
						Runtime.Trace("hook: Cursor.lockState patched OK");
					}
				}
			}
			catch (Exception ex)
			{
				Runtime.Trace("hook error: " + ex.Message);
			}
		}

		public static void OnSetLockState(CursorLockMode mode)
		{
			if (State.Open && mode != null)
			{
				return;
			}
			Marshal.Copy(Hooks._saved, 0, Hooks._origPtr, 13);
			try
			{
				Hooks._origMethod.Invoke(null, new object[]
				{
					mode
				});
			}
			finally
			{
				Hooks.WriteJmp(Hooks._origPtr, Hooks._hookPtr);
			}
		}

		private static void WriteJmp(IntPtr from, IntPtr to)
		{
			byte[] array = new byte[13];
			array[0] = 73;
			array[1] = 187;
			BitConverter.GetBytes(to.ToInt64()).CopyTo(array, 2);
			array[10] = 65;
			array[11] = byte.MaxValue;
			array[12] = 227;
			Marshal.Copy(array, 0, from, 13);
		}

		public static void Uninstall()
		{
			if (!Hooks._active)
			{
				return;
			}
			Marshal.Copy(Hooks._saved, 0, Hooks._origPtr, 13);
			Hooks._active = false;
		}

		private static byte[] _saved;

		private static IntPtr _origPtr;

		private static IntPtr _hookPtr;

		private static MethodInfo _origMethod;

		private static bool _active;
	}
}
