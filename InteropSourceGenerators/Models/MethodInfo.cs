using System.Collections.Immutable;
using HaselTweaks.InteropGenerator;
using HaselTweaks.InteropSourceGenerators.Extensions;
using LanguageExt;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static HaselTweaks.InteropSourceGenerators.DiagnosticDescriptors;
using static LanguageExt.Prelude;

namespace HaselTweaks.InteropSourceGenerators.Models;

internal sealed record MethodInfo(string Name, string Modifiers, string ReturnType, bool IsStatic,
    ImmutableArray<ParameterInfo> Parameters)
{
    public static Validation<DiagnosticInfo, MethodInfo> GetFromRoslyn(MethodDeclarationSyntax methodSyntax,
        IMethodSymbol methodSymbol)
    {
        var validSyntax =
            methodSyntax.HasModifier(SyntaxKind.PartialKeyword)
                ? Success<DiagnosticInfo, MethodDeclarationSyntax>(methodSyntax)
                : Fail<DiagnosticInfo, MethodDeclarationSyntax>(
                    DiagnosticInfo.Create(
                        MethodMustBePartial,
                        methodSyntax,
                        methodSymbol.Name
                    ));
        var validSymbol =
            methodSymbol.ReturnType.IsUnmanagedType
                ? Success<DiagnosticInfo, IMethodSymbol>(methodSymbol)
                : Fail<DiagnosticInfo, IMethodSymbol>(DiagnosticInfo.Create(
                    MethodUsesForbiddenType,
                    methodSyntax,
                    methodSymbol.Name,
                    methodSymbol.ReturnType.GetFullyQualifiedNameWithGenerics()
                ));
        var paramInfos =
            methodSymbol.Parameters.Select(ParameterInfo.GetFromSymbol).Sequence();

        return (validSyntax, validSymbol, paramInfos).Apply(static (syntax, symbol, pInfos) =>
            new MethodInfo(
                symbol.Name,
                syntax.Modifiers.ToString(),
                symbol.ReturnType.GetFullyQualifiedNameWithGenerics(),
                symbol.IsStatic,
                pInfos.ToImmutableArray()
            ));
    }

    public string GetParameterTypeString() => Parameters.Any() ? string.Join(", ", Parameters.Map(p => p.Type)) + ", " : "";

    public string GetParameterNamesString() => string.Join(", ", Parameters.Map(p => p.Name));

    private string GetParameterTypesAndNamesString() => string.Join(", ", Parameters.Map(p => $"{p.Type} {p.Name}"));

    public string GetReturnString() => ReturnType == "void" ? "" : "return ";

    public void RenderStart(IndentedStringBuilder builder)
    {
        builder.AppendLine($"{Modifiers} {ReturnType} {Name}({GetParameterTypesAndNamesString()})");
        builder.AppendLine("{");
        builder.Indent();
    }

    public void RenderStartOverload(IndentedStringBuilder builder, string origType, string replaceType, Option<string> ignoreArgument)
    {
        var paramString = string.Join(", ", Parameters
            .Map(p => p.Type == origType && p.Name != ignoreArgument ? p with { Type = replaceType } : p)
            .Map(p => $"{p.Type} {p.Name}{p.DefaultValue.Match(Some: (p) => $" = {p}", None: "")}"));
        builder.AppendLine($"{Modifiers.Replace(" partial", string.Empty)} {ReturnType} {Name}({paramString})");
        builder.AppendLine("{");
        builder.Indent();
    }

    public void RenderEnd(IndentedStringBuilder builder)
    {
        builder.DecrementIndent();
        builder.AppendLine("}");
    }
}
