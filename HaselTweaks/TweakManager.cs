using System.Collections.Generic;
using Dalamud.Plugin.Services;
using HaselTweaks.Config;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;

namespace HaselTweaks;

public sealed class TweakManager(IPluginLog PluginLog, PluginConfig PluginConfig, IEnumerable<ITweak> Tweaks)
{
    public void Initialize()
    {
        foreach (var tweak in Tweaks)
        {
            if (tweak.Status == TweakStatus.Outdated)
                continue;

            try
            {
                PluginLog.Verbose($"Initializing {tweak.InternalName}");
                tweak.OnInitialize();
                tweak.Status = TweakStatus.Disabled;
            }
            catch (Exception ex)
            {
                tweak.Status = TweakStatus.InitializationFailed;
                PluginLog.Error(ex, $"[{tweak.InternalName}] Error while initializing tweak");
                continue;
            }

            if (!PluginConfig.EnabledTweaks.Contains(tweak.InternalName))
                continue;

            try
            {
                PluginLog.Verbose($"Enabling {tweak.InternalName}");
                tweak.OnEnable();
                tweak.Status = TweakStatus.Enabled;
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"[{tweak.InternalName}] Error while enabling tweak");
            }
        }
    }

    public void UserEnableTweak(ITweak tweak)
    {
        tweak.OnEnable();
        tweak.Status = TweakStatus.Enabled;

        if (!PluginConfig.EnabledTweaks.Contains(tweak.InternalName))
        {
            PluginConfig.EnabledTweaks.Add(tweak.InternalName);
            PluginConfig.Save();
        }
    }

    public void UserDisableTweak(ITweak tweak)
    {
        tweak.OnDisable();
        tweak.Status = TweakStatus.Disabled;

        if (PluginConfig.EnabledTweaks.Contains(tweak.InternalName))
        {
            PluginConfig.EnabledTweaks.Remove(tweak.InternalName);
            PluginConfig.Save();
        }
    }
}
