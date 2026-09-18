// ---------------------------------------------------------------------------
// gw2_aimbot.cpp - aimbot & silent aim implementation
// ---------------------------------------------------------------------------
#include "gw2_aimbot.h"
#include "gw2_entities.h"
#include "gw2_viewproj.h"
#include "framework/variables.h"

#include <windows.h>
#include <cmath>
#include <imgui.h>

namespace aimbot {

// ---------------------------------------------------------------------------
// State
// ---------------------------------------------------------------------------
static bool  g_hasTarget = false;
static float g_targetSX = 0.f, g_targetSY = 0.f;  // Screen coords of current target
static float g_targetDist = 0.f;
static int   g_targetIdx = -1;
static char  g_status[128] = "OFF";

// Key state tracking
static bool g_aimKeyDown = false;
static bool g_aimToggled = false;

// Silent aim hooks (populated by InstallSilentHooks)
static uintptr_t g_bulletSpawnAddr = 0;
static uintptr_t g_aimAngleAddr = 0;
static bool g_silentHooked = false;

// Target aim point in world coords (for silent aim injection)
static float g_aimWorldX = 0.f, g_aimWorldY = 0.f, g_aimWorldZ = 0.f;
static bool  g_silentActive = false;

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------
static float DistanceToCenter(float sx, float sy)
{
    ImVec2 disp = ImGui::GetIO().DisplaySize;
    float cx = disp.x * 0.5f, cy = disp.y * 0.5f;
    float dx = sx - cx, dy = sy - cy;
    return sqrtf(dx * dx + dy * dy);
}

static void MoveMouse(float dx, float dy, int smoothing)
{
    // Apply smoothing
    if (smoothing > 1) {
        dx /= (float)smoothing;
        dy /= (float)smoothing;
    }
    
    // Clamp to reasonable values
    if (dx > 50.f) dx = 50.f;
    if (dx < -50.f) dx = -50.f;
    if (dy > 50.f) dy = 50.f;
    if (dy < -50.f) dy = -50.f;
    
    // SendInput for mouse movement
    INPUT input = {};
    input.type = INPUT_MOUSE;
    input.mi.dx = (LONG)dx;
    input.mi.dy = (LONG)dy;
    input.mi.dwFlags = MOUSEEVENTF_MOVE;
    SendInput(1, &input, sizeof(INPUT));
}

// ---------------------------------------------------------------------------
// Target selection
// ---------------------------------------------------------------------------
struct Target {
    int   idx;
    float sx, sy;        // Screen position
    float dist;          // World distance
    float crosshairDist; // Distance from crosshair in pixels
    float wx, wy, wz;    // World position (aim point)
};

static bool FindBestTarget(Target& out)
{
    if (!var) return false;
    auto& cfg = var->c_aimbot;
    
    float wx[64], wy[64], wz[64], ds[64];
    int   team[64], loc[64], ai[64], fresh[64];
    int n = ent::DebugList(wx, wy, wz, ds, team, loc, ai, fresh, 64);
    if (n <= 0) return false;
    
    // Find local team
    int localTeam = -1;
    for (int i = 0; i < n; i++) {
        if (loc[i]) { localTeam = team[i]; break; }
    }
    
    ImVec2 disp = ImGui::GetIO().DisplaySize;
    float cx = disp.x * 0.5f, cy = disp.y * 0.5f;
    
    // Get FOV for either regular or silent aim (use larger if both)
    float fovPx = 0.f;
    if (cfg.enable && cfg.draw_fov) fovPx = (float)cfg.fov;
    if (cfg.silent_enable && cfg.silent_draw_fov) {
        float sf = (float)cfg.silent_fov;
        if (sf > fovPx) fovPx = sf;
    }
    if (fovPx <= 0.f) fovPx = 9999.f; // No FOV limit
    
    Target best;
    best.idx = -1;
    best.crosshairDist = 99999.f;
    best.dist = 99999.f;
    
    float limbOffset = cfg.limb_height[cfg.limb];
    
    for (int i = 0; i < n; i++) {
        if (loc[i]) continue;  // Skip self
        
        // Enemy filter
        if (cfg.enemies_only && localTeam >= 0 && team[i] == localTeam) continue;
        
        // Fresh filter
        if (cfg.require_fresh && !fresh[i]) continue;
        
        // Distance filter
        if (ds[i] > (float)cfg.max_dist) continue;
        
        // Project aim point (feet + limb offset)
        float aimY = wy[i] + limbOffset;
        float sx, sy;
        if (!vproj::WorldToScreen(wx[i], aimY, wz[i], &sx, &sy)) continue;
        
        float chDist = DistanceToCenter(sx, sy);
        
        // FOV check
        if (chDist > fovPx) continue;
        
        // Priority selection
        bool isBetter = false;
        switch (cfg.target_priority) {
            case 0: // Closest to crosshair
                isBetter = (chDist < best.crosshairDist);
                break;
            case 1: // Closest distance
                isBetter = (ds[i] < best.dist);
                break;
            case 2: // Lowest HP (TODO: need HP data)
                isBetter = (chDist < best.crosshairDist); // Fallback to crosshair
                break;
        }
        
        if (isBetter) {
            best.idx = i;
            best.sx = sx;
            best.sy = sy;
            best.dist = ds[i];
            best.crosshairDist = chDist;
            best.wx = wx[i];
            best.wy = aimY;
            best.wz = wz[i];
        }
    }
    
    if (best.idx >= 0) {
        out = best;
        return true;
    }
    return false;
}

// ---------------------------------------------------------------------------
// Public API
// ---------------------------------------------------------------------------
void Init()
{
    g_hasTarget = false;
    g_silentHooked = false;
    snprintf(g_status, sizeof(g_status), "Ready");
}

void OnFrame()
{
    if (!var) return;
    auto& cfg = var->c_aimbot;
    
    g_hasTarget = false;
    g_silentActive = false;
    
    // Check if either mode is enabled
    bool regularEnabled = cfg.enable;
    bool silentEnabled = cfg.silent_enable;
    
    if (!regularEnabled && !silentEnabled) {
        snprintf(g_status, sizeof(g_status), "OFF");
        return;
    }
    
    // Key handling for regular aimbot
    bool aimActive = false;
    if (regularEnabled) {
        bool keyDown = (GetAsyncKeyState(cfg.key) & 0x8000) != 0;
        if (cfg.hold_key) {
            aimActive = keyDown;
        } else {
            // Toggle mode
            if (keyDown && !g_aimKeyDown) {
                g_aimToggled = !g_aimToggled;
            }
            g_aimKeyDown = keyDown;
            aimActive = g_aimToggled;
        }
    }
    
    // Silent aim activates when firing (left click by default)
    bool silentActive = false;
    if (silentEnabled) {
        silentActive = (GetAsyncKeyState(cfg.silent_key) & 0x8000) != 0;
    }
    
    if (!aimActive && !silentActive) {
        snprintf(g_status, sizeof(g_status), "Idle");
        return;
    }
    
    // Find target
    Target tgt;
    if (!FindBestTarget(tgt)) {
        snprintf(g_status, sizeof(g_status), "No target");
        return;
    }
    
    g_hasTarget = true;
    g_targetSX = tgt.sx;
    g_targetSY = tgt.sy;
    g_targetDist = tgt.dist;
    g_targetIdx = tgt.idx;
    
    // Store aim point for silent aim
    g_aimWorldX = tgt.wx;
    g_aimWorldY = tgt.wy;
    g_aimWorldZ = tgt.wz;
    
    // Regular aimbot: move mouse toward target
    if (aimActive && regularEnabled) {
        ImVec2 disp = ImGui::GetIO().DisplaySize;
        float cx = disp.x * 0.5f, cy = disp.y * 0.5f;
        float dx = tgt.sx - cx;
        float dy = tgt.sy - cy;
        
        // Only move if not already on target
        if (fabsf(dx) > 1.f || fabsf(dy) > 1.f) {
            MoveMouse(dx, dy, cfg.smoothing);
        }
    }
    
    // Silent aim: flag that we want to redirect bullets
    if (silentActive && silentEnabled) {
        g_silentActive = true;
        // The actual angle injection happens in the hook (see InstallSilentHooks)
    }
    
    snprintf(g_status, sizeof(g_status), "Target: %.0fm [%s]", 
             tgt.dist, 
             aimActive ? "AIM" : (silentActive ? "SILENT" : ""));
}

void DrawFOV()
{
    if (!var) return;
    auto& cfg = var->c_aimbot;
    
    ImVec2 disp = ImGui::GetIO().DisplaySize;
    float cx = disp.x * 0.5f, cy = disp.y * 0.5f;
    
    ImDrawList* dl = ImGui::GetBackgroundDrawList();
    
    // Regular aimbot FOV circle
    if (cfg.enable && cfg.draw_fov && cfg.fov > 0) {
        ImU32 col = IM_COL32(
            (int)(cfg.fov_color[0] * 255),
            (int)(cfg.fov_color[1] * 255),
            (int)(cfg.fov_color[2] * 255),
            (int)(cfg.fov_color[3] * 255)
        );
        dl->AddCircle(ImVec2(cx, cy), (float)cfg.fov, col, 64, 1.5f);
    }
    
    // Silent aim FOV circle (larger, different color)
    if (cfg.silent_enable && cfg.silent_draw_fov && cfg.silent_fov > 0) {
        ImU32 col = IM_COL32(
            (int)(cfg.silent_fov_color[0] * 255),
            (int)(cfg.silent_fov_color[1] * 255),
            (int)(cfg.silent_fov_color[2] * 255),
            (int)(cfg.silent_fov_color[3] * 255)
        );
        dl->AddCircle(ImVec2(cx, cy), (float)cfg.silent_fov, col, 64, 1.5f);
    }
    
    // Draw target indicator if we have one
    if (g_hasTarget) {
        ImU32 tgtCol = IM_COL32(255, 0, 0, 200);
        dl->AddCircleFilled(ImVec2(g_targetSX, g_targetSY), 5.f, tgtCol);
        dl->AddLine(ImVec2(cx, cy), ImVec2(g_targetSX, g_targetSY), 
                    IM_COL32(255, 255, 0, 100), 1.f);
    }
}

bool InstallSilentHooks(uintptr_t bulletSpawnAddr, uintptr_t aimAngleAddr)
{
    // TODO: Install MinHook detours once we have the offsets from IDA
    // 
    // For silent aim we need to hook one of:
    // 1. Bullet spawn function - modify trajectory vector before creation
    // 2. Aim angle read - return modified angles when g_silentActive is true
    //
    // The hook would check g_silentActive and if true, calculate the angle
    // from camera to (g_aimWorldX, g_aimWorldY, g_aimWorldZ) and inject that
    // instead of the player's actual aim direction.
    
    if (bulletSpawnAddr == 0 && aimAngleAddr == 0) {
        return false;
    }
    
    g_bulletSpawnAddr = bulletSpawnAddr;
    g_aimAngleAddr = aimAngleAddr;
    
    // Hook installation would go here using MinHook
    // MH_CreateHook(...), MH_EnableHook(...)
    
    g_silentHooked = true;
    return true;
}

const char* StatusText()
{
    return g_status;
}

bool HasTarget()
{
    return g_hasTarget;
}

bool GetTargetScreenPos(float* sx, float* sy)
{
    if (!g_hasTarget) return false;
    *sx = g_targetSX;
    *sy = g_targetSY;
    return true;
}

// ---------------------------------------------------------------------------
// Silent aim angle calculation (for use in hooks)
// ---------------------------------------------------------------------------
// Called from the hooked function to get the redirected aim angles
extern "C" bool GetSilentAimAngles(float* pitch, float* yaw)
{
    if (!g_silentActive || !g_hasTarget) return false;
    
    float cam[3];
    if (!vproj::GetCamera(cam)) return false;
    
    // Vector from camera to target
    float dx = g_aimWorldX - cam[0];
    float dy = g_aimWorldY - cam[1];
    float dz = g_aimWorldZ - cam[2];
    
    float dist = sqrtf(dx*dx + dy*dy + dz*dz);
    if (dist < 0.01f) return false;
    
    // Calculate angles (Frostbite uses radians typically)
    // Yaw: atan2 of X/Z
    // Pitch: asin of Y/dist
    *yaw = atan2f(dx, dz);
    *pitch = asinf(dy / dist);
    
    return true;
}

} // namespace aimbot
