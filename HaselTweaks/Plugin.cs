using System.IO;

namespace HaselTweaks;

public sealed class Plugin : IDalamudPlugin
{
    private readonly ServiceProvider _serviceProvider;

    public Plugin(
        IDalamudPluginInterface pluginInterface,
        ISigScanner sigScanner,
        IDataManager dataManager,
        IFramework framework)
    {
        FFXIVClientStructs.Interop.Generated.Addresses.Register();
        Addresses.Register();
        Resolver.GetInstance.Setup(
            sigScanner.SearchBase,
            dataManager.GameData.Repositories["ffxiv"].Version,
            new FileInfo(Path.Join(pluginInterface.ConfigDirectory.FullName, "SigCache.json")));
        Resolver.GetInstance.Resolve();

        _serviceProvider = new ServiceCollection()
            .AddDalamud(pluginInterface)
            .AddSingleton(PluginConfig.Load)
            .AddHaselCommon()
            .AddHaselTweaks()
            .BuildServiceProvider();

        framework.RunOnFrameworkThread(() =>
        {
            _serviceProvider.GetRequiredService<TweakManager>();
            _serviceProvider.GetRequiredService<CommandManager>();
        });
    }

    void IDisposable.Dispose()
    {
        _serviceProvider.Dispose();
    }
}
