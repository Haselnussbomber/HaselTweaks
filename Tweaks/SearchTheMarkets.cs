using System.Text;
using Dalamud;
using Dalamud.ContextMenu;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselTweaks.Structs;
using HaselTweaks.Utils;
using Lumina.Excel.GeneratedSheets;
using HaselAtkComponentTextInput = HaselTweaks.Structs.AtkComponentTextInput;

namespace HaselTweaks.Tweaks;

public unsafe class SearchTheMarkets : Tweak
{
    public override string Name => "Search the markets";
    public override string Description => "Adds a context menu entry to items in Inventory, Crafting Log, Recipe Tree or Materials List to quickly search for the item on the Market Board. Only visible when Market Board is open.";

    private readonly DalamudContextMenu ContextMenu = null!;
    private readonly GameObjectContextMenuItem ContextMenuItemGame = null!;
    private readonly InventoryContextMenuItem ContextMenuItemInventory = null!;

    private Item? Item = null;

    public SearchTheMarkets()
    {
        ContextMenu = new();

        var text = new SeString(new TextPayload(Service.ClientState.ClientLanguage switch
        {
            ClientLanguage.German => "Auf den M\u00e4rkten suchen",
            // ClientLanguage.French => "",
            // ClientLanguage.Japanese => "",
            _ => "Search the markets"
        }));

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

    private unsafe void ContextMenu_OnOpenGameObjectContextMenu(GameObjectContextMenuOpenArgs args)
    {
        if (args.ParentAddonName is not ("RecipeNote" or "RecipeMaterialList" or "RecipeTree"))
        {
            return;
        }

        var itemId = 0u;

        if (args.ParentAddonName is "RecipeNote")
        {
            var agent = (AgentRecipeNote*)Framework.Instance()->GetUiModule()->GetAgentModule()->GetAgentByInternalId(AgentId.RecipeNote);
            itemId = *(uint*)((IntPtr)agent + 0x398);
        }
        else if (args.ParentAddonName is "RecipeMaterialList" or "RecipeTree")
        {
            // see function "E8 ?? ?? ?? ?? 45 8B C4 41 8B D7" which is passing the uint to Agent 262 (a1)
            var agent = Framework.Instance()->GetUiModule()->GetAgentModule()->GetAgentByInternalId(AgentId.RecipeItemContext);
            itemId = *(uint*)((IntPtr)agent + 0x28);
        }

        Item = Service.Data.GetExcelSheet<Item>()?.GetRow(itemId);

        if (InvalidState())
        {
            return;
        }

        args.AddCustomItem(ContextMenuItemGame);
    }

    private void ContextMenu_OnOpenInventoryContextMenu(InventoryContextMenuOpenArgs args)
    {
        Item = Service.Data.GetExcelSheet<Item>()?.GetRow(args.ItemId);

        if (InvalidState())
        {
            return;
        }

        args.AddCustomItem(ContextMenuItemInventory);
    }

    private void Search()
    {
        if (InvalidState())
        {
            return;
        }

        var itemSearch = (AddonItemSearch*)AtkUtils.GetUnitBase("ItemSearch");

        var itemName = Item!.Name.ToString();
        if (itemName.Length > 40)
        {
            itemName = itemName[..40];
        }

        var byteArray = Encoding.UTF8.GetBytes(itemName);
        fixed (byte* ptr = byteArray)
        {
            itemSearch->TextInput->AtkComponentInputBase.UnkText1.SetString(ptr);
            itemSearch->TextInput->AtkComponentInputBase.UnkText2.SetString(ptr);
            itemSearch->TextInput->UnkText1.SetString(ptr);
            itemSearch->TextInput->UnkText2.SetString(ptr);
        }

        itemSearch->SetModeFilter(AddonItemSearch.SearchMode.Normal, 0xFFFFFFFF);
        ((HaselAtkComponentTextInput*)itemSearch->TextInput)->TriggerRedraw();
        itemSearch->RunSearch(false);
    }

    private bool InvalidState()
    {
        return Item == null || Item.RowId == 0 || Item.IsUntradable || AtkUtils.GetUnitBase("ItemSearch") == null;
    }
}
