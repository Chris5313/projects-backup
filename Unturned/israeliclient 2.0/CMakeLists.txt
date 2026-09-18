cmake_minimum_required(VERSION 3.20)
project(IsraeliClient2 LANGUAGES CXX C)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# ── BattlEye-safe compiler/linker flags ──
if(MSVC)
    add_compile_options(
        /Oi         # Intrinsic functions (sqrtf → SQRTSS)
        /GS-        # No buffer security cookie
        /Zc:threadSafeInit-  # No CRT thread-safe static init
        /fp:fast    # Fast FP
        /O2         # Full optimization
    )
    add_link_options(
        /DEBUG:NONE
        /OPT:REF
        /OPT:ICF
        /INCREMENTAL:NO
        /MANIFEST:NO
    )
endif()

# Static CRT — no VCRUNTIME140.dll dependency
set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded")

add_compile_definitions(
    IMGUI_DISABLE_DEBUG_TOOLS
    IMGUI_DISABLE_DEMO_WINDOWS
    WIN32_LEAN_AND_MEAN
    NOMINMAX
)

set(IMGUI_DIR ${CMAKE_SOURCE_DIR}/vendor/imgui)
set(MINHOOK_DIR ${CMAKE_SOURCE_DIR}/vendor/minhook)

file(GLOB_RECURSE CHEAT_SOURCES src/*.cpp)
list(FILTER CHEAT_SOURCES EXCLUDE REGEX "test_menu2\\.cpp$")

set(IMGUI_SOURCES
    ${IMGUI_DIR}/imgui.cpp
    ${IMGUI_DIR}/imgui_draw.cpp
    ${IMGUI_DIR}/imgui_tables.cpp
    ${IMGUI_DIR}/imgui_widgets.cpp
    ${IMGUI_DIR}/backends/imgui_impl_dx11.cpp
    ${IMGUI_DIR}/backends/imgui_impl_win32.cpp
)

file(GLOB MINHOOK_SOURCES
    ${MINHOOK_DIR}/src/*.c
    ${MINHOOK_DIR}/src/hde/*.c
)

add_library(IsraeliClient2 SHARED
    ${CHEAT_SOURCES}
    ${IMGUI_SOURCES}
    ${MINHOOK_SOURCES}
)

# Find Windows SDK UCRT
if(MSVC)
    file(GLOB _UCRT_DIRS "C:/Program Files (x86)/Windows Kits/10/Include/*/ucrt")
    list(SORT _UCRT_DIRS ORDER DESCENDING)
    list(GET _UCRT_DIRS 0 _UCRT_DIR)
    message(STATUS "UCRT: ${_UCRT_DIR}")
endif()

target_include_directories(IsraeliClient2 PRIVATE
    ${CMAKE_SOURCE_DIR}/src
    ${CMAKE_SOURCE_DIR}/vendor
    ${IMGUI_DIR}
    ${IMGUI_DIR}/backends
    ${MINHOOK_DIR}/include
)
if(MSVC AND _UCRT_DIR)
    target_include_directories(IsraeliClient2 SYSTEM PRIVATE ${_UCRT_DIR})
endif()

target_link_libraries(IsraeliClient2 PRIVATE
    d3d11
    dxgi
)

set_target_properties(IsraeliClient2 PROPERTIES
    OUTPUT_NAME "israeliclient2"
    RUNTIME_OUTPUT_DIRECTORY "${CMAKE_SOURCE_DIR}/build"
    LIBRARY_OUTPUT_DIRECTORY "${CMAKE_SOURCE_DIR}/build"
)
