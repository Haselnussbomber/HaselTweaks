using System.IO;
using Dalamud.Game;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using HaselCommon.Commands;
using HaselCommon.Extensions.DependencyInjection;
using HaselCommon.Services;
using HaselTweaks.Caches;
using HaselTweaks.Config;
using HaselTweaks.Interfaces;
using HaselTweaks.Utils;
using HaselTweaks.Windows;
using HaselTweaks.Windows.PortraitHelperWindows;
using HaselTweaks.Windows.PortraitHelperWindows.Dialogs;
using HaselTweaks.Windows.PortraitHelperWindows.Overlays;
using Microsoft.Extensions.DependencyInjection;

namespace HaselTweaks;

public sealed class Plugin : IDalamudPlugin
{
    private readonly IDalamudPluginInterface PluginInterface;

    public Plugin(
        IDalamudPluginInterface pluginInterface,
        IFramework framework,
        IPluginLog pluginLog,
        ISigScanner sigScanner,
        IDataManager dataManager)
    {
        PluginInterface = pluginInterface;

        Service
            // Dalamud & HaselCommon
            .Initialize(pluginInterface, pluginLog)

            // Config
            .AddSingleton(PluginConfig.Load(pluginInterface, pluginLog))

            // HaselTweaks
            .AddSingleton<TweakManager>()
            .AddSingleton<PluginWindow>()
            .AddSingleton<BannerUtils>()
            .AddIServices<ITweak>()
            .AddSingleton<ConfigGui>()

            // AetherCurrentHelper
            .AddSingleton<EObjDataIdCache>()
            .AddSingleton<LevelObjectCache>()
            .AddSingleton<AetherCurrentHelperWindow>()

            // EnhancedIsleworksAgenda
            .AddSingleton<MJICraftScheduleSettingSearchBar>()

            // GearSetGrid
            .AddSingleton<GearSetGridWindow>()

            // GlamourDresserArmoireAlert
            .AddSingleton<GlamourDresserArmoireAlertWindow>()

            // PresetHelper
            .AddSingleton<MenuBar>()
            .AddScoped<CreatePresetDialog>()
            .AddScoped<CreateTagDialog>()
            .AddScoped<RenameTagDialog>()
            .AddScoped<DeleteTagDialog>()
            .AddScoped<DeletePresetDialog>()
            .AddScoped<EditPresetDialog>()
            .AddScoped<AdvancedEditOverlay>()
            .AddScoped<AdvancedImportOverlay>()
            .AddScoped<AlignmentToolSettingsOverlay>()
            .AddScoped<PresetBrowserOverlay>();

        Service.BuildProvider();

#if HAS_LOCAL_CS
        FFXIVClientStructs.Interop.Generated.Addresses.Register();
        Addresses.Register();
        Resolver.GetInstance.Setup(
            sigScanner.SearchBase,
            dataManager.GameData.Repositories["ffxiv"].Version,
            new FileInfo(Path.Join(pluginInterface.ConfigDirectory.FullName, "SigCache.json")));
        Resolver.GetInstance.Resolve();
#endif

        // TODO: IHostedService?
        framework.RunOnFrameworkThread(() =>
        {
            Service.Get<TweakManager>().Initialize();
            Service.Get<CommandService>().Register(OnCommand, true);
            PluginInterface.UiBuilder.OpenMainUi += ToggleWindow;
        });
    }

    [CommandHandler("/haseltweaks", "HaselTweaks.CommandHandlerHelpMessage")]
    private void OnCommand(string command, string arguments)
    {
        ToggleWindow();
    }

    private static void ToggleWindow()
    {
        Service.Get<PluginWindow>().Toggle();
    }

    void IDisposable.Dispose()
    {
        PluginInterface.UiBuilder.OpenMainUi -= ToggleWindow;

        Service.Dispose();

#if HAS_LOCAL_CS
        Addresses.Unregister();
#endif
    }
}
