using System;
public struct AssetEntry
{
	public AssetEntry(AssetType assetType, string name, object asset)
	{
		this.AssetType = assetType;
		this.Name = name;
		this.Asset = asset;
	}
	public AssetType AssetType;
	public string Name;
	public object Asset;
}
