using HaselTweaks.Config;
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
    public void OnConfigOpen() { }
    public void OnConfigChange(string fieldName) { }
    public void OnConfigClose() { }

    public void DrawConfig()
    {
        using var _ = ConfigGui.PushContext(this);

        ConfigGui.DrawConfigurationHeader();
        ConfigGui.DrawBool("HandleShop", ref Config.HandleShop); // Shop
        ConfigGui.DrawBool("HandleShopExchangeItem", ref Config.HandleShopExchangeItem); // Item Exchange
        ConfigGui.DrawBool("HandleInclusionShop", ref Config.HandleInclusionShop); // Item Exchange (Sundry Splendors)
        ConfigGui.DrawBool("HandleShopExchangeCurrency", ref Config.HandleShopExchangeCurrency); // Currency Exchange
        ConfigGui.DrawBool("HandleGrandCompanyExchange", ref Config.HandleGrandCompanyExchange); // Grand Company Seal Exchange
        ConfigGui.DrawBool("HandleFreeShop", ref Config.HandleFreeShop); // Rewards
    }
}
