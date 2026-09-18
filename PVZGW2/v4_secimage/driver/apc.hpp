#pragma once
#include <ntddk.h>

// Execute the payload's image entry in the target via thread hijack.
// `thunk` = mapped VA of the payload's exported kmap_thunk (.text): the
// driver writes the thread's original RIP into the payload's kmap_ctx cell
// (`ctx`) and redirects KTRAP_FRAME::Rip to the thunk. The thunk saves the
// full user context, calls DllMain(base, DLL_PROCESS_ATTACH, NULL), restores
// the context and jumps back to the original RIP. Retries every 2s up to
// ~30s if no suitable thread is found.
NTSTATUS HijackAndInject(PEPROCESS Target, PVOID Thunk, PVOID Ctx);
