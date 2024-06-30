using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Services;
using HaselTweaks.Config;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;
using HaselTweaks.Structs;
using HaselTweaks.Windows;

namespace HaselTweaks.Tweaks;

public unsafe partial class EnhancedIsleworksAgenda(
    PluginConfig PluginConfig,
    ConfigGui ConfigGui,
    IGameInteropProvider GameInteropProvider,
    AddonObserver AddonObserver,
    MJICraftScheduleSettingSearchBar Window)
    : IConfigurableTweak
{
    public string InternalName => nameof(EnhancedIsleworksAgenda);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    private Hook<AddonMJICraftScheduleSetting.Delegates.ReceiveEvent>? ReceiveEventHook;

    public void OnInitialize()
    {
        ReceiveEventHook = GameInteropProvider.HookFromAddress<AddonMJICraftScheduleSetting.Delegates.ReceiveEvent>(
            AddonMJICraftScheduleSetting.StaticVirtualTablePointer->ReceiveEvent,
            ReceiveEventDetour);
    }

    public void OnEnable()
    {
        if (Config.EnableSearchBar && IsAddonOpen("MJICraftScheduleSetting"))
            Window.Open();

        AddonObserver.AddonOpen += OnAddonOpen;
        AddonObserver.AddonClose += OnAddonClose;

        ReceiveEventHook?.Enable();
    }

    public void OnDisable()
    {
        AddonObserver.AddonOpen -= OnAddonOpen;
        AddonObserver.AddonClose -= OnAddonClose;

        ReceiveEventHook?.Disable();

        Window.Close();
    }

    public void Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();
        ReceiveEventHook?.Dispose();

        Status = TweakStatus.Disposed;
        GC.SuppressFinalize(this);
    }

    private void OnAddonOpen(string addonName)
    {
        if (Config.EnableSearchBar && addonName == "MJICraftScheduleSetting")
            Window.Open();
    }

    private void OnAddonClose(string addonName)
    {
        if (addonName == "MJICraftScheduleSetting")
            Window.Close();
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

        ReceiveEventHook!.Original(addon, eventType, eventParam, atkEvent, atkEventData);
    }
}
