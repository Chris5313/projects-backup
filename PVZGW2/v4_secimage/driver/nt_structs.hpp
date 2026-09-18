#pragma once
// ntifs.h is force-included via ForcedIncludeFiles in kmap.vcxproj.
// It is a superset of ntddk.h and provides KAPC_STATE, KeStackAttachProcess, etc.
// Do NOT include ntddk.h or ntifs.h here — the force-include handles it.
#include <ntimage.h>  // IMAGE_* PE types

// ---------------------------------------------------------------------------
// SystemProcessInformation (class 5) — process + thread enumeration
// ---------------------------------------------------------------------------
typedef struct _SYSTEM_THREAD_INFORMATION {
    LARGE_INTEGER KernelTime;
    LARGE_INTEGER UserTime;
    LARGE_INTEGER CreateTime;
    ULONG         WaitTime;
    PVOID         StartAddress;
    CLIENT_ID     ClientId;      // .UniqueProcess / .UniqueThread
    LONG          Priority;
    LONG          BasePriority;
    ULONG         ContextSwitches;
    ULONG         ThreadState;
    ULONG         WaitReason;
} SYSTEM_THREAD_INFORMATION, *PSYSTEM_THREAD_INFORMATION;

typedef struct _SYSTEM_PROCESS_INFORMATION {
    ULONG          NextEntryOffset;
    ULONG          NumberOfThreads;
    LARGE_INTEGER  WorkingSetPrivateSize;
    ULONG          HardFaultCount;
    ULONG          NumberOfThreadsHighWatermark;
    ULONGLONG      CycleTime;
    LARGE_INTEGER  CreateTime;
    LARGE_INTEGER  UserTime;
    LARGE_INTEGER  KernelTime;
    UNICODE_STRING ImageName;        // process exe name (no path)
    LONG           BasePriority;
    HANDLE         UniqueProcessId;
    HANDLE         InheritedFromUniqueProcessId;
    ULONG          HandleCount;
    ULONG          SessionId;
    ULONG_PTR      UniqueProcessKey;
    SIZE_T         PeakVirtualSize;
    SIZE_T         VirtualSize;
    ULONG          PageFaultCount;
    SIZE_T         PeakWorkingSetSize;
    SIZE_T         WorkingSetSize;
    SIZE_T         QuotaPeakPagedPoolUsage;
    SIZE_T         QuotaPagedPoolUsage;
    SIZE_T         QuotaPeakNonPagedPoolUsage;
    SIZE_T         QuotaNonPagedPoolUsage;
    SIZE_T         PagefileUsage;
    SIZE_T         PeakPagefileUsage;
    SIZE_T         PrivatePageCount;
    LARGE_INTEGER  ReadOperationCount;
    LARGE_INTEGER  WriteOperationCount;
    LARGE_INTEGER  OtherOperationCount;
    LARGE_INTEGER  ReadTransferCount;
    LARGE_INTEGER  WriteTransferCount;
    LARGE_INTEGER  OtherTransferCount;
    SYSTEM_THREAD_INFORMATION Threads[1];
} SYSTEM_PROCESS_INFORMATION, *PSYSTEM_PROCESS_INFORMATION;

// ---------------------------------------------------------------------------
// SystemModuleInformation (class 11)
// ---------------------------------------------------------------------------
typedef struct _RTL_PROCESS_MODULE_INFORMATION {
    HANDLE  Section;
    PVOID   MappedBase;
    PVOID   ImageBase;
    ULONG   ImageSize;
    ULONG   Flags;
    USHORT  LoadOrderIndex;
    USHORT  InitOrderIndex;
    USHORT  LoadCount;
    USHORT  OffsetToFileName;
    UCHAR   FullPathName[256];
} RTL_PROCESS_MODULE_INFORMATION, *PRTL_PROCESS_MODULE_INFORMATION;

typedef struct _RTL_PROCESS_MODULES {
    ULONG NumberOfModules;
    RTL_PROCESS_MODULE_INFORMATION Modules[1];
} RTL_PROCESS_MODULES, *PRTL_PROCESS_MODULES;

// ---------------------------------------------------------------------------
// Minimal user-mode PEB / loader structures (read from user space while attached)
// Based on x64 stable offsets Win7 – Win11
// ---------------------------------------------------------------------------
typedef struct _KMAP_UNICODE_STRING {
    USHORT Length;
    USHORT MaximumLength;
    PWSTR  Buffer;
} KMAP_UNICODE_STRING;

typedef struct _KMAP_LDR_ENTRY {
    LIST_ENTRY          InLoadOrderLinks;
    LIST_ENTRY          InMemoryOrderLinks;  // used by InMemoryOrderModuleList
    LIST_ENTRY          InInitializationOrderLinks;
    PVOID               DllBase;
    PVOID               EntryPoint;
    ULONG               SizeOfImage;
    KMAP_UNICODE_STRING FullDllName;
    KMAP_UNICODE_STRING BaseDllName;
} KMAP_LDR_ENTRY, *PKMAP_LDR_ENTRY;

typedef struct _KMAP_PEB_LDR {
    ULONG     Length;
    BOOLEAN   Initialized;
    PVOID     SsHandle;
    LIST_ENTRY InLoadOrderModuleList;
    LIST_ENTRY InMemoryOrderModuleList;
} KMAP_PEB_LDR, *PKMAP_PEB_LDR;

// NMI callback list (KiNmiCallbackListHead) — kept for future NMI-related work.
typedef struct _KNMI_HANDLER_CALLBACK {
    struct _KNMI_HANDLER_CALLBACK* Next;
    PVOID                          Callback;
    PVOID                          Context;
    PVOID                          Handle;
} KNMI_HANDLER_CALLBACK, *PKNMI_HANDLER_CALLBACK;

// ---------------------------------------------------------------------------
// Internal ntoskrnl exports — not in WDK public headers
// ---------------------------------------------------------------------------
extern "C" {
    // PsGetProcessPeb is in ntoskrnl.lib but not always declared in the .h path
    // we compile against — force-declare here.
    PPEB      NTAPI PsGetProcessPeb(PEPROCESS Process);

    // System info
    NTSTATUS  NTAPI ZwQuerySystemInformation(ULONG Class, PVOID Buf, ULONG Len, PULONG Ret);

    // Virtual memory
    NTSTATUS  NTAPI ZwProtectVirtualMemory(
        HANDLE   ProcessHandle,
        PVOID*   BaseAddress,
        PSIZE_T  NumberOfBytesToProtect,
        ULONG    NewProtect,
        PULONG   OldProtect);

    // Thread alert — takes PETHREAD directly, force-declared because not in
    // the public WDK header on all builds.
    BOOLEAN NTAPI KeAlertThread(
        PETHREAD        Thread,
        KPROCESSOR_MODE AlertMode);
}
