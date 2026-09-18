#pragma once

#define IMGUI_DEFINE_MATH_OPERATORS

#include "imgui.h"
#include "imgui_internal.h"

#include "variables.h"
#include "elements.h"
#include "settings.h"

#include <vector>
#include <sstream>
#include <string>

#include "imgui_impl_dx11.h"
#include "imgui_impl_win32.h"

#include <map>
#include <regex>
#include <unordered_map>
#include <unordered_set>
#include <array>

inline ID3D11Device* g_pd3dDevice = nullptr;
inline ID3D11DeviceContext* g_pd3dDeviceContext = nullptr;
inline IDXGISwapChain* g_pSwapChain = nullptr;
inline bool                     g_SwapChainOccluded = false;
inline UINT                     g_ResizeWidth = 0, g_ResizeHeight = 0;
inline ID3D11RenderTargetView* g_mainRenderTargetView = nullptr;

#define SCALE(...) scale_impl(__VA_ARGS__, var->c_dpi.dpi)

inline ImVec2 scale_impl(const ImVec2& vec, float dpi) {
    return ImVec2(roundf(vec.x * dpi), roundf(vec.y * dpi));
}

inline ImVec2 scale_impl(float x, float y, float dpi) {
    return ImVec2(roundf(x * dpi), roundf(y * dpi));
}

inline float scale_impl(float var, float dpi) {
    return roundf(var * dpi);
}

#define PI 3.14159265359f

enum notify_position : int
{
    top_left,
    top_right,
    bottom_left,
    bottom_right,
};

enum fade_direction : int
{
    vertically,
    horizontally,
    diagonally,
    diagonally_reversed,
};

enum watermark_position : int {
    mark_top_left,
    mark_top_right,
    mark_bottom_left,
    mark_bottom_right,
};

enum interpolation_type {
    back,
    elastic,
};



using namespace ImGui;

class c_gui
{
public:

    template <typename T>
    T* anim_container(T** state_ptr, ImGuiID id)
    {
        T* state = static_cast<T*>(GetStateStorage()->GetVoidPtr(id));
        if (!state)
            GetStateStorage()->SetVoidPtr(id, state = new T());

        *state_ptr = state;
        return state;
    }

    ImU32                           get_clr(const ImVec4& col, float alpha = 1.f);

    float                           fixed_speed(float speed) { return speed / ImGui::GetIO().Framerate; };

    bool                            begin(std::string_view name, bool* p_open = nullptr, ImGuiWindowFlags flags = ImGuiWindowFlags_None);

    void                            end();

    void                            push_style_color(ImGuiCol idx, ImU32 col);

    void                            pop_style_color(int count = 1);

    void                            push_style_var(ImGuiStyleVar idx, float val);

    void                            push_style_var(ImGuiStyleVar idx, const ImVec2& val);

    void                            pop_style_var(int count = 1);

    void                            push_font(ImFont* font);

    void                            pop_font();

    void                            set_cursor_pos(const ImVec2& local_pos);

    void                            set_cursor_pos_x(float x);
                                    
    void                            set_cursor_pos_y(float y);

    ImVec2                          get_cursor_pos();

    float                           get_cursor_pos_x();

    float                           get_cursor_pos_y();

    void                            spacing();

    void                            sameline(float offset_from_start_x = 0.0f, float spacing_w = -1.0f);

    void                            set_next_window_pos(const ImVec2& pos, ImGuiCond cond = 0, const ImVec2& pivot = ImVec2(0, 0));

    void                            set_next_window_size(const ImVec2& size, ImGuiCond cond = 0);

    void                            begin_group();

    void                            end_group();

    bool                            begin_child(std::string_view name, const ImVec2& size_arg = ImVec2(0, 0), ImGuiChildFlags child_flags = 0, ImGuiWindowFlags window_flags = 0);

    void                            end_child();

    void                            begin_content(const char* id, const ImVec2& size_arg, const ImVec2& padding, const ImVec2& spacing);

    void                            end_content();

    void                            water_mark(std::string name, std::vector<std::string> function, watermark_position type, bool* visible);

    inline                          ImGuiWindow* get_current_window() { ImGuiContext& g = *GImGui; g.CurrentWindow->WriteAccessed = true; return g.CurrentWindow; };

    void                            new_frame();

    void                            end_frame();

	std::string						get_current_date();

	void							rotate_start();

	void							rotate_end(float rad, ImVec2 center = ImVec2(0, 0));

	float							deg_to_rad(float deg);

    void                            render();

};

inline c_gui* gui = nullptr;

class c_widget
{
public:

    bool                            checkbox(std::string_view label, bool* callback);

    bool                            checkbox_with_key(std::string_view label, bool* callback, int* key, bool* mode, bool* value, bool* show_in_binds);

	bool                            checkbox_with_color(std::string_view label, bool* callback, float col[4], bool alpha = true);

    bool                            slider_float(std::string_view label, float* v, float v_min, float v_max, float power = 0.1f, const char* format = "");

    bool                            slider_int(std::string_view label, int* v, int v_min, int v_max, int power = 1, const char* format = "");

    bool                            dropdown(std::string_view label, int* current_item, std::vector<std::string>& items, int max_count);

    bool                            tool_dropdown(std::string_view label, int* current_item, std::vector<std::string>& items, int max_count);
    
    void                            multi_dropdown(std::string_view label, bool variable[], std::vector<std::string>& labels, int max_count);

    bool                            keybind(std::string_view label, int* key);

    bool                            keybind_button(std::string_view name_id, bool* pressing);

    bool                            begin_popup(std::string_view name, float size_w, const ImVec2& position = {0, 0});

    void                            end_popup();

    bool                            set_tooltip(std::string_view tooltip_id, std::string_view tooltip_text);

    void                            text_colored(ImFont* font, const ImU32 col, std::string text);

	bool                            color_edit(std::string_view label, float col[4], ImGuiColorEditFlags flags = 0);

    bool                            button(std::string_view label, const ImVec2& size);
    
    bool                            tool_button(std::string_view label, std::string_view icon, const ImVec2& size);

    bool                            text_field(std::string_view hint, std::string_view label, char* buf, size_t buf_size, const ImVec2& size, ImGuiInputTextFlags flags = 0, ImGuiInputTextCallback callback = 0, void* user_data = 0);

    bool                            selection(std::string_view label, const ImVec2& size, int selection_id, int& selection_variable);

    void                            separator();

    // Section header — visual divider with icon + label.
    // icon: single char from the icon font (e.g. "P" for players).
    //        nullptr / "" = no icon, just text header.
    void                            section(std::string_view label, const char* icon = nullptr);

};

inline c_widget* widget = nullptr;

class c_draw
{
public:

    void                            push_clip_rect(ImDrawList* draw, const ImVec2& cr_min, const ImVec2& cr_max, bool intersect_with_current_clip_rect);
        
    void                            pop_clip_rect(ImDrawList* draw);

    void                            add_text(ImDrawList* draw_list, const ImFont* font, float font_size, const ImVec2& pos, ImU32 col, const char* text_begin, const char* text_end = NULL, float wrap_width = NULL, const ImVec4* cpu_fine_clip_rect = NULL);

    void                            render_text(ImFont* font, ImDrawList* draw_list, const ImVec2& pos_min, const ImVec2& pos_max, ImU32 color, const char* text, const char* text_display_end = NULL, const ImVec2* text_size_if_known = NULL, const ImVec2& align = { 0.5, 0.5 }, const ImRect* clip_rect = NULL);

	void							render_text_with_spacing(const char* text, float spacing, ImVec2 position1, ImVec2 position2, ImU32 color, bool centered);

    void                            rect_filled_multi_color(ImDrawList* draw, const ImVec2& p_min, const ImVec2& p_max, ImU32 col_upr_left, ImU32 col_upr_right, ImU32 col_bot_right, ImU32 col_bot_left, float rounding = 0.f, ImDrawFlags flags = 0);

    void                            add_rect_filled(ImDrawList* draw_list, const ImVec2& p_min, const ImVec2& p_max, ImU32 col, float rounding = 0, ImDrawFlags flags = 0);

    void                            add_circle_filled(ImDrawList* draw_list, const ImVec2& center, float radius, ImU32 col, int num_segments = 0);

    void                            add_circle(ImDrawList* draw_list, const ImVec2& center, float radius, ImU32 col, int num_segments = 0, float thickness = 0);

    void                            add_rect(ImDrawList* draw_list, const ImVec2& p_min, const ImVec2& p_max, ImU32 col, float rounding, ImDrawFlags flags = 0, float tickness = 0);

    void                            fade_rect_filled(ImDrawList* draw, const ImVec2& pos_min, const ImVec2& pos_max, ImU32 col_one, ImU32 col_two, fade_direction direction, float rounding = 0.f, ImDrawFlags flags = 0);

    void                            render_text(ImDrawList* draw_list, ImFont* font, const ImVec2& pos_min, const ImVec2& pos_max, ImU32 color, const char* text, const char* text_display_end = NULL, const ImVec2* text_size_if_known = NULL, const ImVec2& align = ImVec2(0.f, 0.f), const ImRect* clip_rect = NULL);

    void                            radial_gradient(ImDrawList* draw_list, const ImVec2& center, float radius, ImU32 col_in, ImU32 col_out);

    void                            set_linear_color_alpha(ImDrawList* draw_list, int vert_start_idx, int vert_end_idx, ImVec2 gradient_p0, ImVec2 gradient_p1, ImU32 col0, ImU32 col1);

    void                            add_image(ImDrawList* draw_list, ImTextureID user_texture_id, const ImVec2& p_min, const ImVec2& p_max, const ImVec2& uv_min, const ImVec2& uv_max, ImU32 col);

    void                            add_image_rounded(ImDrawList* draw_list, ImTextureID user_texture_id, const ImVec2& p_min, const ImVec2& p_max, const ImVec2& uv_min, const ImVec2& uv_max, ImU32 col, float rounding = 0, ImDrawFlags flags = 0);

    void                            add_line(ImDrawList* draw, const ImVec2& p1, const ImVec2& p2, ImU32 col, float thickness = 0);

};

inline c_draw* draw = nullptr;

struct notify_state
{
    int notify_id;

    std::string_view text;

    float notify_delay;

    float notify_alpha{ 0.f }, notify_offset{ 0.f }, notify_timer{ 0.f };

    bool active_notify{ true };

    notify_position type;
};

class c_notify
{
public:

    void setup_notify();

    void add_notify(std::string_view text, float notify_delay, notify_position type);

    void render_notify(int cur_notify_value, float notify_alpha, float notify_offset, float notify_percentage, float notify_delay, std::string_view text, notify_position type);

    int notify_count{ 0 };

    std::vector<notify_state> notifications;
};

inline c_notify* notify = nullptr;

struct add_item_state
{
    bool window_opened = false;
    float window_alpha = 0.f;
    bool window_hovered = false;
};

class c_easing
{

public:

    float easing_value;

    float ease_in_elastic(float t) { const float c4 = (2 * PI) / 2;  return (t <= 0.01f) ? 0.0f : (t >= 0.60f) ? 1.0f : pow(2, -10 * t) * sin((t * 10 - 0.75) * c4) + 1; }

    float ease_in_back(float t) { const float c1 = 1.70158; const float c3 = c1 + 1; return 1 + c3 * pow(t - 1, 3) + c1 * pow(t - 1, 2); }

    struct easing_state {
        float animTime = 0.0f;
        bool reverse = false;
    };

    template <typename T>
    T im_ease(int animation_id, bool callback, T min, T max, float speed, interpolation_type type) {
        easing_state* state = gui->anim_container(&state, GetCurrentWindow()->GetID(animation_id));

        state->animTime = (callback != state->reverse) ? 0.0f : min(state->animTime + 0.1f * speed, 1.0f);
        state->reverse = callback;

        easing_value = (type == elastic) ? ease_in_elastic(state->animTime) : (type == back) ? ease_in_back(state->animTime) : 0;

        return callback ? easing_value * (max - min) + min : easing_value * (min - max) + max;
    }

    template <>
    ImVec2 im_ease<ImVec2>(int animation_id, bool callback, ImVec2 min, ImVec2 max, float speed, interpolation_type type) {
        easing_state* state = gui->anim_container(&state, GetCurrentWindow()->GetID(animation_id));

        state->animTime = (callback != state->reverse) ? 0.0f : min(state->animTime + 0.1f * speed, 1.0f);
        state->reverse = callback;

        easing_value = (type == elastic) ? ease_in_elastic(state->animTime) : (type == back) ? ease_in_back(state->animTime) : 0;

        ImVec2 result;
        result.x = callback ? easing_value * (max.x - min.x) + min.x : easing_value * (min.x - max.x) + max.x;
        result.y = callback ? easing_value * (max.y - min.y) + min.y : easing_value * (min.y - max.y) + max.y;
        return result;
    }
};

inline std::unique_ptr<c_easing> easing = std::make_unique<c_easing>();

// Sound stubs — no embedded WAV in GW2 build
namespace ui_sound {
    inline void click() {}
    inline void hover() {}
    inline void toggle_on() {}
    inline void toggle_off() {}
    inline void tab_switch() {}
    inline void error() {}
    inline void notify() {}
}
