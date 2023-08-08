using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Caches;
using HaselTweaks.Structs;
using Lumina.Excel.GeneratedSheets;
using HaselAtkComponentTextInput = HaselTweaks.Structs.AtkComponentTextInput;

namespace HaselTweaks.Utils;

public static unsafe class ItemSearchUtils
{
    public static bool CanSearchForItem(uint itemId)
    {
        if (itemId == 0) return false;
        var item = GetRow<Item>(itemId);
        return item != null && !item.IsUntradable && !item.IsCollectable && IsAddonOpen(AgentId.ItemSearch);
    }

    public static void Search(uint itemId)
    {
        if (!CanSearchForItem(itemId))
            return;

        if (!TryGetAddon<AddonItemSearch>(AgentId.ItemSearch, out var addon))
            return;

        if (TryGetAddon<AtkUnitBase>("ItemSearchResult", out var itemSearchResult))
            itemSearchResult->Hide2();

        var itemName = StringCache.GetItemName(itemId % 1000000);
        if (itemName.Length > 40)
            itemName = itemName[..40];

        addon->TextInput->AtkComponentInputBase.UnkText1.SetString(itemName);
        addon->TextInput->AtkComponentInputBase.UnkText2.SetString(itemName);
        addon->TextInput->UnkText1.SetString(itemName);
        addon->TextInput->UnkText2.SetString(itemName);

        addon->SetModeFilter(AddonItemSearch.SearchMode.Normal, -1);
        ((HaselAtkComponentTextInput*)addon->TextInput)->TriggerRedraw();
        addon->RunSearch(false);
    }
}
