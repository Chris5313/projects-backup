using System;
using System.Collections;
using System.Reflection;
using SDG.NetPak;
using SDG.NetTransport;
using SDG.Unturned;
using UnityEngine;
public static class ScreenshotManager
{
	// ─────────────────────────────────────────────────────────────
	// Anti-spy — ported from israeliclient AntiSpy.
	// Hooks Player.ReceiveTakeScreenshot via the [HookMethod] system.
	// ─────────────────────────────────────────────────────────────
	public static bool IsSpying = false;
	public static SimpleDelegate PreDrawEvent;
	public static SimpleDelegate PostDrawEvent;
	public static float ScreenshotTimer = 0f;
	public static Texture2D LastScreenshotTexture;

	private static ReflectedMethod HandleScreenshotData = new ReflectedMethod(typeof(Player), "HandleScreenshotData", BindingFlags.Instance | BindingFlags.NonPublic);
	private static FieldInfo ScreenshotFinalField;

	private static void DestroyPreviewTexture()
	{
		if (LastScreenshotTexture != null)
		{
			UnityEngine.Object.Destroy(LastScreenshotTexture);
			LastScreenshotTexture = null;
		}
	}

	// ── Entry point invoked by PlayerHooks.ReceiveTakeScreenshotOverride ──
	public static void TakeScreenshot(Player p)
	{
		// Start the spy-warning display window immediately
		ScreenshotTimer = Time.realtimeSinceStartup;
		bool passthrough = MiscConfig.spyType == AvatarSpyMode.DontOverride;
		if (passthrough)
		{
			// Run the original unmodified, then still capture the preview so the
			// spy warning shows the exact image the admin received.
			OverrideManager.CallOriginalCore(typeof(PlayerHooks).GetMethod("ReceiveTakeScreenshotOverride"), p, Array.Empty<object>());
			CoroutineHost.StartHostCoroutine(CapturePreviewOnly(p));
			return;
		}
		CoroutineHost.StartHostCoroutine(TakeScreenshotCoroutine(p));
	}

	// DontOverride preview capture: wait for the game's pipeline to finish, then read screenshotFinal.
	private static IEnumerator CapturePreviewOnly(Player p)
	{
		DestroyPreviewTexture();
		for (int i = 0; i < 6; i++) yield return null;
		ReadScreenshotFinal(p);
	}

	public static IEnumerator TakeScreenshotCoroutine(Player p)
	{
		IsSpying = true;
		try
		{
			bool ok = true;
			switch (MiscConfig.spyType)
			{
				case AvatarSpyMode.SpyInFourFrames:
					// Hide everything for a few frames, let the screenshot happen, restore
					DestroyPreviewTexture();
					for (int i = 0; i < 4; i++) yield return null;
					if (PreDrawEvent != null) PreDrawEvent();
					CallOriginalReceiveTakeScreenshot(p);
					// Game's takeScreenshot: WaitForEndOfFrame → capture → encode → send.
					// Wait enough frames for it to fully complete, then capture what was sent.
					for (int i = 0; i < 6; i++) yield return null;
					if (PostDrawEvent != null) PostDrawEvent();
					ReadScreenshotFinal(p);
					break;
				case AvatarSpyMode.SpyInOneFrame:
					DestroyPreviewTexture();
					yield return null;
					if (PreDrawEvent != null) PreDrawEvent();
					CallOriginalReceiveTakeScreenshot(p);
					for (int i = 0; i < 6; i++) yield return null;
					if (PostDrawEvent != null) PostDrawEvent();
					ReadScreenshotFinal(p);
					break;
				case AvatarSpyMode.SpyWithDelay:
					// Hide for a configurable delay before letting the screenshot through
					DestroyPreviewTexture();
					for (float t = 0f; t < MiscConfig.spyDelayTimer; t += Time.unscaledDeltaTime) yield return null;
					if (PreDrawEvent != null) PreDrawEvent();
					CallOriginalReceiveTakeScreenshot(p);
					for (int i = 0; i < 6; i++) yield return null;
					if (PostDrawEvent != null) PostDrawEvent();
					ReadScreenshotFinal(p);
					break;
				case AvatarSpyMode.SendCustomImage:
					ok = TrySendCustomImage(p);
					if (!ok)
					{
						// No custom image available → fall back to frame-hide so cheats are still hidden
						DestroyPreviewTexture();
						for (int i = 0; i < 4; i++) yield return null;
						if (PreDrawEvent != null) PreDrawEvent();
						CallOriginalReceiveTakeScreenshot(p);
						for (int i = 0; i < 6; i++) yield return null;
						if (PostDrawEvent != null) PostDrawEvent();
						ReadScreenshotFinal(p);
					}
					break;
				case AvatarSpyMode.DeclineSpy:
					// Do nothing — the screenshot request is silently dropped
					break;
			}
		}
		finally
		{
			IsSpying = false;
		}
	}

	// Unhook → call original → rehook, via the OverrideManager hook record.
	private static void CallOriginalReceiveTakeScreenshot(Player p)
	{
		try
		{
			OverrideManager.CallOriginalCore(typeof(PlayerHooks).GetMethod("ReceiveTakeScreenshotOverride"), p, Array.Empty<object>());
		}
		catch (Exception ex)
		{
			Logger.LogClient("[antispy] original call failed: " + ex.Message);
		}
	}

	// Grab the screenshot texture the game just sent, so we can show it in the spy warning.
	private static void ReadScreenshotFinal(Player player)
	{
		try
		{
			if (ScreenshotFinalField == null)
			{
				ScreenshotFinalField = typeof(Player).GetField("screenshotFinal", BindingFlags.Instance | BindingFlags.NonPublic);
			}
			if (ScreenshotFinalField == null)
			{
				Logger.LogClient("[antispy] screenshotFinal field not found");
				return;
			}
			Texture2D tex = ScreenshotFinalField.GetValue(player) as Texture2D;
			if (tex == null) return;
			if (tex.width < 4 || tex.height < 4) return;

			// Direct pixel copy — reliable regardless of GPU state
			try
			{
				Color[] pixels = tex.GetPixels();
				LastScreenshotTexture = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
				LastScreenshotTexture.hideFlags = HideFlags.HideAndDontSave;
				LastScreenshotTexture.SetPixels(pixels);
				LastScreenshotTexture.Apply();
			}
			catch
			{
				// Fallback: blit through a render texture
				try
				{
					RenderTexture rt = RenderTexture.GetTemporary(tex.width, tex.height);
					Graphics.Blit(tex, rt);
					RenderTexture prev = RenderTexture.active;
					RenderTexture.active = rt;
					LastScreenshotTexture = new Texture2D(tex.width, tex.height, TextureFormat.RGB24, false);
					LastScreenshotTexture.hideFlags = HideFlags.HideAndDontSave;
					LastScreenshotTexture.ReadPixels(new Rect(0f, 0f, (float)tex.width, (float)tex.height), 0, 0);
					LastScreenshotTexture.Apply();
					RenderTexture.active = prev;
					RenderTexture.ReleaseTemporary(rt);
				}
				catch (Exception ex)
				{
					Logger.LogClient("[antispy] preview capture failed: " + ex.Message);
				}
			}
		}
		catch (Exception ex2)
		{
			Logger.LogClient("[antispy] preview error: " + ex2.Message);
		}
	}

	// ── SendCustomImage mode: send spyimage.png instead of the real screen ──
	private static bool TrySendCustomImage(Player player)
	{
		try
		{
			string path = Application.dataPath + "/spyimage.png";
			if (!System.IO.File.Exists(path))
			{
				Logger.LogClient("[antispy] custom image missing: " + path);
				return false;
			}
			byte[] raw = System.IO.File.ReadAllBytes(path);
			Texture2D tex = new Texture2D(2, 2);
			if (!tex.LoadImage(raw))
			{
				UnityEngine.Object.Destroy(tex);
				Logger.LogClient("[antispy] LoadImage failed");
				return false;
			}

			// Resize to 640x480 like the game's own screenshots
			RenderTexture rt = RenderTexture.GetTemporary(640, 480);
			Graphics.Blit(tex, rt);
			RenderTexture prev2 = RenderTexture.active;
			RenderTexture.active = rt;
			Texture2D resized = new Texture2D(640, 480, TextureFormat.RGB24, false);
			resized.ReadPixels(new Rect(0f, 0f, 640f, 480f), 0, 0);
			resized.Apply();
			RenderTexture.active = prev2;
			RenderTexture.ReleaseTemporary(rt);
			UnityEngine.Object.Destroy(tex);

			byte[] jpg = resized.EncodeToJPG(33);
			UnityEngine.Object.Destroy(resized);
			if (jpg == null || jpg.Length >= 30000)
			{
				Logger.LogClient("[antispy] custom image too large: " + ((jpg == null) ? -1 : jpg.Length));
				return false;
			}

			// Save a preview for the spy notification
			DestroyPreviewTexture();
			LastScreenshotTexture = new Texture2D(2, 2);
			LastScreenshotTexture.LoadImage(jpg);
			LastScreenshotTexture.hideFlags = HideFlags.HideAndDontSave;

			SendScreenshotData(player, jpg);
			return true;
		}
		catch (Exception ex)
		{
			Logger.LogClient("[antispy] custom image error: " + ex.Message);
			return false;
		}
	}

	public static void SendScreenshotData(Player p, byte[] image)
	{
		bool flag = image.Length < 30000;
		if (flag)
		{
			bool isServer = Provider.isServer;
			if (isServer)
			{
				HandleScreenshotData.InvokeOn(p, new object[] { image });
			}
			else
			{
				SendImageDirect(p, image);
			}
		}
	}

	// Send the JPG through the game's own screenshot relay (same path the vanilla client uses),
	// so the server actually accepts and forwards it to the requesting admin.
	private static void SendImageDirect(Player player, byte[] jpg)
	{
		try
		{
			FieldInfo relayField = typeof(Player).GetField("SendScreenshotRelay", BindingFlags.Static | BindingFlags.NonPublic);
			if (relayField == null)
			{
				Logger.LogClient("[antispy] no relay field");
				return;
			}
			object relay = relayField.GetValue(null);
			if (relay == null)
			{
				Logger.LogClient("[antispy] relay null");
				return;
			}

			MethodInfo getNetId = null;
			for (Type t = typeof(Player); t != null && getNetId == null; t = t.BaseType)
			{
				getNetId = t.GetMethod("GetNetId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
			}
			if (getNetId == null)
			{
				Logger.LogClient("[antispy] no GetNetId");
				return;
			}
			object netId = getNetId.Invoke(player, null);

			// Use the game's own writer callback so the wire format matches exactly
			MethodInfo writeMethod = typeof(Player).GetMethod("SendScreenshotRelay_Write", BindingFlags.Instance | BindingFlags.NonPublic);
			if (writeMethod == null)
			{
				Logger.LogClient("[antispy] no write method");
				return;
			}
			Type writerType = writeMethod.GetParameters()[0].ParameterType;
			Type actionType = typeof(Action<,>).MakeGenericType(writerType, typeof(byte[]));
			Delegate writeDel = Delegate.CreateDelegate(actionType, player, writeMethod);

			// Find generic Invoke<T>(NetId, reliability, Action<Writer,T>, T) on the relay
			MethodInfo invokeGeneric = null;
			for (Type rt = relay.GetType(); rt != null && invokeGeneric == null; rt = rt.BaseType)
			{
				foreach (MethodInfo m in rt.GetMethods(BindingFlags.Instance | BindingFlags.Public))
				{
					if (m.Name == "Invoke" && m.IsGenericMethod && m.GetGenericArguments().Length == 1 && m.GetParameters().Length == 4)
					{
						invokeGeneric = m.MakeGenericMethod(typeof(byte[]));
						break;
					}
				}
			}
			if (invokeGeneric == null)
			{
				Logger.LogClient("[antispy] no Invoke<T>(4)");
				return;
			}
			invokeGeneric.Invoke(relay, new object[] { netId, ENetReliability.Reliable, writeDel, jpg });
		}
		catch (Exception ex)
		{
			Logger.LogClient("[antispy] send error: " + ex.Message);
			if (ex.InnerException != null)
			{
				Logger.LogClient("[antispy] send inner: " + ex.InnerException.Message);
			}
		}
	}
}
