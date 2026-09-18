using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
public class Bootstrapper : MonoBehaviour
{
	private void Awake()
	{
		bool flag = !Bootstrapper.InitFinished;
		if (flag)
		{
			this.Update();
		}
	}
	private void Update()
	{
		switch (this.InitStage)
		{
		case 1:
			Logger.LogClient("INIT::CLIENT");
			goto IL_0253;
		case 2:
		case 6:
		case 7:
			goto IL_0253;
		case 3:
			UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
			Bootstrapper.HostGameObject = base.gameObject;
			Logger.LogClient("INIT::OBJECTS");
			goto IL_0253;
		case 4:
			CoroutineHost.Initialize();
			Logger.LogClient("INIT::MANAGER");
			goto IL_0253;
		case 5:
			try
			{
				AttributeRegistry.Initialize();
				Logger.LogClient("INIT::ATR_MANAGER");
			}
			catch (Exception ex)
			{
				Logger.LogClient("INIT::ATR_MANAGER FAILED!!!");
				Logger.LogClient(ex.Message);
				Logger.LogClient(ex.StackTrace);
			}
			try
			{
				GuiStyles.Init();
				Logger.LogClient("INIT::ASSET_MANAGER");
			}
			catch (Exception ex2)
			{
				Logger.LogClient("INIT::ASSET_MANAGER FAILED!!!");
				Logger.LogClient(ex2.Message);
				Logger.LogClient(ex2.StackTrace);
			}
			try
			{
				OverrideManager.ApplyAllOverrides();
				Logger.LogClient("INIT::OV_MANAGER");
				goto IL_0253;
			}
			catch (Exception ex3)
			{
				Logger.LogClient("INIT::OV_MANAGER FAILED!!!");
				Logger.LogClient(ex3.Message);
				Logger.LogClient(ex3.StackTrace);
				goto IL_0253;
			}
			break;
		case 8:
			break;
		case 9:
			try
			{
				KeybindManager.InitializeBindings();
				Logger.LogClient("INIT::BIND_MANAGER");
				goto IL_0253;
			}
			catch (Exception ex4)
			{
				Logger.LogClient("INIT::BIND_MANAGER FAILED!!!");
				Logger.LogClient(ex4.Message);
				Logger.LogClient(ex4.StackTrace);
				goto IL_0253;
			}
			break;
		case 10:
			Bootstrapper.HostGameObject.AddComponent<FpsCounter>();
			Bootstrapper.HostGameObject.AddComponent<PlayTimeScanner>();
			try
			{
				Bootstrapper.HostGameObject.AddComponent<PostProcessDebug>();
			}
			catch
			{
				Logger.LogClient("PostProcessDebug not available");
			}
			Bootstrapper.HostGameObject.AddComponent<MenuState>();
			Logger.LogClient("INIT::COMPONENTS");
			goto IL_0253;
		case 11:
			goto IL_023E;
		default:
			goto IL_0253;
		}
		try
		{
			ConfigManager.InitSaveableFields();
			UnturnedSettingsConfig.Initialize();
			Logger.LogClient("INIT::CONFIGS");
			goto IL_0253;
		}
		catch (Exception ex5)
		{
			Logger.LogClient("INIT::CONFIGS FAILED!!!");
			Logger.LogClient(ex5.Message);
			Logger.LogClient(ex5.StackTrace);
			goto IL_0253;
		}
		IL_023E:
		Bootstrapper.InitFinished = true;
		UnityEngine.Object.Destroy(this);
		Logger.LogClient("INIT::CLEAN");
		return;
		IL_0253:
		this.InitStage += 1;
		bool flag = !Bootstrapper.InitFinished;
		if (flag)
		{
			this.Update();
		}
	}
	public static GameObject HostGameObject;
	public static bool InitFinished;
	public static bool InitFlag2;
	private byte InitStage;
}
