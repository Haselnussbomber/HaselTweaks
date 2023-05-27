using System.Collections.Generic;
using System.Linq;
using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface;
using Dalamud.Interface.Raii;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Client.UI.Shell;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Caches;
using HaselTweaks.Structs;
using HaselTweaks.Utils;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace HaselTweaks.Tweaks;

public unsafe class AutoSorter : Tweak
{
    public override string Name => "Auto Sorter";
    public override string Description => "Sorts items inside various containers when they are opened.";
    public static Configuration Config => Plugin.Config.Tweaks.AutoSorter;

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

    public static readonly List<string> ArmourySubcategories = new()
    {
        "mh",
        "oh",
        "head",
        "body",
        "hands",
        "legs",
        "feet",
        "neck",
        "ears",
        "wrists",
        "rings",
        "soul"
    };

    public static string GetLocalizedParam(uint rowId, string? fallback = null)
    {
        var param = StringCache.GetSheetText<TextCommandParam>(rowId, "Param");
        return string.IsNullOrEmpty(param) ? fallback ?? "" : param.ToLower();
    }

    public static string? GetLocalizedParam(Dictionary<string, uint> dict, string? key, string? fallback = null)
    {
        var str = fallback ?? key;

        if (!string.IsNullOrEmpty(key) && dict.TryGetValue(key, out var rowId))
        {
            var param = StringCache.GetSheetText<TextCommandParam>(rowId, "Param");

            if (!string.IsNullOrEmpty(param))
            {
                str = param.ToLower();
            }
        }

        return str;
    }

    public record SortingRule
    {
        public bool Enabled = true;
        public string? Category = null;
        public string? Condition = null;
        public string? Order = null;

        public string? LocalizedCategory => GetLocalizedParam(CategorySet, Category);
        public string? LocalizedCondition => GetLocalizedParam(ConditionSet, Condition);
        public string? LocalizedOrder => GetLocalizedParam(OrderSet, Order);

        public List<string>? GetErrors(AutoSorter tweak, HashSet<string>? usedCategories)
        {
            List<string>? errors = null;

            if (string.IsNullOrEmpty(Category))
            {
                errors ??= new();
                errors.Add("Category is not set.");
            }

            if (string.IsNullOrEmpty(Condition))
            {
                errors ??= new();
                errors.Add("Condition is not set.");
            }

            if (string.IsNullOrEmpty(Order))
            {
                errors ??= new();
                errors.Add("Order is not set.");
            }

            if (Category == "armoury" && usedCategories != null && ArmourySubcategories.Any(usedCategories.Contains))
            {
                errors ??= new();
                errors.Add("This rule overrides a slot-based armoury rule. Move it up.");
            }

            if (Category is "rightsaddlebag" && !PlayerState.Instance()->HasPremiumSaddlebag)
            {
                errors ??= new();
                errors.Add("Not subscribed to the Companion Premium Service.");
            }

            return errors;
        }
    }

    public class Configuration
    {
        public List<SortingRule> Settings = new();
    }

    public override bool HasCustomConfig => true;
    public override void DrawCustomConfig()
    {
        if (!ImGui.BeginTable("##HaselTweaks_AutoSortSettings", 5, ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.NoPadOuterX))
        {
            ImGui.PopStyleVar();
            return;
        }

        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Category", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Condition", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Order", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, Enabled ? 120 : 85);

        var isWindowFocussed = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows);
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
            ImGui.Checkbox(key + "_Enabled", ref entry.Enabled);
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Rule is " + (entry.Enabled ? "enabled" : "disabled"));
            }
            if (ImGui.IsItemClicked())
            {
                Plugin.Config.Save();
            }

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            preview = entry.LocalizedCategory ?? "Category...";

            if (ImGui.BeginCombo(key + "Category", preview))
            {
                foreach (var kv in CategorySet)
                {
                    if (ImGui.Selectable(GetLocalizedParam(kv.Value), entry.Category == kv.Key))
                    {
                        entry.Category = kv.Key;
                        Plugin.Config.Save();
                    }

                    if (entry.Category == kv.Key)
                        ImGui.SetItemDefaultFocus();
                }

                ImGui.EndCombo();
            }

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            preview = entry.LocalizedCondition ?? "Condition...";

            if (ImGui.BeginCombo(key + "Condition", preview))
            {
                foreach (var kv in ConditionSet)
                {
                    if (ImGui.Selectable(GetLocalizedParam(kv.Value), entry.Condition == kv.Key))
                    {
                        entry.Condition = kv.Key;
                        Plugin.Config.Save();
                    }

                    if (entry.Condition == kv.Key)
                        ImGui.SetItemDefaultFocus();
                }

                ImGui.EndCombo();
            }

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            preview = entry.LocalizedOrder ?? "Order...";

            if (ImGui.BeginCombo(key + "Order", preview))
            {
                foreach (var kv in OrderSet)
                {
                    if (ImGui.Selectable(GetLocalizedParam(kv.Value), entry.Order == kv.Key))
                    {
                        entry.Order = kv.Key;
                        Plugin.Config.Save();
                    }

                    if (entry.Order == kv.Key)
                        ImGui.SetItemDefaultFocus();
                }

                ImGui.EndCombo();
            }

            ImGui.TableNextColumn();
            if (i > 0)
            {
                if (ImGuiUtils.IconButton(key + "_Up", FontAwesomeIcon.ArrowUp, "Move up"))
                {
                    entryToMoveUp = i;
                }
            }
            else
            {
                ImGui.Dummy(new(22, 22));
            }

            ImGui.SameLine();

            if (i < Config.Settings.Count - 1)
            {
                if (ImGuiUtils.IconButton(key + "_Down", FontAwesomeIcon.ArrowDown, "Move down"))
                {
                    entryToMoveDown = i;
                }
            }
            else
            {
                ImGui.Dummy(new(22, 22));
            }

            ImGui.SameLine();

            if (isWindowFocussed && (ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift)))
            {
                if (ImGuiUtils.IconButton(key + "_Delete", FontAwesomeIcon.Trash, "Delete rule"))
                {
                    entryToRemove = i;
                }
            }
            else
            {
                ImGuiUtils.IconButtonDisabled(
                    key + "_Delete",
                    FontAwesomeIcon.Trash,
                    isWindowFocussed
                        ? "Hold shift to delete rule"
                        : "Focus window and hold shift to delete rule");
            }

            ImGui.SameLine();

            if (Enabled)
            {
                if (IsBusy || queue.Any())
                {
                    ImGui.SameLine();
                    ImGuiUtils.IconButtonDisabled(FontAwesomeIcon.Terminal, "Sorting in progress. Please wait.");
                }
                else
                {
                    List<string>? disabledReasons = null;

                    if (entry.Category is "saddlebag" or "rightsaddlebag" && !IsInventoryBuddyOpen)
                    {
                        disabledReasons ??= new();
                        disabledReasons.Add("Sorting saddlebag/rightsaddlebag only works when the window is open.");
                    }

                    if (entry.Category is "retainer" && !IsRetainerInventoryOpen)
                    {
                        disabledReasons ??= new();
                        disabledReasons.Add("Sorting retainer only works when the window is open.");
                    }

                    ImGui.SameLine();

                    if (disabledReasons != null)
                    {
                        ImGuiUtils.IconButtonDisabled(
                            key + "_Execute",
                            FontAwesomeIcon.Terminal,
                            disabledReasons.Count > 1
                                ? "- " + string.Join("\n- ", disabledReasons)
                                : disabledReasons.First());
                    }
                    else
                    {
                        var errors = entry.GetErrors(this, usedCategories);
                        if (errors != null)
                        {
                            using (ImRaii.PushColor(ImGuiCol.Text, 0xff02d2ee)) // safety yellow
                            {
                                ImGuiUtils.IconButton(
                                    key + "_Execute",
                                    FontAwesomeIcon.ExclamationTriangle,
                                    errors.Count > 1
                                        ? "- " + string.Join("\n- ", errors)
                                        : errors.First());
                            }
                        }
                        else if (ImGuiUtils.IconButton(key + "_Execute", FontAwesomeIcon.Terminal, "Execute this rule"))
                        {
                            entryToExecute = i;
                        }
                    }
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
            Plugin.Config.Save();
        }

        if (Enabled)
        {
            ImGui.SameLine();

            if (!IsBusy && !queue.Any())
            {
                if (ImGui.Button("Run All##HaselTweaks_AutoSortSettings_RunAll"))
                {
                    var groups = Config.Settings
                        .FindAll(entry => !string.IsNullOrEmpty(entry.Category))
                        .GroupBy(entry => entry.Category!);

                    foreach (var group in groups)
                    {
                        queue.Enqueue(group);
                    }
                }
            }
            else
            {
                ImGuiUtils.ButtonDisabled("Run All##HaselTweaks_AutoSortSettings_RunAll");
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Sorting in progress. Please wait.");
                }
            }
        }

        if (entryToMoveUp != -1)
        {
            var removedItem = Config.Settings[entryToMoveUp];
            Config.Settings.RemoveAt(entryToMoveUp);
            Config.Settings.Insert(entryToMoveUp - 1, removedItem);
            Plugin.Config.Save();
        }

        if (entryToMoveDown != -1)
        {
            var removedItem = Config.Settings[entryToMoveDown];
            Config.Settings.RemoveAt(entryToMoveDown);
            Config.Settings.Insert(entryToMoveDown + 1, removedItem);
            Plugin.Config.Save();
        }

        if (entryToRemove != -1)
        {
            Config.Settings.RemoveAt(entryToRemove);
            Plugin.Config.Save();
        }

        if (entryToExecute != -1)
        {
            var entry = Config.Settings[entryToExecute];
            queue.Enqueue(new[] { entry }.GroupBy(entry => entry.Category!).First());
        }
    }

    private readonly Queue<IGrouping<string, SortingRule>> queue = new();
    private bool IsBusy = false;

    public static bool IsRetainerInventoryOpen => GetAddon("InventoryRetainer") != null || GetAddon("InventoryRetainerLarge") != null;
    public static bool IsInventoryBuddyOpen => GetAddon("InventoryBuddy") != null;

    private readonly Dictionary<string, bool> InventoryAddons = new()
    {
        ["Inventory"] = false,
        ["InventoryLarge"] = false,
        ["InventoryExpansion"] = false
    };

    public override void Disable()
    {
        queue.Clear();
    }

    public override void OnFrameworkUpdate(Framework framework)
    {
        if (Service.ClientState.IsLoggedIn && !(Service.Condition[ConditionFlag.BetweenAreas] || Service.Condition[ConditionFlag.OccupiedInQuestEvent] || Service.Condition[ConditionFlag.OccupiedInCutSceneEvent]))
        {
            foreach (var (name, wasVisible) in InventoryAddons)
            {
                if (GetAddon(name, out var unitBase))
                {
                    var isVisible = unitBase->IsVisible;

                    if (wasVisible != isVisible)
                    {
                        InventoryAddons[name] = isVisible;

                        if (isVisible)
                        {
                            OnOpenInventory();
                        }
                    }
                }
            }
        }

        ProcessQueue();
    }

    public override void OnAddonOpen(string addonName, AtkUnitBase* unitBase)
    {
        switch (addonName)
        {
            case "ArmouryBoard":
                OnOpenArmoury();
                break;
            // TODO: Inventories are created on login
            //case "Inventory":
            //case "InventoryLarge":
            //case "InventoryExpansion":
            //    OnOpenInventory();
            //    break;
            case "InventoryBuddy":
                OnOpenInventoryBuddy();
                break;
            case "InventoryRetainer":
            case "InventoryRetainerLarge":
                OnOpenRetainer();
                break;
        }
    }

    private void OnOpenArmoury()
    {
        var groups = Config.Settings
            .FindAll(entry => entry.Enabled && (entry.Category is "armoury" || ArmourySubcategories.Any(subcat => subcat == entry.Category)))
            .GroupBy(entry => entry.Category!);

        foreach (var group in groups)
        {
            queue.Enqueue(group);
        }
    }

    private void OnOpenInventory()
    {
        var groups = Config.Settings
            .FindAll(entry => entry.Enabled && entry.Category is "inventory")
            .GroupBy(entry => entry.Category!);

        foreach (var group in groups)
        {
            queue.Enqueue(group);
        }
    }

    private void OnOpenInventoryBuddy()
    {
        var groups = Config.Settings
            .FindAll(entry => entry.Enabled && entry.Category is "saddlebag" or "rightsaddlebag")
            .GroupBy(entry => entry.Category!);

        foreach (var group in groups)
        {
            queue.Enqueue(group);
        }
    }

    private void OnOpenRetainer()
    {
        var groups = Config.Settings
            .FindAll(entry => entry.Enabled && entry.Category is "retainer")
            .GroupBy(entry => entry.Category!);

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

        var itemOrderModule = ItemOrderModule.Instance();
        if (itemOrderModule == null)
            return;

        if (nextGroup.Key is "armoury" || ArmourySubcategories.Any(subcat => subcat == nextGroup.Key))
        {
            // check if ItemOrderModule is busy
            if (itemOrderModule == null || itemOrderModule->UserFileEvent.IsSavePending)
            {
                Debug("ItemOrderModule is busy, waiting.");
                return;
            }

            for (var i = 0; i < itemOrderModule->ArmourySorterSpan.Length; i++)
            {
                var sorter = itemOrderModule->ArmourySorterSpan[i].Value;
                if (sorter != null && sorter->SortFunctionIndex != -1)
                {
                    Debug($"ItemOrderModule: Sorter #{i} ({sorter->InventoryType}) is busy, waiting.");
                    return;
                }
            }
        }

        IsBusy = true;

        try
        {
            var group = queue.Dequeue();

            if (!group.Any())
                return;

            var key = group.Key;

            if (string.IsNullOrEmpty(key))
                return;

            Log($"Sorting Category: {key}");

            var category = GetLocalizedParam(CategorySet, key);
            if (string.IsNullOrEmpty(category))
            {
                Error($"Can not localize category: GetLocalizedParam returned \"{category}\".");
                return;
            }

            var raptureShellModule = RaptureShellModule.Instance;
            if (raptureShellModule == null)
            {
                Warning("Could not resolve RaptureShellModule");
                return;
            }

            if (raptureShellModule->IsTextCommandUnavailable)
            {
                Warning("Text commands are unavailable, skipping.");
                return;
            }

            if ((key is "saddlebag" or "rightsaddlebag") && !IsInventoryBuddyOpen)
            {
                Warning("Sorting for saddlebag/rightsaddlebag only works when the window is open, skipping.");
                return;
            }

            var playerState = PlayerState.Instance();
            if (playerState == null)
            {
                Warning("Could not resolve PlayerState");
                return;
            }

            if (key is "rightsaddlebag" && !playerState->HasPremiumSaddlebag)
            {
                Warning("Not subscribed to the Companion Premium Service, skipping.");
                return;
            }

            if (key is "retainer" && !IsRetainerInventoryOpen)
            {
                Warning("Sorting for retainer only works when the window is open, skipping.");
                return;
            }

            var cmd = $"/itemsort clear {category}";
            Log($"Executing {cmd}");
            Chat.SendMessage(cmd);

            foreach (var rule in group)
            {
                var condition = rule.LocalizedCondition;
                if (string.IsNullOrEmpty(condition))
                {
                    Error($"Can not localize condition \"{rule.Condition}\", skipping. (GetLocalizedParam returned \"{condition}\")");
                    continue;
                }

                var order = rule.LocalizedOrder;
                if (string.IsNullOrEmpty(order))
                {
                    Error($"Can not localize order \"{rule.Order}\", skipping. (GetLocalizedParam returned \"{order}\")");
                    continue;
                }

                cmd = $"/itemsort condition {category} {condition} {order}";
                Log($"Executing {cmd}");
                Chat.SendMessage(cmd);
            }

            cmd = $"/itemsort execute {category}";
            Log($"Executing {cmd}");
            Chat.SendMessage(cmd);
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
}
