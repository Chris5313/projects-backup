using System;
using System.Collections.Generic;
using SDG.Unturned;
using UnityEngine;
public class PlayerBones : MonoBehaviour
{
	[InitializeAttribute]
	private static void Init()
	{
		Provider.onClientDisconnected = (Provider.ClientDisconnected)Delegate.Combine(Provider.onClientDisconnected, new Provider.ClientDisconnected(PlayerBones.ClearAllPlayers));
	}
	public void Awake()
	{
		try
		{
			this.player = base.GetComponent<Player>();
			PlayerBones.playersByNetId.Add(this.player.GetNetId().id, this);
			PlayerBones.playersByPlayer.Add(this.player, this);
			this.bones = default(SkeletonBones);
			this.CacheBones();
		}
		catch (Exception ex)
		{
			Logger.LogClient(ex.Message);
			Logger.LogClient(ex.StackTrace);
		}
	}
	public Vector3 GetLimbPosition(Limb al)
	{
		this.EnsureBonesCached();
		Vector3 vector;
		switch (al)
		{
		case Limb.Head:
			vector = this.bones.HeadBone.transform.position + Vector3.up * 0.4f;
			break;
		case Limb.Body:
			vector = this.bones.SpineBone.transform.position;
			break;
		case Limb.LeftLeg:
			vector = this.bones.LeftFootBone.transform.position;
			break;
		case Limb.RightLeg:
			vector = this.bones.RightFootBone.transform.position;
			break;
		case Limb.LeftHand:
			vector = this.bones.LeftHandBone.transform.position;
			break;
		case Limb.RightHand:
			vector = this.bones.RightHandBone.transform.position;
			break;
		case Limb.Random:
			vector = this.GetLimbPosition((Limb)UnityEngine.Random.Range(0, 6));
			break;
		default:
			vector = base.transform.position;
			break;
		}
		return vector;
	}
	public Vector3 GetVisibleLimbPosition()
	{
		this.EnsureBonesCached();
		bool flag = !Physics.Linecast(EspDrawer.RenderCamera.transform.position, this.bones.HeadBone.position, RayMasks.DAMAGE_CLIENT, QueryTriggerInteraction.Ignore);
		Vector3 vector;
		if (flag)
		{
			vector = this.bones.HeadBone.position + Vector3.up * 0.4f;
		}
		else
		{
			bool flag2 = !Physics.Linecast(EspDrawer.RenderCamera.transform.position, this.bones.SpineBone.position, RayMasks.DAMAGE_CLIENT, QueryTriggerInteraction.Ignore);
			if (flag2)
			{
				vector = this.bones.SpineBone.position;
			}
			else
			{
				bool flag3 = !Physics.Linecast(EspDrawer.RenderCamera.transform.position, this.bones.LeftHandBone.position, RayMasks.DAMAGE_CLIENT, QueryTriggerInteraction.Ignore);
				if (flag3)
				{
					vector = this.bones.LeftHandBone.position;
				}
				else
				{
					bool flag4 = !Physics.Linecast(EspDrawer.RenderCamera.transform.position, this.bones.RightHandBone.position, RayMasks.DAMAGE_CLIENT, QueryTriggerInteraction.Ignore);
					if (flag4)
					{
						vector = this.bones.RightHandBone.position;
					}
					else
					{
						bool flag5 = !Physics.Linecast(EspDrawer.RenderCamera.transform.position, this.bones.LeftFootBone.position, RayMasks.DAMAGE_CLIENT, QueryTriggerInteraction.Ignore);
						if (flag5)
						{
							vector = this.bones.LeftFootBone.position;
						}
						else
						{
							bool flag6 = !Physics.Linecast(EspDrawer.RenderCamera.transform.position, this.bones.RightFootBone.position, RayMasks.DAMAGE_CLIENT, QueryTriggerInteraction.Ignore);
							if (flag6)
							{
								vector = this.bones.RightFootBone.position;
							}
							else
							{
								vector = this.bones.HeadBone.position + Vector3.up * 0.4f;
							}
						}
					}
				}
			}
		}
		return vector;
	}
	public Transform GetLimbTransform(Limb al)
	{
		this.EnsureBonesCached();
		Transform transform;
		switch (al)
		{
		case Limb.Head:
			transform = this.bones.HeadBone.transform;
			break;
		case Limb.Body:
			transform = this.bones.SpineBone.transform;
			break;
		case Limb.LeftLeg:
			transform = this.bones.LeftFootBone.transform;
			break;
		case Limb.RightLeg:
			transform = this.bones.RightFootBone.transform;
			break;
		case Limb.LeftHand:
			transform = this.bones.LeftHandBone.transform;
			break;
		case Limb.RightHand:
			transform = this.bones.RightHandBone.transform;
			break;
		case Limb.Random:
			transform = this.GetLimbTransform((Limb)UnityEngine.Random.Range(0, 6));
			break;
		default:
			transform = base.transform;
			break;
		}
		return transform;
	}
	public Transform GetVisibleLimbTransform()
	{
		this.EnsureBonesCached();
		bool flag = !Physics.Linecast(EspDrawer.RenderCamera.transform.position, this.bones.HeadBone.position, RayMasks.DAMAGE_CLIENT, QueryTriggerInteraction.Ignore);
		Transform transform;
		if (flag)
		{
			transform = this.bones.HeadBone;
		}
		else
		{
			bool flag2 = !Physics.Linecast(EspDrawer.RenderCamera.transform.position, this.bones.SpineBone.position, RayMasks.DAMAGE_CLIENT, QueryTriggerInteraction.Ignore);
			if (flag2)
			{
				transform = this.bones.SpineBone;
			}
			else
			{
				bool flag3 = !Physics.Linecast(EspDrawer.RenderCamera.transform.position, this.bones.LeftHandBone.position, RayMasks.DAMAGE_CLIENT, QueryTriggerInteraction.Ignore);
				if (flag3)
				{
					transform = this.bones.LeftHandBone;
				}
				else
				{
					bool flag4 = !Physics.Linecast(EspDrawer.RenderCamera.transform.position, this.bones.RightHandBone.position, RayMasks.DAMAGE_CLIENT, QueryTriggerInteraction.Ignore);
					if (flag4)
					{
						transform = this.bones.RightHandBone;
					}
					else
					{
						bool flag5 = !Physics.Linecast(EspDrawer.RenderCamera.transform.position, this.bones.LeftFootBone.position, RayMasks.DAMAGE_CLIENT, QueryTriggerInteraction.Ignore);
						if (flag5)
						{
							transform = this.bones.LeftFootBone;
						}
						else
						{
							bool flag6 = !Physics.Linecast(EspDrawer.RenderCamera.transform.position, this.bones.RightFootBone.position, RayMasks.DAMAGE_CLIENT, QueryTriggerInteraction.Ignore);
							if (flag6)
							{
								transform = this.bones.RightFootBone;
							}
							else
							{
								transform = this.bones.HeadBone;
							}
						}
					}
				}
			}
		}
		return transform;
	}
	private void EnsureBonesCached()
	{
		bool flag = this.bones.HeadBone == null || this.bones.SpineBone == null || this.bones.LeftHandBone == null || this.bones.RightHandBone == null || this.bones.LeftFootBone == null || this.bones.RightFootBone == null;
		if (flag)
		{
			this.CacheBones();
		}
	}
	public void CacheBones()
	{
		foreach (Collider collider in base.gameObject.GetComponentsInChildren<Collider>())
		{
			string name = collider.name;
			bool flag = !(name == "Skull");
			if (flag)
			{
				bool flag2 = !(name == "Spine");
				if (flag2)
				{
					bool flag3 = !(name == "Right_Arm");
					if (flag3)
					{
						bool flag4 = !(name == "Left_Arm");
						if (flag4)
						{
							bool flag5 = !(name == "Left_Leg");
							if (flag5)
							{
								bool flag6 = name == "Right_Leg";
								if (flag6)
								{
									this.bones.RightLegBone = collider.transform;
									foreach (Transform transform in collider.GetComponentsInChildren<Transform>())
									{
										bool flag7 = transform.name == "Right_Foot";
										if (flag7)
										{
											this.bones.RightFootBone = transform;
											break;
										}
									}
								}
							}
							else
							{
								this.bones.LeftLegBone = collider.transform;
								foreach (Transform transform2 in collider.GetComponentsInChildren<Transform>())
								{
									bool flag8 = transform2.name == "Left_Foot";
									if (flag8)
									{
										this.bones.LeftFootBone = transform2;
										break;
									}
								}
							}
						}
						else
						{
							this.bones.LeftArmBone = collider.transform;
							foreach (Transform transform3 in collider.GetComponentsInChildren<Transform>())
							{
								bool flag9 = transform3.name == "Left_Hand";
								if (flag9)
								{
									this.bones.LeftHandBone = transform3;
								}
								else
								{
									bool flag10 = transform3.name == "Left_Hook";
									if (flag10)
									{
										this.bones.LeftHookBone = transform3;
									}
								}
							}
						}
					}
					else
					{
						this.bones.RightArmBone = collider.transform;
						foreach (Transform transform4 in collider.GetComponentsInChildren<Transform>())
						{
							bool flag11 = transform4.name == "Right_Hand";
							if (flag11)
							{
								this.bones.RightHandBone = transform4;
							}
							else
							{
								bool flag12 = transform4.name == "Right_Hook";
								if (flag12)
								{
									this.bones.RightHookBone = transform4;
								}
							}
						}
					}
				}
				else
				{
					this.bones.SpineBone = collider.transform;
				}
			}
			else
			{
				this.bones.HeadBone = collider.transform;
			}
		}
	}
	public void OnDestroy()
	{
		PlayerBones.playersByNetId.Remove(this.player.GetNetId().id);
		PlayerBones.playersByPlayer.Remove(this.player);
	}
	public static void UnregisterPlayer(uint playerNetId, Player p = null)
	{
		bool flag = PlayerBones.playersByNetId.ContainsKey(playerNetId);
		if (flag)
		{
			PlayerBones.playersByNetId.Remove(playerNetId);
		}
		bool flag2 = p != null;
		if (flag2)
		{
			PlayerBones.playersByPlayer.Remove(p);
		}
	}
	public static void ClearAllPlayers()
	{
		PlayerBones.playersByNetId.Clear();
		PlayerBones.playersByPlayer.Clear();
	}
	public static Dictionary<Player, PlayerBones> playersByPlayer = new Dictionary<Player, PlayerBones>();
	public static Dictionary<uint, PlayerBones> playersByNetId = new Dictionary<uint, PlayerBones>();
	public static Dictionary<uint, ushort> primarySlotItemIds = new Dictionary<uint, ushort>();
	public static Dictionary<uint, ushort> secondarySlotItemIds = new Dictionary<uint, ushort>();
	public GameObject unusedGameObject;
	public SkeletonBones bones;
	public Player player;
}
