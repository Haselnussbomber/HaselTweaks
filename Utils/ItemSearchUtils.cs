using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselTweaks.Caches;
using HaselTweaks.Structs;
using Lumina.Excel.GeneratedSheets;

namespace HaselTweaks.Utils;

public static unsafe class ItemSearchUtils
{
    public static bool CanSearchForItem(uint itemId)
    {
        if (itemId == 0) return false;
        var item = GetRow<Item>(itemId);
        return item != null && !item.IsUntradable && !item.IsCollectable && GetAddon(AgentId.ItemSearch) != null;
    }

    public static void Search(uint itemId)
    {
        if (!CanSearchForItem(itemId))
            return;

        if (!TryGetAddon<AddonItemSearch>(AgentId.ItemSearch, out var addon))
            return;

        if (TryGetAddon<AddonItemSearchResult>("ItemSearchResult", out var itemSearchResult))
            itemSearchResult->Hide2();

        var itemName = StringCache.GetItemName(itemId % 1000000);
        if (itemName.Length > 40)
            itemName = itemName[..40];

        addon->TextInput->AtkComponentInputBase.UnkText1.SetString(itemName);
        addon->TextInput->AtkComponentInputBase.UnkText2.SetString(itemName);
        addon->TextInput->UnkText1.SetString(itemName);
        addon->TextInput->UnkText2.SetString(itemName);

        addon->SetModeFilter(AddonItemSearch.SearchMode.Normal, -1);
        ((AtkComponentTextInput*)addon->TextInput)->TriggerRedraw();
        addon->RunSearch(false);
    }
}
