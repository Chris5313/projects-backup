using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
[Serializable]
public class ByteReader
{
	public ByteReader(byte[] data)
	{
		this.Data = data;
		this.Position = 0;
	}
	~ByteReader()
	{
		this.Data = new byte[0];
	}
	public EmptyEnum ReadEnum()
	{
		return (EmptyEnum)this.ReadByte();
	}
	public byte ReadByte()
	{
		this.Position++;
		return this.Data[this.Position - 1];
	}
	public ushort ReadUInt16()
	{
		this.Position += 2;
		return BitConverter.ToUInt16(new byte[]
		{
			this.Data[this.Position - 2],
			this.Data[this.Position - 1]
		}, 0);
	}
	public int ReadInt32()
	{
		this.Position += 4;
		return BitConverter.ToInt32(new byte[]
		{
			this.Data[this.Position - 4],
			this.Data[this.Position - 3],
			this.Data[this.Position - 2],
			this.Data[this.Position - 1]
		}, 0);
	}
	public float ReadSingle()
	{
		this.Position += 4;
		return BitConverter.ToSingle(new byte[]
		{
			this.Data[this.Position - 4],
			this.Data[this.Position - 3],
			this.Data[this.Position - 2],
			this.Data[this.Position - 1]
		}, 0);
	}
	public Color32 ReadColor32()
	{
		return new Color32(this.ReadByte(), this.ReadByte(), this.ReadByte(), this.ReadByte());
	}
	public Color ReadColor()
	{
		return new Color((float)this.ReadByte() / 255f, (float)this.ReadByte() / 255f, (float)this.ReadByte() / 255f, (float)this.ReadByte() / 255f);
	}
	public Rect ReadRect()
	{
		return new Rect(this.ReadSingle(), this.ReadSingle(), this.ReadSingle(), this.ReadSingle());
	}
	public Vector2 ReadVector2()
	{
		return new Vector2(this.ReadSingle(), this.ReadSingle());
	}
	public Vector3 ReadVector3()
	{
		return new Vector3(this.ReadSingle(), this.ReadSingle(), this.ReadSingle());
	}
	public byte[] ReadLengthPrefixedBytes()
	{
		byte[] array = new byte[(int)this.ReadUInt16()];
		Array.Copy(this.Data, this.Position, array, 0, array.Length);
		this.Position += array.Length;
		return array;
	}
	public void ReadBytesToPointer(IntPtr ptr)
	{
		ushort num = this.ReadUInt16();
		Marshal.Copy(this.Data, this.Position, ptr, (int)num);
		this.Position += (int)num;
	}
	public byte[] ReadBytes(int length)
	{
		byte[] array = new byte[length];
		Array.Copy(this.Data, this.Position, array, 0, array.Length);
		this.Position += array.Length;
		return array;
	}
	public uint ReadUInt32()
	{
		this.Position += 4;
		return BitConverter.ToUInt32(new byte[]
		{
			this.Data[this.Position - 4],
			this.Data[this.Position - 3],
			this.Data[this.Position - 2],
			this.Data[this.Position - 1]
		}, 0);
	}
	public ulong ReadUInt64()
	{
		this.Position += 8;
		return BitConverter.ToUInt64(new byte[]
		{
			this.Data[this.Position - 8],
			this.Data[this.Position - 7],
			this.Data[this.Position - 6],
			this.Data[this.Position - 5],
			this.Data[this.Position - 4],
			this.Data[this.Position - 3],
			this.Data[this.Position - 2],
			this.Data[this.Position - 1]
		}, 0);
	}
	public string ReadString()
	{
		return Encoding.UTF8.GetString(this.ReadLengthPrefixedBytes());
	}
	public string ReadFixedString(int length)
	{
		return Encoding.UTF8.GetString(this.ReadBytes(length));
	}
	public bool ReadBool()
	{
		this.Position++;
		return this.Data[this.Position - 1] == 1;
	}
	public byte[] Data;
	public int Position;
}
