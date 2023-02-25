using System.Collections.Immutable;
using HaselTweaks.InteropGenerator;
using HaselTweaks.InteropSourceGenerators.Extensions;
using HaselTweaks.InteropSourceGenerators.Models;
using LanguageExt;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static LanguageExt.Prelude;

namespace HaselTweaks.InteropSourceGenerators;

[Generator]
internal sealed class SigHookGenerator : IIncrementalGenerator
{
    private const string SigHookAttributeName = "HaselTweaks.SigHookAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<(Validation<DiagnosticInfo, ClassInfo> ClassInfo,
            Validation<DiagnosticInfo, SigHookInfo> SigHookInfo)> classAndSigHookInfos =
            context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    SigHookAttributeName,
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
                            Info: SigHookInfo.GetFromRoslyn(methodSyntax, methodSymbol));
                    });

        // group by class
        IncrementalValuesProvider<(Validation<DiagnosticInfo, ClassInfo> ClassInfo,
            Validation<DiagnosticInfo, Seq<SigHookInfo>> SigHookInfos)> groupedClassInfoWithMemberInfos =
            classAndSigHookInfos.TupleGroupByValidation();

        // make sure caching is working
        var classWithMemberInfos =
            groupedClassInfoWithMemberInfos.Select(static (item, _) =>
                (item.ClassInfo, item.SigHookInfos).Apply(static (si, mfi) =>
                    new ClassWithSigHookInfos(si, mfi))
            );

        context.RegisterSourceOutput(classWithMemberInfos, (sourceContext, item) =>
        {
            item.Match(
                Fail: diagnosticInfos =>
                {
                    diagnosticInfos.Iter(dInfo => sourceContext.ReportDiagnostic(dInfo.ToDiagnostic()));
                },
                Succ: classWithMemberInfo =>
                {
                    sourceContext.AddSource(classWithMemberInfo.GetFileName(), classWithMemberInfo.RenderSource());
                });
        });
    }

    internal sealed record SigHookInfo(MethodInfo MethodInfo, SignatureInfo SignatureInfo)
    {
        public static Validation<DiagnosticInfo, SigHookInfo> GetFromRoslyn(
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

            var validSignature =
                methodSymbol.GetFirstAttributeDataByTypeName(SigHookAttributeName)
                    .GetValidAttributeArgument<string>("Signature", 0, SigHookAttributeName, methodSymbol)
                    .Bind(signatureString => SignatureInfo.GetValidatedSignature(signatureString, methodSymbol));

            return (validMethodInfo, validSignature).Apply((methodInfo, signature) =>
                new SigHookInfo(methodInfo, signature));
        }

        public void RenderDelegate(IndentedStringBuilder builder)
        {
            builder.AppendLine($"private delegate {MethodInfo.ReturnType} {MethodInfo.Name}Delegate({MethodInfo.GetParameterTypesAndNamesString()});");
        }

        public void RenderHook(IndentedStringBuilder builder)
        {
            builder.AppendLine("");
            builder.AppendLine($"[Dalamud.Utility.Signatures.SignatureAttribute(\"{SignatureInfo.Signature}\", DetourName = nameof({MethodInfo.Name}))]");
            builder.AppendLine($"private Dalamud.Hooking.Hook<{MethodInfo.Name}Delegate> {MethodInfo.Name}Hook {{ get; init; }} = null!;");
        }
    }
    private sealed record ClassWithSigHookInfos(ClassInfo ClassInfo,
        Seq<SigHookInfo> SigHookInfos)
    {
        public string RenderSource()
        {
            IndentedStringBuilder builder = new();

            ClassInfo.RenderStart(builder);

            SigHookInfos.Iter(mfi => mfi.RenderDelegate(builder));
            SigHookInfos.Iter(mfi => mfi.RenderHook(builder));

            ClassInfo.RenderEnd(builder);

            return builder.ToString();
        }

        public string GetFileName()
        {
            return $"{ClassInfo.Namespace}.{ClassInfo.Name}.SigHooks.g.cs";
        }
    }
}
