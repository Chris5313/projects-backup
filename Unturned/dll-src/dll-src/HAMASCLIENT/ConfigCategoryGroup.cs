using System;
using System.Collections.Generic;
public class ConfigCategoryGroup
{
	public ConfigCategoryGroup(List<ValueTuple<DrawEntry, List<List<GuiSection>>>> categories)
	{
		this.Categories = categories;
	}
	public DrawEntry SelectedCategory;
	public int SelectedIndex = 0;
	public List<ValueTuple<DrawEntry, List<List<GuiSection>>>> Categories;
}
