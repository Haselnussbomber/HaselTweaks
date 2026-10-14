using System.Threading;
using System.Threading.Tasks;

namespace HaselTweaks;

[AutoConstruct]
public partial class Plugin : IAsyncDalamudPlugin
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private Host _host;

    [AutoPostConstruct]
    private void Initialize()
    {
        _host = new HostBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(_pluginInterface);
                services.AddHaselCommon();
                services.AddHaselTweaks();
            })
            .Build();
    }

    public Task LoadAsync(CancellationToken cancellationToken)
    {
        _pluginInterface.InitializeCustomClientStructs();
        return _host.StartAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _host.StopAsync().ConfigureAwait(false);
        }
        finally
        {
            await _host.DisposeAsync().ConfigureAwait(false);
        }
    }
}
