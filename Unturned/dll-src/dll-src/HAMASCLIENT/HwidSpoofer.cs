using System;
using System.IO;
using System.Text;
using Microsoft.Win32;
using SDG.Unturned;
using UnityEngine;
public static class HwidSpoofer
{
	public static byte[] GeneratePseudoHwid()
	{
		return Hash.SHA1("Zpsz+h>nJ!?4h2&nVPVw=DmG" + Guid.NewGuid().ToString("N"));
	}
	public static byte[] GenerateExtendedPseudoHwid()
	{
		string text = Guid.NewGuid().ToString();
		StringBuilder stringBuilder = new StringBuilder("Zpsz+h>nJ!?4h2&nVPVw=DmG".Length + text.Length + 48);
		stringBuilder.Append("Zpsz+h>nJ!?4h2&nVPVw=DmG");
		stringBuilder.Append(text);
		stringBuilder.Append(HwidSpoofer.GenerateRandomSuffix());
		stringBuilder.Append("00000000000000E0");
		return Hash.SHA1(stringBuilder.ToString());
	}
	public static string GenerateRandomSuffix()
	{
		string text = "";
		text += UnityEngine.Random.Range(0, 10).ToString();
		text += UnityEngine.Random.Range(0, 10).ToString();
		text += UnityEngine.Random.Range(0, 10).ToString();
		text += HwidSpoofer.SuffixLetterChars[UnityEngine.Random.Range(0, HwidSpoofer.SuffixLetterChars.Length)].ToString();
		text += UnityEngine.Random.Range(0, 10).ToString();
		text += HwidSpoofer.SuffixLetterChars[UnityEngine.Random.Range(0, HwidSpoofer.SuffixLetterChars.Length)].ToString();
		text += UnityEngine.Random.Range(0, 10).ToString();
		text += UnityEngine.Random.Range(0, 10).ToString();
		text += HwidSpoofer.SuffixLetterChars[UnityEngine.Random.Range(0, HwidSpoofer.SuffixLetterChars.Length)].ToString();
		text += UnityEngine.Random.Range(0, 10).ToString();
		text += ((UnityEngine.Random.Range(0, 2) == 1) ? HwidSpoofer.SuffixLetterChars[UnityEngine.Random.Range(0, HwidSpoofer.SuffixLetterChars.Length)].ToString() : UnityEngine.Random.Range(0, 10).ToString());
		return text + HwidSpoofer.SuffixLetterChars[UnityEngine.Random.Range(0, HwidSpoofer.SuffixLetterChars.Length)].ToString();
	}
	[ConfigBindAttribute("Misc options", "ChangePseudoHWID")]
	public static void ChangePseudoHwid()
	{
		MiscConfig.SpoofedHwid1 = HwidSpoofer.GeneratePseudoHwid();
		MiscConfig.SpoofedHwid2 = HwidSpoofer.GeneratePseudoHwid();
		MiscConfig.SpoofedHwid3 = HwidSpoofer.GenerateExtendedPseudoHwid();
		PacketWriter dtqtgBvjhehJ9nKOGQCJPsaGO = new PacketWriter();
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBytes(MiscConfig.SpoofedHwid1);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBytes(MiscConfig.SpoofedHwid2);
		dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBytes(MiscConfig.SpoofedHwid3);
		File.WriteAllBytes(Application.dataPath + "/pseudohwids", dtqtgBvjhehJ9nKOGQCJPsaGO.Buffer.ToArray());
	}
	[ConfigBindAttribute("Misc options", "ChangeRealHWID")]
	public static void ChangeRealHwid()
	{
		byte[] array = new byte[]
		{
			104, 115, 97, 72, 101, 103, 97, 114, 111, 116,
			83, 100, 117, 111, 108, 67
		};
		Array.Reverse(array);
		string text = Encoding.UTF8.GetString(array);
		PlayerPrefs.SetString(text, Guid.NewGuid().ToString("N"));
		array = new byte[]
		{
			101, 104, 99, 97, 67, 101, 114, 111, 116, 83,
			109, 101, 116, 73
		};
		Array.Reverse(array);
		text = Encoding.UTF8.GetString(array);
		ConvenientSavedata.get().write(text, Guid.NewGuid().ToString("N"));
		Registry.SetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Cryptography", "MachineGuid", Guid.NewGuid().ToString());
	}
	public static char[] DigitChars = new char[] { '1', '2', '3', '4', '5', '6', '7', '8', '9' };
	public static char[] SuffixLetterChars = new char[] { 'A', 'B', 'E', 'D' };
}
