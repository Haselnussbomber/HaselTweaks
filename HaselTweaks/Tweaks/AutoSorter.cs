using System.Collections.Generic;
using System.Linq;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Client.UI.Shell;
using HaselCommon.Services;
using HaselCommon.Utils;
using HaselTweaks.Config;
using HaselTweaks.Enums;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Microsoft.Extensions.Logging;

namespace HaselTweaks.Tweaks;

public sealed class AutoSorterConfiguration
{
    public List<SortingRule> Settings = [];
    public bool SortArmouryOnJobChange = true;

    public record SortingRule
    {
        public bool Enabled = true;
        public string? Category = null;
        public string? Condition = null;
        public string? Order = null;

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

public sealed unsafe class AutoSorter(
    ILogger<AutoSorter> Logger,
    PluginConfig PluginConfig,
    TranslationManager TranslationManager,
    IClientState ClientState,
    IFramework Framework,
    AddonObserver AddonObserver)
    : Tweak<AutoSorterConfiguration>(PluginConfig, TranslationManager)
{
    private static readonly Dictionary<string, uint> CategorySet = new()
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

    private static readonly Dictionary<string, uint> ConditionSet = new()
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

    private static readonly Dictionary<string, uint> OrderSet = new()
    {
        ["asc"] = 281,
        ["des"] = 283
    };

    internal static readonly List<string> ArmourySubcategories =
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

    private static string GetLocalizedParam(uint rowId, string? fallback = null)
    {
        var param = GetSheetText<TextCommandParam>(rowId, "Param");
        return string.IsNullOrEmpty(param) ? fallback ?? "" : param.ToLower();
    }

    private static string? GetLocalizedParam(Dictionary<string, uint> dict, string? key, string? fallback = null)
    {
        var str = fallback ?? key;

        if (!string.IsNullOrEmpty(key) && dict.TryGetValue(key, out var rowId))
        {
            var param = GetSheetText<TextCommandParam>(rowId, "Param");

            if (!string.IsNullOrEmpty(param))
            {
                str = param.ToLower();
            }
        }

        return str;
    }

    private readonly Queue<IGrouping<string, AutoSorterConfiguration.SortingRule>> _queue = new();
    private bool _isBusy = false;
    private byte _lastClassJobId = 0;

    private static bool IsRetainerInventoryOpen => IsAddonOpen("InventoryRetainer") || IsAddonOpen("InventoryRetainerLarge");
    private static bool IsInventoryBuddyOpen => IsAddonOpen("InventoryBuddy");

    public override void OnEnable()
    {
        _queue.Clear();

        ClientState.Login += OnLogin;
        ClientState.Logout += OnLogout;
        Framework.Update += OnFrameworkUpdate;
        AddonObserver.AddonOpen += OnAddonOpen;
    }

    public override void OnDisable()
    {
        _queue.Clear();

        ClientState.Login -= OnLogin;
        ClientState.Logout -= OnLogout;
        Framework.Update -= OnFrameworkUpdate;
        AddonObserver.AddonOpen -= OnAddonOpen;
    }

    private void OnLogin()
    {
        _lastClassJobId = (byte)(ClientState.LocalPlayer?.ClassJob.Id ?? 0);
        _queue.Clear();
    }

    private void OnLogout()
    {
        _lastClassJobId = 0;
        _queue.Clear();
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (!ClientState.IsLoggedIn)
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

    private void OnAddonOpen(string addonName)
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
                Logger.LogDebug("ItemOrderModule is busy, waiting.");
                return;
            }

            for (var i = 0; i < itemOrderModule->ArmourySorter.Length; i++)
            {
                var sorter = itemOrderModule->ArmourySorter.GetPointer(i)->Value;
                if (sorter != null && sorter->SortFunctionIndex != -1)
                {
                    Logger.LogDebug("ItemOrderModule: Sorter #{i} ({type}) is busy, waiting.", i, sorter->InventoryType.ToString());
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

            Logger.LogInformation("Sorting Category: {key}", key);

            var category = GetLocalizedParam(CategorySet, key);
            if (string.IsNullOrEmpty(category))
            {
                Logger.LogError("Can not localize category: GetLocalizedParam returned \"{category}\".", category);
                return;
            }

            var raptureShellModule = RaptureShellModule.Instance();
            if (raptureShellModule == null)
            {
                Logger.LogWarning("Could not resolve RaptureShellModule");
                return;
            }

            if (raptureShellModule->IsTextCommandUnavailable)
            {
                Logger.LogWarning("Text commands are unavailable, skipping.");
                return;
            }

            if ((key is "saddlebag" or "rightsaddlebag") && !IsInventoryBuddyOpen)
            {
                Logger.LogWarning("Sorting for saddlebag/rightsaddlebag only works when the window is open, skipping.");
                return;
            }

            var playerState = PlayerState.Instance();
            if (playerState == null)
            {
                Logger.LogWarning("Could not resolve PlayerState");
                return;
            }

            if (key is "rightsaddlebag" && !playerState->HasPremiumSaddlebag)
            {
                Logger.LogWarning("Not subscribed to the Companion Premium Service, skipping.");
                return;
            }

            if (key is "retainer" && !IsRetainerInventoryOpen)
            {
                Logger.LogWarning("Sorting for retainer only works when the window is open, skipping.");
                return;
            }

            var cmd = $"/itemsort clear {category}";
            Logger.LogInformation("Executing {cmd}", cmd);
            ExecuteCommand(cmd);

            foreach (var rule in group)
            {
                var condition = GetLocalizedParam(ConditionSet, rule.Condition);
                if (string.IsNullOrEmpty(condition))
                {
                    Logger.LogError("Can not localize condition \"{ruleCondition}\", skipping. (GetLocalizedParam returned \"{condition}\")", rule.Condition, condition);
                    continue;
                }

                var order = GetLocalizedParam(OrderSet, rule.Order);
                if (string.IsNullOrEmpty(order))
                {
                    Logger.LogError("Can not localize order \"{ruleOrder}\", skipping. (GetLocalizedParam returned \"{order}\")", rule.Order, order);
                    continue;
                }

                cmd = $"/itemsort condition {category} {condition} {order}";
                Logger.LogInformation("Executing {cmd}", cmd);
                ExecuteCommand(cmd);
            }

            cmd = $"/itemsort execute {category}";
            Logger.LogInformation("Executing {cmd}", cmd);
            ExecuteCommand(cmd);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error during sorting");
        }
        finally
        {
            _isBusy = false;
        }
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
            (Status == TweakStatus.Enabled ? ItemInnerSpacing.X + TerminalButtonSize.X : 0));

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
                ImGui.BeginTooltip();
                ImGui.TextUnformatted(t(entry.Enabled
                    ? "AutoSorter.Config.EnableCheckmark.Tooltip.RuleIsEnabled"
                    : "AutoSorter.Config.EnableCheckmark.Tooltip.RuleIsDisabled"));
                ImGui.EndTooltip();
            }
            if (ImGui.IsItemClicked())
            {
                PluginConfig.Save();
            }

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            preview = GetLocalizedParam(CategorySet, entry.Category) ?? t("AutoSorter.Config.CategoryCombo.Placeholder");

            if (ImGui.BeginCombo(key + "Category", preview))
            {
                foreach (var kv in CategorySet)
                {
                    if (ImGui.Selectable(GetLocalizedParam(kv.Value), entry.Category == kv.Key))
                    {
                        entry.Category = kv.Key;
                        PluginConfig.Save();
                    }

                    if (entry.Category == kv.Key)
                        ImGui.SetItemDefaultFocus();
                }

                ImGui.EndCombo();
            }

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            preview = GetLocalizedParam(ConditionSet, entry.Condition) ?? t("AutoSorter.Config.ConditionCombo.Placeholder");

            if (ImGui.BeginCombo(key + "Condition", preview))
            {
                foreach (var kv in ConditionSet)
                {
                    if (ImGui.Selectable(GetLocalizedParam(kv.Value), entry.Condition == kv.Key))
                    {
                        entry.Condition = kv.Key;
                        PluginConfig.Save();
                    }

                    if (entry.Condition == kv.Key)
                        ImGui.SetItemDefaultFocus();
                }

                ImGui.EndCombo();
            }

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            preview = GetLocalizedParam(OrderSet, entry.Order) ?? t("AutoSorter.Config.OrderCombo.Placeholder");

            if (ImGui.BeginCombo(key + "Order", preview))
            {
                foreach (var kv in OrderSet)
                {
                    if (ImGui.Selectable(GetLocalizedParam(kv.Value), entry.Order == kv.Key))
                    {
                        entry.Order = kv.Key;
                        PluginConfig.Save();
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

            if (Status == TweakStatus.Enabled)
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
            PluginConfig.Save();
        }

        if (Status == TweakStatus.Enabled)
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
                {
                    ImGui.BeginTooltip();
                    ImGui.TextUnformatted(t("AutoSorter.SortingInProgress"));
                    ImGui.EndTooltip();
                }
            }
        }

        if (entryToMoveUp != -1)
        {
            var removedItem = Config.Settings[entryToMoveUp];
            Config.Settings.RemoveAt(entryToMoveUp);
            Config.Settings.Insert(entryToMoveUp - 1, removedItem);
            PluginConfig.Save();
        }

        if (entryToMoveDown != -1)
        {
            var removedItem = Config.Settings[entryToMoveDown];
            Config.Settings.RemoveAt(entryToMoveDown);
            Config.Settings.Insert(entryToMoveDown + 1, removedItem);
            PluginConfig.Save();
        }

        if (entryToRemove != -1)
        {
            Config.Settings.RemoveAt(entryToRemove);
            PluginConfig.Save();
        }

        if (entryToExecute != -1)
        {
            var entry = Config.Settings[entryToExecute];
            _queue.Enqueue(new[] { entry }.GroupBy(entry => entry.Category!).First());
        }
    }
}
