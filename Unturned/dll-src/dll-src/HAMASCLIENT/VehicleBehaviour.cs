using System;
using System.Reflection;
using SDG.Unturned;
using Steamworks;
using UnityEngine;
public class VehicleBehaviour : MonoBehaviour
{
	[InitializeAttribute]
	private static void Init()
	{
		Provider.onClientConnected = (Provider.ClientConnected)Delegate.Remove(Provider.onClientConnected, new Provider.ClientConnected(VehicleBehaviour.EnsureComponent));
		Provider.onServerConnected = (Provider.ServerConnected)Delegate.Remove(Provider.onServerConnected, new Provider.ServerConnected(VehicleBehaviour.OnServerConnected));
		Provider.onClientDisconnected = (Provider.ClientDisconnected)Delegate.Remove(Provider.onClientDisconnected, new Provider.ClientDisconnected(VehicleBehaviour.OnDisconnectedDestroy));
		Provider.onClientConnected = (Provider.ClientConnected)Delegate.Combine(Provider.onClientConnected, new Provider.ClientConnected(VehicleBehaviour.EnsureComponent));
		Provider.onServerConnected = (Provider.ServerConnected)Delegate.Combine(Provider.onServerConnected, new Provider.ServerConnected(VehicleBehaviour.OnServerConnected));
		Provider.onClientDisconnected = (Provider.ClientDisconnected)Delegate.Combine(Provider.onClientDisconnected, new Provider.ClientDisconnected(VehicleBehaviour.OnDisconnectedDestroy));
		ScreenshotManager.PreDrawEvent = (SimpleDelegate)Delegate.Combine(ScreenshotManager.PreDrawEvent, new SimpleDelegate(VehicleBehaviour.SnapVehicleToGroundOnSpy));
		ScreenshotManager.PostDrawEvent = (SimpleDelegate)Delegate.Combine(ScreenshotManager.PostDrawEvent, new SimpleDelegate(VehicleBehaviour.RestoreVehicleOnSpyEnd));
		bool flag = Bootstrapper.InitFinished && Provider.isConnected;
		if (flag)
		{
			VehicleBehaviour.EnsureComponent();
		}
	}
	private static void EnsureComponent()
	{
		bool flag = VehicleBehaviour.instance == null;
		if (flag)
		{
			VehicleBehaviour.instance = Bootstrapper.HostGameObject.AddComponent<VehicleBehaviour>();
		}
	}
	private static void OnDisconnectedDestroy()
	{
		UnityEngine.Object.Destroy(VehicleBehaviour.instance);
	}
	public static void RestoreVehicleState()
	{
		VehicleBehaviour.RestoreVehicleCollision(VehicleBehaviour.DvehCollisionVehicle);
		bool flag = VehicleBehaviour.instance != null;
		if (flag)
		{
			VehicleBehaviour.instance.prevUseVehiclePhysics = false;
		}
		bool flag2 = VehicleBehaviour.instance != null && VehicleBehaviour.instance.currentVehicle != null;
		if (flag2)
		{
			VehicleBehaviour.originalPhysicsProfile.applyTo(VehicleBehaviour.instance.currentVehicle);
			ReflectionUtil.SetFieldValue(typeof(VehicleAsset), "_steerMax", VehicleBehaviour.instance.currentVehicle.asset, VehicleBehaviour.savedSteerMax);
			ReflectionUtil.SetFieldValue(typeof(VehicleAsset), "_steerMin", VehicleBehaviour.instance.currentVehicle.asset, VehicleBehaviour.savedSteerMin);
		}
	}
	public static void ApplyVehicleBehaviour(InteractableVehicle vehicle)
	{
		VehicleBehaviour.savedSteerMax = vehicle.asset.steerMax;
		VehicleBehaviour.savedSteerMin = vehicle.asset.steerMin;
		bool flag = VehicleBehaviour.capturedPhysicsProfile == null;
		if (flag)
		{
			VehicleBehaviour.originalPhysicsProfile = VehicleBehaviour.CaptureVehiclePhysicsProfile(vehicle.GetComponent<Rigidbody>(), vehicle);
			VehicleBehaviour.capturedPhysicsProfile = VehicleBehaviour.CaptureVehiclePhysicsProfile(vehicle.GetComponent<Rigidbody>(), vehicle);
		}
		bool flag2 = MiscConfig.customVehicleBehaviour && !MiscConfig.vehicleNoclip;
		if (flag2)
		{
			ReflectionUtil.SetFieldValue(typeof(VehicleAsset), "_steerMax", vehicle.asset, VehicleBehaviour.steerMaxOverride);
			ReflectionUtil.SetFieldValue(typeof(VehicleAsset), "_steerMin", vehicle.asset, VehicleBehaviour.steerMinOverride);
			VehicleBehaviour.capturedPhysicsProfile.applyTo(vehicle);
		}
		bool flag3 = MiscConfig.customVehicleBehaviour && MiscConfig.vehicleNoclip && MiscConfig.useVehiclePhysics;
		if (flag3)
		{
			VehicleBehaviour.ApplyVehiclePlayerOnlyCollision(vehicle);
		}
	}
	public static VehiclePhysicsProfileAsset CaptureVehiclePhysicsProfile(Rigidbody rigid, InteractableVehicle vehicle)
	{
		VehiclePhysicsProfileAsset vehiclePhysicsProfileAsset = new VehiclePhysicsProfileAsset();
		Type typeFromHandle = typeof(VehiclePhysicsProfileAsset);
		typeFromHandle.GetProperty("rootMassOverride").SetValue(vehiclePhysicsProfileAsset, rigid.mass);
		typeFromHandle.GetProperty("rootMassMultiplier").SetValue(vehiclePhysicsProfileAsset, 1f);
		typeFromHandle.GetProperty("rootDragMultiplier").SetValue(vehiclePhysicsProfileAsset, 1f);
		typeFromHandle.GetProperty("rootAngularDragMultiplier").SetValue(vehiclePhysicsProfileAsset, 1f);
		typeFromHandle.GetProperty("wheelStiffnessTractionMultiplier").SetValue(vehiclePhysicsProfileAsset, vehicle.tires[0].stiffnessTractionMultiplier);
		typeFromHandle.GetProperty("wheelDampingRate").SetValue(vehiclePhysicsProfileAsset, vehicle.tires[0].wheel.wheelDampingRate);
		typeFromHandle.GetProperty("wheelSuspensionForce").SetValue(vehiclePhysicsProfileAsset, vehicle.tires[0].wheel.suspensionSpring.spring);
		typeFromHandle.GetProperty("wheelSuspensionDamper").SetValue(vehiclePhysicsProfileAsset, vehicle.tires[0].wheel.suspensionSpring.damper);
		typeFromHandle.GetProperty("wheelMassOverride").SetValue(vehiclePhysicsProfileAsset, vehicle.tires[0].wheel.mass);
		typeFromHandle.GetProperty("wheelMassMultiplier").SetValue(vehiclePhysicsProfileAsset, 1f);
		typeFromHandle.GetProperty("motorTorqueMultiplier").SetValue(vehiclePhysicsProfileAsset, vehicle.tires[0].motorTorqueMultiplier);
		typeFromHandle.GetProperty("motorTorqueClampMultiplier").SetValue(vehiclePhysicsProfileAsset, vehicle.tires[0].motorTorqueClampMultiplier);
		typeFromHandle.GetProperty("brakeTorqueMultiplier").SetValue(vehiclePhysicsProfileAsset, vehicle.tires[0].brakeTorqueMultiplier);
		typeFromHandle.GetProperty("brakeTorqueTractionMultiplier").SetValue(vehiclePhysicsProfileAsset, vehicle.tires[0].brakeTorqueTractionMultiplier);
		typeFromHandle.GetProperty("carjackForceMultiplier").SetValue(vehiclePhysicsProfileAsset, 1f);
		PropertyInfo property = typeFromHandle.GetProperty("sidewaysFriction");
		object obj = vehiclePhysicsProfileAsset;
		property.SetValue(obj, new VehiclePhysicsProfileAsset.Friction
		{
			stiffness = vehicle.tires[0].stiffnessSideways,
			asymptoteSlip = vehicle.tires[0].wheel.sidewaysFriction.asymptoteSlip,
			asymptoteValue = vehicle.tires[0].wheel.sidewaysFriction.asymptoteValue,
			extremumSlip = vehicle.tires[0].wheel.sidewaysFriction.extremumSlip,
			extremumValue = vehicle.tires[0].wheel.sidewaysFriction.extremumValue
		});
		PropertyInfo property2 = typeFromHandle.GetProperty("forwardFriction");
		object obj2 = vehiclePhysicsProfileAsset;
		property2.SetValue(obj2, new VehiclePhysicsProfileAsset.Friction
		{
			stiffness = vehicle.tires[0].stiffnessForward,
			asymptoteSlip = vehicle.tires[0].wheel.forwardFriction.asymptoteSlip,
			asymptoteValue = vehicle.tires[0].wheel.forwardFriction.asymptoteValue,
			extremumSlip = vehicle.tires[0].wheel.forwardFriction.extremumSlip,
			extremumValue = vehicle.tires[0].wheel.forwardFriction.extremumValue
		});
		return vehiclePhysicsProfileAsset;
	}
	public static VehiclePhysicsProfileAsset ClonePhysicsProfile(VehiclePhysicsProfileAsset asset)
	{
		VehiclePhysicsProfileAsset vehiclePhysicsProfileAsset = new VehiclePhysicsProfileAsset();
		Type typeFromHandle = typeof(VehiclePhysicsProfileAsset);
		typeFromHandle.GetProperty("rootMassOverride").SetValue(vehiclePhysicsProfileAsset, asset.rootMassOverride);
		typeFromHandle.GetProperty("rootMassMultiplier").SetValue(vehiclePhysicsProfileAsset, asset.rootMassMultiplier);
		typeFromHandle.GetProperty("rootDragMultiplier").SetValue(vehiclePhysicsProfileAsset, asset.rootDragMultiplier);
		typeFromHandle.GetProperty("rootAngularDragMultiplier").SetValue(vehiclePhysicsProfileAsset, asset.rootAngularDragMultiplier);
		typeFromHandle.GetProperty("wheelStiffnessTractionMultiplier").SetValue(vehiclePhysicsProfileAsset, asset.wheelStiffnessTractionMultiplier);
		typeFromHandle.GetProperty("wheelDampingRate").SetValue(vehiclePhysicsProfileAsset, asset.wheelDampingRate);
		typeFromHandle.GetProperty("wheelSuspensionForce").SetValue(vehiclePhysicsProfileAsset, asset.wheelSuspensionForce);
		typeFromHandle.GetProperty("wheelSuspensionDamper").SetValue(vehiclePhysicsProfileAsset, asset.wheelSuspensionDamper);
		typeFromHandle.GetProperty("wheelMassOverride").SetValue(vehiclePhysicsProfileAsset, asset.wheelMassOverride);
		typeFromHandle.GetProperty("wheelMassMultiplier").SetValue(vehiclePhysicsProfileAsset, asset.wheelMassMultiplier);
		typeFromHandle.GetProperty("motorTorqueMultiplier").SetValue(vehiclePhysicsProfileAsset, asset.motorTorqueMultiplier);
		typeFromHandle.GetProperty("motorTorqueClampMultiplier").SetValue(vehiclePhysicsProfileAsset, asset.motorTorqueClampMultiplier);
		typeFromHandle.GetProperty("brakeTorqueMultiplier").SetValue(vehiclePhysicsProfileAsset, asset.brakeTorqueMultiplier);
		typeFromHandle.GetProperty("brakeTorqueTractionMultiplier").SetValue(vehiclePhysicsProfileAsset, asset.brakeTorqueTractionMultiplier);
		typeFromHandle.GetProperty("carjackForceMultiplier").SetValue(vehiclePhysicsProfileAsset, asset.carjackForceMultiplier);
		PropertyInfo property = typeFromHandle.GetProperty("sidewaysFriction");
		object obj = vehiclePhysicsProfileAsset;
		property.SetValue(obj, new VehiclePhysicsProfileAsset.Friction
		{
			stiffness = asset.forwardFriction.Value.stiffness,
			asymptoteSlip = asset.forwardFriction.Value.asymptoteSlip,
			asymptoteValue = asset.forwardFriction.Value.asymptoteValue,
			extremumSlip = asset.forwardFriction.Value.extremumSlip,
			extremumValue = asset.forwardFriction.Value.extremumValue
		});
		PropertyInfo property2 = typeFromHandle.GetProperty("forwardFriction");
		object obj2 = vehiclePhysicsProfileAsset;
		property2.SetValue(obj2, new VehiclePhysicsProfileAsset.Friction
		{
			stiffness = asset.forwardFriction.Value.stiffness,
			asymptoteSlip = asset.forwardFriction.Value.asymptoteSlip,
			asymptoteValue = asset.forwardFriction.Value.asymptoteValue,
			extremumSlip = asset.forwardFriction.Value.extremumSlip,
			extremumValue = asset.forwardFriction.Value.extremumValue
		});
		return vehiclePhysicsProfileAsset;
	}
	private void Update()
	{
		bool flag = !MiscConfig.customVehicleBehaviour || !MiscConfig.vehicleMouseMove || !PlayerMovementHook.IsDrivingVehicle;
		if (flag)
		{
			this.vehicleLookInitialized = false;
			VehicleBehaviour.mouseSteerInput = 0f;
		}
		else
		{
			bool menuOpened = MenuState.menuOpened;
			if (menuOpened)
			{
				VehicleBehaviour.mouseSteerInput = 0f;
			}
			else
			{
				bool flag2 = PlayerUI.window != null;
				if (flag2)
				{
					PlayerUI.window.showCursor = false;
				}
				Cursor.lockState = CursorLockMode.Locked;
				this.currentVehicle = Player.player.movement.getVehicle();
				bool flag3 = this.currentVehicle == null;
				if (flag3)
				{
					this.vehicleLookInitialized = false;
					VehicleBehaviour.mouseSteerInput = 0f;
				}
				else
				{
					this.vehicleRigidbody = this.currentVehicle.GetComponent<Rigidbody>();
					bool flag4 = this.vehicleRigidbody == null;
					if (flag4)
					{
						this.vehicleLookInitialized = false;
						VehicleBehaviour.mouseSteerInput = 0f;
					}
					else
					{
						try
						{
							Transform transform = this.vehicleRigidbody.transform;
							bool flag5 = this.prevVehicleNoclip != MiscConfig.vehicleNoclip;
							if (flag5)
							{
								this.vehicleLookInitialized = false;
								this.prevVehicleNoclip = MiscConfig.vehicleNoclip;
							}
							bool useDirectMouseLook = MiscConfig.vehicleNoclip || MiscConfig.vehicleMouseMove;
							if (useDirectMouseLook)
							{
								bool flag6 = !this.vehicleLookInitialized;
								if (flag6)
								{
									this.vehicleLookYaw = VehicleBehaviour.NormalizeAngle(transform.eulerAngles.y);
									this.vehicleLookPitch = VehicleBehaviour.NormalizeAngle(transform.eulerAngles.x);
									this.vehicleLookRoll = VehicleBehaviour.NormalizeAngle(transform.eulerAngles.z);
									this.vehicleLookInitialized = true;
								}
								float num = ControlsSettings.mouseAimSensitivity * Input.GetAxis("mouse_x");
								float num2 = ControlsSettings.mouseAimSensitivity * Input.GetAxis("mouse_y");
								this.vehicleLookYaw += num;
								this.vehicleLookPitch -= num2;
								this.vehicleLookPitch = Mathf.Clamp(this.vehicleLookPitch, -89f, 89f);
								bool key = Input.GetKey(ControlsSettings.leanLeft);
								if (key)
								{
									this.vehicleLookRoll += 90f * Time.deltaTime;
								}
								bool key2 = Input.GetKey(ControlsSettings.leanRight);
								if (key2)
								{
									this.vehicleLookRoll -= 90f * Time.deltaTime;
								}
								Quaternion quaternion = Quaternion.Euler(this.vehicleLookPitch, this.vehicleLookYaw, this.vehicleLookRoll);
								transform.rotation = quaternion;
								VehicleBehaviour._yawField.Set(Player.player.look, this.vehicleLookYaw);
								VehicleBehaviour._pitchField.Set(Player.player.look, this.vehicleLookPitch);
								VehicleBehaviour.mouseSteerInput = 0f;
							}
							else
							{
								bool flag7 = !this.vehicleLookInitialized;
								if (flag7)
								{
									this.vehicleLookYaw = VehicleBehaviour.NormalizeAngle(transform.eulerAngles.y);
									this.vehicleLookPitch = 0f;
									this.vehicleLookInitialized = true;
								}
								float axis = Input.GetAxis("mouse_x");
								float axis2 = Input.GetAxis("mouse_y");
								float num3 = ControlsSettings.mouseAimSensitivity * axis;
								float num4 = ControlsSettings.mouseAimSensitivity * axis2;
								this.vehicleLookYaw = VehicleBehaviour.NormalizeAngle(this.vehicleLookYaw + num3);
								this.vehicleLookPitch = Mathf.Clamp(this.vehicleLookPitch - num4, -89f, 89f);
								float num5 = VehicleBehaviour.NormalizeAngle(transform.eulerAngles.y);
								bool flag8 = Mathf.Abs(axis) < 0.05f && Mathf.Abs(axis2) < 0.05f;
								if (flag8)
								{
									this.vehicleLookYaw = VehicleBehaviour.NormalizeAngle(Mathf.LerpAngle(this.vehicleLookYaw, num5, 6f * Time.deltaTime));
								}
								VehicleBehaviour._yawField.Set(Player.player.look, this.vehicleLookYaw);
								VehicleBehaviour._pitchField.Set(Player.player.look, this.vehicleLookPitch);
								float num6 = VehicleBehaviour.NormalizeAngle(this.vehicleLookYaw - num5);
								bool flag9 = Mathf.Abs(num6) < 3f;
								if (flag9)
								{
									VehicleBehaviour.mouseSteerInput = 0f;
								}
								else
								{
									VehicleBehaviour.mouseSteerInput = Mathf.Clamp(num6 / 45f, -1f, 1f);
								}
							}
						}
						catch
						{
						}
					}
				}
			}
		}
	}
	private void LateUpdate()
	{
		bool flag = !MiscConfig.customVehicleBehaviour || !MiscConfig.vehicleMouseMove || !PlayerMovementHook.IsDrivingVehicle;
		if (!flag)
		{
			bool menuOpened = MenuState.menuOpened;
			if (!menuOpened)
			{
				bool flag2 = this.currentVehicle == null || this.vehicleRigidbody == null;
				if (!flag2)
				{
					try
					{
						Transform transform = this.vehicleRigidbody.transform;
						Camera instance = MainCamera.instance;
						bool flag3 = instance != null;
						if (flag3)
						{
							bool flag4 = Input.mouseScrollDelta.y != 0f;
							if (flag4)
							{
								MiscConfig.vehicleCameraDistance = Mathf.Clamp(MiscConfig.vehicleCameraDistance + Input.mouseScrollDelta.y * 0.5f, 3f, 30f);
							}
							Vector3 vector = Quaternion.Euler(0f, this.vehicleLookYaw, 0f) * Vector3.forward;
							Vector3 vector2 = transform.position - vector * MiscConfig.vehicleCameraDistance + Vector3.up * MiscConfig.vehicleCameraHeight;
							float num = Mathf.Clamp(MiscConfig.vehicleCameraSmooth * Time.deltaTime, 0f, 1f);
							Vector3 vector3 = Vector3.Lerp(instance.transform.position, vector2, num);
							vector3.y = vector2.y;
							instance.transform.position = vector3;
							Quaternion quaternion = Quaternion.Euler(this.vehicleLookPitch, this.vehicleLookYaw, 0f);
							instance.transform.rotation = Quaternion.Slerp(instance.transform.rotation, quaternion, num);
						}
					}
					catch
					{
					}
				}
			}
		}
	}
	private void FixedUpdate()
	{
		bool flag = !MiscConfig.customVehicleBehaviour || !(MiscConfig.vehicleNoclip || MiscConfig.vehicleMouseMove || MiscConfig.vehicleLockRotation) || !PlayerMovementHook.IsDrivingVehicle || ScreenshotManager.IsSpying;
		if (!flag)
		{
			this.currentVehicle = Player.player.movement.getVehicle();
			bool flag2 = this.currentVehicle == null;
			if (!flag2)
			{
				this.vehicleRigidbody = this.currentVehicle.GetComponent<Rigidbody>();
				bool flag3 = this.vehicleRigidbody == null;
				if (!flag3)
				{
					try
					{
						bool flag4 = this.prevUseVehiclePhysics != MiscConfig.useVehiclePhysics;
						if (flag4)
						{
							this.prevUseVehiclePhysics = MiscConfig.useVehiclePhysics;
							bool useVehiclePhysics = MiscConfig.useVehiclePhysics;
							if (useVehiclePhysics)
							{
								VehicleBehaviour.ApplyVehiclePlayerOnlyCollision(this.currentVehicle);
							}
							else
							{
								VehicleBehaviour.RestoreVehicleCollision(this.currentVehicle);
							}
						}
						this.vehicleRigidbody.constraints = RigidbodyConstraints.None;
						this.vehicleRigidbody.freezeRotation = false;
						this.vehicleRigidbody.useGravity = false;
						this.vehicleRigidbody.isKinematic = true;
						Transform transform = this.vehicleRigidbody.transform;
						// Scroll wheel adjusts fly speed exponentially (percentage-based) for smooth control:
						// fine steps at low speed, progressively bigger jumps at high speed. Shift gives a big boost.
						// Applied to every flight mode (mouse steer, lock rotation, noclip).
						float scrollTicks = Input.mouseScrollDelta.y;
						if (scrollTicks != 0f)
						{
							// Dead-zero floor: scrolling up from 0 kicks the speed to the minimum
							float baseSpeed = (MiscConfig.vehicleSpeed <= 0f && scrollTicks > 0f) ? 0.1f : MiscConfig.vehicleSpeed;
							MiscConfig.vehicleSpeed = Mathf.Clamp(baseSpeed * Mathf.Pow(1.20f, scrollTicks), 0f, 100f);
						}
						float effectiveSpeed = MiscConfig.vehicleSpeed;
						if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
						{
							effectiveSpeed *= 5f;
						}
						float num = this.currentVehicle.asset.TargetForwardSpeed * effectiveSpeed * Time.fixedDeltaTime;
						bool useVehiclePhysics2 = MiscConfig.useVehiclePhysics;

						// Mouse steer: full takeover - mouse rotates the vehicle, WASD moves in camera direction
						bool mouseSteerActive = MiscConfig.vehicleMouseMove;
						if (mouseSteerActive)
						{
							Vector3 moveDir = Vector3.zero;
							Camera cam = MainCamera.instance;
							if (cam != null)
							{
								Vector3 camForward = cam.transform.forward;
								Vector3 camRight = cam.transform.right;
								camForward.y = 0f;
								camRight.y = 0f;
								camForward.Normalize();
								camRight.Normalize();

								if (InputEx.GetKey(ControlsSettings.up))
									moveDir += camForward;
								if (InputEx.GetKey(ControlsSettings.down))
									moveDir -= camForward;
								if (InputEx.GetKey(ControlsSettings.left))
									moveDir -= camRight;
								if (InputEx.GetKey(ControlsSettings.right))
									moveDir += camRight;
							}
							if (Input.GetKey(ControlsSettings.jump))
								moveDir += Vector3.up;
							if (Input.GetKey(ControlsSettings.crouch) || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
								moveDir -= Vector3.up;

							if (moveDir != Vector3.zero)
							{
								moveDir.Normalize();
								Vector3 velocity = moveDir * num;
								if (useVehiclePhysics2)
									this.vehicleRigidbody.MovePosition(transform.position + velocity);
								else
									transform.position += velocity;
							}
						}
						else if (MiscConfig.vehicleLockRotation)
						{
							// Israeli-style lock rotation: the mouse/camera NEVER rotates the vehicle; WASD moves where you look (full 3D).
							// Speed is controlled globally by scroll wheel / Shift (handled above for all modes).
							float flySpeed = num;
							Vector3 moveDir2 = Vector3.zero;
							Camera cam2 = MainCamera.instance;
							if (cam2 != null)
							{

								Vector3 camForward2 = cam2.transform.forward;
								Vector3 camRight2 = cam2.transform.right;
								camRight2.y = 0f;
								camRight2.Normalize();

								if (InputEx.GetKey(ControlsSettings.up))
									moveDir2 += camForward2;
								if (InputEx.GetKey(ControlsSettings.down))
									moveDir2 -= camForward2;
								if (InputEx.GetKey(ControlsSettings.left))
									moveDir2 -= camRight2;
								if (InputEx.GetKey(ControlsSettings.right))
									moveDir2 += camRight2;
							}
							if (Input.GetKey(ControlsSettings.jump))
								moveDir2 += Vector3.up;
							if (Input.GetKey(KeyCode.X))
								moveDir2 -= Vector3.up;
							// CTRL/crouch is left to the game in lock-rotation fly mode.

							if (moveDir2 != Vector3.zero)
							{
								moveDir2.Normalize();
								Vector3 velocity2 = moveDir2 * flySpeed;
								if (useVehiclePhysics2)
									this.vehicleRigidbody.MovePosition(transform.position + velocity2);
								else
									transform.position += velocity2;
							}

							// Manual rotation with the arrow keys only (turn speed in degrees/sec)
							Vector3 rotateDir = Vector3.zero;
							float turnAmount = MiscConfig.vehicleLockRotationTurnSpeed * Time.fixedDeltaTime;
							if (Input.GetKey(KeyCode.LeftArrow))
								rotateDir.y -= turnAmount;
							if (Input.GetKey(KeyCode.RightArrow))
								rotateDir.y += turnAmount;
							if (Input.GetKey(KeyCode.UpArrow))
								rotateDir.x -= turnAmount;
							if (Input.GetKey(KeyCode.DownArrow))
								rotateDir.x += turnAmount;
							if (rotateDir != Vector3.zero)
								transform.Rotate(rotateDir);
						}
						else
						{
							// Original keyboard rotation when mouse steer / lock rotation is off
							Vector3 forward = transform.forward;
							Vector3 zero = Vector3.zero;
							bool key = Input.GetKey(ControlsSettings.left);
							if (key) zero.y -= 2f;
							bool key2 = Input.GetKey(ControlsSettings.right);
							if (key2) zero.y += 2f;
							bool key3 = Input.GetKey(ControlsSettings.leanLeft);
							if (key3) zero.z += 1.5f;
							bool key4 = Input.GetKey(ControlsSettings.leanRight);
							if (key4) zero.z -= 1.5f;
							bool key5 = Input.GetKey(KeyCode.UpArrow);
							if (key5) zero.x -= 1.5f;
							bool key6 = Input.GetKey(KeyCode.DownArrow);
							if (key6) zero.x += 1.5f;
							bool key7 = Input.GetKey(KeyCode.LeftArrow);
							if (key7) zero.z += 1.5f;
							bool key8 = Input.GetKey(KeyCode.RightArrow);
							if (key8) zero.z -= 1.5f;
							if (zero != Vector3.zero) transform.Rotate(zero);

							Vector3 vector = Vector3.zero;
							if (InputEx.GetKey(ControlsSettings.up))
								vector += forward * num;
							else if (InputEx.GetKey(ControlsSettings.down))
								vector -= forward * num;

							Vector3 vector2 = Vector3.zero;
							if (Input.GetKey(ControlsSettings.jump))
								vector2 += new Vector3(0f, 0.2f, 0f) * MiscConfig.vehicleSpeed;
							else if (Input.GetKey(ControlsSettings.crouch))
								vector2 -= new Vector3(0f, 0.2f, 0f) * MiscConfig.vehicleSpeed;

							if (useVehiclePhysics2)
							{
								Vector3 total = vector + vector2;
								if (total != Vector3.zero)
									this.vehicleRigidbody.MovePosition(transform.position + total);
							}
							else
							{
								if (vector2 != Vector3.zero) transform.position += vector2;
								if (vector != Vector3.zero) transform.position += vector;
							}
						}
					}
					catch
					{
					}
				}
			}
		}
	}
	private static float NormalizeAngle(float angle)
	{
		while (angle > 180f)
		{
			angle -= 360f;
		}
		while (angle < -180f)
		{
			angle += 360f;
		}
		return angle;
	}
	private static void SnapVehicleToGroundOnSpy()
	{
		bool flag = !MiscConfig.vehicleGroundOnSpy;
		if (!flag)
		{
			InteractableVehicle vehicle = Player.player.movement.getVehicle();
			bool flag2 = vehicle == null;
			if (!flag2)
			{
				Transform transform = vehicle.transform;
				VehicleBehaviour.DvehGroundSavedPos = transform.position;
				VehicleBehaviour.DvehGroundSavedRot = transform.rotation;
				Rigidbody component = vehicle.GetComponent<Rigidbody>();
				bool flag3 = component != null;
				if (flag3)
				{
					VehicleBehaviour.DvehGroundSavedVel = component.velocity;
					VehicleBehaviour.DvehGroundSavedAngVel = component.angularVelocity;
				}
				VehicleBehaviour.DvehGroundHasSaved = true;
				int num = vehicle.tires.Length;
				bool flag4 = num == 0;
				if (flag4)
				{
					RaycastHit raycastHit = default(RaycastHit);
					bool flag5 = Physics.Raycast(transform.position + Vector3.up * 5f, Vector3.down, out raycastHit, 1000f, 1048576);
					if (flag5)
					{
						Vector3 vector = new Vector3(transform.position.x, raycastHit.point.y, transform.position.z);
						bool flag6 = component != null;
						if (flag6)
						{
							component.velocity = Vector3.zero;
							component.angularVelocity = Vector3.zero;
							component.position = vector;
						}
						transform.position = vector;
					}
				}
				else
				{
					Vector3[] array = new Vector3[num];
					Vector3[] array2 = new Vector3[num];
					Vector3[] array3 = new Vector3[num];
					int num2 = 0;
					for (int i = 0; i < num; i++)
					{
						WheelCollider wheel = vehicle.tires[i].wheel;
						bool flag7 = wheel == null;
						if (flag7)
						{
							array[i] = Vector3.zero;
							array2[i] = transform.position;
							array3[i] = Vector3.up;
						}
						else
						{
							Vector3 position = wheel.transform.position;
							array[i] = transform.InverseTransformPoint(position);
							float radius = wheel.radius;
							RaycastHit raycastHit2;
							bool flag8 = Physics.Raycast(position + Vector3.up * 5f, Vector3.down, out raycastHit2, 100f, 1048576);
							if (flag8)
							{
								array2[i] = raycastHit2.point + raycastHit2.normal * radius;
								array3[i] = raycastHit2.normal;
								num2++;
							}
							else
							{
								array2[i] = position;
								array3[i] = Vector3.up;
							}
						}
					}
					bool flag9 = num2 == 0;
					if (!flag9)
					{
						Vector3 vector2 = Vector3.zero;
						for (int j = 0; j < num; j++)
						{
							vector2 += array3[j];
						}
						bool flag10 = vector2.sqrMagnitude < 0.001f;
						if (flag10)
						{
							vector2 = Vector3.up;
						}
						vector2.Normalize();
						bool flag11 = vector2.y < 0.5f;
						if (flag11)
						{
							vector2 = Vector3.up;
						}
						Vector3 vector3 = Vector3.ProjectOnPlane(transform.forward, vector2);
						bool flag12 = vector3.sqrMagnitude < 0.001f;
						if (flag12)
						{
							vector3 = transform.forward;
						}
						vector3.Normalize();
						Quaternion quaternion = Quaternion.LookRotation(vector3, vector2);
						Vector3 vector4 = Vector3.zero;
						for (int k = 0; k < num; k++)
						{
							Vector3 vector5 = quaternion * array[k];
							vector4 += array2[k] - vector5;
						}
						vector4 /= (float)num;
						bool flag13 = component != null;
						if (flag13)
						{
							component.velocity = Vector3.zero;
							component.angularVelocity = Vector3.zero;
							component.position = vector4;
							component.rotation = quaternion;
						}
						transform.position = vector4;
						transform.rotation = quaternion;
					}
				}
			}
		}
	}
	private static void RestoreVehicleOnSpyEnd()
	{
		bool flag = !VehicleBehaviour.DvehGroundHasSaved;
		if (!flag)
		{
			VehicleBehaviour.DvehGroundHasSaved = false;
			InteractableVehicle vehicle = Player.player.movement.getVehicle();
			bool flag2 = vehicle == null;
			if (!flag2)
			{
				Transform transform = vehicle.transform;
				Rigidbody component = vehicle.GetComponent<Rigidbody>();
				bool flag3 = component != null;
				if (flag3)
				{
					component.position = VehicleBehaviour.DvehGroundSavedPos;
					component.rotation = VehicleBehaviour.DvehGroundSavedRot;
					component.velocity = VehicleBehaviour.DvehGroundSavedVel;
					component.angularVelocity = VehicleBehaviour.DvehGroundSavedAngVel;
				}
				transform.position = VehicleBehaviour.DvehGroundSavedPos;
				transform.rotation = VehicleBehaviour.DvehGroundSavedRot;
			}
		}
	}
	private static void OnServerConnected(CSteamID steamid)
	{
		VehicleBehaviour.EnsureComponent();
	}
	public static void ApplyVehiclePlayerOnlyCollision(InteractableVehicle vehicle)
	{
		bool flag = vehicle == null;
		if (!flag)
		{
			bool flag2 = VehicleBehaviour.DvehCollisionFilterActive && VehicleBehaviour.DvehCollisionVehicle == vehicle;
			if (!flag2)
			{
				bool flag3 = VehicleBehaviour.DvehCollisionFilterActive && VehicleBehaviour.DvehCollisionVehicle != vehicle;
				if (flag3)
				{
					VehicleBehaviour.RestoreVehicleCollision(VehicleBehaviour.DvehCollisionVehicle);
				}
				VehicleBehaviour.DvehCollisionVehicle = vehicle;
				VehicleBehaviour.DvehCollisionFilterActive = true;
				try
				{
					int num = vehicle.tires.Length;
					for (int i = 0; i < num; i++)
					{
						WheelCollider wheel = vehicle.tires[i].wheel;
						bool flag4 = wheel != null;
						if (flag4)
						{
							wheel.enabled = false;
						}
					}
				}
				catch
				{
				}
			}
		}
	}
	public static void RestoreVehicleCollision(InteractableVehicle vehicle)
	{
		bool flag = vehicle == null || !VehicleBehaviour.DvehCollisionFilterActive;
		if (!flag)
		{
			VehicleBehaviour.DvehCollisionFilterActive = false;
			VehicleBehaviour.DvehCollisionVehicle = null;
			try
			{
				int num = vehicle.tires.Length;
				for (int i = 0; i < num; i++)
				{
					WheelCollider wheel = vehicle.tires[i].wheel;
					bool flag2 = wheel != null;
					if (flag2)
					{
						wheel.enabled = true;
					}
				}
			}
			catch
			{
			}
		}
	}
	public static VehicleBehaviour instance;
	public Rigidbody vehicleRigidbody;
	public InteractableVehicle currentVehicle;
	public static VehiclePhysicsProfileAsset capturedPhysicsProfile;
	public static VehiclePhysicsProfileAsset originalPhysicsProfile;
	public static float savedSteerMax;
	public static float savedSteerMin;
	public static float steerMaxOverride;
	public static float steerMinOverride;
	private static bool DvehGroundHasSaved = false;
	private static Vector3 DvehGroundSavedPos = Vector3.zero;
	private static Quaternion DvehGroundSavedRot = Quaternion.identity;
	private static Vector3 DvehGroundSavedVel = Vector3.zero;
	private static Vector3 DvehGroundSavedAngVel = Vector3.zero;
	public static float mouseSteerInput = 0f;
	private bool vehicleLookInitialized = false;
	private float vehicleLookYaw = 0f;
	private float vehicleLookPitch = 0f;
	private float vehicleLookRoll = 0f;
	private bool prevVehicleNoclip = false;
	private bool prevUseVehiclePhysics = false;
	private static ReflectedField<float> _pitchField = new ReflectedField<float>(typeof(PlayerLook), "_pitch", BindingFlags.Instance | BindingFlags.NonPublic);
	private static ReflectedField<float> _yawField = new ReflectedField<float>(typeof(PlayerLook), "_yaw", BindingFlags.Instance | BindingFlags.NonPublic);
	private static bool DvehCollisionFilterActive = false;
	public static InteractableVehicle DvehCollisionVehicle;
}
