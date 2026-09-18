#pragma once

#include <string>
#include <vector>
#include <imgui.h>

struct lua_data { std::string name; std::string date; bool active = false; };
struct config_data { std::string name; std::string date; };

struct c_variable
{
    // ============================================================
    // ESP Preview
    // ============================================================
    struct c_preview
    {
        float box_c[4]        = { 1.f, 1.f, 1.f, 1.f };
        int   box_type        = 0;
        float corner_color[4] = { 1.f, 1.f, 1.f, 1.f };
        int   flags           = 0;
        int   bars            = 0;
        bool  esp_preview     = false;
        float texts_spacing   = 2.f;
        float bars_spacing    = 2.f;
        float box_padding     = 2.f;
        float anim_speed      = 4.f;
    } c_preview;

    // ============================================================
    // Hex Selector (7 categories)
    // ============================================================
    struct c_selection
    {
        std::vector<std::string> selection_icon = { "K", "F", "N", "H", "J", "V", "E" };
        int   selection        = 0;
        int   selection_active = 0;
        float selection_alpha  = 1.f;
        float selection_add    = 0.f;
    } c_selection;

    // ============================================================
    // Watermark
    // ============================================================
    struct c_watermark
    {
        std::vector<std::string> watermark_content = { "ISRAELI CLIENT", "Unturned", "144FPS", "64PING", "12:15PM" };
        bool watermark          = true;
        int  watermark_position = 1;
    } c_watermark;

    // ============================================================
    // Notifications
    // ============================================================
    struct c_notify
    {
        int notify_position = 0;
    } c_notify;

    // ============================================================
    // Color Picker
    // ============================================================
    struct c_colorpicker
    {
        float  size        = 200.f;
        float  rounding    = 4.f;
        ImVec2 padding     = { 8.f, 8.f };
        ImVec2 picker_size = { 180.f, 180.f };
        ImVec2 bar_size    = { 12.f, 180.f };
    } c_colorpicker;

    // ============================================================
    // DPI
    // ============================================================
    struct c_dpi
    {
        float dpi         = 1.0f;
        float dpi_saved   = 1.0f;
        bool  dpi_changed = false;
    } c_dpi;

    // ============================================================
    // Aimbot
    // ============================================================
    struct c_aimbot
    {
        bool  aimbot          = false;
        int   aimbot_key      = 0;
        bool  aimbot_holding  = false;
        bool  aimbot_value    = false;
        bool  aimbot_show_binds = false;

        bool  silent_aimbot   = false;
        int   fov             = 30;
        int   smoothing       = 5;
        bool  vis_check       = true;
        bool  friendly        = false;
        bool  target_players  = true;
        bool  target_zombies  = false;

        int   limb_selection  = 0;
        std::vector<std::string> limb_list = { "Head", "Chest", "Spine", "Arms", "Legs" };

        bool  draw_fov        = false;
        float fov_color[4]    = { 1.f, 1.f, 1.f, 0.5f };

        bool  spinbot         = false;
        int   spin_type       = 0;
        std::vector<std::string> spin_list = { "4-Dir", "Random", "Left", "Right", "Flip 180" };
        bool  spin_show       = false;
    } c_aimbot;

    // ============================================================
    // Silent Aim
    // ============================================================
    struct c_silent
    {
        bool  silent           = false;
        int   silent_key       = 0;
        bool  silent_holding   = false;
        bool  silent_value     = false;
        bool  silent_show_binds = false;

        int   chance           = 100;
        bool  fov_restrict     = true;
        float silent_fov       = 15.f;
        float max_dist         = 300.f;
        bool  vis_check        = true;

        int   limb_selection   = 0;
        std::vector<std::string> limb_list = { "Head", "Chest", "Spine", "Arms", "Legs" };

        bool  draw_fov         = false;
        float fov_color[4]     = { 0.f, 1.f, 0.f, 0.5f };

        int   target_point     = 0;
        std::vector<std::string> target_list = { "Nearest", "Lowest HP", "Crosshair" };
        float target_color[4]  = { 1.f, 0.f, 0.f, 1.f };

        bool  target_players   = true;
        bool  target_zombies   = false;
        bool  skip_friendly    = true;
        float bounds_exp       = 1.f;
        bool  hitbox_expand    = false;
        bool  proton_mode      = false;
    } c_silent;

    // ============================================================
    // Triggerbot
    // ============================================================
    struct c_trigger
    {
        bool  enable_trigger       = false;
        int   enable_trigger_key   = 0;
        bool  enable_trigger_holding = false;
        bool  enable_trigger_value = false;
        bool  enable_trigger_show_binds = false;

        float delay            = 0.05f;
        bool  hold_fire        = false;
    } c_trigger;

    // ============================================================
    // Weapon Mods
    // ============================================================
    struct c_weapon
    {
        bool  no_recoil        = false;
        bool  no_spread        = false;
        bool  no_sway          = false;
        bool  rapid_fire       = false;
        float rapid_mult       = 2.f;
        bool  instant_reload   = false;
        bool  extended_mag     = false;
        int   mag_mult         = 2;
        bool  extended_melee   = false;
        float melee_range      = 5.f;
        bool  weapon_chams     = false;
        float weapon_chams_color[4] = { 0.2f, 0.6f, 1.f, 1.f };
        bool  hitsound         = false;
        float hitsound_vol     = 1.f;
        float hitsound_pitch   = 1.f;

        bool  auto_semi_burst  = false;
        float damage_flinch_mult = 1.f;
        bool  disable_bino     = false;
        bool  disable_scope    = false;
        bool  extend_ballistic_range = false;
        int   extra_ballistic_steps = 4;
        bool  ignore_leave_timer = false;
        bool  instant_aim      = false;
        bool  no_ballistics    = false;
        bool  force_headshot   = false;
    } c_weapon;

    // ============================================================
    // Global ESP (1.0 State: CornerBoxMode / Box3D)
    // ============================================================
    struct c_esp
    {
        bool corner_mode = true;
        bool box3d       = false;
    } c_esp;

    // ============================================================
    // Player ESP
    // ============================================================
    struct c_pesp
    {
        bool  esp              = false;
        int   esp_key          = 0;
        bool  esp_holding      = false;
        bool  esp_value        = false;
        bool  esp_show_binds   = false;
        bool  box              = true;
        float box_color[4]     = { 0.25f, 0.60f, 1.00f, 1.0f };
        bool  name             = true;
        float name_color[4]    = { 1.f, 1.f, 1.f, 0.95f };
        bool  health           = true;
        bool  distance         = true;
        bool  weapon           = true;
        float weapon_color[4]  = { 0.85f, 0.75f, 1.00f, 0.85f };
        bool  skeleton         = false;
        float skel_color[4]    = { 0.40f, 0.70f, 1.00f, 1.0f };
        bool  snapline         = false;
        float snap_color[4]    = { 0.55f, 0.40f, 0.85f, 1.0f };
        bool  fill             = false;
        float fill_color[4]    = { 0.25f, 0.60f, 1.00f, 1.0f };
        float fill_alpha       = 0.12f;
        float max_dist         = 500.f;
        float thickness        = 1.f;
    } c_pesp;

    // ============================================================
    // Zombie ESP
    // ============================================================
    struct c_zesp
    {
        bool  esp              = false;
        bool  box              = true;
        float box_color[4]     = { 0.70f, 0.70f, 0.10f, 1.0f };
        bool  name             = false;
        float name_color[4]    = { 1.f, 0.95f, 0.50f, 0.9f };
        bool  health           = true;
        bool  distance         = false;
        bool  skeleton         = false;
        float skel_color[4]    = { 0.80f, 0.80f, 0.30f, 1.0f };
        bool  snapline         = false;
        float snap_color[4]    = { 0.70f, 0.70f, 0.10f, 1.0f };
        bool  fill             = false;
        float fill_color[4]    = { 0.70f, 0.70f, 0.10f, 1.0f };
        float fill_alpha       = 0.12f;
        float max_dist         = 300.f;
        float thickness        = 1.f;
    } c_zesp;

    // ============================================================
    // Item ESP
    // ============================================================
    struct c_iesp
    {
        bool  esp              = false;
        float text_color[4]    = { 0.70f, 0.85f, 1.00f, 0.9f };
        bool  snapline         = false;
        float snap_color[4]    = { 0.70f, 0.85f, 1.00f, 1.0f };
        float max_dist         = 200.f;
        bool  clump            = true;
        bool  filter_weapons   = true;
        bool  filter_ammo      = true;
        bool  filter_medical   = true;
        bool  filter_food      = false;
        bool  filter_clothing  = false;
    } c_iesp;

    // ============================================================
    // Vehicle ESP
    // ============================================================
    struct c_vesp
    {
        bool  esp              = false;
        bool  box              = true;
        float box_color[4]     = { 0.30f, 0.50f, 1.00f, 1.0f };
        bool  name             = true;
        float name_color[4]    = { 0.80f, 0.85f, 1.00f, 0.9f };
        bool  health           = true;
        bool  distance         = true;
        bool  locked           = true;
        bool  snapline         = false;
        float snap_color[4]    = { 0.30f, 0.50f, 1.00f, 1.0f };
        bool  fill             = false;
        float fill_color[4]    = { 0.30f, 0.50f, 1.00f, 1.0f };
        float fill_alpha       = 0.10f;
        float max_dist         = 500.f;
        float thickness        = 1.f;
    } c_vesp;

    // ============================================================
    // Animal ESP
    // ============================================================
    struct c_aesp
    {
        bool  esp              = false;
        bool  box              = true;
        float box_color[4]     = { 0.90f, 0.60f, 0.20f, 1.0f };
        bool  name             = true;
        float name_color[4]    = { 1.00f, 0.85f, 0.60f, 0.9f };
        bool  distance         = true;
        bool  snapline         = false;
        float snap_color[4]    = { 0.90f, 0.60f, 0.20f, 1.0f };
        bool  fill             = false;
        float fill_color[4]    = { 0.90f, 0.60f, 0.20f, 1.0f };
        float fill_alpha       = 0.10f;
        float max_dist         = 300.f;
        float thickness        = 1.f;
    } c_aesp;

    // ============================================================
    // Storage ESP
    // ============================================================
    struct c_sesp
    {
        bool  esp              = false;
        bool  box              = true;
        float box_color[4]     = { 0.85f, 0.25f, 0.25f, 1.0f };
        bool  name             = true;
        float name_color[4]    = { 1.00f, 0.70f, 0.70f, 0.9f };
        bool  health           = true;
        bool  distance         = true;
        bool  snapline         = false;
        float snap_color[4]    = { 0.85f, 0.25f, 0.25f, 1.0f };
        bool  fill             = false;
        float fill_color[4]    = { 0.85f, 0.25f, 0.25f, 1.0f };
        float fill_alpha       = 0.10f;
        float max_dist         = 300.f;
        float thickness        = 1.f;
    } c_sesp;

    // ============================================================
    // Airdrop ESP
    // ============================================================
    struct c_desp
    {
        bool  esp              = false;
        bool  box              = true;
        float box_color[4]     = { 1.00f, 0.85f, 0.15f, 1.0f };
        bool  name             = true;
        float name_color[4]    = { 1.00f, 1.00f, 0.70f, 0.9f };
        bool  distance         = true;
        bool  snapline         = false;
        float snap_color[4]    = { 1.00f, 0.85f, 0.15f, 1.0f };
        bool  fill             = false;
        float fill_color[4]    = { 1.00f, 0.85f, 0.15f, 1.0f };
        float fill_alpha       = 0.10f;
        float max_dist         = 2000.f;
        float thickness        = 1.f;
    } c_desp;

    // ============================================================
    // Bed ESP
    // ============================================================
    struct c_besp
    {
        bool  esp              = false;
        bool  box              = true;
        float box_color[4]     = { 0.65f, 0.35f, 0.85f, 1.0f };
        bool  name             = true;
        float name_color[4]    = { 0.85f, 0.70f, 1.00f, 0.9f };
        bool  distance         = true;
        bool  claimed_only     = false;
        bool  show_claimed     = true;
        bool  snapline         = false;
        float snap_color[4]    = { 0.65f, 0.35f, 0.85f, 1.0f };
        bool  fill             = false;
        float fill_color[4]    = { 0.65f, 0.35f, 0.85f, 1.0f };
        float fill_alpha       = 0.10f;
        float max_dist         = 500.f;
        float thickness        = 1.f;
    } c_besp;

    // ============================================================
    // Generator ESP
    // ============================================================
    struct c_gesp
    {
        bool  esp              = false;
        bool  box              = true;
        float box_color[4]     = { 0.95f, 0.65f, 0.15f, 1.0f };
        bool  name             = true;
        float name_color[4]    = { 1.00f, 0.90f, 0.60f, 0.9f };
        bool  distance         = true;
        bool  fuel             = true;
        bool  snapline         = false;
        float snap_color[4]    = { 0.95f, 0.65f, 0.15f, 1.0f };
        bool  fill             = false;
        float fill_color[4]    = { 0.95f, 0.65f, 0.15f, 1.0f };
        float fill_alpha       = 0.10f;
        float max_dist         = 300.f;
        float thickness        = 1.f;
    } c_gesp;

    // ============================================================
    // Turret ESP
    // ============================================================
    struct c_tesp
    {
        bool  esp              = false;
        bool  box              = true;
        float box_color[4]     = { 1.00f, 0.30f, 0.30f, 1.0f };
        bool  name             = true;
        float name_color[4]    = { 1.00f, 0.70f, 0.70f, 0.9f };
        bool  distance         = true;
        bool  state            = true;
        bool  snapline         = false;
        float snap_color[4]    = { 1.00f, 0.30f, 0.30f, 1.0f };
        bool  fill             = false;
        float fill_color[4]    = { 1.00f, 0.30f, 0.30f, 1.0f };
        float fill_alpha       = 0.10f;
        float max_dist         = 500.f;
        float thickness        = 1.f;
    } c_tesp;

    // ============================================================
    // Grenade ESP
    // ============================================================
    struct c_resp
    {
        bool  esp              = false;
        bool  box              = true;
        float box_color[4]     = { 1.00f, 0.20f, 0.20f, 1.0f };
        bool  name             = true;
        float name_color[4]    = { 1.00f, 0.60f, 0.60f, 0.9f };
        bool  radius           = true;
        float rad_color[4]     = { 1.00f, 0.20f, 0.20f, 0.4f };
        bool  snapline         = false;
        float snap_color[4]    = { 1.00f, 0.20f, 0.20f, 1.0f };
        float max_dist         = 300.f;
        float thickness        = 1.f;
    } c_resp;

    // ============================================================
    // Bullet Tracers
    // ============================================================
    struct c_fesp
    {
        bool  esp              = false;
        bool  self             = true;
        bool  others           = true;
        bool  trail            = true;
        float self_color[4]    = { 0.30f, 0.90f, 1.00f, 0.9f };
        float other_color[4]   = { 1.00f, 0.30f, 0.30f, 0.9f };
        bool  snapline         = false;
        float snap_color[4]    = { 1.00f, 0.50f, 0.20f, 1.0f };
        float max_dist         = 500.f;
        float lifetime         = 3.f;
    } c_fesp;

    // ============================================================
    // Crosshair
    // ============================================================
    struct c_crosshair
    {
        bool  enable           = false;
        int   type_selection   = 0;
        std::vector<std::string> type_list = { "Cross", "Dot", "Circle", "Diamond", "Star of David" };
        float size             = 5.f;
        float thick            = 1.f;
        float color[4]         = { 1.f, 1.f, 1.f, 1.f };
        bool  spin             = false;
        float spin_speed       = 1.f;
    } c_crosshair;

    // ============================================================
    // Self / Visual Mods
    // ============================================================
    struct c_self
    {
        bool  custom_fov       = false;
        float fov_deg          = 90.f;
        bool  no_flash         = false;
        bool  no_pain          = false;
        bool  no_hallucination = false;
        bool  no_grayscale     = false;
        bool  no_fog           = false;

        int   night_vision     = 0;
        std::vector<std::string> nv_list = { "Off", "Military", "Civilian", "Custom" };

        bool  footsteps        = false;
        float footstep_color[4] = { 1.f, 0.5f, 0.f, 1.f };
        int   footstep_shape   = 0;   // 0=circle, 1=star
        std::vector<std::string> footstep_shape_list = { "Circle", "Star" };
        bool  footstep_spin    = false;
        float footstep_spin_spd = 90.f;
        float footstep_lifetime = 1.5f;
        float footstep_size    = 1.5f;

        bool  damage_numbers   = false;
        float dmg_lifetime     = 2.f;
        int   dmg_font_size    = 14;
        bool  dmg_custom_color = false;
        float dmg_color[4]     = { 1.f, 0.3f, 0.3f, 1.f };
        // Melee tracers
        bool  melee_tracers    = false;
        float melee_tracer_color[4] = { 1.f, 0.3f, 0.3f, 0.8f };
        float melee_tracer_lifetime = 1.f;
        // Bullet tracers
        bool  bullet_tracers   = false;
        float bullet_tracer_color[4] = { 0.3f, 0.9f, 1.f, 0.9f };
        float bullet_tracer_lifetime = 2.f;

        bool  chams            = false;  // legacy master toggle
        int   chams_key        = 0;
        bool  chams_holding    = false;
        bool  chams_value      = false;
        bool  chams_show_binds = false;
        float chams_color[4]   = { 1.f, 0.f, 0.f, 1.f };
        bool  player_chams     = false;
        bool  zombie_chams     = false;
        bool  self_chams       = false;
        int   chams_pattern    = 1;
        std::vector<std::string> chams_pattern_list = { "Star of David", "Flat", "Hologram", "Glow", "Wireframe", "WF Fill" };
        float chams_vis_color[4]  = { 0.2f, 0.6f, 1.f, 0.6f };
        float chams_nonvis_color[4] = { 1.f, 0.2f, 0.2f, 0.4f };
        float chams_wire_color[4] = { 1.f, 1.f, 1.f, 1.f };
        float chams_wire_behind_color[4] = { 1.f, 1.f, 1.f, 0.7f };
        float self_chams_color[4] = { 0.f, 1.f, 0.f, 0.5f };
        // Outline (game's HighlightingSystem)
        bool  outline          = false;
        float outline_color[4] = { 1.f, 0.f, 1.f, 1.f };

        bool  unlock_perspective = false;
    } c_self;

    // ============================================================
    // World
    // ============================================================
    struct c_world
    {
        bool  custom_time      = false;
        float time_value       = 12.f;
        bool  override_sky     = false;
        float sky_color[4]     = { 0.4f, 0.6f, 1.f, 1.f };
        bool  override_sun     = false;
        float sun_color[4]     = { 1.f, 0.9f, 0.7f, 1.f };
        bool  override_cloud   = false;
        float cloud_color[4]   = { 1.f, 1.f, 1.f, 1.f };
        bool  force_compass    = false;
        bool  force_map        = false;
        bool  map_show_players = false;
        bool  map_show_markers = false;
        bool  override_cloud_rim = false;
        float cloud_rim_color[4] = { 1.f, 0.8f, 0.5f, 1.f };
    } c_world;

    // ============================================================
    // Movement
    // ============================================================
    struct c_movement
    {
        bool  far_reach        = false;
        float reach_dist       = 10.f;
        bool  pickup_walls     = false;
        float pickup_dist      = 10.f;
        bool  extend_nearby    = false;
        float nearby_radius    = 20.f;

        bool  vehicle_fly      = false;
        int   vehicle_fly_key  = 0;
        bool  vehicle_fly_holding = false;
        bool  vehicle_fly_value = false;
        bool  vehicle_fly_show_binds = false;
        float fly_speed        = 10.f;

        bool  no_vehicle_dmg   = false;
        int   no_vehicle_dmg_key = 0;
        bool  no_vehicle_dmg_holding = false;
        bool  no_vehicle_dmg_value = false;
        bool  no_vehicle_dmg_show_binds = false;

        int   vehicle_fly_style = 0;
        std::vector<std::string> vehicle_fly_style_list = { "Original", "MoonClient" };
        bool  nearby_through_walls = false;
    } c_movement;

    // ============================================================
    // Placement
    // ============================================================
    struct c_placement
    {
        bool  ignore_barricade = false;
        bool  ignore_structure = false;
        bool  place_anywhere   = false;
        bool  custom_offset    = false;
        float offset_x         = 0.f;
        float offset_y         = 0.f;
        float offset_z         = 0.f;
        float salvage_multiplier = 1.f;
    } c_placement;

    // ============================================================
    // Fun
    // ============================================================
    struct c_fun
    {
        bool  freecam          = false;
        int   freecam_key      = 0;
        bool  freecam_holding  = false;
        bool  freecam_value    = false;
        bool  freecam_show_binds = false;
        float freecam_speed    = 5.f;

        bool  star_of_david    = false;
    } c_fun;

    // ============================================================
    // Automation
    // ============================================================
    struct c_automation
    {
        bool  auto_pickup          = false;
        float pickup_radius        = 10.f;
        bool  pickup_through_walls = false;
        bool  pickup_filter        = false;
        bool  auto_fish            = false;
        bool  auto_joiner          = false;
        char  server_ip[64]        = { 0 };
        int   server_port          = 27015;
        float auto_fish_delay      = 0.15f;
        bool  auto_forge           = false;
        float auto_forge_radius    = 25.f;
        float auto_forge_delay     = 0.1f;
        bool  auto_join_limit      = false;
        int   auto_join_max        = 10;
        float auto_join_retry      = 10.f;
        float auto_pickup_speed    = 0.05f;
        bool  auto_pickup_skip_empty = false;
    } c_automation;


    // ============================================================
    // Misc tools / player-relation (1.1-only features)
    // ============================================================
    struct c_misc
    {
        bool  item_spawner       = false;
        bool  entity_inspector   = false;
        bool  storage_viewer     = false;
        bool  hwid_changer       = false;
        float friend_color[4]    = { 0.2f, 0.9f, 0.3f, 1.f };
        float enemy_color[4]     = { 1.f, 0.2f, 0.2f, 1.f };
    } c_misc;

    // ============================================================
    // Auto Farm
    // ============================================================
    struct c_autofarm
    {
        bool  auto_farm_on       = false;
        float auto_farm_radius   = 30.f;
        float auto_farm_delay    = 0.5f;
        bool  auto_farm_replant  = true;
        bool  auto_farm_store    = true;
        bool  auto_farm_craft    = false;
        bool  auto_farm_equip_seed = true;
        bool  auto_farm_water    = false;
    } c_autofarm;

    // ============================================================
    // Quest Exploits
    // ============================================================
    struct c_quest
    {
        bool max_ml             = false;
        bool alcohol            = false;
        bool lumberjack         = false;
        bool voucher            = false;
        bool xmas2024           = false;
        bool xmas2025_factory   = false;
        bool xmas2025_penguins  = false;
    } c_quest;
    // ============================================================
    // Appearance
    // ============================================================
    struct c_appearance
    {
        int   menu_key         = 0x2D;
        float accent_color[4]  = { 0.f, 0.22f, 0.72f, 1.f };
        int   font_size        = 14;
        bool  watermark        = true;
        bool  info_bar         = true;
        bool  accent_enabled   = true;
        bool  rgb_mode         = false;
        float rgb_speed        = 1.f;
    } c_appearance;

    // ============================================================
    // Anti-Spy
    // ============================================================
    struct c_antispy
    {
        int spy_mode = 0;
        std::vector<std::string> spy_list = { "4-Frame", "Show", "Custom Image" };
        bool spy_toast = true;
    } c_antispy;


    // ============================================================
    // Proton Anti-Cheat Bypass
    // ============================================================
    struct c_proton
    {
        bool  bypass           = false;
    } c_proton;
    // ============================================================
    // Keybinds
    // ============================================================
    struct c_keybinds
    {
        int aimbot_key  = 0;
        int silent_key  = 0;
        int trigger_key = 0;
        int freecam_key = 0;
        int vfly_key    = 0;
        int vdmg_key    = 0;
    } c_keybinds;

    // ============================================================
    // Lua
    // ============================================================
    struct c_lua
    {
        std::vector<lua_data> data;
        bool opened          = false;
        bool create          = false;
        char name[128]       = { 0 };
        std::string editable;
        int  sort_selection  = 0;
        std::vector<std::string> sort_list = { "Newest", "Oldest" };
    } c_lua;

    // ============================================================
    // Config
    // ============================================================
    struct c_config
    {
        std::vector<config_data> data;
        bool create          = false;
        int  active          = -1;
        char name[128]       = { 0 };
        int  sort_selection  = 0;
        std::vector<std::string> sort_list = { "Newest", "Oldest" };
    } c_config;
};

inline c_variable* var = nullptr;
