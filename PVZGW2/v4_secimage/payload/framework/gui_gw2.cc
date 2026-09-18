// gui_gw2.cc — GW2 framework main render (5-tab layout)
// Ported from israeliclient 2.0 gui.cc, rewritten for PvZ: Garden Warfare 2.
#include "functions.h"
#include "font.h"
#include "tab_icons.h"
#include "logo_data.h"
#include "texture_loader.h"

// Per-color rainbow (defined in color_picker.cpp)
void UpdatePerColorRainbow();

// ============================================================
// Framework object instances (allocated once in GW2_Framework_Init)
// ============================================================
static bool s_framework_inited = false;

void GW2_Framework_Init(ID3D11Device* device)
{
    if (s_framework_inited) return;

    // Allocate singletons
    set    = new c_settings();
    clr    = new c_colors();
    var    = new c_variable();
    gui    = new c_gui();
    widget = new c_widget();
    draw   = new c_draw();
    notify = new c_notify();

    // --- Fonts ---
    ImGuiIO& io = ImGui::GetIO();

    // Inter Medium — main UI font at two sizes
    ImFontConfig cfg;
    cfg.FontDataOwnedByAtlas = false;

    set->c_font.inter_medium[0] = io.Fonts->AddFontFromMemoryTTF(
        (void*)inter_medium, sizeof(inter_medium),
        14.0f, &cfg, io.Fonts->GetGlyphRangesDefault());

    set->c_font.inter_medium[1] = io.Fonts->AddFontFromMemoryTTF(
        (void*)inter_medium, sizeof(inter_medium),
        12.0f, &cfg, io.Fonts->GetGlyphRangesDefault());

    // Icon font — FontAwesome 6 Free Solid (tab_icons.h)
    // FA codepoints for tab icons: =aim(f3af) =visuals(f06e) =misc(f013) =config(f085) =settings(f013)
    // Using standard FA codepoints that render cleanly at small sizes
    static const ImWchar fa_ranges[] = { 0xf000, 0xf8ff, 0 };
    for (int i = 0; i < 7; i++) {
        float icon_sizes[] = { 16.f, 22.f, 14.f, 14.f, 14.f, 38.f, 46.f };
        ImFontConfig ifc; ifc.FontDataOwnedByAtlas = false;
        set->c_font.icon[i] = io.Fonts->AddFontFromMemoryTTF(
            (void*)tab_icon_font, sizeof(tab_icon_font),
            icon_sizes[i], &ifc, fa_ranges);
    }

    // Name font (brand text) — just reuse inter medium
    set->c_font.name = set->c_font.inter_medium[1];

    // --- Logo texture ---
    if (device) {
        LoadTextureFromMemory(logo, sizeof(logo), device, &set->c_texture.logo);
    }

    s_framework_inited = true;
}

void GW2_Framework_Shutdown()
{
    if (!s_framework_inited) return;

    delete notify; notify = nullptr;
    delete draw;   draw   = nullptr;
    delete widget; widget = nullptr;
    delete gui;    gui    = nullptr;
    delete var;    var    = nullptr;
    delete clr;    clr    = nullptr;
    delete set;    set    = nullptr;

    s_framework_inited = false;
}

// ============================================================
// Main render — called every frame from Overlay_RenderFrame
// ============================================================
void GW2_Framework_Render()
{
    if (!s_framework_inited) return;

    gui->new_frame();
    {
        notify->setup_notify();

        // RGB mode — rotate accent hue over time
        if (var->c_appearance.rgb_mode) {
            float hue = fmodf((float)ImGui::GetTime() * var->c_appearance.rgb_speed * 0.2f, 1.0f);
            float r, g, b;
            ImGui::ColorConvertHSVtoRGB(hue, 0.85f, 1.0f, r, g, b);
            clr->c_other_clr.accent_clr = ImVec4(r, g, b, 1.0f);
        } else if (var->c_appearance.accent_enabled) {
            clr->c_other_clr.accent_clr = ImVec4(
                var->c_appearance.accent_color[0],
                var->c_appearance.accent_color[1],
                var->c_appearance.accent_color[2],
                var->c_appearance.accent_color[3]);
        }

        // Per-color rainbow
        UpdatePerColorRainbow();

        gui->set_next_window_size(SCALE(set->c_window.window_size));
        gui->begin({ "HAMAS CLIENT" }, { 0 }, set->c_window.window_flags);

        {
            const ImVec2 pos = GetWindowPos();
            const ImVec2 size = GetWindowSize();
            ImDrawList* draw_list = GetWindowDrawList();
            ImGuiStyle* style = &GetStyle();

            // Style setup
            {
                style->WindowBorderSize = SCALE(set->c_window.border_size);
                style->WindowRounding = SCALE(set->c_window.rounding);
                style->WindowPadding = SCALE(set->c_window.padding);
                style->ScrollbarSize = SCALE(set->c_window.scrollbar_size);
                style->ItemSpacing = SCALE(set->c_window.item_spacing);
            }

            // Panel backgrounds (outer + inner)
            draw->add_rect_filled(draw_list, { pos.x, pos.y }, { pos.x + size.x, pos.y + size.y }, gui->get_clr(clr->c_window.general_layout), SCALE(set->c_window.general_rounding));
            draw->add_rect(draw_list, { pos.x, pos.y }, { pos.x + size.x, pos.y + size.y }, gui->get_clr(clr->c_window.general_stroke), SCALE(set->c_window.general_rounding));

            draw->add_rect_filled(draw_list, { pos.x + SCALE(110), pos.y + SCALE(15) }, { pos.x + (size.x - SCALE(15)), pos.y + (size.y - SCALE(15)) }, gui->get_clr(clr->c_window.layout), SCALE(set->c_window.rounding));
            draw->add_rect(draw_list, { pos.x + SCALE(110), pos.y + SCALE(15) }, { pos.x + (size.x - SCALE(15)), pos.y + (size.y - SCALE(15)) }, gui->get_clr(clr->c_window.stroke), SCALE(set->c_window.rounding));

            // Accent glow line — top edge
            {
                const int vtx_start = draw_list->VtxBuffer.Size;
                draw->add_rect_filled(draw_list, { pos.x + SCALE(110), pos.y + SCALE(15) }, { pos.x + (size.x - SCALE(15)), pos.y + SCALE(18) }, gui->get_clr(clr->c_other_clr.accent_clr), SCALE(set->c_window.rounding), ImDrawFlags_RoundCornersTop);
                const int vtx_end = draw_list->VtxBuffer.Size;
                draw->set_linear_color_alpha(draw_list, vtx_start, vtx_end, { pos.x + SCALE(110), pos.y + SCALE(15) }, { pos.x + SCALE(110), pos.y + SCALE(18) }, gui->get_clr(clr->c_other_clr.accent_clr), ImColor(0, 0, 0, 0));
            }

            // Accent glow line — bottom edge
            {
                const int vtx_start = draw_list->VtxBuffer.Size;
                draw->add_rect_filled(draw_list, { pos.x + SCALE(110), pos.y + (size.y - SCALE(18)) }, { pos.x + (size.x - SCALE(15)), pos.y + (size.y - SCALE(15)) }, gui->get_clr(clr->c_other_clr.accent_clr), SCALE(set->c_window.rounding), ImDrawFlags_RoundCornersBottom);
                const int vtx_end = draw_list->VtxBuffer.Size;
                draw->set_linear_color_alpha(draw_list, vtx_start, vtx_end, { pos.x + SCALE(110), pos.y + (size.y - SCALE(18)) }, { pos.x + SCALE(110), pos.y + (size.y - SCALE(15)) }, ImColor(0, 0, 0, 0), gui->get_clr(clr->c_other_clr.accent_clr));
            }

            // Brand logo image (centered in left sidebar)
            {
                const float icon_size = SCALE(90);
                ImVec2 icon_min = { pos.x + SCALE(110) * 0.5f - icon_size * 0.5f, pos.y + size.y * 0.5f - icon_size * 0.5f };
                ImVec2 icon_max = { icon_min.x + icon_size, icon_min.y + icon_size };
                if (set->c_texture.logo)
                    draw->add_image(draw_list, (ImTextureID)set->c_texture.logo, icon_min, icon_max, { 0, 1 }, { 1, 0 }, gui->get_clr(clr->c_other_clr.accent_clr));
            }

            // Vertical "HAMAS" text above icon (stacked, fading toward logo)
            gui->push_font(set->c_font.inter_medium[1]);
            {
                const char* letters_top[] = { "H", "A", "M", "A", "S" };
                const int count_top = 5;
                const float letter_h = SCALE(16);
                const float total_top = letter_h * count_top;
                const float start_y = pos.y + size.y * 0.5f - SCALE(50) - total_top;

                for (int i = 0; i < count_top; i++)
                {
                    float alpha = (float)(i + 1) / (float)count_top;
                    ImVec2 lt_min = { pos.x, start_y + letter_h * i };
                    ImVec2 lt_max = { pos.x + SCALE(110), lt_min.y + letter_h };
                    ImU32 col = ImGui::ColorConvertFloat4ToU32(ImVec4(1.f, 1.f, 1.f, alpha));
                    draw->render_text(draw_list, set->c_font.inter_medium[1], lt_min, lt_max, col, letters_top[i], 0, 0, { 0.5f, 0.5f });
                }
            }
            gui->pop_font();

            // Vertical "CLIENT" text below icon (stacked, fading away from logo)
            gui->push_font(set->c_font.inter_medium[1]);
            {
                const char* letters_bot[] = { "C", "L", "I", "E", "N", "T" };
                const int count_bot = 6;
                const float letter_h = SCALE(16);
                const float total_bot = letter_h * count_bot;
                const float start_y = pos.y + size.y * 0.5f + SCALE(50);

                for (int i = 0; i < count_bot; i++)
                {
                    float alpha = 1.f - (float)i / (float)count_bot;
                    ImVec2 lt_min = { pos.x, start_y + letter_h * i };
                    ImVec2 lt_max = { pos.x + SCALE(110), lt_min.y + letter_h };
                    ImU32 col = ImGui::ColorConvertFloat4ToU32(ImVec4(1.f, 1.f, 1.f, alpha));
                    draw->render_text(draw_list, set->c_font.inter_medium[1], lt_min, lt_max, col, letters_bot[i], 0, 0, { 0.5f, 0.5f });
                }
            }
            gui->pop_font();

            // =================================================================
            // Tab bar — 5 tabs
            // =================================================================
            {
                const float tab_h = SCALE(38);
                const float content_x = pos.x + SCALE(110);
                const float content_w = size.x - SCALE(125);
                const float tab_y = pos.y + SCALE(18);
                const int count = 5;
                const float tab_w = content_w / (float)count;
                const char* tab_labels[] = { "Combat", "Visuals", "Misc", "Config", "Settings" };

                for (int i = 0; i < count; i++) {
                    ImVec2 t_min = { content_x + tab_w * i, tab_y };
                    ImVec2 t_max = { t_min.x + tab_w, tab_y + tab_h };

                    bool hov = ImGui::IsMouseHoveringRect(t_min, t_max);
                    bool pressed = hov && ImGui::GetIO().MouseClicked[0];
                    if (pressed) { var->c_selection.selection = i; }

                    bool active = (var->c_selection.selection_active == i);

                    ImU32 bg = active ? gui->get_clr(clr->c_other_clr.accent_clr, 0.25f) :
                               hov   ? gui->get_clr(clr->c_element.layout, 1.5f) :
                                       IM_COL32(0, 0, 0, 0);
                    if (bg) draw->add_rect_filled(draw_list, t_min, t_max, bg, 0);

                    if (active)
                        draw->add_rect_filled(draw_list, { t_min.x, t_max.y - SCALE(2) }, t_max, gui->get_clr(clr->c_other_clr.accent_clr), 0);

                    // Icon letter
                    ImU32 icon_clr = active ? gui->get_clr(clr->c_other_clr.accent_clr) :
                                    hov    ? gui->get_clr(clr->c_text.text_hov) :
                                             gui->get_clr(clr->c_text.text);
                    {
                        ImVec2 icon_min = { t_min.x, t_min.y };
                        ImVec2 icon_max = { t_min.x + SCALE(30), t_max.y };
                        draw->render_text(draw_list, set->c_font.icon[0], icon_min, icon_max, icon_clr,
                            var->c_selection.selection_icon[i].data(), 0, 0, { 0.7f, 0.5f });
                    }

                    // Label
                    ImU32 txt_clr = active ? gui->get_clr(clr->c_text.text_active) :
                                   hov    ? gui->get_clr(clr->c_text.text_hov) :
                                            gui->get_clr(clr->c_text.text);
                    ImVec2 lbl_min = { t_min.x + SCALE(24), t_min.y };
                    draw->render_text(draw_list, set->c_font.inter_medium[0], lbl_min, t_max, txt_clr,
                        tab_labels[i], 0, 0, { 0.3f, 0.5f });
                }

                // Separator line below tabs
                draw->add_rect_filled(draw_list, { content_x, tab_y + tab_h }, { content_x + content_w, tab_y + tab_h + SCALE(1) }, gui->get_clr(clr->c_window.stroke), 0);
            }

            // Content area
            gui->set_cursor_pos(SCALE(110, 58));

            // Selection fade animation
            var->c_selection.selection_alpha = ImClamp(var->c_selection.selection_alpha + (4.f * ImGui::GetIO().DeltaTime * (var->c_selection.selection == var->c_selection.selection_active ? 1.f : -1.f)), 0.f, 1.f);
            if (var->c_selection.selection_alpha == 0.f && var->c_selection.selection_add == 0.f)
                var->c_selection.selection_active = var->c_selection.selection;

            gui->push_style_var(ImGuiStyleVar_Alpha, var->c_selection.selection_alpha * style->Alpha);

            // Outer scrollable content area
            gui->begin_content("content", GetContentRegionAvail() - SCALE(15, 15), { 15, 15 }, { 15, 15 });
            {
                // =============================================================
                // TAB 0 — Combat
                // =============================================================
                if (var->c_selection.selection_active == 0)
                {
                    gui->begin_group();
                    {
                        gui->begin_child("aimbot");
                        {
                            widget->section("Aimbot", "\xef\x81\x9b");
                            widget->checkbox("Enable", &var->c_aimbot.aimbot);
                            if (var->c_aimbot.aimbot) {
                                widget->separator();
                                widget->checkbox("Silent aim", &var->c_aimbot.silent_aimbot);
                                widget->separator();
                                widget->slider_int("FOV", &var->c_aimbot.fov, 1, 180, 1, "%d");
                                widget->separator();
                                widget->slider_int("Smoothing", &var->c_aimbot.smoothing, 1, 100, 1, "%d");
                                widget->separator();
                                widget->dropdown("Bone", &var->c_aimbot.limb_selection, var->c_aimbot.limb_list, (int)var->c_aimbot.limb_list.size());
                                widget->separator();
                                widget->checkbox("Visibility check", &var->c_aimbot.vis_check);
                            }
                        }
                        gui->end_child();

                        gui->begin_child("triggerbot");
                        {
                            widget->section("Triggerbot", "\xef\x83\xa7");
                            widget->checkbox("Enable", &var->c_trigger.enable_trigger);
                            if (var->c_trigger.enable_trigger) {
                                widget->separator();
                                widget->slider_int("Delay (ms)", &var->c_trigger.delay, 0, 500, 1, "%dms");
                            }
                        }
                        gui->end_child();
                    }
                    gui->end_group();
                }

                // =============================================================
                // TAB 1 — Visuals
                // =============================================================
                if (var->c_selection.selection_active == 1)
                {
                    gui->begin_group();
                    {
                        gui->begin_child("esp");
                        {
                            widget->section("ESP", "\xef\x81\xae");
                            widget->checkbox("Enable", &var->c_esp.enable);
                            if (var->c_esp.enable) {
                                widget->separator();
                                widget->dropdown("Box type", &var->c_esp.box_type, var->c_esp.box_list, (int)var->c_esp.box_list.size());
                                widget->separator();
                                widget->checkbox("Name", &var->c_esp.name);
                                widget->separator();
                                widget->checkbox("Health bar", &var->c_esp.health);
                                widget->separator();
                                widget->checkbox("Distance", &var->c_esp.distance);
                            }
                        }
                        gui->end_child();

                        gui->begin_child("chams");
                        {
                            widget->section("Chams", "\xef\x87\xbc");
                            widget->checkbox("Enable", &var->c_chams.enable);
                            if (var->c_chams.enable) {
                                widget->separator();
                                widget->color_edit("Visible color", var->c_chams.visible_color);
                                widget->separator();
                                widget->color_edit("Invisible color", var->c_chams.invisible_color);
                            }
                        }
                        gui->end_child();
                    }
                    gui->end_group();
                }

                // =============================================================
                // TAB 2 — Misc
                // =============================================================
                if (var->c_selection.selection_active == 2)
                {
                    gui->begin_group();
                    {
                        gui->begin_child("weapon mods");
                        {
                            widget->section("Weapon Mods", "\xef\x82\xad");
                            widget->checkbox("No spread", &var->c_misc.no_spread);
                            widget->separator();
                            widget->checkbox("No recoil", &var->c_misc.no_recoil);
                            widget->separator();
                            widget->checkbox("Rapid fire", &var->c_misc.rapid_fire);
                        }
                        gui->end_child();

                        gui->begin_child("movement");
                        {
                            widget->section("Movement", "\xef\x9c\x8c");
                            widget->checkbox("Speedhack", &var->c_misc.speedhack);
                            if (var->c_misc.speedhack) {
                                widget->separator();
                                widget->slider_float("Speed multiplier", &var->c_misc.speed_mult, 1.f, 10.f, 0.1f, "%.1fx");
                            }
                            widget->separator();
                            widget->checkbox("Fly hack", &var->c_misc.fly_hack);
                        }
                        gui->end_child();
                    }
                    gui->end_group();
                }

                // =============================================================
                // TAB 3 — Config
                // =============================================================
                if (var->c_selection.selection_active == 3)
                {
                    gui->begin_group();
                    {
                        gui->begin_child("config placeholder");
                        {
                            widget->section("Config", "\xef\x81\xbc");
                            // Placeholder — config save/load coming later
                            gui->push_font(set->c_font.inter_medium[1]);
                            ImGui::TextColored(ImVec4(0.5f, 0.55f, 0.7f, 1.f), "Coming soon...");
                            ImGui::TextColored(ImVec4(0.35f, 0.4f, 0.55f, 1.f), "Config save/load will be added in a future update.");
                            gui->pop_font();
                        }
                        gui->end_child();
                    }
                    gui->end_group();
                }

                // =============================================================
                // TAB 4 — Settings
                // =============================================================
                if (var->c_selection.selection_active == 4)
                {
                    gui->begin_group();
                    {
                        gui->begin_child("appearance");
                        {
                            widget->section("Appearance", "\xef\x94\xbf");
                            widget->color_edit("Accent color", var->c_appearance.accent_color);
                            widget->separator();
                            widget->checkbox("Custom accent", &var->c_appearance.accent_enabled);
                            widget->separator();
                            widget->checkbox("RGB mode", &var->c_appearance.rgb_mode);
                            if (var->c_appearance.rgb_mode) {
                                widget->separator();
                                widget->slider_float("RGB speed", &var->c_appearance.rgb_speed, 0.1f, 5.f, 0.1f, "%.1f");
                            }
                            widget->separator();
                            widget->slider_int("Font size", &var->c_appearance.font_size, 11, 18, 1, "%d");
                        }
                        gui->end_child();

                        gui->begin_child("misc settings");
                        {
                            widget->section("Miscellaneous", "\xef\x85\x81");
                            widget->checkbox("Watermark", &var->c_watermark.watermark);
                        }
                        gui->end_child();
                    }
                    gui->end_group();
                }
            }
            gui->end_content();
            gui->pop_style_var();

            // Window drag — from sidebar, tab bar, or any area where no widget is active
            {
                ImGuiContext& g = *GImGui;
                ImGuiWindow* window = g.CurrentWindow;
                ImGuiID move_id = window->MoveId;

                bool is_dragging = (g.MovingWindow == window);
                ImRect sidebar_rect(pos, ImVec2(pos.x + SCALE(110), pos.y + size.y));
                ImRect tabbar_rect(ImVec2(pos.x + SCALE(110), pos.y), ImVec2(pos.x + size.x, pos.y + SCALE(56)));

                bool in_sidebar = sidebar_rect.Contains(ImGui::GetIO().MousePos);
                bool in_tabbar = tabbar_rect.Contains(ImGui::GetIO().MousePos);
                bool no_widget_active = (g.ActiveId == 0 || g.ActiveId == move_id);

                if ((in_sidebar || in_tabbar) && no_widget_active && ImGui::GetIO().MouseClicked[0]) {
                    g.MovingWindow = window;
                    g.ActiveId = move_id;
                    g.ActiveIdClickOffset = ImGui::GetIO().MousePos - pos;
                }
            }
        }
        gui->end();

        // Always render watermark on top of everything
        gui->water_mark("watermark", var->c_watermark.watermark_content,
            static_cast<watermark_position>(var->c_watermark.watermark_position),
            &var->c_watermark.watermark);

        gui->end_frame();
    }
}

// ============================================================
// Watermark-only render - called even when menu is closed
// ============================================================
void GW2_Framework_RenderWatermark()
{
    if (!s_framework_inited) return;

    gui->new_frame();
    {
        // RGB mode for accent color
        if (var->c_appearance.rgb_mode) {
            float hue = fmodf((float)ImGui::GetTime() * var->c_appearance.rgb_speed * 0.2f, 1.0f);
            float r, g, b;
            ImGui::ColorConvertHSVtoRGB(hue, 0.85f, 1.0f, r, g, b);
            clr->c_other_clr.accent_clr = ImVec4(r, g, b, 1.0f);
        } else if (var->c_appearance.accent_enabled) {
            clr->c_other_clr.accent_clr = ImVec4(
                var->c_appearance.accent_color[0],
                var->c_appearance.accent_color[1],
                var->c_appearance.accent_color[2],
                var->c_appearance.accent_color[3]);
        }

        // Render watermark
        gui->water_mark("watermark", var->c_watermark.watermark_content,
            static_cast<watermark_position>(var->c_watermark.watermark_position),
            &var->c_watermark.watermark);

        gui->end_frame();
    }
}
