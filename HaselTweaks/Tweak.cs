using System.Threading;
using System.Threading.Tasks;
using Dalamud.Utility.Signatures;

namespace HaselTweaks.Tweaks;

[AutoConstruct]
public abstract unsafe partial class Tweak : ITweak, IHostedService
{
    private readonly PluginConfig _pluginConfig;
    private readonly IFramework _framework;

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

        return _framework.RunOnFrameworkThread(() =>
        {
            try
            {
                OnEnable();
                Status = TweakStatus.Enabled;
            }
            catch (SignatureException ex)
            {
                Status = TweakStatus.Outdated;
                _logger.LogError(ex, "[{tweakName}] Error while enabling tweak", InternalName);
            }
            catch (KeyNotFoundException ex)
            {
                Status = TweakStatus.Outdated;
                _logger.LogError(ex, "[{tweakName}] Error while enabling tweak", InternalName);
            }
            catch (Exception ex)
            {
                Status = TweakStatus.Error;
                _logger.LogError(ex, "[{tweakName}] Error while enabling tweak", InternalName);
            }
        });
    }

    public Task StopAsync(CancellationToken _)
    {
        return _framework.RunOnFrameworkThread(() =>
        {
            try
            {
                OnDisable();
                Status = TweakStatus.Disabled;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{tweakName}] Error while disabling tweak", InternalName);
            }
        });
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();
        Status = TweakStatus.Disposed;
    }

    public virtual void OnEnable()
    {
    }

    public virtual void OnDisable()
    {
    }
}
