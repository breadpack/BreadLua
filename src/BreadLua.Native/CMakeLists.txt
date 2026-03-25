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

# iOS requires static library; allow override via BUILD_SHARED_LIBS=OFF
if(BUILD_SHARED_LIBS OR NOT DEFINED BUILD_SHARED_LIBS)
    if(CMAKE_SYSTEM_NAME STREQUAL "iOS")
        add_library(breadlua_native STATIC ${LUA_SOURCES} ${BREAD_SOURCES})
    else()
        add_library(breadlua_native SHARED ${LUA_SOURCES} ${BREAD_SOURCES})
    endif()
else()
    add_library(breadlua_native STATIC ${LUA_SOURCES} ${BREAD_SOURCES})
endif()

target_include_directories(breadlua_native PRIVATE lua54)

if(WIN32)
    target_compile_definitions(breadlua_native PRIVATE LUA_BUILD_AS_DLL)
endif()

set_target_properties(breadlua_native PROPERTIES
    LIBRARY_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin"
    RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin"
    ARCHIVE_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin"
)

# Platform-specific flags
if(NOT WIN32)
    target_compile_options(breadlua_native PRIVATE -fPIC -fvisibility=hidden)
endif()

if(ANDROID)
    target_compile_definitions(breadlua_native PRIVATE LUA_USE_LINUX _LARGEFILE_SOURCE _FILE_OFFSET_BITS=64)
endif()

if(CMAKE_SYSTEM_NAME STREQUAL "iOS")
    target_compile_definitions(breadlua_native PRIVATE LUA_USE_IOS)
endif()

if(CMAKE_SYSTEM_NAME STREQUAL "Linux")
    target_compile_definitions(breadlua_native PRIVATE LUA_USE_LINUX)
endif()

if(CMAKE_SYSTEM_NAME STREQUAL "Darwin")
    target_compile_definitions(breadlua_native PRIVATE LUA_USE_MACOSX)
endif()
