using System;
using System.Reflection;
using SDG.NetPak;
using SDG.NetTransport;
using SDG.Unturned;
using UnityEngine;
public class PlayerInputOverride : PlayerInput
{
	[HookMethodAttribute(typeof(PlayerInput), "InitializePlayer", new Type[] { })]
	private static void OnInitializePlayerOverride(PlayerInput instance)
	{
		bool isLocalPlayer = instance.channel.IsLocalPlayer;
		if (isLocalPlayer)
		{
			PlayerInputOverride.TargetSimulationTick = 0U;
			PlayerInputOverride.FixedUpdateCounter = 0U;
			PlayerInputOverride.ClockAccumulator = 0U;
		}
		OverrideManager.CallOriginal(instance, Array.Empty<object>());
	}
	[HookMethodAttribute(typeof(PlayerInput), "FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic, new Type[] { })]
	public void FixedUpdateOverride()
	{
		bool flag = !MiscConfig.freeCamera && !MiscConfig.modifyMoveBehaviour && !MiscConfig.rawWalk && !MiscConfig.vehicleSpinbot;
		if (flag)
		{
			bool isLocalPlayer = base.channel.IsLocalPlayer;
			if (isLocalPlayer)
			{
				PlayerInputOverride.ThirdPersonAnimatorField.Get(Player.player.animator).transform.localEulerAngles = new Vector3(90f, 0f, 0f);
			}
			PlayerInputOverride.PlayerSpinActive = false;
			PlayerInputOverride.AntiAimSmoothInitialized = false;
			OverrideManager.CallOriginal(this, Array.Empty<object>());
		}
		else
		{
			bool flag2 = Provider.isServer && MiscConfig.freeCamera;
			if (flag2)
			{
				bool isAlive = base.player.life.IsAlive;
				if (isAlive)
				{
					EAttackInputFlags eattackInputFlags = PlayerInputOverride.PendingPrimaryAttackField.Get(this);
					EAttackInputFlags eattackInputFlags2 = PlayerInputOverride.PendingSecondaryAttackField.Get(this);
					this.UpdateAttackInputs(out eattackInputFlags, out eattackInputFlags2);
					PlayerInputOverride.PendingPrimaryAttackField.Set(this, eattackInputFlags);
					PlayerInputOverride.PendingSecondaryAttackField.Set(this, eattackInputFlags2);
					base.player.equipment.simulate(base.simulation, PlayerInputOverride.PendingPrimaryAttackField.Get(this), PlayerInputOverride.PendingSecondaryAttackField.Get(this), base.keys[9]);
					PlayerInputOverride.SimulationField.Set(this, base.simulation + 1U);
				}
			}
			else
			{
				bool flag3 = Provider.isServer || !base.channel.IsLocalPlayer;
				if (flag3)
				{
					OverrideManager.CallOriginal(this, Array.Empty<object>());
				}
				else
				{
					PlayerInputOverride.PendingInputPacketField.instance = this;
					bool flag4 = PlayerInputOverride.FixedUpdateCounter % PlayerInput.SAMPLES == 0U;
					if (flag4)
					{
						bool flag5 = !MiscConfig.rawWalk;
						if (flag5)
						{
							PlayerInputOverride.TickField.Set(this, Time.realtimeSinceStartup);
						}
						bool flag6 = PlayerInputOverride.PendingResimulationField.Get(this);
						if (flag6)
						{
							PlayerInputOverride.PendingResimulationField.Set(this, false);
							PlayerInputOverride.ClientResimulateMethod.InvokeOn(this, Array.Empty<object>());
						}
						base.keys[0] = base.player.movement.jump;
						base.keys[1] = false;
						base.keys[2] = false;
						base.keys[3] = base.player.stance.crouch;
						base.keys[4] = base.player.stance.prone;
						base.keys[5] = base.player.stance.sprint;
						base.keys[6] = !MiscConfig.localLeans && base.player.animator.leanLeft;
						base.keys[7] = !MiscConfig.localLeans && base.player.animator.leanRight;
						base.keys[8] = false;
						base.keys[9] = PlayerInputOverride.SteadyAimField.Get(base.player.stance);
						bool freeCamera = MiscConfig.freeCamera;
						if (freeCamera)
						{
							base.keys[0] = false;
							base.keys[2] = false;
							base.keys[6] = false;
							base.keys[7] = false;
						}
						bool flag7 = MenuConfigurationControlsUI.binding == byte.MaxValue;
						for (int i = 0; i < (int)ControlsSettings.NUM_PLUGIN_KEYS; i++)
						{
							int num = base.keys.Length - (int)ControlsSettings.NUM_PLUGIN_KEYS + i;
							base.keys[num] = flag7 && InputEx.GetKey(ControlsSettings.getPluginKeyCode(i));
						}
						EAttackInputFlags eattackInputFlags3 = PlayerInputOverride.PendingPrimaryAttackField.Get(this);
						EAttackInputFlags eattackInputFlags4 = PlayerInputOverride.PendingSecondaryAttackField.Get(this);
						this.UpdateAttackInputs(out eattackInputFlags3, out eattackInputFlags4);
						bool flag8 = MiscConfig.automaticSemiBurst && !MenuState.menuOpenedBacking && base.player.equipment.useable is UseableGun && InputEx.GetKey(ControlsSettings.primary) && PlayerInputOverride.GunFiremodeField.Get(base.player.equipment.useable) != EFiremode.AUTO;
						if (flag8)
						{
							switch (PlayerInputOverride.PendingPrimaryAttackField.Get(this))
							{
							case EAttackInputFlags.None:
								eattackInputFlags3 = EAttackInputFlags.Start;
								break;
							case EAttackInputFlags.Start:
								eattackInputFlags3 = EAttackInputFlags.Stop;
								break;
							case EAttackInputFlags.Stop:
								eattackInputFlags3 = EAttackInputFlags.Start;
								break;
							}
						}
						PlayerInputOverride.PendingPrimaryAttackField.Set(this, eattackInputFlags3);
						PlayerInputOverride.PendingSecondaryAttackField.Set(this, eattackInputFlags4);
						base.player.life.simulate(base.simulation);
						bool crouch = base.player.stance.crouch;
						bool prone = base.player.stance.prone;
						bool sprint = base.player.stance.sprint;
						bool flag9 = !MiscConfig.freeCamera;
						if (flag9)
						{
							base.player.stance.simulate(base.simulation, crouch, prone, sprint);
						}
						int num2 = (int)(base.player.movement.horizontal - 1);
						int num3 = (int)(base.player.movement.vertical - 1);
						bool flag10 = base.player.movement.jump;
						bool freeCamera2 = MiscConfig.freeCamera;
						if (freeCamera2)
						{
							num2 = 0;
							num3 = 0;
							flag10 = false;
						}
						base.player.movement.simulate(base.simulation, 0, num2, num3, base.player.look.look_x, base.player.look.look_y, flag10, sprint, PlayerInput.RATE);
						bool flag11 = base.player.stance.stance == EPlayerStance.DRIVING;
						if (flag11)
						{
							PlayerInputOverride.PlayerSpinActive = false;
							PlayerInputOverride.AntiAimSmoothInitialized = false;
							PlayerInputOverride.PendingInputPacketField.value = new DrivingPlayerInputPacket(base.player.movement.getVehicle());
						}
						else
						{
							bool freeCamera3 = MiscConfig.freeCamera;
							if (freeCamera3)
							{
								PlayerInputOverride.PendingInputPacketField.value = new WalkingPlayerInputPacket
								{
									analog = 17,
									clientPosition = base.transform.position,
									pitch = base.player.look.pitch,
									yaw = base.player.look.yaw
								};
							}
							else
							{
								bool modifyMoveBehaviour = MiscConfig.modifyMoveBehaviour;
								if (modifyMoveBehaviour)
								{
									float num4;
									int num5;
									byte b;
									byte b2;
									this.ComputeMoveOverride(out num4, out num5, out b, out b2);
									PlayerInputOverride.PlayerSpinActive = true;
									PlayerInputOverride.PlayerServerYaw = num4;
									PlayerInputOverride.PlayerServerPitch = (float)num5;
									PlayerInputOverride.PlayerServerPosition = base.transform.position;
									float num6 = ((MiscConfig.playerSpinbotDesync && !ScreenshotManager.IsSpying) ? MiscConfig.playerSpinbotDesyncAmount : 0f);
									float num7 = (ScreenshotManager.IsSpying ? 0f : (MiscConfig.showMoveModifying ? (num4 - base.player.look.yaw + num6) : num6));
									PlayerInputOverride.ThirdPersonAnimatorField.Get(Player.player.animator).transform.localEulerAngles = new Vector3(90f, num7, 0f);
									PlayerInputOverride.PendingInputPacketField.value = new WalkingPlayerInputPacket
									{
										analog = (byte)(((int)b << 4) | (int)b2),
										clientPosition = base.transform.position,
										pitch = (float)num5,
										yaw = num4
									};
								}
								else
								{
									PlayerInputOverride.ThirdPersonAnimatorField.Get(Player.player.animator).transform.localEulerAngles = new Vector3(90f, 0f, 0f);
									PlayerInputOverride.PendingInputPacketField.value = new WalkingPlayerInputPacket
									{
										analog = (byte)(((int)base.player.movement.horizontal << 4) | (int)base.player.movement.vertical),
										clientPosition = base.transform.position,
										pitch = base.player.look.pitch,
										yaw = base.player.look.yaw
									};
								}
							}
							object obj = Activator.CreateInstance(PlayerInputOverride.ClientMovementInputType);
							PlayerInputOverride.ClientInputFrameNumberField.Set(obj, base.simulation);
							PlayerInputOverride.ClientInputCrouchField.Set(obj, crouch);
							PlayerInputOverride.ClientInputProneField.Set(obj, prone);
							PlayerInputOverride.ClientInputXField.Set(obj, num2);
							PlayerInputOverride.ClientInputYField.Set(obj, num3);
							PlayerInputOverride.ClientInputJumpField.Set(obj, flag10);
							PlayerInputOverride.ClientInputSprintField.Set(obj, sprint);
							PlayerInputOverride.ClientInputRotationField.Set(obj, base.transform.rotation);
							PlayerInputOverride.ClientInputAimRotationField.Set(obj, base.player.look.aim.rotation);
							PlayerInputOverride.ClientInputHistoryAddMethod.InvokeOn(PlayerInputOverride.ClientInputHistoryList.GetValueFrom(this), new object[] { obj });
						}
						PlayerInputOverride.PendingInputPacketField.value.clientSimulationFrameNumber = base.simulation;
						PlayerInputOverride.PendingInputPacketField.value.recov = this.recov;
						base.player.equipment.simulate(base.simulation, PlayerInputOverride.PendingPrimaryAttackField.Get(this), PlayerInputOverride.PendingSecondaryAttackField.Get(this), PlayerInputOverride.SteadyAimField.Get(base.player.stance));
						bool freeCamera4 = MiscConfig.freeCamera;
						if (freeCamera4)
						{
							base.player.animator.simulate(base.simulation, false, false);
						}
						else
						{
							base.player.animator.simulate(base.simulation, base.player.animator.leanLeft, base.player.animator.leanRight);
						}
						PlayerInputOverride.TargetSimulationTick += PlayerInput.SAMPLES;
						PlayerInputOverride.SimulationField.Set(this, base.simulation + 1U);
					}
					bool flag12 = PlayerInputOverride.ClockAccumulator < PlayerInputOverride.TargetSimulationTick;
					if (flag12)
					{
						PlayerInputOverride.ClockAccumulator += 1U;
						base.player.equipment.tock(base.clock);
						PlayerInputOverride.ClockField.Set(this, PlayerInputOverride.ClockField.Get(this) + 1U);
					}
					bool flag13 = PlayerInputOverride.ClockAccumulator == PlayerInputOverride.TargetSimulationTick && PlayerInputOverride.PendingInputPacketField.Get(this) != null && !Provider.isServer;
					if (flag13)
					{
						ushort num8 = 0;
						byte b3 = 0;
						PlayerInputOverride.KeyFlagsField.instance = this;
						PlayerInputOverride.KeyFlagsField.RefereshFieldValue();
						while ((int)b3 < base.keys.Length)
						{
							bool flag14 = base.keys[(int)b3];
							if (flag14)
							{
								num8 |= PlayerInputOverride.KeyFlagsField.fldValue[(int)b3];
							}
							b3 += 1;
						}
						PlayerInputOverride.PendingInputPacketField.value.keys = num8;
						PlayerInputOverride.PendingInputPacketField.value.primaryAttack = PlayerInputOverride.PendingPrimaryAttackField.Get(this);
						PlayerInputOverride.PendingInputPacketField.value.secondaryAttack = PlayerInputOverride.PendingSecondaryAttackField.Get(this);
						bool flag15 = PlayerInputOverride.PendingInputPacketField.value is DrivingPlayerInputPacket;
						if (flag15)
						{
							DrivingPlayerInputPacket drivingPlayerInputPacket = PlayerInputOverride.PendingInputPacketField.value as DrivingPlayerInputPacket;
							InteractableVehicle vehicle = base.player.movement.getVehicle();
							bool flag16 = vehicle != null;
							if (flag16)
							{
								Transform transform = vehicle.transform;
								bool flag17 = vehicle.asset.engine == EEngine.TRAIN;
								if (flag17)
								{
									drivingPlayerInputPacket.position = PlayerInputOverride.RoadPositionToWorldPosition(vehicle.roadPosition);
								}
								else
								{
									drivingPlayerInputPacket.position = transform.position;
								}
								drivingPlayerInputPacket.rotation = transform.rotation;
								base.player.look.aim.rotation = transform.rotation;
								bool flag18 = MiscConfig.vehicleSpinbot && vehicle != null && !ScreenshotManager.IsSpying;
								if (flag18)
								{
									PlayerInputOverride.VehicleSpinActive = true;
									PlayerInputOverride.VehicleSpinTarget = vehicle;
									Quaternion quaternion;
									switch (MiscConfig.vehicleSpinbotType)
									{
									case DxVehSpinType.Flip180:
									{
										quaternion = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y + ((PlayerInputOverride.VehicleSpinStep == 0) ? 0f : 180f), transform.eulerAngles.z);
										PlayerInputOverride.VehicleSpinStep += 1;
										bool flag19 = PlayerInputOverride.VehicleSpinStep >= 2;
										if (flag19)
										{
											PlayerInputOverride.VehicleSpinStep = 0;
										}
										break;
									}
									case DxVehSpinType.Steps360:
									{
										float[] array = new float[] { 0f, 90f, 180f, 270f };
										quaternion = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y + array[(int)PlayerInputOverride.VehicleSpinStep], transform.eulerAngles.z);
										PlayerInputOverride.VehicleSpinStep += 1;
										bool flag20 = PlayerInputOverride.VehicleSpinStep >= 4;
										if (flag20)
										{
											PlayerInputOverride.VehicleSpinStep = 0;
										}
										break;
									}
									case DxVehSpinType.Continuous:
										PlayerInputOverride.VehicleSpinAngle += MiscConfig.vehicleSpinbotSpeed * Time.fixedDeltaTime;
										while (PlayerInputOverride.VehicleSpinAngle > 360f)
										{
											PlayerInputOverride.VehicleSpinAngle -= 360f;
										}
										quaternion = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y + PlayerInputOverride.VehicleSpinAngle, transform.eulerAngles.z);
										break;
									case DxVehSpinType.Jitter:
										quaternion = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y + UnityEngine.Random.Range(0f, 360f), transform.eulerAngles.z);
										break;
									default:
										quaternion = transform.rotation;
										break;
									}
									drivingPlayerInputPacket.rotation = quaternion;
									base.player.look.aim.rotation = quaternion;
									PlayerInputOverride.VehicleServerRotation = quaternion;
									PlayerInputOverride.VehicleServerPosition = transform.position;
								}
								else
								{
									PlayerInputOverride.VehicleSpinActive = false;
									PlayerInputOverride.VehicleSpinTarget = null;
								}
								drivingPlayerInputPacket.speed = vehicle.ReplicatedSpeed;
								drivingPlayerInputPacket.forwardVelocity = vehicle.ReplicatedForwardVelocity;
								bool flag21 = MiscConfig.vehicleMouseMove && PlayerMovementHook.IsDrivingVehicle && !MiscConfig.vehicleNoclip;
								if (flag21)
								{
									drivingPlayerInputPacket.steeringInput = VehicleBehaviour.mouseSteerInput;
								}
								else
								{
									drivingPlayerInputPacket.steeringInput = vehicle.ReplicatedSteeringInput;
								}
								drivingPlayerInputPacket.velocityInput = vehicle.ReplicatedVelocityInput;
							}
						}
						bool isConnected = Provider.isConnected;
						if (isConnected)
						{
							PlayerInputOverride.SendInputs.Invoke(base.GetNetId(), ENetReliability.Reliable, delegate(NetPakWriter writer)
							{
								bool flag22 = PlayerInputOverride.PendingInputPacketField.value is DrivingPlayerInputPacket;
								if (flag22)
								{
									writer.WriteBit(true);
								}
								else
								{
									writer.WriteBit(false);
								}
								PlayerInputOverride.PendingInputPacketField.value.write(writer);
							});
						}
					}
					PlayerInputOverride.FixedUpdateCounter += 1U;
				}
			}
		}
	}
	public void ComputeMoveOverride(out float yaw, out int pitch, out byte horizontal, out byte vertical)
	{
		switch (MiscConfig.moveType)
		{
		case MoveType.WalkLeft:
			yaw = base.player.look.yaw - 90f;
			pitch = (int)base.player.look.pitch;
			horizontal = base.player.movement.vertical;
			vertical = PlayerInputOverride.InvertMoveInput(base.player.movement.horizontal);
			break;
		case MoveType.WalkRight:
			yaw = base.player.look.yaw - 270f;
			pitch = (int)base.player.look.pitch;
			horizontal = PlayerInputOverride.InvertMoveInput(base.player.movement.vertical);
			vertical = base.player.movement.horizontal;
			break;
		case MoveType.WalkBack:
			yaw = base.player.look.yaw - 180f;
			pitch = (int)base.player.look.pitch;
			horizontal = PlayerInputOverride.InvertMoveInput(base.player.movement.horizontal);
			vertical = PlayerInputOverride.InvertMoveInput(base.player.movement.vertical);
			break;
		case MoveType.TwoTactSpin:
		{
			yaw = ((PlayerInputOverride.SpinPhaseCounter == 0) ? base.player.look.yaw : (base.player.look.yaw - 180f));
			pitch = ((PlayerInputOverride.SpinPhaseCounter == 0) ? UnityEngine.Random.Range(0, 90) : UnityEngine.Random.Range(90, 180));
			horizontal = ((PlayerInputOverride.SpinPhaseCounter == 0) ? base.player.movement.horizontal : PlayerInputOverride.InvertMoveInput(base.player.movement.horizontal));
			vertical = ((PlayerInputOverride.SpinPhaseCounter == 0) ? base.player.movement.vertical : PlayerInputOverride.InvertMoveInput(base.player.movement.vertical));
			PlayerInputOverride.SpinPhaseCounter += 1;
			bool flag = PlayerInputOverride.SpinPhaseCounter >= 2;
			if (flag)
			{
				PlayerInputOverride.SpinPhaseCounter = 0;
			}
			break;
		}
		case MoveType.FourTactSpin:
		{
			PlayerInputOverride.SpinPhaseCounter += 1;
			bool flag2 = PlayerInputOverride.SpinPhaseCounter == 5 || PlayerInputOverride.SpinPhaseCounter == 0;
			if (flag2)
			{
				PlayerInputOverride.SpinPhaseCounter = 1;
			}
			switch (PlayerInputOverride.SpinPhaseCounter)
			{
			case 1:
				yaw = base.player.look.yaw;
				pitch = 0;
				horizontal = base.player.movement.horizontal;
				vertical = base.player.movement.vertical;
				break;
			case 2:
				yaw = base.player.look.yaw - 90f;
				pitch = 0;
				horizontal = base.player.movement.vertical;
				vertical = PlayerInputOverride.InvertMoveInput(base.player.movement.horizontal);
				break;
			case 3:
				yaw = base.player.look.yaw - 180f;
				pitch = 180;
				horizontal = PlayerInputOverride.InvertMoveInput(base.player.movement.horizontal);
				vertical = PlayerInputOverride.InvertMoveInput(base.player.movement.vertical);
				break;
			default:
				yaw = base.player.look.yaw - 270f;
				pitch = 180;
				horizontal = PlayerInputOverride.InvertMoveInput(base.player.movement.vertical);
				vertical = base.player.movement.horizontal;
				break;
			}
			break;
		}
		case MoveType.Jitter:
			yaw = UnityEngine.Random.Range(0f, 360f);
			pitch = UnityEngine.Random.Range(0, 180);
			PlayerInputOverride.RemapMovementForYaw(base.player.movement.horizontal, base.player.movement.vertical, base.player.look.yaw, yaw, out horizontal, out vertical);
			break;
		case MoveType.Continuous:
			PlayerInputOverride.PlayerSpinAngle += MiscConfig.playerSpinbotSpeed * Time.fixedDeltaTime;
			while (PlayerInputOverride.PlayerSpinAngle > 360f)
			{
				PlayerInputOverride.PlayerSpinAngle -= 360f;
			}
			while (PlayerInputOverride.PlayerSpinAngle < 0f)
			{
				PlayerInputOverride.PlayerSpinAngle += 360f;
			}
			yaw = base.player.look.yaw + PlayerInputOverride.PlayerSpinAngle;
			pitch = (int)base.player.look.pitch;
			PlayerInputOverride.RemapMovementForYaw(base.player.movement.horizontal, base.player.movement.vertical, base.player.look.yaw, yaw, out horizontal, out vertical);
			break;
		case MoveType.RandomPitch:
			yaw = base.player.look.yaw;
			pitch = UnityEngine.Random.Range(0, 180);
			horizontal = base.player.movement.horizontal;
			vertical = base.player.movement.vertical;
			break;
		case MoveType.AntiAim:
			yaw = this.GetAntiAimYaw();
			pitch = (int)base.player.look.pitch;
			PlayerInputOverride.RemapMovementForYaw(base.player.movement.horizontal, base.player.movement.vertical, base.player.look.yaw, yaw, out horizontal, out vertical);
			break;
		default:
			pitch = (int)base.player.look.pitch;
			yaw = base.player.look.yaw;
			horizontal = base.player.movement.horizontal;
			vertical = base.player.movement.vertical;
			break;
		}
		bool flag3 = MiscConfig.playerSpinbotRandomPitch && MiscConfig.moveType != MoveType.Jitter && MiscConfig.moveType != MoveType.RandomPitch && MiscConfig.moveType != MoveType.TwoTactSpin && MiscConfig.moveType != MoveType.FourTactSpin;
		if (flag3)
		{
			pitch = UnityEngine.Random.Range(0, 180);
		}
		bool flag4;
		if (Player.player.stance.stance != EPlayerStance.SWIM)
		{
			EPlayerStance stance = Player.player.stance.stance;
			flag4 = false;
		}
		else
		{
			flag4 = true;
		}
		bool flag5 = flag4;
		if (flag5)
		{
			pitch = (int)base.player.look.pitch;
		}
	}
	private float GetAntiAimYaw()
	{
		float playerSpinbotAntiAimDistance = MiscConfig.playerSpinbotAntiAimDistance;
		Vector3 position = base.transform.position;
		SteamPlayer steamPlayer = null;
		float num = float.MaxValue;
		bool flag = Provider.clients != null;
		if (flag)
		{
			foreach (SteamPlayer steamPlayer2 in Provider.clients)
			{
				bool flag2 = steamPlayer2 == null || steamPlayer2.player == null || steamPlayer2.player.life == null || steamPlayer2.player.life.isDead;
				if (!flag2)
				{
					bool isLocalPlayer = steamPlayer2.player.channel.IsLocalPlayer;
					if (!isLocalPlayer)
					{
						float num2 = Vector3.Distance(position, steamPlayer2.player.transform.position);
						bool flag3 = num2 <= playerSpinbotAntiAimDistance && num2 < num;
						if (flag3)
						{
							num = num2;
							steamPlayer = steamPlayer2;
						}
					}
				}
			}
		}
		bool flag4 = steamPlayer == null;
		float num3;
		if (flag4)
		{
			num3 = base.player.look.yaw;
		}
		else
		{
			Vector3 vector = position - steamPlayer.player.transform.position;
			vector.y = 0f;
			bool flag5 = vector.sqrMagnitude < 0.001f;
			if (flag5)
			{
				num3 = base.player.look.yaw;
			}
			else
			{
				num3 = Mathf.Atan2(vector.x, vector.z) * 57.29578f;
			}
		}
		bool flag6 = !PlayerInputOverride.AntiAimSmoothInitialized;
		if (flag6)
		{
			PlayerInputOverride.AntiAimSmoothedYaw = num3;
			PlayerInputOverride.AntiAimSmoothInitialized = true;
		}
		else
		{
			PlayerInputOverride.AntiAimSmoothedYaw = Mathf.LerpAngle(PlayerInputOverride.AntiAimSmoothedYaw, num3, 15f * Time.fixedDeltaTime);
		}
		return PlayerInputOverride.AntiAimSmoothedYaw;
	}
	public static byte InvertMoveInput(byte input)
	{
		bool flag = input == 2;
		byte b;
		if (flag)
		{
			b = 0;
		}
		else
		{
			bool flag2 = input == 0;
			if (flag2)
			{
				b = 2;
			}
			else
			{
				b = 1;
			}
		}
		return b;
	}
	public static void RemapMovementForYaw(byte origHorizontal, byte origVertical, float origYaw, float spinbotYaw, out byte newHorizontal, out byte newVertical)
	{
		float num = (float)origHorizontal - 1f;
		float num2 = (float)origVertical - 1f;
		bool flag = num == 0f && num2 == 0f;
		if (flag)
		{
			newHorizontal = 1;
			newVertical = 1;
		}
		else
		{
			float num3;
			for (num3 = (spinbotYaw - origYaw) * 0.017453292f; num3 > 3.1415927f; num3 -= 6.2831855f)
			{
			}
			while (num3 < -3.1415927f)
			{
				num3 += 6.2831855f;
			}
			float num4 = Mathf.Cos(num3);
			float num5 = Mathf.Sin(num3);
			float num6 = num * num4 - num2 * num5;
			float num7 = num * num5 + num2 * num4;
			newHorizontal = (byte)Mathf.Clamp(Mathf.RoundToInt(num6) + 1, 0, 2);
			newVertical = (byte)Mathf.Clamp(Mathf.RoundToInt(num7) + 1, 0, 2);
		}
	}
	internal void UpdateAttackInputs(out EAttackInputFlags primaryAttack, out EAttackInputFlags secondaryAttack)
	{
		primaryAttack = EAttackInputFlags.None;
		secondaryAttack = EAttackInputFlags.None;
		bool flag = PlayerInputOverride.PrimaryPressedField.Get(Player.player.equipment) || PlayerInputOverride.PrimaryHeldField.Get(Player.player.equipment);
		if (flag)
		{
			primaryAttack |= EAttackInputFlags.Start;
		}
		bool flag2 = PlayerInputOverride.PrimaryReleasedField.Get(Player.player.equipment);
		if (flag2)
		{
			primaryAttack |= EAttackInputFlags.Stop;
		}
		bool flag3 = PlayerInputOverride.SecondaryPressedField.Get(Player.player.equipment) || PlayerInputOverride.SecondaryHeldField.Get(Player.player.equipment);
		if (flag3)
		{
			secondaryAttack |= EAttackInputFlags.Start;
		}
		bool flag4 = PlayerInputOverride.SecondaryReleasedField.Get(Player.player.equipment);
		if (flag4)
		{
			secondaryAttack |= EAttackInputFlags.Stop;
		}
		PlayerInputOverride.PrimaryPressedField.Set(Player.player.equipment, false);
		PlayerInputOverride.PrimaryReleasedField.Set(Player.player.equipment, false);
		PlayerInputOverride.SecondaryPressedField.Set(Player.player.equipment, false);
		PlayerInputOverride.SecondaryReleasedField.Set(Player.player.equipment, false);
	}
	internal static Vector3 RoadPositionToWorldPosition(float roadPosition)
	{
		bool flag = roadPosition >= 16384f;
		Vector3 vector;
		if (flag)
		{
			vector = new Vector3(4096f, 4096f, roadPosition - 20480f);
		}
		else
		{
			bool flag2 = roadPosition >= 8192f;
			if (flag2)
			{
				vector = new Vector3(4096f, roadPosition - 12288f, -4096f);
			}
			else
			{
				vector = new Vector3(roadPosition - 4096f, -4096f, -4096f);
			}
		}
		return vector;
	}
	private static readonly ServerInstanceMethod SendInputs = ServerInstanceMethod.Get(typeof(PlayerInput), "ReceiveInputs");
	private static Type ClientMovementInputType = ReflectionUtil.FindTypeInUnturned("ClientMovementInput");
	private static ReflectedField<uint> ClientInputFrameNumberField = new ReflectedField<uint>(PlayerInputOverride.ClientMovementInputType, "frameNumber", BindingFlags.Instance | BindingFlags.Public);
	private static ReflectedField<bool> ClientInputCrouchField = new ReflectedField<bool>(PlayerInputOverride.ClientMovementInputType, "crouch", BindingFlags.Instance | BindingFlags.Public);
	private static ReflectedField<bool> ClientInputProneField = new ReflectedField<bool>(PlayerInputOverride.ClientMovementInputType, "prone", BindingFlags.Instance | BindingFlags.Public);
	private static ReflectedField<int> ClientInputXField = new ReflectedField<int>(PlayerInputOverride.ClientMovementInputType, "input_x", BindingFlags.Instance | BindingFlags.Public);
	private static ReflectedField<int> ClientInputYField = new ReflectedField<int>(PlayerInputOverride.ClientMovementInputType, "input_y", BindingFlags.Instance | BindingFlags.Public);
	private static ReflectedField<bool> ClientInputJumpField = new ReflectedField<bool>(PlayerInputOverride.ClientMovementInputType, "jump", BindingFlags.Instance | BindingFlags.Public);
	private static ReflectedField<bool> ClientInputSprintField = new ReflectedField<bool>(PlayerInputOverride.ClientMovementInputType, "sprint", BindingFlags.Instance | BindingFlags.Public);
	private static ReflectedField<Quaternion> ClientInputRotationField = new ReflectedField<Quaternion>(PlayerInputOverride.ClientMovementInputType, "rotation", BindingFlags.Instance | BindingFlags.Public);
	private static ReflectedField<Quaternion> ClientInputAimRotationField = new ReflectedField<Quaternion>(PlayerInputOverride.ClientMovementInputType, "aimRotation", BindingFlags.Instance | BindingFlags.Public);
	private static ReflectedField<bool> PrimaryPressedField = new ReflectedField<bool>(typeof(PlayerEquipment), "localWasPrimaryPressedBetweenSimulationFrames", BindingFlags.Instance | BindingFlags.NonPublic);
	private static ReflectedField<bool> PrimaryHeldField = new ReflectedField<bool>(typeof(PlayerEquipment), "localWasPrimaryHeldLastFrame", BindingFlags.Instance | BindingFlags.NonPublic);
	private static ReflectedField<bool> PrimaryReleasedField = new ReflectedField<bool>(typeof(PlayerEquipment), "localWasPrimaryReleasedBetweenSimulationFrames", BindingFlags.Instance | BindingFlags.NonPublic);
	private static ReflectedField<bool> SecondaryPressedField = new ReflectedField<bool>(typeof(PlayerEquipment), "localWasSecondaryPressedBetweenSimulationFrames", BindingFlags.Instance | BindingFlags.NonPublic);
	private static ReflectedField<bool> SecondaryHeldField = new ReflectedField<bool>(typeof(PlayerEquipment), "localWasSecondaryHeldLastFrame", BindingFlags.Instance | BindingFlags.NonPublic);
	private static ReflectedField<bool> SecondaryReleasedField = new ReflectedField<bool>(typeof(PlayerEquipment), "localWasSecondaryReleasedBetweenSimulationFrames", BindingFlags.Instance | BindingFlags.NonPublic);
	private static ReflectedField<uint> ClockField = new ReflectedField<uint>(typeof(PlayerInput), "_clock", BindingFlags.Instance | BindingFlags.NonPublic);
	private static ReflectedField<uint> SimulationField = new ReflectedField<uint>(typeof(PlayerInput), "_simulation", BindingFlags.Instance | BindingFlags.NonPublic);
	private static ReflectedField<float> TickField = new ReflectedField<float>(typeof(PlayerInput), "_tick", BindingFlags.Instance | BindingFlags.NonPublic);
	private static ReflectedField<ushort[]> KeyFlagsField = new ReflectedField<ushort[]>(typeof(PlayerInput), "flags", BindingFlags.Instance | BindingFlags.NonPublic);
	private static ReflectedField<bool> IsDismissedField = new ReflectedField<bool>(typeof(PlayerInput), "isDismissed", BindingFlags.Instance | BindingFlags.NonPublic);
	private static ReflectedField<bool> PendingResimulationField = new ReflectedField<bool>(typeof(PlayerInput), "clientHasPendingResimulation", BindingFlags.Instance | BindingFlags.NonPublic);
	private static ReflectedField<bool> SteadyAimField = new ReflectedField<bool>(typeof(PlayerStance), "localWantsToSteadyAim", BindingFlags.Instance | BindingFlags.NonPublic);
	private static ReflectedField<EFiremode> GunFiremodeField = new ReflectedField<EFiremode>(typeof(UseableGun), "firemode", BindingFlags.Instance | BindingFlags.NonPublic);
	private static ReflectedField<CharacterAnimator> ThirdPersonAnimatorField = new ReflectedField<CharacterAnimator>(typeof(PlayerAnimator), "thirdAnimator", BindingFlags.Instance | BindingFlags.NonPublic);
	private static ReflectedField<PlayerInputPacket> PendingInputPacketField = new ReflectedField<PlayerInputPacket>(typeof(PlayerInput), "clientPendingInput", BindingFlags.Instance | BindingFlags.NonPublic);
	private static ReflectedField<EAttackInputFlags> PendingPrimaryAttackField = new ReflectedField<EAttackInputFlags>(typeof(PlayerInput), "pendingPrimaryAttackInput", BindingFlags.Instance | BindingFlags.NonPublic);
	private static ReflectedField<EAttackInputFlags> PendingSecondaryAttackField = new ReflectedField<EAttackInputFlags>(typeof(PlayerInput), "pendingSecondaryAttackInput", BindingFlags.Instance | BindingFlags.NonPublic);
	private static FieldRef ClientInputHistoryList = new FieldRef(typeof(PlayerInput), "clientInputHistory", BindingFlags.Instance | BindingFlags.NonPublic);
	private static ReflectedMethod ClientInputHistoryAddMethod = new ReflectedMethod(typeof(PlayerInput).GetField("clientInputHistory", BindingFlags.Instance | BindingFlags.NonPublic).FieldType, "Add", BindingFlags.Instance | BindingFlags.Public);
	private static ReflectedMethod ClientResimulateMethod = new ReflectedMethod(typeof(PlayerInput), "ClientResimulate", BindingFlags.Instance | BindingFlags.NonPublic);
	private static uint TargetSimulationTick = 0U;
	private static uint FixedUpdateCounter = 0U;
	private static uint ClockAccumulator = 0U;
	public static int SharedStateInt = 0;
	public static byte SpinPhaseCounter;
	public static byte VehicleSpinStep;
	private static float VehicleSpinAngle;
	public static Quaternion VehicleServerRotation = Quaternion.identity;
	public static Vector3 VehicleServerPosition = Vector3.zero;
	public static bool VehicleSpinActive = false;
	public static InteractableVehicle VehicleSpinTarget = null;
	public static float PlayerServerYaw = 0f;
	public static float PlayerServerPitch = 0f;
	public static Vector3 PlayerServerPosition = Vector3.zero;
	public static bool PlayerSpinActive = false;
	private static float PlayerSpinAngle = 0f;
	private static float AntiAimSmoothedYaw = 0f;
	private static bool AntiAimSmoothInitialized = false;
}
