using System;
using System.Collections.Generic;
using SDG.Unturned;
using UnityEngine;
using System.Reflection;

namespace gatyware
{
	[Obfuscation(Exclude = true)]
	public class HitboxExpander : MonoBehaviour
	{
		public void Init(int type)
		{
			this._type = type;
			this._sphere = GameObject.CreatePrimitive(0);
			this._sphere.name = "Hitbox";
			this._sphere.transform.parent = base.transform;
			this._sphere.transform.localPosition = new Vector3(0f, 1f, 0f);
			this._sphere.layer = 23;
			Renderer component = this._sphere.GetComponent<Renderer>();
			if (component != null)
			{
				component.enabled = false;
			}
			MeshRenderer component2 = this._sphere.GetComponent<MeshRenderer>();
			if (component2 != null)
			{
				component2.enabled = false;
			}
			SphereCollider component3 = this._sphere.GetComponent<SphereCollider>();
			if (component3 != null)
			{
				component3.isTrigger = false;
			}
			this._sphere.transform.localScale = new Vector3(2f, 2f, 2f);
		}

		private void FixedUpdate()
		{
			if (this._sphere == null)
			{
				return;
			}
			if (State.SilentAimVisCheck)
			{
				this._sphere.transform.localScale = Vector3.zero;
				return;
			}
			if (this.IsDead())
			{
				this._sphere.transform.localScale = Vector3.zero;
				return;
			}
			Vector3 vector;
			vector = new Vector3(0f, 1f, 0f);
			Vector3 pos = base.transform.position + vector;
			if (!this.IsVisible(pos))
			{
				Vector3? vector2 = this.FindVisiblePos(base.transform.position);
				if (vector2 != null)
				{
					vector = base.transform.InverseTransformPoint(vector2.Value);
				}
			}
			this._sphere.transform.localPosition = vector;
			this._sphere.transform.localScale = new Vector3(2f, 2f, 2f);
		}

		private void OnDestroy()
		{
			if (this._sphere != null)
			{
				UnityEngine.Object.Destroy(this._sphere);
			}
		}

		private bool IsVisible(Vector3 pos)
		{
			Camera instance = MainCamera.instance;
			return instance == null || !Physics.Linecast(instance.transform.position, pos, RayMasks.DAMAGE_SERVER, (QueryTriggerInteraction)1);
		}

		private Vector3? FindVisiblePos(Vector3 center)
		{
			Camera instance = MainCamera.instance;
			if (instance == null)
			{
				return null;
			}
			Vector3 position = instance.transform.position;
			float[] array = new float[]
			{
				1f,
				3f,
				-1f
			};
			Vector3? result = null;
			float num = float.MaxValue;
			for (int i = 0; i < 12; i++)
			{
				float num2 = (float)i * 30f * 0.017453292f;
				float num3 = Mathf.Cos(num2) * 2f;
				float num4 = Mathf.Sin(num2) * 2f;
				for (int j = 0; j < array.Length; j++)
				{
					Vector3 vector = center + new Vector3(num3, array[j], num4);
					if (this.IsVisible(vector))
					{
						float num5 = Vector3.Distance(position, vector);
						if (num5 < num)
						{
							num = num5;
							result = new Vector3?(vector);
						}
					}
				}
			}
			return result;
		}

		private bool IsDead()
		{
			switch (this._type)
			{
			case 0:
			{
				Player component = base.GetComponent<Player>();
				return component == null || component.life.isDead;
			}
			case 1:
			{
				Zombie component2 = base.GetComponent<Zombie>();
				return component2 == null || component2.isDead;
			}
			case 2:
			{
				Animal component3 = base.GetComponent<Animal>();
				return component3 == null || component3.isDead;
			}
			default:
				return true;
			}
		}

		public static void UpdateAll()
		{
			if (State.SilentAimVisCheck)
			{
				return;
			}
			HitboxExpander._scanTimer += Time.deltaTime;
			if (HitboxExpander._scanTimer < 2f)
			{
				return;
			}
			HitboxExpander._scanTimer = 0f;
			if (State.SilentAimTargetPlayers)
			{
				for (int i = 0; i < Provider.clients.Count; i++)
				{
					SteamPlayer steamPlayer = Provider.clients[i];
					if (steamPlayer != null && !(steamPlayer.player == null) && !steamPlayer.player.channel.IsLocalPlayer && !steamPlayer.player.life.isDead && (!State.SilentAimFriendly || !PlayerRelation.IsFriend(steamPlayer)))
					{
						int instanceID = steamPlayer.player.GetInstanceID();
						if (!HitboxExpander._tracked.ContainsKey(instanceID))
						{
							if (steamPlayer.player.GetComponent<HitboxExpander>() == null)
							{
								steamPlayer.player.gameObject.AddComponent<HitboxExpander>().Init(0);
							}
							HitboxExpander._tracked[instanceID] = true;
						}
					}
				}
			}
			if (State.SilentAimTargetZombies && ZombieManager.regions != null)
			{
				for (int j = 0; j < ZombieManager.regions.Length; j++)
				{
					ZombieRegion zombieRegion = ZombieManager.regions[j];
					if (zombieRegion != null)
					{
						for (int k = 0; k < zombieRegion.zombies.Count; k++)
						{
							Zombie zombie = zombieRegion.zombies[k];
							if (!(zombie == null) && !zombie.isDead)
							{
								int instanceID2 = zombie.GetInstanceID();
								if (!HitboxExpander._tracked.ContainsKey(instanceID2))
								{
									if (zombie.GetComponent<HitboxExpander>() == null)
									{
										zombie.gameObject.AddComponent<HitboxExpander>().Init(1);
									}
									HitboxExpander._tracked[instanceID2] = true;
								}
							}
						}
					}
				}
			}
			if (AnimalManager.animals != null)
			{
				for (int l = 0; l < AnimalManager.animals.Count; l++)
				{
					Animal animal = AnimalManager.animals[l];
					if (!(animal == null) && !animal.isDead)
					{
						int instanceID3 = animal.GetInstanceID();
						if (!HitboxExpander._tracked.ContainsKey(instanceID3))
						{
							if (animal.GetComponent<HitboxExpander>() == null)
							{
								animal.gameObject.AddComponent<HitboxExpander>().Init(2);
							}
							HitboxExpander._tracked[instanceID3] = true;
						}
					}
				}
			}
		}

		public static void CleanupAll()
		{
			HitboxExpander._tracked.Clear();
			HitboxExpander[] array = UnityEngine.Object.FindObjectsOfType<HitboxExpander>();
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i] != null)
				{
					UnityEngine.Object.Destroy(array[i]);
				}
			}
		}

		private GameObject _sphere;

		private int _type;

		private static float _scanTimer;

		private static Dictionary<int, bool> _tracked = new Dictionary<int, bool>();
	}
}
