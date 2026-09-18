#pragma once
#include <ntddk.h>

NTSTATUS MapUserDll(PEPROCESS Target, PVOID DllBuffer);
