using System;
using SDG.Unturned;
using UnityEngine;
public class WindowBase
{
	// (get) Token: 0x060003C6 RID: 966 RVA: 0x00040E90 File Offset: 0x0003F090
	// (set) Token: 0x060003C7 RID: 967 RVA: 0x00040EE1 File Offset: 0x0003F0E1
	public Vector2 windowRect
	{
		get
		{
			bool flag = this.UseStaticRect();
			bool flag2 = flag;
			Vector2 dkPiBrZfDuyssgVsFrnhxjO4v;
			if (flag2)
			{
				dkPiBrZfDuyssgVsFrnhxjO4v = new Vector2(this.GetStaticRect().x, this.GetStaticRect().y);
			}
			else
			{
				dkPiBrZfDuyssgVsFrnhxjO4v = this.WindowPosition;
			}
			return dkPiBrZfDuyssgVsFrnhxjO4v;
		}
		set
		{
			this.WindowPosition = value;
		}
	}
	public virtual Vector2 GetSize()
	{
		return new Vector2(100f, 100f);
	}
	public virtual Rect GetStaticRect()
	{
		return new Rect(0f, 0f, 100f, 100f);
	}
	public virtual bool UseStaticRect()
	{
		return false;
	}
	public virtual bool IsShowOnMenu()
	{
		return false;
	}
	public virtual bool GetAviablity()
	{
		return true;
	}
	public void DrawSectionHeader(string text)
	{
		GuiAreaState.Box(text, 14);
	}
	public void DrawWindowFrame()
	{
		GUI.color = (this.IsShowOnMenu() ? MenuState.GetMenuFadeColor() : MenuState.GetWorldDimColor());
		Color color = ColorConfig.GetColor("Menu background color");
		Color32 color2 = ColorConfig.GetColor("Menu line color");
		MenuGuiHelper.DrawRect(new Rect(this.windowRect.x, this.windowRect.y, this.GetSize().x, this.GetSize().y), color, this.IsShowOnMenu(), ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(this.windowRect.x, this.windowRect.y, this.GetSize().x, 1f), color2, this.IsShowOnMenu(), ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(this.windowRect.x, this.windowRect.y + this.GetSize().y - 1f, this.GetSize().x, 1f), color2, this.IsShowOnMenu(), ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(this.windowRect.x, this.windowRect.y, 1f, this.GetSize().y), color2, this.IsShowOnMenu(), ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(this.windowRect.x + this.GetSize().x - 1f, this.windowRect.y, 1f, this.GetSize().y), color2, this.IsShowOnMenu(), ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(this.windowRect.x, this.windowRect.y, this.GetSize().x, 22f), new Color32(18, 18, 20, byte.MaxValue), this.IsShowOnMenu(), ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(this.windowRect.x, this.windowRect.y + 22f, this.GetSize().x, 1f), color2, this.IsShowOnMenu(), ScaleMode.StretchToFill);
		GuiAreaState.PushArea(new Rect(this.windowRect.x + 2f, this.windowRect.y + 2f, this.GetSize().x - 4f, this.GetSize().y - 4f));
		this.DrawWindow();
		GuiAreaState.PopArea();
		bool flag = new Rect(this.windowRect.x, this.windowRect.y, this.GetSize().x, this.GetSize().y).Contains(new Vector2(Input.mousePosition.x, (float)Screen.height - Input.mousePosition.y));
		bool flag2 = flag;
		if (flag2)
		{
			Rect rect = new Rect(Input.mousePosition.x, (float)Screen.height - Input.mousePosition.y, 20f * GraphicsSettings.userInterfaceScale, 20f * GraphicsSettings.userInterfaceScale);
			GUI.DrawTexture(rect, GuiStyles.CursorTextureRef);
		}
	}
	public virtual void DrawWindow()
	{
	}
	public Vector2 WindowPosition = new Vector2(100f, 100f);
}
