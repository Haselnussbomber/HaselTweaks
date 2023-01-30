using System.Collections.Generic;
using System.Linq;
using Dalamud.Game;
using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Structs;
using HaselTweaks.Utils;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace HaselTweaks.Tweaks;

public unsafe class AutoSorter : Tweak
{
    public override string Name => "Auto Sorter";
    public override string Description => "Sorts items inside various containers when they are opened.";
    public static Configuration Config => HaselTweaks.Configuration.Instance.Tweaks.AutoSorter;

    public static readonly Dictionary<string, uint> CategorySet = new()
    {
        ["inventory"] = 257,
        ["retainer"] = 261,
        ["armoury"] = 259,
        ["saddlebag"] = 467,
        ["rightsaddlebag"] = 469,
        ["mh"] = 26,
        ["oh"] = 28,
        ["head"] = 37,
        ["body"] = 41,
        ["hands"] = 47,
        ["legs"] = 45,
        ["feet"] = 49,
        ["neck"] = 53,
        ["ears"] = 285,
        ["wrists"] = 287,
        ["rings"] = 289,
        ["soul"] = 291
    };

    public static readonly Dictionary<string, uint> ConditionSet = new()
    {
        ["id"] = 271,
        ["spiritbond"] = 275,
        ["category"] = 263,
        ["lv"] = 265,
        ["ilv"] = 267,
        ["stack"] = 269,
        ["hq"] = 277,
        ["materia"] = 279,
        ["pdamage"] = 293,
        ["mdamage"] = 295,
        ["delay"] = 297,
        ["autoattack"] = 299,
        ["blockrate"] = 301,
        ["blockstrength"] = 303,
        ["defense"] = 305,
        ["mdefense"] = 307,
        ["str"] = 309,
        ["dex"] = 311,
        ["vit"] = 313,
        ["int"] = 315,
        ["mnd"] = 317,
        ["craftsmanship"] = 321,
        ["control"] = 323,
        ["gathering"] = 325,
        ["perception"] = 327,
        ["tab"] = 273
    };

    public static readonly Dictionary<string, uint> OrderSet = new()
    {
        ["asc"] = 281,
        ["des"] = 283
    };

    public static string GetLocalizedParam(uint rowId, string fallback = "")
        => StringUtils.GetSheetText<TextCommandParam>(rowId, "Param") ?? fallback;

    public static string GetLocalizedParam(Dictionary<string, uint> dict, string key, string fallback = "")
        => dict.TryGetValue(key, out var value) ? GetLocalizedParam(value, fallback) : fallback;

    public record SortingRule
    {
        public string Category = "";
        public string Condition = "";
        public string Order = "";
    }

    public class Configuration
    {
        public List<SortingRule> Settings = new();
    }

    private static void SaveConfig() => HaselTweaks.Configuration.Save();
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

            preview = GetLocalizedParam(CategorySet, entry.Category, "Category...");

            if (ImGui.BeginCombo(key + "Category", preview))
            {
                foreach (var kv in CategorySet)
                {
                    if (ImGui.Selectable(GetLocalizedParam(kv.Value), entry.Category == kv.Key))
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

            preview = GetLocalizedParam(ConditionSet, entry.Condition, "Condition...");

            if (ImGui.BeginCombo(key + "Condition", preview))
            {
                foreach (var kv in ConditionSet)
                {
                    if (ImGui.Selectable(GetLocalizedParam(kv.Value), entry.Condition == kv.Key))
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

            preview = GetLocalizedParam(OrderSet, entry.Order, "Order...");

            if (ImGui.BeginCombo(key + "Order", preview))
            {
                foreach (var kv in OrderSet)
                {
                    if (ImGui.Selectable(GetLocalizedParam(kv.Value), entry.Order == kv.Key))
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

            if (entry.Category is "rightsaddlebag" && !HasPremiumSaddlebag)
            {
                errors ??= new();
                errors.Add("Not subscribed to the Companion Premium Service.");
            }

            if (entry.Category is "saddlebag" or "rightsaddlebag" && !InventoryBuddyObserver.IsOpen)
            {
                errors ??= new();
                errors.Add("Sorting for saddlebag/rightsaddlebag only works when the window is open.");
            }

            if (entry.Category is "retainer" && !RetainerObserver.IsOpen)
            {
                errors ??= new();
                errors.Add("Sorting for retainer only works when the window is open.");
            }

            if (errors != null)
            {
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Text, 0xff02d2ee); // safety yellow
                ImGuiUtils.IconButton(FontAwesomeIcon.ExclamationTriangle);
                ImGui.PopStyleColor();
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("This rule has errors:\n\n- " + string.Join("\n- ", errors));
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
            foreach (var group in Config.Settings.GroupBy(entry => entry.Category))
            {
                queue.Enqueue(group);
            }
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
            queue.Enqueue(new[] { entry }.GroupBy(entry => entry.Category).First());
        }
    }

    private readonly AddonObserver ArmouryObserver = new(() => GetAddon(AgentId.ArmouryBoard));
    private readonly AddonObserver InventoryObserver = new(() => GetAddon(AgentId.Inventory));
    private readonly AddonObserver InventoryBuddyObserver = new(() => GetAddon(AgentId.InventoryBuddy));
    private readonly AddonObserver RetainerObserver = new(() => GetAddon(AgentId.Retainer));

    private bool HasPremiumSaddlebag => *(bool*)((nint)PlayerState.Instance() + 0x133); // last checked: Patch 6.31

    private readonly Queue<IGrouping<string, SortingRule>> queue = new();
    private bool IsBusy = false;

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

    public override void OnFrameworkUpdate(Framework framework)
    {
        ArmouryObserver.Update();
        InventoryObserver.Update();
        InventoryBuddyObserver.Update();
        RetainerObserver.Update();

        ProcessQueue();
    }

    private void OnOpenArmoury(AddonObserver sender, AtkUnitBase* unitBase)
    {
        var groups = Config.Settings
            .FindAll(entry => entry.Category is "armoury" or "mh" or "oh" or "head" or "body" or "hands" or "legs" or "feet" or "neck" or "ears" or "wrists" or "rings" or "soul")
            .GroupBy(entry => entry.Category);

        foreach (var group in groups)
        {
            queue.Enqueue(group);
        }
    }

    private void OnOpenInventory(AddonObserver sender, AtkUnitBase* unitBase)
    {
        var groups = Config.Settings
            .FindAll(entry => entry.Category is "inventory")
            .GroupBy(entry => entry.Category);

        foreach (var group in groups)
        {
            queue.Enqueue(group);
        }
    }

    private void OnOpenInventoryBuddy(AddonObserver sender, AtkUnitBase* unitBase)
    {
        var groups = Config.Settings
            .FindAll(entry => entry.Category is "saddlebag" or "rightsaddlebag")
            .GroupBy(entry => entry.Category);

        foreach (var group in groups)
        {
            queue.Enqueue(group);
        }
    }

    private void OnOpenRetainer(AddonObserver sender, AtkUnitBase* unitBase)
    {
        var groups = Config.Settings
            .FindAll(entry => entry.Category is "retainer")
            .GroupBy(entry => entry.Category);

        foreach (var group in groups)
        {
            queue.Enqueue(group);
        }
    }

    private void ProcessQueue()
    {
        if (IsBusy || !queue.Any())
            return;

        var nextGroup = queue.Peek();
        if (nextGroup == null)
            return;

        if (nextGroup.Key is "armoury" or "mh" or "oh" or "head" or "body" or "hands" or "legs" or "feet" or "neck" or "ears" or "wrists" or "rings" or "soul")
        {
            // check if ItemOrderModule is busy
            var itemOrderModule = ItemOrderModule.Instance;
            if (itemOrderModule == null || itemOrderModule->IsLocked)
            {
                Debug("ItemOrderModule is busy, waiting.");
                return;
            }
            if (itemOrderModule->ArmouryBoardSorter == null || itemOrderModule->ArmouryBoardSorter->Status != -1)
            {
                Debug("ItemOrderModule ArmouryBoardSorter is busy, waiting.");
                return;
            }
        }

        IsBusy = true;

        try
        {
            var group = queue.Dequeue();

            if (!group.Any())
                return;

            var category = group.Key;

            Log($"Sorting Category: {category}");

            if ((category is "saddlebag" or "rightsaddlebag") && !InventoryBuddyObserver.IsOpen)
            {
                Warning("Sorting for saddlebag/rightsaddlebag only works when the window is open, skipping.");
                return;
            }

            if (category is "rightsaddlebag" && !HasPremiumSaddlebag)
            {
                Warning("Not subscribed to the Companion Premium Service, skipping.");
                return;
            }

            if (category is "retainer" && !RetainerObserver.IsOpen)
            {
                Warning("Sorting for retainer only works when the window is open, skipping.");
                return;
            }

            ClearConditions(category);

            foreach (var rule in group)
            {
                DefineCondition(rule.Category, rule.Condition, rule.Order);
            }

            ExecuteSort(category);
        }
        catch (Exception ex)
        {
            Error(ex, "Unexpected error during sorting");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ClearConditions(string category)
    {
        category = GetLocalizedParam(CategorySet, category);

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
        category = GetLocalizedParam(CategorySet, category);
        condition = GetLocalizedParam(ConditionSet, condition);
        order = GetLocalizedParam(OrderSet, order);

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
        category = GetLocalizedParam(CategorySet, category);

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
