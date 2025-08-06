using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class FixInventoryOpenTab : Tweak
{
    private readonly IAddonLifecycle _addonLifecycle;

    public override void OnEnable()
    {
        _addonLifecycle.RegisterListener(AddonEvent.PreRefresh, ["Inventory", "InventoryLarge", "InventoryExpansion"], OnPreRefresh);
    }

    public override void OnDisable()
    {
        _addonLifecycle.UnregisterListener(AddonEvent.PreRefresh, ["Inventory", "InventoryLarge", "InventoryExpansion"], OnPreRefresh);
    }

    private void OnPreRefresh(AddonEvent type, AddonArgs args)
    {
        if (args is not AddonRefreshArgs refreshArgs || refreshArgs.AtkValues == 0 || refreshArgs.AtkValueCount == 0)
            return;

        var addon = (AtkUnitBase*)args.Addon.Address;
        if (addon->IsVisible)
            return; // Skipping: Addon is visible (using games logic)

        if (GetTabIndex(addon) == 0)
            return; // Skipping: TabIndex already 0 (nothing to do)

        var values = new Span<AtkValue>((void*)refreshArgs.AtkValues, (int)refreshArgs.AtkValueCount);
        if (values[0].Type != ValueType.Int)
            return; // Skipping: value[0] is not int (invalid)

        if (values[0].Int == 6)
            return; // Skipping: value[0] is 6 (means it requested to open key items)

        ResetTabIndex(addon);
    }

    private int GetTabIndex(AtkUnitBase* addon)
    {
        return addon->NameString switch
        {
            "Inventory" => ((AddonInventory*)addon)->TabIndex,
            "InventoryLarge" => ((AddonInventoryLarge*)addon)->TabIndex,
            "InventoryExpansion" => ((AddonInventoryExpansion*)addon)->TabIndex,
            _ => 0,
        };
    }

    private void ResetTabIndex(AtkUnitBase* addon)
    {
        _logger.LogTrace("[{addonName}] [PreRefresh] Resetting tab to 0", addon->NameString);

        switch (addon->NameString)
        {
            case "Inventory": ((AddonInventory*)addon)->SetTab(0); break;
            case "InventoryLarge": ((AddonInventoryLarge*)addon)->SetTab(0); break;
            case "InventoryExpansion": ((AddonInventoryExpansion*)addon)->SetTab(0, false); break;
        }
    }
}
