#include "fb_dumper.h"
#include <windows.h>
#include <cstdio>
#include <cstring>

// Forward declare the overlay logger
namespace oilog { void Line(const char*); }

namespace fb {

static ClassInfo* g_classHead = nullptr;

// Pattern matching helper
static bool DataCompare(const uint8_t* data, const uint8_t* pattern, const char* mask) {
    for (; *mask; ++mask, ++data, ++pattern) {
        if (*mask == 'x' && *data != *pattern)
            return false;
    }
    return true;
}

static uintptr_t FindPattern(uintptr_t start, size_t len, const uint8_t* pattern, const char* mask) {
    size_t patLen = strlen(mask);
    for (size_t i = 0; i < len - patLen; i++) {
        if (DataCompare((const uint8_t*)(start + i), pattern, mask))
            return start + i;
    }
    return 0;
}

ClassInfo* FindClassInfoHead(uintptr_t base, size_t size) {
    if (g_classHead) return g_classHead;
    
    // Try main pattern
    uintptr_t match = FindPattern(base, size, 
        (const uint8_t*)TYPEINFO_PATTERN, TYPEINFO_MASK);
    
    if (!match) {
        // Try alternate pattern
        match = FindPattern(base, size,
            (const uint8_t*)ALT_PATTERN1, ALT_MASK1);
        oilog::Line("FB_DUMP: Alt pattern search...");
    }
    
    if (!match) {
        oilog::Line("FB_DUMP: Pattern not found");
        return nullptr;
    }
    
    // Resolve RIP-relative address
    int32_t offset = *(int32_t*)(match + 3);
    uintptr_t addr = match + 7 + offset;
    
    // Check if it's a negative offset (sign extension for 64-bit)
    g_classHead = *(ClassInfo**)addr;
    
    char buf[256];
    snprintf(buf, sizeof(buf), "FB_DUMP: ClassInfo head at 0x%llX (via match 0x%llX)", 
        (uint64_t)g_classHead, (uint64_t)match);
    oilog::Line(buf);
    
    return g_classHead;
}

// Helper to safely read class info (returns true if valid)
static bool SafeReadClassInfo(ClassInfo* c, char* outBuf, size_t bufSize, int* fieldsOut) {
    *fieldsOut = 0;
    int pos = 0;
    
    __try {
        if (!c || !c->typeInfo || !c->typeInfo->name)
            return false;
            
        TypeInfo* ti = c->typeInfo;
        
        // Validate name pointer
        if ((uintptr_t)ti->name < 0x10000 || (uintptr_t)ti->name > 0x7FFFFFFFFFFF)
            return false;
        if (IsBadReadPtr(ti->name, 1))
            return false;
        
        // Write header
        pos += snprintf(outBuf + pos, bufSize - pos, "\n// Class: %s\n", ti->name);
        pos += snprintf(outBuf + pos, bufSize - pos, "// Size: 0x%X (%d bytes)\n", ti->totalSize, ti->totalSize);
        pos += snprintf(outBuf + pos, bufSize - pos, "// Flags: 0x%X\n", ti->flags);
        pos += snprintf(outBuf + pos, bufSize - pos, "// Fields: %d\n", ti->fieldCount);
        
        if (c->parent && c->parent->typeInfo && c->parent->typeInfo->name) {
            if (!IsBadReadPtr(c->parent->typeInfo->name, 1)) {
                pos += snprintf(outBuf + pos, bufSize - pos, "// Parent: %s\n", c->parent->typeInfo->name);
            }
        }
        
        pos += snprintf(outBuf + pos, bufSize - pos, "struct %s {\n", ti->name);
        
        // Dump fields
        if (ti->fields && ti->fieldCount > 0) {
            for (int i = 0; i < ti->fieldCount && i < 200 && pos < (int)bufSize - 200; i++) {
                __try {
                    FieldInfo* fi = &ti->fields[i];
                    if (!fi->name || IsBadReadPtr(fi->name, 1))
                        continue;
                    
                    const char* typeName = "unknown";
                    if (fi->typeInfo && fi->typeInfo->typeInfo && fi->typeInfo->typeInfo->name) {
                        if (!IsBadReadPtr(fi->typeInfo->typeInfo->name, 1))
                            typeName = fi->typeInfo->typeInfo->name;
                    }
                    
                    pos += snprintf(outBuf + pos, bufSize - pos, "    /* 0x%03X */ %s %s;\n",
                        fi->offset, typeName, fi->name);
                    (*fieldsOut)++;
                } __except(EXCEPTION_EXECUTE_HANDLER) {
                    continue;
                }
            }
        }
        
        pos += snprintf(outBuf + pos, bufSize - pos, "};\n");
        return true;
        
    } __except(EXCEPTION_EXECUTE_HANDLER) {
        return false;
    }
}

void DumpAllClasses(const char* filepath) {
    if (!g_classHead) {
        HMODULE hMod = GetModuleHandle(NULL);
        PIMAGE_DOS_HEADER dos = (PIMAGE_DOS_HEADER)hMod;
        PIMAGE_NT_HEADERS nt = (PIMAGE_NT_HEADERS)((uint8_t*)hMod + dos->e_lfanew);
        FindClassInfoHead((uintptr_t)hMod, nt->OptionalHeader.SizeOfImage);
    }
    
    if (!g_classHead) {
        oilog::Line("FB_DUMP: Cannot dump - no ClassInfo found");
        return;
    }
    
    FILE* out = fopen(filepath, "w");
    if (!out) {
        char buf[256];
        snprintf(buf, sizeof(buf), "FB_DUMP: Cannot open %s for writing", filepath);
        oilog::Line(buf);
        return;
    }
    
    fprintf(out, "// Frostbite SDK Dump - PvZ GW2\n");
    fprintf(out, "// Generated by fb_dumper\n\n");
    
    int classCount = 0;
    int totalFields = 0;
    char classBuf[16384]; // Buffer for each class
    
    ClassInfo* c = g_classHead;
    while (c) {
        int fields = 0;
        if (SafeReadClassInfo(c, classBuf, sizeof(classBuf), &fields)) {
            fputs(classBuf, out);
            classCount++;
            totalFields += fields;
        }
        
        __try {
            c = c->next;
        } __except(EXCEPTION_EXECUTE_HANDLER) {
            break;
        }
        
        // Safety limit
        if (classCount > 50000) break;
    }
    
    fclose(out);
    
    char buf[256];
    snprintf(buf, sizeof(buf), "FB_DUMP: Dumped %d classes, %d fields to %s", 
        classCount, totalFields, filepath);
    oilog::Line(buf);
}

ClassInfo* FindClass(const char* name) {
    if (!g_classHead) {
        HMODULE hMod = GetModuleHandle(NULL);
        PIMAGE_DOS_HEADER dos = (PIMAGE_DOS_HEADER)hMod;
        PIMAGE_NT_HEADERS nt = (PIMAGE_NT_HEADERS)((uint8_t*)hMod + dos->e_lfanew);
        FindClassInfoHead((uintptr_t)hMod, nt->OptionalHeader.SizeOfImage);
    }
    
    if (!g_classHead) return nullptr;
    
    ClassInfo* c = g_classHead;
    while (c) {
        __try {
            if (c->typeInfo && c->typeInfo->name) {
                if (!IsBadReadPtr(c->typeInfo->name, 1)) {
                    if (strcmp(c->typeInfo->name, name) == 0)
                        return c;
                }
            }
        } __except(EXCEPTION_EXECUTE_HANDLER) {}
        
        __try {
            c = c->next;
        } __except(EXCEPTION_EXECUTE_HANDLER) {
            break;
        }
    }
    return nullptr;
}

void DumpClassFields(ClassInfo* ci, char* buf, size_t bufSize) {
    if (!ci || !ci->typeInfo) {
        snprintf(buf, bufSize, "NULL class");
        return;
    }
    
    __try {
        TypeInfo* ti = ci->typeInfo;
        int pos = snprintf(buf, bufSize, "Class %s (size=0x%X, fields=%d):\n",
            ti->name ? ti->name : "?", ti->totalSize, ti->fieldCount);
        
        if (ti->fields && ti->fieldCount > 0) {
            for (int i = 0; i < ti->fieldCount && i < 50 && pos < (int)bufSize - 100; i++) {
                __try {
                    FieldInfo* fi = &ti->fields[i];
                    if (!fi->name || IsBadReadPtr(fi->name, 1))
                        continue;
                    
                    const char* typeName = "?";
                    if (fi->typeInfo && fi->typeInfo->typeInfo && fi->typeInfo->typeInfo->name) {
                        if (!IsBadReadPtr(fi->typeInfo->typeInfo->name, 1))
                            typeName = fi->typeInfo->typeInfo->name;
                    }
                    
                    pos += snprintf(buf + pos, bufSize - pos, "  +0x%03X: %s %s\n",
                        fi->offset, typeName, fi->name);
                } __except(EXCEPTION_EXECUTE_HANDLER) {}
            }
        }
    } __except(EXCEPTION_EXECUTE_HANDLER) {
        snprintf(buf, bufSize, "Exception reading class");
    }
}

void SearchClasses(const char* pattern, char* buf, size_t bufSize) {
    if (!g_classHead) {
        HMODULE hMod = GetModuleHandle(NULL);
        PIMAGE_DOS_HEADER dos = (PIMAGE_DOS_HEADER)hMod;
        PIMAGE_NT_HEADERS nt = (PIMAGE_NT_HEADERS)((uint8_t*)hMod + dos->e_lfanew);
        FindClassInfoHead((uintptr_t)hMod, nt->OptionalHeader.SizeOfImage);
    }
    
    if (!g_classHead) {
        snprintf(buf, bufSize, "No ClassInfo found");
        return;
    }
    
    int pos = snprintf(buf, bufSize, "Classes matching '%s':\n", pattern);
    int count = 0;
    
    ClassInfo* c = g_classHead;
    while (c && pos < (int)bufSize - 100 && count < 100) {
        __try {
            if (c->typeInfo && c->typeInfo->name) {
                if (!IsBadReadPtr(c->typeInfo->name, 1)) {
                    // Case-insensitive substring search
                    const char* name = c->typeInfo->name;
                    bool match = false;
                    for (const char* p = name; *p && !match; p++) {
                        const char* a = p;
                        const char* b = pattern;
                        while (*a && *b) {
                            char ca = (*a >= 'A' && *a <= 'Z') ? *a + 32 : *a;
                            char cb = (*b >= 'A' && *b <= 'Z') ? *b + 32 : *b;
                            if (ca != cb) break;
                            a++; b++;
                        }
                        if (!*b) match = true;
                    }
                    
                    if (match) {
                        TypeInfo* ti = c->typeInfo;
                        pos += snprintf(buf + pos, bufSize - pos, "  %s (size=0x%X, fields=%d)\n",
                            name, ti->totalSize, ti->fieldCount);
                        count++;
                    }
                }
            }
        } __except(EXCEPTION_EXECUTE_HANDLER) {}
        
        __try {
            c = c->next;
        } __except(EXCEPTION_EXECUTE_HANDLER) {
            break;
        }
    }
    
    if (count == 0) {
        snprintf(buf + pos, bufSize - pos, "  (none found)\n");
    }
}

} // namespace fb
