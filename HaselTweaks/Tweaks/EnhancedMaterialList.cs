using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Inventory.InventoryEventArgTypes;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Services;
using HaselTweaks.Config;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;
using HaselTweaks.Structs;
using Lumina.Excel.Sheets;
using Lumina.Extensions;
using Lumina.Text;
using Lumina.Text.Payloads;
using Lumina.Text.ReadOnly;
using Microsoft.Extensions.Logging;
using Character = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class EnhancedMaterialList : IConfigurableTweak
{
    private readonly PluginConfig _pluginConfig;
    private readonly ConfigGui _configGui;
    private readonly ILogger<EnhancedMaterialList> _logger;
    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly IAddonLifecycle _addonLifecycle;
    private readonly IFramework _framework;
    private readonly IClientState _clientState;
    private readonly IGameInventory _gameInventory;
    private readonly IAetheryteList _aetheryteList;
    private readonly AddonObserver _addonObserver;
    private readonly ExcelService _excelService;
    private readonly MapService _mapService;
    private readonly ItemService _itemService;

    private Hook<AgentRecipeMaterialList.Delegates.ReceiveEvent>? _agentRecipeMaterialListReceiveEventHook;
    private Hook<AddonRecipeMaterialList.Delegates.SetupRow>? _addonRecipeMaterialListSetupRowHook;
    private Hook<AgentRecipeItemContext.Delegates.AddItemContextMenuEntries>? _addItemContextMenuEntriesHook;

    private bool _canRefreshMaterialList;
    private bool _pendingMaterialListRefresh;
    private DateTime _timeOfMaterialListRefresh;
    private bool _recipeMaterialListLockPending;

    private bool _canRefreshRecipeTree;
    private bool _pendingRecipeTreeRefresh;
    private DateTime _timeOfRecipeTreeRefresh;
    private bool _handleRecipeResultItemContextMenu;

    public string InternalName => nameof(EnhancedMaterialList);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    public void OnInitialize()
    {
        _agentRecipeMaterialListReceiveEventHook = _gameInteropProvider.HookFromAddress<AgentRecipeMaterialList.Delegates.ReceiveEvent>(
            AgentRecipeMaterialList.StaticVirtualTablePointer->ReceiveEvent,
            AgentRecipeMaterialListReceiveEventDetour);

        _addonRecipeMaterialListSetupRowHook = _gameInteropProvider.HookFromAddress<AddonRecipeMaterialList.Delegates.SetupRow>(
            AddonRecipeMaterialList.MemberFunctionPointers.SetupRow,
            AddonRecipeMaterialListSetupRowDetour);

        _addItemContextMenuEntriesHook = _gameInteropProvider.HookFromAddress<AgentRecipeItemContext.Delegates.AddItemContextMenuEntries>(
            AgentRecipeItemContext.MemberFunctionPointers.AddItemContextMenuEntries,
            AddItemContextMenuEntriesDetour);
    }

    public void OnEnable()
    {
        _addonLifecycle.RegisterListener(AddonEvent.PostReceiveEvent, "RecipeMaterialList", RecipeMaterialList_PostReceiveEvent);
        _addonLifecycle.RegisterListener(AddonEvent.PostReceiveEvent, "RecipeTree", RecipeTree_PostReceiveEvent);

        _framework.Update += OnFrameworkUpdate;
        _addonObserver.AddonOpen += OnAddonOpen;
        _gameInventory.InventoryChangedRaw += OnInventoryUpdate;
        _clientState.Login += OnLogin;

        _agentRecipeMaterialListReceiveEventHook?.Enable();
        _addonRecipeMaterialListSetupRowHook?.Enable();
        _addItemContextMenuEntriesHook?.Enable();
    }

    public void OnDisable()
    {
        _addonLifecycle.UnregisterListener(AddonEvent.PostReceiveEvent, "RecipeMaterialList", RecipeMaterialList_PostReceiveEvent);
        _addonLifecycle.UnregisterListener(AddonEvent.PostReceiveEvent, "RecipeTree", RecipeTree_PostReceiveEvent);

        _framework.Update -= OnFrameworkUpdate;
        _addonObserver.AddonOpen -= OnAddonOpen;
        _gameInventory.InventoryChangedRaw -= OnInventoryUpdate;
        _clientState.Login -= OnLogin;

        _agentRecipeMaterialListReceiveEventHook?.Disable();
        _addonRecipeMaterialListSetupRowHook?.Disable();
        _addItemContextMenuEntriesHook?.Disable();

        if (Status is TweakStatus.Enabled && TryGetAddon<AtkUnitBase>("RecipeMaterialList", out var addon))
            addon->Close(true);
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();
        _agentRecipeMaterialListReceiveEventHook?.Dispose();
        _addonRecipeMaterialListSetupRowHook?.Dispose();
        _addItemContextMenuEntriesHook?.Dispose();

        Status = TweakStatus.Disposed;
    }

    private void OnAddonOpen(string addonName)
    {
        if (addonName == "RecipeMaterialList")
            _canRefreshMaterialList = true;

        if (addonName == "RecipeTree")
            _canRefreshRecipeTree = true;
    }

    private void OnInventoryUpdate(IReadOnlyCollection<InventoryEventArgs> events)
    {
        _pendingMaterialListRefresh = true;
        _timeOfMaterialListRefresh = DateTime.UtcNow;
        _pendingRecipeTreeRefresh = true;
        _timeOfRecipeTreeRefresh = DateTime.UtcNow;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (!_clientState.IsLoggedIn)
            return;

        // added a 500ms delay because selling items updates the addons before the item is gone...

        if (_pendingMaterialListRefresh && DateTime.UtcNow - _timeOfMaterialListRefresh >= TimeSpan.FromMilliseconds(500))
            RefreshMaterialList();

        if (_pendingRecipeTreeRefresh && DateTime.UtcNow - _timeOfRecipeTreeRefresh >= TimeSpan.FromMilliseconds(500))
            RefreshRecipeTree();

        if (_recipeMaterialListLockPending && TryGetAddon<AddonRecipeMaterialList>(AgentId.RecipeMaterialList, out var recipeMaterialList))
        {
            _recipeMaterialListLockPending = false;
            recipeMaterialList->SetWindowLock(true);
        }
    }

    private void OnLogin()
    {
        if (!Config.RestoreMaterialList || Config.RestoreMaterialListRecipeId == 0)
            return;

        var agentRecipeMaterialList = AgentRecipeMaterialList.Instance();
        if (agentRecipeMaterialList->RecipeId != Config.RestoreMaterialListRecipeId)
        {
            _recipeMaterialListLockPending = true;
            _logger.LogInformation("Restoring RecipeMaterialList");
            agentRecipeMaterialList->OpenByRecipeId((ushort)Config.RestoreMaterialListRecipeId, Math.Max(Config.RestoreMaterialListAmount, 1));
        }
    }

    private void RecipeMaterialList_PostReceiveEvent(AddonEvent type, AddonArgs args)
    {
        if (args is not AddonReceiveEventArgs receiveEventArgs)
            return;

        switch ((AtkEventType)receiveEventArgs.AtkEventType)
        {
            case AtkEventType.ButtonClick when receiveEventArgs.EventParam == 1: // refresh button clicked
                _canRefreshMaterialList = false;
                return;

            case AtkEventType.ListItemToggle:
                if (!Config.ClickToOpenMap)
                    return;

                var data = receiveEventArgs.Data;
                if (data == 0 || *(byte*)(data + 0x18) == 1) // ignore right click
                    return;

                var rowData = **(nint**)(data + 0x08);
                var itemId = *(uint*)(rowData + 0x04);

                var itemRef = _excelService.CreateRef<Item>(itemId);
                if (!itemRef.IsValid)
                    return;

                if (Config.DisableClickToOpenMapForCrystals && itemRef.Value.ItemUICategory.RowId == 59)
                    return;

                var tuple = GetPointForItem(itemId);
                if (tuple == null)
                    return;

                var (totalPoints, point, cost, isSameZone, placeName) = tuple.Value;

                _mapService.OpenMap(point, itemRef, "HaselTweaks"u8);

                return;

            case AtkEventType.TimerTick: // gets fired every second unless it's refreshing the material list
                _canRefreshMaterialList = true;
                return;
        }
    }

    private void RecipeTree_PostReceiveEvent(AddonEvent type, AddonArgs args)
    {
        if (args is not AddonReceiveEventArgs receiveEventArgs)
            return;

        switch ((AtkEventType)receiveEventArgs.AtkEventType)
        {
            case AtkEventType.ButtonClick when receiveEventArgs.EventParam == 0: // refresh button clicked
                _canRefreshRecipeTree = false;
                return;

            case AtkEventType.TimerTick: // gets fired every second unless it's refreshing the recipe tree
                _canRefreshRecipeTree = true;
                return;
        }
    }

    private void RefreshMaterialList()
    {
        _pendingMaterialListRefresh = false;

        if (!Config.AutoRefreshMaterialList || !_canRefreshMaterialList || !TryGetAddon<AddonRecipeMaterialList>(AgentId.RecipeMaterialList, out var recipeMaterialList))
            return;

        _logger.LogInformation("Refreshing RecipeMaterialList");
        var atkEvent = new AtkEvent();
        recipeMaterialList->AtkUnitBase.ReceiveEvent(AtkEventType.ButtonClick, 1, &atkEvent);
    }

    private void RefreshRecipeTree()
    {
        _pendingRecipeTreeRefresh = false;

        if (!Config.AutoRefreshRecipeTree || !_canRefreshRecipeTree || !TryGetAddon<AtkUnitBase>(AgentId.RecipeTree, out var recipeTree))
            return;

        _logger.LogInformation("Refreshing RecipeTree");
        var atkEvent = new AtkEvent();
        recipeTree->ReceiveEvent(AtkEventType.ButtonClick, 0, &atkEvent);
    }

    private AtkValue* AgentRecipeMaterialListReceiveEventDetour(AgentRecipeMaterialList* agent, AtkValue* returnValue, AtkValue* values, uint valueCount, ulong eventKind)
    {
        var ret = _agentRecipeMaterialListReceiveEventHook!.Original(agent, returnValue, values, valueCount, eventKind);

        if (eventKind != 1 && valueCount >= 1 && values->Int == 4)
        {
            _handleRecipeResultItemContextMenu = true;
        }

        // TODO: add conditions?
        SaveRestoreMaterialList(agent);

        return ret;
    }

    private void SaveRestoreMaterialList(AgentRecipeMaterialList* agent)
    {
        var shouldSave = Config.RestoreMaterialList && agent->WindowLocked;
        var recipeId = shouldSave ? agent->RecipeId : 0u;
        var amount = shouldSave ? agent->Amount : 0u;

        if (Config.RestoreMaterialListRecipeId != recipeId || Config.RestoreMaterialListAmount != amount)
        {
            Config.RestoreMaterialListRecipeId = recipeId;
            Config.RestoreMaterialListAmount = amount;
            _pluginConfig.Save();
        }
    }

    private void AddonRecipeMaterialListSetupRowDetour(AddonRecipeMaterialList* addon, nint a2, nint a3)
    {
        _addonRecipeMaterialListSetupRowHook!.Original(addon, a2, a3);
        RecipeMaterialList_HandleSetupRow(a2, a3);
    }

    private void RecipeMaterialList_HandleSetupRow(nint a2, nint a3)
    {
        if (!Config.EnableZoneNames)
            return;

        var data = **(nint**)(a2 + 0x08);
        var itemId = *(uint*)(data + 0x04);

        // TODO: only for missing items?

        if (!_excelService.TryGetRow<Item>(itemId, out var item))
            return;

        // Exclude Crystals
        if (Config.DisableZoneNameForCrystals && item.ItemUICategory.RowId == 59)
            return;

        var tuple = GetPointForItem(itemId);
        if (tuple == null)
            return;

        var (totalPoints, point, cost, isSameZone, placeNameSeString) = tuple.Value;

        var nameNode = *(AtkTextNode**)(a3 + 0x08);
        if (nameNode == null)
            return;

        var textPtr = nameNode->GetText();
        if (textPtr == null)
            return;

        // when you don't know how to add text nodes... Sadge

        nameNode->AtkResNode.Y = 14;
        nameNode->AtkResNode.DrawFlags |= 0x1;

        nameNode->TextFlags = 192; // allow multiline text (not sure on the actual flags it sets though)
        nameNode->LineSpacing = 17;

        var itemName = new ReadOnlySeStringSpan(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(textPtr)).ExtractText().Replace("\r\n", "");
        if (itemName.Length > 23)
            itemName = itemName[..20] + "...";

        var placeName = placeNameSeString.ExtractText();
        if (placeName.Length > 23)
            placeName = placeName[..20] + "...";

        var str = new SeStringBuilder()
            .Append(itemName)
            .BeginMacro(MacroCode.NewLine).EndMacro()
            .PushColorType((ushort)(isSameZone ? 570 : 4))
            .PushEdgeColorType(550)
            .Append(placeName)
            .PopEdgeColorType()
            .PopColorType()
            .GetViewAsSpan();

        nameNode->SetText(str);
    }

    private void AddItemContextMenuEntriesDetour(AgentRecipeItemContext* agent, uint itemId, byte flags, byte* itemName, byte a5, byte a6)
    {
        UpdateContextMenuFlag(itemId, ref flags);
        _addItemContextMenuEntriesHook!.Original(agent, itemId, flags, itemName, a5, a6);
    }

    private void UpdateContextMenuFlag(uint itemId, ref byte flags)
    {
        if (!_handleRecipeResultItemContextMenu)
            return;

        _handleRecipeResultItemContextMenu = false;

        if (!Config.AddSearchForItemByCraftingMethodContextMenuEntry)
            return;

        if (!IsAddonOpen(AgentId.RecipeMaterialList))
            return;

        var agentRecipeMaterialList = AgentRecipeMaterialList.Instance();
        if (agentRecipeMaterialList->Recipe == null || agentRecipeMaterialList->Recipe->ResultItemId != itemId)
            return;

        var localPlayer = (Character*)(_clientState.LocalPlayer?.Address ?? 0);
        if (localPlayer == null || localPlayer->Mode == CharacterModes.Crafting)
            return;

        flags |= 2;
    }

    private (int, GatheringPoint, uint, bool, ReadOnlySeString)? GetPointForItem(uint itemId)
    {
        var gatheringItems = _itemService.GetGatheringItems(itemId);
        if (!gatheringItems.Any())
            return null;

        // TODO: rethink this
        var gatheringPointSheet = _excelService.GetSheet<GatheringPoint>();
        var gatheringPoints = _excelService.GetSheet<GatheringPointBase>()
            .Where(row => row.Item.Any(item => item.RowId == gatheringItems.First().RowId))
            .Select(row =>
            {
                var hasValue = gatheringPointSheet.TryGetFirst(gprow => gprow.GatheringPointBase.RowId == row.RowId && gprow.TerritoryType.RowId > 1, out var value);
                return (HasValue: hasValue, Value: value);
            })
            .Where(row => row.HasValue)
            .Select(row => row.Value)
            .ToList();

        if (gatheringPoints.Count == 0)
            return null;

        var currentTerritoryTypeId = GameMain.Instance()->CurrentTerritoryTypeId;
        var point = gatheringPoints.FirstOrDefault(row => row.TerritoryType.RowId == currentTerritoryTypeId);
        var isSameZone = point.RowId != 0;
        var cost = 0u;
        if (point.RowId == 0)
        {
            foreach (var p in gatheringPoints)
            {
                foreach (var aetheryte in _aetheryteList)
                {
                    if (aetheryte.AetheryteId == p!.TerritoryType.Value!.Aetheryte.RowId && (cost == 0 || aetheryte.GilCost < cost))
                    {
                        cost = aetheryte.GilCost;
                        point = p;
                        break;
                    }
                }
            }
        }

        if (point.RowId == 0)
            return null;

        var placeName = point.TerritoryType.ValueNullable?.PlaceName.ValueNullable?.Name;
        return placeName == null ? null : (gatheringPoints.Count, point, cost, isSameZone, (ReadOnlySeString)placeName);
    }
}
