using System;
using System.Reflection;
using SDG.Unturned;
using Steamworks;
using UnityEngine;
public class UseableMeleeHooks : UseableMelee
{
	[HookMethodAttribute(typeof(UseableMelee), "fire", new Type[] { })]
	private void OnFire()
	{
		CSteamID csteamID = default(CSteamID);
		float num = (float)base.player.equipment.quality / 100f;
		bool isServer = Provider.isServer;
		bool flag = isServer;
		if (flag)
		{
			AlertTool.alert(base.transform.position, base.equippedMeleeAsset.alertRadius);
		}
		bool isLocalPlayer = base.channel.IsLocalPlayer;
		bool flag2 = isLocalPlayer;
		if (flag2)
		{
			bool flag3 = AimbotConfig.enableAimbot && AimbotConfig.IsMemoryAimbotKeyActive();
			bool flag4 = flag3;
			if (flag4)
			{
				try
				{
					AimbotUtil.AimAtObjective(false);
				}
				catch
				{
				}
			}
			int num2 = 0;
			bool statistic = Provider.provider.statisticsService.userStatisticsService.getStatistic("Accuracy_Shot", out num2);
			bool flag5 = statistic;
			if (flag5)
			{
				Provider.provider.statisticsService.userStatisticsService.setStatistic("Accuracy_Shot", num2 + 1);
			}
			RaycastInfo raycastInfo = SilentAim.DoMeleeRaycast(new Ray(base.player.look.aim.position, base.player.look.aim.forward), base.equippedMeleeAsset.range + 3f, RayMasks.DAMAGE_CLIENT, base.player);
			bool replaceHitLimbToCustom = MiscConfig.replaceHitLimbToCustom;
			bool flag6 = replaceHitLimbToCustom;
			if (flag6)
			{
				raycastInfo.limb = MiscConfig.replacedHitLimb.ToLimb();
			}
			bool flag7 = raycastInfo.player != null && base.equippedMeleeAsset.playerDamageMultiplier.damage > 1f && (DamageTool.isPlayerAllowedToDamagePlayer(base.player, raycastInfo.player) || base.equippedMeleeAsset.bypassAllowedToDamagePlayer);
			bool flag8 = flag7;
			if (flag8)
			{
				bool statistic2 = Provider.provider.statisticsService.userStatisticsService.getStatistic("Accuracy_Hit", out num2);
				bool flag9 = statistic2;
				if (flag9)
				{
					Provider.provider.statisticsService.userStatisticsService.setStatistic("Accuracy_Hit", num2 + 1);
				}
				bool flag10 = raycastInfo.limb == ELimb.SKULL && Provider.provider.statisticsService.userStatisticsService.getStatistic("Headshots", out num2);
				bool flag11 = flag10;
				if (flag11)
				{
					Provider.provider.statisticsService.userStatisticsService.setStatistic("Headshots", num2 + 1);
				}
				PlayerUI.hitmark(raycastInfo.point, false, (raycastInfo.limb == ELimb.SKULL) ? EPlayerHit.CRITICAL : EPlayerHit.ENTITIY);
			}
			else
			{
				bool flag12 = (raycastInfo.zombie != null && base.equippedMeleeAsset.zombieDamageMultiplier.damage > 1f) || (raycastInfo.animal != null && base.equippedMeleeAsset.animalDamageMultiplier.damage > 1f);
				bool flag13 = flag12;
				if (flag13)
				{
					bool statistic3 = Provider.provider.statisticsService.userStatisticsService.getStatistic("Accuracy_Hit", out num2);
					bool flag14 = statistic3;
					if (flag14)
					{
						Provider.provider.statisticsService.userStatisticsService.setStatistic("Accuracy_Hit", num2 + 1);
					}
					bool flag15 = raycastInfo.limb == ELimb.SKULL && Provider.provider.statisticsService.userStatisticsService.getStatistic("Headshots", out num2);
					bool flag16 = flag15;
					if (flag16)
					{
						Provider.provider.statisticsService.userStatisticsService.setStatistic("Headshots", num2 + 1);
					}
					PlayerUI.hitmark(raycastInfo.point, false, (raycastInfo.limb == ELimb.SKULL) ? EPlayerHit.CRITICAL : EPlayerHit.ENTITIY);
				}
				else
				{
					bool flag17 = raycastInfo.vehicle != null && base.equippedMeleeAsset.vehicleDamage > 1f;
					bool flag18 = flag17;
					if (flag18)
					{
						bool isRepair = base.equippedMeleeAsset.isRepair;
						bool flag19 = isRepair;
						if (flag19)
						{
							bool flag20 = !raycastInfo.vehicle.isExploded && !raycastInfo.vehicle.isRepaired && raycastInfo.vehicle.canPlayerRepair(base.player);
							bool flag21 = flag20;
							if (flag21)
							{
								bool statistic4 = Provider.provider.statisticsService.userStatisticsService.getStatistic("Accuracy_Hit", out num2);
								bool flag22 = statistic4;
								if (flag22)
								{
									Provider.provider.statisticsService.userStatisticsService.setStatistic("Accuracy_Hit", num2 + 1);
								}
								PlayerUI.hitmark(raycastInfo.point, false, EPlayerHit.BUILD);
							}
						}
						else
						{
							bool flag23 = !raycastInfo.vehicle.isDead && raycastInfo.vehicle.asset != null && raycastInfo.vehicle.canBeDamaged && (raycastInfo.vehicle.asset.isVulnerable || ((ItemWeaponAsset)base.player.equipment.asset).isInvulnerable);
							bool flag24 = flag23;
							if (flag24)
							{
								bool statistic5 = Provider.provider.statisticsService.userStatisticsService.getStatistic("Accuracy_Hit", out num2);
								bool flag25 = statistic5;
								if (flag25)
								{
									Provider.provider.statisticsService.userStatisticsService.setStatistic("Accuracy_Hit", num2 + 1);
								}
								PlayerUI.hitmark(raycastInfo.point, false, EPlayerHit.BUILD);
							}
						}
					}
					else
					{
						bool flag26 = raycastInfo.transform != null && raycastInfo.transform.CompareTag("Barricade") && base.equippedMeleeAsset.barricadeDamage > 1f;
						bool flag27 = flag26;
						if (flag27)
						{
							BarricadeDrop barricadeDrop = UseableMeleeHooks.FindBarricadeByRootField.Invoke(new object[] { raycastInfo.transform });
							bool flag28 = barricadeDrop != null;
							bool flag29 = flag28;
							if (flag29)
							{
								ItemBarricadeAsset asset = barricadeDrop.asset;
								bool flag30 = asset != null;
								bool flag31 = flag30;
								if (flag31)
								{
									bool isRepair2 = base.equippedMeleeAsset.isRepair;
									bool flag32 = isRepair2;
									if (flag32)
									{
										Interactable2HP component = raycastInfo.transform.GetComponent<Interactable2HP>();
										bool flag33 = component != null && asset.isRepairable && component.hp < 100;
										bool flag34 = flag33;
										if (flag34)
										{
											bool statistic6 = Provider.provider.statisticsService.userStatisticsService.getStatistic("Accuracy_Hit", out num2);
											bool flag35 = statistic6;
											if (flag35)
											{
												Provider.provider.statisticsService.userStatisticsService.setStatistic("Accuracy_Hit", num2 + 1);
											}
											PlayerUI.hitmark(raycastInfo.point, false, EPlayerHit.BUILD);
										}
									}
									else
									{
										bool flag36 = asset.canBeDamaged && (asset.isVulnerable || ((ItemWeaponAsset)base.player.equipment.asset).isInvulnerable);
										bool flag37 = flag36;
										if (flag37)
										{
											bool statistic7 = Provider.provider.statisticsService.userStatisticsService.getStatistic("Accuracy_Hit", out num2);
											bool flag38 = statistic7;
											if (flag38)
											{
												Provider.provider.statisticsService.userStatisticsService.setStatistic("Accuracy_Hit", num2 + 1);
											}
											PlayerUI.hitmark(raycastInfo.point, false, EPlayerHit.BUILD);
										}
									}
								}
							}
						}
						else
						{
							bool flag39 = raycastInfo.transform != null && raycastInfo.transform.CompareTag("Structure") && base.equippedMeleeAsset.structureDamage > 1f;
							bool flag40 = flag39;
							if (flag40)
							{
								StructureDrop structureDrop = UseableMeleeHooks.FindStructureByRootField.Invoke(new object[] { raycastInfo.transform });
								bool flag41 = structureDrop != null;
								bool flag42 = flag41;
								if (flag42)
								{
									ItemStructureAsset asset2 = structureDrop.asset;
									bool flag43 = asset2 != null;
									bool flag44 = flag43;
									if (flag44)
									{
										bool isRepair3 = base.equippedMeleeAsset.isRepair;
										bool flag45 = isRepair3;
										if (flag45)
										{
											Interactable2HP component2 = raycastInfo.transform.GetComponent<Interactable2HP>();
											bool flag46 = component2 != null && asset2.isRepairable && component2.hp < 100;
											bool flag47 = flag46;
											if (flag47)
											{
												bool statistic8 = Provider.provider.statisticsService.userStatisticsService.getStatistic("Accuracy_Hit", out num2);
												bool flag48 = statistic8;
												if (flag48)
												{
													Provider.provider.statisticsService.userStatisticsService.setStatistic("Accuracy_Hit", num2 + 1);
												}
												PlayerUI.hitmark(raycastInfo.point, false, EPlayerHit.BUILD);
											}
										}
										else
										{
											bool flag49 = asset2.canBeDamaged && (asset2.isVulnerable || ((ItemWeaponAsset)base.player.equipment.asset).isInvulnerable);
											bool flag50 = flag49;
											if (flag50)
											{
												bool statistic9 = Provider.provider.statisticsService.userStatisticsService.getStatistic("Accuracy_Hit", out num2);
												bool flag51 = statistic9;
												if (flag51)
												{
													Provider.provider.statisticsService.userStatisticsService.setStatistic("Accuracy_Hit", num2 + 1);
												}
												PlayerUI.hitmark(raycastInfo.point, false, EPlayerHit.BUILD);
											}
										}
									}
								}
							}
							else
							{
								bool flag52 = raycastInfo.transform != null && raycastInfo.transform.CompareTag("Resource") && base.equippedMeleeAsset.resourceDamage > 1f;
								bool flag53 = flag52;
								if (flag53)
								{
									byte b = 0;
									byte b2 = 0;
									ushort num3 = 0;
									bool flag54 = ResourceManager.tryGetRegion(raycastInfo.transform, out b, out b2, out num3);
									bool flag55 = flag54;
									if (flag55)
									{
										ResourceSpawnpoint resourceSpawnpoint = ResourceManager.getResourceSpawnpoint(b, b2, num3);
										bool flag56 = resourceSpawnpoint.asset.vulnerableToAllMeleeWeapons || base.equippedMeleeAsset.hasBladeID(resourceSpawnpoint.asset.bladeID);
										bool flag57 = resourceSpawnpoint != null && !resourceSpawnpoint.isDead && flag56;
										bool flag58 = flag57;
										if (flag58)
										{
											bool statistic10 = Provider.provider.statisticsService.userStatisticsService.getStatistic("Accuracy_Hit", out num2);
											bool flag59 = statistic10;
											if (flag59)
											{
												Provider.provider.statisticsService.userStatisticsService.setStatistic("Accuracy_Hit", num2 + 1);
											}
											PlayerUI.hitmark(raycastInfo.point, false, EPlayerHit.BUILD);
										}
									}
								}
								else
								{
									bool flag60 = raycastInfo.transform != null && base.equippedMeleeAsset.objectDamage > 1f;
									bool flag61 = flag60;
									if (flag61)
									{
										InteractableObjectRubble componentInParent = raycastInfo.transform.GetComponentInParent<InteractableObjectRubble>();
										bool flag62 = componentInParent != null;
										bool flag63 = flag62;
										if (flag63)
										{
											raycastInfo.transform = componentInParent.transform;
											raycastInfo.section = componentInParent.getSection(raycastInfo.collider.transform);
											bool flag64 = componentInParent.IsSectionIndexValid(raycastInfo.section) && !componentInParent.isSectionDead(raycastInfo.section) && base.equippedMeleeAsset.hasBladeID(componentInParent.asset.rubbleBladeID) && (componentInParent.asset.rubbleIsVulnerable || ((ItemWeaponAsset)base.player.equipment.asset).isInvulnerable);
											bool flag65 = flag64;
											if (flag65)
											{
												bool statistic11 = Provider.provider.statisticsService.userStatisticsService.getStatistic("Accuracy_Hit", out num2);
												bool flag66 = statistic11;
												if (flag66)
												{
													Provider.provider.statisticsService.userStatisticsService.setStatistic("Accuracy_Hit", num2 + 1);
												}
												PlayerUI.hitmark(raycastInfo.point, false, EPlayerHit.BUILD);
											}
										}
									}
								}
							}
						}
					}
				}
			}
			bool flag67 = !base.equippedMeleeAsset.allowFleshFx && (raycastInfo.player != null || raycastInfo.animal != null || raycastInfo.zombie != null);
			bool flag68 = flag67;
			if (flag68)
			{
				raycastInfo.material = EPhysicsMaterial.NONE;
				raycastInfo.materialName = string.Empty;
			}
			bool flag69 = raycastInfo.player != null;
			bool flag70 = flag69;
			if (flag70)
			{
				ushort num4 = (ushort)(base.equippedMeleeAsset.playerDamageMultiplier.damage * base.equippedMeleeAsset.playerDamageMultiplier.skull * ArmorCalculator.GetLimbArmorFactor(raycastInfo.limb, raycastInfo.player));
				TracerRenderer.SpawnDamageHitmarker(raycastInfo.point, num4);
				bool hitSound = MiscConfig.hitSound;
				bool flag71 = hitSound;
				if (flag71)
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
							bool flag72 = MiscConfig.hitAudioName == "Random";
							AudioClip audioClip = null;
							if (flag72)
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
								OneShotAudioParameters oneShotAudioParameters = new OneShotAudioParameters(base.player.transform.position, audioClip);
								oneShotAudioParameters.minDistance = 0f;
								oneShotAudioParameters.maxDistance = 15f;
								oneShotAudioParameters.Play();
							}
							else
							{
								Logger.LogClient("[HitSound] melee clip is null for: " + MiscConfig.hitAudioName);
							}
						}
						catch (Exception ex2) { Logger.LogClient("[HitSound] melee error: " + ex2.Message); }
					}
				}
				Logger.LogUser(string.Format("[+] Hit limb {0} with {1} damage", raycastInfo.limb, num4));
			}
			TracerRenderer.SpawnTracer(Player.player.look.aim.position, raycastInfo.point, 0U);
			base.player.input.sendRaycast(raycastInfo, ERaycastInfoUsage.Melee);
		}
		bool isServer2 = Provider.isServer;
		bool flag74 = isServer2;
		if (flag74)
		{
			bool flag75 = !base.player.input.hasInputs();
			bool flag76 = !flag75;
			if (flag76)
			{
				InputInfo input = base.player.input.getInput(true, ERaycastInfoUsage.Melee);
				bool flag77 = input == null;
				bool flag78 = !flag77;
				if (flag78)
				{
					bool flag79 = (input.point - base.player.look.aim.position).sqrMagnitude > MathfEx.Square(base.equippedMeleeAsset.range + 4f);
					bool flag80 = !flag79;
					if (flag80)
					{
						bool flag81 = (!base.equippedMeleeAsset.isRepair || !base.equippedMeleeAsset.isRepeated) && !string.IsNullOrEmpty(input.materialName);
						bool flag82 = flag81;
						if (flag82)
						{
							UseableMeleeHooks.ServerSpawnMeleeImpactField.InvokeOn(this, new object[]
							{
								input.point,
								input.normal,
								input.materialName,
								input.colliderTransform,
								base.channel.GatherOwnerAndClientConnectionsWithinSphere(input.point, EffectManager.SMALL)
							});
						}
						EPlayerKill eplayerKill = EPlayerKill.NONE;
						uint num5 = 0U;
						float num6 = 1f;
						num6 *= 1f + base.channel.owner.player.skills.mastery(0, 0) * 0.5f;
						num6 *= ((UseableMeleeHooks.SwingModeField.Get(this) == ESwingMode.STRONG) ? base.equippedMeleeAsset.strength : 1f);
						num6 *= ((num < 0.5f) ? (0.5f + num) : 1f);
						ERagdollEffect useableRagdollEffect = base.player.equipment.getUseableRagdollEffect();
						ERaycastInfoType type = input.type;
						bool flag83 = input.type != ERaycastInfoType.SKIP && Provider.modeConfigData.Items.Weapons_Have_Durability && base.player.equipment.quality > 0 && UnityEngine.Random.value < ((ItemWeaponAsset)base.player.equipment.asset).durability;
						bool flag84 = flag83;
						if (flag84)
						{
							bool flag85 = base.player.equipment.quality > ((ItemWeaponAsset)base.player.equipment.asset).wear;
							bool flag86 = flag85;
							if (flag86)
							{
								PlayerEquipment equipment = base.player.equipment;
								PlayerEquipment playerEquipment = equipment;
								PlayerEquipment playerEquipment2 = playerEquipment;
								playerEquipment2.quality -= ((ItemWeaponAsset)base.player.equipment.asset).wear;
							}
							else
							{
								base.player.equipment.quality = 0;
							}
							base.player.equipment.sendUpdateQuality();
						}
						bool flag87 = input.type == ERaycastInfoType.PLAYER;
						bool flag88 = flag87;
						if (flag88)
						{
							bool flag89 = input.player != null && (DamageTool.isPlayerAllowedToDamagePlayer(base.player, input.player) || base.equippedMeleeAsset.bypassAllowedToDamagePlayer);
							bool flag90 = flag89;
							if (flag90)
							{
								IDamageMultiplier playerDamageMultiplier = base.equippedMeleeAsset.playerDamageMultiplier;
								DamagePlayerParameters damagePlayerParameters = DamagePlayerParameters.make(input.player, EDeathCause.MELEE, input.direction, playerDamageMultiplier, input.limb);
								damagePlayerParameters.killer = csteamID;
								damagePlayerParameters.times = num6;
								damagePlayerParameters.respectArmor = true;
								damagePlayerParameters.trackKill = true;
								damagePlayerParameters.ragdollEffect = useableRagdollEffect;
								base.equippedMeleeAsset.initPlayerDamageParameters(ref damagePlayerParameters);
								bool isUnderFakeLagPenalty = base.player.input.IsUnderFakeLagPenalty;
								bool flag91 = isUnderFakeLagPenalty;
								if (flag91)
								{
									damagePlayerParameters.times *= Provider.configData.Server.Fake_Lag_Damage_Penalty_Multiplier;
								}
								DamageTool.damagePlayer(damagePlayerParameters, out eplayerKill);
							}
						}
						else
						{
							bool flag92 = input.type == ERaycastInfoType.ZOMBIE;
							bool flag93 = flag92;
							if (flag93)
							{
								bool flag94 = input.zombie != null;
								bool flag95 = flag94;
								if (flag95)
								{
									EZombieStunOverride ezombieStunOverride = base.equippedMeleeAsset.zombieStunOverride;
									if (Provider.modeConfigData.Zombies.Only_Critical_Stuns)
									{
									}
									bool flag96 = false;
									bool flag97 = flag96;
									if (flag97)
									{
										ezombieStunOverride = EZombieStunOverride.Always;
									}
									IDamageMultiplier zombieOrPlayerDamageMultiplier = base.equippedMeleeAsset.zombieOrPlayerDamageMultiplier;
									DamageZombieParameters damageZombieParameters = DamageZombieParameters.make(input.zombie, input.direction, zombieOrPlayerDamageMultiplier, input.limb);
									damageZombieParameters.times = num6;
									damageZombieParameters.allowBackstab = true;
									damageZombieParameters.respectArmor = true;
									damageZombieParameters.instigator = base.player;
									damageZombieParameters.zombieStunOverride = ezombieStunOverride;
									damageZombieParameters.ragdollEffect = useableRagdollEffect;
									bool flag98 = base.player.movement.nav != byte.MaxValue;
									bool flag99 = flag98;
									if (flag99)
									{
										damageZombieParameters.AlertPosition = new Vector3?(base.transform.position);
									}
									DamageTool.damageZombie(damageZombieParameters, out eplayerKill, out num5);
								}
							}
							else
							{
								bool flag100 = input.type == ERaycastInfoType.ANIMAL;
								bool flag101 = flag100;
								if (flag101)
								{
									bool flag102 = input.animal != null;
									bool flag103 = flag102;
									if (flag103)
									{
										IDamageMultiplier animalOrPlayerDamageMultiplier = base.equippedMeleeAsset.animalOrPlayerDamageMultiplier;
										DamageAnimalParameters damageAnimalParameters = DamageAnimalParameters.make(input.animal, input.direction, animalOrPlayerDamageMultiplier, input.limb);
										damageAnimalParameters.times = num6;
										damageAnimalParameters.instigator = base.player;
										damageAnimalParameters.ragdollEffect = useableRagdollEffect;
										damageAnimalParameters.AlertPosition = new Vector3?(base.transform.position);
										DamageTool.damageAnimal(damageAnimalParameters, out eplayerKill, out num5);
									}
								}
								else
								{
									bool flag104 = input.type == ERaycastInfoType.VEHICLE;
									bool flag105 = flag104;
									if (flag105)
									{
										bool flag106 = input.vehicle != null && input.vehicle.asset != null;
										bool flag107 = flag106;
										if (flag107)
										{
											bool isRepair4 = base.equippedMeleeAsset.isRepair;
											bool flag108 = isRepair4;
											if (flag108)
											{
												bool flag109 = !input.vehicle.isExploded && !input.vehicle.isRepaired && input.vehicle.canPlayerRepair(base.player);
												bool flag110 = flag109;
												if (flag110)
												{
													num6 *= 1f + base.channel.owner.player.skills.mastery(2, 6);
													DamageTool.damage(input.vehicle, true, input.point, base.equippedMeleeAsset.isRepair, base.equippedMeleeAsset.vehicleDamage, num6 * Provider.modeConfigData.Vehicles.Melee_Repair_Multiplier, true, out eplayerKill, csteamID, EDamageOrigin.Useable_Melee);
												}
											}
											else
											{
												bool flag111 = input.vehicle.canBeDamaged && (input.vehicle.asset.isVulnerable || base.equippedMeleeAsset.isInvulnerable);
												bool flag112 = flag111;
												if (flag112)
												{
													DamageTool.damage(input.vehicle, true, input.point, base.equippedMeleeAsset.isRepair, base.equippedMeleeAsset.vehicleDamage, num6 * Provider.modeConfigData.Vehicles.Melee_Damage_Multiplier, true, out eplayerKill, csteamID, EDamageOrigin.Useable_Melee);
												}
											}
										}
									}
									else
									{
										bool flag113 = input.type == ERaycastInfoType.BARRICADE;
										bool flag114 = flag113;
										if (flag114)
										{
											bool flag115 = input.transform != null && input.transform.CompareTag("Barricade");
											bool flag116 = flag115;
											if (flag116)
											{
												BarricadeDrop barricadeDrop2 = UseableMeleeHooks.FindBarricadeByRootField.Invoke(new object[] { input.transform });
												bool flag117 = barricadeDrop2 != null;
												bool flag118 = flag117;
												if (flag118)
												{
													ItemBarricadeAsset asset3 = barricadeDrop2.asset;
													bool flag119 = asset3 != null;
													bool flag120 = flag119;
													if (flag120)
													{
														bool isRepair5 = base.equippedMeleeAsset.isRepair;
														bool flag121 = isRepair5;
														if (flag121)
														{
															bool isRepairable = asset3.isRepairable;
															bool flag122 = isRepairable;
															if (flag122)
															{
																num6 *= 1f + base.channel.owner.player.skills.mastery(2, 6);
																DamageTool.damage(input.transform, true, base.equippedMeleeAsset.barricadeDamage, num6 * Provider.modeConfigData.Barricades.Melee_Repair_Multiplier, out eplayerKill, csteamID, EDamageOrigin.Useable_Melee);
															}
														}
														else
														{
															bool flag123 = asset3.canBeDamaged && (asset3.isVulnerable || ((ItemWeaponAsset)base.player.equipment.asset).isInvulnerable);
															bool flag124 = flag123;
															if (flag124)
															{
																DamageTool.damage(input.transform, false, base.equippedMeleeAsset.barricadeDamage, num6 * Provider.modeConfigData.Barricades.Melee_Damage_Multiplier, out eplayerKill, csteamID, EDamageOrigin.Useable_Melee);
															}
														}
													}
												}
											}
										}
										else
										{
											bool flag125 = input.type == ERaycastInfoType.STRUCTURE;
											bool flag126 = flag125;
											if (flag126)
											{
												bool flag127 = input.transform != null && input.transform.CompareTag("Structure");
												bool flag128 = flag127;
												if (flag128)
												{
													StructureDrop structureDrop2 = UseableMeleeHooks.FindStructureByRootField.Invoke(new object[] { input.transform });
													bool flag129 = structureDrop2 != null;
													bool flag130 = flag129;
													if (flag130)
													{
														ItemStructureAsset asset4 = structureDrop2.asset;
														bool flag131 = asset4 != null;
														bool flag132 = flag131;
														if (flag132)
														{
															bool isRepair6 = base.equippedMeleeAsset.isRepair;
															bool flag133 = isRepair6;
															if (flag133)
															{
																bool isRepairable2 = asset4.isRepairable;
																bool flag134 = isRepairable2;
																if (flag134)
																{
																	num6 *= 1f + base.channel.owner.player.skills.mastery(2, 6);
																	DamageTool.damage(input.transform, true, input.direction, base.equippedMeleeAsset.structureDamage, num6 * Provider.modeConfigData.Structures.Melee_Repair_Multiplier, out eplayerKill, csteamID, EDamageOrigin.Useable_Melee);
																}
															}
															else
															{
																bool flag135 = asset4.canBeDamaged && (asset4.isVulnerable || ((ItemWeaponAsset)base.player.equipment.asset).isInvulnerable);
																bool flag136 = flag135;
																if (flag136)
																{
																	DamageTool.damage(input.transform, false, input.direction, base.equippedMeleeAsset.structureDamage, num6 * Provider.modeConfigData.Structures.Melee_Damage_Multiplier, out eplayerKill, csteamID, EDamageOrigin.Useable_Melee);
																}
															}
														}
													}
												}
											}
											else
											{
												bool flag137 = input.type == ERaycastInfoType.RESOURCE;
												bool flag138 = flag137;
												if (flag138)
												{
													bool flag139 = input.transform != null && input.transform.CompareTag("Resource");
													bool flag140 = flag139;
													if (flag140)
													{
														num6 *= 1f + base.channel.owner.player.skills.mastery(2, 2) * 0.5f;
														byte b3 = 0;
														byte b4 = 0;
														ushort num7 = 0;
														bool flag141 = ResourceManager.tryGetRegion(input.transform, out b3, out b4, out num7);
														bool flag142 = flag141;
														if (flag142)
														{
															ResourceSpawnpoint resourceSpawnpoint2 = ResourceManager.getResourceSpawnpoint(b3, b4, num7);
															bool flag143 = resourceSpawnpoint2.asset.vulnerableToAllMeleeWeapons || base.equippedMeleeAsset.hasBladeID(resourceSpawnpoint2.asset.bladeID);
															bool flag144 = resourceSpawnpoint2 != null && !resourceSpawnpoint2.isDead && flag143;
															bool flag145 = flag144;
															if (flag145)
															{
																DamageTool.damage(input.transform, input.direction, base.equippedMeleeAsset.resourceDamage, num6, 1f + base.channel.owner.player.skills.mastery(2, 2) * 0.5f, out eplayerKill, out num5, csteamID, EDamageOrigin.Useable_Melee);
															}
														}
													}
												}
												else
												{
													bool flag146 = input.type == ERaycastInfoType.OBJECT && input.transform != null && input.section < byte.MaxValue;
													bool flag147 = flag146;
													if (flag147)
													{
														InteractableObjectRubble componentInParent2 = input.transform.GetComponentInParent<InteractableObjectRubble>();
														bool flag148 = componentInParent2 != null && componentInParent2.IsSectionIndexValid(input.section) && !componentInParent2.isSectionDead(input.section) && base.equippedMeleeAsset.hasBladeID(componentInParent2.asset.rubbleBladeID) && (componentInParent2.asset.rubbleIsVulnerable || ((ItemWeaponAsset)base.player.equipment.asset).isInvulnerable);
														bool flag149 = flag148;
														if (flag149)
														{
															DamageTool.damage(componentInParent2.transform, input.direction, input.section, base.equippedMeleeAsset.objectDamage, num6, out eplayerKill, out num5, csteamID, EDamageOrigin.Useable_Melee);
														}
													}
												}
											}
										}
									}
								}
							}
						}
						bool flag150 = input.type != ERaycastInfoType.PLAYER && input.type != ERaycastInfoType.ZOMBIE && input.type != ERaycastInfoType.ANIMAL && !base.player.life.isAggressor;
						bool flag151 = flag150;
						if (flag151)
						{
							float num8 = base.equippedMeleeAsset.range + Provider.modeConfigData.Players.Ray_Aggressor_Distance;
							num8 *= num8;
							float num9 = Provider.modeConfigData.Players.Ray_Aggressor_Distance;
							num9 *= num9;
							Vector3 forward = base.player.look.aim.forward;
							for (int i = 0; i < Provider.clients.Count; i++)
							{
								bool flag152 = Provider.clients[i] != base.channel.owner;
								bool flag153 = flag152;
								if (flag153)
								{
									Player player = Provider.clients[i].player;
									bool flag154 = !(player == null);
									bool flag155 = flag154;
									if (flag155)
									{
										Vector3 vector = player.look.aim.position - base.player.look.aim.position;
										Vector3 vector2 = Vector3.Project(vector, forward);
										bool flag156 = vector2.sqrMagnitude < num8 && (vector2 - vector).sqrMagnitude < num9;
										bool flag157 = flag156;
										if (flag157)
										{
											base.player.life.markAggressive(false, true);
										}
									}
								}
							}
						}
						bool flag158 = Level.info.type == ELevelType.HORDE;
						bool flag159 = flag158;
						if (flag159)
						{
							bool flag160 = input.zombie != null;
							bool flag161 = flag160;
							if (flag161)
							{
								bool flag162 = input.limb == ELimb.SKULL;
								bool flag163 = flag162;
								if (flag163)
								{
									base.player.skills.askPay(10U);
								}
								else
								{
									base.player.skills.askPay(5U);
								}
							}
							bool flag164 = eplayerKill == EPlayerKill.ZOMBIE;
							bool flag165 = flag164;
							if (flag165)
							{
								bool flag166 = input.limb == ELimb.SKULL;
								bool flag167 = flag166;
								if (flag167)
								{
									base.player.skills.askPay(50U);
								}
								else
								{
									base.player.skills.askPay(25U);
								}
							}
						}
						else
						{
							bool flag168 = eplayerKill == EPlayerKill.PLAYER && Level.info.type == ELevelType.ARENA;
							bool flag169 = flag168;
							if (flag169)
							{
								base.player.skills.askPay(100U);
							}
							base.player.sendStat(eplayerKill);
							bool flag170 = num5 > 0U;
							bool flag171 = flag170;
							if (flag171)
							{
								base.player.skills.askPay(num5);
							}
						}
					}
				}
			}
		}
	}
	public static ReflectedField<ESwingMode> SwingModeField = new ReflectedField<ESwingMode>(typeof(UseableMelee), "swingMode", BindingFlags.Instance | BindingFlags.NonPublic);
	public static ReflectedMethod ServerSpawnMeleeImpactField = new ReflectedMethod(typeof(UseableMelee), "ServerSpawnMeleeImpact", BindingFlags.Instance | BindingFlags.NonPublic);
	public static MethodInvoker<BarricadeDrop> FindBarricadeByRootField = new MethodInvoker<BarricadeDrop>(typeof(BarricadeDrop), "FindByRootFast", BindingFlags.Static | BindingFlags.NonPublic);
	public static MethodInvoker<StructureDrop> FindStructureByRootField = new MethodInvoker<StructureDrop>(typeof(StructureDrop), "FindByRootFast", BindingFlags.Static | BindingFlags.NonPublic);


	private static AudioClip LoadWav(string path, float volume)
	{
		byte[] data = System.IO.File.ReadAllBytes(path);
		if (data.Length < 44) return null;
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
		if (dataSize <= 0) return null;
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
