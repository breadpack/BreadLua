using System;

namespace BreadPack.NativeLua;

[AttributeUsage(AttributeTargets.Struct)]
public sealed class LuaBridgeAttribute : Attribute
{
    public string Name { get; }
    public LuaBridgeAttribute(string name) { Name = name; }
}
