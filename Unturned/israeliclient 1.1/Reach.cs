using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SDG.Unturned;
using UnityEngine;

namespace gatyware
{
	public static class Reach
	{
		[DllImport("kernel32.dll")]
		private static extern bool VirtualProtect(IntPtr addr, int size, uint prot, out uint old);

		private static void Cache()
		{
			if (Reach._cached)
			{
				return;
			}
			Reach._cached = true;
			BindingFlags bindingFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
			BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.NonPublic;
			Reach._piHit = typeof(PlayerInteract).GetField("hit", bindingFlags | BindingFlags.Instance);
			Reach._piLastInteract = typeof(PlayerInteract).GetField("lastInteract", bindingFlags);
			Reach._barPoint = typeof(UseableBarricade).GetField("point", bindingAttr);
			Reach._barHit = typeof(UseableBarricade).GetField("hit", bindingAttr);
			Reach._barAngleY = typeof(UseableBarricade).GetField("angle_y", bindingAttr);
			Reach._strType = typeof(Provider).Assembly.GetType("SDG.Unturned.UseableStructure");
			if (Reach._strType != null)
			{
				Reach._strPos = Reach._strType.GetField("pendingPlacementPosition", bindingAttr);
				Reach._strYaw = Reach._strType.GetField("pendingPlacementYaw", bindingAttr);
			}
		}

		private static Camera GetCam()
		{
			if (!(FreeCam.FreeCamCamera != null))
			{
				return MainCamera.instance;
			}
			return FreeCam.FreeCamCamera;
		}

		private static bool IsInventoryOpen()
		{
			bool result;
			try
			{
				result = (PlayerDashboardUI.active && PlayerDashboardInventoryUI.active);
			}
			catch
			{
				result = false;
			}
			return result;
		}

		public static void InitInteractHook()
		{
			if (Reach._h1ok)
			{
				return;
			}
			try
			{
				Reach.Cache();
				Reach._h1m = typeof(PlayerInteract).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
				if (!(Reach._h1m == null))
				{
					Reach.Hook(Reach._h1m, typeof(Reach).GetMethod("H1", BindingFlags.Static | BindingFlags.NonPublic), ref Reach._h1p, Reach._h1s);
					Reach._h1ok = true;
					Runtime.Trace("reach: interact hook OK");
				}
			}
			catch (Exception ex)
			{
				Runtime.Trace("reach: interact err " + ex.Message);
			}
		}

		private static void H1(PlayerInteract self)
		{
			bool flag = State.FarReach && !State.IsSpying;
			if (State.PickupThroughWalls && !State.IsSpying && (FreeCam.Instance != null || Reach.IsInventoryOpen()) && Reach.FakeItemHit())
			{
				Reach.Trampoline(Reach._h1p, Reach._h1s, Reach._h1m, self, "H1");
				return;
			}

			// Run the game's normal interact logic first so close-range interactions always work
			Reach.Trampoline(Reach._h1p, Reach._h1s, Reach._h1m, self, "H1");

			// Far reach: only override if the normal raycast didn't find anything useful
			if (flag && Reach._piHit != null)
			{
				// Check if the game's normal hit produced a valid collider hit
				RaycastHit existingHit = default(RaycastHit);
				try { existingHit = (RaycastHit)Reach._piHit.GetValue(null); } catch { }

				if (existingHit.collider == null)
				{
					// Normal interact found nothing — try far reach raycast
					Camera cam = Reach.GetCam();
					if (cam != null)
					{
						RaycastHit farHit = default(RaycastHit);
						Physics.Raycast(new Ray(cam.transform.position, cam.transform.forward), out farHit, State.FarReachDist, RayMasks.PLAYER_INTERACT, QueryTriggerInteraction.Collide);
						if (farHit.collider != null)
						{
							Reach._piHit.SetValue(null, farHit);
							if (Reach._piLastInteract != null)
								Reach._piLastInteract.SetValue(null, 0f); // bypass lastInteract cooldown
						}
					}
				}
			}
		}

		private static bool FakeItemHit()
		{
			if (Time.realtimeSinceStartup - Reach._pickScanT < 0.1f)
			{
				return false;
			}
			Reach._pickScanT = Time.realtimeSinceStartup;
			Player localPlayer = Player.LocalPlayer;
			if (localPlayer == null)
			{
				return false;
			}
			Reach._pickBuf.Clear();
			try
			{
				Vector3 position = localPlayer.transform.position;
				float num = State.PickupDistance * State.PickupDistance;
				List<InteractableItem> clampedItems = ItemManager.clampedItems;
				if (clampedItems == null)
				{
					return false;
				}
				for (int i = 0; i < clampedItems.Count; i++)
				{
					InteractableItem interactableItem = clampedItems[i];
					if (((interactableItem != null) ? interactableItem.asset : null) != null && (interactableItem.transform.position - position).sqrMagnitude <= num)
					{
						Reach._pickBuf.Add(interactableItem);
					}
				}
			}
			catch
			{
				return false;
			}
			if (Reach._pickBuf.Count == 0)
			{
				return false;
			}
			Camera cam = Reach.GetCam();
			if (cam == null)
			{
				return false;
			}
			Vector2 vector = new Vector2((float)Screen.width * 0.5f, (float)Screen.height * 0.5f);
			InteractableItem interactableItem2 = null;
			float num2 = float.MaxValue;
			for (int j = 0; j < Reach._pickBuf.Count; j++)
			{
				Vector3 vector2 = cam.WorldToScreenPoint(Reach._pickBuf[j].transform.position);
				if (vector2.z > 0f)
				{
					float num3 = Vector2.Distance(vector, new Vector2(vector2.x, vector2.y));
					if (num3 < num2)
					{
						num2 = num3;
						interactableItem2 = Reach._pickBuf[j];
					}
				}
			}
			if (interactableItem2 == null || num2 > 200f)
			{
				return false;
			}
			if (Reach._piHit == null)
			{
				return false;
			}
			Collider collider = interactableItem2.transform.GetComponent<Collider>();
			bool flag = false;
			if (collider == null)
			{
				collider = interactableItem2.transform.gameObject.AddComponent<BoxCollider>();
				((BoxCollider)collider).size = new Vector3(0.25f, 0.25f, 0.25f);
				flag = true;
			}
			RaycastHit raycastHit = default;
			Physics.Raycast(new Ray(collider.bounds.center + Vector3.up * (collider.bounds.extents.y + 0.1f), Vector3.down), out raycastHit, 4f, RayMasks.PLAYER_INTERACT, (QueryTriggerInteraction)1);
			Reach._piHit.SetValue(null, raycastHit);
			if (Reach._piLastInteract != null)
			{
				Reach._piLastInteract.SetValue(null, Time.realtimeSinceStartup);
			}
			if (flag)
			{
				UnityEngine.Object.Destroy(collider);
			}
			return true;
		}

		public static void InitBarricadeHook()
		{
			if (Reach._h2ok)
			{
				return;
			}
			try
			{
				Reach.Cache();
				Reach._h2m = typeof(UseableBarricade).GetMethod("checkSpace", BindingFlags.Instance | BindingFlags.NonPublic);
				if (!(Reach._h2m == null))
				{
					Reach.Hook(Reach._h2m, typeof(Reach).GetMethod("H2", BindingFlags.Static | BindingFlags.NonPublic), ref Reach._h2p, Reach._h2s);
					Reach._h2ok = true;
					Runtime.Trace("reach: checkSpace hook OK");
				}
			}
			catch (Exception ex)
			{
				Runtime.Trace("reach: checkSpace err " + ex.Message);
			}
		}

		private static bool H2(UseableBarricade self)
		{
			bool flag = (bool)Reach.Trampoline(Reach._h2p, Reach._h2s, Reach._h2m, self, "H2");
			if (State.PlaceAnywhere && self.channel.IsLocalPlayer)
			{
				EBuild build = self.equippedBarricadeAsset.build;
				if (build == (EBuild)2 || build == (EBuild)3 || build == (EBuild)34 || build == (EBuild)24 || build == (EBuild)32 || build == (EBuild)0)
				{
					Camera camera = (FreeCam.Instance != null && FreeCam.FreeCamCamera != null) ? FreeCam.FreeCamCamera : MainCamera.instance;
					RaycastHit raycastHit = default;
					Physics.SphereCast(new Ray(camera.transform.position, camera.transform.forward), 0.1f, out raycastHit, self.equippedBarricadeAsset.range, RayMasks.BARRICADE_INTERACT, (QueryTriggerInteraction)1);
					if (Reach._barHit != null)
					{
						Reach._barHit.SetValue(self, raycastHit);
					}
					if (raycastHit.transform == null)
					{
						if (Reach._barPoint != null)
						{
							Reach._barPoint.SetValue(self, Vector3.zero);
						}
						return false;
					}
					Vector3 vector = (raycastHit.normal.y > 0.75f) ? (raycastHit.point + raycastHit.normal * self.equippedBarricadeAsset.offset) : (raycastHit.point + Vector3.up * self.equippedBarricadeAsset.offset);
					if (Reach._barPoint != null)
					{
						Reach._barPoint.SetValue(self, vector);
					}
					if (Reach._barAngleY != null)
					{
						Reach._barAngleY.SetValue(self, camera.transform.eulerAngles.y);
					}
					return true;
				}
			}
			if (FreeCam.Instance != null && FreeCam.FreeCamCamera != null && self.channel.IsLocalPlayer)
			{
				Camera freeCamCamera = FreeCam.FreeCamCamera;
				RaycastHit raycastHit2 = default;
				Physics.Raycast(new Ray(freeCamCamera.transform.position, freeCamCamera.transform.forward), out raycastHit2, self.equippedBarricadeAsset.range, RayMasks.SLOTS_INTERACT, (QueryTriggerInteraction)1);
				if (Reach._barHit != null)
				{
					Reach._barHit.SetValue(self, raycastHit2);
				}
				if (raycastHit2.collider == null)
				{
					if (Reach._barPoint != null)
					{
						Reach._barPoint.SetValue(self, Vector3.zero);
					}
					if (self.channel.IsLocalPlayer)
					{
						PlayerUI.hint(null, (EPlayerMessage)24);
					}
					return false;
				}
				Vector3 vector2 = (raycastHit2.normal.y > 0.75f) ? (raycastHit2.point + raycastHit2.normal * self.equippedBarricadeAsset.offset) : (raycastHit2.point + Vector3.up * self.equippedBarricadeAsset.offset);
				if (Reach._barPoint != null)
				{
					Reach._barPoint.SetValue(self, vector2);
				}
				return true;
			}
			else
			{
				if (State.CustomBuildOffset)
				{
					RaycastHit raycastHit3 = default;
					Physics.Raycast(new Ray(MainCamera.instance.transform.position, MainCamera.instance.transform.forward), out raycastHit3, self.equippedBarricadeAsset.range, RayMasks.SLOTS_INTERACT, (QueryTriggerInteraction)1);
					if (Reach._barHit != null)
					{
						Reach._barHit.SetValue(self, raycastHit3);
					}
					Vector3 vector3 = MainCamera.instance.transform.position + MainCamera.instance.transform.forward * 2f;
					vector3.x += State.BuildOffsetX;
					vector3.y += State.BuildOffsetY;
					vector3.z += State.BuildOffsetZ;
					if (raycastHit3.normal.y > 0.75f)
					{
						vector3 += raycastHit3.normal * self.equippedBarricadeAsset.offset;
					}
					else
					{
						vector3 += Vector3.up * self.equippedBarricadeAsset.offset;
					}
					if (Reach._barPoint != null)
					{
						Reach._barPoint.SetValue(self, vector3);
					}
					return true;
				}
				return State.IgnoreBarricadeErrors || flag;
			}
		}

		public static void InitClaimsHook()
		{
			if (Reach._h3ok)
			{
				return;
			}
			try
			{
				Reach._h3m = typeof(UseableBarricade).GetMethod("checkClaims", BindingFlags.Instance | BindingFlags.NonPublic);
				if (!(Reach._h3m == null))
				{
					Reach.Hook(Reach._h3m, typeof(Reach).GetMethod("H3", BindingFlags.Static | BindingFlags.NonPublic), ref Reach._h3p, Reach._h3s);
					Reach._h3ok = true;
					Runtime.Trace("reach: checkClaims hook OK");
				}
			}
			catch (Exception ex)
			{
				Runtime.Trace("reach: checkClaims err " + ex.Message);
			}
		}

		private static bool H3(UseableBarricade self)
		{
			bool flag = (bool)Reach.Trampoline(Reach._h3p, Reach._h3s, Reach._h3m, self, "H3");
			return State.IgnoreBarricadeErrors || flag;
		}

		public static void InitStructureHook()
		{
			if (Reach._h4ok)
			{
				return;
			}
			try
			{
				Reach.Cache();
				if (!(Reach._strType == null))
				{
					Reach._h4m = Reach._strType.GetMethod("UpdatePendingPlacement", BindingFlags.Instance | BindingFlags.NonPublic);
					if (!(Reach._h4m == null))
					{
						Reach.Hook(Reach._h4m, typeof(Reach).GetMethod("H4", BindingFlags.Static | BindingFlags.NonPublic), ref Reach._h4p, Reach._h4s);
						Reach._h4ok = true;
						Runtime.Trace("reach: structure hook OK");
					}
				}
			}
			catch (Exception ex)
			{
				Runtime.Trace("reach: structure err " + ex.Message);
			}
		}

		private static bool H4(object self)
		{
			bool flag = (bool)Reach.Trampoline(Reach._h4p, Reach._h4s, Reach._h4m, self, "H4");
			if (FreeCam.Instance != null && FreeCam.FreeCamCamera != null)
			{
				Camera freeCamCamera = FreeCam.FreeCamCamera;
				float num = 64f;
				try
				{
					num = ((UseableStructure)self).equippedStructureAsset.range;
				}
				catch
				{
				}
				RaycastHit raycastHit = default;
				if (!Physics.SphereCast(new Ray(freeCamCamera.transform.position, freeCamCamera.transform.forward), 0.1f, out raycastHit, num, RayMasks.STRUCTURE_INTERACT, (QueryTriggerInteraction)1))
				{
					if (Reach._strPos != null)
					{
						Reach._strPos.SetValue(self, Vector3.zero);
					}
					return false;
				}
				if (Reach._strPos != null)
				{
					Reach._strPos.SetValue(self, raycastHit.point);
				}
				if (Reach._strYaw != null)
				{
					Reach._strYaw.SetValue(self, freeCamCamera.transform.eulerAngles.y);
				}
				return true;
			}
			else
			{
				if (State.CustomBuildOffset)
				{
					Vector3 vector = MainCamera.instance.transform.position + MainCamera.instance.transform.forward * 2f;
					vector.x += State.BuildOffsetX;
					vector.y += State.BuildOffsetY;
					vector.z += State.BuildOffsetZ;
					if (Reach._strPos != null)
					{
						Reach._strPos.SetValue(self, vector);
					}
					if (Reach._strYaw != null)
					{
						Reach._strYaw.SetValue(self, MainCamera.instance.transform.eulerAngles.y);
					}
					return true;
				}
				return State.IgnoreStructureErrors || flag;
			}
		}

		public static void InitNearbyHook()
		{
			if (Reach._h5ok)
			{
				return;
			}
			if (!State.ExtendNearbyRadius)
			{
				return;
			}
			try
			{
				Reach._h5m = typeof(ItemManager).GetMethod("findSimulatedItemsInRadius", BindingFlags.Static | BindingFlags.Public);
				if (!(Reach._h5m == null))
				{
					Reach.Hook(Reach._h5m, typeof(Reach).GetMethod("H5", BindingFlags.Static | BindingFlags.NonPublic), ref Reach._h5p, Reach._h5s);
					Reach._h5ok = true;
					Runtime.Trace("reach: findRadius hook OK");
				}
			}
			catch
			{
			}
		}

		private static void H5(Vector3 center, float sqrRadius, List<InteractableItem> result)
		{
			if (State.ExtendNearbyRadius)
			{
				sqrRadius = State.NearbyRadius * State.NearbyRadius;
			}
			List<InteractableItem> clampedItems = ItemManager.clampedItems;
			if (clampedItems != null)
			{
				int num = 0;
				for (int i = 0; i < clampedItems.Count; i++)
				{
					InteractableItem interactableItem = clampedItems[i];
					if (interactableItem != null && (interactableItem.transform.position - center).sqrMagnitude <= sqrRadius)
					{
						result.Add(interactableItem);
						num++;
					}
				}
				if (++Reach._h5log % 30 == 1)
				{
					Runtime.Trace(string.Concat(new string[]
					{
						"H5: clampedItems=",
						clampedItems.Count.ToString(),
						" sqrR=",
						((int)sqrRadius).ToString(),
						" found=",
						num.ToString(),
						" ExtNear=",
						State.ExtendNearbyRadius.ToString(),
						" rad=",
						State.NearbyRadius.ToString()
					}));
				}
			}
		}

		public static void InitNearbyWallHook()
		{
			if (Reach._h7ok)
			{
				return;
			}
			if (!State.ExtendNearbyRadius && !State.NearbyThroughWalls)
			{
				return;
			}
			try
			{
				Type typeFromHandle = typeof(PlayerDashboardInventoryUI);
				BindingFlags bindingAttr = BindingFlags.Static | BindingFlags.NonPublic;
				Reach._h7m = typeFromHandle.GetMethod("updateNearbyDrops", BindingFlags.Static | BindingFlags.Public);
				if (!(Reach._h7m == null))
				{
					Reach._pendingField = typeFromHandle.GetField("pendingItemsInRadius", bindingAttr);
					Reach._areaItemsField = typeFromHandle.GetField("areaItems", bindingAttr);
					Reach._createElemMethod = typeFromHandle.GetMethod("createElementForNearbyDrop", bindingAttr);
					Reach._updateBoxMethod = typeFromHandle.GetMethod("updateBoxAreas", bindingAttr);
					Reach.Hook(Reach._h7m, typeof(Reach).GetMethod("H7", BindingFlags.Static | BindingFlags.NonPublic), ref Reach._h7p, Reach._h7s);
					Reach._h7ok = true;
					Runtime.Trace("reach: nearbyDrops hook OK");
				}
			}
			catch (Exception ex)
			{
				Runtime.Trace("reach: nearbyDrops err " + ex.Message);
			}
		}

		private static void H7()
		{
			if (!State.ExtendNearbyRadius && !State.NearbyThroughWalls)
			{
				uint prot;
				Reach.VirtualProtect(Reach._h7p, 14, 64U, out prot);
				Marshal.Copy(Reach._h7s, 0, Reach._h7p, 14);
				Reach.VirtualProtect(Reach._h7p, 14, prot, out prot);
				try
				{
					Reach._h7m.Invoke(null, null);
				}
				finally
				{
					Reach.WriteJmp(Reach._h7p, typeof(Reach).GetMethod("H7", BindingFlags.Static | BindingFlags.NonPublic).MethodHandle.GetFunctionPointer());
				}
			}
			if (Reach._pendingField == null)
			{
				return;
			}
			if (!PlayerDashboardInventoryUI.active)
			{
				return;
			}
			List<InteractableItem> list = Reach._pendingField.GetValue(null) as List<InteractableItem>;
			if (list == null || list.Count < 1)
			{
				return;
			}
			FieldInfo areaItemsField = Reach._areaItemsField;
			object obj = (areaItemsField != null) ? areaItemsField.GetValue(null) : null;
			int num = 0;
			if (obj != null)
			{
				PropertyInfo property = obj.GetType().GetProperty("height");
				if (property != null)
				{
					num = (int)((byte)property.GetValue(obj, null));
				}
			}
			Vector3 eyesPositionWithoutLeaning = Player.LocalPlayer.look.GetEyesPositionWithoutLeaning();
			int num2 = Mathf.Max(0, list.Count - 20);
			for (int i = list.Count - 1; i >= num2; i--)
			{
				InteractableItem interactableItem = list[i];
				list.RemoveAt(i);
				if (!(interactableItem == null) && interactableItem.item != null)
				{
					Renderer componentInChildren = interactableItem.transform.GetComponentInChildren<Renderer>();
					if (!(componentInChildren == null))
					{
						Vector3 center = componentInChildren.bounds.center;
						RaycastHit raycastHit = default;
						if ((State.NearbyThroughWalls || !Physics.Linecast(eyesPositionWithoutLeaning, center, out raycastHit, RayMasks.BLOCK_PICKUP, (QueryTriggerInteraction)1)) && Reach._createElemMethod != null)
						{
							Reach._createElemMethod.Invoke(null, new object[]
							{
								interactableItem
							});
						}
					}
				}
			}
			if (obj != null && Reach._updateBoxMethod != null)
			{
				PropertyInfo property2 = obj.GetType().GetProperty("height");
				if (property2 != null && (int)((byte)property2.GetValue(obj, null)) > num)
				{
					Reach._updateBoxMethod.Invoke(null, null);
				}
			}
		}

		public static void InitItemDropHook()
		{
			if (Reach._h8ok)
			{
				return;
			}
			if (!State.ExtendNearbyRadius && !State.NearbyThroughWalls)
			{
				return;
			}
			try
			{
				Type typeFromHandle = typeof(PlayerDashboardInventoryUI);
				Reach._h8m = typeFromHandle.GetMethod("onItemDropAdded", BindingFlags.Static | BindingFlags.NonPublic);
				if (!(Reach._h8m == null))
				{
					Reach.Hook(Reach._h8m, typeof(Reach).GetMethod("H8", BindingFlags.Static | BindingFlags.NonPublic), ref Reach._h8p, Reach._h8s);
					Reach._h8ok = true;
					if (Reach._pendingField == null)
					{
						Reach._pendingField = typeFromHandle.GetField("pendingItemsInRadius", BindingFlags.Static | BindingFlags.NonPublic);
					}
					if (Reach._areaItemsField == null)
					{
						Reach._areaItemsField = typeFromHandle.GetField("areaItems", BindingFlags.Static | BindingFlags.NonPublic);
					}
					Runtime.Trace("reach: itemDrop hook OK");
				}
			}
			catch (Exception ex)
			{
				Runtime.Trace("reach: itemDrop err " + ex.Message);
			}
		}

		private static void H8(Transform model, InteractableItem item)
		{
			if (!PlayerDashboardInventoryUI.active || !PlayerDashboardUI.active)
			{
				return;
			}
			if (Player.LocalPlayer == null)
			{
				return;
			}
			FieldInfo areaItemsField = Reach._areaItemsField;
			Items items = ((areaItemsField != null) ? areaItemsField.GetValue(null) : null) as Items;
			if (items != null && items.getItemCount() >= 200)
			{
				return;
			}
			if (item == null)
			{
				return;
			}
			Vector3 eyesPositionWithoutLeaning = Player.LocalPlayer.look.GetEyesPositionWithoutLeaning();
			float num = State.ExtendNearbyRadius ? (State.NearbyRadius * State.NearbyRadius) : 16f;
			if ((model.position - eyesPositionWithoutLeaning).sqrMagnitude > num)
			{
				return;
			}
			FieldInfo pendingField = Reach._pendingField;
			List<InteractableItem> list = ((pendingField != null) ? pendingField.GetValue(null) : null) as List<InteractableItem>;
			if (list != null)
			{
				list.Add(item);
			}
		}

		public static void InitSalvageHook()
		{
			if (Reach._h6ok)
			{
				return;
			}
			try
			{
				Reach.Cache();
				Reach._h6m = typeof(PlayerInteract).GetMethod("get_salvageTime", BindingFlags.Instance | BindingFlags.NonPublic);
				if (!(Reach._h6m == null))
				{
					Reach.Hook(Reach._h6m, typeof(Reach).GetMethod("H6", BindingFlags.Static | BindingFlags.NonPublic), ref Reach._h6p, Reach._h6s);
					Reach._h6ok = true;
					Runtime.Trace("reach: salvage hook OK");
				}
			}
			catch (Exception ex)
			{
				Runtime.Trace("reach: salvage err " + ex.Message);
			}
		}

		private static float H6(PlayerInteract self)
		{
			float num = (float)Reach.Trampoline(Reach._h6p, Reach._h6s, Reach._h6m, self, "H6");
			if (self.channel != null && self.channel.IsLocalPlayer)
			{
				return num * State.SalvageMultiplier;
			}
			return num;
		}

		public static void Update()
		{
		}

		private static void Hook(MethodInfo target, MethodInfo replacement, ref IntPtr ptr, byte[] saved)
		{
			RuntimeHelpers.PrepareMethod(target.MethodHandle);
			ptr = target.MethodHandle.GetFunctionPointer();
			RuntimeHelpers.PrepareMethod(replacement.MethodHandle);
			Marshal.Copy(ptr, saved, 0, 14);
			Reach.WriteJmp(ptr, replacement.MethodHandle.GetFunctionPointer());
		}

		private static object Trampoline(IntPtr ptr, byte[] saved, MethodInfo orig, object self, string hookName)
		{
			uint prot;
			Reach.VirtualProtect(ptr, 14, 64U, out prot);
			Marshal.Copy(saved, 0, ptr, 14);
			Reach.VirtualProtect(ptr, 14, prot, out prot);
			object result;
			try
			{
				result = orig.Invoke(self, null);
			}
			finally
			{
				string name = "H" + hookName.Substring(1);
				Reach.WriteJmp(ptr, typeof(Reach).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic).MethodHandle.GetFunctionPointer());
			}
			return result;
		}

		private static void WriteJmp(IntPtr from, IntPtr to)
		{
			uint prot;
			Reach.VirtualProtect(from, 14, 64U, out prot);
			byte[] array = new byte[14];
			array[0] = byte.MaxValue;
			array[1] = 37;
			BitConverter.GetBytes(to.ToInt64()).CopyTo(array, 6);
			Marshal.Copy(array, 0, from, 14);
			Reach.VirtualProtect(from, 14, prot, out prot);
		}

		private static FieldInfo _piHit;

		private static FieldInfo _piLastInteract;

		private static FieldInfo _barPoint;

		private static FieldInfo _barHit;

		private static FieldInfo _barAngleY;

		private static FieldInfo _strPos;

		private static FieldInfo _strYaw;

		private static Type _strType;

		private static bool _cached;

		private static byte[] _h1s = new byte[14];

		private static IntPtr _h1p;

		private static MethodInfo _h1m;

		private static bool _h1ok;

		private static float _pickScanT;

		private static List<InteractableItem> _pickBuf = new List<InteractableItem>();

		private static byte[] _h2s = new byte[14];

		private static IntPtr _h2p;

		private static MethodInfo _h2m;

		private static bool _h2ok;

		private static byte[] _h3s = new byte[14];

		private static IntPtr _h3p;

		private static MethodInfo _h3m;

		private static bool _h3ok;

		private static byte[] _h4s = new byte[14];

		private static IntPtr _h4p;

		private static MethodInfo _h4m;

		private static bool _h4ok;

		private static byte[] _h5s = new byte[14];

		private static IntPtr _h5p;

		private static MethodInfo _h5m;

		private static bool _h5ok;

		private static int _h5log;

		private static byte[] _h7s = new byte[14];

		private static IntPtr _h7p;

		private static MethodInfo _h7m;

		private static bool _h7ok;

		private static FieldInfo _pendingField;

		private static FieldInfo _areaItemsField;

		private static MethodInfo _createElemMethod;

		private static MethodInfo _updateBoxMethod;

		private static byte[] _h8s = new byte[14];

		private static IntPtr _h8p;

		private static MethodInfo _h8m;

		private static bool _h8ok;

		private static byte[] _h6s = new byte[14];

		private static IntPtr _h6p;

		private static MethodInfo _h6m;

		private static bool _h6ok;
	}
}
