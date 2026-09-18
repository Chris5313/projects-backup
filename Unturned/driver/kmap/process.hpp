#pragma once

// Find process by image name via ZwQuerySystemInformation(5).
// Caller must ObDereferenceObject(*out) when done.
NTSTATUS FindProcessByName(const char* name, PEPROCESS* out);

// Walk export table of a module mapped in the current address space.
PVOID GetExportByName(PVOID moduleBase, const char* exportName);
PVOID GetExportByOrdinal(PVOID moduleBase, ULONG ordinal);

// Get base address of a kernel module by filename (e.g. "ntoskrnl.exe").
PVOID GetKernelModuleBase(const char* name);

// Get base address of a user-mode module by name.
// Must be called while KeStackAttachProcess is active on the target.
PVOID GetUserModuleBase(const WCHAR* name);
