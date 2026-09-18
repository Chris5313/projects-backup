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

// ---- Known symbols (survived statically in the on-disk exe) ----------------
namespace offsets {
    namespace fn {
        // From Ghidra entry-point analysis of the original exe.
        // RVAs = VA - 0x140000000.
        constexpr uintptr_t IsGameRuntime   = 0x0012C710;
        constexpr uintptr_t SecureVar_setValue = 0x00BC6460; // AntiTamper::SecureVariable<float>::setValue
        constexpr uintptr_t SecureVar_setValue2 = 0x00BC64B0; // AntiTamper::SecureVariable<...> overload
    }
}

} // namespace gw2
