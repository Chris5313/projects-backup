using System;
using SDG.Unturned;
using UnityEngine;
public static class EnumExtensions
{
	public static T Next<T>(this T src) where T : struct
	{
		bool flag = !typeof(T).IsEnum;
		if (flag)
		{
			throw new ArgumentException(string.Format("Argument {0} is not an Enum", typeof(T).FullName));
		}
		T[] array = (T[])Enum.GetValues(src.GetType());
		int num = Array.IndexOf<T>(array, src) + 1;
		bool flag2 = array.Length != num;
		T t;
		if (flag2)
		{
			t = array[num];
		}
		else
		{
			t = array[0];
		}
		return t;
	}
	public static T Previous<T>(this T src) where T : struct
	{
		bool flag = !typeof(T).IsEnum;
		if (flag)
		{
			throw new ArgumentException(string.Format("Argument {0} is not an Enum", typeof(T).FullName));
		}
		T[] array = (T[])Enum.GetValues(src.GetType());
		int num = Array.IndexOf<T>(array, src) - 1;
		bool flag2 = 0 <= num;
		T t;
		if (flag2)
		{
			t = array[num];
		}
		else
		{
			t = array[array.Length - 1];
		}
		return t;
	}
	public static ELightingVision ToLightingVision(this NightVisionType nvt)
	{
		ELightingVision elightingVision;
		switch (nvt)
		{
		case NightVisionType.Military:
			elightingVision = ELightingVision.MILITARY;
			break;
		case NightVisionType.Civilian:
			elightingVision = ELightingVision.CIVILIAN;
			break;
		case NightVisionType.Custom:
			elightingVision = ELightingVision.MILITARY;
			break;
		default:
			elightingVision = ELightingVision.NONE;
			break;
		}
		return elightingVision;
	}
	public static string ToDisplayNameOutlineMode(this TextOutlineStyle tom)
	{
		string text;
		switch (tom)
		{
		case TextOutlineStyle.RightDownSided:
			text = "Right down sided";
			break;
		case TextOutlineStyle.RightTopSided:
			text = "Right top sided";
			break;
		case TextOutlineStyle.LeftTopSided:
			text = "Left top sided";
			break;
		case TextOutlineStyle.LeftDownSided:
			text = "Left down sided";
			break;
		case TextOutlineStyle.FourSided:
			text = "Four sided";
			break;
		default:
			text = "None";
			break;
		}
		return text;
	}
	public static string ToDisplayNameTextCase(this TextCase tct)
	{
		TextCase dveWYqH4HEAYXL5Vu3HrmvSnF = tct;
		bool flag = dveWYqH4HEAYXL5Vu3HrmvSnF != TextCase.UpperCase;
		string text;
		if (flag)
		{
			bool flag2 = dveWYqH4HEAYXL5Vu3HrmvSnF != TextCase.LowerCase;
			if (flag2)
			{
				text = tct.ToString();
			}
			else
			{
				text = "Lower case";
			}
		}
		else
		{
			text = "Upper case";
		}
		return text;
	}
	public static ELimb ToLimb(this Limb limb)
	{
		ELimb elimb;
		switch (limb)
		{
		case Limb.Head:
			elimb = ELimb.SKULL;
			break;
		case Limb.Body:
			elimb = ELimb.SPINE;
			break;
		case Limb.LeftLeg:
			elimb = ELimb.LEFT_LEG;
			break;
		case Limb.RightLeg:
			elimb = ELimb.RIGHT_LEG;
			break;
		case Limb.LeftHand:
			elimb = ELimb.LEFT_HAND;
			break;
		case Limb.RightHand:
			elimb = ELimb.RIGHT_HAND;
			break;
		case Limb.Random:
		{
			bool flag = UnityEngine.Random.Range(0, 100) < AimbotConfig.randomLimbHeadHitChance;
			if (flag)
			{
				elimb = ELimb.SKULL;
			}
			else
			{
				ELimb[] array = new ELimb[]
				{
					ELimb.SPINE,
					ELimb.LEFT_LEG,
					ELimb.RIGHT_LEG,
					ELimb.LEFT_HAND,
					ELimb.RIGHT_HAND
				};
				elimb = array[UnityEngine.Random.Range(0, array.Length)];
			}
			break;
		}
		default:
			elimb = ELimb.SPINE;
			break;
		}
		return elimb;
	}
	public static TargetType ToTargetType(this object o)
	{
		bool flag = o is Player;
		TargetType dr5qliNNQh3jZolh9fn7SFNyi;
		if (flag)
		{
			dr5qliNNQh3jZolh9fn7SFNyi = TargetType.Player;
		}
		else
		{
			bool flag2 = o is InteractableClaim;
			if (flag2)
			{
				dr5qliNNQh3jZolh9fn7SFNyi = TargetType.ClaimFlags;
			}
			else
			{
				bool flag3 = o is InteractableBed;
				if (flag3)
				{
					dr5qliNNQh3jZolh9fn7SFNyi = TargetType.Beds;
				}
				else
				{
					bool flag4 = o is InteractableStorage;
					if (flag4)
					{
						dr5qliNNQh3jZolh9fn7SFNyi = TargetType.Storages;
					}
					else
					{
						bool flag5 = o is Zombie;
						if (flag5)
						{
							dr5qliNNQh3jZolh9fn7SFNyi = TargetType.Zombies;
						}
						else
						{
							bool flag6 = o is Animal;
							if (flag6)
							{
								dr5qliNNQh3jZolh9fn7SFNyi = TargetType.Animals;
							}
							else
							{
								bool flag7 = o is InteractableVehicle;
								if (flag7)
								{
									dr5qliNNQh3jZolh9fn7SFNyi = TargetType.Vehicles;
								}
								else
								{
									dr5qliNNQh3jZolh9fn7SFNyi = TargetType.ClaimFlags;
								}
							}
						}
					}
				}
			}
		}
		return dr5qliNNQh3jZolh9fn7SFNyi;
	}
	public static string ToDisplayNameHwidType(this HwidMode hst)
	{
		switch (hst)
		{
		case HwidMode.SendRandomHWID:
			return "Send random HWID";
		case HwidMode.UsePseudoHWID:
			return "Use pseudo HWID";
		case HwidMode.SendRealHWID:
			return "Send real HWID";
		case HwidMode.SendNoHWID:
			return "Don't send HWID";
		}
		return hst.ToString();
	}
	public static string ToDisplayNameSpyType(this AvatarSpyMode st)
	{
		string text;
		switch (st)
		{
		case AvatarSpyMode.DontOverride:
			text = "Dont override";
			break;
		case AvatarSpyMode.SendCustomImage:
			text = "Send custom image";
			break;
		case AvatarSpyMode.SpyWithDelay:
			text = "Spy with delay";
			break;
		case AvatarSpyMode.SpyInFourFrames:
			text = "Bypass spy in four frames";
			break;
		case AvatarSpyMode.SpyInOneFrame:
			text = "Bypass spy in one frame";
			break;
		case AvatarSpyMode.DeclineSpy:
			text = "Decline spy";
			break;
		default:
			text = st.ToString();
			break;
		}
		return text;
	}
	public static string ToDisplayNameSilentAimType(this SilentAimType sat)
	{
		string text;
		switch (sat)
		{
		case SilentAimType.Aim:
			text = "Silent";
			break;
		case SilentAimType.Distance:
			text = "Distance";
			break;
		case SilentAimType.Sphere:
			text = "Sphere";
			break;
		default:
			text = sat.ToString();
			break;
		}
		return text;
	}
	public static string ToDisplayNameMoveType(this MoveType sat)
	{
		string text;
		switch (sat)
		{
		case MoveType.WalkLeft:
			text = "Left";
			break;
		case MoveType.WalkRight:
			text = "Right";
			break;
		case MoveType.WalkBack:
			text = "Backwards";
			break;
		case MoveType.TwoTactSpin:
			text = "180 Spin";
			break;
		case MoveType.FourTactSpin:
			text = "360 Spin";
			break;
		case MoveType.Jitter:
			text = "Jitter";
			break;
		case MoveType.Continuous:
			text = "Continuous";
			break;
		case MoveType.RandomPitch:
			text = "Random Pitch";
			break;
		case MoveType.AntiAim:
			text = "Anti-Aim";
			break;
		default:
			text = sat.ToString();
			break;
		}
		return text;
	}
	public static string DvehSpinTypeName(this DxVehSpinType sat)
	{
		string text;
		switch (sat)
		{
		case DxVehSpinType.Flip180:
			text = "180 Flip";
			break;
		case DxVehSpinType.Steps360:
			text = "360 Steps";
			break;
		case DxVehSpinType.Continuous:
			text = "Continuous";
			break;
		case DxVehSpinType.Jitter:
			text = "Jitter";
			break;
		default:
			text = sat.ToString();
			break;
		}
		return text;
	}
	public static string ToDisplayNameLimb(this Limb sat)
	{
		string text;
		switch (sat)
		{
		case Limb.Head:
			text = "Head";
			break;
		case Limb.Body:
			text = "Body";
			break;
		case Limb.LeftLeg:
			text = "Left leg";
			break;
		case Limb.RightLeg:
			text = "Right leg";
			break;
		case Limb.LeftHand:
			text = "Left hand";
			break;
		case Limb.RightHand:
			text = "Right hand";
			break;
		case Limb.Random:
			text = "Random";
			break;
		default:
			text = sat.ToString();
			break;
		}
		return text;
	}
	public static string ToDisplayNameBallisticStepType(this ProjectileType tt)
	{
		ProjectileType d5VsESizw2NoAi19PtCEVYVgW = tt;
		bool flag = d5VsESizw2NoAi19PtCEVYVgW != ProjectileType.BallisticProceed;
		string text;
		if (flag)
		{
			bool flag2 = d5VsESizw2NoAi19PtCEVYVgW != ProjectileType.BallisticMoved;
			if (flag2)
			{
				text = tt.ToString();
			}
			else
			{
				text = "Ballistic moved";
			}
		}
		else
		{
			text = "Ballistic proceed";
		}
		return text;
	}
	public static string AppendTypeSuffix(this string s, SaveValueType vt)
	{
		return s + "->" + vt.ToString() + "s";
	}
}
