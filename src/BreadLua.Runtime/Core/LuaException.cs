using System;

namespace BreadPack.NativeLua;

public class LuaException : Exception
{
    public string? LuaStackTrace { get; }
    public string? ScriptFile { get; }
    public int Line { get; }

    public LuaException(string message, string? luaStackTrace = null, string? scriptFile = null, int line = 0)
        : base(message)
    {
        LuaStackTrace = luaStackTrace;
        ScriptFile = scriptFile;
        Line = line;
    }
}
