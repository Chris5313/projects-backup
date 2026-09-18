using System;
using UnityEngine;
public static class WorldToScreenUtil
{
	public static bool IsOnScreen(this Vector3 pos)
	{
		bool dpxZp2Jw9ifhgtZ5Fai9okn4i = EspManager.EspDirtyFlag;
		bool flag = dpxZp2Jw9ifhgtZ5Fai9okn4i;
		bool flag2;
		if (flag)
		{
			pos = EspDrawer.RenderCamera.WorldToViewportPoint(pos);
			flag2 = pos.z > 0f && pos.x > 0f && pos.x < 1f && pos.y > 0f && pos.y < 1f;
		}
		else
		{
			Camera dr38GGKdBZ2EXiNCUdvVjgmhc = EspDrawer.RenderCamera;
			WorldToScreenUtil.ViewDirection = Vector3.Normalize(pos - dr38GGKdBZ2EXiNCUdvVjgmhc.transform.position);
			WorldToScreenUtil.ViewAngle = Mathf.Acos(Vector3.Dot(dr38GGKdBZ2EXiNCUdvVjgmhc.transform.forward, WorldToScreenUtil.ViewDirection)) * 57.29578f;
			float num = dr38GGKdBZ2EXiNCUdvVjgmhc.fieldOfView * 0.5f;
			float num2 = Mathf.Atan(Mathf.Tan(num * 0.01745329f) * dr38GGKdBZ2EXiNCUdvVjgmhc.aspect) * 57.29578f;
			float num3 = Mathf.Max(num, num2);
			flag2 = Vector3.Dot(dr38GGKdBZ2EXiNCUdvVjgmhc.transform.forward, WorldToScreenUtil.ViewDirection) > 0f && WorldToScreenUtil.ViewAngle <= num3;
		}
		return flag2;
	}
	public static Vector2 WorldToScreenPoint(this Vector3 pos)
	{
		WorldToScreenUtil.ViewProjectionMatrix = EspDrawer.ProjectionMatrix * EspDrawer.WorldToCameraMatrix;
		Vector4 vector = WorldToScreenUtil.ViewProjectionMatrix * new Vector4(pos.x, pos.y, pos.z, 1f);
		bool flag = vector.w < 0.1f;
		Vector2 vector2;
		if (flag)
		{
			vector2 = new Vector2(float.NaN, float.NaN);
		}
		else
		{
			WorldToScreenUtil.ScreenPoint = new Vector3(vector.x / vector.w, vector.y / vector.w, 0f);
			WorldToScreenUtil.ScreenPoint = new Vector3((WorldToScreenUtil.ScreenPoint.x + 1f) * (float)Screen.width, (WorldToScreenUtil.ScreenPoint.y + 1f) * (float)Screen.height, 0f) / 2f;
			float num = WorldToScreenUtil.ScreenPoint.x;
			float num2 = (float)Screen.height - WorldToScreenUtil.ScreenPoint.y;
			float num3 = (float)Mathf.Max(Screen.width, Screen.height);
			num = Mathf.Clamp(num, -num3, (float)Screen.width + num3);
			num2 = Mathf.Clamp(num2, -num3, (float)Screen.height + num3);
			vector2 = new Vector2(num, num2);
		}
		return vector2;
	}
	private static Matrix4x4 ViewProjectionMatrix;
	private static Vector3 ScreenPoint;
	private static Vector3 ViewDirection;
	private static float ViewAngle;
}
