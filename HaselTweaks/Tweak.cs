using System.Threading;
using System.Threading.Tasks;
using Dalamud.Utility.Signatures;

namespace HaselTweaks.Tweaks;

[AutoConstruct]
public abstract unsafe partial class Tweak : ITweak, IHostedService
{
    protected readonly PluginConfig _pluginConfig;

    protected ILogger _logger;

    public string InternalName { get; private set; }
    public TweakStatus Status { get; set; } = TweakStatus.Disabled;

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
            Status = TweakStatus.Disabled;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while disabling tweak");
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        try
        {
            _logger.LogInformation("Disposing tweak");
            OnDisable();
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
