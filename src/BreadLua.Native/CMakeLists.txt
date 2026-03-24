cmake_minimum_required(VERSION 3.16)
project(breadlua_native C)

set(CMAKE_C_STANDARD 11)

# Lua 5.4 source
file(GLOB LUA_SOURCES "lua54/*.c")
list(REMOVE_ITEM LUA_SOURCES
    "${CMAKE_CURRENT_SOURCE_DIR}/lua54/lua.c"
    "${CMAKE_CURRENT_SOURCE_DIR}/lua54/luac.c"
)

# BreadLua bridge source
file(GLOB BREAD_SOURCES "src/*.c")

add_library(breadlua_native SHARED ${LUA_SOURCES} ${BREAD_SOURCES})

target_include_directories(breadlua_native PRIVATE lua54)

if(WIN32)
    target_compile_definitions(breadlua_native PRIVATE LUA_BUILD_AS_DLL)
endif()

set_target_properties(breadlua_native PROPERTIES
    LIBRARY_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin"
    RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin"
)
