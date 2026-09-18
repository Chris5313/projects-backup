using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
public class Logger
{
	public static string GetTimestamp()
	{
		DateTime now = DateTime.Now;
		string text = ((now.Second < 10) ? ("0" + now.Second.ToString()) : now.Second.ToString());
		string text2 = ((now.Hour < 10) ? ("0" + now.Hour.ToString()) : now.Hour.ToString());
		string text3 = ((now.Minute < 10) ? ("0" + now.Minute.ToString()) : now.Minute.ToString());
		return string.Concat(new string[] { text2, ":", text3, ":", text });
	}
	public static void TrimLogs()
	{
		bool flag = Logger.AllLogs.Count > 3000;
		if (flag)
		{
			Logger.AllLogs.Remove(Logger.AllLogs.First<string>());
		}
		bool flag2 = Logger.UnityLogs.Count > 3000;
		if (flag2)
		{
			Logger.UnityLogs.Remove(Logger.UnityLogs.First<string>());
		}
		bool flag3 = Logger.ClientLogs.Count > 3000;
		if (flag3)
		{
			Logger.ClientLogs.Remove(Logger.ClientLogs.First<string>());
		}
		bool flag4 = Logger.UnturnedLogs.Count > 3000;
		if (flag4)
		{
			Logger.UnturnedLogs.Remove(Logger.UnturnedLogs.First<string>());
		}
	}
	public static void LogUser(string s)
	{
		bool enableUserLogger = Settings.enableUserLogger;
		if (enableUserLogger)
		{
			Logger.UserLogs.Add(new ValueTuple<string, float>(s, 0f));
		}
	}
	public static void UpdateUserLogs()
	{
		bool enableUserLogger = Settings.enableUserLogger;
		if (enableUserLogger)
		{
			for (int i = 0; i < Logger.UserLogs.Count; i++)
			{
				Logger.UserLogs[i] = new ValueTuple<string, float>(Logger.UserLogs[i].Item1, Logger.UserLogs[i].Item2 + Time.deltaTime);
				bool flag = Logger.UserLogs[i].Item2 > 5f;
				if (flag)
				{
					Logger.UserLogs.RemoveAt(i);
				}
			}
		}
	}
	public static void LogUnturned(string s)
	{
		try
		{
			Logger.UnturnedLogs.Add("[" + Logger.GetTimestamp() + "] [Unturned] " + s);
			Logger.AllLogs.Add("[" + Logger.GetTimestamp() + "] [Unturned] " + s);
			Logger.TrimLogs();
			bool dtfo7I1kbT4SC4NokdAQjG17G = Logger.WriteToFile;
			if (dtfo7I1kbT4SC4NokdAQjG17G)
			{
				try
				{
					File.AppendAllText(Application.dataPath + "/logs.txt", Logger.AllLogs.ElementAt<string>(Logger.AllLogs.Count - 1) + Environment.NewLine);
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
	}
	public static void OnUnityLog(string text, string stack, LogType type)
	{
		try
		{
			Logger.UnityLogs.Add("[" + Logger.GetTimestamp() + "] [Unity] " + text);
			Logger.AllLogs.Add("[" + Logger.GetTimestamp() + "] [Unity] " + text);
			Logger.TrimLogs();
			bool dtfo7I1kbT4SC4NokdAQjG17G = Logger.WriteToFile;
			if (dtfo7I1kbT4SC4NokdAQjG17G)
			{
				try
				{
					File.AppendAllText(Application.dataPath + "/logs.txt", Logger.AllLogs.ElementAt<string>(Logger.AllLogs.Count - 1) + Environment.NewLine);
				}
				catch
				{
				}
			}
			bool flag = type <= LogType.Warning || type == LogType.Exception;
			if (flag)
			{
				Logger.UnityLogs.Add("[" + Logger.GetTimestamp() + "] [Unity] Stack: " + stack);
				Logger.AllLogs.Add("[" + Logger.GetTimestamp() + "] [Unity] Stack: " + stack);
				Logger.TrimLogs();
				bool dtfo7I1kbT4SC4NokdAQjG17G2 = Logger.WriteToFile;
				if (dtfo7I1kbT4SC4NokdAQjG17G2)
				{
					try
					{
						File.AppendAllText(Application.dataPath + "/logs.txt", Logger.AllLogs.ElementAt<string>(Logger.AllLogs.Count - 1) + Environment.NewLine);
					}
					catch
					{
					}
				}
			}
		}
		catch
		{
		}
	}
	public static void LogClient(string s)
	{
		try
		{
			Logger.ClientLogs.Add("[" + Logger.GetTimestamp() + "] [Client] " + s);
			Logger.AllLogs.Add("[" + Logger.GetTimestamp() + "] [Client] " + s);
			Logger.TrimLogs();
			bool dtfo7I1kbT4SC4NokdAQjG17G = Logger.WriteToFile;
			if (dtfo7I1kbT4SC4NokdAQjG17G)
			{
				try
				{
					File.AppendAllText(Application.dataPath + "/logs.txt", Logger.AllLogs.ElementAt<string>(Logger.AllLogs.Count - 1) + Environment.NewLine);
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
	}
	public static void Initialize()
	{
		bool dtfo7I1kbT4SC4NokdAQjG17G = Logger.WriteToFile;
		if (dtfo7I1kbT4SC4NokdAQjG17G)
		{
			File.WriteAllText(Application.dataPath + "/logs.txt", "");
		}
		Application.logMessageReceived += Logger.OnUnityLog;
		Application.quitting += delegate
		{
			string text = "";
			foreach (string text2 in Logger.AllLogs)
			{
				text = text + text2 + Environment.NewLine;
			}
			try
			{
				File.WriteAllText(Application.dataPath + "/logs.txt", text);
			}
			catch
			{
			}
		};
	}
	public static HashSet<string> AllLogs = new HashSet<string>();
	public static HashSet<string> UnityLogs = new HashSet<string>();
	public static HashSet<string> ClientLogs = new HashSet<string>();
	public static HashSet<string> UnturnedLogs = new HashSet<string>();
	public static List<ValueTuple<string, float>> UserLogs = new List<ValueTuple<string, float>>();
	private static bool WriteToFile = true;
}
