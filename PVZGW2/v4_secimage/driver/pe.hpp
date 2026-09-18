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
