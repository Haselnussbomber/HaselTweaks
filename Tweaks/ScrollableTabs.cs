using System;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Structs;
using HaselTweaks.Utils;

namespace HaselTweaks.Tweaks;

public unsafe class ScrollableTabs : Tweak
{
    public override string Name => "Scrollable Tabs";
    public override string Description => "Enables mouse wheel to switch tabs (like with LB/RB on controllers).";
    public static Configuration Config => HaselTweaks.Configuration.Instance.Tweaks.ScrollableTabs;

    public class Configuration
    {
        [ConfigField(SeparatorAfter = true)]
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

    // called via ArmouryBoard_ReceiveEvent event/case 12 -> case a4 == 16
    [Signature("E8 ?? ?? ?? ?? EB E0 84 C9")]
    private ArmouryBoardNextTabDelegate ArmouryBoardNextTab { get; init; } = null!;
    private delegate void ArmouryBoardNextTabDelegate(AddonArmouryBoard* addon, byte a2);

    // called via ArmouryBoard_ReceiveEvent event/case 12 -> after switch (a4 == 17)
    [Signature("40 53 48 83 EC 20 80 B9 ?? ?? ?? ?? ?? 48 8B D9 75 11")]
    private ArmouryBoardPreviousTabDelegate ArmouryBoardPreviousTab { get; init; } = null!;
    private delegate void ArmouryBoardPreviousTabDelegate(AddonArmouryBoard* addon, byte a2);

    // called via Inventory vf67
    [Signature("E9 ?? ?? ?? ?? 83 FD 10")]
    private InventorySetTabDelegate InventorySetTab { get; init; } = null!;
    private delegate void InventorySetTabDelegate(AddonInventory* addon, int tab);

    [Signature("E9 ?? ?? ?? ?? CC CC CC CC CC CC CC CC CC CC 48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 44 88 44 24")]
    private SwitchToInventoryEventDelegate SwitchToInventoryEvent { get; init; } = null!;
    private delegate void SwitchToInventoryEventDelegate(AddonInventory* addon, byte a2);

    [Signature("E8 ?? ?? ?? ?? B0 01 EB 02 32 C0 48 8B 5C 24 ?? 48 8B 6C 24 ?? 48 8B 74 24 ?? 48 83 C4 30 41 5F 41 5E 41 5D 41 5C 5F C3 CC CC CC CC CC CC CC")]
    private SwitchToInventoryDelegate SwitchToInventory { get; init; } = null!;
    private delegate void SwitchToInventoryDelegate(AddonInventoryEvent* addon, byte a2);

    [Signature("E8 ?? ?? ?? ?? EB 09 83 FF 01")]
    private InventoryEventSetTabDelegate InventoryEventSetTab { get; init; } = null!;
    private delegate void InventoryEventSetTabDelegate(AddonInventoryEvent* addon, int tab);

    // called via InventoryLarge vf67
    [Signature("E9 ?? ?? ?? ?? 41 83 FF 46")]
    private InventoryLargeSetTabDelegate InventoryLargeSetTab { get; init; } = null!;
    private delegate void InventoryLargeSetTabDelegate(AddonInventoryLarge* addon, int tab);

    // called via InventoryExpansion vf67
    [Signature("E8 ?? ?? ?? ?? BB ?? ?? ?? ?? 83 EB 01")]
    private InventoryExpansionSetTabDelegate InventoryExpansionSetTab { get; init; } = null!;
    private delegate void InventoryExpansionSetTabDelegate(AddonInventoryExpansion* addon, int tab, bool force);

    // called via RetainerInventory vf67
    [Signature("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 70 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 44 24 ?? 48 8B F1 48 8B 89 ?? ?? ?? ??")]
    private RetainerInventorySetTabDelegate RetainerInventorySetTab { get; init; } = null!;
    private delegate void RetainerInventorySetTabDelegate(AddonInventoryRetainer* addon, int tab);

    // called via RetainerInventoryLarge vf67
    [Signature("E8 ?? ?? ?? ?? 48 83 C4 38 41 5E 41 5D C3")]
    private RetainerInventoryLargeSetTabDelegate RetainerInventoryLargeSetTab { get; init; } = null!;
    private delegate void RetainerInventoryLargeSetTabDelegate(AddonInventoryRetainerLarge* addon, int tab);

    // called via AOZNotebook vf67
    [Signature("E8 ?? ?? ?? ?? 33 D2 49 8B CF E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 41 8D 40 F7")]
    private AOZNotebookSetTabDelegate AOZNotebookSetTab { get; init; } = null!;
    private delegate void AOZNotebookSetTabDelegate(AddonAOZNotebook* addon, int tab, bool a3);

    // called via FateProgress vf67
    [Signature("83 FA 01 0F 87 ?? ?? ?? ?? 48 89 5C 24 ?? 48 89 6C 24")]
    private FateProgressSetTabDelegate FateProgressSetTab { get; init; } = null!;
    private delegate void FateProgressSetTabDelegate(AddonFateProgress* addon, int tab, IntPtr atkEvent);

    // called in AetherCurrent vf54
    [Signature("E8 ?? ?? ?? ?? 84 C0 74 65 39 9D")]
    private AetherCurrentSetTabDelegate AetherCurrentSetTab { get; init; } = null!;
    private delegate void AetherCurrentSetTabDelegate(AddonAetherCurrent* addon, int tab);

    [Signature("48 83 EC 38 44 8B 89")]
    private RadioButtonSetActiveDelegate RadioButtonSetActive { get; init; } = null!;
    private delegate void RadioButtonSetActiveDelegate(IntPtr button, bool active);

    [AutoHook, Signature("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 49 8B F8 C6 05", DetourName = nameof(WindowProcHandlerDetour))]
    private Hook<WindowProcHandlerDelegate> WindowProcHandlerHook { get; init; } = null!;
    private delegate ulong WindowProcHandlerDelegate(IntPtr hWnd, int uMsg, int wParam);

    [Signature("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 8D 42 D3 83 F8 08")]
    private MountMinionSwitchToFavoritesDelegate MountMinionSwitchToFavorites { get; init; } = null!;
    private delegate void MountMinionSwitchToFavoritesDelegate(AtkUnitBase* addon);

    private const uint WM_MOUSEWHEEL = 0x020A;
    private const uint WHEEL_DELTA = 120;

    private short wheelState;

    private ulong WindowProcHandlerDetour(IntPtr hwnd, int uMsg, int wParam)
    {
        if (uMsg == WM_MOUSEWHEEL)
            wheelState = (short)Math.Clamp((wParam >> 16) / WHEEL_DELTA * (Config.Invert ? -1 : 1), -1, 1);

        return WindowProcHandlerHook.Original(hwnd, uMsg, wParam);
    }

    public override void OnFrameworkUpdate(Dalamud.Game.Framework framework)
    {
        if (wheelState == 0) return;

        var hoveredUnitBase = AtkUtils.GetHighestAtkUnitBaseAtPosition();
        if (hoveredUnitBase == null) goto ResetWheelState;

        var name = Marshal.PtrToStringAnsi((IntPtr)hoveredUnitBase->Name);
        if (string.IsNullOrEmpty(name)) goto ResetWheelState;

        // parent lookup
        switch (name)
        {
            // use these directly
            case "AetherCurrent":          // Aether Currents
            case "ArmouryBoard":           // Armoury Chest
            case "AOZNotebook":            // Blue Magic Spellbook
            case "OrnamentNoteBook":       // Fashion Accessories
            case "MYCWarResultNotebook":   // Field Records
            case "FishGuide":              // Fish Guide
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
                if (AtkUtils.GetUnitBase(name) == null)
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

        var unitBase = AtkUtils.GetUnitBase(name);
        if (unitBase == null || !unitBase->IsVisible)
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
            UpdateMountMinion(unitBase);
        }
        else if (Config.HandleFishGuide && name == "FishGuide")
        {
            UpdateTabSwitcher((IntPtr)unitBase, ((AddonFishGuide*)unitBase)->TabSwitcher);
        }
        else if (Config.HandleAdventureNoteBook && name == "AdventureNoteBook")
        {
            UpdateTabSwitcher((IntPtr)unitBase, ((AddonAdventureNoteBook*)unitBase)->TabSwitcher);
        }
        else if (Config.HandleOrnamentNoteBook && name == "OrnamentNoteBook")
        {
            UpdateTabSwitcher((IntPtr)unitBase, ((AddonOrnamentNoteBook*)unitBase)->TabSwitcher);
        }
        else if (Config.HandleGoldSaucerCardList && name == "GSInfoCardList")
        {
            UpdateTabSwitcher((IntPtr)unitBase, ((AddonGoldSaucerCardList*)unitBase)->TabSwitcher);
        }
        else if (Config.HandleGoldSaucerCardDeckEdit && name == "GSInfoEditDeck")
        {
            UpdateTabSwitcher((IntPtr)unitBase, ((AddonGoldSaucerCardDeckEdit*)unitBase)->TabSwitcher);
        }
        else if (Config.HandleLovmPaletteEdit && name == "LovmPaletteEdit")
        {
            UpdateTabSwitcher((IntPtr)unitBase, ((AddonLovmPaletteEdit*)unitBase)->TabSwitcher);
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

        if (addon->TabIndex < tabIndex) ArmouryBoardNextTab(addon, 0);
        else if (addon->TabIndex > tabIndex) ArmouryBoardPreviousTab(addon, 0);
    }

    private void UpdateInventory(AddonInventory* addon)
    {
        if (addon->TabIndex == AddonInventory.NUM_TABS - 1 && wheelState > 0)
        {
            SwitchToInventoryEvent(addon, 0);
        }
        else
        {
            var tabIndex = GetTabIndex(addon->TabIndex, AddonInventory.NUM_TABS);
            if (addon->TabIndex == tabIndex) return;

            InventorySetTab(addon, tabIndex);
        }
    }

    private void UpdateInventoryEvent(AddonInventoryEvent* addon)
    {
        if (addon->TabIndex == 0 && wheelState < 0)
        {
            SwitchToInventory(addon, 0);
        }
        else
        {
            var tabIndex = GetTabIndex(addon->TabIndex, addon->NumTabs);
            if (addon->TabIndex == tabIndex) return;

            InventoryEventSetTab(addon, tabIndex);
        }
    }

    private void UpdateInventoryLarge(AddonInventoryLarge* addon)
    {
        var tabIndex = GetTabIndex(addon->TabIndex, AddonInventoryLarge.NUM_TABS);
        if (addon->TabIndex == tabIndex) return;

        InventoryLargeSetTab(addon, tabIndex);
    }

    private void UpdateInventoryExpansion(AddonInventoryExpansion* addon)
    {
        var tabIndex = GetTabIndex(addon->TabIndex, AddonInventoryExpansion.NUM_TABS);
        if (addon->TabIndex == tabIndex) return;

        InventoryExpansionSetTab(addon, tabIndex, false);
    }

    private void UpdateInventoryRetainer(AddonInventoryRetainer* addon)
    {
        var tabIndex = GetTabIndex(addon->TabIndex, AddonInventoryRetainer.NUM_TABS);
        if (addon->TabIndex == tabIndex) return;

        RetainerInventorySetTab(addon, tabIndex);
    }

    private void UpdateInventoryRetainerLarge(AddonInventoryRetainerLarge* addon)
    {
        var tabIndex = GetTabIndex(addon->TabIndex, AddonInventoryRetainerLarge.NUM_TABS);
        if (addon->TabIndex == tabIndex) return;

        RetainerInventoryLargeSetTab(addon, tabIndex);
    }

    private void UpdateTabSwitcher(IntPtr addon, TabSwitcher tabSwitcher)
    {
        var tabIndex = GetTabIndex(tabSwitcher.CurrentTabIndex, tabSwitcher.NumTabs);
        if (tabSwitcher.CurrentTabIndex == tabIndex) return;

        var callbackAddress = (IntPtr)tabSwitcher.Callback;
        if (callbackAddress == IntPtr.Zero) return;

        Marshal.GetDelegateForFunctionPointer<TabSwitcher.CallbackDelegate>(callbackAddress)(tabIndex, addon);
    }

    private void UpdateAOZNotebook(AddonAOZNotebook* addon)
    {
        var tabIndex = GetTabIndex(addon->TabIndex, addon->NumTabs);
        if (addon->TabIndex == tabIndex) return;

        AOZNotebookSetTab(addon, tabIndex, true);
    }

    private void UpdateAetherCurrent(AddonAetherCurrent* addon)
    {
        var tabIndex = GetTabIndex(addon->TabIndex, addon->NumTabs);
        if (addon->TabIndex == tabIndex) return;

        AetherCurrentSetTab(addon, tabIndex);

        var tabs = (IntPtr)addon + 0x228;
        for (var i = 0; i < addon->NumTabs; i++)
        {
            // WAYTOODANK, this is basically like writing addon->Tabs[i]
            // but because this is dynamic (depending on NumTabs), we can't do that... thanks, C#!
            var tabPtr = *(IntPtr*)(tabs + i * 8);
            RadioButtonSetActive(tabPtr, i == tabIndex);
        }
    }

    private void UpdateFateProgress(AddonFateProgress* addon)
    {
        var tabIndex = GetTabIndex(addon->TabIndex, addon->NumTabs);
        if (addon->TabIndex == tabIndex) return;
        if (!addon->Loaded) return;

        // fake event, so it can call SetEventIsHandled
        var atkEvent = Marshal.AllocHGlobal(30);
        FateProgressSetTab(addon, tabIndex, atkEvent);
        Marshal.FreeHGlobal(atkEvent);
    }

    private void UpdateFieldNotes(AddonMYCWarResultNotebook* addon)
    {
        if (AtkUtils.IsCursorIntersecting(addon->AtkUnitBase.UldManager, addon->DescriptionCollisionNode))
        {
            return;
        }

        var atkEvent = (AtkEvent*)IMemorySpace.GetUISpace()->Malloc<AtkEvent>();
        var eventParam = Math.Clamp(addon->CurrentNoteIndex % 10 + wheelState, -1, addon->MaxNoteIndex - 1);

        if (eventParam == -1)
        {
            if (addon->CurrentPageIndex > 0)
            {
                var page = addon->CurrentPageIndex - 1;
                addon->vtbl->ReceiveEvent(addon, AtkEventType.ButtonClick, page + 10, atkEvent);
                addon->vtbl->ReceiveEvent(addon, AtkEventType.ButtonClick, 9, atkEvent);
            }
        }
        else if (eventParam == 10)
        {
            if (addon->CurrentPageIndex < 4)
            {
                var page = addon->CurrentPageIndex + 1;
                addon->vtbl->ReceiveEvent(addon, AtkEventType.ButtonClick, page + 10, atkEvent);
            }
        }
        else
        {
            addon->vtbl->ReceiveEvent(addon, AtkEventType.ButtonClick, eventParam, atkEvent);
        }

        IMemorySpace.Free(atkEvent);
    }

    private void UpdateMountMinion(AtkUnitBase* unitBase)
    {
        var addon = (MountMinionNoteBookBase*)unitBase;

        if (addon->CurrentView == MountMinionNoteBookBase.ViewType.Normal)
        {
            if (addon->TabSwitcher.CurrentTabIndex == 0 && wheelState < 0)
            {
                MountMinionSwitchToFavorites(unitBase);
            }
            else
            {
                UpdateTabSwitcher((IntPtr)addon, addon->TabSwitcher);
            }
        }
        else if (addon->CurrentView == MountMinionNoteBookBase.ViewType.Favorites && wheelState > 0)
        {
            var callbackAddress = (IntPtr)addon->TabSwitcher.Callback;
            if (callbackAddress == IntPtr.Zero)
            {
                return;
            }

            Marshal.GetDelegateForFunctionPointer<TabSwitcher.CallbackDelegate>(callbackAddress)(0, (IntPtr)unitBase);
        }
    }
}
