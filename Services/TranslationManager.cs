using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Dalamud;
using Dalamud.Game.Text.SeStringHandling;

namespace HaselTweaks.Services;

public class TranslationManager : IDisposable
{
    private static Dictionary<string, Dictionary<string, string>> Translations = new();

    public CultureInfo CultureInfo { get; private set; } = new("en");
    public string Locale { get; private set; } = "en";

    public TranslationManager()
    {
        var code = Service.ClientState.ClientLanguage switch
        {
            ClientLanguage.Japanese => "ja",
            ClientLanguage.German => "de",
            ClientLanguage.French => "fr",
            _ => "en"
        };

        CultureInfo = new(code);
        Locale = code;

        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"HaselTweaks.Assets.Translations.json");
        if (stream == null)
            return;

        Translations = JsonSerializer.Deserialize< Dictionary<string, Dictionary<string, string>>>(stream) ?? new();
    }

    public bool TryGetTranslation(string key, [MaybeNullWhen(false)] out string text)
    {
        text = default;
        return Translations.TryGetValue(key, out var entry) && (entry.TryGetValue(Locale, out text) || entry.TryGetValue("en", out text));
    }

    public string Translate(string key)
        => TryGetTranslation(key, out var text) ? text : key;

    public string Translate(string key, params object?[] args)
        => TryGetTranslation(key, out var text) ? string.Format(text, args) : key;

    public SeString TranslateSeString(string key, params IEnumerable<Payload>[] args)
    {
        if (!TryGetTranslation(key, out var format))
            return key;

        var placeholders = format.Split(new[] { '{', '}' });
        var sb = new SeStringBuilder();

        for (var i = 0; i < placeholders.Length; i++)
        {
            if (i % 2 == 1) // odd indices contain placeholders
            {
                if (int.TryParse(placeholders[i], out var placeholderIndex))
                {
                    if (placeholderIndex < args.Length)
                    {
                        sb.BuiltString.Payloads.AddRange(args[placeholderIndex]);
                    }
                    else
                    {
                        sb.AddText($"{placeholderIndex}"); // fallback
                    }
                }
            }
            else
            {
                sb.AddText(placeholders[i]);
            }
        }

        return sb.Build();
    }

    public void Dispose()
    {
        Translations.Clear();
    }
}
