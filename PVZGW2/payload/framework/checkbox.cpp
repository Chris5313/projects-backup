#include "functions.h"

bool c_widget::checkbox(std::string_view label, bool* callback)
{
    struct c_checkbox
    {
        ImVec4 text_colored = clr->c_text.text;
    };

    ImGuiWindow* window = GetCurrentWindow();
    ImGuiContext& g = *GImGui;

    const ImGuiID id = window->GetID(label.data());
    const ImVec2 pos = window->DC.CursorPos;

    const float w = GetContentRegionAvail().x;
    const float h = SCALE(30);

    const ImRect clickable_rect(pos, pos + ImVec2(w, h));
    const ImRect rect(pos + ImVec2(w - SCALE(40), SCALE(4)), pos + ImVec2(w, h - SCALE(4)));

    ItemSize(clickable_rect, 0.f);
    if (!ItemAdd(clickable_rect, id)) return false;

    bool hovered, held, pressed = ButtonBehavior(clickable_rect, id, &hovered, &held);

    c_checkbox* state = gui->anim_container(&state, id);
    state->text_colored = ImLerp(state->text_colored, *callback ? clr->c_text.text_active : hovered ? clr->c_text.text_hov : clr->c_text.text, ImGui::GetIO().DeltaTime * 8.f);

    if (pressed)
    {
        *callback = !(*callback);
        MarkItemEdited(id);
        if (*callback) ui_sound::toggle_on(); else ui_sound::toggle_off();
    }

    // Switch renders at final target colors directly — animation state is
    // unreliable across DLL reloads (stale ImGui context garbage).
    ImVec4 bg_target = *callback ? clr->c_element.checkbox_active : clr->c_element.layout;
    ImVec4 circle_target = *callback ? clr->c_other_clr.accent_clr : clr->c_element.circle;
    float offset_target = *callback ? 28.f : 12.f;
    draw->add_rect_filled(window->DrawList, rect.Min, rect.Max, gui->get_clr(bg_target), SCALE(100.f));
    draw->add_circle_filled(window->DrawList, { rect.Min.x + SCALE(offset_target), rect.GetCenter().y }, SCALE(6.f), gui->get_clr(circle_target), SCALE(100.f));

    draw->render_text(window->DrawList, set->c_font.inter_medium[0], clickable_rect.Min, clickable_rect.Max, gui->get_clr(state->text_colored), label.data(), NULL, NULL, { 0.0, 0.5 });

    return pressed;
}

bool c_widget::checkbox_with_key(std::string_view label, bool* callback, int* key, bool* mode, bool* value, bool* show_in_binds)
{
    struct c_checkbox
    {
        ImVec4 text_colored = clr->c_text.text;
    };

    ImGuiWindow* window = GetCurrentWindow();
    ImGuiContext& g = *GImGui;

    const ImGuiID id = window->GetID(label.data());
    const ImVec2 pos = window->DC.CursorPos;

    const float w = GetContentRegionAvail().x;
    const float h = SCALE(30);

    const ImRect clickable_rect(pos, pos + ImVec2(w, h));
    const ImRect rect(pos + ImVec2(w - SCALE(40), SCALE(4)), pos + ImVec2(w, h - SCALE(4)));

    ItemSize(clickable_rect, 0.f);
    if (!ItemAdd(clickable_rect, id)) return false;

    bool hovered, held, pressed = ButtonBehavior(clickable_rect, id, &hovered, &held);

    c_checkbox* state = gui->anim_container(&state, id);
    state->text_colored = ImLerp(state->text_colored, *callback ? clr->c_text.text_active : hovered ? clr->c_text.text_hov : clr->c_text.text, ImGui::GetIO().DeltaTime * 8.f);

    char kb_name[64]; snprintf(kb_name, sizeof(kb_name), "%ukeybind", id);
    widget->begin_popup(kb_name, 170, {w - SCALE(92), SCALE(-40)});
    {
        widget->keybind("keybind", key);
        widget->separator();
        widget->keybind_button("mode", mode);
        widget->separator();
        widget->checkbox("Value", value);
        widget->separator();
        widget->checkbox("Show binds", show_in_binds);
    }
    widget->end_popup();

    if (pressed)
    {
        *callback = !(*callback);
        MarkItemEdited(id);
        if (*callback) ui_sound::toggle_on(); else ui_sound::toggle_off();
    }

    ImVec4 bg_target_k = *callback ? clr->c_element.checkbox_active : clr->c_element.layout;
    ImVec4 circle_target = *callback ? clr->c_other_clr.accent_clr : clr->c_element.circle;
    float offset_target_k = *callback ? 28.f : 12.f;
    draw->add_rect_filled(window->DrawList, rect.Min, rect.Max, gui->get_clr(bg_target_k), SCALE(100.f));
    draw->add_circle_filled(window->DrawList, { rect.Min.x + SCALE(offset_target_k), rect.GetCenter().y }, SCALE(6.f), gui->get_clr(circle_target), SCALE(100.f));

    draw->render_text(window->DrawList, set->c_font.icon[1], clickable_rect.Min, clickable_rect.Max - ImVec2(SCALE(60), 0), gui->get_clr(clr->c_element.popup_icon), "\xef\x84\x9c", NULL, NULL, {1.0, 0.5});
    draw->render_text(window->DrawList, set->c_font.inter_medium[0], clickable_rect.Min, clickable_rect.Max, gui->get_clr(state->text_colored), label.data(), NULL, NULL, { 0.0, 0.5 });

    return pressed;
}

bool c_widget::checkbox_with_color(std::string_view label, bool* callback, float col[4], bool alpha)
{
    struct c_checkbox
    {
        ImVec4 text_colored = clr->c_text.text;
    };

    ImGuiWindow* window = GetCurrentWindow();
    ImGuiContext& g = *GImGui;

    const ImGuiID id = window->GetID(label.data());
    const ImVec2 pos = window->DC.CursorPos;

    const float w = GetContentRegionAvail().x;
    const float h = SCALE(30);

    const ImRect clickable_rect(pos, pos + ImVec2(w, h));
    const ImRect rect(pos + ImVec2(w - SCALE(40), SCALE(4)), pos + ImVec2(w, h - SCALE(4)));

    ItemSize(clickable_rect, 0.f);
    if (!ItemAdd(clickable_rect, id)) return false;
    const ImVec2 stored_pos = GetCursorScreenPos();

    bool hovered, held, pressed = ButtonBehavior(clickable_rect, id, &hovered, &held);

    c_checkbox* state = gui->anim_container(&state, id);
    state->text_colored = ImLerp(state->text_colored, *callback ? clr->c_text.text_active : hovered ? clr->c_text.text_hov : clr->c_text.text, ImGui::GetIO().DeltaTime * 8.f);

    SetCursorScreenPos(clickable_rect.Max - SCALE(78, 24));
    char cp_name[64]; snprintf(cp_name, sizeof(cp_name), "##%ucolorpicker", id);
    widget->color_edit(cp_name, col, alpha ? ImGuiColorEditFlags_AlphaBar : 0);
    SetCursorScreenPos(stored_pos);

    if (pressed)
    {
        *callback = !(*callback);
        MarkItemEdited(id);
        if (*callback) ui_sound::toggle_on(); else ui_sound::toggle_off();
    }

    ImVec4 bg_target_c = *callback ? clr->c_element.checkbox_active : clr->c_element.layout;
    ImVec4 circle_target2 = *callback ? clr->c_other_clr.accent_clr : clr->c_element.circle;
    float offset_target_c = *callback ? 28.f : 12.f;
    draw->add_rect_filled(window->DrawList, rect.Min, rect.Max, gui->get_clr(bg_target_c), SCALE(100.f));
    draw->add_circle_filled(window->DrawList, { rect.Min.x + SCALE(offset_target_c), rect.GetCenter().y }, SCALE(6.f), gui->get_clr(circle_target2), SCALE(100.f));

    draw->render_text(window->DrawList, set->c_font.inter_medium[0], clickable_rect.Min, clickable_rect.Max, gui->get_clr(state->text_colored), label.data(), NULL, NULL, { 0.0, 0.5 });

    return pressed;
}
