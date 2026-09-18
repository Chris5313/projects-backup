using System;
using System.Collections.Generic;
using System.Reflection;
public class AttributeRegistry
{
	public static void Initialize()
	{
		AttributeRegistry.PropertiesByAttribute.Add(typeof(ConfigBindAttribute), new List<PropertyInfo>());
		AttributeRegistry.PropertiesByAttribute.Add(typeof(SaveableNameAttribute), new List<PropertyInfo>());
		AttributeRegistry.FieldsByAttribute.Add(typeof(ConfigBindAttribute), new List<FieldInfo>());
		AttributeRegistry.FieldsByAttribute.Add(typeof(SaveableNameAttribute), new List<FieldInfo>());
		AttributeRegistry.MethodsByAttribute.Add(typeof(ConfigBindAttribute), new List<MethodInfo>());
		AttributeRegistry.MethodsByAttribute.Add(typeof(HookMethodAttribute), new List<MethodInfo>());
		try
		{
			foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
			{
				try
				{
					foreach (PropertyInfo propertyInfo in type.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
					{
						bool flag = propertyInfo.IsDefined(typeof(ConfigBindAttribute));
						bool flag2 = flag;
						if (flag2)
						{
							AttributeRegistry.PropertiesByAttribute[typeof(ConfigBindAttribute)].Add(propertyInfo);
						}
						bool flag3 = propertyInfo.IsDefined(typeof(SaveableNameAttribute));
						bool flag4 = flag3;
						if (flag4)
						{
							AttributeRegistry.PropertiesByAttribute[typeof(SaveableNameAttribute)].Add(propertyInfo);
						}
					}
					foreach (FieldInfo fieldInfo in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
					{
						bool flag5 = fieldInfo.IsDefined(typeof(ConfigBindAttribute));
						bool flag6 = flag5;
						if (flag6)
						{
							AttributeRegistry.FieldsByAttribute[typeof(ConfigBindAttribute)].Add(fieldInfo);
						}
						bool flag7 = fieldInfo.IsDefined(typeof(SaveableNameAttribute));
						bool flag8 = flag7;
						if (flag8)
						{
							AttributeRegistry.FieldsByAttribute[typeof(SaveableNameAttribute)].Add(fieldInfo);
						}
					}
					foreach (MethodInfo methodInfo in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
					{
						bool flag9 = methodInfo.IsDefined(typeof(HookMethodAttribute));
						bool flag10 = flag9;
						if (flag10)
						{
							AttributeRegistry.MethodsByAttribute[typeof(HookMethodAttribute)].Add(methodInfo);
						}
						bool flag11 = methodInfo.IsDefined(typeof(ConfigBindAttribute)) && methodInfo.IsStatic;
						bool flag12 = flag11;
						if (flag12)
						{
							AttributeRegistry.MethodsByAttribute[typeof(ConfigBindAttribute)].Add(methodInfo);
						}
						bool flag13 = methodInfo.IsDefined(typeof(InitializeAttribute)) && methodInfo.IsStatic;
						bool flag14 = flag13;
						if (flag14)
						{
							Logger.LogClient("Begin invoking " + type.Name + "." + methodInfo.Name);
							try
							{
								methodInfo.Invoke(null, new object[0]);
							}
							catch (Exception ex)
							{
								Logger.LogClient("Error due to invoking a init method");
								Logger.LogClient(ex.Message);
								Logger.LogClient(ex.StackTrace);
							}
						}
					}
				}
				catch (Exception ex2)
				{
					Logger.LogClient("Error due to attributes initialize on type");
					Logger.LogClient(ex2.Message);
					Logger.LogClient(ex2.StackTrace);
				}
			}
		}
		catch (Exception ex3)
		{
			Logger.LogClient("Error due to attributes initialize");
			Logger.LogClient(ex3.Message);
			Logger.LogClient(ex3.StackTrace);
		}
	}
	public static Dictionary<Type, List<MethodInfo>> MethodsByAttribute = new Dictionary<Type, List<MethodInfo>>();
	public static Dictionary<Type, List<FieldInfo>> FieldsByAttribute = new Dictionary<Type, List<FieldInfo>>();
	public static Dictionary<Type, List<PropertyInfo>> PropertiesByAttribute = new Dictionary<Type, List<PropertyInfo>>();
}
