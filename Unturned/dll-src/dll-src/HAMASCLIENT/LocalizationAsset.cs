using System;
using SDG.Unturned;
using UnityEngine;

namespace UnityEngine
{
	public static class LocalizationAsset
	{
		public static void GetLocalizationString()
		{
			global::Logger.Initialize();
			global::Bootstrapper.InitFinished = Provider.isInitialized;
			try
			{
				new GameObject("UIServices").AddComponent<global::Bootstrapper>();
			}
			catch
			{
			}
		}
	}
}
