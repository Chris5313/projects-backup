// GW2 UI Preview - EXACT replica of the PvZ GW2 cheat menu
// Based on v4_secimage/payload/framework/gui_gw2.cc
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <d3d11.h>
#include <tchar.h>
#include <string>
#include <vector>
#include <cmath>

#define IMGUI_DEFINE_MATH_OPERATORS
#include "imgui.h"
#include "imgui_internal.h"
#include "imgui_impl_win32.h"
#include "imgui_impl_dx11.h"

#pragma comment(lib, "d3d11.lib")

extern IMGUI_IMPL_API LRESULT ImGui_ImplWin32_WndProcHandler(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam);

// ============================================================================
// FontAwesome icon codepoints (from variables.h)
// ============================================================================
#define ICON_FA_CROSSHAIRS     "\xef\x81\x9b"  // U+F05B Combat
#define ICON_FA_LAYER_GROUP    "\xef\x97\xbd"  // U+F5FD Visuals  
#define ICON_FA_BOLT           "\xef\x83\xa7"  // U+F0E7 Misc
#define ICON_FA_FOLDER_OPEN    "\xef\x81\xbc"  // U+F07C Config
#define ICON_FA_SLIDERS_H      "\xef\x87\x9e"  // U+F1DE Settings
#define ICON_FA_EYE            "\xef\x81\xae"  // U+F06E ESP
#define ICON_FA_PALETTE        "\xef\x94\xbf"  // U+F53F Appearance
#define ICON_FA_COG            "\xef\x80\x93"  // U+F013 Settings section
#define ICON_FA_GAMEPAD        "\xef\x84\x9b"  // U+F11B Misc section

// ============================================================================
// Fonts
// ============================================================================
static ImFont* g_fontMain = nullptr;
static ImFont* g_fontIcons = nullptr;
static ImFont* g_fontMedium = nullptr;

// ============================================================================
// Colors (from settings.h - exact values)
// ============================================================================
namespace clr {
    ImVec4 accent = ImVec4(0.f, 0.65f, 0.32f, 1.f);  // Hamas green
    ImVec4 general_layout = ImColor(0, 0, 0, 240);
    ImVec4 general_stroke = ImColor(18, 22, 32, 255);
    ImVec4 layout = ImColor(10, 12, 18, 255);
    ImVec4 stroke = ImColor(16, 20, 30, 255);
    ImVec4 child_layout = ImColor(13, 15, 22, 255);
    ImVec4 child_stroke = ImColor(16, 20, 30, 255);
    ImVec4 element_layout = ImColor(18, 21, 30, 255);
    ImVec4 separator = ImColor(24, 30, 44, 255);
    ImVec4 checkbox_active = ImColor(28, 33, 50, 255);
    ImVec4 text_active = ImColor(255, 255, 255, 255);
    ImVec4 text_hov = ImColor(140, 150, 175, 255);
    ImVec4 text = ImColor(80, 90, 115, 255);
}

// ============================================================================
// Variables
// ============================================================================
struct {
    int selection = 0;
    int selection_active = 0;
    float selection_alpha = 1.f;
} g_tab;

struct {
    bool aimbot = false;
    bool silent_aimbot = false;
    int fov = 30;
    int smoothing = 10;
    bool vis_check = true;
    int limb_selection = 0;
    std::vector<std::string> limb_list = { "Head", "Chest", "Pelvis" };
} g_aimbot;

struct {
    bool enable_trigger = false;
    int delay = 50;
} g_trigger;

struct {
    bool enable = true;
    int box_type = 0;
    std::vector<std::string> box_list = { "2D", "Corner", "3D" };
    bool name = true;
    bool health = true;
    bool distance = true;
} g_esp;

struct {
    bool enable = false;
    float visible_color[4] = { 0, 1, 0, 1 };
    float invisible_color[4] = { 1, 0, 0, 1 };
} g_chams;

struct {
    bool no_spread = false;
    bool no_recoil = false;
    bool rapid_fire = false;
    bool speedhack = false;
    float speed_mult = 2.f;
    bool fly_hack = false;
} g_misc;

struct {
    float accent_color[4] = { 0.f, 0.65f, 0.32f, 1.f };
    bool accent_enabled = true;
    bool rgb_mode = false;
    float rgb_speed = 1.f;
    int font_size = 13;
} g_appearance;

struct {
    bool watermark = false;
} g_watermark;

// ============================================================================
// D3D11 globals
// ============================================================================
static ID3D11Device*            g_pd3dDevice = nullptr;
static ID3D11DeviceContext*     g_pd3dDeviceContext = nullptr;
static IDXGISwapChain*          g_pSwapChain = nullptr;
static bool                     g_SwapChainOccluded = false;
static UINT                     g_ResizeWidth = 0, g_ResizeHeight = 0;
static ID3D11RenderTargetView*  g_mainRenderTargetView = nullptr;

bool CreateDeviceD3D(HWND hWnd);
void CleanupDeviceD3D();
void CreateRenderTarget();
void CleanupRenderTarget();
LRESULT WINAPI WndProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam);

// ============================================================================
// Helpers
// ============================================================================
ImU32 GetClr(const ImVec4& col, float alpha = 1.f) {
    return ImGui::ColorConvertFloat4ToU32(ImVec4(col.x, col.y, col.z, col.w * alpha));
}

ImVec4 GetAccent() {
    if (g_appearance.rgb_mode) {
        float hue = fmodf((float)ImGui::GetTime() * g_appearance.rgb_speed * 0.2f, 1.0f);
        float r, g, b;
        ImGui::ColorConvertHSVtoRGB(hue, 0.85f, 1.0f, r, g, b);
        return ImVec4(r, g, b, 1.0f);
    } else if (g_appearance.accent_enabled) {
        return ImVec4(g_appearance.accent_color[0], g_appearance.accent_color[1],
                      g_appearance.accent_color[2], g_appearance.accent_color[3]);
    }
    return clr::accent;
}

void SetLinearColorAlpha(ImDrawList* dl, int vtx_start, int vtx_end, ImVec2 p0, ImVec2 p1, ImU32 col0, ImU32 col1) {
    ImVec2 dir = p1 - p0;
    float len = sqrtf(dir.x * dir.x + dir.y * dir.y);
    if (len < 0.001f) return;
    dir.x /= len; dir.y /= len;
    
    for (int i = vtx_start; i < vtx_end; i++) {
        ImDrawVert& v = dl->VtxBuffer[i];
        float t = ((v.pos.x - p0.x) * dir.x + (v.pos.y - p0.y) * dir.y) / len;
        t = ImClamp(t, 0.0f, 1.0f);
        
        ImVec4 c0 = ImGui::ColorConvertU32ToFloat4(col0);
        ImVec4 c1 = ImGui::ColorConvertU32ToFloat4(col1);
        ImVec4 mixed(c0.x + (c1.x - c0.x) * t, c0.y + (c1.y - c0.y) * t, 
                     c0.z + (c1.z - c0.z) * t, c0.w + (c1.w - c0.w) * t);
        v.col = ImGui::ColorConvertFloat4ToU32(mixed);
    }
}

// Centered text helper
void RenderTextCentered(ImDrawList* dl, ImFont* font, ImVec2 rect_min, ImVec2 rect_max, ImU32 col, const char* text) {
    ImVec2 text_size = font->CalcTextSizeA(font->FontSize, FLT_MAX, 0.0f, text);
    ImVec2 pos(
        rect_min.x + (rect_max.x - rect_min.x - text_size.x) * 0.5f,
        rect_min.y + (rect_max.y - rect_min.y - text_size.y) * 0.5f
    );
    dl->AddText(font, font->FontSize, pos, col, text);
}

// ============================================================================
// Widget implementations (match framework widgets)
// ============================================================================
namespace widget {
    void Section(const char* label, const char* icon = nullptr) {
        ImVec4 accent = GetAccent();
        
        ImGui::PushFont(g_fontIcons);
        if (icon) {
            ImGui::TextColored(accent, "%s", icon);
            ImGui::SameLine();
        }
        ImGui::PopFont();
        
        ImGui::PushFont(g_fontMedium);
        ImGui::TextColored(accent, "%s", label);
        ImGui::PopFont();
        
        ImGui::PushStyleColor(ImGuiCol_Separator, clr::separator);
        ImGui::Separator();
        ImGui::PopStyleColor();
        ImGui::Spacing();
    }

    void Separator() {
        ImGui::PushStyleColor(ImGuiCol_Separator, clr::separator);
        ImGui::Separator();
        ImGui::PopStyleColor();
    }

    bool Checkbox(const char* label, bool* v) {
        ImVec4 accent = GetAccent();
        ImGui::PushStyleColor(ImGuiCol_FrameBg, clr::element_layout);
        ImGui::PushStyleColor(ImGuiCol_FrameBgHovered, clr::checkbox_active);
        ImGui::PushStyleColor(ImGuiCol_FrameBgActive, clr::checkbox_active);
        ImGui::PushStyleColor(ImGuiCol_CheckMark, accent);
        ImGui::PushStyleVar(ImGuiStyleVar_FrameRounding, 2.0f);
        ImGui::PushStyleVar(ImGuiStyleVar_FramePadding, ImVec2(3, 3));
        bool ret = ImGui::Checkbox(label, v);
        ImGui::PopStyleVar(2);
        ImGui::PopStyleColor(4);
        return ret;
    }

    bool SliderInt(const char* label, int* v, int min, int max, const char* fmt = "%d") {
        ImVec4 accent = GetAccent();
        ImGui::PushStyleColor(ImGuiCol_FrameBg, clr::element_layout);
        ImGui::PushStyleColor(ImGuiCol_FrameBgHovered, clr::checkbox_active);
        ImGui::PushStyleColor(ImGuiCol_FrameBgActive, clr::checkbox_active);
        ImGui::PushStyleColor(ImGuiCol_SliderGrab, accent);
        ImGui::PushStyleColor(ImGuiCol_SliderGrabActive, accent);
        ImGui::PushStyleVar(ImGuiStyleVar_FrameRounding, 2.0f);
        ImGui::PushStyleVar(ImGuiStyleVar_GrabRounding, 2.0f);
        ImGui::PushItemWidth(ImGui::GetContentRegionAvail().x - 100);
        bool ret = ImGui::SliderInt(label, v, min, max, fmt);
        ImGui::PopItemWidth();
        ImGui::PopStyleVar(2);
        ImGui::PopStyleColor(5);
        return ret;
    }

    bool SliderFloat(const char* label, float* v, float min, float max, const char* fmt = "%.1f") {
        ImVec4 accent = GetAccent();
        ImGui::PushStyleColor(ImGuiCol_FrameBg, clr::element_layout);
        ImGui::PushStyleColor(ImGuiCol_FrameBgHovered, clr::checkbox_active);
        ImGui::PushStyleColor(ImGuiCol_FrameBgActive, clr::checkbox_active);
        ImGui::PushStyleColor(ImGuiCol_SliderGrab, accent);
        ImGui::PushStyleColor(ImGuiCol_SliderGrabActive, accent);
        ImGui::PushStyleVar(ImGuiStyleVar_FrameRounding, 2.0f);
        ImGui::PushStyleVar(ImGuiStyleVar_GrabRounding, 2.0f);
        ImGui::PushItemWidth(ImGui::GetContentRegionAvail().x - 100);
        bool ret = ImGui::SliderFloat(label, v, min, max, fmt);
        ImGui::PopItemWidth();
        ImGui::PopStyleVar(2);
        ImGui::PopStyleColor(5);
        return ret;
    }

    bool Dropdown(const char* label, int* current, std::vector<std::string>& items) {
        ImVec4 accent = GetAccent();
        ImGui::PushStyleColor(ImGuiCol_FrameBg, clr::element_layout);
        ImGui::PushStyleColor(ImGuiCol_FrameBgHovered, clr::checkbox_active);
        ImGui::PushStyleColor(ImGuiCol_PopupBg, clr::child_layout);
        ImGui::PushStyleColor(ImGuiCol_Header, GetClr(accent, 0.3f));
        ImGui::PushStyleColor(ImGuiCol_HeaderHovered, GetClr(accent, 0.5f));
        ImGui::PushStyleVar(ImGuiStyleVar_FrameRounding, 2.0f);
        ImGui::PushItemWidth(ImGui::GetContentRegionAvail().x - 100);
        
        bool ret = false;
        if (ImGui::BeginCombo(label, items[*current].c_str())) {
            for (int i = 0; i < (int)items.size(); i++) {
                bool selected = (*current == i);
                if (ImGui::Selectable(items[i].c_str(), selected)) {
                    *current = i;
                    ret = true;
                }
                if (selected) ImGui::SetItemDefaultFocus();
            }
            ImGui::EndCombo();
        }
        ImGui::PopItemWidth();
        ImGui::PopStyleVar();
        ImGui::PopStyleColor(5);
        return ret;
    }

    void ColorEdit(const char* label, float col[4]) {
        ImGui::PushStyleColor(ImGuiCol_FrameBg, clr::element_layout);
        ImGui::PushStyleVar(ImGuiStyleVar_FrameRounding, 2.0f);
        ImGui::ColorEdit4(label, col, ImGuiColorEditFlags_NoInputs | ImGuiColorEditFlags_AlphaBar);
        ImGui::PopStyleVar();
        ImGui::PopStyleColor();
    }
}

// ============================================================================
// Child window helper
// ============================================================================
void BeginChild(const char* id) {
    ImGui::PushStyleColor(ImGuiCol_ChildBg, clr::child_layout);
    ImGui::PushStyleColor(ImGuiCol_Border, clr::child_stroke);
    ImGui::PushStyleVar(ImGuiStyleVar_ChildRounding, 4.0f);
    ImGui::PushStyleVar(ImGuiStyleVar_ChildBorderSize, 1.0f);
    ImGui::PushStyleVar(ImGuiStyleVar_WindowPadding, ImVec2(12, 12));
    ImGui::BeginChild(id, ImVec2(0, 0), ImGuiChildFlags_Border | ImGuiChildFlags_AutoResizeY);
}

void EndChild() {
    ImGui::EndChild();
    ImGui::PopStyleVar(3);
    ImGui::PopStyleColor(2);
    ImGui::Spacing();
}

// ============================================================================
// Main menu render - EXACT match to gui_gw2.cc
// ============================================================================
void RenderMenu() {
    ImVec4 accent = GetAccent();
    
    // Window sizing (from settings.h)
    const ImVec2 window_size(880, 700);
    const float sidebar_w = 110;
    const float padding = 15;
    const float tab_h = 38;
    const float rounding = 12.f;
    const float inner_rounding = 8.f;

    ImGui::SetNextWindowSize(window_size, ImGuiCond_Always);
    ImGui::PushStyleVar(ImGuiStyleVar_WindowPadding, ImVec2(0, 0));
    ImGui::PushStyleVar(ImGuiStyleVar_WindowBorderSize, 0);
    ImGui::PushStyleVar(ImGuiStyleVar_WindowRounding, rounding);
    ImGui::PushStyleColor(ImGuiCol_WindowBg, ImVec4(0, 0, 0, 0));

    ImGui::Begin("##main", nullptr, 
        ImGuiWindowFlags_NoTitleBar | ImGuiWindowFlags_NoResize | 
        ImGuiWindowFlags_NoScrollbar | ImGuiWindowFlags_NoScrollWithMouse |
        ImGuiWindowFlags_NoBringToFrontOnFocus);
    {
        ImVec2 pos = ImGui::GetWindowPos();
        ImVec2 size = ImGui::GetWindowSize();
        ImDrawList* dl = ImGui::GetWindowDrawList();

        // =====================================================================
        // Panel backgrounds (from gui_gw2.cc lines 130-134)
        // =====================================================================
        // Outer panel (full window)
        dl->AddRectFilled(pos, ImVec2(pos.x + size.x, pos.y + size.y), 
            GetClr(clr::general_layout), rounding);
        dl->AddRect(pos, ImVec2(pos.x + size.x, pos.y + size.y), 
            GetClr(clr::general_stroke), rounding);

        // Inner content panel (right side)
        ImVec2 inner_min(pos.x + sidebar_w, pos.y + padding);
        ImVec2 inner_max(pos.x + size.x - padding, pos.y + size.y - padding);
        dl->AddRectFilled(inner_min, inner_max, GetClr(clr::layout), inner_rounding);
        dl->AddRect(inner_min, inner_max, GetClr(clr::stroke), inner_rounding);

        // =====================================================================
        // Accent glow lines (from gui_gw2.cc lines 136-150)
        // =====================================================================
        // Top glow
        {
            int vtx_start = dl->VtxBuffer.Size;
            dl->AddRectFilled(inner_min, ImVec2(inner_max.x, inner_min.y + 3), 
                GetClr(accent), inner_rounding, ImDrawFlags_RoundCornersTop);
            int vtx_end = dl->VtxBuffer.Size;
            SetLinearColorAlpha(dl, vtx_start, vtx_end, inner_min, 
                ImVec2(inner_min.x, inner_min.y + 3), GetClr(accent), IM_COL32(0, 0, 0, 0));
        }
        // Bottom glow
        {
            int vtx_start = dl->VtxBuffer.Size;
            dl->AddRectFilled(ImVec2(inner_min.x, inner_max.y - 3), inner_max, 
                GetClr(accent), inner_rounding, ImDrawFlags_RoundCornersBottom);
            int vtx_end = dl->VtxBuffer.Size;
            SetLinearColorAlpha(dl, vtx_start, vtx_end, ImVec2(inner_min.x, inner_max.y - 3), 
                inner_max, IM_COL32(0, 0, 0, 0), GetClr(accent));
        }

        // =====================================================================
        // Sidebar - Logo (circle with accent, "H" inside)
        // =====================================================================
        {
            float icon_size = 90;
            ImVec2 icon_center(pos.x + sidebar_w * 0.5f, pos.y + size.y * 0.5f);
            
            // Glow effect
            for (int i = 3; i >= 0; i--) {
                float alpha = 0.08f * (4 - i);
                dl->AddCircleFilled(icon_center, icon_size * 0.5f + i * 3, GetClr(accent, alpha), 48);
            }
            
            // Main circle
            dl->AddCircleFilled(icon_center, icon_size * 0.5f, GetClr(accent, 0.15f), 48);
            dl->AddCircle(icon_center, icon_size * 0.5f, GetClr(accent, 0.8f), 48, 2.0f);
            
            // "H" letter in center
            ImGui::PushFont(g_fontMedium);
            const char* h = "H";
            ImVec2 ts = g_fontMedium->CalcTextSizeA(24, FLT_MAX, 0, h);
            dl->AddText(g_fontMedium, 24, 
                ImVec2(icon_center.x - ts.x * 0.5f, icon_center.y - ts.y * 0.5f), 
                GetClr(accent), h);
            ImGui::PopFont();
        }

        // =====================================================================
        // Sidebar - Vertical "HAMAS" text above logo (from gui_gw2.cc lines 161-178)
        // =====================================================================
        ImGui::PushFont(g_fontMedium);
        {
            const char* letters_top[] = { "H", "A", "M", "A", "S" };
            const int count_top = 5;
            const float letter_h = 16;
            const float total_top = letter_h * count_top;
            const float start_y = pos.y + size.y * 0.5f - 50 - total_top;

            for (int i = 0; i < count_top; i++) {
                float alpha = (float)(i + 1) / (float)count_top;
                ImVec2 lt_min = { pos.x, start_y + letter_h * i };
                ImVec2 lt_max = { pos.x + sidebar_w, lt_min.y + letter_h };
                ImU32 col = IM_COL32(255, 255, 255, (int)(alpha * 255));
                RenderTextCentered(dl, g_fontMedium, lt_min, lt_max, col, letters_top[i]);
            }
        }
        ImGui::PopFont();

        // =====================================================================
        // Sidebar - Vertical "CLIENT" text below logo (from gui_gw2.cc lines 181-198)
        // =====================================================================
        ImGui::PushFont(g_fontMedium);
        {
            const char* letters_bot[] = { "C", "L", "I", "E", "N", "T" };
            const int count_bot = 6;
            const float letter_h = 16;
            const float start_y = pos.y + size.y * 0.5f + 50;

            for (int i = 0; i < count_bot; i++) {
                float alpha = 1.f - (float)i / (float)count_bot;
                ImVec2 lt_min = { pos.x, start_y + letter_h * i };
                ImVec2 lt_max = { pos.x + sidebar_w, lt_min.y + letter_h };
                ImU32 col = IM_COL32(255, 255, 255, (int)(alpha * 255));
                RenderTextCentered(dl, g_fontMedium, lt_min, lt_max, col, letters_bot[i]);
            }
        }
        ImGui::PopFont();

        // =====================================================================
        // Tab bar (from gui_gw2.cc lines 201-253)
        // =====================================================================
        {
            const float content_x = pos.x + sidebar_w;
            const float content_w = size.x - sidebar_w - padding;
            const float tab_y = pos.y + 18;  // pos.y + padding + 3 (glow)
            const int count = 5;
            const float tab_w = content_w / (float)count;
            
            const char* tab_labels[] = { "Combat", "Visuals", "Misc", "Config", "Settings" };
            const char* tab_icons[] = { 
                ICON_FA_CROSSHAIRS, 
                ICON_FA_LAYER_GROUP, 
                ICON_FA_BOLT, 
                ICON_FA_FOLDER_OPEN, 
                ICON_FA_SLIDERS_H 
            };

            for (int i = 0; i < count; i++) {
                ImVec2 t_min = { content_x + tab_w * i, tab_y };
                ImVec2 t_max = { t_min.x + tab_w, tab_y + tab_h };

                bool hov = ImGui::IsMouseHoveringRect(t_min, t_max);
                bool pressed = hov && ImGui::IsMouseClicked(0);
                if (pressed) g_tab.selection = i;

                bool active = (g_tab.selection_active == i);

                // Background
                ImU32 bg = active ? GetClr(accent, 0.25f) :
                           hov   ? GetClr(clr::element_layout, 1.5f) :
                                   IM_COL32(0, 0, 0, 0);
                if (bg) dl->AddRectFilled(t_min, t_max, bg, 0);

                // Active underline
                if (active)
                    dl->AddRectFilled(ImVec2(t_min.x, t_max.y - 2), t_max, GetClr(accent), 0);

                // Icon
                ImU32 icon_col = active ? GetClr(accent) : 
                                 hov    ? GetClr(clr::text_hov) : 
                                          GetClr(clr::text);
                ImGui::PushFont(g_fontIcons);
                ImVec2 icon_size = g_fontIcons->CalcTextSizeA(g_fontIcons->FontSize, FLT_MAX, 0, tab_icons[i]);
                dl->AddText(g_fontIcons, g_fontIcons->FontSize,
                    ImVec2(t_min.x + 12, t_min.y + (tab_h - icon_size.y) * 0.5f), 
                    icon_col, tab_icons[i]);
                ImGui::PopFont();

                // Label
                ImU32 txt_col = active ? GetClr(clr::text_active) : 
                                hov    ? GetClr(clr::text_hov) : 
                                         GetClr(clr::text);
                ImGui::PushFont(g_fontMain);
                dl->AddText(g_fontMain, g_fontMain->FontSize,
                    ImVec2(t_min.x + 32, t_min.y + (tab_h - g_fontMain->FontSize) * 0.5f), 
                    txt_col, tab_labels[i]);
                ImGui::PopFont();
            }

            // Separator line below tabs
            dl->AddRectFilled(
                ImVec2(content_x, tab_y + tab_h), 
                ImVec2(content_x + content_w, tab_y + tab_h + 1), 
                GetClr(clr::stroke), 0);
        }

        // Tab fade animation
        g_tab.selection_alpha = ImClamp(g_tab.selection_alpha + 
            (4.f * ImGui::GetIO().DeltaTime * (g_tab.selection == g_tab.selection_active ? 1.f : -1.f)), 
            0.f, 1.f);
        if (g_tab.selection_alpha == 0.f)
            g_tab.selection_active = g_tab.selection;

        // =====================================================================
        // Content area
        // =====================================================================
        ImGui::SetCursorPos(ImVec2(sidebar_w + 10, 18 + tab_h + 15));
        ImGui::PushStyleVar(ImGuiStyleVar_Alpha, g_tab.selection_alpha);
        ImGui::PushStyleVar(ImGuiStyleVar_WindowPadding, ImVec2(10, 10));
        ImGui::PushStyleVar(ImGuiStyleVar_ItemSpacing, ImVec2(8, 8));
        
        ImGui::BeginChild("##content", 
            ImVec2(size.x - sidebar_w - padding - 20, size.y - padding - 18 - tab_h - 30), 
            false);
        {
            // TAB 0 - Combat
            if (g_tab.selection_active == 0) {
                BeginChild("aimbot");
                widget::Section("Aimbot", ICON_FA_CROSSHAIRS);
                widget::Checkbox("Enable", &g_aimbot.aimbot);
                if (g_aimbot.aimbot) {
                    widget::Separator();
                    widget::Checkbox("Silent aim", &g_aimbot.silent_aimbot);
                    widget::Separator();
                    widget::SliderInt("FOV", &g_aimbot.fov, 1, 180, "%d");
                    widget::Separator();
                    widget::SliderInt("Smoothing", &g_aimbot.smoothing, 1, 100, "%d");
                    widget::Separator();
                    widget::Dropdown("Bone", &g_aimbot.limb_selection, g_aimbot.limb_list);
                    widget::Separator();
                    widget::Checkbox("Visibility check", &g_aimbot.vis_check);
                }
                EndChild();

                BeginChild("triggerbot");
                widget::Section("Triggerbot", ICON_FA_BOLT);
                widget::Checkbox("Enable", &g_trigger.enable_trigger);
                if (g_trigger.enable_trigger) {
                    widget::Separator();
                    widget::SliderInt("Delay (ms)", &g_trigger.delay, 0, 500, "%dms");
                }
                EndChild();
            }

            // TAB 1 - Visuals
            if (g_tab.selection_active == 1) {
                BeginChild("esp");
                widget::Section("ESP", ICON_FA_EYE);
                widget::Checkbox("Enable", &g_esp.enable);
                if (g_esp.enable) {
                    widget::Separator();
                    widget::Dropdown("Box type", &g_esp.box_type, g_esp.box_list);
                    widget::Separator();
                    widget::Checkbox("Name", &g_esp.name);
                    widget::Separator();
                    widget::Checkbox("Health bar", &g_esp.health);
                    widget::Separator();
                    widget::Checkbox("Distance", &g_esp.distance);
                }
                EndChild();

                BeginChild("chams");
                widget::Section("Chams", ICON_FA_PALETTE);
                widget::Checkbox("Enable", &g_chams.enable);
                if (g_chams.enable) {
                    widget::Separator();
                    widget::ColorEdit("Visible color", g_chams.visible_color);
                    widget::Separator();
                    widget::ColorEdit("Invisible color", g_chams.invisible_color);
                }
                EndChild();
            }

            // TAB 2 - Misc
            if (g_tab.selection_active == 2) {
                BeginChild("weapon_mods");
                widget::Section("Weapon Mods", ICON_FA_GAMEPAD);
                widget::Checkbox("No spread", &g_misc.no_spread);
                widget::Separator();
                widget::Checkbox("No recoil", &g_misc.no_recoil);
                widget::Separator();
                widget::Checkbox("Rapid fire", &g_misc.rapid_fire);
                EndChild();

                BeginChild("movement");
                widget::Section("Movement", ICON_FA_BOLT);
                widget::Checkbox("Speedhack", &g_misc.speedhack);
                if (g_misc.speedhack) {
                    widget::Separator();
                    widget::SliderFloat("Speed multiplier", &g_misc.speed_mult, 1.f, 10.f, "%.1fx");
                }
                widget::Separator();
                widget::Checkbox("Fly hack", &g_misc.fly_hack);
                EndChild();
            }

            // TAB 3 - Config
            if (g_tab.selection_active == 3) {
                BeginChild("config");
                widget::Section("Config", ICON_FA_FOLDER_OPEN);
                ImGui::TextColored(ImVec4(0.5f, 0.55f, 0.7f, 1.f), "Coming soon...");
                ImGui::TextColored(ImVec4(0.35f, 0.4f, 0.55f, 1.f), "Config save/load will be added.");
                EndChild();
            }

            // TAB 4 - Settings
            if (g_tab.selection_active == 4) {
                BeginChild("appearance");
                widget::Section("Appearance", ICON_FA_PALETTE);
                widget::ColorEdit("Accent color", g_appearance.accent_color);
                widget::Separator();
                widget::Checkbox("Custom accent", &g_appearance.accent_enabled);
                widget::Separator();
                widget::Checkbox("RGB mode", &g_appearance.rgb_mode);
                if (g_appearance.rgb_mode) {
                    widget::Separator();
                    widget::SliderFloat("RGB speed", &g_appearance.rgb_speed, 0.1f, 5.f, "%.1f");
                }
                EndChild();

                BeginChild("misc_settings");
                widget::Section("Miscellaneous", ICON_FA_COG);
                widget::Checkbox("Watermark", &g_watermark.watermark);
                EndChild();
            }
        }
        ImGui::EndChild();
        ImGui::PopStyleVar(3);
    }
    ImGui::End();

    ImGui::PopStyleColor();
    ImGui::PopStyleVar(3);
}

// ============================================================================
// Main
// ============================================================================
int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow) {
    WNDCLASSEXW wc = { sizeof(wc), CS_CLASSDC, WndProc, 0L, 0L, GetModuleHandle(nullptr), nullptr, nullptr, nullptr, nullptr, L"GW2 UI Preview", nullptr };
    ::RegisterClassExW(&wc);
    HWND hwnd = ::CreateWindowW(wc.lpszClassName, L"PvZ GW2 UI Preview", WS_OVERLAPPEDWINDOW, 100, 100, 1000, 780, nullptr, nullptr, wc.hInstance, nullptr);

    if (!CreateDeviceD3D(hwnd)) {
        CleanupDeviceD3D();
        ::UnregisterClassW(wc.lpszClassName, wc.hInstance);
        return 1;
    }

    ::ShowWindow(hwnd, SW_SHOWDEFAULT);
    ::UpdateWindow(hwnd);

    IMGUI_CHECKVERSION();
    ImGui::CreateContext();
    ImGuiIO& io = ImGui::GetIO();
    io.ConfigFlags |= ImGuiConfigFlags_NavEnableKeyboard;
    io.IniFilename = nullptr;  // Disable imgui.ini

    // Load fonts
    ImFontConfig cfg;
    cfg.OversampleH = 2;
    cfg.OversampleV = 2;
    
    // Main font (system default)
    g_fontMain = io.Fonts->AddFontFromFileTTF("C:\\Windows\\Fonts\\segoeui.ttf", 14.0f, &cfg);
    if (!g_fontMain) g_fontMain = io.Fonts->AddFontDefault();
    
    // Medium font for headers
    g_fontMedium = io.Fonts->AddFontFromFileTTF("C:\\Windows\\Fonts\\segoeuib.ttf", 14.0f, &cfg);
    if (!g_fontMedium) g_fontMedium = g_fontMain;

    // FontAwesome icons - load specific glyph ranges
    static const ImWchar icon_ranges[] = { 0xf000, 0xf8ff, 0 };  // FontAwesome range
    ImFontConfig icon_cfg;
    icon_cfg.MergeMode = false;
    icon_cfg.PixelSnapH = true;
    icon_cfg.OversampleH = 2;
    icon_cfg.OversampleV = 2;
    
    g_fontIcons = io.Fonts->AddFontFromFileTTF("fa-solid-900.ttf", 14.0f, &icon_cfg, icon_ranges);
    if (!g_fontIcons) {
        // Fallback - try absolute path
        g_fontIcons = io.Fonts->AddFontFromFileTTF(
            "C:\\Users\\Shadow\\Documents\\Projects\\PVZGW2\\ui_preview\\fa-solid-900.ttf", 
            14.0f, &icon_cfg, icon_ranges);
    }
    if (!g_fontIcons) g_fontIcons = g_fontMain;

    io.Fonts->Build();

    ImGui::StyleColorsDark();
    ImGuiStyle& style = ImGui::GetStyle();
    style.WindowRounding = 12.0f;
    style.FrameRounding = 2.0f;
    style.GrabRounding = 2.0f;
    style.ScrollbarRounding = 2.0f;
    style.Colors[ImGuiCol_Text] = clr::text_active;
    style.Colors[ImGuiCol_WindowBg] = ImVec4(0.06f, 0.06f, 0.08f, 1.00f);
    style.Colors[ImGuiCol_ScrollbarBg] = clr::layout;
    style.Colors[ImGuiCol_ScrollbarGrab] = clr::element_layout;
    style.Colors[ImGuiCol_ScrollbarGrabHovered] = clr::checkbox_active;
    style.Colors[ImGuiCol_ScrollbarGrabActive] = clr::checkbox_active;

    ImGui_ImplWin32_Init(hwnd);
    ImGui_ImplDX11_Init(g_pd3dDevice, g_pd3dDeviceContext);

    ImVec4 clear_color = ImVec4(0.04f, 0.04f, 0.06f, 1.00f);

    bool done = false;
    while (!done) {
        MSG msg;
        while (::PeekMessage(&msg, nullptr, 0U, 0U, PM_REMOVE)) {
            ::TranslateMessage(&msg);
            ::DispatchMessage(&msg);
            if (msg.message == WM_QUIT)
                done = true;
        }
        if (done) break;

        if (g_SwapChainOccluded && g_pSwapChain->Present(0, DXGI_PRESENT_TEST) == DXGI_STATUS_OCCLUDED) {
            ::Sleep(10);
            continue;
        }
        g_SwapChainOccluded = false;

        if (g_ResizeWidth != 0 && g_ResizeHeight != 0) {
            CleanupRenderTarget();
            g_pSwapChain->ResizeBuffers(0, g_ResizeWidth, g_ResizeHeight, DXGI_FORMAT_UNKNOWN, 0);
            g_ResizeWidth = g_ResizeHeight = 0;
            CreateRenderTarget();
        }

        ImGui_ImplDX11_NewFrame();
        ImGui_ImplWin32_NewFrame();
        ImGui::NewFrame();

        RenderMenu();

        ImGui::Render();
        const float clear_color_with_alpha[4] = { clear_color.x, clear_color.y, clear_color.z, clear_color.w };
        g_pd3dDeviceContext->OMSetRenderTargets(1, &g_mainRenderTargetView, nullptr);
        g_pd3dDeviceContext->ClearRenderTargetView(g_mainRenderTargetView, clear_color_with_alpha);
        ImGui_ImplDX11_RenderDrawData(ImGui::GetDrawData());

        HRESULT hr = g_pSwapChain->Present(1, 0);
        g_SwapChainOccluded = (hr == DXGI_STATUS_OCCLUDED);
    }

    ImGui_ImplDX11_Shutdown();
    ImGui_ImplWin32_Shutdown();
    ImGui::DestroyContext();

    CleanupDeviceD3D();
    ::DestroyWindow(hwnd);
    ::UnregisterClassW(wc.lpszClassName, wc.hInstance);

    return 0;
}

// ============================================================================
// D3D11 helpers
// ============================================================================
bool CreateDeviceD3D(HWND hWnd) {
    DXGI_SWAP_CHAIN_DESC sd = {};
    sd.BufferCount = 2;
    sd.BufferDesc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
    sd.BufferDesc.RefreshRate.Numerator = 60;
    sd.BufferDesc.RefreshRate.Denominator = 1;
    sd.Flags = DXGI_SWAP_CHAIN_FLAG_ALLOW_MODE_SWITCH;
    sd.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
    sd.OutputWindow = hWnd;
    sd.SampleDesc.Count = 1;
    sd.Windowed = TRUE;
    sd.SwapEffect = DXGI_SWAP_EFFECT_DISCARD;

    D3D_FEATURE_LEVEL featureLevelArray[2] = { D3D_FEATURE_LEVEL_11_0, D3D_FEATURE_LEVEL_10_0 };
    D3D_FEATURE_LEVEL featureLevel;
    HRESULT res = D3D11CreateDeviceAndSwapChain(nullptr, D3D_DRIVER_TYPE_HARDWARE, nullptr, 0, 
        featureLevelArray, 2, D3D11_SDK_VERSION, &sd, &g_pSwapChain, &g_pd3dDevice, &featureLevel, &g_pd3dDeviceContext);
    if (res == DXGI_ERROR_UNSUPPORTED)
        res = D3D11CreateDeviceAndSwapChain(nullptr, D3D_DRIVER_TYPE_WARP, nullptr, 0, 
            featureLevelArray, 2, D3D11_SDK_VERSION, &sd, &g_pSwapChain, &g_pd3dDevice, &featureLevel, &g_pd3dDeviceContext);
    if (res != S_OK) return false;
    CreateRenderTarget();
    return true;
}

void CleanupDeviceD3D() {
    CleanupRenderTarget();
    if (g_pSwapChain) { g_pSwapChain->Release(); g_pSwapChain = nullptr; }
    if (g_pd3dDeviceContext) { g_pd3dDeviceContext->Release(); g_pd3dDeviceContext = nullptr; }
    if (g_pd3dDevice) { g_pd3dDevice->Release(); g_pd3dDevice = nullptr; }
}

void CreateRenderTarget() {
    ID3D11Texture2D* pBackBuffer;
    g_pSwapChain->GetBuffer(0, IID_PPV_ARGS(&pBackBuffer));
    g_pd3dDevice->CreateRenderTargetView(pBackBuffer, nullptr, &g_mainRenderTargetView);
    pBackBuffer->Release();
}

void CleanupRenderTarget() {
    if (g_mainRenderTargetView) { g_mainRenderTargetView->Release(); g_mainRenderTargetView = nullptr; }
}

LRESULT WINAPI WndProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam) {
    if (ImGui_ImplWin32_WndProcHandler(hWnd, msg, wParam, lParam))
        return true;

    switch (msg) {
    case WM_SIZE:
        if (wParam == SIZE_MINIMIZED) return 0;
        g_ResizeWidth = (UINT)LOWORD(lParam);
        g_ResizeHeight = (UINT)HIWORD(lParam);
        return 0;
    case WM_SYSCOMMAND:
        if ((wParam & 0xfff0) == SC_KEYMENU) return 0;
        break;
    case WM_DESTROY:
        ::PostQuitMessage(0);
        return 0;
    }
    return ::DefWindowProcW(hWnd, msg, wParam, lParam);
}
