using System;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SDG.Unturned;
using UnityEngine;

namespace gatyware
{
	public static class WeaponMods
	{
		[DllImport("kernel32.dll")]
		private static extern bool VirtualProtect(IntPtr addr, int size, uint prot, out uint old);

		public static void InitScopeHook()
		{
			if (WeaponMods._ok1)
			{
				return;
			}
			try
			{
				WeaponMods._m1 = typeof(PlayerUI).GetMethod("updateScope", BindingFlags.Static | BindingFlags.Public);
				if (!(WeaponMods._m1 == null))
				{
					WeaponMods.Hook(WeaponMods._m1, "HookScope", ref WeaponMods._p1, WeaponMods._s1);
					WeaponMods._ok1 = true;
					Runtime.Trace("wpn: scope hook OK");
				}
			}
			catch (Exception ex)
			{
				Runtime.Trace("wpn: scope err " + ex.Message);
			}
		}

		private static void HookScope(bool isScoped)
		{
			if (State.DisableScope && !State.IsSpying)
			{
				isScoped = false;
			}
			uint prot;
			WeaponMods.VirtualProtect(WeaponMods._p1, 14, 64U, out prot);
			Marshal.Copy(WeaponMods._s1, 0, WeaponMods._p1, 14);
			WeaponMods.VirtualProtect(WeaponMods._p1, 14, prot, out prot);
			try
			{
				WeaponMods._m1.Invoke(null, new object[]
				{
					isScoped
				});
			}
			finally
			{
				WeaponMods.WriteJmp(WeaponMods._p1, WeaponMods.Hm("HookScope"));
			}
		}

		public static void InitBinoHook()
		{
			if (WeaponMods._ok2)
			{
				return;
			}
			try
			{
				WeaponMods._m2 = typeof(PlayerUI).GetMethod("updateBinoculars", BindingFlags.Static | BindingFlags.Public);
				if (!(WeaponMods._m2 == null))
				{
					WeaponMods.Hook(WeaponMods._m2, "HookBino", ref WeaponMods._p2, WeaponMods._s2);
					WeaponMods._ok2 = true;
					Runtime.Trace("wpn: bino hook OK");
				}
			}
			catch (Exception ex)
			{
				Runtime.Trace("wpn: bino err " + ex.Message);
			}
		}

		private static void HookBino(bool isBino)
		{
			if (State.DisableBino && !State.IsSpying)
			{
				isBino = false;
			}
			uint prot;
			WeaponMods.VirtualProtect(WeaponMods._p2, 14, 64U, out prot);
			Marshal.Copy(WeaponMods._s2, 0, WeaponMods._p2, 14);
			WeaponMods.VirtualProtect(WeaponMods._p2, 14, prot, out prot);
			try
			{
				WeaponMods._m2.Invoke(null, new object[]
				{
					isBino
				});
			}
			finally
			{
				WeaponMods.WriteJmp(WeaponMods._p2, WeaponMods.Hm("HookBino"));
			}
		}

		public static void InitSpreadHook()
		{
			if (WeaponMods._ok3)
			{
				return;
			}
			try
			{
				Type type = null;
				string[] array = new string[]
				{
					"RandomEx",
					"UnityEx.RandomEx",
					"SDG.Unturned.RandomEx"
				};
				foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
				{
					foreach (string name in array)
					{
						type = assembly.GetType(name);
						if (type != null)
						{
							break;
						}
					}
					if (type != null)
					{
						break;
					}
					try
					{
						foreach (Type type2 in assembly.GetTypes())
						{
							if (type2.Name == "RandomEx")
							{
								type = type2;
								break;
							}
						}
					}
					catch
					{
					}
					if (type != null)
					{
						break;
					}
				}
				if (type == null)
				{
					WeaponMods._ok3 = true;
					Runtime.Trace("wpn: RandomEx not in any assembly (scanned all types)");
				}
				else
				{
					WeaponMods._m3 = type.GetMethod("GetRandomForwardVectorInCone", BindingFlags.Static | BindingFlags.Public);
					if (WeaponMods._m3 == null)
					{
						WeaponMods._ok3 = true;
						Runtime.Trace("wpn: GetRandomForwardVectorInCone not found on " + type.FullName);
					}
					else
					{
						WeaponMods.Hook(WeaponMods._m3, "HookSpread", ref WeaponMods._p3, WeaponMods._s3);
						WeaponMods._ok3 = true;
						Runtime.Trace("wpn: spread hook OK on " + type.FullName);
					}
				}
			}
			catch (Exception ex)
			{
				WeaponMods._ok3 = true;
				Runtime.Trace("wpn: spread err " + ex.Message);
			}
		}

		private static Vector3 HookSpread(float halfAngle)
		{
			if (!State.IsSpying && WeaponMods.LocalFiring)
			{
				halfAngle *= State.SpreadMult;
			}
			uint prot;
			WeaponMods.VirtualProtect(WeaponMods._p3, 14, 64U, out prot);
			Marshal.Copy(WeaponMods._s3, 0, WeaponMods._p3, 14);
			WeaponMods.VirtualProtect(WeaponMods._p3, 14, prot, out prot);
			Vector3 result;
			try
			{
				result = (Vector3)WeaponMods._m3.Invoke(null, new object[]
				{
					halfAngle
				});
			}
			finally
			{
				WeaponMods.WriteJmp(WeaponMods._p3, WeaponMods.Hm("HookSpread"));
			}
			return result;
		}

		public static void InitAimHook()
		{
			if (WeaponMods._ok4)
			{
				return;
			}
			try
			{
				WeaponMods._m4 = typeof(UseableGun).GetMethod("GetInterpolatedAimAlpha", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (!(WeaponMods._m4 == null))
				{
					WeaponMods.Hook(WeaponMods._m4, "HookAim", ref WeaponMods._p4, WeaponMods._s4);
					WeaponMods._ok4 = true;
					Runtime.Trace("wpn: aim hook OK");
				}
			}
			catch (Exception ex)
			{
				Runtime.Trace("wpn: aim err " + ex.Message);
			}
		}

		private static float HookAim(UseableGun self)
		{
			if (State.InstantAim && !State.IsSpying && self.channel.IsLocalPlayer && self.isAiming)
			{
				return 1f;
			}
			uint prot;
			WeaponMods.VirtualProtect(WeaponMods._p4, 14, 64U, out prot);
			Marshal.Copy(WeaponMods._s4, 0, WeaponMods._p4, 14);
			WeaponMods.VirtualProtect(WeaponMods._p4, 14, prot, out prot);
			float result;
			try
			{
				result = (float)WeaponMods._m4.Invoke(self, null);
			}
			finally
			{
				WeaponMods.WriteJmp(WeaponMods._p4, WeaponMods.Hm("HookAim"));
			}
			return result;
		}

		public static void InitRecoilHook()
		{
			if (WeaponMods._ok5)
			{
				return;
			}
			try
			{
				WeaponMods._m5 = typeof(PlayerLook).GetMethod("recoil", BindingFlags.Instance | BindingFlags.Public);
				if (!(WeaponMods._m5 == null))
				{
					WeaponMods.Hook(WeaponMods._m5, "HookRecoil", ref WeaponMods._p5, WeaponMods._s5);
					WeaponMods._ok5 = true;
					Runtime.Trace("wpn: recoil hook OK");
				}
			}
			catch (Exception ex)
			{
				Runtime.Trace("wpn: recoil err " + ex.Message);
			}
		}

		private static void HookRecoil(PlayerLook self, float x, float y, float h, float v)
		{
			if (!State.IsSpying && self.channel.IsLocalPlayer)
			{
				x *= State.RecoilMult;
				y *= State.RecoilMult;
			}
			uint prot;
			WeaponMods.VirtualProtect(WeaponMods._p5, 14, 64U, out prot);
			Marshal.Copy(WeaponMods._s5, 0, WeaponMods._p5, 14);
			WeaponMods.VirtualProtect(WeaponMods._p5, 14, prot, out prot);
			try
			{
				WeaponMods._m5.Invoke(self, new object[]
				{
					x,
					y,
					h,
					v
				});
			}
			finally
			{
				WeaponMods.WriteJmp(WeaponMods._p5, WeaponMods.Hm("HookRecoil"));
			}
		}

		public static void InitMeleeReachHook()
		{
			if (WeaponMods._ok7)
			{
				return;
			}
			try
			{
				WeaponMods._m7 = typeof(UseableMelee).GetMethod("fire", BindingFlags.Instance | BindingFlags.NonPublic);
				if (!(WeaponMods._m7 == null))
				{
					WeaponMods.Hook(WeaponMods._m7, "HookMeleeFire", ref WeaponMods._p7, WeaponMods._s7);
					WeaponMods._ok7 = true;
					Runtime.Trace("wpn: melee reach hook OK");
				}
			}
			catch (Exception ex)
			{
				Runtime.Trace("wpn: melee reach err " + ex.Message);
			}
		}

		private static void HookMeleeFire(UseableMelee self)
		{
			float num = 0f;
			bool flag = false;
			if (State.MeleeReach && !State.IsSpying && self.channel.IsLocalPlayer && self.equippedMeleeAsset != null)
			{
				num = self.equippedMeleeAsset.range;
				self.equippedMeleeAsset.range = num + State.MeleeReachExtra;
				flag = true;
			}
			if (State.SilentAimOn && !State.IsSpying && self.channel.IsLocalPlayer)
			{
				SilentAim.EnableRaycastHook();
			}
			try
			{
				uint prot;
				WeaponMods.VirtualProtect(WeaponMods._p7, 14, 64U, out prot);
				Marshal.Copy(WeaponMods._s7, 0, WeaponMods._p7, 14);
				WeaponMods.VirtualProtect(WeaponMods._p7, 14, prot, out prot);
				try
				{
					WeaponMods._m7.Invoke(self, null);
				}
				finally
				{
					WeaponMods.WriteJmp(WeaponMods._p7, WeaponMods.Hm("HookMeleeFire"));
				}
			}
			finally
			{
				SilentAim.DisableRaycastHook();
				if (flag)
				{
					self.equippedMeleeAsset.range = num;
				}
			}
		}

		public static void InitFireHook()
		{
			if (WeaponMods._ok6)
			{
				return;
			}
			try
			{
				WeaponMods._m6 = typeof(UseableGun).GetMethod("fire", BindingFlags.Instance | BindingFlags.NonPublic);
				if (!(WeaponMods._m6 == null))
				{
					WeaponMods._bulletsListField = typeof(UseableGun).GetField("bullets", BindingFlags.Instance | BindingFlags.NonPublic);
					WeaponMods.Hook(WeaponMods._m6, "HookFire", ref WeaponMods._p6, WeaponMods._s6);
					WeaponMods._ok6 = true;
					Runtime.Trace("wpn: fire hook OK");
				}
			}
			catch (Exception ex)
			{
				Runtime.Trace("wpn: fire err " + ex.Message);
			}
		}

		private static void HookFire(UseableGun self)
		{
			int num = 0;
			IList list = null;
			if (State.BulletEsp && WeaponMods._bulletsListField != null)
			{
				list = (WeaponMods._bulletsListField.GetValue(self) as IList);
				if (list != null)
				{
					num = list.Count;
				}
			}
			bool flag = self.channel != null && self.channel.IsLocalPlayer;
			if (flag)
			{
				WeaponMods.LocalFiring = true;
			}
			uint prot;
			WeaponMods.VirtualProtect(WeaponMods._p6, 14, 64U, out prot);
			Marshal.Copy(WeaponMods._s6, 0, WeaponMods._p6, 14);
			WeaponMods.VirtualProtect(WeaponMods._p6, 14, prot, out prot);
			try
			{
				WeaponMods._m6.Invoke(self, null);
			}
			finally
			{
				WeaponMods.WriteJmp(WeaponMods._p6, WeaponMods.Hm("HookFire"));
			}
			if (flag)
			{
				WeaponMods.LocalFiring = false;
			}
			if (list != null && State.BulletEsp)
			{
				for (int i = num; i < list.Count; i++)
				{
					BulletInfo bulletInfo = list[i] as BulletInfo;
					if (bulletInfo != null)
					{
						bool flag2 = self.channel != null && self.channel.IsLocalPlayer;
						if ((!flag2 || State.BulletSelf) && (flag2 || State.BulletOthers))
						{
							Vector3 normalized = bulletInfo.velocity.normalized;
							float num2 = 512f;
							if (self.equippedGunAsset != null)
							{
								num2 = self.equippedGunAsset.range;
							}
							RaycastHit raycastHit = default;
							Vector3 hit;
							if (Physics.Raycast(bulletInfo.origin, normalized, out raycastHit, num2, RayMasks.DAMAGE_CLIENT))
							{
								hit = raycastHit.point;
							}
							else
							{
								hit = bulletInfo.origin + normalized * num2;
							}
							Color col = flag2 ? State.BulletSelfColor : State.BulletOtherColor;
							ESP.AddBulletGhost(bulletInfo.origin, hit, col);
						}
					}
				}
			}
		}

		public static void InitPunchReachHook()
		{
			if (WeaponMods._ok8)
			{
				return;
			}
			try
			{
				WeaponMods._m8 = typeof(PlayerEquipment).GetMethod("punch", BindingFlags.Instance | BindingFlags.NonPublic);
				if (!(WeaponMods._m8 == null))
				{
					WeaponMods._playPunchAudio = typeof(PlayerEquipment).GetMethod("PlayPunchAudioClip", BindingFlags.Instance | BindingFlags.NonPublic);
					WeaponMods.Hook(WeaponMods._m8, "HookPunch", ref WeaponMods._p8, WeaponMods._s8);
					WeaponMods._ok8 = true;
					Runtime.Trace("wpn: punch reach hook OK");
				}
			}
			catch (Exception ex)
			{
				Runtime.Trace("wpn: punch reach err " + ex.Message);
			}
		}

		private static void HookPunch(PlayerEquipment self, EPlayerPunch mode)
		{
			if (State.MeleeReach && !State.IsSpying && self.channel.IsLocalPlayer)
			{
				float num = 1.75f + State.MeleeReachExtra;
				if (num > 6f)
				{
					num = 6f;
				}
				WeaponMods.PunchRangeOverride = num;
			}
			if (State.SilentAimOn && !State.IsSpying && self.channel.IsLocalPlayer)
			{
				SilentAim.EnableRaycastHook();
			}
			uint prot;
			WeaponMods.VirtualProtect(WeaponMods._p8, 14, 64U, out prot);
			Marshal.Copy(WeaponMods._s8, 0, WeaponMods._p8, 14);
			WeaponMods.VirtualProtect(WeaponMods._p8, 14, prot, out prot);
			try
			{
				WeaponMods._m8.Invoke(self, new object[]
				{
					mode
				});
			}
			finally
			{
				WeaponMods.WriteJmp(WeaponMods._p8, WeaponMods.Hm("HookPunch"));
			}
			SilentAim.DisableRaycastHook();
			WeaponMods.PunchRangeOverride = 0f;
		}

		public static void InitBallisticsHook()
		{
			if (WeaponMods._ok9)
			{
				return;
			}
			try
			{
				WeaponMods._m9 = typeof(UseableGun).GetMethod("ballistics", BindingFlags.Instance | BindingFlags.NonPublic);
				if (!(WeaponMods._m9 == null))
				{
					WeaponMods._bulletGravField = typeof(ItemGunAsset).GetField("bulletGravityMultiplier", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					WeaponMods.Hook(WeaponMods._m9, "HookBallistics", ref WeaponMods._p9, WeaponMods._s9);
					WeaponMods._ok9 = true;
					Runtime.Trace("wpn: ballistics hook OK");
				}
			}
			catch (Exception ex)
			{
				Runtime.Trace("wpn: ballistics err " + ex.Message);
			}
		}

		private static void HookBallistics(UseableGun self)
		{
			float num = 0f;
			float range = 0f;
			bool flag = false;
			bool flag2 = false;
			if (!State.IsSpying && self.channel.IsLocalPlayer && self.equippedGunAsset != null)
			{
				if ((State.NoBallistics || State.SilentAimOn) && WeaponMods._bulletGravField != null)
				{
					num = (float)WeaponMods._bulletGravField.GetValue(self.equippedGunAsset);
					WeaponMods._bulletGravField.SetValue(self.equippedGunAsset, 0f);
					flag = true;
				}
				if (State.ExtendBallisticRange)
				{
					int ballisticSteps = (int)self.equippedGunAsset.ballisticSteps;
					float ballisticTravel = self.equippedGunAsset.ballisticTravel;
					range = self.equippedGunAsset.range;
					self.equippedGunAsset.range = ballisticTravel * (float)(ballisticSteps + State.ExtraBallisticSteps) + 4f;
					flag2 = true;
					if (Provider.modeConfigData.Gameplay.Ballistics && WeaponMods._bulletsListField != null)
					{
						try
						{
							IList list = WeaponMods._bulletsListField.GetValue(self) as IList;
							if (list != null && list.Count > 0)
							{
								if (WeaponMods._bulletVelProp == null)
								{
									WeaponMods._bulletVelProp = typeof(BulletInfo).GetProperty("velocity", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
								}
								if (WeaponMods._bulletVelProp != null)
								{
									float num2 = ballisticTravel * (float)State.ExtraBallisticSteps / 0.02f;
									for (int i = 0; i < list.Count; i++)
									{
										BulletInfo bulletInfo = list[i] as BulletInfo;
										if (bulletInfo != null && (int)(bulletInfo.steps + 1) >= ballisticSteps)
										{
											Vector3 velocity = bulletInfo.velocity;
											float magnitude = velocity.magnitude;
											if (magnitude > 0.01f)
											{
												Vector3 vector = velocity / magnitude;
												WeaponMods._bulletVelProp.SetValue(bulletInfo, vector * (magnitude + num2));
											}
										}
									}
								}
							}
						}
						catch
						{
						}
					}
				}
			}
			try
			{
				uint prot;
				WeaponMods.VirtualProtect(WeaponMods._p9, 14, 64U, out prot);
				Marshal.Copy(WeaponMods._s9, 0, WeaponMods._p9, 14);
				WeaponMods.VirtualProtect(WeaponMods._p9, 14, prot, out prot);
				try
				{
					WeaponMods._m9.Invoke(self, null);
				}
				finally
				{
					WeaponMods.WriteJmp(WeaponMods._p9, WeaponMods.Hm("HookBallistics"));
				}
			}
			finally
			{
				if (flag)
				{
					WeaponMods._bulletGravField.SetValue(self.equippedGunAsset, num);
				}
				if (flag2)
				{
					self.equippedGunAsset.range = range;
				}
			}
		}

		public static void InitFlinchHook()
		{
			if (WeaponMods._ok10)
			{
				return;
			}
			try
			{
				WeaponMods._m10 = typeof(PlayerLook).GetMethod("FlinchFromDamage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (!(WeaponMods._m10 == null))
				{
					WeaponMods.Hook(WeaponMods._m10, "HookFlinch", ref WeaponMods._p10, WeaponMods._s10);
					WeaponMods._ok10 = true;
					Runtime.Trace("wpn: flinch hook OK");
				}
			}
			catch (Exception ex)
			{
				Runtime.Trace("wpn: flinch err " + ex.Message);
			}
		}

		private static void HookFlinch(PlayerLook self, byte damageAmount, Vector3 worldDirection)
		{
			if (!State.IsSpying && self.channel.IsLocalPlayer && State.DamageFlinchMult < 1f)
			{
				damageAmount = (byte)((float)damageAmount * State.DamageFlinchMult);
			}
			uint prot;
			WeaponMods.VirtualProtect(WeaponMods._p10, 14, 64U, out prot);
			Marshal.Copy(WeaponMods._s10, 0, WeaponMods._p10, 14);
			WeaponMods.VirtualProtect(WeaponMods._p10, 14, prot, out prot);
			try
			{
				WeaponMods._m10.Invoke(self, new object[]
				{
					damageAmount,
					worldDirection
				});
			}
			finally
			{
				WeaponMods.WriteJmp(WeaponMods._p10, WeaponMods.Hm("HookFlinch"));
			}
		}

		public static void Update()
		{
			if (State.IsSpying)
			{
				return;
			}
			Player localPlayer = Player.LocalPlayer;
			if (((localPlayer != null) ? localPlayer.animator : null) == null)
			{
				return;
			}
			if (State.SwayMult < 1f)
			{
				localPlayer.animator.scopeSway *= State.SwayMult;
			}
			if (State.AutoSemiBurst)
			{
				PlayerEquipment equipment = localPlayer.equipment;
				if (((equipment != null) ? equipment.useable : null) is UseableGun)
				{
					UseableGun useableGun = localPlayer.equipment.useable as UseableGun;
					try
					{
						if (WeaponMods._firemodeField == null)
						{
							WeaponMods._firemodeField = typeof(UseableGun).GetField("firemode", BindingFlags.Instance | BindingFlags.NonPublic);
						}
						if (WeaponMods._firemodeField != null && (EFiremode)WeaponMods._firemodeField.GetValue(useableGun) != (EFiremode)2 && InputEx.GetKey(ControlsSettings.primary))
						{
							WeaponMods._semiToggle = !WeaponMods._semiToggle;
							if (WeaponMods._semiToggle)
							{
								useableGun.startPrimary();
							}
						}
					}
					catch
					{
					}
				}
			}
		}

		public static void InitLeaveTimerHook()
		{
			if (WeaponMods._ok11)
			{
				return;
			}
			try
			{
				Type typeFromHandle = typeof(PlayerPauseUI);
				BindingFlags bindingAttr = BindingFlags.Static | BindingFlags.NonPublic;
				WeaponMods._m11 = typeFromHandle.GetMethod("onClickedExitButton", bindingAttr);
				WeaponMods._m12 = typeFromHandle.GetMethod("onClickedQuitButton", bindingAttr);
				if (WeaponMods._m11 != null)
				{
					WeaponMods.Hook(WeaponMods._m11, "HookExit", ref WeaponMods._p11, WeaponMods._s11);
				}
				if (WeaponMods._m12 != null)
				{
					WeaponMods.Hook(WeaponMods._m12, "HookQuit", ref WeaponMods._p12, WeaponMods._s12);
				}
				WeaponMods._ok11 = true;
				Runtime.Trace("wpn: leave timer hook OK");
			}
			catch (Exception ex)
			{
				Runtime.Trace("wpn: leave err " + ex.Message);
			}
		}

		private static void HookExit(SleekButtonIconConfirm button)
		{
			if (!PlayerPauseUI.shouldExitButtonRespectTimer || Time.realtimeSinceStartup - PlayerPauseUI.lastLeave >= Provider.modeConfigData.Gameplay.Timer_Exit || State.IgnoreLeaveTimer)
			{
				Provider.RequestDisconnect("clicked exit button from in-game pause menu");
			}
		}

		private static void HookQuit(SleekButtonIconConfirm button)
		{
			if (!PlayerPauseUI.shouldExitButtonRespectTimer || Time.realtimeSinceStartup - PlayerPauseUI.lastLeave >= Provider.modeConfigData.Gameplay.Timer_Exit || State.IgnoreLeaveTimer)
			{
				Provider.QuitGame("clicked quit from in-game pause menu");
			}
		}

		private static void Hook(MethodInfo target, string hookName, ref IntPtr ptr, byte[] saved)
		{
			RuntimeHelpers.PrepareMethod(target.MethodHandle);
			ptr = target.MethodHandle.GetFunctionPointer();
			MethodInfo method = typeof(WeaponMods).GetMethod(hookName, BindingFlags.Static | BindingFlags.NonPublic);
			RuntimeHelpers.PrepareMethod(method.MethodHandle);
			Marshal.Copy(ptr, saved, 0, 14);
			WeaponMods.WriteJmp(ptr, method.MethodHandle.GetFunctionPointer());
		}

		private static IntPtr Hm(string name)
		{
			return typeof(WeaponMods).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic).MethodHandle.GetFunctionPointer();
		}

		private static void WriteJmp(IntPtr from, IntPtr to)
		{
			uint prot;
			WeaponMods.VirtualProtect(from, 14, 64U, out prot);
			byte[] array = new byte[14];
			array[0] = byte.MaxValue;
			array[1] = 37;
			BitConverter.GetBytes(to.ToInt64()).CopyTo(array, 6);
			Marshal.Copy(array, 0, from, 14);
			WeaponMods.VirtualProtect(from, 14, prot, out prot);
		}

		private static byte[] _s1 = new byte[14];

		private static IntPtr _p1;

		private static MethodInfo _m1;

		private static bool _ok1;

		private static byte[] _s2 = new byte[14];

		private static IntPtr _p2;

		private static MethodInfo _m2;

		private static bool _ok2;

		private static byte[] _s3 = new byte[14];

		private static IntPtr _p3;

		private static MethodInfo _m3;

		private static bool _ok3;

		private static int _spreadLog;

		public static bool LocalFiring;

		private static byte[] _s4 = new byte[14];

		private static IntPtr _p4;

		private static MethodInfo _m4;

		private static bool _ok4;

		private static byte[] _s5 = new byte[14];

		private static IntPtr _p5;

		private static MethodInfo _m5;

		private static bool _ok5;

		private static byte[] _s7 = new byte[14];

		private static IntPtr _p7;

		private static MethodInfo _m7;

		private static bool _ok7;

		private static int _meleeLog;

		private static byte[] _s6 = new byte[14];

		private static IntPtr _p6;

		private static MethodInfo _m6;

		private static bool _ok6;

		private static FieldInfo _bulletsListField;

		private static byte[] _s8 = new byte[14];

		private static IntPtr _p8;

		private static MethodInfo _m8;

		private static bool _ok8;

		private static MethodInfo _playPunchAudio;

		public static float PunchRangeOverride;

		private static byte[] _s9 = new byte[14];

		private static IntPtr _p9;

		private static MethodInfo _m9;

		private static bool _ok9;

		private static FieldInfo _bulletGravField;

		private static PropertyInfo _bulletVelProp;

		private static byte[] _s10 = new byte[14];

		private static IntPtr _p10;

		private static MethodInfo _m10;

		private static bool _ok10;

		private static FieldInfo _firemodeField;

		private static bool _semiToggle;

		private static byte[] _s11 = new byte[14];

		private static byte[] _s12 = new byte[14];

		private static IntPtr _p11;

		private static IntPtr _p12;

		private static MethodInfo _m11;

		private static MethodInfo _m12;

		private static bool _ok11;
	}
}
