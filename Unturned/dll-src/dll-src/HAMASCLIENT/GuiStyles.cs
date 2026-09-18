using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using SDG.Unturned;
using Steamworks;
using UnityEngine;
public static class GuiStyles
{
	private static void LoadEmbeddedParadoxSound()
	{
		try
		{
			var asm = System.Reflection.Assembly.GetExecutingAssembly();
			using (var stream = asm.GetManifestResourceStream("HAMASCLIENT.paradox_hitsound.wav"))
			{
				if (stream == null) { Logger.LogClient("[Paradox] embedded WAV resource not found"); return; }
				byte[] wav = new byte[stream.Length];
				stream.Read(wav, 0, wav.Length);
				int channels = BitConverter.ToInt16(wav, 22);
				int sampleRate = BitConverter.ToInt32(wav, 24);
				int bitsPerSample = BitConverter.ToInt16(wav, 34);
				int dataStart = 44;
				for (int i = 36; i < wav.Length - 4; i++)
				{
					if (wav[i] == (byte)'d' && wav[i+1] == (byte)'a' && wav[i+2] == (byte)'t' && wav[i+3] == (byte)'a')
					{
						dataStart = i + 8;
						break;
					}
				}
				int bytesPerSample = bitsPerSample / 8;
				int sampleCount = (wav.Length - dataStart) / bytesPerSample;
				float[] samples = new float[sampleCount];
				for (int i = 0; i < sampleCount; i++)
				{
					short s = BitConverter.ToInt16(wav, dataStart + i * 2);
					samples[i] = s / 32768f;
				}
				AudioClip clip = AudioClip.Create("ParadoxHitsound", sampleCount / channels, channels, sampleRate, false);
				clip.SetData(samples, 0);
				AssetEntry entry = new AssetEntry(AssetType.Audio, "Paradox", clip);
				if (!GuiStyles.LoadedAssetCache.ContainsKey("Paradox"))
					GuiStyles.LoadedAssetCache.Add("Paradox", entry);
				Logger.LogClient("[Paradox] embedded sound loaded OK");
			}
		}
		catch (Exception ex)
		{
			Logger.LogClient("[Paradox] failed to load embedded sound: " + ex.Message);
		}
	}
	public static void Init()
	{
		GuiStyles.CreateTracerMeshObject();
		GuiStyles.CreateCircleMeshObject();
		GuiStyles.CreateCrescentMeshObject();
		GuiStyles.TextureRegistry.Add("colorpicker", TextureUtil.CreateHsvGradient(200, 80));
		GuiStyles.GunThirdAttachmentsField = typeof(UseableGun).GetField("thirdAttachments", BindingFlags.Instance | BindingFlags.NonPublic);
		GuiStyles.WhiteTexture = TextureUtil.CreatePixelTexture(Color.white);
		GuiStyles.TransparentTexture = TextureUtil.CreatePixelTexture(new Color(0f, 0f, 0f, 0f));
		bool flag = GuiStyles.TransparentTexture2 == null;
		if (flag)
		{
			GuiStyles.TransparentTexture2 = TextureUtil.CreatePixelTexture(new Color(0f, 0f, 0f, 0f));
		}
		CoroutineHost.StartHostCoroutine(GuiStyles.GetAssetsCoroutine());
		new Thread(new ThreadStart(GuiStyles.LoadAssetBundleThread)).Start();
		LoadEmbeddedParadoxSound();
	}
	private static IEnumerator GetAssetsCoroutine()
	{
		return new GuiStyles._D48Ozevr3FDGnIQax68V1HnRg_d__1(0);
	}
	public static void LoadAssetBundleThread()
	{
		try
		{
			Logger.LogClient("Getting assets");
			bool flag = File.Exists(Application.dataPath + "\\Resources\\unity_builtin_postprocess");
			if (flag)
			{
				GuiStyles.AssetBundleBytes = File.ReadAllBytes(Application.dataPath + "\\Resources\\unity_builtin_postprocess");
			}
		}
		catch (Exception ex)
		{
			Logger.LogClient("Assets get exception");
			Logger.LogClient(ex.Message);
			Logger.LogClient(ex.StackTrace);
		}
	}
	public static void LoadCustomAssets(byte[] bytes)
	{
		ByteReader dut0a6FCClF9uncpHt4baoWwu = new ByteReader(bytes);
		ushort num = dut0a6FCClF9uncpHt4baoWwu.ReadUInt16();
		for (int i = 0; i < (int)num; i++)
		{
			AssetEntry daTYqALDIRJJkrPUKrsyqwNAB = new AssetEntry
			{
				AssetType = (AssetType)dut0a6FCClF9uncpHt4baoWwu.ReadByte(),
				Name = dut0a6FCClF9uncpHt4baoWwu.ReadString()
			};
			switch (daTYqALDIRJJkrPUKrsyqwNAB.AssetType)
			{
			case AssetType.Mesh:
			{
				Mesh mesh = new Mesh();
				Vector3[] array = new Vector3[dut0a6FCClF9uncpHt4baoWwu.ReadInt32()];
				for (int j = 0; j < array.Length; j++)
				{
					array[j] = new Vector3(dut0a6FCClF9uncpHt4baoWwu.ReadSingle(), dut0a6FCClF9uncpHt4baoWwu.ReadSingle(), dut0a6FCClF9uncpHt4baoWwu.ReadSingle());
				}
				int[] array2 = new int[dut0a6FCClF9uncpHt4baoWwu.ReadInt32()];
				for (int k = 0; k < array2.Length; k++)
				{
					array2[k] = dut0a6FCClF9uncpHt4baoWwu.ReadInt32();
				}
				Vector3[] array3 = new Vector3[dut0a6FCClF9uncpHt4baoWwu.ReadInt32()];
				for (int l = 0; l < array3.Length; l++)
				{
					array3[l] = new Vector3(dut0a6FCClF9uncpHt4baoWwu.ReadSingle(), dut0a6FCClF9uncpHt4baoWwu.ReadSingle(), dut0a6FCClF9uncpHt4baoWwu.ReadSingle());
				}
				Vector2[] array4 = new Vector2[dut0a6FCClF9uncpHt4baoWwu.ReadInt32()];
				for (int m = 0; m < array4.Length; m++)
				{
					array4[m] = new Vector2(dut0a6FCClF9uncpHt4baoWwu.ReadSingle(), dut0a6FCClF9uncpHt4baoWwu.ReadSingle());
				}
				mesh.vertices = array;
				mesh.triangles = array2;
				mesh.normals = array3;
				mesh.uv = array4;
				mesh.RecalculateNormals();
				daTYqALDIRJJkrPUKrsyqwNAB.Asset = mesh;
				break;
			}
			case AssetType.Texture:
			{
				Texture2D texture2D = new Texture2D(dut0a6FCClF9uncpHt4baoWwu.ReadInt32(), dut0a6FCClF9uncpHt4baoWwu.ReadInt32());
				texture2D.filterMode = (FilterMode)dut0a6FCClF9uncpHt4baoWwu.ReadByte();
				texture2D.anisoLevel = (int)dut0a6FCClF9uncpHt4baoWwu.ReadByte();
				texture2D.wrapMode = (TextureWrapMode)dut0a6FCClF9uncpHt4baoWwu.ReadByte();
				Color32[] array5 = new Color32[dut0a6FCClF9uncpHt4baoWwu.ReadInt32()];
				for (int n = 0; n < array5.Length; n++)
				{
					array5[n] = dut0a6FCClF9uncpHt4baoWwu.ReadColor32();
				}
				texture2D.SetPixels32(array5);
				texture2D.Apply();
				daTYqALDIRJJkrPUKrsyqwNAB.Asset = texture2D;
				break;
			}
			case AssetType.Audio:
			{
				int num2 = dut0a6FCClF9uncpHt4baoWwu.ReadInt32();
				float[] array6 = new float[dut0a6FCClF9uncpHt4baoWwu.ReadInt32()];
				for (int num3 = 0; num3 < array6.Length; num3++)
				{
					array6[num3] = dut0a6FCClF9uncpHt4baoWwu.ReadSingle();
				}
				AudioClip audioClip = AudioClip.Create("LoadedAudio", array6.Length, 1, num2, false);
				audioClip.SetData(array6, 0);
				daTYqALDIRJJkrPUKrsyqwNAB.Asset = audioClip;
				break;
			}
			}
			GuiStyles.LoadedAssetCache.Add(daTYqALDIRJJkrPUKrsyqwNAB.Name, daTYqALDIRJJkrPUKrsyqwNAB);
		}
	}
	public static byte[] GetPlayerItemState(Player p)
	{
		bool flag = p.equipment.asset is ItemGunAsset;
		byte[] array;
		if (flag)
		{
			Attachments attachments = GuiStyles.GunThirdAttachmentsField.GetValue(p.equipment.useable) as Attachments;
			array = (p.equipment.asset as ItemGunAsset).getState(attachments.sightID, attachments.tacticalID, attachments.gripID, attachments.barrelID, attachments.magazineID, 0);
		}
		else
		{
			array = p.equipment.asset.getState(EItemOrigin.WORLD);
		}
		return array;
	}
	public static Texture2D GetPlayerEquippedItemIcon(Player player)
	{
		ushort num = ((player.equipment.asset != null) ? player.equipment.asset.id : player.equipment.itemID);
		bool flag = num == 0;
		Texture2D texture2D;
		if (flag)
		{
			texture2D = null;
		}
		else
		{
			texture2D = GuiStyles.GetItemIconWithState(num, GuiStyles.GetPlayerItemState(player));
		}
		return texture2D;
	}
	public static Texture2D GetItemIcon(ushort id)
	{
		return GuiStyles.GetItemIconWithState(id, new byte[0]);
	}
	public static Texture2D GetItemIconWithState(ushort id, byte[] state)
	{
		bool flag = !GuiStyles.ItemIconCache.ContainsKey(id);
		Texture2D texture2D;
		if (flag)
		{
			GuiStyles.ItemIconCache.Add(id, new ItemStateTextureCache(id));
			GuiStyles.ItemIconCache[id].StateTextures.Add(state, null);
			ItemTool.getIcon(id, 100, state, delegate(int handle, Texture2D icon)
			{
				bool flag3 = GuiStyles.ItemIconCache.ContainsKey(id) && GuiStyles.ItemIconCache[id].StateTextures.ContainsKey(state);
				if (flag3)
				{
					GuiStyles.ItemIconCache[id].StateTextures[state] = icon;
				}
			});
			texture2D = null;
		}
		else
		{
			bool flag2 = !GuiStyles.ItemIconCache[id].HasState(state);
			if (flag2)
			{
				GuiStyles.ItemIconCache[id].StateTextures.Add(state, null);
				ItemTool.getIcon(id, 100, state, delegate(int handle, Texture2D icon)
				{
					bool flag4 = GuiStyles.ItemIconCache.ContainsKey(id) && GuiStyles.ItemIconCache[id].StateTextures.ContainsKey(state);
					if (flag4)
					{
						GuiStyles.ItemIconCache[id].StateTextures[state] = icon;
					}
				});
				texture2D = null;
			}
			else
			{
				texture2D = GuiStyles.ItemIconCache[id].GetTexture(state);
			}
		}
		return texture2D;
	}
	public static Texture2D GetSteamAvatarIcon(CSteamID playerId)
	{
		bool flag = !GuiStyles.AvatarCache.ContainsKey(playerId.m_SteamID);
		if (flag)
		{
			GuiStyles.AvatarCache.Add(playerId.m_SteamID, Provider.provider.communityService.getIcon(playerId, true));
		}
		return GuiStyles.AvatarCache[playerId.m_SteamID];
	}
	public static void BuildGuiStyles(GUISkin defaultSkin)
	{
		GuiStyles.StylesReady = true;
		GuiStyles.BigBoldLabelStyle = new GUIStyle(GUI.skin.label);
		GuiStyles.BigBoldLabelStyle.alignment = TextAnchor.MiddleCenter;
		GuiStyles.BigBoldLabelStyle.normal.textColor = new Color32(225, 225, 225, byte.MaxValue);
		GuiStyles.BigBoldLabelStyle.fontSize = 30;
		GuiStyles.BigBoldLabelStyle.richText = true;
		Font font = Font.CreateDynamicFontFromOSFont("Trebuchet MS", 30);
		bool flag = font == null;
		if (flag)
		{
			font = Font.CreateDynamicFontFromOSFont("Verdana", 30);
		}
		bool flag2 = font == null;
		if (flag2)
		{
			font = ((defaultSkin != null) ? defaultSkin.font : GUI.skin.font);
		}
		GuiStyles.BigBoldLabelStyle.font = font;
		GuiStyles.BigBoldLabelStyle.fontStyle = FontStyle.Bold;
		FontManager.fontList.Add(GuiStyles.BigBoldLabelStyle.font);
		GuiStyles.BigBoldLabelShadowStyle = new GUIStyle(GuiStyles.BigBoldLabelStyle);
		GuiStyles.BigBoldLabelShadowStyle.normal.textColor = new Color32(20, 20, 20, byte.MaxValue);
		GuiStyles.TinyLabelStyle = new GUIStyle(GUI.skin.label);
		GuiStyles.TinyLabelStyle.alignment = TextAnchor.MiddleCenter;
		GuiStyles.TinyLabelStyle.normal.textColor = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
		GuiStyles.TinyLabelStyle.fontSize = 6;
		GuiStyles.GrayLabelStyle = new GUIStyle(GUI.skin.label);
		GuiStyles.GrayLabelStyle.alignment = TextAnchor.UpperLeft;
		GuiStyles.GrayLabelStyle.normal.textColor = new Color32(100, 100, 100, byte.MaxValue);
		GuiStyles.GrayLabelStyle.fontSize = 20;
		GuiStyles.TransparentPanelStyle = new GUIStyle(GuiStyles.PanelStyleBase);
		GuiStyles.TransparentPanelStyle.normal.background = TextureUtil.CreatePixelTexture(new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, 0));
		GuiStyles.BaseLabelStyle = new GUIStyle(GUI.skin.label);
		GuiStyles.DarkLabelStyle = new GUIStyle(GuiStyles.BaseLabelStyle);
		GuiStyles.DarkLabelStyle.normal.textColor = new Color32(5, 5, 5, byte.MaxValue);
		GuiStyles.CenterLabelStyle = new GUIStyle(GUI.skin.label);
		GuiStyles.CenterLabelStyle.alignment = TextAnchor.MiddleCenter;
		GuiStyles.DarkCenterLabelStyle = new GUIStyle(GuiStyles.CenterLabelStyle);
		GuiStyles.DarkCenterLabelStyle.normal.textColor = new Color32(5, 5, 5, byte.MaxValue);
		EspManager.InitCategoryLabelStyles(GUI.skin.label);
		GuiStyles.SmallBoldGrayLabelStyle = new GUIStyle(GUI.skin.label);
		GuiStyles.SmallBoldGrayLabelStyle.alignment = TextAnchor.MiddleLeft;
		GuiStyles.SmallBoldGrayLabelStyle.fontSize = 11;
		Font font2 = Font.CreateDynamicFontFromOSFont("Arial Bold", 11);
		bool flag3 = font2 == null;
		if (flag3)
		{
			font2 = Font.CreateDynamicFontFromOSFont("Arial", 11);
		}
		bool flag4 = font2 == null;
		if (flag4)
		{
			font2 = ((defaultSkin != null) ? defaultSkin.font : GUI.skin.font);
		}
		GuiStyles.SmallBoldGrayLabelStyle.font = font2;
		GuiStyles.SmallBoldGrayLabelStyle.normal.textColor = new Color32(105, 105, 105, byte.MaxValue);
		GuiStyles.SmallBoldGrayLabelStyle.fontStyle = FontStyle.Bold;
		GuiStyles.SmallBoldGrayLabelStyle.clipping = TextClipping.Overflow;
		GuiStyles.SmallBoldWhiteLabelStyle = new GUIStyle(GUI.skin.label);
		GuiStyles.SmallBoldWhiteLabelStyle.alignment = TextAnchor.UpperLeft;
		GuiStyles.SmallBoldWhiteLabelStyle.fontSize = 11;
		Font font3 = Font.CreateDynamicFontFromOSFont("Arial Bold", 11);
		bool flag5 = font3 == null;
		if (flag5)
		{
			font3 = Font.CreateDynamicFontFromOSFont("Arial", 11);
		}
		bool flag6 = font3 == null;
		if (flag6)
		{
			font3 = ((defaultSkin != null) ? defaultSkin.font : GUI.skin.font);
		}
		GuiStyles.SmallBoldWhiteLabelStyle.font = font3;
		GuiStyles.SmallBoldWhiteLabelStyle.normal.textColor = new Color32(225, 225, 225, byte.MaxValue);
		GuiStyles.SmallBoldWhiteLabelStyle.fontStyle = FontStyle.Bold;
		GuiStyles.SmallBoldWhiteLabelStyle.clipping = TextClipping.Overflow;
		// SmallGrayLabelStyle - for secondary text in NetworkTab
		GuiStyles.SmallGrayLabelStyle = new GUIStyle(GUI.skin.label);
		GuiStyles.SmallGrayLabelStyle.alignment = TextAnchor.MiddleLeft;
		GuiStyles.SmallGrayLabelStyle.fontSize = 10;
		GuiStyles.SmallGrayLabelStyle.normal.textColor = new Color32(140, 140, 140, 255);
		// BoxStyle - for list items
		GuiStyles.BoxStyle = new GUIStyle(GUI.skin.box);
		GuiStyles.BoxStyle.normal.background = TextureUtil.CreatePixelTexture(new Color32(35, 35, 40, 200));
		GuiStyles.BoxStyle.margin = new RectOffset(0, 0, 2, 2);
		GuiStyles.BoxStyle.padding = new RectOffset(5, 5, 3, 3);
		GuiStyles.LogoTexture = new Texture2D(50, 50);
		for (int i = 0; i < 50; i++)
		{
			for (int j = 0; j < 50; j++)
			{
				GuiStyles.LogoTexture.SetPixel(i, j, Color.clear);
			}
		}
		for (int k = 10; k < 40; k++)
		{
			for (int l = 20 - (k - 10) / 2; l < 30 + (k - 10) / 2; l++)
			{
				GuiStyles.LogoTexture.SetPixel(k, l, Color.white);
			}
		}
		GuiStyles.LogoTexture.Apply();
		try
		{
			GuiStyles.BuildCustomSkin(defaultSkin);
		}
		catch
		{
		}
		GuiStyles.CustomToggleStyle = new GUIStyle(GuiStyles.CustomSkin.toggle);
	}
	private static void BuildCustomSkin(GUISkin defaultSkin)
	{
		GuiStyles.CustomSkin = ScriptableObject.CreateInstance<GUISkin>();
		GuiStyles.CustomSkin.font = defaultSkin.font;
		GuiStyles.CustomSkin.box = new GUIStyle(defaultSkin.box);
		GuiStyles.CustomSkin.button = new GUIStyle(defaultSkin.button);
		GuiStyles.CustomSkin.button.normal.background = TextureUtil.CreatePixelTexture(new Color32(62, 62, 62, byte.MaxValue));
		GuiStyles.CustomSkin.button.active.background = TextureUtil.CreatePixelTexture(new Color32(32, 32, 33, byte.MaxValue));
		GuiStyles.CustomSkin.button.focused.background = TextureUtil.CreatePixelTexture(new Color32(32, 32, 33, byte.MaxValue));
		GuiStyles.CustomSkin.button.hover.background = TextureUtil.CreatePixelTexture(new Color32(42, 42, 43, byte.MaxValue));
		GuiStyles.CustomSkin.button.alignment = TextAnchor.MiddleCenter;
		GuiStyles.CustomSkin.button.fixedHeight = 14f;
		GuiStyles.CustomSkin.button.fontSize = 11;
		GuiStyles.CustomSkin.button.stretchWidth = true;
		GuiStyles.CustomSkin.button.stretchHeight = false;
		GuiStyles.CustomSkin.button.contentOffset = new Vector2(0f, 3f);
		GuiStyles.CustomSkin.button.border = new RectOffset(4, 4, 0, -1);
		GuiStyles.CustomSkin.button.margin = new RectOffset(4, 4, 4, 6);
		GuiStyles.CustomSkin.button.padding = new RectOffset(0, 0, 0, 0);
		GuiStyles.CustomSkin.button.overflow = new RectOffset(0, 0, -4, 4);
		GuiStyles.CustomSkin.label = new GUIStyle(defaultSkin.label);
		GuiStyles.CustomSkin.label.fontSize = 11;
		GuiStyles.CustomSkin.label.wordWrap = true;
		GuiStyles.CustomSkin.label.contentOffset = Vector2.zero;
		GuiStyles.CustomSkin.label.border = new RectOffset(0, 0, 0, 0);
		GuiStyles.CustomSkin.label.margin = new RectOffset(4, 4, 4, 4);
		GuiStyles.CustomSkin.label.padding = new RectOffset(0, 0, 0, 0);
		GuiStyles.CustomSkin.label.overflow = new RectOffset(0, 0, 0, 0);
		Texture2D texture2D = TextureUtil.CreateSolidTexture(52, 40, new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, 0));
		TextureUtil.FillRectPixels(new Rect(0f, 37f, 52f, 3f), new Color32(137, 207, 240, byte.MaxValue), ref texture2D);
		GuiStyles.DtextFieldBorderTex = texture2D;
		GuiStyles.CustomSkin.textField = new GUIStyle(defaultSkin.textField);
		GuiStyles.CustomSkin.textField.normal.textColor = Color.white;
		GuiStyles.CustomSkin.textField.normal.background = texture2D;
		GuiStyles.CustomSkin.textField.hover.textColor = Color.white;
		GuiStyles.CustomSkin.textField.hover.background = texture2D;
		GuiStyles.CustomSkin.textField.active.textColor = Color.white;
		GuiStyles.CustomSkin.textField.active.background = texture2D;
		GuiStyles.CustomSkin.textField.focused.textColor = Color.white;
		GuiStyles.CustomSkin.textField.focused.background = texture2D;
		GuiStyles.CustomSkin.textField.onActive.textColor = Color.white;
		GuiStyles.CustomSkin.textField.onActive.background = texture2D;
		GuiStyles.CustomSkin.textField.onFocused.textColor = Color.white;
		GuiStyles.CustomSkin.textField.onFocused.background = texture2D;
		GuiStyles.CustomSkin.textField.onHover.textColor = Color.white;
		GuiStyles.CustomSkin.textField.onHover.background = texture2D;
		GuiStyles.CustomSkin.textField.onNormal.textColor = Color.white;
		GuiStyles.CustomSkin.textField.onNormal.background = texture2D;
		GuiStyles.CustomSkin.textField.alignment = TextAnchor.UpperCenter;
		GuiStyles.CustomSkin.textField.fixedHeight = 25f;
		GuiStyles.CustomSkin.textField.border = new RectOffset(4, 4, 0, 0);
		GuiStyles.CustomSkin.textField.margin = new RectOffset(4, 4, 4, 4);
		GuiStyles.CustomSkin.textField.padding = new RectOffset(3, 3, 3, 3);
		GuiStyles.CustomSkin.textField.overflow = new RectOffset(0, 0, 0, 0);
		GuiStyles.CustomSkin.textField.active.background = texture2D;
		GuiStyles.CustomSkin.toggle = new GUIStyle(defaultSkin.toggle);
		Texture2D texture2D2 = TextureUtil.CreateSolidTexture(15, 15, new Color32(40, 40, 40, byte.MaxValue));
		Texture2D texture2D3 = TextureUtil.CreateSolidTexture(15, 15, new Color32(137, 207, 240, byte.MaxValue));
		GuiStyles.DtoggleActiveTex = texture2D3;
		TextureUtil.FillRectPixels(new Rect(4f, 4f, 7f, 7f), new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue), ref texture2D3);
		GuiStyles.CustomSkin.toggle.normal.background = texture2D2;
		GuiStyles.CustomSkin.toggle.hover.background = null;
		GuiStyles.CustomSkin.toggle.active.background = texture2D3;
		GuiStyles.CustomSkin.toggle.onNormal.background = texture2D3;
		GuiStyles.CustomSkin.toggle.onHover.background = texture2D3;
		GuiStyles.CustomSkin.toggle.onActive.background = texture2D3;
		GuiStyles.CustomSkin.toggle.fixedHeight = 15f;
		GuiStyles.CustomSkin.toggle.fixedWidth = 15f;
		GuiStyles.CustomSkin.toggle.stretchHeight = false;
		GuiStyles.CustomSkin.toggle.stretchWidth = false;
		GuiStyles.CustomSkin.toggle.clipping = TextClipping.Overflow;
		GuiStyles.CustomSkin.toggle.fontSize = 0;
		GuiStyles.CustomSkin.toggle.contentOffset = Vector2.zero;
		GuiStyles.CustomSkin.toggle.alignment = TextAnchor.MiddleLeft;
		GuiStyles.CustomSkin.toggle.border = new RectOffset(15, 0, 0, 0);
		GuiStyles.CustomSkin.toggle.margin = new RectOffset(4, 4, 4, 4);
		GuiStyles.CustomSkin.toggle.padding = new RectOffset(23, 0, 0, 0);
		GuiStyles.CustomSkin.toggle.overflow = new RectOffset(0, 0, 0, 0);
		GuiStyles.CustomSkin.horizontalSlider = new GUIStyle(defaultSkin.horizontalSlider);
		GuiStyles.CustomSkin.horizontalSlider.normal.background = GuiStyles.TransparentTexture;
		GuiStyles.CustomSkin.horizontalSlider.border = new RectOffset(0, 0, 0, 0);
		GuiStyles.CustomSkin.horizontalSlider.margin = new RectOffset(0, 0, 0, 0);
		GuiStyles.CustomSkin.horizontalSlider.padding = new RectOffset(0, 0, 0, 0);
		GuiStyles.CustomSkin.horizontalSlider.overflow = new RectOffset(0, 0, 0, 0);
		Texture2D texture2D4 = TextureUtil.CreateSolidTexture(13, 12, new Color32(137, 207, 240, byte.MaxValue));
		GuiStyles.CustomSkin.horizontalSliderThumb = new GUIStyle(defaultSkin.horizontalSliderThumb);
		GuiStyles.CustomSkin.horizontalSliderThumb.normal.background = GuiStyles.TransparentTexture;
		GuiStyles.CustomSkin.horizontalSliderThumb.active.background = GuiStyles.TransparentTexture;
		GuiStyles.CustomSkin.horizontalSliderThumb.focused.background = GuiStyles.TransparentTexture;
		GuiStyles.CustomSkin.horizontalSliderThumb.hover.background = GuiStyles.TransparentTexture;
		GuiStyles.CustomSkin.horizontalSliderThumb.fixedHeight = 5f;
		GuiStyles.CustomSkin.horizontalSliderThumb.fixedWidth = 1f;
		GuiStyles.CustomSkin.horizontalSliderThumb.border = new RectOffset(0, 0, 0, 0);
		GuiStyles.CustomSkin.horizontalSliderThumb.margin = new RectOffset(0, 0, 0, 0);
		GuiStyles.CustomSkin.horizontalSliderThumb.padding = new RectOffset(0, 0, 0, 0);
		GuiStyles.CustomSkin.horizontalSliderThumb.overflow = new RectOffset(-1, -1, -9, 8);
		GuiStyles.CustomSkin.horizontalSliderThumb.contentOffset = Vector2.zero;
		GuiStyles.CustomSkin.verticalSlider = new GUIStyle(defaultSkin.verticalSlider);
		GuiStyles.CustomSkin.verticalSlider.normal.background = GuiStyles.CustomSkin.horizontalSlider.normal.background;
		GuiStyles.CustomSkin.verticalSliderThumb = new GUIStyle(defaultSkin.verticalSliderThumb);
		GuiStyles.CustomSkin.verticalSliderThumb.normal.background = GuiStyles.CustomSkin.horizontalSliderThumb.normal.background;
		GuiStyles.CustomSkin.horizontalScrollbar = new GUIStyle(defaultSkin.horizontalScrollbar);
		GuiStyles.CustomSkin.horizontalScrollbar.normal.background = GuiStyles.TransparentTexture;
		GuiStyles.CustomSkin.horizontalScrollbar.active.background = GuiStyles.TransparentTexture;
		GuiStyles.CustomSkin.horizontalScrollbar.hover.background = GuiStyles.TransparentTexture;
		GuiStyles.CustomSkin.horizontalScrollbar.focused.background = GuiStyles.TransparentTexture;
		GuiStyles.CustomSkin.horizontalScrollbarThumb = new GUIStyle(defaultSkin.horizontalScrollbarThumb);
		GuiStyles.CustomSkin.horizontalScrollbarThumb.normal.background = GuiStyles.TransparentTexture;
		GuiStyles.CustomSkin.horizontalScrollbarThumb.active.background = GuiStyles.TransparentTexture;
		GuiStyles.CustomSkin.horizontalScrollbarThumb.hover.background = GuiStyles.TransparentTexture;
		GuiStyles.CustomSkin.horizontalScrollbarThumb.focused.background = GuiStyles.TransparentTexture;
		GuiStyles.CustomSkin.horizontalScrollbarLeftButton = new GUIStyle(defaultSkin.horizontalScrollbarLeftButton);
		GuiStyles.CustomSkin.horizontalScrollbarRightButton = new GUIStyle(defaultSkin.horizontalScrollbarRightButton);
		GuiStyles.CustomSkin.verticalScrollbar = new GUIStyle(defaultSkin.verticalScrollbar);
		GuiStyles.CustomSkin.verticalScrollbar.fixedWidth = 6f;
		GuiStyles.CustomSkin.verticalScrollbar.normal.background = TextureUtil.CreateSolidTexture(6, 16, new Color32(30, 30, 30, byte.MaxValue));
		GuiStyles.CustomSkin.verticalScrollbarThumb = new GUIStyle(defaultSkin.verticalScrollbarThumb);
		GuiStyles.CustomSkin.verticalScrollbarThumb.normal.background = TextureUtil.CreateSolidTexture(4, 12, new Color32(137, 207, 240, byte.MaxValue));
		GuiStyles.DscrollbarThumbTex = GuiStyles.CustomSkin.verticalScrollbarThumb.normal.background;
		GuiStyles.CustomSkin.verticalScrollbar.fixedWidth = 4f;
		GuiStyles.CustomSkin.verticalScrollbarDownButton = new GUIStyle(defaultSkin.verticalScrollbarDownButton);
		GuiStyles.CustomSkin.verticalScrollbarUpButton = new GUIStyle(defaultSkin.verticalScrollbarUpButton);
		GuiStyles.CustomSkin.scrollView = new GUIStyle(defaultSkin.scrollView);
		GuiStyles.CustomSkin.window = new GUIStyle(defaultSkin.window);
		GuiStyles.CustomSkin.window.normal.background = GuiStyles.TransparentTexture;
		GuiStyles.CustomSkin.window.hover.background = GuiStyles.TransparentTexture;
		GuiStyles.CustomSkin.window.active.background = GuiStyles.TransparentTexture;
		GuiStyles.CustomSkin.window.focused.background = GuiStyles.TransparentTexture;
		GuiStyles.CustomSkin.window.onNormal.background = GuiStyles.TransparentTexture;
		GuiStyles.CustomSkin.window.onHover.background = GuiStyles.TransparentTexture;
		GuiStyles.CustomSkin.window.onActive.background = GuiStyles.TransparentTexture;
		GuiStyles.CustomSkin.window.onFocused.background = GuiStyles.TransparentTexture;
		GuiStyles.CustomSkin.customStyles = new GUIStyle[1];
		GuiStyles.CustomSkin.customStyles[0] = new GUIStyle(GuiStyles.CustomSkin.box);
		GuiStyles.CustomSkin.customStyles[0].normal.background = TextureUtil.CreateSolidTexture(100, 40, new Color32(52, 52, 52, byte.MaxValue));
		GuiStyles.CustomSkin.customStyles[0].fontSize = 10;
		GuiStyles.CustomSkin.customStyles[0].alignment = TextAnchor.MiddleCenter;
		GuiStyles.CustomSkin.customStyles[0].border = new RectOffset(2, 2, 2, 2);
		GuiStyles.CustomSkin.customStyles[0].margin = new RectOffset(4, 4, 4, 4);
		GuiStyles.CustomSkin.customStyles[0].padding = new RectOffset(0, 0, 0, 0);
		GuiStyles.CustomSkin.customStyles[0].overflow = new RectOffset(0, 0, 0, 0);
		GuiStyles.CustomSkin.name = "CustomSkin";
		UnityEngine.Object.DontDestroyOnLoad(GuiStyles.CustomSkin);
		GuiStyles.TextureRegistry.Add("customStyles[0].normal", GuiStyles.CustomSkin.customStyles[0].normal.background);
		GuiStyles.TextureRegistry.Add("customStyles[0].active", GuiStyles.CustomSkin.customStyles[0].active.background);
		GuiStyles.TextureRegistry.Add("customStyles[0].hover", GuiStyles.CustomSkin.customStyles[0].hover.background);
		GuiStyles.TextureRegistry.Add("toggle.normal", texture2D2);
		GuiStyles.TextureRegistry.Add("activeTexture", texture2D3);
		GuiStyles.TextureRegistry.Add("textFieldTexture", texture2D);
		GuiStyles.TextureRegistry.Add("horizontalSlider.normal", GuiStyles.CustomSkin.horizontalSlider.normal.background);
		GuiStyles.TextureRegistry.Add("horizontalSliderThumb.normal", texture2D4);
		GuiStyles.TextureRegistry.Add("verticalScrollbar.normal", GuiStyles.CustomSkin.verticalScrollbar.normal.background);
		GuiStyles.TextureRegistry.Add("verticalScrollbarThumb.normal", GuiStyles.CustomSkin.verticalScrollbarThumb.normal.background);
		GuiStyles.TextureRegistry.Add("button.normal", GuiStyles.CustomSkin.button.normal.background);
		GuiStyles.TextureRegistry.Add("button.active", GuiStyles.CustomSkin.button.active.background);
		GuiStyles.TextureRegistry.Add("button.focused", GuiStyles.CustomSkin.button.focused.background);
		GuiStyles.TextureRegistry.Add("button.hover", GuiStyles.CustomSkin.button.hover.background);
	}
	private static void CreateCircleMeshObject()
	{
		try
		{
			GuiStyles.CircleGameObject = new GameObject("Circle");
			UnityEngine.Object.DontDestroyOnLoad(GuiStyles.CircleGameObject);
			GuiStyles.CircleGameObject.transform.position = new Vector3(0f, -245f, 0f);
			GuiStyles.CircleGameObject.transform.eulerAngles += new Vector3(0f, 0f, 90f);
			MeshFilter meshFilter = GuiStyles.CircleGameObject.AddComponent<MeshFilter>();
			MeshRenderer meshRenderer = GuiStyles.CircleGameObject.AddComponent<MeshRenderer>();
			GuiStyles.CircleGameObject.layer = 16;
			Mesh mesh = new Mesh();
			meshFilter.mesh = mesh;
			List<Vector3> list = new List<Vector3>();
			List<int> list2 = new List<int>();
			float num = 0.09817477f;
			for (int i = 0; i <= 64; i++)
			{
				float num2 = (float)i * num;
				float num3 = Mathf.Cos(num2);
				float num4 = Mathf.Sin(num2);
				list.Add(new Vector3(num3, num4, 0f));
				bool flag = i > 0;
				if (flag)
				{
					list2.Add(i - 1);
					list2.Add(i);
				}
			}
			list2.Add(64);
			list2.Add(0);
			mesh.vertices = list.ToArray();
			mesh.SetIndices(list2.ToArray(), MeshTopology.Lines, 0);
			Material material = new Material(Shader.Find("Hidden/Internal-Colored"))
			{
				hideFlags = HideFlags.HideAndDontSave
			};
			material.SetInt("_SrcBlend", 5);
			material.SetInt("_DstBlend", 10);
			material.SetInt("_Cull", 0);
			material.SetInt("_ZWrite", 0);
			material.SetInt("_ZTest", 8);
			meshRenderer.sharedMaterial = new Material(material);
		}
		catch (Exception ex)
		{
			Logger.LogClient(ex.Message);
			Logger.LogClient(ex.StackTrace);
		}
	}
	private static void CreateCrescentMeshObject()
	{
		try
		{
			GuiStyles.CrescentGameObject = new GameObject("Crescent");
			UnityEngine.Object.DontDestroyOnLoad(GuiStyles.CrescentGameObject);
			GuiStyles.CrescentGameObject.transform.position = new Vector3(0f, -245f, 0f);
			GuiStyles.CrescentGameObject.transform.eulerAngles += new Vector3(0f, 0f, 90f);
			MeshFilter meshFilter = GuiStyles.CrescentGameObject.AddComponent<MeshFilter>();
			MeshRenderer meshRenderer = GuiStyles.CrescentGameObject.AddComponent<MeshRenderer>();
			GuiStyles.CrescentGameObject.layer = 16;
			Mesh mesh = new Mesh();
			meshFilter.mesh = mesh;
			List<Vector3> verts = new List<Vector3>();
			List<int> lines = new List<int>();
			// ── Crescent (outer circle minus bite circle) ──
			float R = 1f, bx = 0.55f, Rb = 0.8f;
			float ix = (R * R - Rb * Rb + bx * bx) / (2f * bx);
			float iy = Mathf.Sqrt(Mathf.Max(0f, R * R - ix * ix));
			float aLow = -Mathf.Atan2(iy, ix);
			float aHigh = Mathf.Atan2(iy, ix);
			float biteUpper = Mathf.Atan2(iy, ix - bx);
			// Outer arc: aLow -> aHigh through 0 (left side)
			int outerSteps = 40;
			for (int i = 0; i <= outerSteps; i++)
			{
				float t = (float)i / outerSteps;
				float a = Mathf.Lerp(aLow, aHigh, t);
				verts.Add(new Vector3(Mathf.Cos(a) * R, Mathf.Sin(a) * R, 0f));
			}
			// Inner bite arc: biteUpper -> 2π - biteUpper (through π)
			int innerSteps = 40;
			for (int i = 0; i <= innerSteps; i++)
			{
				float t = (float)i / innerSteps;
				float a = Mathf.Lerp(biteUpper, Mathf.PI * 2f - biteUpper, t);
				verts.Add(new Vector3(bx + Mathf.Cos(a) * Rb, Mathf.Sin(a) * Rb, 0f));
			}
			int loopCount = verts.Count;
			for (int i = 0; i < loopCount; i++)
			{
				lines.Add(i);
				lines.Add((i + 1) % loopCount);
			}
			// ── Five-pointed star ──
			float starR = 0.5f, starR2 = 0.2f;
			Vector3 starC = new Vector3(1.35f, 0f, 0f);
			int starStart = verts.Count;
			for (int i = 0; i < 10; i++)
			{
				float a = Mathf.PI / 2f + (float)i * Mathf.PI / 5f;
				float rr = (i % 2 == 0) ? starR : starR2;
				verts.Add(starC + new Vector3(Mathf.Cos(a) * rr, Mathf.Sin(a) * rr, 0f));
			}
			for (int i = 0; i < 10; i++)
			{
				lines.Add(starStart + i);
				lines.Add(starStart + (i + 1) % 10);
			}
			mesh.vertices = verts.ToArray();
			mesh.SetIndices(lines.ToArray(), MeshTopology.Lines, 0);
			Material material = new Material(Shader.Find("Hidden/Internal-Colored"))
			{
				hideFlags = HideFlags.HideAndDontSave
			};
			material.SetInt("_SrcBlend", 5);
			material.SetInt("_DstBlend", 10);
			material.SetInt("_Cull", 0);
			material.SetInt("_ZWrite", 0);
			material.SetInt("_ZTest", 8);
			meshRenderer.sharedMaterial = new Material(material);
		}
		catch (Exception ex)
		{
			Logger.LogClient("crescent mesh: " + ex.Message);
		}
	}
	private static void CreateTracerMeshObject()
	{
		try
		{
			GuiStyles.TracerGameObject = new GameObject("Tracer");
			UnityEngine.Object.DontDestroyOnLoad(GuiStyles.TracerGameObject);
			GuiStyles.TracerGameObject.transform.position = new Vector3(0f, -245f, 0f);
			GuiStyles.TracerGameObject.transform.eulerAngles += new Vector3(0f, 0f, 90f);
			MeshFilter meshFilter = GuiStyles.TracerGameObject.AddComponent<MeshFilter>();
			MeshRenderer meshRenderer = GuiStyles.TracerGameObject.AddComponent<MeshRenderer>();
			GuiStyles.TracerGameObject.layer = 16;
			Mesh mesh = new Mesh();
			meshFilter.mesh = mesh;
			float num = 0.2f;
			float num2 = 0.2f;
			Vector3[] array = new Vector3[]
			{
				new Vector3(-num / 2f, 0f, -num2 / 2f),
				new Vector3(-num / 2f, 0f, num2 / 2f),
				new Vector3(num / 2f, 0f, num2 / 2f),
				new Vector3(num / 2f, 0f, -num2 / 2f)
			};
			int[] array2 = new int[] { 0, 1, 2, 2, 3, 0 };
			Vector2[] array3 = new Vector2[]
			{
				new Vector2(0f, 0f),
				new Vector2(0f, 1f),
				new Vector2(1f, 1f),
				new Vector2(1f, 0f)
			};
			mesh.vertices = array;
			mesh.triangles = array2;
			mesh.uv = array3;
			mesh.RecalculateNormals();
			Material material = new Material(Shader.Find("Hidden/Internal-Colored"))
			{
				hideFlags = HideFlags.HideAndDontSave
			};
			material.SetInt("_SrcBlend", 5);
			material.SetInt("_DstBlend", 10);
			material.SetInt("_Cull", 0);
			material.SetInt("_ZWrite", 0);
			meshRenderer.sharedMaterial = new Material(material);
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(GuiStyles.TracerGameObject);
			gameObject.transform.SetParent(GuiStyles.TracerGameObject.transform);
			gameObject.transform.localEulerAngles = new Vector3(0f, 0f, 180f);
			CombineInstance[] array4 = new CombineInstance[2];
			array4[0] = default(CombineInstance);
			array4[0].mesh = mesh;
			array4[0].transform = meshRenderer.transform.localToWorldMatrix;
			array4[1] = default(CombineInstance);
			array4[1].mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
			array4[1].transform = gameObject.transform.localToWorldMatrix;
			new Mesh().CombineMeshes(array4);
			UnityEngine.Object.Destroy(gameObject);
		}
		catch (Exception ex)
		{
			Logger.LogClient(ex.Message);
			Logger.LogClient(ex.StackTrace);
		}
	}
	public static void DupdateAccentTextures(Color32 accent)
	{
		bool flag = GuiStyles.DtextFieldBorderTex != null;
		if (flag)
		{
			TextureUtil.FillRectPixels(new Rect(0f, 37f, 52f, 3f), accent, ref GuiStyles.DtextFieldBorderTex);
		}
		bool flag2 = GuiStyles.DtoggleActiveTex != null;
		if (flag2)
		{
			TextureUtil.FillRectPixels(new Rect(0f, 0f, 15f, 15f), accent, ref GuiStyles.DtoggleActiveTex);
			TextureUtil.FillRectPixels(new Rect(4f, 4f, 7f, 7f), new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue), ref GuiStyles.DtoggleActiveTex);
		}
		bool flag3 = GuiStyles.DscrollbarThumbTex != null;
		if (flag3)
		{
			Texture2D texture2D = TextureUtil.CreateSolidTexture(4, 12, accent);
			GuiStyles.DscrollbarThumbTex = texture2D;
			GuiStyles.CustomSkin.verticalScrollbarThumb.normal.background = texture2D;
		}
		GuiStyles.DlastAccentColor = accent;
	}
	public static void DcheckAccentUpdate()
	{
		Color32 accentColor = MenuGuiHelper.GetAccentColor(byte.MaxValue);
		bool flag = accentColor.r != GuiStyles.DlastAccentColor.r || accentColor.g != GuiStyles.DlastAccentColor.g || accentColor.b != GuiStyles.DlastAccentColor.b;
		if (flag)
		{
			GuiStyles.DupdateAccentTextures(accentColor);
		}
	}
	public static Dictionary<string, Texture2D> TextureRegistry = new Dictionary<string, Texture2D>();
	public static Dictionary<string, AssetEntry> LoadedAssetCache = new Dictionary<string, AssetEntry>();
	public static StaticResourceRef<Texture2D> CursorTextureRef = new StaticResourceRef<Texture2D>("UI/Glazier_IMGUI/Cursor");
	public static Texture2D WhiteTexture;
	public static Texture2D TransparentTexture;
	public static Texture2D TransparentTexture2;
	public static Texture2D LogoTexture;
	public static bool StylesReady = false;
	public static GUISkin CustomSkin;
	public static GUIStyle TransparentPanelStyle;
	public static GUIStyle PanelStyleBase;
	public static GUIStyle BigBoldLabelStyle;
	public static GUIStyle BigBoldLabelShadowStyle;
	public static GUIStyle TinyLabelStyle;
	public static GUIStyle GrayLabelStyle;
	public static GUIStyle BaseLabelStyle;
	public static GUIStyle DarkLabelStyle;
	public static GUIStyle CenterLabelStyle;
	public static GUIStyle DarkCenterLabelStyle;
	public static GUIStyle SmallBoldGrayLabelStyle;
	public static GUIStyle SmallBoldWhiteLabelStyle;
	public static GUIStyle CustomToggleStyle;
	public static GUIStyle SmallGrayLabelStyle;
	public static GUIStyle BoxStyle;
	public static string[] SoundNames = new string[] { "Fart sound", "Nya", "Roblox death", "Cheat hit", "Uwu", "Random", "Paradox" };
	private const string AssetFileNameConst = "Resources\\unity_builtin_postprocess";
	private static byte[] AssetBundleBytes;
	public static GameObject TracerGameObject;
	public static GameObject CircleGameObject;
	public static GameObject CrescentGameObject;
	private static FieldInfo GunThirdAttachmentsField;
	private static Dictionary<ushort, ItemStateTextureCache> ItemIconCache = new Dictionary<ushort, ItemStateTextureCache>();
	private static Dictionary<ulong, Texture2D> AvatarCache = new Dictionary<ulong, Texture2D>();
	private static Texture2D DtextFieldBorderTex;
	private static Texture2D DtoggleActiveTex;
	private static Texture2D DscrollbarThumbTex;
	private static Color32 DlastAccentColor = new Color32(0, 180, 216, byte.MaxValue);
	private sealed class _D48Ozevr3FDGnIQax68V1HnRg_d__1 : IEnumerator
	{
		private int state;
		private object current;

		public _D48Ozevr3FDGnIQax68V1HnRg_d__1(int state)
		{
			this.state = state;
		}

		public object Current
		{
			get { return current; }
		}

		public bool MoveNext()
		{
			return false;
		}

		public void Reset()
		{
		}
	}
}
