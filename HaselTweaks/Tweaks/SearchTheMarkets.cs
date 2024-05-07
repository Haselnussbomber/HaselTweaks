using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Sheets;
using HaselCommon.Utils;

namespace HaselTweaks.Tweaks;

[Tweak]
public unsafe class SearchTheMarkets : Tweak
{
    private MenuItem? MenuItem;
    private ExtendedItem? Item;

    public override void Enable()
    {
        MenuItem ??= new()
        {
            Name = t("ItemContextMenu.SearchTheMarkets"),
            Prefix = SeIconChar.BoxedLetterH,
            PrefixColor = 32,
            OnClicked = (_) =>
            {
                if (Item != null)
                {
                    ItemSearchUtils.Search(Item);
                    Item = null;
                }
            }
        };

        Service.ContextMenu.OnMenuOpened += ContextMenu_OnMenuOpened;
    }

    public override void Disable()
    {
        Service.ContextMenu.OnMenuOpened -= ContextMenu_OnMenuOpened;
    }

    public override void OnLanguageChange()
    {
        if (MenuItem != null)
            MenuItem.Name = t("ItemContextMenu.SearchTheMarkets");
    }

    private void ContextMenu_OnMenuOpened(MenuOpenedArgs args)
    {
        if (MenuItem == null)
            return;

        var itemId = args.AddonName switch
        {
            _ when args.Target is MenuTargetInventory inv => inv.TargetItem?.ItemId ?? 0,
            "RecipeNote" => GetAgent<AgentRecipeNote>()->ContextMenuResultItemId,
            "RecipeTree" or "RecipeMaterialList" or "RecipeProductList" => GetAgent<AgentRecipeItemContext>()->ResultItemId, // see function "E8 ?? ?? ?? ?? 45 8B C4 41 8B D7" which is passing the uint (a2) to AgentRecipeItemContext
            "ChatLog" => GetAgent<AgentChatLog>()->ContextItemId,
            "ContentsInfoDetail" => GetAgent<AgentContentsTimer>()->ContextMenuItemId,
            "DailyQuestSupply" => GetAgent<AgentDailyQuestSupply>()->ContextMenuItemId,
            _ => 0u,
        };

        if (itemId == 0)
            return;

        Item = GetRow<ExtendedItem>(itemId);

        if (Item == null || !Item.CanSearchForItem)
            return;

        args.AddMenuItem(MenuItem);
    }
}
