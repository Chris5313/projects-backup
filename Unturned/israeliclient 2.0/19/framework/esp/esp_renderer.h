#pragma once
// IsraeliClient 2.0 — ESP renderer  (header-only, ImGui foreground draw list)
// Full 1:1 port of IsraeliClient 1.0 ESP.cs draw layer.

#include "game_data.h"
#include "gatyware_bridge.h"
#include "gatyware_settings.h"
#include "../settings/variables.h"
#include <imgui.h>
#include <cmath>
#include <cstring>

#ifndef ESP_PI
#define ESP_PI 3.14159265358979323846f
#endif

// Defined in color_picker.cpp (global scope) — rotates any color whose picker
// rainbow slot is on. Declared here so ESP::Render() can keep rainbow colors
// animating while the menu is closed (gui->render() doesn't run then).
void UpdatePerColorRainbow();

namespace ESP {

// ══════════════════════════════════════════════════════════════════════════
//  Draw Primitives  (1:1 port of Draw.cs)
// ══════════════════════════════════════════════════════════════════════════

inline ImU32 Col(const float c[4], float mul_a = 1.f) {
    return IM_COL32(
        (int)(c[0] * 255.f), (int)(c[1] * 255.f),
        (int)(c[2] * 255.f), (int)(c[3] * mul_a * 255.f));
}

// Outlined line: dark shadow (black, half alpha, +1.5 width) then colored core
inline void OLine(ImDrawList* d, float x1, float y1, float x2, float y2, ImU32 col, float w) {
    ImU32 dark = IM_COL32(0, 0, 0, (int)(((col >> 24) & 0xFF) * 0.5f));
    d->AddLine({x1, y1}, {x2, y2}, dark, w + 1.5f);
    d->AddLine({x1, y1}, {x2, y2}, col,  w);
}

// Full box (4 outlined edges)
inline void Box2D(ImDrawList* d, float x, float y, float w, float h, ImU32 col, float th) {
    OLine(d, x, y, x + w, y, col, th);
    OLine(d, x, y + h, x + w, y + h, col, th);
    OLine(d, x, y, x, y + h, col, th);
    OLine(d, x + w, y, x + w, y + h, col, th);
}

// Corner box: 25 % corners
inline void CornerBox2D(ImDrawList* d, float x, float y, float w, float h, ImU32 col, float th) {
    float cl = (w < h ? w : h) * 0.25f;
    OLine(d, x,     y,     x + cl, y,     col, th);
    OLine(d, x,     y,     x,      y + cl, col, th);
    OLine(d, x + w, y,     x + w - cl, y, col, th);
    OLine(d, x + w, y,     x + w, y + cl, col, th);
    OLine(d, x,     y + h, x + cl, y + h, col, th);
    OLine(d, x,     y + h, x,      y + h - cl, col, th);
    OLine(d, x + w, y + h, x + w - cl, y + h, col, th);
    OLine(d, x + w, y + h, x + w, y + h - cl, col, th);
}

// Filled rect, inset 1 px
inline void FillBox(ImDrawList* d, float x, float y, float w, float h, ImU32 col) {
    d->AddRectFilled({x + 1, y + 1}, {x + w - 1, y + h - 1}, col);
}

// ══════════════════════════════════════════════════════════════════════════
//  CHAMS — ImGui overlay body silhouettes
//
//  Material-swap chams crash from a manually-mapped DLL (every mono approach
//  to Renderer/Material modification abort()s due to Unity icall assertions
//  about the calling context). Instead we draw body-shaped filled polygons
//  on the ImGui foreground overlay using the bone system. Always visible
//  through walls (it's an overlay), colored per settings, toggled per keybind.
// ══════════════════════════════════════════════════════════════════════════
// Forward declares
inline bool Project(const float pos[3], float cup, float wh, float ww,
                    float& sx, float& sy, float& sh, float& sw);
namespace xhair { inline void* FollowStub(void* addr); }


// Skeleton line pairs — used by DrawSkeleton AND chams glow pass
static constexpr int kSkelPairs[9][2] = {
    { game::BONE_HEAD, game::BONE_SPINE },
    { game::BONE_SPINE, game::BONE_RARM }, { game::BONE_RARM, game::BONE_RHAND },
    { game::BONE_SPINE, game::BONE_LARM }, { game::BONE_LARM, game::BONE_LHAND },
    { game::BONE_SPINE, game::BONE_RLEG }, { game::BONE_RLEG, game::BONE_RFOOT },
    { game::BONE_SPINE, game::BONE_LLEG }, { game::BONE_LLEG, game::BONE_LFOOT },
};

// Health bar (3 px tall, gradient red→yellow→green)
inline void HealthBar(ImDrawList* d, float x, float y, float w, float pct) {
    if (pct < 0.f) pct = 0.f; if (pct > 1.f) pct = 1.f;
    constexpr float h = 3.f;
    d->AddRectFilled({x - 1, y - 1}, {x + w + 1, y + h + 1}, IM_COL32(0, 0, 0, 128));
    float r, g, b;
    if (pct > 0.5f) {
        float t = (pct - 0.5f) * 2.f;
        r = 1.f  + (0.2f  - 1.f)  * t;
        g = 0.9f + (1.f   - 0.9f) * t;
        b = 0.f  + (0.3f)         * t;
    } else {
        float t = pct * 2.f;
        r = 1.f;
        g = 0.15f + (0.9f  - 0.15f) * t;
        b = 0.1f  + (0.f   - 0.1f)  * t;
    }
    float fw = w * pct; if (fw < 1.f) fw = 1.f;
    d->AddRectFilled({x, y}, {x + fw, y + h},
        IM_COL32((int)(r * 255), (int)(g * 255), (int)(b * 255), 255));
}

// Centered label with shadow
inline void Label(ImDrawList* d, float cx, float y, const char* text, ImU32 col) {
    ImVec2 sz = ImGui::CalcTextSize(text);
    float x = cx - sz.x * 0.5f;
    ImU32 shad = IM_COL32(0, 0, 0, (int)(((col >> 24) & 0xFF) * 0.7f));
    d->AddText({x + 1, y + 1}, shad, text);
    d->AddText({x,     y},     col,  text);
}

// Snapline: plain line from bottom center of screen
inline void SnapLine(ImDrawList* d, float tx, float ty, ImU32 col, float th) {
    d->AddLine({game::display_w * 0.5f, game::display_h}, {tx, ty}, col, th);
}

// 3D AABB (12 edges, optional corner-style 25 %)
inline void Box3D(ImDrawList* d, const float cen[3], const float ext[3], ImU32 col, float th, bool corner) {
    float sp[8][2]; bool vis[8];
    for (int i = 0; i < 8; i++) {
        float c[3] = {
            cen[0] + ((i & 1) ? ext[0] : -ext[0]),
            cen[1] + ((i & 2) ? ext[1] : -ext[1]),
            cen[2] + ((i & 4) ? ext[2] : -ext[2])
        };
        vis[i] = game::world_to_screen(c, sp[i][0], sp[i][1]);
    }
    static constexpr int edges[12][2] = {
        {0,1},{2,3},{4,5},{6,7}, {0,2},{1,3},{4,6},{5,7}, {0,4},{1,5},{2,6},{3,7}
    };
    for (int e = 0; e < 12; e++) {
        int a = edges[e][0], b = edges[e][1];
        if (!vis[a] || !vis[b]) continue;
        if (corner) {
            float dx = (sp[b][0] - sp[a][0]) * 0.25f, dy = (sp[b][1] - sp[a][1]) * 0.25f;
            OLine(d, sp[a][0], sp[a][1], sp[a][0] + dx, sp[a][1] + dy, col, th);
            OLine(d, sp[b][0], sp[b][1], sp[b][0] - dx, sp[b][1] - dy, col, th);
        } else {
            OLine(d, sp[a][0], sp[a][1], sp[b][0], sp[b][1], col, th);
        }
    }
}

// Projected radius circle on the ground (Y plane)
inline void RadiusCircle(ImDrawList* d, const float cen[3], float radius, ImU32 col, float th) {
    constexpr int N = 24;
    float pts[N][2]; bool pv[N];
    for (int i = 0; i < N; i++) {
        float a = (float)i / N * 2.f * ESP_PI;
        float w[3] = { cen[0] + cosf(a) * radius, cen[1], cen[2] + sinf(a) * radius };
        pv[i] = game::world_to_screen(w, pts[i][0], pts[i][1]);
    }
    for (int i = 0; i < N; i++) {
        int j = (i + 1) % N;
        if (pv[i] && pv[j])
            d->AddLine({pts[i][0], pts[i][1]}, {pts[j][0], pts[j][1]}, col, th);
    }
}

// ══════════════════════════════════════════════════════════════════════════
//  Projection helpers
// ══════════════════════════════════════════════════════════════════════════

// Project world pos to 2D screen box (fixed world-space constants).
// Returns top-left (sx, sy) and size (sw, sh).
inline bool Project(const float pos[3], float cup, float wh, float ww,
                    float& sx, float& sy, float& sh, float& sw) {
    float p[3] = { pos[0], pos[1] + cup, pos[2] };
    float cw;
    if (!game::w2s_d(p, sx, sy, cw)) return false;
    float fov_rad = game::cam_fov * (ESP_PI / 180.f) * 0.5f;
    float f = 2.f * cw * tanf(fov_rad);
    if (f < 0.001f) return false;
    sh = (wh / f) * game::display_h;
    sw = (ww / f) * game::display_h;
    sx -= sw * 0.5f;
    sy -= sh * 0.5f;
    return sh > 1.f && sw > 1.f;
}

// Project renderer AABB (8 corners) to 2D screen rect
inline bool ProjectBounds(const float cen[3], const float ext[3],
                          float& sx, float& sy, float& sh, float& sw) {
    float mn_x = 1e30f, mn_y = 1e30f, mx_x = -1e30f, mx_y = -1e30f;
    int vis = 0;
    for (int i = 0; i < 8; i++) {
        float c[3] = {
            cen[0] + ((i & 1) ? ext[0] : -ext[0]),
            cen[1] + ((i & 2) ? ext[1] : -ext[1]),
            cen[2] + ((i & 4) ? ext[2] : -ext[2])
        };
        float px, py;
        if (game::world_to_screen(c, px, py)) {
            if (px < mn_x) mn_x = px; if (px > mx_x) mx_x = px;
            if (py < mn_y) mn_y = py; if (py > mx_y) mx_y = py;
            vis++;
        }
    }
    if (!vis) return false;
    sx = mn_x; sy = mn_y; sw = mx_x - mn_x; sh = mx_y - mn_y;
    return sw > 1.f && sh > 1.f;
}

// ══════════════════════════════════════════════════════════════════════════
//  Utility
// ══════════════════════════════════════════════════════════════════════════

inline float Dist3(const float a[3], const float b[3]) {
    float dx = a[0] - b[0], dy = a[1] - b[1], dz = a[2] - b[2];
    return sqrtf(dx * dx + dy * dy + dz * dz);
}

inline float Clamp01(float v) { return v < 0.f ? 0.f : (v > 1.f ? 1.f : v); }


inline void DrawSkeleton(ImDrawList* d, void* comp, ImU32 col, float th, unsigned long long now) {
    float bp[game::BONE_COUNT][3]; bool bp_ok[game::BONE_COUNT];
    if (game::fill_bone_positions(comp, bp, bp_ok, now) < 3) return;
    for (auto& seg : kSkelPairs) {
        if (!bp_ok[seg[0]] || !bp_ok[seg[1]]) continue;
        float s1x, s1y, s2x, s2y;
        if (game::world_to_screen(bp[seg[0]], s1x, s1y) &&
            game::world_to_screen(bp[seg[1]], s2x, s2y))
            OLine(d, s1x, s1y, s2x, s2y, col, th);
    }
}

// Unified 2D/3D entity box: draws box, fill, and outputs the 2D rect for labels.
inline void DrawEntityBox(ImDrawList* d, const float pos[3], void* comp,
                          float cup, float wh, float ww,
                          const float bcol[4], const float fcol[4], float fa,
                          bool show_box, bool show_fill, float th,
                          bool use_bounds, bool cache_static, unsigned long long now,
                          float& bx, float& by, float& bw, float& bh, bool& visible) {
    visible = false;
    float cen[3], ext[3];
    // Renderer bounds — read from the main-thread-populated cache.
    // get_renderer_bounds reads the cache without invoking GCIC (safe from render thread).
    // The cache is populated by the main-thread Provider.Update hook's bounds refresh.
    bool have = use_bounds && game::get_renderer_bounds(comp, cen, ext, cache_static, now);

    if (var->c_esp.box3d) {
        // 3D mode
        ImU32 c = Col(bcol);
        if (have) Box3D(d, cen, ext, c, th, var->c_esp.corner_mode);
        else {
            float fc[3] = { pos[0], pos[1] + cup, pos[2] };
            float fe[3] = { ww * 0.5f, wh * 0.5f, ww * 0.5f };
            Box3D(d, fc, fe, c, th, var->c_esp.corner_mode);
        }
        // Compute 2D rect for labels
        if (have) { if (!ProjectBounds(cen, ext, bx, by, bh, bw)) if (!Project(pos, cup, wh, ww, bx, by, bh, bw)) return; }
        else      { if (!Project(pos, cup, wh, ww, bx, by, bh, bw)) return; }
        visible = true;
    } else {
        // 2D mode
        if (have) { if (!ProjectBounds(cen, ext, bx, by, bh, bw)) if (!Project(pos, cup, wh, ww, bx, by, bh, bw)) return; }
        else      { if (!Project(pos, cup, wh, ww, bx, by, bh, bw)) return; }
        visible = true;
        if (show_box) {
            ImU32 c = Col(bcol);
            if (var->c_esp.corner_mode) CornerBox2D(d, bx, by, bw, bh, c, th);
            else                        Box2D(d, bx, by, bw, bh, c, th);
        }
    }

    if (visible && show_fill)
        FillBox(d, bx, by, bw, bh,
                IM_COL32((int)(fcol[0]*255), (int)(fcol[1]*255), (int)(fcol[2]*255), (int)(fa*255)));
}

// ══════════════════════════════════════════════════════════════════════════
//  Players
// ══════════════════════════════════════════════════════════════════════════

inline void RenderPlayers(ImDrawList* d, unsigned long long now) {
    auto& s = var->c_pesp;
    game::PlayerInfo buf[64];
    int n = game::get_players(buf, 64);
    for (int i = 0; i < n; i++) {
        auto& p = buf[i];
        if (p.is_dead || p.is_self) continue;
        float dist = Dist3(p.pos, game::local_pos);
        if (dist > s.max_dist) continue;

        float bx, by, bw, bh; bool vis;
        DrawEntityBox(d, p.pos, p.component, 1.1f, 2.2f, 1.3f,
                      s.box_color, s.fill_color, s.fill_alpha,
                      s.box, s.fill, s.thickness,
                      false, false, now, bx, by, bw, bh, vis);
        if (!vis) continue;


        float top = by - 14.f, bot = by + bh + 2.f;

        // Name + distance
        if (s.name || s.distance) {
            char lbl[128];
            if (s.name && s.distance) snprintf(lbl, sizeof(lbl), "%s [%dm]", p.name, (int)dist);
            else if (s.name)          snprintf(lbl, sizeof(lbl), "%s", p.name);
            else                      snprintf(lbl, sizeof(lbl), "[%dm]", (int)dist);
            Label(d, bx + bw * 0.5f, top, lbl, Col(s.name_color));
        }

        // Weapon name (tinted by weapon chams color when enabled)
        if (s.weapon) {
            char wn[64];
            if (game::get_weapon_name(p.component, wn, sizeof(wn))) {
                ImU32 wc = var->c_weapon.weapon_chams
                    ? Col(var->c_weapon.weapon_chams_color)
                    : Col(s.weapon_color);
                Label(d, bx + bw * 0.5f, bot + 14.f, wn, wc);
            }
        }
        // Health bar
        if (s.health) { HealthBar(d, bx, bot, bw, p.health / 100.f); }

        // Skeleton
        if (s.skeleton) DrawSkeleton(d, p.component, Col(s.skel_color), s.thickness, now);

        // Snapline
        if (s.snapline) SnapLine(d, bx + bw * 0.5f, by + bh, Col(s.snap_color), s.thickness);
    }
}

// ══════════════════════════════════════════════════════════════════════════
//  Zombies
// ══════════════════════════════════════════════════════════════════════════

inline void RenderZombies(ImDrawList* d, unsigned long long now) {
    auto& s = var->c_zesp;
    game::ZombieInfo buf[256];
    int n = game::get_zombies(buf, 256);
    for (int i = 0; i < n; i++) {
        auto& z = buf[i];
        float dist = Dist3(z.pos, game::local_pos);
        if (dist > s.max_dist) continue;

        float bx, by, bw, bh; bool vis;
        DrawEntityBox(d, z.pos, z.component, 1.0f, 2.1f, 1.2f,
                      s.box_color, s.fill_color, s.fill_alpha,
                      s.box, s.fill, s.thickness,
                      false, false, now, bx, by, bw, bh, vis);
        if (!vis) continue;


        float top = by - 14.f, bot = by + bh + 2.f;

        if (s.name || s.distance) {
            char lbl[96];
            if (s.name && s.distance) snprintf(lbl, sizeof(lbl), "Zombie [%dm]", (int)dist);
            else if (s.distance)      snprintf(lbl, sizeof(lbl), "[%dm]", (int)dist);
            else { lbl[0] = 'Z'; lbl[1] = 'o'; lbl[2] = 'm'; lbl[3] = 'b'; lbl[4] = 'i'; lbl[5] = 'e'; lbl[6] = 0; }
            Label(d, bx + bw * 0.5f, top, lbl, Col(s.name_color));
        }

        if (s.health && z.max_health > 0)
            HealthBar(d, bx, bot, bw, (float)z.health / (float)z.max_health);

        if (s.skeleton) DrawSkeleton(d, z.component, Col(s.skel_color), s.thickness, now);
        if (s.snapline) SnapLine(d, bx + bw * 0.5f, by + bh, Col(s.snap_color), s.thickness);
    }
}

// ══════════════════════════════════════════════════════════════════════════
//  Items  (distance-fade alpha + screen-space clumping)
// ══════════════════════════════════════════════════════════════════════════

inline void RenderItems(ImDrawList* d) {
    auto& s = var->c_iesp;

    game::ItemInfo buf[512];
    int n = game::get_items(buf, 512);

    bool any_filt = s.filter_weapons || s.filter_ammo || s.filter_medical
                 || s.filter_food    || s.filter_clothing;

    struct Pt { float sx, sy, dist, alpha; char name[64]; };
    Pt pts[512]; int pn = 0;

    for (int i = 0; i < n && pn < 512; i++) {
        auto& it = buf[i];
        float dist = Dist3(it.pos, game::local_pos);
        if (dist > s.max_dist) continue;
        if (any_filt && it.type >= 0) {
            bool ok = false;
            int t = it.type;
            if (s.filter_weapons  && (t == 7 || t == 16 || t == 26)) ok = true;
            if (s.filter_ammo     && t >= 8 && t <= 12)              ok = true;
            if (s.filter_medical  && t == 15)                        ok = true;
            if (s.filter_food     && (t == 13 || t == 14))           ok = true;
            if (s.filter_clothing && t <= 6)                         ok = true;
            if (!ok) continue;
        }
        float sx, sy;
        if (!game::world_to_screen(it.pos, sx, sy)) continue;
        auto& p = pts[pn++];
        p.sx = sx; p.sy = sy; p.dist = dist;
        p.alpha = Clamp01(1.f - dist / s.max_dist) * 0.9f;
        strncpy(p.name, it.name, 63); p.name[63] = 0;
    }
    if (!pn) return;

    if (s.clump) {
        // ── Clumping (40 px radius, max 6 names + "+N more") ──
        constexpr int MAX_CLUMPS = 256, MAX_NAMES = 6;
        struct Clump { float cx, cy, best_d, alpha; int total, nn; char names[MAX_NAMES][64]; };
        Clump cl[MAX_CLUMPS]; int cn = 0;
        int16_t asgn[512]; memset(asgn, -1, sizeof(int16_t) * pn);

        for (int i = 0; i < pn; i++) {
            if (asgn[i] >= 0) continue;
            if (cn >= MAX_CLUMPS) break;
            auto& c = cl[cn];
            c.cx = pts[i].sx; c.cy = pts[i].sy; c.best_d = pts[i].dist;
            c.alpha = pts[i].alpha; c.total = 1; c.nn = 1;
            strncpy(c.names[0], pts[i].name, 63); c.names[0][63] = 0;
            asgn[i] = (int16_t)cn;
            for (int j = i + 1; j < pn; j++) {
                if (asgn[j] >= 0) continue;
                float dx = pts[j].sx - c.cx, dy = pts[j].sy - c.cy;
                if (dx * dx + dy * dy < 1600.f) {  // 40²
                    asgn[j] = (int16_t)cn; c.total++;
                    if (pts[j].dist < c.best_d) { c.best_d = pts[j].dist; c.alpha = pts[j].alpha; }
                    if (c.nn < MAX_NAMES) { strncpy(c.names[c.nn], pts[j].name, 63); c.names[c.nn][63] = 0; c.nn++; }
                }
            }
            cn++;
        }
        for (int i = 0; i < cn; i++) {
            auto& c = cl[i];
            ImU32 col = Col(s.text_color, c.alpha);
            float ty = c.cy;
            for (int j = 0; j < c.nn; j++) {
                char lbl[80];
                if (j == 0) snprintf(lbl, sizeof(lbl), "%s [%dm]", c.names[j], (int)c.best_d);
                else        snprintf(lbl, sizeof(lbl), "%s", c.names[j]);
                Label(d, c.cx, ty, lbl, col); ty += 14.f;
            }
            if (c.total > c.nn) {
                char more[32]; snprintf(more, sizeof(more), "+%d more", c.total - c.nn);
                Label(d, c.cx, ty, more, col);
            }
            if (s.snapline) SnapLine(d, c.cx, c.cy, Col(s.snap_color, c.alpha), 1.f);
        }
    } else {
        for (int i = 0; i < pn; i++) {
            ImU32 col = Col(s.text_color, pts[i].alpha);
            char lbl[80]; snprintf(lbl, sizeof(lbl), "%s [%dm]", pts[i].name, (int)pts[i].dist);
            Label(d, pts[i].sx, pts[i].sy, lbl, col);
            if (s.snapline) SnapLine(d, pts[i].sx, pts[i].sy, Col(s.snap_color, pts[i].alpha), 1.f);
        }
    }
}

// ══════════════════════════════════════════════════════════════════════════
//  Vehicles
// ══════════════════════════════════════════════════════════════════════════

inline void RenderVehicles(ImDrawList* d, unsigned long long now) {
    auto& s = var->c_vesp;
    game::VehicleInfo buf[128];
    int n = game::get_vehicles(buf, 128);
    for (int i = 0; i < n; i++) {
        auto& v = buf[i];
        if (game::g_local_vehicle && v.component == game::g_local_vehicle) continue;
        float dist = Dist3(v.pos, game::local_pos);
        if (dist > s.max_dist) continue;

        float bx, by, bw, bh; bool vis;
        DrawEntityBox(d, v.pos, v.component, 1.5f, 3.0f, 2.2f,
                      s.box_color, s.fill_color, s.fill_alpha,
                      s.box, s.fill, s.thickness,
                      true, false, now, bx, by, bw, bh, vis);
        if (!vis) continue;


        float top = by - 14.f, bot = by + bh + 2.f;

        if (s.name || s.distance || s.locked) {
            char lbl[128]; int off = 0;
            if (s.name)     off += snprintf(lbl + off, sizeof(lbl) - off, "%s", v.name);
            if (s.locked)   off += snprintf(lbl + off, sizeof(lbl) - off, " %s",
                                            v.locked ? "[Locked]" : "[Open]");
            if (s.distance) snprintf(lbl + off, sizeof(lbl) - off, " [%dm]", (int)dist);
            Label(d, bx + bw * 0.5f, top, lbl, Col(s.name_color));
        }

        if (s.health) HealthBar(d, bx, bot, bw, v.health / v.health_max);
        if (s.snapline) SnapLine(d, bx + bw * 0.5f, by + bh, Col(s.snap_color), s.thickness);
    }
}

// ══════════════════════════════════════════════════════════════════════════
//  Animals
// ══════════════════════════════════════════════════════════════════════════

inline void RenderAnimals(ImDrawList* d, unsigned long long now) {
    auto& s = var->c_aesp;
    game::AnimalInfo buf[64];
    int n = game::get_animals(buf, 64);
    for (int i = 0; i < n; i++) {
        auto& a = buf[i];
        float dist = Dist3(a.pos, game::local_pos);
        if (dist > s.max_dist) continue;

        float bx, by, bw, bh; bool vis;
        DrawEntityBox(d, a.pos, a.component, 0.7f, 1.4f, 1.2f,
                      s.box_color, s.fill_color, s.fill_alpha,
                      s.box, s.fill, s.thickness,
                      true, false, now, bx, by, bw, bh, vis);
        if (!vis) continue;

        if (s.name || s.distance) {
            char lbl[80];
            if (s.name && s.distance) snprintf(lbl, sizeof(lbl), "%s [%dm]", a.name, (int)dist);
            else if (s.name)          snprintf(lbl, sizeof(lbl), "%s", a.name);
            else                      snprintf(lbl, sizeof(lbl), "[%dm]", (int)dist);
            Label(d, bx + bw * 0.5f, by - 14.f, lbl, Col(s.name_color));
        }
        if (s.snapline) SnapLine(d, bx + bw * 0.5f, by + bh, Col(s.snap_color), s.thickness);
    }
}

// ══════════════════════════════════════════════════════════════════════════
//  Barricades  (storage / bed / generator / turret)
// ══════════════════════════════════════════════════════════════════════════

inline void RenderBarricades(ImDrawList* d, unsigned long long now) {
    game::BarricadeInfo buf[512];
    int n = game::get_barricades(buf, 512, var->c_sesp.health);

    for (int i = 0; i < n; i++) {
        auto& b = buf[i];

        const float *bc, *fc, *nc, *sc;
        bool en, sb, sf, sn, sd, ss;
        float fa, md, th;

        switch (b.type) {
        case game::BARRICADE_STORAGE: {
            auto& v = var->c_sesp;
            en=v.esp; sb=v.box; bc=v.box_color; sf=v.fill; fc=v.fill_color; fa=v.fill_alpha;
            sn=v.name; nc=v.name_color; sd=v.distance; ss=v.snapline; sc=v.snap_color;
            md=v.max_dist; th=v.thickness;
        } break;
        case game::BARRICADE_BED: {
            auto& v = var->c_besp;
            en=v.esp; sb=v.box; bc=v.box_color; sf=v.fill; fc=v.fill_color; fa=v.fill_alpha;
            sn=v.name; nc=v.name_color; sd=v.distance; ss=v.snapline; sc=v.snap_color;
            md=v.max_dist; th=v.thickness;
            if (v.claimed_only && !b.claimed) continue;
        } break;
        case game::BARRICADE_GENERATOR: {
            auto& v = var->c_gesp;
            en=v.esp; sb=v.box; bc=v.box_color; sf=v.fill; fc=v.fill_color; fa=v.fill_alpha;
            sn=v.name; nc=v.name_color; sd=v.distance; ss=v.snapline; sc=v.snap_color;
            md=v.max_dist; th=v.thickness;
        } break;
        case game::BARRICADE_TURRET: {
            auto& v = var->c_tesp;
            en=v.esp; sb=v.box; bc=v.box_color; sf=v.fill; fc=v.fill_color; fa=v.fill_alpha;
            sn=v.name; nc=v.name_color; sd=v.distance; ss=v.snapline; sc=v.snap_color;
            md=v.max_dist; th=v.thickness;
        } break;
        default: continue;
        }
        if (!en) continue;
        float dist = Dist3(b.pos, game::local_pos);
        if (dist > md) continue;

        float cup, wh, ww;
        switch (b.type) {
            case game::BARRICADE_STORAGE:   cup=0.5f; wh=1.0f; ww=1.0f; break;
            case game::BARRICADE_BED:       cup=0.3f; wh=0.6f; ww=1.0f; break;
            case game::BARRICADE_GENERATOR: cup=0.5f; wh=1.0f; ww=0.8f; break;
            case game::BARRICADE_TURRET:    cup=0.5f; wh=1.0f; ww=0.8f; break;
            default:                        cup=0.5f; wh=1.0f; ww=1.0f; break;
        }

        float bx, by, bw, bh; bool vis;
        DrawEntityBox(d, b.pos, b.model, cup, wh, ww,
                      bc, fc, fa, sb, sf, th,
                      true, true, now, bx, by, bw, bh, vis);
        if (!vis) continue;

        float top = by - 14.f, bot = by + bh + 2.f;

        if (sn || sd) {
            char lbl[128]; int off = 0;
            if (sn) off += snprintf(lbl + off, sizeof(lbl) - off, "%s", b.name);
            switch (b.type) {
            case game::BARRICADE_BED:
                if (var->c_besp.show_claimed)
                    off += snprintf(lbl + off, sizeof(lbl) - off, " %s", b.claimed?"[Claimed]":"[Free]");
                break;
            case game::BARRICADE_GENERATOR:
                if (var->c_gesp.fuel)
                    off += snprintf(lbl + off, sizeof(lbl) - off, " %s %d%%", b.powered?"[ON]":"[OFF]", b.fuel_pct);
                break;
            case game::BARRICADE_TURRET:
                if (var->c_tesp.state)
                    off += snprintf(lbl + off, sizeof(lbl) - off, " %s", b.powered?"[Active]":"[Off]");
                break;
            default: break;
            }
            if (sd) snprintf(lbl + off, sizeof(lbl) - off, " [%dm]", (int)dist);
            Label(d, bx + bw * 0.5f, top, lbl, Col(nc));
        }

        if (b.type == game::BARRICADE_STORAGE && var->c_sesp.health && b.hp >= 0.f)
            HealthBar(d, bx, bot, bw, b.hp);

        if (ss) SnapLine(d, bx + bw * 0.5f, by + bh, Col(sc), th);
    }
}

// ══════════════════════════════════════════════════════════════════════════
//  Airdrops
// ══════════════════════════════════════════════════════════════════════════

inline void RenderAirdrops(ImDrawList* d, unsigned long long now) {
    auto& s = var->c_desp;
    game::AirdropInfo buf[64];
    int n = game::get_airdrops(buf, 64);
    for (int i = 0; i < n; i++) {
        float dist = Dist3(buf[i].pos, game::local_pos);
        if (dist > s.max_dist) continue;

        float bx, by, bw, bh; bool vis;
        DrawEntityBox(d, buf[i].pos, buf[i].component, 1.0f, 2.0f, 1.5f,
                      s.box_color, s.fill_color, s.fill_alpha,
                      s.box, s.fill, s.thickness,
                      true, true, now, bx, by, bw, bh, vis);
        if (!vis) continue;

        if (s.name || s.distance) {
            char lbl[80];
            if (s.name && s.distance) snprintf(lbl, sizeof(lbl), "Airdrop [%dm]", (int)dist);
            else if (s.distance)      snprintf(lbl, sizeof(lbl), "[%dm]", (int)dist);
            else                      strcpy(lbl, "Airdrop");
            Label(d, bx + bw * 0.5f, by - 14.f, lbl, Col(s.name_color));
        }
        if (s.snapline) SnapLine(d, bx + bw * 0.5f, by + bh, Col(s.snap_color), s.thickness);
    }
}

// ══════════════════════════════════════════════════════════════════════════
//  Grenades
// ══════════════════════════════════════════════════════════════════════════

inline void RenderGrenades(ImDrawList* d) {
    auto& s = var->c_resp;
    game::GrenadeInfo buf[64];
    int n = game::get_grenades(buf, 64);
    for (int i = 0; i < n; i++) {
        auto& g = buf[i];
        float dist = Dist3(g.pos, game::local_pos);
        if (dist > s.max_dist) continue;

        float sx, sy;
        if (!game::world_to_screen(g.pos, sx, sy)) continue;

        float bx, by, bh, bw;
        if (Project(g.pos, 0.15f, 0.3f, 0.3f, bx, by, bh, bw) && s.box) {
            ImU32 c = Col(s.box_color);
            if (var->c_esp.corner_mode) CornerBox2D(d, bx, by, bw, bh, c, s.thickness);
            else                        Box2D(d, bx, by, bw, bh, c, s.thickness);
        }
        if (s.name) Label(d, sx, sy - 20.f, g.name, Col(s.name_color));
        if (s.radius && g.range > 0.f) RadiusCircle(d, g.pos, g.range, Col(s.rad_color), s.thickness);
        if (s.snapline) SnapLine(d, sx, sy, Col(s.snap_color), s.thickness);
    }
}

// ══════════════════════════════════════════════════════════════════════════
//  Bullet Tracers  (ghost system: birth-on-first-sight, lifetime fade)
// ══════════════════════════════════════════════════════════════════════════

struct BulletGhost { float origin[3], pos[3]; double birth; bool is_self; bool active; };
inline BulletGhost g_ghosts[300] = {};

inline void RenderBullets(ImDrawList* d) {
    auto& s = var->c_fesp;
    double now = ImGui::GetTime();
    float life = s.lifetime < 0.1f ? 0.1f : s.lifetime;

    // Fetch live bullets
    game::BulletShot raw[256];
    int rn = game::get_bullets(raw, 256, game::g_local_player);

    // Expire old ghosts
    for (auto& g : g_ghosts)
        if (g.active && (now - g.birth) > life) g.active = false;

    // Match live → existing ghosts (update pos); create new ghosts
    for (int r = 0; r < rn; r++) {
        auto& shot = raw[r];
        bool found = false;
        for (auto& g : g_ghosts) {
            if (!g.active || g.is_self != shot.is_self) continue;
            float dx = g.origin[0] - shot.origin[0];
            float dy = g.origin[1] - shot.origin[1];
            float dz = g.origin[2] - shot.origin[2];
            if (dx*dx + dy*dy + dz*dz < 0.01f) {
                memcpy(g.pos, shot.pos, 12); found = true; break;
            }
        }
        if (!found) {
            for (auto& g : g_ghosts)
                if (!g.active) {
                    g.active = true; g.is_self = shot.is_self; g.birth = now;
                    memcpy(g.origin, shot.origin, 12);
                    memcpy(g.pos, shot.pos, 12);
                    break;
                }
        }
    }

    // Draw
    for (auto& g : g_ghosts) {
        if (!g.active) continue;
        if (g.is_self && !s.self) continue;
        if (!g.is_self && !s.others) continue;
        float dist = Dist3(g.pos, game::local_pos);
        if (dist > s.max_dist) continue;

        float fade = Clamp01(1.f - (float)(now - g.birth) / life);
        const float* col = g.is_self ? s.self_color : s.other_color;

        // Trail
        if (s.trail) {
            float s1x, s1y, s2x, s2y;
            if (game::world_to_screen(g.origin, s1x, s1y) &&
                game::world_to_screen(g.pos, s2x, s2y))
                d->AddLine({s1x, s1y}, {s2x, s2y}, Col(col, fade * 0.5f), 1.5f);
        }

        // Marker + label
        float sx, sy;
        if (game::world_to_screen(g.pos, sx, sy)) {
            float sz = 10.f * fade; if (sz < 3.f) sz = 3.f;
            d->AddCircleFilled({sx, sy}, sz, Col(col, fade));
            if (fade > 0.6f)
                Label(d, sx, sy - 20.f, g.is_self ? "Bullet" : "Incoming", Col(col, fade));
        }

        if (s.snapline) {
            float sx2, sy2;
            if (game::world_to_screen(g.pos, sx2, sy2))
                SnapLine(d, sx2, sy2, Col(s.snap_color, fade), 1.f);
        }
    }
}


// ══════════════════════════════════════════════════════════════════════════
//  Custom Crosshair  (ImGui foreground, 5 types + spin/color)
// ══════════════════════════════════════════════════════════════════════════

inline void DrawCustomCrosshair() {
    if (!var->c_crosshair.enable) return;
    if (!game::level_loaded()) return;  // in-game only, like 1.0 (not in menu/loading)
    auto& io = ImGui::GetIO();
    float sw = io.DisplaySize.x, sh = io.DisplaySize.y;
    if (sw < 1.f || sh < 1.f) return;
    ImDrawList* d = ImGui::GetForegroundDrawList();
    float cx = sw * 0.5f, cy = sh * 0.5f;
    float sz = var->c_crosshair.size;
    float th = var->c_crosshair.thick;
    ImU32 col = Col(var->c_crosshair.color);
    float ang = var->c_crosshair.spin ? (float)ImGui::GetTime() * var->c_crosshair.spin_speed * 60.f : 0.f;
    float rad = ang * (ESP_PI / 180.f);

    switch (var->c_crosshair.type_selection) {
    case 0: { // Cross
        float g = sz * 0.25f;
        float cs = cosf(rad), sn = sinf(rad);
        float dirs[4][2] = {{0,-1},{0,1},{-1,0},{1,0}};
        for (int i = 0; i < 4; i++) {
            float dx = dirs[i][0], dy = dirs[i][1];
            float ax = dx*g, ay = dy*g, bx = dx*sz, by = dy*sz;
            d->AddLine({cx+ax*cs-ay*sn, cy+ax*sn+ay*cs}, {cx+bx*cs-by*sn, cy+bx*sn+by*cs}, col, th);
        }
    } break;
    case 1: // Dot
        d->AddCircleFilled({cx,cy}, sz > 1.f ? sz*0.3f : 1.f, col);
        break;
    case 2: { // Circle
        int n = 32;
        for (int i = 0; i < n; i++) {
            float a1 = i*(6.2832f/n)+rad, a2 = (i+1)*(6.2832f/n)+rad;
            d->AddLine({cx+cosf(a1)*sz, cy+sinf(a1)*sz}, {cx+cosf(a2)*sz, cy+sinf(a2)*sz}, col, th);
        }
    } break;
    case 3: { // Diamond
        float px[4], py[4];
        for (int i=0;i<4;i++){float a=i*1.5708f+rad; px[i]=cx+cosf(a)*sz; py[i]=cy+sinf(a)*sz;}
        for (int i=0;i<4;i++){int j=(i+1)%4; d->AddLine({px[i],py[i]},{px[j],py[j]},col,th);}
    } break;
    case 4: { // Star of David (two overlapping triangles)
        for (int t=0;t<2;t++){
            float off = rad + (t ? 1.5708f : -1.5708f);
            float px[3],py[3];
            for(int i=0;i<3;i++){float a=off+i*2.0944f; px[i]=cx+cosf(a)*sz; py[i]=cy+sinf(a)*sz;}
            for(int i=0;i<3;i++){int j=(i+1)%3; d->AddLine({px[i],py[i]},{px[j],py[j]},col,th);}
        }
    } break;
    }
}

// ══════════════════════════════════════════════════════════════════════════
//  Main entry point  (called from HookedPresent / c_gui::render)
// ══════════════════════════════════════════════════════════════════════════

// ── Game crosshair suppression (MinHook version with stub-following) ────────
// mono_compile_method may return jump stubs — FollowStub resolves the real
// code before MH_CreateHook, fixing the earlier MinHook failure.
namespace xhair {

inline void* FollowStub(void* addr) {
    for (int i = 0; i < 5 && addr; i++) {
        BYTE* p = (BYTE*)addr;
        if (p[0] == 0xE9) { addr = p + 5 + *(int*)(p + 1); }
        else if (p[0] == 0xFF && p[1] == 0x25 && *(DWORD*)(p + 2) == 0) { addr = *(void**)(p + 6); }
        else break;
    }
    return addr;
}

} // namespace xhair


// ══════════════════════════════════════════════════════════════════════════
//  Main-thread hook — Provider.Update detour via MinHook
//  Runs FindObjectsOfType (airdrops, grenades) safely on Unity's main thread.
//  The render thread reads the cached component arrays without invoking FoT.
// ══════════════════════════════════════════════════════════════════════════

inline bool mt_hooked = false;
inline void* g_update_jit = nullptr;
inline unsigned char g_update_saved[14] = {};
inline volatile LONG g_update_hooked = 0;

inline void __fastcall hk_provider_update(void* self);

// Patch Provider.Update entry with `mov rax, imm64 (hk); jmp rax` = 12 bytes + 2 NOP.
inline void mt_patch() {
    if (!g_update_jit) return;
    unsigned char patch[14] = { 0x48, 0xB8, 0,0,0,0,0,0,0,0, 0xFF, 0xE0, 0x90, 0x90 };
    void* target = (void*)&hk_provider_update;
    memcpy(patch + 2, &target, 8);
    DWORD oldProt = 0;
    VirtualProtect(g_update_jit, 14, PAGE_EXECUTE_READWRITE, &oldProt);
    for (int i = 0; i < 14; i++) ((unsigned char*)g_update_jit)[i] = patch[i];
    VirtualProtect(g_update_jit, 14, oldProt, &oldProt);
    FlushInstructionCache(GetCurrentProcess(), g_update_jit, 14);
}

// Restore the saved original 14 bytes so it can run at its real address.
inline void mt_restore() {
    if (!g_update_jit) return;
    DWORD oldProt = 0;
    VirtualProtect(g_update_jit, 14, PAGE_EXECUTE_READWRITE, &oldProt);
    for (int i = 0; i < 14; i++) ((unsigned char*)g_update_jit)[i] = g_update_saved[i];
    VirtualProtect(g_update_jit, 14, oldProt, &oldProt);
    FlushInstructionCache(GetCurrentProcess(), g_update_jit, 14);
}

inline void __fastcall hk_provider_update(void* self) {
    if (!g_update_jit) return;

    // Restore original -> run it at its real address (RIP-relative safe) ->
    // re-patch for the next frame. Provider.Update is main-thread only, so
    // there is no concurrent execution of the patched bytes.
    mt_restore();
    ((void(__fastcall*)(void*))g_update_jit)(self);
    mt_patch();

    // C# bootstrap — only needs game::ready (mono + Assembly-CSharp up).
    // invoke_init_on_main_thread() is internally one-shot (g_init_invoked).
    if (game::ready) {
        __try { gatyware::invoke_init_on_main_thread(); }
        __except(1) { game::log("main_thread: gatyware invoke faulted"); }
    }

    // ESP main-thread work — still gated on esp_armed (world loaded).
    if (game::ready && game::esp_armed()) {
        unsigned long long now = GetTickCount64();
        __try {
            game::main_thread_fot_scan(now);
            game::main_thread_bounds_refresh(now);
        } __except(1) {
            game::log("main_thread: tick faulted");
        }
    }
}

inline void InstallMainThreadHook() {
    if (mt_hooked || !game::ready) return;
    void* kl = game::detail::kl_provider;
    if (!kl) return;
    void* m = mono::class_get_method_from_name(kl, "Update", 0);
    if (!m) { mt_hooked = true; game::log("esp: Provider.Update not found"); return; }
    void* c = xhair::FollowStub(mono::compile_method(m));
    if (!c) { mt_hooked = true; game::log("esp: Provider.Update jit failed"); return; }
    g_update_jit = c;

    // Save the original 14 bytes, then write the inline JMP.
    for (int i = 0; i < 14; i++) g_update_saved[i] = ((unsigned char*)c)[i];
    mt_patch();

    InterlockedExchange(&g_update_hooked, 1);
    mt_hooked = true;
    game::log("esp: main-thread inline hook installed (Provider.Update)");
}


// Defined in color_picker.cpp — rotates any color whose picker rainbow slot is on.
// Called here too (not just from the menu) so rainbow colors animate while the
// menu is closed; ESP/crosshair draw from the menu-closed branch of Present.
inline void Render() {
    if (!var) return;

    ::UpdatePerColorRainbow();

    // Crosshair always draws — independent of game state / ESP arming
    DrawCustomCrosshair();

    if (!game::ready) { game::init(); return; }

    // C# bootstrap + Provider.Update main-thread hook: only need game::ready
    // (works at the main menu — world load is NOT required).
    gatyware::try_load();
    gatyware::sync_settings();
    InstallMainThreadHook();

    if (!game::esp_armed()) return;

    game::update(0.f);
    unsigned long long now = GetTickCount64();
    game::refresh_fot_scans(now);

    ImDrawList* d = ImGui::GetForegroundDrawList();

    if (var->c_pesp.esp)  RenderPlayers(d, now);
    if (var->c_zesp.esp)  RenderZombies(d, now);
    if (var->c_iesp.esp)  RenderItems(d);
    if (var->c_vesp.esp)  RenderVehicles(d, now);
    if (var->c_aesp.esp)  RenderAnimals(d, now);
    if (var->c_sesp.esp || var->c_besp.esp || var->c_gesp.esp || var->c_tesp.esp)
        RenderBarricades(d, now);
    if (var->c_desp.esp)  RenderAirdrops(d, now);
    if (var->c_resp.esp)  RenderGrenades(d);
    if (var->c_fesp.esp)  RenderBullets(d);
}

} // namespace ESP
