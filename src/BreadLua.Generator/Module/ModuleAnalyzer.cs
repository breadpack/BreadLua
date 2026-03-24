using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using BreadPack.NativeLua.Generator.Util;

namespace BreadPack.NativeLua.Generator.Module
{
    internal sealed class ModuleInfo
    {
        public string Namespace { get; set; }
        public string ClassName { get; set; }
        public string LuaModuleName { get; set; }
        public List<ExportedMethod> Methods { get; set; }

        public ModuleInfo()
        {
            Namespace = "";
            ClassName = "";
            LuaModuleName = "";
            Methods = new List<ExportedMethod>();
        }
    }

    internal sealed class ExportedMethod
    {
        public string CsName { get; set; }
        public string LuaName { get; set; }
        public string ReturnType { get; set; }
        public List<MethodParam> Parameters { get; set; }

        public ExportedMethod()
        {
            CsName = "";
            LuaName = "";
            ReturnType = "void";
            Parameters = new List<MethodParam>();
        }
    }

    internal sealed class MethodParam
    {
        public string Name { get; set; }
        public string CsType { get; set; }

        public MethodParam()
        {
            Name = "";
            CsType = "";
        }
    }

    internal static class ModuleAnalyzer
    {
        public static ModuleInfo Analyze(GeneratorAttributeSyntaxContext context)
        {
            var symbol = context.TargetSymbol as INamedTypeSymbol;
            if (symbol == null) return null;

            var moduleAttr = symbol.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass != null && a.AttributeClass.Name == "LuaModuleAttribute");
            if (moduleAttr == null) return null;

            string moduleName = moduleAttr.ConstructorArguments.Length > 0
                ? moduleAttr.ConstructorArguments[0].Value as string ?? symbol.Name
                : symbol.Name;

            var methods = new List<ExportedMethod>();
            foreach (var member in symbol.GetMembers().OfType<IMethodSymbol>())
            {
                if (!member.IsStatic) continue;
                if (member.MethodKind != MethodKind.Ordinary) continue;

                var exportAttr = member.GetAttributes()
                    .FirstOrDefault(a => a.AttributeClass != null && a.AttributeClass.Name == "LuaExportAttribute");
                if (exportAttr == null) continue;

                string luaName = null;
                if (exportAttr.ConstructorArguments.Length > 0)
                {
                    luaName = exportAttr.ConstructorArguments[0].Value as string;
                }
                if (string.IsNullOrEmpty(luaName))
                {
                    luaName = NamingHelper.ToSnakeCase(member.Name);
                }

                var parameters = new List<MethodParam>();
                foreach (var p in member.Parameters)
                {
                    string csType = MapSpecialType(p.Type);
                    parameters.Add(new MethodParam { Name = p.Name, CsType = csType });
                }

                string returnType = member.ReturnsVoid ? "void" : MapSpecialType(member.ReturnType);

                methods.Add(new ExportedMethod
                {
                    CsName = member.Name,
                    LuaName = luaName,
                    ReturnType = returnType,
                    Parameters = parameters,
                });
            }

            return new ModuleInfo
            {
                Namespace = symbol.ContainingNamespace.ToDisplayString(),
                ClassName = symbol.Name,
                LuaModuleName = moduleName,
                Methods = methods,
            };
        }

        private static string MapSpecialType(ITypeSymbol type)
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
