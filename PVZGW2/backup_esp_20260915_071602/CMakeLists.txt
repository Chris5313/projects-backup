cmake_minimum_required(VERSION 3.20)
project(GW2Payload LANGUAGES CXX C)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# ---- Vendored MinHook ----
set(MINHOOK_DIR ${CMAKE_SOURCE_DIR}/../vendor/minhook)
set(MINHOOK_SOURCES
    ${MINHOOK_DIR}/src/hook.c
    ${MINHOOK_DIR}/src/trampoline.c
    ${MINHOOK_DIR}/src/buffer.c
    ${MINHOOK_DIR}/src/hde/hde32.c
    ${MINHOOK_DIR}/src/hde/hde64.c
)

# ---- Vendored ImGui ----
set(IMGUI_DIR ${CMAKE_SOURCE_DIR}/../vendor/imgui)
set(IMGUI_SOURCES
    ${IMGUI_DIR}/imgui.cpp
    ${IMGUI_DIR}/imgui_draw.cpp
    ${IMGUI_DIR}/imgui_tables.cpp
    ${IMGUI_DIR}/imgui_widgets.cpp
    ${IMGUI_DIR}/backends/imgui_impl_dx11.cpp
    ${IMGUI_DIR}/backends/imgui_impl_win32.cpp
)

# ---- Framework (ported from israeliclient 2.0) ----
set(FRAMEWORK_DIR ${CMAKE_SOURCE_DIR}/framework)
set(FRAMEWORK_SOURCES
    ${FRAMEWORK_DIR}/gui_gw2.cc
    ${FRAMEWORK_DIR}/base_elements.cpp
    ${FRAMEWORK_DIR}/checkbox.cpp
    ${FRAMEWORK_DIR}/button.cpp
    ${FRAMEWORK_DIR}/drag_slider.cpp
    ${FRAMEWORK_DIR}/dropdown.cpp
    ${FRAMEWORK_DIR}/color_picker.cpp
    ${FRAMEWORK_DIR}/custom_draw.cpp
    ${FRAMEWORK_DIR}/selection.cpp
    ${FRAMEWORK_DIR}/notifications.cpp
    ${FRAMEWORK_DIR}/begin.cpp
    ${FRAMEWORK_DIR}/begin_child.cpp
    ${FRAMEWORK_DIR}/keybind.cpp
    ${FRAMEWORK_DIR}/texture_loader.cpp
    ${FRAMEWORK_DIR}/textfield.cpp
)

add_library(gw2_payload SHARED
    main.cpp
    gw2_scanner.cpp
    overlay.cpp
    gw2_viewproj.cpp
    gw2_entities.cpp
    gw2_trigger.cpp
    gw2_aimbot.cpp
    gw2_dumper.cpp
    ${MINHOOK_SOURCES}
    ${IMGUI_SOURCES}
    ${FRAMEWORK_SOURCES}
)

target_include_directories(gw2_payload PRIVATE
    ${CMAKE_SOURCE_DIR}
    ${CMAKE_SOURCE_DIR}/framework
    ${MINHOOK_DIR}/include
    ${MINHOOK_DIR}/src
    ${IMGUI_DIR}
    ${IMGUI_DIR}/backends
)

target_compile_definitions(gw2_payload PRIVATE
    IMGUI_DEFINE_MATH_OPERATORS
    WIN32_LEAN_AND_MEAN
)

# GW2_RELEASE (build_release.bat): no debug dump paths — F2 = light scan,
# F3 disabled, per-lock logging compiled out. Debug build keeps everything
# and outputs gw2_payload_debug.dll.
option(GW2_RELEASE "Shipping build: strip debug dumps/logs" OFF)
if(GW2_RELEASE)
    target_compile_definitions(gw2_payload PRIVATE GW2_RELEASE=1)
    set_target_properties(gw2_payload PROPERTIES OUTPUT_NAME "gw2_payload")
else()
    set_target_properties(gw2_payload PROPERTIES OUTPUT_NAME "gw2_payload_debug")
endif()

target_compile_options(gw2_payload PRIVATE
    $<$<COMPILE_LANGUAGE:CXX>:/MT /O1>
    $<$<COMPILE_LANGUAGE:C>:/MT /O1>
)

target_link_libraries(gw2_payload PRIVATE
    d3d11.lib
    user32.lib
)

