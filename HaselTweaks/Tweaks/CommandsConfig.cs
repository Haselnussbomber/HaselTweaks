namespace HaselTweaks.Tweaks;

public class CommandsConfiguration
{
    public bool EnableItemLinkCommand = true;
    public bool EnableWhatMountCommand = true;
    public bool EnableWhatEmoteCommand = true;
    public bool EnableWhatBardingCommand = true;
    public bool EnableGlamourPlateCommand = true;
    public bool EnableReloadUICommand = true;
}

public unsafe partial class Commands
{
    public override void OnConfigChange(string fieldName)
    {
        UpdateCommands(Status == TweakStatus.Enabled);
    }

    public override void DrawConfig()
    {
        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool("EnableItemLinkCommand", ref _config.EnableItemLinkCommand);
        _configGui.DrawBool("EnableWhatMountCommand", ref _config.EnableWhatMountCommand);
        _configGui.DrawBool("EnableWhatEmoteCommand", ref _config.EnableWhatEmoteCommand);
        _configGui.DrawBool("EnableWhatBardingCommand", ref _config.EnableWhatBardingCommand);
        _configGui.DrawBool("EnableGlamourPlateCommand", ref _config.EnableGlamourPlateCommand);
        _configGui.DrawBool("EnableReloadUICommand", ref _config.EnableReloadUICommand, noFixSpaceAfter: true);
    }
}
