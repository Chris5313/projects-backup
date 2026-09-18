#pragma once
#include <windows.h>
#include <cstdint>

// Frostbite TypeInfo structures from FrostbiteGen
// These walk the engine's type system to dump all classes

namespace fb {

class TypeInfo;
class FieldInfo;

// ClassInfo - linked list of all classes in the engine
struct ClassInfo {
    TypeInfo* typeInfo;     // 0x00
    ClassInfo* next;        // 0x08
    uint16_t id;            // 0x10
    uint16_t isDataContainer; // 0x12
    char pad_0014[4];       // 0x14
    ClassInfo* parent;      // 0x18
    char pad_0020[8];       // 0x20
    uint16_t id3;           // 0x28
    char pad_002C[0x94];    // 0x2C
}; // Size = 0xC0

// TypeInfo - describes a class/struct
struct TypeInfo {
    const char* name;       // 0x00
    uint16_t flags;         // 0x08
    uint16_t totalSize;     // 0x0A
    char pad_000C[4];       // 0x0C
    uint16_t flags2;        // 0x10
    char pad_0012[6];       // 0x12
    uint16_t alignment;     // 0x18
    uint16_t fieldCount;    // 0x1A
    char pad_001C[4];       // 0x1C
    FieldInfo* enumFields;  // 0x20
    FieldInfo* structFields;// 0x28
    FieldInfo* fields;      // 0x30
}; // Size = 0x38

struct MemberTypeInfo {
    TypeInfo* typeInfo;     // 0x00
    uint16_t flags;         // 0x08
    char pad[8];            // 0x0A
};

// FieldInfo - describes a field in a class
struct FieldInfo {
    const char* name;       // 0x00
    uint16_t flags;         // 0x08
    uint16_t offset;        // 0x0A
    char pad[4];            // 0x0C
    MemberTypeInfo* typeInfo; // 0x10
};

// Pattern to find ClassInfo list head (from FrostbiteGen)
// This works for Frostbite 3 games like BF4, SWBF, PvZ GW2
constexpr const char* TYPEINFO_PATTERN = 
    "\x48\x8B\x05\x00\x00\x00\x00\x48\x89\x41\x08\x48\x89\x0D\x00\x00\x00\x00\xC3";
constexpr const char* TYPEINFO_MASK = "xxx????xxxxxxx????x";

// Alternative patterns for different FB versions
constexpr const char* ALT_PATTERN1 = 
    "\x48\x8B\x05\x00\x00\x00\x00\x48\x85\xC0\x74\x00\x48\x8B\x00\x48\x89\x05";
constexpr const char* ALT_MASK1 = "xxx????xxxx?xxxxxx";

// Find the first ClassInfo
ClassInfo* FindClassInfoHead(uintptr_t base, size_t size);

// Dump all classes to a file
void DumpAllClasses(const char* filepath);

// Find a specific class by name
ClassInfo* FindClass(const char* name);

// Dump fields of a specific class
void DumpClassFields(ClassInfo* ci, char* buf, size_t bufSize);

// Search for classes matching a pattern
void SearchClasses(const char* pattern, char* buf, size_t bufSize);

} // namespace fb
