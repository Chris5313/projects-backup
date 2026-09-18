using System;
using UnityEngine;
public class BoolOption : OptionBase
{
	// (get) Token: 0x06000062 RID: 98 RVA: 0x00006110 File Offset: 0x00004310
	// (set) Token: 0x06000063 RID: 99 RVA: 0x0000612D File Offset: 0x0000432D
	public bool option
	{
		get
		{
			return (bool)this.SortSettings;
		}
		set
		{
			this.SortSettings = value;
		}
	}
	public BoolOption(bool option, string optionName)
	{
		this.option = option;
		this.OptionName = optionName;
	}
	public override void Serialize(PacketWriter writer)
	{
		writer.WriteBool(this.option);
	}
	public override void Deserialize(ByteReader reader)
	{
		this.option = reader.ReadBool();
	}
	public override void DisplayOption()
	{
		this.option = MenuGuiHelper.DrawCheckbox(this.option, this.OptionName, Array.Empty<GUILayoutOption>());
	}
	public string OptionName;
}
