using System;
namespace BreadPack.NativeLua;
[AttributeUsage(AttributeTargets.Method)]
public sealed class LuaExportAttribute : Attribute
{
    public string? Name { get; }
    public LuaExportAttribute(string? name = null) { Name = name; }
}
