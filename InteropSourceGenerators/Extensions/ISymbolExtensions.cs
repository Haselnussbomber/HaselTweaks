using LanguageExt;
using Microsoft.CodeAnalysis;

namespace HaselTweaks.InteropSourceGenerators.Extensions;

internal static class SymbolExtensions
{
    public static Option<AttributeData> GetFirstAttributeDataByTypeName(this ISymbol symbol, string typeName)
    {
        return symbol.GetAttributes()
            .FirstOrDefault(attributeData => attributeData.AttributeClass?.ToDisplayString() == typeName);
    }
}
