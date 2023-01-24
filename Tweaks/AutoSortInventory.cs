using System.Collections.Generic;
using Dalamud;
using Dalamud.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselTweaks.Structs;

namespace HaselTweaks.Tweaks;

public class AutoSortInventory : Tweak
{
    public override string Name => "Auto Sort Inventory";
    public override string Description => "Sorts items inside the Inventory upon opening it.";
    public static Configuration Config => HaselTweaks.Configuration.Instance.Tweaks.AutoSortInventory;

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

    private bool wasVisible;

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

    public override unsafe void OnFrameworkUpdate(Framework framework)
    {
        var addon = GetAddon<AddonInventory>(AgentId.Inventory);
        if (addon == null || addon->AtkUnitBase.RootNode == null)
            return;

        var isVisible = addon->AtkUnitBase.RootNode->IsVisible;

        if (!wasVisible && isVisible)
            Run();

        wasVisible = isVisible;
    }

    private void Run()
    {
        var definition = $"/isort condition inventory {Config.Condition} {Config.Order}";
        var execute = "/isort execute inventory";

        switch (Service.ClientState.ClientLanguage)
        {
            case ClientLanguage.German:
                definition = $"/sort def inventar {Config.Condition} {Config.Order}";
                execute = "/sort los inventar";
                break;
            case ClientLanguage.French:
                definition = $"/triobjet condition inventaire {Config.Condition} {Config.Order}";
                execute = "/triobjet exécuter inventaire";
                break;
            case ClientLanguage.Japanese:
                definition = $"/itemsort condition アーマリーチェスト {Config.Condition} {Config.Order}";
                execute = "/itemsort execute アーマリーチェスト";
                break;
        }

        Log($"Executing {definition}");
        Chat.SendMessage(definition);

        Log($"Executing {execute}");
        Chat.SendMessage(execute);
    }
}
