using HaselTweaks.InteropSourceGenerators.Models;
using LanguageExt;
using Microsoft.CodeAnalysis;
using static HaselTweaks.InteropSourceGenerators.DiagnosticDescriptors;

namespace HaselTweaks.InteropSourceGenerators.Extensions;

internal static class OptionExtensions
{
    public static Validation<DiagnosticInfo, T> GetValidAttributeArgument<T>(this Option<AttributeData> attribute,
        string argumentName, int argumentIndex, string attributeName, ISymbol location)
    {
        return attribute
            .Bind(attributeData => attributeData.GetAttributeArgument<T>(argumentName, argumentIndex))
            .ToValidation(DiagnosticInfo.Create(AttributeArgumentInvalid, location, attributeName, argumentName));
    }
}
