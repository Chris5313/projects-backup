#pragma once
#include <string>
#include <vector>
#include <imgui.h>

struct c_variable
{
    struct c_selection
    {
        std::vector<std::string> selection_icon = {
            "\xef\x81\x9b",  // U+F05B fa-crosshairs (Combat)
            "\xef\x97\xbd",  // U+F5FD fa-layer-group (Visuals)
            "\xef\x83\xa7",  // U+F0E7 fa-bolt (Misc)
            "\xef\x81\xbc",  // U+F07C fa-folder-open (Config)
            "\xef\x87\x9e"   // U+F1DE fa-sliders-h (Settings)
        };
        int   selection        = 0;
        int   selection_active = 0;
        float selection_alpha  = 1.f;
        float selection_add    = 0.f;
    } c_selection;

    struct c_aimbot
    {
        // Regular aimbot (moves mouse toward target)
        bool enable = false;
        int  fov = 60;                   // FOV radius in pixels
        bool draw_fov = true;            // Draw FOV circle
        float fov_color[4] = { 1.f, 1.f, 1.f, 0.5f };  // White, 50% alpha
        int  smoothing = 8;              // 1 = instant, higher = smoother
        int  key = 0x02;                 // VK_RBUTTON (right mouse)
        bool hold_key = true;            // true = hold to aim, false = toggle
        
        // Silent aim (bullets go to target, no visual aim change)
        bool silent_enable = false;
        int  silent_fov = 120;           // Larger FOV for silent
        bool silent_draw_fov = true;
        float silent_fov_color[4] = { 1.f, 0.f, 0.f, 0.3f };  // Red, 30% alpha
        int  silent_key = 0x01;          // VK_LBUTTON (fires with left click)
        
        // Shared settings
        int  target_priority = 0;        // 0=closest to crosshair, 1=closest distance, 2=lowest HP
        std::vector<std::string> priority_list = { "Crosshair", "Distance", "Health" };
        int  limb = 0;                   // 0=head, 1=chest, 2=pelvis
        std::vector<std::string> limb_list = { "Head", "Chest", "Pelvis" };
        float limb_height[3] = { 1.7f, 1.2f, 0.5f };  // World Y offset for each limb
        bool enemies_only = true;        // Only target enemies
        bool require_fresh = true;       // Only target live (moving) entities
        int  max_dist = 100;             // Max targeting distance in meters
    } c_aimbot;

    struct c_trigger
    {
        bool enable_trigger = false;
        int  delay = 50;
    } c_trigger;

    struct c_esp
    {
        bool enable = true;              // v17: ON by default — fresh test builds
                                         // must never silently run with ESP off
                                         // (the v14-v16 'no boxes' mystery)
        int  box_type = 0;
        std::vector<std::string> box_list = { "2D", "Corner" };
        // colors (RGBA floats, editable via color pickers)
        float plant_color[4]  = { 0.31f, 1.00f, 0.31f, 1.00f };  // green
        float zombie_color[4] = { 1.00f, 0.59f, 0.16f, 1.00f };  // orange
        float unknown_color[4]= { 1.00f, 0.86f, 0.24f, 1.00f };  // yellow
        float npc_alpha = 0.5f;          // NPC box transparency
        // toggles
        bool show_ai = true;             // draw boxes for NPCs
        bool require_fresh = false;      // v18: liveness gate OFF by default — if proxy
                                         // position reads fail it all-or-nothing culls
                                         // every box (fresh[] = moved within ~8 s)
        bool distance = true;            // "Xm" under the box
        bool status_info = true;         // on-screen camera/entity health lines
        bool fill_box = true;            // translucent box fill
        // filters
        int  filter = 0;                 // 0 all / 1 enemies only / 2 teammates only
        std::vector<std::string> filter_list = { "All", "Enemies only", "Teammates only" };
        int  max_dist = 250;             // meters
    } c_esp;

    struct c_chams
    {
        bool enable = false;
        float visible_color[4] = { 0, 1, 0, 1 };
        float invisible_color[4] = { 1, 0, 0, 1 };
    } c_chams;

    struct c_misc
    {
        bool no_spread = false;
        bool no_recoil = false;
        bool rapid_fire = false;
        bool speedhack = false;
        float speed_mult = 2.f;
        bool fly_hack = false;
    } c_misc;

    struct c_appearance
    {
        float accent_color[4] = { 0.f, 0.65f, 0.32f, 1.f };  // Hamas green
        bool accent_enabled = true;
        bool rgb_mode = false;
        float rgb_speed = 1.f;
        int font_size = 13;
    } c_appearance;

    struct c_dpi
    {
        float dpi = 1.f;
        bool dpi_changed = false;
    } c_dpi;

    struct c_watermark
    {
        bool watermark = false;
        float pos_x = 10.f;  // Saved X position
        float pos_y = 10.f;  // Saved Y position
        bool use_accent_color = true;  // Use RGB/accent instead of custom
        float watermark_color[4] = { 0.f, 0.65f, 0.32f, 1.f };  // Custom line color
        std::vector<std::string> watermark_content = { "HAMAS CLIENT", "PvZGW2" };
    } c_watermark;

    struct c_notify
    {
        int notify_position = 0;
    } c_notify;

    struct c_colorpicker
    {
        float col_bg_alpha_0[4] = { 0.49f, 0.49f, 0.49f, 1.f };
        float col_bg_alpha_1[4] = { 0.73f, 0.73f, 0.73f, 1.f };
        int hue = 0;
        int alpha = 255;
        bool popup_open = false;
    } c_colorpicker;
};

inline c_variable* var = nullptr;