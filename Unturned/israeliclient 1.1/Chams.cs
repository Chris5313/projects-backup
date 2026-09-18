using System;
using System.Collections.Generic;
using SDG.Unturned;
using UnityEngine;

namespace gatyware
{
	public static class Chams
	{
		private static void Init()
		{
			if (Chams._inited)
			{
				return;
			}
			Chams._inited = true;
			Shader shader = Shader.Find("Hidden/Internal-Colored");
			if (shader == null)
			{
				return;
			}
			Chams._visM = Chams.MakeMat(shader, 2);
			Chams._nonVisM = Chams.MakeMat(shader, 5);
			Chams._selfM = Chams.MakeMat(shader, 0);
			Chams._wpnM = Chams.MakeMat(shader, 0);
			Chams._wpnWireM = Chams.MakeWireMat(shader, 0);
			Chams._visWireM = Chams.MakeWireMat(shader, 2);
			Chams._nonVisWireM = Chams.MakeWireMat(shader, 5);
		}

		private static Material MakeMat(Shader s, int zTest)
		{
			Material material = new Material(s);
			material.hideFlags = (HideFlags)61;
			material.SetInt("_SrcBlend", 5);
			material.SetInt("_DstBlend", 10);
			material.SetInt("_Cull", 0);
			material.SetInt("_ZWrite", 0);
			material.SetInt("_ZTest", zTest);
			return material;
		}

		private static Material MakeWireMat(Shader s, int zTest)
		{
			Material material = new Material(s);
			material.hideFlags = (HideFlags)61;
			material.SetInt("_SrcBlend", 1);
			material.SetInt("_DstBlend", 1);
			material.SetInt("_Cull", 0);
			material.SetInt("_ZWrite", 0);
			material.SetInt("_ZTest", zTest);
			return material;
		}

		private static void ApplySelf()
		{
			if (!State.SelfChams || State.IsSpying)
			{
				if (Chams._selfApplied)
				{
					Chams.RestoreSelf();
				}
				return;
			}
			Player localPlayer = Player.LocalPlayer;
			if (localPlayer == null)
			{
				if (Chams._selfApplied)
				{
					Chams.RestoreSelf();
				}
				return;
			}
			if (Chams._ownModel0 == null || Chams._ownModel1 == null)
			{
				Renderer[] componentsInChildren = localPlayer.GetComponentsInChildren<Renderer>(true);
				Runtime.Trace("self: " + componentsInChildren.Length.ToString() + " renderers on local player");
				foreach (Renderer renderer in componentsInChildren)
				{
					if (!(renderer == null) && renderer is SkinnedMeshRenderer)
					{
						string name = renderer.gameObject.name;
						if (name == "Model_0" && Chams._ownModel0 == null)
						{
							Chams._ownModel0 = renderer;
						}
						else if (name == "Model_1" && Chams._ownModel1 == null)
						{
							Chams._ownModel1 = renderer;
						}
					}
				}
				if (Chams._ownModel1 == null && localPlayer.clothing != null)
				{
					try
					{
						HumanClothes thirdClothes = localPlayer.clothing.thirdClothes;
						if (thirdClothes != null)
						{
							Renderer componentInChildren = thirdClothes.GetComponentInChildren<Renderer>(true);
							if (componentInChildren != null)
							{
								Chams._ownModel1 = componentInChildren;
								Runtime.Trace("  3rd via thirdClothes: " + componentInChildren.gameObject.name);
							}
						}
					}
					catch
					{
					}
				}
			}
			if (Chams._ownHands == null)
			{
				PlayerAnimator animator = localPlayer.animator;
				if (animator != null && animator.viewmodelParentTransform != null && animator.viewmodelParentTransform.childCount > 0)
				{
					Transform child = animator.viewmodelParentTransform.GetChild(0);
					if (child != null)
					{
						Chams._ownHands = child.GetComponentInChildren<SkinnedMeshRenderer>();
					}
				}
				Runtime.Trace(string.Concat(new string[]
				{
					"self: M0=",
					(Chams._ownModel0 != null).ToString(),
					" M1=",
					(Chams._ownModel1 != null).ToString(),
					" hands=",
					(Chams._ownHands != null).ToString()
				}));
			}
			Chams._selfM.color = State.SelfChamsColor;
			Material[] materials = new Material[]
			{
				Chams._selfM
			};
			if (Chams._ownModel0 != null && Chams._ownOrigMats0 == null)
			{
				Chams._ownOrigMats0 = Chams._ownModel0.sharedMaterials;
				Chams._ownModel0.materials = materials;
			}
			else if (Chams._ownModel0 != null)
			{
				Chams._ownModel0.materials = materials;
			}
			if (Chams._ownModel1 != null && Chams._ownOrigMats1 == null)
			{
				Chams._ownOrigMats1 = Chams._ownModel1.sharedMaterials;
				Chams._ownModel1.materials = materials;
			}
			else if (Chams._ownModel1 != null)
			{
				Chams._ownModel1.materials = materials;
			}
			if (Chams._ownHands != null && Chams._ownOrigHands == null)
			{
				Chams._ownOrigHands = Chams._ownHands.sharedMaterials;
				Chams._ownHands.materials = materials;
			}
			else if (Chams._ownHands != null)
			{
				Chams._ownHands.materials = materials;
			}
			Chams._selfApplied = true;
		}

		private static void RestoreSelf()
		{
			if (Chams._ownModel0 != null && Chams._ownOrigMats0 != null)
			{
				Chams._ownModel0.materials = Chams._ownOrigMats0;
			}
			if (Chams._ownModel1 != null && Chams._ownOrigMats1 != null)
			{
				Chams._ownModel1.materials = Chams._ownOrigMats1;
			}
			if (Chams._ownHands != null && Chams._ownOrigHands != null)
			{
				Chams._ownHands.materials = Chams._ownOrigHands;
			}
			Player lp = Player.LocalPlayer;
			if (lp != null)
			{
				foreach (Renderer r in lp.GetComponentsInChildren<Renderer>(true))
				{
					if (r != null && r.sharedMaterial == Chams._selfM)
					{
						r.materials = new Material[] { r.material };
						r.material.color = Color.white;
						r.material.shader = Shader.Find("Standard");
					}
				}
			}
			Chams._ownOrigMats0 = (Chams._ownOrigMats1 = (Chams._ownOrigHands = null));
			Chams._ownModel0 = (Chams._ownModel1 = null);
			Chams._ownHands = null;
			Chams._selfApplied = false;
		}

		private static void CollectWeaponRenderers(Transform model)
		{
			if (model == null)
			{
				return;
			}
			foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
			{
				if (!(renderer == null))
				{
					string name = renderer.GetType().Name;
					if (!(name == "ParticleSystemRenderer") && !(name == "TrailRenderer") && !(name == "LineRenderer"))
					{
						Chams._wpnRends.Add(renderer);
						Chams._wpnOrigMats.Add(renderer.sharedMaterials);
					}
				}
			}
		}

		private static void ApplyWeaponChams()
		{
			if (!State.WeaponChams || State.IsSpying)
			{
				if (Chams._wpnApplied)
				{
					Chams.RestoreWeaponChams();
				}
				return;
			}
			Player localPlayer = Player.LocalPlayer;
			if (((localPlayer != null) ? localPlayer.equipment : null) == null)
			{
				if (Chams._wpnApplied)
				{
					Chams.RestoreWeaponChams();
				}
				return;
			}
			int num = (int)((localPlayer.equipment.asset != null) ? localPlayer.equipment.asset.id : 0);
			bool flag = num != Chams._wpnLastEquipId || Chams._wpnRends.Count == 0;
			if (!flag)
			{
				for (int i = 0; i < Chams._wpnRends.Count; i++)
				{
					if (Chams._wpnRends[i] == null)
					{
						flag = true;
						break;
					}
				}
				int num2 = 0;
				if (localPlayer.equipment.firstModel != null)
				{
					num2 += localPlayer.equipment.firstModel.GetComponentsInChildren<Renderer>(true).Length;
				}
				if (localPlayer.equipment.thirdModel != null)
				{
					num2 += localPlayer.equipment.thirdModel.GetComponentsInChildren<Renderer>(true).Length;
				}
				if (num2 != Chams._wpnRends.Count)
				{
					flag = true;
				}
			}
			if (flag)
			{
				Chams.RestoreWeaponChams();
				Chams._wpnLastEquipId = num;
				if (num == 0)
				{
					return;
				}
				Chams.CollectWeaponRenderers(localPlayer.equipment.firstModel);
				Chams.CollectWeaponRenderers(localPlayer.equipment.thirdModel);
			}
			Chams._wpnM.color = State.WeaponChamsColor;
			Material[] materials = new Material[]
			{
				Chams._wpnM
			};
			if (State.WeaponWireframe)
			{
				Chams._wpnWireM.color = State.WeaponChamsColor;
				materials = new Material[]
				{
					Chams._wpnWireM
				};
			}
			for (int j = 0; j < Chams._wpnRends.Count; j++)
			{
				if (Chams._wpnRends[j] != null)
				{
					Chams._wpnRends[j].materials = materials;
				}
			}
			Chams._wpnApplied = true;
		}

		private static void RestoreWeaponChams()
		{
			for (int i = 0; i < Chams._wpnRends.Count; i++)
			{
				if (Chams._wpnRends[i] != null && i < Chams._wpnOrigMats.Count && Chams._wpnOrigMats[i] != null)
				{
					Chams._wpnRends[i].materials = Chams._wpnOrigMats[i];
				}
			}
			Chams._wpnRends.Clear();
			Chams._wpnOrigMats.Clear();
			Chams._wpnApplied = false;
		}

		private static void ApplyPlayerModel(GameObject go, Color visC, Color nonVisC)
		{
			int instanceID = go.GetInstanceID();
			Chams._active[instanceID] = true;
			Material material = State.ChamsWireframe ? Chams._visWireM : Chams._visM;
			Material material2 = State.ChamsWireframe ? Chams._nonVisWireM : Chams._nonVisM;
			material.color = visC;
			material2.color = nonVisC;
			if (Chams._applied.ContainsKey(instanceID))
			{
				return;
			}
			List<Chams.Entry> list = new List<Chams.Entry>();
			foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>(true))
			{
				if (!(renderer == null))
				{
					string name = renderer.gameObject.name;
					if (!(name != "Model_0") || !(name != "Model_1"))
					{
						list.Add(new Chams.Entry
						{
							rend = renderer,
							orig = renderer.sharedMaterials
						});
						renderer.materials = new Material[]
						{
							material2,
							material
						};
					}
				}
			}
			Chams._applied[instanceID] = list;
		}

		private static void Apply(GameObject go, Color visC, Color nonVisC)
		{
			int instanceID = go.GetInstanceID();
			Chams._active[instanceID] = true;
			Material material = State.ChamsWireframe ? Chams._visWireM : Chams._visM;
			Material material2 = State.ChamsWireframe ? Chams._nonVisWireM : Chams._nonVisM;
			material.color = visC;
			material2.color = nonVisC;
			if (Chams._applied.ContainsKey(instanceID))
			{
				return;
			}
			List<Chams.Entry> list = new List<Chams.Entry>();
			foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>(true))
			{
				if (!(renderer == null))
				{
					list.Add(new Chams.Entry
					{
						rend = renderer,
						orig = renderer.sharedMaterials
					});
					renderer.materials = new Material[]
					{
						material2,
						material
					};
				}
			}
			Chams._applied[instanceID] = list;
		}

		public static void BeginFrame()
		{
			Chams.Init();
			Chams._active.Clear();
			if (Chams._lastWireframe != State.ChamsWireframe)
			{
				Chams._lastWireframe = State.ChamsWireframe;
				foreach (KeyValuePair<int, List<Chams.Entry>> keyValuePair in Chams._applied)
				{
					foreach (Chams.Entry entry in keyValuePair.Value)
					{
						if (entry.rend != null)
						{
							entry.rend.materials = entry.orig;
						}
					}
				}
				Chams._applied.Clear();
			}
			Chams.ApplySelf();
			Chams.ApplyWeaponChams();
		}

		public static void ApplyPlayer(SteamPlayer sp)
		{
			if (State.IsSpying)
			{
				return;
			}
			if (!State.PlayerChams || ((sp != null) ? sp.player : null) == null)
			{
				return;
			}
			if (sp.player == Player.LocalPlayer)
			{
				return;
			}
			if (sp.player.channel != null && sp.player.channel.IsLocalPlayer)
			{
				return;
			}
			Chams.ApplyPlayerModel(sp.player.gameObject, State.PlayerChamsVis, State.PlayerChamsNonVis);
		}

		public static void ApplyZombie(Zombie z)
		{
			if (State.IsSpying)
			{
				return;
			}
			if (!State.ZombieChams || z == null)
			{
				return;
			}
			Chams.Apply(z.gameObject, State.ZombieChamsVis, State.ZombieChamsNonVis);
		}

		public static void ApplyVehicle(InteractableVehicle v)
		{
			if (State.IsSpying)
			{
				return;
			}
			if (!State.VehicleChams || v == null)
			{
				return;
			}
			Chams.Apply(v.gameObject, State.VehicleChamsVis, State.VehicleChamsNonVis);
		}

		public static void EndFrame()
		{
			Chams._removeBuffer.Clear();
			List<int> removeBuffer = Chams._removeBuffer;
			foreach (KeyValuePair<int, List<Chams.Entry>> keyValuePair in Chams._applied)
			{
				if (!Chams._active.ContainsKey(keyValuePair.Key))
				{
					removeBuffer.Add(keyValuePair.Key);
				}
			}
			foreach (int key in removeBuffer)
			{
				foreach (Chams.Entry entry in Chams._applied[key])
				{
					if (entry.rend != null)
					{
						entry.rend.materials = entry.orig;
					}
				}
				Chams._applied.Remove(key);
			}
		}

		public static bool ForceDisable;

		public static void CleanupAll()
		{
			RestoreSelf();
			RestoreWeaponChams();
			foreach (KeyValuePair<int, List<Chams.Entry>> kvp in Chams._applied)
			{
				foreach (Chams.Entry entry in kvp.Value)
				{
					if (entry.rend != null)
					{
						entry.rend.materials = entry.orig;
					}
				}
			}
			Chams._applied.Clear();
		}

		private static Material _visM;

		private static Material _nonVisM;

		private static Material _selfM;

		private static Material _wpnM;

		private static Material _wpnWireM;

		private static Material _visWireM;

		private static Material _nonVisWireM;

		private static bool _inited;

		private static Dictionary<int, List<Chams.Entry>> _applied = new Dictionary<int, List<Chams.Entry>>();

		private static Dictionary<int, bool> _active = new Dictionary<int, bool>();

		private static readonly List<int> _removeBuffer = new List<int>();

		private static Renderer _ownModel0;

		private static Renderer _ownModel1;

		private static Renderer _ownHands;

		private static Material[] _ownOrigMats0;

		private static Material[] _ownOrigMats1;

		private static Material[] _ownOrigHands;

		private static bool _selfApplied;

		private static List<Renderer> _wpnRends = new List<Renderer>();

		private static List<Material[]> _wpnOrigMats = new List<Material[]>();

		private static bool _wpnApplied;

		private static int _wpnLastEquipId;

		private static bool _lastWireframe;

		private struct Entry
		{
			public Renderer rend;

			public Material[] orig;
		}
	}
}
