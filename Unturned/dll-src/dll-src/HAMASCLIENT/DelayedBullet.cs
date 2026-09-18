using System;
using SDG.Unturned;
using UnityEngine;
public class DelayedBullet
{
	public Vector3 Origin;
	public Vector3 Position;
	public Vector3 Velocity;
	public byte Step = 0;
	public byte BallisticIndex;
	public uint ProjectileId = 0U;
	public ItemBarrelAsset BarrelAsset;
	public ItemMagazineAsset MagazineAsset;
	public Vector3 SphereOffset;
	public TargetType TargetType;
	public object Target;
	public float fireTime = 0f;
}
