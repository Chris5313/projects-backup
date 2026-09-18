using System;
using System.Collections.Generic;
using SDG.Unturned;
using UnityEngine;
public static class EspManager
{
	[InitializeAttribute]
	private static void Init()
	{
		EspManager.ScreenLineMaterial = EspManager.CreateColoredMaterial();
		EspManager.PlayerChamMaterialA = EspManager.CreateColoredMaterial();
		EspManager.PlayerChamMaterialB = EspManager.CreateColoredMaterial();
		EspManager.PlayerChamMaterialC = EspManager.CreateColoredMaterial();
		EspManager.DwireframeMaterial = new Material(Shader.Find("Hidden/Internal-Colored"))
		{
			hideFlags = HideFlags.HideAndDontSave
		};
		EspManager.DwireframeMaterial.SetInt("_SrcBlend", 5);
		EspManager.DwireframeMaterial.SetInt("_DstBlend", 10);
		EspManager.DwireframeMaterial.SetInt("_Cull", 0);
		EspManager.DwireframeMaterial.SetInt("_ZWrite", 0);
		EspManager.DwireframeMaterial.SetInt("_ZTest", 8);
		EspManager.DbacktrackMaterial = new Material(Shader.Find("Hidden/Internal-Colored"))
		{
			hideFlags = HideFlags.HideAndDontSave
		};
		EspManager.DbacktrackMaterial.SetInt("_SrcBlend", 5);
		EspManager.DbacktrackMaterial.SetInt("_DstBlend", 10);
		EspManager.DbacktrackMaterial.SetInt("_Cull", 0);
		EspManager.DbacktrackMaterial.SetInt("_ZWrite", 0);
		EspManager.DbacktrackMaterial.SetInt("_ZTest", 8);
		EspManager.HookDisconnectCleanup();
		Camera.onPostRender = (Camera.CameraCallback)Delegate.Combine(Camera.onPostRender, new Camera.CameraCallback(EspManager.DOnPostRender));
		UseableThrowable.onThrowableSpawned += delegate(UseableThrowable grenade, GameObject go)
		{
			EspManager.ActiveThrowables.Add(new ValueTuple<GameObject, ItemThrowableAsset>(go, grenade.equippedThrowableAsset));
		};
	}
	private static void HookDisconnectCleanup()
	{
		Provider.onClientDisconnected = (Provider.ClientDisconnected)Delegate.Combine(Provider.onClientDisconnected, new Provider.ClientDisconnected(delegate
		{
			EspManager.VehicleChamList.Clear();
			EspManager.ItemChamList.Clear();
			EspManager.ZombieChamList.Clear();
			EspManager.GeneratorChamList.Clear();
			EspManager.BedChamList.Clear();
			EspManager.TurretChamList.Clear();
			EspManager.StorageChamList.Clear();
			EspManager.AirdropChamList.Clear();
			EspManager.OreChamListA.Clear();
			EspManager.OreChamListB.Clear();
			EspManager.OreChamListC.Clear();
			EspManager.OreChamListD.Clear();
			EspManager.OreChamListE.Clear();
			EspManager.OreChamListF.Clear();
			EspManager.GrenadeChamList.Clear();
			EspManager.VehicleBoundsCache.Clear();
			EspManager.ZombieColliderCache.Clear();
			EspManager.GeneratorBoundsCache.Clear();
			EspManager.AnimalColliderCache.Clear();
			EspManager.BedBoundsCache.Clear();
			EspManager.TurretBoundsCache.Clear();
			EspManager.StorageBoundsCache.Clear();
			EspManager.AirdropColliderCache.Clear();
			EspManager.OreColliderCacheA.Clear();
			EspManager.OreColliderCacheB.Clear();
			EspManager.OreColliderCacheC.Clear();
			EspManager.OreColliderCacheD.Clear();
			EspManager.OreColliderCacheE.Clear();
			EspManager.OreColliderCacheF.Clear();
			EspManager.GrenadeBoundsCache.Clear();
			EspManager.VisibleItemList.Clear();
			EspManager.PlayerChamComponents.Clear();
			EspManager.DoreGatherList.Clear();
			EspManager.DoreBoundsCache.Clear();
			EspManager.DoreGatherTimer = 0f;
			EspManager.DrendererCache.Clear();
			EspManager.DchamsRendererCacheUShort.Clear();
			EspManager.DchamsRendererCacheUInt.Clear();
			EspManager.DprevWireframeChams = false;
			EspManager.DprevLocalPlayerWireframe = false;
			ChamWireframeComponent.DClearEdgeCache();
			LocalPlayerChams.ClearWireframeCache();
			bool flag = EspManager.DbacktrackMeshCache != null;
			if (flag)
			{
				foreach (KeyValuePair<ulong, Mesh> keyValuePair in EspManager.DbacktrackMeshCache)
				{
					bool flag2 = keyValuePair.Value != null;
					if (flag2)
					{
						UnityEngine.Object.Destroy(keyValuePair.Value);
					}
				}
				EspManager.DbacktrackMeshCache.Clear();
			}
		}));
	}
	public static void InitCategoryLabelStyles(GUIStyle defaultLabel)
	{
		foreach (EspCategory dwIoOmLGoIyIvOm9z0SHgcarr in EspCategories.categories)
		{
			dwIoOmLGoIyIvOm9z0SHgcarr.TextStyle = new GUIStyle(defaultLabel)
			{
				alignment = TextAnchor.MiddleCenter
			};
			dwIoOmLGoIyIvOm9z0SHgcarr.TextShadowStyle = new GUIStyle(defaultLabel)
			{
				alignment = TextAnchor.MiddleCenter,
				normal = new GUIStyleState
				{
					textColor = new Color32(5, 5, 5, byte.MaxValue)
				}
			};
		}
	}
	private static Material CreateColoredMaterial()
	{
		Material material = new Material(Shader.Find("Hidden/Internal-Colored"))
		{
			hideFlags = HideFlags.HideAndDontSave
		};
		material.SetInt("_SrcBlend", 5);
		material.SetInt("_DstBlend", 10);
		material.SetInt("_Cull", 0);
		material.SetInt("_ZWrite", 0);
		material.SetInt("_ZTest", 0);
		return material;
	}
	private static void DUpdateWireframeForPlayer(ValueTuple<ChamWireframeComponent, ChamWireframeComponent> valueTuple, bool enable)
	{
		if (enable)
		{
			bool flag = valueTuple.Item1 != null;
			if (flag)
			{
				valueTuple.Item1.DEnableWireframe();
			}
			bool flag2 = valueTuple.Item2 != null;
			if (flag2)
			{
				valueTuple.Item2.DEnableWireframe();
			}
		}
		else
		{
			bool flag3 = valueTuple.Item1 != null;
			if (flag3)
			{
				valueTuple.Item1.DDisableWireframe();
			}
			bool flag4 = valueTuple.Item2 != null;
			if (flag4)
			{
				valueTuple.Item2.DDisableWireframe();
			}
		}
	}
	private static void DOnPostRender(Camera cam)
	{
		bool flag = cam == null;
		if (!flag)
		{
			bool flag2 = MainCamera.instance != null && cam == MainCamera.instance;
			bool flag3 = FreeCamera.FreeCameraComponent != null && cam == FreeCamera.FreeCameraComponent;
			bool flag4 = !flag2 && !flag3;
			if (!flag4)
			{
				bool dbjv74arVJtUMAqsSN0cWr9w = ScreenshotManager.IsSpying;
				if (!dbjv74arVJtUMAqsSN0cWr9w)
				{
					bool flag5 = (ChamWireframeComponent.DwireframeInstances.Count > 0 || Settings.localPlayerWireframe) && EspManager.DwireframeMaterial != null;
					bool flag6 = AimbotConfig.enableBacktrack && AimbotConfig.showBacktrackVisualizer && EspManager.DbacktrackMaterial != null;
					bool flag7 = !flag5 && !flag6;
					if (!flag7)
					{
						bool flag8 = flag5;
						if (flag8)
						{
							EspManager.DwireframeMaterial.color = EspCategories.ChamWireframeColor;
							EspManager.DfrustumPlanes = GeometryUtility.CalculateFrustumPlanes(cam);
							EspManager.DwireframeCleanupTimer += Time.deltaTime;
							bool flag9 = EspManager.DwireframeCleanupTimer > 5f;
							if (flag9)
							{
								EspManager.DwireframeCleanupTimer = 0f;
								ChamWireframeComponent.DCleanupNullInstances();
							}
							bool flag10 = EspManager.DwireframeRenderList == null;
							if (flag10)
							{
								EspManager.DwireframeRenderList = new List<ChamWireframeComponent>();
							}
							EspManager.DwireframeRenderList.Clear();
							EspManager.DwireframeRenderList.AddRange(ChamWireframeComponent.DwireframeInstances);
							EspManager.DwireframeRenderList.Sort(delegate(ChamWireframeComponent a, ChamWireframeComponent b)
							{
								bool flag13 = a == null;
								int num;
								if (flag13)
								{
									num = 1;
								}
								else
								{
									bool flag14 = b == null;
									if (flag14)
									{
										num = -1;
									}
									else
									{
										Bounds bounds = a.DGetRendererBounds();
										Bounds bounds2 = b.DGetRendererBounds();
										float sqrMagnitude = (cam.transform.position - bounds.center).sqrMagnitude;
										float sqrMagnitude2 = (cam.transform.position - bounds2.center).sqrMagnitude;
										num = sqrMagnitude.CompareTo(sqrMagnitude2);
									}
								}
								return num;
							});
							for (int i = 0; i < EspManager.DwireframeRenderList.Count; i++)
							{
								try
								{
									bool flag11 = EspManager.DwireframeRenderList[i] != null;
									if (flag11)
									{
										EspManager.DwireframeRenderList[i].DRenderWireframe(EspManager.DwireframeMaterial, EspManager.DfrustumPlanes);
									}
								}
								catch (Exception ex)
								{
									Debug.LogWarning("[DOnPostRender] Wireframe render error: " + ex.Message);
								}
							}
						}
						bool flag12 = flag6;
						if (flag12)
						{
							try
							{
								EspManager.DRenderBacktrackGhosts();
							}
							catch (Exception ex2)
							{
								Debug.LogWarning("[DOnPostRender] Backtrack ghost render error: " + ex2.Message);
							}
						}
					}
				}
			}
		}
	}
	private static void DRenderBacktrackGhosts()
	{
		EspCategory dwIoOmLGoIyIvOm9z0SHgcarr = EspCategories.categories[0];
		foreach (SteamPlayer steamPlayer in Provider.clients)
		{
			try
			{
				bool flag = steamPlayer == null || steamPlayer.player == null;
				if (!flag)
				{
					bool flag2 = steamPlayer.player.channel.IsLocalPlayer || steamPlayer.player.life.isDead;
					if (!flag2)
					{
						ulong steamID = steamPlayer.playerID.steamID.m_SteamID;
						Vector3 backtrackPosition = AimbotUtil.GetBacktrackPosition(steamPlayer.player);
						Vector3 position = steamPlayer.player.transform.position;
						bool flag3 = Vector3.Distance(backtrackPosition, position) < 0.01f;
						if (!flag3)
						{
							bool flag4 = PlayerPriorityManager.IsFriendOrGroupMatePlayer(steamPlayer.player);
							bool flag5 = PlayerPriorityManager.IsEnemyPlayer(steamPlayer.player);
							bool flag6 = flag4;
							Color32 color;
							if (flag6)
							{
								color = dwIoOmLGoIyIvOm9z0SHgcarr.LineColors[14].Color;
							}
							else
							{
								bool flag7 = flag5;
								if (flag7)
								{
									color = dwIoOmLGoIyIvOm9z0SHgcarr.LineColors[21].Color;
								}
								else
								{
									color = dwIoOmLGoIyIvOm9z0SHgcarr.LineColors[7].Color;
								}
							}
							color.a = 100;
							EspManager.DbacktrackMaterial.color = color;
							SkinnedMeshRenderer skinnedMeshRenderer = EspManager.ThirdRenderer0Field.Get(steamPlayer.player.animator);
							SkinnedMeshRenderer skinnedMeshRenderer2 = EspManager.ThirdRenderer1Field.Get(steamPlayer.player.animator);
							Vector3 vector = backtrackPosition - position;
							EspManager.DbacktrackMaterial.SetPass(0);
							bool flag8 = skinnedMeshRenderer != null && skinnedMeshRenderer.sharedMesh != null;
							if (flag8)
							{
								Mesh mesh = EspManager.DGetBacktrackBakedMesh(steamID);
								skinnedMeshRenderer.BakeMesh(mesh);
								Matrix4x4 matrix4x = Matrix4x4.TRS(skinnedMeshRenderer.transform.position + vector, skinnedMeshRenderer.transform.rotation, skinnedMeshRenderer.transform.lossyScale);
								Graphics.DrawMeshNow(mesh, matrix4x);
							}
							bool flag9 = skinnedMeshRenderer2 != null && skinnedMeshRenderer2.sharedMesh != null;
							if (flag9)
							{
								Mesh mesh2 = EspManager.DGetBacktrackBakedMesh(steamID | unchecked((ulong)int.MinValue));
								skinnedMeshRenderer2.BakeMesh(mesh2);
								Matrix4x4 matrix4x2 = Matrix4x4.TRS(skinnedMeshRenderer2.transform.position + vector, skinnedMeshRenderer2.transform.rotation, skinnedMeshRenderer2.transform.lossyScale);
								Graphics.DrawMeshNow(mesh2, matrix4x2);
							}
						}
					}
				}
			}
			catch
			{
			}
		}
	}
	private static Mesh DGetBacktrackBakedMesh(ulong key)
	{
		Mesh mesh;
		bool flag = !EspManager.DbacktrackMeshCache.TryGetValue(key, out mesh) || mesh == null;
		if (flag)
		{
			mesh = new Mesh
			{
				hideFlags = HideFlags.HideAndDontSave
			};
			EspManager.DbacktrackMeshCache[key] = mesh;
		}
		return mesh;
	}
	public static void DrawAllCategories()
	{
		for (int i = 0; i < EspCategories.activeCategories.Count; i++)
		{
			EspManager.CurrentCategory = EspCategories.activeCategories[i];
			bool flag = EspManager.CurrentCategory.RefreshAction != null;
			bool flag2 = flag;
			if (flag2)
			{
				EspManager.CurrentCategory.RefreshAction(EspManager.CurrentCategory);
			}
		}
	}
	[ConfigBindAttribute("Visuals", "Master ESP toggle")]
	public static void ToggleMasterESP()
	{
		EspManager.masterESPEnabled = !EspManager.masterESPEnabled;
	}
	[ConfigBindAttribute("Visuals", "Toggle Player ESP")]
	public static void TogglePlayerESP()
	{
		EspCategories.categories[0].enabled = !EspCategories.categories[0].enabled;
	}
	[ConfigBindAttribute("Visuals", "Toggle Item ESP")]
	public static void ToggleItemESP()
	{
		EspCategories.categories[1].enabled = !EspCategories.categories[1].enabled;
	}
	[ConfigBindAttribute("Visuals", "Toggle Vehicle ESP")]
	public static void ToggleVehicleESP()
	{
		EspCategories.categories[2].enabled = !EspCategories.categories[2].enabled;
	}
	[ConfigBindAttribute("Visuals", "Toggle Zombie ESP")]
	public static void ToggleZombieESP()
	{
		EspCategories.categories[3].enabled = !EspCategories.categories[3].enabled;
	}
	[ConfigBindAttribute("Visuals", "Toggle Generator ESP")]
	public static void ToggleGeneratorESP()
	{
		EspCategories.categories[4].enabled = !EspCategories.categories[4].enabled;
	}
	[ConfigBindAttribute("Visuals", "Toggle Animal ESP")]
	public static void ToggleAnimalESP()
	{
		EspCategories.categories[5].enabled = !EspCategories.categories[5].enabled;
	}
	[ConfigBindAttribute("Visuals", "Toggle Bed ESP")]
	public static void ToggleBedESP()
	{
		EspCategories.categories[6].enabled = !EspCategories.categories[6].enabled;
	}
	[ConfigBindAttribute("Visuals", "Toggle Turret ESP")]
	public static void ToggleTurretESP()
	{
		EspCategories.categories[7].enabled = !EspCategories.categories[7].enabled;
	}
	[ConfigBindAttribute("Visuals", "Toggle Bullet ESP")]
	public static void ToggleBulletESP()
	{
		EspCategories.categories[8].enabled = !EspCategories.categories[8].enabled;
	}
	[ConfigBindAttribute("Visuals", "Toggle Storage ESP")]
	public static void ToggleStorageESP()
	{
		EspCategories.categories[9].enabled = !EspCategories.categories[9].enabled;
	}
	[ConfigBindAttribute("Visuals", "Toggle Airdrop ESP")]
	public static void ToggleAirdropESP()
	{
		EspCategories.categories[10].enabled = !EspCategories.categories[10].enabled;
	}
	[ConfigBindAttribute("Visuals", "Toggle Grenade ESP")]
	public static void ToggleGrenadeESP()
	{
		EspCategories.categories[11].enabled = !EspCategories.categories[11].enabled;
	}
	public static void DrawEspWorld()
	{
		bool flag = !EspManager.masterESPEnabled;
		if (!flag)
		{
			EspDrawer.UpdateCameraMatrices();
			bool flag2 = !Provider.isConnected && !Provider.isServer;
			bool flag3 = !flag2;
			if (flag3)
			{
				for (int i = 0; i < EspCategories.activeCategories.Count; i++)
				{
					EspManager.CurrentCategory = EspCategories.activeCategories[i];
					bool dsy8h4Odqh4GzyIBRSA0YZwCx = EspManager.CurrentCategory.SettingFlag14;
					bool flag4 = dsy8h4Odqh4GzyIBRSA0YZwCx;
					if (flag4)
					{
						EspManager.CurrentCategory.ScrollPosition = new Vector2(EspManager.CurrentCategory.AnchorOffset.x * (float)Screen.width, EspManager.CurrentCategory.AnchorOffset.y * (float)Screen.height);
					}
					try
					{
						EspManager.CurrentCategory.DrawAction(EspManager.CurrentCategory);
					}
					catch
					{
					}
				}
				OverlayDrawer.DrawAimTargets();
			}
			bool flag5 = MiscConfig.showFarmGrid && Player.player != null;
			if (flag5)
			{
				EspManager.DrawFarmGrid();
			}
			bool flag6 = MiscConfig.autoFishing && MiscConfig.autoFishingShowStats && Player.player != null;
			if (flag6)
			{
				EspManager.DrawFishingStats();
			}
			bool flag7 = MiscConfig.autoFarmShowStats && (MiscConfig.autoFarmHarvest || MiscConfig.autoFarmFertilize || MiscConfig.autoFarmPlant || MiscConfig.autoPlacePlant || MiscConfig.autoFarmCraft || MiscConfig.autoFarmStore) && Player.player != null;
			if (flag7)
			{
				EspManager.DrawFarmStats();
			}
		}
	}
	public static void DrawFishingStats()
	{
		float num = 230f;
		bool flag = EspManager.fishingPanelPos.x < 0f || EspManager.fishingPanelPos.y < 0f;
		if (flag)
		{
			EspManager.fishingPanelPos = new Vector2((float)Screen.width - num - 10f, 10f);
		}
		float num2 = EspManager.fishingPanelPos.x;
		float num3 = EspManager.fishingPanelPos.y;
		float num4 = 18f;
		float num5 = 24f;
		int num6 = 0;
		num6++;
		num6++;
		num6++;
		num6++;
		num6++;
		num6++;
		bool flag2 = AutomationBot.fishingLastCatchTime != DateTime.MinValue;
		if (flag2)
		{
			num6++;
		}
		num6++;
		num6++;
		float num7 = num5 + (float)num6 * num4 + 8f;
		EspManager.lastFishingPanelHeight = num7;
		Rect rect = new Rect(num2, num3, num, num7);
		bool menuOpened = MenuState.menuOpened;
		if (menuOpened)
		{
			Vector2 vector = new Vector2(Input.mousePosition.x, (float)Screen.height - Input.mousePosition.y);
			Rect rect2 = new Rect(num2, num3, num, num5);
			bool flag3 = !Input.GetMouseButton(0);
			if (flag3)
			{
				EspManager.fishingPanelDragging = false;
			}
			else
			{
				bool flag4 = Input.GetMouseButtonDown(0) && rect2.Contains(vector) && !EspManager.farmPanelDragging;
				if (flag4)
				{
					EspManager.fishingPanelDragging = true;
					EspManager.fishingPanelDragOffset = new Vector2(vector.x - num2, vector.y - num3);
				}
			}
			bool flag5 = EspManager.fishingPanelDragging;
			if (flag5)
			{
				num2 = vector.x - EspManager.fishingPanelDragOffset.x;
				num3 = vector.y - EspManager.fishingPanelDragOffset.y;
				num2 = Mathf.Clamp(num2, 0f, (float)Screen.width - num);
				num3 = Mathf.Clamp(num3, 0f, (float)Screen.height - num7);
				rect = new Rect(num2, num3, num, num7);
				EspManager.fishingPanelPos = new Vector2(num2, num3);
			}
		}
		MenuGuiHelper.DrawRect(new Rect(rect.x - 1f, rect.y - 1f, rect.width + 2f, rect.height + 2f), new Color32(10, 10, 10, 200), false, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(rect, new Color32(22, 22, 26, 220), false, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y, rect.width, num5), MenuGuiHelper.GetAccentColor(180), false, ScaleMode.StretchToFill);
		Color32 color = new Color32(50, 50, 55, byte.MaxValue);
		MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), color, false, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y + rect.height - 1f, rect.width, 1f), color, false, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), color, false, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 1f, rect.y, 1f, rect.height), color, false, ScaleMode.StretchToFill);
		GUIStyle guistyle = new GUIStyle(GUI.skin.label);
		guistyle.fontSize = 11;
		guistyle.fontStyle = FontStyle.Bold;
		guistyle.alignment = TextAnchor.MiddleCenter;
		guistyle.normal.textColor = Color.white;
		GUI.Label(new Rect(rect.x, rect.y, rect.width, num5), "Fishing Statistics", guistyle);
		float num8 = rect.y + num5 + 4f;
		float num9 = rect.x + 10f;
		float num10 = rect.x + rect.width - 70f;
		float num11 = 60f;
		GUIStyle guistyle2 = new GUIStyle(GUI.skin.label);
		guistyle2.fontSize = 10;
		guistyle2.normal.textColor = new Color32(180, 180, 180, byte.MaxValue);
		GUIStyle guistyle3 = new GUIStyle(GUI.skin.label);
		guistyle3.fontSize = 10;
		guistyle3.alignment = TextAnchor.MiddleRight;
		guistyle3.normal.textColor = new Color32(230, 230, 230, byte.MaxValue);
		GUI.Label(new Rect(num9, num8, 120f, num4), "Casts:", guistyle2);
		GUI.Label(new Rect(num10, num8, num11, num4), AutomationBot.fishingTotalCasts.ToString(), guistyle3);
		num8 += num4;
		GUI.Label(new Rect(num9, num8, 120f, num4), "Catches:", guistyle2);
		GUI.Label(new Rect(num10, num8, num11, num4), AutomationBot.fishingTotalCatches.ToString(), guistyle3);
		num8 += num4;
		GUI.Label(new Rect(num9, num8, 120f, num4), "Misses:", guistyle2);
		GUI.Label(new Rect(num10, num8, num11, num4), AutomationBot.fishingTotalMisses.ToString(), guistyle3);
		num8 += num4;
		GUI.Label(new Rect(num9, num8, 120f, num4), "Empty casts:", guistyle2);
		GUI.Label(new Rect(num10, num8, num11, num4), AutomationBot.fishingTotalEmptyCasts.ToString(), guistyle3);
		num8 += num4;
		int num12 = AutomationBot.fishingTotalCatches + AutomationBot.fishingTotalMisses + AutomationBot.fishingTotalEmptyCasts;
		float num13 = ((num12 > 0) ? ((float)AutomationBot.fishingTotalCatches / (float)num12 * 100f) : 0f);
		GUI.Label(new Rect(num9, num8, 120f, num4), "Catch rate:", guistyle2);
		GUI.Label(new Rect(num10, num8, num11, num4), num13.ToString("F1") + "%", guistyle3);
		num8 += num4;
		bool flag6 = AutomationBot.fishingLastCatchTime != DateTime.MinValue;
		if (flag6)
		{
			GUI.Label(new Rect(num9, num8, 120f, num4), "Last catch:", guistyle2);
			GUI.Label(new Rect(num10, num8, num11, num4), AutomationBot.fishingLastCatchItem, guistyle3);
			num8 += num4;
		}
		bool flag7 = AutomationBot.fishingSessionStart != DateTime.MinValue;
		if (flag7)
		{
			TimeSpan timeSpan = DateTime.Now - AutomationBot.fishingSessionStart;
			string text = string.Format("{0}m {1}s", (int)timeSpan.TotalMinutes, timeSpan.Seconds);
			GUI.Label(new Rect(num9, num8, 120f, num4), "Session:", guistyle2);
			GUI.Label(new Rect(num10, num8, num11, num4), text, guistyle3);
			num8 += num4;
		}
		GUI.Label(new Rect(num9, num8, 120f, num4), "Status:", guistyle2);
		GUIStyle guistyle4 = new GUIStyle(guistyle3);
		guistyle4.alignment = TextAnchor.MiddleRight;
		guistyle4.normal.textColor = new Color32(100, byte.MaxValue, 100, byte.MaxValue);
		GUI.Label(new Rect(num10 - 40f, num8, num11 + 40f, num4), AutomationBot.fishingStatusText, guistyle4);
	}
	public static void DrawFarmStats()
	{
		float num = 230f;
		float num2 = 10f;
		bool flag = MiscConfig.autoFishing && MiscConfig.autoFishingShowStats;
		if (flag)
		{
			num2 += EspManager.lastFishingPanelHeight + 10f;
		}
		bool flag2 = EspManager.farmPanelPos.x < 0f || EspManager.farmPanelPos.y < 0f;
		if (flag2)
		{
			EspManager.farmPanelPos = new Vector2((float)Screen.width - num - 10f, num2);
		}
		float num3 = EspManager.farmPanelPos.x;
		float num4 = EspManager.farmPanelPos.y;
		float num5 = 18f;
		float num6 = 24f;
		int num7 = 0;
		bool showFarmStatPlanted = MiscConfig.showFarmStatPlanted;
		if (showFarmStatPlanted)
		{
			num7++;
		}
		bool showFarmStatHarvested = MiscConfig.showFarmStatHarvested;
		if (showFarmStatHarvested)
		{
			num7++;
		}
		bool showFarmStatFertilized = MiscConfig.showFarmStatFertilized;
		if (showFarmStatFertilized)
		{
			num7++;
		}
		bool showFarmStatGrowing = MiscConfig.showFarmStatGrowing;
		if (showFarmStatGrowing)
		{
			num7++;
		}
		bool showFarmStatGrown = MiscConfig.showFarmStatGrown;
		if (showFarmStatGrown)
		{
			num7++;
		}
		bool showFarmStatEmpty = MiscConfig.showFarmStatEmpty;
		if (showFarmStatEmpty)
		{
			num7++;
		}
		bool showFarmStatCrafted = MiscConfig.showFarmStatCrafted;
		if (showFarmStatCrafted)
		{
			num7++;
		}
		bool showFarmStatStored = MiscConfig.showFarmStatStored;
		if (showFarmStatStored)
		{
			num7++;
		}
		num7++;
		float num8 = num6 + (float)num7 * num5 + 8f;
		Rect rect = new Rect(num3, num4, num, num8);
		bool menuOpened = MenuState.menuOpened;
		if (menuOpened)
		{
			Vector2 vector = new Vector2(Input.mousePosition.x, (float)Screen.height - Input.mousePosition.y);
			Rect rect2 = new Rect(num3, num4, num, num6);
			bool flag3 = !Input.GetMouseButton(0);
			if (flag3)
			{
				EspManager.farmPanelDragging = false;
			}
			else
			{
				bool flag4 = Input.GetMouseButtonDown(0) && rect2.Contains(vector) && !EspManager.fishingPanelDragging;
				if (flag4)
				{
					EspManager.farmPanelDragging = true;
					EspManager.farmPanelDragOffset = new Vector2(vector.x - num3, vector.y - num4);
				}
			}
			bool flag5 = EspManager.farmPanelDragging;
			if (flag5)
			{
				num3 = vector.x - EspManager.farmPanelDragOffset.x;
				num4 = vector.y - EspManager.farmPanelDragOffset.y;
				num3 = Mathf.Clamp(num3, 0f, (float)Screen.width - num);
				num4 = Mathf.Clamp(num4, 0f, (float)Screen.height - num8);
				rect = new Rect(num3, num4, num, num8);
				EspManager.farmPanelPos = new Vector2(num3, num4);
			}
		}
		MenuGuiHelper.DrawRect(new Rect(rect.x - 1f, rect.y - 1f, rect.width + 2f, rect.height + 2f), new Color32(10, 10, 10, 200), false, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(rect, new Color32(22, 22, 26, 220), false, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y, rect.width, num6), MenuGuiHelper.GetAccentColor(180), false, ScaleMode.StretchToFill);
		Color32 color = new Color32(50, 50, 55, byte.MaxValue);
		MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), color, false, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y + rect.height - 1f, rect.width, 1f), color, false, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), color, false, ScaleMode.StretchToFill);
		MenuGuiHelper.DrawRect(new Rect(rect.x + rect.width - 1f, rect.y, 1f, rect.height), color, false, ScaleMode.StretchToFill);
		GUIStyle guistyle = new GUIStyle(GUI.skin.label);
		guistyle.fontSize = 11;
		guistyle.fontStyle = FontStyle.Bold;
		guistyle.alignment = TextAnchor.MiddleCenter;
		guistyle.normal.textColor = Color.white;
		GUI.Label(new Rect(rect.x, rect.y, rect.width, num6), "Farm Statistics", guistyle);
		float num9 = rect.y + num6 + 4f;
		float num10 = rect.x + 10f;
		float num11 = rect.x + rect.width - 70f;
		float num12 = 60f;
		GUIStyle guistyle2 = new GUIStyle(GUI.skin.label);
		guistyle2.fontSize = 10;
		guistyle2.normal.textColor = new Color32(180, 180, 180, byte.MaxValue);
		GUIStyle guistyle3 = new GUIStyle(GUI.skin.label);
		guistyle3.fontSize = 10;
		guistyle3.alignment = TextAnchor.MiddleRight;
		guistyle3.normal.textColor = new Color32(230, 230, 230, byte.MaxValue);
		bool showFarmStatPlanted2 = MiscConfig.showFarmStatPlanted;
		if (showFarmStatPlanted2)
		{
			GUI.Label(new Rect(num10, num9, 120f, num5), "Planted:", guistyle2);
			GUI.Label(new Rect(num11, num9, num12, num5), AutomationBot.farmTotalPlanted.ToString(), guistyle3);
			num9 += num5;
		}
		bool showFarmStatHarvested2 = MiscConfig.showFarmStatHarvested;
		if (showFarmStatHarvested2)
		{
			GUI.Label(new Rect(num10, num9, 120f, num5), "Harvested:", guistyle2);
			GUI.Label(new Rect(num11, num9, num12, num5), AutomationBot.farmTotalHarvested.ToString(), guistyle3);
			num9 += num5;
		}
		bool showFarmStatFertilized2 = MiscConfig.showFarmStatFertilized;
		if (showFarmStatFertilized2)
		{
			GUI.Label(new Rect(num10, num9, 120f, num5), "Fertilized:", guistyle2);
			GUI.Label(new Rect(num11, num9, num12, num5), AutomationBot.farmTotalFertilized.ToString(), guistyle3);
			num9 += num5;
		}
		bool showFarmStatGrowing2 = MiscConfig.showFarmStatGrowing;
		if (showFarmStatGrowing2)
		{
			GUI.Label(new Rect(num10, num9, 120f, num5), "Growing:", guistyle2);
			GUI.Label(new Rect(num11, num9, num12, num5), AutomationBot.farmPlotsGrowing.ToString(), guistyle3);
			num9 += num5;
		}
		bool showFarmStatGrown2 = MiscConfig.showFarmStatGrown;
		if (showFarmStatGrown2)
		{
			GUI.Label(new Rect(num10, num9, 120f, num5), "Grown:", guistyle2);
			GUI.Label(new Rect(num11, num9, num12, num5), AutomationBot.farmPlotsGrown.ToString(), guistyle3);
			num9 += num5;
		}
		bool showFarmStatEmpty2 = MiscConfig.showFarmStatEmpty;
		if (showFarmStatEmpty2)
		{
			GUI.Label(new Rect(num10, num9, 120f, num5), "Empty:", guistyle2);
			GUI.Label(new Rect(num11, num9, num12, num5), AutomationBot.farmPlotsEmpty.ToString(), guistyle3);
			num9 += num5;
		}
		bool showFarmStatCrafted2 = MiscConfig.showFarmStatCrafted;
		if (showFarmStatCrafted2)
		{
			GUI.Label(new Rect(num10, num9, 120f, num5), "Crafted:", guistyle2);
			GUI.Label(new Rect(num11, num9, num12, num5), AutomationBot.farmTotalCrafted.ToString(), guistyle3);
			num9 += num5;
		}
		bool showFarmStatStored2 = MiscConfig.showFarmStatStored;
		if (showFarmStatStored2)
		{
			GUI.Label(new Rect(num10, num9, 120f, num5), "Stored:", guistyle2);
			GUI.Label(new Rect(num11, num9, num12, num5), AutomationBot.farmTotalStored.ToString(), guistyle3);
			num9 += num5;
		}
		bool flag6 = AutomationBot.farmSessionStart != DateTime.MinValue;
		if (flag6)
		{
			TimeSpan timeSpan = DateTime.Now - AutomationBot.farmSessionStart;
			string text = string.Format("{0}m {1}s", (int)timeSpan.TotalMinutes, timeSpan.Seconds);
			GUI.Label(new Rect(num10, num9, 120f, num5), "Session:", guistyle2);
			GUI.Label(new Rect(num11, num9, num12, num5), text, guistyle3);
		}
	}
	public static void DrawFarmGrid()
	{
		float num = (float)MiscConfig.autoFarmDistance;
		EspManager._gridFarmCacheTimer += Time.deltaTime;
		bool flag = EspManager._gridFarmCacheTimer > 1f;
		bool flag2 = flag;
		if (flag2)
		{
			EspManager._gridFarmCacheTimer = 0f;
			EspManager._gridFarmCache = UnityEngine.Object.FindObjectsOfType<InteractableFarm>();
		}
		InteractableFarm[] gridFarmCache = EspManager._gridFarmCache;
		bool flag3 = gridFarmCache == null;
		if (!flag3)
		{
			Color color = new Color32(0, byte.MaxValue, 0, 200);
			Color color2 = new Color32(byte.MaxValue, 165, 0, 200);
			Color color3 = new Color32(byte.MaxValue, byte.MaxValue, 0, 200);
			Color color4 = new Color32(byte.MaxValue, 60, 60, 200);
			Vector3 position = Player.player.transform.position;
			foreach (InteractableFarm interactableFarm in gridFarmCache)
			{
				bool flag4 = interactableFarm == null || !interactableFarm.transform.position.IsOnScreen() || Vector3.Distance(position, interactableFarm.transform.position) > num;
				bool flag5 = flag4;
				if (!flag5)
				{
					Vector3 position2 = interactableFarm.transform.position;
					float num2 = 0.5f;
					bool isFullyGrown = interactableFarm.IsFullyGrown;
					bool flag6 = interactableFarm.planted == 0U;
					bool flag7 = !isFullyGrown && !flag6 && interactableFarm.canFertilize;
					bool flag8 = isFullyGrown;
					Color color5;
					string text;
					if (flag8)
					{
						color5 = color;
						text = "Harvest";
					}
					else
					{
						bool flag9 = flag6;
						if (flag9)
						{
							color5 = color4;
							text = "Plant";
						}
						else
						{
							bool flag10 = flag7;
							if (flag10)
							{
								color5 = color2;
								text = "Fertilize";
							}
							else
							{
								color5 = color3;
								text = "Growing";
							}
						}
					}
					Vector3 vector = new Vector3(position2.x - num2, position2.y + 0.1f, position2.z - num2).WorldToScreenPoint();
					Vector3 vector2 = new Vector3(position2.x + num2, position2.y + 0.1f, position2.z - num2).WorldToScreenPoint();
					Vector3 vector3 = new Vector3(position2.x + num2, position2.y + 0.1f, position2.z + num2).WorldToScreenPoint();
					Vector3 vector4 = new Vector3(position2.x - num2, position2.y + 0.1f, position2.z + num2).WorldToScreenPoint();
					EspDrawer.DrawLine(vector, vector2, color5, 2f);
					EspDrawer.DrawLine(vector2, vector3, color5, 2f);
					EspDrawer.DrawLine(vector3, vector4, color5, 2f);
					EspDrawer.DrawLine(vector4, vector, color5, 2f);
					Vector3 vector5 = new Vector3(position2.x - num2, position2.y + 1f, position2.z - num2).WorldToScreenPoint();
					Vector3 vector6 = new Vector3(position2.x + num2, position2.y + 1f, position2.z - num2).WorldToScreenPoint();
					Vector3 vector7 = new Vector3(position2.x + num2, position2.y + 1f, position2.z + num2).WorldToScreenPoint();
					Vector3 vector8 = new Vector3(position2.x - num2, position2.y + 1f, position2.z + num2).WorldToScreenPoint();
					EspDrawer.DrawLine(vector5, vector6, color5, 1f);
					EspDrawer.DrawLine(vector6, vector7, color5, 1f);
					EspDrawer.DrawLine(vector7, vector8, color5, 1f);
					EspDrawer.DrawLine(vector8, vector5, color5, 1f);
					EspDrawer.DrawLine(vector, vector5, color5, 1f);
					EspDrawer.DrawLine(vector2, vector6, color5, 1f);
					EspDrawer.DrawLine(vector3, vector7, color5, 1f);
					EspDrawer.DrawLine(vector4, vector8, color5, 1f);
					EspDrawer.DrawDistanceLabel(position2, text, color5, 10);
				}
			}
			bool showFarmGridPlacement = MiscConfig.showFarmGridPlacement;
			if (showFarmGridPlacement)
			{
				Color color6 = new Color32(0, 200, byte.MaxValue, 200);
				Vector3 position3 = Player.player.transform.position;
				EspManager._gridPlacementCacheTimer += Time.deltaTime;
				bool flag11 = EspManager._gridPlacementCacheTimer > 0.5f || (position3 - EspManager._gridPlacementLastPos).sqrMagnitude > 1f;
				bool flag12 = flag11;
				if (flag12)
				{
					EspManager._gridPlacementCacheTimer = 0f;
					EspManager._gridPlacementLastPos = position3;
					EspManager._gridPlacementCache.Clear();
					float num3 = Mathf.Min((float)MiscConfig.autoFarmDistance, 10f);
					float num4 = 2f;
					for (float num5 = -num3; num5 <= num3; num5 += num4)
					{
						for (float num6 = -num3; num6 <= num3; num6 += num4)
						{
							Vector3 vector9 = position3 + new Vector3(num5, 0f, num6);
							RaycastHit raycastHit;
							bool flag13 = Physics.Raycast(vector9 + Vector3.up * 5f, Vector3.down, out raycastHit, 10f, RayMasks.BARRICADE_INTERACT);
							if (flag13)
							{
								bool flag14 = raycastHit.normal.y > 0.75f && raycastHit.collider.transform.CompareTag("Ground");
								if (flag14)
								{
									bool flag15 = false;
									for (int j = 0; j < gridFarmCache.Length; j++)
									{
										bool flag16 = gridFarmCache[j] != null && Vector3.Distance(raycastHit.point, gridFarmCache[j].transform.position) < 2f;
										if (flag16)
										{
											flag15 = true;
											break;
										}
									}
									bool flag17 = !flag15;
									if (flag17)
									{
										EspManager._gridPlacementCache.Add(raycastHit.point);
									}
								}
							}
						}
					}
				}
				float num7 = 0.9f;
				for (int k = 0; k < EspManager._gridPlacementCache.Count; k++)
				{
					Vector3 vector10 = EspManager._gridPlacementCache[k];
					Vector3 up = Vector3.up;
					Vector3 right = Vector3.right;
					Vector3 forward = Vector3.forward;
					Vector3 vector11 = up * 0.1f;
					Vector3 vector12 = (vector10 + vector11 - right * num7 - forward * num7).WorldToScreenPoint();
					Vector3 vector13 = (vector10 + vector11 + right * num7 - forward * num7).WorldToScreenPoint();
					Vector3 vector14 = (vector10 + vector11 + right * num7 + forward * num7).WorldToScreenPoint();
					Vector3 vector15 = (vector10 + vector11 - right * num7 + forward * num7).WorldToScreenPoint();
					EspDrawer.DrawLine(vector12, vector13, color6, 2f);
					EspDrawer.DrawLine(vector13, vector14, color6, 2f);
					EspDrawer.DrawLine(vector14, vector15, color6, 2f);
					EspDrawer.DrawLine(vector15, vector12, color6, 2f);
				}
			}
			bool flag18 = AutomationBot.gridFrozen && AutomationBot.frozenGridPoints.Count > 0;
			if (flag18)
			{
				Color color7 = new Color32(byte.MaxValue, 100, byte.MaxValue, 220);
				float num8 = 0.5f;
				for (int l = 0; l < AutomationBot.frozenGridPoints.Count; l++)
				{
					Vector3 vector16 = AutomationBot.frozenGridPoints[l];
					bool flag19 = !vector16.IsOnScreen();
					if (!flag19)
					{
						Vector3 vector17 = new Vector3(vector16.x - num8, vector16.y + 0.1f, vector16.z - num8).WorldToScreenPoint();
						Vector3 vector18 = new Vector3(vector16.x + num8, vector16.y + 0.1f, vector16.z - num8).WorldToScreenPoint();
						Vector3 vector19 = new Vector3(vector16.x + num8, vector16.y + 0.1f, vector16.z + num8).WorldToScreenPoint();
						Vector3 vector20 = new Vector3(vector16.x - num8, vector16.y + 0.1f, vector16.z + num8).WorldToScreenPoint();
						EspDrawer.DrawLine(vector17, vector18, color7, 2f);
						EspDrawer.DrawLine(vector18, vector19, color7, 2f);
						EspDrawer.DrawLine(vector19, vector20, color7, 2f);
						EspDrawer.DrawLine(vector20, vector17, color7, 2f);
					}
				}
			}
		}
	}
	public static void DrawPlayerEsp(EspCategory category)
	{
		EspManager.TopIconRowCount = 0;
		EspManager.PlayerChamMaterialA.color = category.LineColors[7].Color;
		EspManager.PlayerChamMaterialC.color = category.LineColors[23].Color;
		EspManager.PlayerChamMaterialB.color = category.LineColors[15].Color;
		foreach (SteamPlayer steamPlayer in Provider.clients)
		{
			try
			{
				bool flag = steamPlayer == null || steamPlayer.player == null || steamPlayer.player.channel.IsLocalPlayer;
				bool flag2 = !flag;
				if (flag2)
				{
					bool flag3 = steamPlayer.player.life.isDead && !(bool)EspCategories.categories[0].Options[8].SortSettings;
					bool flag4 = !flag3;
					if (flag4)
					{
						ValueTuple<ChamWireframeComponent, ChamWireframeComponent> valueTuple;
						bool flag5 = !EspManager.PlayerChamComponents.TryGetValue(steamPlayer.playerID.steamID.m_SteamID, out valueTuple);
						bool flag6 = flag5;
						if (flag6)
						{
							try
							{
								valueTuple = new ValueTuple<ChamWireframeComponent, ChamWireframeComponent>(EspManager.ThirdRenderer0Field.Get(steamPlayer.player.animator).gameObject.AddComponent<ChamWireframeComponent>(), EspManager.ThirdRenderer1Field.Get(steamPlayer.player.animator).gameObject.AddComponent<ChamWireframeComponent>());
							}
							catch (Exception ex)
							{
								Debug.LogWarning("[DrawPlayerEsp] Failed to create chams components: " + ex.Message);
							}
							EspManager.PlayerChamComponents.Add(steamPlayer.playerID.steamID.m_SteamID, valueTuple);
						}
						else
						{
							bool flag7 = valueTuple.Item1 == null || valueTuple.Item2 == null;
							bool flag8 = flag7;
							if (flag8)
							{
								try
								{
									bool flag9 = valueTuple.Item1 == null;
									bool flag10 = flag9;
									if (flag10)
									{
										valueTuple.Item1 = EspManager.ThirdRenderer0Field.Get(steamPlayer.player.animator).gameObject.AddComponent<ChamWireframeComponent>();
									}
									bool flag11 = valueTuple.Item2 == null;
									bool flag12 = flag11;
									if (flag12)
									{
										valueTuple.Item2 = EspManager.ThirdRenderer1Field.Get(steamPlayer.player.animator).gameObject.AddComponent<ChamWireframeComponent>();
									}
								}
								catch (Exception ex2)
								{
									Debug.LogWarning("[DrawPlayerEsp] Failed to create chams component: " + ex2.Message);
								}
								EspManager.PlayerChamComponents[steamPlayer.playerID.steamID.m_SteamID] = valueTuple;
							}
						}
						bool flag13 = valueTuple.Item1 == null && valueTuple.Item2 == null;
						if (!flag13)
						{
							bool flag14 = !EspDrawer.IsWithinRange(category, steamPlayer.player.gameObject) && (!(bool)category.Options[2].SortSettings || !PlayerPriorityManager.IsFriendOrGroupMatePlayer(steamPlayer.player)) && (!(bool)category.Options[3].SortSettings || !PlayerPriorityManager.IsEnemyPlayer(steamPlayer.player));
							bool flag15 = flag14;
							if (flag15)
							{
								try
								{
									bool flag16 = valueTuple.Item1 != null;
									if (flag16)
									{
										valueTuple.Item1.RestoreOriginalMaterials();
									}
									bool flag17 = valueTuple.Item2 != null;
									if (flag17)
									{
										valueTuple.Item2.RestoreOriginalMaterials();
									}
								}
								catch (Exception ex3)
								{
									Debug.LogWarning("[DrawPlayerEsp] Chams restore error: " + ex3.Message);
								}
								EspManager.DUpdateWireframeForPlayer(valueTuple, category.WireframeEnabled);
							}
							else
							{
								bool flag18 = PlayerPriorityManager.IsFriendOrGroupMatePlayer(steamPlayer.player);
								bool flag19 = flag18;
								int num;
								int num2;
								int num3;
								int num4;
								int num5;
								int num6;
								int num7;
								if (flag19)
								{
									num = 8;
									num2 = 9;
									num3 = 10;
									num4 = 11;
									num5 = 12;
									num6 = 13;
									num7 = 14;
									try
									{
										bool d2JTP060dwZSwDD85nvmWwK0A = category.SettingFlag11;
										bool flag20 = d2JTP060dwZSwDD85nvmWwK0A;
										if (flag20)
										{
											bool flag21 = valueTuple.Item1 != null;
											if (flag21)
											{
												valueTuple.Item1.ApplyChamMaterial(EspManager.PlayerChamMaterialB);
											}
											bool flag22 = valueTuple.Item2 != null;
											if (flag22)
											{
												valueTuple.Item2.ApplyChamMaterial(EspManager.PlayerChamMaterialB);
											}
										}
										else
										{
											bool flag23 = valueTuple.Item1 != null;
											if (flag23)
											{
												valueTuple.Item1.RestoreOriginalMaterials();
											}
											bool flag24 = valueTuple.Item2 != null;
											if (flag24)
											{
												valueTuple.Item2.RestoreOriginalMaterials();
											}
										}
									}
									catch (Exception ex4)
									{
										Debug.LogWarning("[DrawPlayerEsp] Chams apply error (friend): " + ex4.Message);
									}
								}
								else
								{
									bool flag25 = PlayerPriorityManager.IsEnemyPlayer(steamPlayer.player);
									bool flag26 = flag25;
									if (flag26)
									{
										num = 15;
										num2 = 16;
										num3 = 17;
										num4 = 18;
										num5 = 19;
										num6 = 20;
										num7 = 21;
										try
										{
											bool d2JTP060dwZSwDD85nvmWwK0A2 = category.SettingFlag11;
											bool flag27 = d2JTP060dwZSwDD85nvmWwK0A2;
											if (flag27)
											{
												bool flag28 = valueTuple.Item1 != null;
												if (flag28)
												{
													valueTuple.Item1.ApplyChamMaterial(EspManager.PlayerChamMaterialC);
												}
												bool flag29 = valueTuple.Item2 != null;
												if (flag29)
												{
													valueTuple.Item2.ApplyChamMaterial(EspManager.PlayerChamMaterialC);
												}
											}
											else
											{
												bool flag30 = valueTuple.Item1 != null;
												if (flag30)
												{
													valueTuple.Item1.RestoreOriginalMaterials();
												}
												bool flag31 = valueTuple.Item2 != null;
												if (flag31)
												{
													valueTuple.Item2.RestoreOriginalMaterials();
												}
											}
										}
										catch (Exception ex5)
										{
											Debug.LogWarning("[DrawPlayerEsp] Chams apply error (enemy): " + ex5.Message);
										}
									}
									else
									{
										num = 0;
										num2 = 1;
										num3 = 2;
										num4 = 3;
										num5 = 4;
										num6 = 5;
										num7 = 6;
										try
										{
											bool d2JTP060dwZSwDD85nvmWwK0A3 = category.SettingFlag11;
											bool flag32 = d2JTP060dwZSwDD85nvmWwK0A3;
											if (flag32)
											{
												bool flag33 = valueTuple.Item1 != null;
												if (flag33)
												{
													valueTuple.Item1.ApplyChamMaterial(EspManager.PlayerChamMaterialA);
												}
												bool flag34 = valueTuple.Item2 != null;
												if (flag34)
												{
													valueTuple.Item2.ApplyChamMaterial(EspManager.PlayerChamMaterialA);
												}
											}
											else
											{
												bool flag35 = valueTuple.Item1 != null;
												if (flag35)
												{
													valueTuple.Item1.RestoreOriginalMaterials();
												}
												bool flag36 = valueTuple.Item2 != null;
												if (flag36)
												{
													valueTuple.Item2.RestoreOriginalMaterials();
												}
											}
										}
										catch (Exception ex6)
										{
											Debug.LogWarning("[DrawPlayerEsp] Chams apply error (default): " + ex6.Message);
										}
									}
								}
								EspManager.DUpdateWireframeForPlayer(valueTuple, category.WireframeEnabled);
								bool flag37 = true;
								bool flag38 = (bool)category.Options[12].SortSettings;
								if (flag38)
								{
									bool flag39 = MainCamera.instance != null && steamPlayer.player.look != null;
									if (flag39)
									{
										Vector3 position = MainCamera.instance.transform.position;
										Vector3 position2 = steamPlayer.player.look.aim.position;
										RaycastHit raycastHit;
										bool flag40 = Physics.Linecast(position, position2, out raycastHit, RayMasks.DAMAGE_CLIENT);
										if (flag40)
										{
											bool flag41 = raycastHit.transform != steamPlayer.player.transform && !raycastHit.transform.IsChildOf(steamPlayer.player.transform) && raycastHit.transform != Player.player.transform && !raycastHit.transform.IsChildOf(Player.player.transform);
											if (flag41)
											{
												flag37 = false;
											}
										}
									}
								}
								Color32 color = category.LineColors[num].Color;
								Color32 color2 = category.LineColors[num4].Color;
								Color32 dkJGdJpvFP4j4uWN4CyFixyQ = category.LineColors[num2].Color;
								Color32 color3 = category.LineColors[num7].Color;
								bool flag42 = (bool)category.Options[12].SortSettings;
								if (flag42)
								{
									Color32 color4 = (flag37 ? new Color32(0, byte.MaxValue, 0, byte.MaxValue) : new Color32(byte.MaxValue, 0, 0, byte.MaxValue));
									color = color4;
									color2 = color4;
									color3 = color4;
									dkJGdJpvFP4j4uWN4CyFixyQ = new Color32(color4.r, color4.g, color4.b, 20);
								}
								bool flag43 = category.Options.Length > 14 && (bool)category.Options[14].SortSettings;
								bool flag44 = flag43;
								if (flag44)
								{
									Vector3 position3 = steamPlayer.player.transform.position;
									Vector3 vector = EspDrawer.RenderCamera.WorldToScreenPoint(position3);
									bool flag45 = vector.z < 0f || vector.x < 0f || vector.x > (float)Screen.width || vector.y < 0f || vector.y > (float)Screen.height;
									if (flag45)
									{
										float num8 = ((category.Options.Length > 15) ? Convert.ToSingle(category.Options[15].SortSettings) : 20f);
										float num9 = ((category.Options.Length > 16) ? Convert.ToSingle(category.Options[16].SortSettings) : 100f);
										Vector2 vector2 = new Vector2((float)Screen.width / 2f, (float)Screen.height / 2f);
										vector.y = (float)Screen.height - vector.y;
										bool flag46 = vector.z < 0f;
										if (flag46)
										{
											vector.x = (float)Screen.width - vector.x;
											vector.y = (float)Screen.height - vector.y;
										}
										Vector2 normalized = (new Vector2(vector.x, vector.y) - vector2).normalized;
										Vector2 vector3 = vector2 + normalized * num9;
										Vector2 vector4 = vector3 + normalized * num8;
										Vector2 vector5 = vector3 - normalized * (num8 * 0.5f) + new Vector2(-normalized.y, normalized.x) * (num8 * 0.5f);
										Vector2 vector6 = vector3 - normalized * (num8 * 0.5f) - new Vector2(-normalized.y, normalized.x) * (num8 * 0.5f);
										EspDrawer.DrawFilledTriangle(vector4, vector5, vector6, color2);
									}
								}
								bool flag47 = !steamPlayer.player.transform.position.IsOnScreen();
								if (!flag47)
								{
									EspManager.DefaultPlayerBounds.center = steamPlayer.player.transform.position + EspManager.PlayerNameOffset;
									EspDrawer.DrawBoundsBox(category, EspManager.DefaultPlayerBounds, color, color2, dkJGdJpvFP4j4uWN4CyFixyQ);
									bool flag48 = category.Options.Length > 17 && (bool)category.Options[17].SortSettings;
									bool flag49 = category.Options.Length > 18 && (bool)category.Options[18].SortSettings;
									bool flag50 = flag48 || flag49;
									if (flag50)
									{
										float num10 = 30f;
										bool flag51 = category.Options.Length > 20;
										if (flag51)
										{
											num10 = Convert.ToSingle(category.Options[20].SortSettings);
										}
										bool flag52 = EspManager.DiconBuffer == null;
										if (flag52)
										{
											EspManager.DiconBuffer = new List<Texture2D>();
										}
										List<Texture2D> diconBuffer = EspManager.DiconBuffer;
										diconBuffer.Clear();
										EspManager.DAddIcon(steamPlayer.player.clothing.hatAsset, diconBuffer);
										EspManager.DAddIcon(steamPlayer.player.clothing.vestAsset, diconBuffer);
										EspManager.DAddIcon(steamPlayer.player.clothing.shirtAsset, diconBuffer);
										EspManager.DAddIcon(steamPlayer.player.clothing.pantsAsset, diconBuffer);
										EspManager.DAddIcon(steamPlayer.player.clothing.backpackAsset, diconBuffer);
										try
										{
											Items items = steamPlayer.player.inventory.items[0];
											bool flag53 = items != null && items.items.Count > 0;
											if (flag53)
											{
												EspManager.DAddIcon(steamPlayer.player.inventory.items[0].items[0].item.GetAsset(), diconBuffer);
											}
											Items items2 = steamPlayer.player.inventory.items[1];
											bool flag54 = items2 != null && items2.items.Count > 0;
											if (flag54)
											{
												EspManager.DAddIcon(steamPlayer.player.inventory.items[1].items[0].item.GetAsset(), diconBuffer);
											}
										}
										catch (Exception ex7)
										{
											Debug.LogWarning("[DrawPlayerEsp] Icon inventory error: " + ex7.Message);
										}
										bool flag55 = diconBuffer.Count > 0;
										if (flag55)
										{
											float num11 = (float)diconBuffer.Count * num10;
											bool flag56 = flag48;
											if (flag56)
											{
												float num12 = ((float)Screen.width - num11) / 2f;
												float num13 = 10f + (float)EspManager.TopIconRowCount * (num10 + 4f);
												for (int i = 0; i < diconBuffer.Count; i++)
												{
													Rect rect = new Rect(num12 + (float)i * num10, num13, num10, num10);
													GUI.DrawTexture(new Rect(Mathf.Round(rect.x), Mathf.Round(rect.y), Mathf.Round(rect.width), Mathf.Round(rect.height)), diconBuffer[i]);
												}
												EspManager.TopIconRowCount++;
											}
											bool flag57 = flag49;
											if (flag57)
											{
												Vector3 vector7 = EspDrawer.RenderCamera.WorldToScreenPoint(steamPlayer.player.transform.position + new Vector3(0f, 2f, 0f));
												bool flag58 = vector7.z > 0f;
												if (flag58)
												{
													vector7.y = (float)Screen.height - vector7.y;
													float num14 = vector7.x - num11 / 2f;
													for (int j = 0; j < diconBuffer.Count; j++)
													{
														Rect rect2 = new Rect(num14 + (float)j * num10, vector7.y - num10 - 10f, num10, num10);
														GUI.DrawTexture(new Rect(Mathf.Round(rect2.x), Mathf.Round(rect2.y), Mathf.Round(rect2.width), Mathf.Round(rect2.height)), diconBuffer[j]);
													}
												}
											}
										}
									}
									bool dsy8h4Odqh4GzyIBRSA0YZwCx = category.SettingFlag14;
									bool flag59 = dsy8h4Odqh4GzyIBRSA0YZwCx;
									if (flag59)
									{
										EspDrawer.DrawTracer(category.ScrollPosition, steamPlayer.player.transform.position, category.LineColors[num3].Color, category.LineColors[num4].Color, category.SettingFlag15);
									}
									bool flag60 = (bool)category.Options[4].SortSettings;
									bool flag61 = flag60;
									if (flag61)
									{
										EspManager.DrawPlayerEquipIcon(steamPlayer.player);
									}
									bool flag62 = (bool)category.Options[0].SortSettings;
									bool flag63 = flag62;
									if (flag63)
									{
										try
										{
											float num15 = 1f;
											bool flag64 = category.Options.Length > 13;
											if (flag64)
											{
												num15 = Convert.ToSingle(category.Options[13].SortSettings);
											}
											EspManager.DrawPlayerSkeletonLines(steamPlayer.player.GetOrAddPlayerBones(), color3, num15);
										}
										catch
										{
										}
									}
									bool darar3adCBuubvdP0AmlvE03C = category.SettingFlag13;
									bool flag65 = darar3adCBuubvdP0AmlvE03C;
									if (flag65)
									{
										EspManager.TargetSteamPlayerId = steamPlayer.playerID;
										bool flag66 = (bool)category.Options[6].SortSettings;
										bool flag67 = flag66;
										if (flag67)
										{
											EspManager.DrawPlayerAvatarIcon(steamPlayer.player, (int)(category.TextStyle.CalcSize(new GUIContent(EspManager.CachedFormatString)).x / 2f) + (int)EspCategories.categories[0].Options[7].SortSettings);
										}
										foreach (EspTextEntry drtz0MPdBhZh1V5PmbtgwG0pX in category.LineEntries)
										{
											string text2;
											try
											{
												ItemAsset itemAsset = steamPlayer.player.equipment.asset;
												bool flag68 = itemAsset == null && steamPlayer.player.equipment.itemID > 0;
												if (flag68)
												{
													itemAsset = (ItemAsset)Assets.find(EAssetType.ITEM, steamPlayer.player.equipment.itemID);
												}
												string text = ((itemAsset != null) ? itemAsset.itemName : "None");
												bool flag69 = category.Options.Length > 19 && (bool)category.Options[19].SortSettings;
												if (flag69)
												{
													ItemGunAsset itemGunAsset = itemAsset as ItemGunAsset;
													bool flag70 = itemGunAsset != null && steamPlayer.player.equipment.state != null && steamPlayer.player.equipment.state.Length >= 10;
													if (flag70)
													{
														byte b = steamPlayer.player.equipment.state[10];
														byte b2 = itemGunAsset.ammoMax;
														try
														{
															ushort num16 = BitConverter.ToUInt16(steamPlayer.player.equipment.state, 8);
															ItemAsset itemAsset2 = (ItemAsset)Assets.find(EAssetType.ITEM, num16);
															ItemMagazineAsset itemMagazineAsset = itemAsset2 as ItemMagazineAsset;
															bool flag71 = itemMagazineAsset != null;
															if (flag71)
															{
																b2 = itemMagazineAsset.amount;
															}
														}
														catch
														{
														}
														text += string.Format(" [{0}/{1}]", b, b2);
													}
												}
												text2 = string.Format(drtz0MPdBhZh1V5PmbtgwG0pX.formatText, new object[]
												{
													"{0}",
													EspManager.TargetSteamPlayerId.playerName,
													(EspManager.TargetSteamPlayerId.characterName != null) ? EspManager.TargetSteamPlayerId.characterName : EspManager.TargetSteamPlayerId.playerName,
													(EspManager.TargetSteamPlayerId.nickName != null) ? EspManager.TargetSteamPlayerId.nickName : EspManager.TargetSteamPlayerId.playerName,
													text,
													steamPlayer.player.life.isDead ? ((string)EspCategories.categories[0].Options[9].SortSettings) : "",
													((bool)EspCategories.categories[0].Options[10].SortSettings) ? (LevelNodes.isPointInsideSafezone(steamPlayer.player.transform.position, out steamPlayer.player.movement.isSafeInfo) ? ((string)EspCategories.categories[0].Options[11].SortSettings) : "") : ""
												});
											}
											catch
											{
												text2 = string.Format(drtz0MPdBhZh1V5PmbtgwG0pX.formatText, new object[] { "{0}", "ERROR", "ERROR", "ERROR", "ERROR", "ERROR", "ERROR" });
											}
											EspDrawer.DrawTextEntry(category, drtz0MPdBhZh1V5PmbtgwG0pX, text2, steamPlayer.player.transform.position, category.LineColors[num5].Color, category.LineColors[num6].Color);
										}
									}
									else
									{
										bool flag72 = (bool)category.Options[6].SortSettings;
										bool flag73 = flag72;
										if (flag73)
										{
											EspManager.DrawPlayerAvatarIcon(steamPlayer.player, (int)EspCategories.categories[0].Options[7].SortSettings / 2);
										}
									}
								}
							}
						}
					}
				}
			}
			catch
			{
			}
		}
	}
	private static void DrawPlayerSkeletonLines(PlayerBones cp, Color32 lineColor, float thickness = 1f)
	{
		bool flag = cp.bones.HeadBone == null;
		bool flag2 = !flag;
		if (flag2)
		{
			EspManager.PlayerBoneScreenPositions[0] = cp.bones.HeadBone.position.WorldToScreenPoint();
			EspManager.PlayerBoneScreenPositions[1] = cp.bones.SpineBone.position.WorldToScreenPoint();
			EspManager.PlayerBoneScreenPositions[2] = cp.bones.LeftArmBone.position.WorldToScreenPoint();
			EspManager.PlayerBoneScreenPositions[3] = cp.bones.LeftHandBone.position.WorldToScreenPoint();
			EspManager.PlayerBoneScreenPositions[4] = cp.bones.RightArmBone.position.WorldToScreenPoint();
			EspManager.PlayerBoneScreenPositions[5] = cp.bones.RightHandBone.position.WorldToScreenPoint();
			EspManager.PlayerBoneScreenPositions[6] = cp.bones.LeftLegBone.position.WorldToScreenPoint();
			EspManager.PlayerBoneScreenPositions[7] = cp.bones.RightLegBone.position.WorldToScreenPoint();
			EspManager.PlayerBoneScreenPositions[8] = cp.bones.LeftFootBone.position.WorldToScreenPoint();
			EspManager.PlayerBoneScreenPositions[9] = cp.bones.RightFootBone.position.WorldToScreenPoint();
			EspManager.PlayerBoneScreenPositions[10] = cp.bones.LeftHookBone.position.WorldToScreenPoint();
			EspManager.PlayerBoneScreenPositions[11] = cp.bones.RightHookBone.position.WorldToScreenPoint();
			EspManager.PlayerBoneScreenPositions[12] = (cp.bones.HeadBone.position + MathUtil.DirectionTo(cp.bones.SpineBone.position, cp.bones.HeadBone.position) * 0.4f).WorldToScreenPoint();
			EspManager.PlayerBoneScreenPositions[13] = (cp.bones.LeftFootBone.position + MathUtil.DirectionTo(cp.bones.LeftLegBone.position, cp.bones.LeftFootBone.position) * 0.4f).WorldToScreenPoint();
			EspManager.PlayerBoneScreenPositions[14] = (cp.bones.RightFootBone.position + MathUtil.DirectionTo(cp.bones.RightLegBone.position, cp.bones.RightFootBone.position) * 0.4f).WorldToScreenPoint();
			EspDrawer.DrawLine(EspManager.PlayerBoneScreenPositions[0], EspManager.PlayerBoneScreenPositions[12], lineColor, thickness);
			EspDrawer.DrawLine(EspManager.PlayerBoneScreenPositions[0], EspManager.PlayerBoneScreenPositions[1], lineColor, thickness);
			EspDrawer.DrawLine(EspManager.PlayerBoneScreenPositions[0], EspManager.PlayerBoneScreenPositions[2], lineColor, thickness);
			EspDrawer.DrawLine(EspManager.PlayerBoneScreenPositions[0], EspManager.PlayerBoneScreenPositions[4], lineColor, thickness);
			EspDrawer.DrawLine(EspManager.PlayerBoneScreenPositions[1], EspManager.PlayerBoneScreenPositions[6], lineColor, thickness);
			EspDrawer.DrawLine(EspManager.PlayerBoneScreenPositions[1], EspManager.PlayerBoneScreenPositions[7], lineColor, thickness);
			EspDrawer.DrawLine(EspManager.PlayerBoneScreenPositions[6], EspManager.PlayerBoneScreenPositions[8], lineColor, thickness);
			EspDrawer.DrawLine(EspManager.PlayerBoneScreenPositions[7], EspManager.PlayerBoneScreenPositions[9], lineColor, thickness);
			EspDrawer.DrawLine(EspManager.PlayerBoneScreenPositions[8], EspManager.PlayerBoneScreenPositions[13], lineColor, thickness);
			EspDrawer.DrawLine(EspManager.PlayerBoneScreenPositions[9], EspManager.PlayerBoneScreenPositions[14], lineColor, thickness);
			EspDrawer.DrawLine(EspManager.PlayerBoneScreenPositions[2], EspManager.PlayerBoneScreenPositions[3], lineColor, thickness);
			EspDrawer.DrawLine(EspManager.PlayerBoneScreenPositions[4], EspManager.PlayerBoneScreenPositions[5], lineColor, thickness);
			EspDrawer.DrawLine(EspManager.PlayerBoneScreenPositions[5], EspManager.PlayerBoneScreenPositions[11], lineColor, thickness);
			EspDrawer.DrawLine(EspManager.PlayerBoneScreenPositions[3], EspManager.PlayerBoneScreenPositions[10], lineColor, thickness);
		}
	}
	private static void DrawPlayerAvatarIcon(Player player, int x)
	{
		Texture2D texture2D = GuiStyles.GetSteamAvatarIcon(player.channel.owner.playerID.steamID);
		bool flag = texture2D != null;
		bool flag2 = flag;
		if (flag2)
		{
			Vector3 vector = player.transform.position.WorldToScreenPoint();
			int num = (int)EspCategories.categories[0].Options[7].SortSettings;
			Rect rect = new Rect(vector.x - (float)x, vector.y, (float)num, (float)num);
			GUI.DrawTexture(new Rect(Mathf.Round(rect.x), Mathf.Round(rect.y), Mathf.Round(rect.width), Mathf.Round(rect.height)), texture2D, ScaleMode.ScaleToFit);
		}
	}
	private static void DrawPlayerEquipIcon(Player player)
	{
		bool flag = player.equipment.asset == null && player.equipment.itemID == 0;
		bool flag2 = !flag;
		if (flag2)
		{
			Texture2D texture2D = GuiStyles.GetPlayerEquippedItemIcon(player);
			bool flag3 = texture2D != null;
			bool flag4 = flag3;
			if (flag4)
			{
				Vector3 vector = player.transform.position.WorldToScreenPoint();
				int num = (int)EspCategories.categories[0].Options[5].SortSettings;
				Rect rect = new Rect(vector.x - (float)(num / 2), vector.y, (float)num, (float)num);
				GUI.DrawTexture(new Rect(Mathf.Round(rect.x), Mathf.Round(rect.y), Mathf.Round(rect.width), Mathf.Round(rect.height)), texture2D, ScaleMode.ScaleToFit);
			}
		}
	}
	public static void DrawVehicleEsp(EspCategory category)
	{
		category.DrawMaterial.color = category.LineColors[6].Color;
		foreach (InteractableVehicle interactableVehicle in VehicleManager.vehicles)
		{
			bool flag = interactableVehicle == null || interactableVehicle.isDead || (Player.player != null && Player.player.movement.getVehicle() == interactableVehicle) || ((bool)category.Options[0].SortSettings && interactableVehicle.isLocked) || !EspDrawer.IsVisibleInRange(category, interactableVehicle.gameObject);
			bool flag2 = !flag;
			if (flag2)
			{
				EspDrawer.DrawBoundsBox(category, EspManager.GetCachedRendererBounds(interactableVehicle.GetNetId().id, interactableVehicle.gameObject, EspManager.VehicleBoundsCache), category.LineColors[0].Color, category.LineColors[3].Color, category.LineColors[1].Color);
				bool dsy8h4Odqh4GzyIBRSA0YZwCx = category.SettingFlag14;
				bool flag3 = dsy8h4Odqh4GzyIBRSA0YZwCx;
				if (flag3)
				{
					EspDrawer.DrawTracer(category.ScrollPosition, interactableVehicle.transform.position, category.LineColors[2].Color, category.LineColors[3].Color, category.SettingFlag15);
				}
				bool darar3adCBuubvdP0AmlvE03C = category.SettingFlag13;
				bool flag4 = darar3adCBuubvdP0AmlvE03C;
				if (flag4)
				{
					foreach (EspTextEntry drtz0MPdBhZh1V5PmbtgwG0pX in category.LineEntries)
					{
						EspDrawer.DrawTextEntry(category, drtz0MPdBhZh1V5PmbtgwG0pX, string.Format(drtz0MPdBhZh1V5PmbtgwG0pX.formatText, "{0}", interactableVehicle.asset.vehicleName, interactableVehicle.isLocked ? "Locked" : "Unlocked"), interactableVehicle.transform.position, category.LineColors[4].Color, category.LineColors[5].Color);
					}
				}
				bool flag5 = category.Options != null && category.Options.Length > 1 && (bool)category.Options[1].SortSettings;
				if (flag5)
				{
					float num = (float)interactableVehicle.health / (float)interactableVehicle.asset.health;
					Bounds bounds = EspManager.GetCachedRendererBounds(interactableVehicle.GetNetId().id, interactableVehicle.gameObject, EspManager.VehicleBoundsCache);
					Vector3 vector = EspDrawer.RenderCamera.WorldToScreenPoint(bounds.center);
					bool flag6 = vector.z > 0f;
					if (flag6)
					{
						Rect boundsScreenRect = EspDrawer.GetBoundsScreenRect(bounds);
						float num2 = Mathf.Max(boundsScreenRect.width, 10f);
						float num3 = 8f / vector.z;
						num3 = Mathf.Clamp(num3, 4f, 10f);
						EspDrawer.DrawLine(new Vector2(boundsScreenRect.center.x - num2 / 2f, boundsScreenRect.yMax + 10f), new Vector2(boundsScreenRect.center.x + num2 / 2f, boundsScreenRect.yMax + 10f), new Color(0.1f, 0.1f, 0.1f, 0.9f), num3 + 3f);
						EspDrawer.DrawLine(new Vector2(boundsScreenRect.center.x - num2 / 2f + 1f, boundsScreenRect.yMax + 10f), new Vector2(boundsScreenRect.center.x + num2 / 2f - 1f, boundsScreenRect.yMax + 10f), new Color(0.2f, 0.2f, 0.2f, 0.8f), num3);
						bool flag7 = num > 0f;
						if (flag7)
						{
							EspDrawer.DrawLine(new Vector2(boundsScreenRect.center.x - num2 / 2f + 1f, boundsScreenRect.yMax + 10f), new Vector2(boundsScreenRect.center.x - num2 / 2f + 1f + (num2 - 2f) * num, boundsScreenRect.yMax + 10f), Color.Lerp(new Color(1f, 0.2f, 0.2f), new Color(0.2f, 1f, 0.2f), num), num3);
						}
					}
				}
				bool d2JTP060dwZSwDD85nvmWwK0A = category.SettingFlag11;
				bool flag8 = d2JTP060dwZSwDD85nvmWwK0A;
				if (flag8)
				{
					EspManager.ApplyChamsForIdUInt(interactableVehicle.GetNetId().id, interactableVehicle.gameObject, EspManager.VehicleChamList, category);
				}
				else
				{
					EspManager.RemoveChamsForIdUInt(interactableVehicle.GetNetId().id, interactableVehicle.gameObject, EspManager.VehicleChamList, category);
				}
			}
		}
	}
	public static void CollectEspItems(EspCategory category)
	{
		EspManager.VisibleItemList.Clear();
		foreach (InteractableItem interactableItem in InteractableItemPatches.TrackedItems)
		{
			bool flag = interactableItem != null && EspDrawer.IsWithinRange(category, interactableItem.gameObject) && AutomationBot.IsItemVisibleByFilter(interactableItem);
			bool flag2 = flag;
			if (flag2)
			{
				EspManager.VisibleItemList.Add(new ValueTuple<Collider, InteractableItem>(interactableItem.GetComponent<Collider>(), interactableItem));
			}
		}
	}
	public static void DrawItemEsp(EspCategory category)
	{
		category.DrawMaterial.color = category.LineColors[6].Color;
		foreach (ValueTuple<Collider, InteractableItem> valueTuple in EspManager.VisibleItemList)
		{
			Collider item = valueTuple.Item1;
			InteractableItem item2 = valueTuple.Item2;
			bool flag = item2 == null || !EspDrawer.IsVisibleInRange(category, item2.gameObject);
			bool flag2 = !flag;
			if (flag2)
			{
				bool flag3 = item != null;
				bool flag4 = flag3;
				if (flag4)
				{
					EspDrawer.DrawBoundsBox(category, item.bounds, category.LineColors[0].Color, category.LineColors[3].Color, category.LineColors[1].Color);
				}
				bool dsy8h4Odqh4GzyIBRSA0YZwCx = category.SettingFlag14;
				bool flag5 = dsy8h4Odqh4GzyIBRSA0YZwCx;
				if (flag5)
				{
					EspDrawer.DrawTracer(category.ScrollPosition, item2.transform.position, category.LineColors[2].Color, category.LineColors[3].Color, category.SettingFlag15);
				}
				bool flag6 = (bool)category.Options[1].SortSettings;
				bool flag7 = flag6;
				if (flag7)
				{
					EspManager.DrawItemIcon(item2);
				}
				bool darar3adCBuubvdP0AmlvE03C = category.SettingFlag13;
				bool flag8 = darar3adCBuubvdP0AmlvE03C;
				if (flag8)
				{
					foreach (EspTextEntry drtz0MPdBhZh1V5PmbtgwG0pX in category.LineEntries)
					{
						EspDrawer.DrawTextEntry(category, drtz0MPdBhZh1V5PmbtgwG0pX, string.Format(drtz0MPdBhZh1V5PmbtgwG0pX.formatText, "{0}", item2.asset.itemName), item2.transform.position, category.LineColors[4].Color, category.LineColors[5].Color);
					}
				}
				bool flag9 = category.SettingFlag11 || category.WireframeEnabled;
				bool flag10 = flag9;
				if (flag10)
				{
					EspManager.ApplyChamsForIdUInt((uint)item2.GetInstanceID(), item2.gameObject, EspManager.ItemChamList, category);
				}
				else
				{
					EspManager.RemoveChamsForIdUInt((uint)item2.GetInstanceID(), item2.gameObject, EspManager.ItemChamList, category);
				}
			}
		}
	}
	private static void DrawItemIcon(InteractableItem ii)
	{
		bool flag = ii.asset == null || ii.item == null;
		bool flag2 = !flag;
		if (flag2)
		{
			Texture2D texture2D = GuiStyles.GetItemIconWithState(ii.asset.id, ii.item.state);
			bool flag3 = texture2D != null;
			bool flag4 = flag3;
			if (flag4)
			{
				Vector3 vector = ii.transform.position.WorldToScreenPoint();
				int num = (int)EspCategories.categories[1].Options[2].SortSettings;
				Rect rect = new Rect(vector.x - (float)(num / 2), vector.y, (float)num, (float)num);
				GUI.DrawTexture(new Rect(Mathf.Round(rect.x), Mathf.Round(rect.y), Mathf.Round(rect.width), Mathf.Round(rect.height)), texture2D, ScaleMode.ScaleToFit);
			}
		}
	}
	private static void DrawZombieSkeleton(Zombie zombie, Color32 lineColor, float thickness)
	{
		Transform transform = null;
		Transform transform2 = null;
		Transform transform3 = null;
		Transform transform4 = null;
		Transform transform5 = null;
		Transform transform6 = null;
		Transform transform7 = null;
		Transform transform8 = null;
		Transform transform9 = null;
		Transform transform10 = null;
		foreach (Transform transform11 in zombie.GetComponentsInChildren<Transform>())
		{
			string name = transform11.name;
			bool flag = name == "Skull";
			if (flag)
			{
				transform = transform11;
			}
			else
			{
				bool flag2 = name == "Spine";
				if (flag2)
				{
					transform2 = transform11;
				}
				else
				{
					bool flag3 = name == "Left_Arm";
					if (flag3)
					{
						transform3 = transform11;
					}
					else
					{
						bool flag4 = name == "Left_Hand";
						if (flag4)
						{
							transform4 = transform11;
						}
						else
						{
							bool flag5 = name == "Right_Arm";
							if (flag5)
							{
								transform5 = transform11;
							}
							else
							{
								bool flag6 = name == "Right_Hand";
								if (flag6)
								{
									transform6 = transform11;
								}
								else
								{
									bool flag7 = name == "Left_Leg";
									if (flag7)
									{
										transform7 = transform11;
									}
									else
									{
										bool flag8 = name == "Left_Foot";
										if (flag8)
										{
											transform8 = transform11;
										}
										else
										{
											bool flag9 = name == "Right_Leg";
											if (flag9)
											{
												transform9 = transform11;
											}
											else
											{
												bool flag10 = name == "Right_Foot";
												if (flag10)
												{
													transform10 = transform11;
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}
		bool flag11 = transform != null && transform2 != null && transform10 != null;
		if (flag11)
		{
			Vector3[] array = new Vector3[]
			{
				transform.position.WorldToScreenPoint(),
				transform2.position.WorldToScreenPoint(),
				transform3.position.WorldToScreenPoint(),
				transform4.position.WorldToScreenPoint(),
				transform5.position.WorldToScreenPoint(),
				transform6.position.WorldToScreenPoint(),
				transform7.position.WorldToScreenPoint(),
				transform9.position.WorldToScreenPoint(),
				transform8.position.WorldToScreenPoint(),
				transform10.position.WorldToScreenPoint(),
				transform4.position.WorldToScreenPoint(),
				transform6.position.WorldToScreenPoint(),
				(transform.position + MathUtil.DirectionTo(transform2.position, transform.position) * 0.4f).WorldToScreenPoint(),
				(transform8.position + MathUtil.DirectionTo(transform7.position, transform8.position) * 0.4f).WorldToScreenPoint(),
				(transform10.position + MathUtil.DirectionTo(transform9.position, transform10.position) * 0.4f).WorldToScreenPoint()
			};
			EspDrawer.DrawLine(array[0], array[12], lineColor, thickness);
			EspDrawer.DrawLine(array[0], array[1], lineColor, thickness);
			EspDrawer.DrawLine(array[0], array[2], lineColor, thickness);
			EspDrawer.DrawLine(array[0], array[4], lineColor, thickness);
			EspDrawer.DrawLine(array[1], array[6], lineColor, thickness);
			EspDrawer.DrawLine(array[1], array[7], lineColor, thickness);
			EspDrawer.DrawLine(array[6], array[8], lineColor, thickness);
			EspDrawer.DrawLine(array[7], array[9], lineColor, thickness);
			EspDrawer.DrawLine(array[8], array[13], lineColor, thickness);
			EspDrawer.DrawLine(array[9], array[14], lineColor, thickness);
			EspDrawer.DrawLine(array[2], array[3], lineColor, thickness);
			EspDrawer.DrawLine(array[4], array[5], lineColor, thickness);
			EspDrawer.DrawLine(array[5], array[11], lineColor, thickness);
			EspDrawer.DrawLine(array[3], array[10], lineColor, thickness);
		}
	}
	public static void DrawZombieEsp(EspCategory category)
	{
		category.DrawMaterial.color = category.LineColors[6].Color;
		foreach (Zombie zombie in ZombiePatches.TrackedZombies)
		{
			bool flag = zombie == null || zombie.isDead || !EspDrawer.IsVisibleInRange(category, zombie.gameObject);
			bool flag2 = !flag;
			if (flag2)
			{
				EspDrawer.DrawBoundsBox(category, EspManager.GetCachedColliderBounds(zombie.id, zombie.gameObject, EspManager.ZombieColliderCache), category.LineColors[0].Color, category.LineColors[3].Color, category.LineColors[1].Color);
				bool flag3 = category.Options != null && category.Options.Length > 1 && (bool)category.Options[0].SortSettings;
				if (flag3)
				{
					float num = Convert.ToSingle(category.Options[1].SortSettings);
					Color32 dkJGdJpvFP4j4uWN4CyFixyQ = category.LineColors[0].Color;
					EspManager.DrawZombieSkeleton(zombie, dkJGdJpvFP4j4uWN4CyFixyQ, num);
				}
				bool dsy8h4Odqh4GzyIBRSA0YZwCx = category.SettingFlag14;
				bool flag4 = dsy8h4Odqh4GzyIBRSA0YZwCx;
				if (flag4)
				{
					EspDrawer.DrawTracer(category.ScrollPosition, zombie.transform.position, category.LineColors[2].Color, category.LineColors[3].Color, category.SettingFlag15);
				}
				bool darar3adCBuubvdP0AmlvE03C = category.SettingFlag13;
				bool flag5 = darar3adCBuubvdP0AmlvE03C;
				if (flag5)
				{
					EspDrawer.DrawCategoryTexts(category, zombie.transform.position, null, category.LineColors[4].Color, category.LineColors[5].Color);
				}
				bool flag6 = category.SettingFlag11 || category.WireframeEnabled;
				bool flag7 = flag6;
				if (flag7)
				{
					EspManager.ApplyChamsForIdUShort(zombie.id, zombie.gameObject, EspManager.ZombieChamList, category);
				}
				else
				{
					EspManager.RemoveChamsForIdUShort(zombie.id, zombie.gameObject, EspManager.ZombieChamList, category);
				}
			}
		}
	}
	public static void DrawGeneratorEsp(EspCategory category)
	{
		category.DrawMaterial.color = category.LineColors[6].Color;
		foreach (InteractableGenerator interactableGenerator in InteractableTracker.Generators)
		{
			bool flag = interactableGenerator == null || !EspDrawer.IsVisibleInRange(category, interactableGenerator.gameObject);
			bool flag2 = !flag;
			if (flag2)
			{
				EspDrawer.DrawBoundsBox(category, EspManager.GetCachedRendererBounds(interactableGenerator.GetNetId().id, interactableGenerator.gameObject, EspManager.GeneratorBoundsCache), category.LineColors[0].Color, category.LineColors[3].Color, category.LineColors[1].Color);
				bool dsy8h4Odqh4GzyIBRSA0YZwCx = category.SettingFlag14;
				bool flag3 = dsy8h4Odqh4GzyIBRSA0YZwCx;
				if (flag3)
				{
					EspDrawer.DrawTracer(category.ScrollPosition, interactableGenerator.transform.position, category.LineColors[2].Color, category.LineColors[3].Color, category.SettingFlag15);
				}
				bool darar3adCBuubvdP0AmlvE03C = category.SettingFlag13;
				bool flag4 = darar3adCBuubvdP0AmlvE03C;
				if (flag4)
				{
					foreach (EspTextEntry drtz0MPdBhZh1V5PmbtgwG0pX in category.LineEntries)
					{
						EspDrawer.DrawTextEntry(category, drtz0MPdBhZh1V5PmbtgwG0pX, string.Format(drtz0MPdBhZh1V5PmbtgwG0pX.formatText, "{0}", interactableGenerator.isPowered ? "Enabled" : "Disabled", interactableGenerator.fuel), interactableGenerator.transform.position, category.LineColors[4].Color, category.LineColors[5].Color);
					}
				}
				bool flag5 = category.SettingFlag11 || category.WireframeEnabled;
				bool flag6 = flag5;
				if (flag6)
				{
					EspManager.ApplyChamsForIdUInt(interactableGenerator.GetNetId().id, interactableGenerator.gameObject, EspManager.GeneratorChamList, category);
				}
				else
				{
					EspManager.RemoveChamsForIdUInt(interactableGenerator.GetNetId().id, interactableGenerator.gameObject, EspManager.GeneratorChamList, category);
				}
			}
		}
	}
	public static void DrawAnimalEsp(EspCategory category)
	{
		category.DrawMaterial.color = category.LineColors[6].Color;
		foreach (Animal animal in AnimalManager.animals)
		{
			bool flag = animal == null || animal.isDead || !EspDrawer.IsVisibleInRange(category, animal.gameObject);
			bool flag2 = !flag;
			if (flag2)
			{
				EspDrawer.DrawBoundsBox(category, EspManager.GetCachedColliderBounds(animal.id, animal.gameObject, EspManager.AnimalColliderCache), category.LineColors[0].Color, category.LineColors[3].Color, category.LineColors[1].Color);
				bool dsy8h4Odqh4GzyIBRSA0YZwCx = category.SettingFlag14;
				bool flag3 = dsy8h4Odqh4GzyIBRSA0YZwCx;
				if (flag3)
				{
					EspDrawer.DrawTracer(category.ScrollPosition, animal.transform.position, category.LineColors[2].Color, category.LineColors[3].Color, category.SettingFlag15);
				}
				bool darar3adCBuubvdP0AmlvE03C = category.SettingFlag13;
				bool flag4 = darar3adCBuubvdP0AmlvE03C;
				if (flag4)
				{
					foreach (EspTextEntry drtz0MPdBhZh1V5PmbtgwG0pX in category.LineEntries)
					{
						EspDrawer.DrawTextEntry(category, drtz0MPdBhZh1V5PmbtgwG0pX, string.Format(drtz0MPdBhZh1V5PmbtgwG0pX.formatText, "{0}", animal.asset.animalName), animal.transform.position, category.LineColors[4].Color, category.LineColors[5].Color);
					}
				}
				bool flag5 = category.SettingFlag11 || category.WireframeEnabled;
				bool flag6 = flag5;
				if (flag6)
				{
					EspManager.ApplyChamsForIdUShort(animal.id, animal.gameObject, EspManager.AnimalChamList, category);
				}
				else
				{
					EspManager.RemoveChamsForIdUShort(animal.id, animal.gameObject, EspManager.AnimalChamList, category);
				}
			}
		}
	}
	public static void DrawBedEsp(EspCategory category)
	{
		category.DrawMaterial.color = category.LineColors[6].Color;
		foreach (InteractableBed interactableBed in InteractableTracker.Beds)
		{
			bool flag = interactableBed == null || ((bool)category.Options[0].SortSettings && interactableBed.isClaimed) || !EspDrawer.IsVisibleInRange(category, interactableBed.gameObject);
			bool flag2 = !flag;
			if (flag2)
			{
				EspDrawer.DrawBoundsBox(category, EspManager.GetCachedRendererBounds(interactableBed.GetNetId().id, interactableBed.gameObject, EspManager.BedBoundsCache), category.LineColors[0].Color, category.LineColors[3].Color, category.LineColors[1].Color);
				bool dsy8h4Odqh4GzyIBRSA0YZwCx = category.SettingFlag14;
				bool flag3 = dsy8h4Odqh4GzyIBRSA0YZwCx;
				if (flag3)
				{
					EspDrawer.DrawTracer(category.ScrollPosition, interactableBed.transform.position, category.LineColors[2].Color, category.LineColors[3].Color, category.SettingFlag15);
				}
				bool darar3adCBuubvdP0AmlvE03C = category.SettingFlag13;
				bool flag4 = darar3adCBuubvdP0AmlvE03C;
				if (flag4)
				{
					foreach (EspTextEntry drtz0MPdBhZh1V5PmbtgwG0pX in category.LineEntries)
					{
						try
						{
							EspDrawer.DrawTextEntry(category, drtz0MPdBhZh1V5PmbtgwG0pX, string.Format(drtz0MPdBhZh1V5PmbtgwG0pX.formatText, "{0}", (Assets.find(EAssetType.ITEM, ushort.Parse(interactableBed.name)) as ItemAsset).itemName, interactableBed.isClaimed ? "Claimed" : "Unclaimed"), interactableBed.transform.position, category.LineColors[4].Color, category.LineColors[5].Color);
						}
						catch
						{
							EspDrawer.DrawTextEntry(category, drtz0MPdBhZh1V5PmbtgwG0pX, string.Format(drtz0MPdBhZh1V5PmbtgwG0pX.formatText, "{0}", interactableBed.name, interactableBed.isClaimed ? "Claimed" : "Unclaimed"), interactableBed.transform.position, category.LineColors[4].Color, category.LineColors[5].Color);
						}
					}
				}
				bool flag5 = category.SettingFlag11 || category.WireframeEnabled;
				bool flag6 = flag5;
				if (flag6)
				{
					EspManager.ApplyChamsForIdUInt(interactableBed.GetNetId().id, interactableBed.gameObject, EspManager.BedChamList, category);
				}
				else
				{
					EspManager.RemoveChamsForIdUInt(interactableBed.GetNetId().id, interactableBed.gameObject, EspManager.BedChamList, category);
				}
			}
		}
	}
	public static void DrawTurretEsp(EspCategory category)
	{
		category.DrawMaterial.color = category.LineColors[6].Color;
		foreach (InteractableSentry interactableSentry in InteractableTracker.Sentries)
		{
			bool flag = interactableSentry == null || !EspDrawer.IsVisibleInRange(category, interactableSentry.gameObject);
			bool flag2 = !flag;
			if (flag2)
			{
				EspDrawer.DrawBoundsBox(category, EspManager.GetCachedRendererBounds(interactableSentry.GetNetId().id, interactableSentry.gameObject, EspManager.TurretBoundsCache), category.LineColors[0].Color, category.LineColors[3].Color, category.LineColors[1].Color);
				bool dsy8h4Odqh4GzyIBRSA0YZwCx = category.SettingFlag14;
				bool flag3 = dsy8h4Odqh4GzyIBRSA0YZwCx;
				if (flag3)
				{
					EspDrawer.DrawTracer(category.ScrollPosition, interactableSentry.transform.position, category.LineColors[2].Color, category.LineColors[3].Color, category.SettingFlag15);
				}
				bool darar3adCBuubvdP0AmlvE03C = category.SettingFlag13;
				bool flag4 = darar3adCBuubvdP0AmlvE03C;
				if (flag4)
				{
					foreach (EspTextEntry drtz0MPdBhZh1V5PmbtgwG0pX in category.LineEntries)
					{
						EspDrawer.DrawTextEntry(category, drtz0MPdBhZh1V5PmbtgwG0pX, string.Format(drtz0MPdBhZh1V5PmbtgwG0pX.formatText, "{0}", interactableSentry.sentryAsset.itemName), interactableSentry.transform.position, category.LineColors[4].Color, category.LineColors[5].Color);
					}
				}
				bool flag5 = category.SettingFlag11 || category.WireframeEnabled;
				bool flag6 = flag5;
				if (flag6)
				{
					EspManager.ApplyChamsForIdUInt(interactableSentry.GetNetId().id, interactableSentry.gameObject, EspManager.TurretChamList, category);
				}
				else
				{
					EspManager.RemoveChamsForIdUInt(interactableSentry.GetNetId().id, interactableSentry.gameObject, EspManager.TurretChamList, category);
				}
			}
		}
	}
	public static void DrawBulletEsp(EspCategory category)
	{
		bool flag = !Provider.modeConfigData.Gameplay.Ballistics || UseableGunHooks.ActiveBullets == null;
		bool flag2 = !flag;
		if (flag2)
		{
			foreach (DelayedBullet drxteGz0evnNDVy6poswGHA5b in UseableGunHooks.ActiveBullets)
			{
				bool flag3 = drxteGz0evnNDVy6poswGHA5b == null || !drxteGz0evnNDVy6poswGHA5b.Position.IsOnScreen() || !EspDrawer.IsWithinDistance(category, drxteGz0evnNDVy6poswGHA5b.Position);
				bool flag4 = !flag3;
				if (flag4)
				{
					EspManager.BulletEspBounds.center = drxteGz0evnNDVy6poswGHA5b.Position;
					EspDrawer.DrawBoundsBox(category, EspManager.BulletEspBounds, category.LineColors[0].Color, category.LineColors[3].Color, category.LineColors[1].Color);
					bool dsy8h4Odqh4GzyIBRSA0YZwCx = category.SettingFlag14;
					bool flag5 = dsy8h4Odqh4GzyIBRSA0YZwCx;
					if (flag5)
					{
						EspDrawer.DrawTracer(category.ScrollPosition, drxteGz0evnNDVy6poswGHA5b.Position, category.LineColors[2].Color, category.LineColors[3].Color, category.SettingFlag15);
					}
					bool darar3adCBuubvdP0AmlvE03C = category.SettingFlag13;
					bool flag6 = darar3adCBuubvdP0AmlvE03C;
					if (flag6)
					{
						EspDrawer.DrawCategoryTexts(category, drxteGz0evnNDVy6poswGHA5b.Position, null, category.LineColors[4].Color, category.LineColors[5].Color);
					}
				}
			}
		}
	}
	public static void DrawAirdropEsp(EspCategory category)
	{
		category.DrawMaterial.color = category.LineColors[6].Color;
		foreach (CarepackageTracker drytcLmeSNr8Q0N26WubhHXJd in CarepackageTracker.Trackers)
		{
			bool flag = drytcLmeSNr8Q0N26WubhHXJd == null || !drytcLmeSNr8Q0N26WubhHXJd.transform.position.IsOnScreen() || !EspDrawer.IsWithinDistance(category, drytcLmeSNr8Q0N26WubhHXJd.transform.position);
			bool flag2 = !flag;
			if (flag2)
			{
				EspDrawer.DrawBoundsBox(category, EspManager.GetCachedColliderBounds(drytcLmeSNr8Q0N26WubhHXJd.Id, drytcLmeSNr8Q0N26WubhHXJd.gameObject, EspManager.AirdropColliderCache), category.LineColors[0].Color, category.LineColors[3].Color, category.LineColors[1].Color);
				bool dsy8h4Odqh4GzyIBRSA0YZwCx = category.SettingFlag14;
				bool flag3 = dsy8h4Odqh4GzyIBRSA0YZwCx;
				if (flag3)
				{
					EspDrawer.DrawTracer(category.ScrollPosition, drytcLmeSNr8Q0N26WubhHXJd.transform.position, category.LineColors[2].Color, category.LineColors[3].Color, category.SettingFlag15);
				}
				bool darar3adCBuubvdP0AmlvE03C = category.SettingFlag13;
				bool flag4 = darar3adCBuubvdP0AmlvE03C;
				if (flag4)
				{
					EspDrawer.DrawCategoryTexts(category, drytcLmeSNr8Q0N26WubhHXJd.transform.position, null, category.LineColors[4].Color, category.LineColors[5].Color);
				}
				try
				{
					bool flag5 = category.SettingFlag11 || category.WireframeEnabled;
					bool flag6 = flag5;
					if (flag6)
					{
						EspManager.ApplyChamsForIdUShort(drytcLmeSNr8Q0N26WubhHXJd.Id, drytcLmeSNr8Q0N26WubhHXJd.gameObject, EspManager.AirdropChamList, category);
					}
					else
					{
						EspManager.RemoveChamsForIdUShort(drytcLmeSNr8Q0N26WubhHXJd.Id, drytcLmeSNr8Q0N26WubhHXJd.gameObject, EspManager.AirdropChamList, category);
					}
				}
				catch
				{
				}
			}
		}
	}
	public static void DrawStorageEsp(EspCategory category)
	{
		category.DrawMaterial.color = category.LineColors[6].Color;
		foreach (InteractableStorage interactableStorage in InteractableTracker.Storages)
		{
			bool flag = interactableStorage == null || !EspDrawer.IsVisibleInRange(category, interactableStorage.gameObject);
			bool flag2 = !flag;
			if (flag2)
			{
				EspDrawer.DrawBoundsBox(category, EspManager.GetCachedRendererBounds(interactableStorage.GetNetId().id, interactableStorage.gameObject, EspManager.StorageBoundsCache), category.LineColors[0].Color, category.LineColors[3].Color, category.LineColors[1].Color);
				bool dsy8h4Odqh4GzyIBRSA0YZwCx = category.SettingFlag14;
				bool flag3 = dsy8h4Odqh4GzyIBRSA0YZwCx;
				if (flag3)
				{
					EspDrawer.DrawTracer(category.ScrollPosition, interactableStorage.transform.position, category.LineColors[2].Color, category.LineColors[3].Color, category.SettingFlag15);
				}
				bool flag4 = (bool)category.Options[1].SortSettings;
				bool flag5 = flag4;
				if (flag5)
				{
					EspManager.DrawStorageIcon(interactableStorage);
				}
				bool flag6 = category.Options != null && category.Options.Length > 3 && (bool)category.Options[3].SortSettings;
				if (flag6)
				{
					Interactable2HP component = interactableStorage.GetComponent<Interactable2HP>();
					bool flag7 = component != null;
					if (flag7)
					{
						float num = (float)component.hp / 100f;
						Bounds bounds = EspManager.GetCachedRendererBounds(interactableStorage.GetNetId().id, interactableStorage.gameObject, EspManager.StorageBoundsCache);
						Vector3 vector = EspDrawer.RenderCamera.WorldToScreenPoint(bounds.center);
						bool flag8 = vector.z > 0f;
						if (flag8)
						{
							Rect boundsScreenRect = EspDrawer.GetBoundsScreenRect(bounds);
							float num2 = Mathf.Max(boundsScreenRect.width, 10f);
							float num3 = 8f / vector.z;
							num3 = Mathf.Clamp(num3, 4f, 10f);
							EspDrawer.DrawLine(new Vector2(boundsScreenRect.center.x - num2 / 2f, boundsScreenRect.yMax + 10f), new Vector2(boundsScreenRect.center.x + num2 / 2f, boundsScreenRect.yMax + 10f), new Color(0.1f, 0.1f, 0.1f, 0.9f), num3 + 3f);
							EspDrawer.DrawLine(new Vector2(boundsScreenRect.center.x - num2 / 2f + 1f, boundsScreenRect.yMax + 10f), new Vector2(boundsScreenRect.center.x + num2 / 2f - 1f, boundsScreenRect.yMax + 10f), new Color(0.2f, 0.2f, 0.2f, 0.8f), num3);
							bool flag9 = num > 0f;
							if (flag9)
							{
								EspDrawer.DrawLine(new Vector2(boundsScreenRect.center.x - num2 / 2f + 1f, boundsScreenRect.yMax + 10f), new Vector2(boundsScreenRect.center.x - num2 / 2f + 1f + (num2 - 2f) * num, boundsScreenRect.yMax + 10f), Color.Lerp(new Color(1f, 0.2f, 0.2f), new Color(0.2f, 1f, 0.2f), num), num3);
							}
						}
					}
				}
				bool darar3adCBuubvdP0AmlvE03C = category.SettingFlag13;
				bool flag10 = darar3adCBuubvdP0AmlvE03C;
				if (flag10)
				{
					foreach (EspTextEntry drtz0MPdBhZh1V5PmbtgwG0pX in category.LineEntries)
					{
						try
						{
							EspDrawer.DrawTextEntry(category, drtz0MPdBhZh1V5PmbtgwG0pX, string.Format(drtz0MPdBhZh1V5PmbtgwG0pX.formatText, "{0}", (Assets.find(EAssetType.ITEM, ushort.Parse(interactableStorage.name)) as ItemAsset).itemName), interactableStorage.transform.position, category.LineColors[4].Color, category.LineColors[5].Color);
						}
						catch
						{
							EspDrawer.DrawTextEntry(category, drtz0MPdBhZh1V5PmbtgwG0pX, string.Format(drtz0MPdBhZh1V5PmbtgwG0pX.formatText, "{0}", interactableStorage.name), interactableStorage.transform.position, category.LineColors[4].Color, category.LineColors[5].Color);
						}
					}
				}
				bool flag11 = category.SettingFlag11 || category.WireframeEnabled;
				bool flag12 = flag11;
				if (flag12)
				{
					EspManager.ApplyChamsForIdUInt(interactableStorage.GetNetId().id, interactableStorage.gameObject, EspManager.StorageChamList, category);
				}
				else
				{
					EspManager.RemoveChamsForIdUInt(interactableStorage.GetNetId().id, interactableStorage.gameObject, EspManager.StorageChamList, category);
				}
			}
		}
	}
	private static void DrawStorageIcon(InteractableStorage iss)
	{
		Texture2D texture2D = GuiStyles.GetItemIconWithState(ushort.Parse(iss.name), new byte[0]);
		bool flag = texture2D != null;
		bool flag2 = flag;
		if (flag2)
		{
			Vector3 vector = iss.transform.position.WorldToScreenPoint();
			int num = (int)EspCategories.categories[9].Options[2].SortSettings;
			Rect rect = new Rect(vector.x - (float)(num / 2), vector.y, (float)num, (float)num);
			GUI.DrawTexture(new Rect(Mathf.Round(rect.x), Mathf.Round(rect.y), Mathf.Round(rect.width), Mathf.Round(rect.height)), texture2D, ScaleMode.ScaleToFit);
		}
	}
	public static void DrawGrenadeEsp(EspCategory category)
	{
		category.DrawMaterial.color = category.LineColors[6].Color;
		foreach (ValueTuple<GameObject, ItemThrowableAsset> valueTuple in EspManager.ActiveThrowables)
		{
			GameObject item = valueTuple.Item1;
			ItemThrowableAsset item2 = valueTuple.Item2;
			bool flag = item == null || !EspDrawer.IsVisibleInRange(category, item.gameObject);
			bool flag2 = !flag;
			if (flag2)
			{
				EspDrawer.DrawBoundsBox(category, EspManager.GetCachedRendererBounds((uint)item.GetInstanceID(), item, EspManager.GrenadeBoundsCache), category.LineColors[0].Color, category.LineColors[3].Color, category.LineColors[1].Color);
				bool dsy8h4Odqh4GzyIBRSA0YZwCx = category.SettingFlag14;
				bool flag3 = dsy8h4Odqh4GzyIBRSA0YZwCx;
				if (flag3)
				{
					EspDrawer.DrawTracer(category.ScrollPosition, item.transform.position, category.LineColors[2].Color, category.LineColors[3].Color, category.SettingFlag15);
				}
				bool darar3adCBuubvdP0AmlvE03C = category.SettingFlag13;
				bool flag4 = darar3adCBuubvdP0AmlvE03C;
				if (flag4)
				{
					EspDrawer.DrawCategoryTexts(category, item.transform.position, null, category.LineColors[4].Color, category.LineColors[5].Color);
				}
				bool flag5 = category.SettingFlag11 || category.WireframeEnabled;
				bool flag6 = flag5;
				if (flag6)
				{
					EspManager.ApplyChamsForIdUInt((uint)item.GetInstanceID(), item, EspManager.GrenadeChamList, category);
				}
				else
				{
					EspManager.RemoveChamsForIdUInt((uint)item.GetInstanceID(), item, EspManager.GrenadeChamList, category);
				}
				bool flag7 = (bool)category.Options[0].SortSettings && item2 != null;
				bool flag8 = flag7;
				if (flag8)
				{
					RuntimeGizmos.Get().Sphere(item.transform.position, item2.range, category.LineColors[5].Color, 0f, EGizmoLayer.World);
				}
			}
		}
	}
	public static void DrawOresESP(EspCategory category)
	{
		ColorSetting[] dytdRk0oVzSXoI44WWs8r2oSt = category.LineColors;
		category.DrawMaterial.color = dytdRk0oVzSXoI44WWs8r2oSt[6].Color;
		OptionBase[] dtyDRC9cdG8IGn9zMpTt5eaiL = category.Options;
		bool flag = dtyDRC9cdG8IGn9zMpTt5eaiL != null && dtyDRC9cdG8IGn9zMpTt5eaiL.Length != 0 && (bool)dtyDRC9cdG8IGn9zMpTt5eaiL[0].SortSettings;
		bool flag2 = dtyDRC9cdG8IGn9zMpTt5eaiL != null && dtyDRC9cdG8IGn9zMpTt5eaiL.Length > 1 && (bool)dtyDRC9cdG8IGn9zMpTt5eaiL[1].SortSettings;
		bool dsy8h4Odqh4GzyIBRSA0YZwCx = category.SettingFlag14;
		bool darar3adCBuubvdP0AmlvE03C = category.SettingFlag13;
		bool dbbuE0zU9gDLa80kNfgXhIB4w = category.SettingFlag15;
		Color32 dkJGdJpvFP4j4uWN4CyFixyQ = dytdRk0oVzSXoI44WWs8r2oSt[0].Color;
		Color32 dkJGdJpvFP4j4uWN4CyFixyQ2 = dytdRk0oVzSXoI44WWs8r2oSt[3].Color;
		Color32 dkJGdJpvFP4j4uWN4CyFixyQ3 = dytdRk0oVzSXoI44WWs8r2oSt[1].Color;
		Color32 dkJGdJpvFP4j4uWN4CyFixyQ4 = dytdRk0oVzSXoI44WWs8r2oSt[2].Color;
		Color32 dkJGdJpvFP4j4uWN4CyFixyQ5 = dytdRk0oVzSXoI44WWs8r2oSt[4].Color;
		Color32 dkJGdJpvFP4j4uWN4CyFixyQ6 = dytdRk0oVzSXoI44WWs8r2oSt[5].Color;
		EspManager.DoreGatherTimer += Time.deltaTime;
		bool flag3 = EspManager.DoreGatherTimer >= EspManager.DoreGatherInterval;
		if (flag3)
		{
			EspManager.DoreGatherTimer = 0f;
			EspManager.DoreGatherList.Clear();
			EspManager.DoreBoundsCache.Clear();
			LevelGround.GatherAllTrees(EspManager.DoreGatherList);
		}
		string doreNameFilter = EspManager.DoreNameFilter;
		bool flag4 = !string.IsNullOrEmpty(doreNameFilter);
		foreach (ResourceSpawnpoint resourceSpawnpoint in EspManager.DoreGatherList)
		{
			bool flag5 = resourceSpawnpoint == null || resourceSpawnpoint.model == null;
			if (!flag5)
			{
				bool flag6 = resourceSpawnpoint.isDead && !flag;
				if (!flag6)
				{
					ResourceAsset asset = resourceSpawnpoint.asset;
					bool flag7 = flag4;
					if (flag7)
					{
						string text = ((asset != null) ? asset.resourceName : "Resource");
						bool flag8 = text == null || text.IndexOf(doreNameFilter, StringComparison.OrdinalIgnoreCase) < 0;
						if (flag8)
						{
							continue;
						}
					}
					Transform model = resourceSpawnpoint.model;
					GameObject gameObject = model.gameObject;
					bool flag9 = !EspDrawer.IsVisibleInRange(category, gameObject);
					if (!flag9)
					{
						uint instanceID = (uint)gameObject.GetInstanceID();
						Vector3 position = model.position;
						Bounds bounds = EspManager.GetCachedRendererBounds(instanceID, gameObject, EspManager.DoreBoundsCache);
						EspDrawer.DrawBoundsBox(category, bounds, dkJGdJpvFP4j4uWN4CyFixyQ, dkJGdJpvFP4j4uWN4CyFixyQ2, dkJGdJpvFP4j4uWN4CyFixyQ3);
						bool flag10 = dsy8h4Odqh4GzyIBRSA0YZwCx;
						if (flag10)
						{
							EspDrawer.DrawTracer(category.ScrollPosition, position, dkJGdJpvFP4j4uWN4CyFixyQ4, dkJGdJpvFP4j4uWN4CyFixyQ2, dbbuE0zU9gDLa80kNfgXhIB4w);
						}
						float num = ((asset != null && asset.health > 0) ? ((float)resourceSpawnpoint.health / (float)asset.health) : 0f);
						bool flag11 = darar3adCBuubvdP0AmlvE03C;
						if (flag11)
						{
							string text2 = ((asset != null) ? asset.resourceName : "Resource");
							int num2 = Mathf.RoundToInt(num * 100f);
							foreach (EspTextEntry drtz0MPdBhZh1V5PmbtgwG0pX in category.LineEntries)
							{
								EspDrawer.DrawTextEntry(category, drtz0MPdBhZh1V5PmbtgwG0pX, string.Format(drtz0MPdBhZh1V5PmbtgwG0pX.formatText, "{0}", text2, num2), position, dkJGdJpvFP4j4uWN4CyFixyQ5, dkJGdJpvFP4j4uWN4CyFixyQ6);
							}
						}
						bool flag12 = flag2;
						if (flag12)
						{
							Vector3 vector = EspDrawer.RenderCamera.WorldToScreenPoint(bounds.center);
							bool flag13 = vector.z > 0f;
							if (flag13)
							{
								Rect boundsScreenRect = EspDrawer.GetBoundsScreenRect(bounds);
								float num3 = Mathf.Max(boundsScreenRect.width, 10f);
								float num4 = Mathf.Clamp(8f / vector.z, 4f, 10f);
								float num5 = boundsScreenRect.yMax + 10f;
								float num6 = boundsScreenRect.center.x - num3 / 2f;
								float num7 = boundsScreenRect.center.x + num3 / 2f;
								EspDrawer.DrawLine(new Vector2(num6, num5), new Vector2(num7, num5), new Color(0.1f, 0.1f, 0.1f, 0.9f), num4 + 3f);
								EspDrawer.DrawLine(new Vector2(num6 + 1f, num5), new Vector2(num7 - 1f, num5), new Color(0.2f, 0.2f, 0.2f, 0.8f), num4);
								bool flag14 = num > 0f;
								if (flag14)
								{
									EspDrawer.DrawLine(new Vector2(num6 + 1f, num5), new Vector2(num6 + 1f + (num3 - 2f) * num, num5), Color.Lerp(new Color(1f, 0.2f, 0.2f), new Color(0.2f, 1f, 0.2f), num), num4);
								}
							}
						}
					}
				}
			}
		}
	}
	private static Bounds GetCachedColliderBounds(ushort id, GameObject go, Dictionary<ushort, Collider> dictionary)
	{
		Collider component;
		bool flag = dictionary.TryGetValue(id, out component);
		bool flag2 = flag;
		Bounds bounds;
		if (flag2)
		{
			bounds = component.bounds;
		}
		else
		{
			component = go.GetComponent<Collider>();
			dictionary.Add(id, component);
			bounds = component.bounds;
		}
		return bounds;
	}
	public static void ReapplyAllChams()
	{
		foreach (ChamWireframeComponent dg7HgPuhdjH1X6wkz4QJsSTzj in ChamWireframeComponent.AllInstances)
		{
			dg7HgPuhdjH1X6wkz4QJsSTzj.ReapplyChamMaterial();
		}
	}
	private static Bounds GetCachedRendererBounds(uint id, GameObject go, Dictionary<uint, GameObject> dictionary)
	{
		GameObject gameObject;
		bool flag = dictionary.TryGetValue(id, out gameObject);
		Bounds bounds = default(Bounds);
		bool flag2 = !flag || gameObject == null;
		if (flag2)
		{
			gameObject = go;
			bool flag3 = flag;
			if (flag3)
			{
				dictionary[id] = go;
				EspManager.DrendererCache.Remove(id);
			}
			else
			{
				dictionary.Add(id, go);
			}
		}
		Renderer[] componentsInChildren;
		bool flag4 = !EspManager.DrendererCache.TryGetValue(id, out componentsInChildren) || componentsInChildren == null;
		if (flag4)
		{
			componentsInChildren = gameObject.GetComponentsInChildren<Renderer>();
			EspManager.DrendererCache[id] = componentsInChildren;
		}
		bool flag5 = false;
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			bool flag6 = componentsInChildren[i] == null;
			if (!flag6)
			{
				bool flag7 = componentsInChildren[i] is ParticleSystemRenderer || componentsInChildren[i] is TrailRenderer || componentsInChildren[i] is LineRenderer;
				if (!flag7)
				{
					bool flag8 = !flag5;
					if (flag8)
					{
						bounds = componentsInChildren[i].bounds;
						flag5 = true;
					}
					else
					{
						bounds.Encapsulate(componentsInChildren[i].bounds);
					}
				}
			}
		}
		bool flag9 = !flag5;
		if (flag9)
		{
			Collider component = gameObject.GetComponent<Collider>();
			bool flag10 = component != null;
			if (flag10)
			{
				bounds = component.bounds;
			}
			else
			{
				bounds = new Bounds(gameObject.transform.position, new Vector3(2f, 2f, 4f));
			}
		}
		return bounds;
	}
	private static void ApplyChamsForIdUShort(ushort id, GameObject go, List<ushort> list, EspCategory espc)
	{
		bool flag = !list.Contains(id);
		bool flag2 = flag;
		if (flag2)
		{
			list.Add(id);
		}
		Renderer[] componentsInChildren = null;
		bool flag3 = flag || !EspManager.DchamsRendererCacheUShort.TryGetValue(id, out componentsInChildren) || componentsInChildren == null;
		if (flag3)
		{
			componentsInChildren = go.GetComponentsInChildren<Renderer>();
			EspManager.DchamsRendererCacheUShort[id] = componentsInChildren;
		}
		Renderer[] array = componentsInChildren;
		int i = 0;
		while (i < array.Length)
		{
			Renderer renderer = array[i];
			bool flag4 = flag;
			ChamWireframeComponent dg7HgPuhdjH1X6wkz4QJsSTzj;
			if (flag4)
			{
				dg7HgPuhdjH1X6wkz4QJsSTzj = renderer.gameObject.AddComponent<ChamWireframeComponent>();
				espc.LineComponents.Add(dg7HgPuhdjH1X6wkz4QJsSTzj);
				goto IL_00A9;
			}
			dg7HgPuhdjH1X6wkz4QJsSTzj = renderer.gameObject.GetComponent<ChamWireframeComponent>();
			bool flag5 = dg7HgPuhdjH1X6wkz4QJsSTzj == null;
			if (!flag5)
			{
				goto IL_00A9;
			}
			IL_00F4:
			i++;
			continue;
			IL_00A9:
			bool d2JTP060dwZSwDD85nvmWwK0A = espc.SettingFlag11;
			if (d2JTP060dwZSwDD85nvmWwK0A)
			{
				dg7HgPuhdjH1X6wkz4QJsSTzj.ApplyChamMaterial(espc.DrawMaterial);
			}
			else
			{
				dg7HgPuhdjH1X6wkz4QJsSTzj.RestoreOriginalMaterials();
			}
			bool dwireframeEnabled = espc.WireframeEnabled;
			if (dwireframeEnabled)
			{
				dg7HgPuhdjH1X6wkz4QJsSTzj.DEnableWireframe();
			}
			else
			{
				dg7HgPuhdjH1X6wkz4QJsSTzj.DDisableWireframe();
			}
			goto IL_00F4;
		}
	}
	private static void ApplyChamsForIdUInt(uint id, GameObject go, List<uint> list, EspCategory espc)
	{
		bool flag = !list.Contains(id);
		bool flag2 = flag;
		if (flag2)
		{
			list.Add(id);
		}
		Renderer[] componentsInChildren = null;
		bool flag3 = flag || !EspManager.DchamsRendererCacheUInt.TryGetValue(id, out componentsInChildren) || componentsInChildren == null;
		if (flag3)
		{
			componentsInChildren = go.GetComponentsInChildren<Renderer>();
			EspManager.DchamsRendererCacheUInt[id] = componentsInChildren;
		}
		Renderer[] array = componentsInChildren;
		int i = 0;
		while (i < array.Length)
		{
			Renderer renderer = array[i];
			bool flag4 = flag;
			ChamWireframeComponent dg7HgPuhdjH1X6wkz4QJsSTzj;
			if (flag4)
			{
				dg7HgPuhdjH1X6wkz4QJsSTzj = renderer.gameObject.AddComponent<ChamWireframeComponent>();
				espc.LineComponents.Add(dg7HgPuhdjH1X6wkz4QJsSTzj);
				goto IL_00A9;
			}
			dg7HgPuhdjH1X6wkz4QJsSTzj = renderer.gameObject.GetComponent<ChamWireframeComponent>();
			bool flag5 = dg7HgPuhdjH1X6wkz4QJsSTzj == null;
			if (!flag5)
			{
				goto IL_00A9;
			}
			IL_00F4:
			i++;
			continue;
			IL_00A9:
			bool d2JTP060dwZSwDD85nvmWwK0A = espc.SettingFlag11;
			if (d2JTP060dwZSwDD85nvmWwK0A)
			{
				dg7HgPuhdjH1X6wkz4QJsSTzj.ApplyChamMaterial(espc.DrawMaterial);
			}
			else
			{
				dg7HgPuhdjH1X6wkz4QJsSTzj.RestoreOriginalMaterials();
			}
			bool dwireframeEnabled = espc.WireframeEnabled;
			if (dwireframeEnabled)
			{
				dg7HgPuhdjH1X6wkz4QJsSTzj.DEnableWireframe();
			}
			else
			{
				dg7HgPuhdjH1X6wkz4QJsSTzj.DDisableWireframe();
			}
			goto IL_00F4;
		}
	}
	private static void RemoveChamsForIdUShort(ushort id, GameObject go, List<ushort> list, EspCategory espc)
	{
		bool flag = list.Contains(id);
		bool flag2 = flag;
		if (flag2)
		{
			list.Remove(id);
			EspManager.DchamsRendererCacheUShort.Remove(id);
			Renderer[] componentsInChildren = go.GetComponentsInChildren<Renderer>();
			foreach (Renderer renderer in componentsInChildren)
			{
				ChamWireframeComponent component = renderer.gameObject.GetComponent<ChamWireframeComponent>();
				bool flag3 = component == null;
				if (!flag3)
				{
					espc.LineComponents.Remove(component);
					component.RestoreOriginalMaterials();
					component.DDisableWireframe();
					UnityEngine.Object.Destroy(component);
				}
			}
		}
	}
	private static void RemoveChamsForIdUInt(uint id, GameObject go, List<uint> list, EspCategory espc)
	{
		bool flag = list.Contains(id);
		bool flag2 = flag;
		if (flag2)
		{
			list.Remove(id);
			EspManager.DchamsRendererCacheUInt.Remove(id);
			Renderer[] componentsInChildren = go.GetComponentsInChildren<Renderer>();
			foreach (Renderer renderer in componentsInChildren)
			{
				ChamWireframeComponent component = renderer.gameObject.GetComponent<ChamWireframeComponent>();
				bool flag3 = component == null;
				if (!flag3)
				{
					espc.LineComponents.Remove(component);
					component.RestoreOriginalMaterials();
					component.DDisableWireframe();
					UnityEngine.Object.Destroy(component);
				}
			}
		}
	}
	private static void DAddIcon(ItemAsset asset, List<Texture2D> icons)
	{
		bool flag = asset != null;
		if (flag)
		{
			Texture2D texture2D = GuiStyles.GetItemIcon(asset.id);
			bool flag2 = texture2D != null;
			if (flag2)
			{
				icons.Add(texture2D);
			}
		}
	}
	public static float lastFishingPanelHeight = 0f;
	public static Vector2 fishingPanelPos = new Vector2(-1f, -1f);
	private static bool fishingPanelDragging = false;
	private static Vector2 fishingPanelDragOffset = Vector2.zero;
	public static Vector2 farmPanelPos = new Vector2(-1f, -1f);
	private static bool farmPanelDragging = false;
	private static Vector2 farmPanelDragOffset = Vector2.zero;
	private static InteractableFarm[] _gridFarmCache = null;
	private static float _gridFarmCacheTimer = 0f;
	private static List<Vector3> _gridPlacementCache = new List<Vector3>();
	private static float _gridPlacementCacheTimer = 0f;
	private static Vector3 _gridPlacementLastPos = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
	public static bool masterESPEnabled = true;
	public static Material ScreenLineMaterial;
	public static bool EspDirtyFlag = false;
	private static List<ValueTuple<GameObject, ItemThrowableAsset>> ActiveThrowables = new List<ValueTuple<GameObject, ItemThrowableAsset>>();
	private static EspCategory CurrentCategory;
	private static ValueTuple<ColorSetting, List<Vector2>> CachedGradientPolyline;
	private static string CachedFormatString;
	private static Bounds DefaultPlayerBounds = new Bounds(Vector3.zero, new Vector3(1f, 1.95f, 1f));
	private static Vector3 PlayerNameOffset = new Vector3(0f, 1.1f, 0f);
	private static int TopIconRowCount = 0;
	private static SteamPlayerID TargetSteamPlayerId;
	private static Vector3[] PlayerBoneScreenPositions = new Vector3[15];
	private static Dictionary<ulong, ValueTuple<ChamWireframeComponent, ChamWireframeComponent>> PlayerChamComponents = new Dictionary<ulong, ValueTuple<ChamWireframeComponent, ChamWireframeComponent>>();
	public static Material PlayerChamMaterialA;
	public static Material PlayerChamMaterialB;
	public static Material PlayerChamMaterialC;
	public static Material DwireframeMaterial;
	public static Material DbacktrackMaterial;
	private static Dictionary<ulong, Mesh> DbacktrackMeshCache = new Dictionary<ulong, Mesh>();
	private static bool DprevWireframeChams = false;
	private static bool DprevLocalPlayerWireframe = false;
	private static Plane[] DfrustumPlanes = new Plane[6];
	private static float DwireframeCleanupTimer = 0f;
	private static ReflectedField<SkinnedMeshRenderer> ThirdRenderer0Field = new ReflectedField<SkinnedMeshRenderer>(typeof(PlayerAnimator), "thirdRenderer_0");
	private static ReflectedField<SkinnedMeshRenderer> ThirdRenderer1Field = new ReflectedField<SkinnedMeshRenderer>(typeof(PlayerAnimator), "thirdRenderer_1");
	private static Bounds BulletEspBounds = new Bounds(Vector3.zero, new Vector3(0.25f, 0.25f, 0.25f));
	private static List<ValueTuple<Collider, InteractableItem>> VisibleItemList = new List<ValueTuple<Collider, InteractableItem>>();
	private static Dictionary<uint, GameObject> VehicleBoundsCache = new Dictionary<uint, GameObject>();
	private static Dictionary<ushort, Collider> ZombieColliderCache = new Dictionary<ushort, Collider>();
	private static Dictionary<uint, GameObject> GeneratorBoundsCache = new Dictionary<uint, GameObject>();
	private static Dictionary<ushort, Collider> AnimalColliderCache = new Dictionary<ushort, Collider>();
	private static Dictionary<uint, GameObject> BedBoundsCache = new Dictionary<uint, GameObject>();
	private static Dictionary<uint, GameObject> TurretBoundsCache = new Dictionary<uint, GameObject>();
	private static Dictionary<uint, GameObject> StorageBoundsCache = new Dictionary<uint, GameObject>();
	private static Dictionary<ushort, Collider> AirdropColliderCache = new Dictionary<ushort, Collider>();
	private static Dictionary<ushort, Collider> OreColliderCacheA = new Dictionary<ushort, Collider>();
	private static Dictionary<ushort, Collider> OreColliderCacheB = new Dictionary<ushort, Collider>();
	private static Dictionary<ushort, Collider> OreColliderCacheC = new Dictionary<ushort, Collider>();
	private static Dictionary<ushort, Collider> OreColliderCacheD = new Dictionary<ushort, Collider>();
	private static Dictionary<ushort, Collider> OreColliderCacheE = new Dictionary<ushort, Collider>();
	private static Dictionary<ushort, Collider> OreColliderCacheF = new Dictionary<ushort, Collider>();
	private static Dictionary<uint, GameObject> GrenadeBoundsCache = new Dictionary<uint, GameObject>();
	private static List<uint> VehicleChamList = new List<uint>();
	private static List<uint> ItemChamList = new List<uint>();
	private static List<ushort> ZombieChamList = new List<ushort>();
	private static List<uint> GeneratorChamList = new List<uint>();
	private static List<ushort> AnimalChamList = new List<ushort>();
	private static List<uint> BedChamList = new List<uint>();
	private static List<uint> TurretChamList = new List<uint>();
	private static List<uint> StorageChamList = new List<uint>();
	private static List<ushort> AirdropChamList = new List<ushort>();
	private static List<ushort> OreChamListA = new List<ushort>();
	private static List<ushort> OreChamListB = new List<ushort>();
	private static List<ushort> OreChamListC = new List<ushort>();
	private static List<ushort> OreChamListD = new List<ushort>();
	private static List<ushort> OreChamListE = new List<ushort>();
	private static List<ushort> OreChamListF = new List<ushort>();
	private static List<uint> GrenadeChamList = new List<uint>();
	private static List<ResourceSpawnpoint> DoreGatherList = new List<ResourceSpawnpoint>();
	private static Dictionary<uint, GameObject> DoreBoundsCache = new Dictionary<uint, GameObject>();
	public static string DoreNameFilter = "";
	private static float DoreGatherTimer = 0f;
	private static float DoreGatherInterval = 0.5f;
	private static Dictionary<uint, Renderer[]> DrendererCache = new Dictionary<uint, Renderer[]>();
	private static Dictionary<ushort, Renderer[]> DchamsRendererCacheUShort = new Dictionary<ushort, Renderer[]>();
	private static Dictionary<uint, Renderer[]> DchamsRendererCacheUInt = new Dictionary<uint, Renderer[]>();
	private static List<Texture2D> DiconBuffer;
	private static List<ChamWireframeComponent> DwireframeRenderList;
	private static List<Bounds> DwireframeRenderedBounds = new List<Bounds>();
}
