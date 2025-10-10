using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class ShopItemIcons : ConfigurableTweak<ShopItemIconsConfiguration>
{
    private readonly IAddonLifecycle _addonLifecycle;

    public override void OnEnable()
    {
        _addonLifecycle.RegisterListener(AddonEvent.PreSetup, "Shop", OnShopPreSetup);
        _addonLifecycle.RegisterListener(AddonEvent.PreRefresh, "Shop", OnShopPreRefresh);
        _addonLifecycle.RegisterListener(AddonEvent.PreRefresh, ["ShopExchangeItem", "ShopExchangeCurrency"], OnShopExchangePreRefresh);
        _addonLifecycle.RegisterListener(AddonEvent.PreRefresh, "GrandCompanyExchange", OnGrandCompanyExchangePreRefresh);
        _addonLifecycle.RegisterListener(AddonEvent.PreRefresh, "InclusionShop", OnInclusionShopPreRefresh);
        _addonLifecycle.RegisterListener(AddonEvent.PreRefresh, "FreeShop", OnFreeShopPreRefresh);
    }

    public override void OnDisable()
    {
        _addonLifecycle.UnregisterListener(AddonEvent.PreSetup, "Shop", OnShopPreSetup);
        _addonLifecycle.UnregisterListener(AddonEvent.PreRefresh, "Shop", OnShopPreRefresh);
        _addonLifecycle.UnregisterListener(AddonEvent.PreRefresh, ["ShopExchangeItem", "ShopExchangeCurrency"], OnShopExchangePreRefresh);
        _addonLifecycle.UnregisterListener(AddonEvent.PreRefresh, "GrandCompanyExchange", OnGrandCompanyExchangePreRefresh);
        _addonLifecycle.UnregisterListener(AddonEvent.PreRefresh, "InclusionShop", OnInclusionShopPreRefresh);
        _addonLifecycle.UnregisterListener(AddonEvent.PreRefresh, "FreeShop", OnFreeShopPreRefresh);
    }

    private void OnShopPreSetup(AddonEvent type, AddonArgs args)
    {
        if (!_config.HandleShop || args is not AddonSetupArgs setupArgs)
            return;

        UpdateShopIcons(setupArgs.GetAtkValues());
    }

    private void OnShopPreRefresh(AddonEvent type, AddonArgs args)
    {
        if (!_config.HandleShop || args is not AddonRefreshArgs refreshArgs)
            return;

        UpdateShopIcons(refreshArgs.GetAtkValues());
    }

    private void UpdateShopIcons(Span<AtkValue> values)
    {
        if (values.IsEmpty)
            return;

        var handler = ShopEventHandler.AgentProxy.Instance()->Handler;
        if (handler == null)
            return;

        if (values[0] is not { Type: ValueType.UInt, UInt: var tabIndex })
            return;

        const int IconIdOffset = 197;

        // Buy
        if (tabIndex == 0)
        {
            for (var i = 0; i < handler->VisibleItemsCount; i++)
            {
                ref var iconIdValue = ref values[IconIdOffset + i];
                if (iconIdValue.Type != ValueType.UInt)
                    continue;

                var itemIndex = handler->VisibleItems[i];
                if (itemIndex < 0 || itemIndex > handler->ItemsCount)
                    continue;

                var item = new ItemHandle(handler->Items[itemIndex].ItemId);
                if (item.IsEmpty)
                    continue;

                iconIdValue.UInt = item.Icon;
            }
        }
        // Buyback
        else if (tabIndex == 1)
        {
            for (var i = 0; i < handler->BuybackCount; i++)
            {
                ref var iconIdValue = ref values[IconIdOffset + i];
                if (iconIdValue.Type != ValueType.UInt)
                    continue;

                var item = new ItemHandle(handler->Buyback[i].ItemId);
                if (item.IsEmpty)
                    continue;

                iconIdValue.UInt = item.Icon;
            }
        }
    }

    private void OnShopExchangePreRefresh(AddonEvent type, AddonArgs args)
    {
        if (args is not AddonRefreshArgs refreshArgs)
            return;

        if ((args.AddonName == "ShopExchangeItem" && !_config.HandleShopExchangeItem) ||
            (args.AddonName == "ShopExchangeCurrency" && !_config.HandleShopExchangeCurrency))
        {
            return;
        }

        var values = refreshArgs.GetAtkValues();

        for (var i = 0; i < 60; i++)
        {
            ref var itemIdValue = ref values[1063 + i];
            ref var iconIdValue = ref values[209 + i];

            if (itemIdValue.Type != ValueType.UInt || iconIdValue.Type != ValueType.Int || itemIdValue.UInt == 0)
                continue;

            iconIdValue.UInt = new ItemHandle(itemIdValue.UInt).Icon;
        }
    }

    private void OnGrandCompanyExchangePreRefresh(AddonEvent type, AddonArgs args)
    {
        if (!_config.HandleGrandCompanyExchange || args is not AddonRefreshArgs refreshArgs)
            return;

        var values = refreshArgs.GetAtkValues();

        // sometimes it refreshes with just 10 values
        if (values.Length != 556)
            return;

        // last function called in GCShopEventHandler_vf48
        for (var i = 0; i < 50; i++)
        {
            ref var itemIdValue = ref values[317 + i];
            ref var iconIdValue = ref values[167 + i];

            if (itemIdValue.Type != ValueType.UInt || iconIdValue.Type != ValueType.UInt || itemIdValue.UInt == 0)
                continue;

            iconIdValue.UInt = new ItemHandle(itemIdValue.UInt).Icon;
        }
    }

    private void OnInclusionShopPreRefresh(AddonEvent type, AddonArgs args)
    {
        if (!_config.HandleInclusionShop || args is not AddonRefreshArgs refreshArgs)
            return;

        var values = refreshArgs.GetAtkValues();

        // "E8 ?? ?? ?? ?? 89 9D ?? ?? ?? ?? 8B FE"
        var itemCount = values[298].UInt;
        for (var i = 0; i < itemCount; i++)
        {
            ref var itemIdValue = ref values[300 + i * 18];
            ref var iconIdValue = ref values[300 + i * 18 + 1];

            if (itemIdValue.Type != ValueType.UInt || iconIdValue.Type != ValueType.UInt || itemIdValue.UInt == 0)
                continue;

            iconIdValue.UInt = new ItemHandle(itemIdValue.UInt).Icon;
        }
    }

    private void OnFreeShopPreRefresh(AddonEvent type, AddonArgs args)
    {
        if (!_config.HandleFreeShop || args is not AddonRefreshArgs refreshArgs)
            return;

        var values = refreshArgs.GetAtkValues();

        var itemCount = values[3].UInt;
        for (var i = 0; i < itemCount; i++)
        {
            ref var itemIdValue = ref values[65 + i];
            ref var iconIdValue = ref values[126 + i];

            if (itemIdValue.Type != ValueType.UInt || iconIdValue.Type != ValueType.UInt || itemIdValue.UInt == 0)
                continue;

            iconIdValue.UInt = new ItemHandle(itemIdValue.UInt).Icon;
        }
    }
}
