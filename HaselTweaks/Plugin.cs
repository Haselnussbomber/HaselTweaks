using System.IO;

namespace HaselTweaks;

public sealed class Plugin : IDalamudPlugin
{
    private readonly IHost _host;

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

        _host = new HostBuilder()
            .UseContentRoot(pluginInterface.AssemblyLocation.Directory!.FullName)
            .ConfigureServices(services =>
            {
                services.AddDalamud(pluginInterface);
                services.AddSingleton(PluginConfig.Load);
                services.AddHaselCommon();
                services.AddHaselTweaks();
            })
            .Build();

        _host.Start();
    }

    void IDisposable.Dispose()
    {
        _host.StopAsync().GetAwaiter().GetResult();
        _host.Dispose();
    }
}
