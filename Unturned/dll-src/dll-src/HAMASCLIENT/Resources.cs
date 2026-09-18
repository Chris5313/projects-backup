using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;
[GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
[DebuggerNonUserCode]
[CompilerGenerated]
internal class Resources
{
	internal Resources()
	{
	}
	// (get) Token: 0x060004B1 RID: 1201 RVA: 0x0004D928 File Offset: 0x0004BB28
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	internal static ResourceManager ResourceManager
	{
		get
		{
			bool flag = Resources.resourceMan == null;
			if (flag)
			{
				Resources.resourceMan = new ResourceManager("UnityEngine.Properties.Resources", typeof(Resources).Assembly);
			}
			return Resources.resourceMan;
		}
	}
	// (get) Token: 0x060004B2 RID: 1202 RVA: 0x0004D96C File Offset: 0x0004BB6C
	// (set) Token: 0x060004B3 RID: 1203 RVA: 0x0004D983 File Offset: 0x0004BB83
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	internal static CultureInfo Culture
	{
		get
		{
			return Resources.resourceCulture;
		}
		set
		{
			Resources.resourceCulture = value;
		}
	}
	// (get) Token: 0x060004B4 RID: 1204 RVA: 0x0004D98C File Offset: 0x0004BB8C
	internal static string BuildDate
	{
		get
		{
			return Resources.ResourceManager.GetString("BuildDate", Resources.resourceCulture);
		}
	}
	private static ResourceManager resourceMan;
	private static CultureInfo resourceCulture;
}
