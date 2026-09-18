using System;
using System.IO;
using System.Reflection;
using SDG.Unturned;
using UnityEngine;

namespace gatyware
{
	[Obfuscation(Exclude = true)]
	public class Ticker : MonoBehaviour
	{
		private void Update()
		{
			try
			{
				State.UpdateRainbows();

				// StreamProof toggle
				if (State.StreamProofOn && !StreamProofOverlay.IsActive)
				{
					StreamProofOverlay.Start();
				}
				else if (!State.StreamProofOn && StreamProofOverlay.IsActive)
				{
					StreamProofOverlay.Stop();
				}

				if (State.MenuKey != KeyCode.None && Input.GetKeyDown(State.MenuKey))
				{
					State.Open = !State.Open;
					this.SetScrollBlock(State.Open);
					if (!State.Open)
					{
						// Restore look/input immediately on close
						Ticker.SetPlayerLookEnabled(true);
					}
				}
				if (State.Open)
				{
					// Force cursor free every Update frame — PlayerLook can re-lock it
					Cursor.lockState = CursorLockMode.None;
					Cursor.visible = true;
					if (PlayerUI.window != null)
						PlayerUI.window.showCursor = true;
					Ticker.SetPlayerLookEnabled(false);
				}
				else if (Ticker._lookWasDisabled)
				{
					Ticker.SetPlayerLookEnabled(true);
				}
				if (State.FreeCamKey != KeyCode.None && Input.GetKeyDown(State.FreeCamKey) && !State.Open)
				{
					State.FreeCamOn = !State.FreeCamOn;
					if (State.FreeCamOn)
					{
						FreeCam.Enable();
					}
					else
					{
						FreeCam.Disable();
					}
				}
				bool flag = State.FreeCamOn && !State.IsSpying && Player.LocalPlayer != null;
				if (!flag && FreeCam.Instance != null)
				{
					FreeCam.Disable();
				}
				if (flag && FreeCam.Instance == null)
				{
					FreeCam.Enable();
				}
				if (State.VehicleFlyKey != KeyCode.None && Input.GetKeyDown(State.VehicleFlyKey) && !State.Open)
				{
					State.VehicleFlyOn = !State.VehicleFlyOn;
				}
				if (State.VehicleDamageKey != KeyCode.None && Input.GetKeyDown(State.VehicleDamageKey) && !State.Open)
				{
					State.VehicleDamageOff = !State.VehicleDamageOff;
				}
				VehicleFly.UpdateInput();
			}
			catch (Exception ex)
			{
				Runtime.Trace("tick err: " + ex.Message);
			}
			if (State.ChatSpamming && !State.IsSpying && !string.IsNullOrEmpty(State.SpamText) && Provider.isConnected)
			{
				this._spamTimer += Time.deltaTime;
				if (this._spamTimer < State.ChatSpamDelay)
				{
					goto IL_166;
				}
				this._spamTimer = 0f;
				try
				{
					ChatManager.sendChat(0, State.SpamText);
					goto IL_166;
				}
				catch
				{
					goto IL_166;
				}
			}
			this._spamTimer = 0f;
			IL_166:
			try
			{
				Aimbot.TriggerTick();
			}
			catch
			{
			}
			try
			{
				Reach.InitInteractHook();
			}
			catch (Exception ex2)
			{
				Runtime.Trace("reach1 err: " + ex2.Message);
			}
			try
			{
				Reach.InitBarricadeHook();
			}
			catch (Exception ex3)
			{
				Runtime.Trace("reach2 err: " + ex3.Message);
			}
			try
			{
				Reach.InitClaimsHook();
			}
			catch (Exception ex4)
			{
				Runtime.Trace("reach3 err: " + ex4.Message);
			}
			try
			{
				Reach.InitStructureHook();
			}
			catch (Exception ex5)
			{
				Runtime.Trace("reach4 err: " + ex5.Message);
			}
			try
			{
				Reach.InitNearbyHook();
			}
			catch (Exception ex6)
			{
				Runtime.Trace("reach5 err: " + ex6.Message);
			}
			try
			{
				Reach.InitNearbyWallHook();
			}
			catch (Exception ex7)
			{
				Runtime.Trace("reach5b err: " + ex7.Message);
			}
			try
			{
				Reach.InitItemDropHook();
			}
			catch (Exception ex8)
			{
				Runtime.Trace("reach5c err: " + ex8.Message);
			}
			try
			{
				Reach.InitSalvageHook();
			}
			catch (Exception ex9)
			{
				Runtime.Trace("reach6 err: " + ex9.Message);
			}
			try
			{
				MapHook.Init();
			}
			catch (Exception ex10)
			{
				Runtime.Trace("map err: " + ex10.Message);
			}
			try
			{
				WeaponMods.InitScopeHook();
			}
			catch (Exception ex11)
			{
				Runtime.Trace("wpn1 err: " + ex11.Message);
			}
			try
			{
				WeaponMods.InitBinoHook();
			}
			catch (Exception ex12)
			{
				Runtime.Trace("wpn2 err: " + ex12.Message);
			}
			try
			{
				WeaponMods.InitSpreadHook();
			}
			catch (Exception ex13)
			{
				Runtime.Trace("wpn3 err: " + ex13.Message);
			}
			try
			{
				WeaponMods.InitAimHook();
			}
			catch (Exception ex14)
			{
				Runtime.Trace("wpn4 err: " + ex14.Message);
			}
			try
			{
				WeaponMods.InitRecoilHook();
			}
			catch (Exception ex15)
			{
				Runtime.Trace("wpn5 err: " + ex15.Message);
			}
			try
			{
				WeaponMods.InitMeleeReachHook();
			}
			catch (Exception ex16)
			{
				Runtime.Trace("wpn7 err: " + ex16.Message);
			}
			try
			{
				WeaponMods.InitPunchReachHook();
			}
			catch (Exception ex17)
			{
				Runtime.Trace("wpn8 err: " + ex17.Message);
			}
			try
			{
				WeaponMods.InitBallisticsHook();
			}
			catch (Exception ex18)
			{
				Runtime.Trace("wpn9 err: " + ex18.Message);
			}
			try
			{
				WeaponMods.InitFlinchHook();
			}
			catch (Exception ex19)
			{
				Runtime.Trace("wpn10 err: " + ex19.Message);
			}
			try
			{
				WeaponMods.InitLeaveTimerHook();
			}
			catch (Exception ex20)
			{
				Runtime.Trace("wpn11 err: " + ex20.Message);
			}
			try
			{
				WeaponMods.Update();
			}
			catch
			{
			}
			try
			{
				AutoPickup.Update();
			}
			catch
			{
			}
			try
			{
				AutoFarm.Update();
			}
			catch (Exception exA)
			{
				Runtime.Trace("autofarm err: " + exA.Message);
			}
			try
			{
				AutoFish.Update();
			}
			catch (Exception exF)
			{
				Runtime.Trace("autofish err: " + exF.Message);
			}
			try
			{
				AutoJoin.Update();
			}
			catch { }
			try
			{
				GroupSync.Update();
			}
			catch { }
			try
			{
				HwidChanger.Init();
			}
			catch (Exception ex21)
			{
				Runtime.Trace("hwid err: " + ex21.Message);
			}
			try
			{
				FreeCam.InstallInputHook();
			}
			catch (Exception ex22)
			{
				Runtime.Trace("fc1 err: " + ex22.Message);
			}
			try
			{
				FreeCam.InstallLookHook();
			}
			catch (Exception ex23)
			{
				Runtime.Trace("fc2 err: " + ex23.Message);
			}
			try
			{
				VehicleFly.InitExitHook();
			}
			catch (Exception ex24)
			{
				Runtime.Trace("vexit err: " + ex24.Message);
			}
			try
			{
				SilentAim.InitHook();
			}
			catch (Exception ex25)
			{
				Runtime.Trace("saim err: " + ex25.Message);
			}
			try
			{
				HitboxExpander.UpdateAll();
			}
			catch
			{
			}
			try
			{
				EntityInspector.Update();
			}
			catch { }
			try
			{
				StorageViewer.Update();
			}
			catch { }
			try
			{
				MiscVisuals.Update();
			}
			catch (Exception ex26)
			{
				Runtime.Trace("misc err: " + ex26.Message);
			}
			try
			{
				Footsteps.Update();
			}
			catch (Exception ex27)
			{
				Runtime.Trace("footsteps err: " + ex27.Message);
			}
			try
			{
				AutoForge.Update();
			}
			catch (Exception ex28)
			{
				Runtime.Trace("autoforge err: " + ex28.Message);
			}
			try
			{
				Chams.BeginFrame();
				Chams.EndFrame();
			}
			catch (Exception ex29)
			{
				Runtime.Trace("chams err: " + ex29.Message);
			}
		}

		private void FixedUpdate()
		{
			try
			{
				VehicleFly.FixedTick();
			}
			catch
			{
			}
		}

		private void OnDestroy()
		{
			try
			{
				HitboxExpander.CleanupAll();
			}
			catch
			{
			}
		}

        private void LateUpdate()
        {
            // Force cursor free in LateUpdate so PlayerLook can't re-lock after Update
            if (State.Open)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            try
            {
                Aimbot.Tick();
            }
            catch
            {
            }
            try
            {
                SilentAim.UpdateTarget();
            }
            catch
            {
            }
            try
            {
                AimAssist.Update();
            }
            catch
            {
            }
        }

        private void SetScrollBlock(bool block)
		{
			try
			{
				if (this._scrollType == null)
				{
					foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
					{
						this._scrollType = assembly.GetType("SDG.Unturned.ScrollRectEx");
						if (this._scrollType == null)
						{
							this._scrollType = assembly.GetType("ScrollRectEx");
						}
						if (this._scrollType != null)
						{
							break;
						}
					}
					if (this._scrollType != null)
					{
						this._scrollProp = this._scrollType.GetProperty("scrollSensitivity");
					}
				}
				if (!(this._scrollType == null) && !(this._scrollProp == null))
				{
					if (block)
					{
						UnityEngine.Object[] array = UnityEngine.Object.FindObjectsOfType(this._scrollType);
						this._savedScrolls = new object[array.Length];
						for (int j = 0; j < array.Length; j++)
						{
							this._savedScrolls[j] = array[j];
						}
						foreach (object obj in this._savedScrolls)
						{
							this._scrollProp.SetValue(obj, 0f, null);
						}
						this._scrollsBlocked = true;
					}
					else if (this._scrollsBlocked)
					{
						foreach (object obj2 in this._savedScrolls)
						{
							try
							{
								this._scrollProp.SetValue(obj2, 40f, null);
							}
							catch
							{
							}
						}
						this._savedScrolls = new object[0];
						this._scrollsBlocked = false;
					}
				}
			}
			catch
			{
			}
		}

		private float _spamTimer;

		private object[] _savedScrolls = new object[0];

		private Type _scrollType;

		private PropertyInfo _scrollProp;

		private bool _scrollsBlocked;

		private static bool _lookWasDisabled;

		private static FieldInfo _lookIsLockedField;
		private static bool _lookFieldCached;

		private static void SetPlayerLookEnabled(bool enabled)
		{
			try
			{
				Player lp = Player.LocalPlayer;
				if (lp == null) return;

				// Cache the PlayerLook cursor-lock field once
				if (!_lookFieldCached)
				{
					_lookFieldCached = true;
					BindingFlags bf = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
					// Try common names Unturned uses for the "is cursor locked" state
					foreach (string name in new[] { "_isLocked", "isLocked", "_cursorLocked", "cursorLocked", "isCursorLocked" })
					{
						_lookIsLockedField = typeof(PlayerLook).GetField(name, bf);
						if (_lookIsLockedField != null && _lookIsLockedField.FieldType == typeof(bool)) break;
						_lookIsLockedField = null;
					}
				}

				if (!enabled)
				{
					// Menu opening: force cursor unlocked
					// Set the PlayerLook "is locked" field so it stops locking every frame
					if (_lookIsLockedField != null && lp.look != null)
						_lookIsLockedField.SetValue(lp.look, false);

					Cursor.lockState = CursorLockMode.None;
					Cursor.visible = true;
					if (PlayerUI.window != null)
						PlayerUI.window.showCursor = true;
				}
				else
				{
					// Menu closing: restore cursor lock
					if (_lookIsLockedField != null && lp.look != null)
						_lookIsLockedField.SetValue(lp.look, true);

					Cursor.lockState = CursorLockMode.Locked;
					Cursor.visible = false;
					if (PlayerUI.window != null)
						PlayerUI.window.showCursor = false;
				}

				_lookWasDisabled = !enabled;
			}
			catch
			{
			}
		}
	}
}
