namespace HaselTweaks.Tweaks;

public class ScrollableTabsConfiguration
{
    public bool Invert = true;
    public bool SuppressQuickPanelSounds = true;
    public bool HandleAetherCurrent = true;
    public bool HandleArmouryBoard = true;
    public bool HandleAOZNotebook = true;
    public bool HandleCharacter = true;
    public bool HandleCharacterClass = true;
    public bool HandleCharacterRepute = true;
    public bool HandleInventoryBuddy = true;
    public bool HandleBuddy = true;
    public bool HandleCurrency = true;
    public bool HandleGlassSelect = true;
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
    public override void DrawConfig()
    {
        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool("Invert", ref _config.Invert);
        _configGui.DrawBool("SuppressQuickPanelSounds", ref _config.SuppressQuickPanelSounds);
        _configGui.DrawBool("HandleAetherCurrent", ref _config.HandleAetherCurrent);
        _configGui.DrawBool("HandleArmouryBoard", ref _config.HandleArmouryBoard);
        _configGui.DrawBool("HandleAOZNotebook", ref _config.HandleAOZNotebook);
        _configGui.DrawBool("HandleCharacter", ref _config.HandleCharacter);
        _configGui.DrawBool("HandleCharacterClass", ref _config.HandleCharacterClass);
        _configGui.DrawBool("HandleCharacterRepute", ref _config.HandleCharacterRepute);
        _configGui.DrawBool("HandleInventoryBuddy", ref _config.HandleInventoryBuddy);
        _configGui.DrawBool("HandleBuddy", ref _config.HandleBuddy);
        _configGui.DrawBool("HandleCurrency", ref _config.HandleCurrency);
        _configGui.DrawBool("HandleGlassSelect", ref _config.HandleGlassSelect);
        _configGui.DrawBool("HandleOrnamentNoteBook", ref _config.HandleOrnamentNoteBook);
        _configGui.DrawBool("HandleFieldRecord", ref _config.HandleFieldRecord);
        _configGui.DrawBool("HandleFishGuide", ref _config.HandleFishGuide);
        _configGui.DrawBool("HandleMiragePrismPrismBox", ref _config.HandleMiragePrismPrismBox);
        _configGui.DrawBool("HandleGoldSaucerCardList", ref _config.HandleGoldSaucerCardList);
        _configGui.DrawBool("HandleGoldSaucerCardDeckEdit", ref _config.HandleGoldSaucerCardDeckEdit);
        _configGui.DrawBool("HandleLovmPaletteEdit", ref _config.HandleLovmPaletteEdit);
        _configGui.DrawBool("HandleInventory", ref _config.HandleInventory);
        _configGui.DrawBool("HandleMJIMinionNoteBook", ref _config.HandleMJIMinionNoteBook);
        _configGui.DrawBool("HandleMinionNoteBook", ref _config.HandleMinionNoteBook);
        _configGui.DrawBool("HandleMountNoteBook", ref _config.HandleMountNoteBook);
        _configGui.DrawBool("HandleRetainer", ref _config.HandleRetainer);
        _configGui.DrawBool("HandleFateProgress", ref _config.HandleFateProgress);
        _configGui.DrawBool("HandleAdventureNoteBook", ref _config.HandleAdventureNoteBook);
    }
}
