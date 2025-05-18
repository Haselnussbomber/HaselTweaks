namespace HaselTweaks.Services;

[RegisterSingleton, AutoConstruct]
public partial class TweakManager
{
    private readonly ILogger<TweakManager> _logger;
    private readonly PluginConfig _pluginConfig;
    private readonly IEnumerable<ITweak> _tweaks;

    [AutoPostConstruct]
    private void Initialize()
    {
        foreach (var tweak in _tweaks)
        {
            if (tweak.Status == TweakStatus.Outdated)
                continue;

            var tweakName = tweak.GetInternalName();

            try
            {
                _logger.LogTrace("Initializing {tweakName}", tweakName);
                tweak.OnInitialize();
                tweak.Status = TweakStatus.Disabled;
            }
            catch (Exception ex)
            {
                tweak.Status = TweakStatus.InitializationFailed;
                _logger.LogError(ex, "[{tweakName}] Error while initializing tweak", tweakName);
                continue;
            }

            if (!_pluginConfig.EnabledTweaks.Contains(tweakName))
                continue;

            try
            {
                _logger.LogTrace("Enabling {tweakName}", tweakName);
                tweak.OnEnable();
                tweak.Status = TweakStatus.Enabled;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{tweakName}] Error while enabling tweak", tweakName);
            }
        }
    }

    public void UserEnableTweak(ITweak tweak)
    {
        tweak.OnEnable();
        tweak.Status = TweakStatus.Enabled;

        var tweakName = tweak.GetInternalName();

        if (!_pluginConfig.EnabledTweaks.Contains(tweakName))
        {
            _pluginConfig.EnabledTweaks.Add(tweakName);
            _pluginConfig.Save();
        }
    }

    public void UserDisableTweak(ITweak tweak)
    {
        tweak.OnDisable();
        tweak.Status = TweakStatus.Disabled;

        var tweakName = tweak.GetInternalName();

        if (_pluginConfig.EnabledTweaks.Contains(tweakName))
        {
            _pluginConfig.EnabledTweaks.Remove(tweakName);
            _pluginConfig.Save();
        }
    }
}
