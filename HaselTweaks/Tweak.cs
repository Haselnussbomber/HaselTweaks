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
    protected DisposableBag? _disposables;

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

            if (_disposables?.IsDisposed == false)
                await _disposables.DisposeAsync().ConfigureAwait(false);

            Status = TweakStatus.Disabled;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while disabling tweak");
        }
    }

    public virtual async ValueTask DisposeAsync()
    {
        try
        {
            if (Status is not (TweakStatus.Disposed or TweakStatus.Outdated or TweakStatus.Disabled))
            {
                _logger.LogWarning("Disabling tweak in DisposeAsync");
                await OnDisable().ConfigureAwait(false);
            }

            if (_disposables?.IsDisposed == false)
            {
                _logger.LogInformation("Disposing tweak");
                await _disposables.DisposeAsync().ConfigureAwait(false);
            }
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
