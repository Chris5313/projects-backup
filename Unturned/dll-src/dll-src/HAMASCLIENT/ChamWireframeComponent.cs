using System;
using System.Collections.Generic;
using UnityEngine;
public class ChamWireframeComponent : MonoBehaviour
{
	private void Awake()
	{
		this.CachedRenderer = base.GetComponent<Renderer>();
		this.originalMaterials = this.CachedRenderer.sharedMaterials;
		this.OriginalSharedMaterial = this.CachedRenderer.sharedMaterial;
		this.originalMaterialCount = this.originalMaterials.Length;
		bool flag = this.originalMaterialCount == 0;
		if (flag)
		{
			this.originalMaterialCount = 1;
		}
		this.isSkinned = this.CachedRenderer is SkinnedMeshRenderer;
		ChamWireframeComponent.AllInstances.Add(this);
	}
	private void OnDestroy()
	{
		ChamWireframeComponent.AllInstances.Remove(this);
		ChamWireframeComponent.DwireframeInstances.Remove(this);
		bool flag = this.CachedRenderer != null;
		if (flag)
		{
			this.RestoreOriginalMaterials();
		}
		bool flag2 = this.wireframeMesh != null;
		if (flag2)
		{
			UnityEngine.Object.Destroy(this.wireframeMesh);
		}
		bool flag3 = this.bakedMesh != null;
		if (flag3)
		{
			UnityEngine.Object.Destroy(this.bakedMesh);
		}
		bool flag4 = this.myInstancedMaterial != null;
		if (flag4)
		{
			UnityEngine.Object.Destroy(this.myInstancedMaterial);
		}
		bool flag5 = this.myInstancedMaterialHidden != null;
		if (flag5)
		{
			UnityEngine.Object.Destroy(this.myInstancedMaterialHidden);
		}
	}
	public void ApplyChamMaterial(Material m)
	{
		this.ChamMaterialSource = m;
		this.chamsApplied = true;
		bool flag = !ScreenshotManager.IsSpying;
		bool flag2 = flag;
		if (flag2)
		{
			bool flag3 = this.myInstancedMaterial == null;
			if (flag3)
			{
				this.myInstancedMaterial = new Material(m);
			}
			else
			{
				this.myInstancedMaterial.CopyPropertiesFromMaterial(m);
			}
			bool flag4 = this.myInstancedMaterialHidden == null;
			if (flag4)
			{
				this.myInstancedMaterialHidden = new Material(m);
			}
			else
			{
				this.myInstancedMaterialHidden.CopyPropertiesFromMaterial(m);
			}
			this.myInstancedMaterial.shader = Shader.Find("Hidden/Internal-Colored");
			this.myInstancedMaterialHidden.shader = Shader.Find("Hidden/Internal-Colored");
			bool chamVisibilityCheck = VisualsTab.ChamVisibilityCheck;
			if (chamVisibilityCheck)
			{
				Color color = EspCategories.ChamVisibleColor;
				Color color2 = EspCategories.ChamInvisibleColor;
				this.myInstancedMaterial.color = color;
				this.myInstancedMaterialHidden.color = color2;
			}
			else
			{
				this.myInstancedMaterial.color = m.color;
				this.myInstancedMaterialHidden.color = m.color;
			}
			bool chamVisibilityCheck2 = VisualsTab.ChamVisibilityCheck;
			bool flag5 = chamVisibilityCheck2;
			if (flag5)
			{
				this.myInstancedMaterial.SetInt("_ZTest", 4);
				this.myInstancedMaterialHidden.SetInt("_ZTest", 6);
				bool flag6 = this.cachedDoubleMaterialArray == null || this.cachedDoubleMaterialArray.Length != this.originalMaterialCount * 2;
				if (flag6)
				{
					this.cachedDoubleMaterialArray = new Material[this.originalMaterialCount * 2];
				}
				for (int i = 0; i < this.originalMaterialCount; i++)
				{
					this.cachedDoubleMaterialArray[i] = this.myInstancedMaterialHidden;
					this.cachedDoubleMaterialArray[i + this.originalMaterialCount] = this.myInstancedMaterial;
				}
				this.CachedRenderer.sharedMaterials = this.cachedDoubleMaterialArray;
			}
			else
			{
				this.myInstancedMaterial.SetInt("_ZTest", 8);
				bool flag7 = this.cachedSingleMaterialArray == null || this.cachedSingleMaterialArray.Length != 1;
				if (flag7)
				{
					this.cachedSingleMaterialArray = new Material[] { this.myInstancedMaterial };
				}
				this.cachedSingleMaterialArray[0] = this.myInstancedMaterial;
				this.CachedRenderer.sharedMaterials = this.cachedSingleMaterialArray;
			}
		}
	}
	public void ReapplyChamMaterial()
	{
		bool flag = this.ChamMaterialSource != null;
		if (flag)
		{
			this.ApplyChamMaterial(this.ChamMaterialSource);
		}
	}
	public void RestoreOriginalMaterials()
	{
		this.chamsApplied = false;
		bool flag = this.CachedRenderer == null;
		if (!flag)
		{
			bool flag2 = this.originalMaterials != null;
			if (flag2)
			{
				this.CachedRenderer.sharedMaterials = this.originalMaterials;
			}
			else
			{
				bool flag3 = this.OriginalSharedMaterial != null;
				if (flag3)
				{
					this.CachedRenderer.sharedMaterial = this.OriginalSharedMaterial;
				}
			}
		}
	}
	// (get) Token: 0x060001CE RID: 462 RVA: 0x0001B6C0 File Offset: 0x000198C0
	public bool HasChamMaterial
	{
		get
		{
			return this.chamsApplied;
		}
	}
	public void DEnableWireframe()
	{
		bool flag = !this.wireframeEnabled;
		if (flag)
		{
			this.wireframeEnabled = true;
			ChamWireframeComponent.DwireframeInstances.Add(this);
			this.DGenerateWireframe();
		}
	}
	public void DSetLocalPlayerWireframe(bool isLocal)
	{
		this.isLocalPlayerWireframe = isLocal;
	}
	public void DDisableWireframe()
	{
		bool flag = this.wireframeEnabled;
		if (flag)
		{
			this.wireframeEnabled = false;
			ChamWireframeComponent.DwireframeInstances.Remove(this);
		}
	}
	private void DGenerateWireframe()
	{
		Mesh mesh = null;
		bool flag = this.isSkinned;
		if (flag)
		{
			mesh = ((SkinnedMeshRenderer)this.CachedRenderer).sharedMesh;
		}
		else
		{
			MeshFilter component = this.CachedRenderer.GetComponent<MeshFilter>();
			bool flag2 = component != null;
			if (flag2)
			{
				mesh = component.sharedMesh;
			}
		}
		bool flag3 = mesh == null;
		if (!flag3)
		{
			int[] array;
			bool flag4 = ChamWireframeComponent.DcachedEdgeIndices.TryGetValue(mesh, out array) && array != null && array.Length != 0;
			if (flag4)
			{
				this.allWireframeIndices = array;
				bool flag5 = this.wireframeMesh == null;
				if (flag5)
				{
					this.wireframeMesh = new Mesh();
					this.wireframeMesh.hideFlags = HideFlags.HideAndDontSave;
				}
				this.wireframeMesh.Clear();
				this.wireframeMesh.vertices = mesh.vertices;
				this.wireframeMesh.bounds = mesh.bounds;
				this.currentLodLineCount = -1;
				this.DApplyWireframeDensity(1f);
			}
			else
			{
				bool flag6 = this.edgeSet == null;
				if (flag6)
				{
					this.edgeSet = new HashSet<long>();
				}
				else
				{
					this.edgeSet.Clear();
				}
				bool flag7 = this.edgeIndices == null;
				if (flag7)
				{
					this.edgeIndices = new List<int>();
				}
				else
				{
					this.edgeIndices.Clear();
				}
				int subMeshCount = mesh.subMeshCount;
				bool flag8 = subMeshCount <= 1;
				if (flag8)
				{
					int[] triangles = mesh.triangles;
					bool flag9 = triangles == null || triangles.Length == 0;
					if (flag9)
					{
						return;
					}
					for (int i = 0; i < triangles.Length; i += 3)
					{
						int num = triangles[i];
						int num2 = triangles[i + 1];
						int num3 = triangles[i + 2];
						ChamWireframeComponent.DAddEdge(this.edgeIndices, this.edgeSet, num, num2);
						ChamWireframeComponent.DAddEdge(this.edgeIndices, this.edgeSet, num2, num3);
						ChamWireframeComponent.DAddEdge(this.edgeIndices, this.edgeSet, num, num3);
					}
				}
				else
				{
					for (int j = 0; j < subMeshCount; j++)
					{
						int[] triangles2 = mesh.GetTriangles(j);
						bool flag10 = triangles2 == null || triangles2.Length == 0;
						if (!flag10)
						{
							for (int k = 0; k < triangles2.Length; k += 3)
							{
								int num4 = triangles2[k];
								int num5 = triangles2[k + 1];
								int num6 = triangles2[k + 2];
								ChamWireframeComponent.DAddEdge(this.edgeIndices, this.edgeSet, num4, num5);
								ChamWireframeComponent.DAddEdge(this.edgeIndices, this.edgeSet, num5, num6);
								ChamWireframeComponent.DAddEdge(this.edgeIndices, this.edgeSet, num4, num6);
							}
						}
					}
				}
				bool flag11 = this.edgeIndices.Count == 0;
				if (!flag11)
				{
					this.allWireframeIndices = this.edgeIndices.ToArray();
					ChamWireframeComponent.DcachedEdgeIndices[mesh] = this.allWireframeIndices;
					bool flag12 = this.wireframeMesh == null;
					if (flag12)
					{
						this.wireframeMesh = new Mesh();
						this.wireframeMesh.hideFlags = HideFlags.HideAndDontSave;
					}
					this.wireframeMesh.Clear();
					this.wireframeMesh.vertices = mesh.vertices;
					this.wireframeMesh.bounds = mesh.bounds;
					this.currentLodLineCount = -1;
					this.DApplyWireframeDensity(1f);
				}
			}
		}
	}
	private static void DAddEdge(List<int> indices, HashSet<long> edgeSet, int a, int b)
	{
		long num = ((a < b) ? (((long)a << 32) | (long)((ulong)b)) : (((long)b << 32) | (long)((ulong)a)));
		bool flag = edgeSet.Add(num);
		if (flag)
		{
			indices.Add((a < b) ? a : b);
			indices.Add((a < b) ? b : a);
		}
	}
	public void DRenderWireframe(Material wfMat, Plane[] frustumPlanes)
	{
		bool flag = this.CachedRenderer == null;
		if (!flag)
		{
			bool flag2 = !this.isLocalPlayerWireframe;
			if (flag2)
			{
				Bounds bounds = this.CachedRenderer.bounds;
				bool flag3 = frustumPlanes != null && !GeometryUtility.TestPlanesAABB(frustumPlanes, bounds);
				if (flag3)
				{
					return;
				}
				Camera main = Camera.main;
				bool flag4 = main != null;
				if (flag4)
				{
					float sqrMagnitude = (main.transform.position - bounds.center).sqrMagnitude;
					bool flag5 = sqrMagnitude > 1000000f;
					if (flag5)
					{
						return;
					}
				}
			}
			bool flag6 = this.wireframeMesh == null || this.allWireframeIndices == null || this.allWireframeIndices.Length == 0;
			if (flag6)
			{
				this.DGenerateWireframe();
				bool flag7 = this.wireframeMesh == null;
				if (flag7)
				{
					return;
				}
			}
			float num = 1f;
			Camera main2 = Camera.main;
			bool flag8 = main2 != null;
			if (flag8)
			{
				float num2 = Vector3.Distance(main2.transform.position, this.CachedRenderer.bounds.center);
				num = Mathf.Clamp(1f - (num2 - 15f) / 85f * 0.8f, 0.15f, 1f);
			}
			this.DApplyWireframeDensity(num);
			bool flag9 = this.isSkinned;
			Matrix4x4 matrix4x;
			if (flag9)
			{
				SkinnedMeshRenderer skinnedMeshRenderer = (SkinnedMeshRenderer)this.CachedRenderer;
				bool flag10 = this.bakedMesh == null;
				if (flag10)
				{
					this.bakedMesh = new Mesh();
					this.bakedMesh.hideFlags = HideFlags.HideAndDontSave;
				}
				skinnedMeshRenderer.BakeMesh(this.bakedMesh);
				this.wireframeMesh.vertices = this.bakedMesh.vertices;
				this.wireframeMesh.bounds = this.bakedMesh.bounds;
				matrix4x = skinnedMeshRenderer.transform.localToWorldMatrix;
			}
			else
			{
				bool flag11 = this.wireframeMesh.vertexCount == 0;
				if (flag11)
				{
					MeshFilter component = this.CachedRenderer.GetComponent<MeshFilter>();
					bool flag12 = component != null && component.sharedMesh != null;
					if (flag12)
					{
						this.wireframeMesh.vertices = component.sharedMesh.vertices;
					}
				}
				matrix4x = this.CachedRenderer.transform.localToWorldMatrix;
			}
			wfMat.SetPass(0);
			Graphics.DrawMeshNow(this.wireframeMesh, matrix4x);
		}
	}
	public Bounds DGetRendererBounds()
	{
		bool flag = this.CachedRenderer == null;
		Bounds bounds;
		if (flag)
		{
			bounds = new Bounds(Vector3.zero, Vector3.zero);
		}
		else
		{
			bounds = this.CachedRenderer.bounds;
		}
		return bounds;
	}
	public static void DCleanupNullInstances()
	{
		for (int i = ChamWireframeComponent.DwireframeInstances.Count - 1; i >= 0; i--)
		{
			bool flag = ChamWireframeComponent.DwireframeInstances[i] == null;
			if (flag)
			{
				ChamWireframeComponent.DwireframeInstances.RemoveAt(i);
			}
		}
	}
	public static void DClearEdgeCache()
	{
		ChamWireframeComponent.DcachedEdgeIndices.Clear();
	}
	public static void DInvalidateAllLod()
	{
		for (int i = 0; i < ChamWireframeComponent.DwireframeInstances.Count; i++)
		{
			bool flag = ChamWireframeComponent.DwireframeInstances[i] != null;
			if (flag)
			{
				ChamWireframeComponent.DwireframeInstances[i].currentLodLineCount = -1;
			}
		}
	}
	private void DApplyWireframeDensity(float distanceFactor)
	{
		bool flag = this.allWireframeIndices == null || this.allWireframeIndices.Length < 2 || this.wireframeMesh == null;
		if (!flag)
		{
			int num = this.allWireframeIndices.Length / 2;
			float num2 = (float)Settings.wireframeLineDensity / 100f;
			int num3 = Mathf.Max(1, Mathf.RoundToInt((float)num * num2 * distanceFactor));
			num3 = Mathf.Min(num3, num);
			bool flag2 = num3 == this.currentLodLineCount && this.lodIndexArray != null && this.lodIndexArray.Length == num3 * 2;
			if (!flag2)
			{
				int num4 = num3 * 2;
				bool flag3 = this.lodIndexArray == null || this.lodIndexArray.Length != num4;
				if (flag3)
				{
					this.lodIndexArray = new int[num4];
				}
				bool flag4 = num3 == num;
				if (flag4)
				{
					Array.Copy(this.allWireframeIndices, this.lodIndexArray, this.allWireframeIndices.Length);
				}
				else
				{
					for (int i = 0; i < num3; i++)
					{
						int num5 = i * num / num3;
						this.lodIndexArray[i * 2] = this.allWireframeIndices[num5 * 2];
						this.lodIndexArray[i * 2 + 1] = this.allWireframeIndices[num5 * 2 + 1];
					}
				}
				this.currentLodLineCount = num3;
				this.wireframeMesh.SetIndices(this.lodIndexArray, MeshTopology.Lines, 0);
			}
		}
	}
	public static List<ChamWireframeComponent> AllInstances = new List<ChamWireframeComponent>();
	private Material OriginalSharedMaterial;
	private Material ChamMaterialSource;
	private Renderer CachedRenderer;
	private Material[] originalMaterials;
	private Material myInstancedMaterial;
	private Material myInstancedMaterialHidden;
	private int originalMaterialCount = 1;
	private Material[] cachedSingleMaterialArray;
	private Material[] cachedDoubleMaterialArray;
	public static List<ChamWireframeComponent> DwireframeInstances = new List<ChamWireframeComponent>();
	private bool chamsApplied;
	private bool wireframeEnabled;
	public bool isLocalPlayerWireframe;
	private Mesh wireframeMesh;
	private int[] allWireframeIndices;
	private int currentLodLineCount = -1;
	private int[] lodIndexArray;
	private Mesh bakedMesh;
	private bool isSkinned;
	private HashSet<long> edgeSet;
	private List<int> edgeIndices;
	private static Dictionary<Mesh, int[]> DcachedEdgeIndices = new Dictionary<Mesh, int[]>();
	private const float DmaxRenderDistSq = 1000000f;
}
