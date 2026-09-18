#include "game.h"

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <tlhelp32.h>
#include <shellapi.h>
#include <cstdio>
#include <cstring>
#include <vector>
#include <fstream>
#include <sstream>

static std::string s_Error;
static std::string s_CachedPath;

// ── Check if a file exists ──
static bool FileExists(const std::string& path) {
    DWORD attr = GetFileAttributesA(path.c_str());
    return (attr != INVALID_FILE_ATTRIBUTES && !(attr & FILE_ATTRIBUTE_DIRECTORY));
}

// ── Parse Steam libraryfolders.vdf to find all library paths ──
static std::vector<std::string> GetSteamLibraryPaths() {
    std::vector<std::string> libs;

    // Find Steam install from registry
    HKEY hKey;
    char steamPath[MAX_PATH] = {};
    DWORD size = sizeof(steamPath);

    // Try 64-bit registry first
    if (RegOpenKeyExA(HKEY_LOCAL_MACHINE,
            "SOFTWARE\\Wow6432Node\\Valve\\Steam", 0, KEY_READ, &hKey) == ERROR_SUCCESS) {
        RegQueryValueExA(hKey, "InstallPath", nullptr, nullptr, (LPBYTE)steamPath, &size);
        RegCloseKey(hKey);
    }
    // Try 32-bit
    if (!steamPath[0]) {
        size = sizeof(steamPath);
        if (RegOpenKeyExA(HKEY_LOCAL_MACHINE,
                "SOFTWARE\\Valve\\Steam", 0, KEY_READ, &hKey) == ERROR_SUCCESS) {
            RegQueryValueExA(hKey, "InstallPath", nullptr, nullptr, (LPBYTE)steamPath, &size);
            RegCloseKey(hKey);
        }
    }
    // Try current user
    if (!steamPath[0]) {
        size = sizeof(steamPath);
        if (RegOpenKeyExA(HKEY_CURRENT_USER,
                "SOFTWARE\\Valve\\Steam", 0, KEY_READ, &hKey) == ERROR_SUCCESS) {
            RegQueryValueExA(hKey, "SteamPath", nullptr, nullptr, (LPBYTE)steamPath, &size);
            RegCloseKey(hKey);
        }
    }

    if (!steamPath[0]) return libs;

    // Default library is the Steam install folder itself
    libs.push_back(std::string(steamPath));

    // Parse libraryfolders.vdf for additional libraries
    std::string vdfPath = std::string(steamPath) + "\\steamapps\\libraryfolders.vdf";
    std::ifstream vdf(vdfPath);
    if (vdf.is_open()) {
        std::string line;
        while (std::getline(vdf, line)) {
            // Look for "path" entries: "path"		"D:\\SteamLibrary"
            size_t pathPos = line.find("\"path\"");
            if (pathPos == std::string::npos) continue;

            // Find the value after "path"
            size_t firstQuote = line.find('"', pathPos + 6);
            if (firstQuote == std::string::npos) continue;
            size_t secondQuote = line.find('"', firstQuote + 1);
            if (secondQuote == std::string::npos) continue;

            std::string libPath = line.substr(firstQuote + 1, secondQuote - firstQuote - 1);

            // Unescape double backslashes
            std::string clean;
            for (size_t i = 0; i < libPath.size(); i++) {
                if (libPath[i] == '\\' && i + 1 < libPath.size() && libPath[i + 1] == '\\')
                    i++; // skip one
                clean += libPath[i];
            }

            // Don't add duplicates
            bool dup = false;
            for (auto& l : libs) {
                if (_stricmp(l.c_str(), clean.c_str()) == 0) { dup = true; break; }
            }
            if (!dup) libs.push_back(clean);
        }
    }

    // Also brute-force check common paths on all drives
    char drives[128];
    DWORD len = GetLogicalDriveStringsA(sizeof(drives), drives);
    for (DWORD i = 0; i < len;) {
        std::string drive(drives + i);
        i += (DWORD)drive.size() + 1;

        // Common Steam library locations
        std::string paths[] = {
            drive + "SteamLibrary",
            drive + "Steam",
            drive + "Program Files (x86)\\Steam",
            drive + "Program Files\\Steam",
            drive + "Games\\Steam",
            drive + "Games\\SteamLibrary",
        };
        for (auto& p : paths) {
            bool dup = false;
            for (auto& l : libs) {
                if (_stricmp(l.c_str(), p.c_str()) == 0) { dup = true; break; }
            }
            if (!dup && GetFileAttributesA(p.c_str()) != INVALID_FILE_ATTRIBUTES)
                libs.push_back(p);
        }
    }

    return libs;
}

std::string Game::FindUnturnedPath() {
    if (!s_CachedPath.empty() && FileExists(s_CachedPath + "\\Unturned_BE.exe"))
        return s_CachedPath;

    auto libs = GetSteamLibraryPaths();

    for (auto& lib : libs) {
        // Standard Steam app location
        std::string gamePath = lib + "\\steamapps\\common\\Unturned";
        std::string beExe = gamePath + "\\Unturned_BE.exe";

        if (FileExists(beExe)) {
            s_CachedPath = gamePath;
            return gamePath;
        }

        // Also check without steamapps/common (if lib IS the game folder)
        std::string direct = lib + "\\Unturned\\Unturned_BE.exe";
        if (FileExists(direct)) {
            s_CachedPath = lib + "\\Unturned";
            return s_CachedPath;
        }
    }

    s_Error = "Unturned not found. Make sure it's installed via Steam.";
    return "";
}

bool Game::IsUnturnedRunning() {
    HANDLE snap = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
    if (snap == INVALID_HANDLE_VALUE) return false;

    PROCESSENTRY32 pe;
    pe.dwSize = sizeof(pe);

    bool found = false;
    if (Process32First(snap, &pe)) {
        do {
            if (_stricmp(pe.szExeFile, "Unturned.exe") == 0 ||
                _stricmp(pe.szExeFile, "Unturned_BE.exe") == 0) {
                found = true;
                break;
            }
        } while (Process32Next(snap, &pe));
    }

    CloseHandle(snap);
    return found;
}

bool Game::LaunchUnturned() {
    // Check if already running
    if (IsUnturnedRunning()) {
        s_Error = "Unturned is already running. Close it first, then inject.";
        return false;
    }

    // Find the game
    std::string gamePath = FindUnturnedPath();
    if (gamePath.empty()) return false;

    // Launch via Steam protocol (App ID 304930) — this starts with BattlEye
    HINSTANCE result = ShellExecuteA(nullptr, "open",
        "steam://rungameid/304930", nullptr, nullptr, SW_SHOWNORMAL);

    if ((intptr_t)result <= 32) {
        // Fallback: try direct exe launch
        std::string beExe = gamePath + "\\Unturned_BE.exe";
        STARTUPINFOA si = {};
        si.cb = sizeof(si);
        PROCESS_INFORMATION pi = {};

        if (!CreateProcessA(beExe.c_str(), nullptr, nullptr, nullptr, FALSE,
                0, nullptr, gamePath.c_str(), &si, &pi)) {
            s_Error = "Failed to launch Unturned.";
            return false;
        }
        CloseHandle(pi.hProcess);
        CloseHandle(pi.hThread);
    }

    return true;
}

const char* Game::GetError() {
    return s_Error.c_str();
}
