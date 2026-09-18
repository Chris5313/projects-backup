using System;
using System.Collections.Generic;
using UnityEngine;
public class EspCategory
{
	// (get) Token: 0x0600044D RID: 1101 RVA: 0x0004805C File Offset: 0x0004625C
	// (set) Token: 0x0600044E RID: 1102 RVA: 0x00048074 File Offset: 0x00046274
	public bool enabled
	{
		get
		{
			return this.EnabledBackingField;
		}
		set
		{
			bool flag = this.EnabledBackingField != value;
			bool flag2 = flag;
			if (flag2)
			{
				bool flag3 = !value;
				bool flag4 = flag3;
				if (flag4)
				{
					EspCategories.activeCategories.Remove(this);
					for (int i = 0; i < this.LineComponents.Count; i++)
					{
						bool flag5 = this.LineComponents[i] == null;
						bool flag6 = !flag5;
						if (flag6)
						{
							try
							{
								this.LineComponents[i].RestoreOriginalMaterials();
								UnityEngine.Object.Destroy(this.LineComponents[i]);
							}
							catch
							{
							}
						}
					}
					this.LineComponents.Clear();
				}
				else
				{
					EspCategories.activeCategories.Add(this);
				}
			}
			this.EnabledBackingField = value;
		}
	}
	public EspCategory(string categoryName, string formattedText, EspCategoryAction refreshAction, EspCategoryAction drawAction, ValueTuple<Color32, string>[] lineLists, params OptionBase[] options)
	{
		this.FormattedText = formattedText;
		this.CategoryName = categoryName;
		this.LineEntries = new List<EspTextEntry>
		{
			new EspTextEntry(formattedText)
		};
		this.RefreshAction = refreshAction;
		this.DrawAction = drawAction;
		this.Options = options;
		this.LineColors = new ColorSetting[lineLists.Length];
		for (int i = 0; i < lineLists.Length; i++)
		{
			this.LineColors[i] = new ColorSetting(lineLists[i].Item1, lineLists[i].Item2, false);
		}
		this.DrawMaterial = new Material(Shader.Find("Hidden/Internal-Colored"))
		{
			hideFlags = HideFlags.HideAndDontSave,
			color = Color.white
		};
		this.DrawMaterial.SetInt("_SrcBlend", 5);
		this.DrawMaterial.SetInt("_DstBlend", 10);
		this.DrawMaterial.SetInt("_Cull", 0);
		this.DrawMaterial.SetInt("_ZWrite", 0);
		this.DrawMaterial.SetInt("_ZTest", 0);
	}
	public void Serialize(PacketWriter writer)
	{
		writer.WriteByte(9);
		writer.WriteBool(this.EnabledBackingField);
		writer.WriteBool(this.SettingFlag1);
		writer.WriteBool(this.SettingFlag5);
		writer.WriteBool(this.SettingFlag8);
		writer.WriteBool(this.SettingFlag2);
		writer.WriteBool(this.SettingFlag3);
		writer.WriteBool(this.SettingFlag4);
		writer.WriteBool(this.SettingFlag6);
		writer.WriteBool(this.SettingFlag7);
		writer.WriteBool(this.SettingFlag9);
		writer.WriteBool(this.SettingFlag10);
		writer.WriteBool(this.SettingFlag14);
		writer.WriteBool(this.SettingFlag15);
		writer.WriteVector2(this.AnchorOffset);
		writer.WriteBool(this.SettingFlag12);
		writer.WriteInt32(this.MaxLineCount);
		writer.WriteBool(this.SettingFlag13);
		writer.WriteBool(this.SettingFlag11);
		writer.WriteByte((byte)this.LineEntries.Count);
		foreach (EspTextEntry drtz0MPdBhZh1V5PmbtgwG0pX in this.LineEntries)
		{
			writer.WriteBool(drtz0MPdBhZh1V5PmbtgwG0pX.drawEnabled);
			writer.WriteBool(drtz0MPdBhZh1V5PmbtgwG0pX.showDistanceTag);
			writer.WriteString(drtz0MPdBhZh1V5PmbtgwG0pX.formatText);
			writer.WriteBool(drtz0MPdBhZh1V5PmbtgwG0pX.formatNewlines);
			writer.WriteVector2(drtz0MPdBhZh1V5PmbtgwG0pX.screenOffset);
			writer.WriteVector3(drtz0MPdBhZh1V5PmbtgwG0pX.worldOffset);
			writer.WriteByte((byte)drtz0MPdBhZh1V5PmbtgwG0pX.outlineMode._enum);
			writer.WriteBool(drtz0MPdBhZh1V5PmbtgwG0pX.useGlobalColor);
			writer.WriteColor32(drtz0MPdBhZh1V5PmbtgwG0pX.textColor.Color);
			writer.WriteBool(drtz0MPdBhZh1V5PmbtgwG0pX.textColor.IsGradientBacking);
			writer.WriteInt32(drtz0MPdBhZh1V5PmbtgwG0pX.fontSize);
			writer.WriteBool(drtz0MPdBhZh1V5PmbtgwG0pX.scaleByDistance);
			writer.WriteInt32(drtz0MPdBhZh1V5PmbtgwG0pX.minScaleDistance);
			writer.WriteInt32(drtz0MPdBhZh1V5PmbtgwG0pX.maxScaleDistance);
			writer.WriteInt32(drtz0MPdBhZh1V5PmbtgwG0pX.minFontSize);
			writer.WriteInt32(drtz0MPdBhZh1V5PmbtgwG0pX.maxFontSize);
			writer.WriteInt32(drtz0MPdBhZh1V5PmbtgwG0pX.outlineThickness);
		}
		writer.WriteString(this.FontName);
		writer.WriteByte((byte)this.Options.Length);
		writer.WriteByte((byte)this.LineColors.Length);
		byte b = 0;
		while ((int)b < this.LineColors.Length)
		{
			writer.WriteColor32(this.LineColors[(int)b].Color);
			writer.WriteBool(this.LineColors[(int)b].IsGradientBacking);
			writer.WriteSingle(this.LineColors[(int)b].GradientSpeed);
			b += 1;
		}
		for (int i = 0; i < this.Options.Length; i++)
		{
			this.Options[i].Serialize(writer);
		}
	}
	public void Deserialize(ByteReader reader)
	{
		byte b = reader.ReadByte();
		this.enabled = reader.ReadBool();
		this.SettingFlag1 = reader.ReadBool();
		this.SettingFlag5 = reader.ReadBool();
		this.SettingFlag8 = reader.ReadBool();
		bool flag = b >= 3;
		bool flag2 = flag;
		if (flag2)
		{
			this.SettingFlag2 = reader.ReadBool();
			this.SettingFlag3 = reader.ReadBool();
			this.SettingFlag4 = reader.ReadBool();
			this.SettingFlag6 = reader.ReadBool();
			this.SettingFlag7 = reader.ReadBool();
			this.SettingFlag9 = reader.ReadBool();
			this.SettingFlag10 = reader.ReadBool();
		}
		this.SettingFlag14 = reader.ReadBool();
		bool flag3 = b >= 3;
		bool flag4 = flag3;
		if (flag4)
		{
			this.SettingFlag15 = reader.ReadBool();
		}
		this.AnchorOffset = reader.ReadVector2();
		this.SettingFlag12 = reader.ReadBool();
		this.MaxLineCount = reader.ReadInt32();
		this.SettingFlag13 = reader.ReadBool();
		bool flag5 = b >= 8;
		bool flag6 = flag5;
		if (flag6)
		{
			this.SettingFlag11 = reader.ReadBool();
		}
		bool flag7 = b >= 3;
		bool flag8 = flag7;
		if (flag8)
		{
			this.LineEntries.Clear();
			byte b2 = reader.ReadByte();
			for (byte b3 = 0; b3 < b2; b3 += 1)
			{
				this.LineEntries.Add(new EspTextEntry(""));
				this.LineEntries[(int)b3].drawEnabled = reader.ReadBool();
				this.LineEntries[(int)b3].showDistanceTag = reader.ReadBool();
				this.LineEntries[(int)b3].formatText = reader.ReadString();
				this.LineEntries[(int)b3].formatNewlines = reader.ReadBool();
				this.LineEntries[(int)b3].screenOffset = reader.ReadVector2();
				this.LineEntries[(int)b3].worldOffset = reader.ReadVector3();
				this.LineEntries[(int)b3].outlineMode = (TextOutlineStyle)reader.ReadByte();
				bool flag9 = b >= 4;
				bool flag10 = flag9;
				if (flag10)
				{
					this.LineEntries[(int)b3].useGlobalColor = reader.ReadBool();
					this.LineEntries[(int)b3].textColor.settedColor = reader.ReadColor32();
					this.LineEntries[(int)b3].textColor.isGradient = reader.ReadBool();
				}
				this.LineEntries[(int)b3].fontSize = reader.ReadInt32();
				bool flag11 = b >= 5;
				bool flag12 = flag11;
				if (flag12)
				{
					this.LineEntries[(int)b3].scaleByDistance = reader.ReadBool();
					this.LineEntries[(int)b3].minScaleDistance = reader.ReadInt32();
					this.LineEntries[(int)b3].maxScaleDistance = reader.ReadInt32();
					this.LineEntries[(int)b3].minFontSize = reader.ReadInt32();
					this.LineEntries[(int)b3].maxFontSize = reader.ReadInt32();
				}
				bool flag13 = b >= 7;
				bool flag14 = flag13;
				if (flag14)
				{
					this.LineEntries[(int)b3].outlineThickness = reader.ReadInt32();
				}
			}
		}
		else
		{
			this.LineEntries = new List<EspTextEntry>
			{
				new EspTextEntry("")
			};
			this.LineEntries[0].showDistanceTag = reader.ReadBool();
			this.LineEntries[0].formatText = reader.ReadString();
			this.LineEntries[0].formatNewlines = reader.ReadBool();
			this.LineEntries[0].screenOffset = reader.ReadVector2();
			this.LineEntries[0].worldOffset = reader.ReadVector3();
			this.LineEntries[0].outlineMode = (TextOutlineStyle)reader.ReadByte();
			this.LineEntries[0].fontSize = reader.ReadInt32();
		}
		bool flag15 = b >= 6;
		bool flag16 = flag15;
		if (flag16)
		{
			string text = reader.ReadString();
			this.FontName = text;
			bool flag17 = !string.IsNullOrEmpty(text);
			bool flag18 = flag17;
			if (flag18)
			{
				bool flagFont = FontManager.fontsByName.ContainsKey(text);
				if (flagFont)
				{
					this.TextStyle.font = FontManager.fontsByName[text];
					this.TextShadowStyle.font = FontManager.fontsByName[text];
				}
				else
				{
					this.TextStyle.font = GuiStyles.CenterLabelStyle.font;
					this.TextShadowStyle.font = GuiStyles.DarkCenterLabelStyle.font;
				}
			}
			else
			{
				this.TextStyle.font = GuiStyles.CenterLabelStyle.font;
				this.TextShadowStyle.font = GuiStyles.DarkCenterLabelStyle.font;
			}
		}
		byte b4 = reader.ReadByte();
		bool flag19 = b >= 2;
		bool flag20 = flag19;
		if (flag20)
		{
			byte b5 = reader.ReadByte();
			byte b6 = 0;
			while ((int)b6 < Mathf.Min((int)b5, this.LineColors.Length))
			{
				this.LineColors[(int)b6].settedColor = reader.ReadColor32();
				bool flag21 = b >= 9;
				if (flag21)
				{
					try
					{
						this.LineColors[(int)b6].isGradient = reader.ReadBool();
						this.LineColors[(int)b6].GradientSpeed = reader.ReadSingle();
					}
					catch
					{
					}
				}
				b6 += 1;
			}
		}
		for (int i = 0; i < (int)b4; i++)
		{
			this.Options[i].Deserialize(reader);
		}
	}
	private const byte SerializationVersion = 9;
	public bool EnabledBackingField = false;
	public string CategoryName;
	public bool SettingFlag1 = false;
	public bool SettingFlag2 = false;
	public bool SettingFlag3 = false;
	public bool SettingFlag4 = false;
	public bool SettingFlag5 = true;
	public bool SettingFlag6 = false;
	public bool SettingFlag7 = false;
	public bool SettingFlag8 = false;
	public bool SettingFlag9 = false;
	public bool SettingFlag10 = false;
	public bool SettingFlag11 = false;
	public bool WireframeEnabled = false;
	public bool SettingFlag12 = true;
	public int MaxLineCount = 360;
	public bool SettingFlag13 = true;
	public List<EspTextEntry> LineEntries;
	public Vector2 ScrollPosition;
	public bool SettingFlag14 = true;
	public bool SettingFlag15 = false;
	public Vector2 AnchorOffset = new Vector2(0.5f, 1f);
	public ColorSetting[] LineColors;
	public Vector2 PositionOffset;
	public Material DrawMaterial;
	public List<ChamWireframeComponent> LineComponents = new List<ChamWireframeComponent>();
	public OptionBase[] Options;
	public EspCategoryAction RefreshAction;
	public EspCategoryAction DrawAction;
	public GUIStyle TextStyle;
	public GUIStyle TextShadowStyle;
	public string FormattedText;
	public string FontName = "";
}
