#pragma once

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <d3d11.h>
#include "imgui.h"


class c_settings
{
public:

	struct
	{
		ImGuiWindowFlags window_flags = ImGuiWindowFlags_NoSavedSettings | ImGuiWindowFlags_NoBringToFrontOnFocus | ImGuiWindowFlags_NoDecoration | ImGuiWindowFlags_NoScrollbar | ImGuiWindowFlags_NoScrollWithMouse;

		std::string name = "I\nS\nR\nA\nE\nL\nI\n \nC\nL\nI\nE\nN\nT";

		ImVec2 window_size = ImVec2(880, 700);
		ImVec2 padding = ImVec2(0, 0);
		ImVec2 item_spacing = ImVec2(4, 4);

		float scrollbar_size = 20.f;
		float border_size = 0.f;

		float general_rounding = 12.f;
		float rounding = 8.f;

		bool slider_hovered = false;
		ImGuiID slider_id = 0;
	} c_window;

	struct
	{
		ImVec2 child_window_padding = ImVec2(10, 10);
		ImVec2 child_item_spacing = ImVec2(10, 10);

		float rounding = 4.f;

	} c_child;

	struct
	{
		float rounding = 2.f;

	} c_element;

	struct
	{
		float size = 11.f;
		float rounding = 2.f;
		ImVec2 padding = ImVec2(10, 10);
		ImVec2 picker_size = ImVec2(150, 100);
		ImVec2 bar_size = ImVec2(picker_size.x, 8);
	} c_picker;

	struct
	{
		ID3D11ShaderResourceView* bg = nullptr;
		ID3D11ShaderResourceView* logo = nullptr;
		ID3D11ShaderResourceView* tab_visuals = nullptr;
		ID3D11ShaderResourceView* tab_players = nullptr;
	} c_texture;

	struct
	{
		ImFont* icon[7];
		ImFont* inter_medium[2];
		ImFont* name;

	} c_font;

};

inline c_settings* set = nullptr;

class c_colors
{
public:

	struct
	{
		ImVec4 col_bg_alpha_0 = ImColor(125, 125, 125, 255);
		ImVec4 col_bg_alpha_1 = ImColor(185, 185, 185, 255);

		ImVec4 accent_clr = ImColor(0, 166, 81, 255);  // #00A651 Hamas green

		ImVec4 black_clr = ImColor(0, 0, 0, 255);
		ImVec4 white_clr = ImColor(255, 255, 255, 255);

	} c_other_clr;

	struct
	{
		ImVec4 general_layout = ImColor(0, 0, 0, 240);
		ImVec4 general_stroke = ImColor(18, 22, 32, 255);

		ImVec4 layout = ImColor(10, 12, 18, 255);
		ImVec4 stroke = ImColor(16, 20, 30, 255);

	} c_window;

	struct
	{
		ImVec4 layout = ImColor(13, 15, 22, 255);
		ImVec4 stroke = ImColor(16, 20, 30, 255);

	} c_child;

	struct
	{
		ImVec4 dropdown_selection_layout = ImColor(24, 28, 42, 255);
		ImVec4 popup_icon = ImColor(55, 62, 85, 255);

		ImVec4 checkbox_active = ImColor(28, 33, 50, 255);
		ImVec4 separator = ImColor(24, 30, 44, 255);

		ImVec4 layout = ImColor(18, 21, 30, 255);
		ImVec4 circle = ImColor(32, 38, 54, 255);

	} c_element;

	struct
	{
		ImVec4 text_active = ImColor(255, 255, 255, 255);
		ImVec4 text_hov = ImColor(140, 150, 175, 255);
		ImVec4 text = ImColor(80, 90, 115, 255);

	} c_text;

	struct
	{
		ImVec4 background = ImColor(14, 16, 24);
		ImVec4 stroke = ImColor(18, 22, 32);
	} c_lua;

};

inline c_colors* clr = nullptr;
