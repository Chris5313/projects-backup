using System;
using SDG.Unturned;
using UnityEngine;
public class OtherTab : FeatureTabBase
{
	public override string GetName()
	{
		return "Other";
	}
	public override TabCount GetTabCounts()
	{
		return TabCount.Three;
	}
	public override void DoTab(TabCount tc)
	{
		bool flag = tc == TabCount.One;
		if (flag)
		{
			base.DrawSectionHeader("FOV Sizes");
			for (int i = 0; i < FovCircleRenderer.FovCircles.Length; i++)
			{
				bool flag2 = MenuGuiHelper.Button(FovCircleRenderer.FovCircles[i].FovName, -1, true, null);
				if (flag2)
				{
					this.SelectedFovIndex = i;
				}
			}
		}
		else
		{
			bool flag3 = tc != TabCount.Two;
			if (flag3)
			{
				base.DrawSectionHeader("Experimental");
				bool flag4 = MenuGuiHelper.Button("Disconnect from server", -1, true, null) && Provider.isConnected;
				if (flag4)
				{
					Provider.disconnect();
				}
				GUILayout.Label("bytesSent: " + Provider.bytesSent.ToString(), Array.Empty<GUILayoutOption>());
				GUILayout.Label("bytesRecieved: " + Provider.bytesReceived.ToString(), Array.Empty<GUILayoutOption>());
				GUILayout.Label("packetsSent: " + Provider.packetsSent.ToString(), Array.Empty<GUILayoutOption>());
				GUILayout.Label("packetsRecieved: " + Provider.packetsReceived.ToString(), Array.Empty<GUILayoutOption>());
			}
			else
			{
				bool flag5 = FovCircleRenderer.FovCircles.Length < this.SelectedFovIndex;
				if (flag5)
				{
					base.DrawSectionHeader("FOV option");
				}
				else
				{
					FovCircleSetting dxMhfufThyuW1UdZ5MxaTXe5X = FovCircleRenderer.FovCircles[this.SelectedFovIndex];
					base.DrawSectionHeader(dxMhfufThyuW1UdZ5MxaTXe5X.FovName);
					dxMhfufThyuW1UdZ5MxaTXe5X.DrawEnabled = MenuGuiHelper.DrawCheckbox(dxMhfufThyuW1UdZ5MxaTXe5X.DrawEnabled, "Draw FOV", Array.Empty<GUILayoutOption>());
					bool flag6 = MenuGuiHelper.DrawCheckbox(dxMhfufThyuW1UdZ5MxaTXe5X.RainbowEnabled, "Rainbow fading", Array.Empty<GUILayoutOption>());
					bool flag7 = flag6 != dxMhfufThyuW1UdZ5MxaTXe5X.RainbowEnabled;
					if (flag7)
					{
						dxMhfufThyuW1UdZ5MxaTXe5X.RainbowEnabled = flag6;
						ColorSetting dsuKLIF35AtXmFdSPtbkEXBdM = ColorConfig.GetColorEntry(dxMhfufThyuW1UdZ5MxaTXe5X.FovColorSettingName);
						dsuKLIF35AtXmFdSPtbkEXBdM.isGradient = flag6;
					}
					try
					{
						dxMhfufThyuW1UdZ5MxaTXe5X.UseFovScale = MenuGuiHelper.DrawCheckbox(dxMhfufThyuW1UdZ5MxaTXe5X.UseFovScale, "Use FOV scaled system", Array.Empty<GUILayoutOption>());
						bool d90SIYM0nK4JzltkCLOO2tC9W = dxMhfufThyuW1UdZ5MxaTXe5X.UseFovScale;
						if (d90SIYM0nK4JzltkCLOO2tC9W)
						{
							dxMhfufThyuW1UdZ5MxaTXe5X.FovScaleDegrees = MenuGuiHelper.LabeledIntSlider("FOV scale: ", dxMhfufThyuW1UdZ5MxaTXe5X.FovScaleDegrees, 1, ((MainCamera.instance != null) ? ((int)MainCamera.instance.fieldOfView) : ((int)Camera.current.fieldOfView)) + 40, -1);
						}
						else
						{
							dxMhfufThyuW1UdZ5MxaTXe5X.FovPixels = MenuGuiHelper.LabeledIntSlider("FOV pixels: ", dxMhfufThyuW1UdZ5MxaTXe5X.FovPixels, 1, 400, -1);
						}
					}
					catch
					{
					}
					FovCircleRenderer.FovCircles[this.SelectedFovIndex] = dxMhfufThyuW1UdZ5MxaTXe5X;
				}
			}
		}
	}
	public Vector2 ScrollPosition = Vector2.zero;
	public int SelectedFovIndex;
}
