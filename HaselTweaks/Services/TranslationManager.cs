using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Dalamud;
using Dalamud.Game.Text.SeStringHandling;
using HaselTweaks.Enums;

namespace HaselTweaks.Services;

public class TranslationManager : IDisposable
{
    public static readonly string DefaultLanguage = "en";
    public static readonly Dictionary<string, string> AllowedLanguages = new()
    {
        ["de"] = "German",
        ["en"] = "English",
        ["fr"] = "French",
        ["ja"] = "Japanese"
    };
    public static CultureInfo CultureInfo = new(DefaultLanguage);

    public static string DalamudLanguageCode
        => AllowedLanguages.ContainsKey(Service.PluginInterface.UiLanguage)
            ? Service.PluginInterface.UiLanguage
            : DefaultLanguage;

    public static string DalamudLanguageLabel
        => AllowedLanguages.ContainsKey(DalamudLanguageCode)
            ? $"Override: Dalamud ({DalamudLanguageCode})"
            : $"Override: Dalamud ({DalamudLanguageCode} is not supported, using fallback {DefaultLanguage})";

    public static string ClientLanguageCode
        => Service.ClientState.ClientLanguage switch
        {
            ClientLanguage.English => "en",
            ClientLanguage.German => "de",
            ClientLanguage.French => "fr",
            ClientLanguage.Japanese => "ja",
            _ => DefaultLanguage
        };

    public static string ClientLanguageLabel
        => AllowedLanguages.ContainsKey(ClientLanguageCode)
            ? $"Override: Client ({ClientLanguageCode})"
            : $"Override: Client ({ClientLanguageCode} is not supported, using fallback {DefaultLanguage})";

    private readonly Dictionary<string, Dictionary<string, string>> _translations = new();

    public TranslationManager()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"HaselTweaks.Translations.json");
        if (stream == null)
            return;

        _translations = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(stream) ?? new();

        Service.PluginInterface.LanguageChanged += PluginInterface_LanguageChanged;
    }

    private void PluginInterface_LanguageChanged(string langCode)
    {
        if (Plugin.Config.PluginLanguageOverride == PluginLanguageOverride.Dalamud)
        {
            Plugin.Config.UpdateLanguage();
        }
    }

    public bool TryGetTranslation(string key, [MaybeNullWhen(false)] out string text)
    {
        text = default;
        return _translations.TryGetValue(key, out var entry) && (entry.TryGetValue(Plugin.Config.PluginLanguage, out text) || entry.TryGetValue("en", out text));
    }

    public string Translate(string key)
        => TryGetTranslation(key, out var text) ? text : key;

    public string Translate(string key, params object?[] args)
        => TryGetTranslation(key, out var text) ? string.Format(CultureInfo, text, args) : key;

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
        Service.PluginInterface.LanguageChanged -= PluginInterface_LanguageChanged;
        _translations.Clear();
    }
}
