using Dalamud.Plugin.Services;
using HaselCommon.Services;
using HaselTweaks.Config;
using HaselTweaks.Windows;

namespace HaselTweaks.Tweaks;

public sealed class GearSetGridConfiguration
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

public sealed class GearSetGrid(
    PluginConfig PluginConfig,
    TranslationManager TranslationManager,
    ICommandManager CommandManager,
    AddonObserver AddonObserver,
    GearSetGridWindow Window)
    : Tweak<GearSetGridConfiguration>(PluginConfig, TranslationManager)
{
    public override void OnEnable()
    {
        AddonObserver.AddonOpen += OnAddonOpen;
        AddonObserver.AddonClose += OnAddonClose;

        if (Config.AutoOpenWithGearSetList && IsAddonOpen("GearSetList"))
            Window.Open();

        if (Config.RegisterCommand)
            CommandManager.AddHandler("/gsg", new(OnGsgCommand) { HelpMessage = t("GearSetGrid.CommandHandlerHelpMessage") });
    }

    public override void OnDisable()
    {
        AddonObserver.AddonOpen -= OnAddonOpen;
        AddonObserver.AddonClose -= OnAddonClose;

        Window.Close();

        CommandManager.RemoveHandler("/gsg");
    }

    public override void OnConfigChange(string fieldName)
    {
        if (fieldName == "RegisterCommand")
        {
            if (Config.RegisterCommand)
                CommandManager.AddHandler("/gsg", new(OnGsgCommand) { HelpMessage = t("GearSetGrid.CommandHandlerHelpMessage") });
            else
                CommandManager.RemoveHandler("/gsg");
        }
    }

    private void OnAddonOpen(string addonName)
    {
        if (Config.AutoOpenWithGearSetList && addonName == "GearSetList")
            Window.Open();
    }

    private void OnAddonClose(string addonName)
    {
        if (Config.AutoOpenWithGearSetList && addonName == "GearSetList")
            Window.Close();
    }

    private void OnGsgCommand(string command, string arguments)
    {
        if (Window.IsOpen)
            Window.Close();
        else
            Window.Open();
    }
}
