using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using AtkEventInterface = FFXIVClientStructs.FFXIV.Component.GUI.AtkModuleInterface.AtkEventInterface;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class LockWindowPosition : IConfigurableTweak
{
    private const int EventParamLock = 9901;
    private const int EventParamUnlock = 9902;
    private static readonly string[] IgnoredAddons = [
        "CharaCardEditMenu", // always opens docked to CharaCard (OnSetup)
    ];

    private readonly PluginConfig _pluginConfig;
    private readonly ConfigGui _configGui;
    private readonly TextService _textService;
    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly IAddonLifecycle _addonLifecycle;

    private Hook<AtkUnitBase.Delegates.MoveDelta>? _moveDeltaHook;
    private Hook<RaptureAtkUnitManagerVf6Delegate>? _raptureAtkUnitManagerVf6Hook;
    private Hook<AgentContext.Delegates.ClearMenu>? _clearMenuHook;
    private Hook<AgentContext.Delegates.AddMenuItem2>? _addMenuItem2Hook;
    private Hook<AgentContext.Delegates.OpenContextMenuForAddon>? _openContextMenuForAddonHook;
    private Hook<AtkEventInterface.Delegates.ReceiveEvent>? _windowContextMenuHandlerReceiveEventHook;

    private bool _showPicker = false;
    private string _hoveredWindowName = "";
    private Vector2 _hoveredWindowPos;
    private Vector2 _hoveredWindowSize;
    private int _eventIndexToDisable = 0;

    private delegate bool RaptureAtkUnitManagerVf6Delegate(RaptureAtkUnitManager* self, nint a2);

    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    public void OnInitialize()
    {
        _moveDeltaHook = _gameInteropProvider.HookFromAddress<AtkUnitBase.Delegates.MoveDelta>(
            AtkUnitBase.MemberFunctionPointers.MoveDelta,
            MoveDeltaDetour);

        _raptureAtkUnitManagerVf6Hook = _gameInteropProvider.HookFromVTable<RaptureAtkUnitManagerVf6Delegate>(
            RaptureAtkUnitManager.StaticVirtualTablePointer, 6,
            RaptureAtkUnitManagerVf6Detour);

        _clearMenuHook = _gameInteropProvider.HookFromAddress<AgentContext.Delegates.ClearMenu>(
            AgentContext.MemberFunctionPointers.ClearMenu,
            ClearMenuDetour);

        _addMenuItem2Hook = _gameInteropProvider.HookFromAddress<AgentContext.Delegates.AddMenuItem2>(
            AgentContext.MemberFunctionPointers.AddMenuItem2,
            AddMenuItem2Detour);

        _openContextMenuForAddonHook = _gameInteropProvider.HookFromAddress<AgentContext.Delegates.OpenContextMenuForAddon>(
            AgentContext.MemberFunctionPointers.OpenContextMenuForAddon,
            OpenContextMenuForAddonDetour);

        _windowContextMenuHandlerReceiveEventHook = _gameInteropProvider.HookFromAddress<AtkEventInterface.Delegates.ReceiveEvent>(
            RaptureAtkUnitManager.Instance()->WindowContextMenuHandler.VirtualTable->ReceiveEvent,
            WindowContextMenuHandlerReceiveEventDetour);
    }

    public void OnEnable()
    {
        _addonLifecycle.RegisterListener(AddonEvent.PostSetup, "GearSetList", GearSetList_PostSetup);

        _moveDeltaHook?.Enable();
        _raptureAtkUnitManagerVf6Hook?.Enable();
        _clearMenuHook?.Enable();
        _addMenuItem2Hook?.Enable();
        _openContextMenuForAddonHook?.Enable();
        _windowContextMenuHandlerReceiveEventHook?.Enable();
    }

    public void OnDisable()
    {
        _addonLifecycle.UnregisterListener(AddonEvent.PostSetup, "GearSetList", GearSetList_PostSetup);

        _moveDeltaHook?.Disable();
        _raptureAtkUnitManagerVf6Hook?.Disable();
        _clearMenuHook?.Disable();
        _addMenuItem2Hook?.Disable();
        _openContextMenuForAddonHook?.Disable();
        _windowContextMenuHandlerReceiveEventHook?.Disable();
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();
        _moveDeltaHook?.Dispose();
        _raptureAtkUnitManagerVf6Hook?.Dispose();
        _clearMenuHook?.Dispose();
        _addMenuItem2Hook?.Dispose();
        _openContextMenuForAddonHook?.Dispose();
        _windowContextMenuHandlerReceiveEventHook?.Dispose();

        Status = TweakStatus.Disposed;
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

        return _moveDeltaHook!.Original(atkUnitBase, xDelta, yDelta);
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
                        _hoveredWindowSize = new(atkUnitBase->WindowNode->Width, atkUnitBase->WindowNode->Height - 7);
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

        return _raptureAtkUnitManagerVf6Hook!.Original(self, a2);
    }

    private void ClearMenuDetour(AgentContext* agent)
    {
        if (_eventIndexToDisable != 0)
            _eventIndexToDisable = 0;

        _clearMenuHook!.Original(agent);
    }

    private void AddMenuItem2Detour(AgentContext* agent, uint addonRowId, AtkEventInterface* handlerPtr, long handlerParam, bool disabled, bool submenu)
    {
        if (addonRowId == 8660 && agent->ContextMenuIndex == 0) // "Return to Default Position"
        {
            _eventIndexToDisable = agent->CurrentContextMenu->CurrentEventIndex;
        }

        _addMenuItem2Hook!.Original(agent, addonRowId, handlerPtr, handlerParam, disabled, submenu);
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
                            AddMenuEntry(_textService.Translate("LockWindowPosition.UnlockPosition"), EventParamUnlock);
                        }
                    }
                    else
                    {
                        if (Config.AddLockUnlockContextMenuEntries)
                        {
                            AddMenuEntry(_textService.Translate("LockWindowPosition.LockPosition"), EventParamLock);
                        }
                    }
                }
            }
        }

        _openContextMenuForAddonHook!.Original(agent, ownerAddonId, bindToOwner);
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

                _pluginConfig.Save();
            }

            _eventIndexToDisable = 0;

            returnValue->Type = ValueType.Bool;
            returnValue->Byte = 0;
            return returnValue;
        }

        if (_eventIndexToDisable != 0)
            _eventIndexToDisable = 0;

        return _windowContextMenuHandlerReceiveEventHook!.Original(self, returnValue, values, valueCount, eventKind);
    }

    private void AddMenuEntry(string text, int eventParam)
    {
        using var rssb = new RentedSeStringBuilder();

        AgentContext.Instance()->AddMenuItem(
            rssb.Builder
                .AppendHaselTweaksPrefix()
                .Append(text)
                .GetViewAsSpan(),
            &AtkStage.Instance()->RaptureAtkUnitManager->WindowContextMenuHandler,
            eventParam);
    }
}
