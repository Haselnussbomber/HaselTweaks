using System.IO;
using Dalamud.Game;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using HaselCommon;
using HaselTweaks.Config;
using HaselTweaks.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HaselTweaks;

public sealed class Plugin : IDalamudPlugin
{
    public Plugin(
        IDalamudPluginInterface pluginInterface,
        ISigScanner sigScanner,
        IDataManager dataManager)
    {
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

        Service.Initialize(() =>
        {
            Service.Get<TweakManager>();
            Service.Get<CommandManager>();
        });
    }

    void IDisposable.Dispose()
    {
        Service.Dispose();
    }
}
