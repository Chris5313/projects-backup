using System;
using UnityEngine;
public struct AimObjective
{
	public AimObjective(TargetType goal, object objective, Vector3 pointOffset)
	{
		this.TargetType = goal;
		this.Target = objective;
		this.PointOffset = pointOffset;
	}
	public TargetType TargetType;
	public object Target;
	public Vector3 PointOffset;
}
