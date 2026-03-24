using System;

namespace BreadPack.NativeLua;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class LuaFieldAttribute : Attribute
{
    public string? Name { get; }
    public LuaFieldAttribute(string? name = null) { Name = name; }
}
