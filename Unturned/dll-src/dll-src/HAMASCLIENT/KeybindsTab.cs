using System;
using UnityEngine;
public class KeybindsTab : FeatureTabBase
{
	public override string GetName()
	{
		return "Keybinds";
	}
	public override TabCount GetTabCounts()
	{
		return TabCount.Three;
	}
	public override void DoTab(TabCount tc)
	{
		bool flag = tc == TabCount.One;
		bool flag2 = flag;
		if (flag2)
		{
			this.ScrollPositions[0] = GUILayout.BeginScrollView(this.ScrollPositions[0], Array.Empty<GUILayoutOption>());
			foreach (Keybind dr3ZjnRxP0iAQVvAkuCp9m5Mb in KeybindManager.activeBindings)
			{
				bool flag3 = !KeybindManager.bindingsByName.ContainsKey(dr3ZjnRxP0iAQVvAkuCp9m5Mb.Name);
				if (!flag3)
				{
					bool flag4 = MenuGuiHelper.Button(string.Concat(new string[]
					{
						"[",
						(dr3ZjnRxP0iAQVvAkuCp9m5Mb.InputType == BindInputType.Keyboard) ? dr3ZjnRxP0iAQVvAkuCp9m5Mb.Key.ToString() : dr3ZjnRxP0iAQVvAkuCp9m5Mb.InputType.ToString(),
						"] ",
						KeybindManager.bindingsByName[dr3ZjnRxP0iAQVvAkuCp9m5Mb.Name].categoryName,
						" > ",
						dr3ZjnRxP0iAQVvAkuCp9m5Mb.Name
					}), -1, true, null) && !KeybindManager.awaitingKeyCapture;
					bool flag5 = flag4;
					if (flag5)
					{
						KeybindsTab.IsAddingBind = false;
						KeybindsTab.SelectedBind = dr3ZjnRxP0iAQVvAkuCp9m5Mb;
					}
				}
			}
			bool flag6 = MenuGuiHelper.Button("Add key", -1, true, null);
			bool flag7 = flag6;
			if (flag7)
			{
				KeybindsTab.IsConfiguringOperand = false;
				KeybindsTab.IsAddingBind = true;
			}
			GUILayout.EndScrollView();
		}
		else
		{
			bool flag8 = tc == TabCount.Two;
			bool flag9 = flag8;
			if (flag9)
			{
				this.ScrollPositions[1] = GUILayout.BeginScrollView(this.ScrollPositions[1], Array.Empty<GUILayoutOption>());
				bool dtjicbrmaNmkP0dD3QCiL0YNJ = KeybindsTab.IsAddingBind;
				bool flag10 = dtjicbrmaNmkP0dD3QCiL0YNJ;
				if (flag10)
				{
					foreach (string text in KeybindManager.bindingCategoryLabels)
					{
						bool flag11 = MenuGuiHelper.Button(text, -1, true, null);
						bool flag12 = flag11;
						if (flag12)
						{
							KeybindsTab.SelectedActionName = text;
						}
					}
				}
				else
				{
					bool flag13 = KeybindsTab.SelectedBind != null && KeybindManager.bindingsByName.ContainsKey(KeybindsTab.SelectedBind.Name);
					bool flag14 = flag13;
					if (flag14)
					{
						GUILayout.Label(string.Concat(new string[]
						{
							"[",
							(KeybindsTab.SelectedBind.InputType == BindInputType.Keyboard) ? KeybindsTab.SelectedBind.Key.ToString() : KeybindsTab.SelectedBind.InputType.ToString(),
							"] ",
							KeybindManager.bindingsByName[KeybindsTab.SelectedBind.Name].categoryName,
							" > ",
							KeybindsTab.SelectedBind.Name
						}), Array.Empty<GUILayoutOption>());
						bool flag15 = KeybindManager.bindingsByName[KeybindsTab.SelectedBind.Name].operandEditor != null;
						bool flag16 = flag15;
						if (flag16)
						{
							GUILayout.Label("Operand: " + KeybindManager.bindingsByName[KeybindsTab.SelectedBind.Name].operandEditor.GetOperandValue(), Array.Empty<GUILayoutOption>());
							bool flag17 = KeybindManager.bindingsByName[KeybindsTab.SelectedBind.Name].methodInfo == null && MenuGuiHelper.Button("Change operand", -1, true, null);
							bool flag18 = flag17;
							if (flag18)
							{
								KeybindsTab.IsConfiguringOperand = !KeybindsTab.IsConfiguringOperand;
							}
						}
						bool flag19 = MenuGuiHelper.Button("Change bind", -1, true, null);
						bool flag20 = flag19;
						if (flag20)
						{
							KeybindManager.AwaitKeyCapture(KeybindsTab.SelectedBind.Name);
						}
						KeybindsTab.SelectedBind.InputType = KeybindsTab.SelectedBind.InputType.EnumPopup("Bind type:", "");
						bool flag21 = KeybindsTab.SelectedBind.Name == "Open menu";
						bool flag22 = flag21;
						if (flag22)
						{
							GUILayout.Label("You cannot delete this bind", Array.Empty<GUILayoutOption>());
						}
						else
						{
							bool flag23 = !KeybindManager.awaitingKeyCapture && MenuGuiHelper.Button("Remove key", -1, true, null);
							bool flag24 = flag23;
							if (flag24)
							{
								KeybindManager.boundVarNames.Remove(KeybindsTab.SelectedBind.Name);
								KeybindManager.activeBindings.Remove(KeybindsTab.SelectedBind);
								KeybindsTab.SelectedBind = null;
							}
						}
					}
					else
					{
						GUILayout.Label("Select a bind", Array.Empty<GUILayoutOption>());
					}
				}
				GUILayout.EndScrollView();
			}
			else
			{
				this.ScrollPositions[2] = GUILayout.BeginScrollView(this.ScrollPositions[2], Array.Empty<GUILayoutOption>());
				bool daDVuyXoGh5CtNAjpbSI9fhho = KeybindManager.awaitingKeyCapture;
				bool flag25 = daDVuyXoGh5CtNAjpbSI9fhho;
				if (flag25)
				{
					GUILayout.Label("Press any key to bind it", Array.Empty<GUILayoutOption>());
				}
				else
				{
					bool dtjicbrmaNmkP0dD3QCiL0YNJ2 = KeybindsTab.IsAddingBind;
					bool flag26 = dtjicbrmaNmkP0dD3QCiL0YNJ2;
					if (flag26)
					{
						foreach (BindableVariable dgm8QBfL795yoNKOj7zPTCd8n in KeybindManager.bindingsByName.Values)
						{
							bool flag27 = dgm8QBfL795yoNKOj7zPTCd8n.categoryName == KeybindsTab.SelectedActionName && !KeybindManager.boundVarNames.Contains(dgm8QBfL795yoNKOj7zPTCd8n.variableName) && MenuGuiHelper.Button(dgm8QBfL795yoNKOj7zPTCd8n.variableName, -1, true, null);
							bool flag28 = flag27;
							if (flag28)
							{
								KeybindManager.AwaitKeyCapture(dgm8QBfL795yoNKOj7zPTCd8n.variableName);
							}
						}
					}
					else
					{
						bool flag29 = KeybindsTab.IsConfiguringOperand && KeybindsTab.SelectedBind != null && KeybindManager.bindingsByName.ContainsKey(KeybindsTab.SelectedBind.Name);
						bool flag30 = flag29;
						if (flag30)
						{
							KeybindManager.bindingsByName[KeybindsTab.SelectedBind.Name].operandEditor.DrawConfigureTab();
						}
					}
				}
				GUILayout.EndScrollView();
			}
		}
	}
	public static bool IsAddingBind = false;
	public static bool IsConfiguringOperand = false;
	public static Keybind SelectedBind;
	public static string SelectedActionName = "";
	public Vector2[] ScrollPositions = new Vector2[3];
}
