using System;
using System.Runtime.CompilerServices;
using UnityEngine;
public class FeatureTabBase
{
	public void DrawTabHeader(int index)
	{
		Rect rect = new Rect(MenuState.menuRect.x + 10f, MenuState.menuRect.y + 85f + (float)(index * 20), 120f, 20f);
		bool flag = Input.GetMouseButtonDown(0) && rect.Contains(Event.current.mousePosition);
		bool flag2 = flag;
		if (flag2)
		{
			MenuState.activePopupOwner = null;
			MenuState.pendingTabSwitch = this;
		}
		bool flag3 = Event.current.type != EventType.Repaint;
		bool flag4 = !flag3;
		if (flag4)
		{
			bool flag5 = this.activeAnim > 0f;
			bool flag6 = flag5;
			if (flag6)
			{
				this.hoverAnim = 1f;
			}
			else
			{
				bool flag7 = rect.Contains(Event.current.mousePosition);
				bool flag8 = flag7;
				if (flag8)
				{
					this.hoverAnim += Time.deltaTime * 4f;
				}
				else
				{
					this.hoverAnim -= Time.deltaTime * 8f;
				}
			}
			this.hoverAnim = Mathf.Clamp(this.hoverAnim, 0f, 1f);
			bool flag9 = MenuState.currentTab == this;
			bool flag10 = flag9;
			if (flag10)
			{
				this.activeAnim += Time.deltaTime * 3.6f;
			}
			else
			{
				this.activeAnim -= Time.deltaTime * 5f;
			}
			this.activeAnim = Mathf.Clamp(this.activeAnim, 0f, 1f);
		}
		float num = Mathf.Max(this.hoverAnim, this.activeAnim);
		bool flag11 = num > 0f;
		if (flag11)
		{
			Color32 color = new Color32(24, 24, 28, (byte)(num * 255f));
			MenuGuiHelper.DrawRect(rect, color, false, ScaleMode.StretchToFill);
		}
		bool flag12 = this.activeAnim > 0f;
		if (flag12)
		{
			Color32 accentColor = MenuGuiHelper.GetAccentColor((byte)(this.activeAnim * 255f));
			MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y, 3f, rect.height), accentColor, false, ScaleMode.StretchToFill);
		}
		else
		{
			bool flag13 = this.hoverAnim > 0f;
			if (flag13)
			{
				Color32 accentColor2 = MenuGuiHelper.GetAccentColor((byte)(this.hoverAnim * 100f));
				MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y, 3f, rect.height), accentColor2, false, ScaleMode.StretchToFill);
			}
		}
		GUIStyle dtwXQKSBGkPc5vLHkpnmJXyJ = GuiStyles.SmallBoldGrayLabelStyle;
		Color32 color2 = dtwXQKSBGkPc5vLHkpnmJXyJ.normal.textColor;
		bool flag14 = this.activeAnim > 0.5f;
		if (flag14)
		{
			dtwXQKSBGkPc5vLHkpnmJXyJ.normal.textColor = MenuGuiHelper.GetAccentColor(byte.MaxValue);
		}
		else
		{
			bool flag15 = this.hoverAnim > 0.5f;
			if (flag15)
			{
				dtwXQKSBGkPc5vLHkpnmJXyJ.normal.textColor = new Color32(180, 180, 180, byte.MaxValue);
			}
		}
		GUI.Label(new Rect(rect.x + 30f, rect.y, rect.width - 30f, rect.height), this.GetName(), dtwXQKSBGkPc5vLHkpnmJXyJ);
		dtwXQKSBGkPc5vLHkpnmJXyJ.normal.textColor = color2;
		GUILayout.Space(6f);
	}
	public virtual string GetName()
	{
		return "Null";
	}
	public virtual int SortId()
	{
		return -1;
	}
	public virtual void DoTab(TabCount tc)
	{
	}
	public virtual TabCount GetTabCounts()
	{
		return TabCount.Two;
	}
	public void DrawSectionHeader(string text)
	{
		Rect rect = GUILayoutUtility.GetRect(-1f, 18f);
		GUI.Label(new Rect(rect.x + 20f, rect.y, rect.width - 20f, rect.height), text, GuiStyles.SmallBoldWhiteLabelStyle);
	}
	[CompilerGenerated]
	private Color32 TabHoverBackgroundColor(byte r, byte g, byte b)
	{
		return new Color32(r, g, b, (byte)(255f * (this.hoverAnim / 2f + this.activeAnim / 2f)));
	}
	public float hoverAnim = 0f;
	public float activeAnim = 0f;
}
