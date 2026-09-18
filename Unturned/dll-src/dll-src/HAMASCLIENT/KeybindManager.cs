using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
public class KeybindManager : MonoBehaviour
{
	public static void InitializeBindings()
	{
		KeybindManager.activeBindings.Clear();
		KeybindManager.bindingCategoryLabels.Clear();
		KeybindManager.boundVarNames.Clear();
		KeybindManager.bindingsByName.Clear();
		foreach (PropertyInfo propertyInfo in AttributeRegistry.PropertiesByAttribute[typeof(ConfigBindAttribute)])
		{
			try
			{
				ConfigBindAttribute dvBeyuawbq3X9NsoJgcUZmAIZ = (ConfigBindAttribute)propertyInfo.GetCustomAttribute(typeof(ConfigBindAttribute));
				bool flag = !KeybindManager.bindingsByName.ContainsKey(dvBeyuawbq3X9NsoJgcUZmAIZ.VariableName);
				bool flag2 = flag;
				if (flag2)
				{
					SaveValueType da7EjeELhPnQjMBiXW309VJV = ConfigManager.TypeToSaveValueType(propertyInfo.PropertyType);
					bool flag3 = da7EjeELhPnQjMBiXW309VJV == SaveValueType.Unknown;
					bool flag4 = !flag3;
					if (flag4)
					{
						KeybindManager.bindingsByName.Add(dvBeyuawbq3X9NsoJgcUZmAIZ.VariableName, new BindableVariable(propertyInfo, dvBeyuawbq3X9NsoJgcUZmAIZ.CategoryName.AppendTypeSuffix(da7EjeELhPnQjMBiXW309VJV), dvBeyuawbq3X9NsoJgcUZmAIZ.VariableName, da7EjeELhPnQjMBiXW309VJV));
						bool flag5 = !KeybindManager.bindingCategoryLabels.Contains(dvBeyuawbq3X9NsoJgcUZmAIZ.CategoryName.AppendTypeSuffix(da7EjeELhPnQjMBiXW309VJV));
						bool flag6 = flag5;
						if (flag6)
						{
							KeybindManager.bindingCategoryLabels.Add(dvBeyuawbq3X9NsoJgcUZmAIZ.CategoryName.AppendTypeSuffix(da7EjeELhPnQjMBiXW309VJV));
						}
					}
				}
				else
				{
					Logger.LogClient("Bind with name " + dvBeyuawbq3X9NsoJgcUZmAIZ.VariableName + " have a copy, skip it");
				}
			}
			catch
			{
			}
		}
		foreach (FieldInfo fieldInfo in AttributeRegistry.FieldsByAttribute[typeof(ConfigBindAttribute)])
		{
			try
			{
				SaveValueType da7EjeELhPnQjMBiXW309VJV2 = ConfigManager.TypeToSaveValueType(fieldInfo.FieldType);
				bool flag7 = da7EjeELhPnQjMBiXW309VJV2 == SaveValueType.Unknown;
				bool flag8 = !flag7;
				if (flag8)
				{
					ConfigBindAttribute dvBeyuawbq3X9NsoJgcUZmAIZ2 = (ConfigBindAttribute)fieldInfo.GetCustomAttribute(typeof(ConfigBindAttribute));
					bool flag9 = !KeybindManager.bindingsByName.ContainsKey(dvBeyuawbq3X9NsoJgcUZmAIZ2.VariableName);
					bool flag10 = flag9;
					if (flag10)
					{
						KeybindManager.bindingsByName.Add(dvBeyuawbq3X9NsoJgcUZmAIZ2.VariableName, new BindableVariable(fieldInfo, dvBeyuawbq3X9NsoJgcUZmAIZ2.CategoryName.AppendTypeSuffix(da7EjeELhPnQjMBiXW309VJV2), dvBeyuawbq3X9NsoJgcUZmAIZ2.VariableName, da7EjeELhPnQjMBiXW309VJV2));
						bool flag11 = !KeybindManager.bindingCategoryLabels.Contains(dvBeyuawbq3X9NsoJgcUZmAIZ2.CategoryName.AppendTypeSuffix(da7EjeELhPnQjMBiXW309VJV2));
						bool flag12 = flag11;
						if (flag12)
						{
							KeybindManager.bindingCategoryLabels.Add(dvBeyuawbq3X9NsoJgcUZmAIZ2.CategoryName.AppendTypeSuffix(da7EjeELhPnQjMBiXW309VJV2));
						}
					}
					else
					{
						Logger.LogClient("Bind with name " + dvBeyuawbq3X9NsoJgcUZmAIZ2.VariableName + " have a copy, skip it");
					}
				}
			}
			catch (Exception ex)
			{
				Logger.LogClient("Error due to interact with " + fieldInfo.Name + " field");
				Logger.LogClient(ex.Message);
				Logger.LogClient(ex.StackTrace);
			}
		}
		foreach (MethodInfo methodInfo in AttributeRegistry.MethodsByAttribute[typeof(ConfigBindAttribute)])
		{
			try
			{
				ConfigBindAttribute dvBeyuawbq3X9NsoJgcUZmAIZ3 = (ConfigBindAttribute)methodInfo.GetCustomAttribute(typeof(ConfigBindAttribute));
				bool flag13 = !KeybindManager.bindingsByName.ContainsKey(dvBeyuawbq3X9NsoJgcUZmAIZ3.VariableName);
				bool flag14 = flag13;
				if (flag14)
				{
					KeybindManager.bindingsByName.Add(dvBeyuawbq3X9NsoJgcUZmAIZ3.VariableName, new BindableVariable(methodInfo, dvBeyuawbq3X9NsoJgcUZmAIZ3.CategoryName + "->Methods", dvBeyuawbq3X9NsoJgcUZmAIZ3.VariableName));
					bool flag15 = !KeybindManager.bindingCategoryLabels.Contains(dvBeyuawbq3X9NsoJgcUZmAIZ3.CategoryName + "->Methods");
					bool flag16 = flag15;
					if (flag16)
					{
						KeybindManager.bindingCategoryLabels.Add(dvBeyuawbq3X9NsoJgcUZmAIZ3.CategoryName + "->Methods");
					}
				}
				else
				{
					Logger.LogClient("Bind with name " + dvBeyuawbq3X9NsoJgcUZmAIZ3.VariableName + " have a copy, skip it");
				}
			}
			catch (Exception ex2)
			{
				Logger.LogClient("Error due to interact with " + methodInfo.Name + " field");
				Logger.LogClient(ex2.Message);
				Logger.LogClient(ex2.StackTrace);
			}
		}
		KeybindManager.BindKey(KeyCode.F1, "Open menu");
		KeybindManager.BindKey(KeyCode.None, "Toggle vehicle player collision");
		Bootstrapper.HostGameObject.AddComponent<KeybindManager>();
	}
	public void Update()
	{
		List<Keybind> list = new List<Keybind>(KeybindManager.activeBindings);
		foreach (Keybind dr3ZjnRxP0iAQVvAkuCp9m5Mb in list)
		{
			try
			{
				bool flag = false;
				switch (dr3ZjnRxP0iAQVvAkuCp9m5Mb.InputType)
				{
				case BindInputType.LeftMouse:
					flag = Input.GetMouseButtonDown(0);
					break;
				case BindInputType.RightMouse:
					flag = Input.GetMouseButtonDown(1);
					break;
				case BindInputType.MouseScrollButton:
					flag = Input.GetMouseButtonDown(2);
					break;
				case BindInputType.Mouse4:
					flag = Input.GetMouseButtonDown(3);
					break;
				case BindInputType.Mouse5:
					flag = Input.GetMouseButtonDown(4);
					break;
				case BindInputType.Keyboard:
					flag = Input.GetKeyDown(dr3ZjnRxP0iAQVvAkuCp9m5Mb.Key);
					break;
				}
				bool flag2 = flag;
				bool flag3 = flag2;
				if (flag3)
				{
					bool flag4 = KeybindManager.bindingsByName[dr3ZjnRxP0iAQVvAkuCp9m5Mb.Name].fieldInfo != null;
					bool flag5 = flag4;
					if (flag5)
					{
						KeybindManager.bindingsByName[dr3ZjnRxP0iAQVvAkuCp9m5Mb.Name].operandEditor.Proceed();
					}
					else
					{
						KeybindManager.bindingsByName[dr3ZjnRxP0iAQVvAkuCp9m5Mb.Name].operandEditor.Proceed();
					}
				}
			}
			catch
			{
				KeybindManager.activeBindings.Remove(dr3ZjnRxP0iAQVvAkuCp9m5Mb);
			}
		}
	}
	public static void AwaitKeyCapture(string varNameForBinding)
	{
		KeybindManager.awaitingKeyCapture = true;
		Bootstrapper.HostGameObject.gameObject.AddComponent<KeybindCapture>().targetVariableName = varNameForBinding;
	}
	public static void BindKey(KeyCode key, string varNameForBinding)
	{
		KeybindsTab.IsAddingBind = false;
		KeybindManager.awaitingKeyCapture = false;
		bool flag = KeybindManager.boundVarNames.Contains(varNameForBinding);
		bool flag2 = flag;
		if (flag2)
		{
			foreach (Keybind dr3ZjnRxP0iAQVvAkuCp9m5Mb in KeybindManager.activeBindings)
			{
				bool flag3 = dr3ZjnRxP0iAQVvAkuCp9m5Mb.Name == varNameForBinding;
				bool flag4 = flag3;
				if (flag4)
				{
					KeybindManager.activeBindings.Remove(dr3ZjnRxP0iAQVvAkuCp9m5Mb);
					break;
				}
			}
		}
		else
		{
			KeybindManager.boundVarNames.Add(varNameForBinding);
		}
		KeybindManager.activeBindings.Add(new Keybind
		{
			Key = key,
			Name = varNameForBinding
		});
	}
	public static void BindKeyWithType(KeyCode key, BindInputType bindType, string varNameForBinding)
	{
		KeybindsTab.IsAddingBind = false;
		KeybindManager.awaitingKeyCapture = false;
		bool flag = KeybindManager.boundVarNames.Contains(varNameForBinding);
		bool flag2 = flag;
		if (flag2)
		{
			foreach (Keybind dr3ZjnRxP0iAQVvAkuCp9m5Mb in KeybindManager.activeBindings)
			{
				bool flag3 = dr3ZjnRxP0iAQVvAkuCp9m5Mb.Name == varNameForBinding;
				bool flag4 = flag3;
				if (flag4)
				{
					KeybindManager.activeBindings.Remove(dr3ZjnRxP0iAQVvAkuCp9m5Mb);
					break;
				}
			}
		}
		else
		{
			KeybindManager.boundVarNames.Add(varNameForBinding);
		}
		KeybindManager.activeBindings.Add(new Keybind
		{
			Key = key,
			Name = varNameForBinding,
			InputType = bindType
		});
	}
	public static Dictionary<SaveValueType, Type> valueTypeToEditorMap = new Dictionary<SaveValueType, Type>
	{
		{
			SaveValueType.Bool,
			typeof(BoolOperandEditor)
		},
		{
			SaveValueType.Int,
			typeof(ArithmeticOperand)
		},
		{
			SaveValueType.Float,
			typeof(FloatOperand)
		},
		{
			SaveValueType.Enum,
			typeof(EnumOperandEditor)
		}
	};
	public static Dictionary<string, BindableVariable> bindingsByName = new Dictionary<string, BindableVariable>();
	public static List<Keybind> activeBindings = new List<Keybind>();
	public static List<string> bindingCategoryLabels = new List<string>();
	public static List<string> boundVarNames = new List<string>();
	public static bool awaitingKeyCapture = false;
}
