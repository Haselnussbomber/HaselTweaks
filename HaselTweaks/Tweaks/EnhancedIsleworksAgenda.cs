using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Windows;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class EnhancedIsleworksAgenda : IConfigurableTweak
{
    private readonly PluginConfig _pluginConfig;
    private readonly ConfigGui _configGui;
    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly AddonObserver _addonObserver;
    private readonly MJICraftScheduleSettingSearchBar _window;

    private Hook<AddonMJICraftScheduleSetting.Delegates.ReceiveEvent>? _receiveEventHook;

    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    public void OnInitialize()
    {
        _receiveEventHook = _gameInteropProvider.HookFromAddress<AddonMJICraftScheduleSetting.Delegates.ReceiveEvent>(
            AddonMJICraftScheduleSetting.StaticVirtualTablePointer->ReceiveEvent,
            ReceiveEventDetour);
    }

    public void OnEnable()
    {
        if (Config.EnableSearchBar && IsAddonOpen("MJICraftScheduleSetting"))
            _window.Open();

        _addonObserver.AddonOpen += OnAddonOpen;
        _addonObserver.AddonClose += OnAddonClose;

        _receiveEventHook?.Enable();
    }

    public void OnDisable()
    {
        _addonObserver.AddonOpen -= OnAddonOpen;
        _addonObserver.AddonClose -= OnAddonClose;

        _receiveEventHook?.Disable();

        _window.Close();
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();
        _receiveEventHook?.Dispose();

        Status = TweakStatus.Disposed;
    }

    private void OnAddonOpen(string addonName)
    {
        if (Config.EnableSearchBar && addonName == "MJICraftScheduleSetting")
            _window.Open();
    }

    private void OnAddonClose(string addonName)
    {
        if (addonName == "MJICraftScheduleSetting")
            _window.Close();
    }

    private void ReceiveEventDetour(AddonMJICraftScheduleSetting* addon, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData)
    {
        if (eventType == AtkEventType.ListItemRollOver && eventParam == 2 && Config.DisableTreeListTooltips)
        {
            var index = atkEventData->ListItemData.SelectedIndex;
            var itemPtr = addon->TreeList->GetItem(index);
            if (itemPtr != null && itemPtr->UIntValues.LongCount >= 1)
            {
                if (itemPtr->UIntValues[0] != (uint)AtkComponentTreeListItemType.CollapsibleGroupHeader)
                {
                    atkEvent->SetEventIsHandled();
                    return;
                }
            }
        }

        _receiveEventHook!.Original(addon, eventType, eventParam, atkEvent, atkEventData);
    }
}
