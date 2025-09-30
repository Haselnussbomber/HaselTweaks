using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Dalamud.Configuration;
using Dalamud.Utility;
using HaselTweaks.Config.Migrations;
using HaselTweaks.JsonConverters;

namespace HaselTweaks.Config;

public partial class PluginConfig : IPluginConfiguration
{
    [JsonIgnore]
    public const int CURRENT_CONFIG_VERSION = 8;

    [JsonIgnore]
    public int LastSavedConfigHash { get; set; }

    [JsonIgnore]
    public static JsonSerializerOptions? SerializerOptions { get; private set; }

    [JsonIgnore]
    private static IDalamudPluginInterface? PluginInterface;

    [JsonIgnore]
    private static IPluginLog? PluginLog;

    public static PluginConfig Load(IDalamudPluginInterface pluginInterface, IPluginLog pluginLog)
    {
        PluginInterface = pluginInterface;
        PluginLog = pluginLog;

        SerializerOptions = new JsonSerializerOptions()
        {
            IncludeFields = true,
            WriteIndented = true,
        };
        SerializerOptions.Converters.Add(new ReadOnlySeStringConverter());

        var fileInfo = PluginInterface.ConfigFile;
        if (!fileInfo.Exists || fileInfo.Length < 2)
            return new();

        var json = File.ReadAllText(fileInfo.FullName);
        var node = JsonNode.Parse(json);
        if (node == null)
            return new();

        if (node is not JsonObject config)
            return new();

        var version = config[nameof(Version)]?.GetValue<int>();
        if (version == null)
            return new();

        var migrated = false;

        IConfigMigration[] migrations = [
            new Version2(),
            new Version5(),
            new Version6(PluginInterface, PluginLog),
            new Version7(),
            new Version8()
        ];

        foreach (var migration in migrations)
        {
            if (version < migration.Version)
            {
                PluginLog.Information("Migrating from version {version} to {migrationVersion}...", version, migration.Version);

                migration.Migrate(ref config);
                version = migration.Version;
                config[nameof(Version)] = version;
                migrated = true;
            }
        }

        var obj = JsonSerializer.Deserialize<PluginConfig>(config, SerializerOptions) ?? new();

        if (migrated)
        {
            PluginLog.Information("Configuration migrated successfully.");
            obj.Save();
        }

        return obj;
    }

    public void Save()
    {
        try
        {
            var serialized = JsonSerializer.Serialize(this, SerializerOptions);
            var hash = serialized.GetHashCode();

            if (LastSavedConfigHash != hash)
            {
                FilesystemUtil.WriteAllTextSafe(PluginInterface!.ConfigFile.FullName, serialized);
                LastSavedConfigHash = hash;
                PluginLog?.Information("Configuration saved.");
            }
        }
        catch (Exception e)
        {
            PluginLog?.Error(e, "Error saving config");
        }
    }
}

public partial class PluginConfig
{
    public int Version { get; set; } = CURRENT_CONFIG_VERSION;
    public HashSet<string> EnabledTweaks { get; set; } = [];
    public TweakConfigs Tweaks { get; set; } = new();
}

public class TweakConfigs
{
    public AchievementLinkTooltipConfiguration AchievementLinkTooltip { get; init; } = new();
    public AetherCurrentHelperConfiguration AetherCurrentHelper { get; init; } = new();
    public AutoSorterConfiguration AutoSorter { get; init; } = new();
    public BackgroundMusicKeybindConfiguration BackgroundMusicKeybind { get; init; } = new();
    public CharacterClassSwitcherConfiguration CharacterClassSwitcher { get; init; } = new();
    public CosmicResearchTodoConfiguration CosmicResearchTodo { get; init; } = new();
    public CommandsConfiguration Commands { get; init; } = new();
    public CustomChatMessageFormatsConfiguration CustomChatMessageFormats { get; init; } = new();
    public CustomChatTimestampConfiguration CustomChatTimestamp { get; init; } = new();
    public DTRConfiguration DTR { get; init; } = new();
    public EnhancedExpBarConfiguration EnhancedExpBar { get; init; } = new();
    public EnhancedIsleworksAgendaConfiguration EnhancedIsleworksAgenda { get; init; } = new();
    public EnhancedLoginLogoutConfiguration EnhancedLoginLogout { get; init; } = new();
    public EnhancedMaterialListConfiguration EnhancedMaterialList { get; init; } = new();
    public EnhancedMonsterNoteConfiguration EnhancedMonsterNote { get; init; } = new();
    public EnhancedTargetInfoConfiguration EnhancedTargetInfo { get; init; } = new();
    public ForcedCutsceneMusicConfiguration ForcedCutsceneMusic { get; init; } = new();
    public GearSetGridConfiguration GearSetGrid { get; init; } = new();
    public InventoryHighlightConfiguration InventoryHighlight { get; init; } = new();
    public LockWindowPositionConfiguration LockWindowPosition { get; init; } = new();
    public MaterialAllocationConfiguration MaterialAllocation { get; init; } = new();
    public MinimapAdjustmentsConfiguration MinimapAdjustments { get; init; } = new();
    public PortraitHelperConfiguration PortraitHelper { get; init; } = new();
    public ScrollableTabsConfiguration ScrollableTabs { get; init; } = new();
    public ShopItemIconsConfiguration ShopItemIcons { get; init; } = new();
}

public static class PluginConfigExtension
{
    public static void AddConfig(this IServiceCollection services, PluginConfig pluginConfig)
    {
        services.AddSingleton(pluginConfig);
        services.AddSingleton(pluginConfig.Tweaks.AchievementLinkTooltip);
        services.AddSingleton(pluginConfig.Tweaks.AetherCurrentHelper);
        services.AddSingleton(pluginConfig.Tweaks.AutoSorter);
        services.AddSingleton(pluginConfig.Tweaks.BackgroundMusicKeybind);
        services.AddSingleton(pluginConfig.Tweaks.CharacterClassSwitcher);
        services.AddSingleton(pluginConfig.Tweaks.CosmicResearchTodo);
        services.AddSingleton(pluginConfig.Tweaks.Commands);
        services.AddSingleton(pluginConfig.Tweaks.CustomChatMessageFormats);
        services.AddSingleton(pluginConfig.Tweaks.CustomChatTimestamp);
        services.AddSingleton(pluginConfig.Tweaks.DTR);
        services.AddSingleton(pluginConfig.Tweaks.EnhancedExpBar);
        services.AddSingleton(pluginConfig.Tweaks.EnhancedIsleworksAgenda);
        services.AddSingleton(pluginConfig.Tweaks.EnhancedLoginLogout);
        services.AddSingleton(pluginConfig.Tweaks.EnhancedMaterialList);
        services.AddSingleton(pluginConfig.Tweaks.EnhancedMonsterNote);
        services.AddSingleton(pluginConfig.Tweaks.EnhancedTargetInfo);
        services.AddSingleton(pluginConfig.Tweaks.ForcedCutsceneMusic);
        services.AddSingleton(pluginConfig.Tweaks.GearSetGrid);
        services.AddSingleton(pluginConfig.Tweaks.InventoryHighlight);
        services.AddSingleton(pluginConfig.Tweaks.LockWindowPosition);
        services.AddSingleton(pluginConfig.Tweaks.MaterialAllocation);
        services.AddSingleton(pluginConfig.Tweaks.MinimapAdjustments);
        services.AddSingleton(pluginConfig.Tweaks.PortraitHelper);
        services.AddSingleton(pluginConfig.Tweaks.ScrollableTabs);
        services.AddSingleton(pluginConfig.Tweaks.ShopItemIcons);
    }
}
