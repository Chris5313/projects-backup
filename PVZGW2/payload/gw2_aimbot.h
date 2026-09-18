// ---------------------------------------------------------------------------
// gw2_aimbot.h - aimbot & silent aim module
//
// Regular aimbot: calculates angle to target, moves mouse via SendInput.
// Silent aim: hooks bullet creation to redirect trajectory (needs IDA offsets).
// ---------------------------------------------------------------------------
#pragma once
#include <cstdint>

namespace aimbot {

// Initialize (called once at DLL load)
void Init();

// Per-frame update - handle targeting and aim correction
void OnFrame();

// Draw FOV circles (call from overlay render)
void DrawFOV();

// Silent aim hook installation (requires offsets from IDA analysis)
// Returns true if hooks installed successfully
bool InstallSilentHooks(uintptr_t bulletSpawnAddr, uintptr_t aimAngleAddr);

// Current target info for debug display
const char* StatusText();

// Is currently aiming at a valid target?
bool HasTarget();

// Get current target screen position (for visual feedback)
bool GetTargetScreenPos(float* sx, float* sy);

} // namespace aimbot
