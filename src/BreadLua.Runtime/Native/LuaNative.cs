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
}
