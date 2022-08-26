using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dalamud.Configuration;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Logging;
using HaselTweaks.Tweaks;
using Newtonsoft.Json.Linq;

namespace HaselTweaks;

[Serializable]
internal partial class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    public HashSet<string> EnabledTweaks { get; private set; } = new();
    public TweakConfigs Tweaks { get; init; } = new();
}

public class TweakConfigs
{
    public AutoSortArmouryChest.Configuration AutoSortArmouryChest { get; init; } = new();
    public CustomChatTimestamp.Configuration CustomChatTimestamp { get; init; } = new();
    public MinimapAdjustments.Configuration MinimapAdjustments { get; init; } = new();
    public ForcedCutsceneMusic.Configuration ForcedCutsceneMusic { get; init; } = new();
    public ScrollableTabs.Configuration ScrollableTabs { get; init; } = new();
    public Commands.Configuration Commands { get; init; } = new();
    public CharacterClassSwitcher.Configuration CharacterClassSwitcher { get; init; } = new();
    public EnhancedExpBar.Configuration EnhancedExpBar { get; init; } = new();
}

internal partial class Configuration : IDisposable
{
    public static Configuration Instance { get; private set; } = null!;

    internal static Configuration Load(string[] tweakNames)
    {
        if (Instance != null)
            return Instance;

        var configPath = Service.PluginInterface.ConfigFile.FullName;
        JObject? config = null;

        try
        {
            var jsonData = File.Exists(configPath) ? File.ReadAllText(configPath) : null;
            if (string.IsNullOrEmpty(jsonData))
                return Instance = new();

            config = JObject.Parse(jsonData);
        }
        catch (Exception e)
        {
            PluginLog.Error(e, "Could not load configuration, creating a new one");

            Service.PluginInterface.UiBuilder.AddNotification(
                "Could not load the configuration file. Creating a new one.",
                "HaselTweaks",
                NotificationType.Error,
                5000
            );
        }

        if (config == null)
            return Instance = new();

        try
        {
            var version = (int?)config[nameof(Version)];
            var enabledTweaks = (JArray?)config[nameof(EnabledTweaks)];
            var tweakConfigs = (JObject?)config[nameof(Tweaks)];

            if (version == null || enabledTweaks == null || tweakConfigs == null)
                return Instance = new();

            var renamedTweaks = new Dictionary<string, string>()
            {
                ["RevealDungeonRequirements"] = "RevealDutyRequirements", // commit 7ce9b37b
                ["SeriesExpBar"] = "EnhancedExpBar",
            };

            var newEnabledTweaks = new JArray();

            foreach (var tweakToken in enabledTweaks)
            {
                var tweakName = (string?)tweakToken;
                if (string.IsNullOrEmpty(tweakName)) continue;

                // re-enable renamed tweaks
                if (renamedTweaks.ContainsKey(tweakName))
                {
                    var newTweakName = renamedTweaks[tweakName];

                    PluginLog.Log($"Renamed Tweak: {tweakName} => {newTweakName}");

                    // copy renamed tweak config
                    var tweakConfig = (JObject?)tweakConfigs[tweakName];
                    if (tweakConfig != null)
                    {
                        // adjust $type
                        var type = (string?)tweakConfig["type"];
                        if (type != null)
                            tweakConfig["type"] = type.Replace(tweakName, newTweakName);

                        tweakConfigs[newTweakName] = tweakConfig;
                        tweakConfigs.Remove(tweakName);
                    }

                    tweakName = newTweakName;
                }

                // only copy valid ones
                if (tweakNames.Contains(tweakName))
                    newEnabledTweaks.Add(tweakName);
            }

            config[nameof(EnabledTweaks)] = newEnabledTweaks;
        }
        catch (Exception e)
        {
            PluginLog.Error(e, "Could not migrate configuration");
            // continue, for now
        }

        return Instance = config.ToObject<Configuration>() ?? new();
    }

    internal static void Save()
    {
        Service.PluginInterface.SavePluginConfig(Instance);
    }

    void IDisposable.Dispose()
    {
        Instance = null!;
    }
}
