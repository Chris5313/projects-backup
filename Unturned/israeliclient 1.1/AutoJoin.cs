using System;
using System.Net;
using System.Reflection;
using SDG.Unturned;
using UnityEngine;

namespace gatyware
{
	public static class AutoJoin
	{
		public static string StatusText = "Idle";

		private static float _nextAttempt;
		private static bool _wasConnected;

		// Reflected connect method — Provider.connect(SteamConnectionInfo info) or similar
		private static MethodInfo _connectMethod;
		private static bool _connectReflected;

		private static void ReflectConnect()
		{
			if (_connectReflected) return;
			_connectReflected = true;
			BindingFlags bf = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
			// Try several known signatures across Unturned versions
			_connectMethod = typeof(Provider).GetMethod("connect", bf);
			if (_connectMethod == null)
				_connectMethod = typeof(Provider).GetMethod("Connect", bf);
			Runtime.Trace("autojoin: connect=" + (_connectMethod != null));
		}

		public static void Update()
		{
			if (!State.AutoJoinOn) return;
			if (string.IsNullOrEmpty(State.AutoJoinIP)) { StatusText = "Set an IP first"; return; }

			// Track connection changes
			bool connected = Provider.isConnected;
			if (connected)
			{
				_wasConnected = true;
				// Check player limit while connected
				if (State.AutoJoinPlayerLimit && Provider.clients != null
					&& Provider.clients.Count > State.AutoJoinMaxPlayers)
				{
					Provider.disconnect();
					StatusText = "Too many players, retrying...";
					_nextAttempt = Time.realtimeSinceStartup + State.AutoJoinRetryDelay;
					return;
				}
				StatusText = "Connected  (" + (Provider.clients != null ? Provider.clients.Count : 0) + " players)";
				return;
			}

			// Not connected — wait for retry delay then attempt
			float now = Time.realtimeSinceStartup;
			float wait = _nextAttempt - now;
			if (wait > 0f)
			{
				StatusText = "Retrying in " + Mathf.CeilToInt(wait) + "s...";
				return;
			}

			// Attempt connection
			StatusText = "Connecting to " + State.AutoJoinIP + ":" + State.AutoJoinPort + "...";
			TryConnect();
			_nextAttempt = now + State.AutoJoinRetryDelay;
		}

		private static void TryConnect()
		{
			try
			{
				ReflectConnect();

				// Parse IP — use manual IPv4 parser to avoid IPAddress.TryParse's newer
				// TryParse(ReadOnlySpan<char>, out IPAddress) overload, which would force
				// the compiler to load System.ReadOnlySpan<T>. The Unturned-bundled
				// System.Runtime.dll v4.1.0.0 lacks ReadOnlySpan<T>'s type forwarder, so
				// MSBuild's ReferencePath priority (UnturnedManagedDir over framework facade)
				// makes CS0518 fire even with <LangVersion>latest</LangVersion>.
				uint ipUint = 0;
				if (!TryParseIPv4(State.AutoJoinIP, out ipUint))
				{
					// Try resolve hostname via Dns (no ReadOnlySpan overload exists here)
					IPAddress[] addrs;
					try { addrs = Dns.GetHostAddresses(State.AutoJoinIP); }
					catch { addrs = null; }
					if (addrs != null && addrs.Length > 0)
					{
						byte[] b = addrs[0].GetAddressBytes();
						if (b.Length == 4)
						{
							// Bit-pack into a single uint in network byte order (big-endian) so
							// Winsock's sin_addr.S_un.S_addr / steam's SteamConnectionInfo.ip gets
							// the right 32-bit pattern. byte[0] is the highest octet because
							// IPAddress.GetAddressBytes() returns network-order bytes.
							ipUint = ((uint)b[0] << 24) | ((uint)b[1] << 16) | ((uint)b[2] << 8) | b[3];
						}
					}
				}

				// Both literal-IPv4 parse and DNS resolve produced 0.0.0.0 — don't keep the
				// retry loop silently hammering Provider.connect(0.0.0.0); surface it in the UI
				// and bail out this attempt.
				if (ipUint == 0)
				{
					StatusText = "AutoJoin: could not parse '" + State.AutoJoinIP + "' as IP or hostname";
					return;
				}

				ushort port = (ushort)Mathf.Clamp(State.AutoJoinPort, 1, 65535);
				string password = State.AutoJoinPassword ?? "";

				if (_connectMethod != null)
				{
					ParameterInfo[] ps = _connectMethod.GetParameters();
					// Provider.connect(SteamConnectionInfo) — most common
					if (ps.Length == 1)
					{
						// Construct SteamConnectionInfo(uint ip, ushort port, string password)
						Type infoType = ps[0].ParameterType;
						object info = null;
						try
						{
							info = Activator.CreateInstance(infoType,
								new object[] { ipUint, port, password });
						}
						catch
						{
							// Some versions take ip as string
							try { info = Activator.CreateInstance(infoType,
								new object[] { State.AutoJoinIP, port, password }); }
							catch { }
						}
						if (info != null)
						{
							_connectMethod.Invoke(null, new object[] { info });
							return;
						}
					}
					// Provider.connect(uint ip, ushort port, string pw) — legacy signature only.
					// Modern Unturned uses connect(ServerConnectParameters, SteamServerAdvertisement,
					// List<PublishedFileId_t>) with complex Unturned-internal types we cannot construct
					// here; only match when ps[0]/ps[1] are simple numeric types.
					if (ps.Length == 3 &&
						(ps[0].ParameterType == typeof(uint) || ps[0].ParameterType == typeof(int)) &&
						(ps[1].ParameterType == typeof(ushort) || ps[1].ParameterType == typeof(int)))
					{
						_connectMethod.Invoke(null, new object[] {
							Convert.ChangeType(ipUint, ps[0].ParameterType),
							Convert.ChangeType(port,   ps[1].ParameterType),
							password
						});
						return;
					}

					// Final reflective fallback: invoke whatever connect signature exists with safe
					// defaults (e.g. modern connect(ServerConnectParameters, SteamServerAdvertisement,
					// List<PublishedFileId_t>)). Strings get the password, numeric value types get
					// ip/port, everything else gets zero / null. If this fails (e.g. Unturned
					// rejects the default-constructed types) we log and let the retry loop try again.
					try
					{
						ParameterInfo[] callPs = _connectMethod.GetParameters();
						object[] callArgs = new object[callPs.Length];
						for (int i = 0; i < callPs.Length; i++)
						{
							Type pt = callPs[i].ParameterType;
							if (pt == typeof(string)) callArgs[i] = password;
							else if (pt == typeof(uint) || pt == typeof(int)) callArgs[i] = ipUint;
							else if (pt == typeof(ushort)) callArgs[i] = port;
							else if (pt == typeof(short)) callArgs[i] = (short)port;
							else callArgs[i] = pt.IsValueType ? Activator.CreateInstance(pt) : null;
						}
						_connectMethod.Invoke(null, callArgs);
						return;
					}
					catch (Exception fallbackEx)
					{
						Runtime.Trace("autojoin fallback invoke err: " + fallbackEx.Message);
						StatusText = "AutoJoin failed: " + fallbackEx.Message;
					}
				}
				else
				{
					StatusText = "AutoJoin: connect method not found in this Unturned version";
				}
			}
			catch (Exception ex)
			{
				Runtime.Trace("autojoin connect err: " + ex.Message);
				StatusText = "Connect error: " + ex.Message;
			}
		}

		/// Called when AutoJoin is toggled off, to clean up
		public static void Stop()
		{
			StatusText = "Idle";
			_nextAttempt = 0f;
		}

		// Manual IPv4 parser — character-by-character, deliberately avoiding any
		// numeric/parse APIs that have a ReadOnlySpan<char> overload (uint.TryParse,
		// int.TryParse, byte.TryParse, int.Parse, etc.). When LangVersion=latest lets
		// the compiler bind the ReadOnlySpan overload, MSBuild asks for the
		// System.ReadOnlySpan<T> predefined type and Unturned's System.Runtime v4.1.0.0
		// doesn't have the forwarder, causing CS0518. Parsing IPv4 ourselves keeps us
		// inside string-only APIs and dodges the whole overload-resolution fight.
		private static bool TryParseOctet(string s, out uint value)
		{
			value = 0;
			if (string.IsNullOrEmpty(s) || s.Length > 3) return false;
			int n = 0;
			for (int i = 0; i < s.Length; i++)
			{
				char c = s[i];
				if (c < '0' || c > '9') return false;
				n = n * 10 + (c - '0');
			}
			if (n > 255) return false;
			value = (uint)n;
			return true;
		}

		private static bool TryParseIPv4(string s, out uint ipUint)
		{
			ipUint = 0;
			if (string.IsNullOrEmpty(s)) return false;
			string[] parts = s.Split('.');
			if (parts.Length != 4) return false;
			uint a, b, c, d;
			if (!TryParseOctet(parts[0], out a) ||
				!TryParseOctet(parts[1], out b) ||
				!TryParseOctet(parts[2], out c) ||
				!TryParseOctet(parts[3], out d))
				return false;
			ipUint = (a << 24) | (b << 16) | (c << 8) | d;
			return true;
		}
	}
}
