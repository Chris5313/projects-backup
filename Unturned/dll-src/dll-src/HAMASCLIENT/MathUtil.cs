using System;
using SDG.Unturned;
using UnityEngine;
public static class MathUtil
{
	public static string FormatSeconds(int givedSeconds)
	{
		int num = givedSeconds % 60;
		int num2 = givedSeconds / 60;
		int num3 = num2 / 60;
		int num4 = (givedSeconds - num3 * 3600) / 60;
		string text = ((num3 < 10) ? ("0" + num3.ToString()) : num3.ToString());
		string text2 = ((num4 < 10) ? ("0" + num4.ToString()) : num4.ToString());
		string text3 = ((num < 10) ? ("0" + num.ToString()) : num.ToString());
		return string.Concat(new string[] { text, ":", text2, ":", text3 });
	}
	public static string GetCurrentTimeString()
	{
		DateTime now = DateTime.Now;
		string text = ((now.Second < 10) ? ("0" + now.Second.ToString()) : now.Second.ToString());
		string text2 = ((now.Hour < 10) ? ("0" + now.Hour.ToString()) : now.Hour.ToString());
		string text3 = ((now.Minute < 10) ? ("0" + now.Minute.ToString()) : now.Minute.ToString());
		return string.Concat(new string[] { text2, ":", text3, ":", text });
	}
	public static Quaternion LookRotationTo(Vector3 origin, Vector3 pos)
	{
		return Quaternion.LookRotation(MathUtil.Normalize(pos - origin));
	}
	public static Vector3 LookRotationEulerTo(Vector3 origin, Vector3 pos)
	{
		return Quaternion.LookRotation(MathUtil.Normalize(pos - origin)).eulerAngles;
	}
	public static Vector3 DirectionTo(Vector3 origin, Vector3 pos)
	{
		return MathUtil.Normalize(pos - origin);
	}
	public static float Distance(Vector3 a, Vector3 b)
	{
		float num = a.x - b.x;
		float num2 = a.y - b.y;
		float num3 = a.z - b.z;
		return Mathf.Sqrt(num * num + num2 * num2 + num3 * num3);
	}
	public static Vector3 Normalize(Vector3 vector)
	{
		return vector / (float)Math.Sqrt((double)(vector.x * vector.x + vector.y * vector.y + vector.z * vector.z));
	}
	public static int GetAimTargetDistance()
	{
		bool setDistanceByGunRange = AimbotConfig.setDistanceByGunRange;
		bool flag = setDistanceByGunRange;
		int num;
		if (flag)
		{
			bool flag2 = Player.player != null && Player.player.equipment.asset != null && Player.player.equipment.asset is ItemGunAsset;
			bool flag3 = flag2;
			if (flag3)
			{
				bool flag4 = AimbotConfig.manuallyCalculateBallisticDistance && Provider.modeConfigData.Gameplay.Ballistics;
				bool flag5 = flag4;
				if (flag5)
				{
					num = (int)((Player.player.equipment.asset as ItemGunAsset).ballisticTravel * (float)((int)(Player.player.equipment.asset as ItemGunAsset).ballisticSteps + (MiscConfig.extendBallisticRange ? MiscConfig.additionalBallisticSteps : 0)) + (float)(MiscConfig.extendBallisticRange ? 4 : 0) + (AimbotConfig.expandRangeBySphere ? AimbotConfig.MaxSphereSize : 0f));
				}
				else
				{
					num = (int)(Player.player.equipment.asset as ItemGunAsset).range + (MiscConfig.extendBallisticRange ? 4 : 0) + (int)(AimbotConfig.expandRangeBySphere ? AimbotConfig.MaxSphereSize : 0f);
				}
			}
			else
			{
				bool flag6 = Player.player != null && Player.player.equipment.asset != null && Player.player.equipment.asset is ItemMeleeAsset;
				bool flag7 = flag6;
				if (flag7)
				{
					num = 19 + (int)(Player.player.equipment.asset as ItemMeleeAsset).range;
				}
				else
				{
					num = 21;
				}
			}
		}
		else
		{
			num = AimbotConfig.aimTargetDistance;
		}
		return num;
	}
}
