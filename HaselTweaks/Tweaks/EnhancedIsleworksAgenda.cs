using Dalamud;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Services;
using HaselCommon.Utils;
using HaselTweaks.Enums;
using HaselTweaks.Structs;
using HaselTweaks.Windows;

namespace HaselTweaks.Tweaks;

public class EnhancedIsleworksAgendaConfiguration
{
    [BoolConfig]
    public bool EnableSearchBar = true;

    [BoolConfig]
    public bool DisableTreeListTooltips = true;

    public ClientLanguage SearchLanguage = ClientLanguage.English;
}

[Tweak]
public unsafe partial class EnhancedIsleworksAgenda : Tweak<EnhancedIsleworksAgendaConfiguration>
{
    private VFuncHook<AddonMJICraftScheduleSetting.Delegates.ReceiveEvent>? ReceiveEventHook;

    public override void SetupHooks()
    {
        ReceiveEventHook = new(AddonMJICraftScheduleSetting.StaticVirtualTablePointer, (int)AtkUnitBaseVfs.ReceiveEvent, ReceiveEventDetour);
    }

    public override void Enable()
    {
        if (Config.EnableSearchBar && IsAddonOpen("MJICraftScheduleSetting"))
            Service.WindowManager.OpenWindow<MJICraftScheduleSettingSearchBar>();
    }

    public override void Disable()
    {
        if (Service.HasService<WindowManager>())
            Service.WindowManager.CloseWindow<MJICraftScheduleSettingSearchBar>();
    }

    public override void OnAddonOpen(string addonName)
    {
        if (Config.EnableSearchBar && addonName == "MJICraftScheduleSetting")
            Service.WindowManager.OpenWindow<MJICraftScheduleSettingSearchBar>();
    }

    public override void OnAddonClose(string addonName)
    {
        if (addonName == "MJICraftScheduleSetting")
            Service.WindowManager.CloseWindow<MJICraftScheduleSettingSearchBar>();
    }

    public override void OnConfigChange(string fieldName)
    {
        if (fieldName == "EnableSearchBar")
        {
            if (Config.EnableSearchBar && IsAddonOpen("MJICraftScheduleSetting"))
                Service.WindowManager.OpenWindow<MJICraftScheduleSettingSearchBar>();
            else
                Service.WindowManager.CloseWindow<MJICraftScheduleSettingSearchBar>();
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

        ReceiveEventHook!.OriginalDisposeSafe(addon, eventType, eventParam, atkEvent, atkEventData);
    }
}
