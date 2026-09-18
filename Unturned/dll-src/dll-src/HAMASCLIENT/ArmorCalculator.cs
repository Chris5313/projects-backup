using System;
using SDG.Unturned;
public static class ArmorCalculator
{
	public static float GetLimbArmorFactor(ELimb limb, Player player)
	{
		bool flag = limb == ELimb.LEFT_LEG || limb == ELimb.RIGHT_FOOT || limb == ELimb.RIGHT_LEG;
		bool flag2 = flag;
		if (flag2)
		{
			ItemClothingAsset pantsAsset = player.clothing.pantsAsset;
			bool flag3 = pantsAsset != null;
			bool flag4 = flag3;
			if (flag4)
			{
				return pantsAsset.armor + (1f - pantsAsset.armor) * (1f - (float)player.clothing.pantsQuality / 100f);
			}
		}
		else
		{
			bool flag5 = limb == ELimb.LEFT_HAND || limb == ELimb.LEFT_ARM || limb == ELimb.RIGHT_HAND || limb == ELimb.RIGHT_ARM;
			bool flag6 = flag5;
			if (flag6)
			{
				ItemClothingAsset shirtAsset = player.clothing.shirtAsset;
				bool flag7 = shirtAsset != null;
				bool flag8 = flag7;
				if (flag8)
				{
					return shirtAsset.armor + (1f - shirtAsset.armor) * (1f - (float)player.clothing.shirtQuality / 100f);
				}
			}
			else
			{
				bool flag9 = limb == ELimb.SPINE;
				bool flag10 = flag9;
				if (flag10)
				{
					float num = 1f;
					bool flag11 = player.clothing.vestAsset != null;
					bool flag12 = flag11;
					if (flag12)
					{
						ItemClothingAsset vestAsset = player.clothing.vestAsset;
						num *= vestAsset.armor + (1f - vestAsset.armor) * (1f - (float)player.clothing.vestQuality / 100f);
					}
					bool flag13 = player.clothing.shirtAsset != null;
					bool flag14 = flag13;
					if (flag14)
					{
						ItemClothingAsset shirtAsset2 = player.clothing.shirtAsset;
						num *= shirtAsset2.armor + (1f - shirtAsset2.armor) * (1f - (float)player.clothing.shirtQuality / 100f);
					}
					return num;
				}
				bool flag15 = limb == ELimb.SKULL;
				bool flag16 = flag15;
				if (flag16)
				{
					ItemClothingAsset hatAsset = player.clothing.hatAsset;
					bool flag17 = hatAsset != null;
					bool flag18 = flag17;
					if (flag18)
					{
						return hatAsset.armor + (1f - hatAsset.armor) * (1f - (float)player.clothing.hatQuality / 100f);
					}
				}
			}
		}
		return 1f;
	}
}
