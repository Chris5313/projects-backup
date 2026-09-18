using System;
using SDG.Unturned;
using UnityEngine;
public class WeaponInfoWindow : WindowBase
{
	public override bool GetAviablity()
	{
		return MiscConfig.showWeaponInfo && Provider.isConnected && Player.player != null && Player.player.equipment.asset != null && (Player.player.equipment.asset is ItemGunAsset || Player.player.equipment.asset is ItemMeleeAsset);
	}
	public override bool UseStaticRect()
	{
		return MiscConfig.useStaticRectForWeaponInfo;
	}
	public override Rect GetStaticRect()
	{
		return new Rect((float)(Screen.width - 175), (float)(Screen.height / 2 - 85), 175f, 170f);
	}
	public override Vector2 GetSize()
	{
		return new Vector2(175f, 170f);
	}
	public override void DrawWindow()
	{
		base.DrawSectionHeader("Weapon Information");
		bool flag = AimbotUtil.aimObjective.TargetType == TargetType.Player;
		Player player = ((flag && AimbotUtil.currentAimObjective.Target != null) ? ((Player)AimbotUtil.currentAimObjective.Target) : null);
		bool flag2 = Player.player.equipment.asset is ItemGunAsset;
		bool flag3 = flag2;
		if (flag3)
		{
			ItemGunAsset itemGunAsset = Player.player.equipment.asset as ItemGunAsset;
			this.DrawTextLine(string.Format("{0} - {1}", itemGunAsset.itemName, itemGunAsset.id));
			this.DrawTextLine(string.Format("Range: {0}", MathUtil.GetAimTargetDistance()));
			string text = ((flag && player != null) ? string.Format(" * {0} = {1}", ArmorCalculator.GetLimbArmorFactor(ELimb.SKULL, player), itemGunAsset.playerDamageMultiplier.damage * itemGunAsset.playerDamageMultiplier.skull * ArmorCalculator.GetLimbArmorFactor(ELimb.SKULL, player)) : "");
			string text2 = ((flag && player != null) ? string.Format(" * {0} = {1}", ArmorCalculator.GetLimbArmorFactor(ELimb.SPINE, player), itemGunAsset.playerDamageMultiplier.damage * itemGunAsset.playerDamageMultiplier.spine * ArmorCalculator.GetLimbArmorFactor(ELimb.SPINE, player)) : "");
			string text3 = ((flag && player != null) ? string.Format(" * {0} = {1}", ArmorCalculator.GetLimbArmorFactor(ELimb.LEFT_ARM, player), itemGunAsset.playerDamageMultiplier.damage * itemGunAsset.playerDamageMultiplier.arm * ArmorCalculator.GetLimbArmorFactor(ELimb.LEFT_ARM, player)) : "");
			string text4 = ((flag && player != null) ? string.Format(" * {0} = {1}", ArmorCalculator.GetLimbArmorFactor(ELimb.LEFT_LEG, player), itemGunAsset.playerDamageMultiplier.damage * itemGunAsset.playerDamageMultiplier.leg * ArmorCalculator.GetLimbArmorFactor(ELimb.LEFT_LEG, player)) : "");
			this.DrawTextLine(string.Format("Skull: {0}{1}", itemGunAsset.playerDamageMultiplier.damage * itemGunAsset.playerDamageMultiplier.skull, text));
			this.DrawTextLine(string.Format("Body: {0}{1}", itemGunAsset.playerDamageMultiplier.damage * itemGunAsset.playerDamageMultiplier.spine, text2));
			this.DrawTextLine(string.Format("Arms: {0}{1}", itemGunAsset.playerDamageMultiplier.damage * itemGunAsset.playerDamageMultiplier.arm, text3));
			this.DrawTextLine(string.Format("Legs: {0}{1}", itemGunAsset.playerDamageMultiplier.damage * itemGunAsset.playerDamageMultiplier.leg, text4));
			GuiAreaState.DrawTexture(GuiStyles.GetPlayerEquippedItemIcon(Player.player), 171, 60);
		}
		else
		{
			ItemMeleeAsset itemMeleeAsset = Player.player.equipment.asset as ItemMeleeAsset;
			this.DrawTextLine(string.Format("{0} - {1}", itemMeleeAsset.itemName, itemMeleeAsset.id));
			this.DrawTextLine(string.Format("Range: {0}", MathUtil.GetAimTargetDistance()));
			string text5 = ((flag && player != null) ? string.Format(" * {0} = {1}", ArmorCalculator.GetLimbArmorFactor(ELimb.SKULL, player), itemMeleeAsset.playerDamageMultiplier.damage * itemMeleeAsset.playerDamageMultiplier.skull * ArmorCalculator.GetLimbArmorFactor(ELimb.SKULL, player)) : "");
			string text6 = ((flag && player != null) ? string.Format(" * {0} = {1}", ArmorCalculator.GetLimbArmorFactor(ELimb.SPINE, player), itemMeleeAsset.playerDamageMultiplier.damage * itemMeleeAsset.playerDamageMultiplier.spine * ArmorCalculator.GetLimbArmorFactor(ELimb.SPINE, player)) : "");
			string text7 = ((flag && player != null) ? string.Format(" * {0} = {1}", ArmorCalculator.GetLimbArmorFactor(ELimb.LEFT_ARM, player), itemMeleeAsset.playerDamageMultiplier.damage * itemMeleeAsset.playerDamageMultiplier.arm * ArmorCalculator.GetLimbArmorFactor(ELimb.LEFT_ARM, player)) : "");
			string text8 = ((flag && player != null) ? string.Format(" * {0} = {1}", ArmorCalculator.GetLimbArmorFactor(ELimb.LEFT_LEG, player), itemMeleeAsset.playerDamageMultiplier.damage * itemMeleeAsset.playerDamageMultiplier.leg * ArmorCalculator.GetLimbArmorFactor(ELimb.LEFT_LEG, player)) : "");
			this.DrawTextLine(string.Format("Skull: {0}{1}", itemMeleeAsset.playerDamageMultiplier.damage * itemMeleeAsset.playerDamageMultiplier.skull, text5));
			this.DrawTextLine(string.Format("Body: {0}{1}", itemMeleeAsset.playerDamageMultiplier.damage * itemMeleeAsset.playerDamageMultiplier.spine, text6));
			this.DrawTextLine(string.Format("Arms: {0}{1}", itemMeleeAsset.playerDamageMultiplier.damage * itemMeleeAsset.playerDamageMultiplier.arm, text7));
			this.DrawTextLine(string.Format("Legs: {0}{1}", itemMeleeAsset.playerDamageMultiplier.damage * itemMeleeAsset.playerDamageMultiplier.leg, text8));
			GuiAreaState.DrawTexture(GuiStyles.GetPlayerEquippedItemIcon(Player.player), 171, 60);
		}
	}
	public void DrawTextLine(string text)
	{
		GuiAreaState.Label(text);
		GuiAreaState.Space(-4);
	}
}
