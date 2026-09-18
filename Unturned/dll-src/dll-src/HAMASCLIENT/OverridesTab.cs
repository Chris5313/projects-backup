using System;
using UnityEngine;
public class OverridesTab : FeatureTabBase
{
	public override string GetName()
	{
		return "Overrides";
	}
	public override TabCount GetTabCounts()
	{
		return TabCount.One;
	}
	public override void DoTab(TabCount tc)
	{
		try
		{
			GUILayout.Label("Displays only successfull overides, so you can disable only workable overrides", Array.Empty<GUILayoutOption>());
			this.ScrollPosition = GUILayout.BeginScrollView(this.ScrollPosition, Array.Empty<GUILayoutOption>());
			foreach (DetourHook d9Tzj1SkWAoyZl0owsCmbQM3M in OverrideManager.OverridesByToken.Values)
			{
				string text = d9Tzj1SkWAoyZl0owsCmbQM3M.originalMethod.DeclaringType.Name + "." + d9Tzj1SkWAoyZl0owsCmbQM3M.originalMethod.Name;
				bool flag = !(text == "File.InternalWriteAllBytes") && !(text == "Type.GetFields") && !(text == "Type.GetTypes") && !(text == "MethodBase.GetMethodBody") && MenuGuiHelper.Button((d9Tzj1SkWAoyZl0owsCmbQM3M.isApplied ? "[Overrided] " : "[Reverted] ") + text, -1, true, null);
				if (flag)
				{
					bool dslkeUnnABbJwjQrJAIPWkhOS = d9Tzj1SkWAoyZl0owsCmbQM3M.isApplied;
					if (dslkeUnnABbJwjQrJAIPWkhOS)
					{
						d9Tzj1SkWAoyZl0owsCmbQM3M.Revert();
					}
					else
					{
						d9Tzj1SkWAoyZl0owsCmbQM3M.Apply();
					}
				}
			}
			GUILayout.EndScrollView();
		}
		catch
		{
		}
	}
	public Vector2 ScrollPosition = Vector2.zero;
}
