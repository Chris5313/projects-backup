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
        bool aimbot = false;
        int  aimbot_key = 0;
        bool aimbot_holding = false;
        int  aimbot_value = 0;
        bool aimbot_show_binds = false;
        bool silent_aimbot = false;
        int  fov = 30;
        int  smoothing = 10;
        bool vis_check = true;
        bool friendly = true;
        bool target_players = true;
        int  limb_selection = 0;
        std::vector<std::string> limb_list = { "Head", "Chest", "Pelvis" };
        bool draw_fov = false;
    } c_aimbot;

    struct c_trigger
    {
        bool enable_trigger = false;
        int  enable_trigger_key = 0;
        bool enable_trigger_holding = false;
        int  enable_trigger_value = 0;
        bool enable_trigger_show_binds = false;
        int  delay = 50;
    } c_trigger;

    struct c_esp
    {
        bool enable = false;
        int  box_type = 0;
        std::vector<std::string> box_list = { "2D", "Corner", "3D" };
        bool name = true;
        bool health = true;
        bool distance = true;
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
        int  watermark_position = 0;  // 0 = top_left (changed from 1)
        std::vector<std::string> watermark_content = { "HAMAS CLIENT", "PvZGW2" };
        float watermark_color[4] = { 0.f, 0.65f, 0.32f, 1.f };  // Accent line color
        bool use_accent_color = true;  // Use accent color instead of custom
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