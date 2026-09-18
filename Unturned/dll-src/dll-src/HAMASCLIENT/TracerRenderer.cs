using System;
using System.Collections.Generic;
using SDG.Unturned;
using UnityEngine;
public class TracerRenderer
{
	[InitializeAttribute]
	private static void Init()
	{
		ScreenshotManager.PreDrawEvent = (SimpleDelegate)Delegate.Combine(ScreenshotManager.PreDrawEvent, new SimpleDelegate(delegate
		{
			TracerRenderer.SetEffectsActive(false);
		}));
		ScreenshotManager.PostDrawEvent = (SimpleDelegate)Delegate.Combine(ScreenshotManager.PostDrawEvent, new SimpleDelegate(delegate
		{
			TracerRenderer.SetEffectsActive(true);
		}));
		Provider.onClientDisconnected = (Provider.ClientDisconnected)Delegate.Combine(Provider.onClientDisconnected, new Provider.ClientDisconnected(delegate
		{
			foreach (Tracer dvokU5hcv2P63Imn0f6sStWTw in TracerRenderer.activeTracers)
			{
				bool flag = dvokU5hcv2P63Imn0f6sStWTw.TracerObject != null;
				bool flag2 = flag;
				if (flag2)
				{
					UnityEngine.Object.Destroy(dvokU5hcv2P63Imn0f6sStWTw.TracerObject);
				}
			}
			foreach (StepMarker dcjw8k4jS5tnCUQXemffRym in TracerRenderer.activeStepMarkers)
			{
				bool flag3 = dcjw8k4jS5tnCUQXemffRym.gameObject != null;
				bool flag4 = flag3;
				if (flag4)
				{
					UnityEngine.Object.Destroy(dcjw8k4jS5tnCUQXemffRym.gameObject);
				}
			}
		}));
	}
	private static void SetEffectsActive(bool state)
	{
		foreach (Tracer dvokU5hcv2P63Imn0f6sStWTw in TracerRenderer.activeTracers)
		{
			dvokU5hcv2P63Imn0f6sStWTw.TracerObject.SetActive(state);
		}
		foreach (StepMarker dcjw8k4jS5tnCUQXemffRym in TracerRenderer.activeStepMarkers)
		{
			dcjw8k4jS5tnCUQXemffRym.gameObject.SetActive(state);
		}
	}
	public static void ReplaceTracer(Vector3 startPoint, Vector3 endPoint, uint index)
	{
		for (int i = 0; i < TracerRenderer.activeTracers.Count; i++)
		{
			bool flag = TracerRenderer.activeTracers[i].TracerIndex == index;
			bool flag2 = flag;
			if (flag2)
			{
				UnityEngine.Object.Destroy(TracerRenderer.activeTracers[i].TracerObject);
				TracerRenderer.activeTracers.RemoveAt(i);
				TracerRenderer.SpawnTracer(startPoint, endPoint, index);
				break;
			}
		}
	}
	public static Tracer SpawnTracer(Vector3 startPoint, Vector3 endPoint, uint index = 0U)
	{
		bool flag = !Settings.drawTracers || startPoint == Vector3.zero || endPoint == Vector3.zero;
		bool flag2 = flag;
		Tracer dvokU5hcv2P63Imn0f6sStWTw;
		if (flag2)
		{
			dvokU5hcv2P63Imn0f6sStWTw = new Tracer(Vector3.zero, Vector3.zero, 0f, uint.MaxValue);
		}
		else
		{
			Tracer dvokU5hcv2P63Imn0f6sStWTw2 = new Tracer(startPoint, endPoint, 0f, (index == 0U) ? TracerRenderer.nextTracerIndex : index);
			TracerRenderer.activeTracers.Add(dvokU5hcv2P63Imn0f6sStWTw2);
			TracerRenderer.nextTracerIndex += 1U;
			dvokU5hcv2P63Imn0f6sStWTw = dvokU5hcv2P63Imn0f6sStWTw2;
		}
		return dvokU5hcv2P63Imn0f6sStWTw;
	}
	public static void SpawnDamageHitmarker(Vector3 point, ushort damage)
	{
		bool drawDamageHitmark = Settings.drawDamageHitmark;
		bool flag = drawDamageHitmark;
		if (flag)
		{
			bool flag2 = Settings.isDamageHitmarkersCombined && TracerRenderer.activeHitmarkers.Count != 0;
			bool flag3 = flag2;
			if (flag3)
			{
				for (int i = 0; i < TracerRenderer.activeHitmarkers.Count; i++)
				{
					bool flag4 = Vector3.Distance(TracerRenderer.activeHitmarkers[i].hitPoint, point) <= Settings.damageHitmarksCombineDistance;
					bool flag5 = flag4;
					if (flag5)
					{
						DamageHitmark dz3Itlkf53ILNBmHkkOKBK3gF = TracerRenderer.activeHitmarkers[i];
						dz3Itlkf53ILNBmHkkOKBK3gF.damage += damage;
						dz3Itlkf53ILNBmHkkOKBK3gF.hitPoint = point;
						dz3Itlkf53ILNBmHkkOKBK3gF.labelPosition = point;
						dz3Itlkf53ILNBmHkkOKBK3gF.lifeProgress = 0f;
						bool scaleCombinedHitmarkers = Settings.scaleCombinedHitmarkers;
						bool flag6 = scaleCombinedHitmarkers;
						if (flag6)
						{
							dz3Itlkf53ILNBmHkkOKBK3gF.combineCount += 1;
						}
						TracerRenderer.activeHitmarkers[i] = dz3Itlkf53ILNBmHkkOKBK3gF;
						return;
					}
				}
			}
			TracerRenderer.activeHitmarkers.Add(new DamageHitmark(point, damage));
		}
	}
	public static void SpawnStepMarker(PlayerMovement movement, bool isLand = false)
	{
		bool flag = Vector3.Distance(movement.transform.position, MainCamera.instance.transform.position) < (float)Settings.stepsDrawDistance;
		bool flag2 = flag;
		if (flag2)
		{
			TracerRenderer.activeStepMarkers.Add(new StepMarker(movement, isLand, movement.player.stance.stance == EPlayerStance.SPRINT));
		}
	}
	private static void UpdateTracers()
	{
		bool useGLTracers = Settings.useGLTracers;
		bool flag = useGLTracers;
		if (flag)
		{
			EspManager.ScreenLineMaterial.SetPass(0);
			GL.PushMatrix();
			GL.LoadProjectionMatrix(EspDrawer.ProjectionMatrix);
			GL.modelview = EspDrawer.WorldToCameraMatrix;
			GL.Begin(1);
		}
		Color color = ColorConfig.GetColor("Tracers color");
		float a = color.a;
		List<int> list = new List<int>();
		for (int i = 0; i < TracerRenderer.activeTracers.Count; i++)
		{
			Tracer dvokU5hcv2P63Imn0f6sStWTw = TracerRenderer.activeTracers[i];
			dvokU5hcv2P63Imn0f6sStWTw.DeleteProgression += Time.deltaTime / Settings.tracersLifetime;
			bool flag2 = dvokU5hcv2P63Imn0f6sStWTw.DeleteProgression > 1f;
			bool flag3 = flag2;
			if (flag3)
			{
				list.Add(i);
			}
			else
			{
				bool useGLTracers2 = Settings.useGLTracers;
				bool flag4 = useGLTracers2;
				if (flag4)
				{
					GL.Color(new Color(color.r, color.g, color.b, a * (1f - dvokU5hcv2P63Imn0f6sStWTw.DeleteProgression)));
					GL.Vertex(dvokU5hcv2P63Imn0f6sStWTw.StartPoint);
					GL.Vertex(dvokU5hcv2P63Imn0f6sStWTw.EndPoint);
				}
				else
				{
					bool flag5 = TracerRenderer.activeTracers[i].TracerMaterial != null;
					bool flag6 = flag5;
					if (flag6)
					{
						TracerRenderer.activeTracers[i].TracerMaterial.color = new Color(color.r, color.g, color.b, a * (1f - dvokU5hcv2P63Imn0f6sStWTw.DeleteProgression));
					}
				}
			}
			TracerRenderer.activeTracers[i] = dvokU5hcv2P63Imn0f6sStWTw;
		}
		bool useGLTracers3 = Settings.useGLTracers;
		bool flag7 = useGLTracers3;
		if (flag7)
		{
			GL.End();
			GL.PopMatrix();
		}
		bool flag8 = list.Count > 0;
		bool flag9 = flag8;
		if (flag9)
		{
			list.Reverse();
			foreach (int num in list)
			{
				bool flag10 = !Settings.useGLTracers && TracerRenderer.activeTracers[num].TracerObject != null;
				bool flag11 = flag10;
				if (flag11)
				{
					UnityEngine.Object.Destroy(TracerRenderer.activeTracers[num].TracerObject);
				}
				TracerRenderer.activeTracers.RemoveAt(num);
			}
		}
	}
	private static void UpdateStepMarkers()
	{
		Color color = ColorConfig.GetColor("Player step color");
		float a = color.a;
		int num = -1;
		for (int i = 0; i < TracerRenderer.activeStepMarkers.Count; i++)
		{
			StepMarker dcjw8k4jS5tnCUQXemffRym = TracerRenderer.activeStepMarkers[i];
			dcjw8k4jS5tnCUQXemffRym.lifeProgress += Time.deltaTime / Settings.stepsLifetime;
			bool flag = dcjw8k4jS5tnCUQXemffRym.lifeProgress > 1f || TracerRenderer.activeStepMarkers[i].gameObject == null;
			bool flag2 = flag;
			if (flag2)
			{
				num = i;
			}
			else
			{
				TracerRenderer.activeStepMarkers[i].material.color = new Color(color.r, color.g, color.b, a * (1f - dcjw8k4jS5tnCUQXemffRym.lifeProgress));
				TracerRenderer.activeStepMarkers[i].gameObject.transform.localScale = Vector3.one * (dcjw8k4jS5tnCUQXemffRym.lifeProgress * Settings.stepsSpreadingDistance * TracerRenderer.activeStepMarkers[i].spreadMultiplier);
			}
			TracerRenderer.activeStepMarkers[i] = dcjw8k4jS5tnCUQXemffRym;
		}
		bool flag3 = num != -1;
		bool flag4 = flag3;
		if (flag4)
		{
			UnityEngine.Object.Destroy(TracerRenderer.activeStepMarkers[num].gameObject);
			TracerRenderer.activeStepMarkers.RemoveAt(num);
		}
	}
	private static void UpdateDamageHitmarkers()
	{
		Color color = ColorConfig.GetColor("Damage hitmarkers color");
		float a = color.a;
		int num = -1;
		for (int i = 0; i < TracerRenderer.activeHitmarkers.Count; i++)
		{
			DamageHitmark dz3Itlkf53ILNBmHkkOKBK3gF = TracerRenderer.activeHitmarkers[i];
			dz3Itlkf53ILNBmHkkOKBK3gF.lifeProgress += Time.deltaTime / Settings.damageHitmarksLifetime;
			bool flag = dz3Itlkf53ILNBmHkkOKBK3gF.lifeProgress > 1f;
			bool flag2 = flag;
			if (flag2)
			{
				num = i;
			}
			else
			{
				dz3Itlkf53ILNBmHkkOKBK3gF.labelPosition += Vector3.up * Time.deltaTime * 5f;
				bool flag3 = dz3Itlkf53ILNBmHkkOKBK3gF.labelPosition.IsOnScreen();
				bool flag4 = flag3;
				if (flag4)
				{
					EspDrawer.DrawDistanceLabel(dz3Itlkf53ILNBmHkkOKBK3gF.labelPosition, dz3Itlkf53ILNBmHkkOKBK3gF.damage.ToString(), new Color(color.r, color.g, color.b, a * (1f - dz3Itlkf53ILNBmHkkOKBK3gF.lifeProgress)), (int)(12 + dz3Itlkf53ILNBmHkkOKBK3gF.combineCount * 6));
				}
			}
			TracerRenderer.activeHitmarkers[i] = dz3Itlkf53ILNBmHkkOKBK3gF;
		}
		bool flag5 = num != -1;
		bool flag6 = flag5;
		if (flag6)
		{
			TracerRenderer.activeHitmarkers.RemoveAt(num);
		}
	}
	private static void DrawWalkingTracers()
	{
		bool flag = !Settings.walkingTracers || Provider.clients == null || MainCamera.instance == null;
		bool flag2 = !flag;
		if (flag2)
		{
			EspManager.ScreenLineMaterial.SetPass(0);
			GL.PushMatrix();
			GL.LoadProjectionMatrix(EspDrawer.ProjectionMatrix);
			GL.modelview = EspDrawer.WorldToCameraMatrix;
			GL.Begin(7);
			foreach (SteamPlayer steamPlayer in Provider.clients)
			{
				try
				{
					bool flag3 = steamPlayer == null || steamPlayer.player.life.isDead;
					bool flag4 = !flag3;
					if (flag4)
					{
						bool flag5 = steamPlayer.player.channel.IsLocalPlayer && !Settings.seeOwnWalkingTracers;
						bool flag6 = !flag5;
						if (flag6)
						{
							bool flag7 = !TracerRenderer.walkingTracerStates.ContainsKey(steamPlayer.playerID.steamID.m_SteamID);
							bool flag8 = !flag7;
							if (flag8)
							{
								WalkingTracerState dwC8HjHCTqbh7Mqi1vZHJKcMF = TracerRenderer.walkingTracerStates[steamPlayer.playerID.steamID.m_SteamID];
								Color color = ColorConfig.GetColor("Walking tracers color");
								float a = color.a;
								int num = -1;
								for (int i = 0; i < dwC8HjHCTqbh7Mqi1vZHJKcMF.TracerSegments.Count; i++)
								{
									QuadCorners dhVpCMii4tWhywCamQwr2E8m = dwC8HjHCTqbh7Mqi1vZHJKcMF.TracerSegments[i];
									dhVpCMii4tWhywCamQwr2E8m.Offset += Time.deltaTime / Settings.walkingTracersLifetime;
									bool flag9 = dhVpCMii4tWhywCamQwr2E8m.Offset > 1f;
									bool flag10 = flag9;
									if (flag10)
									{
										num = i;
									}
									else
									{
										GL.Color(new Color(color.r, color.g, color.b, a * (1f - dhVpCMii4tWhywCamQwr2E8m.Offset)));
										GL.Vertex(dhVpCMii4tWhywCamQwr2E8m.LeftBottom);
										GL.Vertex(dhVpCMii4tWhywCamQwr2E8m.LeftTop);
										GL.Vertex(dhVpCMii4tWhywCamQwr2E8m.RightTop);
										GL.Vertex(dhVpCMii4tWhywCamQwr2E8m.RightBottom);
									}
									dwC8HjHCTqbh7Mqi1vZHJKcMF.TracerSegments[i] = dhVpCMii4tWhywCamQwr2E8m;
								}
								bool flag11 = num != -1;
								bool flag12 = flag11;
								if (flag12)
								{
									dwC8HjHCTqbh7Mqi1vZHJKcMF.TracerSegments.RemoveAt(num);
								}
							}
						}
					}
				}
				catch
				{
				}
			}
			GL.End();
			GL.PopMatrix();
		}
	}
	public static void RecordWalkingTracers()
	{
		bool flag = !Settings.walkingTracers;
		bool flag2 = !flag;
		if (flag2)
		{
			foreach (SteamPlayer steamPlayer in Provider.clients)
			{
				bool flag3 = steamPlayer == null || steamPlayer.player.life.isDead;
				bool flag4 = !flag3;
				if (flag4)
				{
					bool flag5 = steamPlayer.player.channel.IsLocalPlayer && !Settings.seeOwnWalkingTracers;
					bool flag6 = !flag5;
					if (flag6)
					{
						bool flag7 = Vector3.Distance(steamPlayer.player.transform.position, MainCamera.instance.transform.position) > (float)Settings.walkingTracersDrawDistance;
						bool flag8 = flag7;
						if (flag8)
						{
							bool flag9 = TracerRenderer.walkingTracerStates.ContainsKey(steamPlayer.playerID.steamID.m_SteamID);
							bool flag10 = flag9;
							if (flag10)
							{
								TracerRenderer.walkingTracerStates.Remove(steamPlayer.playerID.steamID.m_SteamID);
							}
						}
						else
						{
							Vector3 position = steamPlayer.player.transform.position;
							bool flag11 = !TracerRenderer.walkingTracerStates.ContainsKey(steamPlayer.playerID.steamID.m_SteamID);
							bool flag12 = flag11;
							if (flag12)
							{
								TracerRenderer.walkingTracerStates.Add(steamPlayer.playerID.steamID.m_SteamID, new WalkingTracerState(position));
							}
							WalkingTracerState dwC8HjHCTqbh7Mqi1vZHJKcMF = TracerRenderer.walkingTracerStates[steamPlayer.playerID.steamID.m_SteamID];
							Vector3 vector = (dwC8HjHCTqbh7Mqi1vZHJKcMF.LastPoint - position).normalized;
							bool flag13 = vector == Vector3.zero;
							bool flag14 = flag13;
							if (flag14)
							{
								vector = dwC8HjHCTqbh7Mqi1vZHJKcMF.LastMoveDirection;
							}
							else
							{
								dwC8HjHCTqbh7Mqi1vZHJKcMF.LastMoveDirection = vector;
							}
							Vector3 vector2 = Vector3.Cross(vector, Vector3.up);
							Vector3 vector3 = -vector2;
							dwC8HjHCTqbh7Mqi1vZHJKcMF.TracerSegments.Add(new QuadCorners(position + vector2 * Settings.walkingTracersWidth, position + vector3 * Settings.walkingTracersWidth, new Vector3(dwC8HjHCTqbh7Mqi1vZHJKcMF.LeftEdgePoint.x, dwC8HjHCTqbh7Mqi1vZHJKcMF.LeftEdgePoint.y, dwC8HjHCTqbh7Mqi1vZHJKcMF.LeftEdgePoint.z), new Vector3(dwC8HjHCTqbh7Mqi1vZHJKcMF.RightEdgePoint.x, dwC8HjHCTqbh7Mqi1vZHJKcMF.RightEdgePoint.y, dwC8HjHCTqbh7Mqi1vZHJKcMF.RightEdgePoint.z)));
							dwC8HjHCTqbh7Mqi1vZHJKcMF.LeftEdgePoint = position + vector2 * Settings.walkingTracersWidth;
							dwC8HjHCTqbh7Mqi1vZHJKcMF.RightEdgePoint = position + vector3 * Settings.walkingTracersWidth;
							dwC8HjHCTqbh7Mqi1vZHJKcMF.LastPoint = position;
							TracerRenderer.walkingTracerStates[steamPlayer.playerID.steamID.m_SteamID] = dwC8HjHCTqbh7Mqi1vZHJKcMF;
						}
					}
				}
			}
		}
	}
	public static void RenderAll()
	{
		EspDrawer.UpdateCameraMatrices();
		TracerRenderer.DrawWalkingTracers();
		TracerRenderer.UpdateTracers();
		TracerRenderer.UpdateDamageHitmarkers();
		TracerRenderer.UpdateStepMarkers();
		TracerRenderer.DrawSpinDirectionLines();
	}
	private static void DrawSpinDirectionLines()
	{
		bool flag = Player.player != null && Player.player.look.perspective == EPlayerPerspective.FIRST;
		if (!flag)
		{
			bool flag2 = MiscConfig.vehicleSpinbot && MiscConfig.vehicleSpinbotDrawDirection && PlayerInputOverride.VehicleSpinActive && EspDrawer.RenderCamera != null && !ScreenshotManager.IsSpying;
			bool flag3 = MiscConfig.modifyMoveBehaviour && MiscConfig.playerSpinbotDrawDirection && PlayerInputOverride.PlayerSpinActive && EspDrawer.RenderCamera != null && Player.player != null && !ScreenshotManager.IsSpying;
			bool flag4 = !flag2 && !flag3;
			if (!flag4)
			{
				Vector3 vector = Vector3.zero;
				Vector3 vector2 = Vector3.zero;
				bool flag5 = false;
				bool flag6 = flag2;
				if (flag6)
				{
					Vector3 vector3 = PlayerInputOverride.VehicleServerPosition;
					bool flag7 = PlayerInputOverride.VehicleSpinTarget != null;
					if (flag7)
					{
						vector3 = PlayerInputOverride.VehicleSpinTarget.transform.position;
					}
					bool flag8 = vector3 != Vector3.zero;
					if (flag8)
					{
						Vector3 vector4 = PlayerInputOverride.VehicleServerRotation * Vector3.forward;
						vector = vector3;
						vector2 = vector3 + vector4 * 5f;
						flag5 = true;
					}
				}
				Vector3 vector5 = Vector3.zero;
				Vector3 vector6 = Vector3.zero;
				Vector3 vector7 = Vector3.zero;
				bool flag9 = false;
				bool flag10 = flag3;
				if (flag10)
				{
					vector5 = Player.player.transform.position + Vector3.up * 1.5f;
					Quaternion quaternion = Quaternion.Euler(0f, PlayerInputOverride.PlayerServerYaw, 0f);
					vector6 = vector5 + quaternion * Vector3.forward * 3f;
					Quaternion quaternion2 = Quaternion.Euler(PlayerInputOverride.PlayerServerPitch - 90f, PlayerInputOverride.PlayerServerYaw, 0f);
					vector7 = vector5 + quaternion2 * Vector3.forward * 2f;
					flag9 = true;
				}
				bool flag11 = !flag5 && !flag9;
				if (!flag11)
				{
					EspManager.ScreenLineMaterial.SetPass(0);
					GL.PushMatrix();
					GL.LoadProjectionMatrix(EspDrawer.ProjectionMatrix);
					GL.modelview = EspDrawer.WorldToCameraMatrix;
					GL.Begin(1);
					bool flag12 = flag5;
					if (flag12)
					{
						GL.Color(new Color(1f, 0f, 1f, 0.9f));
						GL.Vertex(vector);
						GL.Vertex(vector2);
					}
					bool flag13 = flag9;
					if (flag13)
					{
						GL.Color(new Color(0f, 1f, 1f, 0.9f));
						GL.Vertex(vector5);
						GL.Vertex(vector6);
						GL.Color(new Color(1f, 1f, 0f, 0.9f));
						GL.Vertex(vector5);
						GL.Vertex(vector7);
					}
					GL.End();
					GL.PopMatrix();
				}
			}
		}
	}
	private static List<Tracer> activeTracers = new List<Tracer>();
	private static List<DamageHitmark> activeHitmarkers = new List<DamageHitmark>();
	private static List<StepMarker> activeStepMarkers = new List<StepMarker>();
	private static Dictionary<ulong, WalkingTracerState> walkingTracerStates = new Dictionary<ulong, WalkingTracerState>();
	private static uint nextTracerIndex = 1U;
}
