using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SDG.Unturned;
using UnityEngine;

namespace gatyware
{
	public static class AntiSpy
	{
		[DllImport("kernel32.dll")]
		private static extern bool VirtualProtect(IntPtr addr, int size, uint prot, out uint old);
        private static byte[] _saved2 = new byte[14];
        private static IntPtr _origPtr2;
        private static IntPtr _hookPtr2;
        private static MethodInfo _origMethod2;
        public static void Init()
        {
            try
            {
                if (!Directory.Exists(AntiSpy.SpyImageDir))
                {
                    Directory.CreateDirectory(AntiSpy.SpyImageDir);
                }
                GameObject gameObject = new GameObject("mc_spy");
                UnityEngine.Object.DontDestroyOnLoad(gameObject);
                gameObject.hideFlags = (HideFlags)61;
                AntiSpy._mono = gameObject.AddComponent<AntiSpy.AntiSpyBehaviour>();

                // Hook primary method
                AntiSpy._origMethod = typeof(Player).GetMethod("ReceiveTakeScreenshot", BindingFlags.Instance | BindingFlags.Public);
                if (AntiSpy._origMethod != null)
                {
                    RuntimeHelpers.PrepareMethod(AntiSpy._origMethod.MethodHandle);
                    AntiSpy._origPtr = AntiSpy._origMethod.MethodHandle.GetFunctionPointer();
                    MethodInfo method = typeof(AntiSpy).GetMethod("OnReceiveTakeScreenshot", BindingFlags.Static | BindingFlags.Public);
                    RuntimeHelpers.PrepareMethod(method.MethodHandle);
                    AntiSpy._hookPtr = method.MethodHandle.GetFunctionPointer();
                    Marshal.Copy(AntiSpy._origPtr, AntiSpy._saved, 0, 14);
                    AntiSpy.WriteJmp(AntiSpy._origPtr, AntiSpy._hookPtr);
                    AntiSpy._active = true;
                    Runtime.Trace("antispy: hooked OK");
                }
                else
                {
                    Runtime.Trace("antispy: primary method not found");
                }

                // BACKUP: Hook ReceiveTakeScreenshotRequest if it exists
                AntiSpy._origMethod2 = typeof(Player).GetMethod("ReceiveTakeScreenshotRequest", BindingFlags.Instance | BindingFlags.Public);
                if (AntiSpy._origMethod2 != null)
                {
                    RuntimeHelpers.PrepareMethod(AntiSpy._origMethod2.MethodHandle);
                    AntiSpy._origPtr2 = AntiSpy._origMethod2.MethodHandle.GetFunctionPointer();
                    MethodInfo method2 = typeof(AntiSpy).GetMethod("OnReceiveTakeScreenshotRequest", BindingFlags.Static | BindingFlags.Public);
                    RuntimeHelpers.PrepareMethod(method2.MethodHandle);
                    AntiSpy._hookPtr2 = method2.MethodHandle.GetFunctionPointer();
                    Marshal.Copy(AntiSpy._origPtr2, AntiSpy._saved2, 0, 14);
                    AntiSpy.WriteJmp(AntiSpy._origPtr2, AntiSpy._hookPtr2);
                    Runtime.Trace("antispy: backup hooked OK");
                }
            }
            catch (Exception ex)
            {
                Runtime.Trace("antispy: " + ex.Message);
            }
        }
        private static void WriteJmp(IntPtr from, IntPtr to)
		{
			uint prot;
			AntiSpy.VirtualProtect(from, 14, 64U, out prot);
			byte[] array = new byte[14];
			array[0] = byte.MaxValue;
			array[1] = 37;
			BitConverter.GetBytes(to.ToInt64()).CopyTo(array, 6);
			Marshal.Copy(array, 0, from, 14);
			AntiSpy.VirtualProtect(from, 14, prot, out prot);
		}
        public static void OnReceiveTakeScreenshotRequest(Player self)
        {
            OnReceiveTakeScreenshot(self);
        }

        public static void OnReceiveTakeScreenshot(Player self)
		{
			try
			{
				State.LastSpyTime = Time.unscaledTime;
				AntiSpy._spyDepth++;
				State.IsSpying = true;
                State.SpyBlockUntil = Time.unscaledTime + 1f;
                Chams.ForceDisable = true;
				switch (State.SpyMode)
				{
				case 0:
					AntiSpy.ClearPreview();
					AntiSpy._mono.StartCoroutine(AntiSpy.FrameHide(self));
					break;
				case 1:
					AntiSpy.ClearPreview();
					AntiSpy._mono.StartCoroutine(AntiSpy.ShowAndCapture(self));
					break;
				case 2:
					if (!AntiSpy.TrySendCustomImage(self))
					{
						AntiSpy.ClearPreview();
						AntiSpy._mono.StartCoroutine(AntiSpy.FrameHide(self));
					}
					else
					{
						AntiSpy.SpyEnd();
					}
					break;
				default:
					AntiSpy.SpyEnd();
					AntiSpy.CallOriginal(self);
					break;
				}
			}
			catch
			{
				AntiSpy.SpyEnd();
				AntiSpy.CallOriginal(self);
			}
		}

        private static void SpyEnd()
        {
            AntiSpy._spyDepth--;
            if (AntiSpy._spyDepth <= 0)
            {
                AntiSpy._spyDepth = 0;
                // Delay clearing IsSpying to ensure screenshot frame is fully captured
                _mono.StartCoroutine(DelayedSpyClear());
            }
        }

        private static IEnumerator DelayedSpyClear()
        {
            yield return new WaitForSeconds(0.5f);
            State.IsSpying = false;
            Chams.ForceDisable = false;
        }

        private static void ClearPreview()
		{
			if (AntiSpy._spyPreview != null)
			{
				UnityEngine.Object.Destroy(AntiSpy._spyPreview);
				AntiSpy._spyPreview = null;
			}
		}

		private static IEnumerator FrameHide(Player player)
		{
			yield return null;
			yield return new WaitForEndOfFrame();
			AntiSpy.CallOriginal(player);
			yield return null;
			AntiSpy.SpyEnd();
		}

		private static IEnumerator ShowAndCapture(Player player)
		{
			yield return null;
			yield return new WaitForEndOfFrame();
			AntiSpy.ReadScreenshotFinal(player);
			yield return null;
			AntiSpy.SpyEnd();
		}

		private static void ReadScreenshotFinal(Player player)
		{
			try
			{
				AntiSpy.ClearPreview();
				FieldInfo field = typeof(Player).GetField("screenshotFinal", BindingFlags.Instance | BindingFlags.NonPublic);
				if (field == null)
				{
					Runtime.Trace("spy pv: no field");
				}
				else
				{
					Texture2D texture2D = field.GetValue(player) as Texture2D;
					if (texture2D == null)
					{
						Runtime.Trace("spy pv: tex null");
					}
					else if (texture2D.width < 4 || texture2D.height < 4)
					{
						Runtime.Trace("spy pv: tex tiny " + texture2D.width.ToString() + "x" + texture2D.height.ToString());
					}
					else
					{
						try
						{
							Color[] pixels = texture2D.GetPixels();
							AntiSpy._spyPreview = new Texture2D(texture2D.width, texture2D.height, (TextureFormat)4, false);
							AntiSpy._spyPreview.hideFlags = (HideFlags)61;
							AntiSpy._spyPreview.SetPixels(pixels);
							AntiSpy._spyPreview.Apply();
							Runtime.Trace("spy pv: ok " + texture2D.width.ToString() + "x" + texture2D.height.ToString());
						}
						catch
						{
							RenderTexture temporary = RenderTexture.GetTemporary(texture2D.width, texture2D.height);
							Graphics.Blit(texture2D, temporary);
							RenderTexture active = RenderTexture.active;
							RenderTexture.active = temporary;
							AntiSpy._spyPreview = new Texture2D(texture2D.width, texture2D.height, (TextureFormat)3, false);
							AntiSpy._spyPreview.hideFlags = (HideFlags)61;
							AntiSpy._spyPreview.ReadPixels(new Rect(0f, 0f, (float)texture2D.width, (float)texture2D.height), 0, 0);
							AntiSpy._spyPreview.Apply();
							RenderTexture.active = active;
							RenderTexture.ReleaseTemporary(temporary);
							Runtime.Trace("spy pv: blit fallback " + texture2D.width.ToString() + "x" + texture2D.height.ToString());
						}
					}
				}
			}
			catch (Exception ex)
			{
				Runtime.Trace("spy pv err: " + ex.Message);
			}
		}

		private static void CallOriginal(Player player)
		{
			uint prot;
			AntiSpy.VirtualProtect(AntiSpy._origPtr, 14, 64U, out prot);
			Marshal.Copy(AntiSpy._saved, 0, AntiSpy._origPtr, 14);
			AntiSpy.VirtualProtect(AntiSpy._origPtr, 14, prot, out prot);
			try
			{
				AntiSpy._origMethod.Invoke(player, null);
			}
			finally
			{
				AntiSpy.WriteJmp(AntiSpy._origPtr, AntiSpy._hookPtr);
			}
		}

		private static bool TrySendCustomImage(Player player)
		{
			bool result = default;
			try
			{
				if (!Directory.Exists(AntiSpy.SpyImageDir))
				{
					Runtime.Trace("spy: dir missing");
					result = false;
				}
				else
				{
					List<string> list = new List<string>();
					foreach (string text in Directory.GetFiles(AntiSpy.SpyImageDir))
					{
						string a = Path.GetExtension(text).ToLowerInvariant();
						if (a == ".jpg" || a == ".jpeg" || a == ".png" || a == ".bmp")
						{
							list.Add(text);
						}
					}
					if (list.Count == 0)
					{
						Runtime.Trace("spy: no images");
						result = false;
					}
					else
					{
						string path = list[AntiSpy._imgIdx % list.Count];
						AntiSpy._imgIdx++;
						Runtime.Trace("spy: loading " + Path.GetFileName(path));
						byte[] array = File.ReadAllBytes(path);
						Texture2D texture2D = new Texture2D(2, 2);
						if (!ImageConversion.LoadImage(texture2D, array))
						{
							UnityEngine.Object.Destroy(texture2D);
							Runtime.Trace("spy: LoadImage failed");
							result = false;
						}
						else
						{
							RenderTexture temporary = RenderTexture.GetTemporary(640, 480);
							Graphics.Blit(texture2D, temporary);
							RenderTexture active = RenderTexture.active;
							RenderTexture.active = temporary;
							Texture2D texture2D2 = new Texture2D(640, 480, (TextureFormat)3, false);
							texture2D2.ReadPixels(new Rect(0f, 0f, 640f, 480f), 0, 0);
							texture2D2.Apply();
							RenderTexture.active = active;
							RenderTexture.ReleaseTemporary(temporary);
							UnityEngine.Object.Destroy(texture2D);
							byte[] array2 = ImageConversion.EncodeToJPG(texture2D2, 33);
							UnityEngine.Object.Destroy(texture2D2);
							Runtime.Trace("spy: jpg size " + array2.Length.ToString());
							if (array2.Length >= 40000)
							{
								Runtime.Trace("spy: jpg too large");
								result = false;
							}
							else
							{
								AntiSpy.ClearPreview();
								AntiSpy._spyPreview = new Texture2D(2, 2);
								ImageConversion.LoadImage(AntiSpy._spyPreview, array2);
								AntiSpy._spyPreview.hideFlags = (HideFlags)61;
								AntiSpy.SendImageDirect(player, array2);
								result = true;
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Runtime.Trace("spy err: " + ex.GetType().Name + ": " + ex.Message);
				result = false;
			}
			return result;
		}

		private static void SendImageDirect(Player player, byte[] jpg)
		{
			try
			{
				FieldInfo field = typeof(Player).GetField("SendScreenshotRelay", BindingFlags.Static | BindingFlags.NonPublic);
				if (field == null)
				{
					Runtime.Trace("spy: no relay field");
				}
				else
				{
					object value = field.GetValue(null);
					if (value == null)
					{
						Runtime.Trace("spy: relay null");
					}
					else
					{
						MethodInfo methodInfo = null;
						Type type = typeof(Player);
						while (type != null && methodInfo == null)
						{
							methodInfo = type.GetMethod("GetNetId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
							type = type.BaseType;
						}
						if (methodInfo == null)
						{
							Runtime.Trace("spy: no GetNetId");
						}
						else
						{
							object obj = methodInfo.Invoke(player, null);
							Type type2 = null;
							Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
							for (int i = 0; i < assemblies.Length; i++)
							{
								type2 = assemblies[i].GetType("SDG.NetTransport.ENetReliability");
								if (type2 != null)
								{
									break;
								}
							}
							if (type2 == null)
							{
								Runtime.Trace("spy: no reliability type");
							}
							else
							{
								object obj2 = Enum.Parse(type2, "Reliable");
								MethodInfo method = typeof(Player).GetMethod("SendScreenshotRelay_Write", BindingFlags.Instance | BindingFlags.NonPublic);
								if (method == null)
								{
									Runtime.Trace("spy: no write method");
								}
								else
								{
									Type parameterType = method.GetParameters()[0].ParameterType;
									Delegate @delegate = Delegate.CreateDelegate(typeof(Action<, >).MakeGenericType(new Type[]
									{
										parameterType,
										typeof(byte[])
									}), player, method);
									MethodInfo methodInfo2 = null;
									Type type3 = value.GetType();
									while (type3 != null && methodInfo2 == null)
									{
										foreach (MethodInfo methodInfo3 in type3.GetMethods(BindingFlags.Instance | BindingFlags.Public))
										{
											if (methodInfo3.Name == "Invoke" && methodInfo3.IsGenericMethod && methodInfo3.GetGenericArguments().Length == 1 && methodInfo3.GetParameters().Length == 4)
											{
												methodInfo2 = methodInfo3.MakeGenericMethod(new Type[]
												{
													typeof(byte[])
												});
												break;
											}
										}
										type3 = type3.BaseType;
									}
									if (methodInfo2 == null)
									{
										Runtime.Trace("spy: no Invoke<T>(4)");
									}
									else
									{
										Runtime.Trace("spy: sending " + jpg.Length.ToString() + " bytes");
										methodInfo2.Invoke(value, new object[]
										{
											obj,
											obj2,
											@delegate,
											jpg
										});
										Runtime.Trace("spy: sent OK");
									}
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Runtime.Trace("spy send: " + ex.GetType().Name + ": " + ex.Message);
				if (ex.InnerException != null)
				{
					Runtime.Trace("spy inner: " + ex.InnerException.Message);
				}
			}
		}


		private static byte[] _saved = new byte[14];

		private static IntPtr _origPtr;

		private static IntPtr _hookPtr;

		private static MethodInfo _origMethod;

		private static bool _active;

		public static void Uninstall()
		{
			if (!AntiSpy._active) return;
			uint prot;
			AntiSpy.VirtualProtect(AntiSpy._origPtr, 14, 64U, out prot);
			Marshal.Copy(AntiSpy._saved, 0, AntiSpy._origPtr, 14);
			AntiSpy.VirtualProtect(AntiSpy._origPtr, 14, prot, out prot);
			AntiSpy._active = false;
			State.IsSpying = false;
			Chams.ForceDisable = false;
			AntiSpy._spyDepth = 0;
		}

		private static AntiSpy.AntiSpyBehaviour _mono;

		private static Texture2D _spyPreview;

		private static readonly string SpyImageDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "gatyware", "spyimage");

		private const uint PAGE_EXECUTE_READWRITE = 64U;

		private static int _imgIdx;
		private static int _spyDepth;

		public class AntiSpyBehaviour : MonoBehaviour
		{
		}
	}
}
