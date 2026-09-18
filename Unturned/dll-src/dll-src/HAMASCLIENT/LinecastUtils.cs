using System;
using SDG.Unturned;
using UnityEngine;
public static class LinecastUtils
{
	public static bool IsVisibleFromCamera(this Vector3 point)
	{
		return point.GetBestVisiblePoint(Player.player.look.aim.position, true) != Vector3.zero;
	}
	public static bool TryGetVisiblePoint(this Vector3 point, Vector3 camPos, out Vector3 returnVec, bool tryExpand = true)
	{
		returnVec = point.GetBestVisiblePoint(camPos, tryExpand);
		return returnVec != Vector3.zero;
	}
	public static bool TryGetVisiblePointFromCamera(this Vector3 point, out Vector3 returnVec, bool tryExpand = true)
	{
		returnVec = point.GetBestVisiblePoint(Player.player.look.aim.position, tryExpand);
		return returnVec != Vector3.zero;
	}
	public static Vector3 GetVisiblePointFromCamera(this Vector3 point, bool tryExpand = true)
	{
		return point.GetBestVisiblePoint(Player.player.look.aim.position, tryExpand);
	}
	public static Vector3 GetBestVisiblePoint(this Vector3 point, Vector3 camPos, bool tryExpand = true)
	{
		bool flag = Vector3.Distance(camPos, point) <= AimbotConfig.MaxSphereSize && AimbotConfig.setHitPointToCameraIfAviable;
		bool flag2 = flag;
		Vector3 vector;
		if (flag2)
		{
			vector = camPos;
		}
		else
		{
			Vector3 normalized = (point - camPos).normalized;
			bool flag3 = tryExpand && AimbotConfig.expandRangeBySphere && Vector3.Distance(camPos, point) > (float)MathUtil.GetAimTargetDistance() - AimbotConfig.MaxSphereSize;
			float num = (flag3 ? ((float)MathUtil.GetAimTargetDistance() - AimbotConfig.MaxSphereSize) : float.MaxValue);
			Vector3 vector2 = Vector3.zero;
			float num2 = float.MaxValue;
			float num3 = 0.0001f;
			Vector3[] dfCAnLXlM0BjQRdGSl6900LuF = SpherePointGenerator.SpherePoints;
			for (int i = 0; i < dfCAnLXlM0BjQRdGSl6900LuF.Length; i++)
			{
				Vector3 vector3 = point + dfCAnLXlM0BjQRdGSl6900LuF[i];
				bool flag4 = flag3 && Vector3.Distance(camPos, vector3) > num;
				if (!flag4)
				{
					bool flag5 = !Physics.Linecast(camPos, vector3, RayMasks.DAMAGE_CLIENT, QueryTriggerInteraction.Ignore);
					bool flag6 = !flag5;
					if (!flag6)
					{
						bool flag7 = !AimbotConfig.verifySphereToPlayerPointByLinecast || !Physics.Linecast(vector3, point, RayMasks.DAMAGE_CLIENT, QueryTriggerInteraction.Ignore);
						bool flag8 = !flag7;
						if (!flag8)
						{
							Vector3 vector4 = vector3 - camPos;
							float num4 = Vector3.Dot(vector4, normalized);
							float sqrMagnitude = (vector4 - normalized * num4).sqrMagnitude;
							bool flag9 = sqrMagnitude < num2;
							if (flag9)
							{
								num2 = sqrMagnitude;
								vector2 = vector3;
								bool flag10 = sqrMagnitude <= num3;
								if (flag10)
								{
									break;
								}
							}
						}
					}
				}
			}
			vector = vector2;
		}
		return vector;
	}
	public static Vector3 GetBestVisiblePointFromMainCamera(this Vector3 point, Vector3 camPos, bool tryExpand = true)
	{
		bool flag = Vector3.Distance(point, camPos) <= AimbotConfig.MaxSphereSize && AimbotConfig.setHitPointToCameraIfAviable;
		bool flag2 = flag;
		Vector3 vector;
		if (flag2)
		{
			vector = camPos;
		}
		else
		{
			Vector3 normalized = (point - camPos).normalized;
			Vector3 position = MainCamera.instance.transform.position;
			bool flag3 = tryExpand && AimbotConfig.expandRangeBySphere && Vector3.Distance(position, point) > (float)MathUtil.GetAimTargetDistance() - AimbotConfig.MaxSphereSize;
			float num = (flag3 ? ((float)MathUtil.GetAimTargetDistance() - AimbotConfig.MaxSphereSize) : float.MaxValue);
			Vector3 vector2 = Vector3.zero;
			float num2 = float.MaxValue;
			float num3 = 0.0001f;
			Vector3[] dfCAnLXlM0BjQRdGSl6900LuF = SpherePointGenerator.SpherePoints;
			for (int i = 0; i < dfCAnLXlM0BjQRdGSl6900LuF.Length; i++)
			{
				Vector3 vector3 = point + dfCAnLXlM0BjQRdGSl6900LuF[i];
				bool flag4 = flag3 && Vector3.Distance(position, vector3) > num;
				if (!flag4)
				{
					bool flag5 = !Physics.Linecast(position, vector3, RayMasks.DAMAGE_CLIENT, QueryTriggerInteraction.Ignore);
					bool flag6 = !flag5;
					if (!flag6)
					{
						bool flag7 = !AimbotConfig.verifySphereToPlayerPointByLinecast || !Physics.Linecast(vector3, point, RayMasks.DAMAGE_CLIENT, QueryTriggerInteraction.Ignore);
						bool flag8 = !flag7;
						if (!flag8)
						{
							Vector3 vector4 = vector3 - camPos;
							float num4 = Vector3.Dot(vector4, normalized);
							float sqrMagnitude = (vector4 - normalized * num4).sqrMagnitude;
							bool flag9 = sqrMagnitude < num2;
							if (flag9)
							{
								num2 = sqrMagnitude;
								vector2 = vector3;
								bool flag10 = sqrMagnitude <= num3;
								if (flag10)
								{
									break;
								}
							}
						}
					}
				}
			}
			vector = vector2;
		}
		return vector;
	}
}
