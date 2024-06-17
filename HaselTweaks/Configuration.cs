using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using HaselCommon;
using HaselCommon.Interfaces;
using HaselTweaks.JsonConverters;
using HaselTweaks.Tweaks;

namespace HaselTweaks;

public partial class Configuration : IConfiguration
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
    public AchievementLinkTooltipConfiguration AchievementLinkTooltip { get; init; } = new();
    public AetherCurrentHelperConfiguration AetherCurrentHelper { get; init; } = new();
    public AutoSorterConfiguration AutoSorter { get; init; } = new();
    public BackgroundMusicKeybindConfiguration BackgroundMusicKeybind { get; init; } = new();
    public CharacterClassSwitcherConfiguration CharacterClassSwitcher { get; init; } = new();
    public CommandsConfiguration Commands { get; init; } = new();
    public CustomChatMessageFormatsConfiguration CustomChatMessageFormats { get; init; } = new();
    public CustomChatTimestampConfiguration CustomChatTimestamp { get; init; } = new();
    public DTRConfiguration DTR { get; init; } = new();
    public EnhancedExpBarConfiguration EnhancedExpBar { get; init; } = new();
    public EnhancedIsleworksAgendaConfiguration EnhancedIsleworksAgenda { get; init; } = new();
    public EnhancedLoginLogoutConfiguration EnhancedLoginLogout { get; init; } = new();
    public EnhancedMaterialListConfiguration EnhancedMaterialList { get; init; } = new();
    public ForcedCutsceneMusicConfiguration ForcedCutsceneMusic { get; init; } = new();
    public GearSetGridConfiguration GearSetGrid { get; init; } = new();
    public InventoryHighlightConfiguration InventoryHighlight { get; init; } = new();
    public LockWindowPositionConfiguration LockWindowPosition { get; init; } = new();
    public MaterialAllocationConfiguration MaterialAllocation { get; init; } = new();
    public MinimapAdjustmentsConfiguration MinimapAdjustments { get; init; } = new();
    public PortraitHelperConfiguration PortraitHelper { get; init; } = new();
    public ScrollableTabsConfiguration ScrollableTabs { get; init; } = new();
}

public partial class Configuration
{
    private static JsonSerializerOptions? _serializerOptions = null;
    public static JsonSerializerOptions SerializerOptions
    {
        get
        {
            if (_serializerOptions == null)
            {
                _serializerOptions = new JsonSerializerOptions()
                {
                    IncludeFields = true,
                    WriteIndented = true,
                };

                _serializerOptions.Converters.Add(new HaselCommonTextSeStringConverter());
            }

            return _serializerOptions;
        }
    }

    [JsonIgnore]
    public int LastSavedConfigHash { get; set; }

    public void Save()
        => ConfigurationManager.Save(this);

    public string Serialize()
        => JsonSerializer.Serialize(this, SerializerOptions);

    public static Configuration Load()
    {
        return ConfigurationManager.Load(CURRENT_CONFIG_VERSION, Deserialize, Migrate);
    }

    public static Configuration? Deserialize(ref JsonObject config)
        => config.Deserialize<Configuration>(SerializerOptions);

    public static bool Migrate(int version, ref JsonObject config)
    {
        var enabledTweaks = (JsonArray?)config[nameof(EnabledTweaks)];
        var tweakConfigs = (JsonObject?)config[nameof(Tweaks)];

        if (enabledTweaks == null || tweakConfigs == null)
            return true;

        var success = true;

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
                Service.Get<IPluginLog>().Info("[MigrationV6] Portrait thumbnails now use the preset guid as the name. Renaming files...");

                var newPresets = new JsonArray();
                var portraitsPath = Path.Join(Service.Get<DalamudPluginInterface>().ConfigDirectory.FullName, "Portraits");

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

                        Service.Get<IPluginLog>().Info($"[MigrationV6]   {oldPath} => {newPath}");

                        try
                        {
                            File.Move(oldPath, newPath);
                        }
                        catch (Exception e)
                        {
                            Service.Get<IPluginLog>().Error(e, "[MigrationV6] Could not move file {0} to {1}", oldPath, newPath);
                            success &= false;
                        }

                        presetCopy.Remove("TextureHash");

                        newPresets.Add(presetCopy);
                    }
                    else
                    {
                        var presetCode = (string?)preset["Preset"];
                        Service.Get<IPluginLog>().Error("[MigrationV6] Could not find thumbnail {0} for {1}. Please re-import.", oldPath, presetCode ?? string.Empty);
                        success &= false;
                    }
                }

                tweakConfigs!["PortraitHelper"]!["Presets"] = newPresets;

                Service.Get<IPluginLog>().Info("[MigrationV6] Done!");
            }
        }

        return success;
    }
}
