// A/B isolation test: marker-only payload.
// Same driver, same mapping, same thread-hijack entry — but DllMain does
// NOTHING except write the 0x42 success marker. No VEH, no MinHook, no
// vtable patches, no overlay, no log files, no imports beyond the loader.
//
// Result interpretation (machine behavior with this payload):
//   survives 5+ min -> EAAC kills on DLL runtime artifacts (hooks/vtable)
//   resets ~3 min    -> EAAC kills on the driver's mapping itself
//                       (unbacked private RW region with exec PTEs)
#include <windows.h>

BOOL WINAPI DllMain(HINSTANCE hInst, DWORD reason, LPVOID)
{
    if (reason != DLL_PROCESS_ATTACH) return TRUE;
    *(volatile unsigned char*)((ULONG_PTR)hInst + 0x100) = 0x42;
    return TRUE;
}
