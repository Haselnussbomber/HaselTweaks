using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class OutfitGlamourTryOn : ITweak
{
    private readonly IContextMenu _contextMenu;
    private readonly ItemService _itemService;
    private readonly TextService _textService;

    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    public void OnInitialize() { }

    public void OnEnable()
    {
        _contextMenu.OnMenuOpened += ContextMenu_OnMenuOpened;
    }

    public void OnDisable()
    {
        _contextMenu.OnMenuOpened -= ContextMenu_OnMenuOpened;
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();

        Status = TweakStatus.Disposed;
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
