namespace BreadPack.NativeLua;

public sealed class LuaConfig
{
    public bool OpenStandardLibs { get; set; } = true;
    public string? ScriptBasePath { get; set; }
}
