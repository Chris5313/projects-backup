using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SDG.Unturned;
using UnityEngine;
using UnityEngine.Rendering;
public static class DTungTungSahurModel
{
	// (get) Token: 0x060003FA RID: 1018 RVA: 0x00042D40 File Offset: 0x00040F40
	private static string CurrentFBXResourceName
	{
		get
		{
			DCustomModelType dcustomModelType = MiscConfig.customModelType;
			string text;
			if (dcustomModelType == DCustomModelType.BenjaminNetanyahu)
			{
				text = "HAMASCLIENT.benjamin_model.fbx";
			}
			else
			{
				text = "HAMASCLIENT.tungtung_model.fbx";
			}
			return text;
		}
	}
	// (get) Token: 0x060003FB RID: 1019 RVA: 0x00042D70 File Offset: 0x00040F70
	private static string CurrentTextureResourceName
	{
		get
		{
			DCustomModelType dcustomModelType2 = MiscConfig.customModelType;
			string text;
			if (dcustomModelType2 == DCustomModelType.BenjaminNetanyahu)
			{
				text = "HAMASCLIENT.benjamin_model_texture.png";
			}
			else
			{
				text = "HAMASCLIENT.tungtung_model_texture.png";
			}
			return text;
		}
	}
	// (get) Token: 0x060003FC RID: 1020 RVA: 0x00042DA0 File Offset: 0x00040FA0
	private static string CurrentFallbackTextureResourceName
	{
		get
		{
			DCustomModelType dcustomModelType3 = MiscConfig.customModelType;
			string text;
			if (dcustomModelType3 == DCustomModelType.BenjaminNetanyahu)
			{
				text = "HAMASCLIENT.benjamin_model_texture.png";
			}
			else
			{
				text = "HAMASCLIENT.tungtungsahur_texture.png";
			}
			return text;
		}
	}
	// (get) Token: 0x060003FD RID: 1021 RVA: 0x00042DD0 File Offset: 0x00040FD0
	private static string CurrentModelRootName
	{
		get
		{
			DCustomModelType dcustomModelType4 = MiscConfig.customModelType;
			string text;
			if (dcustomModelType4 == DCustomModelType.BenjaminNetanyahu)
			{
				text = "BenjaminNetanyahuModel";
			}
			else
			{
				text = "TungTungSahurModel";
			}
			return text;
		}
	}
	// (get) Token: 0x060003FE RID: 1022 RVA: 0x00042E00 File Offset: 0x00041000
	private static string CurrentLogTag
	{
		get
		{
			DCustomModelType dcustomModelType5 = MiscConfig.customModelType;
			string text;
			if (dcustomModelType5 == DCustomModelType.BenjaminNetanyahu)
			{
				text = "[BenjaminNetanyahu]";
			}
			else
			{
				text = "[TungTungSahur]";
			}
			return text;
		}
	}
	public static void Update()
	{
		bool flag = !MiscConfig.replaceLocalPlayerModel;
		if (flag)
		{
			bool flag2 = DTungTungSahurModel.modelInstance != null;
			if (flag2)
			{
				DTungTungSahurModel.CleanupModel();
			}
		}
		else
		{
			bool flag3 = Player.player == null;
			if (flag3)
			{
				DTungTungSahurModel.CleanupModel();
			}
			else
			{
				bool flag4 = ScreenshotManager.IsSpying && !MiscConfig.showCustomModelOnSpy;
				if (flag4)
				{
					bool flag5 = DTungTungSahurModel.modelInstance != null;
					if (flag5)
					{
						DTungTungSahurModel.modelInstance.SetActive(false);
					}
					DTungTungSahurModel.ShowOriginalRenderers();
				}
				else
				{
					bool flag6 = DTungTungSahurModel.modelInstance == null;
					if (flag6)
					{
						DTungTungSahurModel.CreateModel();
					}
					bool flag7 = DTungTungSahurModel.modelInstance == null;
					if (!flag7)
					{
						DTungTungSahurModel.ApplyModelScale();
						bool flag8 = Player.player.look.perspective == EPlayerPerspective.FIRST;
						bool flag9 = flag8;
						if (flag9)
						{
							DTungTungSahurModel.modelInstance.SetActive(false);
							DTungTungSahurModel.ShowOriginalRenderersExceptBody();
						}
						else
						{
							DTungTungSahurModel.modelInstance.SetActive(true);
							DTungTungSahurModel.HideOriginalRenderers();
						}
					}
				}
			}
		}
	}
	public static void LateUpdate()
	{
		bool flag = DTungTungSahurModel.modelInstance == null || !DTungTungSahurModel.modelInstance.activeSelf;
		if (!flag)
		{
			bool flag2 = Player.player == null;
			if (!flag2)
			{
				DTungTungSahurModel.modelInstance.transform.position = Player.player.transform.position;
				float num = Player.player.look.yaw;
				bool flag3 = PlayerInputOverride.PlayerSpinActive && !ScreenshotManager.IsSpying;
				if (flag3)
				{
					float num2 = (MiscConfig.playerSpinbotDesync ? MiscConfig.playerSpinbotDesyncAmount : 0f);
					float num3 = (MiscConfig.showMoveModifying ? (PlayerInputOverride.PlayerServerYaw - Player.player.look.yaw + num2) : num2);
					num = Player.player.look.yaw + num3;
				}
				DTungTungSahurModel.modelInstance.transform.rotation = Quaternion.Euler(0f, num + 180f, 0f);
			}
		}
	}
	private static void CreateModel()
	{
		try
		{
			DTungTungSahurModel.BuildFBXModel();
		}
		catch (Exception ex)
		{
			Debug.LogWarning(string.Concat(new string[]
			{
				DTungTungSahurModel.CurrentLogTag,
				" Model creation failed: ",
				ex.Message,
				"\n",
				ex.StackTrace
			}));
		}
	}
	private static void EnsureResourcesLoaded()
	{
		bool flag = DTungTungSahurModel.resourcesLoadAttempted;
		if (!flag)
		{
			DTungTungSahurModel.resourcesLoadAttempted = true;
			string currentLogTag = DTungTungSahurModel.CurrentLogTag;
			string currentFBXResourceName = DTungTungSahurModel.CurrentFBXResourceName;
			string currentTextureResourceName = DTungTungSahurModel.CurrentTextureResourceName;
			string currentFallbackTextureResourceName = DTungTungSahurModel.CurrentFallbackTextureResourceName;
			DTungTungSahurModel.sharedMesh = null;
			DFBXLoader.FBXMesh fbxmesh = DFBXLoader.LoadFromEmbeddedResource(currentFBXResourceName);
			bool flag2 = fbxmesh != null;
			if (flag2)
			{
				DTungTungSahurModel.sharedMesh = new Mesh();
				DTungTungSahurModel.sharedMesh.name = DTungTungSahurModel.CurrentModelRootName + "FBXMesh";
				bool flag3 = fbxmesh.vertices.Length > 65000;
				if (flag3)
				{
					DTungTungSahurModel.sharedMesh.indexFormat = IndexFormat.UInt32;
				}
				DTungTungSahurModel.sharedMesh.vertices = fbxmesh.vertices;
				DTungTungSahurModel.sharedMesh.triangles = fbxmesh.triangles;
				DTungTungSahurModel.sharedMesh.normals = fbxmesh.normals;
				DTungTungSahurModel.sharedMesh.uv = fbxmesh.uv;
				DTungTungSahurModel.sharedMesh.RecalculateBounds();
				DTungTungSahurModel.sharedMesh.hideFlags = HideFlags.HideInHierarchy;
				DTungTungSahurModel.modelRotationCorrection = DTungTungSahurModel.CurrentModelRotationCorrection;
				DTungTungSahurModel.meshMinY = DTungTungSahurModel.ComputeRotatedMeshMinY(DTungTungSahurModel.modelRotationCorrection);
				Debug.Log(string.Concat(new string[]
				{
					currentLogTag,
					" FBX mesh loaded: ",
					fbxmesh.vertices.Length.ToString(),
					" verts, ",
					(fbxmesh.triangles.Length / 3).ToString(),
					" tris, bounds size=",
					fbxmesh.boundsSize.ToString("F3"),
					", meshMinY=",
					DTungTungSahurModel.meshMinY.ToString("F4")
				}));
			}
			else
			{
				Debug.LogWarning(currentLogTag + " Failed to load FBX mesh from resource: " + currentFBXResourceName);
			}
			DTungTungSahurModel.sharedTexture = DTungTungSahurModel.LoadEmbeddedTexture(currentTextureResourceName);
			bool flag4 = DTungTungSahurModel.sharedTexture == null;
			if (flag4)
			{
				Debug.LogWarning(currentLogTag + " Primary texture not found, trying fallback...");
				DTungTungSahurModel.sharedTexture = DTungTungSahurModel.LoadEmbeddedTexture(currentFallbackTextureResourceName);
			}
		}
	}
	private static void BuildFBXModel()
	{
		DTungTungSahurModel.EnsureResourcesLoaded();
		string currentLogTag = DTungTungSahurModel.CurrentLogTag;
		bool flag = DTungTungSahurModel.sharedMesh == null;
		if (flag)
		{
			Debug.LogWarning(currentLogTag + " No mesh available, cannot build model");
		}
		else
		{
			int playerModelLayer = DTungTungSahurModel.GetPlayerModelLayer();
			Texture2D texture2D = DTungTungSahurModel.sharedTexture;
			bool flag2 = DTungTungSahurModel.sharedMaterial == null;
			if (flag2)
			{
				DTungTungSahurModel.sharedMaterial = DTungTungSahurModel.CreateMaterial(texture2D);
			}
			string text = ((DTungTungSahurModel.sharedMaterial != null && DTungTungSahurModel.sharedMaterial.shader != null) ? DTungTungSahurModel.sharedMaterial.shader.name : "none");
			Debug.Log(string.Concat(new string[]
			{
				currentLogTag,
				" Creating FBX model - layer: ",
				playerModelLayer.ToString(),
				", mesh verts: ",
				DTungTungSahurModel.sharedMesh.vertexCount.ToString(),
				", texture: ",
				(texture2D != null) ? "loaded" : "null",
				", shader: ",
				text
			}));
			float modelScale = DTungTungSahurModel.GetModelScale();
			GameObject gameObject = new GameObject(DTungTungSahurModel.CurrentModelRootName);
			gameObject.transform.position = Player.player.transform.position;
			gameObject.transform.rotation = Quaternion.Euler(0f, Player.player.look.yaw + 180f, 0f);
			gameObject.hideFlags = HideFlags.HideInHierarchy;
			GameObject gameObject2 = new GameObject("CustomModel_Mesh");
			gameObject2.transform.SetParent(gameObject.transform, false);
			gameObject2.transform.localPosition = new Vector3(0f, -DTungTungSahurModel.meshMinY * modelScale, 0f);
			gameObject2.transform.localRotation = DTungTungSahurModel.modelRotationCorrection;
			gameObject2.transform.localScale = Vector3.one * modelScale;
			gameObject2.hideFlags = HideFlags.HideInHierarchy;
			MeshFilter meshFilter = gameObject2.AddComponent<MeshFilter>();
			meshFilter.sharedMesh = DTungTungSahurModel.sharedMesh;
			meshFilter.hideFlags = HideFlags.HideInHierarchy;
			MeshRenderer meshRenderer = gameObject2.AddComponent<MeshRenderer>();
			meshRenderer.sharedMaterial = DTungTungSahurModel.sharedMaterial;
			meshRenderer.hideFlags = HideFlags.HideInHierarchy;
			DTungTungSahurModel.SetLayerRecursive(gameObject, playerModelLayer);
			DTungTungSahurModel.modelInstance = gameObject;
			DTungTungSahurModel.currentAppliedScale = modelScale;
		}
	}
	private static float GetModelScale()
	{
		float num = MiscConfig.tungTungSahurModelScale;
		bool flag = num <= 0f;
		if (flag)
		{
			num = 1.8f;
		}
		return num;
	}
	private static void ApplyModelScale()
	{
		float modelScale = DTungTungSahurModel.GetModelScale();
		bool flag = Mathf.Approximately(modelScale, DTungTungSahurModel.currentAppliedScale);
		if (!flag)
		{
			DTungTungSahurModel.currentAppliedScale = modelScale;
			Transform transform = DTungTungSahurModel.modelInstance.transform.Find("CustomModel_Mesh");
			bool flag2 = transform != null;
			if (flag2)
			{
				transform.localScale = Vector3.one * modelScale;
				transform.localPosition = new Vector3(0f, -DTungTungSahurModel.meshMinY * modelScale, 0f);
			}
		}
	}
	// (get) Token: 0x06000406 RID: 1030 RVA: 0x0004358C File Offset: 0x0004178C
	private static Quaternion CurrentModelRotationCorrection
	{
		get
		{
			DCustomModelType dcustomModelType = MiscConfig.customModelType;
			Quaternion quaternion;
			if (dcustomModelType == DCustomModelType.BenjaminNetanyahu)
			{
				quaternion = Quaternion.Euler(90f, 0f, 0f);
			}
			else
			{
				quaternion = Quaternion.identity;
			}
			return quaternion;
		}
	}
	private static float ComputeRotatedMeshMinY(Quaternion rotation)
	{
		bool flag = DTungTungSahurModel.sharedMesh == null;
		float num;
		if (flag)
		{
			num = 0f;
		}
		else
		{
			Vector3 min = DTungTungSahurModel.sharedMesh.bounds.min;
			Vector3 max = DTungTungSahurModel.sharedMesh.bounds.max;
			float num2 = float.MaxValue;
			for (int i = 0; i < 8; i++)
			{
				Vector3 vector = new Vector3(((i & 1) == 0) ? min.x : max.x, ((i & 2) == 0) ? min.y : max.y, ((i & 4) == 0) ? min.z : max.z);
				Vector3 vector2 = rotation * vector;
				bool flag2 = vector2.y < num2;
				if (flag2)
				{
					num2 = vector2.y;
				}
			}
			num = num2;
		}
		return num;
	}
	private static Material CreateMaterial(Texture2D texture)
	{
		Shader shader = Shader.Find("Standard");
		bool flag = shader == null;
		if (flag)
		{
			shader = Shader.Find("Diffuse");
		}
		bool flag2 = shader == null;
		if (flag2)
		{
			shader = Shader.Find("Unlit/Texture");
		}
		bool flag3 = shader == null;
		if (flag3)
		{
			shader = Shader.Find("Hidden/Internal-Colored");
		}
		Material material = new Material(shader);
		bool flag4 = texture != null;
		if (flag4)
		{
			material.mainTexture = texture;
			material.color = Color.white;
		}
		else
		{
			material.color = new Color(0.62f, 0.42f, 0.22f, 1f);
		}
		bool flag5 = material.HasProperty("_Cull");
		if (flag5)
		{
			material.SetInt("_Cull", 2);
		}
		bool flag6 = material.HasProperty("_CullMode");
		if (flag6)
		{
			material.SetInt("_CullMode", 2);
		}
		material.hideFlags = HideFlags.HideInHierarchy;
		return material;
	}
	private static Texture2D LoadEmbeddedTexture(string resourceName)
	{
		Texture2D texture2D;
		try
		{
			Assembly executingAssembly = Assembly.GetExecutingAssembly();
			using (Stream manifestResourceStream = executingAssembly.GetManifestResourceStream(resourceName))
			{
				bool flag = manifestResourceStream == null;
				if (flag)
				{
					Debug.LogWarning(DTungTungSahurModel.CurrentLogTag + " Embedded resource not found: " + resourceName);
					texture2D = null;
				}
				else
				{
					byte[] array = new byte[manifestResourceStream.Length];
					int num = manifestResourceStream.Read(array, 0, array.Length);
					bool flag2 = num != array.Length;
					if (flag2)
					{
						Debug.LogWarning(DTungTungSahurModel.CurrentLogTag + " Failed to read full embedded texture stream");
						texture2D = null;
					}
					else
					{
						Texture2D texture2D2 = new Texture2D(2, 2, TextureFormat.RGBA32, false);
						bool flag3 = !texture2D2.LoadImage(array);
						if (flag3)
						{
							UnityEngine.Object.Destroy(texture2D2);
							texture2D = null;
						}
						else
						{
							texture2D2.name = DTungTungSahurModel.CurrentModelRootName + "Texture";
							texture2D2.filterMode = FilterMode.Bilinear;
							texture2D2.hideFlags = HideFlags.HideInHierarchy;
							texture2D = texture2D2;
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning(DTungTungSahurModel.CurrentLogTag + " Embedded texture load failed: " + ex.Message);
			texture2D = null;
		}
		return texture2D;
	}
	private static void HideOriginalRenderers()
	{
		bool flag = Player.player == null || DTungTungSahurModel.modelInstance == null;
		if (!flag)
		{
			Transform transform = DTungTungSahurModel.modelInstance.transform;
			for (int i = 0; i < DTungTungSahurModel.hiddenRenderers.Count; i++)
			{
				Renderer renderer = DTungTungSahurModel.hiddenRenderers[i];
				bool flag2 = renderer == null;
				if (!flag2)
				{
					bool enabled = renderer.enabled;
					if (enabled)
					{
						renderer.enabled = false;
					}
				}
			}
			bool flag3 = Time.time - DTungTungSahurModel.lastRendererScanTime < 0.5f;
			if (!flag3)
			{
				DTungTungSahurModel.lastRendererScanTime = Time.time;
				Renderer[] componentsInChildren = Player.player.GetComponentsInChildren<Renderer>();
				DTungTungSahurModel.currentRendererSet.Clear();
				foreach (Renderer renderer2 in componentsInChildren)
				{
					bool flag4 = renderer2 == null;
					if (!flag4)
					{
						bool flag5 = DTungTungSahurModel.IsChildOf(renderer2.transform, transform);
						if (!flag5)
						{
							DTungTungSahurModel.currentRendererSet.Add(renderer2);
							bool enabled2 = renderer2.enabled;
							if (enabled2)
							{
								renderer2.enabled = false;
								bool flag6 = !DTungTungSahurModel.hiddenSet.Contains(renderer2);
								if (flag6)
								{
									DTungTungSahurModel.hiddenRenderers.Add(renderer2);
									DTungTungSahurModel.hiddenSet.Add(renderer2);
								}
							}
						}
					}
				}
				for (int k = DTungTungSahurModel.hiddenRenderers.Count - 1; k >= 0; k--)
				{
					Renderer renderer3 = DTungTungSahurModel.hiddenRenderers[k];
					bool flag7 = renderer3 == null || !DTungTungSahurModel.currentRendererSet.Contains(renderer3);
					if (flag7)
					{
						bool flag8 = renderer3 != null;
						if (flag8)
						{
							renderer3.enabled = true;
						}
						DTungTungSahurModel.hiddenRenderers.RemoveAt(k);
						DTungTungSahurModel.hiddenSet.Remove(renderer3);
					}
				}
			}
		}
	}
	private static bool IsChildOf(Transform t, Transform parent)
	{
		Transform transform = t;
		while (transform != null)
		{
			bool flag = transform == parent;
			if (flag)
			{
				return true;
			}
			transform = transform.parent;
		}
		return false;
	}
	private static void ShowOriginalRenderers()
	{
		foreach (Renderer renderer in DTungTungSahurModel.hiddenRenderers)
		{
			bool flag = renderer != null;
			if (flag)
			{
				renderer.enabled = true;
			}
		}
		DTungTungSahurModel.hiddenRenderers.Clear();
		DTungTungSahurModel.hiddenSet.Clear();
	}
	private static void ShowOriginalRenderersExceptBody()
	{
		int playerModelLayer = DTungTungSahurModel.GetPlayerModelLayer();
		for (int i = DTungTungSahurModel.hiddenRenderers.Count - 1; i >= 0; i--)
		{
			Renderer renderer = DTungTungSahurModel.hiddenRenderers[i];
			bool flag = renderer == null;
			if (flag)
			{
				DTungTungSahurModel.hiddenRenderers.RemoveAt(i);
				DTungTungSahurModel.hiddenSet.Remove(renderer);
			}
			else
			{
				bool flag2 = renderer.gameObject.layer == playerModelLayer;
				if (!flag2)
				{
					renderer.enabled = true;
					DTungTungSahurModel.hiddenRenderers.RemoveAt(i);
					DTungTungSahurModel.hiddenSet.Remove(renderer);
				}
			}
		}
	}
	public static void CleanupModel()
	{
		DTungTungSahurModel.ShowOriginalRenderers();
		DTungTungSahurModel.lastRendererScanTime = 0f;
		bool flag = DTungTungSahurModel.modelInstance != null;
		if (flag)
		{
			UnityEngine.Object.Destroy(DTungTungSahurModel.modelInstance);
			DTungTungSahurModel.modelInstance = null;
		}
		DTungTungSahurModel.currentAppliedScale = -1f;
	}
	public static void FullCleanup()
	{
		DTungTungSahurModel.CleanupModel();
		bool flag = DTungTungSahurModel.sharedMesh != null;
		if (flag)
		{
			UnityEngine.Object.Destroy(DTungTungSahurModel.sharedMesh);
			DTungTungSahurModel.sharedMesh = null;
		}
		bool flag2 = DTungTungSahurModel.sharedTexture != null;
		if (flag2)
		{
			UnityEngine.Object.Destroy(DTungTungSahurModel.sharedTexture);
			DTungTungSahurModel.sharedTexture = null;
		}
		bool flag3 = DTungTungSahurModel.sharedMaterial != null;
		if (flag3)
		{
			UnityEngine.Object.Destroy(DTungTungSahurModel.sharedMaterial);
			DTungTungSahurModel.sharedMaterial = null;
		}
		DTungTungSahurModel.resourcesLoadAttempted = false;
	}
	private static void SetLayerRecursive(GameObject obj, int layer)
	{
		obj.layer = layer;
		foreach (object obj2 in obj.transform)
		{
			Transform transform = (Transform)obj2;
			DTungTungSahurModel.SetLayerRecursive(transform.gameObject, layer);
		}
	}
	private static int GetPlayerModelLayer()
	{
		bool flag = LocalPlayerChams.Model0Renderer != null;
		int num;
		if (flag)
		{
			num = LocalPlayerChams.Model0Renderer.gameObject.layer;
		}
		else
		{
			bool flag2 = LocalPlayerChams.Model1Renderer != null;
			if (flag2)
			{
				num = LocalPlayerChams.Model1Renderer.gameObject.layer;
			}
			else
			{
				bool flag3 = Player.player != null;
				if (flag3)
				{
					foreach (Renderer renderer in Player.player.GetComponentsInChildren<Renderer>())
					{
						bool flag4 = renderer != null && renderer.enabled;
						if (flag4)
						{
							return renderer.gameObject.layer;
						}
					}
				}
				num = Player.player.gameObject.layer;
			}
		}
		return num;
	}
	private const string TungTungFBXResourceName = "UnityEngine.tungtung_model.fbx";
	private const string TungTungTextureResourceName = "UnityEngine.tungtung_model_texture.png";
	private const string TungTungFallbackTextureResourceName = "UnityEngine.tungtungsahur_texture.png";
	private const string BenjaminFBXResourceName = "UnityEngine.benjamin_model.fbx";
	private const string BenjaminTextureResourceName = "UnityEngine.benjamin_model_texture.png";
	private const float DefaultModelScale = 1.8f;
	private static float meshMinY;
	private static Quaternion modelRotationCorrection = Quaternion.identity;
	private static float currentAppliedScale = -1f;
	private static GameObject modelInstance;
	private static Mesh sharedMesh;
	private static Texture2D sharedTexture;
	private static Material sharedMaterial;
	private static bool resourcesLoadAttempted;
	private static readonly List<Renderer> hiddenRenderers = new List<Renderer>();
	private static readonly HashSet<Renderer> hiddenSet = new HashSet<Renderer>();
	private static readonly HashSet<Renderer> currentRendererSet = new HashSet<Renderer>();
	private static float lastRendererScanTime;
	private const float RendererScanInterval = 0.5f;
	private const float ModelYawOffset = 180f;
}
