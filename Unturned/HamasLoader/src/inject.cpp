#include "inject.h"
#include "download.h"
#include "discord_log.h"
#include "gui.h"
#include "xorstr.hpp"


#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <winreg.h>
#include <tlhelp32.h>
#include <shellapi.h>
#include <cstdio>
#include <cstring>
#include <cstdarg>
#include <string>
#include <thread>
#include <atomic>

// ── Game definitions ────────────────────────────────────────────────────────
// Central game table — add new games here without touching the inject flow.
// 0 = Unturned, 1 = PvZ GW2, 2 = Roblox (external cheat, no driver).
enum GameId { GAME_UNTURNED = 0, GAME_GW2 = 1, GAME_ROBLOX = 2, GAME_COUNT = 3 };

struct GameConfig {
    const char* name;         // display name
    const char* process;      // primary process image name
    const char* processAlt;   // alt process ("" if none)
    const char* payloadRes;   // temp filename of payload (extracted resource)
    const char* payloadDst;   // full deploy path for payload
    const char* managedRes;   // temp filename of managed DLL ("" if none)
    const char* managedDst;   // full deploy path for managed DLL ("" if none)
    const char* driverRes;    // temp filename of driver
    const char* steamUrl;     // steam:// URL ("" if none)
    const char* externalExe;  // external cheat exe launched directly ("" if none)
    const char* statusLog;    // driver's live status log (Step 5 watcher)
    const char* hitFile;      // driver's one-shot success marker
};

static const GameConfig kGames[GAME_COUNT] = {
    { // Unturned
        "Unturned",
        "Unturned.exe",
        "Unturned_BE.exe",
        "msec_data.bin",
        "C:\\Users\\Public\\msec_data.bin",
        "HAMASCLIENT.dll",
        "C:\\ProgramData\\Microsoft\\DeviceSync\\HAMASCLIENT.dll",
        "runtime.sys",
        "steam://rungameid/304930",
        "",
        "C:\\Users\\Public\\msec_trace.log",
        "C:\\Users\\Public\\msec_done.log",
    },
    { // PvZ GW2
        "PvZ GW2",
        "GW2.Main_Win64_Retail.exe",
        "",
        "gw2_payload.dll",
        "C:\\Users\\Public\\gw2_payload.dll",
        "",
        "",
        "gw2_svc.sys",
        "",
        "",
        "C:\\Users\\Public\\kmap_status.txt",
        "C:\\Users\\Public\\kmap_hit.txt",
    },
    { // Roblox — external cheat, no driver/injection
        "Roblox",
        "RobloxPlayerBeta.exe",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "HamasRoblox.exe",
        "",
        "",
    },
};

// ── State ──
static std::atomic<bool> s_Done{false};
static std::atomic<bool> s_Success{false};
static char s_Status[512] = {};
static bool s_AutoStart = false;
static int  s_Game = GAME_UNTURNED;

// ── Helpers ──

static void SetStatus(const char* msg) {
    strncpy(s_Status, msg, sizeof(s_Status) - 1);
    s_Status[sizeof(s_Status) - 1] = '\0';
}

static bool FileExists(const char* path) {
    DWORD attr = GetFileAttributesA(path);
    return (attr != INVALID_FILE_ATTRIBUTES && !(attr & FILE_ATTRIBUTE_DIRECTORY));
}

static bool IsProcessRunning(const char* name) {
    if (!name || !name[0]) return false;
    HANDLE snap = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
    if (snap == INVALID_HANDLE_VALUE) return false;
    PROCESSENTRY32 pe;
    pe.dwSize = sizeof(pe);
    bool found = false;
    if (Process32First(snap, &pe)) {
        do {
            if (_stricmp(pe.szExeFile, name) == 0) { found = true; break; }
        } while (Process32Next(snap, &pe));
    }
    CloseHandle(snap);
    return found;
}

static void CleanupTempDir(const char* dir) {
    WIN32_FIND_DATAA fd;
    char search[MAX_PATH];
    snprintf(search, MAX_PATH, "%s\\*", dir);
    HANDLE h = FindFirstFileA(search, &fd);
    if (h != INVALID_HANDLE_VALUE) {
        do {
            if (fd.cFileName[0] == '.') continue;
            char fp[MAX_PATH];
            snprintf(fp, MAX_PATH, "%s\\%s", dir, fd.cFileName);
            SetFileAttributesA(fp, FILE_ATTRIBUTE_NORMAL);
            DeleteFileA(fp);
        } while (FindNextFileA(h, &fd));
        FindClose(h);
    }
    RemoveDirectoryA(dir);
}

static void DeleteQuiet(const char* path) {
    if (!path || !path[0]) return;
    SetFileAttributesA(path, FILE_ATTRIBUTE_NORMAL);
    DeleteFileA(path);
}

static void CleanupDeployedFiles(const GameConfig& g) {
    DeleteQuiet(g.payloadDst);
    if (g.managedDst[0]) DeleteQuiet(g.managedDst);
    // NOTE: kmap_status.txt / kmap_hit.txt are NOT deleted here anymore —
    // they're the only diagnostic evidence when injection fails. The next
    // run's Step 3 wipes them for a clean slate.
    DeleteQuiet("C:\\Users\\Public\\hamas_debug.txt");
}

static void InjectLog(const char* msg) {
    FILE* f = fopen("C:\\Users\\Public\\hamas_debug.txt", "a");
    if (f) { fprintf(f, "%s\n", msg); fclose(f); }
}

static void InjectLogFmt(const char* fmt, ...) {
    char buf[512];
    va_list args;
    va_start(args, fmt);
    vsnprintf(buf, sizeof(buf), fmt, args);
    va_end(args);
    InjectLog(buf);
}

// ── Windows 10 compatibility helpers ────────────────────────────────────────

// Windows 10 (and updated Win11) ship the Microsoft Vulnerable Driver Blocklist
// ENABLED by default. kdmapper's vulnerable driver (Intel NAL) is on that list,
// so NtLoadDriver is refused with STATUS_DRIVER_BLOCKED (0xC0000365) before the
// driver is ever mapped — this is the "works on my machine, fails on users'
// machines" split, because it is a per-machine policy flag, not an OS capability.
// The loader runs elevated, so we flip the policy off for the load; CI re-reads
// the value per driver load and it does not need a reboot.
static void DisableVulnerableDriverBlocklist() {
    HKEY hKey = nullptr;
    if (RegOpenKeyExA(HKEY_LOCAL_MACHINE,
            "SYSTEM\\CurrentControlSet\\Control\\CI\\Config",
            0, KEY_SET_VALUE | KEY_QUERY_VALUE, &hKey) != ERROR_SUCCESS) {
        InjectLog("WARN: CI\\Config key not writable - blocklist policy untouched");
        return;
    }

    DWORD prev = 0, prevSz = sizeof(prev);
    RegQueryValueExA(hKey, "VulnerableDriverBlocklistEnable", nullptr, nullptr,
        (LPBYTE)&prev, &prevSz);

    if (prev == 0) {
        InjectLog("VulnerableDriverBlocklistEnable already 0");
    } else {
        DWORD zero = 0;
        if (RegSetValueExA(hKey, "VulnerableDriverBlocklistEnable", 0, REG_DWORD,
                (const BYTE*)&zero, sizeof(zero)) == ERROR_SUCCESS) {
            InjectLog("VulnerableDriverBlocklistEnable set 1 -> 0");
        } else {
            InjectLog("WARN: could not write VulnerableDriverBlocklistEnable");
        }
    }
    RegCloseKey(hKey);
}

// Tail of kdmapper's own console output, so a failure message names the real
// reason ("vulnerable driver list enabled", "failed to resolve import", ...)
// instead of a generic antivirus hint.
static std::string ReadKdmapperOutput() {
    std::string out;
    FILE* f = nullptr;
    fopen_s(&f, "C:\\Users\\Public\\kdmapper_output.txt", "rb");
    if (!f) return out;
    fseek(f, 0, SEEK_END);
    long sz = ftell(f);
    long start = (sz > 400) ? (sz - 400) : 0;
    fseek(f, start, SEEK_SET);
    char buf[512] = {};
    size_t n = fread(buf, 1, sizeof(buf) - 1, f);
    fclose(f);
    buf[n] = '\0';
    out = buf;
    if (start > 0) {
        size_t nl = out.find('\n');
        if (nl != std::string::npos) out = out.substr(nl + 1);
    }
    // Collapse whitespace so it fits a single status line / log entry.
    while (!out.empty() && (out.back() == '\n' || out.back() == '\r')) out.pop_back();
    return out;
}

// ── Main injection thread ──

static void InjectThread() {
    s_Success = false;
    char tempDir[MAX_PATH] = {};
    InjectLog("==== InjectThread START ====");
    // Mirror live download progress (X%, n/7 files) into our status
    Download::SetStatusHook([](const char* s) { SetStatus(s); });
    DiscordLog::PostEvent(DiscordLog::EVENT_INJECT_START,
        (s_Game >= 0 && s_Game < GAME_COUNT) ? kGames[s_Game].name : "Unknown");

    if (s_Game < 0 || s_Game >= GAME_COUNT) {
        SetStatus("ERROR: Invalid game selection.");
        DiscordLog::PostEvent(DiscordLog::EVENT_INJECT_FAIL, "Invalid game selection");
        s_Done = true;
        return;
    }
    const GameConfig& g = kGames[s_Game];
    InjectLogFmt("game: %s", g.name);

    // ── Step 0: Download files from VPS ──
    SetStatus("Downloading files...");
    bool fetchOK = Download::FetchFiles(s_Game, tempDir, MAX_PATH);
    if (!fetchOK) {
        InjectLog("File load FAILED");
        SetStatus(Download::GetStatus());
        DiscordLog::PostEvent(DiscordLog::EVENT_INJECT_FAIL, "Failed to load files");
        if (tempDir[0]) CleanupTempDir(tempDir);
        s_Done = true;
        return;
    }
    InjectLogFmt("Files OK, tempDir: %s", tempDir);
    Sleep(200);

    // ── External cheat path (Roblox): game must be running, just launch the exe ──
    if (g.externalExe[0]) {
        SetStatus("Checking Roblox...");
        if (!IsProcessRunning(g.process) && !IsProcessRunning(g.processAlt)) {
            SetStatus("Open a Roblox game first, then press Launch.");
            CleanupTempDir(tempDir);
            s_Done = true;
            return;
        }

        char exePath[MAX_PATH];
        snprintf(exePath, MAX_PATH, "%s\\%s", tempDir, g.externalExe);
        if (!FileExists(exePath)) {
            SetStatus("ERROR: Missing cheat file.");
            CleanupTempDir(tempDir);
            s_Done = true;
            return;
        }

        SetStatus("Launching Hamas Client...");
        STARTUPINFOA esi = {};
        esi.cb = sizeof(esi);
        PROCESS_INFORMATION epi = {};
        if (!CreateProcessA(exePath, nullptr, nullptr, nullptr, FALSE,
                CREATE_NO_WINDOW, nullptr, tempDir, &esi, &epi)) {
            InjectLogFmt("launch %s FAILED err=%lu", g.externalExe, GetLastError());
            SetStatus("ERROR: Failed to launch cheat.");
            CleanupTempDir(tempDir);
            s_Done = true;
            return;
        }
        CloseHandle(epi.hProcess);
        CloseHandle(epi.hThread);
        InjectLogFmt("Launched %s", g.externalExe);
        Beep(1000, 150); Sleep(100); Beep(1000, 150);
        SetStatus("Hamas Client launched!");
        s_Success = true;
        DiscordLog::PostEvent(DiscordLog::EVENT_INJECT_SUCCESS, g.name);
        DiscordLog::StartGameMonitor(g.process);
        s_Done = true;
        return;
    }

    // ── Step 1: Verify game not running ──
    SetStatus("Checking environment...");
    if (IsProcessRunning(g.process) || IsProcessRunning(g.processAlt)) {
        char msg[256];
        snprintf(msg, sizeof(msg), "ERROR: %s is already running. Close it first.", g.name);
        SetStatus(msg);
        CleanupTempDir(tempDir);
        s_Done = true;
        return;
    }
    Sleep(300);

    // ── Step 2: Deploy payload + (optional) managed DLL ──
    SetStatus("Deploying payload...");
    {
        char src[MAX_PATH], dst[MAX_PATH];
        snprintf(src, MAX_PATH, "%s\\%s", tempDir, g.payloadRes);
        snprintf(dst, MAX_PATH, "%s", g.payloadDst);
        SetFileAttributesA(dst, FILE_ATTRIBUTE_NORMAL);
        DeleteFileA(dst);
        if (!CopyFileA(src, dst, FALSE)) {
            InjectLogFmt("FATAL: deploy %s failed err=%lu", g.payloadRes, GetLastError());
            SetStatus("ERROR: Failed to deploy payload.");
            CleanupTempDir(tempDir);
            s_Done = true;
            return;
        }
        InjectLogFmt("Deployed %s -> %s", g.payloadRes, g.payloadDst);
    }

    if (g.managedRes[0]) {
        SetStatus("Deploying files...");
        char src[MAX_PATH], dst[MAX_PATH];
        snprintf(src, MAX_PATH, "%s\\%s", tempDir, g.managedRes);
        snprintf(dst, MAX_PATH, "%s", g.managedDst);
        CreateDirectoryA("C:\\ProgramData\\Microsoft\\DeviceSync", nullptr);
        // Write ghost mode flag so DLL knows to skip network features
        const char* ghostPath = "C:\\ProgramData\\Microsoft\\DeviceSync\\ghost.txt";
        if (GUI::GhostMode) {
            FILE* gf = fopen(ghostPath, "w");
            if (gf) { fputc('1', gf); fclose(gf); }
        } else {
            DeleteFileA(ghostPath);
        }
        // Write session token so DLL can fetch online Hamas users
        const char* tokenPath = "C:\\ProgramData\\Microsoft\\DeviceSync\\session.txt";
        const char* token = DiscordLog::GetSessionToken();
        if (token && token[0]) {
            FILE* tf = fopen(tokenPath, "w");
            if (tf) { fputs(token, tf); fclose(tf); }
        }
        SetFileAttributesA(dst, FILE_ATTRIBUTE_NORMAL);
        DeleteFileA(dst);
        if (!CopyFileA(src, dst, FALSE)) {
            InjectLogFmt("FATAL: deploy %s failed err=%lu", g.managedRes, GetLastError());
            SetStatus("ERROR: Failed to deploy managed DLL.");
            CleanupTempDir(tempDir);
            s_Done = true;
            return;
        }
        InjectLogFmt("Deployed %s -> %s", g.managedRes, g.managedDst);
    }

    // ── Step 3: Clean old trace files ──
    DeleteQuiet("C:\\Users\\Public\\wer_status.log");
    DeleteQuiet("C:\\Users\\Public\\wer_done.log");
    DeleteQuiet("C:\\ProgramData\\Microsoft\\Windows\\WER\\ReportArchive\\status.log");
    DeleteQuiet("C:\\ProgramData\\Microsoft\\Windows\\WER\\ReportArchive\\done.log");
    DeleteQuiet("C:\\ProgramData\\Microsoft\\Diagnosis\\ETLLogs\\svc_trace.log");
    DeleteQuiet("C:\\ProgramData\\Microsoft\\Diagnosis\\ETLLogs\\svc_done.log");
    DeleteQuiet("C:\\Users\\Public\\msec_trace.log");
    DeleteQuiet("C:\\Users\\Public\\msec_done.log");
    // kmap driver trace files MUST be wiped per run: they are append/stale-across
    // runs otherwise — a stale kmap_hit.txt fakes success, an old kmap_status.txt
    // "ERROR" line fakes failure. (On failure they are deliberately preserved
    // as this run's diagnostic evidence.)
    DeleteQuiet("C:\\Users\\Public\\kmap_status.txt");
    DeleteQuiet("C:\\Users\\Public\\kmap_hit.txt");
    InjectLog("Traces cleaned");

    // ── Step 4: Load kernel driver via kdmapper ──
    SetStatus("Loading driver...");
    char loaderPath[MAX_PATH], drvPath[MAX_PATH];
    snprintf(loaderPath, MAX_PATH, "%s\\svchost32.exe", tempDir);
    snprintf(drvPath,    MAX_PATH, "%s\\%s", tempDir, g.driverRes);

    if (!FileExists(loaderPath) || !FileExists(drvPath)) {
        SetStatus("ERROR: Missing required files.");
        CleanupDeployedFiles(g);
        CleanupTempDir(tempDir);
        s_Done = true;
        return;
    }

    // Windows 10: NtLoadDriver is refused while the Microsoft Vulnerable Driver
    // Blocklist is on (default). Disable the policy for this load session.
    DisableVulnerableDriverBlocklist();

    char cmdLine[MAX_PATH * 3];
    snprintf(cmdLine, sizeof(cmdLine),
        "cmd /c \"\"%s\" \"%s\" > C:\\Users\\Public\\kdmapper_output.txt 2>&1\"",
        loaderPath, drvPath);
    InjectLogFmt("cmdLine: %s", cmdLine);

    STARTUPINFOA si = {};
    si.cb = sizeof(si);
    si.dwFlags = STARTF_USESHOWWINDOW;
    si.wShowWindow = SW_HIDE;
    CreateDirectoryA("C:\\ProgramData\\Microsoft\\DeviceSync", nullptr);

    DWORD exitCode = 1;
    bool loaded = false;

    // Two attempts: the first failure is frequently transient (AV holding the
    // freshly extracted files, first-touch policy). The blocklist fix above is
    // already in effect for both.
    for (int attempt = 1; attempt <= 2 && !loaded; attempt++) {
        if (attempt > 1) {
            InjectLog("kdmapper attempt 2 after failure");
            SetStatus("Retrying driver load...");
            Sleep(1500);
        }

        PROCESS_INFORMATION pi = {};
        if (!CreateProcessA(nullptr, cmdLine, nullptr, nullptr, FALSE,
                CREATE_NO_WINDOW, nullptr, tempDir, &si, &pi)) {
            InjectLogFmt("CreateProcess FAILED err=%lu", GetLastError());
            continue;
        }

        DWORD waitResult = WaitForSingleObject(pi.hProcess, 30000);
        GetExitCodeProcess(pi.hProcess, &exitCode);
        InjectLogFmt("kdmapper wait=%lu exit=0x%lX", waitResult, exitCode);

        if (waitResult == WAIT_TIMEOUT) {
            // kdmapper is stuck; kill it so the retry starts clean.
            TerminateProcess(pi.hProcess, 1);
            exitCode = 0xFFFFFFFF;
        }
        CloseHandle(pi.hProcess);
        CloseHandle(pi.hThread);

        loaded = (waitResult != WAIT_TIMEOUT && exitCode == 0);
    }

    if (!loaded) {
        std::string kmOut = ReadKdmapperOutput();
        for (auto& c : kmOut) { if (c == '\r') c = ' '; }
        InjectLogFmt("kdmapper output: %s", kmOut.c_str());

        char msg[512];
        if (exitCode == 0xC0000365
            || kmOut.find("vulnerable driver") != std::string::npos) {
            snprintf(msg, sizeof(msg),
                "ERROR: Driver blocked by Windows. Open Windows Security > Device "
                "security > Core isolation and turn OFF 'Microsoft Vulnerable Driver "
                "Blocklist', then retry.");
        } else {
            snprintf(msg, sizeof(msg), "ERROR: Driver load failed (0x%lX). %s",
                exitCode, kmOut.empty() ? "Disable anti-virus and retry." : kmOut.c_str());
        }
        SetStatus(msg);
        DiscordLog::PostEvent(DiscordLog::EVENT_INJECT_FAIL, msg);
        CleanupDeployedFiles(g);
        CleanupTempDir(tempDir);
        s_Done = true;
        return;
    }

    // ── Driver loaded! Single beep ──
    Beep(800, 200);
    Sleep(300);
    DeleteFileA(loaderPath);
    DeleteFileA(drvPath);

    // ── Step 5: Wait for game to start ──
    if (s_AutoStart && g.steamUrl[0]) {
        SetStatus("Starting game...");
        ShellExecuteA(nullptr, "open", g.steamUrl, nullptr, nullptr, SW_SHOWNORMAL);
    } else {
        char msg[128];
        snprintf(msg, sizeof(msg), "Driver loaded! Start %s now...", g.name);
        SetStatus(msg);
    }

    bool gameDetected = false;
    // Per-game paths from the game table: the GW2 driver writes to
    // Users\Public (klog.hpp), the Unturned driver to the WER ReportArchive.
    const char* statusLog = g.statusLog;
    const char* hitFile   = g.hitFile;

    for (int i = 0; i < 600; i++) {
        Sleep(1000);

        if (!gameDetected) {
            if (IsProcessRunning(g.process) || IsProcessRunning(g.processAlt)) {
                gameDetected = true;
                char msg[128];
                snprintf(msg, sizeof(msg), "%s detected! Waiting for injection...", g.name);
                SetStatus(msg);
            } else if (s_AutoStart && g.steamUrl[0]) {
                SetStatus(i < 15 ? "Starting game..." : "Waiting for game to launch...");
            } else {
                char wMsg[128];
                snprintf(wMsg, sizeof(wMsg), "Waiting for %s... (%d min remaining)",
                    g.name, (600 - i) / 60);
                SetStatus(wMsg);
            }
        }

        if (gameDetected && FileExists(hitFile)) {
            Beep(1000, 150); Sleep(100); Beep(1000, 150);
            SetStatus("Injected successfully!");
            s_Success = true;
            DiscordLog::PostEvent(DiscordLog::EVENT_INJECT_SUCCESS, g.name);
            DiscordLog::StartGameMonitor(g.process);
            CleanupDeployedFiles(g);
            CleanupTempDir(tempDir);
            s_Done = true;
            return;
        }

        if (gameDetected && FileExists(statusLog)) {
            HANDLE hFile = CreateFileA(statusLog, GENERIC_READ,
                FILE_SHARE_READ | FILE_SHARE_WRITE, nullptr,
                OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, nullptr);
            if (hFile != INVALID_HANDLE_VALUE) {
                // Read the TAIL of the log (the kmap driver's log grows during
                // the module-wait loop; the success/error markers are at the end).
                char logBuf[4096] = {};
                DWORD bytesRead = 0;
                LARGE_INTEGER fsize = {};
                if (GetFileSizeEx(hFile, &fsize) && fsize.QuadPart > (LONGLONG)sizeof(logBuf) - 1)
                    SetFilePointer(hFile, (LONG)(fsize.QuadPart - (sizeof(logBuf) - 1)), nullptr, FILE_END);
                ReadFile(hFile, logBuf, sizeof(logBuf) - 1, &bytesRead, nullptr);
                CloseHandle(hFile);
                logBuf[bytesRead] = '\0';

                if (strstr(logBuf, "Injecting DLL"))
                    SetStatus("Injecting...");

                if (strstr(logBuf, "done") ||
                    strstr(logBuf, "DllMain executed") ||
                    strstr(logBuf, "SUCCESS: DLL injected") ||
                    strstr(logBuf, "DllMain CONFIRMED")) {
                    Beep(1000, 150); Sleep(100); Beep(1000, 150);
                    SetStatus("Injected successfully!");
                    s_Success = true;
                    DiscordLog::PostEvent(DiscordLog::EVENT_INJECT_SUCCESS, g.name);
                    DiscordLog::StartGameMonitor(g.process);
                    CleanupDeployedFiles(g);
                    CleanupTempDir(tempDir);
                    s_Done = true;
                    return;
                }

                if (strstr(logBuf, "ERROR") || strstr(logBuf, "FAIL")) {
                    SetStatus("ERROR: Injection failed. Restart and try again.");
                    DiscordLog::PostEvent(DiscordLog::EVENT_INJECT_FAIL, "Injection error detected in status log");
                    CleanupDeployedFiles(g);
                    CleanupTempDir(tempDir);
                    s_Done = true;
                    return;
                }
            }
        }
    }

    // Timeout after 10 minutes
    CleanupDeployedFiles(g);
    CleanupTempDir(tempDir);
    char tMsg[128];
    snprintf(tMsg, sizeof(tMsg), "ERROR: Timed out waiting for %s.", g.name);
    SetStatus(tMsg);
    DiscordLog::PostEvent(DiscordLog::EVENT_INJECT_FAIL, tMsg);
    s_Done = true;
}

// ── Public API ──

void Inject::StartAsync(int game, bool autoStartGame) {
    s_Done = false;
    s_Success = false;
    s_Game = game;
    s_AutoStart = autoStartGame;
    SetStatus("Starting...");
    std::thread(InjectThread).detach();
}

bool Inject::IsDone() {
    return s_Done.load();
}

bool Inject::Succeeded() {
    return s_Success.load();
}

const char* Inject::GetStatus() {
    return s_Status;
}
