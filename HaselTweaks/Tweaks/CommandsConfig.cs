using HaselTweaks.Config;
using HaselTweaks.Enums;

namespace HaselTweaks.Tweaks;

public class CommandsConfiguration
{
    public bool EnableItemLinkCommand = true;
    public bool EnableWhatMountCommand = true;
    public bool EnableWhatEmoteCommand = true;
    public bool EnableWhatBardingCommand = true;
    public bool EnableGlamourPlateCommand = true;
}

public unsafe partial class Commands
{
    private CommandsConfiguration Config => PluginConfig.Tweaks.Commands;

    public void OnConfigOpen() { }
    public void OnConfigClose() { }

    public void OnConfigChange(string fieldName)
    {
        UpdateCommands(Status == TweakStatus.Enabled);
    }

    public void DrawConfig()
    {
        using var _ = ConfigGui.PushContext(this);

        ConfigGui.DrawConfigurationHeader();
        ConfigGui.DrawBool("EnableItemLinkCommand", ref Config.EnableItemLinkCommand);
        ConfigGui.DrawBool("EnableWhatMountCommand", ref Config.EnableWhatMountCommand);
        ConfigGui.DrawBool("EnableWhatEmoteCommand", ref Config.EnableWhatEmoteCommand);
        ConfigGui.DrawBool("EnableWhatBardingCommand", ref Config.EnableWhatBardingCommand);
        ConfigGui.DrawBool("EnableGlamourPlateCommand", ref Config.EnableGlamourPlateCommand, noFixSpaceAfter: true);
    }
}
