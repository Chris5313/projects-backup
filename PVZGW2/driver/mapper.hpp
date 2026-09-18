#pragma once
#include <ntddk.h>


// Diagnostic (EAAC observation): full VA snapshot to gw2_dump.txt.
// idx < 0 = baseline (pre-inject). RunObservationDumps: 1s polling for
// 600s post-success, change-triggered dumps (region/PTE/content).
VOID DumpSnapshot(PEPROCESS Target, int Idx, const char* Tag);
VOID RunObservationDumps(PEPROCESS Target);
NTSTATUS MapUserDll(PEPROCESS Target, PVOID DllBuffer);

// Decrypted runtime image of the main game module →
// C:\Users\Public\gw2_image_dump_<Idx>.bin + manifest. Idx 0 = right after
// injection (boot-decrypted), Idx 1 = end of the observation window (pages
// Arxan decrypts on demand during play). Independent of injection success.
VOID DumpThreadRips(PEPROCESS Target);
VOID DumpGameImage(PEPROCESS Target, int Idx);
