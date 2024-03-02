using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Dalamud.Configuration;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Utility;
using HaselTweaks.Tweaks;

namespace HaselTweaks;

public partial class Configuration : IPluginConfiguration
{
    [JsonIgnore]
    public const int CURRENT_CONFIG_VERSION = 6;

    public int Version { get; set; } = CURRENT_CONFIG_VERSION;
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

// I really wish I could move this to HaselCommon, but I haven't found a way yet.
public partial class Configuration : IDisposable
{
    public static JsonSerializerOptions DefaultJsonSerializerOptions = new()
    {
        IncludeFields = true,
        WriteIndented = true
    };

    [JsonIgnore]
    public int LastSavedConfigHash;

    public static Configuration Load()
    {
        try
        {
            var configPath = Service.PluginInterface.ConfigFile.FullName;
            if (!File.Exists(configPath))
                return new();

            var jsonData = File.ReadAllText(configPath);
            if (string.IsNullOrEmpty(jsonData))
                return new();

            var config = JsonNode.Parse(jsonData);
            if (config is not JsonObject configObject)
                return new();

            var version = (int?)configObject[nameof(Version)] ?? 0;
            if (version == 0)
                return new();

            if (version < CURRENT_CONFIG_VERSION)
            {
                try
                {
                    var configBackupPath = configPath + ".bak";
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

                Service.PluginLog.Information("Starting config migration: {currentVersion} -> {targetVersion}", version, CURRENT_CONFIG_VERSION);

                Migrate(version, configObject);

                config[nameof(Version)] = CURRENT_CONFIG_VERSION;

                Service.PluginLog.Information("Config migration completed.");
            }

            var deserializedConfig = configObject.Deserialize<Configuration>(DefaultJsonSerializerOptions);
            if (deserializedConfig == null)
                return new();

            deserializedConfig.Save();

            return deserializedConfig;
        }
        catch (Exception ex)
        {
            Service.PluginLog.Error(ex, "Could not load the configuration file. Creating a new one.");

            if (!Service.TranslationManager.TryGetTranslation("Plugin.DisplayName", out var pluginName))
                pluginName = Service.PluginInterface.InternalName;

            Service.PluginInterface.UiBuilder.AddNotification(
                t("Notification.CouldNotLoadConfig"),
                pluginName,
                NotificationType.Error,
                5000
            );

            return new();
        }
    }

    public static void Migrate(int version, JsonObject config)
    {
        var enabledTweaks = (JsonArray?)config[nameof(EnabledTweaks)];
        var tweakConfigs = (JsonObject?)config[nameof(Tweaks)];

        if (enabledTweaks == null || tweakConfigs == null)
            return;

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

        // Version 6: removed TextureHash in favor of Id
        if (version < 6)
        {
            var presets = (JsonArray?)tweakConfigs?["PortraitHelper"]?["Presets"];
            if (presets != null && presets.Count > 0)
            {
                Service.PluginLog.Info("[MigrationV6] Portrait thumbnails now use the preset guid as the name. Renaming files...");

                var newPresets = new JsonArray();
                var portraitsPath = Path.Join(Service.PluginInterface.ConfigDirectory.FullName, "Portraits");

                if (!Directory.Exists(portraitsPath))
                    Directory.CreateDirectory(portraitsPath);

                for (var i = 0; i < presets.Count; i++)
                {
                    var preset = (JsonObject?)presets[i];
                    if (preset == null)
                        continue;

                    var presetCopy = preset.Deserialize<JsonObject>(); // net8: switch to .Clone()
                    if (presetCopy == null)
                        continue;

                    var id = (string?)preset["Id"];
                    var textureHash = (string?)preset["TextureHash"];

                    if (id == null || textureHash == null)
                        continue;

                    var guid = Guid.Parse(id);

                    var oldPath = Path.Join(portraitsPath, $"{textureHash}.png");

                    if (File.Exists(oldPath))
                    {
                        var newPath = PortraitHelper.GetPortraitThumbnailPath(guid);

                        Service.PluginLog.Info($"[MigrationV6]   {oldPath} => {newPath}");

                        try
                        {
                            File.Move(oldPath, newPath);
                        }
                        catch (Exception e)
                        {
                            Service.PluginLog.Error(e, "[MigrationV6] Could not move file {0} to {1}", oldPath, newPath);
                        }

                        presetCopy.Remove("TextureHash");

                        newPresets.Add(presetCopy);
                    }
                    else
                    {
                        var presetCode = (string?)preset["Preset"];
                        Service.PluginLog.Error("[MigrationV6] Could not find thumbnail {0} for {1}. Please re-import.", oldPath, presetCode ?? string.Empty);
                    }
                }

                tweakConfigs!["PortraitHelper"]!["Presets"] = newPresets;

                Service.PluginLog.Info("[MigrationV6] Done!");
            }
        }
    }

    public void Save()
    {
        try
        {
            var serialized = JsonSerializer.Serialize(this, DefaultJsonSerializerOptions);
            var hash = serialized.GetHashCode();

            if (LastSavedConfigHash != hash)
            {
                Util.WriteAllTextSafe(Service.PluginInterface.ConfigFile.FullName, serialized);
                LastSavedConfigHash = hash;
                Service.PluginLog.Information("Configuration saved.");
            }
        }
        catch (Exception e)
        {
            Service.PluginLog.Error(e, "Error saving config");
        }
    }

    void IDisposable.Dispose()
    {
        Save();
    }
}
