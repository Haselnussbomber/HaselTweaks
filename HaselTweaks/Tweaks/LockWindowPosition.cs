using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Extensions;
using HaselCommon.Services;
using HaselTweaks.Config;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;
using AtkEventInterface = FFXIVClientStructs.FFXIV.Component.GUI.AtkModuleInterface.AtkEventInterface;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace HaselTweaks.Tweaks;

public unsafe partial class LockWindowPosition(
    PluginConfig PluginConfig,
    ConfigGui ConfigGui,
    TextService TextService,
    IGameInteropProvider GameInteropProvider,
    IAddonLifecycle AddonLifecycle)
    : IConfigurableTweak
{
    public string InternalName => nameof(LockWindowPosition);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

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

    public void OnInitialize()
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

    public void OnEnable()
    {
        AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "GearSetList", GearSetList_PostSetup);

        MoveDeltaHook?.Enable();
        RaptureAtkUnitManagerVf6Hook?.Enable();
        ClearMenuHook?.Enable();
        AddMenuItem2Hook?.Enable();
        OpenContextMenuForAddonHook?.Enable();
        WindowContextMenuHandlerReceiveEventHook?.Enable();
    }

    public void OnDisable()
    {
        AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "GearSetList", GearSetList_PostSetup);

        MoveDeltaHook?.Disable();
        RaptureAtkUnitManagerVf6Hook?.Disable();
        ClearMenuHook?.Disable();
        AddMenuItem2Hook?.Disable();
        OpenContextMenuForAddonHook?.Disable();
        WindowContextMenuHandlerReceiveEventHook?.Disable();
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();
        MoveDeltaHook?.Dispose();
        RaptureAtkUnitManagerVf6Hook?.Dispose();
        ClearMenuHook?.Dispose();
        AddMenuItem2Hook?.Dispose();
        OpenContextMenuForAddonHook?.Dispose();
        WindowContextMenuHandlerReceiveEventHook?.Dispose();

        Status = TweakStatus.Disposed;
        GC.SuppressFinalize(this);
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
