using System.Collections.Generic;
using System.Linq;
using Dalamud;
using Dalamud.Game;
using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Structs;
using HaselTweaks.Utils;
using ImGuiNET;

namespace HaselTweaks.Tweaks;

public unsafe class AutoSorter : Tweak
{
    public override string Name => "Auto Sorter";
    public override string Description => "Sorts items inside various containers when they are opened.";
    public static Configuration Config => HaselTweaks.Configuration.Instance.Tweaks.AutoSorter;

    public static readonly Dictionary<ClientLanguage, Dictionary<string, string>> CategorySet = new()
    {
        [ClientLanguage.English] = new()
        {
            ["inventory"] = "inventory",
            ["retainer"] = "retainer",
            ["armoury"] = "armoury",
            ["saddlebag"] = "saddlebag",
            ["rightsaddlebag"] = "rightsaddlebag",
            ["mh"] = "mh",
            ["oh"] = "oh",
            ["head"] = "head",
            ["body"] = "body",
            ["hands"] = "hands",
            ["legs"] = "legs",
            ["feet"] = "feet",
            ["neck"] = "neck",
            ["ears"] = "ears",
            ["wrists"] = "wrists",
            ["rings"] = "rings",
            ["soul"] = "soul"
        },
        [ClientLanguage.German] = new()
        {
            ["inventory"] = "inventar",
            ["retainer"] = "gehilfe",
            ["armoury"] = "arsenal",
            ["saddlebag"] = "satteltasche",
            ["rightsaddlebag"] = "satteltasche2",
            ["mh"] = "hauptwaffe",
            ["oh"] = "nebenwaffe",
            ["head"] = "kopf",
            ["body"] = "rumpf",
            ["hands"] = "hände",
            ["legs"] = "beine",
            ["feet"] = "füße",
            ["neck"] = "hals",
            ["ears"] = "ohren",
            ["wrists"] = "handgelenke",
            ["rings"] = "ringe",
            ["soul"] = "job"
        },
        [ClientLanguage.French] = new()
        {
            ["inventory"] = "inventaire",
            ["retainer"] = "servant",
            ["armoury"] = "arsenal",
            ["saddlebag"] = "sacochechocobo",
            ["rightsaddlebag"] = "sacochechocobo2",
            ["mh"] = "direct",
            ["oh"] = "nondir",
            ["head"] = "tête",
            ["body"] = "torse",
            ["hands"] = "mains",
            ["legs"] = "jambes",
            ["feet"] = "pieds",
            ["neck"] = "cou",
            ["ears"] = "oreilles",
            ["wrists"] = "poignets",
            ["rings"] = "bagues",
            ["soul"] = "âme"
        },
        [ClientLanguage.Japanese] = new()
        {
            ["inventory"] = "所持品",
            ["retainer"] = "リテイナー所持品",
            ["armoury"] = "アーマリーチェスト",
            ["saddlebag"] = "チョコボかばん",
            ["rightsaddlebag"] = "チョコボかばん2",
            ["mh"] = "メインアーム",
            ["oh"] = "サブアーム",
            ["head"] = "頭",
            ["body"] = "胴",
            ["hands"] = "手",
            ["legs"] = "脚",
            ["feet"] = "足",
            ["neck"] = "首",
            ["ears"] = "耳",
            ["wrists"] = "腕",
            ["rings"] = "指",
            ["soul"] = "ソウルクリスタル"
        },
    };

    public static readonly Dictionary<ClientLanguage, Dictionary<string, string>> ConditionSet = new()
    {
        [ClientLanguage.English] = new()
        {
            ["id"] = "id",
            ["spiritbond"] = "spiritbond",
            ["category"] = "category",
            ["lv"] = "lv",
            ["ilv"] = "ilv",
            ["stack"] = "stack",
            ["hq"] = "hq",
            ["materia"] = "materia",
            ["pdamage"] = "pdamage",
            ["mdamage"] = "mdamage",
            ["delay"] = "delay",
            ["autoattack"] = "autoattack",
            ["blockrate"] = "blockrate",
            ["blockstrength"] = "blockstrength",
            ["defense"] = "defense",
            ["mdefense"] = "mdefense",
            ["str"] = "str",
            ["dex"] = "dex",
            ["vit"] = "vit",
            ["int"] = "int",
            ["mnd"] = "mnd",
            ["craftsmanship"] = "craftsmanship",
            ["control"] = "control",
            ["gathering"] = "gathering",
            ["perception"] = "perception",
            ["tab"] = "tab"
        },
        [ClientLanguage.German] = new()
        {
            ["id"] = "id",
            ["spiritbond"] = "bindung",
            ["category"] = "kategorie",
            ["lv"] = "stufe",
            ["ilv"] = "ggstufe",
            ["stack"] = "stapel",
            ["hq"] = "hq",
            ["materia"] = "materia",
            ["pdamage"] = "pschaden",
            ["mdamage"] = "mschaden",
            ["delay"] = "verzögerung",
            ["autoattack"] = "pautoattacke",
            ["blockrate"] = "blockrate",
            ["blockstrength"] = "blockeffekt",
            ["defense"] = "verteidigung",
            ["mdefense"] = "mabwehr",
            ["str"] = "str",
            ["dex"] = "ges",
            ["vit"] = "kon",
            ["int"] = "int",
            ["mnd"] = "wlk",
            ["craftsmanship"] = "kunstfertigkeit",
            ["control"] = "kontrolle",
            ["gathering"] = "sammeln",
            ["perception"] = "expertise",
            ["tab"] = "reiter"
        },
        [ClientLanguage.French] = new()
        {
            ["id"] = "id",
            ["spiritbond"] = "symbiose",
            ["category"] = "catégorie",
            ["lv"] = "niveau",
            ["ilv"] = "niveauobjet",
            ["stack"] = "exemplaires",
            ["hq"] = "hq",
            ["materia"] = "matéria",
            ["pdamage"] = "dégâtsphysiques",
            ["mdamage"] = "dégâtsmagiques",
            ["delay"] = "délai",
            ["autoattack"] = "attaqueauto",
            ["blockrate"] = "tauxblocage",
            ["blockstrength"] = "forceblocage",
            ["defense"] = "défense",
            ["mdefense"] = "défensemagique",
            ["str"] = "for",
            ["dex"] = "dex",
            ["vit"] = "vit",
            ["int"] = "int",
            ["mnd"] = "esp",
            ["craftsmanship"] = "habileté",
            ["control"] = "contrôle",
            ["gathering"] = "collecte",
            ["perception"] = "savoir-faire",
            ["tab"] = "onglet"
        },
        [ClientLanguage.Japanese] = new()
        {
            ["id"] = "アイテムID",
            ["spiritbond"] = "錬精度",
            ["category"] = "アイテムカテゴリー",
            ["lv"] = "装備レベル",
            ["ilv"] = "アイテムレベル",
            ["stack"] = "スタック数",
            ["hq"] = "HQ付き",
            ["materia"] = "マテリア数",
            ["pdamage"] = "物理基本性能",
            ["mdamage"] = "魔法基本性能",
            ["delay"] = "攻撃間隔",
            ["autoattack"] = "物理オートアタック",
            ["blockrate"] = "ブロック発動力",
            ["blockstrength"] = "ブロック性能",
            ["defense"] = "物理防御力",
            ["mdefense"] = "魔法防御力",
            ["str"] = "STR",
            ["dex"] = "DEX",
            ["vit"] = "VIT",
            ["int"] = "INT",
            ["mnd"] = "MND",
            ["craftsmanship"] = "作業精度",
            ["control"] = "加工精度",
            ["gathering"] = "獲得力",
            ["perception"] = "技術力",
            ["tab"] = "block"
        },
    };

    public static readonly Dictionary<ClientLanguage, Dictionary<string, string>> OrderSet = new()
    {
        [ClientLanguage.English] = new()
        {
            ["asc"] = "asc",
            ["des"] = "des"
        },
        [ClientLanguage.German] = new()
        {
            ["asc"] = "aufs",
            ["des"] = "abs"
        },
        [ClientLanguage.French] = new()
        {
            ["asc"] = "croissant",
            ["des"] = "décroissant"
        },
        [ClientLanguage.Japanese] = new()
        {
            ["asc"] = "昇順",
            ["des"] = "降順"
        },
    };

    public static string GetLocalizedString(Dictionary<ClientLanguage, Dictionary<string, string>> dict, string key, string fallback = "")
    {
        if (string.IsNullOrEmpty(key) || !dict.ContainsKey(Service.ClientState.ClientLanguage))
            return fallback;

        dict[Service.ClientState.ClientLanguage].TryGetValue(key, out var result);
        return result ?? fallback;
    }

    public record CategorySetting
    {
        public string Category = "";
        public string Condition = "";
        public string Order = "";
    }

    public class Configuration
    {
        public List<CategorySetting> Settings = new();
    }

    private void SaveConfig() => HaselTweaks.Configuration.Save();
    public override bool HasCustomConfig => true;
    public override void DrawCustomConfig()
    {
        if (!ImGui.BeginTable("##HaselTweaks_AutoSortSettings", 4, ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.NoPadOuterX))
        {
            ImGui.PopStyleVar();
            return;
        }

        ImGui.TableSetupColumn("Category", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Condition", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Order", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 120);

        var lang = Service.ClientState.ClientLanguage;
        var preview = "";
        var i = 0;
        var entryToRemove = -1;
        var entryToMoveUp = -1;
        var entryToMoveDown = -1;
        var entryToExecute = -1;
        var usedCategories = new HashSet<string>();

        foreach (var entry in Config.Settings)
        {
            var key = $"##HaselTweaks_AutoSortSettings_Setting[{i}]";

            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            preview = GetLocalizedString(CategorySet, entry.Category, "Category...");

            if (ImGui.BeginCombo(key + "Category", preview))
            {
                foreach (var kv in CategorySet[lang])
                {
                    if (ImGui.Selectable(kv.Value, entry.Category == kv.Key))
                    {
                        entry.Category = kv.Key;
                        SaveConfig();
                    }

                    if (entry.Category == kv.Key)
                        ImGui.SetItemDefaultFocus();
                }

                ImGui.EndCombo();
            }

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            preview = GetLocalizedString(ConditionSet, entry.Condition, "Condition...");

            if (ImGui.BeginCombo(key + "Condition", preview))
            {
                foreach (var kv in ConditionSet[lang])
                {
                    if (ImGui.Selectable(kv.Value, entry.Condition == kv.Key))
                    {
                        entry.Condition = kv.Key;
                        SaveConfig();
                    }

                    if (entry.Condition == kv.Key)
                        ImGui.SetItemDefaultFocus();
                }

                ImGui.EndCombo();
            }

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            preview = GetLocalizedString(OrderSet, entry.Order, "Order...");

            if (ImGui.BeginCombo(key + "Order", preview))
            {
                foreach (var kv in OrderSet[lang])
                {
                    if (ImGui.Selectable(kv.Value, entry.Order == kv.Key))
                    {
                        entry.Order = kv.Key;
                        SaveConfig();
                    }

                    if (entry.Order == kv.Key)
                        ImGui.SetItemDefaultFocus();
                }

                ImGui.EndCombo();
            }

            ImGui.TableNextColumn();
            if (i > 0)
            {
                if (ImGuiUtils.IconButton(FontAwesomeIcon.ArrowUp, key + "Up"))
                {
                    entryToMoveUp = i;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Move up");
                }
            }
            else
            {
                ImGui.Dummy(new(22, 22));
            }

            ImGui.SameLine();

            if (i < Config.Settings.Count - 1)
            {
                if (ImGuiUtils.IconButton(FontAwesomeIcon.ArrowDown, key + "Down"))
                {
                    entryToMoveDown = i;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Move down");
                }
            }
            else
            {
                ImGui.Dummy(new(22, 22));
            }

            ImGui.SameLine();

            if (ImGuiUtils.IconButton(FontAwesomeIcon.Trash, key + "Delete"))
            {
                entryToRemove = i;
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Delete");
            }

            ImGui.SameLine();

            List<string>? errors = null;

            if (string.IsNullOrEmpty(entry.Category))
            {
                errors ??= new();
                errors.Add("Category is not set.");
            }

            if (string.IsNullOrEmpty(entry.Condition))
            {
                errors ??= new();
                errors.Add("Condition is not set.");
            }

            if (string.IsNullOrEmpty(entry.Order))
            {
                errors ??= new();
                errors.Add("Order is not set.");
            }

            if (entry.Category == "armoury" && (
                usedCategories.Contains("mh") ||
                usedCategories.Contains("oh") ||
                usedCategories.Contains("head") ||
                usedCategories.Contains("body") ||
                usedCategories.Contains("hands") ||
                usedCategories.Contains("legs") ||
                usedCategories.Contains("feet") ||
                usedCategories.Contains("neck") ||
                usedCategories.Contains("ears") ||
                usedCategories.Contains("wrists") ||
                usedCategories.Contains("rings") ||
                usedCategories.Contains("soul")
                ))
            {
                errors ??= new();
                errors.Add("This rule overrides a slot-based armoury rule. Move it up.");
            }

            if (errors != null)
            {
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Text, 0xff02d2ee); // safety yellow
                ImGuiUtils.IconButton(FontAwesomeIcon.ExclamationTriangle);
                ImGui.PopStyleColor();
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("This rule has errors:\n\n- " + string.Join("\n -", errors));
                }
            }
            else if ((entry.Category is "saddlebag" or "rightsaddlebag") && !InventoryBuddyObserver.IsOpen)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, 0xff02d2ee); // safety yellow
                ImGuiUtils.IconButton(FontAwesomeIcon.ExclamationTriangle);
                ImGui.PopStyleColor();
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Sorting for saddlebags/rightsaddlebag only works when the window is open.");
                }
            }
            else
            {
                if (ImGuiUtils.IconButton(FontAwesomeIcon.Terminal, key + "Execute"))
                {
                    entryToExecute = i;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Execute this rule only");
                }
            }

            if (!string.IsNullOrEmpty(entry.Category))
                usedCategories.Add(entry.Category);

            i++;
        }

        ImGui.EndTable();

        if (ImGui.Button("Add##HaselTweaks_AutoSortSettings_Add"))
        {
            Config.Settings.Add(new());
            SaveConfig();
        }

        ImGui.SameLine();

        if (ImGui.Button("Run All##HaselTweaks_AutoSortSettings_RunAll"))
        {
            var settings = Config.Settings
                .GroupBy(entry => entry.Category);

            ProcessCategories(settings);
        }

        if (entryToMoveUp != -1)
        {
            var removedItem = Config.Settings[entryToMoveUp];
            Config.Settings.RemoveAt(entryToMoveUp);
            Config.Settings.Insert(entryToMoveUp - 1, removedItem);
            SaveConfig();
        }

        if (entryToMoveDown != -1)
        {
            var removedItem = Config.Settings[entryToMoveDown];
            Config.Settings.RemoveAt(entryToMoveDown);
            Config.Settings.Insert(entryToMoveDown + 1, removedItem);
            SaveConfig();
        }

        if (entryToRemove != -1)
        {
            Config.Settings.RemoveAt(entryToRemove);
            SaveConfig();
        }

        if (entryToExecute != -1)
        {
            var entry = Config.Settings[entryToExecute];
            ProcessCategory(new[] { entry });
        }
    }

    private readonly unsafe AddonObserver ArmouryObserver = new(() => GetAddon(AgentId.ArmouryBoard));
    private readonly unsafe AddonObserver InventoryObserver = new(() => GetAddon(AgentId.Inventory));
    private readonly unsafe AddonObserver InventoryBuddyObserver = new(() => GetAddon(AgentId.InventoryBuddy));
    private readonly unsafe AddonObserver RetainerObserver = new(() => GetAddon(AgentId.Retainer));

    public override void Enable()
    {
        ArmouryObserver.OnOpen += OnOpenArmoury;
        InventoryObserver.OnOpen += OnOpenInventory;
        InventoryBuddyObserver.OnOpen += OnOpenInventoryBuddy;
        RetainerObserver.OnOpen += OnOpenRetainer;
    }

    public override void Disable()
    {
        ArmouryObserver.OnOpen -= OnOpenArmoury;
        InventoryObserver.OnOpen -= OnOpenInventory;
        InventoryBuddyObserver.OnOpen -= OnOpenInventoryBuddy;
        RetainerObserver.OnOpen -= OnOpenRetainer;
    }

    public override unsafe void OnFrameworkUpdate(Framework framework)
    {
        ArmouryObserver.Update();
        InventoryObserver.Update();
        InventoryBuddyObserver.Update();
        RetainerObserver.Update();
    }

    private void OnOpenArmoury(AddonObserver sender, AtkUnitBase* unitBase)
    {
        var settings = Config.Settings
            .FindAll(entry => entry.Category is "armoury" or "mh" or "oh" or "head" or "body" or "hands" or "legs" or "feet" or "neck" or "ears" or "wrists" or "rings" or "soul")
            .GroupBy(entry => entry.Category);

        ProcessCategories(settings);
    }

    private void OnOpenInventory(AddonObserver sender, AtkUnitBase* unitBase)
    {
        var category = Config.Settings
            .FindAll(entry => entry.Category is "inventory");

        ProcessCategory(category);
    }

    private void OnOpenInventoryBuddy(AddonObserver sender, AtkUnitBase* unitBase)
    {
        var categories = Config.Settings
            .FindAll(entry => entry.Category is "saddlebag" or "rightsaddlebag")
            .GroupBy(entry => entry.Category);

        ProcessCategories(categories);
    }

    private void OnOpenRetainer(AddonObserver sender, AtkUnitBase* unitBase)
    {
        var category = Config.Settings
            .FindAll(entry => entry.Category is "retainer");

        ProcessCategory(category);
    }

    private void ProcessCategories(IEnumerable<IGrouping<string, CategorySetting>> categories)
    {
        if (!categories.Any())
            return;

        foreach (var category in categories)
        {
            Log($"Sorting Category: {category.Key}");
            ProcessCategory(category);
        }
    }

    private void ProcessCategory(IEnumerable<CategorySetting> settings)
    {
        if (!settings.Any())
            return;

        var category = settings.First().Category;

        if ((category is "saddlebag" or "rightsaddlebag") && !InventoryBuddyObserver.IsOpen)
        {
            Warning("Sorting for saddlebags/rightsaddlebag only works when the window is open, skipping.");
            return;
        }

        ClearConditions(category);

        foreach (var entry in settings)
        {
            DefineCondition(entry.Category, entry.Condition, entry.Order);
        }

        ExecuteSort(category);
    }

    private void ClearConditions(string category)
    {
        category = GetLocalizedString(CategorySet, category);

        if (string.IsNullOrEmpty(category))
        {
            Error("Category not set!");
            return;
        }

        var definition = $"/itemsort clear {category}";
        Log($"Executing {definition}");
        Chat.SendMessage(definition);
    }

    private void DefineCondition(string category, string condition, string order)
    {
        category = GetLocalizedString(CategorySet, category);
        condition = GetLocalizedString(ConditionSet, condition);
        order = GetLocalizedString(OrderSet, order);

        if (string.IsNullOrEmpty(category))
        {
            Error("Category not set!");
            return;
        }

        if (string.IsNullOrEmpty(condition))
        {
            Error("Condition not set!");
            return;
        }

        if (string.IsNullOrEmpty(order))
        {
            Error("Order not set!");
            return;
        }

        var definition = $"/itemsort condition {category} {condition} {order}";
        Log($"Executing {definition}");
        Chat.SendMessage(definition);
    }

    private void ExecuteSort(string category)
    {
        category = GetLocalizedString(CategorySet, category);

        if (string.IsNullOrEmpty(category))
        {
            Error("Category not set!");
            return;
        }

        var execute = $"/itemsort execute {category}";
        Log($"Executing {execute}");
        Chat.SendMessage(execute);
    }
}
