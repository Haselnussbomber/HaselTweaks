using System.Collections.Generic;
using System.IO;
using System.IO.Hashing;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Dalamud.Configuration;
using Dalamud.Interface.Internal.Notifications;
using HaselCommon.Enums;
using HaselCommon.Interfaces;
using HaselTweaks.Tweaks;

namespace HaselTweaks;

public partial class Configuration : IPluginConfiguration
{
    [JsonIgnore]
    public const int CURRENT_CONFIG_VERSION = 5;

    public int Version { get; set; } = CURRENT_CONFIG_VERSION;
}

public partial class Configuration : ITranslationConfig
{
    public string PluginLanguage { get; set; } = "en";
    public PluginLanguageOverride PluginLanguageOverride { get; set; } = PluginLanguageOverride.Dalamud;
}

public partial class Configuration
{
    public HashSet<string> EnabledTweaks { get; init; } = [];
    public TweakConfigs Tweaks { get; init; } = new();
    public HashSet<string> LockedImGuiWindows { get; init; } = [];
}

public class TweakConfigs
{
    public AetherCurrentHelperConfiguration AetherCurrentHelper { get; init; } = new();
    public AutoSorterConfiguration AutoSorter { get; init; } = new();
    public BackgroundMusicKeybindConfiguration BackgroundMusicKeybind { get; init; } = new();
    public CharacterClassSwitcherConfiguration CharacterClassSwitcher { get; init; } = new();
    public CommandsConfiguration Commands { get; init; } = new();
    public CustomChatTimestampConfiguration CustomChatTimestamp { get; init; } = new();
    public DTRConfiguration DTR { get; init; } = new();
    public EnhancedExpBarConfiguration EnhancedExpBar { get; init; } = new();
    public EnhancedIsleworksAgendaConfiguration EnhancedIsleworksAgenda { get; init; } = new();
    public EnhancedLoginLogoutConfiguration EnhancedLoginLogout { get; init; } = new();
    public EnhancedMaterialListConfiguration EnhancedMaterialList { get; init; } = new();
    public ForcedCutsceneMusicConfiguration ForcedCutsceneMusic { get; init; } = new();
    public GearSetGridConfiguration GearSetGrid { get; init; } = new();
    public LockWindowPositionConfiguration LockWindowPosition { get; init; } = new();
    public MaterialAllocationConfiguration MaterialAllocation { get; init; } = new();
    public MinimapAdjustmentsConfiguration MinimapAdjustments { get; init; } = new();
    public PortraitHelperConfiguration PortraitHelper { get; init; } = new();
    public ScrollableTabsConfiguration ScrollableTabs { get; init; } = new();
}

public partial class Configuration
{
    public static JsonSerializerOptions DefaultJsonSerializerOptions = new()
    {
        IncludeFields = true,
        WriteIndented = true
    };

    public static string LastSavedConfigHash { get; private set; } = string.Empty;

    internal static Configuration Load()
    {
        var configPath = Service.PluginInterface.ConfigFile.FullName;
        var configBackupPath = configPath + ".bak";
        var jsonData = string.Empty;
        JsonNode? config = null;

        try
        {
            jsonData = File.Exists(configPath) ? File.ReadAllText(configPath) : null;

            if (string.IsNullOrEmpty(jsonData))
                return new();

            config = JsonNode.Parse(jsonData);
        }
        catch (Exception ex)
        {
            Service.PluginLog.Error(ex, "Could not load configuration, creating a new one");

            Service.PluginInterface.UiBuilder.AddNotification(
                t("HaselTweaks.Config.CouldNotLoadConfigNotification.Text"),
                "HaselTweaks",
                NotificationType.Error,
                5000
            );
        }

        if (config is not JsonObject configObject)
            return new();

        var version = (int?)configObject[nameof(Version)] ?? 0;
        if (version == 0)
            return new();

        var needsMigration = version < CURRENT_CONFIG_VERSION;
        if (needsMigration)
        {
            try
            {
                var jsonBackupData = File.Exists(configBackupPath) ? File.ReadAllText(configBackupPath) : null;
                if (string.IsNullOrEmpty(jsonBackupData) || !string.Equals(jsonData, jsonBackupData))
                {
                    File.Copy(configPath, configBackupPath, true);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Could not back up config before migration", ex);
            }

            Service.PluginLog.Information($"Starting config migration: {version} -> {CURRENT_CONFIG_VERSION}");

            try
            {
                var enabledTweaks = (JsonArray?)configObject[nameof(EnabledTweaks)];
                var tweakConfigs = (JsonObject?)configObject[nameof(Tweaks)];

                if (enabledTweaks == null || tweakConfigs == null)
                    return new();

                void RenameTweak(string oldName, string newName)
                {
                    if (enabledTweaks.Contains(oldName))
                    {
                        enabledTweaks.Remove(oldName);
                        enabledTweaks.Add(newName);
                    }

                    if (tweakConfigs[oldName] is JsonObject tweakConfig)
                    {
                        tweakConfigs.Remove(oldName);
                        tweakConfigs[newName] = tweakConfig;
                    }
                }

                if (version < 2)
                {
                    RenameTweak("RevealDungeonRequirements", "RevealDutyRequirements"); // commit 7ce9b37b
                    RenameTweak("SeriesExpBar", "EnhancedExpBar"); // commit 11b6231f
                    RenameTweak("RequisiteMaterials", "MaterialAllocation"); // commit 730257d9

                    if (tweakConfigs?["EnhancedExpBar"]?["ForcePvPSeasonBar"] != null)
                    {
                        tweakConfigs["EnhancedExpBar"]!["ForcePvPSeriesBar"] = tweakConfigs["EnhancedExpBar"]!["ForcePvPSeasonBar"];
                        ((JsonObject?)tweakConfigs["EnhancedExpBar"]!).Remove("ForcePvPSeasonBar");
                    }
                }

                // Version 5: switched to System.Text.Json
                if (version < 5)
                {
                    // fix for "The JSON value could not be converted to System.UInt64. Path: $.Tweaks.EnhancedLoginLogout.PetMirageSettings.$type"
                    ((JsonObject?)tweakConfigs?["EnhancedLoginLogout"]?["PetMirageSettings"])?.Remove("$type");

                    // fix for "The JSON value could not be converted to System.UInt64. Path: $.Tweaks.EnhancedLoginLogout.SelectedEmotes.$type"
                    ((JsonObject?)tweakConfigs?["EnhancedLoginLogout"]?["SelectedEmotes"])?.Remove("$type");
                }

                configObject[nameof(Version)] = CURRENT_CONFIG_VERSION;
            }
            catch (Exception ex)
            {
                Service.PluginLog.Error(ex, "Could not migrate configuration");
                // continue, for now
            }

            Service.PluginLog.Information("Config migration completed.");
        }

        Configuration? deserializedConfig;

        try
        {
            deserializedConfig = configObject.Deserialize<Configuration>(DefaultJsonSerializerOptions);
        }
        catch (Exception ex)
        {
            Service.PluginLog.Error(ex, "Could not deserialize configuration, creating a new one");

            Service.PluginInterface.UiBuilder.AddNotification(
                t("HaselTweaks.Config.CouldNotLoadConfigNotification.Text"),
                "HaselTweaks",
                NotificationType.Error,
                5000
            );

            return new();
        }

        if (deserializedConfig == null)
            return new();

        if (needsMigration)
        {
            deserializedConfig.Save();
        }
        else
        {
            try
            {
                var serialized = JsonSerializer.Serialize(deserializedConfig, DefaultJsonSerializerOptions);
                LastSavedConfigHash = GenerateHash(serialized);
            }
            catch (Exception e)
            {
                Service.PluginLog.Error(e, "Error generating config hash");
            }
        }

        return deserializedConfig;
    }

    internal void Save()
    {
        try
        {
            var configPath = Service.PluginInterface.ConfigFile.FullName;
            var serialized = JsonSerializer.Serialize(this, DefaultJsonSerializerOptions);
            var hash = GenerateHash(serialized);

            if (LastSavedConfigHash != hash)
            {
                File.WriteAllText(configPath, serialized);
                LastSavedConfigHash = hash;
                Service.PluginLog.Information("Configuration saved.");
            }
        }
        catch (Exception e)
        {
            Service.PluginLog.Error(e, "Error saving config");
        }
    }

    private static string GenerateHash(string serialized)
        => BitConverter.ToInt64(XxHash64.Hash(Encoding.UTF8.GetBytes(serialized))).ToString("x");
}
