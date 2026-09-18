using System;
using SDG.Unturned;
public class HwidSpoofHook
{
	[HookMethodAttribute(typeof(LocalHwid), "GetHwids", new Type[] { })]
	public static byte[][] GetHwidsHook()
	{
		switch (MiscConfig.hwidType)
		{
		case HwidMode.SendRandomHWID:
			return new byte[][]
			{
				BitConverter.GetBytes(Provider.client.m_SteamID + 87654321012345678UL),
				BitConverter.GetBytes(Provider.client.m_SteamID + 76543210123456789UL),
				BitConverter.GetBytes(Provider.client.m_SteamID + 65432101234567890UL)
			};
		case HwidMode.UsePseudoHWID:
			return new byte[][]
			{
				MiscConfig.SpoofedHwid1,
				MiscConfig.SpoofedHwid2,
				MiscConfig.SpoofedHwid3
			};
		case HwidMode.SendLinuxPseudoHWID:
			return new byte[][]
			{
				MiscConfig.SpoofedHwid1,
				MiscConfig.SpoofedHwid2
			};
		case HwidMode.SendNoHWID:
			return new byte[0][];
		}
		return OverrideManager.CallOriginalInstance<byte[][]>(null, Array.Empty<object>());
	}
	public static char[] PseudoHwidDigits = new char[] { '1', '2', '3', '4', '5', '6', '7', '8', '9' };
	public static char[] PseudoHwidLetters = new char[] { 'A', 'B', 'E', 'D' };
}
