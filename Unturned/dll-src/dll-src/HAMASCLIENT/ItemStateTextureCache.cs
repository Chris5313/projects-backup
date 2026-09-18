using System;
using System.Collections.Generic;
using UnityEngine;
public class ItemStateTextureCache
{
	public ItemStateTextureCache(ushort itemId)
	{
		this.ItemId = itemId;
		this.StateTextures = new Dictionary<byte[], Texture2D>();
	}
	public bool HasState(byte[] state)
	{
		foreach (byte[] array in this.StateTextures.Keys)
		{
			bool flag = true;
			bool flag2 = state.Length != array.Length;
			bool flag3 = !flag2;
			if (flag3)
			{
				for (int i = 0; i < array.Length; i++)
				{
					bool flag4 = state[i] != array[i];
					bool flag5 = flag4;
					if (flag5)
					{
						flag = false;
						break;
					}
				}
				bool flag6 = flag;
				bool flag7 = flag6;
				if (flag7)
				{
					return true;
				}
			}
		}
		return false;
	}
	public Texture2D GetTexture(byte[] state)
	{
		foreach (byte[] array in this.StateTextures.Keys)
		{
			bool flag = true;
			bool flag2 = state.Length != array.Length;
			bool flag3 = !flag2;
			if (flag3)
			{
				for (int i = 0; i < array.Length; i++)
				{
					bool flag4 = state[i] != array[i];
					bool flag5 = flag4;
					if (flag5)
					{
						flag = false;
						break;
					}
				}
				bool flag6 = flag;
				bool flag7 = flag6;
				if (flag7)
				{
					return this.StateTextures[array];
				}
			}
		}
		return null;
	}
	public ushort ItemId;
	public Dictionary<byte[], Texture2D> StateTextures;
}
