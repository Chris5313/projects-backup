#include <windows.h>

BOOL WINAPI DllMain(HINSTANCE hInst, DWORD reason, LPVOID)
{
    if (reason != DLL_PROCESS_ATTACH) return TRUE;

    // Write a marker to our own header page (PAGE_READWRITE, zeroed by mapper).
    // If APC dispatch works at all, this byte changes from 0 to 0x42.
    // Check with: ReadProcessMemory at mapped_base + 0x100
    volatile unsigned char* marker = (volatile unsigned char*)((ULONG_PTR)hInst + 0x100);
    *marker = 0x42;

    return TRUE;
}
