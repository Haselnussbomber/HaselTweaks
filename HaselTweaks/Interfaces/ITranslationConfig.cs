using HaselTweaks.Enums;

namespace HaselTweaks.Interfaces;

public interface ITranslationConfig
{
    public string PluginLanguage { get; set; }
    public PluginLanguageOverride PluginLanguageOverride { get; set; }
}
