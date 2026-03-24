using System;

namespace BreadPack.NativeLua;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class LuaReadOnlyAttribute : Attribute { }
