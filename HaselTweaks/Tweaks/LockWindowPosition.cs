using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using AtkEventInterface = FFXIVClientStructs.FFXIV.Component.GUI.AtkModuleInterface.AtkEventInterface;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class LockWindowPosition : ConfigurableTweak<LockWindowPositionConfiguration>
{
    private const int EventParamLock = 9901;
    private const int EventParamUnlock = 9902;
    private static readonly string[] IgnoredAddons = [
        "CharaCardEditMenu", // always opens docked to CharaCard (OnSetup)
    ];

    private readonly TextService _textService;
    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly IAddonLifecycle _addonLifecycle;
    private readonly IFramework _framework;

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

    private delegate byte RaptureAtkUnitManagerVf6Delegate(RaptureAtkUnitManager* self, nint a2);

    public override ValueTask OnEnable()
    {
        return new ValueTask(_framework.Run(() =>
        {
            _disposables = DisposableBag.Create(
                _moveDeltaHook = _gameInteropProvider.EnabledHookFromAddress<AtkUnitBase.Delegates.MoveDelta>(
                    AtkUnitBase.MemberFunctionPointers.MoveDelta,
                    MoveDeltaDetour),

                _raptureAtkUnitManagerVf6Hook = _gameInteropProvider.EnabledHookFromVTable<RaptureAtkUnitManagerVf6Delegate>(
                    RaptureAtkUnitManager.StaticVirtualTablePointer, 6,
                    RaptureAtkUnitManagerVf6Detour),

                _clearMenuHook = _gameInteropProvider.EnabledHookFromAddress<AgentContext.Delegates.ClearMenu>(
                    AgentContext.MemberFunctionPointers.ClearMenu,
                    ClearMenuDetour),

                _addMenuItem2Hook = _gameInteropProvider.EnabledHookFromAddress<AgentContext.Delegates.AddMenuItem2>(
                    AgentContext.MemberFunctionPointers.AddMenuItem2,
                    AddMenuItem2Detour),

                _openContextMenuForAddonHook = _gameInteropProvider.EnabledHookFromAddress<AgentContext.Delegates.OpenContextMenuForAddon>(
                    AgentContext.MemberFunctionPointers.OpenContextMenuForAddon,
                    OpenContextMenuForAddonDetour),

                _windowContextMenuHandlerReceiveEventHook = _gameInteropProvider.EnabledHookFromAddress<AtkEventInterface.Delegates.ReceiveEvent>(
                    RaptureAtkUnitManager.Instance()->WindowContextMenuHandler.VirtualTable->ReceiveEvent,
                    WindowContextMenuHandlerReceiveEventDetour),

                _addonLifecycle.OnPostSetup(OnGearSetListPostSetup, "GearSetList"));
        }));
    }

    public override ValueTask OnDisable()
    {
        return new ValueTask(_framework.Run(() => DisposeAndNull(ref _disposables)));
    }

    // block GearSetList from moving when opened by Character
    private void OnGearSetListPostSetup(AddonArgs args)
    {
        var addon = args.GetAddon<AddonGearSetList>();

        var isLocked = _config.LockedWindows.Any(entry => entry.Enabled && entry.Name == "GearSetList");

        if (_config.Inverted)
            isLocked = !isLocked;

        if (isLocked)
            addon->ShouldResetPosition = false;
    }

    private bool MoveDeltaDetour(AtkUnitBase* atkUnitBase, short* xDelta, short* yDelta)
    {
        if (atkUnitBase != null)
        {
            var name = atkUnitBase->NameString;
            var isLocked = _config.LockedWindows.Any(entry => entry.Enabled && entry.Name == name);

            if (_config.Inverted)
                isLocked = !isLocked;

            if (isLocked)
                return false;
        }

        return _moveDeltaHook!.Original(atkUnitBase, xDelta, yDelta);
    }

    private byte RaptureAtkUnitManagerVf6Detour(RaptureAtkUnitManager* self, nint a2)
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

            return 0;
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
        if (_eventIndexToDisable == 8 && agent->ContextMenuIndex == 0)
        {
            var addon = GetAddon<AtkUnitBase>((ushort)ownerAddonId);
            if (addon != null)
            {
                var name = addon->NameString;

                if (!IgnoredAddons.Contains(name))
                {
                    var isLocked = _config.LockedWindows.Any(entry => entry.Enabled && entry.Name == name);

                    if (_config.Inverted)
                        isLocked = !isLocked;

                    if (isLocked)
                    {
                        agent->CurrentContextMenu->ContextItemDisabledMask |= 1; // keeping it simple. disables "Return to Default Position"

                        if (_config.AddLockUnlockContextMenuEntries)
                        {
                            AddMenuEntry(_textService.Translate("LockWindowPosition.UnlockPosition"), EventParamUnlock);
                        }
                    }
                    else
                    {
                        if (_config.AddLockUnlockContextMenuEntries)
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
        if (_eventIndexToDisable == 8 && (int)eventKind is EventParamUnlock or EventParamLock)
        {
            if (TryGetAddon<AtkUnitBase>((ushort)AgentContext.Instance()->OwnerAddon, out var addon))
            {
                var name = addon->NameString;
                var entry = _config.LockedWindows.FirstOrDefault(entry => entry?.Name == name, null);
                var isLocked = eventKind == EventParamLock;

                if (_config.Inverted)
                    isLocked = !isLocked;

                if (entry != null)
                {
                    entry.Enabled = isLocked;
                }
                else
                {
                    _config.LockedWindows.Add(new()
                    {
                        Enabled = isLocked,
                        Name = name,
                    });
                }

                _pluginConfig.Save();
            }

            _eventIndexToDisable = 0;

            returnValue->Type = AtkValueType.Bool;
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
