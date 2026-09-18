using System;
using UnityEngine;
public class WindowDragger
{
	public static void DragWindow(ref Rect rect)
	{
		bool flag = !Input.GetMouseButton(0) || Input.GetMouseButtonUp(0);
		bool flag2 = flag;
		if (flag2)
		{
			WindowDragger.IsDragging = false;
		}
		bool flag3 = (Input.GetMouseButtonDown(0) || WindowDragger.IsDragging) && rect.Contains(WindowDragger.LastMousePosition) && WindowDragger.LastMousePosition != new Vector2(Input.mousePosition.x, Input.mousePosition.y);
		bool flag4 = flag3;
		if (flag4)
		{
			WindowDragger.IsDragging = true;
			rect.x -= WindowDragger.LastMousePosition.x - Input.mousePosition.x;
			rect.y -= WindowDragger.LastMousePosition.y - ((float)Screen.height - Input.mousePosition.y);
		}
	}
	public static void UpdateMousePosition()
	{
		WindowDragger.LastMousePosition = new Vector2(Input.mousePosition.x, (float)Screen.height - Input.mousePosition.y);
	}
	public static bool IsDragging = false;
	public static Vector2 LastMousePosition = Vector2.zero;
}
