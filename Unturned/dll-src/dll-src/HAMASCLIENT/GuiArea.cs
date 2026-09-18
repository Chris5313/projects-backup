using System;
using System.Collections.Generic;
using UnityEngine;
public class GuiArea
{
	// (get) Token: 0x060000C9 RID: 201 RVA: 0x00009988 File Offset: 0x00007B88
	// (set) Token: 0x060000CA RID: 202 RVA: 0x000099A0 File Offset: 0x00007BA0
	public int offset
	{
		get
		{
			return this.height;
		}
		set
		{
			bool flag = GuiAreaState.currentScroll != null && !this.isScrollArea;
			bool flag2 = flag;
			if (flag2)
			{
				GuiAreaState.currentScroll.startY += value - this.height;
				GuiAreaState.currentScroll.endY = value - this.height;
			}
			else
			{
				bool dhMRJNZeRavE6LVktl4SUercR = this.isScrollArea;
				bool flag3 = dhMRJNZeRavE6LVktl4SUercR;
				if (flag3)
				{
					this.currentColumn++;
				}
			}
			bool flag4 = !this.isScrollArea;
			bool flag5 = flag4;
			if (flag5)
			{
				this.height = value;
			}
		}
	}
	public GuiArea(Rect rect)
	{
		this.rect = rect;
		this.height = 0;
		this.isScrollArea = false;
		this.subAreas = new List<ScrollState>();
	}
	public Rect rect;
	public int elementsPerRow = 0;
	public int currentColumn = 0;
	public int height;
	public List<ScrollState> subAreas = new List<ScrollState>();
	public GuiElementType contentType = GuiElementType.Text;
	public bool isScrollArea;
}
