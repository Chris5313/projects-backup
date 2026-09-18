// ---------------------------------------------------------------------------
// gw2_trigger.cpp — triggerbot v3 implementation.
//
// v3 postmortem (tom-pearl MP dump, teams 16P/13Z fine, bot silent):
//   the entity point is the proxy origin at the entity's FEET, but the game
//   reticle aims at the BODY — a ~7 px point test around the feet point can
//   literally never match a body-aimed crosshair. Fix: ent::DebugList also
//   projects a torso anchor (world y + 1.2 m); the fire test is now a
//   body-column rect [feet..torso] padded by the threshold.
//   Range cap relaxed 40 -> 60 m (the v2 112 m misfires were the seed bug,
//   fixed — the cap no longer needs to be that tight).
//
// v2 fixes (dump15 postmortem: "shooting at anything"):
//   - fresh[] gate: only entities whose position moved in the last ~4 s
//   - same-frame WorldToScreen via DebugList (behind-camera excluded there)
//
// Detection: ent::DebugList projects live entities through the CURRENT
// viewProj; the crosshair is DisplaySize/2. An entity qualifies when it is
// not the local player, its team is known and differs from the local team,
// it is fresh, within 60 m, and the crosshair sits inside its body column.
#include "gw2_trigger.h"
#include "gw2_entities.h"
#include "framework/variables.h"
#include <windows.h>
#include "gw2_viewproj.h"

namespace trigger {

static bool  g_on     = false;
static bool  g_down   = false;    // button currently held (this frame)
static DWORD g_downAt = 0;        // GetTickCount of the DOWN
static DWORD g_last   = 0;        // GetTickCount of the last completed shot
static int   g_hits   = 0;        // shots injected (status/debug)

// horizontal half-width of the body column, scaled off the smaller screen
// axis. ~13 px @1080p: the crosshair must sit on the enemy's center line —
// a miss to the side does NOT fire.
static float ThresholdPx()
{
    ImVec2 ds = ImGui::GetIO().DisplaySize;
    float m = ds.x < ds.y ? ds.x : ds.y;
    return 0.012f * m;
}

static constexpr DWORD TRIG_HOLD_MS = 14;  // button hold before release

void SetEnabled(bool on)
{
    g_on = on;
    if (var) var->c_trigger.enable_trigger = on;   // keep the menu checkbox in sync
}
bool IsEnabled()         { return g_on; }
static void MouseBtn(bool down)
{
    INPUT inp = {};
    inp.type = INPUT_MOUSE;
    inp.mi.dwFlags = down ? MOUSEEVENTF_LEFTDOWN : MOUSEEVENTF_LEFTUP;
    SendInput(1, &inp, sizeof(INPUT));
}

// head anchor height over the feet origin (world +Y). GW2 characters are
// ~1.2-1.8 m tall; 1.7 m matches the ESP head anchor so the trigger column
// covers feet-to-head. (Was 1.2 m: aiming at a head above chest height never
// fired — the column stopped at the chest.)
static constexpr float TORSO_LIFT = 1.7f;

void OnFrame()
{
    DWORD now = GetTickCount();

    // release a held press after one frame's worth of hold time
    if (g_down) {
        if (now - g_downAt >= TRIG_HOLD_MS) {
            MouseBtn(false);
            g_down = false;
        }
        return;   // never press again in the same frame as a release
    }
    bool on = g_on || (var && var->c_trigger.enable_trigger);  // F6 or menu
    if (!on) return;
    DWORD minMs = 100;                       // menu delay (Combat tab), ms
    if (var && var->c_trigger.delay >= 0) minMs = (DWORD)var->c_trigger.delay;
    if (now - g_last < minMs) return;

    float wx[64], wy[64], wz[64], ds[64];
    int   team[64], loc[64], ai[64], fresh[64];
    int n = ent::DebugList(wx, wy, wz, ds, team, loc, ai, fresh, 64);
    if (n <= 0) return;

    // local team from the isLocal row; unknown local/team = no fire (safety)
    int localTeam = -1;
    for (int i = 0; i < n; i++) if (loc[i]) { localTeam = team[i]; break; }
    if (localTeam < 0) return;

    ImVec2 disp = ImGui::GetIO().DisplaySize;
    float cx = disp.x * 0.5f, cy = disp.y * 0.5f;
    float thr = ThresholdPx();

    // body-column hit test: project the entity's feet and head (world y +
    // 1.7 m) through the current viewProj. The hit box is the entity's
    // projected body: height = feet..head span, half-width derived from that
    // height (GW2 body aspect ~0.55, same ratio the ESP box uses) with the
    // pixel threshold as the minimum. (Was a center-line-only column: the
    // crosshair had to sit within ~13 px of the entity's vertical axis —
    // dead-on body hits to the left/right shoulder did NOT fire.)
    bool onTarget = false;
    for (int i = 0; i < n && !onTarget; i++) {
        if (loc[i]) continue;
        if (team[i] < 0 || team[i] == localTeam) continue;   // unknown/teammate
        if (!fresh[i]) continue;        // frozen slot = dead/despawned — NOT a target
        if (ds[i] > 60.f) continue;     // range cap: beyond 60 m aim is fantasy
        float fsx, fsy, tsx, tsy;
        if (!vproj::WorldToScreen(wx[i], wy[i], wz[i], &fsx, &fsy)) continue;
        if (!vproj::WorldToScreen(wx[i], wy[i] + TORSO_LIFT, wz[i], &tsx, &tsy)) {
            tsx = fsx; tsy = fsy;       // extreme-pitch fallback
        }
        float y0 = fsy < tsy ? fsy : tsy, y1 = fsy > tsy ? fsy : tsy;
        float h = y1 - y0;              // projected body height, px
        if (h < 2.f) continue;          // degenerate at range
        float halfW = thr + h * 0.275f; // half body width (0.55 aspect / 2)
        float midX = (fsx + tsx) * 0.5f;
        if (cx >= midX - halfW && cx <= midX + halfW &&
            cy >= y0 - thr && cy <= y1 + thr)
            onTarget = true;
    }
    if (onTarget) {
        MouseBtn(true);
        g_down = true;
        g_downAt = now;
        g_last = now;
        g_hits++;
    }
}
const char* StatusText()
{
    // static buffer, overwritten per call — same pattern as ent::StatusText
    static char s[64];
    if (!g_on) return "TRIG off (F6)";
    // manual itoa, no CRT
    char t[12]; int tn = 0; unsigned v = (unsigned)g_hits;
    if (v == 0) t[tn++] = '0';
    while (v > 0 && tn < 11) { t[tn++] = (char)('0' + v % 10); v /= 10; }
    int k = 0;
    const char* pre = "TRIG ON hits=";
    while (pre[k]) { s[k] = pre[k]; k++; }
    while (tn) s[k++] = t[--tn];
    s[k] = 0;
    return s;
}

} // namespace trigger
