using System;
public struct FovCircleSetting
{
	public FovCircleSetting(string fovName, string fovColorName, BoolConditionDelegate condition = null)
	{
		this.FovName = fovName;
		this.FovColorSettingName = fovColorName;
		this.RainbowEnabled = false;
		this.UseFovScale = true;
		this.FovPixels = 200;
		this.FovScaleDegrees = 55;
		this.DrawEnabled = false;
		this.VisibilityCondition = condition;
	}
	public void Serialize(PacketWriter writer)
	{
		writer.WriteByte(FovCircleSetting.SerializationVersion);
		writer.WriteBool(this.DrawEnabled);
		writer.WriteBool(this.RainbowEnabled);
		writer.WriteBool(this.UseFovScale);
		writer.WriteUInt16((ushort)this.FovPixels);
		writer.WriteUInt16((ushort)this.FovScaleDegrees);
	}
	public static FovCircleSetting Deserialize(FovCircleSetting fov, ByteReader reader)
	{
		byte b = reader.ReadByte();
		fov.DrawEnabled = reader.ReadBool();
		fov.RainbowEnabled = reader.ReadBool();
		fov.UseFovScale = reader.ReadBool();
		fov.FovPixels = (int)reader.ReadUInt16();
		fov.FovScaleDegrees = (int)reader.ReadUInt16();
		return fov;
	}
	public static byte SerializationVersion;
	public string FovName;
	public string FovColorSettingName;
	public bool DrawEnabled;
	public bool RainbowEnabled;
	public bool UseFovScale;
	public int FovPixels;
	public int FovScaleDegrees;
	public BoolConditionDelegate VisibilityCondition;
}
