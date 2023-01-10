using FFXIVClientStructs.InteropGenerator;
using HaselTweaks.InteropSourceGenerators.Extensions;
using HaselTweaks.InteropSourceGenerators.Models;
using LanguageExt;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HaselTweaks.InteropSourceGenerators;

[Generator]
internal sealed class CStrOverloadsGenerator : IIncrementalGenerator
{
    private const string AttributeName = "FFXIVClientStructs.Interop.Attributes.GenerateCStrOverloadsAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<(Validation<DiagnosticInfo, StructInfo> StructInfo,
            Validation<DiagnosticInfo, CStrOverloadInfo> CStrOverloadInfos)> structAndMethodInfos =
            context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    AttributeName,
                    static (node, _) => node is MethodDeclarationSyntax
                    {
                        Parent: StructDeclarationSyntax, AttributeLists.Count: > 0
                    },
                    static (context, _) =>
                    {
                        var structSyntax = (StructDeclarationSyntax)context.TargetNode.Parent!;

                        var methodSyntax = (MethodDeclarationSyntax)context.TargetNode;
                        var methodSymbol = (IMethodSymbol)context.TargetSymbol;

                        return (Struct: StructInfo.GetFromSyntax(structSyntax),
                            Info: CStrOverloadInfo.GetFromRoslyn(methodSyntax, methodSymbol));
                    });

        // group by struct
        IncrementalValuesProvider<(Validation<DiagnosticInfo, StructInfo> StructInfo,
            Validation<DiagnosticInfo, Seq<CStrOverloadInfo>> CStrOverloadInfos)> groupedStructInfoWithMethodInfos =
            structAndMethodInfos.TupleGroupByValidation();

        // make sure caching is working
        var structWithMethodInfos =
            groupedStructInfoWithMethodInfos.Select(static (item, _) =>
                (item.StructInfo, item.CStrOverloadInfos).Apply(static (si, csoi) =>
                    new StructWithCStrOverloadInfos(si, csoi))
            );

        context.RegisterSourceOutput(structWithMethodInfos, (sourceContext, item) =>
        {
            item.Match(
                Fail: diagnosticInfos =>
                {
                    diagnosticInfos.Iter(dInfo => sourceContext.ReportDiagnostic(dInfo.ToDiagnostic()));
                },
                Succ: structWithMethodInfo =>
                {
                    sourceContext.AddSource(structWithMethodInfo.GetFileName(), structWithMethodInfo.RenderSource());
                });
        });
    }

    internal sealed record CStrOverloadInfo(MethodInfo MethodInfo, Option<string> IgnoreArgument)
    {
        public static Validation<DiagnosticInfo, CStrOverloadInfo> GetFromRoslyn(
            MethodDeclarationSyntax methodSyntax, IMethodSymbol methodSymbol)
        {
            var validMethodInfo =
                MethodInfo.GetFromRoslyn(methodSyntax, methodSymbol);

            var optionIgnoreArgument =
                methodSymbol.GetFirstAttributeDataByTypeName(AttributeName)
                    .GetValidAttributeArgument<string>("IgnoreArgument", 0, AttributeName, methodSymbol)
                    .ToOption();

            return validMethodInfo.Bind<CStrOverloadInfo>(methodInfo =>
                new CStrOverloadInfo(methodInfo, optionIgnoreArgument));
        }

        public void RenderOverloadMethods(IndentedStringBuilder builder)
        {
            var overloadParamNames =
                MethodInfo.Parameters.Where(param => param.Type == "byte*" && param.Name != IgnoreArgument)
                    .Map(param => param.Name).ToSeq();

            var paramNames = MethodInfo.GetParameterNamesString();
            foreach (var overloadParamName in overloadParamNames)
                paramNames = paramNames.Replace(overloadParamName, $"{overloadParamName}Ptr");

            var returnString = MethodInfo.ReturnType == "void" ? string.Empty : "return ";

            builder.AppendLine();
            MethodInfo.RenderStartOverload(builder, "byte*", "string", IgnoreArgument);
            foreach (var overloadParamName in overloadParamNames)
            {
                builder.AppendLine(
                    $"Span<byte> {overloadParamName}Bytes = {overloadParamName}.Length <= 512 ? stackalloc byte[{overloadParamName}.Length + 1] : new byte[{overloadParamName}.Length + 1];");
                builder.AppendLine(
                    $"global::System.Text.Encoding.UTF8.GetBytes({overloadParamName}, {overloadParamName}Bytes);");
                builder.AppendLine($"{overloadParamName}Bytes[{overloadParamName}.Length] = 0;");
                builder.AppendLine();
            }

            foreach (var overloadParamName in overloadParamNames)
            {
                builder.AppendLine($"fixed (byte* {overloadParamName}Ptr = {overloadParamName}Bytes)");
                builder.AppendLine("{");
                builder.Indent();
            }

            builder.AppendLine($"{returnString}{MethodInfo.Name}({paramNames});");

            foreach (var _ in overloadParamNames)
            {
                builder.DecrementIndent();
                builder.AppendLine("}");
            }

            MethodInfo.RenderEnd(builder);

            builder.AppendLine();
            MethodInfo.RenderStartOverload(builder, "byte*", "ReadOnlySpan<byte>", IgnoreArgument);
            foreach (var overloadParamName in overloadParamNames)
            {
                builder.AppendLine($"fixed (byte* {overloadParamName}Ptr = {overloadParamName})");
                builder.AppendLine("{");
                builder.Indent();
            }

            builder.AppendLine($"{returnString}{MethodInfo.Name}({paramNames});");

            foreach (var _ in overloadParamNames)
            {
                builder.DecrementIndent();
                builder.AppendLine("}");
            }

            MethodInfo.RenderEnd(builder);
        }
    }

    private sealed record StructWithCStrOverloadInfos(StructInfo StructInfo, Seq<CStrOverloadInfo> CStrOverloadInfos)
    {
        public string RenderSource()
        {
            IndentedStringBuilder builder = new();

            StructInfo.RenderStart(builder);

            CStrOverloadInfos.Iter(csoi => csoi.RenderOverloadMethods(builder));

            StructInfo.RenderEnd(builder);

            return builder.ToString();
        }

        public string GetFileName()
        {
            return $"{StructInfo.Namespace}.{StructInfo.Name}.CStrOverloads.g.cs";
        }
    }
}
