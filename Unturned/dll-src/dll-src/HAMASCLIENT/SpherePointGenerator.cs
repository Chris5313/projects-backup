using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public static class SpherePointGenerator
{
	[InitializeAttribute]
	private static void Initialize()
	{
		SpherePointGenerator.BuildSpherePoints();
	}
	public static void RebuildSpherePoints()
	{
		AimbotConfig.SphereSizes = AimbotConfig.SphereSizes.OrderBy<AimSphereOptions, float>((AimSphereOptions so) => so.sphereSizeField).ToList<AimSphereOptions>();
		SpherePointGenerator.BuildSpherePoints();
	}
	public static void BuildSpherePoints()
	{
		AimbotConfig.MaxSphereSize = 0f;
		Vector3[][] array = new Vector3[AimbotConfig.SphereSizes.Count][];
		int num = 0;
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = SpherePointGenerator.GenerateSpherePoints(AimbotConfig.SphereSizes[i].sphereSegmentsField, AimbotConfig.SphereSizes[i].sphereSizeField);
			num += (AimbotConfig.SphereSizes[i].sphereSegmentsField + 1) * (AimbotConfig.SphereSizes[i].sphereSegmentsField + 1);
			AimbotConfig.MaxSphereSize = Mathf.Max(AimbotConfig.MaxSphereSize, AimbotConfig.SphereSizes[i].sphereSizeField);
		}
		SpherePointGenerator.SpherePoints = new Vector3[num];
		int num2 = 0;
		foreach (Vector3[] array3 in array)
		{
			Array.Copy(array3, 0, SpherePointGenerator.SpherePoints, num2, array3.Length);
			num2 += array3.Length;
		}
	}
	private static Vector3[] GenerateSpherePoints(int segments, float sphereSize)
	{
		Vector3[] array3;
		try
		{
			int num = (segments + 1) * (segments + 1);
			Vector3[] array = new Vector3[num];
			int num2 = 0;
			for (int i = 0; i <= segments; i++)
			{
				float num3 = (float)i / (float)segments;
				float num4 = num3 * 3.1415927f;
				for (int j = 0; j <= segments; j++)
				{
					float num5 = (float)j / (float)segments;
					float num6 = num5 * 2f * 3.1415927f;
					float num7 = Mathf.Sin(num4) * Mathf.Cos(num6) * sphereSize;
					float num8 = Mathf.Cos(num4) * sphereSize;
					float num9 = Mathf.Sin(num4) * Mathf.Sin(num6) * sphereSize;
					array[num2] = new Vector3(num7, num8, num9);
					num2++;
				}
			}
			List<Vector3> list = array.ToList<Vector3>();
			foreach (Vector3 vector in array)
			{
				bool flag = false;
				for (int l = 0; l < list.Count; l++)
				{
					bool flag2 = vector == list[l];
					bool flag3 = flag2;
					if (flag3)
					{
						bool flag4 = !flag;
						bool flag5 = flag4;
						if (flag5)
						{
							flag = true;
						}
						else
						{
							list.RemoveAt(l);
						}
					}
				}
			}
			array3 = list.ToArray();
		}
		catch
		{
			array3 = new Vector3[] { Vector3.up * 5f };
		}
		return array3;
	}
	public static Vector3[] SpherePoints = new Vector3[0];
}
