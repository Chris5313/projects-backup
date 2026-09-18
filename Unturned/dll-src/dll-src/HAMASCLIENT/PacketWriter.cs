using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
[Serializable]
public class PacketWriter
{
	public PacketWriter(EmptyEnum packetType)
	{
		this.Buffer = new List<byte>();
		this.WritePacketType(packetType);
	}
	public PacketWriter()
	{
		this.Buffer = new List<byte>();
	}
	public void WritePacketType(EmptyEnum packetType)
	{
		this.WriteByte((byte)packetType);
	}
	public void WriteByte(byte b)
	{
		this.Buffer.Add(b);
	}
	public void WriteUInt16(ushort s)
	{
		this.Buffer.AddRange(BitConverter.GetBytes(s));
	}
	public void WriteUInt64(ulong i)
	{
		this.Buffer.AddRange(BitConverter.GetBytes(i));
	}
	public void WriteIntAsUInt16(int i)
	{
		this.Buffer.AddRange(BitConverter.GetBytes((ushort)i));
	}
	public void WriteInt32(int i)
	{
		this.Buffer.AddRange(BitConverter.GetBytes(i));
	}
	public void WriteSingle(float f)
	{
		this.Buffer.AddRange(BitConverter.GetBytes(f));
	}
	public void WriteRect(Rect rect)
	{
		this.WriteSingle(rect.x);
		this.WriteSingle(rect.y);
		this.WriteSingle(rect.width);
		this.WriteSingle(rect.height);
	}
	public void WriteVector2(Vector2 vector)
	{
		this.WriteSingle(vector.x);
		this.WriteSingle(vector.y);
	}
	public void WriteVector3(Vector3 vector)
	{
		this.WriteSingle(vector.x);
		this.WriteSingle(vector.y);
		this.WriteSingle(vector.z);
	}
	public void WriteColor(Color color)
	{
		this.WriteByte((byte)(color.r * 255f));
		this.WriteByte((byte)(color.g * 255f));
		this.WriteByte((byte)(color.b * 255f));
		this.WriteByte((byte)(color.a * 255f));
	}
	public void WriteColor32(Color32 color)
	{
		this.WriteByte(color.r);
		this.WriteByte(color.g);
		this.WriteByte(color.b);
		this.WriteByte(color.a);
	}
	public void WriteBytes(byte[] bytes)
	{
		this.WriteUInt16((ushort)bytes.Length);
		this.Buffer.AddRange(bytes);
	}
	public void WriteBool(bool val)
	{
		this.Buffer.Add((byte)(val ? 1 : 0));
	}
	public void WriteString(string val)
	{
		this.WriteBytes(Encoding.UTF8.GetBytes(val));
	}
	public void WriteUInt32(uint i)
	{
		this.Buffer.AddRange(BitConverter.GetBytes(i));
	}
	public List<byte> Buffer;
}
