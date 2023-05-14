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

public unsafe class SearchTheMarkets : Tweak
{
    public override string Name => "Search the markets";
    public override string Description => "Adds a context menu entry to items in Chat, Crafting Log, Inventory, Materials List and Recipe Tree to quickly search for the item on the Market Board. Only visible when Market Board is open.";

    private readonly DalamudContextMenu ContextMenu = new();
    private GameObjectContextMenuItem ContextMenuItemGame = null!;
    private InventoryContextMenuItem ContextMenuItemInventory = null!;

    private uint ItemId;

    private bool IsInvalidState
    {
        get
        {
            var item = Service.Data.GetExcelSheet<Item>()?.GetRow(ItemId);
            return ItemId == 0 || item == null || item.IsUntradable || item.IsCollectable || GetAddon(AgentId.ItemSearch) == null;
        }
    }

    public override void Setup()
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

        ContextMenuItemGame = new(text, (_) => Search(), false);
        ContextMenuItemInventory = new(text, (_) => Search(), false);
    }

    public override void Enable()
    {
        ContextMenu.OnOpenGameObjectContextMenu += ContextMenu_OnOpenGameObjectContextMenu;
        ContextMenu.OnOpenInventoryContextMenu += ContextMenu_OnOpenInventoryContextMenu;
    }

    public override void Disable()
    {
        ContextMenu.OnOpenGameObjectContextMenu -= ContextMenu_OnOpenGameObjectContextMenu;
        ContextMenu.OnOpenInventoryContextMenu -= ContextMenu_OnOpenInventoryContextMenu;
    }

    public override void Dispose()
    {
        ContextMenu.Dispose();
    }

    private void ContextMenu_OnOpenGameObjectContextMenu(GameObjectContextMenuOpenArgs args)
    {
        if (args.ParentAddonName is not ("RecipeNote" or "RecipeMaterialList" or "RecipeTree" or "ChatLog"))
            return;

        ItemId = 0u;

        switch (args.ParentAddonName)
        {
            case "RecipeNote":
                if (GetAgent<AgentRecipeNote>(AgentId.RecipeNote, out var agentRecipeNote))
                    ItemId = agentRecipeNote->ContextMenuResultItemId;
                break;

            case "RecipeTree":
            case "RecipeMaterialList":
                // see function "E8 ?? ?? ?? ?? 45 8B C4 41 8B D7" which is passing the uint (a2) to AgentRecipeItemContext
                if (GetAgent<AgentRecipeItemContext>(AgentId.RecipeItemContext, out var agentRecipeItemContext))
                    ItemId = agentRecipeItemContext->ResultItemId;
                break;

            case "ChatLog":
                if (GetAgent<AgentChatLog>(AgentId.ChatLog, out var agentChatLog))
                    ItemId = agentChatLog->ContextItemId;
                break;
        }

        if (IsInvalidState)
            return;

        args.AddCustomItem(ContextMenuItemGame);
    }

    private void ContextMenu_OnOpenInventoryContextMenu(InventoryContextMenuOpenArgs args)
    {
        ItemId = args.ItemId;

        if (IsInvalidState)
            return;

        args.AddCustomItem(ContextMenuItemInventory);
    }

    private void Search()
    {
        if (IsInvalidState)
            return;

        if (!GetAddon<AddonItemSearch>(AgentId.ItemSearch, out var addon))
            return;

        if (GetAddon<AddonItemSearchResult>("ItemSearchResult", out var itemSearchResult))
            itemSearchResult->Hide2();

        var itemName = StringCache.GetItemName(ItemId % 1000000);
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

        ItemId = 0;
    }
}
