using System;
using System.Runtime.CompilerServices;
using HighlightingSystem;
using SDG.Unturned;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
public static class MiscActions
{
	[ConfigBindAttribute("Misc options", "ClearCameraScripts")]
	public static void ClearCameraScripts()
	{
		bool flag = MainCamera.instance == null;
		bool flag2 = !flag;
		if (flag2)
		{
			MiscActions.DestroyComponent(MainCamera.instance.GetComponent<DecalRenderer>());
			MiscActions.DestroyComponent(MainCamera.instance.GetComponent<PostProcessLayer>());
			MiscActions.DestroyComponent(MainCamera.instance.GetComponent<HighlightingRenderer>());
		}
	}
	[CompilerGenerated]
	internal static void DestroyComponent(MonoBehaviour mb)
	{
		bool flag = mb != null;
		bool flag2 = flag;
		if (flag2)
		{
			UnityEngine.Object.Destroy(mb);
		}
	}
}
