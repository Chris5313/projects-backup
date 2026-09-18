using System;
using System.Reflection;
using SDG.Unturned;
using UnityEngine;
public class StructurePlacementPatch
{
	[HookMethodAttribute(typeof(UseableStructure), "UpdatePendingPlacement", BindingFlags.Instance | BindingFlags.NonPublic, new Type[] { })]
	public static bool UpdatePendingPlacementOverride(UseableStructure instance)
	{
		bool flag = OverrideManager.CallOriginalInstance<bool>(instance, Array.Empty<object>());
		bool flag2 = MiscConfig.freeCamera && FreeCamera.Instance != null && instance.channel.IsLocalPlayer;
		bool flag3 = flag2;
		if (flag3)
		{
			Ray ray = new Ray(FreeCamera.Instance.transform.position, FreeCamera.Instance.transform.forward);
			RaycastHit raycastHit = default(RaycastHit);
			bool flag4 = !Physics.SphereCast(ray.origin, 0.1f, ray.direction, out raycastHit, instance.equippedStructureAsset.range, RayMasks.STRUCTURE_INTERACT);
			bool flag5 = flag4;
			if (flag5)
			{
				StructurePlacementPatch.pendingPlacementPositionField.Set(instance, Vector3.zero);
				return false;
			}
			StructurePlacementPatch.pendingPlacementPositionField.Set(instance, raycastHit.point);
			StructurePlacementPatch.pendingPlacementYawField.Set(instance, FreeCamera.Instance.transform.eulerAngles.y);
		}
		else
		{
			bool customBuildOffset = MiscConfig.customBuildOffset;
			bool flag6 = customBuildOffset;
			if (flag6)
			{
				Vector3 vector = MainCamera.instance.transform.position + MainCamera.instance.transform.forward * MiscConfig.buildForwardOffset;
				vector.y += MiscConfig.buildYOffset;
				StructurePlacementPatch.pendingPlacementPositionField.Set(instance, vector);
				StructurePlacementPatch.pendingPlacementYawField.Set(instance, FreeCamera.Instance.transform.eulerAngles.y);
				return true;
			}
		}
		return MiscConfig.ignoreStructurePlacementErrors || flag;
	}
	public static ReflectedField<Vector3> pendingPlacementPositionField = new ReflectedField<Vector3>(typeof(UseableStructure), "pendingPlacementPosition", BindingFlags.Instance | BindingFlags.NonPublic);
	public static ReflectedField<float> pendingPlacementYawField = new ReflectedField<float>(typeof(UseableStructure), "pendingPlacementYaw", BindingFlags.Instance | BindingFlags.NonPublic);
}
