using System;
using SDG.Unturned;
using UnityEngine;
public class FpsCounter : MonoBehaviour
{
	[InitializeAttribute]
	private static void Initialize()
	{
		Provider.onClientDisconnected = (Provider.ClientDisconnected)Delegate.Combine(Provider.onClientDisconnected, new Provider.ClientDisconnected(delegate
		{
			FpsCounter.SessionSeconds = 0;
		}));
	}
	private void Update()
	{
		this.TimerAccumulator += Time.deltaTime;
		this.FrameCounter++;
		bool flag = this.TimerAccumulator > 1f;
		bool flag2 = flag;
		if (flag2)
		{
			FpsCounter.Fps = this.FrameCounter;
			this.FrameCounter = 0;
			this.TimerAccumulator -= 1f;
			bool flag3 = Provider.isConnected && Player.player != null;
			bool flag4 = flag3;
			if (flag4)
			{
				FpsCounter.SessionSeconds += 1;
			}
		}
	}
	public static int Fps;
	public static ushort SessionSeconds;
	private int FrameCounter = 0;
	private float TimerAccumulator = 0f;
}
