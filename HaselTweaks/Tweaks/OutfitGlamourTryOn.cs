using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class OutfitGlamourTryOn : Tweak
{
    private readonly IContextMenu _contextMenu;
    private readonly ItemService _itemService;
    private readonly TextService _textService;

    public override void OnEnable()
    {
        _contextMenu.OnMenuOpened += ContextMenu_OnMenuOpened;
    }

    public override void OnDisable()
    {
        _contextMenu.OnMenuOpened -= ContextMenu_OnMenuOpened;
    }

    private void ContextMenu_OnMenuOpened(IMenuOpenedArgs args)
    {
        if (args.AddonName != "MiragePrismPrismSetConvert")
            return;

        var agent = (AgentMiragePrismPrismSetConvert*)AgentModule.Instance()->GetAgentByInternalId(AgentId.MiragePrismPrismSetConvert);
        if (agent == null || agent->Data == null)
            return;

        var data = agent->Data;
        if (data->ContextMenuItemIndex > data->Items.Length || data->ContextMenuItemIndex > data->NumItemsInSet)
            return;

        var itemId = data->Items[data->ContextMenuItemIndex].ItemId;

        args.AddMenuItem(new MenuItem()
        {
            Prefix = SeIconChar.BoxedLetterH,
            PrefixColor = 32,
            Name = _textService.GetAddonText(2426),
            IsEnabled = _itemService.CanTryOn(itemId),
            OnClicked = (args) => AgentTryon.TryOn(agent->AgentInterface.AddonId, itemId)
        });
    }
}
