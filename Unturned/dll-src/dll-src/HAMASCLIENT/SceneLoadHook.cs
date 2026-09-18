using System;
using SDG.Unturned;
using UnityEngine.SceneManagement;
public class SceneLoadHook
{
	[HookMethodAttribute(typeof(Level), "onSceneLoaded", new Type[] { })]
	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		SceneLoadHook.currentLevelIndex = scene.buildIndex;
		OverrideManager.CallOriginal(ReflectionUtil.GetFieldValue(typeof(Level), "instance", null), new object[] { scene, mode });
	}
	public static int currentLevelIndex;
}
