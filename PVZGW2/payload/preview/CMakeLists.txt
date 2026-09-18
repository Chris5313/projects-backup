cmake_minimum_required(VERSION 3.20)
project(MenuPreview LANGUAGES CXX C)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

set(IMGUI_DIR ${CMAKE_SOURCE_DIR}/../../vendor/imgui)
set(FRAMEWORK_DIR ${CMAKE_SOURCE_DIR}/../framework)
set(MINHOOK_DIR ${CMAKE_SOURCE_DIR}/../../vendor/minhook)
set(MINHOOK_SOURCES
    ${MINHOOK_DIR}/src/hook.c
    ${MINHOOK_DIR}/src/trampoline.c
    ${MINHOOK_DIR}/src/buffer.c
    ${MINHOOK_DIR}/src/hde/hde32.c
    ${MINHOOK_DIR}/src/hde/hde64.c
)

add_executable(menu_preview
    preview.cpp
    ../overlay.cpp
    ${IMGUI_DIR}/imgui.cpp
    ${IMGUI_DIR}/imgui_draw.cpp
    ${IMGUI_DIR}/imgui_tables.cpp
    ${IMGUI_DIR}/imgui_widgets.cpp
    ${IMGUI_DIR}/backends/imgui_impl_dx11.cpp
    ${IMGUI_DIR}/backends/imgui_impl_win32.cpp
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
    ${FRAMEWORK_DIR}/textfield.cpp
    ${FRAMEWORK_DIR}/texture_loader.cpp
    ${MINHOOK_SOURCES}
)

target_include_directories(menu_preview PRIVATE
    ${MINHOOK_DIR}/include
    ${CMAKE_SOURCE_DIR}/..
    ${FRAMEWORK_DIR}
    ${IMGUI_DIR}
    ${IMGUI_DIR}/backends
)

target_compile_definitions(menu_preview PRIVATE
    IMGUI_DEFINE_MATH_OPERATORS
    WIN32_LEAN_AND_MEAN
    PREVIEW_DEBUG
)

target_link_libraries(menu_preview PRIVATE
    d3d11.lib
    dxgi.lib
    user32.lib
)
