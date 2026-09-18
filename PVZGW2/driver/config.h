// ---------------------------------------------------------------------------
// GW2 driver configuration — edit this file, do not touch literals in code.
// ---------------------------------------------------------------------------
#pragma once

// Process image name the driver polls for (exact name from Task Manager).
#define GW2_TARGET_PROCESS   "GW2.Main_Win64_Retail.exe"

// Payload DLL the driver reads from disk and manual-maps into the game.
// The loader must deploy the payload to this exact path before the driver
// reads it (kernel uses \\??\\ DOS-device namespace).
#define GW2_PAYLOAD_PATH     L"\\??\\C:\\Users\\Public\\gw2_payload.dll"

// User-mode module the driver waits for inside the target before injecting.
// Wait for d3d11.dll: Frostbite loads it during renderer init, and EAAC has
// finished its initial process-protection setup by the time DX is up. The
// previous value (main exe) injected within 2s of process creation, before
// EAAC had stabilised — causing BSOD (STATUS_IN_PAGE_ERROR on the mapped view).
#define GW2_WAIT_MODULE      L"d3d11.dll"

// Max seconds to wait for the wait-module to load before injecting anyway.
#define GW2_WAIT_MODULE_SECS 30

// Extra settle delay (seconds) after the wait-module appears. Gives EAAC's
// kernel driver time to finish its address-space walk before we map anything.
#define GW2_SETTLE_SECS      3

// Main game module (PEB name) — DumpGameImage copies its decrypted runtime
// image to C:\Users\Public\gw2_image_dump_<n>.bin for offline Ghidra work.
#define GW2_IMAGE_MODULE      L"GW2.Main_Win64_Retail.exe"
