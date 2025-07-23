namespace HaselTweaks.Tweaks;

public class ScrollableTabsConfiguration
{
    public bool Invert = true;
    public bool HandleAetherCurrent = true;
    public bool HandleArmouryBoard = true;
    public bool HandleAOZNotebook = true;
    public bool HandleCharacter = true;
    public bool HandleCharacterClass = true;
    public bool HandleCharacterRepute = true;
    public bool HandleInventoryBuddy = true;
    public bool HandleBuddy = true;
    public bool HandleCurrency = true;
    public bool HandleOrnamentNoteBook = true;
    public bool HandleFieldRecord = true;
    public bool HandleFishGuide = true;
    public bool HandleMiragePrismPrismBox = true;
    public bool HandleGoldSaucerCardList = true;
    public bool HandleGoldSaucerCardDeckEdit = true;
    public bool HandleLovmPaletteEdit = true;
    public bool HandleInventory = true;
    public bool HandleMJIMinionNoteBook = true;
    public bool HandleMinionNoteBook = true;
    public bool HandleMountNoteBook = true;
    public bool HandleRetainer = true;
    public bool HandleFateProgress = true;
    public bool HandleAdventureNoteBook = true;
}

public unsafe partial class ScrollableTabs
{
    private ScrollableTabsConfiguration Config => _pluginConfig.Tweaks.ScrollableTabs;

    public override void DrawConfig()
    {
        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool("Invert", ref Config.Invert);
        _configGui.DrawBool("HandleAetherCurrent", ref Config.HandleAetherCurrent);
        _configGui.DrawBool("HandleArmouryBoard", ref Config.HandleArmouryBoard);
        _configGui.DrawBool("HandleAOZNotebook", ref Config.HandleAOZNotebook);
        _configGui.DrawBool("HandleCharacter", ref Config.HandleCharacter);
        _configGui.DrawBool("HandleCharacterClass", ref Config.HandleCharacterClass);
        _configGui.DrawBool("HandleCharacterRepute", ref Config.HandleCharacterRepute);
        _configGui.DrawBool("HandleInventoryBuddy", ref Config.HandleInventoryBuddy);
        _configGui.DrawBool("HandleBuddy", ref Config.HandleBuddy);
        _configGui.DrawBool("HandleCurrency", ref Config.HandleCurrency);
        _configGui.DrawBool("HandleOrnamentNoteBook", ref Config.HandleOrnamentNoteBook);
        _configGui.DrawBool("HandleFieldRecord", ref Config.HandleFieldRecord);
        _configGui.DrawBool("HandleFishGuide", ref Config.HandleFishGuide);
        _configGui.DrawBool("HandleMiragePrismPrismBox", ref Config.HandleMiragePrismPrismBox);
        _configGui.DrawBool("HandleGoldSaucerCardList", ref Config.HandleGoldSaucerCardList);
        _configGui.DrawBool("HandleGoldSaucerCardDeckEdit", ref Config.HandleGoldSaucerCardDeckEdit);
        _configGui.DrawBool("HandleLovmPaletteEdit", ref Config.HandleLovmPaletteEdit);
        _configGui.DrawBool("HandleInventory", ref Config.HandleInventory);
        _configGui.DrawBool("HandleMJIMinionNoteBook", ref Config.HandleMJIMinionNoteBook);
        _configGui.DrawBool("HandleMinionNoteBook", ref Config.HandleMinionNoteBook);
        _configGui.DrawBool("HandleMountNoteBook", ref Config.HandleMountNoteBook);
        _configGui.DrawBool("HandleRetainer", ref Config.HandleRetainer);
        _configGui.DrawBool("HandleFateProgress", ref Config.HandleFateProgress);
        _configGui.DrawBool("HandleAdventureNoteBook", ref Config.HandleAdventureNoteBook);
    }
}
