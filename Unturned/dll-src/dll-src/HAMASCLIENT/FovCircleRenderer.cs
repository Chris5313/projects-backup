using System;
using System.Collections.Generic;
using SDG.Unturned;
using UnityEngine;
public class FovCircleRenderer
{
	[InitializeAttribute]
	private static void BuildNameIndexMap()
	{
		for (int i = 0; i < FovCircleRenderer.FovCircles.Length; i++)
		{
			FovCircleRenderer.FovNameIndexMap.Add(FovCircleRenderer.FovCircles[i].FovName, i);
		}
	}
	public static void DrawFovCircles()
	{
		Camera camera = ((MainCamera.instance != null) ? MainCamera.instance : Camera.main);
		FovCircleRenderer.ScreenCenterX = Screen.width / 2;
		FovCircleRenderer.ScreenCenterY = Screen.height / 2;
		EspManager.ScreenLineMaterial.SetPass(0);
		GL.Begin(1);
		foreach (FovCircleSetting dxMhfufThyuW1UdZ5MxaTXe5X in FovCircleRenderer.FovCircles)
		{
			bool flag = !dxMhfufThyuW1UdZ5MxaTXe5X.DrawEnabled || (dxMhfufThyuW1UdZ5MxaTXe5X.VisibilityCondition != null && !dxMhfufThyuW1UdZ5MxaTXe5X.VisibilityCondition());
			bool flag2 = !flag;
			if (flag2)
			{
				ColorSetting dsuKLIF35AtXmFdSPtbkEXBdM = ColorConfig.GetColorEntry(dxMhfufThyuW1UdZ5MxaTXe5X.FovColorSettingName);
				GL.Color(dsuKLIF35AtXmFdSPtbkEXBdM.settedColor);
				float num;
				try
				{
					num = (dxMhfufThyuW1UdZ5MxaTXe5X.UseFovScale ? ((float)(Math.Tan((double)dxMhfufThyuW1UdZ5MxaTXe5X.FovScaleDegrees * 0.017453292519943295 / 2.0) / Math.Tan((double)camera.fieldOfView * 0.017453292519943295 / 2.0) * (double)Screen.height)) : ((float)dxMhfufThyuW1UdZ5MxaTXe5X.FovPixels));
				}
				catch
				{
					num = (dxMhfufThyuW1UdZ5MxaTXe5X.UseFovScale ? ((float)(Math.Tan((double)dxMhfufThyuW1UdZ5MxaTXe5X.FovScaleDegrees * 0.017453292519943295 / 2.0) / Math.Tan(0.3490658503988659) * (double)Screen.height)) : ((float)dxMhfufThyuW1UdZ5MxaTXe5X.FovPixels));
				}
				bool flag3 = dxMhfufThyuW1UdZ5MxaTXe5X.RainbowEnabled && dsuKLIF35AtXmFdSPtbkEXBdM.isGradient;
				bool flag4 = flag3;
				if (flag4)
				{
					for (float num2 = 0f; num2 < 6.2831855f; num2 += 0.05f)
					{
						float num3 = num2 * 0.15915494f + dsuKLIF35AtXmFdSPtbkEXBdM.GradientOffset;
						bool flag5 = num3 > 1f;
						bool flag6 = flag5;
						if (flag6)
						{
							num3 -= 1f;
						}
						GL.Color(ColorConfig.RainbowColor(num3, dsuKLIF35AtXmFdSPtbkEXBdM.settedColor.a));
						GL.Vertex(new Vector3(Mathf.Cos(num2) * num + (float)FovCircleRenderer.ScreenCenterX, Mathf.Sin(num2) * num + (float)FovCircleRenderer.ScreenCenterY));
						GL.Vertex(new Vector3(Mathf.Cos(num2 + 0.05f) * num + (float)FovCircleRenderer.ScreenCenterX, Mathf.Sin(num2 + 0.05f) * num + (float)FovCircleRenderer.ScreenCenterY));
					}
				}
				else
				{
					for (float num4 = 0f; num4 < 6.2831855f; num4 += 0.05f)
					{
						GL.Vertex(new Vector3(Mathf.Cos(num4) * num + (float)FovCircleRenderer.ScreenCenterX, Mathf.Sin(num4) * num + (float)FovCircleRenderer.ScreenCenterY));
						GL.Vertex(new Vector3(Mathf.Cos(num4 + 0.05f) * num + (float)FovCircleRenderer.ScreenCenterX, Mathf.Sin(num4 + 0.05f) * num + (float)FovCircleRenderer.ScreenCenterY));
					}
				}
			}
		}
		GL.End();
	}
	public static bool IsPointWithinFov(string fovName, Vector2 point, int distance = -1)
	{
		FovCircleSetting dxMhfufThyuW1UdZ5MxaTXe5X = FovCircleRenderer.FovCircles[FovCircleRenderer.FovNameIndexMap[fovName]];
		bool flag = distance == -1;
		bool flag2 = flag;
		if (flag2)
		{
			distance = (int)Vector2.Distance(new Vector2((float)FovCircleRenderer.ScreenCenterX, (float)FovCircleRenderer.ScreenCenterY), point);
		}
		return distance < FovCircleRenderer.GetFovRadiusPixels(dxMhfufThyuW1UdZ5MxaTXe5X);
	}
	public static int GetFovRadius(string fovName)
	{
		return FovCircleRenderer.GetFovRadiusPixels(FovCircleRenderer.FovCircles[FovCircleRenderer.FovNameIndexMap[fovName]]);
	}
	public static int GetFovRadiusPixels(FovCircleSetting fov)
	{
		return fov.UseFovScale ? ((int)(Math.Tan((double)fov.FovScaleDegrees * 0.017453292519943295 / 2.0) / Math.Tan((double)((MainCamera.instance != null) ? MainCamera.instance : Camera.main).fieldOfView * 0.017453292519943295 / 2.0) * (double)Screen.height)) : fov.FovPixels;
	}
	public static FovCircleSetting[] FovCircles = new FovCircleSetting[]
	{
		new FovCircleSetting("Aimbot FOV", "Aimbot FOV color", null),
		new FovCircleSetting("Grab items through walls FOV", "Grab items through walls FOV color", null),
		new FovCircleSetting("Independent player info targeting FOV", "Independent player info targeting color", null)
	};
	public static Dictionary<string, int> FovNameIndexMap = new Dictionary<string, int>();
	private static int ScreenCenterX = 0;
	private static int ScreenCenterY = 0;
}
