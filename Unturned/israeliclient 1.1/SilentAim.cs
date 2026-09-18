// SilentAim — single JMP hook on DamageTool.raycast(Ray, float, int, Player).
// When enabled, finds the best target within FOV and REDIRECTS THE RAY toward
// them. The game's own physics resolves the hit naturally — no faked RaycastInfo.
// Defthack approach: TryGetSilentAimRay computes a redirected ray, CallOriginal
// runs the real raycast with that ray, physics does the hit, we just read the result.
//
// LastSpoofedHitPoint: set from the physics result when a hit lands on target.
//   Read by SilentAimV2 to compute matching packet yaw/pitch.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using SDG.Unturned;

namespace gatyware
{
    public static class SilentAim
    {
        [DllImport("kernel32.dll")]
        static extern bool VirtualProtect(IntPtr addr, int size, uint prot, out uint old);

        static byte[] _saved = new byte[14];
        static IntPtr _origPtr;
        static MethodInfo _origMethod;
        static IntPtr _hookFnPtr;   // cached — NEVER use reflection in finally
        static bool _hooked;

        // Melee context flag — set by WeaponMods Enable/DisableRaycastHook
        static bool _meleeContext;

        // ═══ Target state ═══
        public static object LockedTarget;
        public static Vector3 LockedTargetPos;
        public static Vector3 LastSpoofedHitPoint;
        public static bool CanHitTarget;

        // ═══ Hit chance accumulator — exact dhack logic ═══
        static int _chanceAccum;
        static int _lastChance;

        static bool ShouldHit()
        {
            int chance = State.SilentAimHitChance;
            if (chance >= 100) return true;
            if (chance <= 0) return false;
            if (chance != _lastChance) { _chanceAccum = 0; _lastChance = chance; return true; }
            _chanceAccum += chance;
            if (_chanceAccum >= 100) { _chanceAccum -= 100; return true; }
            return false;
        }

        // ═══════════════════════════════════════════════════════
        // HOOK INIT — JMP hook on DamageTool.raycast(Ray, float, int, Player)
        // Always installed. Never removed.
        // ═══════════════════════════════════════════════════════
        public static void InitHook()
        {
            if (_hooked) return;
            try
            {
                _origMethod = typeof(DamageTool).GetMethod("raycast",
                    BindingFlags.Static | BindingFlags.Public,
                    null,
                    new[] { typeof(Ray), typeof(float), typeof(int), typeof(Player) },
                    null);
                if (_origMethod == null) { Runtime.Trace("saim: raycast method not found"); return; }

                var hookMethod = typeof(SilentAim).GetMethod("HookRaycast",
                    BindingFlags.Static | BindingFlags.NonPublic);

                RuntimeHelpers.PrepareMethod(_origMethod.MethodHandle);
                RuntimeHelpers.PrepareMethod(hookMethod.MethodHandle);

                _origPtr = _origMethod.MethodHandle.GetFunctionPointer();
                _hookFnPtr = hookMethod.MethodHandle.GetFunctionPointer();

                Marshal.Copy(_origPtr, _saved, 0, 14);
                WriteJmp(_origPtr, _hookFnPtr);

                _hooked = true;
                Runtime.Trace("saim: hook OK");
            }
            catch (Exception ex) { Runtime.Trace("saim: hook err " + ex.Message); }
        }

        // ═══════════════════════════════════════════════════════
        // ENABLE / DISABLE RAYCAST HOOK (called by WeaponMods)
        // Hook is ALWAYS installed — these just set melee context flag
        // ═══════════════════════════════════════════════════════
        public static void EnableRaycastHook()
        {
            _meleeContext = true;
        }

        public static void DisableRaycastHook()
        {
            _meleeContext = false;
            LastSpoofedHitPoint = Vector3.zero;
        }

        // ═══════════════════════════════════════════════════════
        // WRITE JMP — 14-byte absolute indirect jump
        // ═══════════════════════════════════════════════════════
        static void WriteJmp(IntPtr site, IntPtr target)
        {
            byte[] jmp = new byte[14];
            jmp[0] = 0xFF; jmp[1] = 0x25;
            // jmp[2..5] = 0 (RIP+0 offset, already zero)
            BitConverter.GetBytes((long)target).CopyTo(jmp, 6);
            uint oldProt;
            VirtualProtect(site, 14, 0x40, out oldProt);
            Marshal.Copy(jmp, 0, site, 14);
            VirtualProtect(site, 14, oldProt, out oldProt);
        }

        // ═══════════════════════════════════════════════════════
        // HOOK — replacement for DamageTool.raycast
        // Defthack pattern: redirect ray, call original, physics resolves
        // ═══════════════════════════════════════════════════════

        // Cached GUIStyle for "Targeted" text (lazy-init, reused every frame)
        static GUIStyle _targetedStyle;
        static GUIStyle GetTargetedStyle()
        {
            if (_targetedStyle == null)
            {
                _targetedStyle = new GUIStyle(GUI.skin.label);
                _targetedStyle.fontSize = 11;
                _targetedStyle.fontStyle = FontStyle.Bold;
                _targetedStyle.normal.textColor = new Color(1f, 0.2f, 0.2f, 1f);
                _targetedStyle.alignment = TextAnchor.MiddleCenter;
            }
            return _targetedStyle;
        }

        // Static arrays to avoid per-frame allocation
        static readonly Vector3[] _boxCorners = new Vector3[8];
        static readonly Vector3[] _starPts = new Vector3[3];

        static RaycastInfo HookRaycast(Ray ray, float range, int mask, Player ignorePlayer)
        {
            try
            {
                // Apply punch range override
                if (WeaponMods.PunchRangeOverride > 0f)
                    range = WeaponMods.PunchRangeOverride;

                // Melee context: apply extended range
                if (_meleeContext && State.MeleeReach && !State.IsSpying)
                {
                    var lp2 = Player.player;
                    if (lp2 != null && lp2.equipment != null && lp2.equipment.asset is ItemMeleeAsset ma)
                        range = ma.range + State.MeleeReachExtra;
                }

                // Pass through when off
                if (!State.SilentAimOn || State.IsSpying)
                    return CallOriginal(ray, range, mask, ignorePlayer);

                var lp = Player.player;
                if (lp == null || ignorePlayer != lp)
                    return CallOriginal(ray, range, mask, ignorePlayer);

                // Find target
                Vector3 targetPos; object targetObj; Collider targetCol;
                if (!FindTarget(lp, out targetPos, out targetObj, out targetCol))
                    return CallOriginal(ray, range, mask, ignorePlayer);

                // Range check — use ACTUAL weapon range, not the per-step ballistic range
                // (ballistics passes velocity*0.02 as range = ~10 units, but gun range is 200+)
                float weaponRange = range;
                if (lp.equipment != null && lp.equipment.asset is ItemGunAsset gr)
                    weaponRange = gr.range;
                else if (lp.equipment != null && lp.equipment.asset is ItemMeleeAsset mr)
                    weaponRange = mr.range + (State.MeleeReach ? State.MeleeReachExtra : 0f);
                float distToTarget = Vector3.Distance(ray.origin, targetPos);
                if (distToTarget > weaponRange + 4f)
                    return CallOriginal(ray, range, mask, ignorePlayer);

                // Hit chance
                if (!ShouldHit())
                    return CallOriginal(ray, range, mask, ignorePlayer);

                LockedTarget = targetObj;
                LockedTargetPos = targetPos;

                // ═══ HIT PIPELINE ═══
                // 1. Check if wall between us and target
                // 2. No wall → direct build (guaranteed, zero misses)
                // 3. Wall + vis check ON → skip (can't hit through walls in safe mode)
                // 4. Wall + vis check OFF → physics ray only (thin walls/fences pass through)
                //    If physics hits target → real hit. If not → SKIP (no ghost marker)

                Vector3 targetCenter;
                if (targetObj is Player tpp)
                    targetCenter = (tpp.movement != null && tpp.movement.controller != null)
                        ? tpp.transform.position + tpp.movement.controller.center
                        : tpp.transform.position + Vector3.up * 1.3f;
                else if (targetObj is Zombie tzz)
                    targetCenter = tzz.transform.position + Vector3.up * 1f;
                else if (targetObj is Animal taa)
                    targetCenter = taa.transform.position + Vector3.up * 0.5f;
                else
                    return CallOriginal(ray, range, mask, ignorePlayer);

                // Wall check — use DAMAGE_SERVER mask (not DAMAGE_CLIENT) to match server exactly.
                // Server ignores SMALL(17), ENTITY(23), VEHICLE(26) — so should we.
                // This lets us target players behind vehicles, small props, etc. that server allows.
                bool wallBlocks = Physics.Linecast(ray.origin, targetCenter, RayMasks.DAMAGE_SERVER, QueryTriggerInteraction.Ignore);

                RaycastInfo ri;
                Vector3 hitPoint;

                if (wallBlocks)
                {
                    if (State.SilentAimVisCheck)
                    {
                        // VIS CHECK ON + wall = skip
                        return CallOriginal(ray, range, mask, ignorePlayer);
                    }

                    // VIS CHECK OFF + wall = scan body points for ANY visible one
                    // (same as UndeadHacks SphereUtilities — find partial exposure)
                    // Uses DAMAGE_CLIENT to match client raycast (superset of DAMAGE_SERVER)
                    bool foundVisible = false;
                    if (targetObj is Player scanP)
                    {
                        Vector3 pos = scanP.transform.position;
                        Vector3 aim = scanP.look != null ? scanP.look.aim.position : pos + Vector3.up * 1.6f;
                        Vector3 right = scanP.look != null ? scanP.look.aim.right : scanP.transform.right;
                        // 10 scan points around the body
                        Vector3[] pts = {
                            aim, aim + right * 0.4f, aim - right * 0.4f,
                            pos + Vector3.up * 1.5f, pos + Vector3.up * 1.0f,
                            pos + Vector3.up * 0.5f, pos + Vector3.up * 0.2f,
                            pos + right * 0.4f + Vector3.up, pos - right * 0.4f + Vector3.up,
                            pos + Vector3.up * 1.8f
                        };
                        int wallMaskClient = RayMasks.DAMAGE_CLIENT & ~RayMasks.ENEMY & ~RayMasks.VEHICLE;
                        for (int s = 0; s < pts.Length; s++)
                        {
                            if (!Physics.Linecast(ray.origin, pts[s], wallMaskClient, QueryTriggerInteraction.Ignore))
                            {
                                targetCenter = pts[s];
                                foundVisible = true;
                                break;
                            }
                        }
                    }
                    else if (targetObj is Zombie scanZ)
                    {
                        Vector3 pos = scanZ.transform.position;
                        Vector3[] pts = { pos + Vector3.up * 1.5f, pos + Vector3.up * 1f, pos + Vector3.up * 0.5f };
                        int wallMaskClient = RayMasks.DAMAGE_CLIENT & ~RayMasks.ENEMY & ~RayMasks.VEHICLE;
                        for (int s = 0; s < pts.Length; s++)
                        {
                            if (!Physics.Linecast(ray.origin, pts[s], wallMaskClient, QueryTriggerInteraction.Ignore))
                            {
                                targetCenter = pts[s];
                                foundVisible = true;
                                break;
                            }
                        }
                    }

                    if (!foundVisible)
                    {
                        // No visible point — target fully behind wall. Skip. No ghost marker.
                        CanHitTarget = false;
                        return CallOriginal(ray, range, mask, ignorePlayer);
                    }
                }

                // ── Build the hit at visible point ──
                hitPoint = targetCenter;
                if (targetObj is Player bp)
                {
                    ri = new RaycastInfo(bp.transform);
                    ri.player = bp; ri.transform = bp.transform;
                }
                else if (targetObj is Zombie bz)
                {
                    ri = new RaycastInfo(bz.transform);
                    ri.zombie = bz; ri.transform = bz.transform;
                }
                else if (targetObj is Animal ba)
                {
                    ri = new RaycastInfo(ba.transform);
                    ri.animal = ba; ri.transform = ba.transform;
                }
                else return CallOriginal(ray, range, mask, ignorePlayer);

                ri.point = hitPoint;
                ri.direction = (hitPoint - ray.origin).normalized;
                ri.normal = -ri.direction;
                ri.limb = GetTargetLimb();
                ri.materialName = "Flesh_Dynamic";
                if (targetCol != null) ri.collider = targetCol;

                LastSpoofedHitPoint = hitPoint;
                CanHitTarget = true;
                return ri;
            }
            catch
            {
                try { return CallOriginal(ray, range, mask, ignorePlayer); }
                catch { return null; }
            }
        }

        // ═══════════════════════════════════════════════════════
        // TRY GET SILENT AIM RAY — defthack pattern
        // Finds target, computes aim point, wall check with bone fallback
        // ═══════════════════════════════════════════════════════
        static bool TryGetSilentAimRay(Player lp, Vector3 origin, float weaponRange, out Ray silentRay)
        {
            silentRay = default(Ray);

            Vector3 targetPos;
            object targetObj;
            Collider targetCol;
            if (!FindTarget(lp, out targetPos, out targetObj, out targetCol))
                return false;

            LockedTarget = targetObj;
            LockedTargetPos = targetPos;

            // Range check — must be within weapon range
            float dist = Vector3.Distance(origin, targetPos);
            if (dist > weaponRange + 1f)
                return false;

            // ── Wall check ──
            // Use DAMAGE_SERVER — matches server's occlusion check exactly
            int wallMask = RayMasks.DAMAGE_SERVER;

            if (State.SilentAimVisCheck)
            {
                // Vis check ON — still use wallMask (strips ENEMY/VEHICLE so we don't
                // reject targets just because another player is between us and them)
                if (Physics.Linecast(origin, targetPos, wallMask, QueryTriggerInteraction.Ignore))
                    return false;
            }
            else
            {
                // Wall bypass with bone fallback (defthack pattern)
                if (Physics.Linecast(origin, targetPos, wallMask, QueryTriggerInteraction.Ignore))
                {
                    bool found = false;
                    if (targetObj is Player)
                    {
                        Player tp = (Player)targetObj;
                        string[] fallbacks = { "Spine", "Right_Hip", "Left_Hip", "Right_Arm", "Left_Arm" };
                        var cols = tp.GetComponentsInChildren<Collider>();
                        for (int i = 0; i < fallbacks.Length; i++)
                        {
                            for (int c = 0; c < cols.Length; c++)
                            {
                                if (cols[c].name == fallbacks[i])
                                {
                                    Vector3 alt = cols[c].bounds.center;
                                    if (!Physics.Linecast(origin, alt, wallMask, QueryTriggerInteraction.Ignore))
                                    {
                                        targetPos = alt;
                                        found = true;
                                    }
                                    break;
                                }
                            }
                            if (found) break;
                        }
                        if (!found) return false;
                    }
                    else if (targetObj is Zombie)
                    {
                        // Try lower body for zombies
                        Vector3 alt = ((Zombie)targetObj).transform.position + Vector3.up * 0.8f;
                        if (!Physics.Linecast(origin, alt, wallMask, QueryTriggerInteraction.Ignore))
                            targetPos = alt;
                        else
                            return false;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            silentRay = new Ray(origin, (targetPos - origin).normalized);
            return true;
        }

        // ═══════════════════════════════════════════════════════
        // CALL ORIGINAL — unhook, call, rehook using CACHED pointer
        // ═══════════════════════════════════════════════════════
        static RaycastInfo CallOriginal(Ray ray, float range, int mask, Player ignorePlayer)
        {
            // Direct Physics.Raycast — NO unhook/rehook JMP dance.
            // Replicates DamageTool.raycast logic exactly (verified via dnSpy).
            RaycastHit hit;
            Physics.Raycast(ray, out hit, range, mask);

            RaycastInfo info = new RaycastInfo(hit);
            info.direction = ray.direction;
            info.limb = ELimb.SPINE;

            if (info.transform != null)
            {
                // ── Hitbox collider resolution — redirect to real target ──
                if (info.transform.name == "Hitbox" && info.transform.parent != null)
                {
                    Transform parent = info.transform.parent;
                    Player hp = parent.GetComponent<Player>();
                    if (hp != null && hp != ignorePlayer)
                    {
                        info.player = hp;
                        info.transform = parent;
                        info.limb = GetTargetLimb();
                        info.materialName = PhysicsTool.GetMaterialName(hit.point, parent, info.collider);
                        return info;
                    }
                    Zombie hz = parent.GetComponent<Zombie>();
                    if (hz != null)
                    {
                        info.zombie = hz;
                        info.transform = parent;
                        info.limb = GetTargetLimb();
                        info.materialName = PhysicsTool.GetMaterialName(hit.point, parent, info.collider);
                        return info;
                    }
                    Animal ha = parent.GetComponent<Animal>();
                    if (ha != null)
                    {
                        info.animal = ha;
                        info.transform = parent;
                        info.limb = ELimb.SPINE;
                        info.materialName = PhysicsTool.GetMaterialName(hit.point, parent, info.collider);
                        return info;
                    }
                }

                if (info.transform.CompareTag("Barricade"))
                    info.transform = DamageTool.getBarricadeRootTransform(info.transform);
                else if (info.transform.CompareTag("Structure"))
                    info.transform = DamageTool.getStructureRootTransform(info.transform);
                else if (info.transform.CompareTag("Resource"))
                    info.transform = DamageTool.getResourceRootTransform(info.transform);
                else if (info.transform.CompareTag("Enemy"))
                {
                    info.player = DamageTool.getPlayer(info.transform);
                    if (info.player == ignorePlayer) info.player = null;
                    info.limb = DamageTool.getLimb(info.transform);
                }
                else if (info.transform.CompareTag("Zombie"))
                {
                    info.zombie = DamageTool.getZombie(info.transform);
                    info.limb = DamageTool.getLimb(info.transform);
                }
                else if (info.transform.CompareTag("Animal"))
                {
                    info.animal = DamageTool.getAnimal(info.transform);
                    info.limb = DamageTool.getLimb(info.transform);
                }
                else if (info.transform.CompareTag("Vehicle"))
                    info.vehicle = DamageTool.getVehicle(info.transform);

                if (info.zombie != null && info.zombie.isRadioactive)
                {
                    info.materialName = "Alien_Dynamic";
                    info.material = EPhysicsMaterial.ALIEN_DYNAMIC;
                }
                else
                {
                    info.materialName = PhysicsTool.GetMaterialName(hit.point, info.transform, info.collider);
                }
            }

            // Override limb from settings
            if (info.player != null || info.zombie != null || info.animal != null)
                info.limb = GetTargetLimb();

            return info;
        }

        // ═══════════════════════════════════════════════════════
        // TARGET SELECTION — finds best target within FOV
        // ═══════════════════════════════════════════════════════
        static bool FindTarget(Player lp, out Vector3 bestPos, out object bestObj, out Collider bestCol)
        {
            bestPos = Vector3.zero;
            bestObj = null;
            bestCol = null;
            float bestDist = float.MaxValue;
            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector3 camPos = MainCamera.instance.transform.position;

            // ── Players ──
            if (State.SilentAimTargetPlayers)
            {
                for (int i = 0; i < Provider.clients.Count; i++)
                {
                    var sp = Provider.clients[i];
                    if (sp == null || sp.player == null) continue;
                    if (sp.player.channel.IsLocalPlayer) continue;
                    if (sp.player.life.isDead) continue;
                    if (State.SilentAimFriendly && PlayerRelation.IsFriend(sp)) continue;

                    Vector3 limbPos = GetPlayerPredLimb(sp.player);
                    Collider limbCol = GetPlayerLimbCollider(sp.player);
                    float d = EvalTarget(limbPos, camPos, screenCenter);
                    if (d < bestDist)
                    {
                        bestDist = d; bestPos = limbPos;
                        bestObj = sp.player; bestCol = limbCol;
                    }
                }
            }

            // ── Zombies ──
            if (State.SilentAimTargetZombies)
            {
                for (int r = 0; r < ZombieManager.regions.Length; r++)
                {
                    var list = ZombieManager.regions[r].zombies;
                    for (int z = 0; z < list.Count; z++)
                    {
                        var zom = list[z];
                        if (zom == null || zom.isDead) continue;

                        Vector3 headPos = GetZombieHead(zom);
                        Collider zCol = GetZombieCollider(zom);
                        float d = EvalTarget(headPos, camPos, screenCenter);
                        if (d < bestDist)
                        {
                            bestDist = d; bestPos = headPos;
                            bestObj = zom; bestCol = zCol;
                        }
                    }
                }
            }

            return bestObj != null;
        }

        static float EvalTarget(Vector3 pos, Vector3 camPos, Vector2 screenCenter)
        {
            float dist = Vector3.Distance(camPos, pos);
            if (dist > State.SilentAimMaxDist) return float.MaxValue;

            // Melee context: target by NEAREST DISTANCE, no FOV restriction
            if (_meleeContext)
                return dist;

            // Gun context: target by screen proximity (closest to crosshair)
            Vector3 sp = MainCamera.instance.WorldToScreenPoint(pos);
            if (sp.z <= 0) return float.MaxValue;
            float pixDist = Vector2.Distance(screenCenter, new Vector2(sp.x, Screen.height - sp.y));
            if (State.SilentAimFovRestrict && pixDist > State.SilentAimFov) return float.MaxValue;

            // NO wall check here — UpdateTarget and HookRaycast do body-point scan
            return pixDist;
        }

        // ═══════════════════════════════════════════════════════
        // LIMB SELECTION
        // ═══════════════════════════════════════════════════════
        static readonly string[] _limbNames = { "Head", "Spine", "Closest", "Random" };
        public static string[] LimbNames { get { return _limbNames; } }

        static ELimb GetTargetLimb()
        {
            switch (State.SilentAimHitLimb)
            {
                case 0: return ELimb.SKULL;
                case 1: return ELimb.SPINE;
                case 2: return ELimb.SPINE;
                case 3:
                    ELimb[] limbs = { ELimb.SKULL, ELimb.SPINE, ELimb.LEFT_ARM, ELimb.RIGHT_ARM,
                                     ELimb.LEFT_LEG, ELimb.RIGHT_LEG };
                    return limbs[UnityEngine.Random.Range(0, limbs.Length)];
                default: return ELimb.SKULL;
            }
        }

        // Prediction limb — what position to aim at for the hit
        static Vector3 GetPlayerPredLimb(Player p)
        {
            string boneName;
            switch (State.SilentAimPredLimb)
            {
                case 0: boneName = "Skull"; break;
                case 1: boneName = "Spine"; break;
                case 2: return GetClosestVisibleLimb(p);
                case 3:
                    string[] bones = { "Skull", "Spine", "Left_Hand", "Right_Hand" };
                    boneName = bones[UnityEngine.Random.Range(0, bones.Length)];
                    break;
                default: boneName = "Skull"; break;
            }

            var cols = p.GetComponentsInChildren<Collider>();
            for (int i = 0; i < cols.Length; i++)
                if (cols[i].name == boneName)
                    return cols[i].bounds.center;
            return p.look.aim.position;
        }

        static Collider GetPlayerLimbCollider(Player p)
        {
            string boneName = State.SilentAimPredLimb == 0 ? "Skull" : "Spine";
            var cols = p.GetComponentsInChildren<Collider>();
            for (int i = 0; i < cols.Length; i++)
                if (cols[i].name == boneName) return cols[i];
            return null;
        }

        static readonly string[] _playerLimbOrder = { "Skull", "Spine", "Left_Hand", "Right_Hand", "Left_Foot", "Right_Foot" };
        static Vector3 GetClosestVisibleLimb(Player p)
        {
            Vector3 cam = MainCamera.instance.transform.position;
            var cols = p.GetComponentsInChildren<Collider>();
            for (int li = 0; li < _playerLimbOrder.Length; li++)
                for (int ci = 0; ci < cols.Length; ci++)
                    if (cols[ci].name == _playerLimbOrder[li])
                    {
                        Vector3 pos = cols[ci].bounds.center;
                        if (!State.SilentAimVisCheck ||
                            !Physics.Linecast(cam, pos, RayMasks.DAMAGE_CLIENT, QueryTriggerInteraction.Ignore))
                            return pos;
                        break;
                    }
            return p.look.aim.position;
        }

        // Zombie helpers
        static Vector3 GetZombieHead(Zombie z)
        {
            Transform skull = FindChildRecursive(z.transform, "Skull");
            if (skull != null) return skull.position;
            Transform spine = FindChildRecursive(z.transform, "Spine");
            if (spine != null) return spine.position + Vector3.up * 0.3f;
            return z.transform.position + Vector3.up * 1.75f;
        }

        static Collider GetZombieCollider(Zombie z)
        {
            var cols = z.GetComponentsInChildren<Collider>();
            for (int i = 0; i < cols.Length; i++)
                if (!cols[i].isTrigger) return cols[i];
            return null;
        }

        static Transform FindChildRecursive(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform c = parent.GetChild(i);
                if (c.name == name) return c;
                Transform found = FindChildRecursive(c, name);
                if (found != null) return found;
            }
            return null;
        }

        // ═══════════════════════════════════════════════════════
        // DRAWING — FOV circle + target line
        // ═══════════════════════════════════════════════════════
        public static void UpdateTarget()
        {
            if (!State.SilentAimOn || State.IsSpying)
            {
                LockedTarget = null;
                return;
            }

            var lp = Player.player;
            if (lp == null) { LockedTarget = null; return; }

            // Get current weapon range
            float weaponRange = 0f;
            if (lp.equipment != null && lp.equipment.asset != null && lp.equipment.isEquipped)
            {
                if (lp.equipment.asset is ItemGunAsset)
                    weaponRange = ((ItemGunAsset)lp.equipment.asset).range;
                else if (lp.equipment.asset is ItemMeleeAsset)
                {
                    weaponRange = ((ItemMeleeAsset)lp.equipment.asset).range;
                    if (State.MeleeReach) weaponRange += State.MeleeReachExtra;
                }
            }
            else
            {
                // Fist/punch
                weaponRange = State.MeleeReach ? Mathf.Min(1.75f + State.MeleeReachExtra, 6f) : 1.75f;
            }

            if (weaponRange <= 0f) { LockedTarget = null; return; }

            Vector3 pos; object obj; Collider col;
            if (FindTarget(lp, out pos, out obj, out col))
            {
                float dist = Vector3.Distance(lp.look.aim.position, pos);
                if (dist > weaponRange + 1f)
                {
                    LockedTarget = null;
                    return;
                }

                // Wall check
                int uwm = RayMasks.DAMAGE_CLIENT & ~RayMasks.ENEMY & ~RayMasks.VEHICLE;
                bool blocked = Physics.Linecast(lp.look.aim.position, pos, uwm, QueryTriggerInteraction.Ignore);
                if (blocked)
                {
                    if (State.SilentAimVisCheck)
                    {
                        LockedTarget = null;
                        return;
                    }
                    // Vis check OFF — scan body points for partial exposure
                    bool anyVisible = false;
                    if (obj is Player utp)
                    {
                        Vector3 p = utp.transform.position;
                        Vector3 a = utp.look != null ? utp.look.aim.position : p + Vector3.up * 1.6f;
                        Vector3 r = utp.look != null ? utp.look.aim.right : utp.transform.right;
                        Vector3[] pts = { a, a + r * 0.4f, a - r * 0.4f,
                            p + Vector3.up * 1.5f, p + Vector3.up * 1f, p + Vector3.up * 0.5f,
                            p + Vector3.up * 0.2f, p + Vector3.up * 1.8f };
                        for (int s = 0; s < pts.Length; s++)
                        {
                            if (!Physics.Linecast(lp.look.aim.position, pts[s], uwm, QueryTriggerInteraction.Ignore))
                            { anyVisible = true; break; }
                        }
                    }
                    else if (obj is Zombie utz)
                    {
                        Vector3 p = utz.transform.position;
                        Vector3[] pts = { p + Vector3.up * 1.5f, p + Vector3.up * 1f, p + Vector3.up * 0.5f };
                        for (int s = 0; s < pts.Length; s++)
                        {
                            if (!Physics.Linecast(lp.look.aim.position, pts[s], uwm, QueryTriggerInteraction.Ignore))
                            { anyVisible = true; break; }
                        }
                    }
                    if (!anyVisible)
                    {
                        LockedTarget = null;
                        return;
                    }
                }

                LockedTarget = obj;
                LockedTargetPos = pos;
            }
            else
            {
                LockedTarget = null;
            }
        }
    }
}
