using System;
using SDG.Unturned;
using UnityEngine;
public static class EspDrawer
{
	public static void UpdateCameraMatrices()
	{
		EspDrawer.RenderCamera = (MiscConfig.freeCameraBacking ? FreeCamera.FreeCameraComponent : ((MainCamera.instance != null) ? MainCamera.instance : Camera.current));
		bool flag = EspDrawer.RenderCamera != null;
		bool flag2 = flag;
		if (flag2)
		{
			EspDrawer.WorldToCameraMatrix = EspDrawer.RenderCamera.worldToCameraMatrix;
			EspDrawer.ProjectionMatrix = EspDrawer.RenderCamera.projectionMatrix;
		}
	}
	public static bool IsVisibleInRange(EspCategory category, GameObject gameObject)
	{
		return EspDrawer.IsWithinDistance(category, gameObject.transform.position) && gameObject.transform.position.IsOnScreen();
	}
	public static bool IsWithinRange(EspCategory category, GameObject gameObject)
	{
		return EspDrawer.IsWithinDistance(category, gameObject.transform.position);
	}
	public static bool IsWithinDistance(EspCategory category, Vector3 position)
	{
		return !category.SettingFlag12 || (Player.player.transform.position - position).sqrMagnitude <= (float)(category.MaxLineCount * category.MaxLineCount);
	}
	public static void DrawDistanceLabel(Vector3 worldPoint, string text, Color cl, int fontSize = 12)
	{
		text = string.Format(text, (int)MathUtil.Distance(Player.player.transform.position, worldPoint));
		Vector3 vector = worldPoint.WorldToScreenPoint();
		Rect rect = new Rect(vector.x - 160f, vector.y - 40f, 320f, 80f);
		GuiStyles.CenterLabelStyle.fontSize = fontSize;
		GuiStyles.CenterLabelStyle.normal.textColor = cl;
		GUI.Label(rect, text, GuiStyles.CenterLabelStyle);
	}
	public static void DrawCategoryTexts(EspCategory category, Vector3 worldPoint, StringRefDelegate ftd, Color c, Color oc)
	{
		foreach (EspTextEntry drtz0MPdBhZh1V5PmbtgwG0pX in category.LineEntries)
		{
			bool flag = !drtz0MPdBhZh1V5PmbtgwG0pX.drawEnabled;
			bool flag2 = !flag;
			if (flag2)
			{
				string text = drtz0MPdBhZh1V5PmbtgwG0pX.formatText;
				bool flag3 = ftd != null;
				bool flag4 = flag3;
				if (flag4)
				{
					try
					{
						ftd(ref text);
					}
					catch
					{
					}
				}
				int num = (int)MathUtil.Distance(Player.player.transform.position, worldPoint);
				text = string.Format(text, num);
				bool d9N2d7gO9JnbsDwXr0pIcoOOf = drtz0MPdBhZh1V5PmbtgwG0pX.formatNewlines;
				bool flag5 = d9N2d7gO9JnbsDwXr0pIcoOOf;
				if (flag5)
				{
					text = text.Replace("\\n", Environment.NewLine);
				}
				bool flag6 = drtz0MPdBhZh1V5PmbtgwG0pX.textCase == TextCase.UpperCase;
				bool flag7 = flag6;
				if (flag7)
				{
					text = text.ToUpper();
				}
				else
				{
					bool flag8 = drtz0MPdBhZh1V5PmbtgwG0pX.textCase == TextCase.LowerCase;
					bool flag9 = flag8;
					if (flag9)
					{
						text = text.ToLower();
					}
				}
				worldPoint += drtz0MPdBhZh1V5PmbtgwG0pX.worldOffset;
				Vector3 vector = worldPoint.WorldToScreenPoint();
				worldPoint -= drtz0MPdBhZh1V5PmbtgwG0pX.worldOffset;
				bool flag10 = !drtz0MPdBhZh1V5PmbtgwG0pX.scaleByDistance;
				bool flag11 = flag10;
				if (flag11)
				{
					category.TextStyle.fontSize = drtz0MPdBhZh1V5PmbtgwG0pX.fontSize;
				}
				else
				{
					category.TextStyle.fontSize = drtz0MPdBhZh1V5PmbtgwG0pX.maxFontSize - (int)((float)(drtz0MPdBhZh1V5PmbtgwG0pX.maxFontSize - drtz0MPdBhZh1V5PmbtgwG0pX.minFontSize) * Mathf.Clamp((float)(num - drtz0MPdBhZh1V5PmbtgwG0pX.minScaleDistance) / (float)drtz0MPdBhZh1V5PmbtgwG0pX.maxScaleDistance, 0f, 1f));
				}
				category.TextShadowStyle.fontSize = category.TextStyle.fontSize;
				category.TextShadowStyle.normal.textColor = oc;
				category.TextStyle.normal.textColor = (drtz0MPdBhZh1V5PmbtgwG0pX.useGlobalColor ? c : (drtz0MPdBhZh1V5PmbtgwG0pX.textColor.isGradient ? c : drtz0MPdBhZh1V5PmbtgwG0pX.textColor.Color));
				Rect rect = new Rect(vector.x - 160f, vector.y - 40f, 320f, 80f);
				rect.x += drtz0MPdBhZh1V5PmbtgwG0pX.screenOffset.x;
				rect.y -= drtz0MPdBhZh1V5PmbtgwG0pX.screenOffset.y;
				GuiStyles.DarkCenterLabelStyle.fontSize = drtz0MPdBhZh1V5PmbtgwG0pX.fontSize;
				bool flag12 = drtz0MPdBhZh1V5PmbtgwG0pX.outlineMode > TextOutlineStyle.None;
				bool flag13 = flag12;
				if (flag13)
				{
					EspDrawer.DrawOutlinedText(rect, text, drtz0MPdBhZh1V5PmbtgwG0pX.outlineMode, category.TextShadowStyle, drtz0MPdBhZh1V5PmbtgwG0pX.outlineThickness);
				}
				GUI.Label(rect, text, category.TextStyle);
			}
		}
	}
	public static void DrawTextEntry(EspCategory category, EspTextEntry txto, string formattedText, Vector3 worldPoint, Color c, Color oc)
	{
		if (!txto.drawEnabled) return;
			int num = (int)MathUtil.Distance(Player.player.transform.position, worldPoint);
			string text = string.Format(formattedText, num);
			bool d9N2d7gO9JnbsDwXr0pIcoOOf = txto.formatNewlines;
			bool flag3 = d9N2d7gO9JnbsDwXr0pIcoOOf;
			if (flag3)
			{
				text = text.Replace("\\n", Environment.NewLine);
			}
			bool flag4 = txto.textCase == TextCase.UpperCase;
			bool flag5 = flag4;
			if (flag5)
			{
				text = text.ToUpper();
			}
			else
			{
				bool flag6 = txto.textCase == TextCase.LowerCase;
				bool flag7 = flag6;
				if (flag7)
				{
					text = text.ToLower();
				}
			}
			worldPoint += txto.worldOffset;
			Vector3 vector = worldPoint.WorldToScreenPoint();
			worldPoint -= txto.worldOffset;
			bool flag8 = !txto.scaleByDistance;
			bool flag9 = flag8;
			if (flag9)
			{
				category.TextStyle.fontSize = txto.fontSize;
			}
			else
			{
				category.TextStyle.fontSize = txto.maxFontSize - (int)((float)(txto.maxFontSize - txto.minFontSize) * Mathf.Clamp((float)(num - txto.minScaleDistance) / (float)txto.maxScaleDistance, 0f, 1f));
			}
			category.TextShadowStyle.fontSize = category.TextStyle.fontSize;
			category.TextShadowStyle.normal.textColor = oc;
			category.TextStyle.normal.textColor = (txto.useGlobalColor ? c : (txto.textColor.isGradient ? c : txto.textColor.Color));
			Rect rect = new Rect(vector.x - 160f, vector.y - 40f, 320f, 80f);
			rect.x += txto.screenOffset.x;
			rect.y -= txto.screenOffset.y;
			GuiStyles.DarkCenterLabelStyle.fontSize = txto.fontSize;
			bool flag10 = txto.outlineMode > TextOutlineStyle.None;
			bool flag11 = flag10;
			if (flag11)
			{
				EspDrawer.DrawOutlinedText(rect, text, txto.outlineMode, category.TextShadowStyle, txto.outlineThickness);
			}
			GUI.Label(rect, text, category.TextStyle);
	}	public static void DrawLine(Vector2 pointA, Vector2 pointB, Color color, float width = 1f)
	{
		bool flag = float.IsNaN(pointA.x) || float.IsNaN(pointA.y) || float.IsNaN(pointB.x) || float.IsNaN(pointB.y);
		if (!flag)
		{
			float num = 50f;
			float num2 = -num;
			float num3 = (float)Screen.width + num;
			float num4 = -num;
			float num5 = (float)Screen.height + num;
			float num6 = pointB.x - pointA.x;
			float num7 = pointB.y - pointA.y;
			float num8 = 0f;
			float num9 = 1f;
			float[] array = new float[]
			{
				-num6,
				num6,
				-num7,
				num7
			};
			float[] array2 = new float[]
			{
				pointA.x - num2,
				num3 - pointA.x,
				pointA.y - num4,
				num5 - pointA.y
			};
			bool flag2 = false;
			for (int i = 0; i < 4; i++)
			{
				bool flag3 = array[i] == 0f;
				if (flag3)
				{
					bool flag4 = array2[i] < 0f;
					if (flag4)
					{
						flag2 = true;
						break;
					}
				}
				else
				{
					float num10 = array2[i] / array[i];
					bool flag5 = array[i] < 0f;
					if (flag5)
					{
						bool flag6 = num10 > num9;
						if (flag6)
						{
							flag2 = true;
							break;
						}
						bool flag7 = num10 > num8;
						if (flag7)
						{
							num8 = num10;
						}
					}
					else
					{
						bool flag8 = num10 < num8;
						if (flag8)
						{
							flag2 = true;
							break;
						}
						bool flag9 = num10 < num9;
						if (flag9)
						{
							num9 = num10;
						}
					}
				}
			}
			bool flag10 = flag2;
			if (!flag10)
			{
				Vector2 vector = new Vector2(pointA.x + num8 * num6, pointA.y + num8 * num7);
				Vector2 vector2 = new Vector2(pointA.x + num9 * num6, pointA.y + num9 * num7);
				Matrix4x4 matrix = GUI.matrix;
				try
				{
					EspDrawer.SavedGuiMatrix = matrix;
					EspDrawer.LineRotationAngle = Vector3.Angle(vector2 - vector, Vector2.right);
					bool flag11 = vector.y > vector2.y;
					bool flag12 = flag11;
					if (flag12)
					{
						EspDrawer.LineRotationAngle = -EspDrawer.LineRotationAngle;
					}
					GUIUtility.ScaleAroundPivot(new Vector2((vector2 - vector).magnitude, width), new Vector2(vector.x, vector.y + 0.5f));
					GUIUtility.RotateAroundPivot(EspDrawer.LineRotationAngle, vector);
					GUI.DrawTexture(new Rect(vector.x, vector.y, 1f, 1f), GuiStyles.WhiteTexture, ScaleMode.StretchToFill, false, 0f, color, 0f, 0f);
				}
				finally
				{
					GUI.matrix = matrix;
				}
			}
		}
	}
	public static Rect GetBoundsScreenRect(Bounds bounds)
	{
		Vector3[] dkiMO3NaucExIbgyGlg41oDzH = EspDrawer.BoundsCornerPoints;
		dkiMO3NaucExIbgyGlg41oDzH[0] = new Vector3(bounds.center.x + bounds.extents.x, bounds.center.y + bounds.extents.y, bounds.center.z + bounds.extents.z).WorldToScreenPoint();
		dkiMO3NaucExIbgyGlg41oDzH[1] = new Vector3(bounds.center.x + bounds.extents.x, bounds.center.y + bounds.extents.y, bounds.center.z - bounds.extents.z).WorldToScreenPoint();
		dkiMO3NaucExIbgyGlg41oDzH[2] = new Vector3(bounds.center.x + bounds.extents.x, bounds.center.y - bounds.extents.y, bounds.center.z + bounds.extents.z).WorldToScreenPoint();
		dkiMO3NaucExIbgyGlg41oDzH[3] = new Vector3(bounds.center.x + bounds.extents.x, bounds.center.y - bounds.extents.y, bounds.center.z - bounds.extents.z).WorldToScreenPoint();
		dkiMO3NaucExIbgyGlg41oDzH[4] = new Vector3(bounds.center.x - bounds.extents.x, bounds.center.y + bounds.extents.y, bounds.center.z + bounds.extents.z).WorldToScreenPoint();
		dkiMO3NaucExIbgyGlg41oDzH[5] = new Vector3(bounds.center.x - bounds.extents.x, bounds.center.y + bounds.extents.y, bounds.center.z - bounds.extents.z).WorldToScreenPoint();
		dkiMO3NaucExIbgyGlg41oDzH[6] = new Vector3(bounds.center.x - bounds.extents.x, bounds.center.y - bounds.extents.y, bounds.center.z + bounds.extents.z).WorldToScreenPoint();
		dkiMO3NaucExIbgyGlg41oDzH[7] = new Vector3(bounds.center.x - bounds.extents.x, bounds.center.y - bounds.extents.y, bounds.center.z - bounds.extents.z).WorldToScreenPoint();
		bool flag = false;
		bool flag2 = false;
		Vector3 vector = Vector3.zero;
		Vector3 vector2 = Vector3.zero;
		for (int i = 0; i < dkiMO3NaucExIbgyGlg41oDzH.Length; i++)
		{
			bool flag3 = float.IsNaN(dkiMO3NaucExIbgyGlg41oDzH[i].x) || float.IsNaN(dkiMO3NaucExIbgyGlg41oDzH[i].y);
			if (flag3)
			{
				flag2 = true;
			}
			else
			{
				bool flag4 = !flag;
				if (flag4)
				{
					vector = dkiMO3NaucExIbgyGlg41oDzH[i];
					vector2 = dkiMO3NaucExIbgyGlg41oDzH[i];
					flag = true;
				}
				else
				{
					vector = Vector3.Min(vector, dkiMO3NaucExIbgyGlg41oDzH[i]);
					vector2 = Vector3.Max(vector2, dkiMO3NaucExIbgyGlg41oDzH[i]);
				}
			}
		}
		bool flag5 = !flag;
		Rect rect;
		if (flag5)
		{
			rect = new Rect(0f, 0f, 0f, 0f);
		}
		else
		{
			float num = vector2.x - vector.x;
			float num2 = vector2.y - vector.y;
			bool flag6 = num < 10f;
			if (flag6)
			{
				float num3 = (vector2.x + vector.x) / 2f;
				vector.x = num3 - 5f;
				vector2.x = num3 + 5f;
			}
			bool flag7 = num2 < 5f;
			if (flag7)
			{
				float num4 = (vector2.y + vector.y) / 2f;
				vector.y = num4 - 2.5f;
				vector2.y = num4 + 2.5f;
			}
			bool flag8 = flag2;
			if (flag8)
			{
				vector.x = Mathf.Clamp(vector.x, 0f, (float)Screen.width);
				vector.y = Mathf.Clamp(vector.y, 0f, (float)Screen.height);
				vector2.x = Mathf.Clamp(vector2.x, 0f, (float)Screen.width);
				vector2.y = Mathf.Clamp(vector2.y, 0f, (float)Screen.height);
			}
			else
			{
				vector.x = Mathf.Clamp(vector.x, (float)(-(float)Screen.width), (float)Screen.width * 2f);
				vector.y = Mathf.Clamp(vector.y, (float)(-(float)Screen.height), (float)Screen.height * 2f);
				vector2.x = Mathf.Clamp(vector2.x, (float)(-(float)Screen.width), (float)Screen.width * 2f);
				vector2.y = Mathf.Clamp(vector2.y, (float)(-(float)Screen.height), (float)Screen.height * 2f);
			}
			rect = new Rect(vector.x, vector.y, vector2.x - vector.x, vector2.y - vector.y);
		}
		return rect;
	}
	public static void DrawBoundsBox(EspCategory category, Bounds bounds, Color32 color, Color32 outlineColor, Color32 fillBoxColor)
	{
		bool dmk5zVuQk0jGUFJWxk9EnD7G = category.SettingFlag1;
		bool flag = dmk5zVuQk0jGUFJWxk9EnD7G;
		if (flag)
		{
			EspDrawer.BoundsCornerPoints[0] = new Vector3(bounds.center.x + bounds.extents.x, bounds.center.y + bounds.extents.y, bounds.center.z + bounds.extents.z).WorldToScreenPoint();
			EspDrawer.BoundsCornerPoints[1] = new Vector3(bounds.center.x + bounds.extents.x, bounds.center.y + bounds.extents.y, bounds.center.z - bounds.extents.z).WorldToScreenPoint();
			EspDrawer.BoundsCornerPoints[2] = new Vector3(bounds.center.x + bounds.extents.x, bounds.center.y - bounds.extents.y, bounds.center.z + bounds.extents.z).WorldToScreenPoint();
			EspDrawer.BoundsCornerPoints[3] = new Vector3(bounds.center.x + bounds.extents.x, bounds.center.y - bounds.extents.y, bounds.center.z - bounds.extents.z).WorldToScreenPoint();
			EspDrawer.BoundsCornerPoints[4] = new Vector3(bounds.center.x - bounds.extents.x, bounds.center.y + bounds.extents.y, bounds.center.z + bounds.extents.z).WorldToScreenPoint();
			EspDrawer.BoundsCornerPoints[5] = new Vector3(bounds.center.x - bounds.extents.x, bounds.center.y + bounds.extents.y, bounds.center.z - bounds.extents.z).WorldToScreenPoint();
			EspDrawer.BoundsCornerPoints[6] = new Vector3(bounds.center.x - bounds.extents.x, bounds.center.y - bounds.extents.y, bounds.center.z + bounds.extents.z).WorldToScreenPoint();
			EspDrawer.BoundsCornerPoints[7] = new Vector3(bounds.center.x - bounds.extents.x, bounds.center.y - bounds.extents.y, bounds.center.z - bounds.extents.z).WorldToScreenPoint();
			bool flag2 = false;
			bool flag3 = false;
			EspDrawer.BoxMinCorner = Vector3.zero;
			EspDrawer.BoxMaxCorner = Vector3.zero;
			for (int i = 0; i < EspDrawer.BoundsCornerPoints.Length; i++)
			{
				bool flag4 = float.IsNaN(EspDrawer.BoundsCornerPoints[i].x) || float.IsNaN(EspDrawer.BoundsCornerPoints[i].y);
				if (flag4)
				{
					flag3 = true;
				}
				else
				{
					bool flag5 = !flag2;
					if (flag5)
					{
						EspDrawer.BoxMinCorner = EspDrawer.BoundsCornerPoints[i];
						EspDrawer.BoxMaxCorner = EspDrawer.BoundsCornerPoints[i];
						flag2 = true;
					}
					else
					{
						EspDrawer.BoxMinCorner = Vector3.Min(EspDrawer.BoxMinCorner, EspDrawer.BoundsCornerPoints[i]);
						EspDrawer.BoxMaxCorner = Vector3.Max(EspDrawer.BoxMaxCorner, EspDrawer.BoundsCornerPoints[i]);
					}
				}
			}
			bool flag6 = !flag2;
			if (flag6)
			{
				return;
			}
			float num = EspDrawer.BoxMaxCorner.x - EspDrawer.BoxMinCorner.x;
			float num2 = EspDrawer.BoxMaxCorner.y - EspDrawer.BoxMinCorner.y;
			bool flag7 = num < 10f;
			if (flag7)
			{
				float num3 = (EspDrawer.BoxMaxCorner.x + EspDrawer.BoxMinCorner.x) / 2f;
				EspDrawer.BoxMinCorner.x = num3 - 5f;
				EspDrawer.BoxMaxCorner.x = num3 + 5f;
			}
			bool flag8 = num2 < 5f;
			if (flag8)
			{
				float num4 = (EspDrawer.BoxMaxCorner.y + EspDrawer.BoxMinCorner.y) / 2f;
				EspDrawer.BoxMinCorner.y = num4 - 2.5f;
				EspDrawer.BoxMaxCorner.y = num4 + 2.5f;
			}
			bool flag9 = flag3;
			if (flag9)
			{
				EspDrawer.BoxMinCorner.x = Mathf.Clamp(EspDrawer.BoxMinCorner.x, 0f, (float)Screen.width);
				EspDrawer.BoxMinCorner.y = Mathf.Clamp(EspDrawer.BoxMinCorner.y, 0f, (float)Screen.height);
				EspDrawer.BoxMaxCorner.x = Mathf.Clamp(EspDrawer.BoxMaxCorner.x, 0f, (float)Screen.width);
				EspDrawer.BoxMaxCorner.y = Mathf.Clamp(EspDrawer.BoxMaxCorner.y, 0f, (float)Screen.height);
			}
			else
			{
				EspDrawer.BoxMinCorner.x = Mathf.Clamp(EspDrawer.BoxMinCorner.x, (float)(-(float)Screen.width), (float)Screen.width * 2f);
				EspDrawer.BoxMinCorner.y = Mathf.Clamp(EspDrawer.BoxMinCorner.y, (float)(-(float)Screen.height), (float)Screen.height * 2f);
				EspDrawer.BoxMaxCorner.x = Mathf.Clamp(EspDrawer.BoxMaxCorner.x, (float)(-(float)Screen.width), (float)Screen.width * 2f);
				EspDrawer.BoxMaxCorner.y = Mathf.Clamp(EspDrawer.BoxMaxCorner.y, (float)(-(float)Screen.height), (float)Screen.height * 2f);
			}
			EspDrawer.BoxCornerTopLeft = new Vector2(EspDrawer.BoxMinCorner.x, EspDrawer.BoxMinCorner.y);
			EspDrawer.BoxCornerBottomRight = new Vector2(EspDrawer.BoxMaxCorner.x, EspDrawer.BoxMaxCorner.y);
			EspDrawer.BoxCornerBottomLeft = new Vector2(EspDrawer.BoxMinCorner.x, EspDrawer.BoxMaxCorner.y);
			EspDrawer.BoxCornerTopRight = new Vector2(EspDrawer.BoxMaxCorner.x, EspDrawer.BoxMinCorner.y);
			EspDrawer.DrawLine(EspDrawer.BoxCornerTopLeft, EspDrawer.BoxCornerBottomLeft, color, 1f);
			EspDrawer.DrawLine(EspDrawer.BoxCornerTopRight, EspDrawer.BoxCornerBottomRight, color, 1f);
			EspDrawer.DrawLine(EspDrawer.BoxCornerTopLeft, EspDrawer.BoxCornerTopRight, color, 1f);
			EspDrawer.DrawLine(EspDrawer.BoxCornerBottomLeft, EspDrawer.BoxCornerBottomRight, color, 1f);
			bool d0VJlj2dXjM6lM16rhlHPeVsM = category.SettingFlag2;
			bool flag10 = d0VJlj2dXjM6lM16rhlHPeVsM;
			if (flag10)
			{
				EspDrawer.BoxCornerTopLeft.x = EspDrawer.BoxCornerTopLeft.x + 1f;
				EspDrawer.BoxCornerTopLeft.y = EspDrawer.BoxCornerTopLeft.y + 1f;
				EspDrawer.BoxCornerBottomLeft.x = EspDrawer.BoxCornerBottomLeft.x + 1f;
				EspDrawer.BoxCornerBottomLeft.y = EspDrawer.BoxCornerBottomLeft.y - 1f;
				EspDrawer.BoxCornerBottomRight.x = EspDrawer.BoxCornerBottomRight.x + 1f;
				EspDrawer.BoxCornerBottomRight.y = EspDrawer.BoxCornerBottomRight.y - 1f;
				EspDrawer.BoxCornerTopRight.x = EspDrawer.BoxCornerTopRight.x + 1f;
				EspDrawer.BoxCornerTopRight.y = EspDrawer.BoxCornerTopRight.y + 1f;
				EspDrawer.DrawLine(EspDrawer.BoxCornerTopLeft, EspDrawer.BoxCornerBottomLeft, outlineColor, 1f);
				EspDrawer.DrawLine(EspDrawer.BoxCornerTopRight, EspDrawer.BoxCornerBottomRight, outlineColor, 1f);
				EspDrawer.DrawLine(EspDrawer.BoxCornerTopLeft, EspDrawer.BoxCornerTopRight, outlineColor, 1f);
				EspDrawer.DrawLine(EspDrawer.BoxCornerBottomLeft, EspDrawer.BoxCornerBottomRight, outlineColor, 1f);
				EspDrawer.BoxCornerTopLeft.x = EspDrawer.BoxCornerTopLeft.x - 1f;
				EspDrawer.BoxCornerTopLeft.y = EspDrawer.BoxCornerTopLeft.y - 1f;
				EspDrawer.BoxCornerBottomLeft.x = EspDrawer.BoxCornerBottomLeft.x - 1f;
				EspDrawer.BoxCornerBottomLeft.y = EspDrawer.BoxCornerBottomLeft.y + 1f;
				EspDrawer.BoxCornerBottomRight.x = EspDrawer.BoxCornerBottomRight.x - 1f;
				EspDrawer.BoxCornerBottomRight.y = EspDrawer.BoxCornerBottomRight.y + 1f;
				EspDrawer.BoxCornerTopRight.x = EspDrawer.BoxCornerTopRight.x - 1f;
				EspDrawer.BoxCornerTopRight.y = EspDrawer.BoxCornerTopRight.y - 1f;
			}
			bool dizjSZiKvyMuCh4R3JSe5Gs7x = category.SettingFlag4;
			bool flag11 = dizjSZiKvyMuCh4R3JSe5Gs7x;
			if (flag11)
			{
				EspDrawer.BoxCornerTopLeft.x = EspDrawer.BoxCornerTopLeft.x + 1f;
				EspDrawer.BoxCornerTopLeft.y = EspDrawer.BoxCornerTopLeft.y + 1f;
				EspDrawer.BoxCornerBottomRight.x = EspDrawer.BoxCornerBottomRight.x + 1f;
				EspDrawer.BoxCornerBottomRight.y = EspDrawer.BoxCornerBottomRight.y - 1f;
				GUI.DrawTexture(new Rect(EspDrawer.BoxCornerTopLeft.x, EspDrawer.BoxCornerTopLeft.y, Mathf.Abs(EspDrawer.BoxCornerTopLeft.x - EspDrawer.BoxCornerBottomRight.x), Mathf.Abs(EspDrawer.BoxCornerTopLeft.y - EspDrawer.BoxCornerBottomRight.y)), GuiStyles.WhiteTexture, ScaleMode.StretchToFill, true, 0f, fillBoxColor, 0f, 0f);
				EspDrawer.BoxCornerTopLeft.x = EspDrawer.BoxCornerTopLeft.x - 1f;
				EspDrawer.BoxCornerTopLeft.y = EspDrawer.BoxCornerTopLeft.y - 1f;
				EspDrawer.BoxCornerBottomRight.x = EspDrawer.BoxCornerBottomRight.x - 1f;
				EspDrawer.BoxCornerBottomRight.y = EspDrawer.BoxCornerBottomRight.y + 1f;
			}
			bool dajq3CYTvDIFQrH3BffYoksFp = category.SettingFlag3;
			bool flag12 = dajq3CYTvDIFQrH3BffYoksFp;
			if (flag12)
			{
				EspDrawer.BoxCornerTopLeft.x = EspDrawer.BoxCornerTopLeft.x - 1f;
				EspDrawer.BoxCornerTopLeft.y = EspDrawer.BoxCornerTopLeft.y - 1f;
				EspDrawer.BoxCornerBottomLeft.x = EspDrawer.BoxCornerBottomLeft.x - 1f;
				EspDrawer.BoxCornerBottomLeft.y = EspDrawer.BoxCornerBottomLeft.y + 1f;
				EspDrawer.BoxCornerBottomRight.x = EspDrawer.BoxCornerBottomRight.x + 1f;
				EspDrawer.BoxCornerBottomRight.y = EspDrawer.BoxCornerBottomRight.y + 1f;
				EspDrawer.BoxCornerTopRight.x = EspDrawer.BoxCornerTopRight.x + 1f;
				EspDrawer.BoxCornerTopRight.y = EspDrawer.BoxCornerTopRight.y - 1f;
				EspDrawer.DrawLine(EspDrawer.BoxCornerTopLeft, EspDrawer.BoxCornerBottomLeft, outlineColor, 1f);
				EspDrawer.DrawLine(EspDrawer.BoxCornerTopRight, EspDrawer.BoxCornerBottomRight, outlineColor, 1f);
				EspDrawer.DrawLine(EspDrawer.BoxCornerTopLeft, EspDrawer.BoxCornerTopRight, outlineColor, 1f);
				EspDrawer.DrawLine(EspDrawer.BoxCornerBottomLeft, EspDrawer.BoxCornerBottomRight, outlineColor, 1f);
			}
		}
		bool d30iUXR8sxNNzAAzBtRq0xi0n = category.SettingFlag5;
		bool flag13 = d30iUXR8sxNNzAAzBtRq0xi0n;
		if (flag13)
		{
			bool flag14 = Vector2.Distance(new Vector3(bounds.center.x + bounds.extents.x, bounds.center.y, bounds.center.z).WorldToScreenPoint(), new Vector3(bounds.center.x - bounds.extents.x, bounds.center.y, bounds.center.z).WorldToScreenPoint()) > Vector2.Distance(new Vector3(bounds.center.x, bounds.center.y, bounds.center.z - bounds.extents.z).WorldToScreenPoint(), new Vector3(bounds.center.x, bounds.center.y, bounds.center.z + bounds.extents.z).WorldToScreenPoint());
			bool flag15 = flag14;
			if (flag15)
			{
				EspDrawer.BoxCornerTopLeft = new Vector3(bounds.center.x + bounds.extents.x, bounds.center.y + bounds.extents.y, bounds.center.z).WorldToScreenPoint();
				EspDrawer.BoxCornerBottomRight = new Vector3(bounds.center.x - bounds.extents.x, bounds.center.y - bounds.extents.y, bounds.center.z).WorldToScreenPoint();
				EspDrawer.BoxCornerBottomLeft = new Vector3(bounds.center.x + bounds.extents.x, bounds.center.y - bounds.extents.y, bounds.center.z).WorldToScreenPoint();
				EspDrawer.BoxCornerTopRight = new Vector3(bounds.center.x - bounds.extents.x, bounds.center.y + bounds.extents.y, bounds.center.z).WorldToScreenPoint();
			}
			else
			{
				EspDrawer.BoxCornerTopLeft = new Vector3(bounds.center.x, bounds.center.y + bounds.extents.y, bounds.center.z + bounds.extents.z).WorldToScreenPoint();
				EspDrawer.BoxCornerBottomRight = new Vector3(bounds.center.x, bounds.center.y - bounds.extents.y, bounds.center.z - bounds.extents.z).WorldToScreenPoint();
				EspDrawer.BoxCornerBottomLeft = new Vector3(bounds.center.x, bounds.center.y - bounds.extents.y, bounds.center.z + bounds.extents.z).WorldToScreenPoint();
				EspDrawer.BoxCornerTopRight = new Vector3(bounds.center.x, bounds.center.y + bounds.extents.y, bounds.center.z - bounds.extents.z).WorldToScreenPoint();
			}
			EspDrawer.DrawLine(EspDrawer.BoxCornerTopLeft, EspDrawer.BoxCornerBottomLeft, color, 1f);
			EspDrawer.DrawLine(EspDrawer.BoxCornerTopRight, EspDrawer.BoxCornerBottomRight, color, 1f);
			EspDrawer.DrawLine(EspDrawer.BoxCornerTopLeft, EspDrawer.BoxCornerTopRight, color, 1f);
			EspDrawer.DrawLine(EspDrawer.BoxCornerBottomLeft, EspDrawer.BoxCornerBottomRight, color, 1f);
			bool daL9kFSE3cYbY6LHeOtuNQDbi = category.SettingFlag6;
			bool flag16 = daL9kFSE3cYbY6LHeOtuNQDbi;
			if (flag16)
			{
				EspDrawer.BoxCornerTopLeft.x = EspDrawer.BoxCornerTopLeft.x + 1f;
				EspDrawer.BoxCornerTopLeft.y = EspDrawer.BoxCornerTopLeft.y + 1f;
				EspDrawer.BoxCornerBottomLeft.x = EspDrawer.BoxCornerBottomLeft.x + 1f;
				EspDrawer.BoxCornerBottomLeft.y = EspDrawer.BoxCornerBottomLeft.y - 1f;
				EspDrawer.BoxCornerBottomRight.x = EspDrawer.BoxCornerBottomRight.x + 1f;
				EspDrawer.BoxCornerBottomRight.y = EspDrawer.BoxCornerBottomRight.y - 1f;
				EspDrawer.BoxCornerTopRight.x = EspDrawer.BoxCornerTopRight.x + 1f;
				EspDrawer.BoxCornerTopRight.y = EspDrawer.BoxCornerTopRight.y + 1f;
				EspDrawer.DrawLine(EspDrawer.BoxCornerTopLeft, EspDrawer.BoxCornerBottomLeft, outlineColor, 1f);
				EspDrawer.DrawLine(EspDrawer.BoxCornerTopRight, EspDrawer.BoxCornerBottomRight, outlineColor, 1f);
				EspDrawer.DrawLine(EspDrawer.BoxCornerTopLeft, EspDrawer.BoxCornerTopRight, outlineColor, 1f);
				EspDrawer.DrawLine(EspDrawer.BoxCornerBottomLeft, EspDrawer.BoxCornerBottomRight, outlineColor, 1f);
				EspDrawer.BoxCornerTopLeft.x = EspDrawer.BoxCornerTopLeft.x - 1f;
				EspDrawer.BoxCornerTopLeft.y = EspDrawer.BoxCornerTopLeft.y - 1f;
				EspDrawer.BoxCornerBottomLeft.x = EspDrawer.BoxCornerBottomLeft.x - 1f;
				EspDrawer.BoxCornerBottomLeft.y = EspDrawer.BoxCornerBottomLeft.y + 1f;
				EspDrawer.BoxCornerBottomRight.x = EspDrawer.BoxCornerBottomRight.x - 1f;
				EspDrawer.BoxCornerBottomRight.y = EspDrawer.BoxCornerBottomRight.y + 1f;
				EspDrawer.BoxCornerTopRight.x = EspDrawer.BoxCornerTopRight.x - 1f;
				EspDrawer.BoxCornerTopRight.y = EspDrawer.BoxCornerTopRight.y - 1f;
			}
			bool dr1UbOEjagrEGWBLZB7U0v8b = category.SettingFlag7;
			bool flag17 = dr1UbOEjagrEGWBLZB7U0v8b;
			if (flag17)
			{
				EspDrawer.BoxCornerTopLeft.x = EspDrawer.BoxCornerTopLeft.x - 1f;
				EspDrawer.BoxCornerTopLeft.y = EspDrawer.BoxCornerTopLeft.y - 1f;
				EspDrawer.BoxCornerBottomLeft.x = EspDrawer.BoxCornerBottomLeft.x - 1f;
				EspDrawer.BoxCornerBottomLeft.y = EspDrawer.BoxCornerBottomLeft.y + 1f;
				EspDrawer.BoxCornerBottomRight.x = EspDrawer.BoxCornerBottomRight.x - 1f;
				EspDrawer.BoxCornerBottomRight.y = EspDrawer.BoxCornerBottomRight.y + 1f;
				EspDrawer.BoxCornerTopRight.x = EspDrawer.BoxCornerTopRight.x - 1f;
				EspDrawer.BoxCornerTopRight.y = EspDrawer.BoxCornerTopRight.y - 1f;
				EspDrawer.DrawLine(EspDrawer.BoxCornerTopLeft, EspDrawer.BoxCornerBottomLeft, outlineColor, 1f);
				EspDrawer.DrawLine(EspDrawer.BoxCornerTopRight, EspDrawer.BoxCornerBottomRight, outlineColor, 1f);
				EspDrawer.DrawLine(EspDrawer.BoxCornerTopLeft, EspDrawer.BoxCornerTopRight, outlineColor, 1f);
				EspDrawer.DrawLine(EspDrawer.BoxCornerBottomLeft, EspDrawer.BoxCornerBottomRight, outlineColor, 1f);
			}
		}
		bool dctnEhAURtzTQjQG5HovyX10f = category.SettingFlag8;
		bool flag18 = dctnEhAURtzTQjQG5HovyX10f;
		if (flag18)
		{
			EspDrawer.BoundsCornerPoints[0] = new Vector3(bounds.center.x + bounds.extents.x, bounds.center.y + bounds.extents.y, bounds.center.z + bounds.extents.z).WorldToScreenPoint();
			EspDrawer.BoundsCornerPoints[1] = new Vector3(bounds.center.x - bounds.extents.x, bounds.center.y - bounds.extents.y, bounds.center.z + bounds.extents.z).WorldToScreenPoint();
			EspDrawer.BoundsCornerPoints[2] = new Vector3(bounds.center.x + bounds.extents.x, bounds.center.y + bounds.extents.y, bounds.center.z - bounds.extents.z).WorldToScreenPoint();
			EspDrawer.BoundsCornerPoints[3] = new Vector3(bounds.center.x - bounds.extents.x, bounds.center.y - bounds.extents.y, bounds.center.z - bounds.extents.z).WorldToScreenPoint();
			EspDrawer.BoundsCornerPoints[4] = new Vector3(bounds.center.x + bounds.extents.x, bounds.center.y - bounds.extents.y, bounds.center.z + bounds.extents.z).WorldToScreenPoint();
			EspDrawer.BoundsCornerPoints[5] = new Vector3(bounds.center.x - bounds.extents.x, bounds.center.y + bounds.extents.y, bounds.center.z + bounds.extents.z).WorldToScreenPoint();
			EspDrawer.BoundsCornerPoints[6] = new Vector3(bounds.center.x + bounds.extents.x, bounds.center.y - bounds.extents.y, bounds.center.z - bounds.extents.z).WorldToScreenPoint();
			EspDrawer.BoundsCornerPoints[7] = new Vector3(bounds.center.x - bounds.extents.x, bounds.center.y + bounds.extents.y, bounds.center.z - bounds.extents.z).WorldToScreenPoint();
			EspDrawer.DrawLine(EspDrawer.BoundsCornerPoints[0], EspDrawer.BoundsCornerPoints[4], color, 1f);
			EspDrawer.DrawLine(EspDrawer.BoundsCornerPoints[1], EspDrawer.BoundsCornerPoints[5], color, 1f);
			EspDrawer.DrawLine(EspDrawer.BoundsCornerPoints[0], EspDrawer.BoundsCornerPoints[5], color, 1f);
			EspDrawer.DrawLine(EspDrawer.BoundsCornerPoints[4], EspDrawer.BoundsCornerPoints[1], color, 1f);
			EspDrawer.DrawLine(EspDrawer.BoundsCornerPoints[2], EspDrawer.BoundsCornerPoints[6], color, 1f);
			EspDrawer.DrawLine(EspDrawer.BoundsCornerPoints[3], EspDrawer.BoundsCornerPoints[7], color, 1f);
			EspDrawer.DrawLine(EspDrawer.BoundsCornerPoints[2], EspDrawer.BoundsCornerPoints[7], color, 1f);
			EspDrawer.DrawLine(EspDrawer.BoundsCornerPoints[6], EspDrawer.BoundsCornerPoints[3], color, 1f);
			EspDrawer.DrawLine(EspDrawer.BoundsCornerPoints[0], EspDrawer.BoundsCornerPoints[2], color, 1f);
			EspDrawer.DrawLine(EspDrawer.BoundsCornerPoints[1], EspDrawer.BoundsCornerPoints[3], color, 1f);
			EspDrawer.DrawLine(EspDrawer.BoundsCornerPoints[4], EspDrawer.BoundsCornerPoints[6], color, 1f);
			EspDrawer.DrawLine(EspDrawer.BoundsCornerPoints[5], EspDrawer.BoundsCornerPoints[7], color, 1f);
		}
	}
	public static void DrawTracer(Vector2 center, Vector3 position, Color32 lineColor, Color32 outlineColor, bool outline)
	{
		EspDrawer.TracerEndPoint = position.WorldToScreenPoint();
		EspDrawer.DrawLine(center, EspDrawer.TracerEndPoint, lineColor, 1f);
		if (outline)
		{
			EspDrawer.TracerEndPoint.x = EspDrawer.TracerEndPoint.x + 1f;
			center.x += 1f;
			EspDrawer.DrawLine(center, EspDrawer.TracerEndPoint, outlineColor, 1f);
			EspDrawer.TracerEndPoint.x = EspDrawer.TracerEndPoint.x - 2f;
			center.x -= 2f;
			EspDrawer.DrawLine(center, EspDrawer.TracerEndPoint, outlineColor, 1f);
		}
	}
	public static void DrawOutlinedText(Rect rect, string text, TextOutlineStyle outline, GUIStyle outlineText, int thickness)
	{
		switch (outline)
		{
		case TextOutlineStyle.RightDownSided:
		{
			for (int i = 0; i < thickness; i++)
			{
				GUI.Label(new Rect(rect.x + (float)i, rect.y, rect.width, rect.height), text, outlineText);
				GUI.Label(new Rect(rect.x, rect.y + (float)i, rect.width, rect.height), text, outlineText);
			}
			break;
		}
		case TextOutlineStyle.RightTopSided:
		{
			for (int j = 0; j < thickness; j++)
			{
				GUI.Label(new Rect(rect.x + (float)j, rect.y, rect.width, rect.height), text, outlineText);
				GUI.Label(new Rect(rect.x, rect.y - (float)j, rect.width, rect.height), text, outlineText);
			}
			break;
		}
		case TextOutlineStyle.LeftTopSided:
		{
			for (int k = 0; k < thickness; k++)
			{
				GUI.Label(new Rect(rect.x - (float)k, rect.y, rect.width, rect.height), text, outlineText);
				GUI.Label(new Rect(rect.x, rect.y - (float)k, rect.width, rect.height), text, outlineText);
			}
			break;
		}
		case TextOutlineStyle.LeftDownSided:
		{
			for (int l = 0; l < thickness; l++)
			{
				GUI.Label(new Rect(rect.x - (float)l, rect.y, rect.width, rect.height), text, outlineText);
				GUI.Label(new Rect(rect.x, rect.y + (float)l, rect.width, rect.height), text, outlineText);
			}
			break;
		}
		case TextOutlineStyle.FourSided:
		{
			for (int m = 0; m < thickness; m++)
			{
				GUI.Label(new Rect(rect.x + (float)m, rect.y, rect.width, rect.height), text, outlineText);
				GUI.Label(new Rect(rect.x, rect.y - (float)m, rect.width, rect.height), text, outlineText);
				GUI.Label(new Rect(rect.x - (float)m, rect.y, rect.width, rect.height), text, outlineText);
				GUI.Label(new Rect(rect.x, rect.y + (float)m, rect.width, rect.height), text, outlineText);
			}
			break;
		}
		}
	}
	public static void DrawFilledTriangle(Vector2 p1, Vector2 p2, Vector2 p3, Color color)
	{
		bool flag = EspManager.ScreenLineMaterial == null;
		if (!flag)
		{
			EspManager.ScreenLineMaterial.SetPass(0);
			GL.PushMatrix();
			GL.LoadPixelMatrix(0f, (float)Screen.width, (float)Screen.height, 0f);
			GL.Begin(4);
			GL.Color(color);
			GL.Vertex3(p1.x, p1.y, 0f);
			GL.Vertex3(p2.x, p2.y, 0f);
			GL.Vertex3(p3.x, p3.y, 0f);
			GL.End();
			GL.PopMatrix();
		}
	}
	private static bool PointInTriangle(Vector2 pt, Vector2 p1, Vector2 p2, Vector2 p3)
	{
		float num = EspDrawer.Sign(pt, p1, p2);
		float num2 = EspDrawer.Sign(pt, p2, p3);
		float num3 = EspDrawer.Sign(pt, p3, p1);
		bool flag = num < 0f || num2 < 0f || num3 < 0f;
		bool flag2 = num > 0f || num2 > 0f || num3 > 0f;
		return !flag || !flag2;
	}
	private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
	{
		return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
	}
	public static Matrix4x4 WorldToCameraMatrix;
	public static Matrix4x4 ProjectionMatrix;
	public static Vector3[] BoundsCornerPoints = new Vector3[8];
	private static Vector2 BoxCornerTopLeft;
	private static Vector2 BoxCornerBottomRight;
	private static Vector2 BoxCornerBottomLeft;
	private static Vector2 BoxCornerTopRight;
	private static Vector2 TracerEndPoint;
	public static Vector3 BoxMinCorner;
	public static Vector3 BoxMaxCorner;
	private static float LineRotationAngle;
	private static Matrix4x4 SavedGuiMatrix;
	public static Camera RenderCamera;
}
