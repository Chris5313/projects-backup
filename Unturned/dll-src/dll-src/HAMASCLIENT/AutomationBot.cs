using System;
using System.Collections.Generic;
using System.Reflection;
using SDG.Unturned;
using Steamworks;
using UnityEngine;
public class AutomationBot : MonoBehaviour
{
	// (get) Token: 0x06000218 RID: 536 RVA: 0x0001EB34 File Offset: 0x0001CD34
	// (set) Token: 0x06000219 RID: 537 RVA: 0x0001EB63 File Offset: 0x0001CD63
	public static Dictionary<ushort, ItemInfo> itemsToESP
	{
		get
		{
			return (EspCategories.categories[1].Options[0].SortSettings as ItemSortConfig).Items;
		}
		set
		{
			(EspCategories.categories[1].Options[0].SortSettings as ItemSortConfig).Items = value;
		}
	}
	[InitializeAttribute]
	public static void Initialize()
	{
		foreach (Type type in AutomationBot.trackedTypes)
		{
			AutomationBot.trackedTypesByName.Add(type.Name, type);
		}
		Provider.onClientConnected = (Provider.ClientConnected)Delegate.Combine(Provider.onClientConnected, new Provider.ClientConnected(AutomationBot.RefreshItemList));
		Provider.onServerConnected = (Provider.ServerConnected)Delegate.Combine(Provider.onServerConnected, new Provider.ServerConnected(delegate(CSteamID steamid)
		{
			AutomationBot.RefreshItemList();
		}));
		Bootstrapper.HostGameObject.AddComponent<AutomationBot>();
	}
	public static void RefreshItemList()
	{
		AutomationBot.allItems.Clear();
		try
		{
			for (ushort num = 0; num < 65535; num += 1)
			{
				Asset asset = Assets.find(EAssetType.ITEM, num);
				bool flag = asset != null && asset is ItemAsset && (asset as ItemAsset).itemName.ToLower() != "name";
				bool flag2 = flag;
				if (flag2)
				{
					AutomationBot.allItems.Add(new ItemInfo(asset.id, (asset as ItemAsset).itemName));
				}
			}
		}
		catch
		{
		}
	}
	public void Update()
	{
		AutomationBot.itemScanTimer += Time.deltaTime;
		float scanInterval = MiscConfig.autoItemPickup ? 0.15f : 2f;
		bool flag = AutomationBot.itemScanTimer > scanInterval;
		bool flag2 = flag;
		if (flag2)
		{
			AutomationBot.itemScanTimer = 0f;
			AutomationBot.nearbyItems.Clear();
			AutomationBot.nearbyItemsById.Clear();
			foreach (InteractableItem interactableItem in InteractableItemPatches.TrackedItems)
			{
				bool flag3 = interactableItem != null && Vector3.Distance(Player.player.transform.position, interactableItem.transform.position) < 35f;
				bool flag4 = flag3;
				if (flag4)
				{
					AutomationBot.nearbyItems.Add(interactableItem);
					bool flag5 = !AutomationBot.nearbyItemsById.ContainsKey(interactableItem.asset.id);
					bool flag6 = flag5;
					if (flag6)
					{
						AutomationBot.nearbyItemsById.Add(interactableItem.asset.id, new List<InteractableItem> { interactableItem });
					}
					else
					{
						AutomationBot.nearbyItemsById[interactableItem.asset.id].Add(interactableItem);
					}
				}
			}
		}
		bool flag7 = MiscConfig.autoItemPickup && Player.player != null;
		bool flag8 = flag7;
		if (flag8)
		{
			AutomationBot.pickupTimer += Time.deltaTime;
			bool flag9 = AutomationBot.pickupTimer > MiscConfig.autoItemPickupDelay;
			bool flag10 = flag9;
			if (flag10)
			{
				AutomationBot.pickupTimer = 0f;
				foreach (InteractableItem interactableItem2 in AutomationBot.nearbyItems)
				{
					bool flag11 = interactableItem2 != null && MiscConfig.AutoPickupWhitelist.ContainsKey(interactableItem2.asset.id) && Vector3.Distance(interactableItem2.transform.position, Player.player.transform.position) <= (float)MiscConfig.autoItemPickupDistance;
					bool flag12 = flag11;
					if (flag12)
					{
						interactableItem2.use();
						break;
					}
				}
			}
		}
		bool flag13 = MiscConfig.autoFishing && Player.player != null && Player.player.equipment != null;
		bool flag14 = flag13;
		if (flag14)
		{
			try
			{
				bool flag15 = AutomationBot.fishingSessionStart == DateTime.MinValue;
				if (flag15)
				{
					AutomationBot.fishingSessionStart = DateTime.Now;
				}
				bool flag16 = !(Player.player.equipment.useable is UseableFisher);
				if (flag16)
				{
					bool autoFishingAutoEquip = MiscConfig.autoFishingAutoEquip;
					if (autoFishingAutoEquip)
					{
						AutomationBot.fishingStatusText = "Equipping rod...";
						AutomationBot._autoEquipTimer += Time.deltaTime;
						bool flag17 = AutomationBot._autoEquipTimer > 1.5f;
						if (flag17)
						{
							AutomationBot._autoEquipTimer = 0f;
							AutomationBot.TryEquipFishingRod();
						}
					}
					else
					{
						AutomationBot.fishingStatusText = "Hold fishing rod";
					}
					AutomationBot._fishingState = 0;
					AutomationBot._fishingTimer = 0f;
					AutomationBot._fishingBiteTimer = 0f;
					AutomationBot._lastTimeSinceBite = 999f;
					AutomationBot._biteDetected = false;
					AutomationBot._enteredCatchChallenge = false;
					AutomationBot._isPullingUp = false;
					AutomationBot._isPrimaryHeld = false;
				}
				else
				{
					AutomationBot._autoEquipTimer = 0f;
					AutomationBot._autoEquippedRod = false;
					UseableFisher useableFisher = (UseableFisher)Player.player.equipment.useable;
					int num = Convert.ToInt32(AutomationBot._fisherStateField.Get(useableFisher));
					float num2 = AutomationBot._fisherTimeSinceBiteField.Get(useableFisher);
					AutomationBot._fishingTimer += Time.deltaTime;
					AutomationBot._stateTimer += Time.deltaTime;
					AutomationBot._challengeTimer += Time.deltaTime;
					bool flag18 = num != AutomationBot._lastFisherState;
					if (flag18)
					{
						AutomationBot._lastFisherState = num;
						AutomationBot._stateTimer = 0f;
					}
					switch (AutomationBot._fishingState)
					{
					case 0:
					{
						AutomationBot.fishingStatusText = "Waiting to cast...";
						bool flag19 = AutomationBot._fishingTimer > 1f && !Player.player.equipment.isBusy;
						if (flag19)
						{
							AutomationBot._fishingTimer = 0f;
							AutomationBot._lastTimeSinceBite = 999f;
							AutomationBot._biteDetected = false;
							AutomationBot._consecutiveBiteChecks = 0;
							AutomationBot._enteredCatchChallenge = false;
							AutomationBot._isPullingUp = false;
							AutomationBot._isPrimaryHeld = true;
							AutomationBot._primaryPressedField.Set(Player.player.equipment, true);
							useableFisher.startPrimary();
							AutomationBot._fishingState = 1;
							AutomationBot.fishingStatusText = "Casting rod...";
						}
						break;
					}
					case 1:
					{
						AutomationBot.fishingStatusText = "Casting rod...";
						bool flag20 = num == 1 && AutomationBot._fishingTimer > 1.5f;
						if (flag20)
						{
							AutomationBot._fishingTimer = 0f;
							AutomationBot._isPrimaryHeld = false;
							AutomationBot._primaryReleasedField.Set(Player.player.equipment, true);
							useableFisher.stopPrimary();
							AutomationBot._fishingState = 2;
							AutomationBot.fishingTotalCasts++;
							AutomationBot.fishingStatusText = "Waiting for bite...";
						}
						else
						{
							bool flag21 = num != 1 && num != 0;
							if (flag21)
							{
								AutomationBot._fishingTimer = 0f;
								bool isPrimaryHeld = AutomationBot._isPrimaryHeld;
								if (isPrimaryHeld)
								{
									AutomationBot._isPrimaryHeld = false;
									AutomationBot._primaryReleasedField.Set(Player.player.equipment, true);
									useableFisher.stopPrimary();
								}
								AutomationBot._fishingState = 2;
								AutomationBot.fishingTotalCasts++;
								AutomationBot.fishingStatusText = "Waiting for bite...";
							}
							else
							{
								bool flag22 = AutomationBot._fishingTimer > 4f;
								if (flag22)
								{
									AutomationBot._fishingTimer = 0f;
									bool isPrimaryHeld2 = AutomationBot._isPrimaryHeld;
									if (isPrimaryHeld2)
									{
										AutomationBot._isPrimaryHeld = false;
										AutomationBot._primaryReleasedField.Set(Player.player.equipment, true);
										useableFisher.stopPrimary();
									}
									AutomationBot._fishingState = 0;
									AutomationBot.fishingStatusText = "Cast timeout, retrying...";
								}
							}
						}
						break;
					}
					case 2:
					{
						bool flag23 = num == 2;
						if (flag23)
						{
							bool flag24 = !AutomationBot._biteDetected;
							if (flag24)
							{
								bool flag25 = num2 < 1f;
								if (flag25)
								{
									AutomationBot._consecutiveBiteChecks++;
									bool flag26 = AutomationBot._consecutiveBiteChecks >= 2;
									if (flag26)
									{
										AutomationBot._biteDetected = true;
										AutomationBot._fishingBiteTimer = 0f;
										AutomationBot.fishingStatusText = "Fish on the line!";
									}
								}
								else
								{
									AutomationBot._consecutiveBiteChecks = 0;
								}
								AutomationBot._lastTimeSinceBite = num2;
							}
							bool biteDetected = AutomationBot._biteDetected;
							if (biteDetected)
							{
								AutomationBot._fishingBiteTimer += Time.deltaTime;
								float num3 = 0.8f + MiscConfig.autoFishingCatchDelay;
								bool flag27 = AutomationBot._fishingBiteTimer >= num3;
								if (flag27)
								{
									AutomationBot._fishingBiteTimer = 0f;
									AutomationBot._fishingTimer = 0f;
									AutomationBot._isPrimaryHeld = true;
									AutomationBot._primaryPressedField.Set(Player.player.equipment, true);
									useableFisher.startPrimary();
									AutomationBot._fishingState = 3;
									AutomationBot._challengeTimer = 0f;
									AutomationBot.fishingStatusText = "Reeling fish!";
								}
								else
								{
									AutomationBot.fishingStatusText = "Fish on! Reeling in " + Mathf.CeilToInt(num3 - AutomationBot._fishingBiteTimer).ToString() + "s...";
								}
							}
							else
							{
								AutomationBot.fishingStatusText = "Waiting for bite... (" + Mathf.CeilToInt((float)MiscConfig.autoFishingMaxWait - AutomationBot._fishingTimer).ToString() + "s left)";
								bool flag28 = AutomationBot._fishingTimer > (float)MiscConfig.autoFishingMaxWait;
								if (flag28)
								{
									AutomationBot._fishingTimer = 0f;
									AutomationBot._isPrimaryHeld = true;
									AutomationBot._primaryPressedField.Set(Player.player.equipment, true);
									useableFisher.startPrimary();
									AutomationBot._fishingState = 4;
									AutomationBot.fishingTotalEmptyCasts++;
									AutomationBot.fishingStatusText = "No bite, reeling in...";
								}
							}
						}
						else
						{
							bool flag29 = num == 0;
							if (flag29)
							{
								AutomationBot._fishingTimer = 0f;
								AutomationBot._fishingState = 0;
								AutomationBot.fishingStatusText = "Rod returned unexpectedly";
							}
							else
							{
								bool flag30 = AutomationBot._stateTimer > 5f;
								if (flag30)
								{
									bool isPrimaryHeld3 = AutomationBot._isPrimaryHeld;
									if (isPrimaryHeld3)
									{
										AutomationBot._isPrimaryHeld = false;
										AutomationBot._primaryReleasedField.Set(Player.player.equipment, true);
										useableFisher.stopPrimary();
									}
									AutomationBot._fishingTimer = 0f;
									AutomationBot._fishingState = 0;
									AutomationBot.fishingStatusText = "Stuck in waiting, resetting...";
									AutomationBot.fishingTotalStuckRecoveries++;
								}
							}
						}
						break;
					}
					case 3:
					{
						bool flag31 = num == 3;
						if (flag31)
						{
							AutomationBot._enteredCatchChallenge = true;
							AutomationBot.fishingStatusText = "Catching fish!";
							try
							{
								int num4 = AutomationBot._fisherFishPositionField.Get(useableFisher);
								int num5 = AutomationBot._fisherChallengeInputPositionField.Get(useableFisher);
								ItemFisherAsset itemFisherAsset = Player.player.equipment.asset as ItemFisherAsset;
								int num6 = ((itemFisherAsset != null) ? itemFisherAsset.CatchChallengeCursorSize : 1000);
								int num7 = num5 + num6 / 2;
								int num8 = AutomationBot._fisherChallengeLengthField.Get(useableFisher);
								int num9 = Mathf.Max(50, num6 / 4);
								bool flag32 = num4 > num7 + num9 && !AutomationBot._isPullingUp;
								if (flag32)
								{
									AutomationBot._isPullingUp = true;
									AutomationBot._isPrimaryHeld = true;
									AutomationBot._primaryPressedField.Set(Player.player.equipment, true);
									useableFisher.startPrimary();
								}
								else
								{
									bool flag33 = num4 < num7 - num9 && AutomationBot._isPullingUp;
									if (flag33)
									{
										AutomationBot._isPullingUp = false;
										AutomationBot._isPrimaryHeld = false;
										AutomationBot._primaryReleasedField.Set(Player.player.equipment, true);
										useableFisher.stopPrimary();
									}
									else
									{
										bool flag34 = Mathf.Abs(num4 - num7) <= num9;
										if (flag34)
										{
											bool flag35 = AutomationBot._isPullingUp && num4 < num7;
											if (flag35)
											{
												AutomationBot._isPullingUp = false;
												AutomationBot._isPrimaryHeld = false;
												AutomationBot._primaryReleasedField.Set(Player.player.equipment, true);
												useableFisher.stopPrimary();
											}
											else
											{
												bool flag36 = !AutomationBot._isPullingUp && num4 > num7;
												if (flag36)
												{
													AutomationBot._isPullingUp = true;
													AutomationBot._isPrimaryHeld = true;
													AutomationBot._primaryPressedField.Set(Player.player.equipment, true);
													useableFisher.startPrimary();
												}
											}
										}
									}
								}
								AutomationBot._lastFishPos = (float)num4;
							}
							catch
							{
							}
							bool flag37 = AutomationBot._challengeTimer > 15f;
							if (flag37)
							{
								bool isPrimaryHeld4 = AutomationBot._isPrimaryHeld;
								if (isPrimaryHeld4)
								{
									AutomationBot._isPrimaryHeld = false;
									AutomationBot._primaryReleasedField.Set(Player.player.equipment, true);
									useableFisher.stopPrimary();
								}
								AutomationBot._fishingState = 0;
								AutomationBot.fishingTotalStuckRecoveries++;
								AutomationBot.fishingStatusText = "Challenge timeout, resetting...";
							}
						}
						else
						{
							bool flag38 = num == 2;
							if (flag38)
							{
								AutomationBot._fishingTimer = 0f;
								AutomationBot._fishingBiteTimer = 0f;
								AutomationBot._lastTimeSinceBite = 999f;
								AutomationBot._biteDetected = false;
								AutomationBot._consecutiveBiteChecks = 0;
								AutomationBot._enteredCatchChallenge = false;
								AutomationBot._isPullingUp = false;
								AutomationBot._isPrimaryHeld = false;
								AutomationBot._fishingState = 2;
								AutomationBot.fishingTotalMisses++;
								AutomationBot.fishingStatusText = "Missed the fish!";
							}
							else
							{
								bool flag39 = num == 0;
								if (flag39)
								{
									bool isPrimaryHeld5 = AutomationBot._isPrimaryHeld;
									if (isPrimaryHeld5)
									{
										AutomationBot._isPrimaryHeld = false;
										AutomationBot._primaryReleasedField.Set(Player.player.equipment, true);
										useableFisher.stopPrimary();
									}
									AutomationBot._fishingTimer = 0f;
									AutomationBot._fishingState = 0;
									AutomationBot.fishingTotalCatches++;
									AutomationBot.fishingLastCatchTime = DateTime.Now;
									AutomationBot.fishingStatusText = "Caught a fish!";
									try
									{
										object obj = AutomationBot._fisherNextRewardField.Get(useableFisher);
										bool flag40 = obj != null;
										if (flag40)
										{
											MethodInfo methodInfo = obj.GetType().GetMethod("Get", Type.EmptyTypes);
											bool flag41 = methodInfo != null && methodInfo.IsGenericMethod;
											if (flag41)
											{
												methodInfo = methodInfo.MakeGenericMethod(new Type[] { typeof(ItemAsset) });
											}
											bool flag42 = methodInfo != null;
											if (flag42)
											{
												ItemAsset itemAsset = methodInfo.Invoke(obj, null) as ItemAsset;
												AutomationBot.fishingLastCatchItem = ((itemAsset != null) ? itemAsset.itemName : "Unknown");
											}
											else
											{
												AutomationBot.fishingLastCatchItem = "Unknown";
											}
										}
									}
									catch
									{
										AutomationBot.fishingLastCatchItem = "Unknown";
									}
								}
								else
								{
									bool flag43 = AutomationBot._stateTimer > 5f;
									if (flag43)
									{
										bool isPrimaryHeld6 = AutomationBot._isPrimaryHeld;
										if (isPrimaryHeld6)
										{
											AutomationBot._isPrimaryHeld = false;
											AutomationBot._primaryReleasedField.Set(Player.player.equipment, true);
											useableFisher.stopPrimary();
										}
										AutomationBot._fishingState = 0;
										AutomationBot.fishingTotalStuckRecoveries++;
										AutomationBot.fishingStatusText = "Stuck in catch, resetting...";
									}
								}
							}
						}
						break;
					}
					case 4:
					{
						AutomationBot.fishingStatusText = "Reeling in empty...";
						bool flag44 = num == 0;
						if (flag44)
						{
							bool isPrimaryHeld7 = AutomationBot._isPrimaryHeld;
							if (isPrimaryHeld7)
							{
								AutomationBot._isPrimaryHeld = false;
								AutomationBot._primaryReleasedField.Set(Player.player.equipment, true);
								useableFisher.stopPrimary();
							}
							AutomationBot._fishingTimer = 0f;
							AutomationBot._fishingState = 0;
							AutomationBot.fishingStatusText = "Ready to cast again";
						}
						else
						{
							bool flag45 = num == 2;
							if (flag45)
							{
								bool isPrimaryHeld8 = AutomationBot._isPrimaryHeld;
								if (isPrimaryHeld8)
								{
									AutomationBot._isPrimaryHeld = false;
									AutomationBot._primaryReleasedField.Set(Player.player.equipment, true);
									useableFisher.stopPrimary();
								}
								AutomationBot._fishingState = 2;
								AutomationBot._fishingTimer = 0f;
							}
							else
							{
								bool flag46 = AutomationBot._stateTimer > 8f;
								if (flag46)
								{
									bool isPrimaryHeld9 = AutomationBot._isPrimaryHeld;
									if (isPrimaryHeld9)
									{
										AutomationBot._isPrimaryHeld = false;
										AutomationBot._primaryReleasedField.Set(Player.player.equipment, true);
										useableFisher.stopPrimary();
									}
									AutomationBot._fishingState = 0;
									AutomationBot.fishingTotalStuckRecoveries++;
									AutomationBot.fishingStatusText = "Stuck in reel-in, resetting...";
								}
							}
						}
						break;
					}
					}
				}
			}
			catch
			{
				AutomationBot.fishingStatusText = "Error, retrying...";
				AutomationBot._fishingState = 0;
				AutomationBot._fishingTimer = 0f;
				AutomationBot._isPrimaryHeld = false;
			}
		}
		else
		{
			AutomationBot._fishingState = 0;
			AutomationBot._fishingTimer = 0f;
			AutomationBot._fishingBiteTimer = 0f;
			AutomationBot._lastTimeSinceBite = 999f;
			AutomationBot._biteDetected = false;
			AutomationBot._enteredCatchChallenge = false;
			AutomationBot._isPullingUp = false;
			AutomationBot._isPrimaryHeld = false;
			AutomationBot._autoEquipTimer = 0f;
			AutomationBot._stateTimer = 0f;
			AutomationBot._challengeTimer = 0f;
			AutomationBot._consecutiveBiteChecks = 0;
			AutomationBot._lastFisherState = -1;
			AutomationBot.fishingSessionStart = DateTime.MinValue;
			AutomationBot.fishingStatusText = "Idle";
		}
		bool flag47 = (MiscConfig.autoFarmHarvest || MiscConfig.autoFarmFertilize || MiscConfig.autoFarmPlant || MiscConfig.autoPlacePlant || MiscConfig.autoFarmCraft || MiscConfig.autoFarmStore) && Player.player != null;
		bool flag48 = flag47;
		if (flag48)
		{
			AutomationBot.ReflectFarm();
			bool farmPrimaryNeedsRelease = AutomationBot._farmPrimaryNeedsRelease;
			if (farmPrimaryNeedsRelease)
			{
				AutomationBot._farmPrimaryReleaseTimer += Time.deltaTime;
				bool flag49 = AutomationBot._farmPrimaryReleaseTimer > 0.15f;
				if (flag49)
				{
					try
					{
						bool flag50 = Player.player != null && Player.player.equipment != null;
						if (flag50)
						{
							AutomationBot._primaryPressedField.Set(Player.player.equipment, false);
							AutomationBot._primaryReleasedField.Set(Player.player.equipment, true);
						}
					}
					catch
					{
					}
					AutomationBot._farmPrimaryNeedsRelease = false;
					AutomationBot._farmPrimaryReleaseTimer = 0f;
				}
			}
			bool flag51 = AutomationBot.farmSessionStart == DateTime.MinValue;
			if (flag51)
			{
				AutomationBot.farmSessionStart = DateTime.Now;
			}
			AutomationBot._farmScanTimer += Time.deltaTime;
			AutomationBot._seedRefreshTimer += Time.deltaTime;
			bool flag52 = AutomationBot._seedRefreshTimer > 2f;
			if (flag52)
			{
				AutomationBot._seedRefreshTimer = 0f;
				AutomationBot.RefreshSeedInventory();
			}
			bool flag53 = AutomationBot._farmScanTimer > 0.5f;
			bool flag54 = flag53;
			if (flag54)
			{
				AutomationBot._farmScanTimer = 0f;
				AutomationBot._nearbyFarms.Clear();
				AutomationBot.farmPlotsGrowing = 0;
				AutomationBot.farmPlotsGrown = 0;
				AutomationBot.farmPlotsEmpty = 0;
				bool autoFarmStore = MiscConfig.autoFarmStore;
				if (autoFarmStore)
				{
					AutomationBot.farmStorages.Clear();
				}
				try
				{
					InteractableFarm[] array = UnityEngine.Object.FindObjectsOfType<InteractableFarm>();
					foreach (InteractableFarm interactableFarm in array)
					{
						bool flag55 = interactableFarm != null && Vector3.Distance(Player.player.transform.position, interactableFarm.transform.position) <= (float)MiscConfig.autoFarmDistance;
						bool flag56 = flag55;
						if (flag56)
						{
							AutomationBot._nearbyFarms.Add(interactableFarm);
							uint planted = interactableFarm.planted;
							bool isFullyGrown = interactableFarm.IsFullyGrown;
							if (isFullyGrown)
							{
								AutomationBot.farmPlotsGrown++;
							}
							else
							{
								bool flag57 = planted == 0U;
								if (flag57)
								{
									AutomationBot.farmPlotsEmpty++;
								}
								else
								{
									AutomationBot.farmPlotsGrowing++;
								}
							}
						}
					}
					bool autoFarmStore2 = MiscConfig.autoFarmStore;
					if (autoFarmStore2)
					{
						InteractableStorage[] array3 = UnityEngine.Object.FindObjectsOfType<InteractableStorage>();
						foreach (InteractableStorage interactableStorage in array3)
						{
							bool flag58 = interactableStorage != null && Vector3.Distance(Player.player.transform.position, interactableStorage.transform.position) <= (float)MiscConfig.autoFarmStoreRadius;
							if (flag58)
							{
								AutomationBot.farmStorages.Add(new AutomationBot.StorageEntry
								{
									storage = interactableStorage,
									position = interactableStorage.transform.position,
									name = interactableStorage.name
								});
							}
						}
					}
				}
				catch
				{
				}
			}
			float num10 = 0.3f;
			bool flag59 = Time.realtimeSinceStartup - AutomationBot._lastFarmAction < num10;
			if (!flag59)
			{
				bool autoFarmHarvest = MiscConfig.autoFarmHarvest;
				if (autoFarmHarvest)
				{
					AutomationBot._harvestTimer += Time.deltaTime;
					bool flag60 = AutomationBot._harvestTimer > 0.3f;
					bool flag61 = flag60;
					if (flag61)
					{
						AutomationBot._harvestTimer = 0f;
						try
						{
							foreach (InteractableFarm interactableFarm2 in AutomationBot._nearbyFarms)
							{
								bool flag62 = interactableFarm2 != null && interactableFarm2.IsFullyGrown;
								if (flag62)
								{
									AutomationBot.InteractFarm(interactableFarm2, Player.player);
									AutomationBot.farmTotalHarvested++;
									AutomationBot._lastFarmAction = Time.realtimeSinceStartup;
									goto IL_1579;
								}
							}
						}
						catch
						{
						}
					}
				}
				bool autoFarmFertilize = MiscConfig.autoFarmFertilize;
				if (autoFarmFertilize)
				{
					AutomationBot._fertilizeTimer += Time.deltaTime;
					bool flag63 = AutomationBot._fertilizeTimer > 0.5f;
					bool flag64 = flag63;
					if (flag64)
					{
						AutomationBot._fertilizeTimer = 0f;
						try
						{
							bool flag65 = AutomationBot.EquipFertilizer(Player.player);
							bool flag66 = flag65;
							if (flag66)
							{
								foreach (InteractableFarm interactableFarm3 in AutomationBot._nearbyFarms)
								{
									bool flag67 = interactableFarm3 != null && interactableFarm3.canFertilize && !interactableFarm3.IsFullyGrown;
									if (flag67)
									{
										bool flag68 = AutomationBot.FarmUseGrower(interactableFarm3, Player.player);
										if (flag68)
										{
											AutomationBot.farmTotalFertilized++;
											AutomationBot._lastFarmAction = Time.realtimeSinceStartup;
											goto IL_1579;
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
				bool autoFarmPlant = MiscConfig.autoFarmPlant;
				if (autoFarmPlant)
				{
					AutomationBot._plantTimer += Time.deltaTime;
					bool flag69 = AutomationBot._plantTimer > 0.5f;
					bool flag70 = flag69;
					if (flag70)
					{
						AutomationBot._plantTimer = 0f;
						ushort seedIdForPlant = AutomationBot.GetSeedIdForPlant();
						bool flag71 = seedIdForPlant > 0;
						if (flag71)
						{
							bool flag72 = AutomationBot.EquipSeed(Player.player, seedIdForPlant);
							if (flag72)
							{
								foreach (InteractableFarm interactableFarm4 in AutomationBot._nearbyFarms)
								{
									bool flag73 = false;
									try
									{
										flag73 = interactableFarm4 != null && interactableFarm4.planted == 0U;
									}
									catch
									{
									}
									bool flag74 = flag73;
									if (flag74)
									{
										bool flag75 = AutomationBot.FarmUseGrower(interactableFarm4, Player.player);
										if (flag75)
										{
											AutomationBot.farmTotalPlanted++;
											AutomationBot._lastFarmAction = Time.realtimeSinceStartup;
											goto IL_1579;
										}
									}
								}
							}
						}
					}
				}
				bool autoFarmCraft = MiscConfig.autoFarmCraft;
				if (autoFarmCraft)
				{
					AutomationBot._craftTimer += Time.deltaTime;
					bool flag76 = AutomationBot._craftTimer > 2f;
					bool flag77 = flag76;
					if (flag77)
					{
						AutomationBot._craftTimer = 0f;
						bool flag78 = AutomationBot.TryCraftSeeds(Player.player);
						if (flag78)
						{
							AutomationBot._lastFarmAction = Time.realtimeSinceStartup;
							goto IL_1579;
						}
					}
				}
				bool autoFarmStore3 = MiscConfig.autoFarmStore;
				if (autoFarmStore3)
				{
					AutomationBot._storeTimer += Time.deltaTime;
					bool flag79 = AutomationBot._storeTimer > 1f;
					bool flag80 = flag79;
					if (flag80)
					{
						AutomationBot._storeTimer = 0f;
						bool flag81 = AutomationBot.TryStoreItems(Player.player);
						if (flag81)
						{
							AutomationBot._lastFarmAction = Time.realtimeSinceStartup;
						}
					}
				}
			}
			IL_1579:;
		}
		else
		{
			bool farmPrimaryNeedsRelease2 = AutomationBot._farmPrimaryNeedsRelease;
			if (farmPrimaryNeedsRelease2)
			{
				try
				{
					bool flag82 = Player.player != null && Player.player.equipment != null;
					if (flag82)
					{
						AutomationBot._primaryPressedField.Set(Player.player.equipment, false);
						AutomationBot._primaryReleasedField.Set(Player.player.equipment, true);
					}
				}
				catch
				{
				}
				AutomationBot._farmPrimaryNeedsRelease = false;
				AutomationBot._farmPrimaryReleaseTimer = 0f;
			}
			AutomationBot.farmSessionStart = DateTime.MinValue;
			AutomationBot.farmPlotsGrowing = 0;
			AutomationBot.farmPlotsGrown = 0;
			AutomationBot.farmPlotsEmpty = 0;
			AutomationBot._nearbyFarms.Clear();
			AutomationBot.farmStorages.Clear();
			AutomationBot._farmScanTimer = 0f;
			AutomationBot._harvestTimer = 0f;
			AutomationBot._fertilizeTimer = 0f;
			AutomationBot._plantTimer = 0f;
			AutomationBot._craftTimer = 0f;
			AutomationBot._storeTimer = 0f;
			AutomationBot._seedRefreshTimer = 0f;
		}
		bool flag83 = AutomationBot._autoPlaceClearTimer > 0f;
		if (flag83)
		{
			AutomationBot._autoPlaceClearTimer -= Time.deltaTime;
			bool flag84 = AutomationBot._autoPlaceClearTimer <= 0f;
			if (flag84)
			{
				AutomationBot.autoPlaceTargetPoint = null;
			}
		}
		bool flag85 = MiscConfig.farmGridFreezeKey != KeyCode.None && Input.GetKeyDown(MiscConfig.farmGridFreezeKey);
		if (flag85)
		{
			AutomationBot.ToggleFreezeGrid();
		}
		bool flag86 = MiscConfig.autoPlacePlant && Player.player != null;
		bool flag87 = flag86;
		if (flag87)
		{
			AutomationBot._placePlantTimer += Time.deltaTime;
			bool flag88 = AutomationBot._placePlantTimer > 1.5f;
			bool flag89 = flag88;
			if (flag89)
			{
				AutomationBot._placePlantTimer = 0f;
				ushort seedIdForPlace = AutomationBot.GetSeedIdForPlace();
				bool flag90 = seedIdForPlace != 0 && AutomationBot.EquipSeed(Player.player, seedIdForPlace);
				if (flag90)
				{
					Useable useable = Player.player.equipment.useable;
					bool flag91 = useable != null && useable is UseableGrower;
					if (flag91)
					{
						InteractableFarm[] array5 = UnityEngine.Object.FindObjectsOfType<InteractableFarm>();
						Vector3? vector = null;
						Vector3 position = Player.player.look.aim.position;
						float num11 = 4.5f;
						bool flag92 = AutomationBot.gridFrozen && AutomationBot.frozenGridPoints.Count > 0;
						if (flag92)
						{
							for (int k = 0; k < AutomationBot.frozenGridPoints.Count; k++)
							{
								int num12 = (AutomationBot.frozenGridPlaceIndex + k) % AutomationBot.frozenGridPoints.Count;
								Vector3 vector2 = AutomationBot.frozenGridPoints[num12];
								bool flag93 = Vector3.Distance(position, vector2) > num11;
								if (!flag93)
								{
									bool flag94 = !AutomationBot.IsPointOccupied(vector2, array5);
									if (flag94)
									{
										vector = new Vector3?(vector2);
										AutomationBot.frozenGridPlaceIndex = num12 + 1;
										break;
									}
								}
							}
						}
						else
						{
							Vector3 position2 = Player.player.transform.position;
							List<Vector3> list = AutomationBot.ScanGridPoints(position2, num11, 2f, array5);
							for (int l = 0; l < list.Count; l++)
							{
								bool flag95 = Vector3.Distance(position, list[l]) > num11;
								if (!flag95)
								{
									bool flag96 = Vector3.Distance(list[l], AutomationBot._lastPlaceAttemptPoint) > 2f;
									if (flag96)
									{
										vector = new Vector3?(list[l]);
										break;
									}
								}
							}
							bool flag97 = vector == null && list.Count > 0;
							if (flag97)
							{
								vector = new Vector3?(list[0]);
							}
						}
						bool flag98 = vector != null;
						if (flag98)
						{
							AutomationBot._lastPlaceAttemptPoint = vector.Value;
							AutomationBot.autoPlaceTargetPoint = vector;
							Quaternion rotation = Player.player.look.aim.rotation;
							Quaternion quaternion = Quaternion.identity;
							Camera instance = MainCamera.instance;
							bool flag99 = instance != null;
							if (flag99)
							{
								quaternion = instance.transform.rotation;
							}
							try
							{
								Vector3 normalized = (vector.Value - position).normalized;
								Player.player.look.aim.rotation = Quaternion.LookRotation(normalized);
								bool flag100 = instance != null;
								if (flag100)
								{
									instance.transform.rotation = Quaternion.LookRotation(vector.Value - instance.transform.position);
								}
								AutomationBot._primaryPressedField.Set(Player.player.equipment, true);
								AutomationBot._primaryReleasedField.Set(Player.player.equipment, false);
								((UseableGrower)useable).startPrimary();
								AutomationBot._farmPrimaryNeedsRelease = true;
								AutomationBot._farmPrimaryReleaseTimer = 0f;
								AutomationBot.farmTotalPlanted++;
							}
							catch
							{
							}
							finally
							{
								Player.player.look.aim.rotation = rotation;
								bool flag101 = instance != null;
								if (flag101)
								{
									instance.transform.rotation = quaternion;
								}
							}
							AutomationBot._autoPlaceClearTimer = 0.5f;
						}
					}
				}
			}
		}
		else
		{
			AutomationBot.autoPlaceTargetPoint = null;
			AutomationBot._placePlantTimer = 0f;
		}
	}
	[ConfigBindAttribute("Misc options", "WhitelistNearbyItem")]
	public static void ToggleWhitelistNearbyItem()
	{
		InteractableItem interactableItem;
		AutomationBot.FindNearestItemInFov(MiscConfig.freeCamera ? ((int)Mathf.Clamp((float)MiscConfig.pickupItemsThroughWallsDistance - Vector3.Distance(Player.player.look.getEyesPosition(), FreeCamera.Instance.transform.position), 0f, (float)MiscConfig.pickupItemsThroughWallsDistance)) : MiscConfig.pickupItemsThroughWallsDistance, FovCircleRenderer.GetFovRadius("Grab items through walls FOV"), out interactableItem);
		bool flag = interactableItem != null && !AutomationBot.itemsToESP.ContainsKey(interactableItem.asset.id);
		bool flag2 = flag;
		if (flag2)
		{
			AutomationBot.itemsToESP.Add(interactableItem.asset.id, new ItemInfo(interactableItem.asset.id, interactableItem.asset.itemName));
		}
		else
		{
			bool flag3 = interactableItem != null && AutomationBot.itemsToESP.ContainsKey(interactableItem.asset.id);
			bool flag4 = flag3;
			if (flag4)
			{
				AutomationBot.itemsToESP.Remove(interactableItem.asset.id);
			}
		}
	}
	public static bool FindNearestItemInFov(int distance, int fov, out InteractableItem rii)
	{
		int num = fov + 1;
		rii = null;
		foreach (InteractableItem interactableItem in AutomationBot.nearbyItems)
		{
			bool flag = interactableItem == null;
			bool flag2 = !flag;
			if (flag2)
			{
				bool flag3 = Vector3.Distance(interactableItem.transform.position, Player.player.transform.position) > (float)distance;
				bool flag4 = !flag3;
				if (flag4)
				{
					bool flag5 = !interactableItem.transform.position.IsOnScreen();
					bool flag6 = !flag5;
					if (flag6)
					{
						int num2 = (int)Vector2.Distance(new Vector2((float)(Screen.width / 2), (float)(Screen.height / 2)), interactableItem.transform.position.WorldToScreenPoint());
						bool flag7 = num2 > fov;
						bool flag8 = !flag7;
						if (flag8)
						{
							bool flag9 = num2 < num;
							bool flag10 = flag9;
							if (flag10)
							{
								num = num2;
								rii = interactableItem;
							}
						}
					}
				}
			}
		}
		return rii != null;
	}
	public static void TryEquipFishingRod()
	{
		try
		{
			bool flag = Player.player == null || Player.player.equipment == null;
			if (!flag)
			{
				byte b = 0;
				while ((int)b < Player.player.inventory.items.Length)
				{
					byte itemCount = Player.player.inventory.getItemCount(b);
					for (byte b2 = 0; b2 < itemCount; b2 += 1)
					{
						ItemJar item = Player.player.inventory.getItem(b, b2);
						bool flag2 = item != null && item.item != null;
						if (flag2)
						{
							ItemAsset itemAsset = Assets.find(EAssetType.ITEM, item.item.id) as ItemAsset;
							bool flag3 = itemAsset != null && itemAsset is ItemFisherAsset;
							if (flag3)
							{
								Player.player.equipment.tryEquip(b, item.x, item.y, item.item.state);
								return;
							}
						}
					}
					b += 1;
				}
			}
		}
		catch
		{
		}
	}
	public static bool IsItemVisibleByFilter(InteractableItem ii)
	{
		return ii != null && (!(EspCategories.categories[1].Options[0].SortSettings as ItemSortConfig).SortItems || ((EspCategories.categories[1].Options[0].SortSettings as ItemSortConfig).IsBlacklist ? (!AutomationBot.itemsToESP.ContainsKey(ii.asset.id)) : AutomationBot.itemsToESP.ContainsKey(ii.asset.id))) && (!(EspCategories.categories[1].Options[0].SortSettings as ItemSortConfig).UseCategoryFiltering || (EspCategories.categories[1].Options[0].SortSettings as ItemSortConfig).Categories.Contains(ii.asset.GetType()));
	}
	public static void AddItemToESP(ItemInfo ci)
	{
		bool flag = !AutomationBot.itemsToESP.ContainsKey(ci.id);
		bool flag2 = flag;
		if (flag2)
		{
			AutomationBot.itemsToESP.Add(ci.id, ci);
		}
	}
	public static void RemoveItemFromESP(ushort id)
	{
		bool flag = AutomationBot.itemsToESP.ContainsKey(id);
		bool flag2 = flag;
		if (flag2)
		{
			AutomationBot.itemsToESP.Remove(id);
		}
	}
	private static void ReflectFarm()
	{
		bool farmReflected = AutomationBot._farmReflected;
		if (!farmReflected)
		{
			AutomationBot._farmReflected = true;
			BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			BindingFlags bindingFlags2 = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
			BindingFlags bindingFlags3 = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
			try
			{
				AutomationBot._askFarm = typeof(InteractableFarm).GetMethod("askFarm", bindingFlags);
			}
			catch
			{
			}
			bool flag = AutomationBot._askFarm == null;
			if (flag)
			{
				try
				{
					AutomationBot._askFarm = typeof(InteractableFarm).GetMethod("askHarvest", bindingFlags);
				}
				catch
				{
				}
			}
			bool flag2 = AutomationBot._askFarm == null;
			if (flag2)
			{
				try
				{
					AutomationBot._askFarm = typeof(InteractableFarm).GetMethod("harvest", bindingFlags);
				}
				catch
				{
				}
			}
			try
			{
				AutomationBot._sendHarvest = typeof(InteractableFarm).GetMethod("SendHarvestRequest", bindingFlags2);
			}
			catch
			{
			}
			bool flag3 = AutomationBot._sendHarvest == null;
			if (flag3)
			{
				try
				{
					AutomationBot._sendHarvest = typeof(InteractableFarm).GetMethod("sendHarvest", bindingFlags2);
				}
				catch
				{
				}
			}
			try
			{
				AutomationBot._clientHarvest = typeof(InteractableFarm).GetMethod("ClientHarvest", bindingFlags | bindingFlags2);
			}
			catch
			{
			}
			bool flag4 = AutomationBot._clientHarvest == null;
			if (flag4)
			{
				try
				{
					AutomationBot._clientHarvest = typeof(InteractableFarm).GetMethod("ClientInteract", bindingFlags);
				}
				catch
				{
				}
			}
			bool flag5 = AutomationBot._clientHarvest == null;
			if (flag5)
			{
				try
				{
					AutomationBot._clientHarvest = typeof(InteractableFarm).GetMethod("ClientInteract", bindingFlags2);
				}
				catch
				{
				}
			}
			try
			{
				AutomationBot._sendInteract = typeof(PlayerInteract).GetMethod("sendInteract", bindingFlags | bindingFlags2);
			}
			catch
			{
			}
			bool flag6 = AutomationBot._sendInteract == null;
			if (flag6)
			{
				try
				{
					AutomationBot._sendInteract = typeof(PlayerInteract).GetMethod("SendInteractRequest", bindingFlags | bindingFlags2);
				}
				catch
				{
				}
			}
			bool flag7 = AutomationBot._sendInteract == null;
			if (flag7)
			{
				try
				{
					AutomationBot._sendInteract = typeof(PlayerInteract).GetMethod("ClientInteract", bindingFlags);
				}
				catch
				{
				}
			}
			try
			{
				AutomationBot._askStoreStorage = typeof(InteractableStorage).GetMethod("askStoreStorage", bindingFlags);
			}
			catch
			{
			}
			bool flag8 = AutomationBot._askStoreStorage == null;
			if (flag8)
			{
				try
				{
					AutomationBot._askStoreStorage = typeof(InteractableStorage).GetMethod("store", bindingFlags | bindingFlags2);
				}
				catch
				{
				}
			}
			try
			{
				AutomationBot._askCraft = typeof(PlayerCrafting).GetMethod("askCraft", bindingFlags);
			}
			catch
			{
			}
			try
			{
				AutomationBot._sendCraft = typeof(PlayerCrafting).GetMethod("sendCraft", bindingFlags);
			}
			catch
			{
			}
			bool flag9 = AutomationBot._sendCraft == null;
			if (flag9)
			{
				try
				{
					AutomationBot._sendCraft = typeof(PlayerCrafting).GetMethod("SendCraftRequest", bindingFlags);
				}
				catch
				{
				}
			}
			try
			{
				AutomationBot._isFullyGrownProp = typeof(InteractableFarm).GetProperty("IsFullyGrown", bindingFlags);
			}
			catch
			{
			}
			try
			{
				AutomationBot._plantedProp = typeof(InteractableFarm).GetProperty("planted", bindingFlags);
			}
			catch
			{
			}
			try
			{
				AutomationBot._growthProp = typeof(InteractableFarm).GetProperty("growth", bindingFlags);
			}
			catch
			{
			}
			try
			{
				AutomationBot._piHit = typeof(PlayerInteract).GetField("hit", bindingFlags3);
			}
			catch
			{
			}
			try
			{
				AutomationBot._piLastInteract = typeof(PlayerInteract).GetField("lastInteract", bindingFlags3);
			}
			catch
			{
			}
			bool flag10 = AutomationBot._piLastInteract == null;
			if (flag10)
			{
				try
				{
					AutomationBot._piLastInteract = typeof(PlayerInteract).GetField("<lastInteract>k__BackingField", bindingFlags3);
				}
				catch
				{
				}
			}
			bool flag11 = !AutomationBot._interactableResolved;
			if (flag11)
			{
				AutomationBot._interactableResolved = true;
				try
				{
					AutomationBot._piInteractable = typeof(PlayerInteract).GetField("interactable", bindingFlags3);
				}
				catch
				{
				}
				bool flag12 = AutomationBot._piInteractable == null;
				if (flag12)
				{
					try
					{
						AutomationBot._piInteractable = typeof(PlayerInteract).GetField("<interactable>k__BackingField", bindingFlags3);
					}
					catch
					{
					}
				}
			}
		}
	}
	private static void InteractFarm(InteractableFarm farm, Player player)
	{
		try
		{
			farm.use();
			return;
		}
		catch
		{
		}
		try
		{
			bool flag = AutomationBot._clientHarvest != null;
			if (flag)
			{
				ParameterInfo[] parameters = AutomationBot._clientHarvest.GetParameters();
				bool flag2 = parameters.Length == 0;
				if (flag2)
				{
					AutomationBot._clientHarvest.Invoke(farm, null);
					return;
				}
			}
		}
		catch
		{
		}
		try
		{
			bool flag3 = AutomationBot._askFarm != null;
			if (flag3)
			{
				ParameterInfo[] parameters2 = AutomationBot._askFarm.GetParameters();
				bool flag4 = parameters2.Length == 1 && parameters2[0].ParameterType == typeof(Player);
				if (flag4)
				{
					AutomationBot._askFarm.Invoke(farm, new object[] { player });
					return;
				}
				bool flag5 = parameters2.Length == 0;
				if (flag5)
				{
					AutomationBot._askFarm.Invoke(farm, null);
					return;
				}
			}
		}
		catch
		{
		}
		try
		{
			bool flag6 = AutomationBot._sendHarvest != null;
			if (flag6)
			{
				ParameterInfo[] parameters3 = AutomationBot._sendHarvest.GetParameters();
				bool flag7 = parameters3.Length == 2;
				if (flag7)
				{
					AutomationBot._sendHarvest.Invoke(null, new object[] { farm, player });
					return;
				}
				bool flag8 = parameters3.Length == 1;
				if (flag8)
				{
					AutomationBot._sendHarvest.Invoke(null, new object[] { farm });
					return;
				}
			}
		}
		catch
		{
		}
		try
		{
			bool flag9 = AutomationBot._sendInteract != null && player != null && player.interact != null;
			if (flag9)
			{
				ParameterInfo[] parameters4 = AutomationBot._sendInteract.GetParameters();
				bool flag10 = parameters4.Length == 0;
				if (flag10)
				{
					AutomationBot._sendInteract.Invoke(player.interact, null);
				}
			}
		}
		catch
		{
		}
	}
	private static bool FarmUseGrower(InteractableFarm farm, Player player)
	{
		bool flag = farm == null || player == null || player.equipment == null;
		bool flag2;
		if (flag)
		{
			flag2 = false;
		}
		else
		{
			Quaternion rotation = player.look.aim.rotation;
			Quaternion quaternion = Quaternion.identity;
			Camera instance = MainCamera.instance;
			bool flag3 = instance != null;
			if (flag3)
			{
				quaternion = instance.transform.rotation;
			}
			try
			{
				Useable useable = player.equipment.useable;
				bool flag4 = !(useable is UseableGrower);
				if (flag4)
				{
					flag2 = false;
				}
				else
				{
					player.look.aim.rotation = Quaternion.LookRotation(farm.transform.position - player.look.aim.position);
					bool flag5 = instance != null;
					if (flag5)
					{
						instance.transform.rotation = Quaternion.LookRotation(farm.transform.position - instance.transform.position);
					}
					AutomationBot._primaryPressedField.Set(player.equipment, true);
					AutomationBot._primaryReleasedField.Set(player.equipment, false);
					((UseableGrower)useable).startPrimary();
					AutomationBot._farmPrimaryNeedsRelease = true;
					AutomationBot._farmPrimaryReleaseTimer = 0f;
					flag2 = true;
				}
			}
			catch
			{
				flag2 = false;
			}
			finally
			{
				player.look.aim.rotation = rotation;
				bool flag6 = instance != null;
				if (flag6)
				{
					instance.transform.rotation = quaternion;
				}
			}
		}
		return flag2;
	}
	private static bool EquipFertilizer(Player player)
	{
		bool flag = player == null || player.equipment == null;
		bool flag2;
		if (flag)
		{
			flag2 = false;
		}
		else
		{
			try
			{
				Useable useable = player.equipment.useable;
				bool flag3 = useable is UseableGrower;
				if (flag3)
				{
					ItemFarmAsset itemFarmAsset = player.equipment.asset as ItemFarmAsset;
					bool flag4 = itemFarmAsset != null && itemFarmAsset.grow == 0;
					if (flag4)
					{
						return true;
					}
				}
				byte b = 0;
				while ((int)b < player.inventory.items.Length)
				{
					byte itemCount = player.inventory.getItemCount(b);
					for (byte b2 = 0; b2 < itemCount; b2 += 1)
					{
						ItemJar item = player.inventory.getItem(b, b2);
						bool flag5 = item != null && item.item != null;
						if (flag5)
						{
							ItemFarmAsset itemFarmAsset2 = Assets.find(EAssetType.ITEM, item.item.id) as ItemFarmAsset;
							bool flag6 = itemFarmAsset2 != null && itemFarmAsset2.grow == 0;
							if (flag6)
							{
								player.equipment.tryEquip(b, item.x, item.y, item.item.state);
								Useable useable2 = player.equipment.useable;
								bool flag7;
								if (useable2 is UseableGrower)
								{
									ItemFarmAsset itemFarmAsset3 = player.equipment.asset as ItemFarmAsset;
									if (itemFarmAsset3 != null)
									{
										flag7 = itemFarmAsset3.grow == 0;
										goto IL_0150;
									}
								}
								flag7 = false;
								IL_0150:
								bool flag8 = flag7;
								if (flag8)
								{
									return true;
								}
								return false;
							}
						}
					}
					b += 1;
				}
			}
			catch
			{
			}
			flag2 = false;
		}
		return flag2;
	}
	public static void RefreshSeedInventory()
	{
		AutomationBot.inventorySeedNames.Clear();
		AutomationBot.inventorySeedIds.Clear();
		AutomationBot.inventoryPlantNames.Clear();
		try
		{
			bool flag = Player.player != null && Player.player.inventory != null;
			if (flag)
			{
				byte b = 0;
				while ((int)b < Player.player.inventory.items.Length)
				{
					byte itemCount = Player.player.inventory.getItemCount(b);
					for (byte b2 = 0; b2 < itemCount; b2 += 1)
					{
						ItemJar item = Player.player.inventory.getItem(b, b2);
						bool flag2 = item != null && item.item != null;
						if (flag2)
						{
							ItemAsset itemAsset = (ItemAsset)Assets.find(EAssetType.ITEM, item.item.id);
							ItemFarmAsset itemFarmAsset = itemAsset as ItemFarmAsset;
							bool flag3 = itemFarmAsset != null && itemFarmAsset.grow > 0;
							if (flag3)
							{
								bool flag4 = !AutomationBot.inventorySeedIds.Contains(item.item.id);
								if (flag4)
								{
									AutomationBot.inventorySeedIds.Add(item.item.id);
									AutomationBot.inventorySeedNames.Add(itemAsset.itemName);
									string text = itemAsset.itemName;
									ItemAsset itemAsset2 = (ItemAsset)Assets.find(EAssetType.ITEM, itemFarmAsset.grow);
									bool flag5 = itemAsset2 != null;
									if (flag5)
									{
										text = itemAsset2.itemName;
									}
									AutomationBot.inventoryPlantNames.Add(text);
								}
							}
						}
					}
					b += 1;
				}
			}
		}
		catch
		{
		}
	}
	private static ushort GetSeedIdForPlant()
	{
		bool flag = MiscConfig.autoFarmPlantSeedIndex >= 0 && MiscConfig.autoFarmPlantSeedIndex < AutomationBot.inventorySeedIds.Count;
		ushort num;
		if (flag)
		{
			num = AutomationBot.inventorySeedIds[MiscConfig.autoFarmPlantSeedIndex];
		}
		else
		{
			bool flag2 = AutomationBot.inventorySeedIds.Count > 0;
			if (flag2)
			{
				num = AutomationBot.inventorySeedIds[0];
			}
			else
			{
				num = 0;
			}
		}
		return num;
	}
	private static ushort GetSeedIdForPlace()
	{
		bool flag = MiscConfig.autoPlacePlantSeedIndex >= 0 && MiscConfig.autoPlacePlantSeedIndex < AutomationBot.inventorySeedIds.Count;
		ushort num;
		if (flag)
		{
			num = AutomationBot.inventorySeedIds[MiscConfig.autoPlacePlantSeedIndex];
		}
		else
		{
			bool flag2 = AutomationBot.inventorySeedIds.Count > 0;
			if (flag2)
			{
				num = AutomationBot.inventorySeedIds[0];
			}
			else
			{
				num = 0;
			}
		}
		return num;
	}
	public static List<Vector3> ScanGridPoints(Vector3 center, float radius, float step, InteractableFarm[] existingFarms)
	{
		List<Vector3> list = new List<Vector3>();
		int num = Mathf.CeilToInt(radius / step);
		for (int i = -num; i <= num; i++)
		{
			for (int j = -num; j <= num; j++)
			{
				Vector3 vector = center + new Vector3((float)i * step, 0f, (float)j * step);
				RaycastHit raycastHit;
				bool flag = Physics.Raycast(vector + Vector3.up * 5f, Vector3.down, out raycastHit, 10f, RayMasks.BARRICADE_INTERACT);
				if (flag)
				{
					bool flag2 = raycastHit.normal.y > 0.75f && raycastHit.collider.transform.CompareTag("Ground");
					if (flag2)
					{
						bool flag3 = false;
						bool flag4 = existingFarms != null;
						if (flag4)
						{
							for (int k = 0; k < existingFarms.Length; k++)
							{
								bool flag5 = existingFarms[k] != null && Vector3.Distance(raycastHit.point, existingFarms[k].transform.position) < 2f;
								if (flag5)
								{
									flag3 = true;
									break;
								}
							}
						}
						bool flag6 = !flag3;
						if (flag6)
						{
							for (int l = 0; l < list.Count; l++)
							{
								bool flag7 = Vector3.Distance(raycastHit.point, list[l]) < 1.5f;
								if (flag7)
								{
									flag3 = true;
									break;
								}
							}
						}
						bool flag8 = !flag3;
						if (flag8)
						{
							list.Add(raycastHit.point);
						}
					}
				}
			}
		}
		list.Sort((Vector3 a, Vector3 b) => Vector3.Distance(center, a).CompareTo(Vector3.Distance(center, b)));
		return list;
	}
	public static void ToggleFreezeGrid()
	{
		bool flag = AutomationBot.gridFrozen;
		if (flag)
		{
			AutomationBot.gridFrozen = false;
			AutomationBot.frozenGridPoints.Clear();
			AutomationBot.frozenGridPlaceIndex = 0;
			AutomationBot.frozenGridOrigin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
		}
		else
		{
			bool flag2 = Player.player == null;
			if (!flag2)
			{
				Vector3 position = Player.player.transform.position;
				float num = Mathf.Min((float)MiscConfig.autoFarmDistance, 10f);
				InteractableFarm[] array = UnityEngine.Object.FindObjectsOfType<InteractableFarm>();
				AutomationBot.frozenGridPoints = AutomationBot.ScanGridPoints(position, num, 2f, array);
				AutomationBot.frozenGridPlaceIndex = 0;
				AutomationBot.frozenGridOrigin = position;
				AutomationBot.gridFrozen = true;
			}
		}
	}
	private static bool IsPointOccupied(Vector3 point, InteractableFarm[] farms)
	{
		bool flag = farms == null;
		bool flag2;
		if (flag)
		{
			flag2 = false;
		}
		else
		{
			for (int i = 0; i < farms.Length; i++)
			{
				bool flag3 = farms[i] != null && Vector3.Distance(point, farms[i].transform.position) < 2f;
				if (flag3)
				{
					return true;
				}
			}
			flag2 = false;
		}
		return flag2;
	}
	private static bool EquipSeed(Player player, ushort targetSeedId)
	{
		bool flag = targetSeedId == 0;
		bool flag2;
		if (flag)
		{
			flag2 = false;
		}
		else
		{
			ushort num = 0;
			try
			{
				bool flag3 = player.equipment.asset != null;
				if (flag3)
				{
					num = player.equipment.asset.id;
				}
			}
			catch
			{
			}
			bool flag4 = num == targetSeedId;
			if (flag4)
			{
				flag2 = true;
			}
			else
			{
				try
				{
					byte b = 0;
					while ((int)b < player.inventory.items.Length)
					{
						byte itemCount = player.inventory.getItemCount(b);
						byte b2 = 0;
						while (b2 < itemCount)
						{
							ItemJar item = player.inventory.getItem(b, b2);
							bool flag5 = item != null && item.item != null && item.item.id == targetSeedId;
							if (flag5)
							{
								player.equipment.tryEquip(b, item.x, item.y, item.item.state);
								bool flag6 = player.equipment.asset != null && player.equipment.asset.id == targetSeedId;
								if (flag6)
								{
									return true;
								}
								return false;
							}
							else
							{
								b2 += 1;
							}
						}
						b += 1;
					}
				}
				catch
				{
				}
				flag2 = false;
			}
		}
		return flag2;
	}
	private static bool TryCraftSeeds(Player player)
	{
		try
		{
			PlayerCrafting crafting = player.crafting;
			bool flag = crafting == null;
			if (flag)
			{
				return false;
			}
			MethodInfo methodInfo = typeof(PlayerCrafting).GetMethod("askCraft", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			bool flag2 = methodInfo == null;
			if (flag2)
			{
				methodInfo = typeof(PlayerCrafting).GetMethod("sendCraft", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			}
			bool flag3 = methodInfo == null;
			if (flag3)
			{
				methodInfo = typeof(PlayerCrafting).GetMethod("SendCraftRequest", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			}
			bool flag4 = methodInfo == null;
			if (flag4)
			{
				methodInfo = typeof(PlayerCrafting).GetMethod("craft", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			}
			bool flag5 = methodInfo == null;
			if (flag5)
			{
				methodInfo = typeof(PlayerCrafting).GetMethod("Craft", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			}
			bool flag6 = methodInfo == null;
			if (flag6)
			{
				methodInfo = typeof(PlayerCrafting).GetMethod("ClientCraft", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			}
			bool flag7 = methodInfo == null;
			if (flag7)
			{
				return false;
			}
			ParameterInfo[] parameters = methodInfo.GetParameters();
			bool flag8 = AutomationBot._seedBlueprintCache == null || Time.realtimeSinceStartup - AutomationBot._seedBlueprintCacheTime > 30f;
			if (flag8)
			{
				AutomationBot._seedBlueprintCache = new List<KeyValuePair<ItemAsset, int>>();
				AutomationBot._seedBlueprintCacheTime = Time.realtimeSinceStartup;
				List<ItemAsset> list = new List<ItemAsset>();
				Assets.find<ItemAsset>(list);
				foreach (ItemAsset itemAsset in list)
				{
					bool flag9 = itemAsset == null || itemAsset.blueprints == null;
					if (!flag9)
					{
						for (int i = 0; i < itemAsset.blueprints.Count; i++)
						{
							Blueprint blueprint = itemAsset.blueprints[i];
							bool flag10 = blueprint == null || blueprint.outputs == null;
							if (!flag10)
							{
								bool flag11 = false;
								foreach (BlueprintOutput blueprintOutput in blueprint.outputs)
								{
									bool flag12 = blueprintOutput != null;
									if (flag12)
									{
										ushort num = 0;
										FieldInfo field = blueprintOutput.GetType().GetField("id");
										bool flag13 = field != null;
										if (flag13)
										{
											num = (ushort)field.GetValue(blueprintOutput);
										}
										else
										{
											PropertyInfo property = blueprintOutput.GetType().GetProperty("id");
											bool flag14 = property != null;
											if (flag14)
											{
												num = (ushort)property.GetValue(blueprintOutput, null);
											}
										}
										bool flag15 = num > 0;
										if (flag15)
										{
											ItemAsset itemAsset2 = Assets.find(EAssetType.ITEM, num) as ItemAsset;
											ItemFarmAsset itemFarmAsset = itemAsset2 as ItemFarmAsset;
											bool flag16 = itemFarmAsset != null && itemFarmAsset.grow > 0;
											if (flag16)
											{
												flag11 = true;
												break;
											}
										}
									}
								}
								bool flag17 = flag11;
								if (flag17)
								{
									AutomationBot._seedBlueprintCache.Add(new KeyValuePair<ItemAsset, int>(itemAsset, i));
								}
							}
						}
					}
				}
			}
			foreach (KeyValuePair<ItemAsset, int> keyValuePair in AutomationBot._seedBlueprintCache)
			{
				ItemAsset key = keyValuePair.Key;
				int value = keyValuePair.Value;
				Blueprint blueprint2 = key.blueprints[value];
				bool flag18 = blueprint2 == null || blueprint2.outputs == null;
				if (!flag18)
				{
					bool flag19 = parameters.Length == 2;
					if (flag19)
					{
						methodInfo.Invoke(crafting, new object[] { key, value });
						AutomationBot.farmTotalCrafted++;
						return true;
					}
					bool flag20 = parameters.Length == 1 && parameters[0].ParameterType == typeof(Blueprint);
					if (flag20)
					{
						methodInfo.Invoke(crafting, new object[] { blueprint2 });
						AutomationBot.farmTotalCrafted++;
						return true;
					}
				}
			}
		}
		catch
		{
		}
		return false;
	}
	private static bool TryStoreItems(Player player)
	{
		bool flag = AutomationBot.farmStorages.Count == 0;
		bool flag2;
		if (flag)
		{
			flag2 = false;
		}
		else
		{
			bool flag3 = AutomationBot._askStoreStorage == null;
			if (flag3)
			{
				flag2 = false;
			}
			else
			{
				AutomationBot.StorageEntry storageEntry = null;
				float num = float.MaxValue;
				Vector3 position = player.transform.position;
				for (int i = 0; i < AutomationBot.farmStorages.Count; i++)
				{
					bool flag4 = AutomationBot.farmStorages[i].storage == null;
					if (flag4)
					{
						AutomationBot.farmStorages.RemoveAt(i);
						i--;
					}
					else
					{
						float sqrMagnitude = (AutomationBot.farmStorages[i].position - position).sqrMagnitude;
						bool flag5 = sqrMagnitude < num;
						if (flag5)
						{
							num = sqrMagnitude;
							storageEntry = AutomationBot.farmStorages[i];
						}
					}
				}
				bool flag6 = storageEntry == null || num > (float)(MiscConfig.autoFarmStoreRadius * MiscConfig.autoFarmStoreRadius);
				if (flag6)
				{
					flag2 = false;
				}
				else
				{
					try
					{
						ParameterInfo[] parameters = AutomationBot._askStoreStorage.GetParameters();
						byte b = 0;
						while ((int)b < player.inventory.items.Length)
						{
							byte itemCount = player.inventory.getItemCount(b);
							for (byte b2 = 0; b2 < itemCount; b2 += 1)
							{
								ItemJar item = player.inventory.getItem(b, b2);
								bool flag7 = item == null || item.item == null;
								if (!flag7)
								{
									ItemAsset itemAsset = Assets.find(EAssetType.ITEM, item.item.id) as ItemAsset;
									bool flag8 = itemAsset == null;
									if (!flag8)
									{
										string text = itemAsset.type.ToString();
										bool flag9 = text != "FOOD" && text != "CONSUMABLE" && text != "FARM";
										if (!flag9)
										{
											bool flag10 = parameters.Length == 5 && parameters[0].ParameterType == typeof(Player);
											if (flag10)
											{
												AutomationBot._askStoreStorage.Invoke(storageEntry.storage, new object[] { player, b, b2, item.x, item.y });
												AutomationBot.farmTotalStored++;
												return true;
											}
											bool flag11 = parameters.Length == 4 && parameters[0].ParameterType == typeof(Player);
											if (flag11)
											{
												AutomationBot._askStoreStorage.Invoke(storageEntry.storage, new object[] { player, b, item.x, item.y });
												AutomationBot.farmTotalStored++;
												return true;
											}
											bool flag12 = parameters.Length == 3 && parameters[0].ParameterType == typeof(Player);
											if (flag12)
											{
												AutomationBot._askStoreStorage.Invoke(storageEntry.storage, new object[] { player, b, b2 });
												AutomationBot.farmTotalStored++;
												return true;
											}
											bool flag13 = parameters.Length == 2 && parameters[0].ParameterType == typeof(Player);
											if (flag13)
											{
												AutomationBot._askStoreStorage.Invoke(storageEntry.storage, new object[] { player, b });
												AutomationBot.farmTotalStored++;
												return true;
											}
										}
									}
								}
							}
							b += 1;
						}
					}
					catch
					{
					}
					flag2 = false;
				}
			}
		}
		return flag2;
	}
	public static List<ItemInfo> allItems = new List<ItemInfo>();
	public static Dictionary<ushort, List<InteractableItem>> nearbyItemsById = new Dictionary<ushort, List<InteractableItem>>();
	public static List<InteractableItem> nearbyItems = new List<InteractableItem>();
	public static List<Type> trackedTypes = new List<Type>
	{
		typeof(ItemGunAsset),
		typeof(ItemMagazineAsset),
		typeof(ItemMedicalAsset),
		typeof(ItemFoodAsset),
		typeof(ItemWaterAsset),
		typeof(ItemBackpackAsset),
		typeof(ItemChargeAsset),
		typeof(ItemFuelAsset),
		typeof(ItemClothingAsset),
		typeof(ItemMeleeAsset),
		typeof(ItemFarmAsset),
		typeof(ItemConsumeableAsset)
	};
	public static Dictionary<string, Type> trackedTypesByName = new Dictionary<string, Type>();
	public static float itemScanTimer = 0f;
	public static float pickupTimer = 0f;
	private static ReflectedField<bool> _primaryPressedField = new ReflectedField<bool>(typeof(PlayerEquipment), "localWasPrimaryPressedBetweenSimulationFrames", BindingFlags.Instance | BindingFlags.NonPublic);
	private static ReflectedField<bool> _primaryReleasedField = new ReflectedField<bool>(typeof(PlayerEquipment), "localWasPrimaryReleasedBetweenSimulationFrames", BindingFlags.Instance | BindingFlags.NonPublic);
	private static ReflectedField<object> _fisherStateField = new ReflectedField<object>(typeof(UseableFisher), "fishingState", BindingFlags.Instance | BindingFlags.NonPublic);
	private static ReflectedField<float> _fisherTimeSinceBiteField = new ReflectedField<float>(typeof(UseableFisher), "timeSinceFishNotification", BindingFlags.Instance | BindingFlags.NonPublic);
	private static ReflectedField<object> _fisherNextRewardField = new ReflectedField<object>(typeof(UseableFisher), "nextRewardItem", BindingFlags.Instance | BindingFlags.NonPublic);
	private static ReflectedField<int> _fisherFishPositionField = new ReflectedField<int>(typeof(UseableFisher), "fishPosition", BindingFlags.Instance | BindingFlags.NonPublic);
	private static ReflectedField<int> _fisherChallengeInputPositionField = new ReflectedField<int>(typeof(UseableFisher), "challengeInputPosition", BindingFlags.Instance | BindingFlags.NonPublic);
	private static ReflectedField<int> _fisherChallengeLengthField = new ReflectedField<int>(typeof(UseableFisher), "challengeLength", BindingFlags.Instance | BindingFlags.NonPublic);
	private static ReflectedField<float> _fisherFishSpeedField = new ReflectedField<float>(typeof(UseableFisher), "fishSpeed", BindingFlags.Instance | BindingFlags.NonPublic);
	private static int _fishingState = 0;
	private static float _fishingTimer = 0f;
	private static float _fishingBiteTimer = 0f;
	private static float _lastTimeSinceBite = 999f;
	private static bool _biteDetected = false;
	private static bool _enteredCatchChallenge = false;
	private static bool _isPullingUp = false;
	private static bool _isPrimaryHeld = false;
	private static float _autoEquipTimer = 0f;
	private static int _lastFisherState = -1;
	private static float _stateTimer = 0f;
	private static int _consecutiveBiteChecks = 0;
	private static float _lastFishPos = 0f;
	private static float _fishVelocity = 0f;
	private static float _challengeTimer = 0f;
	private static bool _autoEquippedRod = false;
	public static int fishingTotalCasts = 0;
	public static int fishingTotalCatches = 0;
	public static int fishingTotalMisses = 0;
	public static int fishingTotalEmptyCasts = 0;
	public static int fishingTotalStuckRecoveries = 0;
	public static DateTime fishingSessionStart = DateTime.MinValue;
	public static string fishingLastCatchItem = "";
	public static DateTime fishingLastCatchTime = DateTime.MinValue;
	public static string fishingStatusText = "Idle";
	private static List<InteractableFarm> _nearbyFarms = new List<InteractableFarm>();
	private static float _farmScanTimer = 0f;
	private static float _seedRefreshTimer = 0f;
	private static float _harvestTimer = 0f;
	private static float _fertilizeTimer = 0f;
	private static float _plantTimer = 0f;
	private static float _placePlantTimer = 0f;
	private static float _autoPlaceClearTimer = 0f;
	private static Vector3 _lastPlaceAttemptPoint = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
	public static int farmTotalPlanted = 0;
	public static int farmTotalHarvested = 0;
	public static int farmTotalFertilized = 0;
	public static int farmPlotsGrowing = 0;
	public static int farmPlotsGrown = 0;
	public static int farmPlotsEmpty = 0;
	public static DateTime farmSessionStart = DateTime.MinValue;
	public static List<string> inventorySeedNames = new List<string>();
	public static List<ushort> inventorySeedIds = new List<ushort>();
	public static List<string> inventoryPlantNames = new List<string>();
	public static Vector3? autoPlaceTargetPoint = null;
	public static List<Vector3> frozenGridPoints = new List<Vector3>();
	public static bool gridFrozen = false;
	public static int frozenGridPlaceIndex = 0;
	public static Vector3 frozenGridOrigin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
	private static bool _farmReflected = false;
	private static MethodInfo _askFarm;
	private static MethodInfo _sendHarvest;
	private static MethodInfo _clientHarvest;
	private static MethodInfo _sendInteract;
	private static MethodInfo _askStoreStorage;
	private static MethodInfo _askCraft;
	private static MethodInfo _sendCraft;
	private static PropertyInfo _isFullyGrownProp;
	private static PropertyInfo _plantedProp;
	private static PropertyInfo _growthProp;
	private static FieldInfo _piHit;
	private static FieldInfo _piLastInteract;
	private static FieldInfo _piInteractable;
	private static bool _interactableResolved = false;
	private static float _craftTimer = 0f;
	private static float _storeTimer = 0f;
	private static float _lastFarmAction = 0f;
	private static bool _farmPrimaryNeedsRelease = false;
	private static float _farmPrimaryReleaseTimer = 0f;
	private static List<KeyValuePair<ItemAsset, int>> _seedBlueprintCache = null;
	private static float _seedBlueprintCacheTime = 0f;
	public static int farmTotalCrafted = 0;
	public static int farmTotalStored = 0;
	public static List<AutomationBot.StorageEntry> farmStorages = new List<AutomationBot.StorageEntry>();
	public class StorageEntry
	{
		public InteractableStorage storage;
		public Vector3 position;
		public string name;
	}
}
