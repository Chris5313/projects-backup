// ---------------------------------------------------------------------------
// GW2 dump offsets — GENERATED FILE, do not hand-edit.
// Regenerate: node C:\Tools\gen_gw2_offsets.js (after re-dumping + analyzing)
//
// Dump:  gw2_image_dump_0.bin @ base 0x140000000 (manifest: gw2_image_manifest.txt)
// Build: GW2.Main_Win64_Retail.exe, 22 sections, 60,817,408 bytes
//
// All values are RVAs (offsets from module base). Resolve at runtime:
//   uintptr_t va = gw2::addr(gw2::offsets::fn::whatever);
// The payload is injected into the game process, so game memory is directly
// readable — no driver calls needed for reads once we're mapped in.
// ---------------------------------------------------------------------------
#pragma once
#include <cstdint>
#include <windows.h>

namespace gw2 {

// Module base of GW2.Main_Win64_Retail.exe (resolved once, cached).
inline uintptr_t base()
{
    static uintptr_t b = 0;
    if (!b) b = (uintptr_t)GetModuleHandleA("GW2.Main_Win64_Retail.exe");
    return b;
}

// RVA -> runtime VA
inline uintptr_t addr(uintptr_t rva) { return base() + rva; }

// RVA -> typed pointer
template <typename T>
inline T* ptr(uintptr_t rva) { return (T*)(base() + rva); }

// Follow a pointer at an RVA (game-style: ptr at rva points to struct)
template <typename T>
inline T* deref(uintptr_t rva)
{
    uintptr_t* p = (uintptr_t*)(base() + rva);
    return (T*)(*p);
}

// ---- Section map (from runtime manifest) ----------------------------------
namespace sections {
    constexpr uintptr_t srdata = 0x00001000; // size 0x01DD8000  R-X  (code, decrypted)
    constexpr uintptr_t xtext  = 0x01DD9000; // size 0x00228000  R-X  (code, decrypted)
    constexpr uintptr_t udata  = 0x02001000; // size 0x00981000  R--  (data)
    constexpr uintptr_t sxdata = 0x02982000; // size 0x00683000  RW-  (data)
    constexpr uintptr_t text   = 0x03005000; // size 0x0019D000  R--  (hollow in mem too)
    constexpr uintptr_t idata  = 0x031A4000; // size 0x0000D000  R--  (IAT, RESOLVED)
    constexpr uintptr_t rsrc   = 0x03356000; // size 0x00065000  R--  (resources)
    constexpr uintptr_t xtls   = 0x03458000; // size 0x004025F6  R-X  (TLS code/data)
    constexpr uintptr_t xdata  = 0x03862000; // size 0x0019CEFC  R--  (unwind info)
}

// ---- Static Pointers (RVAs to global pointers) ----------------------------
// These point to runtime-allocated singletons. Read the pointer, then follow.
namespace statics {
    constexpr uintptr_t GameContext          = 0x02CDBED8; // Main game context (50+ xrefs)
    constexpr uintptr_t CameraManager        = 0x02CEE730; // Camera system root
    constexpr uintptr_t ClientPlayerGateEM   = 0x02CF1900; // ClientPlayerGateEntityManager
    constexpr uintptr_t DynamicModelEM       = 0x02CDB540; // DynamicModelEntityManager table
    constexpr uintptr_t TypeSystemDefault    = 0x02982A00; // Type system default value
}

// ---- VTables (RVAs to vtable pointers) -------------------------------------
// Match these against object vtable to identify type.
namespace vtables {
    constexpr uintptr_t CharObj              = 0x0228B380; // hknpCharacterProxy owner
    constexpr uintptr_t CharObjTypeInfo      = 0x0228B658; // CharObj RTTI
    constexpr uintptr_t TeamReg              = 0x0228BC58; // Team registration
    constexpr uintptr_t Container            = 0x0236B638; // DataContainer
}

// ---- GameContext Offsets ---------------------------------------------------
namespace ctx {
    constexpr uintptr_t SubContext           = 0x10;  // -> nested context
    constexpr uintptr_t PlayerManagerBase    = 0x68;  // -> base for player mgr access
    constexpr uintptr_t EntityListStart      = 0x148; // -> entity list begin
    constexpr uintptr_t EntityListEnd        = 0x150; // -> entity list end
}

// ---- CameraManager Offsets -------------------------------------------------
namespace cam {
    constexpr uintptr_t RenderViews          = 0x68;  // -> RenderView* array
}

// ---- RenderView Offsets (for world-to-screen) ------------------------------
namespace rv {
    constexpr uintptr_t ViewMatrix           = 0x000; // float[16] VIEW
    constexpr uintptr_t ViewMatrix2          = 0x040; // float[16] VIEW copy
    constexpr uintptr_t ViewMatrix3          = 0x340; // float[16] VIEW (use this)
    constexpr uintptr_t ProjMatrix           = 0x380; // float[16] PROJECTION
    constexpr uintptr_t ProjMatrix2          = 0x3E0; // float[16] PROJECTION copy
    constexpr uintptr_t ProjMatrix3          = 0x420; // float[16] PROJECTION variant
}

// ---- CharObj Offsets (entity with physics proxy) ---------------------------
namespace charobj {
    constexpr uintptr_t Marker               = 0x18;  // u32 marker (0x100F)
    constexpr uintptr_t Entity               = 0x20;  // -> Entity ptr
    constexpr uintptr_t ClassBase            = 0x28;  // -> property/classbase node
    constexpr uintptr_t TypeInfo             = 0x48;  // -> TypeInfo
    constexpr uintptr_t Proxy                = 0x50;  // -> hknpCharacterProxy (has position)
    constexpr uintptr_t State                = 0x58;  // -> state object
    constexpr uintptr_t Components           = 0x68;  // -> component array
    constexpr uintptr_t MaxHealth            = 0x8C;  // float (100/155/180/1000)
    constexpr uint32_t  MarkerValue          = 0x100F;
}

// ---- Proxy Offsets (hknpCharacterProxy) ------------------------------------
namespace proxy {
    constexpr uintptr_t Position             = 0x70;  // float[3] world position
}

// ---- ClassBase Chain (for team/faction from class string) ------------------
namespace classbase {
    constexpr uintptr_t Node                 = 0x30;  // -> node
    constexpr uintptr_t NodeString           = 0x10;  // node+0x10 -> class string ptr
    // String format: "Gameplay/Soldiers/PhysicsData/<Class>Physics"
    // [30] = 'Z' (zombie) / 'P' (plant)
    // Contains "_AI" or "Turret" = NPC
}

// ---- Game Object Offsets ---------------------------------------------------
namespace game {
    constexpr uintptr_t LocalPlayers         = 0x680; // -> mLocalPlayers array
    constexpr uintptr_t LocalPlayerMap       = 0x6B8; // -> mLocalPlayerMap
}

// ---- PlayerManager Offsets -------------------------------------------------
namespace playermgr {
    constexpr uintptr_t FromBase             = 0x13A0; // offset from some base to PlayerManager
}

// ---- Known symbols (survived statically in the on-disk exe) ----------------
namespace fn {
    // From Ghidra entry-point analysis of the original exe.
    // RVAs = VA - 0x140000000.
    constexpr uintptr_t IsGameRuntime        = 0x0012C710;
    constexpr uintptr_t SecureVar_setValue   = 0x00BC6460;
    constexpr uintptr_t SecureVar_setValue2  = 0x00BC64B0;
}

} // namespace gw2
