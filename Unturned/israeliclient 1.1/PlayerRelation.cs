using System;
using System.Collections.Generic;
using SDG.Unturned;

namespace gatyware
{
	public static class PlayerRelation
	{
		private static string Key(SteamPlayer sp)
		{
			return sp.playerID.playerName + "|" + sp.playerID.characterName;
		}

		public static RelationType Get(SteamPlayer sp)
		{
			if (sp == null)
			{
				return RelationType.Default;
			}
			RelationType result;
			if (!PlayerRelation._map.TryGetValue(PlayerRelation.Key(sp), out result))
			{
				return RelationType.Default;
			}
			return result;
		}

		public static void Set(SteamPlayer sp, RelationType r)
		{
			if (sp == null)
			{
				return;
			}
			PlayerRelation._map[PlayerRelation.Key(sp)] = r;
		}

		public static bool IsFriend(SteamPlayer sp)
		{
			return PlayerRelation.Get(sp) == RelationType.Friend;
		}

		public static bool IsEnemy(SteamPlayer sp)
		{
			return PlayerRelation.Get(sp) == RelationType.Enemy;
		}

		public static RelationType Get(Player p)
		{
			bool flag;
			if (p == null)
			{
				flag = (null != null);
			}
			else
			{
				SteamChannel channel = p.channel;
				flag = (((channel != null) ? channel.owner : null) != null);
			}
			if (!flag)
			{
				return RelationType.Default;
			}
			return PlayerRelation.Get(p.channel.owner);
		}

		public static bool IsFriend(Player p)
		{
			return PlayerRelation.Get(p) == RelationType.Friend;
		}

		public static bool IsEnemy(Player p)
		{
			return PlayerRelation.Get(p) == RelationType.Enemy;
		}

		public static void Clear()
		{
			PlayerRelation._map.Clear();
		}

		public static Dictionary<string, RelationType> GetAll()
		{
			return PlayerRelation._map;
		}

		public static void SetByKey(string key, RelationType r)
		{
			PlayerRelation._map[key] = r;
		}

		private static Dictionary<string, RelationType> _map = new Dictionary<string, RelationType>();
	}
}
