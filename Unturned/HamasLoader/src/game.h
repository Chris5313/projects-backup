#pragma once
#include <string>

namespace Game {
    // Find the Unturned install path by scanning all Steam libraries
    // Returns empty string if not found
    std::string FindUnturnedPath();

    // Check if Unturned.exe is already running
    bool IsUnturnedRunning();

    // Launch Unturned via Steam (BattlEye version)
    // Returns true if launched successfully
    bool LaunchUnturned();

    // Get last error message
    const char* GetError();
}
