using System;
using System.Reflection;
using SDG.Unturned;
using Steamworks;
using UnityEngine;
public class PlayerEquipmentPatches : PlayerEquipment
{
	[HookMethodAttribute(typeof(PlayerEquipment), "punch", new Type[] { })]
	private void PunchPatch(EPlayerPunch mode)
	{
		CSteamID csteamID = default(CSteamID);
		bool isLocalPlayer = base.channel.IsLocalPlayer;
		bool flag = isLocalPlayer;
		if (flag)
		{
			bool flag2 = AimbotConfig.enableAimbot && AimbotConfig.IsMemoryAimbotKeyActive();
			bool flag3 = flag2;
			if (flag3)
			{
				try
				{
					AimbotUtil.AimAtObjective(true);
				}
				catch
				{
				}
			}
			PlayerEquipmentPatches.PlayPunchAudioClipMethod.InvokeOn(this, Array.Empty<object>());
			RaycastInfo raycastInfo = AimbotUtil.MeleeSilentAimRaycast(new Ray(base.player.look.aim.position, base.player.look.aim.forward), MiscConfig.extendMeleeRange ? 6f : 1.75f, RayMasks.DAMAGE_CLIENT, base.player);
			bool replaceHitLimbToCustom = MiscConfig.replaceHitLimbToCustom;
			bool flag4 = replaceHitLimbToCustom;
			if (flag4)
			{
				raycastInfo.limb = MiscConfig.replacedHitLimb.ToLimb();
			}
			bool flag5 = raycastInfo.player != null && PlayerEquipmentPatches.DAMAGE_PLAYER_MULTIPLIER.damage > 1f && DamageTool.isPlayerAllowedToDamagePlayer(base.player, raycastInfo.player);
			bool flag6 = flag5;
			if (flag6)
			{
				PlayerUI.hitmark(raycastInfo.point, false, (raycastInfo.limb == ELimb.SKULL) ? EPlayerHit.CRITICAL : EPlayerHit.ENTITIY);
			}
			else
			{
				bool flag7 = (raycastInfo.zombie != null && PlayerEquipmentPatches.DAMAGE_ZOMBIE_MULTIPLIER.damage > 1f) || (raycastInfo.animal != null && PlayerEquipmentPatches.DAMAGE_ANIMAL_MULTIPLIER.damage > 1f);
				bool flag8 = flag7;
				if (flag8)
				{
					PlayerUI.hitmark(raycastInfo.point, false, (raycastInfo.limb == ELimb.SKULL) ? EPlayerHit.CRITICAL : EPlayerHit.ENTITIY);
				}
				else
				{
					bool flag9 = raycastInfo.transform != null && raycastInfo.transform.CompareTag("Barricade") && PlayerEquipmentPatches.DAMAGE_BARRICADE > 1f;
					bool flag10 = flag9;
					if (flag10)
					{
						BarricadeDrop barricadeDrop = PlayerEquipmentPatches.BarricadeFindByRootFastMethod.Invoke(new object[] { raycastInfo.transform });
						bool flag11 = barricadeDrop != null;
						bool flag12 = flag11;
						if (flag12)
						{
							ItemBarricadeAsset asset = barricadeDrop.asset;
							bool flag13 = asset != null && asset.canBeDamaged && asset.isVulnerable;
							bool flag14 = flag13;
							if (flag14)
							{
								PlayerUI.hitmark(raycastInfo.point, false, EPlayerHit.BUILD);
							}
						}
					}
					else
					{
						bool flag15 = raycastInfo.transform != null && raycastInfo.transform.CompareTag("Structure") && PlayerEquipmentPatches.DAMAGE_STRUCTURE > 1f;
						bool flag16 = flag15;
						if (flag16)
						{
							StructureDrop structureDrop = PlayerEquipmentPatches.StructureFindByRootFastMethod.Invoke(new object[] { raycastInfo.transform });
							bool flag17 = structureDrop != null;
							bool flag18 = flag17;
							if (flag18)
							{
								ItemStructureAsset asset2 = structureDrop.asset;
								bool flag19 = asset2 != null && asset2.canBeDamaged && asset2.isVulnerable;
								bool flag20 = flag19;
								if (flag20)
								{
									PlayerUI.hitmark(raycastInfo.point, false, EPlayerHit.BUILD);
								}
							}
						}
						else
						{
							bool flag21 = raycastInfo.vehicle != null && !raycastInfo.vehicle.isDead && PlayerEquipmentPatches.DAMAGE_VEHICLE > 1f;
							bool flag22 = flag21;
							if (flag22)
							{
								bool flag23 = raycastInfo.vehicle.asset != null && raycastInfo.vehicle.canBeDamaged && raycastInfo.vehicle.asset.isVulnerable;
								bool flag24 = flag23;
								if (flag24)
								{
									PlayerUI.hitmark(raycastInfo.point, false, EPlayerHit.BUILD);
								}
							}
							else
							{
								bool flag25 = raycastInfo.transform != null && raycastInfo.transform.CompareTag("Resource") && PlayerEquipmentPatches.DAMAGE_RESOURCE > 1f;
								bool flag26 = flag25;
								if (flag26)
								{
									byte b = 0;
									byte b2 = 0;
									ushort num = 0;
									bool flag27 = ResourceManager.tryGetRegion(raycastInfo.transform, out b, out b2, out num);
									bool flag28 = flag27;
									if (flag28)
									{
										ResourceSpawnpoint resourceSpawnpoint = ResourceManager.getResourceSpawnpoint(b, b2, num);
										bool flag29 = resourceSpawnpoint != null && !resourceSpawnpoint.isDead && resourceSpawnpoint.asset.vulnerableToFists;
										bool flag30 = flag29;
										if (flag30)
										{
											PlayerUI.hitmark(raycastInfo.point, false, EPlayerHit.BUILD);
										}
									}
								}
								else
								{
									bool flag31 = raycastInfo.transform != null && PlayerEquipmentPatches.DAMAGE_OBJECT > 1f;
									bool flag32 = flag31;
									if (flag32)
									{
										InteractableObjectRubble componentInParent = raycastInfo.transform.GetComponentInParent<InteractableObjectRubble>();
										bool flag33 = componentInParent != null;
										bool flag34 = flag33;
										if (flag34)
										{
											raycastInfo.transform = componentInParent.transform;
											raycastInfo.section = componentInParent.getSection(raycastInfo.collider.transform);
											bool flag35 = componentInParent.IsSectionIndexValid(raycastInfo.section) && !componentInParent.isSectionDead(raycastInfo.section) && componentInParent.asset.rubbleBladeID == 0 && componentInParent.asset.rubbleIsVulnerable;
											bool flag36 = flag35;
											if (flag36)
											{
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
			bool flag37 = raycastInfo.player != null;
			bool flag38 = flag37;
			if (flag38)
			{
				ushort num2 = (ushort)(PlayerEquipmentPatches.DAMAGE_PLAYER_MULTIPLIER.damage * PlayerEquipmentPatches.DAMAGE_PLAYER_MULTIPLIER.skull * ArmorCalculator.GetLimbArmorFactor(raycastInfo.limb, raycastInfo.player));
				TracerRenderer.SpawnDamageHitmarker(raycastInfo.point, num2);
				bool hitSound = MiscConfig.hitSound;
				bool flag39 = hitSound;
				if (flag39)
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
							bool flag40 = MiscConfig.hitAudioName == "Random";
							AudioClip audioClip = null;
							if (flag40)
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
								Logger.LogClient("[HitSound] punch clip is null for: " + MiscConfig.hitAudioName);
							}
						}
						catch (Exception ex2) { Logger.LogClient("[HitSound] punch error: " + ex2.Message); }
					}
				}
				Logger.LogUser(string.Format("[+] Hit limb {0} with {1} damage", raycastInfo.limb, num2));
			}
			TracerRenderer.SpawnTracer(Player.player.look.aim.position, raycastInfo.point, 0U);
			base.player.input.sendRaycast(raycastInfo, ERaycastInfoUsage.Punch);
		}
		bool flag42 = mode == EPlayerPunch.LEFT;
		bool flag43 = flag42;
		if (flag43)
		{
			base.player.animator.play("Punch_Left", false);
			bool isServer = Provider.isServer;
			bool flag44 = isServer;
			if (flag44)
			{
				base.player.animator.sendGesture(EPlayerGesture.PUNCH_LEFT, false);
			}
		}
		else
		{
			bool flag45 = mode == EPlayerPunch.RIGHT;
			bool flag46 = flag45;
			if (flag46)
			{
				base.player.animator.play("Punch_Right", false);
				bool isServer2 = Provider.isServer;
				bool flag47 = isServer2;
				if (flag47)
				{
					base.player.animator.sendGesture(EPlayerGesture.PUNCH_RIGHT, false);
				}
			}
		}
		PlayerEquipment.OnPunch_Global.TryInvoke("OnPunch_Global", this, mode);
		bool isServer3 = Provider.isServer;
		bool flag48 = isServer3;
		if (flag48)
		{
			bool flag49 = !base.player.input.hasInputs();
			bool flag50 = !flag49;
			if (flag50)
			{
				InputInfo input = base.player.input.getInput(true, ERaycastInfoUsage.Punch);
				bool flag51 = input == null;
				bool flag52 = !flag51;
				if (flag52)
				{
					bool flag53 = (input.point - base.player.look.aim.position).sqrMagnitude > 36f;
					bool flag54 = !flag53;
					if (flag54)
					{
						PlayerEquipmentPatches.LastPunchingField.Instance(this);
						bool flag55 = !string.IsNullOrEmpty(input.materialName);
						bool flag56 = flag55;
						if (flag56)
						{
							PlayerEquipmentPatches.ServerSpawnLegacyImpactMethod.Invoke(new object[]
							{
								input.point,
								input.normal,
								input.materialName,
								input.colliderTransform,
								base.channel.GatherOwnerAndClientConnectionsWithinSphere(input.point, EffectManager.SMALL)
							});
						}
						EPlayerKill eplayerKill = EPlayerKill.NONE;
						uint num3 = 0U;
						float num4 = 1f;
						num4 *= 1f + base.channel.owner.player.skills.mastery(0, 0) * 0.5f;
						bool flag57 = input.type == ERaycastInfoType.PLAYER;
						bool flag58 = flag57;
						if (flag58)
						{
							PlayerEquipmentPatches.LastPunchingField.Set(Time.realtimeSinceStartup);
							bool flag59 = input.player != null && DamageTool.isPlayerAllowedToDamagePlayer(base.player, input.player);
							bool flag60 = flag59;
							if (flag60)
							{
								DamagePlayerParameters damagePlayerParameters = DamagePlayerParameters.make(input.player, EDeathCause.PUNCH, input.direction, PlayerEquipmentPatches.DAMAGE_PLAYER_MULTIPLIER, input.limb);
								damagePlayerParameters.killer = csteamID;
								damagePlayerParameters.times = num4;
								damagePlayerParameters.respectArmor = true;
								damagePlayerParameters.trackKill = true;
								bool isUnderFakeLagPenalty = base.player.input.IsUnderFakeLagPenalty;
								bool flag61 = isUnderFakeLagPenalty;
								if (flag61)
								{
									damagePlayerParameters.times *= Provider.configData.Server.Fake_Lag_Damage_Penalty_Multiplier;
								}
								DamageTool.damagePlayer(damagePlayerParameters, out eplayerKill);
							}
						}
						else
						{
							bool flag62 = input.type == ERaycastInfoType.ZOMBIE;
							bool flag63 = flag62;
							if (flag63)
							{
								bool flag64 = input.zombie != null;
								bool flag65 = flag64;
								if (flag65)
								{
									IDamageMultiplier damage_ZOMBIE_MULTIPLIER = PlayerEquipmentPatches.DAMAGE_ZOMBIE_MULTIPLIER;
									DamageZombieParameters damageZombieParameters = DamageZombieParameters.make(input.zombie, input.direction, damage_ZOMBIE_MULTIPLIER, input.limb);
									damageZombieParameters.times = num4;
									damageZombieParameters.allowBackstab = true;
									damageZombieParameters.respectArmor = true;
									damageZombieParameters.instigator = base.player;
									bool flag66 = base.player.movement.nav != byte.MaxValue;
									bool flag67 = flag66;
									if (flag67)
									{
										damageZombieParameters.AlertPosition = new Vector3?(base.transform.position);
									}
									DamageTool.damageZombie(damageZombieParameters, out eplayerKill, out num3);
								}
							}
							else
							{
								bool flag68 = input.type == ERaycastInfoType.ANIMAL;
								bool flag69 = flag68;
								if (flag69)
								{
									PlayerEquipmentPatches.LastPunchingField.Set(Time.realtimeSinceStartup);
									bool flag70 = input.animal != null;
									bool flag71 = flag70;
									if (flag71)
									{
										IDamageMultiplier damage_ANIMAL_MULTIPLIER = PlayerEquipmentPatches.DAMAGE_ANIMAL_MULTIPLIER;
										DamageAnimalParameters damageAnimalParameters = DamageAnimalParameters.make(input.animal, input.direction, damage_ANIMAL_MULTIPLIER, input.limb);
										damageAnimalParameters.times = num4;
										damageAnimalParameters.instigator = base.player;
										damageAnimalParameters.AlertPosition = new Vector3?(base.transform.position);
										DamageTool.damageAnimal(damageAnimalParameters, out eplayerKill, out num3);
									}
								}
								else
								{
									bool flag72 = input.type == ERaycastInfoType.VEHICLE;
									bool flag73 = flag72;
									if (flag73)
									{
										PlayerEquipmentPatches.LastPunchingField.Set(Time.realtimeSinceStartup);
										bool flag74 = input.vehicle != null && input.vehicle.asset != null && input.vehicle.canBeDamaged && input.vehicle.asset.isVulnerable;
										bool flag75 = flag74;
										if (flag75)
										{
											DamageTool.damage(input.vehicle, false, Vector3.zero, false, PlayerEquipmentPatches.DAMAGE_VEHICLE, num4 * Provider.modeConfigData.Vehicles.Melee_Damage_Multiplier, true, out eplayerKill, csteamID, EDamageOrigin.Punch);
										}
									}
									else
									{
										bool flag76 = input.type == ERaycastInfoType.BARRICADE;
										bool flag77 = flag76;
										if (flag77)
										{
											PlayerEquipmentPatches.LastPunchingField.Set(Time.realtimeSinceStartup);
											bool flag78 = input.transform != null && input.transform.CompareTag("Barricade");
											bool flag79 = flag78;
											if (flag79)
											{
												BarricadeDrop barricadeDrop2 = PlayerEquipmentPatches.BarricadeFindByRootFastMethod.Invoke(new object[] { input.transform });
												bool flag80 = barricadeDrop2 != null;
												bool flag81 = flag80;
												if (flag81)
												{
													ItemBarricadeAsset asset3 = barricadeDrop2.asset;
													bool flag82 = asset3 != null && asset3.canBeDamaged && asset3.isVulnerable;
													bool flag83 = flag82;
													if (flag83)
													{
														DamageTool.damage(input.transform, false, PlayerEquipmentPatches.DAMAGE_BARRICADE, num4 * Provider.modeConfigData.Barricades.Melee_Damage_Multiplier, out eplayerKill, csteamID, EDamageOrigin.Punch);
													}
												}
											}
										}
										else
										{
											bool flag84 = input.type == ERaycastInfoType.STRUCTURE;
											bool flag85 = flag84;
											if (flag85)
											{
												PlayerEquipmentPatches.LastPunchingField.Set(Time.realtimeSinceStartup);
												bool flag86 = input.transform != null && input.transform.CompareTag("Structure");
												bool flag87 = flag86;
												if (flag87)
												{
													StructureDrop structureDrop2 = PlayerEquipmentPatches.StructureFindByRootFastMethod.Invoke(new object[] { input.transform });
													bool flag88 = structureDrop2 != null;
													bool flag89 = flag88;
													if (flag89)
													{
														ItemStructureAsset asset4 = structureDrop2.asset;
														bool flag90 = asset4 != null && asset4.canBeDamaged && asset4.isVulnerable;
														bool flag91 = flag90;
														if (flag91)
														{
															DamageTool.damage(input.transform, false, input.direction, PlayerEquipmentPatches.DAMAGE_STRUCTURE, num4 * Provider.modeConfigData.Structures.Melee_Damage_Multiplier, out eplayerKill, csteamID, EDamageOrigin.Punch);
														}
													}
												}
											}
											else
											{
												bool flag92 = input.type == ERaycastInfoType.RESOURCE;
												bool flag93 = flag92;
												if (flag93)
												{
													PlayerEquipmentPatches.LastPunchingField.Set(Time.realtimeSinceStartup);
													byte b3 = 0;
													byte b4 = 0;
													ushort num5 = 0;
													bool flag94 = input.transform != null && input.transform.CompareTag("Resource") && ResourceManager.tryGetRegion(input.transform, out b3, out b4, out num5);
													bool flag95 = flag94;
													if (flag95)
													{
														ResourceSpawnpoint resourceSpawnpoint2 = ResourceManager.getResourceSpawnpoint(b3, b4, num5);
														bool flag96 = resourceSpawnpoint2 != null && !resourceSpawnpoint2.isDead && resourceSpawnpoint2.asset.vulnerableToFists;
														bool flag97 = flag96;
														if (flag97)
														{
															DamageTool.damage(input.transform, input.direction, PlayerEquipmentPatches.DAMAGE_RESOURCE, num4, 1f, out eplayerKill, out num3, csteamID, EDamageOrigin.Punch);
														}
													}
												}
												else
												{
													bool flag98 = input.type == ERaycastInfoType.OBJECT && input.transform != null && input.section < byte.MaxValue;
													bool flag99 = flag98;
													if (flag99)
													{
														InteractableObjectRubble componentInParent2 = input.transform.GetComponentInParent<InteractableObjectRubble>();
														bool flag100 = componentInParent2 != null && componentInParent2.IsSectionIndexValid(input.section) && !componentInParent2.isSectionDead(input.section) && componentInParent2.asset.rubbleBladeID == 0 && componentInParent2.asset.rubbleIsVulnerable;
														bool flag101 = flag100;
														if (flag101)
														{
															DamageTool.damage(componentInParent2.transform, input.direction, input.section, PlayerEquipmentPatches.DAMAGE_OBJECT, num4, out eplayerKill, out num3, csteamID, EDamageOrigin.Punch);
														}
													}
												}
											}
										}
									}
								}
							}
						}
						bool flag102 = input.type != ERaycastInfoType.PLAYER && input.type != ERaycastInfoType.ZOMBIE && input.type != ERaycastInfoType.ANIMAL && !base.player.life.isAggressor;
						bool flag103 = flag102;
						if (flag103)
						{
							float num6 = 2f + Provider.modeConfigData.Players.Ray_Aggressor_Distance;
							num6 *= num6;
							float num7 = Provider.modeConfigData.Players.Ray_Aggressor_Distance;
							num7 *= num7;
							Vector3 forward = base.player.look.aim.forward;
							for (int i = 0; i < Provider.clients.Count; i++)
							{
								bool flag104 = Provider.clients[i] != base.channel.owner;
								bool flag105 = flag104;
								if (flag105)
								{
									Player player = Provider.clients[i].player;
									bool flag106 = !(player == null);
									bool flag107 = flag106;
									if (flag107)
									{
										Vector3 vector = player.look.aim.position - base.player.look.aim.position;
										Vector3 vector2 = Vector3.Project(vector, forward);
										bool flag108 = vector2.sqrMagnitude < num6 && (vector2 - vector).sqrMagnitude < num7;
										bool flag109 = flag108;
										if (flag109)
										{
											base.player.life.markAggressive(false, true);
										}
									}
								}
							}
						}
						bool flag110 = Level.info.type == ELevelType.HORDE;
						bool flag111 = flag110;
						if (flag111)
						{
							bool flag112 = input.zombie != null;
							bool flag113 = flag112;
							if (flag113)
							{
								bool flag114 = input.limb == ELimb.SKULL;
								bool flag115 = flag114;
								if (flag115)
								{
									base.player.skills.askPay(10U);
								}
								else
								{
									base.player.skills.askPay(5U);
								}
							}
							bool flag116 = eplayerKill == EPlayerKill.ZOMBIE;
							bool flag117 = flag116;
							if (flag117)
							{
								bool flag118 = input.limb == ELimb.SKULL;
								bool flag119 = flag118;
								if (flag119)
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
							bool flag120 = eplayerKill == EPlayerKill.PLAYER && Level.info.type == ELevelType.ARENA;
							bool flag121 = flag120;
							if (flag121)
							{
								base.player.skills.askPay(100U);
							}
							base.player.sendStat(eplayerKill);
							bool flag122 = num3 > 0U;
							bool flag123 = flag122;
							if (flag123)
							{
								base.player.skills.askPay(num3);
							}
						}
					}
				}
			}
		}
	}
	[HookMethodAttribute(typeof(PlayerEquipment), "updateSlot", new Type[] { })]
	private void UpdateSlotPatch(byte slot, ushort id, byte[] state)
	{
		bool flag = slot == 0;
		bool flag2 = flag;
		if (flag2)
		{
			bool flag3 = PlayerBones.primarySlotItemIds.ContainsKey(base.player.GetNetId().id);
			bool flag4 = flag3;
			if (flag4)
			{
				PlayerBones.primarySlotItemIds[base.player.GetNetId().id] = id;
			}
			else
			{
				PlayerBones.primarySlotItemIds.Add(base.player.GetNetId().id, id);
			}
		}
		else
		{
			bool flag5 = slot == 1;
			bool flag6 = flag5;
			if (flag6)
			{
				bool flag7 = PlayerBones.secondarySlotItemIds.ContainsKey(base.player.GetNetId().id);
				bool flag8 = flag7;
				if (flag8)
				{
					PlayerBones.secondarySlotItemIds[base.player.GetNetId().id] = id;
				}
				else
				{
					PlayerBones.secondarySlotItemIds.Add(base.player.GetNetId().id, id);
				}
			}
		}
		OverrideManager.CallOriginal(this, new object[] { slot, id, state });
	}
	[HookMethodAttribute(typeof(PlayerEquipment), "updateVision", new Type[] { })]
	internal void UpdateVisionPatch()
	{
		OverrideManager.CallOriginal(this, Array.Empty<object>());
		bool flag = base.channel.IsLocalPlayer && MiscConfig.imitNightvision && !ScreenshotManager.IsSpying;
		bool flag2 = flag;
		if (flag2)
		{
			switch (MiscConfig.nightVisionType)
			{
			case NightVisionType.Military:
				LevelLighting.nightvisionColor = new Color32(20, 120, 80, 0);
				LevelLighting.nightvisionFogIntensity = 0.2f;
				break;
			case NightVisionType.Civilian:
				LevelLighting.nightvisionColor = new Color(0.4f, 0.4f, 0.4f, 0f);
				LevelLighting.nightvisionFogIntensity = 0.2f;
				break;
			case NightVisionType.Custom:
			{
				Color color = ColorConfig.GetColor("Custom nightvision color");
				LevelLighting.nightvisionColor = new Color(color.r, color.g, color.b);
				LevelLighting.nightvisionFogIntensity = color.a;
				break;
			}
			}
			LevelLighting.vision = MiscConfig.nightVisionType.ToLightingVision();
			LevelLighting.updateLighting();
			LevelLighting.updateLocal();
			PlayerLifeUiHooks.UpdateGrayscaleHook();
		}
	}
	private static readonly PlayerDamageMultiplier DAMAGE_PLAYER_MULTIPLIER = new PlayerDamageMultiplier(15f, 0.6f, 0.6f, 0.8f, 1.1f);
	private static readonly ZombieDamageMultiplier DAMAGE_ZOMBIE_MULTIPLIER = new ZombieDamageMultiplier(15f, 0.3f, 0.3f, 0.6f, 1.1f);
	private static readonly AnimalDamageMultiplier DAMAGE_ANIMAL_MULTIPLIER = new AnimalDamageMultiplier(15f, 0.3f, 0.6f, 1.1f);
	private static readonly float DAMAGE_BARRICADE = 2f;
	private static readonly float DAMAGE_STRUCTURE = 2f;
	private static readonly float DAMAGE_VEHICLE = 0f;
	private static readonly float DAMAGE_RESOURCE = 20f;
	private static readonly float DAMAGE_OBJECT = 5f;
	public static PropertyRef<float> LastPunchingField = new PropertyRef<float>(typeof(PlayerEquipment), "lastPunching", BindingFlags.Instance | BindingFlags.NonPublic);
	public static ReflectedMethod ServerSpawnLegacyImpactMethod = new ReflectedMethod(typeof(DamageTool), "ServerSpawnLegacyImpact", BindingFlags.Static | BindingFlags.NonPublic);
	public static ReflectedMethod PlayPunchAudioClipMethod = new ReflectedMethod(typeof(PlayerEquipment), "PlayPunchAudioClip", BindingFlags.Instance | BindingFlags.NonPublic);
	public static MethodInvoker<BarricadeDrop> BarricadeFindByRootFastMethod = new MethodInvoker<BarricadeDrop>(typeof(BarricadeDrop), "FindByRootFast", BindingFlags.Static | BindingFlags.NonPublic);
	public static MethodInvoker<StructureDrop> StructureFindByRootFastMethod = new MethodInvoker<StructureDrop>(typeof(StructureDrop), "FindByRootFast", BindingFlags.Static | BindingFlags.NonPublic);


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
