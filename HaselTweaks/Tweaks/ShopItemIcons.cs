using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;
using HaselCommon.Services;
using HaselTweaks.Config;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace HaselTweaks.Tweaks;

public unsafe partial class ShopItemIcons(PluginConfig PluginConfig, ConfigGui ConfigGui, ItemService ItemService, IAddonLifecycle AddonLifecycle) : IConfigurableTweak
{
    public string InternalName => nameof(ShopItemIcons);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;
    private ShopItemIconsConfiguration Config => PluginConfig.Tweaks.ShopItemIcons;

    public void OnInitialize() { }

    public void OnEnable()
    {
        AddonLifecycle.RegisterListener(AddonEvent.PreSetup, "Shop", OnShopPreSetup);
        AddonLifecycle.RegisterListener(AddonEvent.PreRefresh, "Shop", OnShopPreRefresh);
        AddonLifecycle.RegisterListener(AddonEvent.PreRefresh, ["ShopExchangeItem", "ShopExchangeCurrency"], OnShopExchangePreRefresh);
        AddonLifecycle.RegisterListener(AddonEvent.PreRefresh, "GrandCompanyExchange", OnGrandCompanyExchangePreRefresh);
        AddonLifecycle.RegisterListener(AddonEvent.PreRefresh, "InclusionShop", OnInclusionShopPreRefresh);
        AddonLifecycle.RegisterListener(AddonEvent.PreRefresh, "FreeShop", OnFreeShopPreRefresh);
    }

    public void OnDisable()
    {
        AddonLifecycle.UnregisterListener(AddonEvent.PreSetup, "Shop", OnShopPreSetup);
        AddonLifecycle.UnregisterListener(AddonEvent.PreRefresh, "Shop", OnShopPreRefresh);
        AddonLifecycle.UnregisterListener(AddonEvent.PreRefresh, ["ShopExchangeItem", "ShopExchangeCurrency"], OnShopExchangePreRefresh);
        AddonLifecycle.UnregisterListener(AddonEvent.PreRefresh, "GrandCompanyExchange", OnGrandCompanyExchangePreRefresh);
        AddonLifecycle.UnregisterListener(AddonEvent.PreRefresh, "InclusionShop", OnInclusionShopPreRefresh);
        AddonLifecycle.UnregisterListener(AddonEvent.PreRefresh, "FreeShop", OnFreeShopPreRefresh);
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();

        Status = TweakStatus.Disposed;
        GC.SuppressFinalize(this);
    }

    private void OnShopPreSetup(AddonEvent type, AddonArgs args)
    {
        if (!Config.HandleShop || args is not AddonSetupArgs setupArgs)
            return;

        UpdateShopIcons(setupArgs.AtkValues, setupArgs.AtkValueCount);
    }

    private void OnShopPreRefresh(AddonEvent type, AddonArgs args)
    {
        if (!Config.HandleShop || args is not AddonRefreshArgs refreshArgs)
            return;

        UpdateShopIcons(refreshArgs.AtkValues, refreshArgs.AtkValueCount);
    }

    private void UpdateShopIcons(nint atkValues, uint atkValueCount)
    {
        var handler = ShopEventHandler.AgentProxy.Instance()->Handler;
        if (handler == null)
            return;

        var values = new Span<AtkValue>((void*)atkValues, (int)atkValueCount);
        var tabIndexValue = values.GetPointer(0);
        if (tabIndexValue->Type != ValueType.UInt)
            return;

        var tabIndex = tabIndexValue->UInt;
        const int IconIdOffset = 197;

        // Buy
        if (tabIndex == 0)
        {
            for (var i = 0; i < handler->VisibleItemsCount; i++)
            {
                var iconIdValue = values.GetPointer(IconIdOffset + i);
                if (iconIdValue->Type != ValueType.UInt)
                    continue;

                var itemIndex = handler->VisibleItems.GetPointer(i);
                var itemId = handler->Items.GetPointer(i)->ItemId;
                iconIdValue->UInt = ItemService.GetIconId(itemId);
            }
        }
        // Buyback
        else if (tabIndex == 1)
        {
            for (var i = 0; i < handler->BuybackCount; i++)
            {
                var iconIdValue = values.GetPointer(IconIdOffset + i);
                if (iconIdValue->Type != ValueType.UInt)
                    continue;

                var itemId = handler->Buyback.GetPointer(i)->ItemId;
                iconIdValue->UInt = ItemService.GetIconId(itemId);
            }
        }
    }

    private void OnShopExchangePreRefresh(AddonEvent type, AddonArgs args)
    {
        if (args is not AddonRefreshArgs refreshArgs)
            return;

        if ((args.AddonName == "ShopExchangeItem" && !Config.HandleShopExchangeItem) ||
            (args.AddonName == "ShopExchangeCurrency" && !Config.HandleShopExchangeCurrency))
        {
            return;
        }

        var values = new Span<AtkValue>((void*)refreshArgs.AtkValues, (int)refreshArgs.AtkValueCount);

        for (var i = 0; i < 60; i++)
        {
            var itemIdValue = values.GetPointer(1063 + i);
            var iconIdValue = values.GetPointer(209 + i);

            if (itemIdValue->Type != ValueType.UInt || iconIdValue->Type != ValueType.Int || itemIdValue->UInt == 0)
                continue;

            iconIdValue->UInt = ItemService.GetIconId(itemIdValue->UInt);
        }
    }

    private void OnGrandCompanyExchangePreRefresh(AddonEvent type, AddonArgs args)
    {
        if (!Config.HandleGrandCompanyExchange || args is not AddonRefreshArgs refreshArgs)
            return;

        // sometimes it refreshes with just 10 values
        if (refreshArgs.AtkValueCount != 556)
            return;

        var values = new Span<AtkValue>((void*)refreshArgs.AtkValues, (int)refreshArgs.AtkValueCount);

        // last function called in GCShopEventHandler_vf48
        for (var i = 0; i < 50; i++)
        {
            var itemIdValue = values.GetPointer(317 + i);
            var iconIdValue = values.GetPointer(167 + i);

            if (itemIdValue->Type != ValueType.UInt || iconIdValue->Type != ValueType.UInt)
                continue;

            iconIdValue->UInt = ItemService.GetIconId(itemIdValue->UInt);
        }
    }

    private void OnInclusionShopPreRefresh(AddonEvent type, AddonArgs args)
    {
        if (!Config.HandleInclusionShop || args is not AddonRefreshArgs refreshArgs)
            return;

        var values = new Span<AtkValue>((void*)refreshArgs.AtkValues, (int)refreshArgs.AtkValueCount);
        // "E8 ?? ?? ?? ?? 89 9D ?? ?? ?? ?? 8B FE"
        var itemCount = values.GetPointer(298)->UInt;
        for (var i = 0; i < itemCount; i++)
        {
            var itemIdValue = values.GetPointer(300 + i * 18);
            var iconIdValue = values.GetPointer(300 + i * 18 + 1);

            if (itemIdValue->Type != ValueType.UInt || iconIdValue->Type != ValueType.UInt)
                continue;

            iconIdValue->UInt = ItemService.GetIconId(itemIdValue->UInt);
        }
    }

    private void OnFreeShopPreRefresh(AddonEvent type, AddonArgs args)
    {
        if (!Config.HandleFreeShop || args is not AddonRefreshArgs refreshArgs)
            return;

        var values = new Span<AtkValue>((void*)refreshArgs.AtkValues, (int)refreshArgs.AtkValueCount);
        var itemCount = values.GetPointer(3)->UInt;
        for (var i = 0; i < itemCount; i++)
        {
            var itemIdValue = values.GetPointer(65 + i);
            var iconIdValue = values.GetPointer(126 + i);

            if (itemIdValue->Type != ValueType.UInt || iconIdValue->Type != ValueType.UInt)
                continue;

            iconIdValue->UInt = ItemService.GetIconId(itemIdValue->UInt);
        }
    }
}
