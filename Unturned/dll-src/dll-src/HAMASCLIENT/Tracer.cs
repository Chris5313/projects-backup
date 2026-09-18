using System;
using UnityEngine;
public struct Tracer
{
	public Tracer(Vector3 startPoint, Vector3 endPoint, float deleteProgression, uint tracerIndex)
	{
		this.StartPoint = startPoint;
		this.EndPoint = endPoint;
		this.TracerIndex = tracerIndex;
		bool useGLTracers = Settings.useGLTracers;
		bool flag = useGLTracers;
		if (flag)
		{
			this.TracerObject = null;
			this.TracerMaterial = null;
		}
		else
		{
			this.TracerObject = UnityEngine.Object.Instantiate<GameObject>(GuiStyles.TracerGameObject);
			this.TracerObject.transform.position = Vector3.Lerp(startPoint, endPoint, 0.5f);
			float num = Settings.tracersWidth * 5f;
			this.TracerObject.transform.localScale = new Vector3(num, num, Vector3.Distance(startPoint, endPoint) * 5f);
			this.TracerObject.transform.LookAt(endPoint);
			this.TracerObject.transform.eulerAngles += new Vector3(0f, 0f, 90f);
			this.TracerObject.SetActive(!ScreenshotManager.IsSpying);
			this.TracerMaterial = this.TracerObject.GetComponent<MeshRenderer>().material;
			this.TracerMaterial.color = ColorConfig.GetColor("Tracers color");
		}
		this.DeleteProgression = deleteProgression;
	}
	public GameObject TracerObject;
	public Material TracerMaterial;
	public Vector3 StartPoint;
	public Vector3 EndPoint;
	public float DeleteProgression;
	public uint TracerIndex;
}
