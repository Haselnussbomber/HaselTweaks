using ZLinq;

[assembly: ZLinqDropIn("ZLinq", DropInGenerateTypes.Everything)]

namespace HaselTweaks;

public sealed class Plugin : IDalamudPlugin
{
    private readonly IHost _host;

    public Plugin(IDalamudPluginInterface pluginInterface, IPluginLog pluginLog, IFramework framework)
    {
        pluginInterface.InitializeCustomClientStructs();

        _host = new HostBuilder()
            .UseContentRoot(pluginInterface.AssemblyLocation.Directory!.FullName)
            .ConfigureServices(services =>
            {
                services.AddDalamud(pluginInterface);
                services.AddConfig(PluginConfig.Load(pluginInterface, pluginLog));
                services.AddHaselCommon();
                services.AddHaselTweaks();
            })
            .Build();

        framework.RunOnFrameworkThread(_host.Start);
    }

    void IDisposable.Dispose()
    {
        _host.StopAsync().GetAwaiter().GetResult();
        _host.Dispose();
    }
}
