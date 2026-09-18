using System;
using System.Collections.Generic;
using UnityEngine;
public class WalkingTracerState
{
	public WalkingTracerState(Vector3 lastPoint)
	{
		this.LastPoint = lastPoint;
		this.LeftEdgePoint = lastPoint;
		this.RightEdgePoint = lastPoint;
		this.LastMoveDirection = Vector3.zero;
	}
	public Vector3 LastPoint;
	public Vector3 LastMoveDirection;
	public Vector3 LeftEdgePoint;
	public Vector3 RightEdgePoint;
	public List<QuadCorners> TracerSegments = new List<QuadCorners>();
}
