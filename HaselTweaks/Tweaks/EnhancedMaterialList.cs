using Dalamud.Game.Inventory.InventoryEventArgTypes;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class EnhancedMaterialList : ConfigurableTweak
{
    private readonly PluginConfig _pluginConfig;
    private readonly ConfigGui _configGui;
    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly IAddonLifecycle _addonLifecycle;
    private readonly IFramework _framework;
    private readonly IClientState _clientState;
    private readonly IGameInventory _gameInventory;
    private readonly AddonObserver _addonObserver;
    private readonly ExcelService _excelService;
    private readonly MapService _mapService;
    private readonly ItemService _itemService;

    private Hook<AgentRecipeMaterialList.Delegates.ReceiveEvent>? _agentRecipeMaterialListReceiveEventHook;
    private Hook<AtkComponentListItemPopulator.PopulateDelegate>? _addonRecipeMaterialListSetupRowHook;
    private Hook<AgentRecipeItemContext.Delegates.AddItemContextMenuEntries>? _addItemContextMenuEntriesHook;

    private bool _canRefreshMaterialList;
    private bool _pendingMaterialListRefresh;
    private DateTime _timeOfMaterialListRefresh;
    private bool _recipeMaterialListLockPending;

    private bool _canRefreshRecipeTree;
    private bool _pendingRecipeTreeRefresh;
    private DateTime _timeOfRecipeTreeRefresh;
    private bool _handleRecipeResultItemContextMenu;

    public override void OnEnable()
    {
        _agentRecipeMaterialListReceiveEventHook = _gameInteropProvider.HookFromAddress<AgentRecipeMaterialList.Delegates.ReceiveEvent>(
            AgentRecipeMaterialList.StaticVirtualTablePointer->ReceiveEvent,
            AgentRecipeMaterialListReceiveEventDetour);

        _addonRecipeMaterialListSetupRowHook = _gameInteropProvider.HookFromSignature<AtkComponentListItemPopulator.PopulateDelegate>(
            "48 89 5C 24 ?? 48 89 54 24 ?? 48 89 4C 24 ?? 55 56 57 41 54 41 55 41 56 41 57 48 83 EC 50 49 8B 08",
            AddonRecipeMaterialListPopulateRowDetour);

        _addItemContextMenuEntriesHook = _gameInteropProvider.HookFromAddress<AgentRecipeItemContext.Delegates.AddItemContextMenuEntries>(
            AgentRecipeItemContext.MemberFunctionPointers.AddItemContextMenuEntries,
            AddItemContextMenuEntriesDetour);

        _agentRecipeMaterialListReceiveEventHook.Enable();
        _addonRecipeMaterialListSetupRowHook.Enable();
        _addItemContextMenuEntriesHook.Enable();

        _addonLifecycle.RegisterListener(AddonEvent.PostReceiveEvent, "RecipeMaterialList", RecipeMaterialList_PostReceiveEvent);
        _addonLifecycle.RegisterListener(AddonEvent.PostReceiveEvent, "RecipeTree", RecipeTree_PostReceiveEvent);

        _framework.Update += OnFrameworkUpdate;
        _addonObserver.AddonOpen += OnAddonOpen;
        _gameInventory.InventoryChangedRaw += OnInventoryUpdate;
        _clientState.Login += OnLogin;

        if (_clientState.IsLoggedIn)
            OnLogin();
    }

    public override void OnDisable()
    {
        _addonLifecycle.UnregisterListener(AddonEvent.PostReceiveEvent, "RecipeMaterialList", RecipeMaterialList_PostReceiveEvent);
        _addonLifecycle.UnregisterListener(AddonEvent.PostReceiveEvent, "RecipeTree", RecipeTree_PostReceiveEvent);

        _framework.Update -= OnFrameworkUpdate;
        _addonObserver.AddonOpen -= OnAddonOpen;
        _gameInventory.InventoryChangedRaw -= OnInventoryUpdate;
        _clientState.Login -= OnLogin;

        _agentRecipeMaterialListReceiveEventHook?.Dispose();
        _agentRecipeMaterialListReceiveEventHook = null;

        _addonRecipeMaterialListSetupRowHook?.Dispose();
        _addonRecipeMaterialListSetupRowHook = null;

        _addItemContextMenuEntriesHook?.Dispose();
        _addItemContextMenuEntriesHook = null;

        if (Status is TweakStatus.Enabled && TryGetAddon<AtkUnitBase>("RecipeMaterialList", out var addon))
            addon->Close(true);
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

            case AtkEventType.ListItemClick:
                if (!Config.ClickToOpenMap)
                    return;

                var data = (AtkEventData.AtkListItemData*)receiveEventArgs.Data;
                if (data == null || data->MouseButtonId == 1) // ignore right click
                    return;

                var itemId = data->ListItem->UIntValues[1];
                if (Config.DisableClickToOpenMapForCrystals && (!_excelService.TryGetRow<Item>(itemId, out var item) || item.ItemUICategory.RowId == 59))
                    return;

                if (!TryGetPointForItem(itemId, out _, out var point, out _, out _, out _))
                    return;

                _mapService.OpenMap(point, itemId, "HaselTweaks"u8);

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
        recipeMaterialList->ReceiveEvent(AtkEventType.ButtonClick, 1, &atkEvent);
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
            _logger.LogDebug("Saving {amount}x {id}", amount, recipeId);
            _pluginConfig.Save();
        }
    }

    private void AddonRecipeMaterialListPopulateRowDetour(AtkUnitBase* thisPtr, AtkComponentListItemPopulator.ListItemInfo* listItemInfo, AtkResNode** nodeList)
    {
        _addonRecipeMaterialListSetupRowHook!.Original(thisPtr, listItemInfo, nodeList);

        if (!Config.EnableZoneNames)
            return;

        var itemId = listItemInfo->ListItem->UIntValues[1];

        var nameNode = nodeList[1]->GetAsAtkTextNode();
        if (nameNode == null)
            return;

        var textPtr = nameNode->GetText();
        if (!textPtr.HasValue)
            return;

        // TODO: only for missing items?

        if (!_excelService.TryGetRow<Item>(itemId, out var item))
            return;

        // Exclude Crystals
        if (Config.DisableZoneNameForCrystals && item.ItemUICategory.RowId == 59)
            return;

        if (!TryGetPointForItem(itemId, out _, out _, out _, out var isSameZone, out var placeNameSeString))
            return;

        nameNode->Y = 4;
        nameNode->Height = 34;
        nameNode->TextFlags = TextFlags.MultiLine;
        nameNode->LineSpacing = 17;
        nameNode->DrawFlags |= 0x1;

        var itemName = textPtr.AsReadOnlySeStringSpan().ToString().Replace("\r\n", "");
        if (itemName.Length > 23)
            itemName = itemName[..20] + "...";

        var placeName = placeNameSeString.ToString();
        if (placeName.Length > 23)
            placeName = placeName[..20] + "...";

        using var rssb = new RentedSeStringBuilder();
        nameNode->SetText(rssb.Builder
            .Append(itemName)
            .BeginMacro(MacroCode.NewLine).EndMacro()
            .PushColorType((ushort)(isSameZone ? 570 : 4))
            .PushEdgeColorType(550)
            .Append(placeName)
            .PopEdgeColorType()
            .PopColorType()
            .GetViewAsSpan());
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

        var localPlayer = Control.GetLocalPlayer();
        if (localPlayer == null || localPlayer->Mode == CharacterModes.Crafting)
            return;

        flags |= 2;
    }

    private bool TryGetPointForItem(
        uint itemId,
        out int totalPoints,
        out GatheringPoint point,
        out uint cost,
        out bool isSameZone,
        out ReadOnlySeString placeName)
    {
        totalPoints = default;
        point = default;
        cost = default;
        isSameZone = default;
        placeName = default;

        var gatheringItems = _itemService.GetGatheringItems(itemId);
        if (gatheringItems.Length == 0)
            return false;

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
            return false;

        var currentTerritoryTypeId = GameMain.Instance()->CurrentTerritoryTypeId;

        totalPoints = gatheringPoints.Count;
        point = gatheringPoints.FirstOrDefault(row => row.TerritoryType.RowId == currentTerritoryTypeId);
        isSameZone = point.RowId != 0;
        cost = 0u;

        if (point.RowId == 0)
        {
            foreach (var p in gatheringPoints)
            {
                foreach (var teleportInfo in Telepo.Instance()->TeleportList)
                {
                    if (teleportInfo.AetheryteId == p!.TerritoryType.Value!.Aetheryte.RowId && (cost == 0 || teleportInfo.GilCost < cost))
                    {
                        cost = teleportInfo.GilCost;
                        point = p;
                        break;
                    }
                }
            }
        }

        if (point.RowId == 0)
            return false;

        var nullablePlayeName = point.TerritoryType.ValueNullable?.PlaceName.ValueNullable?.Name;
        if (nullablePlayeName == null)
            return false;

        placeName = (ReadOnlySeString)nullablePlayeName;
        return true;
    }
}
