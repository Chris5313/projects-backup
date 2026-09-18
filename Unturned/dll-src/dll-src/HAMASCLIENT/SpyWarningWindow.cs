using System;
using UnityEngine;
public class SpyWarningWindow : WindowBase
{
	public bool Is4By3AspectRatio()
	{
		return Mathf.Approximately((float)Screen.width / (float)Screen.height, 1.3333334f);
	}
	public override bool GetAviablity()
	{
		return MiscConfig.notifyAboutSpy && Time.realtimeSinceStartup - ScreenshotManager.ScreenshotTimer < 5f;
	}
	public override bool IsShowOnMenu()
	{
		return false;
	}
	public override bool UseStaticRect()
	{
		return true;
	}
	public override Vector2 GetSize()
	{
		return this.Is4By3AspectRatio() ? new Vector2((float)MiscConfig.spyWindowSize, (float)MiscConfig.spyWindowSize * 0.75f + 18f) : new Vector2((float)MiscConfig.spyWindowSize * 1.6f, (float)MiscConfig.spyWindowSize * 0.9f + 18f);
	}
	public override Rect GetStaticRect()
	{
		Vector2 size = this.GetSize();
		return new Rect((float)(Screen.width - 20) - size.x, 20f, size.x, size.y);
	}
	public override void DrawWindow()
	{
		Vector2 size = this.GetSize();
		bool flag = ScreenshotManager.LastScreenshotTexture != null;
		bool flag2 = flag;
		if (flag2)
		{
			GuiAreaState.DrawTexture(ScreenshotManager.LastScreenshotTexture, (int)(size.x - 4f), (int)(size.y - 22f));
		}
		GuiAreaState.Label("[WARNING] You have been spied by a server admin");
	}
}
