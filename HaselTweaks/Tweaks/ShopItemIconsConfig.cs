using HaselTweaks.Interfaces;

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

public partial class ShopItemIcons : IConfigurableTweak
{
    private ShopItemIconsConfiguration Config => _pluginConfig.Tweaks.ShopItemIcons;

    public void OnConfigOpen() { }
    public void OnConfigChange(string fieldName) { }
    public void OnConfigClose() { }

    public void DrawConfig()
    {
        using var _ = _configGui.PushContext(this);

        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool("HandleShop", ref Config.HandleShop); // Shop
        _configGui.DrawBool("HandleShopExchangeItem", ref Config.HandleShopExchangeItem); // Item Exchange
        _configGui.DrawBool("HandleInclusionShop", ref Config.HandleInclusionShop); // Item Exchange (Sundry Splendors)
        _configGui.DrawBool("HandleShopExchangeCurrency", ref Config.HandleShopExchangeCurrency); // Currency Exchange
        _configGui.DrawBool("HandleGrandCompanyExchange", ref Config.HandleGrandCompanyExchange); // Grand Company Seal Exchange
        _configGui.DrawBool("HandleFreeShop", ref Config.HandleFreeShop); // Rewards
    }
}
