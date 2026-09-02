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
        _disposables = _addonLifecycle.OnPreReceiveEvent(OnPreReceiveEvent, "TelepotTown");
    }

    public override void OnDisable()
    {
        DisposeAndNull(ref _disposables);
    }

    private void OnPreReceiveEvent(AddonReceiveEventArgs args)
    {
        if (args.EventType != AtkEventType.ListItemRollOver)
            return;

        var eventData = args.GetEventData<AtkEventData.AtkListItemData>();
        var index = eventData->SelectedIndex;
        if (index < 0)
            return;

        var addon = args.GetAddon<AddonTeleportTown>();
        var item = addon->List->GetItem(index);
        if (item == null || item->UIntValues.LongCount < 4)
            return;

        var agent = AgentTelepotTown.Instance();
        if (agent->Data == null)
            return;

        agent->Data->SelectedAetheryte = (byte)item->UIntValues[3];
        agent->Data->Flags |= 2;
        eventData->SelectedIndex = -1; // suppress original handling of this event
    }
}
