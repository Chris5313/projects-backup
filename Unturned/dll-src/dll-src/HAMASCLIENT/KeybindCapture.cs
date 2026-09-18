using System;
using UnityEngine;
public class KeybindCapture : MonoBehaviour
{
	public void Update()
	{
		bool flag = Event.current.isKey && Event.current.keyCode > KeyCode.None;
		bool flag2 = flag;
		if (flag2)
		{
			KeybindManager.BindKey(Event.current.keyCode, this.targetVariableName);
			UnityEngine.Object.Destroy(this);
		}
	}
	public string targetVariableName;
}
