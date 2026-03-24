using System;
namespace BreadPack.NativeLua;
[AttributeUsage(AttributeTargets.Class)]
public sealed class LuaModuleAttribute : Attribute
{
    public string Name { get; }
    public LuaModuleAttribute(string name) { Name = name; }
}
