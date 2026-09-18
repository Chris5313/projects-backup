using System;
using System.Reflection;
using System.Linq;
using SDG.Unturned;
using UnityEngine;

namespace gatyware
{
    public static class AutoFish
    {
        // Resolves either a plain field, an auto-property, or the auto-property backing field.
        // Reused for both the rod's state booleans AND PlayerEquipment's press fields, since
        // Unturned has moved fields -> auto-properties for both in different builds.
        private class Member
        {
            public FieldInfo Field;
            public PropertyInfo Property;
            public bool HasMember { get { return Field != null || Property != null; } }

            public bool GetBool(object instance)
            {
                try
                {
                    if (Field != null) return (bool)Field.GetValue(instance);
                    if (Property != null) return (bool)Property.GetValue(instance, null);
                }
                catch (Exception ex) { Runtime.Trace("autofish GetBool err: " + ex.Message); }
                return false;
            }

            public void SetBool(object instance, bool val)
            {
                try
                {
                    if (Field != null) { Field.SetValue(instance, val); return; }
                    if (Property != null && Property.CanWrite) { Property.SetValue(instance, val, null); return; }
                }
                catch (Exception ex) { Runtime.Trace("autofish SetBool err: " + ex.Message); }
            }
        }

        private static bool _reflected;
        private static Member _isBobbing = new Member();
        private static Member _isCast = new Member();
        private static Member _isReeling = new Member();

        // PlayerEquipment input simulation — now resolved with the same Property-aware logic
        private static Member _primaryPressed = new Member();
        private static Member _lastPrimaryPressed = new Member();

        // PlayerInput simulation — the actual path Unturned uses
        private static MethodInfo _simulateMethod;  // PlayerEquipment.simulate(uint sim, bool primary, bool secondary)

        private static float _lastCast;
        private static float _lastReel;
        private static bool _pressingNow;
        private static float _pressStart;
        private const float HOLD = 0.3f; // hold 300ms — covers multiple 50ms sim ticks

        public static string StatusText = "Idle";

        private static void ResolveMember(Type type, string name, Member m, BindingFlags bf)
        {
            // 1. Modern auto-property
            m.Property = type.GetProperty(name, bf);
            if (m.Property != null) return;
            // 2. Auto-property backing field (e.g. <isCast>k__BackingField)
            m.Field = type.GetField("<" + name + ">k__BackingField", bf);
            if (m.Field != null) return;
            // 3. Plain field in older code ("isCast")
            m.Field = type.GetField(name, bf);
            if (m.Field != null) return;
            // 4. Underscore-prefixed variant ("_isCast")
            m.Field = type.GetField("_" + name, bf);
            if (m.Field != null) return;
            // 5. Underscore variant as auto-property ("_isCast" property, rare but seen)
            m.Property = type.GetProperty("_" + name, bf);
        }

        private static void Reflect()
        {
            if (_reflected) return;
            _reflected = true;
            BindingFlags bf = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            // Try multiple assembly names for UseableFishingRod in case it's moved
            Type rodType = typeof(Provider).Assembly.GetType("SDG.Unturned.UseableFishingRod");
            if (rodType == null)
                rodType = typeof(Provider).Assembly.GetType("SDG.Unturned.UseableFishingRod");
            if (rodType == null)
                rodType = Assembly.GetExecutingAssembly().GetType("SDG.Unturned.UseableFishingRod");
            if (rodType == null)
            {
                // Search all loaded assemblies for the type
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    rodType = asm.GetType("SDG.Unturned.UseableFishingRod");
                    if (rodType != null) break;
                    rodType = asm.GetType("UseableFishingRod");
                    if (rodType != null) break;
                }
            }
            if (rodType == null) { Runtime.Trace("autofish: rod type not found in any assembly"); return; }

            ResolveMember(rodType, "isBobbing", _isBobbing, bf);
            ResolveMember(rodType, "isCast", _isCast, bf);
            ResolveMember(rodType, "isReeling", _isReeling, bf);

            // Try alternative field names that might exist in different versions
            if (!_isBobbing.HasMember) ResolveMember(rodType, "bobbing", _isBobbing, bf);
            if (!_isCast.HasMember) ResolveMember(rodType, "cast", _isCast, bf);
            if (!_isReeling.HasMember) ResolveMember(rodType, "reeling", _isReeling, bf);

            // Resolve press fields the same property-aware way instead of GetField-only.
            ResolveMember(typeof(PlayerEquipment), "primaryPressed", _primaryPressed, bf);
            ResolveMember(typeof(PlayerEquipment), "lastPrimaryPressed", _lastPrimaryPressed, bf);

            // Look for the exact overload: simulate(uint, bool, bool) — first-match-by-name
            // was binding to the wrong overload when multiple `simulate` methods exist.
            foreach (MethodInfo m in typeof(PlayerEquipment).GetMethods(bf))
            {
                if (m.Name != "simulate") continue;
                ParameterInfo[] ps = m.GetParameters();
                if (ps.Length == 3
                    && ps[0].ParameterType == typeof(uint)
                    && ps[1].ParameterType == typeof(bool)
                    && ps[2].ParameterType == typeof(bool))
                {
                    _simulateMethod = m;
                    break;
                }
            }
            // Fall back to a looser match only if the exact one wasn't found.
            if (_simulateMethod == null)
            {
                foreach (MethodInfo m in typeof(PlayerEquipment).GetMethods(bf))
                {
                    if (m.Name != "simulate") continue;
                    ParameterInfo[] ps = m.GetParameters();
                    if (ps.Length >= 2) { _simulateMethod = m; break; }
                }
            }

            Runtime.Trace("autofish reflect done: bob=" + _isBobbing.HasMember
                + " (field=" + (_isBobbing.Field != null) + " prop=" + (_isBobbing.Property != null) + ")"
                + " cast=" + _isCast.HasMember
                + " (field=" + (_isCast.Field != null) + " prop=" + (_isCast.Property != null) + ")"
                + " reel=" + _isReeling.HasMember
                + " (field=" + (_isReeling.Field != null) + " prop=" + (_isReeling.Property != null) + ")"
                + " simulate=" + (_simulateMethod != null
                    ? (_simulateMethod.GetParameters().Length + " args")
                    : "NOT FOUND")
                + " pri=" + (_primaryPressed.HasMember + " (field=" + (_primaryPressed.Field != null) + " prop=" + (_primaryPressed.Property != null) + ")")
                + " lastPri=" + (_lastPrimaryPressed.HasMember));
        }

        public static void Update()
        {
            if (!State.AutoFishOn || State.IsSpying) return;
            Player player = Player.LocalPlayer;
            if (player == null || player.equipment == null) return;
            Useable useable = player.equipment.useable;
            
            // Auto-equip fishing rod if not holding one
            if (useable == null || !useable.GetType().Name.Contains("FishingRod"))
            {
                if (TryEquipFishingRod(player))
                {
                    StatusText = "Please equip fishing rod manually";
                    return;
                }
                StatusText = "No fishing rod in inventory";
                if (_pressingNow) EndPress(player);
                _lastCast = 0f;
                return;
            }

            Reflect();
            float now = Time.realtimeSinceStartup;

            // Re-issue the held-press every frame until HOLD expires, then formally release.
            // The previous code only called simulate ONCE and held via SetPressed(field=true),
            // which doesn't generate the rising-edge the rod's cast tick listens for — so
            // the cast never registered. Per-frame simulate(true) keeps the press visible to
            // PlayerInput.update() so the primaryRisingEdge / primaryReleased logic fires.
            if (_pressingNow)
            {
                if (now - _pressStart >= HOLD)
                    EndPress(player);
                else
                    HoldPress(player);
                // Don't return — still read state below
            }

            bool isCast = _isCast.HasMember && _isCast.GetBool(useable);
            bool isBobbing = _isBobbing.HasMember && _isBobbing.GetBool(useable);
            bool isReeling = _isReeling.HasMember && _isReeling.GetBool(useable);

            if (isReeling)
            {
                StatusText = "Reeling...";
                _lastCast = now - 1.5f; // recast soon after reel finishes
                return;
            }

            if (!isCast)
            {
                float elapsed = now - _lastCast;
                if (elapsed >= 2f)
                {
                    if (!_pressingNow)
                    {
                        StartPress(player);
                        _lastCast = now;
                        StatusText = "Casting...";
                    }
                }
                else
                {
                    StatusText = "Casting in " + Mathf.CeilToInt(2f - elapsed) + "s...";
                }
                return;
            }

            if (isBobbing)
            {
                float elapsed = now - _lastReel;
                if (elapsed >= State.AutoFishReelDelay)
                {
                    if (!_pressingNow)
                    {
                        StartPress(player);
                        _lastReel = now;
                        StatusText = "Fish on! Reeling!";
                    }
                }
                else
                {
                    StatusText = "Fish on!";
                }
                return;
            }

            StatusText = "Waiting for bite...";
        }

        private static bool TryEquipFishingRod(Player player)
        {
            try
            {
                if (player.inventory == null) return false;
                
                // Search all inventory pages for fishing rod
                for (byte page = 0; page < PlayerInventory.PAGES; page++)
                {
                    Items items = player.inventory.items[(int)page];
                    if (items == null) continue;
                    
                    for (byte idx = 0; idx < items.getItemCount(); idx++)
                    {
                        ItemJar jar = items.getItem(idx);
                        if (jar?.item == null) continue;
                        
                        ItemAsset asset = Assets.find(EAssetType.ITEM, jar.item.id) as ItemAsset;
                        if (asset == null) continue;
                        
                        // Check if this is a fishing rod item
                        if (asset.itemName.ToLower().Contains("fishing") || asset.itemName.ToLower().Contains("rod"))
                        {
                            // Found fishing rod - return true to indicate it exists
                            // Note: Auto equip disabled due to API compatibility issues
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Runtime.Trace("AutoFish equip err: " + ex.Message);
            }
            return false;
        }


        // Begin a press cycle — fires the rising edge (primary false -> true) on the first
        // simulate() call. Subsequent HoldPress calls keep primary=true across frames.
        private static void StartPress(Player player)
        {
            bool simOk = ApplySimulate(player, true, false);
            if (!simOk)
            {
                // Fallback: write primaryPressed directly with edge-detection reset.
                _lastPrimaryPressed.SetBool(player.equipment, false);
                _primaryPressed.SetBool(player.equipment, true);
            }
            _pressingNow = true;
            _pressStart = Time.realtimeSinceStartup;
            StatusText = simOk ? "Pressing (sim)..." : (_primaryPressed.HasMember ? "Pressing (field)..." : "No input path");
        }

        // Continue holding the primary button mid-press — necessary because Unturned's
        // PlayerInput.update() only reads simulate() output for that frame.
        private static void HoldPress(Player player)
        {
            ApplySimulate(player, true, false);
        }

        // Release the press — fires the falling edge so the rod starts the cast animation.
        private static void EndPress(Player player)
        {
            bool simOk = ApplySimulate(player, false, false);
            if (!simOk && _primaryPressed.HasMember)
                _primaryPressed.SetBool(player.equipment, false);
            _pressingNow = false;
        }

        // Invoke PlayerEquipment.simulate(uint, primary, secondary[, ...]) reflectively.
        // Returns true on a successful invoke, false if no method or all overloads fail.
        private static bool ApplySimulate(Player player, bool primary, bool secondary)
        {
            if (_simulateMethod == null) return false;
            try
            {
                ParameterInfo[] ps = _simulateMethod.GetParameters();
                if (ps.Length == 3)
                {
                    _simulateMethod.Invoke(player.equipment, new object[] { (uint)Time.frameCount, primary, secondary });
                    return true;
                }
                if (ps.Length == 2)
                {
                    _simulateMethod.Invoke(player.equipment, new object[] { (uint)Time.frameCount, primary });
                    return true;
                }
                if (ps.Length >= 4)
                {
                    object[] args = new object[ps.Length];
                    args[0] = (uint)Time.frameCount;
                    args[1] = primary;
                    args[2] = secondary;
                    for (int i = 3; i < args.Length; i++) args[i] = false;
                    _simulateMethod.Invoke(player.equipment, args);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Runtime.Trace("autofish simulate() invoke failed: " + ex.Message);
            }
            return false;
        }

        // Kept for back-compat with any external callers; prefer EndPress + ApplySimulate.
        private static void ReleasePress(Player player)
        {
            EndPress(player);
        }
    }
}