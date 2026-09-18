using System;
using System.Collections.Generic;
using SDG.Unturned;
using UnityEngine;
public class GuiAreaState
{
	// (get) Token: 0x06000359 RID: 857 RVA: 0x00039A5C File Offset: 0x00037C5C
	// (set) Token: 0x0600035A RID: 858 RVA: 0x00039A84 File Offset: 0x00037C84
	public static GuiArea currentArea
	{
		get
		{
			return GuiAreaState.AreaStack[GuiAreaState.AreaStack.Count - 1];
		}
		set
		{
			GuiAreaState.AreaStack[GuiAreaState.AreaStack.Count - 1] = value;
		}
	}
	// (get) Token: 0x0600035B RID: 859 RVA: 0x00039AA0 File Offset: 0x00037CA0
	// (set) Token: 0x0600035C RID: 860 RVA: 0x00039AE7 File Offset: 0x00037CE7
	public static ScrollState currentScroll
	{
		get
		{
			return (GuiAreaState.currentArea.subAreas.Count > 0) ? GuiAreaState.currentArea.subAreas[GuiAreaState.currentArea.subAreas.Count - 1] : null;
		}
		set
		{
			GuiAreaState.currentArea.subAreas[GuiAreaState.currentArea.subAreas.Count - 1] = value;
		}
	}
	// (get) Token: 0x0600035D RID: 861 RVA: 0x00039B0C File Offset: 0x00037D0C
	public static bool hasScroll
	{
		get
		{
			return GuiAreaState.currentScroll != null;
		}
	}
	// (get) Token: 0x0600035E RID: 862 RVA: 0x00039B28 File Offset: 0x00037D28
	public static int width
	{
		get
		{
			return GuiAreaState.currentArea.isScrollArea ? ((int)GuiAreaState.currentArea.rect.width / GuiAreaState.currentArea.elementsPerRow - 4) : ((int)GuiAreaState.currentArea.rect.width);
		}
	}
	// (get) Token: 0x0600035F RID: 863 RVA: 0x00039B78 File Offset: 0x00037D78
	public static int padding
	{
		get
		{
			return (GuiAreaState.hasScroll && (!GuiAreaState.currentArea.isScrollArea || GuiAreaState.currentArea.currentColumn == GuiAreaState.currentArea.currentColumn + 1) && GuiAreaState.currentScroll.predictedSize > GuiAreaState.currentScroll.maxSizeY) ? 22 : 0;
		}
	}
	// (get) Token: 0x06000360 RID: 864 RVA: 0x00039BD0 File Offset: 0x00037DD0
	public static int rectX
	{
		get
		{
			return (GuiAreaState.currentArea.isScrollArea ? ((int)GuiAreaState.currentArea.rect.x + (int)GuiAreaState.currentArea.rect.width / GuiAreaState.currentArea.elementsPerRow * GuiAreaState.currentArea.currentColumn + 2) : ((int)GuiAreaState.currentArea.rect.x)) + GuiAreaState.IndentOffset;
		}
	}
	public static void PushArea(Rect rect)
	{
		GuiAreaState.AreaStack.Add(new GuiArea(rect));
	}
	public static void PopArea()
	{
		GuiAreaState.IndentOffset = 0;
		GuiAreaState.AreaStack.RemoveAt(GuiAreaState.AreaStack.Count - 1);
	}
	public static void BeginColumns(int elementsCount, int offset = 20)
	{
		GuiAreaState.currentArea.offset += offset;
		bool flag = !GuiAreaState.IsInsideScrollViewport();
		bool flag2 = flag;
		if (flag2)
		{
			GuiAreaState.currentArea.height -= offset;
		}
		GuiAreaState.currentArea.isScrollArea = true;
		GuiAreaState.currentArea.elementsPerRow = elementsCount;
		GuiAreaState.currentArea.currentColumn = -1;
	}
	public static void EndColumns()
	{
		GuiAreaState.currentArea.isScrollArea = false;
	}
	public static void Label(string text)
	{
		int num = Mathf.CeilToInt(GUI.skin.label.CalcSize(new GUIContent(text)).x / (float)GuiAreaState.width);
		GuiAreaState.currentArea.offset += 18 * num;
		bool flag = GuiAreaState.IsInsideScrollViewport();
		bool flag2 = flag;
		if (flag2)
		{
			GuiAreaState.currentArea.contentType = GuiElementType.Text;
			GUI.Label(new Rect((float)(GuiAreaState.rectX + 2), GuiAreaState.currentArea.rect.y + (float)GuiAreaState.currentArea.offset - (float)(18 * num), (float)(GuiAreaState.width - 4), (float)(14 * num)), text);
		}
		else
		{
			bool flag3 = !GuiAreaState.currentArea.isScrollArea;
			bool flag4 = flag3;
			if (flag4)
			{
				GuiAreaState.currentArea.height -= 18 * num;
			}
		}
	}
	public static void LabelTooltip(string text, string tooltip)
	{
		int num = Mathf.CeilToInt(GUI.skin.label.CalcSize(new GUIContent(text)).x / (float)GuiAreaState.width);
		GuiAreaState.currentArea.offset += 18 * num;
		bool flag = GuiAreaState.IsInsideScrollViewport();
		bool flag2 = flag;
		if (flag2)
		{
			GuiAreaState.currentArea.contentType = GuiElementType.Text;
			Rect rect = new Rect((float)(GuiAreaState.rectX + 2), GuiAreaState.currentArea.rect.y + (float)GuiAreaState.currentArea.offset - (float)(18 * num), (float)(GuiAreaState.width - 4), (float)(14 * num));
			GUI.Label(rect, text);
			bool flag3 = rect.Contains(GuiAreaState.MousePosition);
			bool flag4 = flag3;
			if (flag4)
			{
				GuiAreaState.tooltips.Add(new ValueTuple<Rect, string>(new Rect(Input.mousePosition.x, (float)Screen.height - Input.mousePosition.y + 20f * GraphicsSettings.userInterfaceScale, 600f, 30f), tooltip));
			}
		}
		else
		{
			bool flag5 = !GuiAreaState.currentArea.isScrollArea;
			bool flag6 = flag5;
			if (flag6)
			{
				GuiAreaState.currentArea.height -= 18 * num;
			}
		}
	}
	public static bool Button(string text)
	{
		GuiAreaState.currentArea.offset += (int)GUI.skin.button.fixedHeight + 4;
		bool flag = GuiAreaState.IsInsideScrollViewport();
		bool flag2 = flag;
		bool flag3;
		if (flag2)
		{
			GuiAreaState.currentArea.contentType = GuiElementType.Button;
			flag3 = GUI.Button(new Rect((float)GuiAreaState.rectX, GuiAreaState.currentArea.rect.y + (float)GuiAreaState.currentArea.offset - GUI.skin.button.fixedHeight - 4f, (float)(GuiAreaState.width - GuiAreaState.padding), GUI.skin.button.fixedHeight), text);
		}
		else
		{
			bool flag4 = !GuiAreaState.currentArea.isScrollArea;
			bool flag5 = flag4;
			if (flag5)
			{
				GuiAreaState.currentArea.height -= (int)GUI.skin.button.fixedHeight + 4;
			}
			flag3 = false;
		}
		return flag3;
	}
	public static bool ButtonTooltip(string text, string tooltip)
	{
		GuiAreaState.currentArea.offset += (int)GUI.skin.button.fixedHeight + 4;
		bool flag = GuiAreaState.IsInsideScrollViewport();
		bool flag2 = flag;
		bool flag5;
		if (flag2)
		{
			GuiAreaState.currentArea.contentType = GuiElementType.Button;
			Rect rect = new Rect((float)GuiAreaState.rectX, GuiAreaState.currentArea.rect.y + (float)GuiAreaState.currentArea.offset - GUI.skin.button.fixedHeight - 4f, (float)(GuiAreaState.width - GuiAreaState.padding), GUI.skin.button.fixedHeight);
			bool flag3 = rect.Contains(GuiAreaState.MousePosition);
			bool flag4 = flag3;
			if (flag4)
			{
				GuiAreaState.tooltips.Add(new ValueTuple<Rect, string>(new Rect(Input.mousePosition.x, (float)Screen.height - Input.mousePosition.y + 20f * GraphicsSettings.userInterfaceScale, 600f, 30f), tooltip));
			}
			flag5 = GUI.Button(rect, text);
		}
		else
		{
			bool flag6 = !GuiAreaState.currentArea.isScrollArea;
			bool flag7 = flag6;
			if (flag7)
			{
				GuiAreaState.currentArea.height -= (int)GUI.skin.button.fixedHeight + 4;
			}
			flag5 = false;
		}
		return flag5;
	}
	public static bool StyledButton(string text, GUIStyle style)
	{
		GuiAreaState.currentArea.offset += (int)style.fixedHeight + 8;
		bool flag = GuiAreaState.IsInsideScrollViewport();
		bool flag2 = flag;
		bool flag3;
		if (flag2)
		{
			flag3 = GUI.Button(new Rect((float)GuiAreaState.rectX, GuiAreaState.currentArea.rect.y + (float)GuiAreaState.currentArea.offset - GUI.skin.button.fixedHeight - 8f, (float)(GuiAreaState.width - GuiAreaState.padding), GUI.skin.button.fixedHeight), text, style);
		}
		else
		{
			bool flag4 = !GuiAreaState.currentArea.isScrollArea;
			bool flag5 = flag4;
			if (flag5)
			{
				GuiAreaState.currentArea.height -= (int)style.fixedHeight + 8;
			}
			flag3 = false;
		}
		return flag3;
	}
	public static void Space(int space)
	{
		GuiAreaState.currentArea.offset += space;
		bool flag = !GuiAreaState.IsInsideScrollViewport();
		bool flag2 = flag;
		if (flag2)
		{
			GuiAreaState.currentArea.height -= space;
		}
	}
	public static void AddIndent(int space)
	{
		GuiAreaState.IndentOffset += space;
	}
	public static bool Toggle(bool variable, string text)
	{
		GuiAreaState.currentArea.offset += (int)GUI.skin.toggle.fixedHeight + 4;
		bool flag = GuiAreaState.IsInsideScrollViewport();
		bool flag2 = flag;
		bool flag5;
		if (flag2)
		{
			bool flag3 = GuiAreaState.currentArea.contentType == GuiElementType.Button;
			bool flag4 = flag3;
			if (flag4)
			{
				GuiAreaState.Space(4);
			}
			GuiAreaState.currentArea.contentType = GuiElementType.Toggle;
			Rect rect = new Rect((float)GuiAreaState.rectX, GuiAreaState.currentArea.rect.y + (float)GuiAreaState.currentArea.offset - GUI.skin.toggle.fixedHeight - 4f, (float)(GuiAreaState.width - GuiAreaState.padding), GUI.skin.toggle.fixedHeight);
			flag5 = GUI.Toggle(rect, variable, text);
		}
		else
		{
			bool flag6 = !GuiAreaState.currentArea.isScrollArea;
			bool flag7 = flag6;
			if (flag7)
			{
				GuiAreaState.currentArea.height -= (int)GUI.skin.toggle.fixedHeight + 4;
			}
			flag5 = variable;
		}
		return flag5;
	}
	public static bool StyledToggle(bool variable, string text, GUIStyle style)
	{
		GuiAreaState.currentArea.offset += (int)GUI.skin.toggle.fixedHeight + 4;
		bool flag = GuiAreaState.IsInsideScrollViewport();
		bool flag2 = flag;
		bool flag5;
		if (flag2)
		{
			bool flag3 = GuiAreaState.currentArea.contentType == GuiElementType.Button;
			bool flag4 = flag3;
			if (flag4)
			{
				GuiAreaState.Space(4);
			}
			GuiAreaState.currentArea.contentType = GuiElementType.Toggle;
			Rect rect = new Rect((float)GuiAreaState.rectX, GuiAreaState.currentArea.rect.y + (float)GuiAreaState.currentArea.offset - GUI.skin.toggle.fixedHeight - 4f, (float)(GuiAreaState.width - GuiAreaState.padding), GUI.skin.toggle.fixedHeight);
			flag5 = GUI.Toggle(rect, variable, text, style);
		}
		else
		{
			bool flag6 = !GuiAreaState.currentArea.isScrollArea;
			bool flag7 = flag6;
			if (flag7)
			{
				GuiAreaState.currentArea.height -= (int)GUI.skin.toggle.fixedHeight + 4;
			}
			flag5 = variable;
		}
		return flag5;
	}
	public static bool ToggleTooltip(bool variable, string text, string tooltip)
	{
		GuiAreaState.currentArea.offset += (int)GUI.skin.toggle.fixedHeight + 4;
		bool flag = GuiAreaState.IsInsideScrollViewport();
		bool flag2 = flag;
		bool flag7;
		if (flag2)
		{
			bool flag3 = GuiAreaState.currentArea.contentType == GuiElementType.Button;
			bool flag4 = flag3;
			if (flag4)
			{
				GuiAreaState.Space(4);
			}
			GuiAreaState.currentArea.contentType = GuiElementType.Toggle;
			Rect rect = new Rect((float)GuiAreaState.rectX, GuiAreaState.currentArea.rect.y + (float)GuiAreaState.currentArea.offset - GUI.skin.toggle.fixedHeight - 4f, (float)(GuiAreaState.width - GuiAreaState.padding), GUI.skin.toggle.fixedHeight);
			bool flag5 = rect.Contains(GuiAreaState.MousePosition);
			bool flag6 = flag5;
			if (flag6)
			{
				GuiAreaState.tooltips.Add(new ValueTuple<Rect, string>(new Rect(Input.mousePosition.x, (float)Screen.height - Input.mousePosition.y + 20f * GraphicsSettings.userInterfaceScale, 600f, 30f), tooltip));
			}
			flag7 = GUI.Toggle(rect, variable, text);
		}
		else
		{
			bool flag8 = !GuiAreaState.currentArea.isScrollArea;
			bool flag9 = flag8;
			if (flag9)
			{
				GuiAreaState.currentArea.height -= (int)GUI.skin.toggle.fixedHeight + 4;
			}
			flag7 = variable;
		}
		return flag7;
	}
	public static void DrawTexture(Texture2D texture, int width = -1, int height = 100)
	{
		GuiAreaState.currentArea.offset += height + 4;
		bool flag = GuiAreaState.IsInsideScrollViewport();
		bool flag2 = flag;
		if (flag2)
		{
			GuiAreaState.currentArea.contentType = GuiElementType.Box;
			bool flag3 = texture != null;
			bool flag4 = flag3;
			if (flag4)
			{
				GUI.DrawTexture(new Rect((float)GuiAreaState.rectX, GuiAreaState.currentArea.rect.y + (float)GuiAreaState.currentArea.offset - (float)(height + 4), (float)((width == -1) ? GuiAreaState.width : width), (float)height), texture, ScaleMode.ScaleToFit);
			}
		}
		else
		{
			bool flag5 = !GuiAreaState.currentArea.isScrollArea;
			bool flag6 = flag5;
			if (flag6)
			{
				GuiAreaState.currentArea.height -= height + 4;
			}
		}
	}
	public static void Box(string text, int height = 14)
	{
		GuiAreaState.currentArea.offset += height + 4;
		bool flag = GuiAreaState.IsInsideScrollViewport();
		bool flag2 = flag;
		if (flag2)
		{
			GuiAreaState.currentArea.contentType = GuiElementType.Box;
			GUI.Box(new Rect((float)GuiAreaState.rectX, GuiAreaState.currentArea.rect.y + (float)GuiAreaState.currentArea.offset - (float)(height + 4), (float)GuiAreaState.width, (float)height), text, GUI.skin.customStyles[0]);
		}
		else
		{
			bool flag3 = !GuiAreaState.currentArea.isScrollArea;
			bool flag4 = flag3;
			if (flag4)
			{
				GuiAreaState.currentArea.height -= height + 4;
			}
		}
	}
	public static float Slider(float value, float minValue, float maxValue)
	{
		GuiAreaState.currentArea.offset += (int)GUI.skin.horizontalSlider.fixedHeight + 4;
		bool flag = GuiAreaState.IsInsideScrollViewport();
		bool flag2 = flag;
		float num;
		if (flag2)
		{
			GuiAreaState.currentArea.contentType = GuiElementType.InputField;
			num = GUI.HorizontalSlider(new Rect((float)GuiAreaState.rectX, GuiAreaState.currentArea.rect.y + (float)GuiAreaState.currentArea.offset - GUI.skin.horizontalSlider.fixedHeight - 4f, (float)(GuiAreaState.width - GuiAreaState.padding - GuiAreaState.IndentOffset), GUI.skin.horizontalSlider.fixedHeight), value, minValue, maxValue);
		}
		else
		{
			bool flag3 = !GuiAreaState.currentArea.isScrollArea;
			bool flag4 = flag3;
			if (flag4)
			{
				GuiAreaState.currentArea.height -= (int)GUI.skin.horizontalSlider.fixedHeight + 4;
			}
			num = value;
		}
		return num;
	}
	public static bool IsInsideScrollViewport()
	{
		return GuiAreaState.currentScroll == null || (GuiAreaState.currentScroll.startY <= GuiAreaState.currentScroll.scrollPosition + GuiAreaState.currentScroll.maxSizeY && GuiAreaState.currentScroll.scrollPosition < GuiAreaState.currentScroll.startY - GuiAreaState.currentScroll.endY / 2 - 1);
	}
	public static string TextField(string text)
	{
		GuiAreaState.currentArea.offset += (int)GUI.skin.textField.fixedHeight + 4;
		bool flag = GuiAreaState.IsInsideScrollViewport();
		bool flag2 = flag;
		string text2;
		if (flag2)
		{
			GuiAreaState.currentArea.contentType = GuiElementType.InputField;
			text2 = GUI.TextField(new Rect((float)GuiAreaState.rectX, GuiAreaState.currentArea.rect.y + (float)GuiAreaState.currentArea.offset - GUI.skin.textField.fixedHeight - 4f, (float)(GuiAreaState.width - GuiAreaState.padding), GUI.skin.textField.fixedHeight), text);
		}
		else
		{
			bool flag3 = !GuiAreaState.currentArea.isScrollArea;
			bool flag4 = flag3;
			if (flag4)
			{
				GuiAreaState.currentArea.height -= (int)GUI.skin.textField.fixedHeight + 4;
			}
			text2 = text;
		}
		return text2;
	}
	public static float FloatField(float val)
	{
		bool flag = val % 1f != 0f;
		return float.Parse(GuiAreaState.TextField(val.ToString() + (flag ? "" : ",0")));
	}
	public static int IntField(int val)
	{
		return int.Parse(GuiAreaState.TextField(val.ToString()));
	}
	public static int IntSlider(Rect rect, int value, int minValue, int maxValue)
	{
		return GuiAreaState.RawSlider(rect, rect, value, minValue, maxValue);
	}
	public static int RawSlider(Rect rect, Rect scrollViewport, int value, int minValue, int maxValue)
	{
		int num = value;
		MenuGuiHelper.DrawRect(rect, new Color32(50, 50, 52, byte.MaxValue), true, ScaleMode.StretchToFill);
		int num2 = (int)((float)num * ((rect.height - 44f) / (float)(maxValue - minValue)));
		Rect rect2 = new Rect(rect.x + 2f, rect.y + 2f + (float)num2, rect.width - 4f, 40f);
		MenuGuiHelper.DrawRect(rect2, ColorConfig.GetColor("Menu lines color"), true, ScaleMode.StretchToFill);
		bool flag = !Input.GetMouseButton(0) || Input.GetMouseButtonUp(0);
		bool flag2 = flag;
		if (flag2)
		{
			GuiAreaState.IsSliderDragging = false;
		}
		bool flag3 = Input.GetMouseButtonDown(0) && rect2.Contains(GuiAreaState.MousePosition);
		bool flag4 = flag3;
		if (flag4)
		{
			GuiAreaState.IsSliderDragging = true;
			GuiAreaState.ActiveSliderRect = rect;
		}
		bool flag5 = GuiAreaState.IsSliderDragging && rect == GuiAreaState.ActiveSliderRect;
		bool flag6 = flag5;
		if (flag6)
		{
			float num3 = GuiAreaState.MousePosition.y - ((float)Screen.height - Input.mousePosition.y);
			float num4 = rect.height - rect2.height - 4f;
			float num5 = (float)(maxValue - minValue);
			float num6 = num3 / num4;
			float num7 = num6 * num5;
			num -= Mathf.RoundToInt(num7);
		}
		bool flag7 = Input.mouseScrollDelta.y != 0f && scrollViewport.Contains(GuiAreaState.MousePosition);
		bool flag8 = flag7;
		if (flag8)
		{
			num -= (int)(Input.mouseScrollDelta.y * 8f);
		}
		return Mathf.Clamp(num, minValue, maxValue);
	}
	public static void BeginScrollView(Vector2 scrollPosition)
	{
		GuiAreaState.currentArea.subAreas.Add(new ScrollState((int)scrollPosition.y, (int)GuiAreaState.currentArea.rect.height - GuiAreaState.currentArea.offset, GuiAreaState.currentArea.offset, (int)scrollPosition.x));
	}
	public static Vector2 EndScrollView()
	{
		int num = ((GuiAreaState.currentScroll.startY > GuiAreaState.currentScroll.maxSizeY) ? GuiAreaState.RawSlider(new Rect((float)GuiAreaState.rectX + GuiAreaState.currentArea.rect.width - 16f, GuiAreaState.currentArea.rect.y + (float)GuiAreaState.currentScroll.startedOffset, 16f, (float)GuiAreaState.currentScroll.maxSizeY), new Rect((float)GuiAreaState.rectX, GuiAreaState.currentArea.rect.y + (float)GuiAreaState.currentScroll.startedOffset, GuiAreaState.currentArea.rect.width, (float)GuiAreaState.currentScroll.maxSizeY), GuiAreaState.currentScroll.scrollPosition, 0, GuiAreaState.currentScroll.startY - GuiAreaState.currentScroll.maxSizeY) : 0);
		int d3sS6k0EkNDcAlJMaCKxD68b = GuiAreaState.currentScroll.startY;
		GuiAreaState.currentArea.subAreas.RemoveAt(GuiAreaState.currentArea.subAreas.Count - 1);
		return new Vector2((float)d3sS6k0EkNDcAlJMaCKxD68b, (float)num);
	}
	public static void Update()
	{
		int depth = GUI.depth;
		GUI.depth = -1000;
		foreach (ValueTuple<Rect, string> valueTuple in GuiAreaState.tooltips)
		{
			GUI.Label(valueTuple.Item1, valueTuple.Item2);
		}
		GUI.depth = depth;
		GuiAreaState.tooltips.Clear();
		GuiAreaState.MousePosition = new Vector2(Input.mousePosition.x, (float)Screen.height - Input.mousePosition.y);
	}
	public static List<GuiArea> AreaStack = new List<GuiArea>();
	public static List<ValueTuple<Rect, string>> tooltips = new List<ValueTuple<Rect, string>>();
	public static bool IsSliderDragging = false;
	public static Rect ActiveSliderRect;
	public static Vector2 MousePosition;
	public static int IndentOffset = 0;
}
