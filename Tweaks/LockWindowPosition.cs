using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface;
using Dalamud.Interface.Raii;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Enums;
using HaselTweaks.Structs.Addons;
using HaselTweaks.Utils;
using ImGuiNET;

namespace HaselTweaks.Tweaks;

[Tweak(
    Name: "Lock Window Position",
    Description: "Lock window positions so they can't move.",
    HasCustomConfig: true
)]
public unsafe partial class LockWindowPosition : Tweak
{
    public static Configuration Config => Plugin.Config.Tweaks.LockWindowPosition;

    public record LockedWindowSetting
    {
        public bool Enabled = true;
        public string Name = "";
    }

    public class Configuration
    {
        public bool Inverted = false;
        public bool AddLockUnlockContextMenuEntries = true;
        public List<LockedWindowSetting> LockedWindows = new();
    }

    private const int EventParamLock = 9901;
    private const int EventParamUnlock = 9902;
    private static readonly string[] IgnoredAddons = new[] {
        "CharaCardEditMenu", // always opens docked to CharaCard (OnSetup)
    };

    private bool _showPicker = false;
    private string _hoveredWindowName = "";
    private Vector2 _hoveredWindowPos;
    private Vector2 _hoveredWindowSize;
    private int _eventIndexToDisable = 0;

    public override void DrawCustomConfig()
    {
        ImGui.Checkbox("Invert logic (locks all windows)##HaselTweaks_LockWindows_Inverted", ref Config.Inverted);
        if (ImGui.IsItemClicked())
        {
            Plugin.Config.Save();
        }

        ImGui.Checkbox("Add Lock/Unlock Position to windows context menu##HaselTweaks_LockWindows_AddLockUnlockContextMenuEntries", ref Config.AddLockUnlockContextMenuEntries);
        if (ImGui.IsItemClicked())
        {
            Plugin.Config.Save();
        }

        var isWindowFocused = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows);

        ImGuiUtils.DrawPaddedSeparator();
        if (Config.LockedWindows.Any())
        {
            ImGui.TextUnformatted("Windows:");

            if (!ImGui.BeginTable("##HaselTweaks_LockWindowsTable", 3, ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.NoPadOuterX))
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
                var key = $"##HaselTweaks_LockWindowsTable_{i}";
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGui.Checkbox(key + "_Enabled", ref entry.Enabled);
                if (ImGui.IsItemHovered())
                {
                    var isLocked = entry.Enabled;

                    if (Config.Inverted)
                        isLocked = !isLocked;

                    ImGui.SetTooltip("Window is " + (isLocked ? "locked" : "unlocked"));
                }
                if (ImGui.IsItemClicked())
                {
                    Plugin.Config.Save();
                }

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(entry.Name);

                ImGui.TableNextColumn();
                if (isWindowFocused && (ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift)))
                {
                    if (ImGuiUtils.IconButton(key + "_Delete", FontAwesomeIcon.Trash, "Delete"))
                    {
                        entryToRemove = i;
                    }
                }
                else
                {
                    ImGuiUtils.IconButton(
                        key + "_Delete",
                        FontAwesomeIcon.Trash,
                        isWindowFocused
                            ? "Hold shift to delete"
                            : "Focus window and hold shift to delete",
                        disabled: true);
                }

                i++;
            }

            ImGui.EndTable();

            if (entryToRemove != -1)
            {
                Config.LockedWindows.RemoveAt(entryToRemove);
                Plugin.Config.Save();
            }
        }
        else
        {
            ImGuiUtils.TextUnformattedDisabled("No windows added yet.");
            ImGuiUtils.PushCursorY(4);
        }

        if (_showPicker)
        {
            if (ImGui.Button("Cancel"))
            {
                _showPicker = false;
            }
        }
        else
        {
            if (ImGui.Button("Pick Window"))
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

            if (ImGui.Button("Toggle All"))
            {
                foreach (var entry in Config.LockedWindows)
                {
                    entry.Enabled = !entry.Enabled;
                }
                Plugin.Config.Save();
            }
        }

        if (_showPicker && _hoveredWindowPos != default)
        {
            ImGui.SetNextWindowPos(_hoveredWindowPos);
            ImGui.SetNextWindowSize(_hoveredWindowSize);

            using var windowBorderSize = ImRaii.PushStyle(ImGuiStyleVar.WindowBorderSize, 1.0f);
            using var borderColor = ImRaii.PushColor(ImGuiCol.Border, (uint)Colors.Gold);
            using var windowBgColor = ImRaii.PushColor(ImGuiCol.WindowBg, new Vector4(0.847f, 0.733f, 0.49f, 0.33f));

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
                        Plugin.Config.Save();
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
    [VTableHook<AddonGearSetList>((int)AtkResNodeVfs.OnSetup)]
    public nint AddonGearSetList_OnSetup(AddonGearSetList* addon, int numAtkValues, AtkValue* atkValues)
    {
        var result = AddonGearSetList_OnSetupHook.OriginalDisposeSafe(addon, numAtkValues, atkValues);

        var isLocked = Config.LockedWindows.Any(entry => entry.Enabled && entry.Name == "GearSetList");

        if (Config.Inverted)
            isLocked = !isLocked;

        if (isLocked)
            addon->ResetPosition = false;

        return result;
    }

    [SigHook("E8 ?? ?? ?? ?? 0F BF 8C 24 ?? ?? ?? ?? 01 8F")]
    public bool Move(AtkUnitBase* atkUnitBase, nint xDelta, nint yDelta)
    {
        if (atkUnitBase != null)
        {
            var name = MemoryHelper.ReadStringNullTerminated((nint)atkUnitBase->Name);
            var isLocked = Config.LockedWindows.Any(entry => entry.Enabled && entry.Name == name);

            if (Config.Inverted)
                isLocked = !isLocked;

            if (isLocked)
                return false;
        }

        return MoveHook.OriginalDisposeSafe(atkUnitBase, xDelta, yDelta);
    }

    [SigHook("48 89 5C 24 ?? 48 89 6C 24 ?? 57 48 83 EC 30 80 7A 37 00")]
    public bool RaptureAtkUnitManager_Vf6(RaptureAtkUnitManager* self, nint a2)
    {
        if (_showPicker)
        {
            if (a2 != 0)
            {
                var atkUnitBase = *(AtkUnitBase**)(a2 + 8);
                if (atkUnitBase != null && atkUnitBase->WindowNode != null && atkUnitBase->WindowCollisionNode != null)
                {
                    var name = MemoryHelper.ReadStringNullTerminated((nint)atkUnitBase->Name);
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
            var addon = GetAddon(addonId);
            if (addon != null)
            {
                var name = MemoryHelper.ReadStringNullTerminated((nint)addon->Name);

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
                            var title = Service.ClientState.ClientLanguage switch
                            {
                                ClientLanguage.German => "Position entsperren",
                                ClientLanguage.French => "Déverrouiller la position",
                                ClientLanguage.Japanese => "ポジションのロック解除",
                                _ => "Unlock Position"
                            };

                            AddMenuEntry(title, EventParamUnlock);
                        }
                    }
                    else
                    {
                        if (Config.AddLockUnlockContextMenuEntries)
                        {
                            var title = Service.ClientState.ClientLanguage switch
                            {
                                ClientLanguage.German => "Position sperren",
                                ClientLanguage.French => "Verrouiller la position",
                                ClientLanguage.Japanese => "ポジションをロックする",
                                _ => "Lock Position"
                            };

                            AddMenuEntry(title, EventParamLock);
                        }
                    }
                }
            }
        }

        return AgentContext_OpenContextMenuForAddonHook.OriginalDisposeSafe(agent, addonId, bindToOwner);
    }

    [SigHook("48 89 6C 24 ?? 48 89 54 24 ?? 56 41 54")]
    public AtkValue* WindowContextMenuEventHandler(nint self, AtkValue* result, nint a3, long a4, long eventParam)
    {
        if (_eventIndexToDisable == 7 && eventParam is EventParamUnlock or EventParamLock && GetAgent<AgentContext>(AgentId.Context, out var agentContext))
        {
            if (GetAddon(agentContext->OwnerAddon, out var addon))
            {
                var name = MemoryHelper.ReadStringNullTerminated((nint)addon->Name);

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

                Plugin.Config.Save();
            }

            _eventIndexToDisable = 0;

            result->Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Bool;
            result->Byte = 0;
            return result;
        }

        if (_eventIndexToDisable != 0)
            _eventIndexToDisable = 0;

        return WindowContextMenuEventHandlerHook.OriginalDisposeSafe(self, result, a3, a4, eventParam);
    }

    private void AddMenuEntry(string text, int eventParam)
    {
        if (!GetAgent<AgentContext>(AgentId.Context, out var agentContext))
            return;

        var bytes = new SeStringBuilder()
            .AddUiForeground("\uE078 ", 32)
            .AddText(text)
            .Encode();

        var handler = (nint)AtkStage.GetSingleton()->RaptureAtkUnitManager + 0x9C88; // see vtbl ptr in ctor
        fixed (byte* ptr = &bytes[0])
        {
            agentContext->AddMenuItem(ptr, (void*)handler, eventParam);
        }
    }
}
