// ---------------------------------------------------------------------------
// gw2_trigger.h — triggerbot v1.
// Crosshair-over-enemy detection from the ESP entity layer + synthetic mouse
// clicks. Toggle: F6. Only fires when the local player's team is known and a
// non-local, non-team entity's projected position is within the pixel
// threshold of screen center.
// ---------------------------------------------------------------------------
#pragma once

namespace trigger {

void SetEnabled(bool on);
bool IsEnabled();

// One Present-thread frame step. Call AFTER ent::OnFrame() so the entity
// table is fresh. Uses ImGui DisplaySize for the crosshair (screen center).
void OnFrame();

const char* StatusText();   // drawn in the top-left status column

} // namespace trigger
