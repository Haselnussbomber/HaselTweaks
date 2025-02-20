using System.IO;
using Dalamud.Game;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using HaselCommon;
using HaselCommon.Commands;
using HaselCommon.Services;
using HaselTweaks.Config;
using HaselTweaks.Windows;
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

#if HAS_LOCAL_CS
        FFXIVClientStructs.Interop.Generated.Addresses.Register();
        Addresses.Register();
        Resolver.GetInstance.Setup(
            sigScanner.SearchBase,
            dataManager.GameData.Repositories["ffxiv"].Version,
            new FileInfo(Path.Join(pluginInterface.ConfigDirectory.FullName, "SigCache.json")));
        Resolver.GetInstance.Resolve();
#endif

        Service.Collection
            .AddDalamud(pluginInterface)
            .AddSingleton(PluginConfig.Load)
            .AddHaselCommon()
            .AddHaselTweaks();

        Service.BuildProvider();

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
