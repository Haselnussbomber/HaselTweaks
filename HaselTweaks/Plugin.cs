using System.IO;

namespace HaselTweaks;

public sealed class Plugin : IDalamudPlugin
{
    public Plugin(
        IDalamudPluginInterface pluginInterface,
        ISigScanner sigScanner,
        IDataManager dataManager)
    {
        FFXIVClientStructs.Interop.Generated.Addresses.Register();
        Addresses.Register();
        Resolver.GetInstance.Setup(
            sigScanner.SearchBase,
            dataManager.GameData.Repositories["ffxiv"].Version,
            new FileInfo(Path.Join(pluginInterface.ConfigDirectory.FullName, "SigCache.json")));
        Resolver.GetInstance.Resolve();

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
