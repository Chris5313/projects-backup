using System;
using System.Collections.Generic;
using SDG.Unturned;
using UnityEngine;
public static class LocalPlayerChams
{
	[InitializeAttribute]
	private static void Initialize()
	{
		LocalPlayerChams.SkinMaterial = new Material(Shader.Find("Hidden/Internal-Colored"))
		{
			hideFlags = HideFlags.HideAndDontSave,
			color = Color.white
		};
		LocalPlayerChams.SkinMaterial.SetInt("_SrcBlend", 5);
		LocalPlayerChams.SkinMaterial.SetInt("_DstBlend", 10);
		LocalPlayerChams.SkinMaterial.SetInt("_Cull", 0);
		LocalPlayerChams.SkinMaterial.SetInt("_ZWrite", 0);
		LocalPlayerChams.SkinMaterial.SetInt("_ZTest", 0);
		LocalPlayerChams.HandsMaterial = new Material(Shader.Find("Hidden/Internal-Colored"))
		{
			hideFlags = HideFlags.HideAndDontSave,
			color = Color.white
		};
		LocalPlayerChams.HandsMaterial.SetInt("_SrcBlend", 5);
		LocalPlayerChams.HandsMaterial.SetInt("_DstBlend", 10);
		LocalPlayerChams.HandsMaterial.SetInt("_Cull", 0);
		LocalPlayerChams.HandsMaterial.SetInt("_ZWrite", 0);
		LocalPlayerChams.HandsMaterial.SetInt("_ZTest", 0);
		ScreenshotManager.PreDrawEvent = (SimpleDelegate)Delegate.Combine(ScreenshotManager.PreDrawEvent, new SimpleDelegate(LocalPlayerChams.OnSkinRepaintEvent));
		ScreenshotManager.PostDrawEvent = (SimpleDelegate)Delegate.Combine(ScreenshotManager.PostDrawEvent, new SimpleDelegate(LocalPlayerChams.OnModelChangedEvent));
	}
	private static void OnSkinRepaintEvent()
	{
		LocalPlayerChams.RestoreOriginalMaterials();
		foreach (ChamWireframeComponent dg7HgPuhdjH1X6wkz4QJsSTzj in ChamWireframeComponent.AllInstances)
		{
			dg7HgPuhdjH1X6wkz4QJsSTzj.RestoreOriginalMaterials();
		}
	}
	private static void OnModelChangedEvent()
	{
		LocalPlayerChams.ApplyOwnSkinChams();
		foreach (ChamWireframeComponent dg7HgPuhdjH1X6wkz4QJsSTzj in ChamWireframeComponent.AllInstances)
		{
			dg7HgPuhdjH1X6wkz4QJsSTzj.ReapplyChamMaterial();
		}
	}
	public static void Update()
	{
		bool flag = Player.player == null;
		if (!flag)
		{
			bool flag2 = LocalPlayerChams.Model0Renderer == null || LocalPlayerChams.Model1Renderer == null;
			bool flag3 = flag2 && Time.time - LocalPlayerChams.LastRendererScanTime >= 0.5f;
			if (flag3)
			{
				LocalPlayerChams.LastRendererScanTime = Time.time;
				foreach (Renderer renderer in Player.player.GetComponentsInChildren<Renderer>())
				{
					bool flag4 = renderer.name == "Model_0";
					if (flag4)
					{
						LocalPlayerChams.Model0Renderer = renderer;
					}
					else
					{
						bool flag5 = renderer.name == "Model_1";
						if (flag5)
						{
							LocalPlayerChams.Model1Renderer = renderer;
						}
					}
				}
				LocalPlayerChams.ApplyOwnSkinChams();
			}
			Color color = ColorConfig.GetColor("Skin color");
			bool flag6 = color != LocalPlayerChams.CachedSkinColor;
			if (flag6)
			{
				LocalPlayerChams.CachedSkinColor = color;
				LocalPlayerChams.SkinMaterial.color = color;
			}
			Color color2 = ColorConfig.GetColor("Hands color");
			bool flag7 = color2 != LocalPlayerChams.CachedHandsColor;
			if (flag7)
			{
				LocalPlayerChams.CachedHandsColor = color2;
				LocalPlayerChams.HandsMaterial.color = color2;
			}
			LocalPlayerChams.UpdateLocalPlayerWireframe();
		}
	}
	public static void ApplyOwnSkinChams()
	{
		bool dbjv74arVJtUMAqsSN0cWr9w = ScreenshotManager.IsSpying;
		if (!dbjv74arVJtUMAqsSN0cWr9w)
		{
			bool replaceLocalPlayerModel = MiscConfig.replaceLocalPlayerModel;
			if (!replaceLocalPlayerModel)
			{
				bool chamsedRepaintOwnSkin = Settings.chamsedRepaintOwnSkin;
				if (chamsedRepaintOwnSkin)
				{
					bool flag = LocalPlayerChams.Model0Renderer != null;
					if (flag)
					{
						LocalPlayerChams.Model0Renderer.material = LocalPlayerChams.SkinMaterial;
					}
					bool flag2 = LocalPlayerChams.Model1Renderer != null;
					if (flag2)
					{
						LocalPlayerChams.Model1Renderer.material = LocalPlayerChams.SkinMaterial;
					}
				}
				else
				{
					LocalPlayerChams.RestoreOriginalMaterials();
				}
			}
		}
	}
	private static void RestoreOriginalMaterials()
	{
		bool flag = LocalPlayerChams.Model0Renderer != null;
		if (flag)
		{
			LocalPlayerChams.Model0Renderer.material = LocalPlayerChams.MaterialClothingField.Get(Player.player.clothing.thirdClothes);
		}
		bool flag2 = LocalPlayerChams.Model1Renderer != null;
		if (flag2)
		{
			LocalPlayerChams.Model1Renderer.material = LocalPlayerChams.MaterialClothingField.Get(Player.player.clothing.thirdClothes);
		}
	}
	private static void UpdateLocalPlayerWireframe()
	{
		bool flag = Settings.localPlayerWireframe && !MiscConfig.replaceLocalPlayerModel && Player.player.look.perspective > EPlayerPerspective.FIRST;
		if (flag)
		{
			LocalPlayerChams.EnsureWireframeComponent(LocalPlayerChams.Model0Renderer);
			LocalPlayerChams.EnsureWireframeComponent(LocalPlayerChams.Model1Renderer);
		}
		else
		{
			LocalPlayerChams.RemoveWireframeComponent(LocalPlayerChams.Model0Renderer);
			LocalPlayerChams.RemoveWireframeComponent(LocalPlayerChams.Model1Renderer);
		}
	}
	private static void EnsureWireframeComponent(Renderer r)
	{
		ChamWireframeComponent dg7HgPuhdjH1X6wkz4QJsSTzj = LocalPlayerChams.GetOrCreateWireframeComponent(r, true);
		bool flag = dg7HgPuhdjH1X6wkz4QJsSTzj != null;
		if (flag)
		{
			dg7HgPuhdjH1X6wkz4QJsSTzj.DSetLocalPlayerWireframe(true);
			dg7HgPuhdjH1X6wkz4QJsSTzj.DEnableWireframe();
		}
	}
	private static void RemoveWireframeComponent(Renderer r)
	{
		ChamWireframeComponent dg7HgPuhdjH1X6wkz4QJsSTzj = LocalPlayerChams.GetOrCreateWireframeComponent(r, false);
		bool flag = dg7HgPuhdjH1X6wkz4QJsSTzj != null;
		if (flag)
		{
			dg7HgPuhdjH1X6wkz4QJsSTzj.DDisableWireframe();
		}
	}
	private static ChamWireframeComponent GetOrCreateWireframeComponent(Renderer r, bool createIfMissing)
	{
		bool flag = r == null;
		ChamWireframeComponent dg7HgPuhdjH1X6wkz4QJsSTzj;
		if (flag)
		{
			dg7HgPuhdjH1X6wkz4QJsSTzj = null;
		}
		else
		{
			bool flag2 = LocalPlayerChams.WireframeComponentCache == null;
			if (flag2)
			{
				LocalPlayerChams.WireframeComponentCache = new Dictionary<Renderer, ChamWireframeComponent>();
			}
			ChamWireframeComponent dg7HgPuhdjH1X6wkz4QJsSTzj2;
			bool flag3 = !LocalPlayerChams.WireframeComponentCache.TryGetValue(r, out dg7HgPuhdjH1X6wkz4QJsSTzj2) || dg7HgPuhdjH1X6wkz4QJsSTzj2 == null;
			if (flag3)
			{
				dg7HgPuhdjH1X6wkz4QJsSTzj2 = r.GetComponent<ChamWireframeComponent>();
				bool flag4 = dg7HgPuhdjH1X6wkz4QJsSTzj2 == null && createIfMissing;
				if (flag4)
				{
					dg7HgPuhdjH1X6wkz4QJsSTzj2 = r.gameObject.AddComponent<ChamWireframeComponent>();
				}
				LocalPlayerChams.WireframeComponentCache[r] = dg7HgPuhdjH1X6wkz4QJsSTzj2;
			}
			dg7HgPuhdjH1X6wkz4QJsSTzj = dg7HgPuhdjH1X6wkz4QJsSTzj2;
		}
		return dg7HgPuhdjH1X6wkz4QJsSTzj;
	}
	public static void ClearWireframeCache()
	{
		bool flag = LocalPlayerChams.WireframeComponentCache != null;
		if (flag)
		{
			LocalPlayerChams.WireframeComponentCache.Clear();
		}
		LocalPlayerChams.Model0Renderer = null;
		LocalPlayerChams.Model1Renderer = null;
	}
	public static Renderer Model0Renderer;
	public static Renderer Model1Renderer;
	private static Material SkinMaterial;
	private static Material HandsMaterial;
	private static ReflectedField<Material> MaterialClothingField = new ReflectedField<Material>(typeof(HumanClothes), "materialClothing");
	private static Color CachedSkinColor = Color.clear;
	private static Color CachedHandsColor = Color.clear;
	private static Dictionary<Renderer, ChamWireframeComponent> WireframeComponentCache;
	private static float LastRendererScanTime;
	private const float RendererScanInterval = 0.5f;
}
