using System;
using UnityEngine;
public class GuiHost : MonoBehaviour
{
	public static void OnGuiDraw()
	{
		bool dbjv74arVJtUMAqsSN0cWr9w = ScreenshotManager.IsSpying;
		bool flag = !dbjv74arVJtUMAqsSN0cWr9w;
		if (flag)
		{
			bool flag2 = !GuiStyles.StylesReady;
			bool flag3 = flag2;
			if (flag3)
			{
				GuiStyles.BuildGuiStyles(GUI.skin);
			}
			try
			{
				bool flag4 = Event.current.type == EventType.Repaint;
				bool flag5 = flag4;
				if (flag5)
				{
					OverlayDrawer.OnUpdate();
					ColorConfig.Update();
					try
					{
						EspManager.DrawEspWorld();
					}
					catch (Exception ex)
					{
						GUI.Label(new Rect(5f, 25f, 300f, 20f), "Error while processing ESP");
						GUI.Label(new Rect(5f, 45f, 300f, 20f), ex.Message);
						GUI.Label(new Rect(5f, 65f, 500f, 200f), ex.StackTrace);
					}
					TracerRenderer.RenderAll();
					HudOverlayDrawer.Draw();
				}
				try
				{
					OverlayDrawer.OnGUI();
				}
				catch
				{
				}
				try
				{
					MenuState.DrawMenu();
				}
				catch
				{
				}
			}
			catch (Exception ex2)
			{
				Debug.Log(ex2.Message);
				Debug.Log(ex2.StackTrace);
			}
			WindowDragger.UpdateMousePosition();
			GuiAreaState.Update();
		}
	}
}
