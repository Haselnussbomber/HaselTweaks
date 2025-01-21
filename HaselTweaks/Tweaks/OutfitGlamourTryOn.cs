using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Services;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;
using HaselTweaks.Structs.Agents;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append)]
public unsafe class OutfitGlamourTryOn(IContextMenu ContextMenu, ItemService ItemService, TextService TextService) : ITweak
{
    public string InternalName => nameof(OutfitGlamourTryOn);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    public void OnInitialize() { }

    public void OnEnable()
    {
        ContextMenu.OnMenuOpened += ContextMenu_OnMenuOpened;
    }

    public void OnDisable()
    {
        ContextMenu.OnMenuOpened -= ContextMenu_OnMenuOpened;
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();

        Status = TweakStatus.Disposed;
        GC.SuppressFinalize(this);
    }

    private void ContextMenu_OnMenuOpened(IMenuOpenedArgs args)
    {
        if (args.AddonName != "MiragePrismPrismSetConvert")
            return;

        var agent = (HaselAgentMiragePrismPrismSetConvert*)AgentModule.Instance()->GetAgentByInternalId(AgentId.MiragePrismPrismSetConvert);
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
            Name = TextService.GetAddonText(2426),
            IsEnabled = ItemService.CanTryOn(itemId),
            OnClicked = (args) => AgentTryon.TryOn(agent->AgentInterface.AddonId, itemId)
        });
    }
}
