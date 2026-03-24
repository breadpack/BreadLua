#include <string.h>
#include "lua.h"
#include "lualib.h"
#include "lauxlib.h"

#ifdef _WIN32
#define BREADLUA_EXPORT __declspec(dllexport)
#else
#define BREADLUA_EXPORT __attribute__((visibility("default")))
#endif

BREADLUA_EXPORT lua_State* breadlua_new(void) {
    lua_State* L = luaL_newstate();
    luaL_openlibs(L);
    return L;
}

BREADLUA_EXPORT void breadlua_close(lua_State* L) {
    if (L) lua_close(L);
}

BREADLUA_EXPORT int breadlua_dostring(lua_State* L, const char* code) {
    return luaL_dostring(L, code);
}

BREADLUA_EXPORT int breadlua_dofile(lua_State* L, const char* path) {
    return luaL_dofile(L, path);
}

BREADLUA_EXPORT const char* breadlua_tostring(lua_State* L, int index) {
    return lua_tostring(L, index);
}

BREADLUA_EXPORT int breadlua_pcall_global(lua_State* L, const char* func_name, int nargs, int nresults) {
    lua_getglobal(L, func_name);
    return lua_pcall(L, nargs, nresults, 0);
}

BREADLUA_EXPORT void breadlua_push_lightuserdata(lua_State* L, void* ptr) {
    lua_pushlightuserdata(L, ptr);
}

BREADLUA_EXPORT void breadlua_setglobal(lua_State* L, const char* name) {
    lua_setglobal(L, name);
}

BREADLUA_EXPORT void breadlua_pushinteger(lua_State* L, long long val) {
    lua_pushinteger(L, (lua_Integer)val);
}

BREADLUA_EXPORT void breadlua_pushnumber(lua_State* L, double val) {
    lua_pushnumber(L, val);
}

BREADLUA_EXPORT void breadlua_pushboolean(lua_State* L, int val) {
    lua_pushboolean(L, val);
}

BREADLUA_EXPORT void breadlua_pushstring(lua_State* L, const char* s) {
    lua_pushstring(L, s);
}

BREADLUA_EXPORT int breadlua_type(lua_State* L, int index) {
    return lua_type(L, index);
}

BREADLUA_EXPORT long long breadlua_tointeger(lua_State* L, int index) {
    return (long long)lua_tointeger(L, index);
}

BREADLUA_EXPORT double breadlua_tonumber(lua_State* L, int index) {
    return lua_tonumber(L, index);
}

BREADLUA_EXPORT int breadlua_toboolean(lua_State* L, int index) {
    return lua_toboolean(L, index);
}

BREADLUA_EXPORT void breadlua_pop(lua_State* L, int n) {
    lua_pop(L, n);
}

BREADLUA_EXPORT int breadlua_gettop(lua_State* L) {
    return lua_gettop(L);
}

/* Generic callback for LuaTinker Bind() */
typedef int (*bread_generic_callback)(lua_State* L, const char* func_name);
static bread_generic_callback g_generic_callback = NULL;

BREADLUA_EXPORT void breadlua_set_generic_callback(void* fn) {
    g_generic_callback = (bread_generic_callback)fn;
}

static int bread_generic_dispatch(lua_State* L) {
    if (!g_generic_callback) return 0;
    const char* name = lua_tostring(L, lua_upvalueindex(1));
    return g_generic_callback(L, name);
}

BREADLUA_EXPORT void breadlua_register_callback(lua_State* L, const char* name) {
    lua_pushstring(L, name);
    lua_pushcclosure(L, bread_generic_dispatch, 1);
    lua_setglobal(L, name);
}

BREADLUA_EXPORT void breadlua_register_module(lua_State* L, const char* name, lua_CFunction openf) {
    luaL_requiref(L, name, openf, 1);
    lua_pop(L, 1);
}

/* Function pointer registration for [LuaModule] */
#define BREAD_MAX_FN 256
static struct {
    const char* name;
    void* fn_ptr;
} g_fn_registry[BREAD_MAX_FN];
static int g_fn_count = 0;

BREADLUA_EXPORT void breadlua_register_fn(const char* name, void* fn_ptr) {
    if (g_fn_count < BREAD_MAX_FN) {
        g_fn_registry[g_fn_count].name = name;
        g_fn_registry[g_fn_count].fn_ptr = fn_ptr;
        g_fn_count++;
    }
}

BREADLUA_EXPORT void* breadlua_get_fn(const char* name) {
    for (int i = 0; i < g_fn_count; i++) {
        if (strcmp(g_fn_registry[i].name, name) == 0) {
            return g_fn_registry[i].fn_ptr;
        }
    }
    return NULL;
}
