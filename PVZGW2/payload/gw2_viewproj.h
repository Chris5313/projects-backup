// ---------------------------------------------------------------------------
// gw2_viewproj.h — world->screen projection via the game's own camera.
//
// 2026-09-03: the viewProj is read directly from the game's CameraManager
// (Ghidra-decoded chain: static 0x142CEE730 -> +0x68 -> view[0]+0x230).
// No hooks, no constant-buffer scraping — this module is a pure reader.
//
// Targets (hknpCharacterProxy instances) are recorded by the scanner and
// their positions refreshed per frame; used for the status line.
// ---------------------------------------------------------------------------
#pragma once
#include <cstdint>

namespace vproj {

// scanner pass A: record a hknpCharacterProxy instance (heap address)
void RecordTarget(uint64_t proxyVA, int posOffset = 0x70);

// resolve NtReadVirtualMemory + init state (no hooks installed)
bool Init(void* device, void* immediateContext);

// Present hook, once per frame: refresh live target positions, read camera
void OnFrame();

// overlay: current backbuffer size in pixels (for NDC -> pixel conversion)
void SetScreenSize(float w, float h);

// true when the engine camera chain delivered a valid matrix
bool HasViewProj();

// world -> screen pixels. false = behind camera / not available.
bool WorldToScreen(float wx, float wy, float wz, float* sx, float* sy);

// v18: W2S health counters since last 1 s bucket boundary (calls, successes).
// DrawEsp logs them in the ESP DIAG line — all-calls-but-zero-success with a
// valid matrix is the silent all-behind-camera cull signature.
void W2SHealth(long* calls, long* ok);

// v19: 1 when the live z-convention measurement negated the engine's
// 3rd camera row (forward-named), 0 while using the row as stored.
int  ZFlip();

// world-space camera position, solved from the current matrix.
bool GetCamera(float cam[3]);

// overlay debug layer: projected dots for every live target.
int  DebugDots(float* sx, float* sy, float* dist, int maxN);

// short human-readable state for the overlay corner
const char* StatusText();

} // namespace vproj
