using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class SimpleAethernetList : Tweak
{
    private readonly IAddonLifecycle _addonLifecycle;

    public override void OnEnable()
    {
        _addonLifecycle.RegisterListener(AddonEvent.PreReceiveEvent, "TelepotTown", OnPreReceiveEvent);
    }

    public override void OnDisable()
    {
        _addonLifecycle.UnregisterListener(AddonEvent.PreReceiveEvent, "TelepotTown", OnPreReceiveEvent);
    }

    private void OnPreReceiveEvent(AddonEvent type, AddonArgs addonArgs)
    {
        if (addonArgs is not AddonReceiveEventArgs args)
            return;

        if ((AtkEventType)args.AtkEventType != AtkEventType.ListItemRollOver)
            return;

        var listItemData = (AtkEventData.AtkListItemData*)args.AtkEventData;
        var index = listItemData->SelectedIndex;
        if (index < 0)
            return;

        var addon = (AddonTeleportTown*)args.Addon.Address;
        var item = addon->List->GetItem(index);
        if (item == null || item->UIntValues.LongCount < 4)
            return;

        var agent = AgentTelepotTown.Instance();
        if (agent->Data == null)
            return;

        agent->Data->SelectedAetheryte = (byte)item->UIntValues[3];
        agent->Data->Flags |= 2;
        listItemData->SelectedIndex = -1; // suppress original handling of this event
    }
}
