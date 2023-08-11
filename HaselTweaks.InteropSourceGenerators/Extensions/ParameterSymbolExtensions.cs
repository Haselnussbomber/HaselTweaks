using LanguageExt;
using Microsoft.CodeAnalysis;
using static LanguageExt.Prelude;

namespace HaselTweaks.InteropSourceGenerators.Extensions;

public static class ParameterSymbolExtensions
{
    public static Option<string> GetDefaultValueString(this IParameterSymbol symbol)
    {
        if (symbol.HasExplicitDefaultValue)
        {
            var defaultValue = symbol.ExplicitDefaultValue;
            return defaultValue switch
            {
                bool boolValue => boolValue ? Some("true") : Some("false"),
                _ => defaultValue is null ? None : Some(defaultValue.ToString()),
            };
        }

        return None;
    }
}
