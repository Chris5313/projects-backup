cmake_minimum_required(VERSION 3.20)
project(GW2Payload LANGUAGES CXX C)

# kmap_thunk.asm — thread-hijack landing pad (thread context save/restore +
# call of the PE entry). Compiled into the image so the driver never needs
# executable scratch memory.
enable_language(ASM_MASM)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# ---- Vendored ImGui ----
set(IMGUI_DIR ${CMAKE_SOURCE_DIR}/../../vendor/imgui)
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
    overlay.cpp
    kmap_thunk.asm
    ${IMGUI_SOURCES}
    ${FRAMEWORK_SOURCES}
)

target_include_directories(gw2_payload PRIVATE
    ${CMAKE_SOURCE_DIR}
    ${CMAKE_SOURCE_DIR}/framework
    ${IMGUI_DIR}
    ${IMGUI_DIR}/backends
)

target_compile_definitions(gw2_payload PRIVATE
    IMGUI_DEFINE_MATH_OPERATORS
    WIN32_LEAN_AND_MEAN
)

target_compile_options(gw2_payload PRIVATE
    $<$<COMPILE_LANGUAGE:CXX>:/MT /O1>
    $<$<COMPILE_LANGUAGE:C>:/MT /O1>
)

target_link_libraries(gw2_payload PRIVATE
    d3d11.lib
    dxgi.lib
    user32.lib
)

# Export the driver-contract symbols (thunk + its ctx data cell; the marker
# byte is dllexport'ed in main.cpp). The driver resolves these in the mapped
# image by walking the export directory.
target_link_options(gw2_payload PRIVATE
    "/EXPORT:kmap_thunk"
    "/EXPORT:kmap_ctx"
)

set_target_properties(gw2_payload PROPERTIES OUTPUT_NAME "gw2_payload")
