#pragma once
#include <windows.h>
#include <cstdint>
#include <cstdio>

// Dump the game's main module as a proper PE file
inline bool DumpGamePE(const char* outputPath) {
    HMODULE hModule = GetModuleHandleA(nullptr);
    if (!hModule) return false;

    auto dosHeader = (PIMAGE_DOS_HEADER)hModule;
    if (dosHeader->e_magic != IMAGE_DOS_SIGNATURE) return false;

    auto ntHeaders = (PIMAGE_NT_HEADERS)((uint8_t*)hModule + dosHeader->e_lfanew);
    if (ntHeaders->Signature != IMAGE_NT_SIGNATURE) return false;

    // Calculate total size from section headers
    auto sectionHeader = IMAGE_FIRST_SECTION(ntHeaders);
    DWORD totalSize = 0;
    
    for (WORD i = 0; i < ntHeaders->FileHeader.NumberOfSections; i++) {
        DWORD sectionEnd = sectionHeader[i].VirtualAddress + sectionHeader[i].Misc.VirtualSize;
        if (sectionEnd > totalSize) totalSize = sectionEnd;
    }

    // Round up to page boundary
    totalSize = (totalSize + 0xFFF) & ~0xFFF;

    FILE* f = fopen(outputPath, "wb");
    if (!f) return false;

    // Write entire image
    size_t written = fwrite(hModule, 1, totalSize, f);
    fclose(f);

    return written == totalSize;
}

// Dump with fixed sections (better for static analysis)
inline bool DumpGamePEFixed(const char* outputPath) {
    HMODULE hModule = GetModuleHandleA(nullptr);
    if (!hModule) return false;

    auto dosHeader = (PIMAGE_DOS_HEADER)hModule;
    if (dosHeader->e_magic != IMAGE_DOS_SIGNATURE) return false;

    auto ntHeaders = (PIMAGE_NT_HEADERS)((uint8_t*)hModule + dosHeader->e_lfanew);
    if (ntHeaders->Signature != IMAGE_NT_SIGNATURE) return false;

    auto sectionHeader = IMAGE_FIRST_SECTION(ntHeaders);
    WORD numSections = ntHeaders->FileHeader.NumberOfSections;

    // Calculate image size
    DWORD imageSize = ntHeaders->OptionalHeader.SizeOfImage;

    // Allocate buffer
    uint8_t* buffer = (uint8_t*)VirtualAlloc(nullptr, imageSize, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
    if (!buffer) return false;

    // Copy headers
    memcpy(buffer, hModule, ntHeaders->OptionalHeader.SizeOfHeaders);

    // Copy sections
    for (WORD i = 0; i < numSections; i++) {
        DWORD srcAddr = sectionHeader[i].VirtualAddress;
        DWORD srcSize = sectionHeader[i].Misc.VirtualSize;
        
        if (srcSize > 0 && srcAddr + srcSize <= imageSize) {
            memcpy(buffer + srcAddr, (uint8_t*)hModule + srcAddr, srcSize);
        }
    }

    // Fix section headers for file alignment = section alignment (raw dump)
    auto fixedNtHeaders = (PIMAGE_NT_HEADERS)(buffer + dosHeader->e_lfanew);
    auto fixedSections = IMAGE_FIRST_SECTION(fixedNtHeaders);
    
    for (WORD i = 0; i < numSections; i++) {
        fixedSections[i].PointerToRawData = fixedSections[i].VirtualAddress;
        fixedSections[i].SizeOfRawData = fixedSections[i].Misc.VirtualSize;
    }

    // Write
    FILE* f = fopen(outputPath, "wb");
    if (!f) {
        VirtualFree(buffer, 0, MEM_RELEASE);
        return false;
    }

    fwrite(buffer, 1, imageSize, f);
    fclose(f);
    VirtualFree(buffer, 0, MEM_RELEASE);

    return true;
}
