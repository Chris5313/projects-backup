using System;
using System.Linq;
using UnityEngine;
public class ConsoleTab : FeatureTabBase
{
	public override string GetName()
	{
		return "Console";
	}
	public override TabCount GetTabCounts()
	{
		return TabCount.Three;
	}
	public override void DoTab(TabCount tc)
	{
		bool flag = tc == TabCount.One;
		if (flag)
		{
			base.DrawSectionHeader("Unity logs");
			this.ScrollPositions[0] = GUILayout.BeginScrollView(this.ScrollPositions[0], Array.Empty<GUILayoutOption>());
			foreach (string text in Logger.UnityLogs.Skip<string>(Math.Max(0, Logger.UnityLogs.Count - 100)))
			{
				GUILayout.Label(text, Array.Empty<GUILayoutOption>());
			}
			GUILayout.EndScrollView();
		}
		else
		{
			bool flag2 = tc == TabCount.Two;
			if (flag2)
			{
				base.DrawSectionHeader("Unturned logs");
				this.ScrollPositions[1] = GUILayout.BeginScrollView(this.ScrollPositions[1], Array.Empty<GUILayoutOption>());
				foreach (string text2 in Logger.UnturnedLogs.Skip<string>(Math.Max(0, Logger.UnturnedLogs.Count - 100)))
				{
					GUILayout.Label(text2, Array.Empty<GUILayoutOption>());
				}
				GUILayout.EndScrollView();
			}
			else
			{
				base.DrawSectionHeader("MC Client Logs");
				this.ScrollPositions[2] = GUILayout.BeginScrollView(this.ScrollPositions[2], Array.Empty<GUILayoutOption>());
				foreach (string text3 in Logger.ClientLogs.Skip<string>(Math.Max(0, Logger.ClientLogs.Count - 100)))
				{
					GUILayout.Label(text3, Array.Empty<GUILayoutOption>());
				}
				GUILayout.EndScrollView();
			}
		}
	}
	public Vector2[] ScrollPositions = new Vector2[3];
}
