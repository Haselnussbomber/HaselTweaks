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
    private CommandsConfiguration Config => _pluginConfig.Tweaks.Commands;

    public override void OnConfigChange(string fieldName)
    {
        UpdateCommands(Status == TweakStatus.Enabled);
    }

    public override void DrawConfig()
    {
        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool("EnableItemLinkCommand", ref Config.EnableItemLinkCommand);
        _configGui.DrawBool("EnableWhatMountCommand", ref Config.EnableWhatMountCommand);
        _configGui.DrawBool("EnableWhatEmoteCommand", ref Config.EnableWhatEmoteCommand);
        _configGui.DrawBool("EnableWhatBardingCommand", ref Config.EnableWhatBardingCommand);
        _configGui.DrawBool("EnableGlamourPlateCommand", ref Config.EnableGlamourPlateCommand);
        _configGui.DrawBool("EnableReloadUICommand", ref Config.EnableReloadUICommand, noFixSpaceAfter: true);
    }
}
