// ---------------------------------------------------------------------------
// gw2_ac.h — EAAC ACHT harvester (Phase 2a-v2).
//
// Passive heap scan driven from the Present hook (reads only) PLUS a
// MinHook detour on the AntiCheatData serializer (descriptor+0x18 ->
// RVA 0x19B2770) that fires exactly when the login packet is built —
// the passive scan alone missed the UUIDs because the packet buffer
// lives only milliseconds.
//
// Verified targets (from gw2_image_dump_0.bin, 2026-09-04; dump file offset
// == RVA, live base 0x140000000; all cross-checked by runtime-pointer
// search — see gw2_ac.cpp for the full table):
//   AntiCheatData TypeRep descriptor RVA 0x2A68D80, handle slot RVA
//   0x2D8BC30, member table RVA 0x2997480 (shieldUUID fieldId 0x10,
//   skyfallUUID fieldId 0x40 — both blob-typed).
//
// Output: C:\ProgramData\Microsoft\DeviceSync\ac_harvest.txt
// ---------------------------------------------------------------------------
#pragma once

namespace ac {
// Call once per frame from the Present hook. Time-sliced: scans at most
// ~4 MB of private heap per call, keeps a rolling cursor across passes.
void HarvestTick();

// Install the AntiCheatData serializer hook (call AFTER MH_Initialize).
void InstallHooks();
}
