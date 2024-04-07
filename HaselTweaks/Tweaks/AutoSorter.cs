using System.Collections.Generic;
using System.Linq;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Client.UI.Shell;
using HaselCommon.Utils;
using HaselTweaks.Structs;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace HaselTweaks.Tweaks;

public class AutoSorterConfiguration
{
    public List<SortingRule> Settings = [];
    public bool SortArmouryOnJobChange = true;

    public record SortingRule
    {
        public bool Enabled = true;
        public string? Category = null;
        public string? Condition = null;
        public string? Order = null;

        public string? LocalizedCategory => AutoSorter.GetLocalizedParam(AutoSorter.CategorySet, Category);
        public string? LocalizedCondition => AutoSorter.GetLocalizedParam(AutoSorter.ConditionSet, Condition);
        public string? LocalizedOrder => AutoSorter.GetLocalizedParam(AutoSorter.OrderSet, Order);

        public List<string>? GetErrors(HashSet<string>? usedCategories)
        {
            List<string>? errors = null;

            if (string.IsNullOrEmpty(Category))
            {
                errors ??= [];
                errors.Add(t("AutoSorter.SortingRule.Error.CategoryNotSet"));
            }

            if (string.IsNullOrEmpty(Condition))
            {
                errors ??= [];
                errors.Add(t("AutoSorter.SortingRule.Error.ConditionNotSet"));
            }

            if (string.IsNullOrEmpty(Order))
            {
                errors ??= [];
                errors.Add(t("AutoSorter.SortingRule.Error.OrderNotSet"));
            }

            if (Category == "armoury" && usedCategories != null && AutoSorter.ArmourySubcategories.Any(usedCategories.Contains))
            {
                errors ??= [];
                errors.Add(t("AutoSorter.SortingRule.Error.OverridesArmouryRule"));
            }

            unsafe
            {
                if (Category is "rightsaddlebag" && !PlayerState.Instance()->HasPremiumSaddlebag)
                {
                    errors ??= [];
                    errors.Add(t("AutoSorter.SortingRule.Error.PremiumSaddlebagNotSubscribed"));
                }
            }

            return errors;
        }
    }
}

[Tweak]
public unsafe class AutoSorter : Tweak<AutoSorterConfiguration>
{
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

    public static readonly List<string> ArmourySubcategories =
    [
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
    ];

    public static string GetLocalizedParam(uint rowId, string? fallback = null)
    {
        var param = Service.DataManager.GetExcelSheet<TextCommandParam>(Service.ClientState.ClientLanguage)?.GetRow(rowId)?.Param.RawString;
        return string.IsNullOrEmpty(param) ? fallback ?? "" : param.ToLower();
    }

    public static string? GetLocalizedParam(Dictionary<string, uint> dict, string? key, string? fallback = null)
    {
        var str = fallback ?? key;

        if (!string.IsNullOrEmpty(key) && dict.TryGetValue(key, out var rowId))
        {
            var param = Service.DataManager.GetExcelSheet<TextCommandParam>(Service.ClientState.ClientLanguage)?.GetRow(rowId)?.Param.RawString;

            if (!string.IsNullOrEmpty(param))
            {
                str = param.ToLower();
            }
        }

        return str;
    }

    public override void DrawConfig()
    {
        ImGuiUtils.DrawSection(t("HaselTweaks.Config.SectionTitle.Configuration"));

        ImGui.Checkbox(t("AutoSorter.Config.SortArmouryOnJobChange.Label"), ref Config.SortArmouryOnJobChange);

        ImGuiUtils.DrawPaddedSeparator();

        using var table = ImRaii.Table("##Table", 5, ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.NoPadOuterX);
        if (!table.Success)
            return;

        var ItemInnerSpacing = ImGui.GetStyle().ItemInnerSpacing;
        var ArrowUpButtonSize = ImGuiUtils.GetIconButtonSize(FontAwesomeIcon.ArrowUp);
        var ArrowDownButtonSize = ImGuiUtils.GetIconButtonSize(FontAwesomeIcon.ArrowDown);
        var TrashButtonSize = ImGuiUtils.GetIconButtonSize(FontAwesomeIcon.Trash);
        var TerminalButtonSize = ImGuiUtils.GetIconButtonSize(FontAwesomeIcon.Terminal);

        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Category", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Condition", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Order", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed,
            ArrowUpButtonSize.X +
            ItemInnerSpacing.X +
            ArrowDownButtonSize.X +
            ItemInnerSpacing.X +
            TrashButtonSize.X +
            (Enabled ? ItemInnerSpacing.X + TerminalButtonSize.X : 0));

        var isWindowFocused = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows);
        var preview = "";
        var i = 0;
        var entryToRemove = -1;
        var entryToMoveUp = -1;
        var entryToMoveDown = -1;
        var entryToExecute = -1;
        var usedCategories = new HashSet<string>();

        foreach (var entry in Config.Settings)
        {
            var key = $"##Setting[{i}]";

            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.Checkbox(key + "_Enabled", ref entry.Enabled);
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(t(entry.Enabled
                    ? "AutoSorter.Config.EnableCheckmark.Tooltip.RuleIsEnabled"
                    : "AutoSorter.Config.EnableCheckmark.Tooltip.RuleIsDisabled"));
            }
            if (ImGui.IsItemClicked())
            {
                Service.GetService<Configuration>().Save();
            }

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            preview = entry.LocalizedCategory ?? t("AutoSorter.Config.CategoryCombo.Placeholder");

            if (ImGui.BeginCombo(key + "Category", preview))
            {
                foreach (var kv in CategorySet)
                {
                    if (ImGui.Selectable(GetLocalizedParam(kv.Value), entry.Category == kv.Key))
                    {
                        entry.Category = kv.Key;
                        Service.GetService<Configuration>().Save();
                    }

                    if (entry.Category == kv.Key)
                        ImGui.SetItemDefaultFocus();
                }

                ImGui.EndCombo();
            }

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            preview = entry.LocalizedCondition ?? t("AutoSorter.Config.ConditionCombo.Placeholder");

            if (ImGui.BeginCombo(key + "Condition", preview))
            {
                foreach (var kv in ConditionSet)
                {
                    if (ImGui.Selectable(GetLocalizedParam(kv.Value), entry.Condition == kv.Key))
                    {
                        entry.Condition = kv.Key;
                        Service.GetService<Configuration>().Save();
                    }

                    if (entry.Condition == kv.Key)
                        ImGui.SetItemDefaultFocus();
                }

                ImGui.EndCombo();
            }

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            preview = entry.LocalizedOrder ?? t("AutoSorter.Config.OrderCombo.Placeholder");

            if (ImGui.BeginCombo(key + "Order", preview))
            {
                foreach (var kv in OrderSet)
                {
                    if (ImGui.Selectable(GetLocalizedParam(kv.Value), entry.Order == kv.Key))
                    {
                        entry.Order = kv.Key;
                        Service.GetService<Configuration>().Save();
                    }

                    if (entry.Order == kv.Key)
                        ImGui.SetItemDefaultFocus();
                }

                ImGui.EndCombo();
            }

            ImGui.TableNextColumn();
            if (i > 0)
            {
                if (ImGuiUtils.IconButton(key + "_Up", FontAwesomeIcon.ArrowUp, t("AutoSorter.Config.MoveRuleUpButton.Tooltip")))
                {
                    entryToMoveUp = i;
                }
            }
            else
            {
                ImGui.Dummy(ArrowUpButtonSize);
            }

            ImGui.SameLine(0, ItemInnerSpacing.X);

            if (i < Config.Settings.Count - 1)
            {
                if (ImGuiUtils.IconButton(key + "_Down", FontAwesomeIcon.ArrowDown, t("AutoSorter.Config.MoveRuleDownButton.Tooltip")))
                {
                    entryToMoveDown = i;
                }
            }
            else
            {
                ImGui.Dummy(ArrowDownButtonSize);
            }

            ImGui.SameLine(0, ItemInnerSpacing.X);

            if (isWindowFocused && (ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift)))
            {
                if (ImGuiUtils.IconButton(key + "_Delete", FontAwesomeIcon.Trash, t("AutoSorter.Config.DeleteRuleButton.Tooltip.HoldingShift")))
                {
                    entryToRemove = i;
                }
            }
            else
            {
                ImGuiUtils.IconButton(
                    key + "_Delete",
                    FontAwesomeIcon.Trash,
                    t(isWindowFocused
                        ? "AutoSorter.Config.DeleteRuleButton.Tooltip.NotHoldingShift"
                        : "AutoSorter.Config.DeleteRuleButton.Tooltip.WindowNotFocused"),
                    disabled: true);
            }

            ImGui.SameLine(0, ItemInnerSpacing.X);

            if (Enabled)
            {
                if (_isBusy || _queue.Count != 0)
                {
                    ImGui.SameLine();
                    ImGuiUtils.IconButton(key + "_Execute", FontAwesomeIcon.Terminal, t("AutoSorter.SortingInProgress"), disabled: true);
                }
                else
                {
                    List<string>? disabledReasons = null;

                    if (entry.Category is "saddlebag" or "rightsaddlebag" && !IsInventoryBuddyOpen)
                    {
                        disabledReasons ??= [];
                        disabledReasons.Add(t("AutoSorter.Config.ExecuteButton.Tooltip.SaddlebagNotOpen"));
                    }

                    if (entry.Category is "retainer" && !IsRetainerInventoryOpen)
                    {
                        disabledReasons ??= [];
                        disabledReasons.Add(t("AutoSorter.Config.ExecuteButton.Tooltip.RetainerNotOpen"));
                    }

                    ImGui.SameLine(0, ItemInnerSpacing.X);

                    if (disabledReasons != null)
                    {
                        ImGuiUtils.IconButton(
                            key + "_Execute",
                            FontAwesomeIcon.Terminal,
                            disabledReasons.Count > 1
                                ? "- " + string.Join("\n- ", disabledReasons)
                                : disabledReasons.First(),
                            disabled: true);
                    }
                    else
                    {
                        var errors = entry.GetErrors(usedCategories);
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
                        else if (ImGuiUtils.IconButton(key + "_Execute", FontAwesomeIcon.Terminal, t("AutoSorter.Config.ExecuteButton.Tooltip.Ready")))
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

        table?.Dispose();

        if (ImGui.Button(t("AutoSorter.Config.AddButton.Label")))
        {
            Config.Settings.Add(new());
            Service.GetService<Configuration>().Save();
        }

        if (Enabled)
        {
            ImGui.SameLine();

            if (!_isBusy && _queue.Count == 0)
            {
                if (ImGui.Button(t("AutoSorter.Config.RunAllButton.Label")))
                {
                    var groups = Config.Settings
                        .FindAll(entry => !string.IsNullOrEmpty(entry.Category))
                        .GroupBy(entry => entry.Category!);

                    foreach (var group in groups)
                    {
                        _queue.Enqueue(group);
                    }
                }
            }
            else
            {
                using (ImRaii.Disabled())
                    ImGui.Button(t("AutoSorter.Config.RunAllButton.Label"));

                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(t("AutoSorter.SortingInProgress"));
            }
        }

        if (entryToMoveUp != -1)
        {
            var removedItem = Config.Settings[entryToMoveUp];
            Config.Settings.RemoveAt(entryToMoveUp);
            Config.Settings.Insert(entryToMoveUp - 1, removedItem);
            Service.GetService<Configuration>().Save();
        }

        if (entryToMoveDown != -1)
        {
            var removedItem = Config.Settings[entryToMoveDown];
            Config.Settings.RemoveAt(entryToMoveDown);
            Config.Settings.Insert(entryToMoveDown + 1, removedItem);
            Service.GetService<Configuration>().Save();
        }

        if (entryToRemove != -1)
        {
            Config.Settings.RemoveAt(entryToRemove);
            Service.GetService<Configuration>().Save();
        }

        if (entryToExecute != -1)
        {
            var entry = Config.Settings[entryToExecute];
            _queue.Enqueue(new[] { entry }.GroupBy(entry => entry.Category!).First());
        }
    }

    private readonly Queue<IGrouping<string, AutoSorterConfiguration.SortingRule>> _queue = new();
    private bool _isBusy = false;
    private byte _lastClassJobId = 0;

    public static bool IsRetainerInventoryOpen => IsAddonOpen("InventoryRetainer") || IsAddonOpen("InventoryRetainerLarge");
    public static bool IsInventoryBuddyOpen => IsAddonOpen("InventoryBuddy");

    public override void OnLogin()
    {
        _lastClassJobId = (byte)(Service.ClientState.LocalPlayer?.ClassJob.Id ?? 0);
        _queue.Clear();
    }

    public override void OnLogout()
    {
        _lastClassJobId = 0;
        _queue.Clear();
    }

    public override void Disable()
    {
        _queue.Clear();
    }

    public override void OnFrameworkUpdate()
    {
        if (!Service.ClientState.IsLoggedIn)
            return;

        if (Config.SortArmouryOnJobChange)
        {
            var classJobId = PlayerState.Instance()->CurrentClassJobId;
            if (_lastClassJobId != classJobId)
            {
                _lastClassJobId = classJobId;

                if (IsAddonOpen("ArmouryBoard"))
                {
                    OnOpenArmoury();
                }
            }
        }

        ProcessQueue();
    }

    public override void OnAddonOpen(string addonName)
    {
        switch (addonName)
        {
            case "ArmouryBoard":
                OnOpenArmoury();
                break;
            case "InventoryBuddy":
                OnOpenInventoryBuddy();
                break;
            case "InventoryRetainer":
            case "InventoryRetainerLarge":
                OnOpenRetainer();
                break;
            case "Inventory":
            case "InventoryLarge":
            case "InventoryExpansion":
                OnOpenInventory();
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
            _queue.Enqueue(group);
        }
    }

    private void OnOpenInventory()
    {
        if (Conditions.IsInBetweenAreas || Conditions.IsOccupiedInQuestEvent || Conditions.IsOccupiedInCutSceneEvent)
            return;

        var groups = Config.Settings
            .FindAll(entry => entry.Enabled && entry.Category is "inventory")
            .GroupBy(entry => entry.Category!);

        foreach (var group in groups)
        {
            _queue.Enqueue(group);
        }
    }

    private void OnOpenInventoryBuddy()
    {
        var groups = Config.Settings
            .FindAll(entry => entry.Enabled && entry.Category is "saddlebag" or "rightsaddlebag")
            .GroupBy(entry => entry.Category!);

        foreach (var group in groups)
        {
            _queue.Enqueue(group);
        }
    }

    private void OnOpenRetainer()
    {
        var groups = Config.Settings
            .FindAll(entry => entry.Enabled && entry.Category is "retainer")
            .GroupBy(entry => entry.Category!);

        foreach (var group in groups)
        {
            _queue.Enqueue(group);
        }
    }

    private void ProcessQueue()
    {
        if (_isBusy || _queue.Count == 0)
            return;

        var nextGroup = _queue.Peek();
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
                var sorter = itemOrderModule->ArmourySorterSpan.GetPointer(i)->Value;
                if (sorter != null && sorter->SortFunctionIndex != -1)
                {
                    Debug($"ItemOrderModule: Sorter #{i} ({sorter->InventoryType}) is busy, waiting.");
                    return;
                }
            }
        }

        _isBusy = true;

        try
        {
            var group = _queue.Dequeue();

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

            var raptureShellModule = RaptureShellModule.Instance();
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
            HaselShellCommandModule.ExecuteCommand(cmd);

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
                HaselShellCommandModule.ExecuteCommand(cmd);
            }

            cmd = $"/itemsort execute {category}";
            Log($"Executing {cmd}");
            HaselShellCommandModule.ExecuteCommand(cmd);
        }
        catch (Exception ex)
        {
            Error(ex, "Unexpected error during sorting");
        }
        finally
        {
            _isBusy = false;
        }
    }
}
