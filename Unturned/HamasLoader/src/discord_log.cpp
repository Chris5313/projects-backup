#include "discord_log.h"
#include "gui.h"
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <winhttp.h>
#include <bcrypt.h>
#include "VMProtectSDK.h"
#include <tlhelp32.h>
#include <wincrypt.h>
#include <iphlpapi.h>

#include <cstdio>
#include <cstring>
#include <cstdarg>
#include <ctime>
#include <string>
#include <thread>
#include <atomic>
#include <mutex>
#include <queue>
#include <fstream>
#include <sstream>

#pragma comment(lib, "winhttp.lib")
#pragma comment(lib, "bcrypt.lib")
#pragma comment(lib, "iphlpapi.lib")

// Debug trace — remove once stable
static void DbgLog(const char* fmt, ...) {
    FILE* f = fopen("C:\\Users\\Public\\discord_debug.txt", "a");
    if (!f) return;
    SYSTEMTIME st; GetLocalTime(&st);
    fprintf(f, "[%02d:%02d:%02d.%03d] ", st.wHour, st.wMinute, st.wSecond, st.wMilliseconds);
    va_list args; va_start(args, fmt);
    vfprintf(f, fmt, args);
    va_end(args);
    fprintf(f, "\n");
    fclose(f);
}

namespace DiscordLog {

// ── Identity ──
static char s_Username[128]   = {};
static char s_Hostname[128]   = {};
static char s_HWID[128]       = {};
static char s_SteamID64[32]   = {};
static char s_SteamName[128]  = {};
static char s_PublicIP[64]    = {};
static bool s_Initialized     = false;
static char s_SessionToken[128] = {};

// ── System fingerprint ──
static char s_OSVersion[64]   = {};
static char s_CPU[128]        = {};
static char s_RAM[32]         = {};
static char s_GPU[128]        = {};
static char s_Resolution[32]  = {};
static char s_Timezone[64]    = {};
static char s_Locale[32]      = {};
static char s_DiskSerial[64]  = {};
static char s_AllMACs[512]    = {};

// ── Game monitor ──
static std::atomic<bool> s_MonitorRun{false};
static std::thread       s_MonitorThread;
static char              s_MonitorProcess[128] = {};
static std::atomic<bool> s_GameWasRunning{false};

// ── Async send queue ──
struct WebhookMsg {
    std::string url;
    std::string json;
};
static std::queue<WebhookMsg> s_Queue;
static std::mutex             s_QueueMutex;
static std::atomic<bool>      s_SenderRun{false};
static std::thread            s_SenderThread;

// ══════════════════════════════════════════════════════════════
static void StartSender();  // forward — defined near Public API
static bool s_AuthRejected = false;  // set on HTTP 403 — definitive, don't retry
static char  s_AuthError[160] = {};  // human-readable reason for the GUI



// ══════════════════════════════════════════════════════════════
//  Identity collection
// ══════════════════════════════════════════════════════════════

static void CollectIdentity() {
    // Windows username
    DWORD sz = sizeof(s_Username);
    if (!GetUserNameA(s_Username, &sz))
        strncpy(s_Username, "Unknown", sizeof(s_Username));

    // Machine hostname
    sz = sizeof(s_Hostname);
    if (!GetComputerNameA(s_Hostname, &sz))
        strncpy(s_Hostname, "Unknown", sizeof(s_Hostname));

    // HWID from first MAC address
    s_HWID[0] = '\0';
    ULONG bufLen = 0;
    GetAdaptersInfo(nullptr, &bufLen);
    if (bufLen > 0) {
        IP_ADAPTER_INFO* adapters = (IP_ADAPTER_INFO*)malloc(bufLen);
        if (adapters && GetAdaptersInfo(adapters, &bufLen) == NO_ERROR) {
            for (IP_ADAPTER_INFO* a = adapters; a; a = a->Next) {
                if (a->AddressLength >= 6) {
                    snprintf(s_HWID, sizeof(s_HWID),
                        "%02X:%02X:%02X:%02X:%02X:%02X",
                        a->Address[0], a->Address[1], a->Address[2],
                        a->Address[3], a->Address[4], a->Address[5]);
                    break;
                }
            }
        }
        free(adapters);
    }
    if (!s_HWID[0]) strncpy(s_HWID, "Unknown", sizeof(s_HWID));
}

static void CollectSteamInfo() {
    s_SteamID64[0] = '\0';
    s_SteamName[0] = '\0';

    // 1. Read ActiveUser (Steam3 account ID) from registry
    HKEY hKey;
    DWORD accountId = 0;
    if (RegOpenKeyExA(HKEY_CURRENT_USER, "Software\\Valve\\Steam\\ActiveProcess",
            0, KEY_READ, &hKey) == ERROR_SUCCESS) {
        DWORD sz = sizeof(accountId);
        RegQueryValueExA(hKey, "ActiveUser", nullptr, nullptr, (LPBYTE)&accountId, &sz);
        RegCloseKey(hKey);
    }

    if (accountId == 0) {
        strncpy(s_SteamID64, "Unknown", sizeof(s_SteamID64));
        strncpy(s_SteamName, "Unknown", sizeof(s_SteamName));
        return;
    }

    // 2. Convert to SteamID64: accountId + 76561197960265728
    unsigned long long steam64 = (unsigned long long)accountId + 76561197960265728ULL;
    snprintf(s_SteamID64, sizeof(s_SteamID64), "%llu", steam64);
    DbgLog("Steam: accountId=%u steam64=%s", accountId, s_SteamID64);

    // 3. Get Steam install path from registry
    char steamPath[MAX_PATH] = {};
    if (RegOpenKeyExA(HKEY_CURRENT_USER, "Software\\Valve\\Steam",
            0, KEY_READ, &hKey) == ERROR_SUCCESS) {
        DWORD sz = sizeof(steamPath);
        RegQueryValueExA(hKey, "SteamPath", nullptr, nullptr, (LPBYTE)steamPath, &sz);
        RegCloseKey(hKey);
    }

    // 4. Parse loginusers.vdf to find PersonaName for this SteamID64
    if (steamPath[0]) {
        std::string vdfPath = std::string(steamPath) + "/config/loginusers.vdf";
        // Normalize slashes
        for (auto& c : vdfPath) if (c == '/') c = '\\';

        std::ifstream vdf(vdfPath);
        if (vdf.is_open()) {
            std::string line;
            bool inOurUser = false;
            while (std::getline(vdf, line)) {
                // Look for our SteamID64 as a key
                if (line.find(s_SteamID64) != std::string::npos) {
                    inOurUser = true;
                    continue;
                }
                if (inOurUser) {
                    // Look for PersonaName
                    size_t pn = line.find("\"PersonaName\"");
                    if (pn != std::string::npos) {
                        // Extract value: "PersonaName"		"VALUE"
                        size_t first = line.find('"', pn + 13);
                        if (first != std::string::npos) {
                            first++; // skip opening quote
                            size_t last = line.find('"', first);
                            if (last != std::string::npos) {
                                std::string name = line.substr(first, last - first);
                                strncpy(s_SteamName, name.c_str(), sizeof(s_SteamName) - 1);
                            }
                        }
                        break;
                    }
                    // Stop if we hit the closing brace of this user block
                    if (line.find('}') != std::string::npos)
                        break;
                }
            }
        }
    }

    if (!s_SteamName[0]) strncpy(s_SteamName, "Unknown", sizeof(s_SteamName));
    DbgLog("Steam: name=%s id=%s", s_SteamName, s_SteamID64);
}

static bool IsValidIPv4(const char* s) {
    if (!s || !*s) return false;
    int dots = 0, digits = 0; unsigned cur = 0;
    for (const char* p = s; *p; ++p) {
        if (*p == '.') {
            if (digits == 0 || cur > 255) return false;
            dots++; digits = 0; cur = 0;
        } else if (*p >= '0' && *p <= '9') {
            cur = cur * 10 + (unsigned)(*p - '0'); digits++;
            if (digits > 3) return false;
        } else return false;
    }
    return dots == 3 && digits > 0 && cur <= 255;
}

static void CollectPublicIP() {
    // Public IPv4 for server-side logging. api.ipify.org has NO AAAA
    // record — the fetch itself goes out over IPv4, so the response
    // body IS our public IPv4. The server only sees the Cloudflare
    // connection IP, which is IPv6 for most users and rotates every
    // session (Windows privacy extensions) — useless for logging.
    // Blocking ~3s worst case: only ever called from the auth worker
    // thread (Authenticate), never the UI thread.
    HINTERNET hSes = WinHttpOpen(L"HamasClient/1.0", WINHTTP_ACCESS_TYPE_DEFAULT_PROXY,
        WINHTTP_NO_PROXY_NAME, WINHTTP_NO_PROXY_BYPASS, 0);
    if (!hSes) return;
    WinHttpSetTimeouts(hSes, 0, 3000, 3000, 3000);
    HINTERNET hCon = WinHttpConnect(hSes, L"api.ipify.org", INTERNET_DEFAULT_HTTPS_PORT, 0);
    if (!hCon) { WinHttpCloseHandle(hSes); return; }
    HINTERNET hReq = WinHttpOpenRequest(hCon, L"GET", L"/", nullptr, WINHTTP_NO_REFERER,
        WINHTTP_DEFAULT_ACCEPT_TYPES, WINHTTP_FLAG_SECURE);
    if (!hReq) { WinHttpCloseHandle(hCon); WinHttpCloseHandle(hSes); return; }
    std::string body;
    if (WinHttpSendRequest(hReq, WINHTTP_NO_ADDITIONAL_HEADERS, 0,
            WINHTTP_NO_REQUEST_DATA, 0, 0, 0) &&
        WinHttpReceiveResponse(hReq, nullptr)) {
        char buf[32]; DWORD rd = 0;
        while (WinHttpQueryDataAvailable(hReq, &rd) && rd > 0 && body.size() < 31) {
            DWORD toRead = (rd < sizeof(buf) - 1) ? rd : (DWORD)sizeof(buf) - 1;
            if (!WinHttpReadData(hReq, buf, toRead, &rd)) break;
            body.append(buf, rd);
        }
    }
    WinHttpCloseHandle(hReq); WinHttpCloseHandle(hCon); WinHttpCloseHandle(hSes);

    std::string ip;
    for (char c : body) if (c == '.' || (c >= '0' && c <= '9')) ip += c;
    if (IsValidIPv4(ip.c_str())) {
        strncpy(s_PublicIP, ip.c_str(), sizeof(s_PublicIP) - 1);
        s_PublicIP[sizeof(s_PublicIP) - 1] = '\0';
        DbgLog("PublicIPv4=%s", s_PublicIP);
    } else {
        DbgLog("PublicIPv4 fetch failed (body='%s') — server logs connection IP only", body.c_str());
    }
}

static void CollectSystemInfo() {

    // OS version from registry (accurate on Win10+)
    HKEY hKey;
    if (RegOpenKeyExA(HKEY_LOCAL_MACHINE, "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion",
            0, KEY_READ, &hKey) == ERROR_SUCCESS) {
        char prodName[128] = {}, build[32] = {}, ubr[32] = {};
        DWORD sz = sizeof(prodName);
        RegQueryValueExA(hKey, "ProductName", nullptr, nullptr, (LPBYTE)prodName, &sz);
        sz = sizeof(build);
        RegQueryValueExA(hKey, "CurrentBuildNumber", nullptr, nullptr, (LPBYTE)build, &sz);
        DWORD ubrVal = 0; sz = sizeof(ubrVal);
        RegQueryValueExA(hKey, "UBR", nullptr, nullptr, (LPBYTE)&ubrVal, &sz);
        snprintf(s_OSVersion, sizeof(s_OSVersion), "%s Build %s.%lu", prodName, build, ubrVal);
        RegCloseKey(hKey);
    }
    if (!s_OSVersion[0]) strncpy(s_OSVersion, "Unknown", sizeof(s_OSVersion));

    // CPU name from registry
    if (RegOpenKeyExA(HKEY_LOCAL_MACHINE, "HARDWARE\\DESCRIPTION\\System\\CentralProcessor\\0",
            0, KEY_READ, &hKey) == ERROR_SUCCESS) {
        DWORD sz = sizeof(s_CPU);
        RegQueryValueExA(hKey, "ProcessorNameString", nullptr, nullptr, (LPBYTE)s_CPU, &sz);
        RegCloseKey(hKey);
    }
    if (!s_CPU[0]) strncpy(s_CPU, "Unknown", sizeof(s_CPU));
    // Trim leading spaces
    char* cpuTrim = s_CPU;
    while (*cpuTrim == ' ') cpuTrim++;
    if (cpuTrim != s_CPU) memmove(s_CPU, cpuTrim, strlen(cpuTrim) + 1);

    // RAM
    MEMORYSTATUSEX mem = {}; mem.dwLength = sizeof(mem);
    if (GlobalMemoryStatusEx(&mem))
        snprintf(s_RAM, sizeof(s_RAM), "%llu MB", mem.ullTotalPhys / (1024 * 1024));
    else
        strncpy(s_RAM, "Unknown", sizeof(s_RAM));

    // GPU from registry (DXGI adapter)
    if (RegOpenKeyExA(HKEY_LOCAL_MACHINE, "SYSTEM\\ControlSet001\\Control\\Class\\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000",
            0, KEY_READ, &hKey) == ERROR_SUCCESS) {
        DWORD sz = sizeof(s_GPU);
        RegQueryValueExA(hKey, "DriverDesc", nullptr, nullptr, (LPBYTE)s_GPU, &sz);
        RegCloseKey(hKey);
    }
    if (!s_GPU[0]) strncpy(s_GPU, "Unknown", sizeof(s_GPU));

    // Screen resolution
    int cx = GetSystemMetrics(SM_CXSCREEN), cy = GetSystemMetrics(SM_CYSCREEN);
    snprintf(s_Resolution, sizeof(s_Resolution), "%dx%d", cx, cy);

    // Timezone
    TIME_ZONE_INFORMATION tz;
    DWORD tzResult = GetTimeZoneInformation(&tz);
    int bias = -(int)tz.Bias;
    int tzH = bias / 60, tzM = abs(bias) % 60;
    char tzName[64] = {};
    WideCharToMultiByte(CP_UTF8, 0, (tzResult == TIME_ZONE_ID_DAYLIGHT) ? tz.DaylightName : tz.StandardName,
        -1, tzName, sizeof(tzName), nullptr, nullptr);
    snprintf(s_Timezone, sizeof(s_Timezone), "UTC%+d:%02d (%s)", tzH, tzM, tzName);

    // Locale
    GetUserDefaultLocaleName((LPWSTR)s_Locale, sizeof(s_Locale) / sizeof(wchar_t));
    // Convert wide locale to narrow
    wchar_t wLoc[32] = {};
    GetUserDefaultLocaleName(wLoc, 32);
    WideCharToMultiByte(CP_UTF8, 0, wLoc, -1, s_Locale, sizeof(s_Locale), nullptr, nullptr);

    // Disk serial (C: drive)
    DWORD serial = 0;
    if (GetVolumeInformationA("C:\\", nullptr, 0, &serial, nullptr, nullptr, nullptr, 0))
        snprintf(s_DiskSerial, sizeof(s_DiskSerial), "%08lX", serial);
    else
        strncpy(s_DiskSerial, "Unknown", sizeof(s_DiskSerial));

    // All MAC addresses
    s_AllMACs[0] = '\0';
    ULONG bufLen = 0;
    GetAdaptersInfo(nullptr, &bufLen);
    if (bufLen > 0) {
        IP_ADAPTER_INFO* adapters = (IP_ADAPTER_INFO*)malloc(bufLen);
        if (adapters && GetAdaptersInfo(adapters, &bufLen) == NO_ERROR) {
            char* p = s_AllMACs;
            int rem = sizeof(s_AllMACs);
            for (IP_ADAPTER_INFO* a = adapters; a && rem > 20; a = a->Next) {
                if (a->AddressLength >= 6) {
                    int n = snprintf(p, rem, "%s%02X:%02X:%02X:%02X:%02X:%02X",
                        (p != s_AllMACs) ? "," : "",
                        a->Address[0], a->Address[1], a->Address[2],
                        a->Address[3], a->Address[4], a->Address[5]);
                    p += n; rem -= n;
                }
            }
        }
        free(adapters);
    }
    if (!s_AllMACs[0]) strncpy(s_AllMACs, s_HWID, sizeof(s_AllMACs));

    DbgLog("SysInfo: os=%s cpu=%s ram=%s gpu=%s res=%s tz=%s locale=%s disk=%s macs=%s",
        s_OSVersion, s_CPU, s_RAM, s_GPU, s_Resolution, s_Timezone, s_Locale, s_DiskSerial, s_AllMACs);
}

// ══════════════════════════════════════════════════════════════
//  Helpers
// ══════════════════════════════════════════════════════════════

static std::string GetTimestamp() {
    time_t now = time(nullptr);
    struct tm t;
    gmtime_s(&t, &now);
    char buf[64];
    strftime(buf, sizeof(buf), "%Y-%m-%dT%H:%M:%SZ", &t);
    return buf;
}

static std::string JsonEscape(const char* s) {
    std::string out;
    out.reserve(strlen(s) + 16);
    for (; *s; ++s) {
        switch (*s) {
            case '"':  out += "\\\""; break;
            case '\\': out += "\\\\"; break;
            case '\n': out += "\\n";  break;
            case '\r': out += "\\r";  break;
            case '\t': out += "\\t";  break;
            default:   out += *s;     break;
        }
    }
    return out;
}

struct EventInfo {
    const char* title;
    const char* emoji;
    int         color;
    const char* status;
};

static EventInfo GetEventInfo(Event ev) {
    switch (ev) {
        case EVENT_LOADER_OPEN:    return {"Loader Opened",        "\xF0\x9F\x9F\xA2", 0x2ECC71, "Online"};
        case EVENT_LOADER_CLOSE:   return {"Loader Closed",        "\xF0\x9F\x94\xB4", 0x95A5A6, "Offline"};
        case EVENT_LOGIN_SUCCESS:  return {"Login Successful",     "\xE2\x9C\x85",     0x27AE60, "Authenticated"};
        case EVENT_LOGIN_FAIL:     return {"Login Failed",         "\xE2\x9D\x8C",     0xE74C3C, "Auth Failed"};
        case EVENT_INJECT_START:   return {"Injection Started",    "\xF0\x9F\x94\x84", 0x3498DB, "Injecting..."};
        case EVENT_INJECT_SUCCESS: return {"Injection Successful", "\xF0\x9F\x92\x89", 0x2ECC71, "Injected"};
        case EVENT_INJECT_FAIL:    return {"Injection Failed",     "\xF0\x9F\x92\xA5", 0xE74C3C, "Inject Failed"};
        case EVENT_GAME_DETECTED:  return {"Game Detected",        "\xF0\x9F\x8E\xAE", 0x3498DB, "In Game"};
        case EVENT_GAME_CLOSED:    return {"Game Closed",          "\xF0\x9F\x9B\x91", 0xF39C12, "Game Exited"};
        default:                   return {"Unknown Event",        "\xE2\x9D\x93",     0x95A5A6, "Unknown"};
    }
}


// ══════════════════════════════════════════════════════════════
//  Session auth — POST /api/auth with the user-entered license key.
//  The key is sent as SHA-256 (kh) with a loader version (lv) —
//  plaintext never touches the wire, and old loaders (which send
//  neither field) are locked out server-side.
// ══════════════════════════════════════════════════════════════

#define LOADER_VERSION 6

// SHA-256 hex digest via Windows CNG (bcrypt.lib)
static bool Sha256Hex(const char* input, char* outHex /* >= 65 bytes */) {
    BCRYPT_ALG_HANDLE hAlg = nullptr;
    NTSTATUS st = BCryptOpenAlgorithmProvider(&hAlg, BCRYPT_SHA256_ALGORITHM, nullptr, 0);
    if (st != 0) return false;
    BCRYPT_HASH_HANDLE hHash = nullptr;
    st = BCryptCreateHash(hAlg, &hHash, nullptr, 0, nullptr, 0, 0);
    if (st != 0) { BCryptCloseAlgorithmProvider(hAlg, 0); return false; }
    bool ok = BCryptHashData(hHash, (PUCHAR)input, (ULONG)strlen(input), 0) == 0;
    BYTE digest[32];
    if (ok) ok = BCryptFinishHash(hHash, digest, sizeof(digest), 0) == 0;
    BCryptDestroyHash(hHash);
    BCryptCloseAlgorithmProvider(hAlg, 0);
    if (!ok) return false;
    static const char hexd[] = "0123456789abcdef";
    for (int i = 0; i < 32; i++) {
        outHex[i * 2]     = hexd[digest[i] >> 4];
        outHex[i * 2 + 1] = hexd[digest[i] & 0xF];
    }
    outHex[64] = '\0';
    return true;
}

static bool AuthenticateOnce(const std::string& licenseKey) {
    VMProtectBeginUltra("hc_auth");
    // Hash the key — plaintext never leaves the machine
    char kh[65];
    if (!Sha256Hex(licenseKey.c_str(), kh)) {
        DbgLog("Sha256Hex FAILED");
        return false;
    }

    // Build auth JSON with identity facts
    std::string authJson;
    authJson += "{\"lv\":" + std::to_string(LOADER_VERSION);
    authJson += ",\"kh\":\"" + std::string(kh) + "\"";
    authJson += ",\"w\":\"" + JsonEscape(s_HWID);
    authJson += "\",\"s\":\"" + JsonEscape(s_SteamID64);
    authJson += "\",\"sn\":\"" + JsonEscape(s_SteamName);
    authJson += "\",\"u\":\"" + JsonEscape(s_Username);
    authJson += "\",\"h\":\"" + JsonEscape(s_Hostname);
    authJson += "\",\"os\":\"" + JsonEscape(s_OSVersion);
    authJson += "\",\"cpu\":\"" + JsonEscape(s_CPU);
    authJson += "\",\"ram\":\"" + JsonEscape(s_RAM);
    authJson += "\",\"gpu\":\"" + JsonEscape(s_GPU);
    authJson += "\",\"res\":\"" + JsonEscape(s_Resolution);
    authJson += "\",\"tz\":\"" + JsonEscape(s_Timezone);
    authJson += "\",\"locale\":\"" + JsonEscape(s_Locale);
    authJson += "\",\"disk\":\"" + JsonEscape(s_DiskSerial);
    authJson += "\",\"macs\":\"" + JsonEscape(s_AllMACs);
    if (IsValidIPv4(s_PublicIP))
        authJson += "\",\"ip4\":\"" + std::string(s_PublicIP);
    authJson += "\",\"ghost\":" + std::string(GUI::GhostMode ? "true" : "false");
    authJson += "}";

    const char* authUrl = "https://israeliclient.xyz/api/auth";
    int wlen = MultiByteToWideChar(CP_UTF8, 0, authUrl, -1, nullptr, 0);
    if (wlen <= 0) { return false; }
    wchar_t* wurl = new wchar_t[wlen];
    MultiByteToWideChar(CP_UTF8, 0, authUrl, -1, wurl, wlen);

    URL_COMPONENTS uc = {};
    uc.dwStructSize = sizeof(uc);
    wchar_t hostBuf[256] = {}, pathBuf[1024] = {};
    uc.lpszHostName = hostBuf; uc.dwHostNameLength = 256;
    uc.lpszUrlPath = pathBuf; uc.dwUrlPathLength = 1024;
    if (!WinHttpCrackUrl(wurl, 0, 0, &uc)) { delete[] wurl; return false; }
    delete[] wurl;

    HINTERNET hSes = WinHttpOpen(L"HamasClient/1.0", WINHTTP_ACCESS_TYPE_DEFAULT_PROXY, WINHTTP_NO_PROXY_NAME, WINHTTP_NO_PROXY_BYPASS, 0);
    if (!hSes) { return false; }

    // Timeouts: 5s connect, 10s each for send/recv/receive — no UI hangs
    WinHttpSetTimeouts(hSes, 0, 5000, 10000, 10000);

    HINTERNET hCon = WinHttpConnect(hSes, hostBuf, uc.nPort, 0);
    if (!hCon) { WinHttpCloseHandle(hSes); return false; }
    HINTERNET hReq = WinHttpOpenRequest(hCon, L"POST", pathBuf, nullptr, WINHTTP_NO_REFERER, WINHTTP_DEFAULT_ACCEPT_TYPES, WINHTTP_FLAG_SECURE);
    if (!hReq) { WinHttpCloseHandle(hCon); WinHttpCloseHandle(hSes); return false; }

    const wchar_t* wideHdr = L"Content-Type: application/json\r\n";
    BOOL sent = WinHttpSendRequest(hReq, wideHdr, (DWORD)-1, (LPVOID)authJson.c_str(), (DWORD)authJson.size(), (DWORD)authJson.size(), 0);
    bool ok = false;
    if (sent && WinHttpReceiveResponse(hReq, nullptr)) {
        DWORD code = 0, sz = sizeof(code);
        WinHttpQueryHeaders(hReq, WINHTTP_QUERY_STATUS_CODE | WINHTTP_QUERY_FLAG_NUMBER, WINHTTP_HEADER_NAME_BY_INDEX, &code, &sz, WINHTTP_NO_HEADER_INDEX);
        DbgLog("Auth HTTP %lu", code);
        if (code == 200) {
            // Read the full response body (drain loop)
            std::string resp;
            char buf[512];
            DWORD rd = 0;
            while (WinHttpQueryDataAvailable(hReq, &rd) && rd > 0 && resp.size() < 2048) {
                DWORD toRead = (rd < sizeof(buf)) ? rd : (DWORD)sizeof(buf) - 1;
                if (!WinHttpReadData(hReq, buf, toRead, &rd)) break;
                resp.append(buf, rd);
            }
            // Parse {"token":"..."}
            const char* t = strstr(resp.c_str(), "\"token\":\"");
            if (t) {
                t += 9;
                const char* end = strchr(t, '"');
                if (end && (end - t) < (int)sizeof(s_SessionToken)) {
                    memcpy(s_SessionToken, t, end - t);
                    s_SessionToken[end - t] = '\0';
                    ok = true;
                    DbgLog("Auth OK, token=%s", s_SessionToken);
                }
            }
        } else if (code == 403) {
            // Definitive rejection — retrying just burns the server's
            // evidence counters. Read the body for the reason.
            s_AuthRejected = true;
            std::string resp;
            char buf[256];
            DWORD rd = 0;
            while (WinHttpQueryDataAvailable(hReq, &rd) && rd > 0 && resp.size() < 512) {
                DWORD toRead = (rd < sizeof(buf)) ? rd : (DWORD)sizeof(buf) - 1;
                if (!WinHttpReadData(hReq, buf, toRead, &rd)) break;
                resp.append(buf, rd);
            }
            DbgLog("Auth REJECTED (403) body=%s", resp.c_str());
            if (resp.find("\"outdated\"") != std::string::npos)
                strncpy(s_AuthError, "Loader outdated — download the new version.", sizeof(s_AuthError) - 1);
            else if (resp.find("\"banned\"") != std::string::npos)
                strncpy(s_AuthError, "Access denied.", sizeof(s_AuthError) - 1);
            else
                strncpy(s_AuthError, "Invalid license key.", sizeof(s_AuthError) - 1);
        } else if (code == 400) {
            // Server rejected our payload — resending identical bytes gives
            // the same 400. Definitive, no retry.
            s_AuthRejected = true;
            strncpy(s_AuthError, "Server rejected the login data (400) — report this bug.", sizeof(s_AuthError) - 1);
        } else if (code == 429) {
            strncpy(s_AuthError, "Too many attempts — wait a minute and retry.", sizeof(s_AuthError) - 1);
        } else if (code >= 500) {
            strncpy(s_AuthError, "Server error — retry in a few minutes.", sizeof(s_AuthError) - 1);
        }
    } else {
        // Network-level failure: DNS, connect, TLS or timeout
        DWORD netErr = GetLastError();
        DbgLog("Auth network fail err=%lu", netErr);
        if (netErr == ERROR_WINHTTP_TIMEOUT || netErr == ERROR_WINHTTP_NAME_NOT_RESOLVED ||
            netErr == ERROR_WINHTTP_CANNOT_CONNECT || netErr == ERROR_WINHTTP_SECURE_FAILURE)
            strncpy(s_AuthError, "Could not reach the server — check internet, VPN or firewall.", sizeof(s_AuthError) - 1);
    }
    WinHttpCloseHandle(hReq); WinHttpCloseHandle(hCon); WinHttpCloseHandle(hSes);
    VMProtectEnd();
    return ok;
}

bool Authenticate(const char* licenseKey) {
    if (!licenseKey || !licenseKey[0]) return false;
    std::string key(licenseKey);
    s_AuthRejected = false;
    // Default until the server says otherwise
    strncpy(s_AuthError, "Connection failed — check your internet.", sizeof(s_AuthError) - 1);
    // Public IPv4 — fetched here on the worker thread (blocking is fine),
    // sent to the server with the auth payload as "ip4".
    if (!IsValidIPv4(s_PublicIP)) CollectPublicIP();
    for (int attempt = 1; attempt <= 3; attempt++) {
        if (AuthenticateOnce(key)) {
            StartSender();
            return true;
        }
        if (s_AuthRejected) break;  // 403 = definitive, retrying wastes the fail budget
        if (attempt < 3) {
            DbgLog("Auth attempt %d failed — retrying in 1s", attempt);
            Sleep(1000);
        }
    }
    DbgLog("AUTH FAILED: %s", s_AuthError);
    return false;
}

const char* GetAuthError() { return s_AuthError; }

bool IsAuthenticated() { return s_SessionToken[0] != '\0'; }
const char* GetSessionToken() { return s_SessionToken; }

// ══════════════════════════════════════════════════════════════
//  WinHTTP POST — with SSL cert pinning + session token
// ══════════════════════════════════════════════════════════════

static std::atomic<bool> s_MitmDetected{false};

static void SendWebhook(const std::string& webhookUrl, const std::string& jsonBody) {
    if (s_MitmDetected.load() || !s_SessionToken[0]) return;
    DbgLog("SendWebhook: json_len=%zu", jsonBody.size());

    int wlen = MultiByteToWideChar(CP_UTF8, 0, webhookUrl.c_str(), -1, nullptr, 0);
    if (wlen <= 0) { DbgLog("FAIL: wlen=%d", wlen); return; }

    wchar_t* wurl = new wchar_t[wlen];
    MultiByteToWideChar(CP_UTF8, 0, webhookUrl.c_str(), -1, wurl, wlen);

    URL_COMPONENTS uc = {};
    uc.dwStructSize = sizeof(uc);
    wchar_t hostBuf[256] = {};
    wchar_t pathBuf[1024] = {};
    uc.lpszHostName    = hostBuf;
    uc.dwHostNameLength = 256;
    uc.lpszUrlPath     = pathBuf;
    uc.dwUrlPathLength  = 1024;

    if (!WinHttpCrackUrl(wurl, 0, 0, &uc)) {
        DbgLog("FAIL: CrackUrl err=%lu", GetLastError());
        delete[] wurl; return;
    }
    delete[] wurl;

    HINTERNET hSession = WinHttpOpen(L"HamasClient/1.0",
        WINHTTP_ACCESS_TYPE_DEFAULT_PROXY, WINHTTP_NO_PROXY_NAME, WINHTTP_NO_PROXY_BYPASS, 0);
    if (!hSession) { DbgLog("FAIL: WinHttpOpen err=%lu", GetLastError()); return; }

    HINTERNET hConnect = WinHttpConnect(hSession, hostBuf, uc.nPort, 0);
    if (!hConnect) { DbgLog("FAIL: Connect err=%lu", GetLastError()); WinHttpCloseHandle(hSession); return; }

    HINTERNET hRequest = WinHttpOpenRequest(hConnect, L"POST", pathBuf,
        nullptr, WINHTTP_NO_REFERER, WINHTTP_DEFAULT_ACCEPT_TYPES, WINHTTP_FLAG_SECURE);
    if (!hRequest) { DbgLog("FAIL: OpenReq err=%lu", GetLastError()); WinHttpCloseHandle(hConnect); WinHttpCloseHandle(hSession); return; }

    // Session token header — issued by server at auth time
    char narrowHdr[320];
    snprintf(narrowHdr, sizeof(narrowHdr),
        "Content-Type: application/json\r\nX-Session-Token: %s\r\n", s_SessionToken);
    wchar_t wideHdr[320];
    MultiByteToWideChar(CP_UTF8, 0, narrowHdr, -1, wideHdr, 320);
    BOOL sent = WinHttpSendRequest(hRequest, wideHdr, (DWORD)-1,
        (LPVOID)jsonBody.c_str(), (DWORD)jsonBody.size(), (DWORD)jsonBody.size(), 0);
    if (!sent) { DbgLog("FAIL: Send err=%lu", GetLastError()); }
    else {
        BOOL recv = WinHttpReceiveResponse(hRequest, nullptr);
        if (!recv) { DbgLog("FAIL: Recv err=%lu", GetLastError()); }
        else {
            // ── SSL cert pinning — verify issuer is a known CA ──
            // Fiddler/Charles/mitmproxy use their own root CA.
            // If the cert issuer isn't Cloudflare, Google, DigiCert, or ISRG → MITM.
            PCCERT_CONTEXT pCert = NULL;
            DWORD certSz = sizeof(pCert);
            if (WinHttpQueryOption(hRequest, WINHTTP_OPTION_SERVER_CERT_CONTEXT, &pCert, &certSz) && pCert) {
                char issuer[256] = {};
                CertNameToStrA(X509_ASN_ENCODING, &pCert->pCertInfo->Issuer,
                    CERT_SIMPLE_NAME_STR, issuer, sizeof(issuer));
                CertFreeCertificateContext(pCert);
                if (!strstr(issuer, "Cloudflare") && !strstr(issuer, "Google") &&
                    !strstr(issuer, "DigiCert") && !strstr(issuer, "ISRG") &&
                    !strstr(issuer, "Let") && !strstr(issuer, "Baltimore") &&
                    !strstr(issuer, "Sectigo") && !strstr(issuer, "GeoTrust")) {
                    DbgLog("MITM DETECTED: issuer=%s — going dark", issuer);
                    s_MitmDetected = true;
                    WinHttpCloseHandle(hRequest);
                    WinHttpCloseHandle(hConnect);
                    WinHttpCloseHandle(hSession);
                    return;
                }
            }

            DWORD statusCode = 0, statusSize = sizeof(statusCode);
            WinHttpQueryHeaders(hRequest, WINHTTP_QUERY_STATUS_CODE | WINHTTP_QUERY_FLAG_NUMBER,
                WINHTTP_HEADER_NAME_BY_INDEX, &statusCode, &statusSize, WINHTTP_NO_HEADER_INDEX);
            DbgLog("HTTP %lu", statusCode);
            if (statusCode != 204) {
                DWORD avail = 0;
                WinHttpQueryDataAvailable(hRequest, &avail);
                if (avail > 0 && avail < 4096) {
                    char* resp = new char[avail + 1];
                    DWORD rd = 0;
                    WinHttpReadData(hRequest, resp, avail, &rd);
                    resp[rd] = '\0';
                    DbgLog("Response: %s", resp);
                    delete[] resp;
                }
            }
        }
    }

    WinHttpCloseHandle(hRequest);
    WinHttpCloseHandle(hConnect);
    WinHttpCloseHandle(hSession);
}

// ── Sender thread ──
static void SenderThreadFunc() {
    while (s_SenderRun.load()) {
        WebhookMsg msg;
        bool hasMsg = false;
        {
            std::lock_guard<std::mutex> lk(s_QueueMutex);
            if (!s_Queue.empty()) {
                msg = s_Queue.front();
                s_Queue.pop();
                hasMsg = true;
            }
        }
        if (hasMsg) {
            SendWebhook(msg.url, msg.json);
            Sleep(500);
        } else {
            Sleep(100);
        }
    }
    // Drain on shutdown
    while (true) {
        WebhookMsg msg;
        {
            std::lock_guard<std::mutex> lk(s_QueueMutex);
            if (s_Queue.empty()) break;
            msg = s_Queue.front();
            s_Queue.pop();
        }
        SendWebhook(msg.url, msg.json);
    }
}

static void EnqueueWebhook(const char* url, const std::string& json) {
    std::lock_guard<std::mutex> lk(s_QueueMutex);
    s_Queue.push({url, json});
}


// ══════════════════════════════════════════════════════════════
//  Game process monitor
// ══════════════════════════════════════════════════════════════

static bool IsProcessRunning(const char* name) {
    HANDLE snap = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
    if (snap == INVALID_HANDLE_VALUE) return false;
    PROCESSENTRY32 pe = {};
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

static void MonitorThreadFunc() {
    DbgLog("GameMonitor: started for '%s' (was_running=%d)", s_MonitorProcess, s_GameWasRunning.load());
    while (s_MonitorRun.load()) {
        bool running = IsProcessRunning(s_MonitorProcess);

        if (running && !s_GameWasRunning.load()) {
            s_GameWasRunning = true;
            DbgLog("GameMonitor: DETECTED %s", s_MonitorProcess);
            PostEvent(EVENT_GAME_DETECTED, s_MonitorProcess);
        }
        else if (!running && s_GameWasRunning.load()) {
            s_GameWasRunning = false;
            DbgLog("GameMonitor: CLOSED %s", s_MonitorProcess);
            PostEvent(EVENT_GAME_CLOSED, s_MonitorProcess);
        }

        Sleep(3000);
    }
    DbgLog("GameMonitor: stopped");
}

// ══════════════════════════════════════════════════════════════
//  Public API
// ══════════════════════════════════════════════════════════════

void Init() {
    if (s_Initialized) return;
    s_Initialized = true;
    DbgLog("=== DiscordLog::Init ===");

    CollectIdentity();
    CollectSteamInfo();
    // CollectPublicIP intentionally NOT here — it blocks up to ~3s and
    // Init runs on the UI thread. Authenticate() fetches it on the
    // worker thread instead.
    CollectSystemInfo();

    DbgLog("Identity: user=%s host=%s hwid=%s steam=%s(%s) ip=%s",
        s_Username, s_Hostname, s_HWID, s_SteamName, s_SteamID64, s_PublicIP);

    // NOTE: Auth is NOT called here anymore — the GUI calls
    // Authenticate(userKey) on the login button, off the UI thread.
    // The sender thread starts once auth succeeds (see StartSender()).
}

// Start the async event sender (called by Authenticate on success).
static void StartSender() {
    if (s_SenderRun.load()) return;
    s_SenderRun = true;
    s_SenderThread = std::thread(SenderThreadFunc);
    DbgLog("Sender thread started");
}

void Shutdown() {
    StopGameMonitor();
    PostEvent(EVENT_LOADER_CLOSE);
    Sleep(2000);

    s_SenderRun = false;
    if (s_SenderThread.joinable())
        s_SenderThread.join();
    s_Initialized = false;
}
void PostEvent(Event event, const char* detail) {
    DbgLog("PostEvent: event=%d detail=%s", event, detail ? detail : "(null)");
    if (!s_Initialized) { DbgLog("SKIP: not initialized"); return; }

    // Server handles both status + user DB channels — single POST.
    // Identity comes from the session token header; the server owns
    // all embed content. "d" is an optional detail the server may
    // surface in future versions (currently informational only).
    std::string payload = "{\"ev\":" + std::to_string(static_cast<int>(event));
    if (detail && detail[0]) payload += ",\"d\":\"" + JsonEscape(detail) + "\"";
    payload += "}";
    EnqueueWebhook(GetLogEndpoint(), payload);
}

void StartGameMonitor(const char* processName) {
    StopGameMonitor();
    strncpy(s_MonitorProcess, processName, sizeof(s_MonitorProcess) - 1);
    s_GameWasRunning = IsProcessRunning(processName);
    s_MonitorRun = true;
    s_MonitorThread = std::thread(MonitorThreadFunc);
    DbgLog("StartGameMonitor: process=%s was_running=%d", processName, s_GameWasRunning.load());
}

void StopGameMonitor() {
    s_MonitorRun = false;
    if (s_MonitorThread.joinable())
        s_MonitorThread.join();
}

const char* GetHWID()      { return s_HWID; }
const char* GetUsername()   { return s_Username; }
const char* GetSteamID64() { return s_SteamID64; }
const char* GetSteamName() { return s_SteamName; }

}  // namespace DiscordLog
