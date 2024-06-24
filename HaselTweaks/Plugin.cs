using System.IO;
using Dalamud.Game;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using HaselCommon.Extensions;
using HaselCommon.Logger;
using HaselTweaks.Caches;
using HaselTweaks.Config;
using HaselTweaks.Interfaces;
using HaselTweaks.Utils;
using HaselTweaks.Windows;
using HaselTweaks.Windows.PortraitHelperWindows;
using HaselTweaks.Windows.PortraitHelperWindows.Dialogs;
using HaselTweaks.Windows.PortraitHelperWindows.Overlays;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HaselTweaks;

public sealed class Plugin : IDalamudPlugin
{
    public Plugin(
        DalamudPluginInterface pluginInterface,
        IFramework framework,
        IPluginLog pluginLog,
        ISigScanner sigScanner,
        IDataManager dataManager)
    {
        Service
            // Dalamud & HaselCommon
            .Initialize(pluginInterface)

            // Logging
            .AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.AddProvider(new DalamudLoggerProvider(pluginLog));
            })

            // Config
            .AddSingleton(PluginConfig.Load(pluginInterface, pluginLog))

            // HaselTweaks
            .AddSingleton<TweakManager>()
            .AddSingleton<PluginWindow>()
            .AddSingleton<BannerUtils>()
            .AddIServices<ITweak>()

            // AetherCurrentHelper
            .AddSingleton<EObjDataIdCache>()
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

        // ---

        FFXIVClientStructs.Interop.Generated.Addresses.Register();
        Addresses.Register();
        Resolver.GetInstance.Setup(
            sigScanner.SearchBase,
            dataManager.GameData.Repositories["ffxiv"].Version,
            new FileInfo(Path.Join(pluginInterface.ConfigDirectory.FullName, "SigCache.json")));
        Resolver.GetInstance.Resolve();

        // ---

        // TODO: IHostedService?
        framework.RunOnFrameworkThread(() =>
        {
            Service.Get<TweakManager>().Initialize();
            Service.Get<PluginWindow>();
        });
    }

    void IDisposable.Dispose()
    {
        Service.Dispose();
        Addresses.Unregister();
    }
}
