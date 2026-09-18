#include "style.h"

void Style::ApplyStyle() {
    ImGuiStyle& style = ImGui::GetStyle();

    // Sizing
    style.WindowRounding    = 12.0f;
    style.FrameRounding     = 8.0f;
    style.GrabRounding      = 6.0f;
    style.FramePadding      = ImVec2(12.0f, 8.0f);
    style.ItemSpacing       = ImVec2(8.0f, 8.0f);
    style.ScrollbarSize     = 0.0f;
    style.WindowBorderSize  = 0.0f;
    style.FrameBorderSize   = 0.0f;
    style.PopupRounding     = 8.0f;
    style.WindowPadding     = ImVec2(0.0f, 0.0f);
    style.AntiAliasedLines  = true;
    style.AntiAliasedFill   = true;

    ImVec4* colors = style.Colors;

    // Window
    colors[ImGuiCol_WindowBg]           = ImVec4(0.0f, 0.0f, 0.0f, 0.0f); // transparent, we draw our own
    colors[ImGuiCol_ChildBg]            = ImVec4(0.0f, 0.0f, 0.0f, 0.0f);
    colors[ImGuiCol_PopupBg]            = ToVec4(COL_CARD);

    // Frame (inputs, checkboxes)
    colors[ImGuiCol_FrameBg]            = ToVec4(COL_INPUT_BG);
    colors[ImGuiCol_FrameBgHovered]     = ImVec4(0.10f, 0.10f, 0.125f, 1.0f); // #1a1a20
    colors[ImGuiCol_FrameBgActive]      = ImVec4(0.10f, 0.10f, 0.125f, 1.0f);

    // Button
    colors[ImGuiCol_Button]             = ToVec4(COL_ACCENT);
    colors[ImGuiCol_ButtonHovered]      = ToVec4(COL_ACCENT_HOVER);
    colors[ImGuiCol_ButtonActive]       = ToVec4(COL_ACCENT_ACTIVE);

    // Text
    colors[ImGuiCol_Text]               = ToVec4(COL_TEXT);
    colors[ImGuiCol_TextDisabled]       = ToVec4(COL_TEXT_DIM);

    // Border
    colors[ImGuiCol_Border]             = ToVec4(COL_CARD_BORDER);
    colors[ImGuiCol_BorderShadow]       = ImVec4(0.0f, 0.0f, 0.0f, 0.0f);

    // Checkmark — blue tick on dark bg (no blue fill behind it)
    colors[ImGuiCol_CheckMark]          = ToVec4(COL_ACCENT);

    // Scrollbar (hidden but set for safety)
    colors[ImGuiCol_ScrollbarBg]        = ImVec4(0.0f, 0.0f, 0.0f, 0.0f);
    colors[ImGuiCol_ScrollbarGrab]      = ImVec4(0.0f, 0.0f, 0.0f, 0.0f);
    colors[ImGuiCol_ScrollbarGrabHovered] = ImVec4(0.0f, 0.0f, 0.0f, 0.0f);
    colors[ImGuiCol_ScrollbarGrabActive]  = ImVec4(0.0f, 0.0f, 0.0f, 0.0f);

    // Header (used by selectable, tree nodes etc.)
    colors[ImGuiCol_Header]             = ToVec4(COL_ACCENT);
    colors[ImGuiCol_HeaderHovered]      = ToVec4(COL_ACCENT_HOVER);
    colors[ImGuiCol_HeaderActive]       = ToVec4(COL_ACCENT_ACTIVE);

    // Separator
    colors[ImGuiCol_Separator]          = ToVec4(COL_CARD_BORDER);

    // Slider
    colors[ImGuiCol_SliderGrab]         = ToVec4(COL_ACCENT);
    colors[ImGuiCol_SliderGrabActive]   = ToVec4(COL_ACCENT_HOVER);

    // Title bar (not used but consistent)
    colors[ImGuiCol_TitleBg]            = ToVec4(COL_CARD);
    colors[ImGuiCol_TitleBgActive]      = ToVec4(COL_CARD);
    colors[ImGuiCol_TitleBgCollapsed]   = ToVec4(COL_CARD);
}
