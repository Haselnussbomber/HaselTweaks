using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HaselTweaks.InteropSourceGenerators.Extensions;

internal static class SyntaxNodeExtensions
{
    public static string GetContainingFileScopedNamespace(this SyntaxNode syntaxNode)
    {
        var potentialNamespaceParentNode = syntaxNode;

        while (potentialNamespaceParentNode != null &&
               potentialNamespaceParentNode is not FileScopedNamespaceDeclarationSyntax)
        {
            potentialNamespaceParentNode = potentialNamespaceParentNode.Parent;
        }

        return potentialNamespaceParentNode is FileScopedNamespaceDeclarationSyntax namespaceNode
            ? namespaceNode.Name.ToString()
            : string.Empty;
    }
}
