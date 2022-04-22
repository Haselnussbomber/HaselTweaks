using System.Collections.Generic;
using Dalamud;
using Dalamud.Game;
using Dalamud.Logging;

namespace HaselTweaks.Tweaks;

public unsafe class AutoSortArmouryChest : Tweak
{
    public override string Name => "Auto Sort Armoury Chest";
    public Configuration Config => Plugin.Config.Tweaks.AutoSortArmouryChest;

    public static readonly Dictionary<ClientLanguage, List<string>> ConditionSet = new()
    {
        [ClientLanguage.English] = new() {
            "id", "spiritbond", "category", "lv", "ilv", "stack",
            "hq", "materia", "pdamage", "mdamage", "delay",
            "autoattack", "blockrate", "blockstrength", "defense",
            "mdefense", "str", "dex", "vit", "int", "mnd",
            "craftsmanship", "control", "gathering", "perception", "tab"
        },
        [ClientLanguage.German] = new() {
            "id", "bindung", "kategorie", "stufe", "ggstufe", "stapel",
            "hq", "materia", "pschaden", "mschaden", "verzögerung",
            "pautoattacke", "blockrate", "blockeffekt", "verteidigung",
            "mabwehr", "str", "ges", "kon", "int", "wlk",
            "kunstfertigkeit", "kontrolle", "sammeln", "expertise", "reiter"
        },
        [ClientLanguage.French] = new()
        {
            "id", "symbiose", "catégorie", "niveau", "niveauobjet", "exemplaires",
            "hq", "matéria", "dégâtsphysiques", "dégâtsmagiques", "délai",
            "attaqueauto", "tauxblocage", "forceblocage", "défense",
            "défensemagique", "for", "dex", "vit", "int", "esp",
            "habileté", "contrôle", "collecte", "savoir-faire", "onglet"
        },
        [ClientLanguage.Japanese] = new() {
            "アイテムID", "錬精度", "アイテムカテゴリー", "装備レベル", "アイテムレベル", "スタック数",
            "HQ付き", "マテリア数", "物理基本性能", "魔法基本性能", "攻撃間隔",
            "物理オートアタック", "ブロック発動力", "ブロック性能", "物理防御力",
            "魔法防御力", "STR", "DEX", "VIT", "INT", "MND",
            "作業精度", "加工精度", "獲得力", "技術力", "block"
        },
    };

    public static readonly Dictionary<ClientLanguage, List<string>> OrderSet = new()
    {
        [ClientLanguage.English] = new() { "asc", "des" },
        [ClientLanguage.German] = new() { "aufs", "abs" },
        [ClientLanguage.French] = new() { "croissant", "décroissant" },
        [ClientLanguage.Japanese] = new() { "昇順", "降順" },
    };

    public class Configuration
    {
        [ConfigField(Type = ConfigFieldTypes.SingleSelect, Options = nameof(ConditionSet))]
        public string Condition = "";

        [ConfigField(Type = ConfigFieldTypes.SingleSelect, Options = nameof(OrderSet))]
        public string Order = "";
    }

    private bool wasVisible = false;

    public override void Setup()
    {
        if (string.IsNullOrEmpty(Config.Condition))
        {
            Config.Condition = Service.ClientState.ClientLanguage switch
            {
                ClientLanguage.German => "ggstufe",
                ClientLanguage.French => "niveauobjet",
                ClientLanguage.Japanese => "アイテムID",
                _ => "ilv",
            };
        }

        if (string.IsNullOrEmpty(Config.Order))
        {
            Config.Order = Service.ClientState.ClientLanguage switch
            {
                ClientLanguage.German => "abs",
                ClientLanguage.French => "décroissant",
                ClientLanguage.Japanese => "降順",
                _ => "des",
            };
        }
    }

    public override void OnFrameworkUpdate(Framework framework)
    {
        var unitBase = Utils.GetUnitBase("ArmouryBoard");
        if (unitBase == null || unitBase->RootNode == null) return;

        var isVisible = unitBase->RootNode->IsVisible;

        if (!wasVisible && isVisible)
            Run();

        wasVisible = isVisible;
    }

    private void Run()
    {
        var definition = $"/isort condition armoury {Config.Condition} {Config.Order}";
        var execute = "/isort execute armoury";

        if (Service.ClientState.ClientLanguage == ClientLanguage.German)
        {
            definition = $"/sort def arsenal {Config.Condition} {Config.Order}";
            execute = "/sort los arsenal";
        }
        else if (Service.ClientState.ClientLanguage == ClientLanguage.French)
        {
            definition = $"/triobjet condition arsenal {Config.Condition} {Config.Order}";
            execute = "/triobjet exécuter arsenal";
        }
        else if (Service.ClientState.ClientLanguage == ClientLanguage.Japanese)
        {
            definition = $"/itemsort condition アーマリーチェスト {Config.Condition} {Config.Order}";
            execute = "/itemsort execute アーマリーチェスト";
        }

        PluginLog.Log($"[AutoSortArmouryChest] Executing {definition}");
        Plugin.XivCommon.Functions.Chat.SendMessage(definition);

        PluginLog.Log($"[AutoSortArmouryChest] Executing {execute}");
        Plugin.XivCommon.Functions.Chat.SendMessage(execute);
    }
}
