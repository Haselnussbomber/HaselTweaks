using System.Collections.Frozen;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class ScrollableTabs : ConfigurableTweak<ScrollableTabsConfiguration>
{
    private const int NumArmouryBoardTabs = 12;
    private const int NumInventoryTabs = 5;
    private const int NumInventoryLargeTabs = 4;
    private const int NumInventoryExpansionTabs = 2;
    private const int NumInventoryRetainerTabs = 6;
    private const int NumInventoryRetainerLargeTabs = 3;
    private const int NumBuddyTabs = 3;

    private static readonly HashSet<string> BuddyAddons =
    [
        "Buddy",
        "BuddyAction",
        "BuddySkill",
        "BuddyAppearance",
    ];

    private static readonly HashSet<string> CharacterAddons =
    [
        "Character",
        "CharacterStatus",
        "CharacterProfile",
    ];

    private static readonly HashSet<string> InventoryAddons =
    [
        "Inventory",
        "InventoryGrid",
        "InventoryGridCrystal",
    ];

    private static readonly HashSet<string> InventoryLargeAddons =
    [
        "InventoryLarge",
        "InventoryEventGrid0",
        "InventoryEventGrid1",
        "InventoryEventGrid2",
        "InventoryGrid0",
        "InventoryGrid1",
    ];

    private static readonly HashSet<string> InventoryExpansionAddons =
    [
        "InventoryExpansion",
        "InventoryEventGrid0E",
        "InventoryEventGrid1E",
        "InventoryEventGrid2E",
        "InventoryGrid0E",
        "InventoryGrid1E",
        "InventoryGrid2E",
        "InventoryGrid3E",
    ];

    private static readonly HashSet<string> InventoryEventAddons =
    [
        "InventoryEvent",
        "InventoryEventGrid",
    ];

    private static readonly HashSet<string> InventoryBuddyAddons =
    [
        "InventoryBuddy",
        "InventoryBuddy2",
    ];

    private static readonly HashSet<string> InventoryRetainerAddons =
    [
        "InventoryRetainer",
        "RetainerGridCrystal",
        "RetainerGrid",
    ];

    private static readonly HashSet<string> InventoryRetainerLargeAddons =
    [
        "InventoryRetainerLarge",
        "RetainerCrystalGrid",
        "RetainerGrid0",
        "RetainerGrid1",
        "RetainerGrid2",
        "RetainerGrid3",
        "RetainerGrid4",
    ];

    private static readonly HashSet<string> MountMinionBaseAddons =
    [
        "MinionNoteBook",
        "MountNoteBook",
    ];

    private readonly IFramework _framework;
    private readonly IClientState _clientState;
    private readonly IGameConfig _gameConfig;
    private readonly IGameInteropProvider _gameInteropProvider;
    private MemoryReplacement? _quickPanelPlaySoundEffectPatch;
    private FrozenDictionary<string, Action<Pointer<AtkUnitBase>>>? _handlers;

    private int _wheelState;

    [Signature("41 B8 ?? ?? ?? ?? 48 8D 54 24 ?? 48 8B 48 ?? ?? ?? ?? FF 50 ?? 48 8D 4C 24 ?? E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 0F B6 47"), AutoConstructIgnore]
    private nint QuickPanelPlaySoundEffectAddress { get; init; }

    private AtkCollisionNode* IntersectingCollisionNode
        => RaptureAtkModule.Instance()->AtkCollisionManager.IntersectingCollisionNode;

    private bool IsNext
        => _wheelState == (!_config.Invert ? 1 : -1);

    private bool IsPrev
        => _wheelState == (!_config.Invert ? -1 : 1);

    public override void OnEnable()
    {
        SetupHandlers();

        if (QuickPanelPlaySoundEffectAddress == nint.Zero)
            _gameInteropProvider.InitializeFromAttributes(this);

        _quickPanelPlaySoundEffectPatch = new(QuickPanelPlaySoundEffectAddress, [0xEB, 0x13]);

        if (_config.SuppressQuickPanelSounds)
            _quickPanelPlaySoundEffectPatch.Enable();

        _framework.Update += OnFrameworkUpdate;
    }

    public override void OnDisable()
    {
        _framework.Update -= OnFrameworkUpdate;

        _quickPanelPlaySoundEffectPatch?.Dispose();
        _quickPanelPlaySoundEffectPatch = null;
    }

    private void SetupHandlers()
    {
        if (_handlers != null)
            return;

        var handlers = new Dictionary<string, Action<Pointer<AtkUnitBase>>>
        {
            ["CharacterClass"] = ptr => UpdateCharacterClass(ptr.Cast<AddonCharacterClass>()),
            ["CharacterRepute"] = ptr => UpdateCharacterRepute(ptr.Cast<AddonCharacterRepute>()),
            ["AOZNotebook"] = ptr => UpdateAOZNotebook(ptr.Cast<AddonAOZNotebook>()),
            ["AdventureNoteBook"] = ptr => UpdateTabController(ptr, &ptr.Cast<AddonAdventureNoteBook>()->TabController, _config.HandleAdventureNoteBook),
            ["AetherCurrent"] = ptr => UpdateAetherCurrent(ptr.Cast<AddonAetherCurrent>()),
            ["ArmouryBoard"] = ptr => UpdateArmouryBoard(ptr.Cast<AddonArmouryBoard>()),
            ["Currency"] = ptr => UpdateCurrency(ptr.Cast<AddonCurrency>()),
            ["FateProgress"] = ptr => UpdateFateProgress(ptr.Cast<AddonFateProgress>()),
            ["FishGuide2"] = ptr => UpdateTabController(ptr, &ptr.Cast<AddonFishGuide2>()->TabController, _config.HandleFishGuide),
            ["GSInfoCardList"] = ptr => UpdateTabController(ptr, &ptr.Cast<AddonGSInfoCardList>()->TabController, _config.HandleGoldSaucerCardList),
            ["GSInfoEditDeck"] = ptr => UpdateTabController(ptr, &ptr.Cast<AddonGSInfoEditDeck>()->TabController, _config.HandleGoldSaucerCardDeckEdit),
            ["GlassSelect"] = ptr => UpdateGlassSelect(ptr.Cast<AddonGlassSelect>()),
            ["LovmPaletteEdit"] = ptr => UpdateTabController(ptr, &ptr.Cast<AddonLovmPaletteEdit>()->TabController, _config.HandleLovmPaletteEdit),
            ["MJIMinionNoteBook"] = ptr => UpdateMJIMinionNoteBook(ptr.Cast<AddonMJIMinionNoteBook>()),
            ["MYCWarResultNotebook"] = ptr => UpdateFieldNotes(ptr.Cast<AddonMYCWarResultNotebook>()),
            ["MiragePrismPrismBox"] = ptr => UpdateMiragePrismPrismBox(ptr.Cast<AddonMiragePrismPrismBox>()),
            ["OrnamentNoteBook"] = ptr => UpdateTabController(ptr, &ptr.Cast<AddonOrnamentNoteBook>()->TabController, _config.HandleOrnamentNoteBook)
        };

        RegisterMultiHandler(handlers, BuddyAddons, ptr => UpdateBuddy());
        RegisterMultiHandler(handlers, CharacterAddons, ptr => UpdateCharacter());
        RegisterMultiHandler(handlers, InventoryAddons, ptr => UpdateInventory());
        RegisterMultiHandler(handlers, InventoryLargeAddons, ptr => UpdateInventoryLarge());
        RegisterMultiHandler(handlers, InventoryExpansionAddons, ptr => UpdateInventoryExpansion());
        RegisterMultiHandler(handlers, InventoryEventAddons, ptr => UpdateInventoryEvent());
        RegisterMultiHandler(handlers, InventoryBuddyAddons, ptr => UpdateInventoryBuddy());
        RegisterMultiHandler(handlers, InventoryRetainerAddons, ptr => UpdateInventoryRetainer());
        RegisterMultiHandler(handlers, InventoryRetainerLargeAddons, ptr => UpdateInventoryRetainerLarge());
        RegisterMultiHandler(handlers, MountMinionBaseAddons, ptr => UpdateMountMinion(ptr.Cast<AddonMinionMountBase>()));

        _handlers = handlers.ToFrozenDictionary();
    }

    private static void RegisterMultiHandler(Dictionary<string, Action<Pointer<AtkUnitBase>>> dict, HashSet<string> names, Action<Pointer<AtkUnitBase>> handler)
    {
        foreach (var name in names)
            dict[name] = handler;
    }

    public override void OnConfigChange(string fieldName)
    {
        if (fieldName == nameof(ScrollableTabsConfiguration.SuppressQuickPanelSounds))
        {
            if (_config.SuppressQuickPanelSounds)
                _quickPanelPlaySoundEffectPatch?.Enable();
            else
                _quickPanelPlaySoundEffectPatch?.Disable();
        }
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (!_clientState.IsLoggedIn)
            return;

        _wheelState = Math.Clamp(UIInputData.Instance()->CursorInputs.MouseWheel, -1, 1);
        if (_wheelState == 0)
            return;

        if (_config.Invert)
            _wheelState *= -1;

        var hoveredUnitBase = RaptureAtkModule.Instance()->AtkCollisionManager.IntersectingAddon;
        if (hoveredUnitBase == null)
        {
            _wheelState = 0;
            return;
        }

        var name = hoveredUnitBase->NameString;
        if (string.IsNullOrEmpty(name))
        {
            _wheelState = 0;
            return;
        }

        if (name == "InventoryCrystalGrid")
        {
            if (_gameConfig.UiConfig.TryGet("ItemInventryWindowSizeType", out uint size) && size == 2)
                UpdateInventoryExpansion();
            else
                UpdateInventoryLarge();

            _wheelState = 0;
            return;
        }

        if (_handlers != null && _handlers.TryGetValue(name, out var handler))
        {
            handler(hoveredUnitBase);
        }

        _wheelState = 0;
    }

    private int GetTabIndex(int currentTabIndex, int numTabs)
        => Math.Clamp(currentTabIndex + _wheelState, 0, numTabs - 1);

    private void UpdateArmouryBoard(AddonArmouryBoard* addon)
    {
        if (!_config.HandleArmouryBoard)
            return;

        var tabIndex = GetTabIndex(addon->TabIndex, NumArmouryBoardTabs);

        if (addon->TabIndex < tabIndex)
            addon->NextTab(0);
        else if (addon->TabIndex > tabIndex)
            addon->PreviousTab(0);
    }

    private void UpdateInventory()
    {
        if (!_config.HandleInventory)
            return;

        if (!TryGetAddon<AddonInventory>("Inventory"u8, out var addon))
            return;

        if (addon->TabIndex == NumInventoryTabs - 1 && _wheelState > 0)
        {
            // Client::UI::AddonInventory.SwitchToKeyItems call in HandleBackButtonInput
            var values = stackalloc AtkValue[3];

            values[0].SetInt(22);
            values[1].SetInt(addon->OpenerAddonId);
            values[2].SetUInt(0);

            addon->FireCallback(3, values);
        }
        else
        {
            var tabIndex = GetTabIndex(addon->TabIndex, NumInventoryTabs);

            if (addon->TabIndex == tabIndex)
                return;

            addon->SetTab(tabIndex);
        }
    }

    private void UpdateInventoryEvent()
    {
        if (!_config.HandleInventory)
            return;

        if (!TryGetAddon<AddonInventoryEvent>("InventoryEvent"u8, out var addon))
            return;

        if (addon->TabIndex == 0 && _wheelState < 0)
        {
            // Client::UI::AddonInventoryEvent.SwitchToInventory call in HandleBackButtonInput
            var values = stackalloc AtkValue[3];

            values[0].SetInt(22);
            values[1].SetInt(addon->OpenerAddonId);
            values[2].SetUInt(2);

            addon->FireCallback(3, values);
        }
        else
        {
            var numEnabledButtons = 0;
            foreach (ref var button in addon->Buttons)
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

    private void UpdateInventoryLarge()
    {
        if (!_config.HandleInventory)
            return;

        if (!TryGetAddon<AddonInventoryLarge>("InventoryLarge"u8, out var addon))
            return;

        var tabIndex = GetTabIndex(addon->TabIndex, NumInventoryLargeTabs);

        if (addon->TabIndex == tabIndex)
            return;

        addon->SetTab(tabIndex);
    }

    private void UpdateInventoryExpansion()
    {
        if (!_config.HandleInventory)
            return;

        if (!TryGetAddon<AddonInventoryExpansion>("InventoryExpansion"u8, out var addon))
            return;

        var tabIndex = GetTabIndex(addon->TabIndex, NumInventoryExpansionTabs);

        if (addon->TabIndex == tabIndex)
            return;

        addon->SetTab(tabIndex, false);
    }

    private void UpdateInventoryRetainer()
    {
        if (!_config.HandleRetainer)
            return;

        if (!TryGetAddon<AddonInventoryRetainer>("InventoryRetainer"u8, out var addon))
            return;

        var tabIndex = GetTabIndex(addon->TabIndex, NumInventoryRetainerTabs);

        if (addon->TabIndex == tabIndex)
            return;

        addon->SetTab(tabIndex);
    }

    private void UpdateInventoryRetainerLarge()
    {
        if (!_config.HandleRetainer)
            return;

        if (!TryGetAddon<AddonInventoryRetainerLarge>("InventoryRetainerLarge"u8, out var addon))
            return;

        var tabIndex = GetTabIndex(addon->TabIndex, NumInventoryRetainerLargeTabs);

        if (addon->TabIndex == tabIndex)
            return;

        addon->SetTab(tabIndex);
    }

    private void UpdateTabController(AtkUnitBase* addon, TabController* tabController, bool isEnabled)
    {
        if (!isEnabled)
            return;

        var tabIndex = GetTabIndex(tabController->TabIndex, tabController->TabCount);

        if (tabController->TabIndex == tabIndex)
            return;

        tabController->TabIndex = tabIndex;
        tabController->CallbackFunction(tabIndex, addon);
    }

    private void UpdateAOZNotebook(AddonAOZNotebook* addon)
    {
        if (!_config.HandleAOZNotebook)
            return;

        var tabIndex = GetTabIndex(addon->TabIndex, addon->TabCount);

        if (addon->TabIndex == tabIndex)
            return;

        addon->SetTab(tabIndex, true);
    }

    private void UpdateAetherCurrent(AddonAetherCurrent* addon)
    {
        if (!_config.HandleAetherCurrent)
            return;

        var tabIndex = GetTabIndex(addon->TabIndex, addon->TabCount);

        if (addon->TabIndex == tabIndex)
            return;

        addon->SetTab(tabIndex);

        for (var i = 0; i < addon->Tabs.Length; i++)
            addon->Tabs[i].Value->IsSelected = i == tabIndex;
    }

    private void UpdateFateProgress(AddonFateProgress* addon)
    {
        if (!_config.HandleFateProgress)
            return;

        var tabIndex = GetTabIndex(addon->TabIndex, addon->TabCount);

        if (!addon->IsLoaded || addon->TabIndex == tabIndex)
            return;

        // fake event, so it can call SetEventIsHandled
        var atkEvent = new AtkEvent();
        addon->SetTab(tabIndex, &atkEvent);
    }

    private void UpdateFieldNotes(AddonMYCWarResultNotebook* addon)
    {
        if (!_config.HandleFieldRecord)
            return;

        if (IntersectingCollisionNode == addon->DescriptionCollisionNode)
            return;

        var atkEvent = new AtkEvent();
        var eventParam = Math.Clamp(addon->CurrentNoteIndex % 10 + _wheelState, -1, addon->MaxNoteIndex - 1);

        if (eventParam == -1)
        {
            if (addon->CurrentPageIndex > 0)
            {
                var page = addon->CurrentPageIndex - 1;
                addon->ReceiveEvent(AtkEventType.ButtonClick, page + 10, &atkEvent);
                addon->ReceiveEvent(AtkEventType.ButtonClick, 9, &atkEvent);
            }
        }
        else if (eventParam == 10)
        {
            if (addon->CurrentPageIndex < 4)
            {
                var page = addon->CurrentPageIndex + 1;
                addon->ReceiveEvent(AtkEventType.ButtonClick, page + 10, &atkEvent);
            }
        }
        else
        {
            addon->ReceiveEvent(AtkEventType.ButtonClick, eventParam, &atkEvent);
        }
    }

    private void UpdateMountMinion(AddonMinionMountBase* addon)
    {
        var isEnabled = addon->NameString switch
        {
            "MinionNoteBook" => _config.HandleMinionNoteBook,
            "MountNoteBook" => _config.HandleMountNoteBook,
            _ => false,
        };

        if (!isEnabled)
            return;

        if (addon->CurrentView == AddonMinionMountBase.ViewType.Normal)
        {
            if (addon->TabController.TabIndex == 0 && _wheelState < 0)
            {
                addon->SwitchToFavorites();
            }
            else
            {
                UpdateTabController((AtkUnitBase*)addon, &addon->TabController, true);
            }
        }
        else if (addon->CurrentView == AddonMinionMountBase.ViewType.Favorites && _wheelState > 0)
        {
            addon->TabController.CallbackFunction(0, (AtkUnitBase*)addon);
        }
    }

    private void UpdateMJIMinionNoteBook(AddonMJIMinionNoteBook* addon)
    {
        if (!_config.HandleMJIMinionNoteBook)
            return;

        var agent = AgentMJIMinionNoteBook.Instance();

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
                UpdateTabController((AtkUnitBase*)addon, &addon->TabController, true);
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

    private void UpdateCurrency(AddonCurrency* addon)
    {
        if (!_config.HandleCurrency)
            return;

        var atkStage = AtkStage.Instance();
        var numberArray = atkStage->GetNumberArrayData(NumberArrayType.Currency);
        var currentTab = numberArray->IntArray[0];
        var newTab = currentTab;

        var enableStates = new bool[addon->Tabs.Length];
        for (var i = 0; i < addon->Tabs.Length; i++)
            enableStates[i] = addon->Tabs[i].Value != null && addon->Tabs[i].Value->IsEnabled;

        if (_wheelState > 0 && currentTab < enableStates.Length)
        {
            for (var i = currentTab + 1; i < enableStates.Length; i++)
            {
                if (enableStates[i])
                {
                    newTab = i;
                    break;
                }
            }
        }
        else if (currentTab > 0)
        {
            for (var i = currentTab - 1; i >= 0; i--)
            {
                if (enableStates[i])
                {
                    newTab = i;
                    break;
                }
            }
        }

        if (currentTab == newTab)
            return;

        numberArray->SetValue(0, newTab);
        addon->OnRequestedUpdate(atkStage->GetNumberArrayData(), atkStage->GetStringArrayData());
    }

    private void UpdateInventoryBuddy()
    {
        if (!_config.HandleInventoryBuddy)
            return;

        if (!PlayerState.Instance()->HasPremiumSaddlebag)
            return;

        if (!TryGetAddon<AddonInventoryBuddy>("InventoryBuddy"u8, out var addon))
            return;

        var tabIndex = GetTabIndex(addon->TabIndex, 2);

        if (addon->TabIndex == tabIndex)
            return;

        addon->SetTab((byte)tabIndex);
    }

    private void UpdateBuddy()
    {
        if (!_config.HandleBuddy)
            return;

        if (!TryGetAddon<AddonBuddy>("Buddy"u8, out var addon))
            return;

        var tabIndex = GetTabIndex(addon->TabIndex, NumBuddyTabs);

        if (addon->TabIndex == tabIndex)
            return;

        addon->SetTab(tabIndex);

        for (var i = 0; i < NumBuddyTabs; i++)
        {
            var button = addon->RadioButtons.GetPointer(i);
            if (button->Value != null)
                button->Value->IsSelected = i == addon->TabIndex;
        }
    }

    private void UpdateMiragePrismPrismBox(AddonMiragePrismPrismBox* addon)
    {
        if (!_config.HandleMiragePrismPrismBox)
            return;

        if (addon->JobDropdown == null ||
            addon->JobDropdown->List == null ||
            addon->JobDropdown->List->OwnerNode == null ||
            addon->JobDropdown->List->OwnerNode->IsVisible())
        {
            return;
        }

        if (addon->OrderDropdown == null ||
            addon->OrderDropdown->List == null ||
            addon->OrderDropdown->List->OwnerNode == null ||
            addon->OrderDropdown->List->OwnerNode->IsVisible())
        {
            return;
        }

        var prevButton = !_config.Invert ? addon->PrevButton : addon->NextButton;
        var nextButton = !_config.Invert ? addon->NextButton : addon->PrevButton;

        if (prevButton == null || (IsPrev && !prevButton->IsEnabled))
            return;

        if (nextButton == null || (IsNext && !nextButton->IsEnabled))
            return;

        if (IsAddonOpen("MiragePrismPrismBoxFilter"u8))
            return;

        var agent = AgentMiragePrismPrismBox.Instance();
        agent->PageIndex += (byte)_wheelState;
        agent->UpdateItems(false, false);
    }

    private void UpdateGlassSelect(AddonGlassSelect* addon)
    {
        if (!_config.HandleGlassSelect)
            return;

        UpdateTabController((AtkUnitBase*)addon, &addon->TabController, true);

        for (var i = 0; i < addon->TabController.TabCount; i++)
        {
            var button = addon->Tabs.GetPointer(i);
            if (button->Value != null)
                button->Value->IsSelected = i == addon->TabController.TabIndex;
        }
    }

    private void UpdateCharacter()
    {
        if (!_config.HandleCharacter)
            return;

        if (!TryGetAddon<AddonCharacter>("Character"u8, out var addon))
            return;

        if (!addon->AddonControl.IsChildSetupComplete)
            return;

        if (IntersectingCollisionNode == addon->PreviewController.CollisionNode)
            return;

        var tabIndex = GetTabIndex(addon->TabIndex, addon->TabCount);

        if (addon->TabIndex == tabIndex)
            return;

        addon->SetTab(tabIndex);

        for (var i = 0; i < addon->TabCount; i++)
        {
            var button = addon->Tabs.GetPointer(i);
            if (button->Value != null)
                button->Value->IsSelected = i == addon->TabIndex;
        }
    }

    private void UpdateCharacterClass(AddonCharacterClass* addon)
    {
        // prev or next embedded addon
        if (!_config.HandleCharacterClass || addon->TabIndex + _wheelState < 0 || addon->TabIndex + _wheelState > 1)
        {
            UpdateCharacter();
            return;
        }

        var tabIndex = GetTabIndex(addon->TabIndex, 2);

        if (addon->TabIndex == tabIndex)
            return;

        addon->SetTab(tabIndex);
    }

    private void UpdateCharacterRepute(AddonCharacterRepute* addon)
    {
        if (addon->ExpansionsDropDownList == null || addon->ExpansionsDropDownList->IsOpen)
            return;

        var currentIndex = addon->ExpansionsDropDownList->GetSelectedItemIndex();

        // prev embedded addon
        if (!_config.HandleCharacterRepute || currentIndex + _wheelState < 0)
        {
            UpdateCharacter();
            return;
        }

        var itemCount = addon->ExpansionsDropDownList->List->GetItemCount();
        var tabIndex = GetTabIndex(currentIndex, itemCount);
        if (currentIndex == tabIndex)
            return;

        var atkEvent = new AtkEvent();
        var data = new AtkEventData();
        data.ListItemData.SelectedIndex = tabIndex;
        addon->AtkUnitBase.ReceiveEvent(AtkEventType.ListItemHighlight, 0, &atkEvent, &data);

        addon->ExpansionsDropDownList->SelectItem(tabIndex);
    }
}
