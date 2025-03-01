using System.Collections.Generic;
using Dalamud.Plugin.Services;
using HaselTweaks.Config;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;

namespace HaselTweaks.Services;

[RegisterSingleton, AutoConstruct]
public partial class TweakManager
{
    private readonly IPluginLog _pluginLog;
    private readonly PluginConfig _pluginConfig;
    private readonly IEnumerable<ITweak> _tweaks;

    [AutoPostConstruct]
    private void Initialize()
    {
        foreach (var tweak in _tweaks)
        {
            if (tweak.Status == TweakStatus.Outdated)
                continue;

            try
            {
                _pluginLog.Verbose($"Initializing {tweak.InternalName}");
                tweak.OnInitialize();
                tweak.Status = TweakStatus.Disabled;
            }
            catch (Exception ex)
            {
                tweak.Status = TweakStatus.InitializationFailed;
                _pluginLog.Error(ex, $"[{tweak.InternalName}] Error while initializing tweak");
                continue;
            }

            if (!_pluginConfig.EnabledTweaks.Contains(tweak.InternalName))
                continue;

            try
            {
                _pluginLog.Verbose($"Enabling {tweak.InternalName}");
                tweak.OnEnable();
                tweak.Status = TweakStatus.Enabled;
            }
            catch (Exception ex)
            {
                _pluginLog.Error(ex, $"[{tweak.InternalName}] Error while enabling tweak");
            }
        }
    }

    public void UserEnableTweak(ITweak tweak)
    {
        tweak.OnEnable();
        tweak.Status = TweakStatus.Enabled;

        if (!_pluginConfig.EnabledTweaks.Contains(tweak.InternalName))
        {
            _pluginConfig.EnabledTweaks.Add(tweak.InternalName);
            _pluginConfig.Save();
        }
    }

    public void UserDisableTweak(ITweak tweak)
    {
        tweak.OnDisable();
        tweak.Status = TweakStatus.Disabled;

        if (_pluginConfig.EnabledTweaks.Contains(tweak.InternalName))
        {
            _pluginConfig.EnabledTweaks.Remove(tweak.InternalName);
            _pluginConfig.Save();
        }
    }
}
