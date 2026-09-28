using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Dalamud.Configuration;
using Dalamud.Utility;
using HaselTweaks.Config.Migrations;
using HaselTweaks.JsonConverters;

namespace HaselTweaks.Config;

[RegisterSingleton(Factory = nameof(ServiceFactory))]
public partial class PluginConfig : IPluginConfiguration
{
    [JsonIgnore]
    public const int CURRENT_CONFIG_VERSION = 9;

    [JsonIgnore]
    public int LastSavedConfigHash { get; set; }

    [JsonIgnore]
    public static JsonSerializerOptions? SerializerOptions { get; private set; }

    [JsonIgnore]
    private static IDalamudPluginInterface? PluginInterface;

    [JsonIgnore]
    private static IPluginLog? PluginLog;

    public static PluginConfig ServiceFactory(IServiceProvider serviceProvider)
    {
        PluginInterface = serviceProvider.GetRequiredService<IDalamudPluginInterface>();
        PluginLog = serviceProvider.GetRequiredService<IPluginLog>();

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
            new Version8(),
            new Version9()
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

        var obj = config.Deserialize<PluginConfig>(SerializerOptions) ?? new();

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
            var hash = StringComparer.Ordinal.GetHashCode(serialized);

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
    public BiggerCharacterPreviewsConfiguration BiggerCharacterPreviews { get; init; } = new();
    public CabinetQuickStoreConfiguration CabinetQuickStore { get; init; } = new();
    public CharacterClassSwitcherConfiguration CharacterClassSwitcher { get; init; } = new();
    public CommandsConfiguration Commands { get; init; } = new();
    public CosmicResearchTodoConfiguration CosmicResearchTodo { get; init; } = new();
    public CustomChatMessageFormatsConfiguration CustomChatMessageFormats { get; init; } = new();
    public CustomChatTimestampConfiguration CustomChatTimestamp { get; init; } = new();
    public DTRConfiguration DTR { get; init; } = new();
    public DisableRewardPopupsConfiguration DisableRewardPopups { get; init; } = new();
    public EnhancedExpBarConfiguration EnhancedExpBar { get; init; } = new();
    public EnhancedIsleworksAgendaConfiguration EnhancedIsleworksAgenda { get; init; } = new();
    public EnhancedLoginLogoutConfiguration EnhancedLoginLogout { get; init; } = new();
    public EnhancedMaterialListConfiguration EnhancedMaterialList { get; init; } = new();
    public EnhancedMiragePrismBoxConfiguration EnhancedMiragePrismBox { get; init; } = new();
    public EnhancedMonsterNoteConfiguration EnhancedMonsterNote { get; init; } = new();
    public EnhancedRecipeNoteConfiguration EnhancedRecipeNote { get; init; } = new();
    public EnhancedTargetInfoConfiguration EnhancedTargetInfo { get; init; } = new();
    public EnhancedTryonConfiguration EnhancedTryon { get; init; } = new();
    public FlashTaskbarConfiguration FlashTaskbar { get; init; } = new();
    public ForcedCutsceneMusicConfiguration ForcedCutsceneMusic { get; init; } = new();
    public GearSetGridConfiguration GearSetGrid { get; init; } = new();
    public GlamourDresserAlertConfiguration GlamourDresserAlert { get; init; } = new();
    public InventoryHighlightConfiguration InventoryHighlight { get; init; } = new();
    public LockWindowPositionConfiguration LockWindowPosition { get; init; } = new();
    public MaterialAllocationConfiguration MaterialAllocation { get; init; } = new();
    public MinimapAdjustmentsConfiguration MinimapAdjustments { get; init; } = new();
    public PortraitHelperConfiguration PortraitHelper { get; init; } = new();

    [RegisterServices]
    public static void Register(IServiceCollection services)
    {
        foreach (var prop in typeof(TweakConfigs).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            services.AddSingleton(prop.PropertyType, provider
                => prop.GetValue(provider.GetRequiredService<PluginConfig>().Tweaks)!);
        }
    }
}
