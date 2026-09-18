using System;
using SDG.Unturned;
using UnityEngine;
public class FreeCamera : MonoBehaviour
{
	[InitializeAttribute]
	private static void Initialize()
	{
		ScreenshotManager.PreDrawEvent = (SimpleDelegate)Delegate.Combine(ScreenshotManager.PreDrawEvent, new SimpleDelegate(FreeCamera.DisableFreeCamera));
		ScreenshotManager.PostDrawEvent = (SimpleDelegate)Delegate.Combine(ScreenshotManager.PostDrawEvent, new SimpleDelegate(FreeCamera.EnableFreeCamera));
	}
	private void Awake()
	{
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		FreeCamera.FreeCameraComponent = base.gameObject.AddComponent<Camera>();
		bool flag = MainCamera.instance != null;
		bool flag2 = flag;
		if (flag2)
		{
			base.transform.position = MainCamera.instance.transform.position;
			FreeCamera.FreeCameraComponent.clearFlags = MainCamera.instance.clearFlags;
			FreeCamera.FreeCameraComponent.fieldOfView = MainCamera.instance.fieldOfView;
			FreeCamera.FreeCameraComponent.farClipPlane = MainCamera.instance.farClipPlane;
			FreeCamera.FreeCameraComponent.nearClipPlane = MainCamera.instance.nearClipPlane;
			FreeCamera.FreeCameraComponent.cullingMask = MainCamera.instance.cullingMask;
			FreeCamera.OriginalMainCamera = MainCamera.instance;
			FreeCamera.OriginalMainCamera.enabled = false;
		}
		base.gameObject.tag = "MainCamera";
	}
	private void OnDestroy()
	{
		bool flag = FreeCamera.OriginalMainCamera != null;
		bool flag2 = flag;
		if (flag2)
		{
			FreeCamera.OriginalMainCamera.enabled = true;
		}
	}
	private static void DisableFreeCamera()
	{
		bool flag = FreeCamera.Instance != null && FreeCamera.OriginalMainCamera != null;
		bool flag2 = flag;
		if (flag2)
		{
			FreeCamera.OriginalMainCamera.enabled = true;
			FreeCamera.Instance.enabled = false;
			FreeCamera.Instance.gameObject.tag = "Default";
		}
	}
	private static void EnableFreeCamera()
	{
		bool flag = FreeCamera.OriginalMainCamera != null && FreeCamera.Instance != null;
		bool flag2 = flag;
		if (flag2)
		{
			FreeCamera.OriginalMainCamera.enabled = false;
			FreeCamera.Instance.enabled = true;
			FreeCamera.Instance.gameObject.tag = "MainCamera";
		}
	}
	private void Update()
	{
		this.VerticalMove = 0f;
		this.HorizontalMove = 0f;
		this.ForwardMove = 0f;
		bool key = InputEx.GetKey(ControlsSettings.sprint);
		bool flag = key;
		if (flag)
		{
			this.MoveSpeed = MiscConfig.freeCameraSpeed * 2;
		}
		else
		{
			this.MoveSpeed = MiscConfig.freeCameraSpeed;
		}
		bool key2 = InputEx.GetKey(ControlsSettings.jump);
		bool flag2 = key2;
		if (flag2)
		{
			this.VerticalMove = (float)this.MoveSpeed * Time.deltaTime;
		}
		else
		{
			bool key3 = InputEx.GetKey(ControlsSettings.crouch);
			bool flag3 = key3;
			if (flag3)
			{
				this.VerticalMove = -((float)this.MoveSpeed * Time.deltaTime);
			}
		}
		bool key4 = InputEx.GetKey(ControlsSettings.left);
		bool flag4 = key4;
		if (flag4)
		{
			this.HorizontalMove = -((float)this.MoveSpeed * Time.deltaTime);
		}
		else
		{
			bool key5 = InputEx.GetKey(ControlsSettings.right);
			bool flag5 = key5;
			if (flag5)
			{
				this.HorizontalMove = (float)this.MoveSpeed * Time.deltaTime;
			}
		}
		bool key6 = InputEx.GetKey(ControlsSettings.up);
		bool flag6 = key6;
		if (flag6)
		{
			this.ForwardMove = (float)this.MoveSpeed * Time.deltaTime;
		}
		else
		{
			bool key7 = InputEx.GetKey(ControlsSettings.down);
			bool flag7 = key7;
			if (flag7)
			{
				this.ForwardMove = -((float)this.MoveSpeed * Time.deltaTime);
			}
		}
		bool flag8 = Input.mouseScrollDelta.y != 0f;
		bool flag9 = flag8;
		if (flag9)
		{
			MiscConfig.freeCameraSpeed = (int)Mathf.Clamp((float)MiscConfig.freeCameraSpeed + Input.mouseScrollDelta.y * 4f, 3f, 35f);
		}
		this.Yaw += ControlsSettings.mouseAimSensitivity * Input.GetAxis("mouse_x");
		this.Pitch -= ControlsSettings.mouseAimSensitivity * Input.GetAxis("mouse_y");
		base.transform.eulerAngles = new Vector3(this.Pitch, this.Yaw, 0f);
		base.transform.Translate(this.HorizontalMove, this.VerticalMove, this.ForwardMove);
	}
	public static FreeCamera Instance;
	public static Camera OriginalMainCamera;
	public static Camera FreeCameraComponent;
	private float Pitch = 0f;
	private float Yaw = 0f;
	private int MoveSpeed = 5;
	private float HorizontalMove = 0f;
	private float ForwardMove = 0f;
	private float VerticalMove = 0f;
}
