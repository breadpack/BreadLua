using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace BreadPack.NativeLua.Generator.Bridge
{
    internal sealed class BridgeStructInfo
    {
        public string Namespace { get; set; }
        public string TypeName { get; set; }
        public string LuaName { get; set; }
        public List<BridgeFieldInfo> Fields { get; set; }

        public BridgeStructInfo()
        {
            Namespace = "";
            TypeName = "";
            LuaName = "";
            Fields = new List<BridgeFieldInfo>();
        }
    }

    internal sealed class BridgeFieldInfo
    {
        public string CsName { get; set; }
        public string LuaName { get; set; }
        public string CsType { get; set; }
        public bool IsReadOnly { get; set; }

        public BridgeFieldInfo()
        {
            CsName = "";
            LuaName = "";
            CsType = "";
        }
    }

    internal static class BridgeAnalyzer
    {
        public static BridgeStructInfo Analyze(GeneratorAttributeSyntaxContext context)
        {
            var symbol = context.TargetSymbol as INamedTypeSymbol;
            if (symbol == null) return null;

            var attr = symbol.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass != null && a.AttributeClass.Name == "LuaBridgeAttribute");
            if (attr == null) return null;

            string luaName = attr.ConstructorArguments.Length > 0
                ? attr.ConstructorArguments[0].Value as string ?? symbol.Name
                : symbol.Name;

            var fields = new List<BridgeFieldInfo>();
            foreach (var member in symbol.GetMembers().OfType<IFieldSymbol>())
            {
                if (member.IsStatic || member.IsConst) continue;
                if (member.DeclaredAccessibility != Accessibility.Public) continue;
                if (member.GetAttributes().Any(a => a.AttributeClass != null && a.AttributeClass.Name == "LuaIgnoreAttribute")) continue;

                string fieldLuaName = member.Name;
                var fieldAttr = member.GetAttributes()
                    .FirstOrDefault(a => a.AttributeClass != null && a.AttributeClass.Name == "LuaFieldAttribute");
                if (fieldAttr != null && fieldAttr.ConstructorArguments.Length > 0)
                {
                    var nameArg = fieldAttr.ConstructorArguments[0].Value as string;
                    if (!string.IsNullOrEmpty(nameArg))
                        fieldLuaName = nameArg;
                }

                bool isReadOnly = member.GetAttributes()
                    .Any(a => a.AttributeClass != null && a.AttributeClass.Name == "LuaReadOnlyAttribute");

                string csType;
                switch (member.Type.SpecialType)
                {
                    case SpecialType.System_Int32: csType = "int"; break;
                    case SpecialType.System_Int64: csType = "long"; break;
                    case SpecialType.System_Single: csType = "float"; break;
                    case SpecialType.System_Double: csType = "double"; break;
                    case SpecialType.System_Boolean: csType = "bool"; break;
                    case SpecialType.System_Int16: csType = "short"; break;
                    case SpecialType.System_Byte: csType = "byte"; break;
                    default: csType = member.Type.ToDisplayString(); break;
                }

                fields.Add(new BridgeFieldInfo
                {
                    CsName = member.Name,
                    LuaName = fieldLuaName,
                    CsType = csType,
                    IsReadOnly = isReadOnly,
                });
            }

            return new BridgeStructInfo
            {
                Namespace = symbol.ContainingNamespace.ToDisplayString(),
                TypeName = symbol.Name,
                LuaName = luaName,
                Fields = fields,
            };
        }
    }
}
