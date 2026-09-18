using System;
using System.Reflection;
using SDG.Unturned;
using UnityEngine;

namespace gatyware
{
    public static class EntityInspector
    {
        private static string _info = "";
        private static float _lastScan;
        private const float ScanInterval = 0.25f;

        public static void Update()
        {
            if (!State.EntityInspectorOn) return;
            if (Player.player == null) return;
            if (Time.time - _lastScan < ScanInterval) return;
            _lastScan = Time.time;

            try
            {
                Camera cam = (FreeCam.FreeCamCamera != null) ? FreeCam.FreeCamCamera : MainCamera.instance;
                if (cam == null) cam = Camera.main;
                if (cam == null) { _info = ""; return; }

                RaycastHit hit;
                if (!Physics.Raycast(new Ray(cam.transform.position, cam.transform.forward), out hit, 50f, RayMasks.BARRICADE_INTERACT | RayMasks.VEHICLE, QueryTriggerInteraction.Collide))
                {
                    _info = "";
                    return;
                }

                Transform t = hit.transform;
                if (t == null) { _info = ""; return; }

                InteractableVehicle veh = t.GetComponentInParent<InteractableVehicle>();
                if (veh != null)
                {
                    string owner = ResolveOwner(veh.lockedOwner.m_SteamID);
                    string group = ResolveGroup(veh.lockedGroup.m_SteamID);
                    _info = string.Format("Vehicle: {0} (ID {1})\nOwner: {2}\nGroup: {3}\nHealth: {4}/{5}\nLocked: {6}",
                        veh.asset != null ? veh.asset.vehicleName : "?",
                        veh.asset != null ? veh.asset.id.ToString() : "?",
                        owner, group,
                        veh.health, veh.asset != null ? veh.asset.health.ToString() : "?",
                        veh.isLocked ? "Yes" : "No");
                    return;
                }

                BarricadeDrop drop = FindBarricadeDrop(t);
                if (drop != null)
                {
                    BarricadeData data = GetBarricadeData(drop);
                    string name = drop.asset != null ? drop.asset.itemName : "?";
                    ushort id = drop.asset != null ? drop.asset.id : (ushort)0;
                    string owner = data != null ? ResolveOwner(data.owner) : "?";
                    string group = data != null ? ResolveGroup(data.group) : "?";
                    _info = string.Format("Barricade: {0} (ID {1})\nOwner: {2}\nGroup: {3}",
                        name, id, owner, group);
                    return;
                }

                _info = "";
            }
            catch
            {
                _info = "";
            }
        }


        private static BarricadeDrop FindBarricadeDrop(Transform t)
        {
            if (BarricadeManager.regions == null) return null;
            for (int i = 0; i < BarricadeManager.regions.GetLength(0); i++)
            {
                for (int j = 0; j < BarricadeManager.regions.GetLength(1); j++)
                {
                    BarricadeRegion region = BarricadeManager.regions[i, j];
                    if (region == null || region.drops == null) continue;
                    for (int k = 0; k < region.drops.Count; k++)
                    {
                        BarricadeDrop d = region.drops[k];
                        if (d != null && d.model != null && d.model == t)
                            return d;
                        if (d != null && d.model != null && t.IsChildOf(d.model))
                            return d;
                    }
                }
            }
            return null;
        }

        private static BarricadeData GetBarricadeData(BarricadeDrop drop)
        {
            try
            {
                FieldInfo fi = typeof(BarricadeDrop).GetField("serversideData",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (fi != null)
                    return fi.GetValue(drop) as BarricadeData;

                PropertyInfo pi = typeof(BarricadeDrop).GetProperty("serversideData",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (pi != null)
                    return pi.GetValue(drop, null) as BarricadeData;

                fi = typeof(BarricadeDrop).GetField("_data",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (fi != null)
                    return fi.GetValue(drop) as BarricadeData;
            }
            catch { }
            return null;
        }

        private static string ResolveOwner(ulong steamId)
        {
            if (steamId == 0UL) return "None";
            SteamPlayer sp = FindSteamPlayer(steamId);
            if (sp != null)
                return sp.playerID.characterName + " (" + steamId.ToString() + ")";
            return steamId.ToString();
        }

        private static string ResolveGroup(ulong groupId)
        {
            if (groupId == 0UL) return "None";
            return groupId.ToString();
        }

        private static SteamPlayer FindSteamPlayer(ulong steamId)
        {
            try
            {
                if (Provider.clients == null) return null;
                for (int i = 0; i < Provider.clients.Count; i++)
                {
                    // FIXED: Explicitly cast CSteamID to ulong
                    if ((ulong)Provider.clients[i].playerID.steamID.m_SteamID == steamId)
                        return Provider.clients[i];
                }
            }
            catch { }
            return null;
        }
    }
}