using System.Collections.Generic;
using Dalamud.Plugin.Services;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;

namespace HaselTweaks;

public sealed class TweakManager(IPluginLog PluginLog, Configuration PluginConfig, IEnumerable<ITweak> Tweaks)
{
    public void Initialize()
    {
        foreach (var tweak in Tweaks)
        {
            try
            {
                PluginLog.Verbose($"Initializing {tweak.InternalName}");
                tweak.OnInitialize();
                tweak.Status = TweakStatus.Initialized;
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
