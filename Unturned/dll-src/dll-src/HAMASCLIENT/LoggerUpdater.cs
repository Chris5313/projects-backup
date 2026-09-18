using System;
using UnityEngine;
public class LoggerUpdater : MonoBehaviour
{
	[InitializeAttribute]
	private static void Initialize()
	{
		Bootstrapper.HostGameObject.AddComponent<LoggerUpdater>();
	}
	private void Update()
	{
		Logger.UpdateUserLogs();
	}
}
