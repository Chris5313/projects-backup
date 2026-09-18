using System;
using System.Reflection;
using SDG.Unturned;
using UnityEngine;
public class BarricadePlacementHook
{
	[HookMethodAttribute(typeof(UseableBarricade), "checkSpace", BindingFlags.Instance | BindingFlags.NonPublic, new Type[] { })]
	public static bool OnCheckSpace(UseableBarricade instance)
	{
		bool flag = AutomationBot.autoPlaceTargetPoint != null && instance.channel.IsLocalPlayer;
		bool flag2;
		if (flag)
		{
			Vector3 value = AutomationBot.autoPlaceTargetPoint.Value;
			RaycastHit raycastHit = default(RaycastHit);
			raycastHit.point = value;
			raycastHit.normal = Vector3.up;
			BarricadePlacementHook.HitField.Set(instance, raycastHit);
			Vector3 vector = value + Vector3.up * instance.equippedBarricadeAsset.offset;
			BarricadePlacementHook.PointField.Set(instance, vector);
			flag2 = true;
		}
		else
		{
			bool flag3 = OverrideManager.CallOriginalInstance<bool>(instance, Array.Empty<object>());
			bool flag4 = MiscConfig.freeCamera && FreeCamera.Instance != null && instance.channel.IsLocalPlayer;
			bool flag5 = flag4;
			bool flag9;
			if (flag5)
			{
				Ray ray = new Ray(FreeCamera.Instance.transform.position, FreeCamera.Instance.transform.forward);
				RaycastHit raycastHit2 = default(RaycastHit);
				Physics.Raycast(ray.origin, ray.direction, out raycastHit2, instance.equippedBarricadeAsset.range, RayMasks.SLOTS_INTERACT);
				BarricadePlacementHook.HitField.Set(instance, raycastHit2);
				bool flag6 = raycastHit2.collider == null;
				bool flag7 = flag6;
				if (flag7)
				{
					BarricadePlacementHook.PointField.Set(instance, Vector3.zero);
					bool isLocalPlayer = instance.channel.IsLocalPlayer;
					bool flag8 = isLocalPlayer;
					if (flag8)
					{
						PlayerUI.hint(null, EPlayerMessage.WINDOW);
					}
					flag9 = false;
				}
				else
				{
					bool flag10 = raycastHit2.normal.y > 0.75f;
					bool flag11 = flag10;
					Vector3 vector2;
					if (flag11)
					{
						vector2 = raycastHit2.point + raycastHit2.normal * instance.equippedBarricadeAsset.offset;
					}
					else
					{
						vector2 = raycastHit2.point + Vector3.up * instance.equippedBarricadeAsset.offset;
					}
					BarricadePlacementHook.PointField.Set(instance, vector2);
					flag9 = true;
				}
			}
			else
			{
				bool customBuildOffset = MiscConfig.customBuildOffset;
				bool flag12 = customBuildOffset;
				if (flag12)
				{
					RaycastHit raycastHit3 = default(RaycastHit);
					Physics.Raycast(new Ray(MainCamera.instance.transform.position, MainCamera.instance.transform.forward), out raycastHit3, instance.equippedBarricadeAsset.range, RayMasks.SLOTS_INTERACT);
					BarricadePlacementHook.HitField.Set(instance, raycastHit3);
					Vector3 vector3 = MainCamera.instance.transform.position + MainCamera.instance.transform.forward * MiscConfig.buildForwardOffset;
					vector3.y += MiscConfig.buildYOffset;
					bool flag13 = raycastHit3.normal.y > 0.75f;
					bool flag14 = flag13;
					if (flag14)
					{
						vector3 += raycastHit3.normal * instance.equippedBarricadeAsset.offset;
					}
					else
					{
						vector3 += Vector3.up * instance.equippedBarricadeAsset.offset;
					}
					BarricadePlacementHook.PointField.Set(instance, vector3);
					flag9 = true;
				}
				else
				{
					flag9 = MiscConfig.ignoreBarricadePlacementErrors || flag3;
				}
			}
			flag2 = flag9;
		}
		return flag2;
	}
	[HookMethodAttribute(typeof(UseableBarricade), "checkClaims", BindingFlags.Instance | BindingFlags.NonPublic, new Type[] { })]
	public static bool OnCheckClaims(UseableBarricade instance)
	{
		bool flag = OverrideManager.CallOriginalInstance<bool>(instance, Array.Empty<object>());
		return MiscConfig.ignoreBarricadePlacementErrors || flag;
	}
	public static ReflectedField<RaycastHit> HitField = new ReflectedField<RaycastHit>(typeof(UseableBarricade), "hit", BindingFlags.Instance | BindingFlags.NonPublic);
	public static ReflectedField<Vector3> PointField = new ReflectedField<Vector3>(typeof(UseableBarricade), "point", BindingFlags.Instance | BindingFlags.NonPublic);
}
