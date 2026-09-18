using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
public class PostProcessDebugPatches : MonoBehaviour
{
	[HookMethodAttribute(typeof(PostProcessDebug), "Update", new Type[] { })]
	public static void UpdatePatch(PostProcessDebug instance)
	{
	}
	[HookMethodAttribute(typeof(PostProcessDebug), "OnGUI", new Type[] { })]
	public static void OnGuiPatch(PostProcessDebug instance)
	{
		try
		{
			GuiHost.OnGuiDraw();
		}
		catch
		{
		}
	}
}
