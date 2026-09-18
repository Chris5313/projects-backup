#pragma once
#include <ntddk.h>

// Execute DllMain in the target process via thread hijacking.
// Suspends a non-main waiting thread, writes shellcode into the mapped PE
// header area (base+0x200), redirects RIP, resumes + alerts to force user-mode
// return. Retries every 2s up to ~30s if no suitable thread is found.
NTSTATUS HijackAndInject(PEPROCESS Target, PVOID EntryPoint, PVOID DllBase);
// Register a leaked (abandoned) payload image range so the hijacker never
// treats its stale shellcode RIP as a valid return address on a retry.
VOID ApcRegisterLeakedRange(ULONG64 Base, ULONG64 Size);
