namespace HaselTweaks.Tweaks;

public class BiggerCharacterPreviewsConfiguration
{
    public bool SharperImages = false;
    public bool EnableCharacter = true;
    public bool EnableCharacterInspect = true;
    public bool EnableColorantColoring = true;
    public bool EnableTryon = true;
}

public partial class BiggerCharacterPreviews
{
    public override void DrawConfig()
    {
        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool(nameof(_config.SharperImages), ref _config.SharperImages);
        _configGui.DrawBool(nameof(_config.EnableCharacter), ref _config.EnableCharacter);
        _configGui.DrawBool(nameof(_config.EnableCharacterInspect), ref _config.EnableCharacterInspect);
        _configGui.DrawBool(nameof(_config.EnableColorantColoring), ref _config.EnableColorantColoring);
        _configGui.DrawBool(nameof(_config.EnableTryon), ref _config.EnableTryon);
    }
}
