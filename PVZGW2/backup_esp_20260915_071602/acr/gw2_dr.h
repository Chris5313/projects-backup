// ---------------------------------------------------------------------------
// gw2_dr.h — EAAC attestation packet capture via user-mode hardware
// breakpoints (Phase 2c).
//
// Sixteen passive harvester runs proved the shield/skyfall UUIDs never sit
// in readable heap storage — they exist only inside the serialized
// LoginRequest packet, for milliseconds, at encode time. This module puts
// hardware execute breakpoints (debug registers DR0-DR3) on the verified
// encode-path functions and catches the packet AT THE MOMENT it is built.
//
// Why user-mode: the breakpoints are set with SetThreadContext from inside
// the game process (own-process, legitimate API) and the #DB exception is
// handled by a vectored exception handler in this DLL. No kernel exception
// hooking, no driver changes, no IDT/PTE tricks — a bug here can crash the
// game process at worst, NEVER the system (no BSOD path exists).
//
// Targets (game RVAs, base = GetModuleHandleA(nullptr)):
//   DR0 0xB5A460  LoginRequest builder (sub_90DDF60) — carries the login
//                 command immediates; PROVEN on the login path; fires once
//                 per login. Its args include the packet buffers.
//   DR1 0x1992DC0 blob encode dispatcher (orig fn in blob TypeRep slot)
//   DR2 0x19B2770 AntiCheatData encode thunk (3-arg dispatcher family)
//   DR3 0x19B2780 dispatcher body
//
// Safety profile:
//   - armed at DLL init, re-armed every 10s for 120s (late threads),
//     fully disarmed (all DRs cleared) at 300s or after capture / 100 hits
//   - VEH hit path: set RF flag + continue — the function runs untouched,
//     we only observe. No code modification anywhere.
//
// v15.2: dynamic write-watchpoints. The passive sweep calls WatchObject()
// when it finds a heap codec object (T6/T1 target hits); DR1-3 become
// 8-byte write watchpoints on it. Any game write fires a hit that dumps
// the object + the RAW STACK — return addresses in it are the REAL
// runtime call chain (replacing all IDA-guessed targets).
#pragma once

namespace acr {
    // Call once from payload init (own thread context OK).
    void Init();

    // v15.2: call when the passive sweep discovers a codec object in the
    // heap (T6/T1 target hits). Write watchpoints get armed on it.
    void WatchObject(unsigned long long heapAddr);

    // Call every Present frame (same cadence as ac::HarvestTick).
    // Drives re-arm passes, watch re-dumps, and the auto-disarm timer.
    void OnTick();
}
