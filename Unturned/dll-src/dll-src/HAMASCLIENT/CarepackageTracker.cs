using System;
using System.Collections.Generic;
using SDG.Unturned;
using UnityEngine;
public class CarepackageTracker : MonoBehaviour
{
	private void Awake()
	{
		this.Id = CarepackageTracker.NextId;
		CarepackageTracker.NextId += 1;
		CarepackageTracker.Trackers.Add(this);
	}
	private void OnDestroy()
	{
		bool flag = base.GetComponent<Carepackage>() != null;
		bool flag2 = flag;
		if (flag2)
		{
			new GameObject("airhook")
			{
				transform = 
				{
					position = base.transform.position
				}
			}.AddComponent<AirhookMarker>();
		}
		CarepackageTracker.Trackers.Remove(this);
	}
	public static List<CarepackageTracker> Trackers = new List<CarepackageTracker>();
	private static ushort NextId = 0;
	public ushort Id = 0;
}
