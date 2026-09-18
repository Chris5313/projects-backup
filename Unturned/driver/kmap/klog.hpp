#pragma once
#include <ntstrsafe.h>

#define KLOG_PATH L"\\??\\C:\\Users\\Public\\msec_trace.log"

inline void KLog(const char* msg)
{
    if (KeGetCurrentIrql() > PASSIVE_LEVEL) {
        DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_INFO_LEVEL, "[diag] %s\n", msg);
        return;
    }

    UNICODE_STRING uPath;
    RtlInitUnicodeString(&uPath, KLOG_PATH);
    OBJECT_ATTRIBUTES oa;
    InitializeObjectAttributes(&oa, &uPath,
        OBJ_CASE_INSENSITIVE | OBJ_KERNEL_HANDLE, NULL, NULL);

    HANDLE h = nullptr;
    IO_STATUS_BLOCK iosb = {};
    LARGE_INTEGER offset; offset.QuadPart = -1;

    NTSTATUS s = ZwCreateFile(&h,
        FILE_APPEND_DATA | SYNCHRONIZE, &oa, &iosb,
        NULL, FILE_ATTRIBUTE_NORMAL, FILE_SHARE_READ | FILE_SHARE_WRITE,
        FILE_OPEN_IF,
        FILE_SYNCHRONOUS_IO_NONALERT | FILE_NON_DIRECTORY_FILE,
        NULL, 0);
    if (!NT_SUCCESS(s)) return;

    LARGE_INTEGER sysTime, localTime;
    KeQuerySystemTime(&sysTime);
    ExSystemTimeToLocalTime(&sysTime, &localTime);
    TIME_FIELDS tf;
    RtlTimeToTimeFields(&localTime, &tf);

    char line[512];
    size_t remain = 0;
    RtlStringCbPrintfExA(line, sizeof line, nullptr, &remain, 0,
        "[%04d-%02d-%02d %02d:%02d:%02d.%03d] %s\r\n",
        tf.Year, tf.Month, tf.Day, tf.Hour, tf.Minute, tf.Second, tf.Milliseconds, msg);
    ULONG len = (ULONG)(sizeof line - remain);

    ZwWriteFile(h, NULL, NULL, NULL, &iosb, line, len, &offset, NULL);
    ZwClose(h);

    DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_INFO_LEVEL, "[diag] %s\n", msg);
}

inline void KLogHex(const char* prefix, ULONG_PTR val)
{
    char buf[64];
    RtlStringCbPrintfA(buf, sizeof buf, "%s0x%p", prefix, (PVOID)val);
    KLog(buf);
}

// One-shot success signal that user-mode loader watches for.
// Only called after the driver has read back the DllMain marker byte, so
// existence of this file means DllMain executed inside the target process.
inline void WriteHitFile(const char* msg)
{
    if (KeGetCurrentIrql() > PASSIVE_LEVEL) return;

    UNICODE_STRING uPath;
    RtlInitUnicodeString(&uPath, L"\\??\\C:\\Users\\Public\\msec_done.log");
    OBJECT_ATTRIBUTES oa;
    InitializeObjectAttributes(&oa, &uPath,
        OBJ_CASE_INSENSITIVE | OBJ_KERNEL_HANDLE, NULL, NULL);

    HANDLE h = nullptr;
    IO_STATUS_BLOCK iosb = {};
    NTSTATUS s = ZwCreateFile(&h,
        GENERIC_WRITE | SYNCHRONIZE, &oa, &iosb,
        NULL, FILE_ATTRIBUTE_NORMAL, FILE_SHARE_READ,
        FILE_OVERWRITE_IF,
        FILE_SYNCHRONOUS_IO_NONALERT | FILE_NON_DIRECTORY_FILE,
        NULL, 0);
    if (!NT_SUCCESS(s)) return;

    size_t len = 0;
    while (msg[len] && len < 256) len++;
    ZwWriteFile(h, NULL, NULL, NULL, &iosb, (PVOID)msg, (ULONG)len, NULL, NULL);
    ZwClose(h);
}
