using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SDG.Unturned;
using UnityEngine;
public class ConfigManager
{
	public static void EnsureConfigsDirectory()
	{
		bool flag = !Directory.Exists(Application.dataPath + "/configs/");
		if (flag)
		{
			Directory.CreateDirectory(Application.dataPath + "/configs/");
		}
	}
	public static void RefreshConfigList()
	{
		ConfigManager.ConfigFileNames.Clear();
		foreach (FileInfo fileInfo in new DirectoryInfo(Application.dataPath + "/configs/").GetFiles())
		{
			bool flag = fileInfo.Name.EndsWith(".conf");
			if (flag)
			{
				ConfigManager.ConfigFileNames.Add(fileInfo.Name);
			}
		}
		ConfigManager.ConfigNameSelector.Options = (from s in ConfigManager.ConfigFileNames.ToArray()
			select s.Replace(".conf", "")).ToArray<string>();
	}
	public static void InitSaveableFields()
	{
		ConfigManager.EnsureConfigsDirectory();
		ConfigManager.RefreshConfigList();
		foreach (FieldInfo fieldInfo in AttributeRegistry.FieldsByAttribute[typeof(SaveableNameAttribute)])
		{
			string text = ((SaveableNameAttribute)fieldInfo.GetCustomAttribute(typeof(SaveableNameAttribute))).SaveableName;
			bool flag = string.IsNullOrEmpty(text);
			if (flag)
			{
				bool flag2 = fieldInfo.IsDefined(typeof(ConfigBindAttribute));
				if (flag2)
				{
					text = ((ConfigBindAttribute)fieldInfo.GetCustomAttribute(typeof(ConfigBindAttribute))).VariableName;
				}
				else
				{
					text = fieldInfo.Name;
				}
			}
			bool flag3 = !ConfigManager.SaveableFieldsByName.ContainsKey(text);
			if (flag3)
			{
				ConfigManager.SaveableFieldsByName.Add(text, new SaveableField(ConfigManager.TypeToSaveValueType(fieldInfo.FieldType), fieldInfo.DeclaringType, fieldInfo.Name, text, false));
			}
			else
			{
				Logger.LogClient("Saveable with name " + text + " have a several saves atributes");
			}
		}
		foreach (PropertyInfo propertyInfo in AttributeRegistry.PropertiesByAttribute[typeof(SaveableNameAttribute)])
		{
			string text2 = ((SaveableNameAttribute)propertyInfo.GetCustomAttribute(typeof(SaveableNameAttribute))).SaveableName;
			bool flag4 = string.IsNullOrEmpty(text2);
			if (flag4)
			{
				bool flag5 = propertyInfo.IsDefined(typeof(ConfigBindAttribute));
				if (flag5)
				{
					text2 = ((ConfigBindAttribute)propertyInfo.GetCustomAttribute(typeof(ConfigBindAttribute))).VariableName;
				}
				else
				{
					text2 = propertyInfo.Name;
				}
			}
			bool flag6 = !ConfigManager.SaveableFieldsByName.ContainsKey(text2);
			if (flag6)
			{
				ConfigManager.SaveableFieldsByName.Add(text2, new SaveableField(ConfigManager.TypeToSaveValueType(propertyInfo.PropertyType), propertyInfo.DeclaringType, propertyInfo.Name, text2, true));
			}
			else
			{
				Logger.LogClient("Saveable with name " + text2 + " have a several saves atributes");
			}
		}
		bool flag7 = File.Exists(Application.dataPath + "/pseudohwids");
		if (flag7)
		{
			ByteReader dut0a6FCClF9uncpHt4baoWwu = new ByteReader(File.ReadAllBytes(Application.dataPath + "/pseudohwids"));
			MiscConfig.SpoofedHwid1 = dut0a6FCClF9uncpHt4baoWwu.ReadLengthPrefixedBytes();
			MiscConfig.SpoofedHwid2 = dut0a6FCClF9uncpHt4baoWwu.ReadLengthPrefixedBytes();
			MiscConfig.SpoofedHwid3 = dut0a6FCClF9uncpHt4baoWwu.ReadLengthPrefixedBytes();
		}
	}
	public static void LoadConfig(string fileName)
	{
		ConfigManager.EnsureConfigsDirectory();
		try
		{
			ByteReader dut0a6FCClF9uncpHt4baoWwu = new ByteReader(File.ReadAllBytes(Application.dataPath + "/configs/" + fileName));
			byte b = dut0a6FCClF9uncpHt4baoWwu.ReadByte();
			bool flag = b > 190;
			int num;
			if (flag)
			{
				num = (int)b;
				b = 0;
			}
			else
			{
				bool flag2 = b >= 5;
				if (flag2)
				{
					num = (int)dut0a6FCClF9uncpHt4baoWwu.ReadUInt16();
				}
				else
				{
					num = (int)dut0a6FCClF9uncpHt4baoWwu.ReadByte();
				}
			}
			int i = 0;
			while (i < num)
			{
				SaveValueType da7EjeELhPnQjMBiXW309VJV = (SaveValueType)dut0a6FCClF9uncpHt4baoWwu.ReadByte();
				string text = dut0a6FCClF9uncpHt4baoWwu.ReadString();
				object obj = null;
				switch (da7EjeELhPnQjMBiXW309VJV)
				{
				case SaveValueType.Bool:
					obj = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
					break;
				case SaveValueType.String:
					obj = dut0a6FCClF9uncpHt4baoWwu.ReadString();
					break;
				case SaveValueType.Int:
					obj = dut0a6FCClF9uncpHt4baoWwu.ReadInt32();
					break;
				case SaveValueType.Float:
					obj = dut0a6FCClF9uncpHt4baoWwu.ReadSingle();
					break;
				case SaveValueType.Enum:
				{
					bool flag3 = ConfigManager.SaveableFieldsByName.ContainsKey(text);
					if (flag3)
					{
						try
						{
							int num2 = ((b >= 6) ? dut0a6FCClF9uncpHt4baoWwu.ReadInt32() : ((int)dut0a6FCClF9uncpHt4baoWwu.ReadByte()));
							obj = ConfigManager.EnumFromValue(ConfigManager.SaveableFieldsByName[text].GetValueType(), num2);
							break;
						}
						catch
						{
							break;
						}
					}
					dut0a6FCClF9uncpHt4baoWwu.Position += ((b >= 6) ? 4 : 1);
					break;
				}
				case SaveValueType.Byte:
					obj = dut0a6FCClF9uncpHt4baoWwu.ReadByte();
					break;
				case SaveValueType.ByteArray:
					obj = dut0a6FCClF9uncpHt4baoWwu.ReadLengthPrefixedBytes();
					break;
				case SaveValueType.Rect:
					obj = dut0a6FCClF9uncpHt4baoWwu.ReadRect();
					break;
				case SaveValueType.Vector2:
					obj = dut0a6FCClF9uncpHt4baoWwu.ReadVector2();
					break;
				case SaveValueType.Unknown:
					goto IL_01C1;
				case SaveValueType.ULong:
					obj = dut0a6FCClF9uncpHt4baoWwu.ReadUInt64();
					break;
				default:
					goto IL_01C1;
				}
				IL_02F4:
				bool flag4 = ConfigManager.SaveableFieldsByName.ContainsKey(text);
				if (flag4)
				{
					ConfigManager.SaveableFieldsByName[text].SetValue(obj);
				}
				i++;
				continue;
				IL_01C1:
				bool flag5 = ConfigManager.SaveableFieldsByName.ContainsKey(text);
				if (flag5)
				{
					switch (ConfigManager.SaveableFieldsByName[text].ValueType)
					{
					case SaveValueType.Bool:
						obj = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
						break;
					case SaveValueType.String:
						obj = dut0a6FCClF9uncpHt4baoWwu.ReadString();
						break;
					case SaveValueType.Int:
						obj = dut0a6FCClF9uncpHt4baoWwu.ReadInt32();
						break;
					case SaveValueType.Float:
						obj = dut0a6FCClF9uncpHt4baoWwu.ReadSingle();
						break;
					case SaveValueType.Enum:
						try
						{
							int num3 = ((b >= 6) ? dut0a6FCClF9uncpHt4baoWwu.ReadInt32() : ((int)dut0a6FCClF9uncpHt4baoWwu.ReadByte()));
							obj = ConfigManager.EnumFromValue(ConfigManager.SaveableFieldsByName[text].GetValueType(), num3);
						}
						catch
						{
						}
						break;
					case SaveValueType.Byte:
						obj = dut0a6FCClF9uncpHt4baoWwu.ReadByte();
						break;
					case SaveValueType.ByteArray:
						obj = dut0a6FCClF9uncpHt4baoWwu.ReadLengthPrefixedBytes();
						break;
					case SaveValueType.Rect:
						obj = dut0a6FCClF9uncpHt4baoWwu.ReadRect();
						break;
					case SaveValueType.Vector2:
						obj = dut0a6FCClF9uncpHt4baoWwu.ReadVector2();
						break;
					case SaveValueType.ULong:
						obj = dut0a6FCClF9uncpHt4baoWwu.ReadUInt64();
						break;
					}
				}
				goto IL_02F4;
			}
			byte b2 = dut0a6FCClF9uncpHt4baoWwu.ReadByte();
			try
			{
				for (int j = ((b == 0) ? 5 : 0); j < (int)(b2 + ((b == 0) ? 6 : 0)) && j < ColorConfig.Colors.Length; j++)
				{
					ColorConfig.Colors[j].isGradient = dut0a6FCClF9uncpHt4baoWwu.ReadBool();
					ColorConfig.Colors[j].settedColor = dut0a6FCClF9uncpHt4baoWwu.ReadColor();
					bool flagCol = b >= 7;
					if (flagCol)
					{
						ColorConfig.Colors[j].GradientSpeed = dut0a6FCClF9uncpHt4baoWwu.ReadSingle();
					}
				}
			}
			catch
			{
			}
			byte b3 = dut0a6FCClF9uncpHt4baoWwu.ReadByte();
			for (int k = 0; k < (int)b3 && k < EspCategories.categories.Length; k++)
			{
				try
				{
					EspCategories.categories[k].Deserialize(dut0a6FCClF9uncpHt4baoWwu);
				}
				catch
				{
				}
			}
			byte b4 = dut0a6FCClF9uncpHt4baoWwu.ReadByte();
			KeybindManager.activeBindings.Clear();
			KeybindManager.boundVarNames.Clear();
			KeybindsTab.SelectedBind = null;
			KeybindsTab.IsAddingBind = false;
			for (int l = 0; l < (int)b4; l++)
			{
				try
				{
					string text2 = dut0a6FCClF9uncpHt4baoWwu.ReadString();
					KeyCode keyCode = (KeyCode)dut0a6FCClF9uncpHt4baoWwu.ReadUInt16();
					bool flag6 = b >= 3;
					if (flag6)
					{
						KeybindManager.BindKeyWithType(keyCode, (BindInputType)dut0a6FCClF9uncpHt4baoWwu.ReadByte(), text2);
					}
				}
				catch
				{
					break;
				}
			}
			byte b5 = dut0a6FCClF9uncpHt4baoWwu.ReadByte();
			SkinsManager.SelectedSkins.Clear();
			for (int m = 0; m < (int)b5; m++)
			{
				try
				{
					ClothingCategory dnFbkaUD6mnA1BajCnfh8ztdO = (ClothingCategory)dut0a6FCClF9uncpHt4baoWwu.ReadByte();
					SkinsManager.SelectedSkins[dnFbkaUD6mnA1BajCnfh8ztdO] = SkinsManager.FindSkinById(dnFbkaUD6mnA1BajCnfh8ztdO, dut0a6FCClF9uncpHt4baoWwu.ReadInt32());
				}
				catch
				{
				}
			}
			for (int n = 0; n < OverlayDrawer.Hooks.Length; n++)
			{
				OverlayDrawer.Hooks[n].WindowPosition = dut0a6FCClF9uncpHt4baoWwu.ReadVector2();
			}
			AutomationBot.itemsToESP.Clear();
			ushort num4 = dut0a6FCClF9uncpHt4baoWwu.ReadUInt16();
			for (int num5 = 0; num5 < (int)num4; num5++)
			{
				ushort num6 = dut0a6FCClF9uncpHt4baoWwu.ReadUInt16();
				Asset asset = Assets.find(EAssetType.ITEM, num6);
				bool flag7 = asset != null && asset is ItemAsset && (asset as ItemAsset).itemName.ToLower() != "name";
				if (flag7)
				{
					AutomationBot.itemsToESP[num6] = new ItemInfo(num6, asset.name);
				}
			}
			MiscConfig.AutoPickupWhitelist.Clear();
			ushort num7 = dut0a6FCClF9uncpHt4baoWwu.ReadUInt16();
			for (int num8 = 0; num8 < (int)num7; num8++)
			{
				ushort num9 = dut0a6FCClF9uncpHt4baoWwu.ReadUInt16();
				Asset asset2 = Assets.find(EAssetType.ITEM, num9);
				bool flag8 = asset2 != null && asset2 is ItemAsset && (asset2 as ItemAsset).itemName.ToLower() != "name";
				if (flag8)
				{
					MiscConfig.AutoPickupWhitelist[num9] = new ItemInfo(num9, asset2.name);
				}
			}
			byte b6 = dut0a6FCClF9uncpHt4baoWwu.ReadByte();
			for (byte b7 = 0; b7 < b6; b7 += 1)
			{
				bool flag9 = FovCircleRenderer.FovCircles.Length > (int)b7;
				if (flag9)
				{
					FovCircleRenderer.FovCircles[(int)b7] = FovCircleSetting.Deserialize(FovCircleRenderer.FovCircles[(int)b7], dut0a6FCClF9uncpHt4baoWwu);
				}
			}
			bool flag10 = b >= 1;
			if (flag10)
			{
				AimbotConfig.SphereSizes.Clear();
				byte b8 = dut0a6FCClF9uncpHt4baoWwu.ReadByte();
				for (byte b9 = 0; b9 < b8; b9 += 1)
				{
					int num10 = (int)dut0a6FCClF9uncpHt4baoWwu.ReadByte();
					bool flag11 = b >= 4;
					float num11;
					if (flag11)
					{
						num11 = dut0a6FCClF9uncpHt4baoWwu.ReadSingle();
					}
					else
					{
						num11 = (float)dut0a6FCClF9uncpHt4baoWwu.ReadByte();
					}
					AimbotConfig.SphereSizes.Add(new AimSphereOptions(num11, num10));
				}
				SpherePointGenerator.BuildSpherePoints();
			}
			bool flag12 = b >= 2;
			if (flag12)
			{
				OverlayDrawer.LoggerWindowRect = new Rect(dut0a6FCClF9uncpHt4baoWwu.ReadVector2(), new Vector2(OverlayDrawer.LoggerWindowRect.width, OverlayDrawer.LoggerWindowRect.height));
			}
			ConfigManager.CurrentConfigName = fileName.Substring(0, fileName.Length - ".conf".Length);
			ConfigManager.ConfigNameSelector.Selected = ConfigManager.CurrentConfigName;
			Logger.LogUser("[+] Loaded configuration " + ConfigManager.CurrentConfigName);
		}
		catch (Exception ex)
		{
			try
			{
				File.WriteAllText(Application.dataPath + "/configs/load_error.log", ex.ToString());
			}
			catch
			{
			}
			Logger.LogUser("[-] Unable to load configuration " + fileName.Substring(0, fileName.Length - ".conf".Length));
			Debug.Log(ex.Message);
			Debug.Log(ex.StackTrace);
		}
	}
	public static Enum EnumFromValue(Type type, int value)
	{
		return (Enum)Enum.ToObject(type, value);
	}
	public static void SaveConfig(string name)
	{
		try
		{
			ConfigManager.EnsureConfigsDirectory();
			PacketWriter dtqtgBvjhehJ9nKOGQCJPsaGO = new PacketWriter();
			dtqtgBvjhehJ9nKOGQCJPsaGO.WriteByte(7);
			dtqtgBvjhehJ9nKOGQCJPsaGO.WriteUInt16((ushort)ConfigManager.SaveableFieldsByName.Values.Count);
			foreach (SaveableField dc0nl3r18TU0Q6PA1kJbeydPJ in ConfigManager.SaveableFieldsByName.Values)
			{
				dtqtgBvjhehJ9nKOGQCJPsaGO.WriteByte((byte)dc0nl3r18TU0Q6PA1kJbeydPJ.ValueType);
				dtqtgBvjhehJ9nKOGQCJPsaGO.WriteString(dc0nl3r18TU0Q6PA1kJbeydPJ.SaveName);
				switch (dc0nl3r18TU0Q6PA1kJbeydPJ.ValueType)
				{
				case SaveValueType.Bool:
					dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(dc0nl3r18TU0Q6PA1kJbeydPJ.GetValueTyped<bool>());
					break;
				case SaveValueType.String:
					dtqtgBvjhehJ9nKOGQCJPsaGO.WriteString(dc0nl3r18TU0Q6PA1kJbeydPJ.GetValueTyped<string>());
					break;
				case SaveValueType.Int:
					dtqtgBvjhehJ9nKOGQCJPsaGO.WriteInt32(dc0nl3r18TU0Q6PA1kJbeydPJ.GetValueTyped<int>());
					break;
				case SaveValueType.Float:
					dtqtgBvjhehJ9nKOGQCJPsaGO.WriteSingle(dc0nl3r18TU0Q6PA1kJbeydPJ.GetValueTyped<float>());
					break;
				case SaveValueType.Enum:
					dtqtgBvjhehJ9nKOGQCJPsaGO.WriteInt32(Convert.ToInt32(dc0nl3r18TU0Q6PA1kJbeydPJ.GetValueObject()));
					break;
				case SaveValueType.Byte:
					dtqtgBvjhehJ9nKOGQCJPsaGO.WriteByte(dc0nl3r18TU0Q6PA1kJbeydPJ.GetValueTyped<byte>());
					break;
				case SaveValueType.ByteArray:
					dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBytes(dc0nl3r18TU0Q6PA1kJbeydPJ.GetValueTyped<byte[]>());
					break;
				case SaveValueType.Rect:
					dtqtgBvjhehJ9nKOGQCJPsaGO.WriteRect(dc0nl3r18TU0Q6PA1kJbeydPJ.GetValueTyped<Rect>());
					break;
				case SaveValueType.Vector2:
					dtqtgBvjhehJ9nKOGQCJPsaGO.WriteVector2(dc0nl3r18TU0Q6PA1kJbeydPJ.GetValueTyped<Vector2>());
					break;
				case SaveValueType.Unknown:
					goto IL_0159;
				case SaveValueType.ULong:
					dtqtgBvjhehJ9nKOGQCJPsaGO.WriteUInt64(dc0nl3r18TU0Q6PA1kJbeydPJ.GetValueTyped<ulong>());
					break;
				default:
					goto IL_0159;
				}
				continue;
				IL_0159:
				Logger.LogClient("Couldn't write unexcepted value");
			}			dtqtgBvjhehJ9nKOGQCJPsaGO.WriteByte((byte)ColorConfig.Colors.Length);
			for (int i = 0; i < ColorConfig.Colors.Length; i++)
			{
				dtqtgBvjhehJ9nKOGQCJPsaGO.WriteBool(ColorConfig.Colors[i].isGradient);
				dtqtgBvjhehJ9nKOGQCJPsaGO.WriteColor32(ColorConfig.Colors[i].settedColor);
				dtqtgBvjhehJ9nKOGQCJPsaGO.WriteSingle(ColorConfig.Colors[i].GradientSpeed);
			}
			dtqtgBvjhehJ9nKOGQCJPsaGO.WriteByte((byte)EspCategories.categories.Length);
			for (int j = 0; j < EspCategories.categories.Length; j++)
			{
				EspCategories.categories[j].Serialize(dtqtgBvjhehJ9nKOGQCJPsaGO);
			}
			dtqtgBvjhehJ9nKOGQCJPsaGO.WriteByte((byte)KeybindManager.activeBindings.Count);
			foreach (Keybind dr3ZjnRxP0iAQVvAkuCp9m5Mb in KeybindManager.activeBindings)
			{
				dtqtgBvjhehJ9nKOGQCJPsaGO.WriteString(dr3ZjnRxP0iAQVvAkuCp9m5Mb.Name);
				dtqtgBvjhehJ9nKOGQCJPsaGO.WriteUInt16((ushort)dr3ZjnRxP0iAQVvAkuCp9m5Mb.Key);
				dtqtgBvjhehJ9nKOGQCJPsaGO.WriteByte((byte)dr3ZjnRxP0iAQVvAkuCp9m5Mb.InputType);
			}
			dtqtgBvjhehJ9nKOGQCJPsaGO.WriteByte((byte)SkinsManager.SelectedSkins.Count);
			foreach (ClothingCategory dnFbkaUD6mnA1BajCnfh8ztdO in SkinsManager.SelectedSkins.Keys)
			{
				dtqtgBvjhehJ9nKOGQCJPsaGO.WriteByte((byte)dnFbkaUD6mnA1BajCnfh8ztdO);
				dtqtgBvjhehJ9nKOGQCJPsaGO.WriteInt32(SkinsManager.SelectedSkins[dnFbkaUD6mnA1BajCnfh8ztdO].skinID);
			}
			for (int k = 0; k < OverlayDrawer.Hooks.Length; k++)
			{
				dtqtgBvjhehJ9nKOGQCJPsaGO.WriteVector2(OverlayDrawer.Hooks[k].WindowPosition);
			}
			dtqtgBvjhehJ9nKOGQCJPsaGO.WriteIntAsUInt16(AutomationBot.itemsToESP.Count);
			foreach (ItemInfo ddb8pIlWKKbHkw2jCuyAPcvL in AutomationBot.itemsToESP.Values)
			{
				dtqtgBvjhehJ9nKOGQCJPsaGO.WriteUInt16(ddb8pIlWKKbHkw2jCuyAPcvL.id);
			}
			dtqtgBvjhehJ9nKOGQCJPsaGO.WriteIntAsUInt16(MiscConfig.AutoPickupWhitelist.Count);
			foreach (ItemInfo ddb8pIlWKKbHkw2jCuyAPcvL2 in MiscConfig.AutoPickupWhitelist.Values)
			{
				dtqtgBvjhehJ9nKOGQCJPsaGO.WriteUInt16(ddb8pIlWKKbHkw2jCuyAPcvL2.id);
			}
			dtqtgBvjhehJ9nKOGQCJPsaGO.WriteByte((byte)FovCircleRenderer.FovCircles.Length);
			foreach (FovCircleSetting dxMhfufThyuW1UdZ5MxaTXe5X in FovCircleRenderer.FovCircles)
			{
				dxMhfufThyuW1UdZ5MxaTXe5X.Serialize(dtqtgBvjhehJ9nKOGQCJPsaGO);
			}
			dtqtgBvjhehJ9nKOGQCJPsaGO.WriteByte((byte)AimbotConfig.SphereSizes.Count);
			byte b = 0;
			while ((int)b < AimbotConfig.SphereSizes.Count)
			{
				dtqtgBvjhehJ9nKOGQCJPsaGO.WriteByte((byte)AimbotConfig.SphereSizes[(int)b].sphereSegmentsField);
				dtqtgBvjhehJ9nKOGQCJPsaGO.WriteSingle(AimbotConfig.SphereSizes[(int)b].sphereSizeField);
				b += 1;
			}
			dtqtgBvjhehJ9nKOGQCJPsaGO.WriteVector2(new Vector2(OverlayDrawer.LoggerWindowRect.x, OverlayDrawer.LoggerWindowRect.y));
			File.WriteAllBytes(Application.dataPath + "/configs/" + name + ".conf", dtqtgBvjhehJ9nKOGQCJPsaGO.Buffer.ToArray());
		}
		catch (Exception ex)
		{
			try
			{
				File.WriteAllText(Application.dataPath + "/configs/save_error.log", ex.ToString());
			}
			catch
			{
			}
		}
	}
	/// <summary>
	/// Delete a local config file
	/// </summary>
	public static bool DeleteConfig(string fileName)
	{
		try
		{
			string path = Application.dataPath + "/configs/" + fileName;
			if (!path.EndsWith(".conf")) path += ".conf";
			if (File.Exists(path))
			{
				File.Delete(path);
				RefreshConfigList();
				Logger.LogUser("[+] Deleted config: " + fileName);
				return true;
			}
			return false;
		}
		catch (Exception ex)
		{
			Logger.LogUser("[-] Failed to delete config: " + ex.Message);
			return false;
		}
	}
	
	/// <summary>
	/// Rename a local config file
	/// </summary>
	public static bool RenameConfig(string oldName, string newName)
	{
		try
		{
			string oldPath = Application.dataPath + "/configs/" + oldName;
			if (!oldPath.EndsWith(".conf")) oldPath += ".conf";
			string newPath = Application.dataPath + "/configs/" + newName;
			if (!newPath.EndsWith(".conf")) newPath += ".conf";
			
			if (!File.Exists(oldPath)) return false;
			if (File.Exists(newPath)) 
			{
				Logger.LogUser("[-] Config already exists: " + newName);
				return false;
			}
			
			File.Move(oldPath, newPath);
			RefreshConfigList();
			Logger.LogUser("[+] Renamed config: " + oldName + " -> " + newName);
			return true;
		}
		catch (Exception ex)
		{
			Logger.LogUser("[-] Failed to rename config: " + ex.Message);
			return false;
		}
	}
	
	/// <summary>
	/// Export config to a standalone file
	/// </summary>
	public static bool ExportConfig(string configName, string exportPath)
	{
		try
		{
			string srcPath = Application.dataPath + "/configs/" + configName;
			if (!srcPath.EndsWith(".conf")) srcPath += ".conf";
			if (!File.Exists(srcPath)) return false;
			
			File.Copy(srcPath, exportPath, true);
			Logger.LogUser("[+] Exported config to: " + exportPath);
			return true;
		}
		catch (Exception ex)
		{
			Logger.LogUser("[-] Export failed: " + ex.Message);
			return false;
		}
	}
	
	/// <summary>
	/// Import config from external file
	/// </summary>
	public static bool ImportConfig(string importPath, string configName = null)
	{
		try
		{
			if (!File.Exists(importPath)) return false;
			
			string name = configName ?? Path.GetFileNameWithoutExtension(importPath);
			string destPath = Application.dataPath + "/configs/" + name + ".conf";
			
			EnsureConfigsDirectory();
			File.Copy(importPath, destPath, true);
			RefreshConfigList();
			Logger.LogUser("[+] Imported config: " + name);
			return true;
		}
		catch (Exception ex)
		{
			Logger.LogUser("[-] Import failed: " + ex.Message);
			return false;
		}
	}
	
	/// <summary>
	/// Get config file info
	/// </summary>
	public static (long size, DateTime modified) GetConfigInfo(string fileName)
	{
		try
		{
			string path = Application.dataPath + "/configs/" + fileName;
			if (!path.EndsWith(".conf")) path += ".conf";
			if (File.Exists(path))
			{
				var info = new FileInfo(path);
				return (info.Length, info.LastWriteTime);
			}
		}
		catch { }
		return (0, DateTime.MinValue);
	}
	
	public static SaveValueType TypeToSaveValueType(Type t)
	{
		SaveValueType da7EjeELhPnQjMBiXW309VJV;
		bool flag = ConfigManager.TypeSaveValueTypeMap.TryGetValue(t, out da7EjeELhPnQjMBiXW309VJV);
		SaveValueType da7EjeELhPnQjMBiXW309VJV2;
		if (flag)
		{
			da7EjeELhPnQjMBiXW309VJV2 = da7EjeELhPnQjMBiXW309VJV;
		}
		else
		{
			bool isEnum = t.IsEnum;
			if (isEnum)
			{
				da7EjeELhPnQjMBiXW309VJV2 = SaveValueType.Enum;
			}
			else
			{
				Logger.LogClient(string.Format("returned {0}", SaveValueType.Unknown));
				da7EjeELhPnQjMBiXW309VJV2 = SaveValueType.Unknown;
			}
		}
		return da7EjeELhPnQjMBiXW309VJV2;
	}
	public const byte CurrentConfigVersion = 7;
	public static Dictionary<Type, SaveValueType> TypeSaveValueTypeMap = new Dictionary<Type, SaveValueType>
	{
		{
			typeof(int),
			SaveValueType.Int
		},
		{
			typeof(float),
			SaveValueType.Float
		},
		{
			typeof(bool),
			SaveValueType.Bool
		},
		{
			typeof(string),
			SaveValueType.String
		},
		{
			typeof(byte),
			SaveValueType.Byte
		},
		{
			typeof(byte[]),
			SaveValueType.ByteArray
		},
		{
			typeof(Rect),
			SaveValueType.Rect
		},
		{
			typeof(Vector2),
			SaveValueType.Vector2
		},
		{
			typeof(ulong),
			SaveValueType.ULong
		}
	};
	public static Dictionary<string, SaveableField> SaveableFieldsByName = new Dictionary<string, SaveableField>();
	public static List<string> ConfigFileNames = new List<string>();
	public static DropdownState ConfigNameSelector = new DropdownState(new string[0], "config name here");
	public const string ConfigFileExtension = ".conf";
	public static string CurrentConfigName = "config save name here";
}
