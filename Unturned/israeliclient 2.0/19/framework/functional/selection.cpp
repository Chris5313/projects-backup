#include "../settings/functions.h"

static bool PointInHexagon(ImVec2 point, ImVec2 center, float radius)
{
    float dx = fabsf(point.x - center.x);
    float dy = fabsf(point.y - center.y);
    // Quick rectangular reject
    if (dx > radius || dy > radius * 0.866f) return false;
    // Hexagon edge test (pointy-top orientation)
    return (radius * 0.866f - dy) * radius > (dx - radius * 0.5f) * radius * 0.866f * 2.0f;
}

bool c_widget::selection(std::string_view label, const ImVec2& size, int selection_id, int& selection_variable)
{
    struct selection_state
    {
        ImVec4 background;
        ImVec4 text;
    };

    ImGuiWindow* window = GetCurrentWindow();
    if (window->SkipItems) return false;

    ImGuiContext& g = *GImGui;
    const ImGuiStyle& style = g.Style;
    const ImGuiID id = window->GetID(label.data());

    const bool selected = selection_id == selection_variable;
    const float label_size = CalcTextSize(label.data(), NULL, true).x;

    const ImVec2 pos = window->DC.CursorPos;
    ImRect rect(pos, pos + SCALE(size));

    ItemSize(rect, 0);
    if (!ItemAdd(rect, id)) return false;

    bool hovered, held;
    bool pressed = ButtonBehavior(rect, id, &hovered, &held);

    // Override: only count as hovered/pressed if mouse is inside the hexagon shape
    ImVec2 center = ImVec2(rect.Min.x + (rect.Max.x - rect.Min.x) * 0.5f, rect.Min.y + (rect.Max.y - rect.Min.y) * 0.5f);
    float hex_radius = (rect.Max.x - rect.Min.x) * 0.5f;
    bool in_hex = PointInHexagon(GetIO().MousePos, center, hex_radius);
    if (!in_hex) { hovered = false; pressed = false; held = false; }

    if (pressed) { selection_variable = selection_id; ui_sound::tab_switch(); }

    selection_state* state = gui->anim_container(&state, id);

    state->background = ImLerp(state->background, (selected || hovered) ? clr->c_element.dropdown_selection_layout : clr->c_element.layout, gui->fixed_speed(12.f));
    state->text = ImLerp(state->text, (selected || hovered) ? clr->c_other_clr.accent_clr : clr->c_text.text, gui->fixed_speed(12.f));
    
    draw->render_text(window->DrawList, set->c_font.icon[6], rect.Min - SCALE(12, 12), rect.Max + SCALE(12, 12), gui->get_clr(clr->c_child.layout), "A", NULL, NULL, ImVec2(0.5f, 0.5f));
    draw->render_text(window->DrawList, set->c_font.icon[5], rect.Min - SCALE(1, 0), rect.Max - SCALE(1, 0), gui->get_clr(state->background), "A", NULL, NULL, ImVec2(0.5f, 0.5f));
    draw->render_text(window->DrawList, set->c_font.icon[1], rect.Min, rect.Max, gui->get_clr(state->text), label.data(), 0, 0, { 0.5, 0.5 });

    return pressed;
}
