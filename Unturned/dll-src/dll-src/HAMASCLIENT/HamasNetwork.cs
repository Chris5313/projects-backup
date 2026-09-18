using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using SDG.Unturned;
using UnityEngine;

/// <summary>
/// Hamas Network - tracks online Hamas users via VPS API.
/// Periodically fetches the list of online Steam IDs and marks them in PlayerPriorityManager.
/// </summary>
public static class HamasNetwork
{
	private static readonly string TokenPath = @"C:\ProgramData\Microsoft\DeviceSync\session.txt";
	private static readonly string GhostPath = @"C:\ProgramData\Microsoft\DeviceSync\ghost.txt";
	private static readonly string ApiUrl = "https://israeliclient.xyz/online";

	private static string _sessionToken = null;
	private static bool _ghostMode = false;
	private static HashSet<ulong> _onlineUsers = new HashSet<ulong>();
	private static Dictionary<ulong, string> _onlineNames = new Dictionary<ulong, string>();
	private static HashSet<ulong> _inGameUsers = new HashSet<ulong>(); // DLL currently injected + connected
	private static string _displayName = null; // user's chosen name (null = never set)
	private static readonly object _lock = new object();
	private static Thread _pollThread = null;
	private static bool _running = false;
	private static DateTime _lastFetch = DateTime.MinValue;
	private static Thread _heartbeatThread = null;
	private static System.Reflection.PropertyInfo _advProp = null;

	public static bool Enabled { get; private set; } = false;
	public static bool GhostMode => _ghostMode;
	public static int OnlineCount { get { lock (_lock) { return _onlineUsers.Count; } } }
	public static int InGameCount { get { lock (_lock) { return _inGameUsers.Count; } } }
	/// <summary>Chosen cloud username. Null = not chosen yet, "" = unknown (fetch pending).</summary>
	public static string DisplayName { get { lock (_lock) { return _displayName; } } }
	/// <summary>Is this steam id's DLL currently injected in-game (heartbeat alive)?</summary>
	public static bool IsInGame(ulong steamId)
	{
		lock (_lock) { return _inGameUsers.Contains(steamId); }
	}

	[InitializeAttribute]
	public static void Initialize()
	{
		// Read session token
		try
		{
			if (File.Exists(TokenPath))
			{
				_sessionToken = File.ReadAllText(TokenPath).Trim();
				if (!string.IsNullOrEmpty(_sessionToken))
				{
					Enabled = true;
					Logger.LogClient("HamasNetwork: Token loaded, network features enabled");
				}
			}
		}
		catch (Exception ex)
		{
			Logger.LogClient("HamasNetwork: Failed to read token - " + ex.Message);
		}

		// Read ghost mode flag
		try
		{
			if (File.Exists(GhostPath))
			{
				string content = File.ReadAllText(GhostPath).Trim();
				_ghostMode = content == "1";
				if (_ghostMode)
					Logger.LogClient("HamasNetwork: Ghost mode active");
			}
		}
		catch { }


		// Start polling thread
		if (Enabled)
		{
			_running = true;
			_pollThread = new Thread(PollLoop);
			_pollThread.IsBackground = true;
			_pollThread.Start();
			_heartbeatThread = new Thread(HeartbeatLoop);
			_heartbeatThread.IsBackground = true;
			_heartbeatThread.Start();
			FetchMe();
		}
	}

	public static void Shutdown()
	{
		_running = false;
		if (_pollThread != null && _pollThread.IsAlive)
		{
			_pollThread.Join(2000);
		}
	}

	private static void PollLoop()
	{
		while (_running)
		{
			try
			{
				FetchOnlineUsers();
			}
			catch (Exception ex)
			{
				Logger.LogClient("HamasNetwork: Fetch error - " + ex.Message);
			}

			// Poll every 30 seconds
			for (int i = 0; i < 30 && _running; i++)
				Thread.Sleep(1000);
		}
	}

	/// <summary>
	/// POST /api/ig every 30s while connected to a server. Server keeps the flag
	/// alive 90s, so users whose DLL stopped sending it drop off the in-game list.
	/// </summary>
	private static void HeartbeatLoop()
	{
		while (_running)
		{
			try
			{
				if (Provider.isConnected && Provider.clients != null)
					SendHeartbeat();
			}
			catch (Exception ex)
			{
				Logger.LogClient("HamasNetwork: heartbeat error - " + ex.Message);
			}
			for (int i = 0; i < 30 && _running; i++)
				Thread.Sleep(1000);
		}
	}

	private static void SendHeartbeat()
	{
		try
		{
			string srv = "unknown";
			try
			{
				// CurrentServerAdvertisement: internal getter; use reflection (cached)
				System.Reflection.PropertyInfo pi = _advProp ?? (_advProp = typeof(Provider).GetProperty(
					"CurrentServerAdvertisement", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public));
				object adv = pi != null ? pi.GetValue(null, null) : null;
				if (adv != null)
				{
					System.Reflection.PropertyInfo pIp = adv.GetType().GetProperty("ip");
					System.Reflection.PropertyInfo pPort = adv.GetType().GetProperty("port");
					string ipStr = pIp != null ? pIp.GetValue(adv, null) as string : null;
					object portVal = pPort != null ? pPort.GetValue(adv, null) : null;
					if (!string.IsNullOrEmpty(ipStr) && portVal != null)
						srv = ipStr + ":" + portVal;
				}
			}
			catch { /* cosmetic only */ }
			string jsonBody = "{\"srv\":\"" + srv.Replace("\\", "").Replace("\"", "") + "\"}";
			byte[] bodyBytes = System.Text.Encoding.UTF8.GetBytes(jsonBody);
			HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://israeliclient.xyz/api/ig");
			req.Method = "POST";
			req.Headers.Add("X-Session-Token", _sessionToken);
			req.ContentType = "application/json";
			req.ContentLength = bodyBytes.Length;
			req.Timeout = 10000;
			using (Stream reqStream = req.GetRequestStream())
				reqStream.Write(bodyBytes, 0, bodyBytes.Length);
			using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse()) { }
		}
		catch (WebException)
		{
			// transient network failure - next cycle retries; server flag TTL is 3x the interval
		}
	}

	private static void FetchOnlineUsers()
	{
		if (string.IsNullOrEmpty(_sessionToken)) return;

		try
		{
			HttpWebRequest req = (HttpWebRequest)WebRequest.Create(ApiUrl);
			req.Method = "GET";
			req.Headers.Add("X-Session-Token", _sessionToken);
			req.Timeout = 10000;

			using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
			using (StreamReader reader = new StreamReader(resp.GetResponseStream()))
			{
				string json = reader.ReadToEnd();
				ParseOnlineUsers(json);
				_lastFetch = DateTime.Now;
			}
		}
		catch (WebException ex)
		{
			HttpWebResponse httpResp = ex.Response as HttpWebResponse;
			if (httpResp != null)
			{
				if (httpResp.StatusCode == HttpStatusCode.Unauthorized)
				{
					// Token expired or invalid
					Enabled = false;
					_running = false;
					Logger.LogClient("HamasNetwork: Session expired");
				}
			}
		}
	}

	private static void ParseOnlineUsers(string json)
	{
		// New format: {"online":[{"s":"76561198...","name":"chosen"}]}
		// Old format: {"online":["76561198..."]} (server < usernames patch)
		HashSet<ulong> newSet = new HashSet<ulong>();
		Dictionary<ulong, string> newNames = new Dictionary<ulong, string>();
		HashSet<ulong> inGameSet = new HashSet<ulong>();

		int start = json.IndexOf("[");
		int end = json.LastIndexOf("]");
		if (start < 0 || end < 0 || end <= start) return;

		string arr = json.Substring(start + 1, end - start - 1);
		int pos = 0;
		while (pos < arr.Length)
		{
			int objStart = arr.IndexOf('{', pos);
			if (objStart < 0) break; // old format or empty
			int objEnd = arr.IndexOf('}', objStart);
			if (objEnd < 0) break;
			string obj = arr.Substring(objStart, objEnd - objStart + 1);
			ulong sid;
			if (ulong.TryParse(ExtractJsonString(obj, "s"), out sid) && sid != 0)
			{
				newSet.Add(sid);
				string nm = ExtractJsonString(obj, "name");
				if (!string.IsNullOrEmpty(nm)) newNames[sid] = nm;
				int igVal;
				if (int.TryParse(ExtractJsonString(obj, "ig"), out igVal) && igVal == 1) inGameSet.Add(sid);
			}
			pos = objEnd + 1;
		}

		// Old format fallback: bare steam id strings
		if (newSet.Count == 0)
		{
			string[] parts = arr.Split(',');
			foreach (string part in parts)
			{
				string cleaned = part.Trim().Trim('"');
				ulong steamId;
				if (ulong.TryParse(cleaned, out steamId)) newSet.Add(steamId);
			}
		}

		lock (_lock)
		{
			// Update PlayerPriorityManager for new/removed users
			foreach (ulong id in newSet)
			{
				if (!_onlineUsers.Contains(id))
				{
					// New Hamas user online: auto-mark, but never override a manual Friend/Enemy
					PlayerRelation cur = PlayerPriorityManager.GetPriority(id);
					if (cur == PlayerRelation.Default || cur == PlayerRelation.GroupMate || cur == PlayerRelation.HamasUser)
						PlayerPriorityManager.SetPriority(id, PlayerRelation.HamasUser);
				}
			}

			foreach (ulong id in _onlineUsers)
			{
				if (!newSet.Contains(id))
				{
					// Hamas user went offline - drop the auto-mark, but keep manual Friend/Enemy
					if (PlayerPriorityManager.GetPriority(id) == PlayerRelation.HamasUser)
					{
						PlayerPriorityManager.SetPriority(id, PlayerRelation.Default);
					}
				}
			}

			_onlineUsers = newSet;
			_onlineNames = newNames;
			_inGameUsers = inGameSet;
		}
	}

	/// <summary>
	/// Check if a Steam ID belongs to an online Hamas user
	/// </summary>
	public static bool IsHamasUser(ulong steamId)
	{
		lock (_lock)
		{
			return _onlineUsers.Contains(steamId);
		}
	}

	/// <summary>
	/// Get all online Hamas user Steam IDs
	/// </summary>
	public static List<ulong> GetOnlineUsers()
	{
		lock (_lock)
		{
			return new List<ulong>(_onlineUsers);
		}
	}

	/// <summary>Chosen username of an online user (fallback: null).</summary>
	public static string GetOnlineName(ulong steamId)
	{
		lock (_lock)
		{
			string nm;
			return _onlineNames.TryGetValue(steamId, out nm) ? nm : null;
		}
	}

	// ═══════════ USERNAME (/api/me + /api/name) ═══════════

	private static void FetchMe()
	{
		ThreadPool.QueueUserWorkItem(_ =>
		{
			try
			{
				HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://israeliclient.xyz/api/me");
				req.Method = "GET";
				req.Headers.Add("X-Session-Token", _sessionToken);
				req.Timeout = 10000;
				using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
				using (StreamReader reader = new StreamReader(resp.GetResponseStream()))
				{
					string json = reader.ReadToEnd();
					string nm = ExtractJsonString(json, "name");
					lock (_lock) { _displayName = string.IsNullOrEmpty(nm) ? null : nm; }
				}
			}
			catch (Exception ex)
			{
				Logger.LogClient("HamasNetwork: /api/me failed - " + ex.Message);
			}
		});
	}

	/// <summary>
	/// Choose/change this user's cloud username (3-16 chars, letters/digits/_/-).
	/// Server enforces uniqueness per account and backfills config authorship.
	/// </summary>
	public static void SetUsername(string name, Action<bool, string> callback)
	{
		if (string.IsNullOrEmpty(_sessionToken))
		{
			callback?.Invoke(false, "Not authenticated");
			return;
		}
		ThreadPool.QueueUserWorkItem(_ =>
		{
			try
			{
				string jsonBody = "{\"name\":\"" + (name ?? "").Trim() + "\"}";
				byte[] bodyBytes = System.Text.Encoding.UTF8.GetBytes(jsonBody);
				HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://israeliclient.xyz/api/name");
				req.Method = "POST";
				req.Headers.Add("X-Session-Token", _sessionToken);
				req.ContentType = "application/json";
				req.ContentLength = bodyBytes.Length;
				req.Timeout = 10000;
				using (Stream reqStream = req.GetRequestStream())
					reqStream.Write(bodyBytes, 0, bodyBytes.Length);
				using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
				{
					lock (_lock) { _displayName = name.Trim(); }
					Logger.LogUser("[+] Username set: " + name);
					callback?.Invoke(true, "");
				}
			}
			catch (WebException ex)
			{
				string error = "Failed";
				if (ex.Response is HttpWebResponse hr)
				{
					int code = (int)hr.StatusCode;
					if (code == 409) error = "Name already taken";
					else if (code == 400)
					{
						error = "Invalid name";
						try
						{
							using (StreamReader r = new StreamReader(hr.GetResponseStream()))
							{
								string body = r.ReadToEnd();
								int q = body.IndexOf("\"error\":\"");
								if (q >= 0)
								{
									int s = q + 9;
									int e2 = body.IndexOf('"', s);
									if (e2 > s) error = body.Substring(s, e2 - s);
								}
							}
						}
						catch { }
					}
				}
				callback?.Invoke(false, error);
			}
			catch (Exception ex)
			{
				callback?.Invoke(false, ex.Message);
			}
		});
	}

	// ═══════════ CLOUD CONFIGS (VPS) ═══════════
	
	public class CloudConfigInfo
	{
		public string Name;
		public string Author;
		public string Description;
		public int Downloads;
		public string Created;
		public int Size;
		public int Own; // 1 = owned by this session's steamId (server flag)
	}
	
	private static List<CloudConfigInfo> _cloudConfigs = new List<CloudConfigInfo>();
	private static DateTime _lastConfigFetch = DateTime.MinValue;
	
	public static List<CloudConfigInfo> CloudConfigs => _cloudConfigs;
	public static object ConfigListLock => _lock; // same lock as internal list mutations
	public static bool IsFetchingConfigs { get; private set; } = false;
	public static string LastError { get; private set; } = "";
	public static bool IsAuthenticated => !string.IsNullOrEmpty(_sessionToken);
	
	/// <summary>
	/// Fetch list of available cloud configs
	/// </summary>
	public static void RefreshCloudConfigs()
	{
		if (IsFetchingConfigs) return;
		
		ThreadPool.QueueUserWorkItem(_ =>
		{
			IsFetchingConfigs = true;
			try
			{
				HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://israeliclient.xyz/api/configs");
				req.Method = "GET";
				req.Timeout = 10000;
				// Token needed so the server flags configs this user owns (enables Delete)
				if (!string.IsNullOrEmpty(_sessionToken))
					req.Headers.Add("X-Session-Token", _sessionToken);
				
				using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
				using (StreamReader reader = new StreamReader(resp.GetResponseStream()))
				{
					string json = reader.ReadToEnd();
					ParseCloudConfigs(json);
					_lastConfigFetch = DateTime.Now;
					LastError = "";
				}
			}
			catch (Exception ex)
			{
				LastError = ex.Message;
				Logger.LogClient("HamasNetwork: Failed to fetch configs - " + ex.Message);
			}
			finally
			{
				IsFetchingConfigs = false;
			}
		});
	}
	
	private static void ParseCloudConfigs(string json)
	{
		List<CloudConfigInfo> newList = new List<CloudConfigInfo>();
		
		int arrStart = json.IndexOf("[");
		int arrEnd = json.LastIndexOf("]");
		if (arrStart < 0 || arrEnd <= arrStart) return;
		
		string arr = json.Substring(arrStart + 1, arrEnd - arrStart - 1);
		
		int pos = 0;
		while (pos < arr.Length)
		{
			int objStart = arr.IndexOf('{', pos);
			if (objStart < 0) break;
			int objEnd = arr.IndexOf('}', objStart);
			if (objEnd < 0) break;
			
			string obj = arr.Substring(objStart, objEnd - objStart + 1);
			CloudConfigInfo info = new CloudConfigInfo();
			
			info.Name = ExtractJsonString(obj, "name");
			info.Author = ExtractJsonString(obj, "author");
			info.Description = ExtractJsonString(obj, "description");
			info.Created = ExtractJsonString(obj, "created");
			int.TryParse(ExtractJsonString(obj, "downloads"), out info.Downloads);
			int.TryParse(ExtractJsonString(obj, "size"), out info.Size);
			int.TryParse(ExtractJsonString(obj, "own"), out info.Own);
			
			if (!string.IsNullOrEmpty(info.Name))
				newList.Add(info);
			
			pos = objEnd + 1;
		}
		
		lock (_lock)
		{
			_cloudConfigs = newList;
		}
	}
	
	private static string ExtractJsonString(string json, string key)
	{
		string search = "\"" + key + "\":";
		int idx = json.IndexOf(search);
		if (idx < 0) return "";
		
		int start = idx + search.Length;
		while (start < json.Length && (json[start] == ' ' || json[start] == '"')) start++;
		if (start >= json.Length) return "";
		
		if (char.IsDigit(json[start]))
		{
			int end = start;
			while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '.')) end++;
			return json.Substring(start, end - start);
		}
		
		start--;
		if (json[start] != '"') return "";
		start++;
		int endQuote = json.IndexOf('"', start);
		if (endQuote < 0) return "";
		return json.Substring(start, endQuote - start);
	}
	
	/// <summary>
	/// Download a cloud config and save it locally
	/// </summary>
	public static void DownloadConfig(string name, Action<bool, string> callback)
	{
		if (string.IsNullOrEmpty(_sessionToken))
		{
			callback?.Invoke(false, "Not authenticated");
			return;
		}
		
		ThreadPool.QueueUserWorkItem(_ =>
		{
			try
			{
				HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://israeliclient.xyz/api/configs/" + Uri.EscapeDataString(name));
				req.Method = "GET";
				req.Headers.Add("X-Session-Token", _sessionToken);
				req.Timeout = 30000;
				
				using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
				using (Stream stream = resp.GetResponseStream())
				using (MemoryStream ms = new MemoryStream())
				{
					stream.CopyTo(ms);
					byte[] data = ms.ToArray();
					
					string localPath = UnityEngine.Application.dataPath + "/configs/" + name + ".conf";
					string dir = Path.GetDirectoryName(localPath);
					if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
					File.WriteAllBytes(localPath, data);
					
					Logger.LogUser("[+] Downloaded cloud config: " + name);
					callback?.Invoke(true, localPath);
				}
			}
			catch (WebException ex)
			{
				string error = "Download failed";
				if (ex.Response is HttpWebResponse hr)
				{
					if (hr.StatusCode == HttpStatusCode.NotFound) error = "Config not found";
					else if (hr.StatusCode == HttpStatusCode.Unauthorized) error = "Session expired";
				}
				Logger.LogUser("[-] " + error + ": " + name);
				callback?.Invoke(false, error);
			}
			catch (Exception ex)
			{
				Logger.LogUser("[-] Download failed: " + ex.Message);
				callback?.Invoke(false, ex.Message);
			}
		});
	}
	
	/// <summary>
	/// Upload config to cloud
	/// </summary>
	public static void UploadConfig(string name, string description, byte[] configData, Action<bool, string> callback)
	{
		if (string.IsNullOrEmpty(_sessionToken))
		{
			callback?.Invoke(false, "Not authenticated - restart loader");
			return;
		}
		
		ThreadPool.QueueUserWorkItem(_ =>
		{
			try
			{
				string base64Data = Convert.ToBase64String(configData);
				string jsonBody = "{\"name\":\"" + name.Replace("\"", "") + "\",\"description\":\"" + description.Replace("\"", "'") + "\",\"data\":\"" + base64Data + "\"}";
				byte[] bodyBytes = System.Text.Encoding.UTF8.GetBytes(jsonBody);
				
				HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://israeliclient.xyz/api/configs");
				req.Method = "POST";
				req.Headers.Add("X-Session-Token", _sessionToken);
				req.ContentType = "application/json";
				req.ContentLength = bodyBytes.Length;
				req.Timeout = 30000;
				
				using (Stream reqStream = req.GetRequestStream())
				{
					reqStream.Write(bodyBytes, 0, bodyBytes.Length);
				}
				
				using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
				{
					Logger.LogUser("[+] Uploaded config: " + name);
					callback?.Invoke(true, "");
				}
			}
			catch (WebException ex)
			{
				string error = "Upload failed";
				if (ex.Response is HttpWebResponse hr)
				{
					int code = (int)hr.StatusCode;
					using (StreamReader r = new StreamReader(hr.GetResponseStream()))
					{
						string body = r.ReadToEnd();
						if (code == 401) error = "Session expired - restart loader";
						else if (code == 403) error = "Name taken by another user";
						else if (body.Contains("too large")) error = "Config too large (max 1MB)";
						else error = "HTTP " + code;
					}
				}
				Logger.LogUser("[-] " + error);
				callback?.Invoke(false, error);
			}
			catch (Exception ex)
			{
				Logger.LogUser("[-] Upload failed: " + ex.Message);
				callback?.Invoke(false, ex.Message);
			}
		});
	}
	
	/// <summary>
	/// Delete own config from cloud
	/// </summary>
	public static void DeleteCloudConfig(string name, Action<bool, string> callback)
	{
		if (string.IsNullOrEmpty(_sessionToken))
		{
			callback?.Invoke(false, "Not authenticated");
			return;
		}
		
		ThreadPool.QueueUserWorkItem(_ =>
		{
			try
			{
				HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://israeliclient.xyz/api/configs/" + Uri.EscapeDataString(name));
				req.Method = "DELETE";
				req.Headers.Add("X-Session-Token", _sessionToken);
				req.Timeout = 10000;
				
				using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
				{
					Logger.LogUser("[+] Deleted cloud config: " + name);
					callback?.Invoke(true, "");
				}
			}
			catch (WebException ex)
			{
				string error = "Delete failed";
				if (ex.Response is HttpWebResponse hr)
				{
					if (hr.StatusCode == HttpStatusCode.Forbidden) error = "Not your config";
					else if (hr.StatusCode == HttpStatusCode.NotFound) error = "Config not found";
				}
				callback?.Invoke(false, error);
			}
			catch (Exception ex)
			{
				callback?.Invoke(false, ex.Message);
			}
		});
	}
}
