#include "lua.h"
#include "lauxlib.h"
#include <string.h>

#ifdef _WIN32
#define BREADLUA_EXPORT __declspec(dllexport)
#else
#define BREADLUA_EXPORT __attribute__((visibility("default")))
#endif

typedef struct {
    void* gc_handle;
} bread_object_t;

typedef void (*bread_release_fn)(void* gc_handle);
static bread_release_fn g_release_fn = NULL;

BREADLUA_EXPORT void breadlua_set_release_fn(void* fn) {
    g_release_fn = (bread_release_fn)fn;
}

static int bread_object_gc(lua_State* L) {
    bread_object_t* obj = (bread_object_t*)lua_touserdata(L, 1);
    if (obj && obj->gc_handle && g_release_fn) {
        g_release_fn(obj->gc_handle);
        obj->gc_handle = NULL;
    }
    return 0;
}

BREADLUA_EXPORT void breadlua_push_object(lua_State* L, void* gc_handle, const char* metatable_name) {
    bread_object_t* obj = (bread_object_t*)lua_newuserdata(L, sizeof(bread_object_t));
    obj->gc_handle = gc_handle;
    luaL_getmetatable(L, metatable_name);
    lua_setmetatable(L, -2);
}

BREADLUA_EXPORT void* breadlua_get_object(lua_State* L, int index) {
    bread_object_t* obj = (bread_object_t*)lua_touserdata(L, index);
    if (obj == NULL) return NULL;
    return obj->gc_handle;
}

BREADLUA_EXPORT void breadlua_create_metatable(lua_State* L, const char* name) {
    luaL_newmetatable(L, name);
    lua_pushvalue(L, -1);
    lua_setfield(L, -2, "__index");
    lua_pushcfunction(L, bread_object_gc);
    lua_setfield(L, -2, "__gc");
    lua_pop(L, 1);
}

BREADLUA_EXPORT void breadlua_set_metatable_fn(lua_State* L, const char* mt_name, const char* fn_name, lua_CFunction fn) {
    luaL_getmetatable(L, mt_name);
    if (lua_isnil(L, -1)) {
        lua_pop(L, 1);
        return;
    }
    lua_pushcfunction(L, fn);
    lua_setfield(L, -2, fn_name);
    lua_pop(L, 1);
}
