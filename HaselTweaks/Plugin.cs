using System.IO;
using Dalamud.Game;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using HaselCommon;
using HaselTweaks.Config;
using HaselTweaks.Interfaces;
using HaselTweaks.Tweaks;
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

            // Tweaks
            .AddSingleton<ITweak, AchievementLinkTooltip>()
            .AddSingleton<ITweak, AetherCurrentHelper>()
            .AddSingleton<ITweak, AutoOpenRecipe>()
            .AddSingleton<ITweak, AutoSorter>()
            .AddSingleton<ITweak, BackgroundMusicKeybind>()
            .AddSingleton<ITweak, CastBarAetheryteNames>()
            .AddSingleton<ITweak, CharacterClassSwitcher>()
            .AddSingleton<ITweak, Commands>()
            .AddSingleton<ITweak, CustomChatMessageFormats>()
            .AddSingleton<ITweak, CustomChatTimestamp>()
            .AddSingleton<ITweak, DTR>()
            .AddSingleton<ITweak, EnhancedExpBar>()
            .AddSingleton<ITweak, EnhancedIsleworksAgenda>()
            .AddSingleton<ITweak, EnhancedLoginLogout>()
            .AddSingleton<ITweak, EnhancedMaterialList>()
            .AddSingleton<ITweak, ExpertDeliveries>()
            .AddSingleton<ITweak, ForcedCutsceneMusic>()
            .AddSingleton<ITweak, GearSetGrid>()
            .AddSingleton<ITweak, GlamourDresserArmoireAlert>()
            .AddSingleton<ITweak, HideMSQComplete>()
            .AddSingleton<ITweak, InventoryHighlight>()
            .AddSingleton<ITweak, KeepScreenAwake>()
            .AddSingleton<ITweak, LockWindowPosition>()
            .AddSingleton<ITweak, MarketBoardItemPreview>()
            .AddSingleton<ITweak, MaterialAllocation>()
            .AddSingleton<ITweak, MinimapAdjustments>()
            .AddSingleton<ITweak, PortraitHelper>()
            .AddSingleton<ITweak, RevealDutyRequirements>()
            .AddSingleton<ITweak, SaferItemSearch>()
            .AddSingleton<ITweak, ScrollableTabs>()
            .AddSingleton<ITweak, SearchTheMarkets>()
            .AddSingleton<ITweak, SimpleAethernetList>()

            // Plugin Window
            .AddSingleton<PluginWindow>()

            // Windows: AetherCurrentHelper
            .AddSingleton<AetherCurrentHelperWindow>()

            // Windows: EnhancedIsleworksAgenda
            .AddSingleton<MJICraftScheduleSettingSearchBar>()

            // Windows: GearSetGrid
            .AddSingleton<GearSetGridWindow>()

            // Windows: GlamourDresserArmoireAlert
            .AddSingleton<GlamourDresserArmoireAlertWindow>()

            // Windows: PresetHelper
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
            .AddScoped<PresetBrowserOverlay>()

            .AddSingleton<TweakManager>();

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
