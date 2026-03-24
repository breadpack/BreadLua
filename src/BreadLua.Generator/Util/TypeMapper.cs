using System;

namespace BreadPack.NativeLua.Generator.Util
{
    internal static class TypeMapper
    {
        public static string ToCType(string csType)
        {
            switch (csType)
            {
                case "void": return "void";
                case "int":
                case "Int32": return "int";
                case "long":
                case "Int64": return "long long";
                case "float":
                case "Single": return "float";
                case "double":
                case "Double": return "double";
                case "bool":
                case "Boolean": return "int";
                case "byte":
                case "Byte": return "unsigned char";
                case "short":
                case "Int16": return "short";
                default: throw new NotSupportedException("Unsupported type: " + csType);
            }
        }

        public static string ToLuaPush(string csType)
        {
            switch (csType)
            {
                case "int": case "Int32": case "long": case "Int64":
                case "short": case "Int16": case "byte": case "Byte":
                    return "lua_pushinteger";
                case "float": case "Single": case "double": case "Double":
                    return "lua_pushnumber";
                case "bool": case "Boolean":
                    return "lua_pushboolean";
                default: throw new NotSupportedException("Unsupported type: " + csType);
            }
        }

        public static string ToLuaCheck(string csType)
        {
            switch (csType)
            {
                case "int": case "Int32": case "long": case "Int64":
                case "short": case "Int16": case "byte": case "Byte":
                    return "luaL_checkinteger";
                case "float": case "Single": case "double": case "Double":
                    return "luaL_checknumber";
                case "bool": case "Boolean":
                    return "lua_toboolean";
                default: throw new NotSupportedException("Unsupported type: " + csType);
            }
        }

        public static string ToCCast(string csType)
        {
            switch (csType)
            {
                case "float": case "Single": return "(float)";
                case "int": case "Int32": return "(int)";
                case "short": case "Int16": return "(short)";
                case "byte": case "Byte": return "(unsigned char)";
                default: return "";
            }
        }
    }
}
