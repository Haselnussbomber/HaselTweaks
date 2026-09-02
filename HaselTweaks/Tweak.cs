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

    public Task StartAsync(CancellationToken _)
    {
        if (!_pluginConfig.EnabledTweaks.Contains(InternalName))
            return Task.CompletedTask;

        try
        {
            _logger.LogInformation("Enabling tweak");
            OnEnable();
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

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken _)
    {
        try
        {
            _logger.LogInformation("Disabling tweak");

            OnDisable();

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

        return Task.CompletedTask;
    }

    public virtual void Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        try
        {
            _logger.LogInformation("Disposing tweak");
            OnDisable();
            DisposeAndNull(ref _disposables);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while disposing tweak");
        }

        Status = TweakStatus.Disposed;
    }

    public virtual void OnEnable()
    {
    }

    public virtual void OnDisable()
    {
    }
}
