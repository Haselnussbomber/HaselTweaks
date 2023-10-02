using System.Collections.Immutable;
using HaselTweaks.InteropGenerator;
using HaselTweaks.InteropSourceGenerators.Extensions;
using HaselTweaks.InteropSourceGenerators.Models;
using LanguageExt;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static HaselTweaks.InteropSourceGenerators.DiagnosticDescriptors;
using static LanguageExt.Prelude;

namespace HaselTweaks.InteropSourceGenerators;

[Generator]
internal sealed class VTableHookGenerator : IIncrementalGenerator
{
    private const string VTableHookAttributeName = "HaselTweaks.VTableHookAttribute`1";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<(Validation<DiagnosticInfo, ClassInfo> ClassInfo,
            Validation<DiagnosticInfo, VTableHookInfoGeneric> VTableHookInfo)> classAndVTableHookInfosGeneric =
            context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    VTableHookAttributeName,
                    static (node, _) => node is MethodDeclarationSyntax
                    {
                        Parent: ClassDeclarationSyntax,
                        AttributeLists.Count: > 0
                    },
                    static (context, _) =>
                    {
                        var classSyntax = (ClassDeclarationSyntax)context.TargetNode.Parent!;

                        var methodSyntax = (MethodDeclarationSyntax)context.TargetNode;
                        var methodSymbol = (IMethodSymbol)context.TargetSymbol;

                        return (Class: ClassInfo.GetFromSyntax(classSyntax),
                            Info: VTableHookInfoGeneric.GetFromRoslyn(methodSyntax, methodSymbol));
                    });

        // group by class
        IncrementalValuesProvider<(Validation<DiagnosticInfo, ClassInfo> ClassInfo,
            Validation<DiagnosticInfo, Seq<VTableHookInfoGeneric>> VTableHookInfos)> groupedClassInfoWithMemberInfosGeneric =
            classAndVTableHookInfosGeneric.TupleGroupByValidation();

        // make sure caching is working
        var classWithMemberInfosGeneric =
            groupedClassInfoWithMemberInfosGeneric.Select(static (item, _) =>
                (item.ClassInfo, item.VTableHookInfos).Apply(static (si, mfi) =>
                    new ClassWithVTableHookInfosGeneric(si, mfi))
            );

        context.RegisterSourceOutput(classWithMemberInfosGeneric, (sourceContext, item) =>
        {
            item.Match(
                Fail: diagnosticInfos =>
                {
                    diagnosticInfos.Iter(dInfo => sourceContext.ReportDiagnostic(dInfo.ToDiagnostic()));
                },
                Succ: classWithMemberInfoGeneric =>
                {
                    sourceContext.AddSource(classWithMemberInfoGeneric.GetFileName(), classWithMemberInfoGeneric.RenderSource());
                });
        });
    }

    internal sealed record VTableHookInfoGeneric(MethodInfo MethodInfo, string structName, int vTableIndex)
    {
        public static Validation<DiagnosticInfo, VTableHookInfoGeneric> GetFromRoslyn(
            MethodDeclarationSyntax methodSyntax, IMethodSymbol methodSymbol)
        {
            var validSyntax = Success<DiagnosticInfo, MethodDeclarationSyntax>(methodSyntax);
            var validSymbol = Success<DiagnosticInfo, IMethodSymbol>(methodSymbol);
            var paramInfos =
                methodSymbol.Parameters.Select(ParameterInfo.GetFromSymbol).Sequence();

            var validMethodInfo = (validSyntax, validSymbol, paramInfos).Apply(static (syntax, symbol, pInfos) =>
                new MethodInfo(
                    symbol.Name,
                    syntax.Modifiers.ToString(),
                    symbol.ReturnType.GetFullyQualifiedNameWithGenerics(),
                    symbol.IsStatic,
                    pInfos.ToImmutableArray()
                ));

            var hookAttr = methodSymbol.GetFirstAttributeDataByTypeName(VTableHookAttributeName.Replace("`1", ""));

            var validStructName = hookAttr
                .Select(attributeData => attributeData.AttributeClass?.TypeArguments[0].GetFullyQualifiedNameWithGenerics())
                .ToValidation(DiagnosticInfo.Create(AttributeGenericTypeArgumentInvalid, methodSymbol, VTableHookAttributeName.Replace("`1", "")));

            var validVTableIndex = hookAttr
                .GetValidAttributeArgument<int>("VTableIndex", 0, VTableHookAttributeName.Replace("`1", ""), methodSymbol);

            return (validMethodInfo, validStructName, validVTableIndex).Apply((methodInfo, structName, vTableIndex) =>
                new VTableHookInfoGeneric(methodInfo, structName ?? "", vTableIndex));
        }

        public void RenderDelegate(IndentedStringBuilder builder)
        {
            builder.AppendLine($"private delegate {MethodInfo.ReturnType} {MethodInfo.Name}Delegate({MethodInfo.GetParameterTypesAndNamesString()});");
        }

        public void RenderHook(IndentedStringBuilder builder)
        {
            builder.AppendLine($"private Dalamud.Hooking.Hook<{MethodInfo.Name}Delegate> {MethodInfo.Name}Hook {{ get; set; }} = null!;");
        }

        public void RenderSetupHook(IndentedStringBuilder builder)
        {
            builder.AppendLine($"{MethodInfo.Name}Hook = HaselTweaks.Service.GameInteropProvider.HookFromFunctionPointerVariable<{MethodInfo.Name}Delegate>((nint)({structName}.StaticAddressPointers.VTable + 8 * {vTableIndex}), {MethodInfo.Name});");
        }
    }

    private sealed record ClassWithVTableHookInfosGeneric(ClassInfo ClassInfo,
        Seq<VTableHookInfoGeneric> VTableHookInfos)
    {
        public string RenderSource()
        {
            IndentedStringBuilder builder = new();

            ClassInfo.RenderStart(builder);

            VTableHookInfos.Iter(mfi => mfi.RenderDelegate(builder));
            VTableHookInfos.Iter(mfi => mfi.RenderHook(builder));

            builder.AppendLine("");
            builder.AppendLine("public override void SetupVTableHooks()");
            builder.AppendLine("{");
            builder.Indent();
            VTableHookInfos.Iter(mfi => mfi.RenderSetupHook(builder));
            builder.DecrementIndent();
            builder.AppendLine("}");

            ClassInfo.RenderEnd(builder);

            return builder.ToString();
        }

        public string GetFileName()
        {
            return $"{ClassInfo.Namespace}.{ClassInfo.Name}.VTableHooksGeneric.g.cs";
        }
    }
}
