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
    public void OnConfigOpen() { }
    public void OnConfigChange(string fieldName) { }
    public void OnConfigClose() { }

    public void DrawConfig()
    {
        using var _ = ConfigGui.PushContext(this);

        ConfigGui.DrawConfigurationHeader();
        ConfigGui.DrawBool("Invert", ref Config.Invert);
        ConfigGui.DrawBool("HandleAetherCurrent", ref Config.HandleAetherCurrent);
        ConfigGui.DrawBool("HandleArmouryBoard", ref Config.HandleArmouryBoard);
        ConfigGui.DrawBool("HandleAOZNotebook", ref Config.HandleAOZNotebook);
        ConfigGui.DrawBool("HandleCharacter", ref Config.HandleCharacter);
        ConfigGui.DrawBool("HandleCharacterClass", ref Config.HandleCharacterClass);
        ConfigGui.DrawBool("HandleCharacterRepute", ref Config.HandleCharacterRepute);
        ConfigGui.DrawBool("HandleInventoryBuddy", ref Config.HandleInventoryBuddy);
        ConfigGui.DrawBool("HandleBuddy", ref Config.HandleBuddy);
        ConfigGui.DrawBool("HandleCurrency", ref Config.HandleCurrency);
        ConfigGui.DrawBool("HandleOrnamentNoteBook", ref Config.HandleOrnamentNoteBook);
        ConfigGui.DrawBool("HandleFieldRecord", ref Config.HandleFieldRecord);
        ConfigGui.DrawBool("HandleFishGuide", ref Config.HandleFishGuide);
        ConfigGui.DrawBool("HandleMiragePrismPrismBox", ref Config.HandleMiragePrismPrismBox);
        ConfigGui.DrawBool("HandleGoldSaucerCardList", ref Config.HandleGoldSaucerCardList);
        ConfigGui.DrawBool("HandleGoldSaucerCardDeckEdit", ref Config.HandleGoldSaucerCardDeckEdit);
        ConfigGui.DrawBool("HandleLovmPaletteEdit", ref Config.HandleLovmPaletteEdit);
        ConfigGui.DrawBool("HandleInventory", ref Config.HandleInventory);
        ConfigGui.DrawBool("HandleMJIMinionNoteBook", ref Config.HandleMJIMinionNoteBook);
        ConfigGui.DrawBool("HandleMinionNoteBook", ref Config.HandleMinionNoteBook);
        ConfigGui.DrawBool("HandleMountNoteBook", ref Config.HandleMountNoteBook);
        ConfigGui.DrawBool("HandleRetainer", ref Config.HandleRetainer);
        ConfigGui.DrawBool("HandleFateProgress", ref Config.HandleFateProgress);
        ConfigGui.DrawBool("HandleAdventureNoteBook", ref Config.HandleAdventureNoteBook);
    }
}
