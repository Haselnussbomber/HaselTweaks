using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Game.Enums;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class AutoSelectSoilAndSeed : Tweak
{
    private readonly IAddonLifecycle _addonLifecycle;
    private readonly ExcelService _excelService;
    private readonly IFramework _framework;

    public override void OnEnable()
    {
        _addonLifecycle.RegisterListener(AddonEvent.PostSetup, "HousingGardening", OnPostSetup);
        _addonLifecycle.RegisterListener(AddonEvent.PreFinalize, "HousingGardening", OnPreFinalize);
    }

    public override void OnDisable()
    {
        _addonLifecycle.UnregisterListener(AddonEvent.PostSetup, "HousingGardening", OnPostSetup);
        _addonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "HousingGardening", OnPreFinalize);
    }

    private void OnPostSetup(AddonEvent type, AddonArgs args)
    {
        _framework.Update += OnUpdate;
    }

    private void OnPreFinalize(AddonEvent type, AddonArgs args)
    {
        _framework.Update -= OnUpdate;
    }

    private void OnUpdate(IFramework framework)
    {
        var agent = AgentHousingPlant.Instance();
        if (!agent->IsAgentActive())
            return;

        var agentEx = (HaselAgentHousingPlant*)agent;
        if (agentEx->State != 0)
            return;

        if (agent->SelectedItems[0].InventoryType != InventoryType.Invalid && agent->SelectedItems[1].InventoryType != InventoryType.Invalid)
            return;

        var isSoilSet = false;
        var isSeedSet = false;

        var inventoryManager = InventoryManager.Instance();
        for (var typeIndex = 0; typeIndex <= 3; typeIndex++) // InventoryType.Inventory0-4
        {
            var inventoryType = (InventoryType)typeIndex;
            var container = inventoryManager->GetInventoryContainer(inventoryType);
            if (container == null || !container->IsLoaded)
                continue;

            for (ushort slotIndex = 0; slotIndex < container->GetSize(); slotIndex++)
            {
                var slot = container->GetInventorySlot(slotIndex);
                if (slot == null || slot->IsEmpty())
                    continue;

                var (baseItemId, itemKind) = ItemUtil.GetBaseId(slot->GetItemId());
                if (itemKind == ItemKind.EventItem)
                    continue;

                if (!_excelService.TryGetRow<Item>(baseItemId, out var itemRow))
                    continue;

                if (!isSoilSet && itemRow.FilterGroup == (byte)ItemFilterGroup.GardeningSoil)
                {
                    _logger.LogDebug("Selecting soil {inventoryType}#{slotIndex} ({itemId}) - {itemName}", inventoryType, slotIndex, baseItemId, itemRow.Name);
                    SelectItem(0, itemRow.RowId, itemRow.Icon, inventoryType, slotIndex);
                    isSoilSet = true;
                }
                else if (!isSeedSet && itemRow.FilterGroup == (byte)ItemFilterGroup.GardeningSeed)
                {
                    _logger.LogDebug("Selecting seed {inventoryType}#{slotIndex} ({itemId}) - {itemName}", inventoryType, slotIndex, baseItemId, itemRow.Name);
                    SelectItem(1, itemRow.RowId, itemRow.Icon, inventoryType, slotIndex);
                    isSeedSet = true;
                }

                if (isSoilSet && isSeedSet)
                    break;
            }
        }

        _framework.Update -= OnUpdate;
    }

    public void SelectItem(int index, uint itemId, int iconId, InventoryType inventoryType, ushort inventorySlot)
    {
        var agent = AgentHousingPlant.Instance();
        if (agent == null || !agent->IsAgentActive())
            return;

        var numberArrayData = AtkStage.Instance()->GetNumberArrayData(NumberArrayType.Housing);
        if (numberArrayData == null || numberArrayData->IntArray == null || numberArrayData->Size < 2087)
            return;

        agent->SelectedItems[index].InventoryType = inventoryType;
        agent->SelectedItems[index].InventorySlot = inventorySlot;
        agent->SelectedItems[index].ItemId = itemId;

        numberArrayData->SetValue(3 * index + 2081, iconId);
        numberArrayData->SetValue(3 * index + 2082, ((ushort)inventoryType << 16) | inventorySlot);

        InventoryManager.Instance()->SetSlotBlocked(inventoryType, (short)inventorySlot);
    }
}
