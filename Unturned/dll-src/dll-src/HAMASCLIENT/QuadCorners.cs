using System;
using UnityEngine;
public struct QuadCorners
{
	public QuadCorners(Vector3 leftTop, Vector3 rightTop, Vector3 leftDown, Vector3 rightDown)
	{
		this.Offset = 0f;
		this.LeftTop = leftTop;
		this.RightTop = rightTop;
		this.LeftBottom = leftDown;
		this.RightBottom = rightDown;
	}
	public float Offset;
	public Vector3 LeftTop;
	public Vector3 RightTop;
	public Vector3 LeftBottom;
	public Vector3 RightBottom;
}
