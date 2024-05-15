using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace HaselTweaks.Tweaks;

public class ScrollableTabsConfiguration
{
    [BoolConfig]
    public bool Invert = true;

    [BoolConfig]
    public bool HandleAetherCurrent = true;

    [BoolConfig]
    public bool HandleArmouryBoard = true;

    [BoolConfig]
    public bool HandleAOZNotebook = true;

    [BoolConfig]
    public bool HandleCharacter = true;

    [BoolConfig]
    public bool HandleCharacterClass = true;

    [BoolConfig]
    public bool HandleCharacterRepute = true;

    [BoolConfig]
    public bool HandleInventoryBuddy = true;

    [BoolConfig]
    public bool HandleBuddy = true;

    [BoolConfig]
    public bool HandleCurrency = true;

    [BoolConfig]
    public bool HandleOrnamentNoteBook = true;

    [BoolConfig]
    public bool HandleFieldRecord = true;

    [BoolConfig]
    public bool HandleFishGuide = true;

    [BoolConfig]
    public bool HandleMiragePrismPrismBox = true;

    [BoolConfig]
    public bool HandleGoldSaucerCardList = true;

    [BoolConfig]
    public bool HandleGoldSaucerCardDeckEdit = true;

    [BoolConfig]
    public bool HandleLovmPaletteEdit = true;

    [BoolConfig]
    public bool HandleInventory = true;

    [BoolConfig]
    public bool HandleMJIMinionNoteBook = true;

    [BoolConfig]
    public bool HandleMinionNoteBook = true;

    [BoolConfig]
    public bool HandleMountNoteBook = true;

    [BoolConfig]
    public bool HandleRetainer = true;

    [BoolConfig]
    public bool HandleFateProgress = true;

    [BoolConfig]
    public bool HandleAdventureNoteBook = true;
}

[Tweak]
public unsafe partial class ScrollableTabs : Tweak<ScrollableTabsConfiguration>
{
    private const int NumArmouryBoardTabs = 12;
    private const int NumInventoryTabs = 5;
    private const int NumInventoryLargeTabs = 4;
    private const int NumInventoryExpansionTabs = 2;
    private const int NumInventoryRetainerTabs = 6;
    private const int NumInventoryRetainerLargeTabs = 3;
    private const int NumBuddyTabs = 3;

    private int _wheelState;

    private AtkUnitBase* IntersectingAddon
        => RaptureAtkModule.Instance()->AtkModule.IntersectingAddon;

    private AtkCollisionNode* IntersectingCollisionNode
        => RaptureAtkModule.Instance()->AtkModule.IntersectingCollisionNode;

    private bool IsNext
        => _wheelState == (!Config.Invert ? 1 : -1);

    private bool IsPrev
        => _wheelState == (!Config.Invert ? -1 : 1);

    public override void OnFrameworkUpdate()
    {
        if (!Service.ClientState.IsLoggedIn)
            return;

        _wheelState = Math.Clamp(UIInputData.Instance()->MouseWheel, -1, 1);
        if (_wheelState == 0)
            return;

        if (Config.Invert)
            _wheelState *= -1;

        var hoveredUnitBase = IntersectingAddon;
        if (hoveredUnitBase == null)
        {
            _wheelState = 0;
            return;
        }

        var name = MemoryHelper.ReadString((nint)hoveredUnitBase->Name, 0x20);
        if (string.IsNullOrEmpty(name))
        {
            _wheelState = 0;
            return;
        }

        // parent lookup
        switch (name)
        {
            // use these directly
            case "AetherCurrent":          // Aether Currents
            case "ArmouryBoard":           // Armoury Chest
            case "AOZNotebook":            // Blue Magic Spellbook
            case "OrnamentNoteBook":       // Fashion Accessories
            case "MYCWarResultNotebook":   // Field Records
            case "FishGuide2":             // Fish Guide
            case "GSInfoCardList":         // Gold Saucer -> Card List
            case "GSInfoEditDeck":         // Gold Saucer -> Decks -> Edit Deck
            case "LovmPaletteEdit":        // Gold Saucer -> Lord of Verminion -> Minion Hotbar
            case "Inventory":              // Inventory
            case "InventoryLarge":         // Inventory
            case "InventoryExpansion":     // Inventory
            case "MinionNoteBook":         // Minions
            case "MountNoteBook":          // Mounts
            case "InventoryRetainer":      // Retainer Inventory
            case "InventoryRetainerLarge": // Retainer Inventory
            case "FateProgress":           // Shared FATE
            case "AdventureNoteBook":      // Sightseeing Log
            case "MJIMinionNoteBook":      // Island Minion Guide
            case "Currency":               // Currency
            case "InventoryBuddy":         // Chocobo Saddlebag
            case "InventoryBuddy2":        // Chocobo Saddlebag (when in Retainer Inventory)
            case "Character":              // Character
            case "CharacterClass":         // Character -> Classes/Jobs
            case "CharacterRepute":        // Character -> Reputation
            case "Buddy":                  // Companion
            case "MiragePrismPrismBox":    // Glamours
                break;

            // used by Inventory
            case "InventoryGrid":
            case "InventoryGridCrystal":
                name = "Inventory";
                break;

            // Key Items (part of Inventory)
            case "InventoryEvent":
            case "InventoryEventGrid":
                name = "InventoryEvent";
                break;

            // used by InventoryLarge or InventoryExpansion
            case "InventoryCrystalGrid":
                name = "InventoryLarge";
                if (Service.GameConfig.UiConfig.TryGet("ItemInventryWindowSizeType", out uint itemInventryWindowSizeType) && itemInventryWindowSizeType == 2)
                    name = "InventoryExpansion";
                break;

            // used by InventoryLarge
            case "InventoryEventGrid0":
            case "InventoryEventGrid1":
            case "InventoryEventGrid2":
            case "InventoryGrid0":
            case "InventoryGrid1":
                name = "InventoryLarge";
                break;

            // used by InventoryExpansion
            case "InventoryEventGrid0E":
            case "InventoryEventGrid1E":
            case "InventoryEventGrid2E":
            case "InventoryGrid0E":
            case "InventoryGrid1E":
            case "InventoryGrid2E":
            case "InventoryGrid3E":
                name = "InventoryExpansion";
                break;

            // used by InventoryRetainer
            case "RetainerGridCrystal":
            case "RetainerGrid":
                name = "InventoryRetainer";
                break;

            // used by InventoryRetainerLarge
            case "RetainerCrystalGrid":
            case "RetainerGrid0":
            case "RetainerGrid1":
            case "RetainerGrid2":
            case "RetainerGrid3":
            case "RetainerGrid4":
                name = "InventoryRetainerLarge";
                break;

            // embedded addons of Character
            case "CharacterStatus":   // Character -> Attributes
            case "CharacterProfile":  // Character -> Profile
                name = "Character";
                break;

            // embedded addons of Buddy
            case "BuddyAction":     // Companion -> Actions
            case "BuddySkill":      // Companion -> Skills
            case "BuddyAppearance": // Companion -> Appearance
                name = "Buddy";
                break;

            default:
#if DEBUG
                Verbose($"Unhandled AtkUnitBase: {name}");
#endif
                _wheelState = 0;
                return;
        }

        if (!TryGetAddon<AtkUnitBase>(name, out var unitBase))
        {
            _wheelState = 0;
            return;
        }

        if (Config.HandleArmouryBoard && name == "ArmouryBoard")
        {
            UpdateArmouryBoard((AddonArmouryBoard*)unitBase);
        }
        else if (Config.HandleInventory && name is "Inventory" or "InventoryEvent" or "InventoryLarge" or "InventoryExpansion")
        {
            switch (name)
            {
                case "Inventory":
                    UpdateInventory((AddonInventory*)unitBase);
                    break;
                case "InventoryEvent":
                    UpdateInventoryEvent((AddonInventoryEvent*)unitBase);
                    break;
                case "InventoryLarge":
                    UpdateInventoryLarge((AddonInventoryLarge*)unitBase);
                    break;
                case "InventoryExpansion":
                    UpdateInventoryExpansion((AddonInventoryExpansion*)unitBase);
                    break;
            }
        }
        else if (Config.HandleRetainer && name is "InventoryRetainer" or "InventoryRetainerLarge")
        {
            switch (name)
            {
                case "InventoryRetainer":
                    UpdateInventoryRetainer((AddonInventoryRetainer*)unitBase);
                    break;
                case "InventoryRetainerLarge":
                    UpdateInventoryRetainerLarge((AddonInventoryRetainerLarge*)unitBase);
                    break;
            }
        }
        else if ((Config.HandleMinionNoteBook && name == "MinionNoteBook") || (Config.HandleMountNoteBook && name == "MountNoteBook"))
        {
            UpdateMountMinion((AddonMinionMountBase*)unitBase);
        }
        else if (Config.HandleFishGuide && name == "FishGuide2")
        {
            UpdateTabController(unitBase, &((AddonFishGuide2*)unitBase)->TabController);
        }
        else if (Config.HandleAdventureNoteBook && name == "AdventureNoteBook")
        {
            UpdateTabController(unitBase, &((AddonAdventureNoteBook*)unitBase)->TabController);
        }
        else if (Config.HandleOrnamentNoteBook && name == "OrnamentNoteBook")
        {
            UpdateTabController(unitBase, &((AddonOrnamentNoteBook*)unitBase)->TabController);
        }
        else if (Config.HandleGoldSaucerCardList && name == "GSInfoCardList")
        {
            UpdateTabController(unitBase, &((AddonGSInfoCardList*)unitBase)->TabController);
        }
        else if (Config.HandleGoldSaucerCardDeckEdit && name == "GSInfoEditDeck")
        {
            UpdateTabController(unitBase, &((AddonGSInfoEditDeck*)unitBase)->TabController);
        }
        else if (Config.HandleLovmPaletteEdit && name == "LovmPaletteEdit")
        {
            UpdateTabController(unitBase, &((AddonLovmPaletteEdit*)unitBase)->TabController);
        }
        else if (Config.HandleAOZNotebook && name == "AOZNotebook")
        {
            UpdateAOZNotebook((AddonAOZNotebook*)unitBase);
        }
        else if (Config.HandleAetherCurrent && name == "AetherCurrent")
        {
            UpdateAetherCurrent((AddonAetherCurrent*)unitBase);
        }
        else if (Config.HandleFateProgress && name == "FateProgress")
        {
            UpdateFateProgress((AddonFateProgress*)unitBase);
        }
        else if (Config.HandleFieldRecord && name == "MYCWarResultNotebook")
        {
            UpdateFieldNotes((AddonMYCWarResultNotebook*)unitBase);
        }
        else if (Config.HandleMJIMinionNoteBook && name == "MJIMinionNoteBook")
        {
            UpdateMJIMinionNoteBook((AddonMJIMinionNoteBook*)unitBase);
        }
        else if (Config.HandleCurrency && name == "Currency")
        {
            UpdateCurrency(unitBase);
        }
        else if (Config.HandleInventoryBuddy && name is "InventoryBuddy" or "InventoryBuddy2")
        {
            UpdateInventoryBuddy((AddonInventoryBuddy*)unitBase);
        }
        else if (Config.HandleBuddy && name == "Buddy")
        {
            UpdateBuddy((AddonBuddy*)unitBase);
        }
        else if (Config.HandleMiragePrismPrismBox && name == "MiragePrismPrismBox")
        {
            UpdateMiragePrismPrismBox((AddonMiragePrismPrismBox*)unitBase);
        }
        else if (name is "Character" or "CharacterClass" or "CharacterRepute")
        {
            var addonCharacter = name == "Character" ? (AddonCharacter*)unitBase : GetAddon<AddonCharacter>("Character");

            if (addonCharacter == null || !addonCharacter->EmbeddedAddonLoaded || IntersectingCollisionNode == addonCharacter->CharacterPreviewCollisionNode)
            {
                _wheelState = 0;
                return;
            }

            switch (name)
            {
                case "Character" when Config.HandleCharacter:
                    UpdateCharacter(addonCharacter);
                    break;
                case "CharacterClass" when Config.HandleCharacter && !Config.HandleCharacterClass:
                    UpdateCharacter(addonCharacter);
                    break;
                case "CharacterClass" when Config.HandleCharacterClass:
                    UpdateCharacterClass(addonCharacter, (AddonCharacterClass*)unitBase);
                    break;
                case "CharacterRepute" when Config.HandleCharacter && !Config.HandleCharacterRepute:
                    UpdateCharacter(addonCharacter);
                    break;
                case "CharacterRepute" when Config.HandleCharacterRepute:
                    UpdateCharacterRepute(addonCharacter, (AddonCharacterRepute*)unitBase);
                    break;
            }
        }

        _wheelState = 0;
    }

    private int GetTabIndex(int currentTabIndex, int numTabs)
        => Math.Clamp(currentTabIndex + _wheelState, 0, numTabs - 1);

    private void UpdateArmouryBoard(AddonArmouryBoard* addon)
    {
        var tabIndex = GetTabIndex(addon->TabIndex, NumArmouryBoardTabs);

        if (addon->TabIndex < tabIndex)
            addon->NextTab(0);
        else if (addon->TabIndex > tabIndex)
            addon->PreviousTab(0);
    }

    private void UpdateInventory(AddonInventory* addon)
    {
        if (addon->TabIndex == NumInventoryTabs - 1 && _wheelState > 0)
        {
            // inside "48 89 6C 24 ?? 56 48 83 EC 20 0F B7 C2", a3 != 17
            var values = stackalloc AtkValue[3];

            values[0].Type = ValueType.Int;
            values[0].Int = 22;

            values[1].Type = ValueType.Int;
            values[1].Int = *(int*)((nint)addon + 0x228);

            values[2].Type = ValueType.UInt;
            values[2].UInt = 0;

            addon->AtkUnitBase.FireCallback(3, values);
        }
        else
        {
            var tabIndex = GetTabIndex(addon->TabIndex, NumInventoryTabs);

            if (addon->TabIndex == tabIndex)
                return;

            addon->SetTab(tabIndex);
        }
    }

    private void UpdateInventoryEvent(AddonInventoryEvent* addon)
    {
        if (addon->TabIndex == 0 && _wheelState < 0)
        {
            // inside Vf68, fn call before return with a2 being 2
            var values = stackalloc AtkValue[3];

            values[0].Type = ValueType.Int;
            values[0].Int = 22;

            values[1].Type = ValueType.Int;
            values[1].Int = *(int*)((nint)addon + 0x280);

            values[2].Type = ValueType.UInt;
            values[2].UInt = 2;

            addon->AtkUnitBase.FireCallback(3, values);
        }
        else
        {
            var numEnabledButtons = 0;
            foreach (ref var button in addon->ButtonsSpan)
            {
                if ((button.Value->AtkComponentButton.Flags & 0x40000) != 0)
                    numEnabledButtons++;
            }

            var tabIndex = GetTabIndex(addon->TabIndex, numEnabledButtons);

            if (addon->TabIndex == tabIndex)
                return;

            addon->SetTab(tabIndex);
        }
    }

    private void UpdateInventoryLarge(AddonInventoryLarge* addon)
    {
        var tabIndex = GetTabIndex(addon->TabIndex, NumInventoryLargeTabs);

        if (addon->TabIndex == tabIndex)
            return;

        addon->SetTab(tabIndex);
    }

    private void UpdateInventoryExpansion(AddonInventoryExpansion* addon)
    {
        var tabIndex = GetTabIndex(addon->TabIndex, NumInventoryExpansionTabs);

        if (addon->TabIndex == tabIndex)
            return;

        addon->SetTab(tabIndex, false);
    }

    private void UpdateInventoryRetainer(AddonInventoryRetainer* addon)
    {
        var tabIndex = GetTabIndex(addon->TabIndex, NumInventoryRetainerTabs);

        if (addon->TabIndex == tabIndex)
            return;

        addon->SetTab(tabIndex);
    }

    private void UpdateInventoryRetainerLarge(AddonInventoryRetainerLarge* addon)
    {
        var tabIndex = GetTabIndex(addon->TabIndex, NumInventoryRetainerLargeTabs);

        if (addon->TabIndex == tabIndex)
            return;

        addon->SetTab(tabIndex);
    }

    private void UpdateTabController(AtkUnitBase* addon, TabController* TabController)
    {
        var tabIndex = GetTabIndex(TabController->TabIndex, TabController->TabCount);

        if (TabController->TabIndex == tabIndex)
            return;

        TabController->TabIndex = tabIndex;
        TabController->CallbackFunction(tabIndex, addon);
    }

    private void UpdateAOZNotebook(AddonAOZNotebook* addon)
    {
        var tabIndex = GetTabIndex(addon->TabIndex, addon->TabCount);

        if (addon->TabIndex == tabIndex)
            return;

        addon->SetTab(tabIndex, true);
    }

    private void UpdateAetherCurrent(AddonAetherCurrent* addon)
    {
        var tabIndex = GetTabIndex(addon->TabIndex, addon->TabCount);
        if (addon->TabIndex == tabIndex) return;

        addon->SetTab(tabIndex);

        var tabs = (nint)addon + 0x228;
        for (var i = 0; i < addon->TabCount; i++)
        {
            // WAYTOODANK, this is basically like writing addon->Tabs[i]
            // but because this is dynamic (depending on NumTabs), we can't do that... thanks, C#!
            var button = *(AtkComponentRadioButton**)(tabs + i * 8);
            button->IsSelected = i == tabIndex;
        }
    }

    private void UpdateFateProgress(AddonFateProgress* addon)
    {
        var tabIndex = GetTabIndex(addon->TabIndex, addon->TabCount);
        if (!addon->Loaded || addon->TabIndex == tabIndex)
            return;

        // fake event, so it can call SetEventIsHandled
        var atkEvent = stackalloc AtkEvent[1];
        addon->SetTab(tabIndex, atkEvent);
    }

    private void UpdateFieldNotes(AddonMYCWarResultNotebook* addon)
    {
        if (IntersectingCollisionNode == addon->DescriptionCollisionNode)
            return;

        var atkEvent = stackalloc AtkEvent[1];
        var eventParam = Math.Clamp(addon->CurrentNoteIndex % 10 + _wheelState, -1, addon->MaxNoteIndex - 1);

        if (eventParam == -1)
        {
            if (addon->CurrentPageIndex > 0)
            {
                var page = addon->CurrentPageIndex - 1;
                addon->AtkUnitBase.ReceiveEvent(AtkEventType.ButtonClick, page + 10, atkEvent, 0);
                addon->AtkUnitBase.ReceiveEvent(AtkEventType.ButtonClick, 9, atkEvent, 0);
            }
        }
        else if (eventParam == 10)
        {
            if (addon->CurrentPageIndex < 4)
            {
                var page = addon->CurrentPageIndex + 1;
                addon->AtkUnitBase.ReceiveEvent(AtkEventType.ButtonClick, page + 10, atkEvent, 0);
            }
        }
        else
        {
            addon->AtkUnitBase.ReceiveEvent(AtkEventType.ButtonClick, eventParam, atkEvent, 0);
        }
    }

    private void UpdateMountMinion(AddonMinionMountBase* addon)
    {
        if (addon->CurrentView == AddonMinionMountBase.ViewType.Normal)
        {
            if (addon->TabController.TabIndex == 0 && _wheelState < 0)
            {
                addon->SwitchToFavorites();
            }
            else
            {
                UpdateTabController((AtkUnitBase*)addon, &addon->TabController);
            }
        }
        else if (addon->CurrentView == AddonMinionMountBase.ViewType.Favorites && _wheelState > 0)
        {
            addon->TabController.CallbackFunction(0, (AtkUnitBase*)addon);
        }
    }

    private void UpdateMJIMinionNoteBook(AddonMJIMinionNoteBook* addon)
    {
        var agent = GetAgent<AgentMJIMinionNoteBook>();

        if (agent->CurrentView == AgentMJIMinionNoteBook.ViewType.Normal)
        {
            if (addon->TabController.TabIndex == 0 && _wheelState < 0)
            {
                agent->CurrentView = AgentMJIMinionNoteBook.ViewType.Favorites;
                agent->SelectedFavoriteMinion.TabIndex = 0;
                agent->SelectedFavoriteMinion.SlotIndex = agent->SelectedNormalMinion.SlotIndex;
                agent->SelectedFavoriteMinion.MinionId = agent->GetSelectedMinionId();
                agent->SelectedMinion = &agent->SelectedFavoriteMinion;
                agent->HandleCommand(0x407);
            }
            else
            {
                UpdateTabController((AtkUnitBase*)addon, &addon->TabController);
                agent->HandleCommand(0x40B);
            }
        }
        else if (agent->CurrentView == AgentMJIMinionNoteBook.ViewType.Favorites && _wheelState > 0)
        {
            agent->CurrentView = AgentMJIMinionNoteBook.ViewType.Normal;
            agent->SelectedNormalMinion.TabIndex = 0;
            agent->SelectedNormalMinion.SlotIndex = agent->SelectedFavoriteMinion.SlotIndex;
            agent->SelectedNormalMinion.MinionId = agent->GetSelectedMinionId();
            agent->SelectedMinion = &agent->SelectedNormalMinion;

            addon->TabController.TabIndex = 0;
            addon->TabController.CallbackFunction(0, (AtkUnitBase*)addon);

            agent->HandleCommand(0x40B);
        }
    }

    private void UpdateCurrency(AtkUnitBase* addon)
    {
        var atkStage = AtkStage.GetSingleton();
        var numberArray = atkStage->GetNumberArrayData()[80];
        var currentTab = numberArray->IntArray[0];

        var newTab = GetTabIndex(currentTab, 4);

        if (currentTab == newTab)
            return;

        numberArray->SetValue(0, newTab);
        addon->OnUpdate(atkStage->GetNumberArrayData(), atkStage->GetStringArrayData());
    }

    private void UpdateInventoryBuddy(AddonInventoryBuddy* addon)
    {
        if (!PlayerState.Instance()->HasPremiumSaddlebag)
            return;

        var tabIndex = GetTabIndex(addon->TabIndex, 2);

        if (addon->TabIndex == tabIndex)
            return;

        addon->SetTab((byte)tabIndex);
    }

    private void UpdateBuddy(AddonBuddy* addon)
    {
        var tabIndex = GetTabIndex(addon->TabIndex, NumBuddyTabs);

        if (addon->TabIndex == tabIndex)
            return;

        addon->SetTab(tabIndex);

        for (var i = 0; i < NumBuddyTabs; i++)
        {
            var button = addon->RadioButtonsSpan.GetPointer(i);
            if (button->Value != null)
            {
                button->Value->IsSelected = i == addon->TabIndex;
            }
        }
    }

    private void UpdateMiragePrismPrismBox(AddonMiragePrismPrismBox* addon)
    {
        if (addon->JobDropdown == null ||
            addon->JobDropdown->List == null ||
            addon->JobDropdown->List->AtkComponentBase.OwnerNode == null ||
            addon->JobDropdown->List->AtkComponentBase.OwnerNode->AtkResNode.IsVisible)
        {
            return;
        }

        if (addon->OrderDropdown == null ||
            addon->OrderDropdown->List == null ||
            addon->OrderDropdown->List->AtkComponentBase.OwnerNode == null ||
            addon->OrderDropdown->List->AtkComponentBase.OwnerNode->AtkResNode.IsVisible)
        {
            return;
        }

        var prevButton = !Config.Invert ? addon->PrevButton : addon->NextButton;
        var nextButton = !Config.Invert ? addon->NextButton : addon->PrevButton;

        if (prevButton == null || (IsPrev && !prevButton->IsEnabled))
            return;

        if (nextButton == null || (IsNext && !nextButton->IsEnabled))
            return;

        if (IsAddonOpen("MiragePrismPrismBoxFilter"))
            return;

        var agent = GetAgent<AgentMiragePrismPrismBox>();
        agent->PageIndex += (byte)_wheelState;
        agent->UpdateItems(false, false);
    }

    private void UpdateCharacter(AddonCharacter* addon)
    {
        var tabIndex = GetTabIndex(addon->TabIndex, addon->TabCount);

        if (addon->TabIndex == tabIndex)
            return;

        addon->SetTab(tabIndex);

        for (var i = 0; i < addon->TabCount; i++)
        {
            var button = addon->RadioButtonsSpan.GetPointer(i);
            if (button->Value != null)
            {
                button->Value->IsSelected = i == addon->TabIndex;
            }
        }
    }

    private void UpdateCharacterClass(AddonCharacter* addonCharacter, AddonCharacterClass* addon)
    {
        // prev or next embedded addon
        if (Config.HandleCharacter && (addon->TabIndex + _wheelState < 0 || addon->TabIndex + _wheelState > 1))
        {
            UpdateCharacter(addonCharacter);
            return;
        }

        var tabIndex = GetTabIndex(addon->TabIndex, 2);

        if (addon->TabIndex == tabIndex)
            return;

        addon->SetTab(tabIndex);
    }

    private void UpdateCharacterRepute(AddonCharacter* addonCharacter, AddonCharacterRepute* addon)
    {
        // prev embedded addon
        if (Config.HandleCharacter && (addon->SelectedExpansion + _wheelState < 0))
        {
            UpdateCharacter(addonCharacter);
            return;
        }

        var tabIndex = GetTabIndex(addon->SelectedExpansion, addon->ExpansionsCount);

        if (addon->SelectedExpansion == tabIndex)
            return;

        var atkEvent = stackalloc AtkEvent[1];
        var data = stackalloc int[5];
        data[4] = tabIndex; // technically the index of an id array, but it's literally the same value
        addon->AtkUnitBase.ReceiveEvent((AtkEventType)37, 0, atkEvent, (nint)data);
    }
}
