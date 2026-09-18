using System;
public class IntOption : OptionBase
{
	// (get) Token: 0x06000466 RID: 1126 RVA: 0x0004A670 File Offset: 0x00048870
	// (set) Token: 0x06000467 RID: 1127 RVA: 0x0004A68D File Offset: 0x0004888D
	public int option
	{
		get
		{
			return (int)this.SortSettings;
		}
		set
		{
			this.SortSettings = value;
		}
	}
	public IntOption(int defaultValue, int minValue, int maxValue, string optionName)
	{
		this.option = defaultValue;
		this.OptionName = optionName;
		this.MinValue = minValue;
		this.MaxValue = maxValue;
	}
	public override void Serialize(PacketWriter writer)
	{
		writer.WriteInt32(this.option);
	}
	public override void Deserialize(ByteReader reader)
	{
		this.option = reader.ReadInt32();
	}
	public override void DisplayOption()
	{
		this.option = MenuGuiHelper.LabeledIntSlider(this.OptionName + ": ", this.option, this.MinValue, this.MaxValue, -1);
	}
	public int MinValue;
	public int MaxValue;
	public string OptionName;
}
