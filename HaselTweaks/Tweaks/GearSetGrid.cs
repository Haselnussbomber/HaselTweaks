using Dalamud.Game.Command;
using HaselTweaks.Windows;

namespace HaselTweaks.Tweaks;

[Tweak]
public unsafe partial class GearSetGrid : Tweak
{
    private const string GSGCommand = "/gsg";

    public static Configuration Config => Plugin.Config.Tweaks.GearSetGrid;

    public class Configuration
    {
        [BoolConfig]
        public bool AutoOpenWithGearSetList = false;

        [BoolConfig]
        public bool RegisterCommand = true;

        [BoolConfig]
        public bool ConvertSeparators = true;

        [StringConfig(DependsOn = nameof(ConvertSeparators), DefaultValue = "===========")]
        public string SeparatorFilter = "===========";

        [BoolConfig(DependsOn = nameof(ConvertSeparators))]
        public bool DisableSeparatorSpacing = false;
    }

    public override void Enable()
    {
        RegisterCommands();

        if (Config.AutoOpenWithGearSetList && IsAddonOpen("GearSetList"))
            Service.WindowManager.OpenWindow<GearSetGridWindow>();
    }

    public override void Disable()
    {
        UnregisterCommand(true);
        Service.WindowManager.CloseWindow<GearSetGridWindow>();
    }

    public override void OnConfigChange(string fieldName)
    {
        if (fieldName is nameof(Configuration.RegisterCommand))
        {
            UnregisterCommand();
            RegisterCommands();
        }
    }

    public override void OnAddonOpen(string addonName)
    {
        if (Config.AutoOpenWithGearSetList && addonName == "GearSetList")
            Service.WindowManager.OpenWindow<GearSetGridWindow>();
    }

    public override void OnAddonClose(string addonName)
    {
        if (Config.AutoOpenWithGearSetList && addonName == "GearSetList")
            Service.WindowManager.CloseWindow<GearSetGridWindow>();
    }

    private void RegisterCommands()
    {
        if (Config.RegisterCommand)
        {
            Service.CommandManager.RemoveHandler(GSGCommand);
            Service.CommandManager.AddHandler(GSGCommand, new CommandInfo(OnGsgCommand)
            {
                HelpMessage = $"Usage: {GSGCommand} <id>",
                ShowInHelp = true
            });
        }
    }

    private static void UnregisterCommand(bool forceRemoval = false)
    {
        if (!Config.RegisterCommand || forceRemoval)
        {
            Service.CommandManager.RemoveHandler(GSGCommand);
        }
    }

    private void OnGsgCommand(string command, string arguments)
    {
        Service.WindowManager.ToggleWindow<GearSetGridWindow>();
    }
}
