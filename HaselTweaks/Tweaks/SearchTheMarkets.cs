using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Services;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;
using Lumina.Excel.GeneratedSheets;

namespace HaselTweaks.Tweaks;

public unsafe class SearchTheMarkets(
    IContextMenu ContextMenu,
    TextService TextService,
    ExcelService ExcelService,
    ItemService ItemService) : ITweak
{
    public string InternalName => nameof(SearchTheMarkets);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    private MenuItem? MenuItem;
    private Item? Item;

    public void OnInitialize() { }

    public void OnEnable()
    {
        MenuItem ??= new()
        {
            Name = TextService.Translate("ItemContextMenu.SearchTheMarkets"),
            Prefix = SeIconChar.BoxedLetterH,
            PrefixColor = 32,
            OnClicked = (_) =>
            {
                if (Item != null)
                {
                    ItemService.Search(Item);
                    Item = null;
                }
            }
        };

        ContextMenu.OnMenuOpened += ContextMenu_OnMenuOpened;
        TextService.LanguageChanged += OnLanguageChange;
    }

    public void OnDisable()
    {
        ContextMenu.OnMenuOpened -= ContextMenu_OnMenuOpened;
        TextService.LanguageChanged -= OnLanguageChange;
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();

        Status = TweakStatus.Disposed;
        GC.SuppressFinalize(this);
    }

    private void OnLanguageChange(string langCode)
    {
        if (MenuItem != null)
            MenuItem.Name = TextService.Translate("ItemContextMenu.SearchTheMarkets");
    }

    private void ContextMenu_OnMenuOpened(IMenuOpenedArgs args)
    {
        if (MenuItem == null)
            return;

        var itemId = args.AddonName switch
        {
            _ when args.Target is MenuTargetInventory inv => inv.TargetItem?.ItemId ?? 0,
            "RecipeNote" => AgentRecipeNote.Instance()->ContextMenuResultItemId,
            "RecipeTree" or "RecipeMaterialList" or "RecipeProductList" => AgentRecipeItemContext.Instance()->ResultItemId, // see function "E8 ?? ?? ?? ?? 45 8B C4 41 8B D7" which is passing the uint (a2) to AgentRecipeItemContext
            "ChatLog" => AgentChatLog.Instance()->ContextItemId,
            "ContentsInfoDetail" => AgentContentsTimer.Instance()->ContextMenuItemId,
            "DailyQuestSupply" => AgentDailyQuestSupply.Instance()->ContextMenuItemId,
            _ => 0u,
        };

        if (itemId == 0)
            return;

        Item = ExcelService.GetRow<Item>(itemId);

        if (Item == null || !ItemService.CanSearchForItem(Item))
            return;

        args.AddMenuItem(MenuItem);
    }
}
