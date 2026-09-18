#include "../settings/functions.h"
#include "../settings/config.h"

bool active_button(std::string_view label, int selection_id, int& selection_variable) {
    ImGuiWindow* window = GetCurrentWindow();

    ImGuiContext& g = *GImGui;
    const ImGuiStyle& style = g.Style;
    const ImGuiID id = window->GetID(label.data());

    const ImVec2 pos = window->DC.CursorPos;
    ImRect rect(pos, pos + SCALE(100, 40));

    ItemSize(rect, 0);
    if (!ItemAdd(rect, id)) return false;

    const bool selected = selection_id == selection_variable;
    bool hovered, held, pressed = ButtonBehavior(rect, id, &hovered, &held);
    if (pressed) { selection_variable = selection_id; ui_sound::click(); }

    float* state = gui->anim_container(&state, id);
    *state = ImClamp(*state + (gui->fixed_speed(8.f) * (!selected ? 1.f : -1.f)), 0.f, 1.f);

    draw->add_rect_filled(window->DrawList, rect.Min, rect.Max, gui->get_clr(clr->c_lua.stroke), SCALE(2.f));
    draw->render_text(window->DrawList, set->c_font.inter_medium[1], rect.Min, rect.Max, gui->get_clr(clr->c_text.text), "LOADED", NULL, NULL, ImVec2(0.5f, 0.5f));

    draw->add_rect_filled(window->DrawList, rect.Min, rect.Max, gui->get_clr(clr->c_other_clr.accent_clr, *state), SCALE(2.f));
    draw->render_text(window->DrawList, set->c_font.inter_medium[1], rect.Min, rect.Max, gui->get_clr(clr->c_text.text_active, *state), "LOAD", NULL, NULL, ImVec2(0.5f, 0.5f));

    return pressed;
}

bool c_widget::config_selectable(config_data* data, int selection_id, int& selection_variable)
{
    ImGuiWindow* window = GetCurrentWindow();
    if (window->SkipItems) return false;

    ImGuiContext& g = *GImGui;
    const ImGuiStyle& style = g.Style;
    char id_buf[64]; snprintf(id_buf, sizeof(id_buf), "%s%d", data->name.data(), selection_id);
    const ImGuiID id = window->GetID(id_buf);

    const bool selected = selection_id == selection_variable;

    const ImVec2 pos = window->DC.CursorPos;
    const float width = GetContentRegionAvail().x;
    ImRect rect(pos, pos + ImVec2(width, SCALE(60)));

    ItemSize(rect, 0);
    if (!ItemAdd(rect, id)) return false;

    draw->add_rect_filled(window->DrawList, rect.Min, rect.Max, gui->get_clr(clr->c_lua.background), SCALE(set->c_child.rounding));
    draw->add_rect(window->DrawList, rect.Min, rect.Max, gui->get_clr(clr->c_lua.stroke), SCALE(set->c_child.rounding), 0, SCALE(1.f));
    draw->render_text(window->DrawList, set->c_font.inter_medium[1], rect.Min + SCALE(10, 11), rect.Max, gui->get_clr(clr->c_text.text_active), data->name.data());
    draw->render_text(window->DrawList, set->c_font.inter_medium[0], rect.Min + SCALE(10, 33), rect.Max, gui->get_clr(clr->c_text.text_active), "Modified:");
    draw->render_text(window->DrawList, set->c_font.inter_medium[0], rect.Min + SCALE(69, 33), rect.Max, gui->get_clr(clr->c_other_clr.accent_clr), data->date.data());

    const ImVec2 stored_cursor_pos = GetCursorScreenPos();

    SetCursorScreenPos(ImVec2(rect.Max.x - SCALE(110), rect.Min.y + SCALE(10)));
    char btn_buf[80]; snprintf(btn_buf, sizeof(btn_buf), "%s%u", data->name.data(), id);
    active_button(btn_buf, selection_id, selection_variable);

    SetCursorScreenPos(ImVec2(rect.Max.x - SCALE(146), rect.Min.y + SCALE(23)));
    char id_str[16]; snprintf(id_str, sizeof(id_str), "%u", id);
    if (lua_tool_button("C", id_str)) {
#ifdef BUILD_DLL
        char dname[260];
        lstrcpynA(dname, data->name.c_str(), 260);
        config_mgr::remove(dname);
        if (selection_variable >= (int)var->c_config.data.size())
            selection_variable = (int)var->c_config.data.size() - 1;
        else if (selection_variable > selection_id)
            selection_variable--;
        config_mgr::g_needs_refresh = true;
#else
        var->c_config.data.erase(var->c_config.data.begin() + selection_id);
#endif
    }

    SetCursorScreenPos(stored_cursor_pos);

    return true;
}