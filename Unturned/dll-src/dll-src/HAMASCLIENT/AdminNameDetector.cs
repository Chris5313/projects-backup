using System;
public static class AdminNameDetector
{
	public static bool ContainsAdminKeyword(this string s)
	{
		string text = s.ToLower();
		foreach (string text2 in AdminNameDetector.adminKeywords)
		{
			bool flag = text.Contains(text2);
			bool flag2 = flag;
			if (flag2)
			{
				return true;
			}
		}
		return false;
	}
	public static string[] adminKeywords = new string[] { "admin", "moder", "?????", "?????", "?????????", "owner", "????????" };
}
