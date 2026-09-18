using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace gatyware
{
	[Obfuscation(Exclude = true)]
	public static class Runtime
	{
		private static void Log(string msg)
		{
			try
			{
				File.AppendAllText("C:\\Users\\Public\\mc_debug.txt", msg + "\r\n");
			}
			catch
			{
			}
		}

		public static void Init()
		{
			try
			{
				File.WriteAllText("C:\\Users\\Public\\mc_debug.txt", "");
			}
			catch
			{
			}
			if (Runtime._booted)
			{
				return;
			}
			Runtime._booted = true;
			Runtime.Log("Init on main thread");
			try
			{
				GameObject gameObject = new GameObject("mc_h");
				UnityEngine.Object.DontDestroyOnLoad(gameObject);
				gameObject.AddComponent<Ticker>();
				Runtime.Log("Ticker added, menu ready — press F1");
				Hooks.Install();
				AntiSpy.Init();
				QuestExploit.Init();
				ProtonBypass.TryInstall();
			}
			catch (Exception ex)
			{
				Runtime.Log("EXCEPTION: " + ex.GetType().Name + ": " + ex.Message);
			}
		}

		public static void Trace(string msg)
		{
			Runtime.Log(msg);
		}

		public static void Eject()
		{
			try
			{
				Hooks.Uninstall();
				AntiSpy.Uninstall();
				Chams.CleanupAll();
				StreamProofOverlay.Stop();
				State.Open = false;
				GameObject mc = GameObject.Find("mc_h");
				if (mc != null) UnityEngine.Object.Destroy(mc);
				GameObject spy = GameObject.Find("mc_spy");
				if (spy != null) UnityEngine.Object.Destroy(spy);
				Runtime._booted = false;
				Runtime.Log("Ejected");
			}
			catch (Exception ex)
			{
				Runtime.Log("Eject error: " + ex.Message);
			}
		}

		private const string LogPath = "C:\\Users\\Public\\mc_debug.txt";

		private static bool _booted;
	}
}
