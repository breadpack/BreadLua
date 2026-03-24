using System;
using System.Runtime.InteropServices;
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
