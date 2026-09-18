// ProtonBypass — ULTIMATE client-side ProtonAC neutraliser.
// =================================================================
// ProtonAC loads a native DLL via a custom PE/ELF memory loader
// (_EA._Vm) and stores 4 native function pointers as C# delegates
// in Class_21. Every server-side Harmony patch (30+ hooks) feeds
// into one of these 4 delegates.
//
// Strategy:
//   _PR  (CheckInput)         → replace delegate with managed lambda
//   _nU  (CheckInputLength)   → replace delegate with managed lambda
//   _Yi  (CheckRaycastBasic)  → JMP hook on delegate.Invoke
//   _dr  (CheckRaycastBullet) → JMP hook on delegate.Invoke
//
// JMP hooks use the SAME technique as SilentAim.cs (PrepareMethod
// → GetFunctionPointer → WriteJmp). Our replacement methods match
// the exact delegate signatures and return pass results directly.
// =================================================================

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SDG.Unturned;
using UnityEngine;

namespace gatyware
{
    public static class ProtonBypass
    {
        [DllImport("kernel32.dll")]
        static extern bool VirtualProtect(IntPtr addr, int size, uint prot, out uint old);

        static bool _installed;

        // Dummy implementations for simple bool delegates
        static bool DummyCheckInput(ref object input) { return true; }
        static bool DummyCheckInputLength(ref bool dir, ref object input) { return true; }

        // Reflection cache
        static Type _class21;
        static FieldInfo _fiM, _fiFo, _fiEM, _fiS59;
        static Type _hjType;

        // ── Find Class_21 by scanning all loaded assemblies ──────
        static bool FindClass21()
        {
            if (_class21 != null) return true;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try {
                    foreach (var t in asm.GetTypes()) {
                        if (t.IsClass && t.IsAbstract && t.IsSealed &&
                            t.GetNestedType("_Bv", BindingFlags.Public) != null &&
                            t.GetNestedType("_hj", BindingFlags.Public) != null) {
                            var m = t.GetField("_M", BindingFlags.Static | BindingFlags.NonPublic);
                            if (m != null) { _class21 = t; _hjType = t.GetNestedType("_hj"); return true; }
                        }
                    }
                } catch { }
            }
            return false;
        }

        static bool CacheFields()
        {
            if (_fiM != null) return true;
            if (!FindClass21()) return false;
            var bf = BindingFlags.Static | BindingFlags.NonPublic;
            _fiM   = _class21.GetField("_M", bf);
            _fiFo  = _class21.GetField("_fo", bf);
            _fiEM  = _class21.GetField("_EM", bf);
            _fiS59 = _class21.GetField("s_field_59", bf);
            return _fiM != null && _fiFo != null && _fiEM != null && _fiS59 != null;
        }


        // ── JMP hook state ──────────────────────────────────────
        // Same technique as SilentAim.cs: PrepareMethod → get ptr → write JMP
        static MethodInfo _yiOrig, _drOrig;
        static IntPtr _yiOrigPtr, _drOrigPtr, _yiHookPtr, _drHookPtr;
        static byte[] _yiSaved = new byte[14], _drSaved = new byte[14];
        static bool _yiHooked, _drHooked;

        static void WriteJmp(IntPtr site, IntPtr target)
        {
            byte[] jmp = new byte[14];
            jmp[0] = 0xFF; jmp[1] = 0x25;
            BitConverter.GetBytes((long)target).CopyTo(jmp, 6);
            uint old;
            VirtualProtect(site, 14, 0x40, out old);
            Marshal.Copy(jmp, 0, site, 14);
            VirtualProtect(site, 14, old, out old);
        }

        // ── Replacement for _Yi.Invoke (CheckRaycastBasic) ──────
        // Signature: _hj(ref bool living, ref bool failDir, ref bool hasDir,
        //               ref bool suspect, ref bool inVeh, ref float maxRng,
        //               ref _Bv fwd, ref _Bv pos, ref _Bv aimDiff, ref _pg input)
        // We return a pass _hj (built on first call from the native struct).
        static object YiHook(
            ref bool a1, ref bool a2, ref bool a3, ref bool a4, ref bool a5,
            ref float a6, ref object a7, ref object a8, ref object a9, ref object a10)
        {
            return BuildPassObj();
        }

        // ── Replacement for _dr.Invoke (CheckRaycastBullet) ─────
        // Signature: _hj(ref bool living, ref bool recentMismatch, ref bool hasDir,
        //               ref bool suspect, ref bool inVeh, ref _Bv aimDiff,
        //               ref _TF bullet, ref _pg input)
        static object DrHook(
            ref bool a1, ref bool a2, ref bool a3, ref bool a4, ref bool a5,
            ref object a6, ref object a7, ref object a8)
        {
            return BuildPassObj();
        }

        static object BuildPassObj()
        {
            if (_hjType == null) return null;
            var obj = Activator.CreateInstance(_hjType);
            try { _hjType.GetField("_BJ").SetValue(obj, 0); } catch { }
            try { _hjType.GetField("_uN").SetValue(obj, IntPtr.Zero); } catch { }
            return obj;
        }

        // ── Install JMP hook on a delegate's Invoke method ──────
        static bool JmpHookDelegate(Type delType, MethodInfo hookMethod,
            ref MethodInfo origMethod, ref IntPtr origPtr, ref IntPtr hookPtr,
            byte[] saved, ref bool hooked)
        {
            if (hooked) return true;
            try
            {
                origMethod = delType.GetMethod("Invoke");
                if (origMethod == null) return false;
                RuntimeHelpers.PrepareMethod(origMethod.MethodHandle);
                RuntimeHelpers.PrepareMethod(hookMethod.MethodHandle);
                origPtr = origMethod.MethodHandle.GetFunctionPointer();
                hookPtr = hookMethod.MethodHandle.GetFunctionPointer();
                Marshal.Copy(origPtr, saved, 0, 14);
                WriteJmp(origPtr, hookPtr);
                hooked = true;
                return true;
            }
            catch (Exception ex) { Runtime.Trace("pb: jmp err " + ex.Message); return false; }
        }


        public static void TryInstall()
        {
            if (_installed) return;
            try
            {
                if (!CacheFields()) return;

                // Wait until ProtonAC has loaded (delegate fields are non-null)
                var curM = _fiM.GetValue(null);
                if (curM == null) return;

                // Replace bool delegates (simple managed lambda, no struct return)
                var dPR = Delegate.CreateDelegate(
                    _class21.GetNestedType("_PR", BindingFlags.Public),
                    typeof(ProtonBypass).GetMethod("DummyCheckInput",
                        BindingFlags.Static | BindingFlags.NonPublic));
                _fiM.SetValue(null, dPR);

                var dNU = Delegate.CreateDelegate(
                    _class21.GetNestedType("_nU", BindingFlags.Public),
                    typeof(ProtonBypass).GetMethod("DummyCheckInputLength",
                        BindingFlags.Static | BindingFlags.NonPublic));
                _fiFo.SetValue(null, dNU);

                // JMP hook the struct-return delegates (_Yi, _dr)
                var yiType = _class21.GetNestedType("_Yi", BindingFlags.Public);
                var yiHook = typeof(ProtonBypass).GetMethod("YiHook",
                    BindingFlags.Static | BindingFlags.NonPublic);
                if (yiType != null && yiHook != null)
                    JmpHookDelegate(yiType, yiHook,
                        ref _yiOrig, ref _yiOrigPtr, ref _yiHookPtr, _yiSaved, ref _yiHooked);

                var drType = _class21.GetNestedType("_dr", BindingFlags.Public);
                var drHook = typeof(ProtonBypass).GetMethod("DrHook",
                    BindingFlags.Static | BindingFlags.NonPublic);
                if (drType != null && drHook != null)
                    JmpHookDelegate(drType, drHook,
                        ref _drOrig, ref _drOrigPtr, ref _drHookPtr, _drSaved, ref _drHooked);

                if (_yiHooked && _drHooked)
                {
                    _installed = true;
                    Runtime.Trace("pb: ProtonAC FULLY BYPASSED (bool+JMP)");
                }
            }
            catch (Exception ex) { Runtime.Trace("pb: " + ex.Message); }
        }
    }
}
