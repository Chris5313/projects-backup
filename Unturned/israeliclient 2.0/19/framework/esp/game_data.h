#pragma once
// Game data extraction via embedded mono — Unturned (SDG.Unturned namespace)
// 1:1 port of the entity layer behind IsraeliClient 1.0's ESP.cs.
//
// Requires mono_bridge.h. All mono calls run on the render thread (attached).

#include "mono_bridge.h"
#include <string.h>
#include <stdio.h>

namespace game {

inline void* g_img_csharp = nullptr;  // Assembly-CSharp image
inline void* g_img_core = nullptr;    // UnityEngine.CoreModule image

// Optional stage logger — main.cpp wires this to DebugWrite so ESP bring-up
// shows up in payload_debug.txt (invaluable when diagnosing load-time crashes).
inline void (*g_log_fn)(const char* msg) = nullptr;
inline void log(const char* msg) { if (g_log_fn) g_log_fn(msg); }

namespace detail {

// ── Cached classes ───────────────────────────────────────────────────────
inline void* kl_camera = nullptr;
inline void* kl_screen = nullptr;
inline void* kl_component = nullptr;
inline void* kl_transform = nullptr;
inline void* kl_object = nullptr;

inline void* kl_provider = nullptr;
inline void* kl_steam_player = nullptr;
inline void* kl_steam_player_id = nullptr;
inline void* kl_player = nullptr;
inline void* kl_player_life = nullptr;
inline void* kl_player_equipment = nullptr;
inline void* kl_zombie_mgr = nullptr;
inline void* kl_zombie_region = nullptr;
inline void* kl_zombie = nullptr;
inline void* kl_item_mgr = nullptr;
inline void* kl_item_region = nullptr;
inline void* kl_item_drop = nullptr;
inline void* kl_interactable_item = nullptr;
inline void* kl_item_asset = nullptr;
inline void* kl_asset = nullptr;
inline void* kl_vehicle_mgr = nullptr;
inline void* kl_vehicle = nullptr;
inline void* kl_vehicle_asset = nullptr;
inline void* kl_animal_mgr = nullptr;
inline void* kl_animal = nullptr;
inline void* kl_animal_asset = nullptr;
inline void* kl_barricade_mgr = nullptr;
inline void* kl_barricade_region = nullptr;
inline void* kl_barricade_drop = nullptr;
inline void* kl_storage = nullptr;
inline void* kl_bed = nullptr;
inline void* kl_generator = nullptr;
inline void* kl_sentry = nullptr;
inline void* kl_useable_gun = nullptr;
inline void* kl_bullet_info = nullptr;
inline void* kl_assets = nullptr;
inline void* kl_gameobject = nullptr;
inline void* kl_player_movement = nullptr;
inline void* kl_renderer = nullptr;
inline void* kl_mesh_renderer = nullptr;
inline void* kl_skinned_renderer = nullptr;
inline void* kl_playerui = nullptr;          // PlayerUI (stun/pain overlays)
inline void* kl_levellighting = nullptr;      // LevelLighting (fog, nightvision)
inline void* kl_player_life_ui = nullptr;     // PlayerLifeUI (grayscale, pain hook)

// ── Cached field offsets (instance) ─────────────────────────────────────
inline int off_sp_player = -1;        // SteamPlayer.player
inline int off_sp_playerid = -1;      // SteamPlayer.playerID
inline int off_spid_charname = -1;    // SteamPlayerID.characterName
inline int off_pl_life = -1;          // Player.life
inline int off_life_health = -1;      // PlayerLife._health
inline int off_life_dead = -1;        // PlayerLife._isDead
inline int off_life_vision = -1;       // PlayerLife._vision (byte, hallucination)
inline int off_pl_equipment = -1;     // Player.equipment
inline int off_eq_useable = -1;       // PlayerEquipment._useable
inline int off_eq_asset = -1;         // PlayerEquipment._asset
inline int off_asset_itemname = -1;   // ItemAsset._itemName (MonoString)
inline int off_asset_id = -1;         // Asset.id
inline int off_zombie_dead = -1;      // Zombie.isDead
inline int off_zombie_health = -1;    // Zombie.health (ushort field)
inline int off_zombie_maxhealth = -1; // Zombie.maxHealth (ushort field)
inline int off_zr_zombies = -1;       // ZombieRegion.zombies
inline int off_ir_drops = -1;         // ItemRegion._drops
inline int off_drop_model = -1;       // ItemDrop._model (Transform)
inline int off_drop_item = -1;        // ItemDrop._interactableItem
inline int off_ii_asset = -1;         // InteractableItem._asset
inline int off_veh_health = -1;       // InteractableVehicle.health
inline int off_veh_locked = -1;       // InteractableVehicle._isLocked
inline int off_veh_asset = -1;        // InteractableVehicle._asset
inline int off_va_name = -1;          // VehicleAsset._vehicleName
inline int off_va_healthmax = -1;     // VehicleAsset._healthMax
inline int off_animal_dead = -1;      // Animal.isDead
inline int off_animal_asset = -1;     // Animal._asset
inline int off_aa_name = -1;          // AnimalAsset._animalName
inline int off_br_drops = -1;         // BarricadeRegion._drops
inline int off_bd_model = -1;         // BarricadeDrop._model
inline int off_bd_interactable = -1;  // BarricadeDrop._interactable
inline int off_bd_asset = -1;         // BarricadeDrop.asset
inline int off_bed_owner = -1;        // InteractableBed.owner (CSteamID = u64)
inline int off_gen_fuel = -1;         // InteractableGenerator._fuel
inline int off_gen_capacity = -1;     // InteractableGenerator._capacity
inline int off_gen_powered = -1;      // InteractableGenerator._isPowered
inline int off_sentry_powered = -1;   // InteractableSentry.isPowered
inline int off_2hp_hp = -1;           // Interactable2HP.hp (byte)
inline int off_gun_bullets = -1;      // UseableGun.bullets (List<BulletInfo>)
inline int off_bullet_origin = -1;    // BulletInfo.origin (Vector3)
inline int off_bullet_pos = -1;       // BulletInfo.<position>k__BackingField
inline int off_grenade_range = -1;    // Grenade.range (float)
inline int off_asset_type = -1;       // ItemAsset.type (EItemType, int)
inline int off_pl_movement = -1;      // Player.movement

// ── Cached methods ───────────────────────────────────────────────────────
inline void* m_camera_main = nullptr;
inline void* m_cam_wvp = nullptr;          // Camera.worldToCameraMatrix
inline void* m_cam_proj = nullptr;         // Camera.projectionMatrix
inline void* m_cam_w2s = nullptr;          // Camera.WorldToScreenPoint
inline void* m_cam_fov = nullptr;          // Camera.fieldOfView
inline void* m_screen_w = nullptr;
inline void* m_screen_h = nullptr;
inline void* m_get_transform = nullptr;    // Component.transform
inline void* m_get_position = nullptr;     // Transform.position
inline void* m_obj_get_name = nullptr;     // Object.name
inline void* m_tr_childcount = nullptr;    // Transform.childCount
inline void* m_tr_getchild = nullptr;      // Transform.GetChild(int)
inline void* m_comp_get_component = nullptr; // Component.GetComponent(Type)
inline void* m_fot = nullptr;              // Object.FindObjectsOfType(Type)
inline void* m_player_get_player = nullptr;// Player.get_player (static)
inline void* m_assets_find = nullptr;      // Assets.find(EAssetType, ushort)
inline void* m_get_components_in_children = nullptr; // Component.GetComponentsInChildren(Type, bool)
inline void* m_renderer_get_bounds = nullptr;        // Renderer.bounds
inline void* m_movement_get_vehicle = nullptr;       // PlayerMovement.getVehicle()

// ── Chams: Material / Shader / Renderer / AssetBundle methods ───────────
inline void* kl_shader = nullptr;
inline void* kl_material = nullptr;
inline void* kl_assetbundle = nullptr;
inline void* m_shader_find = nullptr;              // Shader.Find(string)
inline void* m_material_set_int = nullptr;         // Material.SetInt(string, int)
inline void* m_material_set_color = nullptr;       // Material.SetColor(string, Color)
inline void* m_material_get_shader = nullptr;      // Material.get_shader
inline void* m_material_set_shader = nullptr;      // Material.set_shader(Shader)
inline void* m_renderer_get_material = nullptr;    // Renderer.get_material (instanced copy)
inline void* m_renderer_get_shared = nullptr;      // Renderer.get_sharedMaterial
inline void* m_ab_load_from_memory = nullptr;      // AssetBundle.LoadFromMemory(byte[])
inline void* m_ab_load_asset = nullptr;            // AssetBundle.LoadAsset(string, Type)
inline void* rt_shader = nullptr;                  // typeof(Shader) reflection type

inline void* kl_level = nullptr;   // SDG.Unturned.Level — for level_loaded()

// ── Reflection types (for FindObjectsOfType / GetComponent) ─────────────
inline void* rt_carepackage = nullptr;
inline void* rt_grenade = nullptr;
inline void* rt_2hp = nullptr;
inline void* rt_renderer = nullptr;

// ── Static field accessors ───────────────────────────────────────────────
// Unturned mixes static fields ("_regions"), static auto-props
// ("<regions>k__BackingField") and plain fields — try all spellings.
inline void* find_field(void* klass, const char* name) {
    if (!klass) return nullptr;
    void* f = mono::class_get_field_from_name(klass, name);
    if (f) return f;
    char alt[96];
    snprintf(alt, sizeof(alt), "_%s", name);
    f = mono::class_get_field_from_name(klass, alt);
    if (f) return f;
    snprintf(alt, sizeof(alt), "<%s>k__BackingField", name);
    return mono::class_get_field_from_name(klass, alt);
}

inline int find_field_offset(void* klass, const char* name) {
    void* f = find_field(klass, name);
    return f ? mono::field_get_offset(f) : -1;
}

// Read a static reference-type field's value (managed pointer). Returns nullptr on failure.
inline void* read_static_ref(void* klass, const char* name) {
    void* f = find_field(klass, name);
    if (!f) return nullptr;
    void* vt = mono::class_vtable(mono::g_domain, klass);
    if (!vt) return nullptr;
    void* val = nullptr;
    mono::field_static_get_value(vt, f, &val);
    return val;
}

inline void* invoke0(void* method, void* obj) {
    if (!method) return nullptr;
    void* exc = nullptr;
    void* r = mono::runtime_invoke(method, obj, nullptr, &exc);
    return exc ? nullptr : r;
}

inline void utf8_of(void* mono_str, char* out, int out_size) {
    out[0] = 0;
    if (!mono_str || out_size <= 0) return;
    char* s = mono::string_to_utf8(mono_str);
    if (!s) return;
    strncpy(out, s, out_size - 1);
    out[out_size - 1] = 0;
    mono::free(s);
}

// List<T> layout: +0x10 = T[] _items, +0x18 = int _size. T[] data at +0x20.
inline void* list_items(void* list) {
    return list ? *(void**)((char*)list + 0x10) : nullptr;
}
inline int list_size(void* list) {
    return list ? *(int*)((char*)list + 0x18) : 0;
}
inline void** array_data(void* arr) {
    return arr ? (void**)((char*)arr + 0x20) : nullptr;
}
// 2D array: bounds ptr at +0x10 -> [len0, lo0, len1, lo1] int32s; data at +0x20.
inline void array2_dims(void* arr, int& len0, int& len1) {
    len0 = len1 = 0;
    if (!arr) return;
    int* bounds = *(int**)((char*)arr + 0x10);
    if (!bounds) return;
    len0 = bounds[0];
    len1 = bounds[2];
}

// Transform position — the only per-entity mono invoke we can't avoid.
inline bool transform_pos(void* transform, float out[3]) {
    void* boxed = invoke0(m_get_position, transform);
    if (!boxed) return false;
    memcpy(out, mono::object_unbox(boxed), 12);
    return true;
}

inline bool component_pos(void* component, float out[3]) {
    if (!component) return false;
    void* tr = invoke0(m_get_transform, component);
    return tr && transform_pos(tr, out);
}

// Is klass (or a base class) UseableGun?
inline bool is_useable_gun(void* klass) {
    for (int i = 0; klass && i < 6; i++) {
        if (klass == kl_useable_gun) return true;
        klass = mono::class_get_parent(klass);
    }
    return false;
}

// ── Assembly scan ───────────────────────────────────────────────────────
inline void __cdecl find_images_cb(void* assembly, void* /*ud*/) {
    void* img = mono::assembly_get_image(assembly);
    if (!img) return;
    const char* name = mono::image_get_name(img);
    if (!name) return;
    if (!g_img_csharp && strcmp(name, "Assembly-CSharp") == 0) g_img_csharp = img;
    else if (!g_img_core && strcmp(name, "UnityEngine.CoreModule") == 0) g_img_core = img;
}

inline bool cache_classes() {
    if (!g_img_csharp || !g_img_core) return false;
    void* I = g_img_csharp;
    void* U = g_img_core;

    kl_camera = mono::class_from_name(U, "UnityEngine", "Camera");
    kl_screen = mono::class_from_name(U, "UnityEngine", "Screen");
    kl_component = mono::class_from_name(U, "UnityEngine", "Component");
    kl_transform = mono::class_from_name(U, "UnityEngine", "Transform");
    kl_object = mono::class_from_name(U, "UnityEngine", "Object");
    kl_gameobject = mono::class_from_name(U, "UnityEngine", "GameObject");

    kl_provider = mono::class_from_name(I, "SDG.Unturned", "Provider");
    kl_steam_player = mono::class_from_name(I, "SDG.Unturned", "SteamPlayer");
    kl_steam_player_id = mono::class_from_name(I, "SDG.Unturned", "SteamPlayerID");
    kl_player = mono::class_from_name(I, "SDG.Unturned", "Player");
    kl_player_life = mono::class_from_name(I, "SDG.Unturned", "PlayerLife");
    kl_player_equipment = mono::class_from_name(I, "SDG.Unturned", "PlayerEquipment");
    kl_zombie_mgr = mono::class_from_name(I, "SDG.Unturned", "ZombieManager");
    kl_zombie_region = mono::class_from_name(I, "SDG.Unturned", "ZombieRegion");
    kl_zombie = mono::class_from_name(I, "SDG.Unturned", "Zombie");
    kl_item_mgr = mono::class_from_name(I, "SDG.Unturned", "ItemManager");
    kl_item_region = mono::class_from_name(I, "SDG.Unturned", "ItemRegion");
    kl_item_drop = mono::class_from_name(I, "SDG.Unturned", "ItemDrop");
    kl_interactable_item = mono::class_from_name(I, "SDG.Unturned", "InteractableItem");
    kl_item_asset = mono::class_from_name(I, "SDG.Unturned", "ItemAsset");
    kl_asset = mono::class_from_name(I, "SDG.Unturned", "Asset");
    kl_vehicle_mgr = mono::class_from_name(I, "SDG.Unturned", "VehicleManager");
    kl_vehicle = mono::class_from_name(I, "SDG.Unturned", "InteractableVehicle");
    kl_vehicle_asset = mono::class_from_name(I, "SDG.Unturned", "VehicleAsset");
    kl_animal_mgr = mono::class_from_name(I, "SDG.Unturned", "AnimalManager");
    kl_animal = mono::class_from_name(I, "SDG.Unturned", "Animal");
    kl_animal_asset = mono::class_from_name(I, "SDG.Unturned", "AnimalAsset");
    kl_level = mono::class_from_name(I, "SDG.Unturned", "Level");
    kl_barricade_mgr = mono::class_from_name(I, "SDG.Unturned", "BarricadeManager");
    kl_barricade_region = mono::class_from_name(I, "SDG.Unturned", "BarricadeRegion");
    kl_barricade_drop = mono::class_from_name(I, "SDG.Unturned", "BarricadeDrop");
    kl_storage = mono::class_from_name(I, "SDG.Unturned", "InteractableStorage");
    kl_bed = mono::class_from_name(I, "SDG.Unturned", "InteractableBed");
    kl_generator = mono::class_from_name(I, "SDG.Unturned", "InteractableGenerator");
    kl_sentry = mono::class_from_name(I, "SDG.Unturned", "InteractableSentry");
    kl_useable_gun = mono::class_from_name(I, "SDG.Unturned", "UseableGun");
    kl_bullet_info = mono::class_from_name(I, "SDG.Unturned", "BulletInfo");
    kl_assets = mono::class_from_name(I, "SDG.Unturned", "Assets");
    kl_player_movement = mono::class_from_name(I, "SDG.Unturned", "PlayerMovement");
    kl_renderer = mono::class_from_name(U, "UnityEngine", "Renderer");
    kl_mesh_renderer = mono::class_from_name(U, "UnityEngine", "MeshRenderer");
    kl_skinned_renderer = mono::class_from_name(U, "UnityEngine", "SkinnedMeshRenderer");
    kl_playerui = mono::class_from_name(I, "SDG.Unturned", "PlayerUI");
    kl_levellighting = mono::class_from_name(I, "SDG.Unturned", "LevelLighting");
    kl_player_life_ui = mono::class_from_name(I, "SDG.Unturned", "PlayerLifeUI");
    kl_shader = mono::class_from_name(U, "UnityEngine", "Shader");
    kl_material = mono::class_from_name(U, "UnityEngine", "Material");
    // AssetBundle lives in UnityEngine.AssetBundleModule, not CoreModule
    void* ab_img = mono::p_mono_image_loaded("UnityEngine.AssetBundleModule");
    if (ab_img) kl_assetbundle = mono::class_from_name(ab_img, "UnityEngine", "AssetBundle");

    rt_carepackage = mono::reflection_type_from_name("SDG.Unturned.Carepackage", I);
    rt_grenade = mono::reflection_type_from_name("SDG.Unturned.Grenade", I);
    rt_2hp = mono::reflection_type_from_name("SDG.Unturned.Interactable2HP", I);
    rt_renderer = mono::reflection_type_from_name("UnityEngine.Renderer", U);

    // Core checks — every class whose methods we look up below must resolve.
    // mono_class_get_method_from_name(NULL, ...) is not guaranteed safe on all
    // mono builds, and ready=true would otherwise arm null-target lookups.
    if (!kl_camera || !kl_screen || !kl_component || !kl_transform || !kl_object ||
        !kl_provider || !kl_player || !kl_zombie_mgr || !kl_zombie ||
        !kl_assets || !kl_player_movement || !kl_renderer)
        return false;

    off_sp_player = find_field_offset(kl_steam_player, "player");
    off_sp_playerid = find_field_offset(kl_steam_player, "playerID");
    off_spid_charname = find_field_offset(kl_steam_player_id, "characterName");
    off_pl_life = find_field_offset(kl_player, "life");
    off_life_health = find_field_offset(kl_player_life, "health");
    off_life_dead = find_field_offset(kl_player_life, "isDead");
    off_life_vision = find_field_offset(kl_player_life, "vision");
    off_pl_equipment = find_field_offset(kl_player, "equipment");
    off_eq_useable = find_field_offset(kl_player_equipment, "useable");
    off_eq_asset = find_field_offset(kl_player_equipment, "asset");
    off_asset_itemname = find_field_offset(kl_item_asset, "itemName");
    off_asset_id = find_field_offset(kl_asset, "id");
    off_zombie_dead = find_field_offset(kl_zombie, "isDead");
    off_zombie_health = find_field_offset(kl_zombie, "health");
    off_zombie_maxhealth = find_field_offset(kl_zombie, "maxHealth");
    off_zr_zombies = find_field_offset(kl_zombie_region, "zombies");
    off_ir_drops = find_field_offset(kl_item_region, "drops");
    off_drop_model = find_field_offset(kl_item_drop, "model");
    off_drop_item = find_field_offset(kl_item_drop, "interactableItem");
    off_ii_asset = find_field_offset(kl_interactable_item, "asset");
    off_veh_health = find_field_offset(kl_vehicle, "health");
    off_veh_locked = find_field_offset(kl_vehicle, "isLocked");
    off_veh_asset = find_field_offset(kl_vehicle, "asset");
    off_va_name = find_field_offset(kl_vehicle_asset, "vehicleName");
    off_va_healthmax = find_field_offset(kl_vehicle_asset, "healthMax");
    off_animal_dead = find_field_offset(kl_animal, "isDead");
    off_animal_asset = find_field_offset(kl_animal, "asset");
    off_aa_name = find_field_offset(kl_animal_asset, "animalName");
    off_br_drops = find_field_offset(kl_barricade_region, "drops");
    off_bd_model = find_field_offset(kl_barricade_drop, "model");
    off_bd_interactable = find_field_offset(kl_barricade_drop, "interactable");
    off_bd_asset = find_field_offset(kl_barricade_drop, "asset");
    off_bed_owner = find_field_offset(kl_bed, "owner");
    off_gen_fuel = find_field_offset(kl_generator, "fuel");
    off_gen_capacity = find_field_offset(kl_generator, "capacity");
    off_gen_powered = find_field_offset(kl_generator, "isPowered");
    off_sentry_powered = find_field_offset(kl_sentry, "isPowered");
    {
        void* kl_2hp = mono::class_from_name(I, "SDG.Unturned", "Interactable2HP");
        off_2hp_hp = find_field_offset(kl_2hp, "hp");
    }
    off_gun_bullets = find_field_offset(kl_useable_gun, "bullets");
    off_bullet_origin = find_field_offset(kl_bullet_info, "origin");
    off_bullet_pos = find_field_offset(kl_bullet_info, "position");
    off_asset_type = find_field_offset(kl_item_asset, "type");
    off_pl_movement = find_field_offset(kl_player, "movement");
    {
        void* kl_grenade = mono::class_from_name(I, "SDG.Unturned", "Grenade");
        off_grenade_range = find_field_offset(kl_grenade, "range");
    }

    m_camera_main = mono::class_get_method_from_name(kl_camera, "get_main", 0);
    m_cam_wvp = mono::class_get_method_from_name(kl_camera, "get_worldToCameraMatrix", 0);
    m_cam_proj = mono::class_get_method_from_name(kl_camera, "get_projectionMatrix", 0);
    m_cam_w2s = mono::class_get_method_from_name(kl_camera, "WorldToScreenPoint", 1);
    m_cam_fov = mono::class_get_method_from_name(kl_camera, "get_fieldOfView", 0);
    m_screen_w = mono::class_get_method_from_name(kl_screen, "get_width", 0);
    m_screen_h = mono::class_get_method_from_name(kl_screen, "get_height", 0);
    m_get_transform = mono::class_get_method_from_name(kl_component, "get_transform", 0);
    m_get_position = mono::class_get_method_from_name(kl_transform, "get_position", 0);
    m_obj_get_name = mono::class_get_method_from_name(kl_object, "get_name", 0);
    m_tr_childcount = mono::class_get_method_from_name(kl_transform, "get_childCount", 0);
    m_tr_getchild = mono::class_get_method_from_name(kl_transform, "GetChild", 1);
    m_comp_get_component = mono::class_get_method_from_name(kl_component, "GetComponent", 1);
    m_fot = mono::class_get_method_from_name(kl_object, "FindObjectsOfType", 1);
    m_player_get_player = mono::class_get_method_from_name(kl_player, "get_player", 0);
    m_assets_find = mono::class_get_method_from_name(kl_assets, "find", 2);
    m_get_components_in_children = mono::class_get_method_from_name(kl_component, "GetComponentsInChildren", 2);
    m_renderer_get_bounds = mono::class_get_method_from_name(kl_renderer, "get_bounds", 0);
    m_movement_get_vehicle = mono::class_get_method_from_name(kl_player_movement, "getVehicle", 0);

    // Chams: Shader / Material / Renderer / AssetBundle methods
    if (kl_shader) m_shader_find = mono::class_get_method_from_name(kl_shader, "Find", 1);
    if (kl_material) {
        m_material_set_int = mono::class_get_method_from_name(kl_material, "SetInt", 2);
        m_material_set_color = mono::class_get_method_from_name(kl_material, "SetColor", 2);
        m_material_get_shader = mono::class_get_method_from_name(kl_material, "get_shader", 0);
        m_material_set_shader = mono::class_get_method_from_name(kl_material, "set_shader", 1);
    }
    if (kl_renderer) {
        m_renderer_get_material = mono::class_get_method_from_name(kl_renderer, "get_material", 0);
        m_renderer_get_shared = mono::class_get_method_from_name(kl_renderer, "get_sharedMaterial", 0);
    }
    if (kl_assetbundle) {
        m_ab_load_from_memory = mono::class_get_method_from_name(kl_assetbundle, "LoadFromMemory", 1);
        if (!m_ab_load_from_memory)
            m_ab_load_from_memory = mono::class_get_method_from_name(kl_assetbundle, "LoadFromMemory", 2);
        m_ab_load_asset = mono::class_get_method_from_name(kl_assetbundle, "LoadAsset", 2);
        if (!m_ab_load_asset)
            m_ab_load_asset = mono::class_get_method_from_name(kl_assetbundle, "LoadAsset", 1);
    } else {
        log("chams: AssetBundle class not found");
    }
    // Reflection type for Shader
    rt_shader = mono::reflection_type_from_name("UnityEngine.Shader", U);
    if (!rt_shader) {
        // Try from ShaderModule
        void* sh_img = mono::p_mono_image_loaded("UnityEngine.CoreModule");
        if (sh_img) rt_shader = mono::reflection_type_from_name("UnityEngine.Shader", sh_img);
    }

    return off_sp_player >= 0 && m_camera_main && m_get_transform && m_get_position;
}

} // namespace detail

// ── Camera state ─────────────────────────────────────────────────────────
inline float vp_matrix[16] = {};
inline float cam_fov = 90.0f;
inline int screen_w = 1920, screen_h = 1080;
inline float display_w = 1920.f, display_h = 1080.f;  // actual ImGui/swap-chain resolution
inline float local_pos[3] = {};
inline bool ready = false;

inline unsigned long long g_last_init_try = 0;
inline bool g_init_fail_logged = false;

inline bool init() {
    // Throttle retries — mono API calls during loading can race the game's
    // class loader / JIT and crash inside mono's native code.
    unsigned long long now = GetTickCount64();
    if (now - g_last_init_try < 10000) return false;
    g_last_init_try = now;

    __try {
        if (!mono::init()) return false;
        mono::thread_attach(mono::g_domain);
        mono::assembly_foreach(detail::find_images_cb, nullptr);
        ready = detail::cache_classes();
    } __except(1) {
        // Mono crashed (race with game's loading thread). Reset and retry later.
        ready = false;
        g_img_csharp = nullptr;
        g_img_core = nullptr;
        return false;
    }
    if (ready) log("esp: classes cached");
    else if (!g_init_fail_logged) { g_init_fail_logged = true; log("esp: waiting for game assemblies"); }
    return ready;
}

// distance to local player (local_pos updated in update())
inline float dist(const float pos[3]) {
    float dx = pos[0] - local_pos[0], dy = pos[1] - local_pos[1], dz = pos[2] - local_pos[2];
    float d2 = dx * dx + dy * dy + dz * dz;
    return d2 > 0 ? sqrtf(d2) : 0.0f;
}

// Vehicle the local player is currently in (for self-vehicle skip), refreshed in update()
inline void* g_local_vehicle = nullptr;
inline void* g_local_player = nullptr;

// ── Per-frame update: camera matrices + local player pos ────────────────
inline void update(float /*dt*/) {
    if (!ready) return;
    using namespace detail;


    void* cam = invoke0(m_camera_main, nullptr);
    if (!cam) return;


    // Screen dimensions — needed for y-flip in w2s_d
    {
        void* w_b = invoke0(m_screen_w, nullptr);
        if (w_b) { void* r = mono::object_unbox(w_b); if (r) screen_w = *(int*)r; }
        void* h_b = invoke0(m_screen_h, nullptr);
        if (h_b) { void* r = mono::object_unbox(h_b); if (r) screen_h = *(int*)r; }
    }

    void* fov_b = invoke0(m_cam_fov, cam);
    if (fov_b) {
        void* fov_raw = mono::object_unbox(fov_b);
        if (fov_raw) cam_fov = *(float*)fov_raw;
    }

    void* lp = invoke0(m_player_get_player, nullptr);
    g_local_player = lp;
    g_local_vehicle = nullptr;
    if (lp) {
        float pp[3];
        if (component_pos(lp, pp)) memcpy(local_pos, pp, 12);
        if (off_pl_movement >= 0 && m_movement_get_vehicle) {
            void* mv = *(void**)((char*)lp + off_pl_movement);
            if (mv) g_local_vehicle = invoke0(m_movement_get_vehicle, mv);
        }
    }

}

// ── World gate ───────────────────────────────────────────────────────────
// ZERO mono invokes, ZERO Unity native calls — pure managed-memory reads only.
// Safe on the render thread during loading screens: it never enters the engine,
// so it cannot race Unity's scene load. Everything that invokes managed or
// native code (update, entity getters, FindObjectsOfType) MUST stay behind
// esp_armed() — native calls mid-load are what crashed injection-while-loading.
inline bool world_ready() {
    if (!ready || detail::off_sp_player < 0) return false;
    __try {
        void* clients = detail::read_static_ref(detail::kl_provider, "clients");
        if (!clients) return false;
        if (detail::list_size(clients) <= 0) return false;
        void* items = detail::list_items(clients);
        if (!items) return false;
        void** arr = detail::array_data(items);
        if (!arr) return false;
        void* sp = arr[0];
        if (!sp) return false;
        void* player = *(void**)((char*)sp + detail::off_sp_player);
        return player != nullptr;
    } __except(1) {
        // Managed memory not yet stable (GC / partial construction).
        return false;
    }
}

inline unsigned long long g_armed_since = 0;
inline bool g_armed_logged = false;

// True once the world has been continuously ready for a settle period.
// Joining a server populates Provider.clients while the scene is STILL loading,
// so hold off all native calls briefly to let it finish.
inline bool esp_armed() {
    if (!world_ready()) { g_armed_since = 0; g_armed_logged = false; return false; }
    unsigned long long now = GetTickCount64();
    if (!g_armed_since) { g_armed_since = now; log("esp: world ready, arming"); return false; }
    if (now - g_armed_since < 750) return false;
    if (!g_armed_logged) { g_armed_logged = true; log("esp: armed"); }
    return true;
}

// 1.0 uses Camera.WorldToScreenPoint — same mono invoke approach.
// Unity returns screen-space (x, y, depth) where y is from BOTTOM;
// we flip to top-left origin. cw = depth (view-space z).
inline bool w2s_d(const float pos[3], float& sx, float& sy, float& cw) {
    sx = sy = cw = 0.0f;
    if (!ready || !detail::m_cam_w2s) return false;
    // Pack Vector3 argument
    float* args[1] = { (float*)pos };
    void* exc = nullptr;
    void* cam = detail::invoke0(detail::m_camera_main, nullptr);
    if (!cam) return false;
    void* result = mono::runtime_invoke(detail::m_cam_w2s, cam, (void**)args, &exc);
    if (!result || exc) return false;
    float* sp = (float*)mono::object_unbox(result);
    cw = sp[2];  // depth (positive in front)
    if (cw < 0.1f) return false;
    // Scale from Unity screen-space to ImGui/swap-chain display space
    float sx_u = sp[0], sy_u = sp[1];
    if (screen_w > 0 && screen_h > 0) {
        sx = sx_u * (display_w / (float)screen_w);
        sy = display_h - sy_u * (display_h / (float)screen_h);
    } else {
        sx = sx_u;
        sy = display_h - sy_u;
    }
    return true;
}

inline bool world_to_screen(const float pos[3], float& sx, float& sy) {
    float cw;
    return w2s_d(pos, sx, sy, cw);
}


// ── Player ESP data ─────────────────────────────────────────────────────
struct PlayerInfo {
    float pos[3];
    char name[64];
    int health;
    bool is_dead;
    bool is_self;
    void* component;  // Player — for skeleton/weapon queries
    void* sp;         // SteamPlayer* — identity key for chams (and future per-player state)
};

// True once a map is fully loaded — false in the main menu and on the loading
// screen. Pure static read (Level._isLoaded), safe from the render thread.
inline bool level_loaded() {
    if (!ready || !detail::kl_level) return false;
    void* f = detail::find_field(detail::kl_level, "isLoaded");  // resolves _isLoaded
    if (!f) return false;
    void* vt = mono::class_vtable(mono::g_domain, detail::kl_level);
    if (!vt) return false;
    unsigned char v = 0;
    mono::field_static_get_value(vt, f, &v);
    return v != 0;
}

inline int get_players(PlayerInfo* out, int max) {
    using namespace detail;
    if (!ready || off_sp_player < 0) return 0;

    void* clients = read_static_ref(kl_provider, "clients");
    if (!clients) return 0;
    void* items = list_items(clients);
    int size = list_size(clients);
    void** arr = array_data(items);
    if (!arr || size <= 0) return 0;

    int n = 0;
    for (int i = 0; i < size && n < max; i++) {
        void* sp = arr[i];
        if (!sp) continue;
        void* player = *(void**)((char*)sp + off_sp_player);
        if (!player) continue;

        PlayerInfo& pi = out[n];
        // CRT-free zero (no memset — CRT not initialized in manually-mapped DLL)
        for (size_t z = 0; z < sizeof(PlayerInfo); z++) ((char*)&pi)[z] = 0;
        pi.component = player;
        pi.sp = sp;  // identity key for chams + future per-player state
        pi.is_self = (i == 0);

        if (!component_pos(player, pi.pos)) continue;

        // Name
        if (off_sp_playerid >= 0 && off_spid_charname >= 0) {
            void* pid = *(void**)((char*)sp + off_sp_playerid);
            if (pid) {
                void* mstr = *(void**)((char*)pid + off_spid_charname);
                utf8_of(mstr, pi.name, sizeof(pi.name));
            }
        }

        // Health
        pi.health = 100;
        if (off_pl_life >= 0 && off_life_health >= 0) {
            void* life = *(void**)((char*)player + off_pl_life);
            if (life) {
                pi.health = (int)*(unsigned char*)((char*)life + off_life_health);
                if (off_life_dead >= 0)
                    pi.is_dead = *(bool*)((char*)life + off_life_dead);
            }
        }
        n++;
    }
    return n;
}

// Weapon name for a Player component (equipment._asset._itemName)
inline bool get_weapon_name(void* player_component, char* out, int out_size) {
    using namespace detail;
    out[0] = 0;
    if (!ready || off_pl_equipment < 0 || off_eq_asset < 0 || off_asset_itemname < 0) return false;
    void* eq = *(void**)((char*)player_component + off_pl_equipment);
    if (!eq) return false;
    void* asset = *(void**)((char*)eq + off_eq_asset);
    if (!asset) return false;
    void* mstr = *(void**)((char*)asset + off_asset_itemname);
    utf8_of(mstr, out, out_size);
    return out[0] != 0;
}

// ── Zombie ESP data ─────────────────────────────────────────────────────
struct ZombieInfo {
    float pos[3];
    int health, max_health;
    bool is_dead;
    void* component;
};

inline int get_zombies(ZombieInfo* out, int max) {
    using namespace detail;
    if (!ready || off_zr_zombies < 0) return 0;
    // ZombieManager.regions is "static ZombieRegion[] regions => _regions" — read_static_ref
    // handles the _regions / <regions>k__BackingField spellings.
    void* regions = read_static_ref(kl_zombie_mgr, "regions");
    if (!regions) return 0;
    int len = *(int*)((char*)regions + 0x18);
    void** rarr = array_data(regions);
    if (!rarr || len <= 0) return 0;

    int n = 0;
    for (int r = 0; r < len && n < max; r++) {
        void* zr = rarr[r];
        if (!zr) continue;
        void* zl = *(void**)((char*)zr + off_zr_zombies);
        if (!zl) continue;
        void* zi = list_items(zl);
        int zs = list_size(zl);
        void** za = array_data(zi);
        if (!za) continue;
        for (int i = 0; i < zs && n < max; i++) {
            void* z = za[i];
            if (!z) continue;
            ZombieInfo& info = out[n];
            info.component = z;
            info.is_dead = off_zombie_dead >= 0 && *(bool*)((char*)z + off_zombie_dead);
            if (info.is_dead) continue;
            if (!component_pos(z, info.pos)) continue;
            // Zombie.health/maxHealth are plain ushort fields — read directly, no invoke.
            // (GetHealth()/GetMaxHealth() return FLOAT in the current game; the old code
            // unboxed them as ushort, decoded 0, and the bar's max_health>0 gate failed.)
            info.health = off_zombie_health >= 0 ? (int)*(unsigned short*)((char*)z + off_zombie_health) : 100;
            info.max_health = off_zombie_maxhealth >= 0 ? (int)*(unsigned short*)((char*)z + off_zombie_maxhealth) : 100;
            n++;
        }
    }
    return n;
}

// ── Item ESP data ───────────────────────────────────────────────────────
struct ItemInfo {
    float pos[3];
    char name[64];
    unsigned short id;
    int type;  // EItemType
};

inline int get_items(ItemInfo* out, int max) {
    using namespace detail;
    if (!ready || off_ir_drops < 0 || off_drop_model < 0) return 0;
    void* regions = read_static_ref(kl_item_mgr, "regions");
    if (!regions) return 0;
    int len0, len1;
    array2_dims(regions, len0, len1);
    void** rarr = array_data(regions);
    if (!rarr || len0 <= 0 || len1 <= 0) return 0;

    int n = 0;
    for (int x = 0; x < len0 && n < max; x++) {
        for (int y = 0; y < len1 && n < max; y++) {
            void* region = rarr[x * len1 + y];
            if (!region) continue;
            void* drops = *(void**)((char*)region + off_ir_drops);
            if (!drops) continue;
            void* di = list_items(drops);
            int ds = list_size(drops);
            void** da = array_data(di);
            if (!da) continue;
            for (int i = 0; i < ds && n < max; i++) {
                void* drop = da[i];
                if (!drop) continue;
                void* model = *(void**)((char*)drop + off_drop_model);
                if (!model) continue;
                ItemInfo& ii = out[n];
                if (!transform_pos(model, ii.pos)) continue;
                ii.name[0] = 0; ii.id = 0; ii.type = -1;
                void* item = off_drop_item >= 0 ? *(void**)((char*)drop + off_drop_item) : nullptr;
                if (item) {
                    void* asset = off_ii_asset >= 0 ? *(void**)((char*)item + off_ii_asset) : nullptr;
                    if (asset) {
                        void* mstr = off_asset_itemname >= 0 ? *(void**)((char*)asset + off_asset_itemname) : nullptr;
                        utf8_of(mstr, ii.name, sizeof(ii.name));
                        if (off_asset_id >= 0) ii.id = *(unsigned short*)((char*)asset + off_asset_id);
                        if (off_asset_type >= 0) ii.type = *(int*)((char*)asset + off_asset_type);
                    }
                }
                if (!ii.name[0]) strcpy(ii.name, "Item");
                n++;
            }
        }
    }
    return n;
}

// ── Vehicle ESP data ────────────────────────────────────────────────────
struct VehicleInfo {
    float pos[3];
    float health, health_max;
    char name[64];
    bool locked;
    void* component;
};

inline int get_vehicles(VehicleInfo* out, int max) {
    using namespace detail;
    if (!ready) return 0;
    void* vlist = read_static_ref(kl_vehicle_mgr, "vehicles");
    if (!vlist) return 0;
    void* vi = list_items(vlist);
    int vs = list_size(vlist);
    void** va = array_data(vi);
    if (!va || vs <= 0) return 0;

    int n = 0;
    for (int i = 0; i < vs && n < max; i++) {
        void* v = va[i];
        if (!v) continue;
        VehicleInfo& info = out[n];
        info.component = v;
        if (!component_pos(v, info.pos)) continue;
        info.health = off_veh_health >= 0 ? (float)*(unsigned short*)((char*)v + off_veh_health) : 0.f;
        if (info.health <= 0.f) continue;  // dead vehicle
        info.health_max = 1000.f;
        info.locked = off_veh_locked >= 0 && *(bool*)((char*)v + off_veh_locked);
        info.name[0] = 0;
        void* asset = off_veh_asset >= 0 ? *(void**)((char*)v + off_veh_asset) : nullptr;
        if (asset) {
            void* mstr = off_va_name >= 0 ? *(void**)((char*)asset + off_va_name) : nullptr;
            utf8_of(mstr, info.name, sizeof(info.name));
            if (off_va_healthmax >= 0)
                info.health_max = (float)*(unsigned short*)((char*)asset + off_va_healthmax);
        }
        if (!info.name[0]) strcpy(info.name, "Vehicle");
        if (info.health_max <= 0.f) info.health_max = 1.f;
        n++;
    }
    return n;
}

// ── Animal ESP data ─────────────────────────────────────────────────────
struct AnimalInfo {
    float pos[3];
    char name[48];
    void* component;
};

inline int get_animals(AnimalInfo* out, int max) {
    using namespace detail;
    if (!ready) return 0;
    void* alist = read_static_ref(kl_animal_mgr, "animals");
    if (!alist) return 0;
    void* ai = list_items(alist);
    int as = list_size(alist);
    void** aa = array_data(ai);
    if (!aa || as <= 0) return 0;

    int n = 0;
    for (int i = 0; i < as && n < max; i++) {
        void* a = aa[i];
        if (!a) continue;
        if (off_animal_dead >= 0 && *(bool*)((char*)a + off_animal_dead)) continue;
        AnimalInfo& info = out[n];
        info.component = a;
        if (!component_pos(a, info.pos)) continue;
        info.name[0] = 0;
        void* asset = off_animal_asset >= 0 ? *(void**)((char*)a + off_animal_asset) : nullptr;
        if (asset) {
            void* mstr = off_aa_name >= 0 ? *(void**)((char*)asset + off_aa_name) : nullptr;
            utf8_of(mstr, info.name, sizeof(info.name));
        }
        if (!info.name[0]) strcpy(info.name, "Animal");
        n++;
    }
    return n;
}

// ── Barricade ESP data (storage / bed / generator / turret) ─────────────
enum BarricadeType { BARRICADE_STORAGE = 0, BARRICADE_BED = 1, BARRICADE_GENERATOR = 2, BARRICADE_TURRET = 3 };

struct BarricadeInfo {
    float pos[3];
    char name[64];
    int type;         // BarricadeType
    float hp;         // -1 when unknown / with_hp=false
    bool powered;     // generator/turret
    bool claimed;     // bed
    int fuel_pct;     // generator
    void* model;      // model transform — bounds cache key
};

inline int get_barricades(BarricadeInfo* out, int max, bool with_hp) {
    using namespace detail;
    if (!ready || off_br_drops < 0 || off_bd_interactable < 0 || off_bd_model < 0) return 0;
    void* regions = read_static_ref(kl_barricade_mgr, "regions");
    if (!regions) return 0;
    int len0, len1;
    array2_dims(regions, len0, len1);
    void** rarr = array_data(regions);
    if (!rarr || len0 <= 0 || len1 <= 0) return 0;

    int n = 0;
    for (int x = 0; x < len0 && n < max; x++) {
        for (int y = 0; y < len1 && n < max; y++) {
            void* region = rarr[x * len1 + y];
            if (!region) continue;
            void* drops = *(void**)((char*)region + off_br_drops);
            if (!drops) continue;
            void* di = list_items(drops);
            int ds = list_size(drops);
            void** da = array_data(di);
            if (!da) continue;
            for (int i = 0; i < ds && n < max; i++) {
                void* drop = da[i];
                if (!drop) continue;
                void* inter = *(void**)((char*)drop + off_bd_interactable);
                if (!inter) continue;
                // Classify by managed class pointer — no invokes needed
                void* ic = mono::object_get_class(inter);
                int type = -1;
                if (ic == kl_storage) type = BARRICADE_STORAGE;
                else if (ic == kl_bed) type = BARRICADE_BED;
                else if (ic == kl_generator) type = BARRICADE_GENERATOR;
                else if (ic == kl_sentry) type = BARRICADE_TURRET;
                if (type < 0) continue;

                void* model = *(void**)((char*)drop + off_bd_model);
                if (!model) continue;

                BarricadeInfo& bi = out[n];
                memset(&bi, 0, sizeof(bi));
                bi.type = type;
                bi.hp = -1.f;
                bi.model = model;
                if (!transform_pos(model, bi.pos)) continue;

                // Name from the barricade asset
                void* asset = off_bd_asset >= 0 ? *(void**)((char*)drop + off_bd_asset) : nullptr;
                if (asset && off_asset_itemname >= 0) {
                    void* mstr = *(void**)((char*)asset + off_asset_itemname);
                    utf8_of(mstr, bi.name, sizeof(bi.name));
                }
                if (!bi.name[0]) strcpy(bi.name, "Barricade");

                // Type-specific state (plain field reads, no invokes)
                if (type == BARRICADE_BED && off_bed_owner >= 0)
                    bi.claimed = *(unsigned long long*)((char*)inter + off_bed_owner) != 0ULL;
                else if (type == BARRICADE_GENERATOR) {
                    if (off_gen_fuel >= 0 && off_gen_capacity >= 0) {
                        unsigned short fuel = *(unsigned short*)((char*)inter + off_gen_fuel);
                        unsigned short cap = *(unsigned short*)((char*)inter + off_gen_capacity);
                        bi.fuel_pct = cap > 0 ? (int)((unsigned)fuel * 100 / cap) : 0;
                    }
                    if (off_gen_powered >= 0) bi.powered = *(bool*)((char*)inter + off_gen_powered);
                } else if (type == BARRICADE_TURRET && off_sentry_powered >= 0)
                    bi.powered = *(bool*)((char*)inter + off_sentry_powered);

                // Health via Interactable2HP on the model (1 invoke, only when wanted)
                if (with_hp && m_comp_get_component && rt_2hp && off_2hp_hp >= 0) {
                    void* params[1] = { rt_2hp };
                    void* exc = nullptr;
                    void* hp2 = mono::runtime_invoke(m_comp_get_component, model, params, &exc);
                    if (!exc && hp2) bi.hp = (float)*(unsigned char*)((char*)hp2 + off_2hp_hp);
                }
                n++;
            }
        }
    }
    return n;
}

// ── FindObjectsOfType-backed scans (airdrops, grenades) ─────────────────
// MUST be called from the MAIN GAME THREAD (Unity's FoT crashes on any other).
// The Provider.Update hook in esp_renderer.h calls this on a 500ms timer.
// Render thread reads the cached component arrays without invoking FoT.

inline unsigned long long g_last_airdrop_scan = 0;
inline unsigned long long g_last_grenade_scan = 0;
inline void* g_airdrop_comps[64] = {};
inline volatile int g_airdrop_count = 0;
inline void* g_grenade_comps[64] = {};
inline volatile int g_grenade_count = 0;

// Called from render thread — does nothing (FoT is main-thread only).
// Kept for call-site compatibility in ESP::Render().
inline void refresh_fot_scans(unsigned long long /*now_ms*/) {}

// Called from the Provider.Update hook (main thread). Safe to invoke FoT here.
inline void main_thread_fot_scan(unsigned long long now_ms) {
    using namespace detail;
    if (!ready || !m_fot) return;

    // Airdrops — scan every 500ms
    if (rt_carepackage && (now_ms - g_last_airdrop_scan > 500)) {
        g_last_airdrop_scan = now_ms;
        void* params[1] = { rt_carepackage };
        void* exc = nullptr;
        void* arr = mono::runtime_invoke(m_fot, nullptr, params, &exc);
        if (!exc && arr) {
            int len = *(int*)((char*)arr + 0x18);
            void** data = array_data(arr);
            int n = 0;
            for (int i = 0; i < len && n < 64; i++)
                if (data[i]) g_airdrop_comps[n++] = data[i];
            g_airdrop_count = n;
        }
    }

    // Grenades — scan every 250ms (grenades are short-lived)
    if (rt_grenade && (now_ms - g_last_grenade_scan > 250)) {
        g_last_grenade_scan = now_ms;
        void* params[1] = { rt_grenade };
        void* exc = nullptr;
        void* arr = mono::runtime_invoke(m_fot, nullptr, params, &exc);
        if (!exc && arr) {
            int len = *(int*)((char*)arr + 0x18);
            void** data = array_data(arr);
            int n = 0;
            for (int i = 0; i < len && n < 64; i++)
                if (data[i]) g_grenade_comps[n++] = data[i];
            g_grenade_count = n;
        } else {
            g_grenade_count = 0;
        }
    }
}


struct AirdropInfo { float pos[3]; void* component; };

inline int get_airdrops(AirdropInfo* out, int max) {
    int n = 0;
    for (int i = 0; i < g_airdrop_count && n < max; i++) {
        out[n].component = g_airdrop_comps[i];
        if (!detail::component_pos(g_airdrop_comps[i], out[n].pos)) continue;
        n++;
    }
    return n;
}

struct GrenadeInfo {
    float pos[3];
    float range;
    char name[64];
};

// Cache id -> item name for grenade labels
struct GrenadeNameCache { unsigned short id; char name[64]; };
inline GrenadeNameCache g_grenade_names[64] = {};

inline const char* grenade_name_for_id(unsigned short id, char* fallback) {
    for (int i = 0; i < 64; i++)
        if (g_grenade_names[i].id == id) return g_grenade_names[i].name;
    using namespace detail;
    // Assets.find(EAssetType.ITEM=1, id) -> ItemAsset -> _itemName
    if (m_assets_find && kl_item_asset) {
        int etype = 1;
        void* params[2] = { &etype, &id };
        void* exc = nullptr;
        void* asset = mono::runtime_invoke(m_assets_find, nullptr, params, &exc);
        if (!exc && asset && off_asset_itemname >= 0) {
            char buf[64];
            void* mstr = *(void**)((char*)asset + off_asset_itemname);
            utf8_of(mstr, buf, sizeof(buf));
            if (buf[0]) {
                for (int i = 0; i < 64; i++) {
                    if (g_grenade_names[i].id == 0) {
                        g_grenade_names[i].id = id;
                        strcpy(g_grenade_names[i].name, buf);
                        return g_grenade_names[i].name;
                    }
                }
                strcpy(fallback, buf);
                return fallback;
            }
        }
    }
    return nullptr;
}

inline int get_grenades(GrenadeInfo* out, int max) {
    using namespace detail;
    int n = 0;
    for (int i = 0; i < g_grenade_count && n < max; i++) {
        void* g = g_grenade_comps[i];
        GrenadeInfo& gi = out[n];
        if (!component_pos(g, gi.pos)) continue;
        gi.range = off_grenade_range >= 0 ? *(float*)((char*)g + off_grenade_range) : 10.f;
        // Label: gameobject name is "<id>(Clone)" — resolve once via Assets.find
        gi.name[0] = 0;
        void* name_str = invoke0(m_obj_get_name, g);
        if (name_str) {
            char raw[96];
            utf8_of(name_str, raw, sizeof(raw));
            unsigned short id = (unsigned short)atoi(raw);
            if (id > 0) {
                char tmp[64];
                const char* resolved = grenade_name_for_id(id, tmp);
                if (resolved) { strncpy(gi.name, resolved, sizeof(gi.name) - 1); gi.name[sizeof(gi.name) - 1] = 0; }
            }
        }
        if (!gi.name[0]) strcpy(gi.name, "Grenade");
        n++;
    }
    return n;
}

// ── Bullet data (raw; renderer manages ghosts) ──────────────────────────
struct BulletShot {
    float origin[3];
    float pos[3];
    unsigned char pellet;
    bool is_self;
};

inline int get_bullets(BulletShot* out, int max, void* self_player) {
    using namespace detail;
    if (!ready || off_pl_equipment < 0 || off_eq_useable < 0 || off_gun_bullets < 0) return 0;

    void* clients = read_static_ref(kl_provider, "clients");
    if (!clients) return 0;
    void* items = list_items(clients);
    int size = list_size(clients);
    void** arr = array_data(items);
    if (!arr || size <= 0) return 0;

    int n = 0;
    for (int i = 0; i < size && n < max; i++) {
        void* sp = arr[i];
        if (!sp) continue;
        void* player = *(void**)((char*)sp + off_sp_player);
        if (!player) continue;
        void* eq = *(void**)((char*)player + off_pl_equipment);
        if (!eq) continue;
        void* useable = *(void**)((char*)eq + off_eq_useable);
        if (!useable) continue;
        if (!is_useable_gun(mono::object_get_class(useable))) continue;
        void* bullets = *(void**)((char*)useable + off_gun_bullets);
        if (!bullets) continue;
        void* bi = list_items(bullets);
        int bs = list_size(bullets);
        void** ba = array_data(bi);
        if (!ba) continue;
        for (int j = 0; j < bs && n < max; j++) {
            void* b = ba[j];
            if (!b) continue;
            BulletShot& bs_out = out[n];
            if (off_bullet_origin >= 0) memcpy(bs_out.origin, (char*)b + off_bullet_origin, 12);
            if (off_bullet_pos >= 0) memcpy(bs_out.pos, (char*)b + off_bullet_pos, 12);
            bs_out.pellet = 0;
            bs_out.is_self = (player == self_player);
            n++;
        }
    }
    return n;
}

// ── Skeleton bones (cached hierarchy walk) ──────────────────────────────
// Bone indices — 1:0 HEAD 1 SPINE 2 RARM 3 RHAND 4 LARM 5 LHAND 6 RLEG 7 RFOOT 8 LLEG 9 LFOOT
enum BoneIndex {
    BONE_HEAD = 0, BONE_SPINE, BONE_RARM, BONE_RHAND, BONE_LARM,
    BONE_LHAND, BONE_RLEG, BONE_RFOOT, BONE_LLEG, BONE_LFOOT, BONE_COUNT
};

inline const char* kBoneNames[BONE_COUNT] = {
    "Skull", "Spine", "Right_Arm", "Right_Hand", "Left_Arm",
    "Left_Hand", "Right_Leg", "Right_Foot", "Left_Leg", "Left_Foot"
};

struct BoneCacheEntry {
    void* key;                  // root transform of the entity model
    void* bones[BONE_COUNT];    // bone transforms (nullptr = not found)
    unsigned long long tick;
    bool populated;
};

inline BoneCacheEntry g_bone_cache[128] = {};
inline unsigned long long g_bone_cache_epoch = 0;

inline void bone_walk(void* tr, void* bones[BONE_COUNT], int depth) {
    using namespace detail;
    if (!tr || depth > 6) return;
    void* name_str = invoke0(m_obj_get_name, tr);
    if (name_str) {
        char name[64];
        utf8_of(name_str, name, sizeof(name));
        for (int b = 0; b < BONE_COUNT; b++)
            if (!bones[b] && strcmp(name, kBoneNames[b]) == 0) { bones[b] = tr; break; }
    }
    void* cc_boxed = invoke0(m_tr_childcount, tr);
    if (!cc_boxed) return;
    int cc = *(int*)mono::object_unbox(cc_boxed);
    if (cc <= 0 || cc > 64) return;
    for (int i = 0; i < cc; i++) {
        void* params[1] = { &i };
        void* exc = nullptr;
        void* child = mono::runtime_invoke(m_tr_getchild, tr, params, &exc);
        if (exc || !child) continue;
        bone_walk(child, bones, depth + 1);
    }
}

// Resolve bone transforms for an entity component (cached ~20s like 1.0's GetBones clear)
inline BoneCacheEntry* bones_for(void* component, unsigned long long now_ms) {
    using namespace detail;
    if (!m_obj_get_name || !m_tr_childcount || !m_tr_getchild) return nullptr;
    void* root = invoke0(m_get_transform, component);
    if (!root) return nullptr;

    if (now_ms - g_bone_cache_epoch > 20000) {
        g_bone_cache_epoch = now_ms;
        memset(g_bone_cache, 0, sizeof(g_bone_cache));
    }

    int slot = -1;
    for (int i = 0; i < 128; i++) {
        if (g_bone_cache[i].key == root && g_bone_cache[i].populated)
            return &g_bone_cache[i];
        if (slot < 0 && !g_bone_cache[i].populated) slot = i;
    }
    if (slot < 0) { memset(g_bone_cache, 0, sizeof(g_bone_cache)); slot = 0; }

    BoneCacheEntry& e = g_bone_cache[slot];
    memset(&e, 0, sizeof(e));
    e.key = root;
    e.tick = now_ms;
    bone_walk(root, e.bones, 0);
    e.populated = true;
    return &e;
}

// Fill bone world positions. Returns number of bones with valid positions.
inline int fill_bone_positions(void* component, float out[BONE_COUNT][3], bool present[BONE_COUNT], unsigned long long now_ms) {
    memset(present, 0, BONE_COUNT * sizeof(bool));
    BoneCacheEntry* e = bones_for(component, now_ms);
    if (!e) return 0;
    int n = 0;
    for (int b = 0; b < BONE_COUNT; b++) {
        if (!e->bones[b]) continue;
        if (detail::transform_pos(e->bones[b], out[b])) { present[b] = true; n++; }
    }
    return n;
}

// ── Renderer bounds (ProjectBounds port) ────────────────────────────────
// Split into two functions:
//   get_renderer_bounds() — CACHE-ONLY read, safe from render thread
//   populate_bounds()     — does the GCIC invoke, MAIN THREAD ONLY
struct BoundsCacheEntry { void* key; float center[3]; float extents[3]; bool valid; };
inline BoundsCacheEntry g_bounds_cache[256] = {};
inline unsigned long long g_bounds_cache_epoch = 0;

// Render-thread safe: returns cached bounds only. Never invokes mono.
inline bool get_renderer_bounds(void* component, float center[3], float extents[3], bool cache_static, unsigned long long now_ms) {
    if (!component) return false;
    if (now_ms - g_bounds_cache_epoch > 20000) {
        g_bounds_cache_epoch = now_ms;
        memset(g_bounds_cache, 0, sizeof(g_bounds_cache));
    }
    for (int i = 0; i < 256; i++) {
        if (g_bounds_cache[i].key == component) {
            if (!g_bounds_cache[i].valid) return false;
            memcpy(center, g_bounds_cache[i].center, 12);
            memcpy(extents, g_bounds_cache[i].extents, 12);
            return true;
        }
    }
    return false;  // cache miss — will be populated by main thread next tick
}

// Main-thread only: populates bounds cache for a component via GCIC.
inline void populate_bounds(void* component) {
    using namespace detail;
    if (!m_get_components_in_children || !rt_renderer || !m_renderer_get_bounds || !component) return;

    // Already cached?
    for (int i = 0; i < 256; i++)
        if (g_bounds_cache[i].key == component) return;

    int truthy = 1;
    void* params[2] = { rt_renderer, &truthy };
    void* exc = nullptr;
    void* arr = mono::runtime_invoke(m_get_components_in_children, component, params, &exc);
    bool any = false;
    float mn[3] = { 1e30f, 1e30f, 1e30f }, mx[3] = { -1e30f, -1e30f, -1e30f };
    float cen[3] = {}, ext[3] = {};
    if (!exc && arr) {
        int len = *(int*)((char*)arr + 0x18);
        void** rends = array_data(arr);
        for (int i = 0; i < len; i++) {
            void* r = rends[i];
            if (!r) continue;
            void* rc = mono::object_get_class(r);
            if (rc != kl_mesh_renderer && rc != kl_skinned_renderer) continue;
            void* boxed = invoke0(m_renderer_get_bounds, r);
            if (!boxed) continue;
            float b[6];
            memcpy(b, mono::object_unbox(boxed), 24);
            for (int a = 0; a < 3; a++) {
                float lo = b[a] - b[3 + a], hi = b[a] + b[3 + a];
                if (lo < mn[a]) mn[a] = lo;
                if (hi > mx[a]) mx[a] = hi;
            }
            any = true;
        }
    }
    if (any) {
        for (int a = 0; a < 3; a++) {
            cen[a] = (mn[a] + mx[a]) * 0.5f;
            ext[a] = (mx[a] - mn[a]) * 0.5f;
        }
    }
    // Store in cache
    for (int i = 0; i < 256; i++) {
        if (!g_bounds_cache[i].key) {
            g_bounds_cache[i].key = component;
            g_bounds_cache[i].valid = any;
            if (any) {
                memcpy(g_bounds_cache[i].center, cen, 12);
                memcpy(g_bounds_cache[i].extents, ext, 12);
            }
            break;
        }
    }
}
// ── Main-thread renderer bounds refresh ─────────────────────────────────
// Populates g_bounds_cache from Provider.Update hook (main thread).
// Walks vehicles + barricades and pre-caches their renderer bounds so
// the render thread can read them without invoking GCIC.
inline unsigned long long g_last_bounds_scan = 0;

inline void main_thread_bounds_refresh(unsigned long long now_ms) {
    using namespace detail;
    if (!m_get_components_in_children || !rt_renderer || !m_renderer_get_bounds) return;

    // Scan every 2s — bounds are fairly static
    if (now_ms - g_last_bounds_scan < 2000) return;
    g_last_bounds_scan = now_ms;

    // Refresh epoch
    if (now_ms - g_bounds_cache_epoch > 20000) {
        g_bounds_cache_epoch = now_ms;
        memset(g_bounds_cache, 0, sizeof(g_bounds_cache));
    }

    // Pre-populate vehicle bounds (vehicles use use_bounds=true, cache_static=false)
    void* vlist = read_static_ref(kl_vehicle_mgr, "vehicles");
    if (vlist) {
        void* vi = list_items(vlist);
        int vs = list_size(vlist);
        void** va = array_data(vi);
        if (va) {
            int budget = 16;  // limit per tick to avoid stalling the game
            for (int i = 0; i < vs && budget > 0; i++) {
                if (!va[i]) continue;
                populate_bounds(va[i]);
                budget--;
            }
        }
    }

    // Pre-populate airdrop bounds
    for (int i = 0; i < g_airdrop_count; i++)
        if (g_airdrop_comps[i]) populate_bounds(g_airdrop_comps[i]);
}

} // namespace game
