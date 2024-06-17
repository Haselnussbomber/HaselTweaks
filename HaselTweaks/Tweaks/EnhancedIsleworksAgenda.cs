using Dalamud;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Services;
using HaselTweaks.Structs;
using HaselTweaks.Windows;

namespace HaselTweaks.Tweaks;

public sealed class EnhancedIsleworksAgendaConfiguration
{
    [BoolConfig]
    public bool EnableSearchBar = true;

    [BoolConfig]
    public bool DisableTreeListTooltips = true;

    public ClientLanguage SearchLanguage = ClientLanguage.English;
}

public sealed unsafe class EnhancedIsleworksAgenda(
    IGameInteropProvider GameInteropProvider,
    Configuration PluginConfig,
    TranslationManager TranslationManager,
    AddonObserver AddonObserver,
    MJICraftScheduleSettingSearchBar Window)
    : Tweak<EnhancedIsleworksAgendaConfiguration>(PluginConfig, TranslationManager)
{
    private Hook<AddonMJICraftScheduleSetting.Delegates.ReceiveEvent>? ReceiveEventHook;

    public override void OnInitialize()
    {
        ReceiveEventHook = GameInteropProvider.HookFromAddress<AddonMJICraftScheduleSetting.Delegates.ReceiveEvent>(
            AddonMJICraftScheduleSetting.StaticVirtualTablePointer->ReceiveEvent,
            ReceiveEventDetour);
    }

    public override void OnEnable()
    {
        if (Config.EnableSearchBar && IsAddonOpen("MJICraftScheduleSetting"))
            Window.Open();

        AddonObserver.AddonOpen += OnAddonOpen;
        AddonObserver.AddonClose += OnAddonClose;

        ReceiveEventHook?.Enable();
    }

    public override void OnDisable()
    {
        AddonObserver.AddonOpen -= OnAddonOpen;
        AddonObserver.AddonClose -= OnAddonClose;

        ReceiveEventHook?.Disable();

        Window.Close();
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

    public override void OnConfigChange(string fieldName)
    {
        if (fieldName == "EnableSearchBar")
        {
            if (Config.EnableSearchBar && IsAddonOpen("MJICraftScheduleSetting"))
                Window.Open();
            else
                Window.Close();
        }
    }

    private void ReceiveEventDetour(AddonMJICraftScheduleSetting* addon, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, nint atkEventData)
    {
        if (eventType == AtkEventType.ListItemRollOver && eventParam == 2 && Config.DisableTreeListTooltips)
        {
            var index = *(uint*)(atkEventData + 0x10);
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
