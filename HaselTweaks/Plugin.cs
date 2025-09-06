namespace HaselTweaks;

public sealed class Plugin : IDalamudPlugin
{
    private readonly IHost _host;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.InitializeCustomClientStructs();

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
