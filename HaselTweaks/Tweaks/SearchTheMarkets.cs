using Dalamud.ContextMenu;
using Dalamud.Game.Text.SeStringHandling;
using HaselCommon.Sheets;
using HaselCommon.Utils;
using HaselTweaks.Structs;
using AgentChatLog = HaselTweaks.Structs.AgentChatLog;
using AgentRecipeNote = HaselTweaks.Structs.AgentRecipeNote;

namespace HaselTweaks.Tweaks;

[Tweak]
public unsafe class SearchTheMarkets : Tweak
{
    private readonly DalamudContextMenu _contextMenu;
    private GameObjectContextMenuItem? _contextMenuItemGame;
    private InventoryContextMenuItem? _contextMenuItemInventory;

    private ExtendedItem? _item = null;

    public SearchTheMarkets()
    {
        _contextMenu = new(Service.PluginInterface);
    }

    public override void Enable()
    {
        SetupContextMenus();
        _contextMenu.OnOpenGameObjectContextMenu += ContextMenu_OnOpenGameObjectContextMenu;
        _contextMenu.OnOpenInventoryContextMenu += ContextMenu_OnOpenInventoryContextMenu;
    }

    public override void Disable()
    {
        _contextMenu.OnOpenGameObjectContextMenu -= ContextMenu_OnOpenGameObjectContextMenu;
        _contextMenu.OnOpenInventoryContextMenu -= ContextMenu_OnOpenInventoryContextMenu;
    }

    public override void OnLanguageChange()
    {
        SetupContextMenus();
    }

    public override void Dispose()
    {
        _contextMenu?.Dispose();
    }

    private void SetupContextMenus()
    {
        try
        {
            var text = new SeStringBuilder()
                .AddUiForeground("\uE078 ", 32)
                .AddText(t("ItemContextMenu.SearchTheMarkets"))
                .BuiltString;

            _contextMenuItemGame = new(
                text,
                (_) => { ItemSearchUtils.Search(_item!); _item = null; },
                false);

            _contextMenuItemInventory = new(
                text,
                (_) => { ItemSearchUtils.Search(_item!); _item = null; },
                false);
        }
        catch (Exception ex)
        {
            Error(ex, "Unable to construct context menu entries");
            Ready = false;
        }
    }

    private void ContextMenu_OnOpenGameObjectContextMenu(GameObjectContextMenuOpenArgs args)
    {
        if (args.ParentAddonName is not ("RecipeNote" or "RecipeMaterialList" or "RecipeProductList" or "RecipeTree" or "ChatLog" or "ContentsInfoDetail" or "DailyQuestSupply"))
            return;

        _item = null;

        var itemId = 0u;
        switch (args.ParentAddonName)
        {
            case "RecipeNote":
                itemId = GetAgent<AgentRecipeNote>()->ContextMenuResultItemId;
                break;

            case "RecipeTree":
            case "RecipeMaterialList":
            case "RecipeProductList":
                // see function "E8 ?? ?? ?? ?? 45 8B C4 41 8B D7" which is passing the uint (a2) to AgentRecipeItemContext
                itemId = GetAgent<AgentRecipeItemContext>()->ResultItemId;
                break;

            case "ChatLog":
                itemId = GetAgent<AgentChatLog>()->ContextItemId;
                break;

            case "ContentsInfoDetail":
                itemId = GetAgent<AgentContentsTimer>()->ContextMenuItemId;
                break;

            case "DailyQuestSupply":
                itemId = GetAgent<AgentDailyQuestSupply>()->ContextMenuItemId;
                break;
        }

        if (itemId == 0)
            return;

        _item = GetRow<ExtendedItem>(itemId);

        if (_item == null || !_item.CanSearchForItem)
            return;

        args.AddCustomItem(_contextMenuItemGame);
    }

    private void ContextMenu_OnOpenInventoryContextMenu(InventoryContextMenuOpenArgs args)
    {
        if (args.ItemId == 0)
            return;

        _item = GetRow<ExtendedItem>(args.ItemId);

        if (_item == null || !_item.CanSearchForItem)
            return;

        args.AddCustomItem(_contextMenuItemInventory);
    }
}
