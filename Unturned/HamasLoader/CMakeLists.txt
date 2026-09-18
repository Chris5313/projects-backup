cmake_minimum_required(VERSION 3.20)
project(HamasClient LANGUAGES CXX C)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# ImGui sources
set(IMGUI_DIR ${CMAKE_SOURCE_DIR}/vendor/imgui)
set(IMGUI_SOURCES
    ${IMGUI_DIR}/imgui.cpp
    ${IMGUI_DIR}/imgui_draw.cpp
    ${IMGUI_DIR}/imgui_tables.cpp
    ${IMGUI_DIR}/imgui_widgets.cpp
    ${IMGUI_DIR}/backends/imgui_impl_dx11.cpp
    ${IMGUI_DIR}/backends/imgui_impl_win32.cpp
)

# Application sources — exclude backup files with (1) in name
file(GLOB APP_SOURCES_ALL ${CMAKE_SOURCE_DIR}/src/*.cpp)
list(FILTER APP_SOURCES_ALL EXCLUDE REGEX "\\(1\\)\\.cpp$")
set(APP_SOURCES ${APP_SOURCES_ALL})

add_executable(HamasClient WIN32
    ${APP_SOURCES}
    ${IMGUI_SOURCES}
    ${CMAKE_SOURCE_DIR}/src/resources.rc
)

target_include_directories(HamasClient PRIVATE
    ${IMGUI_DIR}
    ${IMGUI_DIR}/backends
    ${CMAKE_SOURCE_DIR}/src
)


# v13 TEST: NO VMPROTECT for static build
target_link_libraries(HamasClient PRIVATE
    d3d11.lib
    d3dcompiler.lib
    dwmapi.lib
    winmm.lib
    advapi32.lib
    ntdll.lib
    winhttp.lib
    iphlpapi.lib
    crypt32.lib
    bcrypt.lib
    # VMProtect disabled for test build
    # "C:/Users/Shadow/Documents/IC2/IC2/VMProtect/VMProtect/VMProtect_Ultimate_v3.8.4_Build_1754/Lib/Windows/VMProtectSDK64.lib"
)

# Minimize size + SSE2 baseline
# Suppress linker warning about missing PDB for system libs
target_link_options(HamasClient PRIVATE /IGNORE:4099)

# MAP file — useful for crash analysis
target_link_options(HamasClient PRIVATE "/MAP:${CMAKE_SOURCE_DIR}/build/HamasClient.map")
target_link_options(HamasClient PRIVATE /IGNORE:4099)

