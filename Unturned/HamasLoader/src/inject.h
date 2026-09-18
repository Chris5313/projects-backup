#pragma once
namespace Inject {
    // Start async injection pipeline for a game.
    //   game: 0 = Unturned, 1 = PvZ GW2, 2 = Roblox (see GameId in inject.cpp)
    //   autoStartGame: auto-launch the game (Steam games only)
    void StartAsync(int game, bool autoStartGame);

    // Check if pipeline finished
    bool IsDone();

    // Whether injection succeeded
    bool Succeeded();

    // Current status message (updates live during pipeline)
    const char* GetStatus();
}
