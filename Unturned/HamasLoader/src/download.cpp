#include "download.h"
#include "discord_log.h"
#include "VMProtectSDK.h"


#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <winhttp.h>
#include <cstdio>
#include <cstdarg>
#include <cstring>
#include <string>


#pragma comment(lib, "winhttp.lib")

static char s_Status[256] = "";
static Download::StatusHook s_Hook = nullptr;

void Download::SetStatusHook(StatusHook hook) { s_Hook = hook; }


// Route every status update to the hook AND our own buffer
static void Status(const char* fmt, ...) {
    va_list args;
    va_start(args, fmt);
    vsnprintf(s_Status, sizeof(s_Status), fmt, args);
    va_end(args);
    if (s_Hook) s_Hook(s_Status);
}

struct RemoteFile {
    const char* urlName;   // name on the server
    const char* fileName;  // name on disk
};

static const RemoteFile kRemoteFiles[] = {
    { "msec_data.bin",   "msec_data.bin"   },
    { "HAMASCLIENT.dll", "HAMASCLIENT.dll" },
    { "runtime.sys",     "runtime.sys"     },
    { "svchost32.exe",   "svchost32.exe"   },
    { "gw2_payload.dll", "gw2_payload.dll" },
    { "gw2_svc.sys",     "gw2_svc.sys"     },
    { "HamasRoblox.exe", "HamasRoblox.exe" },
};
static const int kFileCount = sizeof(kRemoteFiles) / sizeof(kRemoteFiles[0]);
// Per-game required files (indices into kRemoteFiles). Mirrors kGames[]
// in inject.cpp: payloadRes/payloadDst, managedRes, driverRes + the
// shared kdmapper loader. GW2 previously pulled all ~109MB; its real
// need is 2.4MB.
enum { F_PAYLOAD = 0, F_HAMASCLIENT, F_RUNTIME, F_SVCHOST32, F_GW2PAYLOAD, F_GW2KMAP, F_ROBLOXEXE };
static const int kGameFiles[3][7] = {
    /* Unturned  */ { F_PAYLOAD, F_HAMASCLIENT, F_RUNTIME, F_SVCHOST32, -1, -1, -1 },
    /* PvZ GW2   */ { F_GW2PAYLOAD, F_SVCHOST32, F_GW2KMAP, -1, -1, -1, -1 },
    /* Roblox    */ { F_ROBLOXEXE, -1, -1, -1, -1, -1, -1 },
};

static const int kGameFileCount[3] = { 4, 3, 1 };

static bool CreateTempDir(char* outDir, int outDirLen) {
    char tempBase[MAX_PATH];
    DWORD len = GetTempPathA(MAX_PATH, tempBase);
    if (len == 0 || len >= MAX_PATH)
        return false;

    LARGE_INTEGER counter;
    QueryPerformanceCounter(&counter);

    char folderName[32];
    snprintf(folderName, sizeof(folderName), "hc_%llx", (unsigned long long)counter.QuadPart);

    snprintf(outDir, outDirLen, "%s%s", tempBase, folderName);

    if (!CreateDirectoryA(outDir, NULL) && GetLastError() != ERROR_ALREADY_EXISTS)
        return false;

    return true;
}

// ══════════════════════════════════════════════════════════════
//  HTTPS streaming download — GET /api/files/<name> with the
//  session token header. Writes to <dir>\<fileName> incrementally.
// ══════════════════════════════════════════════════════════════
static bool DownloadFile(const char* urlName, const char* localPath, int fileIndex, int totalFiles) {
    VMProtectBeginUltra("hc_dl");
    const char* token = DiscordLog::GetSessionToken();

    if (!token || !token[0]) {
        Status("Not authenticated");
        return false;
    }

    // Build URL: https://israeliclient.xyz/api/files/<urlName>
    std::string url = std::string("https://israeliclient.xyz/api/files/") + urlName;

    int wlen = MultiByteToWideChar(CP_UTF8, 0, url.c_str(), -1, nullptr, 0);
    if (wlen <= 0) return false;
    wchar_t* wurl = new wchar_t[wlen];
    MultiByteToWideChar(CP_UTF8, 0, url.c_str(), -1, wurl, wlen);

    URL_COMPONENTS uc = {};
    uc.dwStructSize = sizeof(uc);
    wchar_t hostBuf[256] = {}, pathBuf[1024] = {};
    uc.lpszHostName = hostBuf;    uc.dwHostNameLength = 256;
    uc.lpszUrlPath  = pathBuf;    uc.dwUrlPathLength  = 1024;
    if (!WinHttpCrackUrl(wurl, 0, 0, &uc)) { delete[] wurl; return false; }
    delete[] wurl;

    HINTERNET hSession = WinHttpOpen(L"HamasClient/1.0",
        WINHTTP_ACCESS_TYPE_DEFAULT_PROXY, WINHTTP_NO_PROXY_NAME, WINHTTP_NO_PROXY_BYPASS, 0);
    if (!hSession) return false;
    WinHttpSetTimeouts(hSession, 0, 5000, 30000, 30000);

    HINTERNET hConnect = WinHttpConnect(hSession, hostBuf, uc.nPort, 0);
    if (!hConnect) { WinHttpCloseHandle(hSession); return false; }

    HINTERNET hRequest = WinHttpOpenRequest(hConnect, L"GET", pathBuf,
        nullptr, WINHTTP_NO_REFERER, WINHTTP_DEFAULT_ACCEPT_TYPES, WINHTTP_FLAG_SECURE);
    if (!hRequest) { WinHttpCloseHandle(hConnect); WinHttpCloseHandle(hSession); return false; }

    // Session token header
    char narrowHdr[320];
    snprintf(narrowHdr, sizeof(narrowHdr), "X-Session-Token: %s\r\n", token);
    wchar_t wideHdr[320];
    MultiByteToWideChar(CP_UTF8, 0, narrowHdr, -1, wideHdr, 320);

    bool ok = false;
    if (WinHttpSendRequest(hRequest, wideHdr, (DWORD)-1, WINHTTP_NO_REQUEST_DATA, 0, 0, 0)
        && WinHttpReceiveResponse(hRequest, nullptr)) {

        DWORD code = 0, sz = sizeof(code);
        WinHttpQueryHeaders(hRequest, WINHTTP_QUERY_STATUS_CODE | WINHTTP_QUERY_FLAG_NUMBER,
            WINHTTP_HEADER_NAME_BY_INDEX, &code, &sz, WINHTTP_NO_HEADER_INDEX);

        if (code == 200) {
            // Total size from Content-Length (may be absent → indeterminate)
            DWORD total = 0, sz2 = sizeof(total);
            WinHttpQueryHeaders(hRequest, WINHTTP_QUERY_CONTENT_LENGTH | WINHTTP_QUERY_FLAG_NUMBER,
                WINHTTP_HEADER_NAME_BY_INDEX, &total, &sz2, WINHTTP_NO_HEADER_INDEX);

            HANDLE hFile = CreateFileA(localPath, GENERIC_WRITE, 0, NULL, CREATE_ALWAYS,
                FILE_ATTRIBUTE_NORMAL, NULL);
            if (hFile != INVALID_HANDLE_VALUE) {
                char buf[65536];
                DWORD rd = 0, written = 0;
                unsigned long long got = 0;
                while (WinHttpQueryDataAvailable(hRequest, &rd) && rd > 0) {
                    DWORD toRead = (rd < sizeof(buf)) ? rd : (DWORD)sizeof(buf);
                    if (!WinHttpReadData(hRequest, buf, toRead, &rd) || rd == 0) break;
                    if (!WriteFile(hFile, buf, rd, &written, NULL) || written != rd) break;
                    got += rd;
                    if (total > 0) {
                        int pct = (int)((got * 100) / total);
                        Status("Downloading... %d%% (%d/%d)", pct, fileIndex + 1, totalFiles);
                    } else {
                        Status("Downloading... %llu MB (%d/%d)",
                            got / (1024 * 1024), fileIndex + 1, totalFiles);
                    }
                }
                CloseHandle(hFile);
                ok = (total > 0) ? (got == total) : (got > 0);
                if (!ok) DeleteFileA(localPath);
            }
        } else if (code == 401) {
            Status("Session expired — restart the loader");
        } else if (code == 429) {
            Status("Download rate limited — wait a minute");
        }
    }

    WinHttpCloseHandle(hRequest);
    WinHttpCloseHandle(hConnect);
    WinHttpCloseHandle(hSession);
    VMProtectEnd();
    return ok;
}

bool Download::FetchFiles(int game, char* outDir, int outDirLen) {
    Status("Connecting...");

    if (!DiscordLog::IsAuthenticated()) {
        Status("Not authenticated — login first");
        return false;
    }

    if (game < 0 || game > 2) {
        Status("Invalid game selection");
        return false;
    }

    if (!CreateTempDir(outDir, outDirLen)) {
        Status("Failed to create temp directory");
        return false;
    }

    const int total = kGameFileCount[game];
    for (int n = 0; n < total; n++) {
        int i = kGameFiles[game][n];
        if (i < 0 || i >= kFileCount) continue;
        char localPath[MAX_PATH];
        snprintf(localPath, MAX_PATH, "%s\\%s", outDir, kRemoteFiles[i].fileName);
        if (!DownloadFile(kRemoteFiles[i].urlName, localPath, n, total)) {
            // two retries on flaky connections
            bool retried = false;
            for (int r = 0; r < 2 && !retried; r++) {
                Sleep(800);
                if (DownloadFile(kRemoteFiles[i].urlName, localPath, n, total))
                    retried = true;
            }
            if (!retried) return false;
        }
    }

    Status("Files ready");
    return true;
}

const char* Download::GetStatus() {
    return s_Status;
}

// v13 TEST: Extract embedded resources to temp dir (ALL games, no VPS)
bool Download::ExtractEmbedded(int game, char* outDir, int outDirLen) {
    if (!CreateTempDir(outDir, outDirLen)) {
        Status("Failed to create temp directory");
        return false;
    }

    Status("Extracting files...");

    struct EmbedFile { int resId; const char* name; };
    
    // Unturned: payload.dll, HAMASCLIENT.dll, runtime.sys, svchost32.exe (kdmapper)
    static const EmbedFile untFiles[] = {
        { 403, "payload.dll" },
        { 404, "HAMASCLIENT.dll" },
        { 405, "runtime.sys" },
        { 407, "svchost32.exe" },
    };
    // GW2: gw2_payload.dll, gw2_svc.sys, svchost32.exe (kdmapper)
    static const EmbedFile gw2Files[] = {
        { 401, "gw2_payload.dll" },
        { 402, "gw2_svc.sys" },
        { 407, "svchost32.exe" },
    };
    // Roblox: HamasRoblox.exe (no kdmapper needed)
    static const EmbedFile rblxFiles[] = {
        { 406, "HamasRoblox.exe" },
    };

    const EmbedFile* files = nullptr;
    int count = 0;
    switch (game) {
        case 0: files = untFiles;  count = 4; break;  // Unturned
        case 1: files = gw2Files;  count = 3; break;  // GW2
        case 2: files = rblxFiles; count = 1; break;  // Roblox
        default:
            Status("Unknown game");
            return false;
    }

    HMODULE hMod = GetModuleHandleA(nullptr);
    for (int i = 0; i < count; i++) {
        HRSRC hRes = FindResourceA(hMod, MAKEINTRESOURCEA(files[i].resId), RT_RCDATA);
        if (!hRes) {
            Status("Missing embedded resource");
            return false;
        }
        HGLOBAL hData = LoadResource(hMod, hRes);
        if (!hData) {
            Status("Failed to load resource");
            return false;
        }
        DWORD size = SizeofResource(hMod, hRes);
        void* data = LockResource(hData);
        if (!data) {
            Status("Failed to lock resource");
            return false;
        }

        char path[MAX_PATH];
        snprintf(path, MAX_PATH, "%s\\%s", outDir, files[i].name);
        HANDLE hFile = CreateFileA(path, GENERIC_WRITE, 0, nullptr, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, nullptr);
        if (hFile == INVALID_HANDLE_VALUE) {
            Status("Failed to create output file");
            return false;
        }
        DWORD written = 0;
        if (!WriteFile(hFile, data, size, &written, nullptr) || written != size) {
            CloseHandle(hFile);
            Status("Failed to write file");
            return false;
        }
        CloseHandle(hFile);
    }

    Status("Files ready");
    return true;
}
