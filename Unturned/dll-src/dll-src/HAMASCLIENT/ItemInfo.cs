using System;
public struct ItemInfo
{
	public ItemInfo(ushort id, string name)
	{
		this.id = id;
		this.name = name;
	}
	public ushort id;
	public string name;
}
