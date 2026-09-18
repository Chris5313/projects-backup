cmake_minimum_required(VERSION 3.20)
project(gw2_ui_preview)

set(CMAKE_CXX_STANDARD 20)

# ImGui sources
set(IMGUI_DIR "${CMAKE_SOURCE_DIR}/../vendor/imgui")
set(IMGUI_SOURCES
    ${IMGUI_DIR}/imgui.cpp
    ${IMGUI_DIR}/imgui_draw.cpp
    ${IMGUI_DIR}/imgui_tables.cpp
    ${IMGUI_DIR}/imgui_widgets.cpp
    ${IMGUI_DIR}/backends/imgui_impl_dx11.cpp
    ${IMGUI_DIR}/backends/imgui_impl_win32.cpp
)

add_executable(ui_preview WIN32
    main.cpp
    ${IMGUI_SOURCES}
)

target_include_directories(ui_preview PRIVATE
    ${IMGUI_DIR}
    ${IMGUI_DIR}/backends
)

target_link_libraries(ui_preview PRIVATE
    d3d11
    dxgi
    d3dcompiler
)
