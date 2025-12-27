using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Windows;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class EnhancedIsleworksAgenda : ConfigurableTweak<EnhancedIsleworksAgendaConfiguration>
{
    private readonly IAddonLifecycle _addonLifecycle;
    private readonly MJICraftScheduleSettingSearchBar _window;

    public override void OnEnable()
    {
        _addonLifecycle.RegisterListener(AddonEvent.PreReceiveEvent, "MJICraftScheduleSetting", OnPreReceiveEvent);
        _addonLifecycle.RegisterListener(AddonEvent.PostShow, "MJICraftScheduleSetting", OnPostShow);
        _addonLifecycle.RegisterListener(AddonEvent.PreHide, "MJICraftScheduleSetting", OnPreHide);

        if (_config.EnableSearchBar && IsAddonOpen("MJICraftScheduleSetting"u8))
            _window.Open();
    }

    public override void OnDisable()
    {
        _addonLifecycle.UnregisterListener(AddonEvent.PreReceiveEvent, "MJICraftScheduleSetting", OnPreReceiveEvent);
        _addonLifecycle.UnregisterListener(AddonEvent.PostShow, "MJICraftScheduleSetting", OnPostShow);
        _addonLifecycle.UnregisterListener(AddonEvent.PreHide, "MJICraftScheduleSetting", OnPreHide);
        _window.Close();
    }

    private void OnPostShow(AddonEvent type, AddonArgs args)
    {
        if (_config.EnableSearchBar)
            _window.Open();
    }

    private void OnPreHide(AddonEvent type, AddonArgs args)
    {
        _window.Close();
    }

    private void OnPreReceiveEvent(AddonEvent type, AddonArgs addonArgs)
    {
        if (!_config.DisableTreeListTooltips || addonArgs is not AddonReceiveEventArgs args)
            return;

        if ((AtkEventType)args.AtkEventType != AtkEventType.ListItemRollOver || args.EventParam != 2)
            return;

        var addon = (AddonMJICraftScheduleSetting*)args.Addon.Address;
        var index = ((AtkEventData.AtkListItemData*)args.AtkEventData)->SelectedIndex;
        var item = addon->TreeList->GetItem(index);
        if (item == null || item->UIntValues.LongCount < 1)
            return;

        if (item->UIntValues[0] == (uint)AtkComponentTreeListItemType.CollapsibleGroupHeader)
            return;

        args.EventParam = 0;
        ((AtkEvent*)args.AtkEvent)->SetEventIsHandled();
    }
}
