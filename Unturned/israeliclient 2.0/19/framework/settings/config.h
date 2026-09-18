#pragma once
// Config save/load system — binary blob + %APPDATA% persistence
// Auto-generated POD struct mirrors all saveable c_variable fields

#include <windows.h>
#include <shlobj.h>
#pragma comment(lib, "shell32.lib")

struct config_blob {
    int version = 1;

    // Preview
    float preview_box_c[4]; int preview_box_type; float preview_corner_color[4];
    int preview_flags; int preview_bars; bool preview_esp;
    float preview_texts_spacing; float preview_bars_spacing; float preview_box_padding; float preview_anim_speed;

    // DPI
    float dpi;

    // Aimbot
    bool aim_enabled; int aim_fov; int aim_smoothing; bool aim_vis; bool aim_friendly;
    bool aim_target_players; bool aim_target_zombies; int aim_limb; bool aim_draw_fov;
    float aim_fov_color[4]; bool aim_spinbot;

    // Silent
    bool silent; int silent_chance; bool silent_fov_restrict; float silent_fov;
    float silent_max_dist; bool silent_vis; int silent_limb; bool silent_draw_fov;
    float silent_fov_color[4]; int silent_target_point; float silent_target_color[4];
    bool silent_target_players; bool silent_target_zombies; bool silent_skip_friendly; float silent_bounds;

    // Trigger
    bool trigger; float trigger_delay; bool trigger_hold;

    // Weapon
    bool no_recoil; bool no_spread; bool no_sway; bool rapid_fire; float rapid_mult;
    bool instant_reload; bool extended_mag; int mag_mult; bool extended_melee;
    float melee_range; bool weapon_chams; float weapon_chams_color[4]; bool hitsound;
    float hitsound_vol; float hitsound_pitch;

    // Player ESP
    bool pesp; bool pesp_box; float pesp_box_color[4]; bool pesp_name; float pesp_name_color[4];
    bool pesp_health; bool pesp_distance; bool pesp_skeleton; bool pesp_snapline;
    float pesp_snap_color[4]; bool pesp_fill; float pesp_fill_color[4]; float pesp_fill_alpha;
    float pesp_max_dist; float pesp_thickness;

    // Zombie ESP
    bool zesp; bool zesp_box; float zesp_box_color[4]; bool zesp_name; bool zesp_health;
    bool zesp_distance; bool zesp_skeleton; bool zesp_snapline; float zesp_snap_color[4];
    bool zesp_fill; float zesp_fill_color[4]; float zesp_max_dist; float zesp_thickness;

    // Item ESP
    bool iesp; float iesp_text_color[4]; bool iesp_snapline; float iesp_snap_color[4];
    float iesp_max_dist; bool iesp_filter_weapons; bool iesp_filter_ammo;
    bool iesp_filter_medical; bool iesp_filter_food; bool iesp_filter_clothing;

    // Vehicle ESP
    bool vesp; bool vesp_box; float vesp_box_color[4]; bool vesp_name; float vesp_name_color[4];
    bool vesp_health; bool vesp_distance; bool vesp_locked; bool vesp_snapline;
    float vesp_snap_color[4]; bool vesp_fill; float vesp_fill_color[4]; float vesp_fill_alpha;
    float vesp_max_dist; float vesp_thickness;

    // Other ESP (legacy field names kept for blob compat)
    bool oesp_storage; bool oesp_airdrop; bool oesp_bed; bool oesp_generator;
    bool oesp_turret; bool oesp_bullet_tracer; bool oesp_grenade; bool oesp_animal;

    // Crosshair
    bool cross_enable; int cross_type; float cross_size; float cross_thick;
    // cross_rainbow removed: rainbow now lives in the color picker's per-color
    // slots like every other color. Field kept to preserve blob layout.
    float cross_color[4]; bool cross_reserved; bool cross_spin; float cross_spin_speed;

    // Self
    bool custom_fov; float fov_deg; bool no_flash; bool no_pain; bool no_hallucination;
    bool no_grayscale; bool no_fog; int night_vision; bool footsteps; float footstep_color[4];
    int footstep_shape; bool footstep_spin; float footstep_spin_spd;
    float footstep_lifetime; float footstep_size;
    bool damage_numbers; bool chams; float chams_color[4];

    // World
    bool custom_time; float time_value; bool override_sky; float sky_color[4];
    bool override_sun; float sun_color[4]; bool override_cloud; float cloud_color[4];
    bool force_compass; bool force_map; bool map_show_players; bool map_show_markers;

    // Movement
    bool far_reach; float reach_dist; bool pickup_walls; float pickup_dist;
    bool extend_nearby; float nearby_radius; float fly_speed;

    // Placement
    bool ignore_barricade; bool ignore_structure; bool place_anywhere;
    bool custom_offset; float offset_x; float offset_y; float offset_z;

    // Fun
    float freecam_speed; bool star_of_david;

    // Automation
    bool auto_pickup; float pickup_radius; bool pickup_through_walls; bool pickup_filter;
    bool auto_fish; bool auto_joiner; char server_ip[64]; int server_port;

    // Appearance
    float accent_color[4]; int font_size; bool watermark_on; bool info_bar;
    bool accent_enabled; bool rgb_mode; float rgb_speed;

    // Anti-spy
    int spy_mode;

    // Keybinds
    int kb_aimbot; int kb_silent; int kb_trigger; int kb_freecam; int kb_vfly; int kb_vdmg;

    // ---- v2 ESP fields (appended to preserve v1 layout) ----
    // Global ESP
    bool esp_corner_mode; bool esp_box3d;
    // Player ESP v2
    bool pesp_weapon; float pesp_weapon_color[4]; float pesp_skel_color[4];
    // Zombie ESP v2
    float zesp_name_color[4]; float zesp_skel_color[4]; float zesp_fill_alpha;
    // Item ESP v2
    bool iesp_clump;
    // Vehicle ESP v2
    float vesp_name_color_v2[4];
    // Animal ESP
    bool aesp; bool aesp_box; float aesp_box_color[4]; bool aesp_name; float aesp_name_color[4];
    bool aesp_distance; bool aesp_snapline; float aesp_snap_color[4];
    bool aesp_fill; float aesp_fill_color[4]; float aesp_fill_alpha;
    float aesp_max_dist; float aesp_thickness;
    // Storage ESP
    bool sesp; bool sesp_box; float sesp_box_color[4]; bool sesp_name; float sesp_name_color[4];
    bool sesp_health; bool sesp_distance; bool sesp_snapline; float sesp_snap_color[4];
    bool sesp_fill; float sesp_fill_color[4]; float sesp_fill_alpha;
    float sesp_max_dist; float sesp_thickness;
    // Airdrop ESP
    bool desp; bool desp_box; float desp_box_color[4]; bool desp_name; float desp_name_color[4];
    bool desp_distance; bool desp_snapline; float desp_snap_color[4];
    bool desp_fill; float desp_fill_color[4]; float desp_fill_alpha;
    float desp_max_dist; float desp_thickness;
    // Bed ESP
    bool besp; bool besp_box; float besp_box_color[4]; bool besp_name; float besp_name_color[4];
    bool besp_distance; bool besp_claimed_only; bool besp_show_claimed;
    bool besp_snapline; float besp_snap_color[4];
    bool besp_fill; float besp_fill_color[4]; float besp_fill_alpha;
    float besp_max_dist; float besp_thickness;
    // Generator ESP
    bool gesp; bool gesp_box; float gesp_box_color[4]; bool gesp_name; float gesp_name_color[4];
    bool gesp_distance; bool gesp_fuel; bool gesp_snapline; float gesp_snap_color[4];
    bool gesp_fill; float gesp_fill_color[4]; float gesp_fill_alpha;
    float gesp_max_dist; float gesp_thickness;
    // Turret ESP
    bool tesp; bool tesp_box; float tesp_box_color[4]; bool tesp_name; float tesp_name_color[4];
    bool tesp_distance; bool tesp_state; bool tesp_snapline; float tesp_snap_color[4];
    bool tesp_fill; float tesp_fill_color[4]; float tesp_fill_alpha;
    float tesp_max_dist; float tesp_thickness;
    // Grenade ESP
    bool resp; bool resp_box; float resp_box_color[4]; bool resp_name; float resp_name_color[4];
    bool resp_radius; float resp_rad_color[4]; bool resp_snapline; float resp_snap_color[4];
    float resp_max_dist; float resp_thickness;
    // Bullet Tracers
    bool fesp; bool fesp_self; bool fesp_others; bool fesp_trail;
    float fesp_self_color[4]; float fesp_other_color[4];
    bool fesp_snapline; float fesp_snap_color[4];
    float fesp_max_dist; float fesp_lifetime;

    // ---- v3 Chams fields ----
    int  chams_pattern;
    float chams_vis_color[4];
    float chams_nonvis_color[4];
    bool  chams_self;
    float chams_self_color[4];
    // ---- v4 Outline + Wire + Separate toggles ----
    bool  outline;
    float outline_color[4];
    float chams_wire_color[4];
    float chams_wire_behind_color[4];
    bool  player_chams;
    bool  zombie_chams;
    bool  self_chams;
    float self_chams_color[4];
    // ---- v5 1.1-only features ----
    int   spin_type; bool spin_show;
    bool  auto_semi_burst; float damage_flinch_mult; bool disable_bino; bool disable_scope;
    bool  extend_ballistic_range; int extra_ballistic_steps; bool ignore_leave_timer;
    bool  instant_aim; bool no_ballistics; bool force_headshot;
    bool  override_cloud_rim; float cloud_rim_color[4];
    bool  unlock_perspective;
    float friend_color[4]; float enemy_color[4];
    bool  nearby_through_walls; float salvage_multiplier;
    float auto_fish_delay; bool auto_forge; float auto_forge_radius; float auto_forge_delay;
    bool  auto_join_limit; int auto_join_max; float auto_join_retry;
    float auto_pickup_speed; bool auto_pickup_skip_empty;
    bool  misc_item_spawner; bool misc_entity_inspector; bool misc_storage_viewer; bool misc_hwid_changer;
    bool  spy_toast;
    int   vehicle_fly_style;
    bool  af_on; float af_radius; float af_delay; bool af_replant; bool af_store;
    bool  af_craft; bool af_equip_seed; bool af_water;
    bool  q_max_ml; bool q_alcohol; bool q_lumberjack; bool q_voucher;
    bool  q_xmas2024; bool q_xmas2025_factory; bool q_xmas2025_penguins;
};

#ifdef BUILD_DLL
namespace config_mgr {
    inline wchar_t g_dir[MAX_PATH] = {};
    inline char g_autoload_name[128] = {};
    inline char g_status[128] = "Ready";
    inline bool g_needs_refresh = true;  // deferred refresh — safe between frames

    // forward declarations needed by init()
    inline bool load(const char* name);

    inline void init() {
        wchar_t appdata[MAX_PATH];
        if (FAILED(SHGetFolderPathW(NULL, CSIDL_APPDATA, NULL, 0, appdata))) return;
        wsprintfW(g_dir, L"%s\\IsraeliClient", appdata);
        CreateDirectoryW(g_dir, NULL);
        // Load autoload name from persistent file
        wchar_t al[MAX_PATH]; wsprintfW(al, L"%s\\autoload.txt", g_dir);
        HANDLE h = CreateFileW(al, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, 0, NULL);
        if (h != INVALID_HANDLE_VALUE) {
            DWORD r; ReadFile(h, g_autoload_name, 127, &r, NULL);
            // Trim trailing whitespace/newlines (defensive against external edits)
            while (r > 0 && (g_autoload_name[r-1] == '\n' || g_autoload_name[r-1] == '\r' || g_autoload_name[r-1] == ' '))
                r--;
            g_autoload_name[r] = 0;
            CloseHandle(h);
        }
        // Actually load the auto-load config so variables sync with UI
        if (g_autoload_name[0]) {
            if (load(g_autoload_name))
                wsprintfA(g_status, "Auto-loaded: %s", g_autoload_name);
            g_needs_refresh = true;
        }
    }

    inline config_blob to_blob(const c_variable* v) {
        config_blob b = {};
        #define CP4(dst, src) do { dst[0]=src[0]; dst[1]=src[1]; dst[2]=src[2]; dst[3]=src[3]; } while(0)
        // Preview
        CP4(b.preview_box_c, v->c_preview.box_c); b.preview_box_type = v->c_preview.box_type;
        CP4(b.preview_corner_color, v->c_preview.corner_color);
        b.preview_flags = v->c_preview.flags; b.preview_bars = v->c_preview.bars;
        b.preview_esp = v->c_preview.esp_preview;
        b.preview_texts_spacing = v->c_preview.texts_spacing; b.preview_bars_spacing = v->c_preview.bars_spacing;
        b.preview_box_padding = v->c_preview.box_padding; b.preview_anim_speed = v->c_preview.anim_speed;
        b.dpi = v->c_dpi.dpi;
        // Aimbot
        b.aim_enabled = v->c_aimbot.aimbot; b.aim_fov = v->c_aimbot.fov;
        b.aim_smoothing = v->c_aimbot.smoothing; b.aim_vis = v->c_aimbot.vis_check;
        b.aim_friendly = v->c_aimbot.friendly; b.aim_target_players = v->c_aimbot.target_players;
        b.aim_target_zombies = v->c_aimbot.target_zombies; b.aim_limb = v->c_aimbot.limb_selection;
        b.aim_draw_fov = v->c_aimbot.draw_fov; CP4(b.aim_fov_color, v->c_aimbot.fov_color);
        b.aim_spinbot = v->c_aimbot.spinbot;
        // Silent
        b.silent = v->c_silent.silent; b.silent_chance = v->c_silent.chance;
        b.silent_fov_restrict = v->c_silent.fov_restrict; b.silent_fov = v->c_silent.silent_fov;
        b.silent_max_dist = v->c_silent.max_dist; b.silent_vis = v->c_silent.vis_check;
        b.silent_limb = v->c_silent.limb_selection; b.silent_draw_fov = v->c_silent.draw_fov;
        CP4(b.silent_fov_color, v->c_silent.fov_color); b.silent_target_point = v->c_silent.target_point;
        CP4(b.silent_target_color, v->c_silent.target_color);
        b.silent_target_players = v->c_silent.target_players; b.silent_target_zombies = v->c_silent.target_zombies;
        b.silent_skip_friendly = v->c_silent.skip_friendly; b.silent_bounds = v->c_silent.bounds_exp;
        // Trigger
        b.trigger = v->c_trigger.enable_trigger; b.trigger_delay = v->c_trigger.delay;
        b.trigger_hold = v->c_trigger.hold_fire;
        // Weapon
        b.no_recoil = v->c_weapon.no_recoil; b.no_spread = v->c_weapon.no_spread;
        b.no_sway = v->c_weapon.no_sway; b.rapid_fire = v->c_weapon.rapid_fire;
        b.rapid_mult = v->c_weapon.rapid_mult; b.instant_reload = v->c_weapon.instant_reload;
        b.extended_mag = v->c_weapon.extended_mag; b.mag_mult = v->c_weapon.mag_mult;
        b.extended_melee = v->c_weapon.extended_melee; b.melee_range = v->c_weapon.melee_range;
        b.weapon_chams = v->c_weapon.weapon_chams; CP4(b.weapon_chams_color, v->c_weapon.weapon_chams_color);
        b.hitsound = v->c_weapon.hitsound; b.hitsound_vol = v->c_weapon.hitsound_vol;
        b.hitsound_pitch = v->c_weapon.hitsound_pitch;
        // Player ESP
        b.pesp = v->c_pesp.esp; b.pesp_box = v->c_pesp.box; CP4(b.pesp_box_color, v->c_pesp.box_color);
        b.pesp_name = v->c_pesp.name; CP4(b.pesp_name_color, v->c_pesp.name_color);
        b.pesp_health = v->c_pesp.health; b.pesp_distance = v->c_pesp.distance;
        b.pesp_skeleton = v->c_pesp.skeleton; b.pesp_snapline = v->c_pesp.snapline;
        CP4(b.pesp_snap_color, v->c_pesp.snap_color); b.pesp_fill = v->c_pesp.fill;
        CP4(b.pesp_fill_color, v->c_pesp.fill_color); b.pesp_fill_alpha = v->c_pesp.fill_alpha;
        b.pesp_max_dist = v->c_pesp.max_dist; b.pesp_thickness = v->c_pesp.thickness;
        // Zombie ESP
        b.zesp = v->c_zesp.esp; b.zesp_box = v->c_zesp.box; CP4(b.zesp_box_color, v->c_zesp.box_color);
        b.zesp_name = v->c_zesp.name; b.zesp_health = v->c_zesp.health;
        b.zesp_distance = v->c_zesp.distance; b.zesp_skeleton = v->c_zesp.skeleton;
        b.zesp_snapline = v->c_zesp.snapline; CP4(b.zesp_snap_color, v->c_zesp.snap_color);
        b.zesp_fill = v->c_zesp.fill; CP4(b.zesp_fill_color, v->c_zesp.fill_color);
        b.zesp_max_dist = v->c_zesp.max_dist; b.zesp_thickness = v->c_zesp.thickness;
        // Item ESP
        b.iesp = v->c_iesp.esp; CP4(b.iesp_text_color, v->c_iesp.text_color);
        b.iesp_snapline = v->c_iesp.snapline; CP4(b.iesp_snap_color, v->c_iesp.snap_color);
        b.iesp_max_dist = v->c_iesp.max_dist; b.iesp_filter_weapons = v->c_iesp.filter_weapons;
        b.iesp_filter_ammo = v->c_iesp.filter_ammo; b.iesp_filter_medical = v->c_iesp.filter_medical;
        b.iesp_filter_food = v->c_iesp.filter_food; b.iesp_filter_clothing = v->c_iesp.filter_clothing;
        // Vehicle ESP
        b.vesp = v->c_vesp.esp; b.vesp_box = v->c_vesp.box; CP4(b.vesp_box_color, v->c_vesp.box_color);
        b.vesp_name = v->c_vesp.name; CP4(b.vesp_name_color, v->c_vesp.name_color);
        b.vesp_health = v->c_vesp.health; b.vesp_distance = v->c_vesp.distance;
        b.vesp_locked = v->c_vesp.locked; b.vesp_snapline = v->c_vesp.snapline;
        CP4(b.vesp_snap_color, v->c_vesp.snap_color); b.vesp_fill = v->c_vesp.fill;
        CP4(b.vesp_fill_color, v->c_vesp.fill_color); b.vesp_fill_alpha = v->c_vesp.fill_alpha;
        b.vesp_max_dist = v->c_vesp.max_dist; b.vesp_thickness = v->c_vesp.thickness;
        // Other ESP (legacy blob fields mapped to new per-entity structs)
        b.oesp_storage = v->c_sesp.esp; b.oesp_airdrop = v->c_desp.esp;
        b.oesp_bed = v->c_besp.esp; b.oesp_generator = v->c_gesp.esp;
        b.oesp_turret = v->c_tesp.esp; b.oesp_bullet_tracer = v->c_fesp.esp;
        b.oesp_grenade = v->c_resp.esp; b.oesp_animal = v->c_aesp.esp;
        // Crosshair
        b.cross_enable = v->c_crosshair.enable; b.cross_type = v->c_crosshair.type_selection;
        b.cross_size = v->c_crosshair.size; b.cross_thick = v->c_crosshair.thick;
        CP4(b.cross_color, v->c_crosshair.color); b.cross_reserved = false;
        b.cross_spin = v->c_crosshair.spin; b.cross_spin_speed = v->c_crosshair.spin_speed;
        // Self
        b.custom_fov = v->c_self.custom_fov; b.fov_deg = v->c_self.fov_deg;
        b.no_flash = v->c_self.no_flash; b.no_pain = v->c_self.no_pain;
        b.no_hallucination = v->c_self.no_hallucination; b.no_grayscale = v->c_self.no_grayscale;
        b.no_fog = v->c_self.no_fog; b.night_vision = v->c_self.night_vision;
        b.footsteps = v->c_self.footsteps; CP4(b.footstep_color, v->c_self.footstep_color);
        b.footstep_shape = v->c_self.footstep_shape; b.footstep_spin = v->c_self.footstep_spin;
        b.footstep_spin_spd = v->c_self.footstep_spin_spd; b.footstep_lifetime = v->c_self.footstep_lifetime;
        b.footstep_size = v->c_self.footstep_size;
        b.damage_numbers = v->c_self.damage_numbers; b.chams = v->c_self.chams;
        CP4(b.chams_color, v->c_self.chams_color);
        // World
        b.custom_time = v->c_world.custom_time; b.time_value = v->c_world.time_value;
        b.override_sky = v->c_world.override_sky; CP4(b.sky_color, v->c_world.sky_color);
        b.override_sun = v->c_world.override_sun; CP4(b.sun_color, v->c_world.sun_color);
        b.override_cloud = v->c_world.override_cloud; CP4(b.cloud_color, v->c_world.cloud_color);
        b.force_compass = v->c_world.force_compass; b.force_map = v->c_world.force_map;
        b.map_show_players = v->c_world.map_show_players; b.map_show_markers = v->c_world.map_show_markers;
        // Movement
        b.far_reach = v->c_movement.far_reach; b.reach_dist = v->c_movement.reach_dist;
        b.pickup_walls = v->c_movement.pickup_walls; b.pickup_dist = v->c_movement.pickup_dist;
        b.extend_nearby = v->c_movement.extend_nearby; b.nearby_radius = v->c_movement.nearby_radius;
        b.fly_speed = v->c_movement.fly_speed;
        // Placement
        b.ignore_barricade = v->c_placement.ignore_barricade; b.ignore_structure = v->c_placement.ignore_structure;
        b.place_anywhere = v->c_placement.place_anywhere; b.custom_offset = v->c_placement.custom_offset;
        b.offset_x = v->c_placement.offset_x; b.offset_y = v->c_placement.offset_y; b.offset_z = v->c_placement.offset_z;
        // Fun
        b.freecam_speed = v->c_fun.freecam_speed; b.star_of_david = v->c_fun.star_of_david;
        // Automation
        b.auto_pickup = v->c_automation.auto_pickup; b.pickup_radius = v->c_automation.pickup_radius;
        b.pickup_through_walls = v->c_automation.pickup_through_walls; b.pickup_filter = v->c_automation.pickup_filter;
        b.auto_fish = v->c_automation.auto_fish; b.auto_joiner = v->c_automation.auto_joiner;
        for (int i=0;i<64;i++) b.server_ip[i] = v->c_automation.server_ip[i];
        b.server_port = v->c_automation.server_port;
        // Appearance
        CP4(b.accent_color, v->c_appearance.accent_color); b.font_size = v->c_appearance.font_size;
        b.watermark_on = v->c_appearance.watermark; b.info_bar = v->c_appearance.info_bar;
        b.accent_enabled = v->c_appearance.accent_enabled; b.rgb_mode = v->c_appearance.rgb_mode;
        b.rgb_speed = v->c_appearance.rgb_speed;
        // Anti-spy
        b.spy_mode = v->c_antispy.spy_mode;
        // Keybinds
        b.kb_aimbot = v->c_keybinds.aimbot_key; b.kb_silent = v->c_keybinds.silent_key;
        b.kb_trigger = v->c_keybinds.trigger_key; b.kb_freecam = v->c_keybinds.freecam_key;
        b.kb_vfly = v->c_keybinds.vfly_key; b.kb_vdmg = v->c_keybinds.vdmg_key;
        // ---- v2 ESP fields ----
        b.esp_corner_mode = v->c_esp.corner_mode; b.esp_box3d = v->c_esp.box3d;
        b.pesp_weapon = v->c_pesp.weapon; CP4(b.pesp_weapon_color, v->c_pesp.weapon_color);
        CP4(b.pesp_skel_color, v->c_pesp.skel_color);
        CP4(b.zesp_name_color, v->c_zesp.name_color); CP4(b.zesp_skel_color, v->c_zesp.skel_color);
        b.zesp_fill_alpha = v->c_zesp.fill_alpha;
        b.iesp_clump = v->c_iesp.clump;
        CP4(b.vesp_name_color_v2, v->c_vesp.name_color);
        // Animal
        b.aesp = v->c_aesp.esp; b.aesp_box = v->c_aesp.box; CP4(b.aesp_box_color, v->c_aesp.box_color);
        b.aesp_name = v->c_aesp.name; CP4(b.aesp_name_color, v->c_aesp.name_color);
        b.aesp_distance = v->c_aesp.distance; b.aesp_snapline = v->c_aesp.snapline;
        CP4(b.aesp_snap_color, v->c_aesp.snap_color); b.aesp_fill = v->c_aesp.fill;
        CP4(b.aesp_fill_color, v->c_aesp.fill_color); b.aesp_fill_alpha = v->c_aesp.fill_alpha;
        b.aesp_max_dist = v->c_aesp.max_dist; b.aesp_thickness = v->c_aesp.thickness;
        // Storage
        b.sesp = v->c_sesp.esp; b.sesp_box = v->c_sesp.box; CP4(b.sesp_box_color, v->c_sesp.box_color);
        b.sesp_name = v->c_sesp.name; CP4(b.sesp_name_color, v->c_sesp.name_color);
        b.sesp_health = v->c_sesp.health; b.sesp_distance = v->c_sesp.distance;
        b.sesp_snapline = v->c_sesp.snapline; CP4(b.sesp_snap_color, v->c_sesp.snap_color);
        b.sesp_fill = v->c_sesp.fill; CP4(b.sesp_fill_color, v->c_sesp.fill_color);
        b.sesp_fill_alpha = v->c_sesp.fill_alpha; b.sesp_max_dist = v->c_sesp.max_dist;
        b.sesp_thickness = v->c_sesp.thickness;
        // Airdrop
        b.desp = v->c_desp.esp; b.desp_box = v->c_desp.box; CP4(b.desp_box_color, v->c_desp.box_color);
        b.desp_name = v->c_desp.name; CP4(b.desp_name_color, v->c_desp.name_color);
        b.desp_distance = v->c_desp.distance; b.desp_snapline = v->c_desp.snapline;
        CP4(b.desp_snap_color, v->c_desp.snap_color); b.desp_fill = v->c_desp.fill;
        CP4(b.desp_fill_color, v->c_desp.fill_color); b.desp_fill_alpha = v->c_desp.fill_alpha;
        b.desp_max_dist = v->c_desp.max_dist; b.desp_thickness = v->c_desp.thickness;
        // Bed
        b.besp = v->c_besp.esp; b.besp_box = v->c_besp.box; CP4(b.besp_box_color, v->c_besp.box_color);
        b.besp_name = v->c_besp.name; CP4(b.besp_name_color, v->c_besp.name_color);
        b.besp_distance = v->c_besp.distance; b.besp_claimed_only = v->c_besp.claimed_only;
        b.besp_show_claimed = v->c_besp.show_claimed; b.besp_snapline = v->c_besp.snapline;
        CP4(b.besp_snap_color, v->c_besp.snap_color); b.besp_fill = v->c_besp.fill;
        CP4(b.besp_fill_color, v->c_besp.fill_color); b.besp_fill_alpha = v->c_besp.fill_alpha;
        b.besp_max_dist = v->c_besp.max_dist; b.besp_thickness = v->c_besp.thickness;
        // Generator
        b.gesp = v->c_gesp.esp; b.gesp_box = v->c_gesp.box; CP4(b.gesp_box_color, v->c_gesp.box_color);
        b.gesp_name = v->c_gesp.name; CP4(b.gesp_name_color, v->c_gesp.name_color);
        b.gesp_distance = v->c_gesp.distance; b.gesp_fuel = v->c_gesp.fuel;
        b.gesp_snapline = v->c_gesp.snapline; CP4(b.gesp_snap_color, v->c_gesp.snap_color);
        b.gesp_fill = v->c_gesp.fill; CP4(b.gesp_fill_color, v->c_gesp.fill_color);
        b.gesp_fill_alpha = v->c_gesp.fill_alpha; b.gesp_max_dist = v->c_gesp.max_dist;
        b.gesp_thickness = v->c_gesp.thickness;
        // Turret
        b.tesp = v->c_tesp.esp; b.tesp_box = v->c_tesp.box; CP4(b.tesp_box_color, v->c_tesp.box_color);
        b.tesp_name = v->c_tesp.name; CP4(b.tesp_name_color, v->c_tesp.name_color);
        b.tesp_distance = v->c_tesp.distance; b.tesp_state = v->c_tesp.state;
        b.tesp_snapline = v->c_tesp.snapline; CP4(b.tesp_snap_color, v->c_tesp.snap_color);
        b.tesp_fill = v->c_tesp.fill; CP4(b.tesp_fill_color, v->c_tesp.fill_color);
        b.tesp_fill_alpha = v->c_tesp.fill_alpha; b.tesp_max_dist = v->c_tesp.max_dist;
        b.tesp_thickness = v->c_tesp.thickness;
        // Grenade
        b.resp = v->c_resp.esp; b.resp_box = v->c_resp.box; CP4(b.resp_box_color, v->c_resp.box_color);
        b.resp_name = v->c_resp.name; CP4(b.resp_name_color, v->c_resp.name_color);
        b.resp_radius = v->c_resp.radius; CP4(b.resp_rad_color, v->c_resp.rad_color);
        b.resp_snapline = v->c_resp.snapline; CP4(b.resp_snap_color, v->c_resp.snap_color);
        b.resp_max_dist = v->c_resp.max_dist; b.resp_thickness = v->c_resp.thickness;
        // Bullet Tracers
        b.fesp = v->c_fesp.esp; b.fesp_self = v->c_fesp.self; b.fesp_others = v->c_fesp.others;
        b.fesp_trail = v->c_fesp.trail; CP4(b.fesp_self_color, v->c_fesp.self_color);
        CP4(b.fesp_other_color, v->c_fesp.other_color); b.fesp_snapline = v->c_fesp.snapline;
        CP4(b.fesp_snap_color, v->c_fesp.snap_color); b.fesp_max_dist = v->c_fesp.max_dist;
        b.fesp_lifetime = v->c_fesp.lifetime;
        // Chams v3
        b.chams_pattern = v->c_self.chams_pattern;
        CP4(b.chams_vis_color, v->c_self.chams_vis_color);
        CP4(b.chams_nonvis_color, v->c_self.chams_nonvis_color);
        b.chams_self = v->c_self.self_chams;
        CP4(b.chams_self_color, v->c_self.self_chams_color);
        // v4
        b.outline = v->c_self.outline;
        CP4(b.outline_color, v->c_self.outline_color);
        CP4(b.chams_wire_color, v->c_self.chams_wire_color);
        CP4(b.chams_wire_behind_color, v->c_self.chams_wire_behind_color);
        b.player_chams = v->c_self.player_chams;
        b.zombie_chams = v->c_self.zombie_chams;
        b.self_chams = v->c_self.self_chams;
        CP4(b.self_chams_color, v->c_self.self_chams_color);
        // v5 1.1-only features
        b.spin_type = v->c_aimbot.spin_type; b.spin_show = v->c_aimbot.spin_show;
        b.auto_semi_burst = v->c_weapon.auto_semi_burst; b.damage_flinch_mult = v->c_weapon.damage_flinch_mult;
        b.disable_bino = v->c_weapon.disable_bino; b.disable_scope = v->c_weapon.disable_scope;
        b.extend_ballistic_range = v->c_weapon.extend_ballistic_range; b.extra_ballistic_steps = v->c_weapon.extra_ballistic_steps;
        b.ignore_leave_timer = v->c_weapon.ignore_leave_timer; b.instant_aim = v->c_weapon.instant_aim;
        b.no_ballistics = v->c_weapon.no_ballistics; b.force_headshot = v->c_weapon.force_headshot;
        b.override_cloud_rim = v->c_world.override_cloud_rim; CP4(b.cloud_rim_color, v->c_world.cloud_rim_color);
        b.unlock_perspective = v->c_self.unlock_perspective;
        CP4(b.friend_color, v->c_misc.friend_color); CP4(b.enemy_color, v->c_misc.enemy_color);
        b.nearby_through_walls = v->c_movement.nearby_through_walls; b.salvage_multiplier = v->c_placement.salvage_multiplier;
        b.auto_fish_delay = v->c_automation.auto_fish_delay; b.auto_forge = v->c_automation.auto_forge;
        b.auto_forge_radius = v->c_automation.auto_forge_radius; b.auto_forge_delay = v->c_automation.auto_forge_delay;
        b.auto_join_limit = v->c_automation.auto_join_limit; b.auto_join_max = v->c_automation.auto_join_max;
        b.auto_join_retry = v->c_automation.auto_join_retry; b.auto_pickup_speed = v->c_automation.auto_pickup_speed;
        b.auto_pickup_skip_empty = v->c_automation.auto_pickup_skip_empty;
        b.misc_item_spawner = v->c_misc.item_spawner; b.misc_entity_inspector = v->c_misc.entity_inspector;
        b.misc_storage_viewer = v->c_misc.storage_viewer; b.misc_hwid_changer = v->c_misc.hwid_changer;
        b.spy_toast = v->c_antispy.spy_toast;
        b.vehicle_fly_style = v->c_movement.vehicle_fly_style;
        b.af_on = v->c_autofarm.auto_farm_on; b.af_radius = v->c_autofarm.auto_farm_radius;
        b.af_delay = v->c_autofarm.auto_farm_delay; b.af_replant = v->c_autofarm.auto_farm_replant;
        b.af_store = v->c_autofarm.auto_farm_store; b.af_craft = v->c_autofarm.auto_farm_craft;
        b.af_equip_seed = v->c_autofarm.auto_farm_equip_seed; b.af_water = v->c_autofarm.auto_farm_water;
        b.q_max_ml = v->c_quest.max_ml; b.q_alcohol = v->c_quest.alcohol;
        b.q_lumberjack = v->c_quest.lumberjack; b.q_voucher = v->c_quest.voucher;
        b.q_xmas2024 = v->c_quest.xmas2024; b.q_xmas2025_factory = v->c_quest.xmas2025_factory;
        b.q_xmas2025_penguins = v->c_quest.xmas2025_penguins;
        #undef CP4
        return b;
    }

    inline void from_blob(c_variable* v, const config_blob& b) {
        #define CP4(dst, src) do { dst[0]=src[0]; dst[1]=src[1]; dst[2]=src[2]; dst[3]=src[3]; } while(0)
        CP4(v->c_preview.box_c, b.preview_box_c); v->c_preview.box_type = b.preview_box_type;
        CP4(v->c_preview.corner_color, b.preview_corner_color);
        v->c_preview.flags = b.preview_flags; v->c_preview.bars = b.preview_bars;
        v->c_preview.esp_preview = b.preview_esp;
        v->c_preview.texts_spacing = b.preview_texts_spacing; v->c_preview.bars_spacing = b.preview_bars_spacing;
        v->c_preview.box_padding = b.preview_box_padding; v->c_preview.anim_speed = b.preview_anim_speed;
        v->c_dpi.dpi = b.dpi; v->c_dpi.dpi_saved = b.dpi; v->c_dpi.dpi_changed = true;
        v->c_aimbot.aimbot = b.aim_enabled; v->c_aimbot.fov = b.aim_fov;
        v->c_aimbot.smoothing = b.aim_smoothing; v->c_aimbot.vis_check = b.aim_vis;
        v->c_aimbot.friendly = b.aim_friendly; v->c_aimbot.target_players = b.aim_target_players;
        v->c_aimbot.target_zombies = b.aim_target_zombies; v->c_aimbot.limb_selection = b.aim_limb;
        v->c_aimbot.draw_fov = b.aim_draw_fov; CP4(v->c_aimbot.fov_color, b.aim_fov_color);
        v->c_aimbot.spinbot = b.aim_spinbot;
        v->c_silent.silent = b.silent; v->c_silent.chance = b.silent_chance;
        v->c_silent.fov_restrict = b.silent_fov_restrict; v->c_silent.silent_fov = b.silent_fov;
        v->c_silent.max_dist = b.silent_max_dist; v->c_silent.vis_check = b.silent_vis;
        v->c_silent.limb_selection = b.silent_limb; v->c_silent.draw_fov = b.silent_draw_fov;
        CP4(v->c_silent.fov_color, b.silent_fov_color); v->c_silent.target_point = b.silent_target_point;
        CP4(v->c_silent.target_color, b.silent_target_color);
        v->c_silent.target_players = b.silent_target_players; v->c_silent.target_zombies = b.silent_target_zombies;
        v->c_silent.skip_friendly = b.silent_skip_friendly; v->c_silent.bounds_exp = b.silent_bounds;
        v->c_trigger.enable_trigger = b.trigger; v->c_trigger.delay = b.trigger_delay;
        v->c_trigger.hold_fire = b.trigger_hold;
        v->c_weapon.no_recoil = b.no_recoil; v->c_weapon.no_spread = b.no_spread;
        v->c_weapon.no_sway = b.no_sway; v->c_weapon.rapid_fire = b.rapid_fire;
        v->c_weapon.rapid_mult = b.rapid_mult; v->c_weapon.instant_reload = b.instant_reload;
        v->c_weapon.extended_mag = b.extended_mag; v->c_weapon.mag_mult = b.mag_mult;
        v->c_weapon.extended_melee = b.extended_melee; v->c_weapon.melee_range = b.melee_range;
        v->c_weapon.weapon_chams = b.weapon_chams; CP4(v->c_weapon.weapon_chams_color, b.weapon_chams_color);
        v->c_weapon.hitsound = b.hitsound; v->c_weapon.hitsound_vol = b.hitsound_vol;
        v->c_weapon.hitsound_pitch = b.hitsound_pitch;
        v->c_pesp.esp = b.pesp; v->c_pesp.box = b.pesp_box; CP4(v->c_pesp.box_color, b.pesp_box_color);
        v->c_pesp.name = b.pesp_name; CP4(v->c_pesp.name_color, b.pesp_name_color);
        v->c_pesp.health = b.pesp_health; v->c_pesp.distance = b.pesp_distance;
        v->c_pesp.skeleton = b.pesp_skeleton; v->c_pesp.snapline = b.pesp_snapline;
        CP4(v->c_pesp.snap_color, b.pesp_snap_color); v->c_pesp.fill = b.pesp_fill;
        CP4(v->c_pesp.fill_color, b.pesp_fill_color); v->c_pesp.fill_alpha = b.pesp_fill_alpha;
        v->c_pesp.max_dist = b.pesp_max_dist; v->c_pesp.thickness = b.pesp_thickness;
        v->c_zesp.esp = b.zesp; v->c_zesp.box = b.zesp_box; CP4(v->c_zesp.box_color, b.zesp_box_color);
        v->c_zesp.name = b.zesp_name; v->c_zesp.health = b.zesp_health;
        v->c_zesp.distance = b.zesp_distance; v->c_zesp.skeleton = b.zesp_skeleton;
        v->c_zesp.snapline = b.zesp_snapline; CP4(v->c_zesp.snap_color, b.zesp_snap_color);
        v->c_zesp.fill = b.zesp_fill; CP4(v->c_zesp.fill_color, b.zesp_fill_color);
        v->c_zesp.max_dist = b.zesp_max_dist; v->c_zesp.thickness = b.zesp_thickness;
        v->c_iesp.esp = b.iesp; CP4(v->c_iesp.text_color, b.iesp_text_color);
        v->c_iesp.snapline = b.iesp_snapline; CP4(v->c_iesp.snap_color, b.iesp_snap_color);
        v->c_iesp.max_dist = b.iesp_max_dist; v->c_iesp.filter_weapons = b.iesp_filter_weapons;
        v->c_iesp.filter_ammo = b.iesp_filter_ammo; v->c_iesp.filter_medical = b.iesp_filter_medical;
        v->c_iesp.filter_food = b.iesp_filter_food; v->c_iesp.filter_clothing = b.iesp_filter_clothing;
        v->c_vesp.esp = b.vesp; v->c_vesp.box = b.vesp_box; CP4(v->c_vesp.box_color, b.vesp_box_color);
        v->c_vesp.name = b.vesp_name; CP4(v->c_vesp.name_color, b.vesp_name_color);
        v->c_vesp.health = b.vesp_health; v->c_vesp.distance = b.vesp_distance;
        v->c_vesp.locked = b.vesp_locked; v->c_vesp.snapline = b.vesp_snapline;
        CP4(v->c_vesp.snap_color, b.vesp_snap_color); v->c_vesp.fill = b.vesp_fill;
        CP4(v->c_vesp.fill_color, b.vesp_fill_color); v->c_vesp.fill_alpha = b.vesp_fill_alpha;
        v->c_vesp.max_dist = b.vesp_max_dist; v->c_vesp.thickness = b.vesp_thickness;
        // Other ESP (legacy blob fields mapped to new per-entity structs)
        v->c_sesp.esp = b.oesp_storage; v->c_desp.esp = b.oesp_airdrop;
        v->c_besp.esp = b.oesp_bed; v->c_gesp.esp = b.oesp_generator;
        v->c_tesp.esp = b.oesp_turret; v->c_fesp.esp = b.oesp_bullet_tracer;
        v->c_resp.esp = b.oesp_grenade; v->c_aesp.esp = b.oesp_animal;
        v->c_crosshair.enable = b.cross_enable; v->c_crosshair.type_selection = b.cross_type;
        v->c_crosshair.size = b.cross_size; v->c_crosshair.thick = b.cross_thick;
        CP4(v->c_crosshair.color, b.cross_color);
        v->c_crosshair.spin = b.cross_spin; v->c_crosshair.spin_speed = b.cross_spin_speed;
        v->c_self.custom_fov = b.custom_fov; v->c_self.fov_deg = b.fov_deg;
        v->c_self.no_flash = b.no_flash; v->c_self.no_pain = b.no_pain;
        v->c_self.no_hallucination = b.no_hallucination; v->c_self.no_grayscale = b.no_grayscale;
        v->c_self.no_fog = b.no_fog; v->c_self.night_vision = b.night_vision;
        v->c_self.footsteps = b.footsteps; CP4(v->c_self.footstep_color, b.footstep_color);
        v->c_self.footstep_shape = b.footstep_shape; v->c_self.footstep_spin = b.footstep_spin;
        v->c_self.footstep_spin_spd = b.footstep_spin_spd; v->c_self.footstep_lifetime = b.footstep_lifetime;
        v->c_self.footstep_size = b.footstep_size;
        v->c_self.damage_numbers = b.damage_numbers; v->c_self.chams = b.chams;
        CP4(v->c_self.chams_color, b.chams_color);
        v->c_world.custom_time = b.custom_time; v->c_world.time_value = b.time_value;
        v->c_world.override_sky = b.override_sky; CP4(v->c_world.sky_color, b.sky_color);
        v->c_world.override_sun = b.override_sun; CP4(v->c_world.sun_color, b.sun_color);
        v->c_world.override_cloud = b.override_cloud; CP4(v->c_world.cloud_color, b.cloud_color);
        v->c_world.force_compass = b.force_compass; v->c_world.force_map = b.force_map;
        v->c_world.map_show_players = b.map_show_players; v->c_world.map_show_markers = b.map_show_markers;
        v->c_movement.far_reach = b.far_reach; v->c_movement.reach_dist = b.reach_dist;
        v->c_movement.pickup_walls = b.pickup_walls; v->c_movement.pickup_dist = b.pickup_dist;
        v->c_movement.extend_nearby = b.extend_nearby; v->c_movement.nearby_radius = b.nearby_radius;
        v->c_movement.fly_speed = b.fly_speed;
        v->c_placement.ignore_barricade = b.ignore_barricade; v->c_placement.ignore_structure = b.ignore_structure;
        v->c_placement.place_anywhere = b.place_anywhere; v->c_placement.custom_offset = b.custom_offset;
        v->c_placement.offset_x = b.offset_x; v->c_placement.offset_y = b.offset_y; v->c_placement.offset_z = b.offset_z;
        v->c_fun.freecam_speed = b.freecam_speed; v->c_fun.star_of_david = b.star_of_david;
        v->c_automation.auto_pickup = b.auto_pickup; v->c_automation.pickup_radius = b.pickup_radius;
        v->c_automation.pickup_through_walls = b.pickup_through_walls; v->c_automation.pickup_filter = b.pickup_filter;
        v->c_automation.auto_fish = b.auto_fish; v->c_automation.auto_joiner = b.auto_joiner;
        for (int i=0;i<64;i++) v->c_automation.server_ip[i] = b.server_ip[i];
        v->c_automation.server_port = b.server_port;
        CP4(v->c_appearance.accent_color, b.accent_color); v->c_appearance.font_size = b.font_size;
        v->c_appearance.watermark = b.watermark_on; v->c_appearance.info_bar = b.info_bar;
        v->c_appearance.accent_enabled = b.accent_enabled; v->c_appearance.rgb_mode = b.rgb_mode;
        v->c_appearance.rgb_speed = b.rgb_speed;
        v->c_antispy.spy_mode = b.spy_mode;
        v->c_keybinds.aimbot_key = b.kb_aimbot; v->c_keybinds.silent_key = b.kb_silent;
        v->c_keybinds.trigger_key = b.kb_trigger; v->c_keybinds.freecam_key = b.kb_freecam;
        v->c_keybinds.vfly_key = b.kb_vfly; v->c_keybinds.vdmg_key = b.kb_vdmg;
        // ---- v2 ESP fields ----
        v->c_esp.corner_mode = b.esp_corner_mode; v->c_esp.box3d = b.esp_box3d;
        v->c_pesp.weapon = b.pesp_weapon; CP4(v->c_pesp.weapon_color, b.pesp_weapon_color);
        CP4(v->c_pesp.skel_color, b.pesp_skel_color);
        CP4(v->c_zesp.name_color, b.zesp_name_color); CP4(v->c_zesp.skel_color, b.zesp_skel_color);
        v->c_zesp.fill_alpha = b.zesp_fill_alpha;
        v->c_iesp.clump = b.iesp_clump;
        CP4(v->c_vesp.name_color, b.vesp_name_color_v2);
        // Animal
        v->c_aesp.esp = b.aesp; v->c_aesp.box = b.aesp_box; CP4(v->c_aesp.box_color, b.aesp_box_color);
        v->c_aesp.name = b.aesp_name; CP4(v->c_aesp.name_color, b.aesp_name_color);
        v->c_aesp.distance = b.aesp_distance; v->c_aesp.snapline = b.aesp_snapline;
        CP4(v->c_aesp.snap_color, b.aesp_snap_color); v->c_aesp.fill = b.aesp_fill;
        CP4(v->c_aesp.fill_color, b.aesp_fill_color); v->c_aesp.fill_alpha = b.aesp_fill_alpha;
        v->c_aesp.max_dist = b.aesp_max_dist; v->c_aesp.thickness = b.aesp_thickness;
        // Storage
        v->c_sesp.esp = b.sesp; v->c_sesp.box = b.sesp_box; CP4(v->c_sesp.box_color, b.sesp_box_color);
        v->c_sesp.name = b.sesp_name; CP4(v->c_sesp.name_color, b.sesp_name_color);
        v->c_sesp.health = b.sesp_health; v->c_sesp.distance = b.sesp_distance;
        v->c_sesp.snapline = b.sesp_snapline; CP4(v->c_sesp.snap_color, b.sesp_snap_color);
        v->c_sesp.fill = b.sesp_fill; CP4(v->c_sesp.fill_color, b.sesp_fill_color);
        v->c_sesp.fill_alpha = b.sesp_fill_alpha; v->c_sesp.max_dist = b.sesp_max_dist;
        v->c_sesp.thickness = b.sesp_thickness;
        // Airdrop
        v->c_desp.esp = b.desp; v->c_desp.box = b.desp_box; CP4(v->c_desp.box_color, b.desp_box_color);
        v->c_desp.name = b.desp_name; CP4(v->c_desp.name_color, b.desp_name_color);
        v->c_desp.distance = b.desp_distance; v->c_desp.snapline = b.desp_snapline;
        CP4(v->c_desp.snap_color, b.desp_snap_color); v->c_desp.fill = b.desp_fill;
        CP4(v->c_desp.fill_color, b.desp_fill_color); v->c_desp.fill_alpha = b.desp_fill_alpha;
        v->c_desp.max_dist = b.desp_max_dist; v->c_desp.thickness = b.desp_thickness;
        // Bed
        v->c_besp.esp = b.besp; v->c_besp.box = b.besp_box; CP4(v->c_besp.box_color, b.besp_box_color);
        v->c_besp.name = b.besp_name; CP4(v->c_besp.name_color, b.besp_name_color);
        v->c_besp.distance = b.besp_distance; v->c_besp.claimed_only = b.besp_claimed_only;
        v->c_besp.show_claimed = b.besp_show_claimed; v->c_besp.snapline = b.besp_snapline;
        CP4(v->c_besp.snap_color, b.besp_snap_color); v->c_besp.fill = b.besp_fill;
        CP4(v->c_besp.fill_color, b.besp_fill_color); v->c_besp.fill_alpha = b.besp_fill_alpha;
        v->c_besp.max_dist = b.besp_max_dist; v->c_besp.thickness = b.besp_thickness;
        // Generator
        v->c_gesp.esp = b.gesp; v->c_gesp.box = b.gesp_box; CP4(v->c_gesp.box_color, b.gesp_box_color);
        v->c_gesp.name = b.gesp_name; CP4(v->c_gesp.name_color, b.gesp_name_color);
        v->c_gesp.distance = b.gesp_distance; v->c_gesp.fuel = b.gesp_fuel;
        v->c_gesp.snapline = b.gesp_snapline; CP4(v->c_gesp.snap_color, b.gesp_snap_color);
        v->c_gesp.fill = b.gesp_fill; CP4(v->c_gesp.fill_color, b.gesp_fill_color);
        v->c_gesp.fill_alpha = b.gesp_fill_alpha; v->c_gesp.max_dist = b.gesp_max_dist;
        v->c_gesp.thickness = b.gesp_thickness;
        // Turret
        v->c_tesp.esp = b.tesp; v->c_tesp.box = b.tesp_box; CP4(v->c_tesp.box_color, b.tesp_box_color);
        v->c_tesp.name = b.tesp_name; CP4(v->c_tesp.name_color, b.tesp_name_color);
        v->c_tesp.distance = b.tesp_distance; v->c_tesp.state = b.tesp_state;
        v->c_tesp.snapline = b.tesp_snapline; CP4(v->c_tesp.snap_color, b.tesp_snap_color);
        v->c_tesp.fill = b.tesp_fill; CP4(v->c_tesp.fill_color, b.tesp_fill_color);
        v->c_tesp.fill_alpha = b.tesp_fill_alpha; v->c_tesp.max_dist = b.tesp_max_dist;
        v->c_tesp.thickness = b.tesp_thickness;
        // Grenade
        v->c_resp.esp = b.resp; v->c_resp.box = b.resp_box; CP4(v->c_resp.box_color, b.resp_box_color);
        v->c_resp.name = b.resp_name; CP4(v->c_resp.name_color, b.resp_name_color);
        v->c_resp.radius = b.resp_radius; CP4(v->c_resp.rad_color, b.resp_rad_color);
        v->c_resp.snapline = b.resp_snapline; CP4(v->c_resp.snap_color, b.resp_snap_color);
        v->c_resp.max_dist = b.resp_max_dist; v->c_resp.thickness = b.resp_thickness;
        // Bullet Tracers
        v->c_fesp.esp = b.fesp; v->c_fesp.self = b.fesp_self; v->c_fesp.others = b.fesp_others;
        v->c_fesp.trail = b.fesp_trail; CP4(v->c_fesp.self_color, b.fesp_self_color);
        CP4(v->c_fesp.other_color, b.fesp_other_color); v->c_fesp.snapline = b.fesp_snapline;
        CP4(v->c_fesp.snap_color, b.fesp_snap_color); v->c_fesp.max_dist = b.fesp_max_dist;
        v->c_fesp.lifetime = b.fesp_lifetime;
        // Chams v3
        v->c_self.chams_pattern = b.chams_pattern;
        CP4(v->c_self.chams_vis_color, b.chams_vis_color);
        CP4(v->c_self.chams_nonvis_color, b.chams_nonvis_color);
        v->c_self.self_chams = b.chams_self;
        CP4(v->c_self.self_chams_color, b.chams_self_color);
        // v4
        v->c_self.outline = b.outline;
        CP4(v->c_self.outline_color, b.outline_color);
        CP4(v->c_self.chams_wire_color, b.chams_wire_color);
        CP4(v->c_self.chams_wire_behind_color, b.chams_wire_behind_color);
        v->c_self.player_chams = b.player_chams;
        v->c_self.zombie_chams = b.zombie_chams;
        v->c_self.self_chams = b.self_chams;
        CP4(v->c_self.self_chams_color, b.self_chams_color);
        // v5 1.1-only features
        v->c_aimbot.spin_type = b.spin_type; v->c_aimbot.spin_show = b.spin_show;
        v->c_weapon.auto_semi_burst = b.auto_semi_burst; v->c_weapon.damage_flinch_mult = b.damage_flinch_mult;
        v->c_weapon.disable_bino = b.disable_bino; v->c_weapon.disable_scope = b.disable_scope;
        v->c_weapon.extend_ballistic_range = b.extend_ballistic_range; v->c_weapon.extra_ballistic_steps = b.extra_ballistic_steps;
        v->c_weapon.ignore_leave_timer = b.ignore_leave_timer; v->c_weapon.instant_aim = b.instant_aim;
        v->c_weapon.no_ballistics = b.no_ballistics; v->c_weapon.force_headshot = b.force_headshot;
        v->c_world.override_cloud_rim = b.override_cloud_rim; CP4(v->c_world.cloud_rim_color, b.cloud_rim_color);
        v->c_self.unlock_perspective = b.unlock_perspective;
        CP4(v->c_misc.friend_color, b.friend_color); CP4(v->c_misc.enemy_color, b.enemy_color);
        v->c_movement.nearby_through_walls = b.nearby_through_walls; v->c_placement.salvage_multiplier = b.salvage_multiplier;
        v->c_automation.auto_fish_delay = b.auto_fish_delay; v->c_automation.auto_forge = b.auto_forge;
        v->c_automation.auto_forge_radius = b.auto_forge_radius; v->c_automation.auto_forge_delay = b.auto_forge_delay;
        v->c_automation.auto_join_limit = b.auto_join_limit; v->c_automation.auto_join_max = b.auto_join_max;
        v->c_automation.auto_join_retry = b.auto_join_retry; v->c_automation.auto_pickup_speed = b.auto_pickup_speed;
        v->c_automation.auto_pickup_skip_empty = b.auto_pickup_skip_empty;
        v->c_misc.item_spawner = b.misc_item_spawner; v->c_misc.entity_inspector = b.misc_entity_inspector;
        v->c_misc.storage_viewer = b.misc_storage_viewer; v->c_misc.hwid_changer = b.misc_hwid_changer;
        v->c_antispy.spy_toast = b.spy_toast;
        v->c_movement.vehicle_fly_style = b.vehicle_fly_style;
        v->c_autofarm.auto_farm_on = b.af_on; v->c_autofarm.auto_farm_radius = b.af_radius;
        v->c_autofarm.auto_farm_delay = b.af_delay; v->c_autofarm.auto_farm_replant = b.af_replant;
        v->c_autofarm.auto_farm_store = b.af_store; v->c_autofarm.auto_farm_craft = b.af_craft;
        v->c_autofarm.auto_farm_equip_seed = b.af_equip_seed; v->c_autofarm.auto_farm_water = b.af_water;
        v->c_quest.max_ml = b.q_max_ml; v->c_quest.alcohol = b.q_alcohol;
        v->c_quest.lumberjack = b.q_lumberjack; v->c_quest.voucher = b.q_voucher;
        v->c_quest.xmas2024 = b.q_xmas2024; v->c_quest.xmas2025_factory = b.q_xmas2025_factory;
        v->c_quest.xmas2025_penguins = b.q_xmas2025_penguins;
        #undef CP4
    }

    inline bool save(const char* name) {
        wchar_t path[MAX_PATH]; char wn[130];
        wsprintfA(wn, "%s.cfg", name);
        wchar_t wname[130]; MultiByteToWideChar(CP_UTF8, 0, wn, -1, wname, 130);
        wsprintfW(path, L"%s\\%s", g_dir, wname);
        config_blob b = to_blob(var);
        HANDLE h = CreateFileW(path, GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
        if (h == INVALID_HANDLE_VALUE) { wsprintfA(g_status, "Save failed"); return false; }
        DWORD w; WriteFile(h, &b, sizeof(b), &w, NULL);
        CloseHandle(h);
        wsprintfA(g_status, "Saved: %s", name);
        return true;
    }

    inline bool load(const char* name) {
        wchar_t path[MAX_PATH]; char wn[130];
        wsprintfA(wn, "%s.cfg", name);
        wchar_t wname[130]; MultiByteToWideChar(CP_UTF8, 0, wn, -1, wname, 130);
        wsprintfW(path, L"%s\\%s", g_dir, wname);
        HANDLE h = CreateFileW(path, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, 0, NULL);
        if (h == INVALID_HANDLE_VALUE) { wsprintfA(g_status, "Load failed"); return false; }
        config_blob b; DWORD r;
        ReadFile(h, &b, sizeof(b), &r, NULL);
        CloseHandle(h);
        if (r != sizeof(b) || b.version != 1) { wsprintfA(g_status, "Invalid config"); return false; }
        from_blob(var, b);
        wsprintfA(g_status, "Loaded: %s", name);
        return true;
    }

    inline bool remove(const char* name) {
        wchar_t path[MAX_PATH]; char wn[130];
        wsprintfA(wn, "%s.cfg", name);
        wchar_t wname[130]; MultiByteToWideChar(CP_UTF8, 0, wn, -1, wname, 130);
        wsprintfW(path, L"%s\\%s", g_dir, wname);
        if (!DeleteFileW(path)) { wsprintfA(g_status, "Delete failed"); return false; }
        wsprintfA(g_status, "Deleted: %s", name);
        return true;
    }

    inline void refresh_list() {
        var->c_config.data.clear();
        wchar_t search[MAX_PATH]; wsprintfW(search, L"%s\\*.cfg", g_dir);
        WIN32_FIND_DATAW fd;
        HANDLE hf = FindFirstFileW(search, &fd);
        if (hf == INVALID_HANDLE_VALUE) return;
        do {
            config_data cd;
            char name_a[260]; WideCharToMultiByte(CP_UTF8, 0, fd.cFileName, -1, name_a, 260, NULL, NULL);
            // strip .cfg
            int len = lstrlenA(name_a);
            if (len > 4) name_a[len - 4] = 0;
            cd.name = name_a;
            // date from file time
            SYSTEMTIME st; FileTimeToSystemTime(&fd.ftLastWriteTime, &st);
            char date[32]; wsprintfA(date, "%02d/%02d/%04d", st.wMonth, st.wDay, st.wYear);
            cd.date = date;
            var->c_config.data.push_back(cd);
        } while (FindNextFileW(hf, &fd));
        FindClose(hf);
    }

    inline void set_autoload(const char* name) {
        wchar_t al[MAX_PATH]; wsprintfW(al, L"%s\\autoload.txt", g_dir);
        HANDLE h = CreateFileW(al, GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
        if (h != INVALID_HANDLE_VALUE) {
            DWORD w; WriteFile(h, name, lstrlenA(name), &w, NULL);
            CloseHandle(h);
        }
        lstrcpynA(g_autoload_name, name, 128);
        wsprintfA(g_status, "Auto-load: %s", name);
    }

    inline void clear_autoload() {
        wchar_t al[MAX_PATH]; wsprintfW(al, L"%s\\autoload.txt", g_dir);
        DeleteFileW(al);
        g_autoload_name[0] = 0;
        wsprintfA(g_status, "Auto-load cleared");
    }

    inline void autoload_startup() {
        if (g_autoload_name[0]) load(g_autoload_name);
    }
}
#endif
