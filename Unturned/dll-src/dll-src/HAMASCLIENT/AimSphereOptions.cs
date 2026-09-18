using System;
public class AimSphereOptions
{
	// (get) Token: 0x060000E3 RID: 227 RVA: 0x0000AAFC File Offset: 0x00008CFC
	// (set) Token: 0x060000E4 RID: 228 RVA: 0x0000AB14 File Offset: 0x00008D14
	[ConfigBindAttribute("Aim options", "Sphere segments")]
	[SaveableNameAttribute]
	public int sphereSegments
	{
		get
		{
			return this.sphereSegmentsField;
		}
		set
		{
			bool flag = value != this.sphereSegmentsField;
			if (flag)
			{
				this.sphereSegmentsField = value;
				try
				{
					SpherePointGenerator.BuildSpherePoints();
					return;
				}
				catch
				{
					return;
				}
			}
			this.sphereSegmentsField = value;
		}
	}
	public AimSphereOptions(float sphereSize, int sphereSegments)
	{
		this.sphereSize = sphereSize;
		this.sphereSegments = sphereSegments;
	}
	// (get) Token: 0x060000E6 RID: 230 RVA: 0x0000AB8C File Offset: 0x00008D8C
	// (set) Token: 0x060000E7 RID: 231 RVA: 0x0000ABA4 File Offset: 0x00008DA4
	[ConfigBindAttribute("Aim options", "Sphere size")]
	[SaveableNameAttribute]
	public float sphereSize
	{
		get
		{
			return this.sphereSizeField;
		}
		set
		{
			bool flag = value != this.sphereSizeField;
			if (flag)
			{
				this.sphereSizeField = value;
				try
				{
					SpherePointGenerator.BuildSpherePoints();
					return;
				}
				catch
				{
					return;
				}
			}
			this.sphereSizeField = value;
		}
	}
	public float sphereSizeField = 1f;
	public int sphereSegmentsField = 8;
}
