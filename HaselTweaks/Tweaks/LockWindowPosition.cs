using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Utils;
using ImGuiNET;

namespace HaselTweaks.Tweaks;

public class LockWindowPositionConfiguration
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

[Tweak]
public unsafe partial class LockWindowPosition : Tweak<LockWindowPositionConfiguration>
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

    private nint WindowContextMenuHandler_ReceiveEventAddress => (nint)RaptureAtkUnitManager.Instance()->WindowContextMenuHandler.vtbl[0];

    public override void Enable()
    {
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "GearSetList", GearSetList_PostSetup);
    }

    public override void Disable()
    {
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "GearSetList", GearSetList_PostSetup);
    }

    public override void DrawConfig()
    {
        ImGuiUtils.DrawSection(t("HaselTweaks.Config.SectionTitle.Configuration"));

        ImGui.Checkbox(t("LockWindowPosition.Config.Inverted.Label"), ref Config.Inverted);
        if (ImGui.IsItemClicked())
        {
            Service.GetService<Configuration>().Save();
        }

        ImGui.Checkbox(t("LockWindowPosition.Config.AddLockUnlockContextMenuEntries.Label"), ref Config.AddLockUnlockContextMenuEntries);
        if (ImGui.IsItemClicked())
        {
            Service.GetService<Configuration>().Save();
        }

        var isWindowFocused = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows);

        ImGuiUtils.DrawPaddedSeparator();
        if (Config.LockedWindows.Any())
        {
            ImGui.TextUnformatted(t("LockWindowPosition.Config.Windows.Title"));

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

                    ImGui.SetTooltip(t(isLocked
                        ? "LockWindowPosition.Config.EnableCheckmark.Tooltip.Locked"
                        : "LockWindowPosition.Config.EnableCheckmark.Tooltip.Unlocked"));
                }
                if (ImGui.IsItemClicked())
                {
                    Service.GetService<Configuration>().Save();
                }

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(entry.Name);

                ImGui.TableNextColumn();
                if (isWindowFocused && (ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift)))
                {
                    if (ImGuiUtils.IconButton(key + "_Delete", FontAwesomeIcon.Trash, t("LockWindowPosition.Config.DeleteButton.Tooltip")))
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
                Service.GetService<Configuration>().Save();
            }
        }
        else
        {
            ImGuiUtils.TextUnformattedDisabled(t("LockWindowPosition.Config.NoWindowsAddedYet"));
            ImGuiUtils.PushCursorY(4);
        }

        if (_showPicker)
        {
            if (ImGui.Button(t("LockWindowPosition.Config.Picker.CancelButton.Label")))
            {
                _showPicker = false;
            }
        }
        else
        {
            if (ImGui.Button(t("LockWindowPosition.Config.Picker.PickWindowButton.Label")))
            {
                _hoveredWindowName = "";
                _hoveredWindowPos = default;
                _hoveredWindowSize = default;
                _showPicker = true;
            }
        }

        if (Config.LockedWindows.Any())
        {
            ImGui.SameLine();

            if (ImGui.Button(t("LockWindowPosition.Config.Picker.ToggleAllButton.Label")))
            {
                foreach (var entry in Config.LockedWindows)
                {
                    entry.Enabled = !entry.Enabled;
                }
                Service.GetService<Configuration>().Save();
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
                        Service.GetService<Configuration>().Save();
                    }
                }

                ImGui.End();
            }
        }
    }

    public override void OnConfigWindowClose()
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
            addon->ResetPosition = false;
    }

    [AddressHook<AtkUnitBase>(nameof(AtkUnitBase.Addresses.MoveDelta))]
    public bool MoveDelta(AtkUnitBase* atkUnitBase, nint xDelta, nint yDelta)
    {
        if (atkUnitBase != null)
        {
            var name = MemoryHelper.ReadString((nint)atkUnitBase->Name, 0x20);
            var isLocked = Config.LockedWindows.Any(entry => entry.Enabled && entry.Name == name);

            if (Config.Inverted)
                isLocked = !isLocked;

            if (isLocked)
                return false;
        }

        return MoveDeltaHook.OriginalDisposeSafe(atkUnitBase, xDelta, yDelta);
    }

    [VTableHook<RaptureAtkUnitManager>(6)]
    public bool RaptureAtkUnitManager_Vf6(RaptureAtkUnitManager* self, nint a2)
    {
        if (_showPicker)
        {
            if (a2 != 0)
            {
                var atkUnitBase = *(AtkUnitBase**)(a2 + 8);
                if (atkUnitBase != null && atkUnitBase->WindowNode != null && atkUnitBase->WindowCollisionNode != null)
                {
                    var name = MemoryHelper.ReadString((nint)atkUnitBase->Name, 0x20);
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

        return RaptureAtkUnitManager_Vf6Hook.OriginalDisposeSafe(self, a2);
    }

    [AddressHook<AgentContext>(nameof(AgentContext.Addresses.ClearMenu))]
    public nint AgentContext_ClearMenu(AgentContext* agent)
    {
        if (_eventIndexToDisable != 0)
            _eventIndexToDisable = 0;

        return AgentContext_ClearMenuHook.OriginalDisposeSafe(agent);
    }

    [AddressHook<AgentContext>(nameof(AgentContext.Addresses.AddMenuItem2))]
    public nint AgentContext_AddMenuItem2(AgentContext* agent, uint addonRowId, nint handlerPtr, long handlerParam, int disabled, int submenu)
    {
        if (addonRowId == 8660 && agent->ContextMenuIndex == 0) // "Return to Default Position"
        {
            _eventIndexToDisable = agent->CurrentContextMenu->CurrentEventIndex;
        }

        return AgentContext_AddMenuItem2Hook.OriginalDisposeSafe(agent, addonRowId, handlerPtr, handlerParam, disabled, submenu);
    }

    [AddressHook<AgentContext>(nameof(AgentContext.Addresses.OpenContextMenuForAddon))]
    public nint AgentContext_OpenContextMenuForAddon(AgentContext* agent, uint addonId, bool bindToOwner)
    {
        if (_eventIndexToDisable == 7 && agent->ContextMenuIndex == 0)
        {
            var addon = GetAddon<AtkUnitBase>((ushort)addonId);
            if (addon != null)
            {
                var name = MemoryHelper.ReadString((nint)addon->Name, 0x20);

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
                            AddMenuEntry(t("LockWindowPosition.UnlockPosition"), EventParamUnlock);
                        }
                    }
                    else
                    {
                        if (Config.AddLockUnlockContextMenuEntries)
                        {
                            AddMenuEntry(t("LockWindowPosition.LockPosition"), EventParamLock);
                        }
                    }
                }
            }
        }

        return AgentContext_OpenContextMenuForAddonHook.OriginalDisposeSafe(agent, addonId, bindToOwner);
    }

    [AddressHook(nameof(WindowContextMenuHandler_ReceiveEventAddress))]
    public AtkValue* WindowContextMenuHandler_ReceiveEvent(nint self, AtkValue* result, nint a3, long a4, long eventParam)
    {
        if (_eventIndexToDisable == 7 && eventParam is EventParamUnlock or EventParamLock)
        {
            if (TryGetAddon<AtkUnitBase>((ushort)GetAgent<AgentContext>()->OwnerAddon, out var addon))
            {
                var name = MemoryHelper.ReadString((nint)addon->Name, 0x20);

                var entry = Config.LockedWindows.FirstOrDefault(entry => entry?.Name == name, null);
                var isLocked = eventParam == EventParamLock;

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

                Service.GetService<Configuration>().Save();
            }

            _eventIndexToDisable = 0;

            result->Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Bool;
            result->Byte = 0;
            return result;
        }

        if (_eventIndexToDisable != 0)
            _eventIndexToDisable = 0;

        return WindowContextMenuHandler_ReceiveEventHook!.OriginalDisposeSafe(self, result, a3, a4, eventParam);
    }

    private void AddMenuEntry(string text, int eventParam)
    {
        var bytes = new SeStringBuilder()
            .AddUiForeground("\uE078 ", 32)
            .AddText(text)
            .Encode();

        var handler = (nint)AtkStage.GetSingleton()->RaptureAtkUnitManager + 0x9C90; // see vtbl ptr in ctor
        fixed (byte* ptr = &bytes[0])
        {
            GetAgent<AgentContext>()->AddMenuItem(ptr, (void*)handler, eventParam);
        }
    }
}
