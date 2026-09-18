#pragma once
#include <ntddk.h>

// PC Speaker ports
#define TIMER_PORT_2    0x42
#define TIMER_CONTROL   0x43
#define SPEAKER_PORT    0x61

// Beep frequencies
#define BEEP_FREQUENCY  800  // Hz
#define BEEP_SUCCESS    1000 // Hz (higher pitch for success)
#define BEEP_ERROR      400  // Hz (lower pitch for error)

inline VOID KernelBeep(ULONG frequency, ULONG durationMs)
{
    // Calculate divisor for timer
    ULONG divisor = 1193180 / frequency;

    // Set up the timer
    WRITE_PORT_UCHAR((PUCHAR)TIMER_CONTROL, 0xB6);
    WRITE_PORT_UCHAR((PUCHAR)TIMER_PORT_2, (UCHAR)(divisor & 0xFF));
    WRITE_PORT_UCHAR((PUCHAR)TIMER_PORT_2, (UCHAR)(divisor >> 8));

    // Turn on speaker
    UCHAR speakerState = READ_PORT_UCHAR((PUCHAR)SPEAKER_PORT);
    WRITE_PORT_UCHAR((PUCHAR)SPEAKER_PORT, speakerState | 0x03);

    // Wait for duration
    LARGE_INTEGER delay;
    delay.QuadPart = -(LONGLONG)durationMs * 10000LL; // Convert ms to 100ns units
    KeDelayExecutionThread(KernelMode, FALSE, &delay);

    // Turn off speaker
    speakerState = READ_PORT_UCHAR((PUCHAR)SPEAKER_PORT);
    WRITE_PORT_UCHAR((PUCHAR)SPEAKER_PORT, speakerState & 0xFC);
}

// Quick beep patterns
inline VOID BeepDriverLoaded()
{
    KernelBeep(BEEP_FREQUENCY, 100);  // Short beep
}

inline VOID BeepError()
{
    KernelBeep(BEEP_ERROR, 300);      // Lower pitch, longer
}

inline VOID BeepDoubleSuccess()
{
    KernelBeep(BEEP_SUCCESS, 100);
    LARGE_INTEGER delay;
    delay.QuadPart = -500000LL; // 50ms
    KeDelayExecutionThread(KernelMode, FALSE, &delay);
    KernelBeep(BEEP_SUCCESS, 100);
}
