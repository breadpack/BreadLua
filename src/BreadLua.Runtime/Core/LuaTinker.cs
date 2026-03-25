using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BreadPack.NativeLua.Native;

namespace BreadPack.NativeLua;

public class LuaTinker
{
    private readonly LuaState _state;
    private static readonly Dictionary<string, Delegate> _bindings = new();
    private static bool _callbackRegistered;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int GenericCallbackDelegate(IntPtr L, IntPtr namePtr);

    private static GenericCallbackDelegate? _callbackDelegate;

    internal LuaTinker(LuaState state)
    {
        _state = state;
        EnsureCallbackRegistered();
    }

    private static void EnsureCallbackRegistered()
    {
        if (_callbackRegistered) return;
        _callbackDelegate = OnGenericCallback;
        var ptr = Marshal.GetFunctionPointerForDelegate(_callbackDelegate);
        LuaNative.breadlua_set_generic_callback(ptr);
        _callbackRegistered = true;
    }

    public void Bind(string name, Func<int, int, int> func)
    {
        _bindings[name] = func;
        LuaNative.breadlua_register_callback(_state.Handle, name);
    }

    public void Bind(string name, Func<float, float, float> func)
    {
        _bindings[name] = func;
        LuaNative.breadlua_register_callback(_state.Handle, name);
    }

    public void Bind(string name, Func<string, string> func)
    {
        _bindings[name] = func;
        LuaNative.breadlua_register_callback(_state.Handle, name);
    }

    public void Bind(string name, Action<string> func)
    {
        _bindings[name] = func;
        LuaNative.breadlua_register_callback(_state.Handle, name);
    }

    public void Bind(string name, Action func)
    {
        _bindings[name] = func;
        LuaNative.breadlua_register_callback(_state.Handle, name);
    }

    public void Bind(string name, Func<double> func)
    {
        _bindings[name] = func;
        LuaNative.breadlua_register_callback(_state.Handle, name);
    }

    [AOT.MonoPInvokeCallback(typeof(GenericCallbackDelegate))]
    private static int OnGenericCallback(IntPtr L, IntPtr namePtr)
    {
        string? name = Marshal.PtrToStringUTF8(namePtr);
        if (name == null || !_bindings.TryGetValue(name, out var del))
            return 0;

        try
        {
            if (del is Func<int, int, int> intFunc)
            {
                int a = (int)LuaNative.breadlua_tointeger(L, 1);
                int b = (int)LuaNative.breadlua_tointeger(L, 2);
                int result = intFunc(a, b);
                LuaNative.breadlua_pushinteger(L, result);
                return 1;
            }

            if (del is Func<float, float, float> floatFunc)
            {
                float a = (float)LuaNative.breadlua_tonumber(L, 1);
                float b = (float)LuaNative.breadlua_tonumber(L, 2);
                float result = floatFunc(a, b);
                LuaNative.breadlua_pushnumber(L, result);
                return 1;
            }

            if (del is Func<string, string> strFunc)
            {
                IntPtr ptr = LuaNative.breadlua_tostring(L, 1);
                string arg = ptr == IntPtr.Zero ? "" : Marshal.PtrToStringUTF8(ptr) ?? "";
                string result = strFunc(arg);
                LuaNative.breadlua_pushstring(L, result);
                return 1;
            }

            if (del is Action<string> strAction)
            {
                IntPtr ptr = LuaNative.breadlua_tostring(L, 1);
                string arg = ptr == IntPtr.Zero ? "" : Marshal.PtrToStringUTF8(ptr) ?? "";
                strAction(arg);
                return 0;
            }

            if (del is Action action)
            {
                action();
                return 0;
            }

            if (del is Func<double> doubleFunc)
            {
                double result = doubleFunc();
                LuaNative.breadlua_pushnumber(L, result);
                return 1;
            }
        }
        catch (Exception ex)
        {
            LuaNative.breadlua_pushstring(L, ex.Message);
            return -1;
        }

        return 0;
    }
}

#if !UNITY_5_3_OR_NEWER
namespace AOT
{
    [System.AttributeUsage(System.AttributeTargets.Method)]
    internal class MonoPInvokeCallbackAttribute : System.Attribute
    {
        public MonoPInvokeCallbackAttribute(System.Type type) { }
    }
}
#endif
