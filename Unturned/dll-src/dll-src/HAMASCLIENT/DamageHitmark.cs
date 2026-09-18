using System;
using UnityEngine;
public struct DamageHitmark
{
	public DamageHitmark(Vector3 point, ushort damage)
	{
		this.damage = damage;
		this.labelPosition = point;
		this.hitPoint = point;
		this.lifeProgress = 0f;
		this.combineCount = 0;
	}
	public ushort damage;
	public Vector3 hitPoint;
	public Vector3 labelPosition;
	public float lifeProgress;
	public byte combineCount;
}
