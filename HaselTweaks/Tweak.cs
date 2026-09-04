using System.Threading;
using System.Threading.Tasks;
using Dalamud.Utility.Signatures;

namespace HaselTweaks.Tweaks;

[AutoConstruct]
public abstract partial class Tweak : ITweak, IHostedService
{
    protected readonly PluginConfig _pluginConfig;

    protected readonly IServiceProvider _serviceProvider;
    protected ILogger _logger;
    protected IDisposable? _disposables;

    public string InternalName { get; private set; }
    public TweakStatus Status { get; set; } = TweakStatus.Disabled;
    public bool IsObsolete { get; set; }

    [AutoPostConstruct]
    private void Initialize(ILoggerFactory loggerFactory)
    {
        InternalName = GetType().Name;
        _logger = loggerFactory.CreateLogger(InternalName);
    }

    public async Task StartAsync(CancellationToken _)
    {
        if (!_pluginConfig.EnabledTweaks.Contains(InternalName))
            return;

        try
        {
            _logger.LogInformation("Enabling tweak");
            await OnEnable().ConfigureAwait(false);
            Status = TweakStatus.Enabled;
        }
        catch (SignatureException ex)
        {
            Status = TweakStatus.Outdated;
            _logger.LogError(ex, "Error while enabling tweak");
        }
        catch (KeyNotFoundException ex)
        {
            Status = TweakStatus.Outdated;
            _logger.LogError(ex, "Error while enabling tweak");
        }
        catch (Exception ex)
        {
            Status = TweakStatus.Error;
            _logger.LogError(ex, "Error while enabling tweak");
        }
    }

    public async Task StopAsync(CancellationToken _)
    {
        try
        {
            _logger.LogInformation("Disabling tweak");

            await OnDisable().ConfigureAwait(false);

            if (_disposables != null)
            {
                _logger.LogWarning("Disposables not disposed in OnDisable!");
                DisposeAndNull(ref _disposables);
            }

            Status = TweakStatus.Disabled;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while disabling tweak");
        }
    }

    public virtual async ValueTask DisposeAsync()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated or TweakStatus.Disabled)
            return;

        try
        {
            _logger.LogInformation("Disposing tweak");
            await OnDisable();
            DisposeAndNull(ref _disposables);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while disposing tweak");
        }

        Status = TweakStatus.Disposed;
    }

    public virtual ValueTask OnEnable()
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask OnDisable()
    {
        return ValueTask.CompletedTask;
    }
}
