using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using BreadPack.NativeLua.Native;

namespace BreadPack.NativeLua;

public class LuaState : IDisposable
{
    private IntPtr _L;
    private bool _disposed;

    public IntPtr Handle => _L;

    public LuaState(LuaConfig? config = null)
    {
        _L = LuaNative.breadlua_new();
        if (_L == IntPtr.Zero)
            throw new LuaException("Failed to create Lua state");
    }

    public void DoString(string code)
    {
        ThrowIfDisposed();
        int result = LuaNative.breadlua_dostring(_L, code);
        if (result != LuaConstants.LUA_OK)
        {
            string error = GetTopString() ?? "Unknown Lua error";
            LuaNative.breadlua_pop(_L, 1);
            throw new LuaException(error);
        }
    }

    public void DoFile(string path)
    {
        ThrowIfDisposed();
        int result = LuaNative.breadlua_dofile(_L, path);
        if (result != LuaConstants.LUA_OK)
        {
            string error = GetTopString() ?? "Unknown Lua error";
            LuaNative.breadlua_pop(_L, 1);
            throw new LuaException(error, scriptFile: path);
        }
    }

    public void Call(string funcName)
    {
        ThrowIfDisposed();
        int result = LuaNative.breadlua_pcall_global(_L, funcName, 0, 0);
        if (result != LuaConstants.LUA_OK)
        {
            string error = GetTopString() ?? "Unknown Lua error";
            LuaNative.breadlua_pop(_L, 1);
            throw new LuaException(error);
        }
    }

    public void SetGlobal(string name, IntPtr lightuserdata)
    {
        ThrowIfDisposed();
        LuaNative.breadlua_push_lightuserdata(_L, lightuserdata);
        LuaNative.breadlua_setglobal(_L, name);
    }

    public void SetGlobal(string name, long value)
    {
        ThrowIfDisposed();
        LuaNative.breadlua_pushinteger(_L, value);
        LuaNative.breadlua_setglobal(_L, name);
    }

    public void SetGlobal(string name, double value)
    {
        ThrowIfDisposed();
        LuaNative.breadlua_pushnumber(_L, value);
        LuaNative.breadlua_setglobal(_L, name);
    }

    public void SetGlobal(string name, bool value)
    {
        ThrowIfDisposed();
        LuaNative.breadlua_pushboolean(_L, value ? 1 : 0);
        LuaNative.breadlua_setglobal(_L, name);
    }

    public void SetGlobal(string name, string value)
    {
        ThrowIfDisposed();
        LuaNative.breadlua_pushstring(_L, value);
        LuaNative.breadlua_setglobal(_L, name);
    }

    public T Eval<T>(string expression)
    {
        ThrowIfDisposed();
        string code = "__breadlua_result = " + expression;
        int result = LuaNative.breadlua_dostring(_L, code);
        if (result != LuaConstants.LUA_OK)
        {
            string error = GetTopString() ?? "Lua eval error";
            LuaNative.breadlua_pop(_L, 1);
            throw new LuaException(error);
        }

        LuaNative.breadlua_dostring(_L, "return __breadlua_result");
        T value = ReadValue<T>(-1);
        LuaNative.breadlua_pop(_L, 1);
        LuaNative.breadlua_dostring(_L, "__breadlua_result = nil");
        return value;
    }

    public T Call<T>(string funcName, params object[] args)
    {
        ThrowIfDisposed();
        var sb = new StringBuilder();
        sb.Append(funcName);
        sb.Append('(');
        for (int i = 0; i < args.Length; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(FormatLuaValue(args[i]));
        }
        sb.Append(')');
        return Eval<T>(sb.ToString());
    }

    private T ReadValue<T>(int index)
    {
        object result;

        if (typeof(T) == typeof(int))
            result = (int)LuaNative.breadlua_tointeger(_L, index);
        else if (typeof(T) == typeof(long))
            result = LuaNative.breadlua_tointeger(_L, index);
        else if (typeof(T) == typeof(float))
            result = (float)LuaNative.breadlua_tonumber(_L, index);
        else if (typeof(T) == typeof(double))
            result = LuaNative.breadlua_tonumber(_L, index);
        else if (typeof(T) == typeof(bool))
            result = LuaNative.breadlua_toboolean(_L, index) != 0;
        else if (typeof(T) == typeof(string))
        {
            IntPtr ptr = LuaNative.breadlua_tostring(_L, index);
            result = ptr == IntPtr.Zero ? null! : Marshal.PtrToStringAnsi(ptr)!;
        }
        else
            throw new LuaException("Unsupported return type: " + typeof(T).Name);

        return (T)result;
    }

    private static string FormatLuaValue(object? value)
    {
        if (value == null) return "nil";
        if (value is int i) return i.ToString();
        if (value is long l) return l.ToString();
        if (value is float f) return f.ToString(CultureInfo.InvariantCulture);
        if (value is double d) return d.ToString(CultureInfo.InvariantCulture);
        if (value is bool b) return b ? "true" : "false";
        if (value is string s) return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        return value.ToString()!;
    }

    private LuaTinker? _tinker;
    public LuaTinker Tinker => _tinker ??= new LuaTinker(this);

    private string? GetTopString()
    {
        IntPtr ptr = LuaNative.breadlua_tostring(_L, -1);
        return ptr == IntPtr.Zero ? null : Marshal.PtrToStringAnsi(ptr);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(LuaState));
    }

    public void CreateMetatable(string name)
    {
        ThrowIfDisposed();
        LuaNative.breadlua_create_metatable(_L, name);
    }

    public void PushObject(IntPtr gcHandle, string metatableName)
    {
        ThrowIfDisposed();
        LuaNative.breadlua_push_object(_L, gcHandle, metatableName);
    }

    public void RegisterCFunction(string metatableName, string funcName, IntPtr cFunction)
    {
        ThrowIfDisposed();
        LuaNative.breadlua_set_metatable_fn(_L, metatableName, funcName, cFunction);
    }

    private HotReload? _hotReload;

    public void Reload(string path) => (_hotReload ??= new HotReload(this)).Reload(path);
    public void WatchAndReload(string directory, string filter = "*.lua") => (_hotReload ??= new HotReload(this)).WatchAndReload(directory, filter);
    public void StopWatching() => _hotReload?.StopWatching();

    public void StartRepl() => new Repl(this).Start();

    public void Dispose()
    {
        if (!_disposed)
        {
            _hotReload?.Dispose();
            if (_L != IntPtr.Zero)
            {
                LuaNative.breadlua_close(_L);
                _L = IntPtr.Zero;
            }
            _disposed = true;
        }
    }
}
