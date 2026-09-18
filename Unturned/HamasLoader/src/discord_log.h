#pragma once
#include <string>

// ── Discord Webhook Logging System ──
// Sends rich embeds to two Discord channels:
//   1. Activity Log  — every event (login, inject, close, etc.)
//   2. User Status   — mod-only channel, live per-user status updates
//
// SETUP: Replace the webhook URLs below with your actual Discord webhook URLs.
//   - Create webhooks in Discord: Server Settings > Integrations > Webhooks
//   - Activity Log webhook → your #logs channel
//   - User Status webhook  → your #user-status channel (mod-only perms)

namespace DiscordLog {

    // ══════════════════════════════════════════════════════════════
    // Logging endpoints — the real Discord webhook URLs live ONLY on
    // the VPS proxy; the loader only ever talks to these two routes.
    // ══════════════════════════════════════════════════════════════
    inline const char* GetLogEndpoint()    { return "https://israeliclient.xyz/api/log"; }
    inline const char* GetStatusEndpoint() { return "https://israeliclient.xyz/api/status"; }

    // ── Event types ──
    enum Event {
        EVENT_LOADER_OPEN,       // Loader launched
        EVENT_LOADER_CLOSE,      // Loader closed / exiting
        EVENT_LOGIN_SUCCESS,     // Authenticated with valid key
        EVENT_LOGIN_FAIL,        // Bad key attempt
        EVENT_INJECT_START,      // Injection pipeline started
        EVENT_INJECT_SUCCESS,    // Injection completed OK
        EVENT_INJECT_FAIL,       // Injection failed (with reason)
        EVENT_GAME_DETECTED,     // Target game process found running
        EVENT_GAME_CLOSED,       // Target game process exited
    };

    // Initialize the logging system (call once at startup).
    // Collects machine identity (username, hostname, HWID).
    void Init();

    // Authenticate against the VPS with the user-entered license key.
    // Returns true and stores the session token on success.
    // Retries up to 3 times on network failure. Blocking — call off the UI thread.
    bool Authenticate(const char* licenseKey);

    // Human-readable reason for the last auth failure
    // ("Invalid license key.", "Loader outdated...", "Access denied.", ...)
    const char* GetAuthError();

    // True when the current session holds a valid server token.
    bool IsAuthenticated();

    // Session token for file downloads (empty if not authenticated).
    const char* GetSessionToken();

    // Shutdown — posts loader-close event and stops monitor thread.
    void Shutdown();

    // Post an event to Discord. detail is optional extra info.
    // Fires asynchronously — never blocks the UI thread.
    void PostEvent(Event event, const char* detail = nullptr);

    // Start background thread that monitors whether the target game
    // process is running. Posts GAME_DETECTED / GAME_CLOSED events.
    // processName: e.g. "Unturned.exe"
    void StartGameMonitor(const char* processName);

    // Stop the game monitor thread.
    void StopGameMonitor();

    const char* GetHWID();
    const char* GetUsername();
    const char* GetSteamID64();
    const char* GetSteamName();
}
