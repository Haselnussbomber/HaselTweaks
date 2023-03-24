using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Structs;
using HaselAtkComponentRadioButton = HaselTweaks.Structs.AtkComponentRadioButton;

namespace HaselTweaks.Tweaks;

public unsafe partial class ScrollableTabs : Tweak
{
    public override string Name => "Scrollable Tabs";
    public override string Description => "Enables mouse wheel to switch tabs (like with LB/RB on controllers).";
    public static Configuration Config => Plugin.Config.Tweaks.ScrollableTabs;

    public class Configuration
    {
        [ConfigField]
        public bool Invert = true;

        [ConfigField(Label = "Enable in Aether Currents")]
        public bool HandleAetherCurrent = true;

        [ConfigField(Label = "Enable in Armoury Chest")]
        public bool HandleArmouryBoard = true;

        [ConfigField(Label = "Enable in Blue Magic Spellbook")]
        public bool HandleAOZNotebook = true;

        [ConfigField(Label = "Enable in Fashion Accessories")]
        public bool HandleOrnamentNoteBook = true;

        [ConfigField(Label = "Enable in Field Records")]
        public bool HandleFieldRecord = true;

        [ConfigField(Label = "Enable in Fish Guide")]
        public bool HandleFishGuide = true;

        [ConfigField(Label = "Enable in Gold Saucer -> Card List")]
        public bool HandleGoldSaucerCardList = true;

        [ConfigField(Label = "Enable in Gold Saucer -> Decks -> Edit Deck")]
        public bool HandleGoldSaucerCardDeckEdit = true;

        [ConfigField(Label = "Enable in Gold Saucer -> Lord of Verminion -> Minion Hotbar")]
        public bool HandleLovmPaletteEdit = true;

        [ConfigField(Label = "Enable in Inventory")]
        public bool HandleInventory = true;

        [ConfigField(Label = "Enable in Island Minion Guide")]
        public bool HandleMJIMinionNoteBook = true;

        [ConfigField(Label = "Enable in Minions")]
        public bool HandleMinionNoteBook = true;

        [ConfigField(Label = "Enable in Mounts")]
        public bool HandleMountNoteBook = true;

        [ConfigField(Label = "Enable in Retainer Inventory")]
        public bool HandleRetainer = true;

        [ConfigField(Label = "Enable in Shared FATE")]
        public bool HandleFateProgress = true;

        [ConfigField(Label = "Enable in Sightseeing Log")]
        public bool HandleAdventureNoteBook = true;
    }

    private const uint WM_MOUSEWHEEL = 0x020A;
    private const uint WHEEL_DELTA = 120;

    private short wheelState;

    [SigHook("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 49 8B F8 C6 05")]
    private ulong WindowProcHandler(nint hwnd, int uMsg, int wParam)
    {
        if (uMsg == WM_MOUSEWHEEL)
            wheelState = (short)Math.Clamp((wParam >> 16) / WHEEL_DELTA * (Config.Invert ? -1 : 1), -1, 1);

        return WindowProcHandlerHook.Original(hwnd, uMsg, wParam);
    }

    public override void OnFrameworkUpdate(Dalamud.Game.Framework framework)
    {
        if (wheelState == 0)
            return;

        var hoveredUnitBase = Framework.Instance()->GetUiModule()->GetRaptureAtkModule()->AtkModule.IntersectingAddon;
        if (hoveredUnitBase == null)
            goto ResetWheelState;

        var name = Marshal.PtrToStringAnsi((nint)hoveredUnitBase->Name);
        if (string.IsNullOrEmpty(name))
            goto ResetWheelState;

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
                break;

            // used by Inventory
            case "InventoryGrid":
            case "InventoryGridCrystal":
                name = "Inventory";
                break;

            // Key Items (part of Inventory)
            case "InventoryEventGrid":
                name = "InventoryEvent";
                break;

            // used by InventoryLarge or InventoryExpansion
            case "InventoryCrystalGrid":
                name = "InventoryLarge";
                if (GetAddon(name) == null)
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

            default:
#if DEBUG
                Verbose($"Unhandled AtkUnitBase: {name}");
#endif
                goto ResetWheelState;
        }

        if (!GetAddon(name, out var unitBase))
            goto ResetWheelState;

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
            UpdateMountMinion((MountMinionNoteBookBase*)unitBase);
        }
        else if (Config.HandleFishGuide && name == "FishGuide2")
        {
            UpdateTabSwitcher((nint)unitBase, &((AddonFishGuide2*)unitBase)->TabSwitcher);
        }
        else if (Config.HandleAdventureNoteBook && name == "AdventureNoteBook")
        {
            UpdateTabSwitcher((nint)unitBase, &((AddonAdventureNoteBook*)unitBase)->TabSwitcher);
        }
        else if (Config.HandleOrnamentNoteBook && name == "OrnamentNoteBook")
        {
            UpdateTabSwitcher((nint)unitBase, &((AddonOrnamentNoteBook*)unitBase)->TabSwitcher);
        }
        else if (Config.HandleGoldSaucerCardList && name == "GSInfoCardList")
        {
            UpdateTabSwitcher((nint)unitBase, &((AddonGoldSaucerCardList*)unitBase)->TabSwitcher);
        }
        else if (Config.HandleGoldSaucerCardDeckEdit && name == "GSInfoEditDeck")
        {
            UpdateTabSwitcher((nint)unitBase, &((AddonGoldSaucerCardDeckEdit*)unitBase)->TabSwitcher);
        }
        else if (Config.HandleLovmPaletteEdit && name == "LovmPaletteEdit")
        {
            UpdateTabSwitcher((nint)unitBase, &((AddonLovmPaletteEdit*)unitBase)->TabSwitcher);
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
            UpdateMJIMountMinion((AddonMJIMinionNoteBook*)unitBase);
        }

        ResetWheelState:
        wheelState = 0;
    }

    private int GetTabIndex(int currentTabIndex, int numTabs)
    {
        return Math.Clamp(currentTabIndex + wheelState, 0, numTabs - 1);
    }

    private void UpdateArmouryBoard(AddonArmouryBoard* addon)
    {
        var tabIndex = GetTabIndex(addon->TabIndex, AddonArmouryBoard.NUM_TABS);

        if (addon->TabIndex < tabIndex)
            addon->NextTab(0);
        else if (addon->TabIndex > tabIndex)
            addon->PreviousTab(0);
    }

    private void UpdateInventory(AddonInventory* addon)
    {
        if (addon->TabIndex == AddonInventory.NUM_TABS - 1 && wheelState > 0)
        {
            addon->SwitchToInventoryEvent(0);
        }
        else
        {
            var tabIndex = GetTabIndex(addon->TabIndex, AddonInventory.NUM_TABS);

            if (addon->TabIndex == tabIndex)
                return;

            addon->SetTab(tabIndex);
        }
    }

    private void UpdateInventoryEvent(AddonInventoryEvent* addon)
    {
        if (addon->TabIndex == 0 && wheelState < 0)
        {
            addon->SwitchToInventory(0);
        }
        else
        {
            var tabIndex = GetTabIndex(addon->TabIndex, addon->NumTabs);

            if (addon->TabIndex == tabIndex)
                return;

            addon->SetTab(tabIndex);
        }
    }

    private void UpdateInventoryLarge(AddonInventoryLarge* addon)
    {
        var tabIndex = GetTabIndex(addon->TabIndex, AddonInventoryLarge.NUM_TABS);

        if (addon->TabIndex == tabIndex)
            return;

        addon->SetTab(tabIndex);
    }

    private void UpdateInventoryExpansion(AddonInventoryExpansion* addon)
    {
        var tabIndex = GetTabIndex(addon->TabIndex, AddonInventoryExpansion.NUM_TABS);

        if (addon->TabIndex == tabIndex)
            return;

        addon->SetTab(tabIndex, false);
    }

    private void UpdateInventoryRetainer(AddonInventoryRetainer* addon)
    {
        var tabIndex = GetTabIndex(addon->TabIndex, AddonInventoryRetainer.NUM_TABS);

        if (addon->TabIndex == tabIndex)
            return;

        addon->SetTab(tabIndex);
    }

    private void UpdateInventoryRetainerLarge(AddonInventoryRetainerLarge* addon)
    {
        var tabIndex = GetTabIndex(addon->TabIndex, AddonInventoryRetainerLarge.NUM_TABS);

        if (addon->TabIndex == tabIndex)
            return;

        addon->SetTab(tabIndex);
    }

    private void UpdateTabSwitcher(nint addon, TabSwitcher* tabSwitcher)
    {
        var tabIndex = GetTabIndex(tabSwitcher->CurrentTabIndex, tabSwitcher->NumTabs);

        if (tabSwitcher->CurrentTabIndex == tabIndex)
            return;

        tabSwitcher->CurrentTabIndex = tabIndex;

        if (tabSwitcher->CallbackPtr == 0)
            return;

        Marshal.GetDelegateForFunctionPointer<TabSwitcher.CallbackDelegate>(tabSwitcher->CallbackPtr)(tabIndex, addon);
    }

    private void UpdateAOZNotebook(AddonAOZNotebook* addon)
    {
        var tabIndex = GetTabIndex(addon->TabIndex, addon->NumTabs);

        if (addon->TabIndex == tabIndex)
            return;

        addon->SetTab(tabIndex, true);
    }

    private void UpdateAetherCurrent(AddonAetherCurrent* addon)
    {
        var tabIndex = GetTabIndex(addon->TabIndex, addon->NumTabs);
        if (addon->TabIndex == tabIndex) return;

        addon->SetTab(tabIndex);

        var tabs = (nint)addon + 0x228;
        for (var i = 0; i < addon->NumTabs; i++)
        {
            // WAYTOODANK, this is basically like writing addon->Tabs[i]
            // but because this is dynamic (depending on NumTabs), we can't do that... thanks, C#!
            var button = *(HaselAtkComponentRadioButton**)(tabs + i * 8);
            button->SetSelected(i == tabIndex);
        }
    }

    private void UpdateFateProgress(AddonFateProgress* addon)
    {
        var tabIndex = GetTabIndex(addon->TabIndex, addon->NumTabs);
        if (!addon->Loaded || addon->TabIndex == tabIndex)
            return;

        // fake event, so it can call SetEventIsHandled
        var atkEvent = Marshal.AllocHGlobal(30);
        addon->SetTab(tabIndex, atkEvent);
        Marshal.FreeHGlobal(atkEvent);
    }

    private void UpdateFieldNotes(AddonMYCWarResultNotebook* addon)
    {
        if (Framework.Instance()->GetUiModule()->GetRaptureAtkModule()->AtkModule.IntersectingCollisionNode == addon->DescriptionCollisionNode)
            return;

        var atkEvent = (AtkEvent*)IMemorySpace.GetUISpace()->Malloc<AtkEvent>();
        var eventParam = Math.Clamp(addon->CurrentNoteIndex % 10 + wheelState, -1, addon->MaxNoteIndex - 1);

        if (eventParam == -1)
        {
            if (addon->CurrentPageIndex > 0)
            {
                var page = addon->CurrentPageIndex - 1;
                addon->ReceiveEvent(AtkEventType.ButtonClick, page + 10, atkEvent, 0);
                addon->ReceiveEvent(AtkEventType.ButtonClick, 9, atkEvent, 0);
            }
        }
        else if (eventParam == 10)
        {
            if (addon->CurrentPageIndex < 4)
            {
                var page = addon->CurrentPageIndex + 1;
                addon->ReceiveEvent(AtkEventType.ButtonClick, page + 10, atkEvent, 0);
            }
        }
        else
        {
            addon->ReceiveEvent(AtkEventType.ButtonClick, eventParam, atkEvent, 0);
        }

        IMemorySpace.Free(atkEvent);
    }

    private void UpdateMountMinion(MountMinionNoteBookBase* addon)
    {
        if (addon->CurrentView == MountMinionNoteBookBase.ViewType.Normal)
        {
            if (addon->TabSwitcher.CurrentTabIndex == 0 && wheelState < 0)
            {
                addon->SwitchToFavorites();
            }
            else
            {
                UpdateTabSwitcher((nint)addon, &addon->TabSwitcher);
            }
        }
        else if (addon->CurrentView == MountMinionNoteBookBase.ViewType.Favorites && wheelState > 0)
        {
            var callbackAddress = addon->TabSwitcher.CallbackPtr;
            if (callbackAddress != 0)
                Marshal.GetDelegateForFunctionPointer<TabSwitcher.CallbackDelegate>(callbackAddress)(0, (nint)addon);
        }
    }

    private void UpdateMJIMountMinion(AddonMJIMinionNoteBook* addon)
    {
        var agent = GetAgent<AgentMJIMinionNoteBook>(AgentId.MJIMinionNoteBook);
        if (agent == null)
            return;

        if (agent->CurrentView == AgentMJIMinionNoteBook.ViewType.Normal)
        {
            if (addon->Unk220.TabSwitcher.CurrentTabIndex == 0 && wheelState < 0)
            {
                agent->CurrentView = AgentMJIMinionNoteBook.ViewType.Favorites;
                agent->SelectedFavoriteMinion.TabIndex = 0;
                agent->SelectedFavoriteMinion.SlotIndex = agent->SelectedNormalMinion.SlotIndex;
                agent->SelectedFavoriteMinion.MinionId = agent->GetSelectedMinionId();
                agent->SelectedMinion = &agent->SelectedFavoriteMinion;
                agent->UpdateTabFlags(0x407);
            }
            else
            {
                UpdateTabSwitcher((nint)addon, &addon->Unk220.TabSwitcher);
                agent->UpdateTabFlags(0x40B);
            }
        }
        else if (agent->CurrentView == AgentMJIMinionNoteBook.ViewType.Favorites && wheelState > 0)
        {
            agent->CurrentView = AgentMJIMinionNoteBook.ViewType.Normal;
            agent->SelectedNormalMinion.TabIndex = 0;
            agent->SelectedNormalMinion.SlotIndex = agent->SelectedFavoriteMinion.SlotIndex;
            agent->SelectedNormalMinion.MinionId = agent->GetSelectedMinionId();
            agent->SelectedMinion = &agent->SelectedNormalMinion;

            addon->Unk220.TabSwitcher.CurrentTabIndex = 0;

            var callbackAddress = addon->Unk220.TabSwitcher.CallbackPtr;
            if (callbackAddress != 0)
                Marshal.GetDelegateForFunctionPointer<TabSwitcher.CallbackDelegate>(callbackAddress)(0, (nint)addon);

            agent->UpdateTabFlags(0x40B);
        }
    }
}
