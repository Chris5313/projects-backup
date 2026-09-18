#pragma once

namespace Download {
    // Progress callback — invoked on every status update during
    // FetchFiles. Lets the caller (inject) mirror live download
    // progress into its own status display.
    typedef void (*StatusHook)(const char* status);
    void SetStatusHook(StatusHook hook);

    // Download payload files from the VPS (post-auth) to a random
    // temp folder. Requires a valid session token — call after
    // DiscordLog::Authenticate() succeeded.
    //   game: 0 = Unturned, 1 = PvZ GW2, 2 = Roblox (GameId in inject.cpp)
    // Only the files that game needs are downloaded (GW2 = 2.4MB instead
    // of the full 109MB set).
    // Returns the temp folder path in outDir (caller must clean up)
    bool FetchFiles(int game, char* outDir, int outDirLen);
    const char* GetStatus();

    // v13 TEST: Extract embedded payloads (VPS down fallback)
    // For GW2 only. Extracts IDR_GW2_PAYLOAD and IDR_GW2_KMAP to temp dir.
    bool ExtractEmbedded(int game, char* outDir, int outDirLen);
}
