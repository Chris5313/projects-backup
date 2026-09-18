#pragma once
// ═══════════════════════════════════════════════════════════════════
// Compile-time XOR string encryption — header-only, C++17
//
// Usage:  XS("secret string")   → returns const char* (decrypted on stack)
//         XSW(L"wide string")   → returns const wchar_t*
//
// Each string gets a unique 64-bit key derived from __LINE__ + __COUNTER__
// The encrypted bytes live in .rdata; decryption happens at runtime
// into a function-local static buffer (one-time per call site).
// ═══════════════════════════════════════════════════════════════════

#include <cstdint>
#include <cstddef>
#include <utility>

namespace xorstr_detail {

// Compile-time FNV-1a hash for key generation
constexpr uint64_t fnv1a_seed = 0xcbf29ce484222325ULL;
constexpr uint64_t fnv1a_prime = 0x100000001b3ULL;

constexpr uint64_t fnv1a(uint64_t seed, uint64_t val) {
    return (seed ^ val) * fnv1a_prime;
}

constexpr uint64_t make_key(uint64_t a, uint64_t b, uint64_t c) {
    uint64_t h = fnv1a_seed;
    h = fnv1a(h, a);
    h = fnv1a(h, b);
    h = fnv1a(h, c);
    // Ensure no zero bytes in key (would leave chars unencrypted)
    h |= 0x0101010101010101ULL;
    return h;
}

template <typename CharT, size_t N, uint64_t Key>
class encrypted_string {
public:
    // Encrypt at compile time
    constexpr encrypted_string(const CharT (&str)[N])
        : m_data{}
    {
        for (size_t i = 0; i < N; ++i) {
            uint8_t keyByte = static_cast<uint8_t>((Key >> ((i % 8) * 8)) & 0xFF);
            if constexpr (sizeof(CharT) == 1) {
                m_data[i] = static_cast<CharT>(
                    static_cast<uint8_t>(str[i]) ^ keyByte);
            } else {
                // Wide char: XOR low byte, leave high byte (sufficient for ASCII/Latin)
                m_data[i] = static_cast<CharT>(
                    static_cast<uint16_t>(str[i]) ^ keyByte);
            }
        }
    }

    // Decrypt at runtime — uses byte array to avoid shift patterns
    // that MSVC compiles into BMI2/VEX (vpdep) instructions
    __declspec(noinline) const CharT* decrypt() const {
        // Force key into volatile stack var to prevent BMI2 codegen
        volatile uint64_t vk = Key;
        uint64_t k = vk;
        unsigned char kb[8];
        kb[0] = (unsigned char)(k      ); kb[1] = (unsigned char)(k >>  8);
        kb[2] = (unsigned char)(k >> 16); kb[3] = (unsigned char)(k >> 24);
        kb[4] = (unsigned char)(k >> 32); kb[5] = (unsigned char)(k >> 40);
        kb[6] = (unsigned char)(k >> 48); kb[7] = (unsigned char)(k >> 56);
        for (size_t i = 0; i < N; ++i) {
            unsigned char x = kb[i & 7];
            m_decrypted[i] = static_cast<CharT>(
                static_cast<unsigned char>(m_data[i]) ^ x);
        }
        return m_decrypted;
    }

    // Allow implicit conversion
    operator const CharT*() const { return decrypt(); }

private:
    CharT m_data[N];          // Encrypted (in .rdata at compile time)
    mutable CharT m_decrypted[N]; // Decrypted at runtime (on stack/bss)
};

} // namespace xorstr_detail

// ─── Public macros ─────────────────────────────────────────────────
// Each call site gets a unique key from __LINE__ and __COUNTER__
#define XS_KEY_ ::xorstr_detail::make_key(__LINE__, __COUNTER__, \
    sizeof(__FILE__) + sizeof(__DATE__) + sizeof(__TIME__))

#define XS(str)  (::xorstr_detail::encrypted_string<char, sizeof(str), XS_KEY_>(str).decrypt())
#define XSW(str) (::xorstr_detail::encrypted_string<wchar_t, sizeof(str)/sizeof(wchar_t), XS_KEY_>(str).decrypt())
