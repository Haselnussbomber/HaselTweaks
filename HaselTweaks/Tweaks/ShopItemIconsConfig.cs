namespace HaselTweaks.Tweaks;

public class ShopItemIconsConfiguration
{
    public bool HandleShop = true;
    public bool HandleShopExchangeItem = true;
    public bool HandleInclusionShop = true;
    public bool HandleShopExchangeCurrency = true;
    public bool HandleGrandCompanyExchange = true;
    public bool HandleFreeShop = true;
}

public partial class ShopItemIcons
{
    public override void DrawConfig()
    {
        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool("HandleShop", ref _config.HandleShop); // Shop
        _configGui.DrawBool("HandleShopExchangeItem", ref _config.HandleShopExchangeItem); // Item Exchange
        _configGui.DrawBool("HandleInclusionShop", ref _config.HandleInclusionShop); // Item Exchange (Sundry Splendors)
        _configGui.DrawBool("HandleShopExchangeCurrency", ref _config.HandleShopExchangeCurrency); // Currency Exchange
        _configGui.DrawBool("HandleGrandCompanyExchange", ref _config.HandleGrandCompanyExchange); // Grand Company Seal Exchange
        _configGui.DrawBool("HandleFreeShop", ref _config.HandleFreeShop); // Rewards
    }
}
