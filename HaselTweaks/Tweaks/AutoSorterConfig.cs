using System.Collections.Generic;
using System.Linq;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Gui;
using HaselTweaks.Enums;
using ImGuiNET;

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
    }
}

public partial class AutoSorter
{
    private AutoSorterConfiguration Config => _pluginConfig.Tweaks.AutoSorter;

    public void OnConfigOpen() { }
    public void OnConfigClose() { }
    public void OnConfigChange(string fieldName) { }

    public void DrawConfig()
    {
        using var _ = _configGui.PushContext(this);

        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool("SortArmouryOnJobChange", ref Config.SortArmouryOnJobChange);

        ImGuiUtils.DrawPaddedSeparator();

        using var table = ImRaii.Table("##Table", 5, ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.NoPadOuterX);
        if (!table)
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
                ImGui.TextUnformatted(_textService.Translate(entry.Enabled
                    ? "AutoSorter.Config.EnableCheckmark.Tooltip.RuleIsEnabled"
                    : "AutoSorter.Config.EnableCheckmark.Tooltip.RuleIsDisabled"));
                ImGui.EndTooltip();
            }
            if (ImGui.IsItemClicked())
            {
                _pluginConfig.Save();
            }

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            preview = GetLocalizedParam(CategorySet, entry.Category) ?? _textService.Translate("AutoSorter.Config.CategoryCombo.Placeholder");

            if (ImGui.BeginCombo(key + "Category", preview))
            {
                foreach (var kv in CategorySet)
                {
                    if (ImGui.Selectable(GetLocalizedParam(kv.Value), entry.Category == kv.Key))
                    {
                        entry.Category = kv.Key;
                        _pluginConfig.Save();
                    }

                    if (entry.Category == kv.Key)
                        ImGui.SetItemDefaultFocus();
                }

                ImGui.EndCombo();
            }

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            preview = GetLocalizedParam(ConditionSet, entry.Condition) ?? _textService.Translate("AutoSorter.Config.ConditionCombo.Placeholder");

            if (ImGui.BeginCombo(key + "Condition", preview))
            {
                foreach (var kv in ConditionSet)
                {
                    if (ImGui.Selectable(GetLocalizedParam(kv.Value), entry.Condition == kv.Key))
                    {
                        entry.Condition = kv.Key;
                        _pluginConfig.Save();
                    }

                    if (entry.Condition == kv.Key)
                        ImGui.SetItemDefaultFocus();
                }

                ImGui.EndCombo();
            }

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            preview = GetLocalizedParam(OrderSet, entry.Order) ?? _textService.Translate("AutoSorter.Config.OrderCombo.Placeholder");

            if (ImGui.BeginCombo(key + "Order", preview))
            {
                foreach (var kv in OrderSet)
                {
                    if (ImGui.Selectable(GetLocalizedParam(kv.Value), entry.Order == kv.Key))
                    {
                        entry.Order = kv.Key;
                        _pluginConfig.Save();
                    }

                    if (entry.Order == kv.Key)
                        ImGui.SetItemDefaultFocus();
                }

                ImGui.EndCombo();
            }

            ImGui.TableNextColumn();
            if (i > 0)
            {
                if (ImGuiUtils.IconButton(key + "_Up", FontAwesomeIcon.ArrowUp, _textService.Translate("AutoSorter.Config.MoveRuleUpButton.Tooltip")))
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
                if (ImGuiUtils.IconButton(key + "_Down", FontAwesomeIcon.ArrowDown, _textService.Translate("AutoSorter.Config.MoveRuleDownButton.Tooltip")))
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
                if (ImGuiUtils.IconButton(key + "_Delete", FontAwesomeIcon.Trash, _textService.Translate("AutoSorter.Config.DeleteRuleButton.Tooltip.HoldingShift")))
                {
                    entryToRemove = i;
                }
            }
            else
            {
                ImGuiUtils.IconButton(
                    key + "_Delete",
                    FontAwesomeIcon.Trash,
                    _textService.Translate(isWindowFocused
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
                    ImGuiUtils.IconButton(key + "_Execute", FontAwesomeIcon.Terminal, _textService.Translate("AutoSorter.SortingInProgress"), disabled: true);
                }
                else
                {
                    List<string>? disabledReasons = null;

                    if (entry.Category is "saddlebag" or "rightsaddlebag" && !IsInventoryBuddyOpen)
                    {
                        disabledReasons ??= [];
                        disabledReasons.Add(_textService.Translate("AutoSorter.Config.ExecuteButton.Tooltip.SaddlebagNotOpen"));
                    }

                    if (entry.Category is "retainer" && !IsRetainerInventoryOpen)
                    {
                        disabledReasons ??= [];
                        disabledReasons.Add(_textService.Translate("AutoSorter.Config.ExecuteButton.Tooltip.RetainerNotOpen"));
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
                        var errors = GetErrors(entry, usedCategories);
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
                        else if (ImGuiUtils.IconButton(key + "_Execute", FontAwesomeIcon.Terminal, _textService.Translate("AutoSorter.Config.ExecuteButton.Tooltip.Ready")))
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

        if (ImGui.Button(_textService.Translate("AutoSorter.Config.AddButton.Label")))
        {
            Config.Settings.Add(new());
            _pluginConfig.Save();
        }

        if (Status == TweakStatus.Enabled)
        {
            ImGui.SameLine();

            if (!_isBusy && _queue.Count == 0)
            {
                if (ImGui.Button(_textService.Translate("AutoSorter.Config.RunAllButton.Label")))
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
                    ImGui.Button(_textService.Translate("AutoSorter.Config.RunAllButton.Label"));

                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.TextUnformatted(_textService.Translate("AutoSorter.SortingInProgress"));
                    ImGui.EndTooltip();
                }
            }
        }

        if (entryToMoveUp != -1)
        {
            var removedItem = Config.Settings[entryToMoveUp];
            Config.Settings.RemoveAt(entryToMoveUp);
            Config.Settings.Insert(entryToMoveUp - 1, removedItem);
            _pluginConfig.Save();
        }

        if (entryToMoveDown != -1)
        {
            var removedItem = Config.Settings[entryToMoveDown];
            Config.Settings.RemoveAt(entryToMoveDown);
            Config.Settings.Insert(entryToMoveDown + 1, removedItem);
            _pluginConfig.Save();
        }

        if (entryToRemove != -1)
        {
            Config.Settings.RemoveAt(entryToRemove);
            _pluginConfig.Save();
        }

        if (entryToExecute != -1)
        {
            var entry = Config.Settings[entryToExecute];
            _queue.Enqueue(new[] { entry }.GroupBy(entry => entry.Category!).First());
        }
    }

    public List<string>? GetErrors(AutoSorterConfiguration.SortingRule rule, HashSet<string>? usedCategories)
    {
        List<string>? errors = null;

        if (string.IsNullOrEmpty(rule.Category))
        {
            errors ??= [];
            errors.Add(_textService.Translate("AutoSorter.SortingRule.Error.CategoryNotSet"));
        }

        if (string.IsNullOrEmpty(rule.Condition))
        {
            errors ??= [];
            errors.Add(_textService.Translate("AutoSorter.SortingRule.Error.ConditionNotSet"));
        }

        if (string.IsNullOrEmpty(rule.Order))
        {
            errors ??= [];
            errors.Add(_textService.Translate("AutoSorter.SortingRule.Error.OrderNotSet"));
        }

        if (rule.Category == "armoury" && usedCategories != null && ArmourySubcategories.Any(usedCategories.Contains))
        {
            errors ??= [];
            errors.Add(_textService.Translate("AutoSorter.SortingRule.Error.OverridesArmouryRule"));
        }

        unsafe
        {
            if (rule.Category is "rightsaddlebag" && !PlayerState.Instance()->HasPremiumSaddlebag)
            {
                errors ??= [];
                errors.Add(_textService.Translate("AutoSorter.SortingRule.Error.PremiumSaddlebagNotSubscribed"));
            }
        }

        return errors;
    }
}
