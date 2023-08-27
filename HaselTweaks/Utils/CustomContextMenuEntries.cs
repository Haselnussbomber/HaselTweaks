using static HaselCommon.Utils.ImGuiContextMenu;

namespace HaselTweaks.Utils;

public static class CustomContextMenuEntries
{
    public static ContextMenuEntry CreateItemSearch(uint ItemId)
        => new()
        {
            Visible = ItemSearchUtils.CanSearchForItem(ItemId),
            Label = t("ItemContextMenu.SearchTheMarkets"),
            LoseFocusOnClick = true,
            ClickCallback = () =>
            {
                ItemSearchUtils.Search(ItemId);
            }
        };
}
