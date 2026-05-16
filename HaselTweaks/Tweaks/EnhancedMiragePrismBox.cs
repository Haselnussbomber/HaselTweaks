using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class EnhancedMiragePrismBox : ConfigurableTweak<EnhancedMiragePrismBoxConfiguration>
{
    private readonly IAddonLifecycle _addonLifecycle;
    private readonly MirageService _mirageService;

    public override void OnEnable()
    {
        _addonLifecycle.RegisterListener(AddonEvent.PostRefresh, "MiragePrismPrismSetConvert", OnPostRefresh);
    }

    public override void OnDisable()
    {
        _addonLifecycle.UnregisterListener(AddonEvent.PostRefresh, "MiragePrismPrismSetConvert", OnPostRefresh);
    }

    private void OnPostRefresh(AddonEvent type, AddonArgs args)
    {
        if (!_config.EnableAutoFillHandIn)
            return;

        if (args is not AddonRefreshArgs refreshArgs)
            return;

        var values = refreshArgs.GetAtkValues();
        if (values.Length == 0)
            return;

        if (!values[0].TryGetUInt(out var flags))
            return;

        if ((flags & 4) == 0 || (flags & 0x100000) != 0)
            return;

        var agent = AgentMiragePrismPrismSetConvert.Instance();
        if (agent->Data == null)
            return;

        var hasHandInItems = false;

        foreach (ref var item in agent->Data->Items)
        {
            if (item.ItemId == 0 || item.InventoryType != InventoryType.Invalid || _mirageService.IsItemCollected(item.ItemId))
                continue;

            hasHandInItems |= TryFindItem(ref item);
        }

        if (!hasHandInItems)
            return;

        var haselAgent = (HaselAgentMiragePrismPrismSetConvert*)agent;
        haselAgent->UpdateAddon(4 | 0x100000); // imaginary 0x100000 flag, for our safety guard above
    }

    private static bool TryFindItem(ref AgentMiragePrismPrismSetConvert.AgentData.ItemSetItem item)
    {
        var inventoryManager = InventoryManager.Instance();

        for (var i = (int)InventoryType.Inventory1; i <= (int)InventoryType.Inventory4; i++)
        {
            var container = inventoryManager->GetInventoryContainer((InventoryType)i);

            for (var slotIndex = 0; slotIndex < container->GetSize(); slotIndex++)
            {
                var slot = container->GetInventorySlot(slotIndex);
                if (slot == null)
                    continue;

                if (slot->GetItemId() == item.ItemId)
                {
                    item.InventoryType = slot->GetInventoryType();
                    item.Slot = slot->GetSlot();
                    return true;
                }
            }
        }

        return false;
    }
}
