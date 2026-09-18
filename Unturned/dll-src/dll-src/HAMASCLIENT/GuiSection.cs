using System;
using UnityEngine;
public class GuiSection
{
	public GuiSection(int height, SimpleAction action)
	{
		this.Height = height;
		this.DrawAction = action;
	}
	public Vector2 ScrollPosition = Vector2.zero;
	public int Height = 0;
	public int Padding = 5;
	public Texture2D BackgroundTexture;
	public SimpleAction DrawAction;
}
