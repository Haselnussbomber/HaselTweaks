using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Extensions;
using HaselCommon.Services;
using HaselCommon.Utils;
using HaselTweaks.Config;
using ImGuiNET;
using AtkEventInterface = FFXIVClientStructs.FFXIV.Component.GUI.AtkModuleInterface.AtkEventInterface;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace HaselTweaks.Tweaks;

public sealed class LockWindowPositionConfiguration
{
    public bool Inverted = false;
    public bool AddLockUnlockContextMenuEntries = true;
    public List<LockedWindowSetting> LockedWindows = [];

    public record LockedWindowSetting
    {
        public bool Enabled = true;
        public string Name = "";
    }
}

public sealed unsafe class LockWindowPosition(
    PluginConfig pluginConfig,
    TextService textService,
    IGameInteropProvider GameInteropProvider,
    IAddonLifecycle AddonLifecycle)
    : Tweak<LockWindowPositionConfiguration>(pluginConfig, textService)
{
    private const int EventParamLock = 9901;
    private const int EventParamUnlock = 9902;
    private static readonly string[] IgnoredAddons = [
        "CharaCardEditMenu", // always opens docked to CharaCard (OnSetup)
    ];

    private bool _showPicker = false;
    private string _hoveredWindowName = "";
    private Vector2 _hoveredWindowPos;
    private Vector2 _hoveredWindowSize;
    private int _eventIndexToDisable = 0;

    private delegate bool RaptureAtkUnitManagerVf6Delegate(RaptureAtkUnitManager* self, nint a2);

    private Hook<AtkUnitBase.Delegates.MoveDelta>? MoveDeltaHook;
    private Hook<RaptureAtkUnitManagerVf6Delegate>? RaptureAtkUnitManagerVf6Hook;
    private Hook<AgentContext.Delegates.ClearMenu>? ClearMenuHook;
    private Hook<AgentContext.Delegates.AddMenuItem2>? AddMenuItem2Hook;
    private Hook<AgentContext.Delegates.OpenContextMenuForAddon>? OpenContextMenuForAddonHook;
    private Hook<AtkEventInterface.Delegates.ReceiveEvent>? WindowContextMenuHandlerReceiveEventHook;

    public override void OnInitialize()
    {
        MoveDeltaHook = GameInteropProvider.HookFromAddress<AtkUnitBase.Delegates.MoveDelta>(
            AtkUnitBase.MemberFunctionPointers.MoveDelta,
            MoveDeltaDetour);

        RaptureAtkUnitManagerVf6Hook = GameInteropProvider.HookFromVTable<RaptureAtkUnitManagerVf6Delegate>(
            RaptureAtkUnitManager.StaticVirtualTablePointer, 6,
            RaptureAtkUnitManagerVf6Detour);

        ClearMenuHook = GameInteropProvider.HookFromAddress<AgentContext.Delegates.ClearMenu>(
            AgentContext.MemberFunctionPointers.ClearMenu,
            ClearMenuDetour);

        AddMenuItem2Hook = GameInteropProvider.HookFromAddress<AgentContext.Delegates.AddMenuItem2>(
            AgentContext.MemberFunctionPointers.AddMenuItem2,
            AddMenuItem2Detour);

        OpenContextMenuForAddonHook = GameInteropProvider.HookFromAddress<AgentContext.Delegates.OpenContextMenuForAddon>(
            AgentContext.MemberFunctionPointers.OpenContextMenuForAddon,
            OpenContextMenuForAddonDetour);

        WindowContextMenuHandlerReceiveEventHook = GameInteropProvider.HookFromAddress<AtkEventInterface.Delegates.ReceiveEvent>(
            RaptureAtkUnitManager.Instance()->WindowContextMenuHandler.VirtualTable->ReceiveEvent,
            WindowContextMenuHandlerReceiveEventDetour);
    }

    public override void OnEnable()
    {
        AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "GearSetList", GearSetList_PostSetup);

        MoveDeltaHook?.Enable();
        RaptureAtkUnitManagerVf6Hook?.Enable();
        ClearMenuHook?.Enable();
        AddMenuItem2Hook?.Enable();
        OpenContextMenuForAddonHook?.Enable();
        WindowContextMenuHandlerReceiveEventHook?.Enable();
    }

    public override void OnDisable()
    {
        AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "GearSetList", GearSetList_PostSetup);

        MoveDeltaHook?.Disable();
        RaptureAtkUnitManagerVf6Hook?.Disable();
        ClearMenuHook?.Disable();
        AddMenuItem2Hook?.Disable();
        OpenContextMenuForAddonHook?.Disable();
        WindowContextMenuHandlerReceiveEventHook?.Disable();
    }

    public override void DrawConfig()
    {
        ImGuiUtils.DrawSection(TextService.Translate("HaselTweaks.Config.SectionTitle.Configuration"));

        ImGui.Checkbox(TextService.Translate("LockWindowPosition.Config.Inverted.Label"), ref Config.Inverted);
        if (ImGui.IsItemClicked())
        {
            PluginConfig.Save();
        }

        ImGui.Checkbox(TextService.Translate("LockWindowPosition.Config.AddLockUnlockContextMenuEntries.Label"), ref Config.AddLockUnlockContextMenuEntries);
        if (ImGui.IsItemClicked())
        {
            PluginConfig.Save();
        }

        var isWindowFocused = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows);

        ImGuiUtils.DrawPaddedSeparator();
        if (Config.LockedWindows.Count != 0)
        {
            TextService.Draw("LockWindowPosition.Config.Windows.Title");

            if (!ImGui.BeginTable("##Table", 3, ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.NoPadOuterX))
            {
                return;
            }

            ImGui.TableSetupColumn("Enabled", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Action", ImGuiTableColumnFlags.WidthFixed, ImGuiUtils.GetIconButtonSize(FontAwesomeIcon.Trash).X);

            var entryToRemove = -1;
            var i = 0;

            foreach (var entry in Config.LockedWindows)
            {
                var key = $"##Table_Row{i}";
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGui.Checkbox(key + "_Enabled", ref entry.Enabled);
                if (ImGui.IsItemHovered())
                {
                    var isLocked = entry.Enabled;

                    if (Config.Inverted)
                        isLocked = !isLocked;

                    ImGui.BeginTooltip();
                    TextService.Draw(isLocked
                        ? "LockWindowPosition.Config.EnableCheckmark.Tooltip.Locked"
                        : "LockWindowPosition.Config.EnableCheckmark.Tooltip.Unlocked");
                    ImGui.EndTooltip();
                }
                if (ImGui.IsItemClicked())
                {
                    PluginConfig.Save();
                }

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(entry.Name);

                ImGui.TableNextColumn();
                if (isWindowFocused && (ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift)))
                {
                    if (ImGuiUtils.IconButton(key + "_Delete", FontAwesomeIcon.Trash, TextService.Translate("LockWindowPosition.Config.DeleteButton.Tooltip")))
                    {
                        entryToRemove = i;
                    }
                }
                else
                {
                    ImGuiUtils.IconButton(
                        key + "_Delete",
                        FontAwesomeIcon.Trash,
                        TextService.Translate(isWindowFocused
                            ? "LockWindowPosition.Config.DeleteButton.Tooltip.NotHoldingShift"
                            : "LockWindowPosition.Config.DeleteButton.Tooltip.WindowNotFocused"),
                        disabled: true);
                }

                i++;
            }

            ImGui.EndTable();

            if (entryToRemove != -1)
            {
                Config.LockedWindows.RemoveAt(entryToRemove);
                PluginConfig.Save();
            }
        }
        else
        {
            using (ImRaii.Disabled())
                TextService.Draw("LockWindowPosition.Config.NoWindowsAddedYet");
            ImGuiUtils.PushCursorY(4);
        }

        if (_showPicker)
        {
            if (ImGui.Button(TextService.Translate("LockWindowPosition.Config.Picker.CancelButton.Label")))
            {
                _showPicker = false;
            }
        }
        else
        {
            if (ImGui.Button(TextService.Translate("LockWindowPosition.Config.Picker.PickWindowButton.Label")))
            {
                _hoveredWindowName = "";
                _hoveredWindowPos = default;
                _hoveredWindowSize = default;
                _showPicker = true;
            }
        }

        if (Config.LockedWindows.Count != 0)
        {
            ImGui.SameLine();

            if (ImGui.Button(TextService.Translate("LockWindowPosition.Config.Picker.ToggleAllButton.Label")))
            {
                foreach (var entry in Config.LockedWindows)
                {
                    entry.Enabled = !entry.Enabled;
                }
                PluginConfig.Save();
            }
        }

        if (_showPicker && _hoveredWindowPos != default)
        {
            ImGui.SetNextWindowPos(_hoveredWindowPos);
            ImGui.SetNextWindowSize(_hoveredWindowSize);

            using var windowStyles = ImRaii.PushStyle(ImGuiStyleVar.WindowBorderSize, 1.0f);
            using var windowColors = Colors.Gold.Push(ImGuiCol.Border)
                                                .Push(ImGuiCol.WindowBg, new Vector4(0.847f, 0.733f, 0.49f, 0.33f));

            if (ImGui.Begin("Lock Windows Picker", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize))
            {
                var drawList = ImGui.GetForegroundDrawList();
                var textPos = _hoveredWindowPos + new Vector2(0, -ImGui.GetTextLineHeight());
                drawList.AddText(textPos + Vector2.One, Colors.Black, _hoveredWindowName);
                drawList.AddText(textPos, Colors.Gold, _hoveredWindowName);

                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    _showPicker = false;

                    if (_hoveredWindowName != "" && !Config.LockedWindows.Any(entry => entry.Name == _hoveredWindowName))
                    {
                        Config.LockedWindows.Add(new()
                        {
                            Name = _hoveredWindowName
                        });
                        PluginConfig.Save();
                    }
                }

                ImGui.End();
            }
        }
    }

    public override void OnConfigClose()
    {
        _hoveredWindowName = "";
        _hoveredWindowPos = default;
        _hoveredWindowSize = default;
        _showPicker = false;
    }

    // block GearSetList from moving when opened by Character
    private void GearSetList_PostSetup(AddonEvent type, AddonArgs args)
    {
        var addon = (AddonGearSetList*)args.Addon;

        var isLocked = Config.LockedWindows.Any(entry => entry.Enabled && entry.Name == "GearSetList");

        if (Config.Inverted)
            isLocked = !isLocked;

        if (isLocked)
            addon->ShouldResetPosition = false;
    }

    private bool MoveDeltaDetour(AtkUnitBase* atkUnitBase, short* xDelta, short* yDelta)
    {
        if (atkUnitBase != null)
        {
            var name = atkUnitBase->NameString;
            var isLocked = Config.LockedWindows.Any(entry => entry.Enabled && entry.Name == name);

            if (Config.Inverted)
                isLocked = !isLocked;

            if (isLocked)
                return false;
        }

        return MoveDeltaHook!.Original(atkUnitBase, xDelta, yDelta);
    }

    private bool RaptureAtkUnitManagerVf6Detour(RaptureAtkUnitManager* self, nint a2)
    {
        if (_showPicker)
        {
            if (a2 != 0)
            {
                var atkUnitBase = *(AtkUnitBase**)(a2 + 8);
                if (atkUnitBase != null && atkUnitBase->WindowNode != null && atkUnitBase->WindowCollisionNode != null)
                {
                    var name = atkUnitBase->NameString;
                    if (!IgnoredAddons.Contains(name))
                    {
                        _hoveredWindowName = name;
                        _hoveredWindowPos = new(atkUnitBase->X, atkUnitBase->Y);
                        _hoveredWindowSize = new(atkUnitBase->WindowNode->AtkResNode.Width, atkUnitBase->WindowNode->AtkResNode.Height - 7);
                    }
                    else
                    {
                        _hoveredWindowName = "";
                        _hoveredWindowPos = default;
                        _hoveredWindowSize = default;
                    }
                }
                else
                {
                    _hoveredWindowName = "";
                    _hoveredWindowPos = default;
                    _hoveredWindowSize = default;
                }
            }
            else
            {
                _showPicker = false;
            }

            return false;
        }

        return RaptureAtkUnitManagerVf6Hook!.Original(self, a2);
    }

    private void ClearMenuDetour(AgentContext* agent)
    {
        if (_eventIndexToDisable != 0)
            _eventIndexToDisable = 0;

        ClearMenuHook!.Original(agent);
    }

    private void AddMenuItem2Detour(AgentContext* agent, uint addonRowId, AtkEventInterface* handlerPtr, long handlerParam, bool disabled, bool submenu)
    {
        if (addonRowId == 8660 && agent->ContextMenuIndex == 0) // "Return to Default Position"
        {
            _eventIndexToDisable = agent->CurrentContextMenu->CurrentEventIndex;
        }

        AddMenuItem2Hook!.Original(agent, addonRowId, handlerPtr, handlerParam, disabled, submenu);
    }

    private void OpenContextMenuForAddonDetour(AgentContext* agent, uint ownerAddonId, bool bindToOwner)
    {
        if (_eventIndexToDisable == 7 && agent->ContextMenuIndex == 0)
        {
            var addon = GetAddon<AtkUnitBase>((ushort)ownerAddonId);
            if (addon != null)
            {
                var name = addon->NameString;

                if (!IgnoredAddons.Contains(name))
                {
                    var isLocked = Config.LockedWindows.Any(entry => entry.Enabled && entry.Name == name);

                    if (Config.Inverted)
                        isLocked = !isLocked;

                    if (isLocked)
                    {
                        agent->CurrentContextMenu->ContextItemDisabledMask |= 1; // keeping it simple. disables "Return to Default Position"

                        if (Config.AddLockUnlockContextMenuEntries)
                        {
                            AddMenuEntry(TextService.Translate("LockWindowPosition.UnlockPosition"), EventParamUnlock);
                        }
                    }
                    else
                    {
                        if (Config.AddLockUnlockContextMenuEntries)
                        {
                            AddMenuEntry(TextService.Translate("LockWindowPosition.LockPosition"), EventParamLock);
                        }
                    }
                }
            }
        }

        OpenContextMenuForAddonHook!.Original(agent, ownerAddonId, bindToOwner);
    }

    private AtkValue* WindowContextMenuHandlerReceiveEventDetour(AtkEventInterface* self, AtkValue* returnValue, AtkValue* values, uint valueCount, ulong eventKind)
    {
        if (_eventIndexToDisable == 7 && (int)eventKind is EventParamUnlock or EventParamLock)
        {
            if (TryGetAddon<AtkUnitBase>((ushort)AgentContext.Instance()->OwnerAddon, out var addon))
            {
                var name = addon->NameString;
                var entry = Config.LockedWindows.FirstOrDefault(entry => entry?.Name == name, null);
                var isLocked = eventKind == EventParamLock;

                if (Config.Inverted)
                    isLocked = !isLocked;

                if (entry != null)
                {
                    entry.Enabled = isLocked;
                }
                else
                {
                    Config.LockedWindows.Add(new()
                    {
                        Enabled = isLocked,
                        Name = name,
                    });
                }

                PluginConfig.Save();
            }

            _eventIndexToDisable = 0;

            returnValue->Type = ValueType.Bool;
            returnValue->Byte = 0;
            return returnValue;
        }

        if (_eventIndexToDisable != 0)
            _eventIndexToDisable = 0;

        return WindowContextMenuHandlerReceiveEventHook!.Original(self, returnValue, values, valueCount, eventKind);
    }

    private void AddMenuEntry(string text, int eventParam)
    {
        var label = new SeStringBuilder()
            .AddUiForeground("\uE078 ", 32)
            .AddText(text)
            .Encode();

        AgentContext.Instance()->AddMenuItem(
            label,
            (AtkEventInterface*)Unsafe.AsPointer(ref AtkStage.Instance()->RaptureAtkUnitManager->WindowContextMenuHandler),
            eventParam);
    }
}
