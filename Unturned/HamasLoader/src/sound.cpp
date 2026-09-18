#include "sound.h"
#include <windows.h>
#include <mmsystem.h>
#include <cstdio>

#pragma comment(lib, "winmm.lib")

#define IDR_SND_HAMAS    301
#define IDR_SND_CLICK    302
#define IDR_SND_ACTIVATE 303

namespace Sound {

    static bool s_initialized = false;
    static bool s_songPlaying = false;
    static char s_songPath[MAX_PATH] = {};

    // --- waveOut-based short sounds -----------------------------------------
    // PlaySoundA owns a single async channel, so the music loop gets clobbered
    // when a click fires. These short UI sounds use waveOut instead, which
    // mixes independently and leaves the music alone.
    struct WaveSound {
        HWAVEOUT hWave = nullptr;
        WAVEHDR headers[8] = {};
        int next = 0;
        WAVEFORMATEX fmt = {};
        const BYTE* data = nullptr;
        DWORD dataSize = 0;
    };
    static WaveSound s_click;
    static WaveSound s_activate;

    static bool ParseWav(const BYTE* p, DWORD size, WAVEFORMATEX* fmt,
                         const BYTE** dataOut, DWORD* dataSizeOut) {
        if (size < 44) return false;
        if (memcmp(p, "RIFF", 4) != 0) return false;
        if (memcmp(p + 8, "WAVE", 4) != 0) return false;

        DWORD off = 12;
        const BYTE* dataPtr = nullptr;
        DWORD dataSz = 0;

        while (off + 8 <= size) {
            char id[5] = {0};
            memcpy(id, p + off, 4);
            DWORD chunkSize = *(DWORD*)(p + off + 4);
            DWORD bodyOff = off + 8;

            if (memcmp(id, "fmt ", 4) == 0 && chunkSize >= 16) {
                fmt->wFormatTag      = *(WORD*)(p + bodyOff);
                fmt->nChannels       = *(WORD*)(p + bodyOff + 2);
                fmt->nSamplesPerSec  = *(DWORD*)(p + bodyOff + 4);
                fmt->nAvgBytesPerSec = *(DWORD*)(p + bodyOff + 8);
                fmt->nBlockAlign     = *(WORD*)(p + bodyOff + 12);
                fmt->wBitsPerSample  = *(WORD*)(p + bodyOff + 14);
                fmt->cbSize          = 0;
            } else if (memcmp(id, "data", 4) == 0) {
                dataPtr = p + bodyOff;
                dataSz  = chunkSize;
            }

            // advance (chunks are word-aligned)
            off = bodyOff + chunkSize + (chunkSize & 1);
        }

        if (!dataPtr || !dataSz) return false;
        *dataOut = dataPtr;
        *dataSizeOut = dataSz;
        return true;
    }

    static bool InitWaveSound(int resId, WaveSound& s) {
        HRSRC hr = FindResource(nullptr, MAKEINTRESOURCE(resId), RT_RCDATA);
        if (!hr) return false;
        HGLOBAL hg = LoadResource(nullptr, hr);
        if (!hg) return false;
        DWORD sz = SizeofResource(nullptr, hr);
        const BYTE* p = (const BYTE*)LockResource(hg);
        if (!p || !sz) return false;

        if (!ParseWav(p, sz, &s.fmt, &s.data, &s.dataSize)) return false;

        // Data stays valid for the module's lifetime (resource section).
        if (waveOutOpen(&s.hWave, WAVE_MAPPER, &s.fmt, 0, 0, CALLBACK_NULL) != MMSYSERR_NOERROR) {
            return false;
        }
        return true;
    }

    static void PlayWave(WaveSound& s) {
        if (!s.hWave || !s.data || !s.dataSize) return;

        WAVEHDR& hdr = s.headers[s.next];
        s.next = (s.next + 1) % 8;

        // Reuse the slot: if the old header is still playing, unprepare it.
        if (hdr.dwFlags & WHDR_PREPARED) {
            waveOutUnprepareHeader(s.hWave, &hdr, sizeof(hdr));
        }

        hdr.lpData         = (LPSTR)s.data;
        hdr.dwBufferLength = s.dataSize;
        hdr.dwFlags        = 0;
        hdr.dwLoops        = 0;
        if (waveOutPrepareHeader(s.hWave, &hdr, sizeof(hdr)) == MMSYSERR_NOERROR) {
            waveOutWrite(s.hWave, &hdr, sizeof(hdr));
        }
    }

    // --- music (PlaySoundA) -------------------------------------------------
    static void ExtractSongToTemp() {
        HRSRC hr = FindResource(nullptr, MAKEINTRESOURCE(IDR_SND_HAMAS), RT_RCDATA);
        if (!hr) return;
        HGLOBAL hg = LoadResource(nullptr, hr);
        if (!hg) return;
        DWORD sz = SizeofResource(nullptr, hr);
        const void* data = LockResource(hg);
        if (!data || !sz) return;

        char tempDir[MAX_PATH];
        GetTempPathA(MAX_PATH, tempDir);
        snprintf(s_songPath, MAX_PATH, "%shc_song.wav", tempDir);

        HANDLE hFile = CreateFileA(s_songPath, GENERIC_WRITE, 0, nullptr,
            CREATE_ALWAYS, FILE_ATTRIBUTE_HIDDEN | FILE_ATTRIBUTE_TEMPORARY, nullptr);
        if (hFile == INVALID_HANDLE_VALUE) return;
        DWORD written = 0;
        WriteFile(hFile, data, sz, &written, nullptr);
        CloseHandle(hFile);
    }

    // --- public API ---------------------------------------------------------

    void Init() {
        ExtractSongToTemp();
        InitWaveSound(IDR_SND_CLICK,     s_click);
        InitWaveSound(IDR_SND_ACTIVATE,  s_activate);
        s_initialized = true;
    }

    void PlayHover() {
    }

    void PlayClick() {
        if (!s_initialized) return;
        PlayWave(s_click);
    }

    void PlayActivate() {
        if (!s_initialized) return;
        PlayWave(s_activate);
    }

    void PlayHamasSong() {
        if (!s_initialized || !s_songPath[0]) return;
        PlaySoundA(s_songPath, nullptr, SND_FILENAME | SND_ASYNC | SND_LOOP | SND_NODEFAULT);
        s_songPlaying = true;
    }

    void StopHamasSong() {
        if (s_songPlaying) {
            PlaySoundA(nullptr, nullptr, 0);
            s_songPlaying = false;
        }
    }

    void UpdateHover(bool anyItemHovered) {
        (void)anyItemHovered;
    }
}
