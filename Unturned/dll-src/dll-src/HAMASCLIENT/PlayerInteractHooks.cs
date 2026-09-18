using System;
using System.Reflection;
using SDG.Unturned;
using UnityEngine;
public class PlayerInteractHooks : PlayerInteract
{
	[InitializeAttribute]
	public static void Init()
	{
		ScreenshotManager.PreDrawEvent = (SimpleDelegate)Delegate.Combine(ScreenshotManager.PreDrawEvent, new SimpleDelegate(PlayerInteractHooks.ClearInteractHit));
	}
	public static void ClearInteractHit()
	{
		bool pickupItemsThroughWalls = MiscConfig.pickupItemsThroughWalls;
		bool flag = pickupItemsThroughWalls;
		if (flag)
		{
			PlayerInteractHooks.InteractHitField.value = default(RaycastHit);
		}
	}
	[HookMethodAttribute(typeof(PlayerInteract), "Update", new Type[] { })]
	private void Update()
	{
		try
		{
			bool isLocalPlayer = base.channel.IsLocalPlayer;
			bool flag = isLocalPlayer;
			if (flag)
			{
				InteractableItem interactableItem = null;
				bool flag2 = MiscConfig.pickupItemsThroughWalls && !ScreenshotManager.IsSpying && AutomationBot.FindNearestItemInFov(MiscConfig.freeCamera ? ((int)Mathf.Clamp((float)MiscConfig.pickupItemsThroughWallsDistance - Vector3.Distance(Player.player.look.getEyesPosition(), FreeCamera.Instance.transform.position), 0f, (float)MiscConfig.pickupItemsThroughWallsDistance)) : MiscConfig.pickupItemsThroughWallsDistance, FovCircleRenderer.GetFovRadius("Grab items through walls FOV"), out interactableItem);
				bool flag3 = flag2;
				if (flag3)
				{
					Collider collider = interactableItem.transform.GetComponent<Collider>();
					bool flag4 = true;
					bool flag5 = collider == null;
					bool flag6 = flag5;
					if (flag6)
					{
						flag4 = false;
						collider = interactableItem.transform.gameObject.AddComponent<BoxCollider>();
						((BoxCollider)collider).size = new Vector3(0.25f, 0.25f, 0.25f);
					}
					Vector3 vector = collider.bounds.center + new Vector3(0f, collider.bounds.extents.y + 0.1f, 0f);
					RaycastHit raycastHit = default(RaycastHit);
					Physics.Raycast(new Ray(vector, MathUtil.DirectionTo(vector, collider.bounds.center)), out raycastHit, 4f, RayMasks.PLAYER_INTERACT);
					PlayerInteractHooks.InteractHitField.value = raycastHit;
					PlayerInteractHooks.LastInteractField.value = Time.realtimeSinceStartup;
					OverrideManager.CallOriginal(this, Array.Empty<object>());
					bool flag7 = !flag4;
					bool flag8 = flag7;
					if (flag8)
					{
						UnityEngine.Object.Destroy(collider);
					}
				}
				else
				{
					bool flag9 = MiscConfig.freeCamera && !ScreenshotManager.IsSpying;
					bool flag10 = flag9;
					if (flag10)
					{
						RaycastHit raycastHit2 = default(RaycastHit);
						Physics.Raycast(new Ray(FreeCamera.Instance.transform.position, FreeCamera.Instance.transform.forward), out raycastHit2, 20f, RayMasks.PLAYER_INTERACT);
						PlayerInteractHooks.InteractHitField.value = raycastHit2;
						PlayerInteractHooks.LastInteractField.value = Time.realtimeSinceStartup;
						OverrideManager.CallOriginal(this, Array.Empty<object>());
					}
					else
					{
						OverrideManager.CallOriginal(this, Array.Empty<object>());
					}
				}
			}
		}
		catch
		{
		}
	}
	[HookMethodAttribute(typeof(PlayerInteract), "get_salvageTime", new Type[] { })]
	private float GetSalvageTime()
	{
		bool flag = PlayerInteractHooks.ShouldOverrideSalvageTimeField.Get(this);
		bool flag2 = flag;
		float num;
		if (flag2)
		{
			num = PlayerInteractHooks.OverrideSalvageTimeValueField.Get(this) * MiscConfig.salvageTimeMultiplier;
		}
		else
		{
			bool flag3 = Player.player.equipment.useable is UseableHousingPlanner;
			bool flag4 = flag3;
			if (flag4)
			{
				num = 0.5f * MiscConfig.salvageTimeMultiplier;
			}
			else
			{
				bool flag5 = Provider.isServer || Player.player.channel.owner.isAdmin;
				bool flag6 = flag5;
				if (flag6)
				{
					LevelAsset asset = Level.getAsset();
					bool flag7 = asset == null || asset.enableAdminFasterSalvageDuration;
					bool flag8 = flag7;
					if (flag8)
					{
						return 1f * MiscConfig.salvageTimeMultiplier;
					}
				}
				num = 8f * MiscConfig.salvageTimeMultiplier;
			}
		}
		return num;
	}
	public static ReflectedField<RaycastHit> InteractHitField = new ReflectedField<RaycastHit>(typeof(PlayerInteract), "hit", BindingFlags.Static | BindingFlags.NonPublic);
	public static ReflectedField<float> LastInteractField = new ReflectedField<float>(typeof(PlayerInteract), "lastInteract", BindingFlags.Static | BindingFlags.NonPublic);
	public static ReflectedField<bool> ShouldOverrideSalvageTimeField = new ReflectedField<bool>(typeof(PlayerInteract), "shouldOverrideSalvageTime", BindingFlags.Instance | BindingFlags.NonPublic);
	public static ReflectedField<float> OverrideSalvageTimeValueField = new ReflectedField<float>(typeof(PlayerInteract), "overrideSalvageTimeValue", BindingFlags.Instance | BindingFlags.NonPublic);
}
