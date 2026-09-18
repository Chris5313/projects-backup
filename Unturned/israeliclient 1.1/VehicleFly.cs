using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SDG.Unturned;
using UnityEngine;

namespace gatyware
{
	public static class VehicleFly
	{
		[DllImport("kernel32.dll")]
		private static extern bool VirtualProtect(IntPtr addr, int size, uint prot, out uint old);

		public static void UpdateInput()
		{
			if (!VehicleFly._wasActive)
			{
				return;
			}
			if (State.Open || PlayerPauseUI.active || PlayerDashboardUI.active)
			{
				return;
			}
			VehicleFly._scrollDelta += Input.mouseScrollDelta.y;
		}

		public static void FixedTick()
		{
			if (!State.VehicleFlyOn || State.IsSpying)
			{
				VehicleFly.Restore();
				return;
			}
			Player player = Player.player;
			if (player == null)
			{
				VehicleFly.Restore();
				return;
			}
			InteractableVehicle vehicle = player.movement.getVehicle();
			if (vehicle == null)
			{
				VehicleFly.Restore();
				return;
			}
			Rigidbody component = vehicle.GetComponent<Rigidbody>();
			if (component == null)
			{
				VehicleFly.Restore();
				return;
			}

			// MoonClient-style vehicle fly
			if (State.VehicleFlyStyle == 1)
			{
				VehicleFly.MoonClientFixedTick(vehicle, component);
				return;
			}

			// Original vehicle fly
			if (!VehicleFly._wasActive)
			{
				VehicleFly._savedRb = component;
				VehicleFly._savedGravity = component.useGravity;
				VehicleFly._savedKinematic = component.isKinematic;
				try
				{
					VehicleFly._savedUndergroundWhitelist = Level.info.configData.Use_Underground_Whitelist;
					Level.info.configData.Use_Underground_Whitelist = false;
				}
				catch
				{
				}
				if (VehicleFly._frozenField == null)
				{
					VehicleFly._frozenField = typeof(InteractableVehicle).GetField("isFrozen", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				}
				VehicleFly._wasActive = true;
				Runtime.Trace("vfly: ON vehicle=" + vehicle.asset.vehicleName);
			}
			try
			{
				component.constraints = 0;
				component.freezeRotation = false;
				component.useGravity = false;
				component.isKinematic = true;
				try
				{
					if (VehicleFly._frozenField != null)
					{
						VehicleFly._frozenField.SetValue(vehicle, false);
					}
				}
				catch
				{
				}
				if (PlayerPauseUI.active || PlayerDashboardUI.active || State.Open || FreeCam.Instance != null)
				{
					VehicleFly._scrollDelta = 0f;
				}
				else
				{
					Transform transform = vehicle.transform;
					float num = vehicle.asset.speedMax * State.VehicleFlySpeed;
					if ((Input.GetKey((KeyCode)304)) || (Input.GetKey((KeyCode)303)))
					{
						num *= 5f;
					}
					if (VehicleFly._scrollDelta != 0f)
					{
						State.VehicleFlySpeed = Mathf.Clamp(State.VehicleFlySpeed + VehicleFly._scrollDelta * 0.5f, 0.1f, 50f);
						VehicleFly._scrollDelta = 0f;
					}
					Vector3 forward = Vector3.forward;
					Vector3 right = Vector3.right;
					try
					{
						Camera instance = MainCamera.instance;
						if (instance != null)
						{
							forward = instance.transform.forward;
							right = instance.transform.right;
						}
					}
					catch
					{
					}
					float num2 = num * Time.fixedDeltaTime;
					Vector3 vector = Vector3.zero;
					if (InputEx.GetKey(ControlsSettings.up))
					{
						vector += forward * num2;
					}
					if (InputEx.GetKey(ControlsSettings.down))
					{
						vector -= forward * num2;
					}
					if (InputEx.GetKey(ControlsSettings.left))
					{
						vector -= right * num2;
					}
					if (InputEx.GetKey(ControlsSettings.right))
					{
						vector += right * num2;
					}
					float num3 = 0.2f * State.VehicleFlySpeed;
					if (Input.GetKey(ControlsSettings.jump))
					{
						vector += new Vector3(0f, num3, 0f);
					}
					if ((Input.GetKey((KeyCode)306)) || (Input.GetKey((KeyCode)305)) || Input.GetKey(ControlsSettings.crouch))
					{
						vector -= new Vector3(0f, num3, 0f);
					}
					if (vector != Vector3.zero)
					{
						if (State.VehicleDamageOff)
						{
							component.MovePosition(transform.position + vector);
						}
						else
						{
							transform.position += vector;
						}
					}
					if (vector != Vector3.zero)
					{
						if (VehicleFly._rfvSetter == null)
						{
							PropertyInfo property = typeof(InteractableVehicle).GetProperty("ReplicatedForwardVelocity", BindingFlags.Instance | BindingFlags.Public);
							VehicleFly._rfvSetter = ((property != null) ? property.GetSetMethod(true) : null);
						}
						if (VehicleFly._rfvSetter != null)
						{
							VehicleFly._rfvSetter.Invoke(vehicle, new object[]
							{
								num
							});
						}
					}
				}
			}
			catch (Exception ex)
			{
				Runtime.Trace("vfly err: " + ex.Message);
			}
		}

		private static void MoonClientFixedTick(InteractableVehicle vehicle, Rigidbody rb)
		{
			if (!VehicleFly._wasActive)
			{
				VehicleFly._savedRb = rb;
				VehicleFly._savedGravity = rb.useGravity;
				VehicleFly._savedKinematic = rb.isKinematic;
				try
				{
					VehicleFly._savedUndergroundWhitelist = Level.info.configData.Use_Underground_Whitelist;
					Level.info.configData.Use_Underground_Whitelist = false;
				}
				catch
				{
				}
				if (VehicleFly._frozenField == null)
				{
					VehicleFly._frozenField = typeof(InteractableVehicle).GetField("isFrozen", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				}
				VehicleFly._wasActive = true;
				Runtime.Trace("vfly: MoonClient ON vehicle=" + vehicle.asset.vehicleName);
			}

			try
			{
				rb.constraints = RigidbodyConstraints.None;
				rb.freezeRotation = false;
				rb.useGravity = false;
				rb.isKinematic = false;
				rb.velocity = Vector3.zero;
				rb.angularVelocity = Vector3.zero;

				if (VehicleFly._frozenField != null)
				{
					VehicleFly._frozenField.SetValue(vehicle, false);
				}

				if (PlayerPauseUI.active || PlayerDashboardUI.active || State.Open || FreeCam.Instance != null)
				{
					VehicleFly._scrollDelta = 0f;
					return;
				}

				Transform transform = vehicle.transform;
				float speed = vehicle.asset.speedMax * State.VehicleFlySpeed * 2f;

				if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
				{
					speed *= 3f;
				}

				if (VehicleFly._scrollDelta != 0f)
				{
					State.VehicleFlySpeed = Mathf.Clamp(State.VehicleFlySpeed + VehicleFly._scrollDelta * 0.5f, 0.1f, 50f);
					VehicleFly._scrollDelta = 0f;
				}

				Vector3 moveDir = Vector3.zero;
				Camera cam = MainCamera.instance;
				if (cam != null)
				{
					Vector3 camForward = cam.transform.forward;
					Vector3 camRight = cam.transform.right;
					camForward.y = 0;
					camRight.y = 0;
					camForward.Normalize();
					camRight.Normalize();

					if (InputEx.GetKey(ControlsSettings.up))
					{
						moveDir += camForward;
					}
					if (InputEx.GetKey(ControlsSettings.down))
					{
						moveDir -= camForward;
					}
					if (InputEx.GetKey(ControlsSettings.left))
					{
						moveDir -= camRight;
					}
					if (InputEx.GetKey(ControlsSettings.right))
					{
						moveDir += camRight;
					}
				}

				if (Input.GetKey(ControlsSettings.jump))
				{
					moveDir += Vector3.up;
				}
				if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(ControlsSettings.crouch))
				{
					moveDir -= Vector3.up;
				}

				if (moveDir != Vector3.zero)
				{
					moveDir.Normalize();
					rb.velocity = moveDir * speed;
				}
				else
				{
					rb.velocity = Vector3.zero;
				}

				// Sync replication
				if (VehicleFly._rfvSetter == null)
				{
					PropertyInfo prop = typeof(InteractableVehicle).GetProperty("ReplicatedForwardVelocity", BindingFlags.Instance | BindingFlags.Public);
					VehicleFly._rfvSetter = ((prop != null) ? prop.GetSetMethod(true) : null);
				}
				if (VehicleFly._rfvSetter != null)
				{
					VehicleFly._rfvSetter.Invoke(vehicle, new object[] { speed });
				}
			}
			catch (Exception ex)
			{
				Runtime.Trace("vfly moonclient err: " + ex.Message);
			}
		}

		private static void Restore()
		{
			if (!VehicleFly._wasActive)
			{
				return;
			}
			VehicleFly._wasActive = false;
			try
			{
				Level.info.configData.Use_Underground_Whitelist = VehicleFly._savedUndergroundWhitelist;
			}
			catch
			{
			}
			if (VehicleFly._savedRb != null)
			{
				try
				{
					VehicleFly._savedRb.isKinematic = VehicleFly._savedKinematic;
					VehicleFly._savedRb.useGravity = VehicleFly._savedGravity;
					VehicleFly._savedRb.velocity = Vector3.zero;
					VehicleFly._savedRb.angularVelocity = Vector3.zero;
					Runtime.Trace("vfly: OFF restored physics");
				}
				catch
				{
				}
				VehicleFly._savedRb = null;
			}
		}

		public static void InitExitHook()
		{
			if (VehicleFly._recovHooked)
			{
				return;
			}
			try
			{
				VehicleFly._recovMethod = typeof(InteractableVehicle).GetMethod("tellRecov", BindingFlags.Instance | BindingFlags.Public);
				if (VehicleFly._recovMethod == null)
				{
					Runtime.Trace("vfly: tellRecov not found");
				}
				else
				{
					MethodInfo method = typeof(VehicleFly).GetMethod("HookTellRecov", BindingFlags.Static | BindingFlags.NonPublic);
					RuntimeHelpers.PrepareMethod(VehicleFly._recovMethod.MethodHandle);
					RuntimeHelpers.PrepareMethod(method.MethodHandle);
					IntPtr functionPointer = VehicleFly._recovMethod.MethodHandle.GetFunctionPointer();
					IntPtr functionPointer2 = method.MethodHandle.GetFunctionPointer();
					Marshal.Copy(functionPointer, VehicleFly._recovSaved, 0, 14);
					VehicleFly._recovTramp = Marshal.AllocHGlobal(28);
					Marshal.Copy(VehicleFly._recovSaved, 0, VehicleFly._recovTramp, 14);
					byte[] array = new byte[14];
					array[0] = byte.MaxValue;
					array[1] = 37;
					BitConverter.GetBytes((long)(functionPointer + 14)).CopyTo(array, 6);
					Marshal.Copy(array, 0, VehicleFly._recovTramp + 14, 14);
					uint prot;
					VehicleFly.VirtualProtect(VehicleFly._recovTramp, 28, 64U, out prot);
					byte[] array2 = new byte[14];
					array2[0] = byte.MaxValue;
					array2[1] = 37;
					BitConverter.GetBytes((long)functionPointer2).CopyTo(array2, 6);
					VehicleFly.VirtualProtect(functionPointer, 14, 64U, out prot);
					Marshal.Copy(array2, 0, functionPointer, 14);
					VehicleFly.VirtualProtect(functionPointer, 14, prot, out prot);
					VehicleFly._recovHooked = true;
					Runtime.Trace("vfly: tellRecov hook OK");
				}
			}
			catch (Exception ex)
			{
				Runtime.Trace("vfly: recov hook err " + ex.Message);
			}
		}

		private static void HookTellRecov(InteractableVehicle self, Vector3 newPosition, int newRecov)
		{
			VehicleFly.CallOrigRecov(self, newPosition, newRecov);
			if (VehicleFly._wasActive && Player.player != null && Player.player.movement.getVehicle() == self)
			{
				Rigidbody component = self.GetComponent<Rigidbody>();
				if (component != null)
				{
					component.isKinematic = true;
					component.useGravity = false;
				}
				try
				{
					FieldInfo field = typeof(InteractableVehicle).GetField("isFrozen", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					if (field != null)
					{
						field.SetValue(self, false);
					}
				}
				catch
				{
				}
			}
		}

		private static void CallOrigRecov(InteractableVehicle self, Vector3 pos, int recov)
		{
			if (VehicleFly._recovTramp == IntPtr.Zero)
			{
				return;
			}
			IntPtr functionPointer = VehicleFly._recovMethod.MethodHandle.GetFunctionPointer();
			uint prot;
			VehicleFly.VirtualProtect(functionPointer, 14, 64U, out prot);
			Marshal.Copy(VehicleFly._recovSaved, 0, functionPointer, 14);
			VehicleFly.VirtualProtect(functionPointer, 14, prot, out prot);
			try
			{
				VehicleFly._recovMethod.Invoke(self, new object[]
				{
					pos,
					recov
				});
			}
			finally
			{
				byte[] array = new byte[14];
				MethodInfo method = typeof(VehicleFly).GetMethod("HookTellRecov", BindingFlags.Static | BindingFlags.NonPublic);
				RuntimeHelpers.PrepareMethod(method.MethodHandle);
				array[0] = byte.MaxValue;
				array[1] = 37;
				BitConverter.GetBytes((long)method.MethodHandle.GetFunctionPointer()).CopyTo(array, 6);
				VehicleFly.VirtualProtect(functionPointer, 14, 64U, out prot);
				Marshal.Copy(array, 0, functionPointer, 14);
				VehicleFly.VirtualProtect(functionPointer, 14, prot, out prot);
			}
		}

		private static bool _wasActive;

		private static Rigidbody _savedRb;

		private static bool _savedGravity;

		private static bool _savedKinematic;

		private static MethodInfo _rfvSetter;

		private static bool _savedUndergroundWhitelist;

		private static FieldInfo _frozenField;

		private static float _scrollDelta;

		private static byte[] _recovSaved = new byte[14];

		private static IntPtr _recovTramp;

		private static MethodInfo _recovMethod;

		private static bool _recovHooked;
	}
}
