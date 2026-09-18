#pragma once

namespace Sound {
    void Init();
    void PlayHover();
    void PlayClick();
    void PlayActivate();
    void PlayHamasSong();  // Play embedded Hamas nasheed (on login success)
    void StopHamasSong();
    // Call every frame — tracks hover state changes
    void UpdateHover(bool anyItemHovered);
}
