using System;
using System.Collections.Generic;
using System.Reflection;
using SDG.Provider;
using SDG.Unturned;
using Steamworks;
public static class SkinsManager
{
	[InitializeAttribute]
	private static void OnSettingChanged()
	{
		try
		{
			SkinsManager.Initialize();
		}
		catch (Exception ex)
		{
			Logger.LogClient("SkinsManager.Initialize failed: " + ex.Message + "\n" + ex.StackTrace);
		}
	}
	public static void Initialize()
	{
		Provider.onClientConnected = (Provider.ClientConnected)Delegate.Combine(Provider.onClientConnected, new Provider.ClientConnected(SkinsManager.ApplySkinsOnConnect));
		Provider.onServerConnected = (Provider.ServerConnected)Delegate.Combine(Provider.onServerConnected, new Provider.ServerConnected(delegate(CSteamID steamid)
		{
			SkinsManager.ApplySkinsOnConnect();
		}));
		bool flag = Bootstrapper.InitFinished && Provider.isConnected;
		if (flag)
		{
			SkinsManager.ApplySkinsOnConnect();
		}
		Dictionary<ClothingCategory, List<SkinInfo>> dictionary = new Dictionary<ClothingCategory, List<SkinInfo>>();
		foreach (UnturnedEconInfo unturnedEconInfo in SkinsManager.EconInfoField.Get().Values)
		{
			ClothingCategory dnFbkaUD6mnA1BajCnfh8ztdO;
			bool flag2 = SkinsManager.TryParseClothingSlot(unturnedEconInfo.display_type, out dnFbkaUD6mnA1BajCnfh8ztdO);
			if (flag2)
			{
				List<SkinInfo> list;
				bool flag3 = dictionary.TryGetValue(dnFbkaUD6mnA1BajCnfh8ztdO, out list);
				if (flag3)
				{
					list.Add(new SkinInfo(unturnedEconInfo, dnFbkaUD6mnA1BajCnfh8ztdO));
				}
				else
				{
					dictionary.Add(dnFbkaUD6mnA1BajCnfh8ztdO, new List<SkinInfo>
					{
						new SkinInfo(unturnedEconInfo, dnFbkaUD6mnA1BajCnfh8ztdO)
					});
				}
			}
		}
		foreach (ClothingCategory dnFbkaUD6mnA1BajCnfh8ztdO2 in dictionary.Keys)
		{
			SkinsManager.SkinCatalog[dnFbkaUD6mnA1BajCnfh8ztdO2] = dictionary[dnFbkaUD6mnA1BajCnfh8ztdO2].ToArray();
		}
	}
	public static SkinInfo FindSkinById(ClothingCategory st, int skinID)
	{
		foreach (SkinInfo d20i1Qc1Q96crSRmQYn2qE39Z in SkinsManager.SkinCatalog[st])
		{
			bool flag = d20i1Qc1Q96crSRmQYn2qE39Z.skinID == skinID;
			if (flag)
			{
				return d20i1Qc1Q96crSRmQYn2qE39Z;
			}
		}
		throw new NotImplementedException();
	}
	public static bool TryParseClothingSlot(string skinName, out ClothingCategory sk)
	{
		ClothingCategory dnFbkaUD6mnA1BajCnfh8ztdO = ClothingCategory.Unknown;
		bool flag = skinName.Contains("Shirt");
		if (flag)
		{
			dnFbkaUD6mnA1BajCnfh8ztdO = ClothingCategory.Shirt;
		}
		else
		{
			bool flag2 = skinName.Contains("Pants");
			if (flag2)
			{
				dnFbkaUD6mnA1BajCnfh8ztdO = ClothingCategory.Pants;
			}
			else
			{
				bool flag3 = skinName.Contains("Backpack");
				if (flag3)
				{
					dnFbkaUD6mnA1BajCnfh8ztdO = ClothingCategory.Backpack;
				}
				else
				{
					bool flag4 = skinName.Contains("Vest");
					if (flag4)
					{
						dnFbkaUD6mnA1BajCnfh8ztdO = ClothingCategory.Vest;
					}
					else
					{
						bool flag5 = skinName.Contains("Hat");
						if (flag5)
						{
							dnFbkaUD6mnA1BajCnfh8ztdO = ClothingCategory.Hat;
						}
						else
						{
							bool flag6 = skinName.Contains("Mask");
							if (flag6)
							{
								dnFbkaUD6mnA1BajCnfh8ztdO = ClothingCategory.Mask;
							}
							else
							{
								bool flag7 = skinName.Contains("Glass");
								if (flag7)
								{
									dnFbkaUD6mnA1BajCnfh8ztdO = ClothingCategory.Glass;
								}
							}
						}
					}
				}
			}
		}
		sk = dnFbkaUD6mnA1BajCnfh8ztdO;
		return dnFbkaUD6mnA1BajCnfh8ztdO > ClothingCategory.Unknown;
	}
	public static void SetSelectedSkin(SkinInfo ss)
	{
		bool flag = SkinsManager.SelectedSkins.ContainsKey(ss.ItemType);
		if (flag)
		{
			SkinsManager.SelectedSkins[ss.ItemType] = ss;
		}
		else
		{
			SkinsManager.SelectedSkins.Add(ss.ItemType, ss);
		}
		bool flag2 = Player.player != null;
		if (flag2)
		{
			SkinsManager.ApplySkinToPlayer(ss.ItemType, Player.player, true);
		}
	}
	public static void ApplySkinToPlayer(ClothingCategory st, Player p, bool applySkins = true)
	{
		switch (st)
		{
		case ClothingCategory.Hat:
			p.clothing.firstClothes.visualHat = SkinsManager.SelectedSkins[st].skinID;
			p.clothing.thirdClothes.visualHat = SkinsManager.SelectedSkins[st].skinID;
			p.clothing.characterClothes.visualHat = SkinsManager.SelectedSkins[st].skinID;
			break;
		case ClothingCategory.Mask:
			p.clothing.firstClothes.visualMask = SkinsManager.SelectedSkins[st].skinID;
			p.clothing.thirdClothes.visualMask = SkinsManager.SelectedSkins[st].skinID;
			p.clothing.characterClothes.visualMask = SkinsManager.SelectedSkins[st].skinID;
			break;
		case ClothingCategory.Glass:
			p.clothing.firstClothes.visualGlasses = SkinsManager.SelectedSkins[st].skinID;
			p.clothing.thirdClothes.visualGlasses = SkinsManager.SelectedSkins[st].skinID;
			p.clothing.characterClothes.visualGlasses = SkinsManager.SelectedSkins[st].skinID;
			break;
		case ClothingCategory.Vest:
			p.clothing.firstClothes.visualVest = SkinsManager.SelectedSkins[st].skinID;
			p.clothing.thirdClothes.visualVest = SkinsManager.SelectedSkins[st].skinID;
			p.clothing.characterClothes.visualVest = SkinsManager.SelectedSkins[st].skinID;
			break;
		case ClothingCategory.Backpack:
			p.clothing.firstClothes.visualBackpack = SkinsManager.SelectedSkins[st].skinID;
			p.clothing.thirdClothes.visualBackpack = SkinsManager.SelectedSkins[st].skinID;
			p.clothing.characterClothes.visualBackpack = SkinsManager.SelectedSkins[st].skinID;
			break;
		case ClothingCategory.Shirt:
			p.clothing.firstClothes.visualShirt = SkinsManager.SelectedSkins[st].skinID;
			p.clothing.thirdClothes.visualShirt = SkinsManager.SelectedSkins[st].skinID;
			p.clothing.characterClothes.visualShirt = SkinsManager.SelectedSkins[st].skinID;
			break;
		case ClothingCategory.Pants:
			p.clothing.firstClothes.visualPants = SkinsManager.SelectedSkins[st].skinID;
			p.clothing.thirdClothes.visualPants = SkinsManager.SelectedSkins[st].skinID;
			p.clothing.characterClothes.visualPants = SkinsManager.SelectedSkins[st].skinID;
			break;
		}
		if (applySkins)
		{
			p.clothing.firstClothes.apply();
			p.clothing.thirdClothes.apply();
			p.clothing.characterClothes.apply();
		}
	}
	public static void ApplySkinsPreDraw()
	{
		bool flag = SkinsManager.SelectedSkins.Count > 0 && Player.player != null;
		if (flag)
		{
			foreach (ClothingCategory dnFbkaUD6mnA1BajCnfh8ztdO in SkinsManager.SelectedSkins.Keys)
			{
				SkinsManager.ApplySkinToPlayer(dnFbkaUD6mnA1BajCnfh8ztdO, Player.player, false);
			}
			Player.player.clothing.firstClothes.apply();
			Player.player.clothing.thirdClothes.apply();
			Player.player.clothing.characterClothes.apply();
		}
	}
	public static void ApplySkinsPostDraw()
	{
		bool flag = SkinsManager.SelectedSkins.Count > 0 && Player.player != null;
		if (flag)
		{
			foreach (ClothingCategory dnFbkaUD6mnA1BajCnfh8ztdO in SkinsManager.SelectedSkins.Keys)
			{
				SkinsManager.ApplySkinToPlayer(dnFbkaUD6mnA1BajCnfh8ztdO, Player.player, false);
			}
			Player.player.clothing.firstClothes.apply();
			Player.player.clothing.thirdClothes.apply();
			Player.player.clothing.characterClothes.apply();
		}
	}
	public static void ApplySkinsOnConnect()
	{
		bool flag = Player.player == null;
		if (!flag)
		{
			foreach (ClothingCategory dnFbkaUD6mnA1BajCnfh8ztdO in SkinsManager.SelectedSkins.Keys)
			{
				SkinsManager.ApplySkinToPlayer(dnFbkaUD6mnA1BajCnfh8ztdO, Player.player, false);
			}
			Player.player.clothing.firstClothes.apply();
			Player.player.clothing.thirdClothes.apply();
			Player.player.clothing.characterClothes.apply();
		}
	}
	public static Dictionary<ClothingCategory, SkinInfo> SelectedSkins = new Dictionary<ClothingCategory, SkinInfo>();
	public static Dictionary<ClothingCategory, SkinInfo[]> SkinCatalog = new Dictionary<ClothingCategory, SkinInfo[]>();
	public static bool IsInitialized = false;
	private static PropertyRef<Dictionary<int, UnturnedEconInfo>> EconInfoField = new PropertyRef<Dictionary<int, UnturnedEconInfo>>(typeof(TempSteamworksEconomy), "econInfo", BindingFlags.Static | BindingFlags.NonPublic);
}
