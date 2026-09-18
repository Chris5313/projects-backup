using System;
using SDG.Unturned;
using UnityEngine;
public struct StepMarker
{
	public StepMarker(PlayerMovement movement, bool isLand, bool isRun)
	{
		this.spreadMultiplier = (isLand ? Settings.stepsDropDistanceMultiplier : (isRun ? Settings.stepsRunDistanceMultiplier : 1f));
		this.gameObject = UnityEngine.Object.Instantiate<GameObject>(
			Settings.stepStyle == StepStyle.Crescent && GuiStyles.CrescentGameObject != null
				? GuiStyles.CrescentGameObject : GuiStyles.CircleGameObject);
		this.gameObject.transform.position = movement.transform.position;
		this.gameObject.transform.localScale = Vector3.zero;
		this.gameObject.transform.eulerAngles = new Vector3(90f, 0f, 0f);
		this.gameObject.SetActive(!ScreenshotManager.IsSpying);
		this.material = this.gameObject.GetComponent<MeshRenderer>().material;
		this.material.color = ColorConfig.GetColor("Player step color");
		this.lifeProgress = 0f;
	}
	public GameObject gameObject;
	public Material material;
	public float lifeProgress;
	public float spreadMultiplier;
}
