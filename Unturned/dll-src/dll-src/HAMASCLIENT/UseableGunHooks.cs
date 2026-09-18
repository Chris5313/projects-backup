using System;
using System.Collections.Generic;
using System.Reflection;
using SDG.Unturned;
using UnityEngine;
public class UseableGunHooks : UseableGun
{
	// (get) Token: 0x0600032B RID: 811 RVA: 0x00034694 File Offset: 0x00032894
	public static UseableGun gun
	{
		get
		{
			return Player.player.equipment.useable as UseableGun;
		}
	}
	// (get) Token: 0x0600032C RID: 812 RVA: 0x000346BC File Offset: 0x000328BC
	public static bool shouldEnableTacticalStats
	{
		get
		{
			ItemTacticalAsset tacticalAsset = UseableGunHooks.ThirdAttachmentsField.Get(UseableGunHooks.gun).tacticalAsset;
			return tacticalAsset != null && ((!tacticalAsset.isLaser && !tacticalAsset.isLight && !tacticalAsset.isRangefinder) || UseableGunHooks.InteractField.Get(UseableGunHooks.gun));
		}
	}
	public static float GetCurrentSpread()
	{
		float num = (float)Player.player.equipment.quality / 100f;
		float num2 = UseableGunHooks.GetInterpolatedAimAlphaField.InvokeI(UseableGunHooks.gun, Array.Empty<object>());
		return UseableGunHooks.CalculateSpread(num, num2);
	}
	public static float CalculateSpread(float quality, float aimAlpha)
	{
		float num = UseableGunHooks.gun.equippedGunAsset.baseSpreadAngleRadians;
		num *= ((quality < 0.5f) ? (1f + (1f - quality * 2f)) : 1f);
		num *= Mathf.Lerp(1f, UseableGunHooks.gun.equippedGunAsset.spreadAim, aimAlpha);
		num *= 1f - Player.player.skills.mastery(0, 1) * 0.5f;
		UseableGunHooks.ThirdAttachmentsField.RefereshFieldValue(UseableGunHooks.gun);
		bool flag = UseableGunHooks.ThirdAttachmentsField.fldValue.sightAsset != null && (!UseableGunHooks.ThirdAttachmentsField.fldValue.sightAsset.ShouldOnlyAffectAimWhileProne || Player.player.stance.stance == EPlayerStance.PRONE);
		if (flag)
		{
			num *= Mathf.Lerp(1f, UseableGunHooks.ThirdAttachmentsField.fldValue.sightAsset.spread, aimAlpha);
		}
		bool flag2 = UseableGunHooks.ThirdAttachmentsField.fldValue.tacticalAsset != null && UseableGunHooks.shouldEnableTacticalStats && (!UseableGunHooks.ThirdAttachmentsField.fldValue.tacticalAsset.ShouldOnlyAffectAimWhileProne || Player.player.stance.stance == EPlayerStance.PRONE);
		if (flag2)
		{
			num *= UseableGunHooks.ThirdAttachmentsField.fldValue.tacticalAsset.spread;
		}
		bool flag3 = UseableGunHooks.ThirdAttachmentsField.fldValue.gripAsset != null && (!UseableGunHooks.ThirdAttachmentsField.fldValue.gripAsset.ShouldOnlyAffectAimWhileProne || Player.player.stance.stance == EPlayerStance.PRONE);
		if (flag3)
		{
			num *= UseableGunHooks.ThirdAttachmentsField.fldValue.gripAsset.spread;
		}
		bool flag4 = UseableGunHooks.ThirdAttachmentsField.fldValue.barrelAsset != null && (!UseableGunHooks.ThirdAttachmentsField.fldValue.barrelAsset.ShouldOnlyAffectAimWhileProne || Player.player.stance.stance == EPlayerStance.PRONE);
		if (flag4)
		{
			num *= UseableGunHooks.ThirdAttachmentsField.fldValue.barrelAsset.spread;
		}
		bool flag5 = UseableGunHooks.ThirdAttachmentsField.fldValue.magazineAsset != null && (!UseableGunHooks.ThirdAttachmentsField.fldValue.magazineAsset.ShouldOnlyAffectAimWhileProne || Player.player.stance.stance == EPlayerStance.PRONE);
		if (flag5)
		{
			num *= UseableGunHooks.ThirdAttachmentsField.fldValue.magazineAsset.spread;
		}
		bool flag6 = Player.player.stance.stance == EPlayerStance.SPRINT;
		if (flag6)
		{
			num *= UseableGunHooks.gun.equippedGunAsset.spreadSprint;
		}
		else
		{
			bool flag7 = Player.player.stance.stance == EPlayerStance.CROUCH;
			if (flag7)
			{
				num *= UseableGunHooks.gun.equippedGunAsset.spreadCrouch;
			}
			else
			{
				bool flag8 = Player.player.stance.stance == EPlayerStance.PRONE;
				if (flag8)
				{
					num *= UseableGunHooks.gun.equippedGunAsset.spreadProne;
				}
			}
		}
		bool flag9 = Player.player.look.perspective == EPlayerPerspective.THIRD;
		if (flag9)
		{
			num *= Provider.modeConfigData.Gameplay.ThirdPerson_SpreadMultiplier;
		}
		bool flag10 = !Player.player.movement.isGrounded;
		if (flag10)
		{
			num *= 1.5f;
		}
		return num;
	}
	[HookMethodAttribute(typeof(UseableGun), "equip", BindingFlags.Instance | BindingFlags.Public, new Type[] { })]
	public static void OnEquip(UseableGun ug)
	{
		OverrideManager.CallOriginal(ug, Array.Empty<object>());
		bool flag = (ug.channel.IsLocalPlayer || Provider.isServer) && ug.equippedGunAsset.projectile == null;
		if (flag)
		{
			UseableGunHooks.ActiveBullets = new List<DelayedBullet>();
		}
	}
	[HookMethodAttribute(typeof(UseableGun), "ballistics", new Type[] { })]
	private void OnBallisticsTick()
	{
		bool flag = !base.channel.IsLocalPlayer || Provider.isServer;
		if (flag)
		{
			OverrideManager.CallOriginal(this, Array.Empty<object>());
		}
		else
		{
			bool flag2 = base.equippedGunAsset.projectile == null && UseableGunHooks.ActiveBullets != null;
			if (flag2)
			{
				int i = 0;
				while (i < UseableGunHooks.ActiveBullets.Count)
				{
					DelayedBullet drxteGz0evnNDVy6poswGHA5b = UseableGunHooks.ActiveBullets[i];
					Ray ray = new Ray(drxteGz0evnNDVy6poswGHA5b.Position, drxteGz0evnNDVy6poswGHA5b.Velocity);
					float num = 4f;
					bool flag3 = Provider.modeConfigData.Gameplay.Ballistics && drxteGz0evnNDVy6poswGHA5b.Step + 1 >= base.equippedGunAsset.ballisticSteps;
					if (flag3)
					{
						num += base.equippedGunAsset.ballisticTravel * (float)MiscConfig.additionalBallisticSteps;
					}
					float num2 = (Provider.modeConfigData.Gameplay.Ballistics ? base.equippedGunAsset.ballisticTravel : base.equippedGunAsset.range) + (MiscConfig.extendBallisticRange ? num : 0f);
					RaycastInfo raycastInfo = AimbotUtil.SilentAimRaycast(ray, num2, RayMasks.DAMAGE_CLIENT, base.player, ref drxteGz0evnNDVy6poswGHA5b);
					try
					{
						bool flag4 = Provider.modeConfigData.Gameplay.Ballistics && AimbotConfig.bulletDelaying && raycastInfo.player != null && AimbotUtil.bulletEventHistoryPerPlayer.ContainsKey(raycastInfo.player.channel.owner.playerID.steamID.m_SteamID);
						if (flag4)
						{
							bool flag5 = true;
							bool flag6 = AimbotConfig.unholdDelayByMouse;
							List<DelayedBullet> list = AimbotUtil.bulletEventHistoryPerPlayer[raycastInfo.player.channel.owner.playerID.steamID.m_SteamID];
							bool flag7 = AimbotConfig.unholdDelayByMouse && AimbotConfig.momentalyUnhold && !Input.GetMouseButton(0);
							if (flag7)
							{
								list.Clear();
							}
							else
							{
								bool flag8 = list != null && list.Count != 0;
								if (flag8)
								{
									foreach (DelayedBullet drxteGz0evnNDVy6poswGHA5b2 in list)
									{
										bool flag9 = drxteGz0evnNDVy6poswGHA5b2.Target == null || !AimbotUtil.DIsTargetValid(drxteGz0evnNDVy6poswGHA5b2.Target) || (drxteGz0evnNDVy6poswGHA5b2.Target is Player && (PlayerPriorityManager.IsFriendOrGroupMatePlayer((Player)drxteGz0evnNDVy6poswGHA5b2.Target) || (AimbotConfig.dontShootPlayersOnSafezone && LevelNodes.isPointInsideSafezone(((Player)drxteGz0evnNDVy6poswGHA5b2.Target).transform.position, out ((Player)drxteGz0evnNDVy6poswGHA5b2.Target).movement.isSafeInfo))));
										if (flag9)
										{
											flag5 = false;
											break;
										}
										bool flag10 = drxteGz0evnNDVy6poswGHA5b2.Step + 2 >= base.equippedGunAsset.ballisticSteps;
										if (flag10)
										{
											flag5 = false;
										}
										Vector3 vector = (AimbotConfig.enableBacktrack ? AimbotUtil.GetBacktrackPositionAtTime((Player)drxteGz0evnNDVy6poswGHA5b2.Target, drxteGz0evnNDVy6poswGHA5b2.fireTime) : ((Player)drxteGz0evnNDVy6poswGHA5b2.Target).transform.position);
										bool flag11 = AimbotConfig.hookSpherePointToBullet && drxteGz0evnNDVy6poswGHA5b2.SphereOffset != Vector3.zero;
										if (flag11)
										{
											vector += drxteGz0evnNDVy6poswGHA5b2.SphereOffset;
										}
										bool flag12 = AimbotConfig.unholdDelayByMouse && Vector3.Distance(drxteGz0evnNDVy6poswGHA5b2.Position, vector) > num2;
										if (flag12)
										{
											flag6 = false;
										}
									}
									bool flag13 = list.Count >= AimbotConfig.bulletDelayAmount;
									if (flag13)
									{
										flag5 = false;
										flag6 = false;
									}
									bool key = Input.GetKey(AimbotConfig.bulletDelayKeybind);
									if (key)
									{
										flag5 = false;
										flag6 = false;
									}
									bool flag14 = AimbotConfig.bulletDelaySeconds > 0f && (flag5 || flag6);
									if (flag14)
									{
										float num3 = float.MaxValue;
										foreach (DelayedBullet drxteGz0evnNDVy6poswGHA5b3 in list)
										{
											bool flag15 = drxteGz0evnNDVy6poswGHA5b3.fireTime < num3;
											if (flag15)
											{
												num3 = drxteGz0evnNDVy6poswGHA5b3.fireTime;
											}
										}
										bool flag16 = num3 != float.MaxValue && Time.time - num3 < AimbotConfig.bulletDelaySeconds;
										if (flag16)
										{
											goto IL_0DC7;
										}
										flag5 = false;
										flag6 = false;
									}
									bool flag17 = flag5 || flag6;
									if (flag17)
									{
										goto IL_0DC7;
									}
									list.Clear();
								}
							}
						}
					}
					catch
					{
					}
					goto IL_046E;
					IL_0DC7:
					i++;
					i++;
					continue;
					IL_046E:
					bool flag18 = Settings.tracerType == ProjectileType.BallisticProceed;
					if (flag18)
					{
						TracerRenderer.SpawnTracer(ray.origin, (raycastInfo.point != Vector3.zero) ? raycastInfo.point : (ray.origin + ray.direction * num2), 0U);
					}
					else
					{
						bool flag19 = drxteGz0evnNDVy6poswGHA5b.ProjectileId > 0U;
						if (flag19)
						{
							TracerRenderer.ReplaceTracer(ray.origin, (raycastInfo.point != Vector3.zero) ? raycastInfo.point : (ray.origin + ray.direction * num2), drxteGz0evnNDVy6poswGHA5b.ProjectileId);
						}
					}
					bool replaceHitLimbToCustom = MiscConfig.replaceHitLimbToCustom;
					if (replaceHitLimbToCustom)
					{
						raycastInfo.limb = MiscConfig.replacedHitLimb.ToLimb();
					}
					EPlayerHit eplayerHit = EPlayerHit.NONE;
					bool flag20 = raycastInfo.player != null && base.equippedGunAsset.playerDamageMultiplier.damage > 1f && (DamageTool.isPlayerAllowedToDamagePlayer(base.player, raycastInfo.player) || base.equippedGunAsset.bypassAllowedToDamagePlayer);
					if (flag20)
					{
						bool flag21 = eplayerHit != EPlayerHit.CRITICAL;
						if (flag21)
						{
							eplayerHit = ((raycastInfo.limb == ELimb.SKULL) ? EPlayerHit.CRITICAL : EPlayerHit.ENTITIY);
						}
						PlayerUI.hitmark(raycastInfo.point, drxteGz0evnNDVy6poswGHA5b.MagazineAsset.pellets > 1, (raycastInfo.limb == ELimb.SKULL) ? EPlayerHit.CRITICAL : EPlayerHit.ENTITIY);
					}
					else
					{
						bool flag22 = raycastInfo.zombie != null && base.equippedGunAsset.zombieDamageMultiplier.damage > 1f;
						if (flag22)
						{
							EPlayerHit eplayerHit2 = ((raycastInfo.limb == ELimb.SKULL) ? EPlayerHit.CRITICAL : EPlayerHit.ENTITIY);
							bool flag23 = raycastInfo.zombie.getBulletResistance() < 0.2f;
							if (flag23)
							{
								eplayerHit2 = EPlayerHit.GHOST;
							}
							bool flag24 = eplayerHit != EPlayerHit.CRITICAL;
							if (flag24)
							{
								eplayerHit = eplayerHit2;
							}
							PlayerUI.hitmark(raycastInfo.point, drxteGz0evnNDVy6poswGHA5b.MagazineAsset.pellets > 1, eplayerHit2);
						}
						else
						{
							bool flag25 = raycastInfo.animal != null && base.equippedGunAsset.animalDamageMultiplier.damage > 1f;
							if (flag25)
							{
								bool flag26 = eplayerHit != EPlayerHit.CRITICAL;
								if (flag26)
								{
									eplayerHit = ((raycastInfo.limb == ELimb.SKULL) ? EPlayerHit.CRITICAL : EPlayerHit.ENTITIY);
								}
								PlayerUI.hitmark(raycastInfo.point, drxteGz0evnNDVy6poswGHA5b.MagazineAsset.pellets > 1, (raycastInfo.limb == ELimb.SKULL) ? EPlayerHit.CRITICAL : EPlayerHit.ENTITIY);
							}
							else
							{
								bool flag27 = raycastInfo.transform != null && raycastInfo.transform.CompareTag("Barricade") && base.equippedGunAsset.barricadeDamage > 1f;
								if (flag27)
								{
									BarricadeDrop barricadeDrop = UseableGunHooks.FindBarricadeByRootField.Invoke(new object[] { raycastInfo.transform });
									bool flag28 = barricadeDrop != null;
									if (flag28)
									{
										ItemBarricadeAsset asset = barricadeDrop.asset;
										bool flag29 = asset != null && asset.canBeDamaged && (asset.isVulnerable || ((ItemWeaponAsset)base.player.equipment.asset).isInvulnerable);
										if (flag29)
										{
											bool flag30 = eplayerHit == EPlayerHit.NONE;
											if (flag30)
											{
												eplayerHit = EPlayerHit.BUILD;
											}
											PlayerUI.hitmark(raycastInfo.point, drxteGz0evnNDVy6poswGHA5b.MagazineAsset.pellets > 1, EPlayerHit.BUILD);
										}
									}
								}
								else
								{
									bool flag31 = raycastInfo.transform != null && raycastInfo.transform.CompareTag("Structure") && base.equippedGunAsset.structureDamage > 1f;
									if (flag31)
									{
										StructureDrop structureDrop = UseableGunHooks.FindStructureByRootField.Invoke(new object[] { raycastInfo.transform });
										bool flag32 = structureDrop != null;
										if (flag32)
										{
											ItemStructureAsset asset2 = structureDrop.asset;
											bool flag33 = asset2 != null && asset2.canBeDamaged && (asset2.isVulnerable || ((ItemWeaponAsset)base.player.equipment.asset).isInvulnerable);
											if (flag33)
											{
												bool flag34 = eplayerHit == EPlayerHit.NONE;
												if (flag34)
												{
													eplayerHit = EPlayerHit.BUILD;
												}
												PlayerUI.hitmark(raycastInfo.point, drxteGz0evnNDVy6poswGHA5b.MagazineAsset.pellets > 1, EPlayerHit.BUILD);
											}
										}
									}
									else
									{
										bool flag35 = raycastInfo.vehicle != null && !raycastInfo.vehicle.isDead && base.equippedGunAsset.vehicleDamage > 1f;
										if (flag35)
										{
											bool flag36 = raycastInfo.vehicle.asset != null && raycastInfo.vehicle.canBeDamaged && (raycastInfo.vehicle.asset.isVulnerable || ((ItemWeaponAsset)base.player.equipment.asset).isInvulnerable);
											if (flag36)
											{
												bool flag37 = eplayerHit == EPlayerHit.NONE;
												if (flag37)
												{
													eplayerHit = EPlayerHit.BUILD;
												}
												PlayerUI.hitmark(raycastInfo.point, drxteGz0evnNDVy6poswGHA5b.MagazineAsset.pellets > 1, EPlayerHit.BUILD);
											}
										}
										else
										{
											bool flag38 = raycastInfo.transform != null && raycastInfo.transform.CompareTag("Resource") && base.equippedGunAsset.resourceDamage > 1f;
											if (flag38)
											{
												byte b = 0;
												byte b2 = 0;
												ushort num4 = 0;
												bool flag39 = ResourceManager.tryGetRegion(raycastInfo.transform, out b, out b2, out num4);
												if (flag39)
												{
													ResourceSpawnpoint resourceSpawnpoint = ResourceManager.getResourceSpawnpoint(b, b2, num4);
													bool flag40 = resourceSpawnpoint != null && !resourceSpawnpoint.isDead && base.equippedGunAsset.hasBladeID(resourceSpawnpoint.asset.bladeID);
													if (flag40)
													{
														bool flag41 = eplayerHit == EPlayerHit.NONE;
														if (flag41)
														{
															eplayerHit = EPlayerHit.BUILD;
														}
														PlayerUI.hitmark(raycastInfo.point, drxteGz0evnNDVy6poswGHA5b.MagazineAsset.pellets > 1, EPlayerHit.BUILD);
													}
												}
											}
											else
											{
												bool flag42 = raycastInfo.transform != null && base.equippedGunAsset.objectDamage > 1f;
												if (flag42)
												{
													InteractableObjectRubble componentInParent = raycastInfo.transform.GetComponentInParent<InteractableObjectRubble>();
													bool flag43 = componentInParent != null;
													if (flag43)
													{
														raycastInfo.transform = componentInParent.transform;
														raycastInfo.section = componentInParent.getSection(raycastInfo.collider.transform);
														bool flag44 = componentInParent.IsSectionIndexValid(raycastInfo.section) && !componentInParent.isSectionDead(raycastInfo.section) && base.equippedGunAsset.hasBladeID(componentInParent.asset.rubbleBladeID) && (componentInParent.asset.rubbleIsVulnerable || ((ItemWeaponAsset)base.player.equipment.asset).isInvulnerable);
														if (flag44)
														{
															bool flag45 = eplayerHit == EPlayerHit.NONE;
															if (flag45)
															{
																eplayerHit = EPlayerHit.BUILD;
															}
															PlayerUI.hitmark(raycastInfo.point, drxteGz0evnNDVy6poswGHA5b.MagazineAsset.pellets > 1, EPlayerHit.BUILD);
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
					bool flag46 = !base.player.input.isRaycastInvalid(raycastInfo);
					if (flag46)
					{
						bool flag47 = eplayerHit > EPlayerHit.NONE;
						if (flag47)
						{
							int num5 = 0;
							bool statistic = Provider.provider.statisticsService.userStatisticsService.getStatistic("Accuracy_Hit", out num5);
							if (statistic)
							{
								Provider.provider.statisticsService.userStatisticsService.setStatistic("Accuracy_Hit", num5 + 1);
							}
							bool flag48 = eplayerHit == EPlayerHit.CRITICAL && Provider.provider.statisticsService.userStatisticsService.getStatistic("Headshots", out num5);
							if (flag48)
							{
								Provider.provider.statisticsService.userStatisticsService.setStatistic("Headshots", num5 + 1);
							}
						}
						try
						{
							ushort num6 = (ushort)(base.equippedGunAsset.playerDamageMultiplier.damage * base.equippedGunAsset.playerDamageMultiplier.skull * ArmorCalculator.GetLimbArmorFactor(raycastInfo.limb, raycastInfo.player));
							Logger.LogUser(string.Format("[+] Hit limb {0} with {1} damage", raycastInfo.limb, num6));
							bool flag49 = raycastInfo.player != null;
							if (flag49)
							{
								TracerRenderer.SpawnDamageHitmarker(raycastInfo.point, num6);
							Debug.Log("[HAMAS] Gun hit registered on player"); if (MiscConfig.hitSound)
							{
								if (MiscConfig.customHitSound)
								{
									try
									{
										string wavPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "/hitsound.wav";
										if (!System.IO.File.Exists(wavPath)) wavPath = Application.dataPath + "/hitsound.wav";
										if (System.IO.File.Exists(wavPath))
										{
											AudioClip clip = LoadWav(wavPath, MiscConfig.hitSoundVolume); if (clip != null) clip.LoadAudioData();
											OneShotAudioParameters osp = new OneShotAudioParameters(base.player.transform.position, clip);
											osp.Play();
											Logger.LogClient("hit sound played: " + wavPath);
										}
										else Logger.LogClient("hit sound: file not found at " + wavPath);
									}
									catch (Exception ex) { Logger.LogClient("hit sound error: " + ex.Message); }
								}
								else
								{
									try
									{
										bool flagHitRandom = MiscConfig.hitAudioName == "Random";
										AudioClip audioClip = null;
										if (flagHitRandom)
										{
											string rndName = GuiStyles.SoundNames[UnityEngine.Random.Range(0, GuiStyles.SoundNames.Length - 1)];
											if (GuiStyles.LoadedAssetCache.ContainsKey(rndName))
												audioClip = GuiStyles.LoadedAssetCache[rndName].Asset as AudioClip;
										}
										else if (GuiStyles.LoadedAssetCache.ContainsKey(MiscConfig.hitAudioName))
										{
											audioClip = GuiStyles.LoadedAssetCache[MiscConfig.hitAudioName].Asset as AudioClip;
										}
										if (audioClip != null)
										{
											OneShotAudioParameters osp2 = new OneShotAudioParameters(base.player.transform.position, audioClip);
											osp2.minDistance = 0f;
											osp2.maxDistance = 15f;
											osp2.Play();
										}
										else
										{
											Logger.LogClient("[HitSound] clip is null for: " + MiscConfig.hitAudioName);
										}
									}
									catch (Exception ex) { Logger.LogClient("[HitSound] error: " + ex.Message); }
								}
							}
							}
							bool flag50 = Settings.tracerType == ProjectileType.Straight || (Settings.tracerType == ProjectileType.BallisticMoved && !Provider.modeConfigData.Gameplay.Ballistics);
							if (flag50)
							{
								TracerRenderer.SpawnTracer(Player.player.look.aim.position, raycastInfo.point, 0U);
							}
						}
						catch
						{
						}
						base.player.input.sendRaycast(raycastInfo, ERaycastInfoUsage.Gun);
						drxteGz0evnNDVy6poswGHA5b.Step = 254;
						goto IL_0DC7;
					}
					float num7 = Physics.gravity.y;
					bool flag51 = drxteGz0evnNDVy6poswGHA5b.BarrelAsset != null;
					if (flag51)
					{
						num7 *= drxteGz0evnNDVy6poswGHA5b.BarrelAsset.ballisticDrop;
					}
					num7 *= base.equippedGunAsset.bulletGravityMultiplier;
					drxteGz0evnNDVy6poswGHA5b.Position += drxteGz0evnNDVy6poswGHA5b.Velocity * 0.02f;
					bool flag52 = !MiscConfig.noBallistics;
					if (flag52)
					{
						drxteGz0evnNDVy6poswGHA5b.Velocity = new Vector3(drxteGz0evnNDVy6poswGHA5b.Velocity.x, drxteGz0evnNDVy6poswGHA5b.Velocity.y + num7 * 0.02f, drxteGz0evnNDVy6poswGHA5b.Velocity.z);
						goto IL_0DC7;
					}
					goto IL_0DC7;
				}
				for (int j = UseableGunHooks.ActiveBullets.Count - 1; j >= 0; j--)
				{
					DelayedBullet drxteGz0evnNDVy6poswGHA5b4 = UseableGunHooks.ActiveBullets[j];
					DelayedBullet drxteGz0evnNDVy6poswGHA5b5 = drxteGz0evnNDVy6poswGHA5b4;
					drxteGz0evnNDVy6poswGHA5b5.Step += 1;
					bool flag53 = drxteGz0evnNDVy6poswGHA5b4.Step >= base.equippedGunAsset.ballisticSteps;
					if (flag53)
					{
						UseableGunHooks.ActiveBullets.RemoveAt(j);
					}
				}
			}
		}
	}
	[HookMethodAttribute(typeof(UseableGun), "tockShoot", new Type[] { })]
	private void OnTockShoot(uint clock)
	{
		UseableGunHooks.FiremodeField.instance = this;
		UseableGunHooks.IsShootingField.instance = this;
		UseableGunHooks.BurstsField.instance = this;
		UseableGunHooks.FireDelayCounterField.instance = this;
		UseableGunHooks.WasTriggerJustPulledField.instance = this;
		bool flag = (UseableGunHooks.FiremodeField.value == EFiremode.SAFETY) | UseableGunHooks.IsReloadingField.Get(this) | UseableGunHooks.IsHammeringField.Get(this) | UseableGunHooks.IsUnjammingField.Get(this) | UseableGunHooks.IsAttachingField.Get(this) | (!base.player.equipment.asset.canUseUnderwater && (base.player.stance.isSubmerged || base.player.stance.stance == EPlayerStance.SWIM));
		if (flag)
		{
			UseableGunHooks.BurstsField.value = 0;
			UseableGunHooks.FireDelayCounterField.value = 0;
			UseableGunHooks.IsShootingField.Set(false);
			UseableGunHooks.WasTriggerJustPulledField.value = false;
		}
		else
		{
			object dnuBpXW8r7IgW3DCiPdbWlBGC = AimbotUtil.aimObjective.Target;
			bool flag2 = dnuBpXW8r7IgW3DCiPdbWlBGC != null && AimbotUtil.DIsTargetValid(dnuBpXW8r7IgW3DCiPdbWlBGC);
			bool flag3 = flag2 && dnuBpXW8r7IgW3DCiPdbWlBGC is Player;
			if (flag3)
			{
				Player player = (Player)dnuBpXW8r7IgW3DCiPdbWlBGC;
				bool flag4 = PlayerPriorityManager.IsFriendOrGroupMatePlayer(player);
				if (flag4)
				{
					flag2 = false;
				}
				else
				{
					bool flag5 = AimbotConfig.dontShootPlayersOnSafezone && LevelNodes.isPointInsideSafezone(player.transform.position, out player.movement.isSafeInfo);
					if (flag5)
					{
						flag2 = false;
					}
				}
			}
			bool flag6 = AimbotConfig.enableAutoShoot && AimbotConfig.enableSilentAim && flag2;
			if (flag6)
			{
				UseableGunHooks.IsShootingField.Set(true);
			}
			bool flag7 = UseableGunHooks.IsShootingField.Get() || UseableGunHooks.WasTriggerJustPulledField.Get();
			UseableGunHooks.WasTriggerJustPulledField.value = false;
			bool flag8 = UseableGunHooks.FireDelayCounterField.Get() > 1;
			if (flag8)
			{
				ReflectedField<int> dhlvyNPSCxytfY3IOmjda12Gy = UseableGunHooks.FireDelayCounterField;
				int value = dhlvyNPSCxytfY3IOmjda12Gy.value;
				dhlvyNPSCxytfY3IOmjda12Gy.value = value - 1;
			}
			else
			{
				bool flag9 = UseableGunHooks.FireDelayCounterField.Get() > 0;
				if (flag9)
				{
					UseableGunHooks.FireDelayCounterField.value = 0;
					flag7 = true;
				}
				bool flag10 = UseableGunHooks.FiremodeField.value == EFiremode.SEMI;
				if (flag10)
				{
					UseableGunHooks.IsShootingField.Set(false);
				}
				bool flag11 = UseableGunHooks.FiremodeField.value == EFiremode.BURST;
				if (flag11)
				{
					UseableGunHooks.IsShootingField.Set(false);
					bool flag12 = flag7;
					if (flag12)
					{
						UseableGunHooks.BurstsField.value += base.equippedGunAsset.bursts;
					}
				}
				bool correctFirerateToWork = MiscConfig.correctFirerateToWork;
				int num;
				if (correctFirerateToWork)
				{
					num = (int)base.equippedGunAsset.firerate - Mathf.Min(MiscConfig.firerateDecrease, (int)((base.equippedGunAsset.firerate + 1) % 4));
				}
				else
				{
					num = (int)base.equippedGunAsset.firerate - MiscConfig.firerateDecrease;
				}
				bool flag13 = UseableGunHooks.ThirdAttachmentsField.Get(this).sightAsset != null;
				if (flag13)
				{
					num -= UseableGunHooks.ThirdAttachmentsField.Get(this).sightAsset.FirerateOffset;
				}
				bool flag14 = UseableGunHooks.ThirdAttachmentsField.Get(this).tacticalAsset != null && UseableGunHooks.shouldEnableTacticalStats;
				if (flag14)
				{
					num -= UseableGunHooks.ThirdAttachmentsField.Get(this).tacticalAsset.FirerateOffset;
				}
				bool flag15 = UseableGunHooks.ThirdAttachmentsField.Get(this).gripAsset != null;
				if (flag15)
				{
					num -= UseableGunHooks.ThirdAttachmentsField.Get(this).gripAsset.FirerateOffset;
				}
				bool flag16 = UseableGunHooks.ThirdAttachmentsField.Get(this).barrelAsset != null;
				if (flag16)
				{
					num -= UseableGunHooks.ThirdAttachmentsField.Get(this).barrelAsset.FirerateOffset;
				}
				bool flag17 = UseableGunHooks.ThirdAttachmentsField.Get(this).magazineAsset != null;
				if (flag17)
				{
					num -= UseableGunHooks.ThirdAttachmentsField.Get(this).magazineAsset.FirerateOffset;
				}
				num = Mathf.Max(num, 0);
				bool flag18 = (ulong)(clock - UseableGunHooks.LastFireField.Get(this)) > (ulong)((long)num);
				if (flag18)
				{
					bool flag19 = UseableGunHooks.BurstsField.Get() > 0;
					if (flag19)
					{
						ReflectedField<int> dzgjn4fACX06frDKJ8wGhw5Gh = UseableGunHooks.BurstsField;
						int value2 = dzgjn4fACX06frDKJ8wGhw5Gh.value;
						dzgjn4fACX06frDKJ8wGhw5Gh.value = value2 - 1;
					}
					bool flag20 = UseableGunHooks.AmmoField.Get(this) >= base.equippedGunAsset.ammoPerShot;
					if (flag20)
					{
						UseableGunHooks.IsFiredField.Set(this, true);
						UseableGunHooks.LastFireField.Set(this, clock);
						base.player.equipment.isBusy = true;
						this.OnFire();
					}
					else
					{
						bool isServer = Provider.isServer;
						if (isServer)
						{
							UseableGunHooks.TriggerFiremodeEffectField.Invoke(new object[] { base.transform.position });
						}
						UseableGunHooks.BurstsField.value = 0;
						UseableGunHooks.IsShootingField.Set(false);
					}
				}
			}
		}
	}
	[HookMethodAttribute(typeof(UseableGun), "fire", new Type[] { })]
	private void OnFire()
	{
		try
		{
			bool flag = !base.channel.IsLocalPlayer || Provider.isServer;
			if (flag)
			{
				OverrideManager.CallOriginal(this, Array.Empty<object>());
			}
			else
			{
				bool flag2 = AimbotUtil.ShouldAimByChance();
				bool flag3 = AimbotConfig.enableAim && flag2;
				if (flag3)
				{
					AimbotUtil.RefreshAimObjective();
				}
				float num = (float)base.player.equipment.quality / 100f;
				UseableGunHooks.ThirdAttachmentsField.RefereshFieldValue(UseableGunHooks.gun);
				bool flag4 = UseableGunHooks.ThirdAttachmentsField.fldValue.magazineAsset != null;
				if (flag4)
				{
					UseableGunHooks.AmmoField.instance = this;
					bool flag5 = AimbotConfig.enableAimbot && AimbotConfig.IsMemoryAimbotKeyActive();
					if (flag5)
					{
						AimbotUtil.AimAtObjective(false);
					}
					bool flag6 = !base.equippedGunAsset.infiniteAmmo;
					if (flag6)
					{
						bool flag7 = UseableGunHooks.AmmoField.Get() < base.equippedGunAsset.ammoPerShot;
						if (flag7)
						{
							throw new Exception("Insufficient ammo");
						}
						ReflectedField<byte> d0wfKQeI1PTkJOuEzSANt42Wo = UseableGunHooks.AmmoField;
						ReflectedField<byte> dgzv08vQyz81zrHxUqhmYuCdY = d0wfKQeI1PTkJOuEzSANt42Wo;
						dgzv08vQyz81zrHxUqhmYuCdY.value -= base.equippedGunAsset.ammoPerShot;
						bool flag8 = base.equippedGunAsset.action != EAction.String;
						if (flag8)
						{
							base.player.equipment.state[10] = UseableGunHooks.AmmoField.Get();
							base.player.equipment.updateState();
						}
					}
					bool flag9 = base.channel.IsLocalPlayer && UseableGunHooks.AmmoField.Get() < base.equippedGunAsset.ammoPerShot;
					if (flag9)
					{
						PlayerUI.message(EPlayerMessage.RELOAD, "", 2f);
					}
					bool flag10 = !base.isAiming;
					if (flag10)
					{
						base.player.equipment.uninspect();
					}
					bool flag11 = !base.player.look.isCam && base.player.look.perspective == EPlayerPerspective.THIRD;
					if (flag11)
					{
						RaycastHit raycastHit = default(RaycastHit);
						Physics.Raycast(new Ray(MainCamera.instance.transform.position, MainCamera.instance.transform.forward), out raycastHit, 512f, RayMasks.DAMAGE_CLIENT);
						bool flag12 = raycastHit.transform != null;
						if (flag12)
						{
							bool flag13 = Vector3.Dot(raycastHit.point - base.player.look.aim.position, MainCamera.instance.transform.forward) > 0f;
							if (flag13)
							{
								base.player.look.aim.rotation = Quaternion.LookRotation(raycastHit.point - base.player.look.aim.position);
							}
						}
						else
						{
							base.player.look.aim.rotation = Quaternion.LookRotation(MainCamera.instance.transform.position + MainCamera.instance.transform.forward * 512f - base.player.look.aim.position);
						}
					}
					object obj = ((AimbotConfig.enableSilentAim && flag2 && AimbotUtil.aimObjective.Target != null && AimbotUtil.DIsTargetValid(AimbotUtil.aimObjective.Target) && (!(AimbotUtil.aimObjective.Target is Player) || (!PlayerPriorityManager.IsFriendOrGroupMatePlayer((Player)AimbotUtil.aimObjective.Target) && (!AimbotConfig.dontShootPlayersOnSafezone || !LevelNodes.isPointInsideSafezone(((Player)AimbotUtil.aimObjective.Target).transform.position, out ((Player)AimbotUtil.aimObjective.Target).movement.isSafeInfo))))) ? AimbotUtil.aimObjective.Target : null);
					Transform transform = ((obj != null) ? AimbotUtil.GetAimBoneTransform(obj) : null);
					bool flag14 = base.equippedGunAsset.projectile == null;
					if (flag14)
					{
						bool flag15 = obj != null;
						Quaternion quaternion;
						if (flag15)
						{
							switch (AimbotConfig.silentAimType)
							{
							case SilentAimType.Aim:
								quaternion = MathUtil.LookRotationTo(base.player.look.aim.position, (AimbotConfig.enableBacktrack && obj is Player) ? AimbotUtil.GetBacktrackAimPosition((Player)obj) : transform.position);
								break;
							case SilentAimType.Distance:
								quaternion = MathUtil.LookRotationTo(base.player.look.aim.position, (AimbotConfig.enableBacktrack && obj is Player) ? AimbotUtil.GetBacktrackAimPosition((Player)obj) : transform.position);
								break;
							case SilentAimType.Sphere:
							{
								Vector3 vector = ((AimbotConfig.enableBacktrack && obj is Player) ? AimbotUtil.GetBacktrackAimPosition((Player)obj) : transform.position);
								Vector3 vector2;
								vector.TryGetVisiblePoint(base.player.look.aim.position, out vector2, true);
								quaternion = MathUtil.LookRotationTo(base.player.look.aim.position, (vector2 != Vector3.zero) ? vector2 : vector);
								break;
							}
							default:
								quaternion = Quaternion.identity;
								break;
							}
						}
						else
						{
							quaternion = base.player.look.aim.rotation;
						}
						EPlayerPerspective perspective = base.player.look.perspective;
						bool flag16 = false;
						if (flag16)
						{
							Quaternion quaternion2 = Quaternion.Euler(base.player.animator.recoilViewmodelCameraRotation.currentPosition);
							quaternion *= quaternion2;
						}
						float num2 = UseableGunHooks.CalculateSpread(num, UseableGunHooks.GetSimulationAimAlphaField.InvokeI(this, Array.Empty<object>()));
						byte pellets = UseableGunHooks.ThirdAttachmentsField.fldValue.magazineAsset.pellets;
						for (byte b = 0; b < pellets; b += 1)
						{
							DelayedBullet drxteGz0evnNDVy6poswGHA5b = new DelayedBullet();
							bool flag17 = AimbotConfig.enableSilentAim && flag2;
							if (flag17)
							{
								drxteGz0evnNDVy6poswGHA5b.Target = AimbotUtil.aimObjective.Target;
								drxteGz0evnNDVy6poswGHA5b.TargetType = AimbotUtil.aimObjective.TargetType;
								try
								{
									bool flag18 = drxteGz0evnNDVy6poswGHA5b != null && drxteGz0evnNDVy6poswGHA5b.TargetType == TargetType.Player && AimbotConfig.bulletDelaying;
									if (flag18)
									{
										List<DelayedBullet> list;
										bool flag19 = !AimbotUtil.bulletEventHistoryPerPlayer.TryGetValue(((Player)drxteGz0evnNDVy6poswGHA5b.Target).channel.owner.playerID.steamID.m_SteamID, out list);
										if (flag19)
										{
											AimbotUtil.bulletEventHistoryPerPlayer.Add(((Player)drxteGz0evnNDVy6poswGHA5b.Target).channel.owner.playerID.steamID.m_SteamID, list = new List<DelayedBullet>());
										}
										else
										{
											bool flag20 = list == null;
											if (flag20)
											{
												list = (AimbotUtil.bulletEventHistoryPerPlayer[((Player)drxteGz0evnNDVy6poswGHA5b.Target).channel.owner.playerID.steamID.m_SteamID] = new List<DelayedBullet>());
											}
										}
										drxteGz0evnNDVy6poswGHA5b.fireTime = Time.time;
										list.Add(drxteGz0evnNDVy6poswGHA5b);
									}
									else
									{
										bool flag21 = drxteGz0evnNDVy6poswGHA5b != null && AimbotConfig.enableBacktrack;
										if (flag21)
										{
											drxteGz0evnNDVy6poswGHA5b.fireTime = Time.time;
										}
									}
								}
								catch
								{
								}
							}
							drxteGz0evnNDVy6poswGHA5b.Origin = base.player.look.aim.position;
							bool flag22 = Settings.tracerType == ProjectileType.BallisticMoved;
							if (flag22)
							{
								drxteGz0evnNDVy6poswGHA5b.ProjectileId = TracerRenderer.SpawnTracer(drxteGz0evnNDVy6poswGHA5b.Origin, drxteGz0evnNDVy6poswGHA5b.Origin, 0U).TracerIndex;
							}
							drxteGz0evnNDVy6poswGHA5b.Position = drxteGz0evnNDVy6poswGHA5b.Origin;
							Vector3 vector3 = quaternion * RandomConeUtil.GetRandomForwardVectorInCone(num2);
							drxteGz0evnNDVy6poswGHA5b.Velocity = vector3 * base.equippedGunAsset.muzzleVelocity;
							drxteGz0evnNDVy6poswGHA5b.BallisticIndex = b;
							drxteGz0evnNDVy6poswGHA5b.BarrelAsset = UseableGunHooks.ThirdAttachmentsField.fldValue.barrelAsset;
							drxteGz0evnNDVy6poswGHA5b.MagazineAsset = UseableGunHooks.ThirdAttachmentsField.fldValue.magazineAsset;
							UseableGunHooks.ActiveBullets.Add(drxteGz0evnNDVy6poswGHA5b);
							int num3 = 0;
							bool statistic = Provider.provider.statisticsService.userStatisticsService.getStatistic("Accuracy_Shot", out num3);
							if (statistic)
							{
								Provider.provider.statisticsService.userStatisticsService.setStatistic("Accuracy_Shot", num3 + 1);
							}
							bool isAiming = base.isAiming;
							if (isAiming)
							{
								base.equippedGunAsset.recoilMin_x *= base.equippedGunAsset.aimingRecoilMultiplier;
								base.equippedGunAsset.recoilMax_x *= base.equippedGunAsset.aimingRecoilMultiplier;
							}
							bool flag23 = UseableGunHooks.ThirdAttachmentsField.fldValue.sightAsset != null;
							if (flag23)
							{
								base.equippedGunAsset.shakeMin_x *= UseableGunHooks.ThirdAttachmentsField.fldValue.sightAsset.shake;
								base.equippedGunAsset.shakeMin_y *= UseableGunHooks.ThirdAttachmentsField.fldValue.sightAsset.shake;
								base.equippedGunAsset.shakeMin_z *= UseableGunHooks.ThirdAttachmentsField.fldValue.sightAsset.shake;
							}
							bool flag24 = UseableGunHooks.ThirdAttachmentsField.fldValue.tacticalAsset != null && UseableGunHooks.shouldEnableTacticalStats;
							if (flag24)
							{
								base.equippedGunAsset.shakeMin_x *= UseableGunHooks.ThirdAttachmentsField.fldValue.tacticalAsset.shake;
								base.equippedGunAsset.shakeMin_y *= UseableGunHooks.ThirdAttachmentsField.fldValue.tacticalAsset.shake;
								base.equippedGunAsset.shakeMin_z *= UseableGunHooks.ThirdAttachmentsField.fldValue.tacticalAsset.shake;
							}
							bool flag25 = UseableGunHooks.ThirdAttachmentsField.fldValue.gripAsset != null && (!UseableGunHooks.ThirdAttachmentsField.fldValue.gripAsset.ShouldOnlyAffectAimWhileProne || base.player.stance.stance == EPlayerStance.PRONE);
							if (flag25)
							{
								base.equippedGunAsset.shakeMin_x *= UseableGunHooks.ThirdAttachmentsField.fldValue.gripAsset.shake;
								base.equippedGunAsset.shakeMin_y *= UseableGunHooks.ThirdAttachmentsField.fldValue.gripAsset.shake;
								base.equippedGunAsset.shakeMin_z *= UseableGunHooks.ThirdAttachmentsField.fldValue.gripAsset.shake;
							}
							bool flag26 = UseableGunHooks.ThirdAttachmentsField.fldValue.barrelAsset != null;
							if (flag26)
							{
								base.equippedGunAsset.shakeMin_x *= UseableGunHooks.ThirdAttachmentsField.fldValue.barrelAsset.shake;
								base.equippedGunAsset.shakeMin_y *= UseableGunHooks.ThirdAttachmentsField.fldValue.barrelAsset.shake;
								base.equippedGunAsset.shakeMin_z *= UseableGunHooks.ThirdAttachmentsField.fldValue.barrelAsset.shake;
							}
							bool flag27 = UseableGunHooks.ThirdAttachmentsField.fldValue.magazineAsset != null;
							if (flag27)
							{
								base.equippedGunAsset.shakeMin_x *= UseableGunHooks.ThirdAttachmentsField.fldValue.magazineAsset.shake;
								base.equippedGunAsset.shakeMin_y *= UseableGunHooks.ThirdAttachmentsField.fldValue.magazineAsset.shake;
								base.equippedGunAsset.shakeMin_z *= UseableGunHooks.ThirdAttachmentsField.fldValue.magazineAsset.shake;
							}
							bool flag28 = base.player.stance.stance == EPlayerStance.CROUCH;
							if (flag28)
							{
								base.equippedGunAsset.shakeMin_x *= UseableGunHooks.SHAKE_CROUCH;
								base.equippedGunAsset.shakeMin_y *= UseableGunHooks.SHAKE_CROUCH;
								base.equippedGunAsset.shakeMin_z *= UseableGunHooks.SHAKE_CROUCH;
							}
							else
							{
								bool flag29 = base.player.stance.stance == EPlayerStance.PRONE;
								if (flag29)
								{
									base.equippedGunAsset.shakeMin_x *= UseableGunHooks.SHAKE_PRONE;
									base.equippedGunAsset.shakeMin_y *= UseableGunHooks.SHAKE_PRONE;
									base.equippedGunAsset.shakeMin_z *= UseableGunHooks.SHAKE_PRONE;
								}
							}
							base.player.look.recoil(MiscConfig.recoilMultiplier, MiscConfig.recoilMultiplier, base.equippedGunAsset.recover_x, base.equippedGunAsset.recover_y);
						}
					}
					else
					{
						Vector3 vector4 = ((AimbotConfig.enableSilentAim && AimbotUtil.aimObjective.Target != null) ? MathUtil.DirectionTo(base.player.transform.position, AimbotUtil.GetAimBoneTransform(AimbotUtil.aimObjective.Target).position) : base.player.look.aim.forward);
						RaycastInfo raycastInfo = DamageTool.raycast(new Ray(base.player.look.aim.position, vector4), 512f, RayMasks.DAMAGE_CLIENT, base.player);
						bool flag30 = raycastInfo.transform != null;
						if (flag30)
						{
							base.player.input.sendRaycast(raycastInfo, ERaycastInfoUsage.Gun);
						}
						Vector3 vector5 = base.player.look.aim.position;
						RaycastHit raycastHit2 = default(RaycastHit);
						bool flag31 = !Physics.Raycast(new Ray(vector5, vector4), out raycastHit2, 1f, RayMasks.DAMAGE_SERVER);
						if (flag31)
						{
							vector5 += vector4;
						}
						UseableGunHooks.ProjectField.InvokeOn(this, new object[]
						{
							vector5,
							vector4,
							UseableGunHooks.ThirdAttachmentsField.fldValue.barrelAsset,
							UseableGunHooks.ThirdAttachmentsField.fldValue.magazineAsset
						});
						int num4 = 0;
						bool statistic2 = Provider.provider.statisticsService.userStatisticsService.getStatistic("Accuracy_Shot", out num4);
						if (statistic2)
						{
							Provider.provider.statisticsService.userStatisticsService.setStatistic("Accuracy_Shot", num4 + 1);
						}
					}
					UseableGunHooks.UpdateInfoField.InvokeOn(this, Array.Empty<object>());
					bool flag32 = base.equippedGunAsset.projectile == null;
					if (flag32)
					{
						UseableGunHooks.ShootField.InvokeOn(this, Array.Empty<object>());
					}
				}
			}
		}
		catch (Exception ex)
		{
			Logger.LogClient(ex.Message);
			Logger.LogClient(ex.StackTrace);
		}
	}
	[HookMethodAttribute(typeof(UseableGun), "GetInterpolatedAimAlpha", new Type[] { })]
	private static float OnGetInterpolatedAimAlpha(UseableGun instance)
	{
		bool flag = !MiscConfig.instantAiming || !instance.isAiming;
		float num;
		if (flag)
		{
			num = OverrideManager.CallOriginalInstance<float>(instance, Array.Empty<object>());
		}
		else
		{
			num = 1f;
		}
		return num;
	}
	[HookMethodAttribute(typeof(UseableGun), "trace", BindingFlags.Instance | BindingFlags.NonPublic, new Type[] { })]
	public static void OnTrace(UseableGun instance, Vector3 pos, Vector3 dir)
	{
		bool flag = !Settings.disallowWeaponTraces;
		if (flag)
		{
			OverrideManager.CallOriginal(instance, new object[] { pos, dir });
		}
	}
	[MethodHookAttribute(typeof(UseableGun), "applyRecoilMagnitudeModifiers", new Type[] { })]
	private void OnApplyRecoilMagnitudeModifiers(ref float value)
	{
		bool flag = Player.player.stance.stance == EPlayerStance.SPRINT;
		if (flag)
		{
			value *= base.equippedGunAsset.recoilSprint;
		}
		else
		{
			bool flag2 = Player.player.stance.stance == EPlayerStance.CROUCH;
			if (flag2)
			{
				value *= base.equippedGunAsset.recoilCrouch;
			}
			else
			{
				bool flag3 = Player.player.stance.stance == EPlayerStance.PRONE;
				if (flag3)
				{
					value *= base.equippedGunAsset.recoilProne;
				}
				else
				{
					bool flag4 = Player.player.stance.stance == EPlayerStance.SWIM;
					if (flag4)
					{
						value *= base.equippedGunAsset.recoilSwimming;
					}
				}
			}
		}
		bool flag5 = !Player.player.movement.isGrounded;
		if (flag5)
		{
			value *= base.equippedGunAsset.recoilMidair;
		}
		value *= MiscConfig.recoilMultiplier;
	}
	private void ApplyStanceRecoil(ref float value)
	{
		bool flag = Player.player.stance.stance == EPlayerStance.SPRINT;
		if (flag)
		{
			value *= base.equippedGunAsset.recoilSprint;
		}
		else
		{
			bool flag2 = Player.player.stance.stance == EPlayerStance.CROUCH;
			if (flag2)
			{
				value *= base.equippedGunAsset.recoilCrouch;
			}
			else
			{
				bool flag3 = Player.player.stance.stance == EPlayerStance.PRONE;
				if (flag3)
				{
					value *= base.equippedGunAsset.recoilProne;
				}
				else
				{
					bool flag4 = Player.player.stance.stance == EPlayerStance.SWIM;
					if (flag4)
					{
						value *= base.equippedGunAsset.recoilSwimming;
					}
				}
			}
		}
		bool flag5 = !Player.player.movement.isGrounded;
		if (flag5)
		{
			value *= base.equippedGunAsset.recoilMidair;
		}
	}
	public static MethodInvoker<float> GetInterpolatedAimAlphaField = new MethodInvoker<float>(typeof(UseableGun), "GetInterpolatedAimAlpha", BindingFlags.Instance | BindingFlags.NonPublic);
	public static MethodInvoker<float> GetSimulationAimAlphaField = new MethodInvoker<float>(typeof(UseableGun), "GetSimulationAimAlpha", BindingFlags.Instance | BindingFlags.NonPublic);
	public static MethodInvoker<BarricadeDrop> FindBarricadeByRootField = new MethodInvoker<BarricadeDrop>(typeof(BarricadeDrop), "FindByRootFast", BindingFlags.Static | BindingFlags.NonPublic);
	public static MethodInvoker<StructureDrop> FindStructureByRootField = new MethodInvoker<StructureDrop>(typeof(StructureDrop), "FindByRootFast", BindingFlags.Static | BindingFlags.NonPublic);
	public static ReflectedMethod UpdateInfoField = new ReflectedMethod(typeof(UseableGun), "updateInfo", BindingFlags.Instance | BindingFlags.NonPublic);
	public static ReflectedMethod ShootField = new ReflectedMethod(typeof(UseableGun), "shoot", BindingFlags.Instance | BindingFlags.NonPublic);
	public static ReflectedMethod ProjectField = new ReflectedMethod(typeof(UseableGun), "project", BindingFlags.Instance | BindingFlags.NonPublic);
	public static ReflectedMethod TraceField = new ReflectedMethod(typeof(UseableGun), "trace", BindingFlags.Instance | BindingFlags.NonPublic);
	public static ReflectedMethod PlayFlybyAudioField = new ReflectedMethod(typeof(UseableGun), "PlayFlybyAudio", BindingFlags.Instance | BindingFlags.NonPublic);
	public static ReflectedMethod TriggerFiremodeEffectField = new ReflectedMethod(typeof(EffectManager), "TriggerFiremodeEffect", BindingFlags.Static | BindingFlags.NonPublic);
	public static ReflectedField<Attachments> ThirdAttachmentsField = new ReflectedField<Attachments>(typeof(UseableGun), "thirdAttachments", BindingFlags.Instance | BindingFlags.NonPublic);
	public static ReflectedField<bool> InteractField = new ReflectedField<bool>(typeof(UseableGun), "interact", BindingFlags.Instance | BindingFlags.NonPublic);
	public static ReflectedField<byte> AmmoField = new ReflectedField<byte>(typeof(UseableGun), "ammo", BindingFlags.Instance | BindingFlags.NonPublic);
	public static ReflectedField<EFiremode> FiremodeField = new ReflectedField<EFiremode>(typeof(UseableGun), "firemode", BindingFlags.Instance | BindingFlags.NonPublic);
	public static ReflectedField<bool> IsShootingField = new ReflectedField<bool>(typeof(UseableGun), "isShooting", BindingFlags.Instance | BindingFlags.NonPublic);
	public static ReflectedField<bool> IsReloadingField = new ReflectedField<bool>(typeof(UseableGun), "isReloading", BindingFlags.Instance | BindingFlags.NonPublic);
	public static ReflectedField<bool> IsHammeringField = new ReflectedField<bool>(typeof(UseableGun), "isHammering", BindingFlags.Instance | BindingFlags.NonPublic);
	public static ReflectedField<bool> IsUnjammingField = new ReflectedField<bool>(typeof(UseableGun), "isUnjamming", BindingFlags.Instance | BindingFlags.NonPublic);
	public static ReflectedField<bool> IsAttachingField = new ReflectedField<bool>(typeof(UseableGun), "isAttaching", BindingFlags.Instance | BindingFlags.NonPublic);
	public static ReflectedField<bool> WasTriggerJustPulledField = new ReflectedField<bool>(typeof(UseableGun), "wasTriggerJustPulled", BindingFlags.Instance | BindingFlags.NonPublic);
	public static ReflectedField<bool> IsFiredField = new ReflectedField<bool>(typeof(UseableGun), "isFired", BindingFlags.Instance | BindingFlags.NonPublic);
	public static ReflectedField<int> BurstsField = new ReflectedField<int>(typeof(UseableGun), "bursts", BindingFlags.Instance | BindingFlags.NonPublic);
	public static ReflectedField<int> FireDelayCounterField = new ReflectedField<int>(typeof(UseableGun), "fireDelayCounter", BindingFlags.Instance | BindingFlags.NonPublic);
	public static ReflectedField<uint> LastFireField = new ReflectedField<uint>(typeof(UseableGun), "lastFire", BindingFlags.Instance | BindingFlags.NonPublic);
	private static readonly float SHAKE_CROUCH = 0.85f;
	private static readonly float SHAKE_PRONE = 0.7f;
	public static List<DelayedBullet> ActiveBullets = new List<DelayedBullet>();

AudioClip LoadWav(string path, float volume)
{
	byte[] data = System.IO.File.ReadAllBytes(path);
	int channels = BitConverter.ToInt16(data, 22);
	int sampleRate = BitConverter.ToInt32(data, 24);
	int bitsPerSample = BitConverter.ToInt16(data, 34);
	int dataOffset = 12;
	while (dataOffset < data.Length - 8)
	{
		if (data[dataOffset] == 100 && data[dataOffset+1] == 97 && data[dataOffset+2] == 116 && data[dataOffset+3] == 97)
		{
			dataOffset += 8;
			break;
		}
		int chunkSize = BitConverter.ToInt32(data, dataOffset + 4);
		dataOffset += 8 + chunkSize;
	}
	int dataSize = data.Length - dataOffset;
	if (bitsPerSample == 16)
	{
		int sampleCount = dataSize / 2;
		float[] samples = new float[sampleCount];
		for (int i = 0; i < sampleCount; i++)
			samples[i] = BitConverter.ToInt16(data, dataOffset + i * 2) / 32768f * volume;
		AudioClip clip = AudioClip.Create("WavClip", sampleCount / channels, channels, sampleRate, false);
		clip.SetData(samples, 0);
		return clip;
	}
	else if (bitsPerSample == 8)
	{
		int sampleCount = dataSize;
		float[] samples = new float[sampleCount];
		for (int i = 0; i < sampleCount; i++)
			samples[i] = (data[dataOffset + i] - 128) / 128f * volume;
		AudioClip clip = AudioClip.Create("WavClip", sampleCount / channels, channels, sampleRate, false);
		clip.SetData(samples, 0);
		return clip;
	}
	return null;
}
}
