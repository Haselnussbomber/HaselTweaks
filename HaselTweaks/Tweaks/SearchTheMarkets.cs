using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class SearchTheMarkets : Tweak
{
    private readonly IContextMenu _contextMenu;
    private readonly LanguageProvider _languageProvider;
    private readonly TextService _textService;
    private readonly ItemService _itemService;

    private MenuItem? _menuItem;
    private ItemHandle _item;

    public override void OnEnable()
    {
        _menuItem ??= new()
        {
            Name = _textService.Translate("ItemContextMenu.SearchTheMarkets"),
            Prefix = SeIconChar.BoxedLetterH,
            PrefixColor = 32,
            OnClicked = (_) =>
            {
                AgentItemSearch.Instance()->SearchForItem(_item);
                _item = 0;
            }
        };

        _contextMenu.OnMenuOpened += ContextMenu_OnMenuOpened;
        _languageProvider.LanguageChanged += OnLanguageChange;
    }

    public override void OnDisable()
    {
        _contextMenu.OnMenuOpened -= ContextMenu_OnMenuOpened;
        _languageProvider.LanguageChanged -= OnLanguageChange;

        _menuItem = null;
    }

    private void OnLanguageChange(string langCode)
    {
        if (_menuItem != null)
            _menuItem.Name = _textService.Translate("ItemContextMenu.SearchTheMarkets");
    }

    private void ContextMenu_OnMenuOpened(IMenuOpenedArgs args)
    {
        if (_menuItem == null)
            return;

        ItemHandle item = args.AddonName switch
        {
            _ when args.Target is MenuTargetInventory inv => inv.TargetItem?.ItemId ?? 0,
            "GatheringNote" => AgentGatheringNote.Instance()->ContextMenuItemId,
            "RecipeNote" => AgentRecipeNote.Instance()->ContextMenuResultItemId,
            "RecipeTree" or "RecipeMaterialList" or "RecipeProductList" => AgentRecipeItemContext.Instance()->ResultItemId, // see function "E8 ?? ?? ?? ?? 45 8B C4 41 8B D7" which is passing the uint (a2) to AgentRecipeItemContext
            "ChatLog" => AgentChatLog.Instance()->ContextItemId,
            "ContentsInfoDetail" => AgentContentsTimer.Instance()->ContextMenuItemId,
            "DailyQuestSupply" => AgentDailyQuestSupply.Instance()->ContextMenuItemId,
            _ => 0u,
        };

        if (item.IsEmpty || item.IsEventItem)
            return;

        if (!AgentItemSearch.Instance()->CanSearchForItem(item))
            return;

        _item = item;

        args.AddMenuItem(_menuItem);
    }
}
