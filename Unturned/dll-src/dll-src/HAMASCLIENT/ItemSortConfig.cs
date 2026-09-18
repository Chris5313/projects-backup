using System;
using System.Collections.Generic;
public class ItemSortConfig
{
	public bool SortItems;
	public bool IsBlacklist;
	public Dictionary<ushort, ItemInfo> Items;
	public bool UseCategoryFiltering;
	public List<Type> Categories;
}
