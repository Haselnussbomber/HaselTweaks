using System.Threading;
using System.Threading.Tasks;

namespace HaselTweaks;

[AutoConstruct]
public partial class Plugin : IAsyncDalamudPlugin
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly IPluginLog _pluginLog;
    private readonly IFramework _framework;
    private IHost? _host;

    public Task LoadAsync(CancellationToken cancellationToken)
    {
        _pluginInterface.InitializeCustomClientStructs();

        _host = new HostBuilder()
            .UseContentRoot(_pluginInterface.AssemblyLocation.Directory!.FullName)
            .ConfigureServices(services =>
            {
                services.AddDalamud(_pluginInterface);
                services.AddConfig(PluginConfig.Load(_pluginInterface, _pluginLog));
                services.AddHaselCommon();
                services.AddHaselTweaks();
            })
            .Build();

        return _host.StartOnFrameworkThread(_framework, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return _host?.StopOnFrameworkThread(_framework) ?? ValueTask.CompletedTask;
    }
}
