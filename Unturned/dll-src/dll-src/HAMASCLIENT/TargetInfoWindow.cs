using System;
using SDG.Unturned;
using UnityEngine;
public class TargetInfoWindow : WindowBase
{
	public override bool GetAviablity()
	{
		bool independentPlayerInfoTargeting = MiscConfig.independentPlayerInfoTargeting;
		if (independentPlayerInfoTargeting)
		{
			Player player = this.GetAimedPlayer();
			this.CurrentTarget = ((player != null) ? new AimObjective(TargetType.Player, player, Vector3.zero) : new AimObjective(TargetType.Player, null, Vector3.zero));
		}
		else
		{
			this.CurrentTarget = AimbotUtil.currentAimObjective;
		}
		return MiscConfig.displayPlayerInfo && (this.CurrentTarget.Target != null || MiscConfig.displayPlayerInfoAlways);
	}
	public Player GetAimedPlayer()
	{
		float num = (float)FovCircleRenderer.GetFovRadius("Independent player info targeting FOV");
		foreach (SteamPlayer steamPlayer in Provider.clients)
		{
			bool flag = steamPlayer != null && !(steamPlayer.player == null) && !steamPlayer.player.life.isDead && Vector3.Distance(steamPlayer.player.transform.position, Player.player.transform.position) <= (float)MiscConfig.independetPlayerInfoTargetingDistance;
			if (flag)
			{
				Vector3 vector = (MiscConfig.freeCamera ? FreeCamera.FreeCameraComponent.WorldToScreenPoint(steamPlayer.player.transform.position) : MainCamera.instance.WorldToScreenPoint(steamPlayer.player.transform.position));
				bool flag2 = vector.z >= 0f;
				if (flag2)
				{
					vector.y = (float)Screen.height - vector.y;
					bool flag3 = Vector2.Distance(new Vector2((float)(Screen.width / 2), (float)(Screen.height / 2)), new Vector2(vector.x, vector.y)) <= num;
					if (flag3)
					{
						return steamPlayer.player;
					}
				}
			}
		}
		return null;
	}
	public override void DrawWindow()
	{
		base.DrawSectionHeader("Target Information");
		AimObjective dys4vgzJwangT6DO91J7FFHm = this.CurrentTarget;
		bool flag = dys4vgzJwangT6DO91J7FFHm.Target == null;
		if (flag)
		{
			GuiAreaState.Label("No Target");
		}
		else
		{
			switch (dys4vgzJwangT6DO91J7FFHm.TargetType)
			{
			case TargetType.Player:
			{
				Player player = (Player)dys4vgzJwangT6DO91J7FFHm.Target;
				bool flag2 = player != null;
				if (flag2)
				{
					GuiAreaState.Label("Type: Player");
					GuiAreaState.Label("Steam Name: " + player.channel.owner.playerID.playerName);
					GuiAreaState.Label("Character: " + player.channel.owner.playerID.characterName);
					GuiAreaState.Label("Nickname: " + player.channel.owner.playerID.nickName);
					PlayerEquipment equipment = player.equipment;
					bool flag3 = equipment != null && equipment.asset != null;
					if (flag3)
					{
						GuiAreaState.Label("Holding: " + equipment.asset.itemName);
					}
					else
					{
						GuiAreaState.Label("Holding: None");
					}
				}
				else
				{
					GuiAreaState.Label("Type: Player");
					GuiAreaState.Label("Target is null");
				}
				break;
			}
			case TargetType.ClaimFlags:
			{
				InteractableClaim interactableClaim = (InteractableClaim)dys4vgzJwangT6DO91J7FFHm.Target;
				GuiAreaState.Label("Type: Claim Flag");
				GuiAreaState.Label("Name: " + interactableClaim.name);
				break;
			}
			case TargetType.Beds:
			{
				InteractableBed interactableBed = (InteractableBed)dys4vgzJwangT6DO91J7FFHm.Target;
				GuiAreaState.Label("Type: Bed");
				GuiAreaState.Label("Name: " + interactableBed.name);
				break;
			}
			case TargetType.Storages:
			{
				InteractableStorage interactableStorage = (InteractableStorage)dys4vgzJwangT6DO91J7FFHm.Target;
				GuiAreaState.Label("Type: Storage");
				GuiAreaState.Label("Name: " + interactableStorage.name);
				break;
			}
			case TargetType.Zombies:
			{
				Zombie zombie = (Zombie)dys4vgzJwangT6DO91J7FFHm.Target;
				GuiAreaState.Label("Type: Zombie");
				GuiAreaState.Label("Status: " + (zombie.isDead ? "Dead" : "Alive"));
				break;
			}
			case TargetType.Animals:
			{
				Animal animal = (Animal)dys4vgzJwangT6DO91J7FFHm.Target;
				GuiAreaState.Label("Type: Animal");
				GuiAreaState.Label("Status: " + (animal.isDead ? "Dead" : "Alive"));
				break;
			}
			case TargetType.Vehicles:
			{
				InteractableVehicle interactableVehicle = (InteractableVehicle)dys4vgzJwangT6DO91J7FFHm.Target;
				GuiAreaState.Label("Type: Vehicle");
				GuiAreaState.Label("Name: " + interactableVehicle.asset.vehicleName);
				GuiAreaState.Label("Health: " + interactableVehicle.health.ToString() + " / " + interactableVehicle.asset.health.ToString());
				GuiAreaState.Label("Status: " + (interactableVehicle.isLocked ? "Locked" : "Unlocked"));
				break;
			}
			default:
				GuiAreaState.Label("Unknown Target");
				break;
			}
		}
	}
	public override Vector2 GetSize()
	{
		bool flag = this.CurrentTarget.Target == null;
		int num;
		if (flag)
		{
			num = 1;
		}
		else
		{
			switch (this.CurrentTarget.TargetType)
			{
			case TargetType.Player:
				num = 5;
				break;
			case TargetType.ClaimFlags:
			case TargetType.Beds:
			case TargetType.Storages:
				num = 2;
				break;
			case TargetType.Zombies:
			case TargetType.Animals:
				num = 2;
				break;
			case TargetType.Vehicles:
				num = 4;
				break;
			default:
				num = 1;
				break;
			}
		}
		float num2 = 28f + (float)num * 18f + 6f;
		return new Vector2(280f, num2);
	}
	public AimObjective CurrentTarget;
}
