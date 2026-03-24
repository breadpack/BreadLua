using System;
using System.Runtime.InteropServices;

namespace BreadPack.NativeLua.Native;

internal static class LuaNative
{
    private const string Lib = LuaConstants.NativeLib;

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr breadlua_new();

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void breadlua_close(IntPtr L);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int breadlua_dostring(IntPtr L, string code);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int breadlua_dofile(IntPtr L, string path);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr breadlua_tostring(IntPtr L, int index);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int breadlua_pcall_global(IntPtr L, string funcName, int nargs, int nresults);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void breadlua_push_lightuserdata(IntPtr L, IntPtr ptr);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void breadlua_setglobal(IntPtr L, string name);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void breadlua_pushinteger(IntPtr L, long val);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void breadlua_pushnumber(IntPtr L, double val);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void breadlua_pushboolean(IntPtr L, int val);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void breadlua_pushstring(IntPtr L, string s);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int breadlua_type(IntPtr L, int index);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern long breadlua_tointeger(IntPtr L, int index);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern double breadlua_tonumber(IntPtr L, int index);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int breadlua_toboolean(IntPtr L, int index);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void breadlua_pop(IntPtr L, int n);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int breadlua_gettop(IntPtr L);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void breadlua_register_fn(string name, IntPtr fnPtr);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr breadlua_get_fn(string name);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void breadlua_set_release_fn(IntPtr releaseFn);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void breadlua_push_object(IntPtr L, IntPtr gcHandle, string metatableName);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr breadlua_get_object(IntPtr L, int index);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void breadlua_create_metatable(IntPtr L, string name);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void breadlua_set_metatable_fn(IntPtr L, string mtName, string fnName, IntPtr fn);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void breadlua_set_generic_callback(IntPtr fn);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void breadlua_register_callback(IntPtr L, string name);
}
