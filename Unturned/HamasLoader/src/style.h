#pragma once
#include "imgui.h"

namespace Style {
    // Colors
    constexpr ImU32 COL_BG            = IM_COL32(8, 10, 8, 255);        // dark with green tint
    constexpr ImU32 COL_CARD          = IM_COL32(16, 20, 16, 255);      // dark green-black
    constexpr ImU32 COL_CARD_BORDER   = IM_COL32(30, 40, 30, 255);      // dark green border
    constexpr ImU32 COL_INPUT_BG      = IM_COL32(10, 14, 10, 255);      // very dark green
    constexpr ImU32 COL_ACCENT        = IM_COL32(0, 151, 54, 255);      // Hamas green #009736
    constexpr ImU32 COL_ACCENT_HOVER  = IM_COL32(0, 180, 70, 255);      // lighter green
    constexpr ImU32 COL_ACCENT_ACTIVE = IM_COL32(0, 120, 40, 255);      // darker green
    constexpr ImU32 COL_TEXT          = IM_COL32(255, 255, 255, 255);    // white
    constexpr ImU32 COL_TEXT_DIM      = IM_COL32(90, 110, 90, 255);     // muted green-gray
    constexpr ImU32 COL_TEXT_SUB      = IM_COL32(130, 150, 130, 255);   // subtitle green-gray
    constexpr ImU32 COL_CLOSE_HOVER   = IM_COL32(206, 17, 38, 255);     // Hamas red #CE1126

    // As ImVec4 for ImGui style colors
    inline ImVec4 ToVec4(ImU32 col) {
        return ImVec4(
            ((col >> 0)  & 0xFF) / 255.0f,
            ((col >> 8)  & 0xFF) / 255.0f,
            ((col >> 16) & 0xFF) / 255.0f,
            ((col >> 24) & 0xFF) / 255.0f
        );
    }

    void ApplyStyle();
}
