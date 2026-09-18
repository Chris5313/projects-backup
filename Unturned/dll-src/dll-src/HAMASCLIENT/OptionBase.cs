using System;
public class OptionBase
{
	public virtual void DisplayOption()
	{
	}
	public virtual void Serialize(PacketWriter writer)
	{
	}
	public virtual void Deserialize(ByteReader reader)
	{
	}
	public object SortSettings;
}
