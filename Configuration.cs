using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dalamud.Configuration;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Logging;
using HaselTweaks.Tweaks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HaselTweaks;

[Serializable]
internal partial class Configuration : IPluginConfiguration
{
    [JsonIgnore]
    public const int CURRENT_CONFIG_VERSION = 4;

    public int Version { get; set; } = CURRENT_CONFIG_VERSION;
    public HashSet<string> EnabledTweaks { get; private set; } = new();
    public TweakConfigs Tweaks { get; init; } = new();
    public string SigCacheVersion { get; init; } = string.Empty;
    public ConcurrentDictionary<string, long>? SigCache { get; init; } = new();
}

public class TweakConfigs
{
    public AutoSorter.Configuration AutoSorter { get; init; } = new();
    public CustomChatTimestamp.Configuration CustomChatTimestamp { get; init; } = new();
    public MinimapAdjustments.Configuration MinimapAdjustments { get; init; } = new();
    public ForcedCutsceneMusic.Configuration ForcedCutsceneMusic { get; init; } = new();
    public ScrollableTabs.Configuration ScrollableTabs { get; init; } = new();
    public Commands.Configuration Commands { get; init; } = new();
    public CharacterClassSwitcher.Configuration CharacterClassSwitcher { get; init; } = new();
    public EnhancedExpBar.Configuration EnhancedExpBar { get; init; } = new();
    public AetherCurrentHelper.Configuration AetherCurrentHelper { get; init; } = new();
    public LockWindowPosition.Configuration LockWindowPosition { get; init; } = new();
    public MaterialAllocation.Configuration MaterialAllocation { get; init; } = new();
    public EnhancedMaterialList.Configuration EnhancedMaterialList { get; init; } = new();
    public PortraitHelper.Configuration PortraitHelper { get; init; } = new();
}

internal partial class Configuration
{
    internal static Configuration Load(string[] tweakNames, string? gameVersion)
    {
        var configPath = Service.PluginInterface.ConfigFile.FullName;
        JObject? config = null;

        try
        {
            var jsonData = File.Exists(configPath) ? File.ReadAllText(configPath) : null;
            if (string.IsNullOrEmpty(jsonData))
                return new();

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
            return new();

        try
        {
            var version = (int?)config[nameof(Version)] ?? 0;
            var enabledTweaks = (JArray?)config[nameof(EnabledTweaks)];
            var tweakConfigs = (JObject?)config[nameof(Tweaks)];

            if (version == 0 || enabledTweaks == null || tweakConfigs == null)
                return new();

            var renamedTweaks = new Dictionary<string, string>()
            {
                ["RevealDungeonRequirements"] = "RevealDutyRequirements", // commit 7ce9b37b
                ["SeriesExpBar"] = "EnhancedExpBar", // commit 11b6231f
                ["RequisiteMaterials"] = "MaterialAllocation", // commit 730257d9
            };

            var newEnabledTweaks = new JArray();

            foreach (var tweakToken in enabledTweaks)
            {
                var tweakName = (string?)tweakToken;

                if (string.IsNullOrEmpty(tweakName))
                    continue;

                // re-enable renamed tweaks
                if (renamedTweaks.TryGetValue(tweakName, out var newTweakName))
                {
                    PluginLog.Log($"Renamed Tweak: {tweakName} => {newTweakName}");

                    // copy renamed tweak config
                    var tweakConfig = (JObject?)tweakConfigs[tweakName];
                    if (tweakConfig != null)
                    {
                        // adjust $type
                        var type = (string?)tweakConfig["$type"];
                        if (type != null)
                            tweakConfig["$type"] = type.Replace(tweakName, newTweakName);

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

            if (tweakConfigs?["EnhancedExpBar"]?["ForcePvPSeasonBar"] != null)
            {
                tweakConfigs["EnhancedExpBar"]!["ForcePvPSeriesBar"] = tweakConfigs["EnhancedExpBar"]!["ForcePvPSeasonBar"];
                ((JObject?)tweakConfigs["EnhancedExpBar"]!).Remove("ForcePvPSeasonBar");
            }

            if (version < CURRENT_CONFIG_VERSION || gameVersion == null || (string?)config[nameof(SigCacheVersion)] != gameVersion)
            {
                if ((string?)config[nameof(SigCacheVersion)] != null)
                    PluginLog.Information($"SigCache outdated: {(string?)config[nameof(SigCacheVersion)]} => {gameVersion}");

                config[nameof(SigCacheVersion)] = gameVersion;
                config.Remove(nameof(SigCache));
            }

            if (version < CURRENT_CONFIG_VERSION)
            {
                config[nameof(Version)] = CURRENT_CONFIG_VERSION;
            }
        }
        catch (Exception e)
        {
            PluginLog.Error(e, "Could not migrate configuration");
            // continue, for now
        }

        return config.ToObject<Configuration>() ?? new();
    }

    internal void Save()
    {
        Service.PluginInterface.SavePluginConfig(this);
    }
}
