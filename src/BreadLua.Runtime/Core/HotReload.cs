using System;
using System.IO;

namespace BreadPack.NativeLua;

public class HotReload : IDisposable
{
    private readonly LuaState _state;
    private FileSystemWatcher? _watcher;

    internal HotReload(LuaState state)
    {
        _state = state;
    }

    public void Reload(string path)
    {
        _state.DoFile(path);
    }

    public void WatchAndReload(string directory, string filter = "*.lua")
    {
        _watcher?.Dispose();
        _watcher = new FileSystemWatcher(directory, filter)
        {
            NotifyFilter = NotifyFilters.LastWrite,
            EnableRaisingEvents = true,
        };
        _watcher.Changed += (_, e) =>
        {
            try
            {
                // Small delay to avoid file lock issues
                System.Threading.Thread.Sleep(100);
                _state.DoFile(e.FullPath);
            }
            catch (LuaException ex)
            {
                Console.Error.WriteLine($"[BreadLua] Reload error in {e.Name}: {ex.Message}");
            }
        };
    }

    public void StopWatching()
    {
        _watcher?.Dispose();
        _watcher = null;
    }

    public void Dispose()
    {
        _watcher?.Dispose();
    }
}
