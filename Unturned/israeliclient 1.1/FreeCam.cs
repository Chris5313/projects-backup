using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SDG.Framework.Rendering;
using SDG.Unturned;
using UnityEngine;

namespace gatyware
{
	[Obfuscation(Exclude = true)]
	public class FreeCam : MonoBehaviour
	{
		private void Awake()
		{
			UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
			FreeCam.FreeCamCamera = base.gameObject.AddComponent<Camera>();
			Camera instance = MainCamera.instance;
			if (instance != null)
			{
				base.transform.position = instance.transform.position;
				base.transform.rotation = instance.transform.rotation;
				this._yaw = instance.transform.eulerAngles.y;
				this._pitch = instance.transform.eulerAngles.x;
				FreeCam.FreeCamCamera.clearFlags = instance.clearFlags;
				FreeCam.FreeCamCamera.fieldOfView = instance.fieldOfView;
				FreeCam.FreeCamCamera.farClipPlane = instance.farClipPlane;
				FreeCam.FreeCamCamera.nearClipPlane = instance.nearClipPlane;
				FreeCam.FreeCamCamera.cullingMask = instance.cullingMask;
				FreeCam._savedMain = instance;
				FreeCam._savedMain.enabled = false;
			}
			base.gameObject.AddComponent<GLRenderer>();
			base.gameObject.tag = "MainCamera";
			FreeCam.InstallInputHook();
			FreeCam.InstallLookHook();
		}

		private void OnDestroy()
		{
			if (FreeCam._savedMain != null)
			{
				FreeCam._savedMain.enabled = true;
			}
			FreeCam.FreeCamCamera = null;
			FreeCam.Instance = null;
		}

		private void Update()
		{
			float y = Input.mouseScrollDelta.y;
			if (y != 0f)
			{
				State.FreeCamSpeed = Mathf.Clamp(State.FreeCamSpeed + y * 2f, 1f, 100f);
			}
			float num = State.FreeCamSpeed;
			if (InputEx.GetKey(ControlsSettings.sprint))
			{
				num *= 2f;
			}
			float num2 = 0f;
			float num3 = 0f;
			float num4 = 0f;
			if (InputEx.GetKey(ControlsSettings.left))
			{
				num2 = -num * Time.deltaTime;
			}
			if (InputEx.GetKey(ControlsSettings.right))
			{
				num2 = num * Time.deltaTime;
			}
			if (InputEx.GetKey(ControlsSettings.up))
			{
				num4 = num * Time.deltaTime;
			}
			if (InputEx.GetKey(ControlsSettings.down))
			{
				num4 = -num * Time.deltaTime;
			}
			if (InputEx.GetKey(ControlsSettings.jump))
			{
				num3 = num * Time.deltaTime;
			}
			if (InputEx.GetKey(ControlsSettings.crouch))
			{
				num3 = -num * Time.deltaTime;
			}
			this._yaw += ControlsSettings.mouseAimSensitivity * Input.GetAxis("mouse_x");
			this._pitch -= ControlsSettings.mouseAimSensitivity * Input.GetAxis("mouse_y");
			this._pitch = Mathf.Clamp(this._pitch, -89f, 89f);
			base.transform.eulerAngles = new Vector3(this._pitch, this._yaw, 0f);
			base.transform.Translate(num2, num3, num4);
		}

		public static void Enable()
		{
			if (FreeCam.Instance != null)
			{
				return;
			}
			FreeCam.Instance = new GameObject("ic_freecam").AddComponent<FreeCam>();
		}

		public static void Disable()
		{
			if (FreeCam.Instance == null)
			{
				return;
			}
			UnityEngine.Object.Destroy(FreeCam.Instance.gameObject);
		}

		public static void Toggle()
		{
			if (FreeCam.Instance != null)
			{
				FreeCam.Disable();
				return;
			}
			FreeCam.Enable();
		}

		[DllImport("kernel32.dll")]
		private static extern bool VirtualProtect(IntPtr addr, int size, uint prot, out uint old);

		private static void CacheInputFields()
		{
			if (FreeCam._fieldsCached)
			{
				return;
			}
			FreeCam._fieldsCached = true;
			BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			FreeCam._countField = typeof(PlayerInput).GetField("count", bindingAttr);
			FreeCam._simField = typeof(PlayerInput).GetField("_simulation", bindingAttr);
		}

		public static void InstallInputHook()
		{
			if (FreeCam._fixedHooked)
			{
				return;
			}
			try
			{
				FreeCam._fixedOrig = typeof(PlayerInput).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
				if (FreeCam._fixedOrig == null)
				{
					Runtime.Trace("freecam: FixedUpdate not found");
				}
				else
				{
					RuntimeHelpers.PrepareMethod(FreeCam._fixedOrig.MethodHandle);
					FreeCam._fixedPtr = FreeCam._fixedOrig.MethodHandle.GetFunctionPointer();
					MethodInfo method = typeof(FreeCam).GetMethod("HookFixedUpdate", BindingFlags.Static | BindingFlags.NonPublic);
					RuntimeHelpers.PrepareMethod(method.MethodHandle);
					Marshal.Copy(FreeCam._fixedPtr, FreeCam._fixedSaved, 0, 14);
					FreeCam.WriteJmp(FreeCam._fixedPtr, method.MethodHandle.GetFunctionPointer());
					FreeCam._fixedHooked = true;
					FreeCam.CacheInputFields();
					Runtime.Trace("freecam: input hook OK");
				}
			}
			catch (Exception ex)
			{
				Runtime.Trace("freecam: input err " + ex.Message);
			}
		}

		public static void InstallLookHook()
		{
			if (FreeCam._lookHooked)
			{
				return;
			}
			try
			{
				FreeCam._lookOrig = typeof(PlayerLook).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
				if (FreeCam._lookOrig == null)
				{
					Runtime.Trace("freecam: Look.Update not found");
				}
				else
				{
					RuntimeHelpers.PrepareMethod(FreeCam._lookOrig.MethodHandle);
					FreeCam._lookPtr = FreeCam._lookOrig.MethodHandle.GetFunctionPointer();
					MethodInfo method = typeof(FreeCam).GetMethod("HookLookUpdate", BindingFlags.Static | BindingFlags.NonPublic);
					RuntimeHelpers.PrepareMethod(method.MethodHandle);
					Marshal.Copy(FreeCam._lookPtr, FreeCam._lookSaved, 0, 14);
					FreeCam.WriteJmp(FreeCam._lookPtr, method.MethodHandle.GetFunctionPointer());
					FreeCam._lookHooked = true;
					Runtime.Trace("freecam: look hook OK");
				}
			}
			catch (Exception ex)
			{
				Runtime.Trace("freecam: look err " + ex.Message);
			}
		}

		private static void HookLookUpdate(PlayerLook self)
		{
			if (FreeCam.Instance != null && self.channel.IsLocalPlayer)
			{
				return;
			}
			uint prot;
			FreeCam.VirtualProtect(FreeCam._lookPtr, 14, 64U, out prot);
			Marshal.Copy(FreeCam._lookSaved, 0, FreeCam._lookPtr, 14);
			FreeCam.VirtualProtect(FreeCam._lookPtr, 14, prot, out prot);
			try
			{
				FreeCam._lookOrig.Invoke(self, null);
			}
			finally
			{
				FreeCam.WriteJmp(FreeCam._lookPtr, typeof(FreeCam).GetMethod("HookLookUpdate", BindingFlags.Static | BindingFlags.NonPublic).MethodHandle.GetFunctionPointer());
			}
		}

		private static void CacheMoveFields()
		{
			if (FreeCam._moveFieldsCached)
			{
				return;
			}
			FreeCam._moveFieldsCached = true;
			BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.NonPublic;
			FreeCam._stanceField = typeof(PlayerStance).GetField("_stance", bindingAttr);
			FreeCam._horizField = typeof(PlayerMovement).GetField("_horizontal", bindingAttr);
			FreeCam._vertField = typeof(PlayerMovement).GetField("_vertical", bindingAttr);
			FreeCam._jumpField = typeof(PlayerMovement).GetField("_jump", bindingAttr);
		}

		private static void HookFixedUpdate(PlayerInput self)
		{
			bool isLocalPlayer = self.channel.IsLocalPlayer;
			if (!(FreeCam.Instance != null) || !isLocalPlayer)
			{
				FreeCam.CallOrig(self);
				if (isLocalPlayer)
				{
					Spinbot.AfterFixedUpdate(self);
				}
				if (isLocalPlayer)
				{
					SilentAimV2.AfterFixedUpdate(self);
				}
				return;
			}
			FreeCam.CacheMoveFields();
			Vector3 position = self.transform.position;
			Quaternion rotation = self.transform.rotation;
			EPlayerStance stance = self.player.stance.stance;
			PlayerMovement movement = self.player.movement;
			if (movement != null)
			{
				if (FreeCam._horizField != null)
				{
					FreeCam._horizField.SetValue(movement, 1);
				}
				if (FreeCam._vertField != null)
				{
					FreeCam._vertField.SetValue(movement, 1);
				}
				if (FreeCam._jumpField != null)
				{
					FreeCam._jumpField.SetValue(movement, false);
				}
			}
			FreeCam.CallOrig(self);
			Spinbot.AfterFixedUpdate(self);
			SilentAimV2.AfterFixedUpdate(self);
			if (movement != null && movement.controller != null)
			{
				movement.controller.enabled = false;
				self.transform.position = position;
				movement.controller.enabled = true;
			}
			else
			{
				self.transform.position = position;
			}
			self.transform.rotation = rotation;
			if (self.player.stance.stance != stance && FreeCam._stanceField != null)
			{
				try
				{
					FreeCam._stanceField.SetValue(self.player.stance, stance);
				}
				catch
				{
				}
			}
		}

		private static void CallOrig(PlayerInput self)
		{
			uint prot;
			FreeCam.VirtualProtect(FreeCam._fixedPtr, 14, 64U, out prot);
			Marshal.Copy(FreeCam._fixedSaved, 0, FreeCam._fixedPtr, 14);
			FreeCam.VirtualProtect(FreeCam._fixedPtr, 14, prot, out prot);
			try
			{
				FreeCam._fixedOrig.Invoke(self, null);
			}
			finally
			{
				FreeCam.WriteJmp(FreeCam._fixedPtr, typeof(FreeCam).GetMethod("HookFixedUpdate", BindingFlags.Static | BindingFlags.NonPublic).MethodHandle.GetFunctionPointer());
			}
		}

		private static void WriteJmp(IntPtr from, IntPtr to)
		{
			uint prot;
			FreeCam.VirtualProtect(from, 14, 64U, out prot);
			byte[] array = new byte[14];
			array[0] = byte.MaxValue;
			array[1] = 37;
			BitConverter.GetBytes(to.ToInt64()).CopyTo(array, 6);
			Marshal.Copy(array, 0, from, 14);
			FreeCam.VirtualProtect(from, 14, prot, out prot);
		}

		public static FreeCam Instance;

		public static Camera FreeCamCamera;

		private static Camera _savedMain;

		private float _yaw;

		private float _pitch;

		private static byte[] _fixedSaved = new byte[14];

		private static IntPtr _fixedPtr;

		private static MethodInfo _fixedOrig;

		private static bool _fixedHooked;

		private static FieldInfo _keysField;

		private static FieldInfo _countField;

		private static FieldInfo _simField;

		private static PropertyInfo _simProp;

		private static bool _fieldsCached;

		private static byte[] _lookSaved = new byte[14];

		private static IntPtr _lookPtr;

		private static MethodInfo _lookOrig;

		private static bool _lookHooked;

		private static FieldInfo _stanceField;

		private static FieldInfo _horizField;

		private static FieldInfo _vertField;

		private static FieldInfo _jumpField;

		private static bool _moveFieldsCached;
	}
}
