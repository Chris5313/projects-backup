#include "settings/functions.h"
#include "shader/blur.hpp"
#ifdef BUILD_DLL
#include "esp/esp_renderer.h"
#endif

// Per-color rainbow (defined in color_picker.cpp)
void UpdatePerColorRainbow();

void c_gui::render()
{
    gui->new_frame();
    {
        notify->setup_notify();

        // RGB mode — rotate accent hue over time (only accent, not the whole UI)
        if (var->c_appearance.rgb_mode) {
            float hue = fmodf((float)ImGui::GetTime() * var->c_appearance.rgb_speed * 0.2f, 1.0f);
            float r, g, b;
            ImGui::ColorConvertHSVtoRGB(hue, 0.85f, 1.0f, r, g, b);
            clr->c_other_clr.accent_clr = ImVec4(r, g, b, 1.0f);
        } else if (var->c_appearance.accent_enabled) {
            // Apply user accent color from settings
            clr->c_other_clr.accent_clr = ImVec4(
                var->c_appearance.accent_color[0],
                var->c_appearance.accent_color[1],
                var->c_appearance.accent_color[2],
                var->c_appearance.accent_color[3]);
        }

        // Per-color rainbow — any individual color with rainbow enabled gets rotated
        UpdatePerColorRainbow();

        // Background image — skip in DLL overlay (covers the game)
#ifndef BUILD_DLL
        if (set->c_texture.bg) draw->add_image(GetBackgroundDrawList(), set->c_texture.bg, { 0, 0 }, { 1920, 1080 }, { 0, 0 }, { 1, 1 }, gui->get_clr(clr->c_other_clr.white_clr));
#endif

        gui->set_next_window_size(SCALE(set->c_window.window_size));

        gui->begin({ "ISRAELI CLIENT" }, { 0 }, set->c_window.window_flags);

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


            // Background blur — skip in DLL (causes reflective artifact on emulated GPU)
#ifndef BUILD_DLL
            draw_background_blur(draw_list, g_pSwapChain, g_pd3dDevice, g_pd3dDeviceContext, GetWindowPos(), GetWindowPos() + GetWindowSize(), style->WindowRounding);
#endif

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
                if (set->c_texture.logo) draw->add_image(draw_list, (ImTextureID)set->c_texture.logo, icon_min, icon_max, { 0, 1 }, { 1, 0 }, gui->get_clr(clr->c_other_clr.accent_clr));
            }

            // Vertical "ISRAEL" text above icon (stacked letters, fading out going up)
            gui->push_font(set->c_font.inter_medium[1]);
            {
                const char* letters_top[] = { "I", "S", "R", "A", "E", "L" };
                const int count_top = 6;
                const float letter_h = SCALE(16);
                const float total_top = letter_h * count_top;
                const float start_y = pos.y + size.y * 0.5f - SCALE(50) - total_top;

                for (int i = 0; i < count_top; i++)
                {
                    float alpha = (float)(i + 1) / (float)count_top; // fades in toward icon
                    ImVec2 lt_min = { pos.x, start_y + letter_h * i };
                    ImVec2 lt_max = { pos.x + SCALE(110), lt_min.y + letter_h };
                    ImU32 col = ImGui::ColorConvertFloat4ToU32(ImVec4(1.f, 1.f, 1.f, alpha));
                    draw->render_text(draw_list, set->c_font.inter_medium[1], lt_min, lt_max, col, letters_top[i], 0, 0, { 0.5f, 0.5f });
                }
            }
            gui->pop_font();

            // Vertical "CLIENT" text below icon (stacked letters, fading out going down)
            gui->push_font(set->c_font.inter_medium[1]);
            {
                const char* letters_bot[] = { "C", "L", "I", "E", "N", "T" };
                const int count_bot = 6;
                const float letter_h = SCALE(16);
                const float total_bot = letter_h * count_bot;
                const float start_y = pos.y + size.y * 0.5f + SCALE(50);

                for (int i = 0; i < count_bot; i++)
                {
                    float alpha = 1.f - (float)i / (float)count_bot; // fades out away from icon
                    ImVec2 lt_min = { pos.x, start_y + letter_h * i };
                    ImVec2 lt_max = { pos.x + SCALE(110), lt_min.y + letter_h };
                    ImU32 col = ImGui::ColorConvertFloat4ToU32(ImVec4(1.f, 1.f, 1.f, alpha));
                    draw->render_text(draw_list, set->c_font.inter_medium[1], lt_min, lt_max, col, letters_bot[i], 0, 0, { 0.5f, 0.5f });
                }
            }
            gui->pop_font();

            // =================================================================
            // Tab bar — inside content panel, full width
            // =================================================================
            {
                const float tab_h = SCALE(38);
                const float content_x = pos.x + SCALE(110);
                const float content_w = size.x - SCALE(125);  // 110 left + 15 right
                const float tab_y = pos.y + SCALE(18);         // just below accent line
                const int count = 7;
                const float tab_w = content_w / (float)count;
                const char* tab_labels[] = { "Combat", "Visuals", "Players", "Misc", "Auto", "Config", "Settings" };

                for (int i = 0; i < count; i++) {
                    ImVec2 t_min = { content_x + tab_w * i, tab_y };
                    ImVec2 t_max = { t_min.x + tab_w, tab_y + tab_h };
                    ImGuiID tid = GetID(tab_labels[i]);

                    bool hov = ImGui::IsMouseHoveringRect(t_min, t_max);
                    bool pressed = hov && ImGui::GetIO().MouseClicked[0];
                    if (pressed) { var->c_selection.selection = i; ui_sound::tab_switch(); }

                    bool active = (var->c_selection.selection_active == i);

                    // Background
                    ImU32 bg = active ? gui->get_clr(clr->c_other_clr.accent_clr, 0.25f) :
                               hov   ? gui->get_clr(clr->c_element.layout, 1.5f) :
                                       IM_COL32(0, 0, 0, 0);
                    if (bg) draw->add_rect_filled(draw_list, t_min, t_max, bg, 0);

                    // Bottom accent bar on active tab
                    if (active)
                        draw->add_rect_filled(draw_list, { t_min.x, t_max.y - SCALE(2) }, t_max, gui->get_clr(clr->c_other_clr.accent_clr), 0);

                    // Icon
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

            // Content area — shifted down below tab bar
            gui->set_cursor_pos(SCALE(110, 58));

            // Selection fade animation
            var->c_selection.selection_alpha = ImClamp(var->c_selection.selection_alpha + (4.f * ImGui::GetIO().DeltaTime * (var->c_selection.selection == var->c_selection.selection_active ? 1.f : -1.f)), 0.f, 1.f);
            if (var->c_selection.selection_alpha == 0.f && var->c_selection.selection_add == 0.f)
                var->c_selection.selection_active = var->c_selection.selection;

            gui->push_style_var(ImGuiStyleVar_Alpha, var->c_selection.selection_alpha * style->Alpha);

            // Outer scrollable content area
            gui->begin_content("content", GetContentRegionAvail() - SCALE(15, 15), { 15, 15 }, { 15, 15 });
            {
#ifdef BUILD_DLL
                static bool cfg_was_on_config = false;
#endif
                // =============================================================
                // TAB 0 — Combat
                // =============================================================
                if (var->c_selection.selection_active == 0)
                {
                    gui->begin_group();
                    {
                        gui->begin_child("aimbot");
                        {
                            widget->section("Aimbot");
                            widget->checkbox_with_key("Enable aimbot", &var->c_aimbot.aimbot, &var->c_aimbot.aimbot_key, &var->c_aimbot.aimbot_holding, &var->c_aimbot.aimbot_value, &var->c_aimbot.aimbot_show_binds);
                            if (var->c_aimbot.aimbot) {
                                widget->separator();
                                widget->checkbox("Silent aimbot", &var->c_aimbot.silent_aimbot);
                                widget->separator();
                                widget->slider_int("FOV", &var->c_aimbot.fov, 1, 180, 1, "%d");
                                widget->separator();
                                widget->slider_int("Smoothing", &var->c_aimbot.smoothing, 1, 100, 1, "%d");
                                widget->section("Targeting");
                                widget->checkbox("Visibility check", &var->c_aimbot.vis_check);
                                widget->separator();
                                widget->checkbox("Skip friendly", &var->c_aimbot.friendly);
                                widget->separator();
                                widget->checkbox("Target players", &var->c_aimbot.target_players);
                                widget->separator();
                                widget->checkbox("Target zombies", &var->c_aimbot.target_zombies);
                                widget->separator();
                                widget->dropdown("Limb", &var->c_aimbot.limb_selection, var->c_aimbot.limb_list, var->c_aimbot.limb_list.size());
                                widget->separator();
                                widget->checkbox("Draw FOV circle", &var->c_aimbot.draw_fov);
                                widget->separator();
                                widget->checkbox("Spinbot", &var->c_aimbot.spinbot);
                                if (var->c_aimbot.spinbot) {
                                    widget->separator();
                                    widget->dropdown("Spin type", &var->c_aimbot.spin_type, var->c_aimbot.spin_list, var->c_aimbot.spin_list.size());
                                    widget->separator();
                                    widget->checkbox("Show spin", &var->c_aimbot.spin_show);
                                }
                            }
                        }
                        gui->end_child();

                        gui->begin_child("triggerbot");
                        {
                            widget->checkbox_with_key("Enable triggerbot", &var->c_trigger.enable_trigger, &var->c_trigger.enable_trigger_key, &var->c_trigger.enable_trigger_holding, &var->c_trigger.enable_trigger_value, &var->c_trigger.enable_trigger_show_binds);
                            widget->set_tooltip("Triggerbot", "Automatically fires when crosshair is on a target.");
                            if (var->c_trigger.enable_trigger) {
                                widget->separator();
                                widget->slider_float("Delay", &var->c_trigger.delay, 0.f, 1.f, 0.01f, "%.2fs");
                                widget->separator();
                                widget->checkbox("Hold fire", &var->c_trigger.hold_fire);
                            }
                        }
                        gui->end_child();
                    }
                    gui->end_group();

                    gui->sameline();

                    gui->begin_group();
                    {
                        gui->begin_child("weapon mods");
                        {
                            widget->section("Weapon Mods");
                            widget->checkbox("No recoil", &var->c_weapon.no_recoil);
                            widget->separator();
                            widget->checkbox("No spread", &var->c_weapon.no_spread);
                            widget->separator();
                            widget->checkbox("No sway", &var->c_weapon.no_sway);
                            widget->separator();
                            if (var->c_weapon.rapid_fire) {
                                widget->separator();
                                widget->slider_float("Rate multiplier", &var->c_weapon.rapid_mult, 1.f, 10.f, 0.1f, "%.1fx");
                            }
                            widget->separator();
                            widget->checkbox("Instant reload", &var->c_weapon.instant_reload);
                            widget->separator();
                            widget->checkbox("Extended magazine", &var->c_weapon.extended_mag);
                            if (var->c_weapon.extended_mag) {
                                widget->separator();
                                widget->slider_int("Mag multiplier", &var->c_weapon.mag_mult, 1, 10, 1, "%dx");
                            }
                            widget->separator();
                            widget->checkbox("Extended melee", &var->c_weapon.extended_melee);
                            if (var->c_weapon.extended_melee) {
                                widget->separator();
                                widget->slider_float("Melee range", &var->c_weapon.melee_range, 1.f, 20.f, 0.5f, "%.1fm");
                            }
                            widget->separator();
                            widget->checkbox_with_color("Weapon chams", &var->c_weapon.weapon_chams, var->c_weapon.weapon_chams_color, true);
                            widget->separator();
                            widget->checkbox("Hitsound", &var->c_weapon.hitsound);
                            if (var->c_weapon.hitsound) {
                                widget->separator();
                                widget->slider_float("Hit volume", &var->c_weapon.hitsound_vol, 0.f, 1.f, 0.05f, "%.0f%%");
                                widget->separator();
                                widget->slider_float("Hit pitch", &var->c_weapon.hitsound_pitch, 0.5f, 2.f, 0.05f, "%.2f");
                            }
                            widget->separator();
                            widget->checkbox("Disable scope", &var->c_weapon.disable_scope);
                            widget->separator();
                            widget->checkbox("Disable binoculars", &var->c_weapon.disable_bino);
                            widget->separator();
                            widget->checkbox("Instant aim", &var->c_weapon.instant_aim);
                            widget->separator();
                            widget->checkbox("No ballistics", &var->c_weapon.no_ballistics);
                            widget->separator();
                            widget->checkbox("Force headshot", &var->c_weapon.force_headshot);
                            widget->separator();
                            widget->checkbox("Auto semi burst", &var->c_weapon.auto_semi_burst);
                            widget->separator();
                            widget->checkbox("Extend ballistic range", &var->c_weapon.extend_ballistic_range);
                            if (var->c_weapon.extend_ballistic_range) {
                                widget->separator();
                                widget->slider_int("Ballistic steps", &var->c_weapon.extra_ballistic_steps, 1, 16, 1, "%d");
                            }
                            widget->separator();
                            widget->checkbox("Ignore leave timer", &var->c_weapon.ignore_leave_timer);
                            widget->separator();
                            widget->slider_float("Damage flinch", &var->c_weapon.damage_flinch_mult, 0.f, 2.f, 0.05f, "%.2fx");
                        }
                        gui->end_child();

                        gui->begin_child("silent aim");
                        {
                            widget->section("Silent Aim");
                            widget->checkbox_with_key("Enable silent aim", &var->c_silent.silent, &var->c_silent.silent_key, &var->c_silent.silent_holding, &var->c_silent.silent_value, &var->c_silent.silent_show_binds);
                            widget->set_tooltip("Silent Aim", "Redirects bullets server-side to hit targets.");
                            if (var->c_silent.silent) {
                                widget->separator();
                                widget->slider_int("Hit chance", &var->c_silent.chance, 1, 100, 1, "%d%%");
                                widget->separator();
                                widget->checkbox("FOV restrict", &var->c_silent.fov_restrict);
                                if (var->c_silent.fov_restrict) {
                                    widget->separator();
                                    widget->slider_float("Silent FOV", &var->c_silent.silent_fov, 1.f, 180.f, 1.f, "%.0f");
                                }
                                widget->separator();
                                widget->slider_float("Max distance", &var->c_silent.max_dist, 10.f, 1000.f, 10.f, "%.0fm");
                                widget->separator();
                                widget->checkbox("Visibility check", &var->c_silent.vis_check);
                                widget->separator();
                                widget->dropdown("Limb", &var->c_silent.limb_selection, var->c_silent.limb_list, var->c_silent.limb_list.size());
                                widget->separator();
                                widget->dropdown("Target priority", &var->c_silent.target_point, var->c_silent.target_list, var->c_silent.target_list.size());
                                widget->section("Filters");
                                widget->checkbox("Target players", &var->c_silent.target_players);
                                widget->separator();
                                widget->checkbox("Target zombies", &var->c_silent.target_zombies);
                                widget->separator();
                                widget->checkbox("Skip friendly", &var->c_silent.skip_friendly);
                                widget->separator();
                                widget->slider_float("Bounds expand", &var->c_silent.bounds_exp, 0.f, 5.f, 0.1f, "%.1f");
                                widget->separator();
                                widget->checkbox("Draw FOV circle", &var->c_silent.draw_fov);
                                widget->separator();
                                widget->checkbox("Hitbox expander", &var->c_silent.hitbox_expand);
                                widget->set_tooltip("Hitbox Expander", "Invisible colliders on layer 23 for shoot-through-walls. Proton-safe.");
                            }
                        }
                        gui->end_child();

                        gui->begin_child("proton");
                        {
                            widget->section("Anti-Cheat");
                            widget->checkbox("Proton bypass", &var->c_proton.bypass);
                            widget->set_tooltip("Proton Bypass", "Neutralizes ProtonAC server-side checks.");
                            if (var->c_proton.bypass) {
                                widget->separator();
                                widget->checkbox("Proton-safe silent aim", &var->c_silent.proton_mode);
                                widget->set_tooltip("Proton Silent", "Spoofs aim packets to match hits. Required on Proton servers.");
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
                        gui->begin_child("player esp");
                        {
                            widget->section("Player ESP");
                            widget->checkbox_with_key("Enable ESP", &var->c_pesp.esp, &var->c_pesp.esp_key, &var->c_pesp.esp_holding, &var->c_pesp.esp_value, &var->c_pesp.esp_show_binds);
                            widget->set_tooltip("Player ESP", "Renders player information through walls.");
                            if (var->c_pesp.esp) {
                                widget->section("Appearance");
                                widget->checkbox_with_color("Box ESP", &var->c_pesp.box, var->c_pesp.box_color, false);
                                widget->separator();
                                widget->checkbox_with_color("Name ESP", &var->c_pesp.name, var->c_pesp.name_color, false);
                                widget->separator();
                                widget->checkbox("Health bar", &var->c_pesp.health);
                                widget->separator();
                                widget->checkbox("Distance", &var->c_pesp.distance);
                                widget->section("Extras");
                                widget->checkbox_with_color("Skeleton", &var->c_pesp.skeleton, var->c_pesp.skel_color, true);
                                widget->separator();
                                widget->checkbox_with_color("Weapon", &var->c_pesp.weapon, var->c_pesp.weapon_color, true);
                                widget->separator();
                                widget->checkbox_with_color("Snapline", &var->c_pesp.snapline, var->c_pesp.snap_color, true);
                                widget->separator();
                                widget->checkbox_with_color("Fill", &var->c_pesp.fill, var->c_pesp.fill_color, true);
                                widget->separator();
                                widget->slider_float("Fill alpha", &var->c_pesp.fill_alpha, 0.f, 1.f, 0.05f, "%.2f");
                                widget->separator();
                                widget->slider_float("Max distance", &var->c_pesp.max_dist, 50.f, 2000.f, 50.f, "%.0fm");
                                widget->separator();
                                widget->slider_float("Thickness", &var->c_pesp.thickness, 0.5f, 5.f, 0.5f, "%.1f");
                            }
                        }
                        gui->end_child();

                        gui->begin_child("crosshair");
                        {
                            widget->checkbox_with_color("Enable crosshair", &var->c_crosshair.enable, var->c_crosshair.color, false);
                            if (var->c_crosshair.enable) {
                                widget->separator();
                                widget->dropdown("Type", &var->c_crosshair.type_selection, var->c_crosshair.type_list, var->c_crosshair.type_list.size());
                                widget->separator();
                                widget->slider_float("Size", &var->c_crosshair.size, 1.f, 30.f, 0.5f, "%.1f");
                                widget->separator();
                                widget->slider_float("Thickness", &var->c_crosshair.thick, 0.5f, 5.f, 0.5f, "%.1f");
                                widget->separator();
                                widget->checkbox("Spin", &var->c_crosshair.spin);
                                if (var->c_crosshair.spin) {
                                    widget->separator();
                                    widget->slider_float("Spin speed", &var->c_crosshair.spin_speed, 0.1f, 10.f, 0.1f, "%.1f");
                                }
                            }
                        }
                        gui->end_child();

                        gui->begin_child("self / visual");
                        {
                            widget->section("Visual Mods");
                            widget->checkbox("Custom FOV", &var->c_self.custom_fov);
                            if (var->c_self.custom_fov) {
                                widget->separator();
                                widget->slider_float("FOV degrees", &var->c_self.fov_deg, 60.f, 150.f, 1.f, "%.0f");
                            }
                            widget->section("Effects");
                            widget->checkbox("No flash", &var->c_self.no_flash);
                            widget->separator();
                            widget->checkbox("No pain overlay", &var->c_self.no_pain);
                            widget->separator();
                            widget->checkbox("No hallucination", &var->c_self.no_hallucination);
                            widget->separator();
                            widget->checkbox("No grayscale", &var->c_self.no_grayscale);
                            widget->separator();
                            widget->checkbox("No fog", &var->c_self.no_fog);
                            widget->separator();
                            widget->dropdown("Night vision", &var->c_self.night_vision, var->c_self.nv_list, var->c_self.nv_list.size());
                            widget->separator();
                            widget->checkbox_with_color("Footstep ESP", &var->c_self.footsteps, var->c_self.footstep_color, false);
                            if (var->c_self.footsteps) {
                                widget->dropdown("Shape", &var->c_self.footstep_shape, var->c_self.footstep_shape_list, 2);
                                widget->checkbox("Spin", &var->c_self.footstep_spin);
                                if (var->c_self.footstep_spin)
                                    widget->slider_float("Spin speed", &var->c_self.footstep_spin_spd, 10.f, 360.f);
                                widget->slider_float("Lifetime", &var->c_self.footstep_lifetime, 0.5f, 5.f);
                                widget->slider_float("Size", &var->c_self.footstep_size, 0.5f, 5.f);
                            }
                            widget->separator();
                            widget->checkbox("Damage numbers", &var->c_self.damage_numbers);
                            if (var->c_self.damage_numbers) {
                                widget->slider_float("Lifetime", &var->c_self.dmg_lifetime, 0.5f, 5.f, 0.5f, "%.1fs");
                                widget->slider_int("Font size", &var->c_self.dmg_font_size, 8, 24, 1, "%d");
                                static bool dc_clr = true;
                                widget->checkbox_with_color("Custom color", &dc_clr, var->c_self.dmg_color, false);
                            }
                            widget->section("Chams");
                            widget->checkbox("Player Chams", &var->c_self.player_chams);
                            widget->checkbox("Zombie Chams", &var->c_self.zombie_chams);
                            if (var->c_self.player_chams || var->c_self.zombie_chams) {
                                widget->dropdown("Pattern", &var->c_self.chams_pattern, var->c_self.chams_pattern_list, var->c_self.chams_pattern_list.size());
                                static bool pv_vis = true, pv_nonvis = true;
                                widget->checkbox_with_color("Visible", &pv_vis, var->c_self.chams_vis_color, false);
                                widget->checkbox_with_color("Hidden", &pv_nonvis, var->c_self.chams_nonvis_color, false);
                                if (var->c_self.chams_pattern == 5) {
                                    static bool pw_vis = true, pw_nonvis = true;
                                    widget->checkbox_with_color("Wire", &pw_vis, var->c_self.chams_wire_color, false);
                                    widget->checkbox_with_color("Wire H", &pw_nonvis, var->c_self.chams_wire_behind_color, false);
                                }
                            }
                            widget->checkbox("Self Chams", &var->c_self.self_chams);
                            if (var->c_self.self_chams) {
                                static bool sc_clr = true;
                                widget->checkbox_with_color("Self color", &sc_clr, var->c_self.self_chams_color, false);
                            }
                            widget->checkbox("Outline", &var->c_self.outline);
                            if (var->c_self.outline) {
                                static bool ol_clr = true;
                                widget->checkbox_with_color("Outline", &ol_clr, var->c_self.outline_color, false);
                            }
                        }
                        gui->end_child();
                    }
                    gui->end_group();

                    gui->sameline();

                    gui->begin_group();
                    {
                        gui->begin_child("zombie esp");
                        {
                            widget->section("Zombie ESP");
                            widget->checkbox("Enable", &var->c_zesp.esp);
                            if (var->c_zesp.esp) {
                                widget->section("Appearance");
                                widget->checkbox_with_color("Box", &var->c_zesp.box, var->c_zesp.box_color, false);
                                widget->separator();
                                widget->checkbox_with_color("Name", &var->c_zesp.name, var->c_zesp.name_color, false);
                                widget->separator();
                                widget->checkbox("Health", &var->c_zesp.health);
                                widget->separator();
                                widget->checkbox("Distance", &var->c_zesp.distance);
                                widget->section("Extras");
                                widget->checkbox_with_color("Skeleton", &var->c_zesp.skeleton, var->c_zesp.skel_color, true);
                                widget->separator();
                                widget->checkbox_with_color("Snapline", &var->c_zesp.snapline, var->c_zesp.snap_color, true);
                                widget->separator();
                                widget->checkbox_with_color("Fill", &var->c_zesp.fill, var->c_zesp.fill_color, true);
                                widget->separator();
                                widget->slider_float("Fill alpha", &var->c_zesp.fill_alpha, 0.f, 1.f, 0.05f, "%.2f");
                                widget->separator();
                                widget->slider_float("Max distance", &var->c_zesp.max_dist, 50.f, 1000.f, 50.f, "%.0fm");
                                widget->separator();
                                widget->slider_float("Thickness", &var->c_zesp.thickness, 0.5f, 5.f, 0.5f, "%.1f");
                            }
                        }
                        gui->end_child();

                        gui->begin_child("item esp");
                        {
                            widget->section("Item ESP");
                            widget->checkbox("Enable", &var->c_iesp.esp);
                            if (var->c_iesp.esp) {
                                widget->separator();
                                widget->checkbox_with_color("Snapline", &var->c_iesp.snapline, var->c_iesp.snap_color, true);
                                widget->separator();
                                widget->slider_float("Max distance", &var->c_iesp.max_dist, 10.f, 500.f, 10.f, "%.0fm");
                                widget->separator();
                                widget->checkbox("Clump nearby", &var->c_iesp.clump);
                                widget->section("Filters");
                                widget->checkbox("Weapons", &var->c_iesp.filter_weapons);
                                widget->separator();
                                widget->checkbox("Ammo", &var->c_iesp.filter_ammo);
                                widget->separator();
                                widget->checkbox("Medical", &var->c_iesp.filter_medical);
                                widget->separator();
                                widget->checkbox("Food", &var->c_iesp.filter_food);
                                widget->separator();
                                widget->checkbox("Clothing", &var->c_iesp.filter_clothing);
                            }
                        }
                        gui->end_child();

                        gui->begin_child("vehicle esp");
                        {
                            widget->section("Vehicle ESP");
                            widget->checkbox("Enable", &var->c_vesp.esp);
                            if (var->c_vesp.esp) {
                                widget->separator();
                                widget->checkbox_with_color("Box", &var->c_vesp.box, var->c_vesp.box_color, false);
                                widget->separator();
                                widget->checkbox_with_color("Name", &var->c_vesp.name, var->c_vesp.name_color, false);
                                widget->separator();
                                widget->checkbox("Health", &var->c_vesp.health);
                                widget->separator();
                                widget->checkbox("Distance", &var->c_vesp.distance);
                                widget->separator();
                                widget->checkbox("Locked", &var->c_vesp.locked);
                                widget->separator();
                                widget->checkbox_with_color("Snapline", &var->c_vesp.snapline, var->c_vesp.snap_color, true);
                                widget->separator();
                                widget->checkbox_with_color("Fill", &var->c_vesp.fill, var->c_vesp.fill_color, true);
                                widget->separator();
                                widget->slider_float("Fill alpha", &var->c_vesp.fill_alpha, 0.f, 1.f, 0.05f, "%.2f");
                                widget->separator();
                                widget->slider_float("Max distance", &var->c_vesp.max_dist, 50.f, 2000.f, 50.f, "%.0fm");
                                widget->separator();
                                widget->slider_float("Thickness", &var->c_vesp.thickness, 0.5f, 5.f, 0.5f, "%.1f");
                            }
                        }
                        gui->end_child();

                        gui->begin_child("animal esp");
                        {
                            widget->section("Animal ESP");
                            widget->checkbox("Enable", &var->c_aesp.esp);
                            if (var->c_aesp.esp) {
                                widget->separator();
                                widget->checkbox_with_color("Box", &var->c_aesp.box, var->c_aesp.box_color, false);
                                widget->separator();
                                widget->checkbox_with_color("Name", &var->c_aesp.name, var->c_aesp.name_color, false);
                                widget->separator();
                                widget->checkbox("Distance", &var->c_aesp.distance);
                                widget->separator();
                                widget->checkbox_with_color("Snapline", &var->c_aesp.snapline, var->c_aesp.snap_color, true);
                                widget->separator();
                                widget->checkbox_with_color("Fill", &var->c_aesp.fill, var->c_aesp.fill_color, true);
                                widget->separator();
                                widget->slider_float("Fill alpha", &var->c_aesp.fill_alpha, 0.f, 1.f, 0.05f, "%.2f");
                                widget->separator();
                                widget->slider_float("Max distance", &var->c_aesp.max_dist, 50.f, 1000.f, 50.f, "%.0fm");
                                widget->separator();
                                widget->slider_float("Thickness", &var->c_aesp.thickness, 0.5f, 5.f, 0.5f, "%.1f");
                            }
                        }
                        gui->end_child();

                        gui->begin_child("barricade esp");
                        {
                            widget->section("Barricades");
                            widget->checkbox_with_color("Storage", &var->c_sesp.esp, var->c_sesp.box_color, false);
                            widget->separator();
                            widget->checkbox_with_color("Bed", &var->c_besp.esp, var->c_besp.box_color, false);
                            widget->separator();
                            widget->checkbox("Claimed only", &var->c_besp.claimed_only);
                            widget->separator();
                            widget->checkbox("Show claimed", &var->c_besp.show_claimed);
                            widget->separator();
                            widget->checkbox_with_color("Generator", &var->c_gesp.esp, var->c_gesp.box_color, false);
                            widget->separator();
                            widget->checkbox("Fuel", &var->c_gesp.fuel);
                            widget->separator();
                            widget->checkbox_with_color("Turret", &var->c_tesp.esp, var->c_tesp.box_color, false);
                            widget->separator();
                            widget->checkbox("State", &var->c_tesp.state);
                        }
                        gui->end_child();

                        gui->begin_child("projectile esp");
                        {
                            widget->section("Projectiles");
                            widget->checkbox_with_color("Airdrop", &var->c_desp.esp, var->c_desp.box_color, false);
                            widget->separator();
                            widget->checkbox_with_color("Grenade", &var->c_resp.esp, var->c_resp.box_color, false);
                            widget->separator();
                            widget->checkbox("Radius", &var->c_resp.radius);
                            widget->separator();
                            widget->checkbox_with_color("Bullet tracer", &var->c_fesp.esp, var->c_fesp.self_color, false);
                            widget->separator();
                            widget->checkbox("Self", &var->c_fesp.self);
                            widget->separator();
                            widget->checkbox("Others", &var->c_fesp.others);
                            widget->separator();
                            widget->checkbox("Trail", &var->c_fesp.trail);
                            widget->separator();
                            widget->slider_float("Lifetime", &var->c_fesp.lifetime, 0.5f, 10.f, 0.5f, "%.1fs");
                        }
                        gui->end_child();

                        gui->begin_child("global esp");
                        {
                            widget->section("Global Settings");
                            widget->checkbox("Corner box mode", &var->c_esp.corner_mode);
                            widget->separator();
                            widget->checkbox("3D box", &var->c_esp.box3d);
                        }
                        gui->end_child();
                    }
                    gui->end_group();
                }

                // =============================================================
                // TAB 2 — Players (with player search bar)
                // =============================================================
                if (var->c_selection.selection_active == 2)
                {
                    // Player search bar using the framework's native text_field
                    static char player_search_buf[128] = {0};
                    widget->text_field("Search players...", "L", player_search_buf, sizeof(player_search_buf), { GetContentRegionAvail().x, SCALE(30) });
                    widget->separator();

                    gui->begin_group();
                    {
                        gui->begin_child("player list");
                        {
                            gui->push_font(set->c_font.inter_medium[0]);
                            draw->render_text(GetWindowDrawList(), set->c_font.inter_medium[0],
                                GetCursorScreenPos(), GetCursorScreenPos() + GetContentRegionAvail(),
                                gui->get_clr(clr->c_other_clr.white_clr),
                                "Connected players shown in overlay", 0, 0, { 0.5, 0.3 });
                            gui->pop_font();
                        }
                        gui->end_child();
                    }
                    gui->end_group();

                    gui->sameline();

                    gui->begin_group();
                    {
                        gui->begin_child("relations");
                        {
                            gui->push_font(set->c_font.inter_medium[0]);
                            draw->render_text(GetWindowDrawList(), set->c_font.inter_medium[0],
                                GetCursorScreenPos(), GetCursorScreenPos() + GetContentRegionAvail(),
                                gui->get_clr(clr->c_other_clr.white_clr),
                                "Friend/enemy management in-game", 0, 0, { 0.5, 0.3 });
                            gui->pop_font();
                        }
                        gui->end_child();
                    }
                    gui->end_group();
                }

                // =============================================================
                // TAB 3 — Misc
                // =============================================================
                if (var->c_selection.selection_active == 3)
                {
                    gui->begin_group();
                    {
                        gui->begin_child("world");
                        {
                            widget->section("World");
                            widget->checkbox("Custom time", &var->c_world.custom_time);
                            if (var->c_world.custom_time) {
                                widget->separator();
                                widget->slider_float("Time", &var->c_world.time_value, 0.f, 24.f, 0.5f, "%.1fh");
                            }
                            widget->separator();
                            widget->checkbox_with_color("Override sky", &var->c_world.override_sky, var->c_world.sky_color, false);
                            widget->separator();
                            widget->checkbox_with_color("Override sun", &var->c_world.override_sun, var->c_world.sun_color, false);
                            widget->separator();
                            widget->checkbox_with_color("Override clouds", &var->c_world.override_cloud, var->c_world.cloud_color, false);
                            widget->separator();
                            widget->checkbox_with_color("Override cloud rim", &var->c_world.override_cloud_rim, var->c_world.cloud_rim_color, false);
                            widget->separator();
                            widget->checkbox("Force compass", &var->c_world.force_compass);
                            widget->separator();
                            widget->checkbox("Force map", &var->c_world.force_map);
                            widget->separator();
                            widget->checkbox("Show players on map", &var->c_world.map_show_players);
                            widget->separator();
                            widget->checkbox("Show markers on map", &var->c_world.map_show_markers);
                        }
                        gui->end_child();

                        gui->begin_child("placement");
                        {
                            widget->section("Placement");
                            widget->checkbox("Place anywhere", &var->c_placement.place_anywhere);
                            widget->separator();
                            widget->checkbox("Ignore barricade", &var->c_placement.ignore_barricade);
                            widget->separator();
                            widget->checkbox("Ignore structure", &var->c_placement.ignore_structure);
                            widget->separator();
                            widget->checkbox("Custom offset", &var->c_placement.custom_offset);
                            if (var->c_placement.custom_offset) {
                                widget->separator();
                                widget->slider_float("Offset X", &var->c_placement.offset_x, -10.f, 10.f, 0.1f, "%.1f");
                                widget->separator();
                                widget->slider_float("Offset Y", &var->c_placement.offset_y, -10.f, 10.f, 0.1f, "%.1f");
                                widget->separator();
                                widget->slider_float("Offset Z", &var->c_placement.offset_z, -10.f, 10.f, 0.1f, "%.1f");
                            }
                            widget->separator();
                            widget->slider_float("Salvage multiplier", &var->c_placement.salvage_multiplier, 0.5f, 10.f, 0.5f, "%.1fx");
                        }
                        gui->end_child();
                    }
                    gui->end_group();

                    gui->sameline();

                    gui->begin_group();
                    {
                        gui->begin_child("movement");
                        {
                            widget->section("Movement");
                            widget->checkbox("Far reach", &var->c_movement.far_reach);
                            if (var->c_movement.far_reach) {
                                widget->separator();
                                widget->slider_float("Reach distance", &var->c_movement.reach_dist, 1.f, 50.f, 1.f, "%.0fm");
                            }
                            widget->separator();
                            widget->checkbox("Pickup through walls", &var->c_movement.pickup_walls);
                            if (var->c_movement.pickup_walls) {
                                widget->separator();
                                widget->slider_float("Pickup distance", &var->c_movement.pickup_dist, 1.f, 50.f, 1.f, "%.0fm");
                            }
                            widget->separator();
                            widget->checkbox("Extend nearby", &var->c_movement.extend_nearby);
                            if (var->c_movement.extend_nearby) {
                                widget->separator();
                                widget->slider_float("Nearby radius", &var->c_movement.nearby_radius, 5.f, 100.f, 5.f, "%.0fm");
                                widget->separator();
                                widget->checkbox("Nearby through walls", &var->c_movement.nearby_through_walls);
                            }
                            widget->separator();
                            widget->checkbox_with_key("Vehicle fly", &var->c_movement.vehicle_fly, &var->c_movement.vehicle_fly_key, &var->c_movement.vehicle_fly_holding, &var->c_movement.vehicle_fly_value, &var->c_movement.vehicle_fly_show_binds);
                            widget->set_tooltip("Vehicle Fly", "Fly vehicles freely in any direction.");
                            if (var->c_movement.vehicle_fly) {
                                widget->separator();
                                widget->slider_float("Fly speed", &var->c_movement.fly_speed, 1.f, 100.f, 1.f, "%.0f");
                                widget->separator();
                                widget->dropdown("Fly style", &var->c_movement.vehicle_fly_style, var->c_movement.vehicle_fly_style_list, var->c_movement.vehicle_fly_style_list.size());
                            }
                            widget->separator();
                        }
                        gui->end_child();

                        gui->begin_child("fun");
                        {
                            widget->checkbox_with_key("Freecam", &var->c_fun.freecam, &var->c_fun.freecam_key, &var->c_fun.freecam_holding, &var->c_fun.freecam_value, &var->c_fun.freecam_show_binds);
                            widget->set_tooltip("Freecam", "Detach camera from player for free movement.");
                            if (var->c_fun.freecam) {
                                widget->separator();
                                widget->slider_float("Freecam speed", &var->c_fun.freecam_speed, 1.f, 50.f, 1.f, "%.0f");
                            }
                            widget->separator();
                            widget->checkbox("Star of David", &var->c_fun.star_of_david);
                            widget->set_tooltip("Star of David", "Renders a Star of David overlay on screen.");
                        }
                        gui->end_child();

                        gui->begin_child("tools");
                        {
                            widget->section("Tools");
                            widget->checkbox("Item spawner", &var->c_misc.item_spawner);
                            widget->separator();
                            widget->checkbox("Entity inspector", &var->c_misc.entity_inspector);
                            widget->separator();
                            widget->checkbox("Storage viewer", &var->c_misc.storage_viewer);
                            widget->separator();
                            widget->checkbox("HWID changer", &var->c_misc.hwid_changer);
                            widget->separator();
                            widget->checkbox("Unlock perspective", &var->c_self.unlock_perspective);
                        }
                        gui->end_child();

                        gui->begin_child("quests");
                        {
                            widget->section("Quest Exploits");
                            widget->checkbox("Max ML", &var->c_quest.max_ml);
                            widget->separator();
                            widget->checkbox("Alcohol", &var->c_quest.alcohol);
                            widget->separator();
                            widget->checkbox("Lumberjack", &var->c_quest.lumberjack);
                            widget->separator();
                            widget->checkbox("Voucher", &var->c_quest.voucher);
                            widget->separator();
                            widget->checkbox("Xmas 2024", &var->c_quest.xmas2024);
                            widget->separator();
                            widget->checkbox("Xmas 2025 Factory", &var->c_quest.xmas2025_factory);
                            widget->separator();
                            widget->checkbox("Xmas 2025 Penguins", &var->c_quest.xmas2025_penguins);
                        }
                        gui->end_child();

                        gui->begin_child("player colors");
                        {
                            widget->section("Player Colors");
                            static bool friend_clr = true;
                            widget->checkbox_with_color("Friend color", &friend_clr, var->c_misc.friend_color, false);
                            widget->separator();
                            static bool enemy_clr = true;
                            widget->checkbox_with_color("Enemy color", &enemy_clr, var->c_misc.enemy_color, false);
                        }
                        gui->end_child();
                    }
                    gui->end_group();
                }

                // =============================================================
                // TAB 4 — Automation
                // =============================================================
                if (var->c_selection.selection_active == 4)
                {
                    gui->begin_group();
                    {
                        gui->begin_child("auto pickup");
                        {
                            widget->checkbox("Auto pickup", &var->c_automation.auto_pickup);
                            widget->set_tooltip("Auto Pickup", "Automatically picks up nearby items.");
                            if (var->c_automation.auto_pickup) {
                                widget->separator();
                                widget->slider_float("Radius", &var->c_automation.pickup_radius, 1.f, 50.f, 1.f, "%.0fm");
                                widget->separator();
                                widget->checkbox("Through walls", &var->c_automation.pickup_through_walls);
                                widget->separator();
                                widget->checkbox("Item filter", &var->c_automation.pickup_filter);
                                widget->separator();
                                widget->slider_float("Pickup speed", &var->c_automation.auto_pickup_speed, 0.01f, 1.f, 0.01f, "%.2fs");
                                widget->separator();
                                widget->checkbox("Skip empty", &var->c_automation.auto_pickup_skip_empty);
                            }
                        }
                        gui->end_child();

                        gui->begin_child("auto fish");
                        {
                            widget->checkbox("Auto fish", &var->c_automation.auto_fish);
                            if (var->c_automation.auto_fish) {
                                widget->separator();
                                widget->slider_float("Reel delay", &var->c_automation.auto_fish_delay, 0.05f, 2.f, 0.05f, "%.2fs");
                            }
                        }
                        gui->end_child();

                        gui->begin_child("auto forge");
                        {
                            widget->checkbox("Auto forge", &var->c_automation.auto_forge);
                            if (var->c_automation.auto_forge) {
                                widget->separator();
                                widget->slider_float("Radius", &var->c_automation.auto_forge_radius, 5.f, 100.f, 5.f, "%.0fm");
                                widget->separator();
                                widget->slider_float("Delay", &var->c_automation.auto_forge_delay, 0.05f, 2.f, 0.05f, "%.2fs");
                            }
                        }
                        gui->end_child();
                    }
                    gui->end_group();

                    gui->sameline();

                    gui->begin_group();
                    {
                        gui->begin_child("auto joiner");
                        {
                            widget->checkbox("Auto joiner", &var->c_automation.auto_joiner);
                            widget->set_tooltip("Auto Joiner", "Automatically reconnects to a server.");
                            if (var->c_automation.auto_joiner) {
                                widget->separator();
                                widget->text_field("Server IP", "M", var->c_automation.server_ip, sizeof(var->c_automation.server_ip), { GetContentRegionAvail().x, SCALE(30) });
                                widget->separator();
                                widget->slider_int("Port", &var->c_automation.server_port, 1, 65535, 1, "%d");
                                widget->separator();
                                widget->checkbox("Player limit", &var->c_automation.auto_join_limit);
                                if (var->c_automation.auto_join_limit) {
                                    widget->separator();
                                    widget->slider_int("Max players", &var->c_automation.auto_join_max, 1, 100, 1, "%d");
                                }
                                widget->separator();
                                widget->slider_float("Retry delay", &var->c_automation.auto_join_retry, 1.f, 60.f, 1.f, "%.0fs");
                            }
                        }
                        gui->end_child();

                        gui->begin_child("auto farm");
                        {
                            widget->checkbox("Auto farm", &var->c_autofarm.auto_farm_on);
                            if (var->c_autofarm.auto_farm_on) {
                                widget->separator();
                                widget->slider_float("Harvest radius", &var->c_autofarm.auto_farm_radius, 5.f, 100.f, 5.f, "%.0fm");
                                widget->separator();
                                widget->slider_float("Action delay", &var->c_autofarm.auto_farm_delay, 0.1f, 5.f, 0.1f, "%.1fs");
                                widget->separator();
                                widget->checkbox("Auto replant", &var->c_autofarm.auto_farm_replant);
                                widget->separator();
                                widget->checkbox("Auto store", &var->c_autofarm.auto_farm_store);
                                widget->separator();
                                widget->checkbox("Auto craft", &var->c_autofarm.auto_farm_craft);
                                widget->separator();
                                widget->checkbox("Auto equip seed", &var->c_autofarm.auto_farm_equip_seed);
                                widget->separator();
                                widget->checkbox("Auto water", &var->c_autofarm.auto_farm_water);
                            }
                        }
                        gui->end_child();
                    }
                    gui->end_group();
                }

                // =============================================================
                // TAB 5 — Config
                // =============================================================
                if (var->c_selection.selection_active == 5)
                {
#ifdef BUILD_DLL
                    if (config_mgr::g_needs_refresh) {
                        config_mgr::refresh_list();
                        config_mgr::g_needs_refresh = false;
                    }
                    widget->text_field("Config name", "M", var->c_config.name, sizeof(var->c_config.name), { GetContentRegionAvail().x - SCALE(100), SCALE(30) });
                    gui->sameline();
                    if (widget->button("Create", SCALE(90, 30)))
                    {
                        if (var->c_config.name[0] != '\0')
                        {
                            char local_name[128];
                            lstrcpynA(local_name, var->c_config.name, 128);
                            memset(var->c_config.name, 0, sizeof(var->c_config.name));
                            config_mgr::save(local_name);
                            config_mgr::g_needs_refresh = true;
                        }
                    }
                    widget->separator();
                    gui->begin_child("configs", { 0, GetContentRegionAvail().y - SCALE(80) });
                    {
                        for (int i = 0; i < (int)var->c_config.data.size(); i++)
                        {
                            bool is_auto = (lstrcmpA(config_mgr::g_autoload_name, var->c_config.data[i].name.c_str()) == 0);
                            ImVec2 card_pos = ImGui::GetCursorScreenPos();
                            widget->config_selectable(&var->c_config.data[i], i, var->c_config.active);
                            if (is_auto) {
                                ImFont* name_font = set->c_font.inter_medium[1];
                                float name_w = name_font->CalcTextSizeA(name_font->FontSize, FLT_MAX, 0.0f, var->c_config.data[i].name.c_str()).x;
                                float dot_x = card_pos.x + SCALE(10) + name_w + SCALE(10);
                                float dot_y = card_pos.y + SCALE(19);
                                float radius = SCALE(5);
                                ImGui::GetWindowDrawList()->AddCircleFilled({dot_x, dot_y}, radius, gui->get_clr(clr->c_other_clr.accent_clr), 24);
                            }
                        }
                    }
                    gui->end_child();
                    widget->separator();
                    {
                        float bw = (GetContentRegionAvail().x - SCALE(8)) / 3.f;
                        bool valid = var->c_config.active >= 0 && var->c_config.active < (int)var->c_config.data.size();
                        if (widget->button("Save", { bw, SCALE(30) }) && valid)
                        {
                            char sname[260];
                            lstrcpynA(sname, var->c_config.data[var->c_config.active].name.c_str(), 260);
                            config_mgr::save(sname);
                            config_mgr::g_needs_refresh = true;
                        }
                        gui->sameline();
                        if (widget->button("Load", { bw, SCALE(30) }) && valid)
                        {
                            char lname[260];
                            lstrcpynA(lname, var->c_config.data[var->c_config.active].name.c_str(), 260);
                            config_mgr::load(lname);
                        }
                        gui->sameline();
                        if (widget->button("Auto", { bw, SCALE(30) }) && valid)
                        {
                            char aname[260];
                            lstrcpynA(aname, var->c_config.data[var->c_config.active].name.c_str(), 260);
                            if (lstrcmpA(config_mgr::g_autoload_name, aname) == 0)
                                config_mgr::clear_autoload();
                            else
                                config_mgr::set_autoload(aname);
                        }
                    }
                    gui->push_font(set->c_font.inter_medium[0]);
                    draw->render_text(GetWindowDrawList(), set->c_font.inter_medium[0],
                        GetCursorScreenPos(), GetCursorScreenPos() + ImVec2(GetContentRegionAvail().x, SCALE(20)),
                        gui->get_clr(clr->c_text.text), config_mgr::g_status, 0, 0, { 0.5f, 0.5f });
                    gui->pop_font();
#else
                    gui->push_font(set->c_font.inter_medium[0]);
                    draw->render_text(GetWindowDrawList(), set->c_font.inter_medium[0],
                        GetCursorScreenPos(), GetCursorScreenPos() + GetContentRegionAvail(),
                        gui->get_clr(clr->c_text.text), "Configs available in DLL mode", 0, 0, { 0.5, 0.3 });
                    gui->pop_font();
#endif
                }

                // =============================================================
                // TAB 6 — Settings
                // =============================================================
                if (var->c_selection.selection_active == 6)
                {
                    gui->begin_group();
                    {
                        gui->begin_child("appearance");
                        {
                            widget->section("Appearance");
                            widget->slider_float("DPI Scale", &var->c_dpi.dpi, 0.5f, 2.0f, 0.05f, "");
                            if (var->c_dpi.dpi != var->c_dpi.dpi_saved)
                            {
                                var->c_dpi.dpi_changed = true;
                                var->c_dpi.dpi_saved = var->c_dpi.dpi;
                            }
                            widget->separator();
                            widget->checkbox_with_color("Accent color", &var->c_appearance.accent_enabled, var->c_appearance.accent_color, false);
                            widget->separator();
                            widget->checkbox("Watermark", &var->c_appearance.watermark);
                            widget->separator();
                            widget->checkbox("Info bar", &var->c_appearance.info_bar);
                            widget->slider_int("Font size", &var->c_appearance.font_size, 10, 24, 1, "%dpx");
                        }
                        gui->end_child();
                    }
                    gui->end_group();

                    gui->sameline();

                    gui->begin_group();
                    {
                        gui->begin_child("anti-spy");
                        {
                            widget->section("Anti-Spy");
                            widget->dropdown("Spy mode", &var->c_antispy.spy_mode, var->c_antispy.spy_list, var->c_antispy.spy_list.size());
                            widget->set_tooltip("Anti-Spy", "Hides the menu from screenshot capture tools.");
                            widget->separator();
                            gui->push_font(set->c_font.inter_medium[0]);
                            draw->render_text(GetWindowDrawList(), set->c_font.inter_medium[0],
                                GetCursorScreenPos(), GetCursorScreenPos() + ImVec2(GetContentRegionAvail().x, SCALE(40)),
                                gui->get_clr(clr->c_text.text),
                                "Protects against spy screenshots and screen capture", 0, 0, { 0.5, 0.5 });
                            gui->pop_font();
                            gui->set_cursor_pos_y(GetCursorPos().y + SCALE(40));
                        }
                        gui->end_child();

                        gui->begin_child("keybinds");
                        {
                            widget->section("Keybinds");
                            widget->checkbox_with_key("Aimbot", &var->c_aimbot.aimbot, &var->c_keybinds.aimbot_key, &var->c_aimbot.aimbot_holding, &var->c_aimbot.aimbot_value, &var->c_aimbot.aimbot_show_binds);
                            widget->separator();
                            widget->checkbox_with_key("Silent aim", &var->c_silent.silent, &var->c_keybinds.silent_key, &var->c_silent.silent_holding, &var->c_silent.silent_value, &var->c_silent.silent_show_binds);
                            widget->separator();
                            widget->checkbox_with_key("Triggerbot", &var->c_trigger.enable_trigger, &var->c_keybinds.trigger_key, &var->c_trigger.enable_trigger_holding, &var->c_trigger.enable_trigger_value, &var->c_trigger.enable_trigger_show_binds);
                            widget->separator();
                            widget->checkbox_with_key("Freecam", &var->c_fun.freecam, &var->c_keybinds.freecam_key, &var->c_fun.freecam_holding, &var->c_fun.freecam_value, &var->c_fun.freecam_show_binds);
                            widget->separator();
                            widget->checkbox_with_key("Vehicle fly", &var->c_movement.vehicle_fly, &var->c_keybinds.vfly_key, &var->c_movement.vehicle_fly_holding, &var->c_movement.vehicle_fly_value, &var->c_movement.vehicle_fly_show_binds);
                            widget->separator();
                            widget->checkbox_with_key("No vehicle dmg", &var->c_movement.no_vehicle_dmg, &var->c_keybinds.vdmg_key, &var->c_movement.no_vehicle_dmg_holding, &var->c_movement.no_vehicle_dmg_value, &var->c_movement.no_vehicle_dmg_show_binds);
                        }
                        gui->end_child();
                    }
                    gui->end_group();
                }
            }
            gui->end_content();

            gui->pop_style_var(2);
        }

        // Window drag — from sidebar, tab bar, or any area where no widget is active
        {
            static bool was_dragging = false;
            ImVec2 mp = ImGui::GetIO().MousePos;
            ImVec2 wp = ImGui::GetWindowPos();
            ImVec2 ws = ImGui::GetWindowSize();
            bool in_window = mp.x >= wp.x && mp.x <= wp.x + ws.x && mp.y >= wp.y && mp.y <= wp.y + ws.y;
            bool in_sidebar = mp.x >= wp.x && mp.x <= wp.x + SCALE(110);
            bool in_tabbar = mp.y >= wp.y && mp.y <= wp.y + SCALE(58);
            bool can_drag = in_window && !ImGui::IsAnyItemActive() && (in_sidebar || in_tabbar || !ImGui::IsAnyItemHovered());
            // Only start a new drag from inside the window; stop immediately on mouse release
            if (ImGui::IsMouseDragging(0) && (can_drag || was_dragging)) {
                ImGui::SetWindowPos(wp + ImGui::GetIO().MouseDelta);
                was_dragging = true;
            } else {
                was_dragging = false;
            }
        }
        gui->end();

        // =================================================================
        // Watermark
        // =================================================================
#ifdef BUILD_DLL
        // Update watermark with real values
        {
            char fps_buf[16]; snprintf(fps_buf, sizeof(fps_buf), "%.0ffps", ImGui::GetIO().Framerate);
            SYSTEMTIME st; GetLocalTime(&st);
            char time_buf[16]; snprintf(time_buf, sizeof(time_buf), "%d:%02d%s", st.wHour > 12 ? st.wHour - 12 : (st.wHour ? st.wHour : 12), st.wMinute, st.wHour >= 12 ? "PM" : "AM");
            var->c_watermark.watermark_content = { "ISRAELI CLIENT", "Unturned", fps_buf, "0ms", time_buf };
        }
#endif
        gui->water_mark("watermark", var->c_watermark.watermark_content, static_cast<watermark_position>(var->c_watermark.watermark_position), &var->c_appearance.watermark);

#ifdef BUILD_DLL
    // Info bar — left side, always visible
    if (var->c_appearance.info_bar) {
        SYSTEMTIME st; GetLocalTime(&st);
        int fps = (int)GetIO().Framerate;
        int h12 = st.wHour > 12 ? st.wHour - 12 : (st.wHour ? st.wHour : 12);
        char ib[128];
        wsprintfA(ib, "%d FPS  |  %d:%02d%s", fps, h12, st.wMinute, st.wHour >= 12 ? "PM" : "AM");
        ImDrawList* fg = GetForegroundDrawList();
        fg->AddText(set->c_font.inter_medium[0], set->c_font.inter_medium[0]->FontSize, ImVec2(11.f, 11.f), IM_COL32(0,0,0,90), ib);
        fg->AddText(set->c_font.inter_medium[0], set->c_font.inter_medium[0]->FontSize, ImVec2(10.f, 10.f), IM_COL32(255,255,255,130), ib);
    }
#endif
#ifdef BUILD_DLL
    // Custom cursor matching theme (drawn on foreground, above everything)
    {
        ImDrawList* fg = GetForegroundDrawList();
        ImVec2 m = GetIO().MousePos;
        if (m.x >= 0 && m.y >= 0) {
            // Sleek arrow — accent blue fill, light outline
            ImVec2 a = m;                                          // tip
            ImVec2 b = ImVec2(m.x + 1.f,  m.y + 21.f);            // bottom-left
            ImVec2 c = ImVec2(m.x + 7.f,  m.y + 15.f);            // notch
            ImVec2 d = ImVec2(m.x + 15.f, m.y + 19.f);            // tail

            // Body (two triangles for the arrow shape)
            fg->AddTriangleFilled(a, b, c, gui->get_clr(clr->c_other_clr.accent_clr));
            fg->AddTriangleFilled(a, c, d, gui->get_clr(clr->c_other_clr.accent_clr));

            // Outline
            ImVec2 pts[] = { a, b, c, d };
            fg->AddPolyline(pts, 4, IM_COL32(200, 210, 230, 220), ImDrawFlags_Closed, 1.5f);

            // Bright dot at tip for precision
            fg->AddCircleFilled(ImVec2(m.x + 1.f, m.y + 1.f), 1.5f, IM_COL32(255, 255, 255, 240));
        }
    }
#endif

#ifdef BUILD_DLL
    // ESP renders on foreground even when menu is visible.
    // Guarded by the same VEH+setjmp installed in main.cpp.
    extern volatile bool g_InEspCode;
    extern jmp_buf g_EspJmpBuf;
    if (setjmp(g_EspJmpBuf) == 0) {
        g_InEspCode = true;
        ESP::Render();
        g_InEspCode = false;
    }
#endif
    gui->end_frame();
    }
}
