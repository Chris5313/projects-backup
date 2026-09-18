using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SDG.Unturned;
using UnityEngine;

namespace gatyware
{
	public static class DamageNumbers
	{
		[DllImport("kernel32.dll")]
		private static extern bool VirtualProtect(IntPtr addr, int size, uint prot, out uint old);

		public static void Init()
		{
			if (DamageNumbers._hooked)
			{
				return;
			}
			try
			{
				DamageNumbers._hookOrig = typeof(PlayerUI).GetMethod("hitmark", BindingFlags.Static | BindingFlags.Public);
				if (DamageNumbers._hookOrig == null)
				{
					Runtime.Trace("dmgnum: hitmark not found");
				}
				else
				{
					RuntimeHelpers.PrepareMethod(DamageNumbers._hookOrig.MethodHandle);
					DamageNumbers._hookOrigPtr = DamageNumbers._hookOrig.MethodHandle.GetFunctionPointer();
					MethodInfo method = typeof(DamageNumbers).GetMethod("HookHitmark", BindingFlags.Static | BindingFlags.NonPublic);
					RuntimeHelpers.PrepareMethod(method.MethodHandle);
					Marshal.Copy(DamageNumbers._hookOrigPtr, DamageNumbers._hookSaved, 0, 14);
					DamageNumbers.WriteJmp(DamageNumbers._hookOrigPtr, method.MethodHandle.GetFunctionPointer());
					DamageNumbers._hooked = true;
					Runtime.Trace("dmgnum: hook OK");
				}
			}
			catch (Exception ex)
			{
				Runtime.Trace("dmgnum: err " + ex.Message);
			}
		}

		private static void WriteJmp(IntPtr from, IntPtr to)
		{
			uint prot;
			DamageNumbers.VirtualProtect(from, 14, 64U, out prot);
			byte[] array = new byte[14];
			array[0] = byte.MaxValue;
			array[1] = 37;
			BitConverter.GetBytes(to.ToInt64()).CopyTo(array, 6);
			Marshal.Copy(array, 0, from, 14);
			DamageNumbers.VirtualProtect(from, 14, prot, out prot);
		}

		private static void HookHitmark(Vector3 point, bool worldspace, EPlayerHit newHit)
		{
			if (State.DamageNumbers && newHit != (EPlayerHit)0)
			{
				int damage = DamageNumbers.EstimateDamage(newHit);
				DamageNumbers._entries.Add(new DamageNumbers.DmgEntry
				{
					pos = point,
					origin = point,
					birth = Time.unscaledTime,
					damage = damage,
					type = newHit
				});
			}
			if (State.MeleeTracers && newHit != (EPlayerHit)0)
			{
				Player localPlayer = Player.LocalPlayer;
				UnityEngine.Object @object;
				if (localPlayer == null)
				{
					@object = null;
				}
				else
				{
					PlayerLook look = localPlayer.look;
					@object = ((look != null) ? look.aim : null);
				}
				if (@object != null)
				{
					PlayerEquipment equipment = localPlayer.equipment;
					if (((equipment != null) ? equipment.useable : null) == null || localPlayer.equipment.useable is UseableMelee)
					{
						ESP.AddMeleeGhost(localPlayer.look.aim.position, point);
					}
				}
			}
			if (State.BulletEsp && State.BulletTrail && newHit != (EPlayerHit)0)
			{
				Player localPlayer2 = Player.LocalPlayer;
				UnityEngine.Object object2;
				if (localPlayer2 == null)
				{
					object2 = null;
				}
				else
				{
					PlayerLook look2 = localPlayer2.look;
					object2 = ((look2 != null) ? look2.aim : null);
				}
				if (object2 != null)
				{
					PlayerEquipment equipment2 = localPlayer2.equipment;
					if (((equipment2 != null) ? equipment2.useable : null) is UseableGun)
					{
						ESP.AddBulletGhost(localPlayer2.look.aim.position, point, State.BulletSelfColor);
					}
				}
			}
			uint prot;
			DamageNumbers.VirtualProtect(DamageNumbers._hookOrigPtr, 14, 64U, out prot);
			Marshal.Copy(DamageNumbers._hookSaved, 0, DamageNumbers._hookOrigPtr, 14);
			DamageNumbers.VirtualProtect(DamageNumbers._hookOrigPtr, 14, prot, out prot);
			try
			{
				DamageNumbers._hookOrig.Invoke(null, new object[]
				{
					point,
					worldspace,
					newHit
				});
			}
			finally
			{
				DamageNumbers.WriteJmp(DamageNumbers._hookOrigPtr, typeof(DamageNumbers).GetMethod("HookHitmark", BindingFlags.Static | BindingFlags.NonPublic).MethodHandle.GetFunctionPointer());
			}
		}

		private static int EstimateDamage(EPlayerHit type)
		{
			Player localPlayer = Player.LocalPlayer;
			bool flag = default;
			if (localPlayer == null)
			{
				flag = (null != null);
			}
			else
			{
				PlayerEquipment equipment = localPlayer.equipment;
				flag = (((equipment != null) ? equipment.asset : null) != null);
			}
			if (!flag)
			{
				return 0;
			}
			ItemAsset asset = localPlayer.equipment.asset;
			ItemGunAsset itemGunAsset = asset as ItemGunAsset;
			if (itemGunAsset != null)
			{
				switch (type)
				{
				case (EPlayerHit)1:
					return (int)itemGunAsset.playerDamageMultiplier.damage;
				case (EPlayerHit)2:
					return (int)(itemGunAsset.playerDamageMultiplier.damage * itemGunAsset.playerDamageMultiplier.skull);
				case (EPlayerHit)3:
					return (int)itemGunAsset.barricadeDamage;
				case (EPlayerHit)4:
					return 0;
				}
			}
			else
			{
				ItemMeleeAsset itemMeleeAsset = asset as ItemMeleeAsset;
				if (itemMeleeAsset != null)
				{
					switch (type)
					{
					case (EPlayerHit)1:
						return (int)itemMeleeAsset.playerDamageMultiplier.damage;
					case (EPlayerHit)2:
						return (int)(itemMeleeAsset.playerDamageMultiplier.damage * itemMeleeAsset.playerDamageMultiplier.skull);
					case (EPlayerHit)3:
						return (int)itemMeleeAsset.barricadeDamage;
					}
				}
			}
			return 0;
		}


		private static List<DamageNumbers.DmgEntry> _entries = new List<DamageNumbers.DmgEntry>();

		private static byte[] _hookSaved = new byte[14];

		private static IntPtr _hookOrigPtr;

		private static MethodInfo _hookOrig;

		private static bool _hooked;

		private static int _checkCd;

		private struct DmgEntry
		{
			public Vector3 pos;

			public Vector3 origin;

			public float birth;

			public int damage;

			public EPlayerHit type;
		}
	}
}
