using System;

namespace BreadPack.NativeLua;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
public sealed class LuaIgnoreAttribute : Attribute { }
