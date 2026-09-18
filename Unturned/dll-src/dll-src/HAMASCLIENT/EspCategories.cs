using System;
using System.Collections.Generic;
using UnityEngine;
public static class EspCategories
{
	public static EspCategory[] categories = new EspCategory[]
	{
		new EspCategory("Players", "{1} - {4} [{0}]", null, new EspCategoryAction(EspManager.DrawPlayerEsp), new ValueTuple<Color32, string>[]
		{
			new ValueTuple<Color32, string>(Color.red, "Player box color"),
			new ValueTuple<Color32, string>(new Color32(byte.MaxValue, 0, 0, 20), "Player box 2D fill color"),
			new ValueTuple<Color32, string>(Color.red, "Player line color"),
			new ValueTuple<Color32, string>(Color.red, "Player outline color"),
			new ValueTuple<Color32, string>(Color.red, "Player text color"),
			new ValueTuple<Color32, string>(Color.black, "Player text outline color"),
			new ValueTuple<Color32, string>(Color.red, "Player skeleton color"),
			new ValueTuple<Color32, string>(Color.red, "Player chams color"),
			new ValueTuple<Color32, string>(Color.cyan, "Player teammate box color"),
			new ValueTuple<Color32, string>(new Color32(0, byte.MaxValue, byte.MaxValue, 20), "Player teammate box 2D fill color"),
			new ValueTuple<Color32, string>(Color.cyan, "Player teammate line color"),
			new ValueTuple<Color32, string>(Color.cyan, "Player teammate outline color"),
			new ValueTuple<Color32, string>(Color.cyan, "Player teammate text color"),
			new ValueTuple<Color32, string>(Color.black, "Player teammate text outline color"),
			new ValueTuple<Color32, string>(Color.cyan, "Player teammate skeleton color"),
			new ValueTuple<Color32, string>(Color.red, "Player teammate chams color"),
			new ValueTuple<Color32, string>(Color.yellow, "Player enemy box color"),
			new ValueTuple<Color32, string>(new Color32(byte.MaxValue, byte.MaxValue, 0, 20), "Player enemy box 2D fill color"),
			new ValueTuple<Color32, string>(Color.yellow, "Player enemy line color"),
			new ValueTuple<Color32, string>(Color.yellow, "Player enemy outline color"),
			new ValueTuple<Color32, string>(Color.yellow, "Player enemy text color"),
			new ValueTuple<Color32, string>(Color.black, "Player enemy text outline color"),
			new ValueTuple<Color32, string>(Color.yellow, "Player enemy skeleton color"),
			new ValueTuple<Color32, string>(Color.red, "Player enemy chams color")
		}, new OptionBase[]
		{
			new BoolOption(true, "Enable skeleton"),
			new BoolOption(true, "Refresh players by group"),
			new BoolOption(false, "Draw teammates over distance"),
			new BoolOption(false, "Draw enemies over distance"),
			new BoolOption(false, "Draw item icon"),
			new IntOption(50, 20, 200, "Icon size"),
			new BoolOption(false, "Draw avatar"),
			new IntOption(50, 5, 200, "Avatar size"),
			new BoolOption(false, "Show dead players"),
			new StringConfigOption("Dead", "Dead player format"),
			new BoolOption(false, "Format safezone label"),
			new StringConfigOption("Safezone", "Safezone format"),
			new BoolOption(false, "Enable visibility check"),
			new IntOption(1, 1, 5, "Skeleton thickness"),
			new BoolOption(false, "Enable OOF arrows"),
			new IntOption(20, 5, 100, "OOF arrow size"),
			new IntOption(100, 10, 1000, "OOF arrow distance"),
			new BoolOption(false, "Draw equipment icons (screen top)"),
			new BoolOption(false, "Draw equipment icons (on player)"),
			new BoolOption(false, "Show weapon ammo"),
			new IntOption(30, 10, 100, "Equipment icon size")
		}),
		new EspCategory("Items", "{1} [{0}]", new EspCategoryAction(EspManager.CollectEspItems), new EspCategoryAction(EspManager.DrawItemEsp), new ValueTuple<Color32, string>[]
		{
			new ValueTuple<Color32, string>(Color.red, "Items box color"),
			new ValueTuple<Color32, string>(new Color32(byte.MaxValue, 0, 0, 20), "Items box 2D fill color"),
			new ValueTuple<Color32, string>(Color.red, "Items line color"),
			new ValueTuple<Color32, string>(Color.red, "Items outline color"),
			new ValueTuple<Color32, string>(Color.red, "Items text color"),
			new ValueTuple<Color32, string>(Color.black, "Items text outline color"),
			new ValueTuple<Color32, string>(Color.red, "Items chams color")
		}, new OptionBase[]
		{
			new ItemSortFilterOption(),
			new BoolOption(true, "Draw item icon"),
			new IntOption(50, 5, 200, "Icon size")
		}),
		new EspCategory("Vehicle", "{1} - {2} [{0}]", null, new EspCategoryAction(EspManager.DrawVehicleEsp), new ValueTuple<Color32, string>[]
		{
			new ValueTuple<Color32, string>(Color.red, "Vehicles box color"),
			new ValueTuple<Color32, string>(new Color32(byte.MaxValue, 0, 0, 20), "Vehicles box 2D fill color"),
			new ValueTuple<Color32, string>(Color.red, "Vehicles line color"),
			new ValueTuple<Color32, string>(Color.red, "Vehicles outline color"),
			new ValueTuple<Color32, string>(Color.red, "Vehicles text color"),
			new ValueTuple<Color32, string>(Color.black, "Vehicles text outline color"),
			new ValueTuple<Color32, string>(Color.red, "Vehicles chams color")
		}, new OptionBase[]
		{
			new BoolOption(false, "Show only unlocked cars"),
			new BoolOption(true, "Show health bar")
		}),
		new EspCategory("Zombies", "Zombie [{0}]", null, new EspCategoryAction(EspManager.DrawZombieEsp), new ValueTuple<Color32, string>[]
		{
			new ValueTuple<Color32, string>(Color.red, "Zombies box color"),
			new ValueTuple<Color32, string>(new Color32(byte.MaxValue, 0, 0, 20), "Zombies box 2D fill color"),
			new ValueTuple<Color32, string>(Color.red, "Zombies line color"),
			new ValueTuple<Color32, string>(Color.red, "Zombies outline color"),
			new ValueTuple<Color32, string>(Color.red, "Zombies text color"),
			new ValueTuple<Color32, string>(Color.black, "Zombies text outline color"),
			new ValueTuple<Color32, string>(Color.red, "Zombies chams color")
		}, new OptionBase[]
		{
			new BoolOption(false, "Enable skeleton"),
			new IntOption(1, 1, 5, "Skeleton thickness")
		}),
		new EspCategory("Generators", "Generator - {1} ({2}%) [{0}]", null, new EspCategoryAction(EspManager.DrawGeneratorEsp), new ValueTuple<Color32, string>[]
		{
			new ValueTuple<Color32, string>(Color.red, "Generators box color"),
			new ValueTuple<Color32, string>(new Color32(byte.MaxValue, 0, 0, 20), "Generators box 2D fill color"),
			new ValueTuple<Color32, string>(Color.red, "Generators line color"),
			new ValueTuple<Color32, string>(Color.red, "Generators outline color"),
			new ValueTuple<Color32, string>(Color.red, "Generators text color"),
			new ValueTuple<Color32, string>(Color.black, "Generators text outline color"),
			new ValueTuple<Color32, string>(Color.red, "Generators chams color")
		}, Array.Empty<OptionBase>()),
		new EspCategory("Animals", "{1} [{0}]", null, new EspCategoryAction(EspManager.DrawAnimalEsp), new ValueTuple<Color32, string>[]
		{
			new ValueTuple<Color32, string>(Color.red, "Animals box color"),
			new ValueTuple<Color32, string>(new Color32(byte.MaxValue, 0, 0, 20), "Animals box 2D fill color"),
			new ValueTuple<Color32, string>(Color.red, "Animals line color"),
			new ValueTuple<Color32, string>(Color.red, "Animals outline color"),
			new ValueTuple<Color32, string>(Color.red, "Animals text color"),
			new ValueTuple<Color32, string>(Color.black, "Animals text outline color"),
			new ValueTuple<Color32, string>(Color.red, "Animals chams color")
		}, Array.Empty<OptionBase>()),
		new EspCategory("Beds", "{1} - {2} [{0}]", null, new EspCategoryAction(EspManager.DrawBedEsp), new ValueTuple<Color32, string>[]
		{
			new ValueTuple<Color32, string>(Color.red, "Beds box color"),
			new ValueTuple<Color32, string>(new Color32(byte.MaxValue, 0, 0, 20), "Beds box 2D fill color"),
			new ValueTuple<Color32, string>(Color.red, "Beds line color"),
			new ValueTuple<Color32, string>(Color.red, "Beds outline color"),
			new ValueTuple<Color32, string>(Color.red, "Beds text color"),
			new ValueTuple<Color32, string>(Color.black, "Beds text outline color"),
			new ValueTuple<Color32, string>(Color.red, "Beds chams color")
		}, new OptionBase[]
		{
			new BoolOption(false, "Show only claimed beds")
		}),
		new EspCategory("Turrets", "{1} [{0}]", null, new EspCategoryAction(EspManager.DrawTurretEsp), new ValueTuple<Color32, string>[]
		{
			new ValueTuple<Color32, string>(Color.red, "Turrets box color"),
			new ValueTuple<Color32, string>(new Color32(byte.MaxValue, 0, 0, 20), "Turrets box 2D fill color"),
			new ValueTuple<Color32, string>(Color.red, "Turrets line color"),
			new ValueTuple<Color32, string>(Color.red, "Turrets outline color"),
			new ValueTuple<Color32, string>(Color.red, "Turrets text color"),
			new ValueTuple<Color32, string>(Color.black, "Turrets text outline color"),
			new ValueTuple<Color32, string>(Color.red, "Turrets chams color")
		}, Array.Empty<OptionBase>()),
		new EspCategory("Bullets", "Bullet [{0}]", null, new EspCategoryAction(EspManager.DrawBulletEsp), new ValueTuple<Color32, string>[]
		{
			new ValueTuple<Color32, string>(Color.red, "Bullets box color"),
			new ValueTuple<Color32, string>(new Color32(byte.MaxValue, 0, 0, 20), "Bullets box 2D fill color"),
			new ValueTuple<Color32, string>(Color.red, "Bullets line color"),
			new ValueTuple<Color32, string>(Color.red, "Bullets outline color"),
			new ValueTuple<Color32, string>(Color.red, "Bullets text color"),
			new ValueTuple<Color32, string>(Color.black, "Bullets text outline color")
		}, Array.Empty<OptionBase>()),
		new EspCategory("Storages", "{1} [{0}]", null, new EspCategoryAction(EspManager.DrawStorageEsp), new ValueTuple<Color32, string>[]
		{
			new ValueTuple<Color32, string>(Color.red, "Storages box color"),
			new ValueTuple<Color32, string>(new Color32(byte.MaxValue, 0, 0, 20), "Storages box 2D fill color"),
			new ValueTuple<Color32, string>(Color.red, "Storages line color"),
			new ValueTuple<Color32, string>(Color.red, "Storages outline color"),
			new ValueTuple<Color32, string>(Color.red, "Storages text color"),
			new ValueTuple<Color32, string>(Color.black, "Storages text outline color"),
			new ValueTuple<Color32, string>(Color.red, "Storages chams color")
		}, new OptionBase[]
		{
			new ItemSortFilterOption(),
			new BoolOption(false, "Draw storage icon"),
			new IntOption(50, 5, 200, "Icon size"),
			new BoolOption(true, "Draw storage health bar")
		}),
		new EspCategory("Airdrops", "Airdrop [{0}]", null, new EspCategoryAction(EspManager.DrawAirdropEsp), new ValueTuple<Color32, string>[]
		{
			new ValueTuple<Color32, string>(Color.red, "Airdrop box color"),
			new ValueTuple<Color32, string>(new Color32(byte.MaxValue, 0, 0, 20), "Airdrop box 2D fill color"),
			new ValueTuple<Color32, string>(Color.red, "Airdrop line color"),
			new ValueTuple<Color32, string>(Color.red, "Airdrop outline color"),
			new ValueTuple<Color32, string>(Color.red, "Airdrop text color"),
			new ValueTuple<Color32, string>(Color.black, "Airdrop text outline color"),
			new ValueTuple<Color32, string>(Color.red, "Airdrop chams color")
		}, Array.Empty<OptionBase>()),
		new EspCategory("Grenade", "Grenade - [{0}]", null, new EspCategoryAction(EspManager.DrawGrenadeEsp), new ValueTuple<Color32, string>[]
		{
			new ValueTuple<Color32, string>(Color.red, "Grenade box color"),
			new ValueTuple<Color32, string>(new Color32(byte.MaxValue, 0, 0, 20), "Grenade box 2D fill color"),
			new ValueTuple<Color32, string>(Color.red, "Grenade line color"),
			new ValueTuple<Color32, string>(Color.red, "Grenade outline color"),
			new ValueTuple<Color32, string>(Color.red, "Grenade text color"),
			new ValueTuple<Color32, string>(Color.black, "Grenade text outline color"),
			new ValueTuple<Color32, string>(Color.red, "Grenade chams color"),
			new ValueTuple<Color32, string>(Color.red, "Grenade explosion sphere color")
		}, new OptionBase[]
		{
			new BoolOption(false, "Show explosion radius")
		}),
		new EspCategory("Ores", "{1} ({2}%) [{0}]", null, new EspCategoryAction(EspManager.DrawOresESP), new ValueTuple<Color32, string>[]
		{
			new ValueTuple<Color32, string>(Color.red, "Ores box color"),
			new ValueTuple<Color32, string>(new Color32(byte.MaxValue, 0, 0, 20), "Ores box 2D fill color"),
			new ValueTuple<Color32, string>(Color.red, "Ores line color"),
			new ValueTuple<Color32, string>(Color.red, "Ores outline color"),
			new ValueTuple<Color32, string>(Color.red, "Ores text color"),
			new ValueTuple<Color32, string>(Color.black, "Ores text outline color"),
			new ValueTuple<Color32, string>(Color.red, "Ores chams color")
		}, new OptionBase[]
		{
			new BoolOption(false, "Show dead resources"),
			new BoolOption(true, "Show health bar")
		})
	};
	public static List<EspCategory> activeCategories = new List<EspCategory>();
	public static Color32 ChamVisibleColor = new Color32(0, byte.MaxValue, 0, byte.MaxValue);
	public static Color32 ChamInvisibleColor = new Color32(byte.MaxValue, 0, 0, byte.MaxValue);
	public static Color32 ChamWireframeColor = new Color32(0, byte.MaxValue, byte.MaxValue, byte.MaxValue);
}
