using System;
using SDG.Unturned;
using UnityEngine;
public static class PlayerExtensions
{
	public static PlayerBones GetOrAddPlayerBones(this Player p)
	{
		PlayerBones d7ElSFH0pY0XmMbO1Ij5Yf3Tp;
		bool flag = PlayerBones.playersByNetId.TryGetValue(p.GetNetId().id, out d7ElSFH0pY0XmMbO1Ij5Yf3Tp) || PlayerBones.playersByPlayer.TryGetValue(p, out d7ElSFH0pY0XmMbO1Ij5Yf3Tp);
		bool flag2 = flag;
		PlayerBones d7ElSFH0pY0XmMbO1Ij5Yf3Tp2;
		if (flag2)
		{
			d7ElSFH0pY0XmMbO1Ij5Yf3Tp2 = d7ElSFH0pY0XmMbO1Ij5Yf3Tp;
		}
		else
		{
			bool flag3 = p.TryGetComponent<PlayerBones>(out d7ElSFH0pY0XmMbO1Ij5Yf3Tp);
			bool flag4 = flag3;
			if (flag4)
			{
				d7ElSFH0pY0XmMbO1Ij5Yf3Tp2 = d7ElSFH0pY0XmMbO1Ij5Yf3Tp;
			}
			else
			{
				d7ElSFH0pY0XmMbO1Ij5Yf3Tp2 = p.gameObject.AddComponent<PlayerBones>();
			}
		}
		return d7ElSFH0pY0XmMbO1Ij5Yf3Tp2;
	}
	public static Vector3 GetAimPoint(this Player p)
	{
		Vector3 vector;
		try
		{
			bool flag = AimbotConfig.enableVehicleHitboxExploit && p.movement.getVehicle() != null;
			bool flag2 = flag;
			if (flag2)
			{
				vector = p.movement.getVehicle().transform.position;
				return vector;
			}
			bool flag3 = AimbotConfig.enableAimbot && AimbotConfig.IsMemoryAimbotKeyActive() && !AimbotConfig.enableSilentAim;
			bool flag4 = flag3;
			if (flag4)
			{
				bool bestAimbotPartPreselective = AimbotConfig.bestAimbotPartPreselective;
				bool flag5 = bestAimbotPartPreselective;
				if (flag5)
				{
					vector = p.GetOrAddPlayerBones().GetVisibleLimbPosition();
				}
				else
				{
					vector = p.GetOrAddPlayerBones().GetLimbPosition(AimbotConfig.aimbotLimb);
				}
			}
			else
			{
				bool hitPointToTransform = AimbotConfig.hitPointToTransform;
				bool flag6 = hitPointToTransform;
				if (flag6)
				{
					vector = p.transform.position;
				}
				else
				{
					bool bestSilentAimPartPreselective = AimbotConfig.bestSilentAimPartPreselective;
					bool flag7 = bestSilentAimPartPreselective;
					if (flag7)
					{
						vector = p.GetOrAddPlayerBones().GetVisibleLimbPosition();
					}
					else
					{
						vector = p.GetOrAddPlayerBones().GetLimbPosition(AimbotConfig.silentAimHitPointLimb);
					}
				}
			}
		}
		catch
		{
			vector = p.transform.position;
		}
		return vector;
	}
	public static Transform GetAimTransform(this Player p)
	{
		bool flag = AimbotConfig.enableVehicleHitboxExploit && p.movement.getVehicle() != null;
		bool flag2 = flag;
		Transform transform;
		if (flag2)
		{
			transform = p.movement.getVehicle().transform;
		}
		else
		{
			bool flag3 = AimbotConfig.enableAimbot && AimbotConfig.IsMemoryAimbotKeyActive() && !AimbotConfig.enableSilentAim;
			bool flag4 = flag3;
			Transform transform2;
			if (flag4)
			{
				bool bestAimbotPartPreselective = AimbotConfig.bestAimbotPartPreselective;
				bool flag5 = bestAimbotPartPreselective;
				if (flag5)
				{
					transform2 = p.GetOrAddPlayerBones().GetVisibleLimbTransform();
				}
				else
				{
					transform2 = p.GetOrAddPlayerBones().GetLimbTransform(AimbotConfig.aimbotLimb);
				}
			}
			else
			{
				bool hitPointToTransform = AimbotConfig.hitPointToTransform;
				bool flag6 = hitPointToTransform;
				if (flag6)
				{
					transform2 = p.transform;
				}
				else
				{
					bool bestSilentAimPartPreselective = AimbotConfig.bestSilentAimPartPreselective;
					bool flag7 = bestSilentAimPartPreselective;
					if (flag7)
					{
						transform2 = p.GetOrAddPlayerBones().GetVisibleLimbTransform();
					}
					else
					{
						transform2 = p.GetOrAddPlayerBones().GetLimbTransform(AimbotConfig.silentAimHitPointLimb);
					}
				}
			}
			transform = transform2;
		}
		return transform;
	}
}
