using System;
using System.Reflection;
using UnityEngine;
public struct BindableVariable
{
	public BindableVariable(FieldInfo fieldInfo, string categoryName, string variableName, SaveValueType vt)
	{
		this.fieldInfo = fieldInfo;
		this.propertyInfo = null;
		this.methodInfo = null;
		this.categoryName = categoryName;
		this.variableName = variableName;
		this.isHidden = false;
		this.valueType = vt;
		this.operandEditor = (OperandBase)Activator.CreateInstance(KeybindManager.valueTypeToEditorMap[vt], new object[]
		{
			variableName,
			new ReflectedStaticMember(fieldInfo)
		});
		this.keybind = KeyCode.None;
		this.inputType = BindInputType.Keyboard;
	}
	public BindableVariable(PropertyInfo propertyInfo, string categoryName, string variableName, SaveValueType vt)
	{
		this.propertyInfo = propertyInfo;
		this.fieldInfo = null;
		this.methodInfo = null;
		this.categoryName = categoryName;
		this.variableName = variableName;
		this.isHidden = false;
		this.valueType = vt;
		this.operandEditor = (OperandBase)Activator.CreateInstance(KeybindManager.valueTypeToEditorMap[vt], new object[]
		{
			variableName,
			new ReflectedStaticMember(propertyInfo)
		});
		this.keybind = KeyCode.None;
		this.inputType = BindInputType.Keyboard;
	}
	public BindableVariable(MethodInfo methodInfo, string categoryName, string variableName)
	{
		this.methodInfo = methodInfo;
		this.fieldInfo = null;
		this.propertyInfo = null;
		this.categoryName = categoryName;
		this.variableName = variableName;
		this.isHidden = false;
		this.valueType = SaveValueType.Unknown;
		this.operandEditor = new MethodInvokeOperand(variableName, null);
		(this.operandEditor as MethodInvokeOperand).Method = methodInfo;
		this.keybind = KeyCode.None;
		this.inputType = BindInputType.Keyboard;
	}
	public FieldInfo fieldInfo;
	public PropertyInfo propertyInfo;
	public MethodInfo methodInfo;
	public OperandBase operandEditor;
	public string categoryName;
	public string variableName;
	public bool isHidden;
	public SaveValueType valueType;
	public BindInputType inputType;
	public KeyCode keybind;
}
