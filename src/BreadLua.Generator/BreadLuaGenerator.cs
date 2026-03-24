using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using BreadPack.NativeLua.Generator.Bind;
using BreadPack.NativeLua.Generator.Bridge;
using BreadPack.NativeLua.Generator.Module;

namespace BreadPack.NativeLua.Generator
{
    [Generator(LanguageNames.CSharp)]
    public sealed class BreadLuaGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var bridgeStructs = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    "BreadPack.NativeLua.LuaBridgeAttribute",
                    predicate: (node, _) => node is StructDeclarationSyntax,
                    transform: (ctx, _) => BridgeAnalyzer.Analyze(ctx))
                .Where(info => info != null);

            context.RegisterSourceOutput(bridgeStructs, (spc, info) =>
            {
                if (info == null) return;

                spc.AddSource(info.TypeName + "Bridge.g.cs", BridgeCSharpEmitter.Emit(info));
                spc.AddSource("bread_" + info.LuaName + ".c.g.txt", BridgeCEmitter.Emit(info));
                spc.AddSource(info.LuaName + "_wrapper.lua.g.txt", BridgeLuaEmitter.Emit(info));
            });

            var moduleClasses = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    "BreadPack.NativeLua.LuaModuleAttribute",
                    predicate: (node, _) => node is ClassDeclarationSyntax,
                    transform: (ctx, _) => ModuleAnalyzer.Analyze(ctx))
                .Where(info => info != null);

            context.RegisterSourceOutput(moduleClasses, (spc, info) =>
            {
                if (info == null) return;

                spc.AddSource(info.ClassName + "Module.g.cs", ModuleCSharpEmitter.Emit(info));
                spc.AddSource("bread_" + info.LuaModuleName + "_module.c.g.txt", ModuleCEmitter.Emit(info));
            });

            var bindClasses = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    "BreadPack.NativeLua.LuaBindAttribute",
                    predicate: (node, _) => node is ClassDeclarationSyntax,
                    transform: (ctx, _) => BindAnalyzer.Analyze(ctx))
                .Where(info => info != null);

            context.RegisterSourceOutput(bindClasses, (spc, info) =>
            {
                if (info == null) return;

                spc.AddSource(info.ClassName + "Bind.g.cs", BindCSharpEmitter.Emit(info));
                spc.AddSource("bread_" + info.ClassName + "_bind.c.g.txt", BindCEmitter.Emit(info));
                spc.AddSource(info.ClassName + "_wrapper.lua.g.txt", BindLuaEmitter.Emit(info));
            });
        }
    }
}
