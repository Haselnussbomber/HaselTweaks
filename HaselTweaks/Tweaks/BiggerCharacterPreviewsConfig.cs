using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Tweaks;

public class BiggerCharacterPreviewsConfiguration
{
    public bool EnableCharacter = true;
    public bool SharpenCharacter = false;

    public bool EnableCharacterInspect = true;
    public bool SharpenCharacterInspect = true;

    public bool EnableColorantColoring = true;
    public bool SharpenColorantColoring = true;

    public bool EnableTryon = true;
    public bool SharpenTryon = true;
}

public partial class BiggerCharacterPreviews
{
    public override void DrawConfig()
    {
        _configGui.DrawConfigurationHeader();

        _configGui.DrawBool(nameof(_config.EnableCharacter), ref _config.EnableCharacter, drawAfterDescription: () =>
        {
            _configGui.DrawBool(nameof(_config.SharpenCharacter), ref _config.SharpenCharacter);
        });

        _configGui.DrawBool(nameof(_config.EnableCharacterInspect), ref _config.EnableCharacterInspect, drawAfterDescription: () =>
        {
            _configGui.DrawBool(nameof(_config.SharpenCharacterInspect), ref _config.SharpenCharacterInspect);
        });

        _configGui.DrawBool(nameof(_config.EnableColorantColoring), ref _config.EnableColorantColoring, drawAfterDescription: () =>
        {
            _configGui.DrawBool(nameof(_config.SharpenColorantColoring), ref _config.SharpenColorantColoring);
        });

        _configGui.DrawBool(nameof(_config.EnableTryon), ref _config.EnableTryon, drawAfterDescription: () =>
        {
            _configGui.DrawBool(nameof(_config.SharpenTryon), ref _config.SharpenTryon);
        });
    }

    public override unsafe void OnConfigChange(string fieldName)
    {
        foreach (var name in AddonNames)
        {
            if (TryGetAddon<AtkUnitBase>(name, out var addon))
            {
                UpdatePreviewSharpening(addon);
            }
        }
    }
}
