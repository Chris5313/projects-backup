using System;
using UnityEngine;
public class StringConfigOption : OptionBase
{
	// (get) Token: 0x06000107 RID: 263 RVA: 0x0000BE58 File Offset: 0x0000A058
	// (set) Token: 0x06000108 RID: 264 RVA: 0x0000BE75 File Offset: 0x0000A075
	public string option
	{
		get
		{
			return (string)this.SortSettings;
		}
		set
		{
			this.SortSettings = value;
		}
	}
	public StringConfigOption(string option, string optionName)
	{
		this.option = option;
		this.DisplayName = optionName;
	}
	public override void Serialize(PacketWriter writer)
	{
		writer.WriteString(this.option);
	}
	public override void Deserialize(ByteReader reader)
	{
		this.option = reader.ReadString();
	}
	public override void DisplayOption()
	{
		GUILayout.Label(this.DisplayName + ": ", Array.Empty<GUILayoutOption>());
		this.option = GUILayout.TextField(this.option, Array.Empty<GUILayoutOption>());
	}
	public string DisplayName;
}
