using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using BreadPack.NativeLua.Generator.Util;

namespace BreadPack.NativeLua.Generator.Bind
{
    internal sealed class BindClassInfo
    {
        public string Namespace { get; set; }
        public string ClassName { get; set; }
        public List<BindPropertyInfo> Properties { get; set; }
        public List<BindMethodInfo> Methods { get; set; }
        public BindConstructorInfo Constructor { get; set; }

        public BindClassInfo()
        {
            Namespace = "";
            ClassName = "";
            Properties = new List<BindPropertyInfo>();
            Methods = new List<BindMethodInfo>();
        }
    }

    internal sealed class BindPropertyInfo
    {
        public string CsName { get; set; }
        public string LuaName { get; set; }
        public string CsType { get; set; }
        public bool HasGetter { get; set; }
        public bool HasSetter { get; set; }

        public BindPropertyInfo()
        {
            CsName = "";
            LuaName = "";
            CsType = "";
        }
    }

    internal sealed class BindMethodInfo
    {
        public string CsName { get; set; }
        public string LuaName { get; set; }
        public string ReturnType { get; set; }
        public List<BindParamInfo> Parameters { get; set; }

        public BindMethodInfo()
        {
            CsName = "";
            LuaName = "";
            ReturnType = "void";
            Parameters = new List<BindParamInfo>();
        }
    }

    internal sealed class BindParamInfo
    {
        public string Name { get; set; }
        public string CsType { get; set; }

        public BindParamInfo()
        {
            Name = "";
            CsType = "";
        }
    }

    internal sealed class BindConstructorInfo
    {
        public List<BindParamInfo> Parameters { get; set; }

        public BindConstructorInfo()
        {
            Parameters = new List<BindParamInfo>();
        }
    }

    internal static class BindAnalyzer
    {
        public static BindClassInfo Analyze(GeneratorAttributeSyntaxContext context)
        {
            var symbol = context.TargetSymbol as INamedTypeSymbol;
            if (symbol == null) return null;

            var bindAttr = symbol.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass != null && a.AttributeClass.Name == "LuaBindAttribute");
            if (bindAttr == null) return null;

            var info = new BindClassInfo
            {
                Namespace = symbol.ContainingNamespace.ToDisplayString(),
                ClassName = symbol.Name,
            };

            // Properties
            foreach (var member in symbol.GetMembers().OfType<IPropertySymbol>())
            {
                if (member.IsStatic) continue;
                if (member.DeclaredAccessibility != Accessibility.Public) continue;
                if (member.GetAttributes().Any(a => a.AttributeClass != null && a.AttributeClass.Name == "LuaIgnoreAttribute")) continue;

                string luaName = member.Name;
                var fieldAttr = member.GetAttributes()
                    .FirstOrDefault(a => a.AttributeClass != null && a.AttributeClass.Name == "LuaFieldAttribute");
                if (fieldAttr != null && fieldAttr.ConstructorArguments.Length > 0)
                {
                    var nameArg = fieldAttr.ConstructorArguments[0].Value as string;
                    if (!string.IsNullOrEmpty(nameArg)) luaName = nameArg;
                }

                info.Properties.Add(new BindPropertyInfo
                {
                    CsName = member.Name,
                    LuaName = luaName,
                    CsType = MapType(member.Type),
                    HasGetter = member.GetMethod != null,
                    HasSetter = member.SetMethod != null,
                });
            }

            // Methods (instance, public, ordinary, not [LuaIgnore])
            foreach (var member in symbol.GetMembers().OfType<IMethodSymbol>())
            {
                if (member.IsStatic) continue;
                if (member.MethodKind != MethodKind.Ordinary) continue;
                if (member.DeclaredAccessibility != Accessibility.Public) continue;
                if (member.GetAttributes().Any(a => a.AttributeClass != null && a.AttributeClass.Name == "LuaIgnoreAttribute")) continue;

                var methodParams = new List<BindParamInfo>();
                foreach (var p in member.Parameters)
                {
                    methodParams.Add(new BindParamInfo { Name = p.Name, CsType = MapType(p.Type) });
                }

                info.Methods.Add(new BindMethodInfo
                {
                    CsName = member.Name,
                    LuaName = NamingHelper.ToSnakeCase(member.Name),
                    ReturnType = member.ReturnsVoid ? "void" : MapType(member.ReturnType),
                    Parameters = methodParams,
                });
            }

            // Constructor with [LuaConstructor]
            foreach (var ctor in symbol.Constructors)
            {
                if (ctor.GetAttributes().Any(a => a.AttributeClass != null && a.AttributeClass.Name == "LuaConstructorAttribute"))
                {
                    var ctorParams = new List<BindParamInfo>();
                    foreach (var p in ctor.Parameters)
                    {
                        ctorParams.Add(new BindParamInfo { Name = p.Name, CsType = MapType(p.Type) });
                    }
                    info.Constructor = new BindConstructorInfo { Parameters = ctorParams };
                    break;
                }
            }

            return info;
        }

        private static string MapType(ITypeSymbol type)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_Int32: return "int";
                case SpecialType.System_Int64: return "long";
                case SpecialType.System_Single: return "float";
                case SpecialType.System_Double: return "double";
                case SpecialType.System_Boolean: return "bool";
                case SpecialType.System_String: return "string";
                case SpecialType.System_Void: return "void";
                default: return type.ToDisplayString();
            }
        }
    }
}
