using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Services;
using HaselCommon.Sheets;
using HaselCommon.Utils;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;

namespace HaselTweaks.Tweaks;

public sealed unsafe class SearchTheMarkets(
    IContextMenu ContextMenu,
    TranslationManager TranslationManager,
    TextService TextService,
    ExcelService ExcelService) : ITweak
{
    private MenuItem? MenuItem;
    private ExtendedItem? Item;

    public string InternalName => nameof(SearchTheMarkets);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

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
                    ItemSearchUtils.Search(Item);
                    Item = null;
                }
            }
        };

        ContextMenu.OnMenuOpened += ContextMenu_OnMenuOpened;
        TranslationManager.LanguageChanged += OnLanguageChange;
    }

    public void OnDisable()
    {
        ContextMenu.OnMenuOpened -= ContextMenu_OnMenuOpened;
        TranslationManager.LanguageChanged -= OnLanguageChange;
    }

    void IDisposable.Dispose()
    {
        if (Status == TweakStatus.Disposed)
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

    private void ContextMenu_OnMenuOpened(MenuOpenedArgs args)
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

        Item = ExcelService.GetRow<ExtendedItem>(itemId);

        if (Item == null || !Item.CanSearchForItem)
            return;

        args.AddMenuItem(MenuItem);
    }
}
