// Runtime-discovered entity system — dump C (in-match, 2026-08-31 15:49,
// 4.86 GB, map complete, 38 live character proxies).
//
// Discovery: hknpCharacterProxy instances are the universal character
// container — players AND zombie NPCs both use them (Garden Ops showed the
// same struct for teammates and AI). Zombies come from a pre-allocated pool
// (32 idle at (0,0,0) before spawning; every pooled instance becomes live
// with a real position when a zombie spawns).
// ---------------------------------------------------------------------------
#pragma once
#include "gw2_offsets.h"

namespace gw2 {
namespace live {

// ---- hknpCharacterProxy (Havok character physics) ---------------------------
// vtable[0]-8 -> COL ".?AVhknpCharacterProxy@@" (see gw2_scan_data.h)
// Identifies: any character — player, teammate, zombie NPC, boss.
namespace CharacterProxy {
    constexpr uintptr_t UserData   = 0x18;  // u32: player slot index (0..N)
    constexpr uintptr_t Scale      = 0x54;  // float (1.0)
    constexpr uintptr_t Body       = 0x60;  // ptr -> hknpBody (0 when pooled)
    constexpr uintptr_t Position   = 0x70;  // float[3] world XYZ
    constexpr uintptr_t PositionY  = 0x74;
    constexpr uintptr_t PositionZ  = 0x78;
    // live test: pos != (0,0,0); pooled zombie = exactly zero
}

// ---- hknpCharacterStateManager ----------------------------------------------
// 1:1 with proxies (37/37 in Ops, 38/38 in-match). Not yet mapped further.

// ---- heap layout (all three dumps agree) ------------------------------------
//   0x0807xxxx-0x0808xxxx : player-slot proxies (slot 0..4)
//   0x088000160           : mirror/spectator proxy (same pos as slot 0)
//   0x08f-0x092M runs     : pooled zombie proxies (spawn on demand)
//   0x09ac-0x09adM runs   : second zombie pool (bosses / specials?)
//   0x143xxxxxxx          : game image statics (DataContainer headers)
//   heap regions 0x10000..0x168410000 (dump C map)
//
// ---- finding proxies at runtime ---------------------------------------------
// Walk committed RW regions; qword v where *(v-8) == COL of
// hknpCharacterProxy. See gw2_scan_data.h kCols — binary search by VA.
// (Full pass-A walk of 4.8 GB took ~40 s in scan C.)

// ---- viewProj (world-to-screen) — runtime capture ---------------------------
// Proven by exhaustive offline scans of 4.4 GB dumps: NO view/projection
// matrix is ever stored in CPU RAM (all layouts tested, 0 hits). Frostbite
// builds it per frame and uploads it straight into a D3D11 constant buffer.
// gw2_viewproj.cpp hooks the immediate-context vtable (Map=14, Unmap=15,
// UpdateSubresource=48 — same direct vtable swap as the Present hook) and
// snapshots every constant-buffer upload. The matrix is identified by
// CONTENT: it must project live hknpCharacterProxy positions into NDC with
// the extracted camera inside 400 units of the proxy centroid. Two passing
// uploads lock (resource, offset, convention); the slot is then re-read
// every frame. No pointer chains, immune to game updates.

} // namespace live
} // namespace gw2
