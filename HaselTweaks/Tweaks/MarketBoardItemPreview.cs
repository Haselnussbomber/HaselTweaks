using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Services;
using HaselCommon.Sheets;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;
using Microsoft.Extensions.Logging;

namespace HaselTweaks.Tweaks;

public sealed unsafe class MarketBoardItemPreview(
    ILogger<MarketBoardItemPreview> Logger,
    IAddonLifecycle AddonLifecycle,
    ExcelService ExcelService)
    : ITweak
{
    public string InternalName => nameof(MarketBoardItemPreview);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    public void OnInitialize() { }

    public void OnEnable()
    {
        AddonLifecycle.RegisterListener(AddonEvent.PostReceiveEvent, "ItemSearch", ItemSearch_PostReceiveEvent);
    }

    public void OnDisable()
    {
        AddonLifecycle.UnregisterListener(AddonEvent.PostReceiveEvent, "ItemSearch", ItemSearch_PostReceiveEvent);
    }

    void IDisposable.Dispose()
    {
        if (Status == TweakStatus.Disposed)
            return;

        OnDisable();

        Status = TweakStatus.Disposed;
        GC.SuppressFinalize(this);
    }

    private void ItemSearch_PostReceiveEvent(AddonEvent type, AddonArgs args)
    {
        if (args is not AddonReceiveEventArgs addonReceiveEventArgs || addonReceiveEventArgs.AtkEventType != (byte)AtkEventType.ListItemRollOver)
            return;

        var itemIndex = *(int*)(addonReceiveEventArgs.Data + 0x10);
        var realItemIndex = *(byte*)(args.Addon + itemIndex + 0x2D38);
        var itemId = *(uint*)(args.Addon + realItemIndex * 0x20 + 0x3258);
        Logger.LogTrace("Event: {atkEventData} {realItemIndex} {itemId}", itemIndex, realItemIndex, itemId);

        var item = ExcelService.GetRow<ExtendedItem>(itemId);
        if (item == null || !item.CanTryOn)
            return;

        AgentTryon.TryOn(((AtkUnitBase*)args.Addon)->Id, itemId, 0, 0, 0);
    }
}
