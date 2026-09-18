#pragma once
#include <ntimage.h>

inline PIMAGE_NT_HEADERS64 GetNtHeaders(PVOID base)
{
    auto dos = (PIMAGE_DOS_HEADER)base;
    if (dos->e_magic != IMAGE_DOS_SIGNATURE) return nullptr;
    auto nt = (PIMAGE_NT_HEADERS64)((ULONG_PTR)base + dos->e_lfanew);
    if (nt->Signature != IMAGE_NT_SIGNATURE) return nullptr;
    return nt;
}


inline ULONG CharsToProt(ULONG c)
{
    bool x = (c & IMAGE_SCN_MEM_EXECUTE) != 0;
    bool w = (c & IMAGE_SCN_MEM_WRITE)   != 0;
    bool r = (c & IMAGE_SCN_MEM_READ)    != 0;
    if (x && w)  return PAGE_EXECUTE_READWRITE;
    if (x)       return PAGE_EXECUTE_READ;
    if (w)       return PAGE_READWRITE;
    if (r)       return PAGE_READONLY;
    return PAGE_NOACCESS;
}

// Section name compare (8-byte fixed-width, null-padded)
inline bool SecNameEq(const UCHAR* secName, const char* cmp)
{
    for (int i = 0; i < 8; i++) {
        if (!cmp[i]) return secName[i] == 0;
        if (secName[i] != (UCHAR)cmp[i]) return false;
    }
    return true;
}
