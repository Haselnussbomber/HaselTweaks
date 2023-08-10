using Dalamud.Game.Command;
using HaselTweaks.Windows;

namespace HaselTweaks.Tweaks;

[Tweak]
public unsafe partial class GearSetGrid : Tweak
{
    private GearSetGridWindow? _window;
    private const string GSGCommand = "/gsg";

    public static Configuration Config => Plugin.Config.Tweaks.GearSetGrid;

    public class Configuration
    {
        [BoolConfig]
        public bool AutoOpenWithGearSetList = false;

        [BoolConfig]
        public bool RegisterCommand = true;

        [BoolConfig]
        public bool AllowSwitchingGearsets = true;

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
            OpenWindow();
    }

    public override void Disable()
    {
        UnregisterCommand(true);
        CloseWindow();
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
            OpenWindow();
    }

    public override void OnAddonClose(string addonName)
    {
        if (Config.AutoOpenWithGearSetList && addonName == "GearSetList")
            CloseWindow();
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
        ToggleWindow();
    }

    private void ToggleWindow()
    {
        if (_window == null)
        {
            Plugin.WindowSystem.AddWindow(_window = new(this));
            _window.IsOpen = true;
        }
        else
        {
            _window.IsOpen = !_window.IsOpen;
        }
    }

    private void OpenWindow()
    {
        if (_window == null)
            Plugin.WindowSystem.AddWindow(_window = new(this));

        _window.IsOpen = true;
    }

    private void CloseWindow()
    {
        if (_window == null)
            return;

        Plugin.WindowSystem.RemoveWindow(_window);
        _window = null;
    }
}
