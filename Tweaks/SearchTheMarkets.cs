using System.Text;
using Dalamud;
using Dalamud.ContextMenu;
using Dalamud.Game.Text.SeStringHandling;
using HaselTweaks.Caches;
using HaselTweaks.Structs;
using Lumina.Excel.GeneratedSheets;
using AgentId = FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentId;
using AgentRecipeNote = HaselTweaks.Structs.AgentRecipeNote;
using HaselAtkComponentTextInput = HaselTweaks.Structs.AtkComponentTextInput;

namespace HaselTweaks.Tweaks;

[Tweak(
    Name: "Search the markets",
    Description: "Adds a context menu entry to items in Chat, Crafting Log, Inventory, Materials List and Recipe Tree to quickly search for the item on the Market Board. Only visible when Market Board is open."
)]
public unsafe class SearchTheMarkets : Tweak
{
    private readonly DalamudContextMenu _contextMenu = new();
    private readonly GameObjectContextMenuItem _contextMenuItemGame = null!;
    private readonly InventoryContextMenuItem _contextMenuItemInventory = null!;

    private uint _itemId;

    public SearchTheMarkets()
    {
        var text = new SeStringBuilder()
            .AddUiForeground("\uE078 ", 32)
            .AddText(Service.ClientState.ClientLanguage switch
            {
                ClientLanguage.German => "Auf den M\u00e4rkten suchen",
                ClientLanguage.French => "Rechercher sur les marchés",
                ClientLanguage.Japanese => "市場で検索する",
                _ => "Search the markets"
            })
            .BuiltString;

        _contextMenuItemGame = new(text, (_) => Search(), false);
        _contextMenuItemInventory = new(text, (_) => Search(), false);
    }

    private bool IsInvalidState
    {
        get
        {
            var item = Service.Data.GetExcelSheet<Item>()?.GetRow(_itemId);
            return _itemId == 0 || item == null || item.IsUntradable || item.IsCollectable || GetAddon(AgentId.ItemSearch) == null;
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
        if (args.ParentAddonName is not ("RecipeNote" or "RecipeMaterialList" or "RecipeTree" or "ChatLog"))
            return;

        _itemId = 0u;

        switch (args.ParentAddonName)
        {
            case "RecipeNote":
                if (GetAgent<AgentRecipeNote>(AgentId.RecipeNote, out var agentRecipeNote))
                    _itemId = agentRecipeNote->ContextMenuResultItemId;
                break;

            case "RecipeTree":
            case "RecipeMaterialList":
                // see function "E8 ?? ?? ?? ?? 45 8B C4 41 8B D7" which is passing the uint (a2) to AgentRecipeItemContext
                if (GetAgent<AgentRecipeItemContext>(AgentId.RecipeItemContext, out var agentRecipeItemContext))
                    _itemId = agentRecipeItemContext->ResultItemId;
                break;

            case "ChatLog":
                if (GetAgent<AgentChatLog>(AgentId.ChatLog, out var agentChatLog))
                    _itemId = agentChatLog->ContextItemId;
                break;
        }

        if (IsInvalidState)
            return;

        args.AddCustomItem(_contextMenuItemGame);
    }

    private void ContextMenu_OnOpenInventoryContextMenu(InventoryContextMenuOpenArgs args)
    {
        _itemId = args.ItemId;

        if (IsInvalidState)
            return;

        args.AddCustomItem(_contextMenuItemInventory);
    }

    private void Search()
    {
        if (IsInvalidState)
            return;

        if (!GetAddon<AddonItemSearch>(AgentId.ItemSearch, out var addon))
            return;

        if (GetAddon<AddonItemSearchResult>("ItemSearchResult", out var itemSearchResult))
            itemSearchResult->Hide2();

        var itemName = StringCache.GetItemName(_itemId % 1000000);
        if (itemName.Length > 40)
            itemName = itemName[..40];

        var byteArray = Encoding.UTF8.GetBytes(itemName);
        fixed (byte* ptr = byteArray)
        {
            addon->TextInput->AtkComponentInputBase.UnkText1.SetString(ptr);
            addon->TextInput->AtkComponentInputBase.UnkText2.SetString(ptr);
            addon->TextInput->UnkText1.SetString(ptr);
            addon->TextInput->UnkText2.SetString(ptr);
        }

        addon->SetModeFilter(AddonItemSearch.SearchMode.Normal, 0xFFFFFFFF);
        ((HaselAtkComponentTextInput*)addon->TextInput)->TriggerRedraw();
        addon->RunSearch(false);

        _itemId = 0;
    }
}
