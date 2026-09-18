#pragma once
// Mono embedded API bridge — resolves mono-2.0-bdwgc.dll exports at runtime.
// Header-only, no CRT dependency.

#include <Windows.h>

namespace mono {

typedef void (__cdecl *GFunc)(void* data, void* user_data);

#define MONO_FN(name, ret, ...) \
    typedef ret (__cdecl *name##_t)(__VA_ARGS__); \
    inline name##_t p_##name = nullptr;

MONO_FN(mono_get_root_domain, void*)
MONO_FN(mono_thread_attach, void*, void* domain)
MONO_FN(mono_domain_assembly_foreach, void, void* domain, GFunc func, void* user_data)
MONO_FN(mono_assembly_get_image, void*, void* assembly)
MONO_FN(mono_image_get_name, const char*, void* image)
MONO_FN(mono_image_loaded, void*, const char* name)
MONO_FN(mono_class_from_name, void*, void* image, const char* ns, const char* name)
MONO_FN(mono_class_get_parent, void*, void* klass)
MONO_FN(mono_class_get_field_from_name, void*, void* klass, const char* name)
MONO_FN(mono_class_get_property_from_name, void*, void* klass, const char* name)
MONO_FN(mono_class_get_method_from_name, void*, void* klass, const char* name, int param_count)
MONO_FN(mono_class_vtable, void*, void* domain, void* klass)
MONO_FN(mono_field_get_value, void, void* obj, void* field, void* value)
MONO_FN(mono_field_get_offset, int, void* field)
MONO_FN(mono_field_static_get_value, void, void* vtable, void* field, void* value)
MONO_FN(mono_property_get_get_method, void*, void* prop)
MONO_FN(mono_property_get_set_method, void*, void* prop)
MONO_FN(mono_field_static_set_value, void, void* vtable, void* field, void* value)
MONO_FN(mono_vtable_get_static_field_data, void*, void* vtable, void* field)  // optional; resolved without hard-failing init
MONO_FN(mono_string_new, void*, void* domain, const char* text)  // for passing managed strings to mono methods
MONO_FN(mono_runtime_invoke, void*, void* method, void* obj, void** params, void** exc)
MONO_FN(mono_object_unbox, void*, void* obj)
MONO_FN(mono_object_get_class, void*, void* obj)
MONO_FN(mono_string_to_utf8, char*, void* mono_string)
MONO_FN(mono_free, void, void* ptr)
MONO_FN(mono_reflection_type_from_name, void*, char* name, void* image)
MONO_FN(mono_compile_method, void*, void* method)
MONO_FN(mono_object_new, void*, void* domain, void* klass)
MONO_FN(mono_array_new, void*, void* domain, void* klass, uintptr_t n)
MONO_FN(mono_get_corlib, void*)
// Assembly/image loading from memory — used by chams managed helper
MONO_FN(mono_image_open_from_data_with_name, void*, const char* data, unsigned int data_len, int need_copy, int* status, int refonly, const char* name)
MONO_FN(mono_assembly_load_from_full, void*, void* image, const char* fname, int* status, int refonly)
MONO_FN(mono_image_close, void, void* image)
// Assembly loading from file — preferred for chams helper (mono resolves references itself)
MONO_FN(mono_domain_assembly_open, void*, void* domain, const char* filename)
// Method iteration — needed to resolve overloads (e.g. Material.SetColor)
MONO_FN(mono_class_get_methods, void*, void* klass, void** iter)
MONO_FN(mono_method_get_name, const char*, void* method)
MONO_FN(mono_method_signature, void*, void* method)
MONO_FN(mono_signature_get_param_count, unsigned int, void* sig)

#undef MONO_FN

inline void* g_domain = nullptr;

inline bool init() {
    HMODULE h = GetModuleHandleA("mono-2.0-bdwgc.dll");
    if (!h) h = LoadLibraryA("mono-2.0-bdwgc.dll");
    if (!h) return false;

    #define RES(name) p_##name = (name##_t)GetProcAddress(h, #name); if (!p_##name) return false;
    RES(mono_get_root_domain)
    RES(mono_thread_attach)
    RES(mono_domain_assembly_foreach)
    RES(mono_assembly_get_image)
    RES(mono_image_get_name)
    RES(mono_image_loaded)
    RES(mono_class_from_name)
    RES(mono_class_get_parent)
    RES(mono_class_get_field_from_name)
    RES(mono_class_get_property_from_name)
    RES(mono_class_get_method_from_name)
    RES(mono_class_vtable)
    RES(mono_field_get_value)
    RES(mono_field_get_offset)
    RES(mono_field_static_get_value)
    RES(mono_field_static_set_value)
    RES(mono_property_get_get_method)
    RES(mono_property_get_set_method)
    p_mono_vtable_get_static_field_data = (mono_vtable_get_static_field_data_t)GetProcAddress(h, "mono_vtable_get_static_field_data");
    // mono_string_new is optional — only needed for chams etc. Don't fail init if missing.
    p_mono_string_new = (mono_string_new_t)GetProcAddress(h, "mono_string_new");
    RES(mono_runtime_invoke)
    RES(mono_object_unbox)
    RES(mono_object_get_class)
    RES(mono_string_to_utf8)
    RES(mono_free)
    RES(mono_reflection_type_from_name)
    RES(mono_compile_method)
    // Chams needs these; optional — don't fail init if missing
    p_mono_object_new = (mono_object_new_t)GetProcAddress(h, "mono_object_new");
    p_mono_array_new = (mono_array_new_t)GetProcAddress(h, "mono_array_new");
    p_mono_get_corlib = (mono_get_corlib_t)GetProcAddress(h, "mono_get_corlib");
    // Assembly loading from memory
    p_mono_image_open_from_data_with_name = (mono_image_open_from_data_with_name_t)GetProcAddress(h, "mono_image_open_from_data_with_name");
    p_mono_assembly_load_from_full = (mono_assembly_load_from_full_t)GetProcAddress(h, "mono_assembly_load_from_full");
    p_mono_image_close = (mono_image_close_t)GetProcAddress(h, "mono_image_close");
    // Assembly loading from file
    p_mono_domain_assembly_open = (mono_domain_assembly_open_t)GetProcAddress(h, "mono_domain_assembly_open");
    // Method iteration (optional — for overload resolution)
    p_mono_class_get_methods = (mono_class_get_methods_t)GetProcAddress(h, "mono_class_get_methods");
    p_mono_method_get_name = (mono_method_get_name_t)GetProcAddress(h, "mono_method_get_name");
    p_mono_method_signature = (mono_method_signature_t)GetProcAddress(h, "mono_method_signature");
    p_mono_signature_get_param_count = (mono_signature_get_param_count_t)GetProcAddress(h, "mono_signature_get_param_count");
    #undef RES

    g_domain = p_mono_get_root_domain();
    return g_domain != nullptr;
}

// ── Convenience wrappers (null-hardened: mono's own null tolerance varies by version) ──

inline void* get_root_domain() { return p_mono_get_root_domain ? p_mono_get_root_domain() : nullptr; }
inline void  thread_attach(void* domain) { if (domain && p_mono_thread_attach) p_mono_thread_attach(domain); }
inline void  assembly_foreach(GFunc func, void* user_data) { if (g_domain && p_mono_domain_assembly_foreach) p_mono_domain_assembly_foreach(g_domain, func, user_data); }
inline void* assembly_get_image(void* assembly) { return assembly ? p_mono_assembly_get_image(assembly) : nullptr; }
inline const char* image_get_name(void* image) { return image ? p_mono_image_get_name(image) : nullptr; }
inline void* class_from_name(void* image, const char* ns, const char* name) { return image ? p_mono_class_from_name(image, ns, name) : nullptr; }
inline void* class_get_parent(void* klass) { return klass ? p_mono_class_get_parent(klass) : nullptr; }
inline void* class_get_field_from_name(void* klass, const char* name) { return klass ? p_mono_class_get_field_from_name(klass, name) : nullptr; }
inline void* class_get_method_from_name(void* klass, const char* name, int params) { return klass ? p_mono_class_get_method_from_name(klass, name, params) : nullptr; }
inline void* class_vtable(void* domain, void* klass) { return (domain && klass) ? p_mono_class_vtable(domain, klass) : nullptr; }
inline int   field_get_offset(void* field) { return field ? p_mono_field_get_offset(field) : -1; }
inline void  field_static_get_value(void* vtable, void* field, void* out) { if (vtable && field && out) p_mono_field_static_get_value(vtable, field, out); }
inline void  field_static_set_value(void* vtable, void* field, void* value) { if (vtable && field && value) p_mono_field_static_set_value(vtable, field, value); }
inline void* property_get_set_method(void* prop) { return prop ? p_mono_property_get_set_method(prop) : nullptr; }
inline void* class_get_property_from_name(void* klass, const char* name) { return klass ? p_mono_class_get_property_from_name(klass, name) : nullptr; }
inline void* compile_method(void* method) { return method ? p_mono_compile_method(method) : nullptr; }
inline void* runtime_invoke(void* method, void* obj, void** params, void** exc) { if (!method) return nullptr; return p_mono_runtime_invoke(method, obj, params, exc); }
inline void* object_unbox(void* obj) { return obj ? p_mono_object_unbox(obj) : nullptr; }
inline void* object_get_class(void* obj) { return obj ? p_mono_object_get_class(obj) : nullptr; }
inline char* string_to_utf8(void* mono_string) { return mono_string ? p_mono_string_to_utf8(mono_string) : nullptr; }
inline void  free(void* ptr) { if (ptr) p_mono_free(ptr); }
inline void* reflection_type_from_name(const char* name, void* image) { return image ? p_mono_reflection_type_from_name((char*)name, image) : nullptr; }
inline void* string_new(void* domain, const char* text) { return (domain && p_mono_string_new) ? p_mono_string_new(domain, text) : nullptr; }
inline void* object_new(void* domain, void* klass) { return (domain && klass && p_mono_object_new) ? p_mono_object_new(domain, klass) : nullptr; }
inline void* array_new(void* domain, void* klass, uintptr_t n) { return (domain && klass && p_mono_array_new) ? p_mono_array_new(domain, klass, n) : nullptr; }
inline void* get_corlib() { return p_mono_get_corlib ? p_mono_get_corlib() : nullptr; }
// Assembly loading from memory
inline void* image_open_from_data(const char* data, unsigned int len, const char* name) {
    if (!p_mono_image_open_from_data_with_name) return nullptr;
    int status = 0;
    void* img = p_mono_image_open_from_data_with_name(data, len, 1, &status, 0, name);
    return (status == 0) ? img : nullptr;
}
inline void* assembly_load_from(void* image, const char* name) {
    if (!p_mono_assembly_load_from_full || !image) return nullptr;
    int status = 0;
    return p_mono_assembly_load_from_full(image, name, &status, 0);
}
inline void image_close(void* image) {
    if (image && p_mono_image_close) p_mono_image_close(image);
}
inline void* domain_assembly_open(void* domain, const char* path) {
    return (domain && path && p_mono_domain_assembly_open) ? p_mono_domain_assembly_open(domain, path) : nullptr;
}
inline void* find_class(const char* image_name, const char* ns, const char* name) {
    void* img = p_mono_image_loaded(image_name);
    if (!img) return nullptr;
    return p_mono_class_from_name(img, ns, name);
}
inline void* get_field(void* klass, const char* name) { return p_mono_class_get_field_from_name(klass, name); }
inline void* get_method(void* klass, const char* name, int params) { return p_mono_class_get_method_from_name(klass, name, params); }
inline int   get_field_offset(void* field) { return field ? p_mono_field_get_offset(field) : -1; }

// (deleted get_static_field_data: it dereferenced vtable+0x0, which is MonoVTable::klass
//  on this mono build — NOT the static data block. Anything needing a static field's
//  address must use mono_vtable_get_static_field_data, verified against
//  mono_field_static_get_value. See ESP::ApplyCustomFov.)

// Read a value from a mono object field (obj must be non-null)
template<typename T>
inline bool read_field(void* obj, int offset, T& out) {
    if (!obj || offset < 0) return false;
    out = *(T*)((char*)obj + offset);
    return true;
}

inline void* invoke(void* method, void* obj, void** params) {
    void* exc = nullptr;
    void* result = p_mono_runtime_invoke(method, obj, params, &exc);
    return exc ? nullptr : result;
}

inline void* unbox(void* obj) { return p_mono_object_unbox(obj); }

} // namespace mono
