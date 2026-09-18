using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using UnityEngine;
public class DFBXLoader
{
	private DFBXLoader(byte[] data)
	{
		this.data = data;
		this.pos = 0;
	}
	public static DFBXLoader.FBXMesh LoadFromEmbeddedResource(string resourceName)
	{
		Assembly executingAssembly = Assembly.GetExecutingAssembly();
		DFBXLoader.FBXMesh fbxmesh;
		using (Stream manifestResourceStream = executingAssembly.GetManifestResourceStream(resourceName))
		{
			bool flag = manifestResourceStream == null;
			if (flag)
			{
				Debug.LogWarning("[DFBXLoader] Resource not found: " + resourceName);
				fbxmesh = null;
			}
			else
			{
				byte[] array = new byte[manifestResourceStream.Length];
				int num = manifestResourceStream.Read(array, 0, array.Length);
				bool flag2 = num != array.Length;
				if (flag2)
				{
					Debug.LogWarning("[DFBXLoader] Failed to read full resource stream");
					fbxmesh = null;
				}
				else
				{
					fbxmesh = DFBXLoader.LoadFromBytes(array);
				}
			}
		}
		return fbxmesh;
	}
	public static DFBXLoader.FBXMesh LoadFromBytes(byte[] data)
	{
		DFBXLoader.FBXMesh fbxmesh;
		try
		{
			DFBXLoader dfbxloader = new DFBXLoader(data);
			fbxmesh = dfbxloader.Parse();
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[DFBXLoader] Parse failed: " + ex.Message + "\n" + ex.StackTrace);
			fbxmesh = null;
		}
		return fbxmesh;
	}
	private DFBXLoader.FBXMesh Parse()
	{
		bool flag = this.data.Length < 27;
		if (flag)
		{
			throw new Exception("File too small to be FBX");
		}
		string @string = Encoding.ASCII.GetString(this.data, 0, 21);
		bool flag2 = @string != "Kaydara FBX Binary  \0";
		if (flag2)
		{
			throw new Exception("Not a binary FBX file (missing Kaydara header)");
		}
		int num = this.ReadInt32(23);
		this.use64bit = num >= 7500;
		this.offsetSize = (this.use64bit ? 8 : 4);
		this.pos = 27;
		Debug.Log("[DFBXLoader] FBX version: " + num.ToString() + ", 64-bit offsets: " + this.use64bit.ToString());
		DFBXLoader.FBXNode fbxnode = new DFBXLoader.FBXNode
		{
			name = "Root"
		};
		while (this.pos < this.data.Length)
		{
			DFBXLoader.FBXNode fbxnode2 = this.ReadNode();
			bool flag3 = fbxnode2 == null;
			if (flag3)
			{
				break;
			}
			fbxnode.children.Add(fbxnode2);
		}
		DFBXLoader.FBXNode fbxnode3 = DFBXLoader.FindChild(fbxnode, "Objects");
		bool flag4 = fbxnode3 == null;
		if (flag4)
		{
			throw new Exception("No Objects node found in FBX");
		}
		DFBXLoader.FBXNode fbxnode4 = DFBXLoader.FindChild(fbxnode3, "Geometry");
		bool flag5 = fbxnode4 == null;
		if (flag5)
		{
			throw new Exception("No Geometry node found in FBX");
		}
		return this.ExtractMesh(fbxnode4);
	}
	private DFBXLoader.FBXMesh ExtractMesh(DFBXLoader.FBXNode geometry)
	{
		DFBXLoader.FBXNode fbxnode = DFBXLoader.FindChild(geometry, "Vertices");
		bool flag = fbxnode == null || fbxnode.properties.Count == 0;
		if (flag)
		{
			throw new Exception("No Vertices node in Geometry");
		}
		double[] array = (double[])fbxnode.properties[0];
		int num = array.Length / 3;
		DFBXLoader.FBXNode fbxnode2 = DFBXLoader.FindChild(geometry, "PolygonVertexIndex");
		bool flag2 = fbxnode2 == null || fbxnode2.properties.Count == 0;
		if (flag2)
		{
			throw new Exception("No PolygonVertexIndex node in Geometry");
		}
		int[] array2 = (int[])fbxnode2.properties[0];
		DFBXLoader.FBXNode fbxnode3 = DFBXLoader.FindChild(geometry, "LayerElementNormal");
		double[] array3 = null;
		int[] array4 = null;
		bool flag3 = false;
		bool flag4 = true;
		bool flag5 = fbxnode3 != null;
		if (flag5)
		{
			DFBXLoader.FBXNode fbxnode4 = DFBXLoader.FindChild(fbxnode3, "Normals");
			bool flag6 = fbxnode4 != null && fbxnode4.properties.Count > 0;
			if (flag6)
			{
				array3 = (double[])fbxnode4.properties[0];
			}
			DFBXLoader.FBXNode fbxnode5 = DFBXLoader.FindChild(fbxnode3, "NormalsIndex");
			bool flag7 = fbxnode5 != null && fbxnode5.properties.Count > 0;
			if (flag7)
			{
				array4 = (int[])fbxnode5.properties[0];
			}
			DFBXLoader.FBXNode fbxnode6 = DFBXLoader.FindChild(fbxnode3, "MappingInformationType");
			bool flag8 = fbxnode6 != null && fbxnode6.properties.Count > 0;
			if (flag8)
			{
				string text = (string)fbxnode6.properties[0];
				flag3 = text == "ByVertex" || text == "ByVertice";
			}
			DFBXLoader.FBXNode fbxnode7 = DFBXLoader.FindChild(fbxnode3, "ReferenceInformationType");
			bool flag9 = fbxnode7 != null && fbxnode7.properties.Count > 0;
			if (flag9)
			{
				flag4 = (string)fbxnode7.properties[0] == "Direct";
			}
		}
		DFBXLoader.FBXNode fbxnode8 = DFBXLoader.FindChild(geometry, "LayerElementUV");
		double[] array5 = null;
		int[] array6 = null;
		bool flag10 = false;
		bool flag11 = false;
		bool flag12 = fbxnode8 != null;
		if (flag12)
		{
			DFBXLoader.FBXNode fbxnode9 = DFBXLoader.FindChild(fbxnode8, "UV");
			bool flag13 = fbxnode9 != null && fbxnode9.properties.Count > 0;
			if (flag13)
			{
				array5 = (double[])fbxnode9.properties[0];
			}
			DFBXLoader.FBXNode fbxnode10 = DFBXLoader.FindChild(fbxnode8, "UVIndex");
			bool flag14 = fbxnode10 != null && fbxnode10.properties.Count > 0;
			if (flag14)
			{
				array6 = (int[])fbxnode10.properties[0];
			}
			DFBXLoader.FBXNode fbxnode11 = DFBXLoader.FindChild(fbxnode8, "MappingInformationType");
			bool flag15 = fbxnode11 != null && fbxnode11.properties.Count > 0;
			if (flag15)
			{
				string text2 = (string)fbxnode11.properties[0];
				flag10 = text2 == "ByVertex" || text2 == "ByVertice";
			}
			DFBXLoader.FBXNode fbxnode12 = DFBXLoader.FindChild(fbxnode8, "ReferenceInformationType");
			bool flag16 = fbxnode12 != null && fbxnode12.properties.Count > 0;
			if (flag16)
			{
				flag11 = (string)fbxnode12.properties[0] == "Direct";
			}
		}
		List<int> list = new List<int>();
		int num2 = 0;
		for (int i = 0; i < array2.Length; i++)
		{
			bool flag17 = array2[i] < 0;
			if (flag17)
			{
				int num3 = i - num2 + 1;
				bool flag18 = num3 >= 3;
				if (flag18)
				{
					for (int j = 1; j < num3 - 1; j++)
					{
						list.Add(num2);
						list.Add(num2 + j);
						list.Add(num2 + j + 1);
					}
				}
				num2 = i + 1;
			}
		}
		int count = list.Count;
		Vector3[] array7 = new Vector3[count];
		int[] array8 = new int[count];
		Vector3[] array9 = new Vector3[count];
		Vector2[] array10 = new Vector2[count];
		for (int k = 0; k < count; k++)
		{
			int num4 = list[k];
			int num5 = array2[num4];
			bool flag19 = num5 < 0;
			if (flag19)
			{
				num5 = ~num5;
			}
			float num6 = (float)array[num5 * 3];
			float num7 = (float)array[num5 * 3 + 1];
			float num8 = (float)array[num5 * 3 + 2];
			array7[k] = new Vector3(num6, num8, -num7);
			bool flag20 = array3 != null;
			if (flag20)
			{
				int num9 = (flag3 ? num5 : num4);
				bool flag21 = !flag4 && array4 != null && num9 < array4.Length;
				if (flag21)
				{
					num9 = array4[num9];
				}
				bool flag22 = num9 >= 0 && num9 * 3 + 2 < array3.Length;
				if (flag22)
				{
					float num10 = (float)array3[num9 * 3];
					float num11 = (float)array3[num9 * 3 + 1];
					float num12 = (float)array3[num9 * 3 + 2];
					array9[k] = new Vector3(num10, num12, -num11).normalized;
				}
			}
			bool flag23 = array5 != null;
			if (flag23)
			{
				int num13 = (flag10 ? num5 : num4);
				bool flag24 = !flag11 && array6 != null && num13 < array6.Length;
				if (flag24)
				{
					num13 = array6[num13];
				}
				bool flag25 = num13 >= 0 && num13 * 2 + 1 < array5.Length;
				if (flag25)
				{
					array10[k] = new Vector2((float)array5[num13 * 2], (float)array5[num13 * 2 + 1]);
				}
			}
			array8[k] = k;
		}
		bool flag26 = array3 == null;
		if (flag26)
		{
			for (int l = 0; l < count; l += 3)
			{
				Vector3 vector = array7[l];
				Vector3 vector2 = array7[l + 1];
				Vector3 vector3 = array7[l + 2];
				Vector3 normalized = Vector3.Cross(vector2 - vector, vector3 - vector).normalized;
				array9[l] = normalized;
				array9[l + 1] = normalized;
				array9[l + 2] = normalized;
			}
		}
		Vector3 vector4 = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
		Vector3 vector5 = new Vector3(float.MinValue, float.MinValue, float.MinValue);
		for (int m = 0; m < array7.Length; m++)
		{
			vector4 = Vector3.Min(vector4, array7[m]);
			vector5 = Vector3.Max(vector5, array7[m]);
		}
		int num14 = count / 3;
		DFBXLoader.FBXMesh fbxmesh = new DFBXLoader.FBXMesh
		{
			vertices = array7,
			triangles = array8,
			normals = array9,
			uv = array10,
			boundsMin = vector4,
			boundsMax = vector5,
			boundsSize = vector5 - vector4
		};
		Debug.Log(string.Concat(new string[]
		{
			"[DFBXLoader] Mesh extracted: ",
			num.ToString(),
			" unique verts, ",
			num14.ToString(),
			" triangles, ",
			count.ToString(),
			" face-vertices",
			(array3 != null) ? ", normals" : "",
			(array5 != null) ? ", UVs" : ""
		}));
		Debug.Log(string.Concat(new string[]
		{
			"[DFBXLoader] Bounds: ",
			vector4.ToString("F3"),
			" to ",
			vector5.ToString("F3"),
			" size=",
			fbxmesh.boundsSize.ToString("F3")
		}));
		return fbxmesh;
	}
	private DFBXLoader.FBXNode ReadNode()
	{
		bool flag = this.pos + this.offsetSize * 3 >= this.data.Length;
		DFBXLoader.FBXNode fbxnode;
		if (flag)
		{
			fbxnode = null;
		}
		else
		{
			long num = this.ReadOffset();
			long num2 = this.ReadOffset();
			long num3 = this.ReadOffset();
			bool flag2 = this.pos >= this.data.Length;
			if (flag2)
			{
				throw new Exception("Unexpected end of FBX data while reading node header");
			}
			byte[] array = this.data;
			int num4 = this.pos;
			this.pos = num4 + 1;
			int num5 = array[num4];
			bool flag3 = num == 0L && num2 == 0L && num3 == 0L && num5 == 0;
			if (flag3)
			{
				fbxnode = null;
			}
			else
			{
				bool flag4 = num > (long)this.data.Length;
				if (flag4)
				{
					throw new Exception("FBX node endOffset " + num.ToString() + " exceeds data length " + this.data.Length.ToString());
				}
				string text = ((num5 > 0) ? Encoding.ASCII.GetString(this.data, this.pos, num5) : "");
				this.pos += num5;
				DFBXLoader.FBXNode fbxnode2 = new DFBXLoader.FBXNode
				{
					name = text
				};
				for (long num6 = 0L; num6 < num2; num6 += 1L)
				{
					fbxnode2.properties.Add(this.ReadProperty());
				}
				while ((long)this.pos < num)
				{
					DFBXLoader.FBXNode fbxnode3 = this.ReadNode();
					bool flag5 = fbxnode3 == null;
					if (flag5)
					{
						break;
					}
					fbxnode2.children.Add(fbxnode3);
				}
				bool flag6 = (long)this.pos < num;
				if (flag6)
				{
					this.pos = (int)num;
				}
				fbxnode = fbxnode2;
			}
		}
		return fbxnode;
	}
	private object ReadProperty()
	{
		byte[] array = this.data;
		int num = this.pos;
		this.pos = num + 1;
		char c = (char)array[num];
		char c2 = c;
		char c3 = c2;
		if (c3 <= 'S')
		{
			if (c3 <= 'L')
			{
				switch (c3)
				{
				case 'C':
				{
					bool flag = this.data[this.pos] > 0;
					this.pos++;
					return flag;
				}
				case 'D':
				{
					double num2 = BitConverter.ToDouble(this.data, this.pos);
					this.pos += 8;
					return num2;
				}
				case 'E':
				case 'G':
				case 'H':
					break;
				case 'F':
				{
					float num3 = BitConverter.ToSingle(this.data, this.pos);
					this.pos += 4;
					return num3;
				}
				case 'I':
				{
					int num4 = BitConverter.ToInt32(this.data, this.pos);
					this.pos += 4;
					return num4;
				}
				default:
					if (c3 == 'L')
					{
						long num5 = BitConverter.ToInt64(this.data, this.pos);
						this.pos += 8;
						return num5;
					}
					break;
				}
			}
			else if (c3 == 'R' || c3 == 'S')
			{
				int num6 = BitConverter.ToInt32(this.data, this.pos);
				this.pos += 4;
				byte[] array2 = new byte[num6];
				Buffer.BlockCopy(this.data, this.pos, array2, 0, num6);
				this.pos += num6;
				bool flag2 = c == 'S';
				if (flag2)
				{
					return Encoding.UTF8.GetString(array2);
				}
				return array2;
			}
		}
		else if (c3 <= 'f')
		{
			if (c3 == 'Y')
			{
				short num7 = BitConverter.ToInt16(this.data, this.pos);
				this.pos += 2;
				return (int)num7;
			}
			switch (c3)
			{
			case 'b':
				return this.ReadBoolArray();
			case 'd':
				return this.ReadDoubleArray();
			case 'f':
				return this.ReadFloatArray();
			}
		}
		else
		{
			if (c3 == 'i')
			{
				return this.ReadIntArray();
			}
			if (c3 == 'l')
			{
				return this.ReadLongArray();
			}
		}
		throw new Exception("Unknown FBX property type '" + c.ToString() + "' at position " + (this.pos - 1).ToString());
	}
	private double[] ReadDoubleArray()
	{
		int num = BitConverter.ToInt32(this.data, this.pos);
		this.pos += 4;
		int num2 = BitConverter.ToInt32(this.data, this.pos);
		this.pos += 4;
		int num3 = BitConverter.ToInt32(this.data, this.pos);
		this.pos += 4;
		bool flag = num2 == 1;
		byte[] array;
		if (flag)
		{
			using (MemoryStream memoryStream = new MemoryStream(this.data, this.pos, num3))
			{
				memoryStream.ReadByte();
				memoryStream.ReadByte();
				using (DeflateStream deflateStream = new DeflateStream(memoryStream, CompressionMode.Decompress))
				{
					using (MemoryStream memoryStream2 = new MemoryStream(num * 8))
					{
						deflateStream.CopyTo(memoryStream2);
						array = memoryStream2.ToArray();
					}
				}
			}
		}
		else
		{
			array = new byte[num3];
			Buffer.BlockCopy(this.data, this.pos, array, 0, num3);
		}
		this.pos += num3;
		double[] array2 = new double[num];
		Buffer.BlockCopy(array, 0, array2, 0, Math.Min(array.Length, num * 8));
		return array2;
	}
	private float[] ReadFloatArray()
	{
		int num = BitConverter.ToInt32(this.data, this.pos);
		this.pos += 4;
		int num2 = BitConverter.ToInt32(this.data, this.pos);
		this.pos += 4;
		int num3 = BitConverter.ToInt32(this.data, this.pos);
		this.pos += 4;
		bool flag = num2 == 1;
		byte[] array;
		if (flag)
		{
			using (MemoryStream memoryStream = new MemoryStream(this.data, this.pos, num3))
			{
				memoryStream.ReadByte();
				memoryStream.ReadByte();
				using (DeflateStream deflateStream = new DeflateStream(memoryStream, CompressionMode.Decompress))
				{
					using (MemoryStream memoryStream2 = new MemoryStream(num * 4))
					{
						deflateStream.CopyTo(memoryStream2);
						array = memoryStream2.ToArray();
					}
				}
			}
		}
		else
		{
			array = new byte[num3];
			Buffer.BlockCopy(this.data, this.pos, array, 0, num3);
		}
		this.pos += num3;
		float[] array2 = new float[num];
		Buffer.BlockCopy(array, 0, array2, 0, Math.Min(array.Length, num * 4));
		return array2;
	}
	private int[] ReadIntArray()
	{
		int num = BitConverter.ToInt32(this.data, this.pos);
		this.pos += 4;
		int num2 = BitConverter.ToInt32(this.data, this.pos);
		this.pos += 4;
		int num3 = BitConverter.ToInt32(this.data, this.pos);
		this.pos += 4;
		bool flag = num2 == 1;
		byte[] array;
		if (flag)
		{
			using (MemoryStream memoryStream = new MemoryStream(this.data, this.pos, num3))
			{
				memoryStream.ReadByte();
				memoryStream.ReadByte();
				using (DeflateStream deflateStream = new DeflateStream(memoryStream, CompressionMode.Decompress))
				{
					using (MemoryStream memoryStream2 = new MemoryStream(num * 4))
					{
						deflateStream.CopyTo(memoryStream2);
						array = memoryStream2.ToArray();
					}
				}
			}
		}
		else
		{
			array = new byte[num3];
			Buffer.BlockCopy(this.data, this.pos, array, 0, num3);
		}
		this.pos += num3;
		int[] array2 = new int[num];
		Buffer.BlockCopy(array, 0, array2, 0, Math.Min(array.Length, num * 4));
		return array2;
	}
	private long[] ReadLongArray()
	{
		int num = BitConverter.ToInt32(this.data, this.pos);
		this.pos += 4;
		int num2 = BitConverter.ToInt32(this.data, this.pos);
		this.pos += 4;
		int num3 = BitConverter.ToInt32(this.data, this.pos);
		this.pos += 4;
		bool flag = num2 == 1;
		byte[] array;
		if (flag)
		{
			using (MemoryStream memoryStream = new MemoryStream(this.data, this.pos, num3))
			{
				memoryStream.ReadByte();
				memoryStream.ReadByte();
				using (DeflateStream deflateStream = new DeflateStream(memoryStream, CompressionMode.Decompress))
				{
					using (MemoryStream memoryStream2 = new MemoryStream(num * 8))
					{
						deflateStream.CopyTo(memoryStream2);
						array = memoryStream2.ToArray();
					}
				}
			}
		}
		else
		{
			array = new byte[num3];
			Buffer.BlockCopy(this.data, this.pos, array, 0, num3);
		}
		this.pos += num3;
		long[] array2 = new long[num];
		Buffer.BlockCopy(array, 0, array2, 0, Math.Min(array.Length, num * 8));
		return array2;
	}
	private bool[] ReadBoolArray()
	{
		int num = BitConverter.ToInt32(this.data, this.pos);
		this.pos += 4;
		int num2 = BitConverter.ToInt32(this.data, this.pos);
		this.pos += 4;
		int num3 = BitConverter.ToInt32(this.data, this.pos);
		this.pos += 4;
		bool flag = num2 == 1;
		byte[] array;
		if (flag)
		{
			using (MemoryStream memoryStream = new MemoryStream(this.data, this.pos, num3))
			{
				memoryStream.ReadByte();
				memoryStream.ReadByte();
				using (DeflateStream deflateStream = new DeflateStream(memoryStream, CompressionMode.Decompress))
				{
					using (MemoryStream memoryStream2 = new MemoryStream(num))
					{
						deflateStream.CopyTo(memoryStream2);
						array = memoryStream2.ToArray();
					}
				}
			}
		}
		else
		{
			array = new byte[num3];
			Buffer.BlockCopy(this.data, this.pos, array, 0, num3);
		}
		this.pos += num3;
		bool[] array2 = new bool[num];
		int num4 = 0;
		while (num4 < num && num4 < array.Length)
		{
			array2[num4] = array[num4] > 0;
			num4++;
		}
		return array2;
	}
	private long ReadOffset()
	{
		bool flag = this.use64bit;
		long num2;
		if (flag)
		{
			long num = BitConverter.ToInt64(this.data, this.pos);
			this.pos += 8;
			num2 = num;
		}
		else
		{
			int num3 = BitConverter.ToInt32(this.data, this.pos);
			this.pos += 4;
			num2 = (long)num3;
		}
		return num2;
	}
	private int ReadInt32(int offset)
	{
		return BitConverter.ToInt32(this.data, offset);
	}
	private static DFBXLoader.FBXNode FindChild(DFBXLoader.FBXNode parent, string name)
	{
		for (int i = 0; i < parent.children.Count; i++)
		{
			bool flag = parent.children[i].name == name;
			if (flag)
			{
				return parent.children[i];
			}
		}
		return null;
	}
	private byte[] data;
	private int pos;
	private bool use64bit;
	private int offsetSize;
	public class FBXMesh
	{
		public Vector3[] vertices;
		public int[] triangles;
		public Vector3[] normals;
		public Vector2[] uv;
		public Vector3 boundsMin;
		public Vector3 boundsMax;
		public Vector3 boundsSize;
	}
	private class FBXNode
	{
		public string name;
		public List<object> properties = new List<object>();
		public List<DFBXLoader.FBXNode> children = new List<DFBXLoader.FBXNode>();
	}
}
