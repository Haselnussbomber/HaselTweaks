using System;
using System.Text;
using Dalamud;
using Dalamud.ContextMenu;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Structs;
using HaselTweaks.Utils;
using Lumina.Excel.GeneratedSheets;

namespace HaselTweaks.Tweaks;

public unsafe class SearchTheMarkets : Tweak
{
    public override string Name => "Search the markets";
    public override string Description => "Adds a context menu entry to items in Inventory, Crafting Log, Recipe Tree or Materials List to quickly search for it on the Market Board. Only visible when Market Board is open.";

    [Signature("E8 ?? ?? ?? ?? 48 8B DE 48 8D BC 24")]
    private RunSearchDelegate RunSearch { get; init; } = null!;
    private delegate void RunSearchDelegate(AddonItemSearch* addon, bool a2);

    [Signature("E8 ?? ?? ?? ?? EB 40 41 8D 40 FD")]
    private SetModeFilterDelegate SetModeFilter { get; init; } = null!;
    private delegate void SetModeFilterDelegate(AddonItemSearch* addon, AddonItemSearch.SearchMode mode, uint filter);

    [Signature("E8 ?? ?? ?? ?? 48 0F BF 56")]
    private TriggerRedrawDelegate TriggerRedraw { get; init; } = null!;
    private delegate IntPtr TriggerRedrawDelegate(AtkComponentTextInput* self);

    private readonly DalamudContextMenu ContextMenu = null!;
    private readonly GameObjectContextMenuItem ContextMenuItemGame = null!;
    private readonly InventoryContextMenuItem ContextMenuItemInventory = null!;

    private Item? Item = null;

    public SearchTheMarkets()
    {
        this.ContextMenu = new();

        var text = new SeString(new TextPayload(Service.ClientState.ClientLanguage switch
        {
            ClientLanguage.German => "Auf den M\u00e4rkten suchen",
            // ClientLanguage.French => "",
            // ClientLanguage.Japanese => "",
            _ => "Search the markets"
        }));

        this.ContextMenuItemGame = new(text, this.ActionGame, false);
        this.ContextMenuItemInventory = new(text, this.ActionInventory, false);
    }

    public override void Enable()
    {
        this.ContextMenu.OnOpenGameObjectContextMenu += this.ContextMenu_OnOpenGameObjectContextMenu;
        this.ContextMenu.OnOpenInventoryContextMenu += this.ContextMenu_OnOpenInventoryContextMenu;
    }

    public override void Disable()
    {
        this.ContextMenu.OnOpenGameObjectContextMenu -= this.ContextMenu_OnOpenGameObjectContextMenu;
        this.ContextMenu.OnOpenInventoryContextMenu -= this.ContextMenu_OnOpenInventoryContextMenu;
    }

    public override void Dispose()
    {
        this.ContextMenu.Dispose();
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
            // see function "E8 ?? ?? ?? ?? 89 2B 48 8B C3" which is passing 3 uints to Agent 257 (a1)
            var agent = Framework.Instance()->GetUiModule()->GetAgentModule()->GetAgentByInternalID(257);
            itemId = *(uint*)((IntPtr)agent + 0x28);
        }

        this.Item = Service.Data.GetExcelSheet<Item>()?.GetRow(itemId);

        if (InvalidState())
        {
            return;
        }

        args.AddCustomItem(this.ContextMenuItemGame);
    }

    private void ContextMenu_OnOpenInventoryContextMenu(InventoryContextMenuOpenArgs args)
    {
        this.Item = Service.Data.GetExcelSheet<Item>()?.GetRow(args.ItemId);

        if (InvalidState())
        {
            return;
        
        }
        args.AddCustomItem(this.ContextMenuItemInventory);
    }

    private void ActionGame(GameObjectContextMenuItemSelectedArgs args)
    {
        Search();
    }

    private void ActionInventory(InventoryContextMenuItemSelectedArgs args)
    {
        Search();
    }

    private void Search()
    {
        if (InvalidState())
        {
            return;
        }

        var itemSearch = (AddonItemSearch*)AtkUtils.GetUnitBase("ItemSearch");

        var itemName = this.Item!.Name.ToString();
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

        this.SetModeFilter(itemSearch, AddonItemSearch.SearchMode.Normal, 0xFFFFFFFF);
        this.TriggerRedraw(itemSearch->TextInput);
        this.RunSearch(itemSearch, false);
    }

    private bool InvalidState()
    {
        return this.Item == null || this.Item.IsUntradable || AtkUtils.GetUnitBase("ItemSearch") == null;
    }
}
