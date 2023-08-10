using Dalamud.ContextMenu;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Logging;
using HaselTweaks.Structs;
using HaselTweaks.Utils;
using AgentRecipeNote = HaselTweaks.Structs.AgentRecipeNote;

namespace HaselTweaks.Tweaks;

[Tweak(
    Name: "Search the markets",
    Description: @"Adds an entry to item context menus that allows you to quickly search for the item on the market board. Only visible when market board is open!

Supports context menus in the following windows:
- Chat
- Crafting Log
- Ehcatl Nine Delivery Quests (via /timers)
- Grand Company Delivery Missions (via /timers)
- Inventory
- Materials List
- Recipe Tree"
)]
public unsafe class SearchTheMarkets : Tweak
{
    private readonly DalamudContextMenu _contextMenu = new();
    private readonly GameObjectContextMenuItem _contextMenuItemGame = null!;
    private readonly InventoryContextMenuItem _contextMenuItemInventory = null!;

    private uint _itemId;

    public SearchTheMarkets()
    {
        try
        {
            var text = new SeStringBuilder()
                .AddUiForeground("\uE078 ", 32)
                .AddText(t("ItemSearch.SearchTheMarkets"))
                .BuiltString;

            _contextMenuItemGame = new(
                text,
                (_) => { ItemSearchUtils.Search(_itemId); _itemId = 0; },
                false);

            _contextMenuItemInventory = new(
                text,
                (_) => { ItemSearchUtils.Search(_itemId); _itemId = 0; },
                false);
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "Unable to construct context menu entries");
            Ready = false;
        }
    }

    public override void Enable()
    {
        _contextMenu.OnOpenGameObjectContextMenu += ContextMenu_OnOpenGameObjectContextMenu;
        _contextMenu.OnOpenInventoryContextMenu += ContextMenu_OnOpenInventoryContextMenu;
    }

    public override void Disable()
    {
        _contextMenu.OnOpenGameObjectContextMenu -= ContextMenu_OnOpenGameObjectContextMenu;
        _contextMenu.OnOpenInventoryContextMenu -= ContextMenu_OnOpenInventoryContextMenu;
    }

    public override void Dispose()
    {
        _contextMenu.Dispose();
    }

    private void ContextMenu_OnOpenGameObjectContextMenu(GameObjectContextMenuOpenArgs args)
    {
        if (args.ParentAddonName is not ("RecipeNote" or "RecipeMaterialList" or "RecipeTree" or "ChatLog" or "ContentsInfoDetail" or "DailyQuestSupply"))
            return;

        _itemId = 0u;

        switch (args.ParentAddonName)
        {
            case "RecipeNote":
                _itemId = GetAgent<AgentRecipeNote>()->ContextMenuResultItemId;
                break;

            case "RecipeTree":
            case "RecipeMaterialList":
                // see function "E8 ?? ?? ?? ?? 45 8B C4 41 8B D7" which is passing the uint (a2) to AgentRecipeItemContext
                _itemId = GetAgent<AgentRecipeItemContext>()->ResultItemId;
                break;

            case "ChatLog":
                _itemId = GetAgent<AgentChatLog>()->ContextItemId;
                break;

            case "ContentsInfoDetail":
                _itemId = GetAgent<AgentContentsTimer>()->ContextMenuItemId;
                break;

            case "DailyQuestSupply":
                _itemId = GetAgent<AgentDailyQuestSupply>()->ContextMenuItemId;
                break;
        }

        if (!ItemSearchUtils.CanSearchForItem(_itemId))
            return;

        args.AddCustomItem(_contextMenuItemGame);
    }

    private void ContextMenu_OnOpenInventoryContextMenu(InventoryContextMenuOpenArgs args)
    {
        _itemId = args.ItemId;

        if (!ItemSearchUtils.CanSearchForItem(_itemId))
            return;

        args.AddCustomItem(_contextMenuItemInventory);
    }
}
