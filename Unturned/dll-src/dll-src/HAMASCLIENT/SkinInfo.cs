using System;
using SDG.Provider;
public struct SkinInfo
{
	// (get) Token: 0x06000046 RID: 70 RVA: 0x00004AA8 File Offset: 0x00002CA8
	// (set) Token: 0x06000047 RID: 71 RVA: 0x00004AD5 File Offset: 0x00002CD5
	public int skinID
	{
		get
		{
			bool dbjv74arVJtUMAqsSN0cWr9w = ScreenshotManager.IsSpying;
			bool flag = dbjv74arVJtUMAqsSN0cWr9w;
			int num;
			if (flag)
			{
				num = 0;
			}
			else
			{
				num = this.ItemDefId;
			}
			return num;
		}
		set
		{
			this.ItemDefId = value;
		}
	}
	public SkinInfo(UnturnedEconInfo info, ClothingCategory st)
	{
		this.ItemName = info.name;
		this.ItemDefId = info.itemdefid;
		this.ItemType = st;
	}
	public ClothingCategory ItemType;
	public string ItemName;
	public int ItemDefId;
}
