using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HaselTweaks.InteropSourceGenerators.Extensions;

internal static class StructDeclarationSyntaxExtensions
{
    public static string GetNameWithTypeDeclarationList(this StructDeclarationSyntax structDeclarationSyntax)
    {
        return structDeclarationSyntax.Identifier.ToString() + structDeclarationSyntax.TypeParameterList;
    }
}
