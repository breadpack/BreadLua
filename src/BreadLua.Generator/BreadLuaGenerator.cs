using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using BreadPack.NativeLua.Generator.Bridge;

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
        }
    }
}
