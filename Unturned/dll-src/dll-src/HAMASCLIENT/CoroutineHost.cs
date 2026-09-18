using System;
using System.Collections;
using UnityEngine;
public class CoroutineHost : MonoBehaviour
{
	public static void Initialize()
	{
		CoroutineHost.Instance = Bootstrapper.HostGameObject.AddComponent<CoroutineHost>();
	}
	public static Coroutine StartHostCoroutine(IEnumerator coroutine)
	{
		return CoroutineHost.Instance.StartCoroutine(coroutine);
	}
	public static void StopHostCoroutine(Coroutine coroutine)
	{
		CoroutineHost.Instance.StopCoroutine(coroutine);
	}
	private static CoroutineHost Instance;
}
