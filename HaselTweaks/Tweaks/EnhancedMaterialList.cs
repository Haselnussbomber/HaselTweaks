using System.Linq;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Memory;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.SheetLookup;
using HaselCommon.Sheets;
using HaselTweaks.Enums;
using HaselTweaks.Structs;
using Lumina.Excel.GeneratedSheets;
using Character = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;

namespace HaselTweaks.Tweaks;

public class EnhancedMaterialListConfiguration
{
    [BoolConfig]
    public bool EnableZoneNames = true;

    [BoolConfig(DependsOn = nameof(EnableZoneNames))]
    public bool DisableZoneNameForCrystals = true;

    [BoolConfig]
    public bool ClickToOpenMap = true;

    [BoolConfig(DependsOn = nameof(ClickToOpenMap))]
    public bool DisableClickToOpenMapForCrystals = true;

    [BoolConfig]
    public bool AutoRefreshMaterialList = true;

    [BoolConfig]
    public bool AutoRefreshRecipeTree = true;

    [BoolConfig]
    public bool RestoreMaterialList = true;

    public uint RestoreMaterialListRecipeId = 0;
    public uint RestoreMaterialListAmount = 0;

    [BoolConfig]
    public bool AddSearchForItemByCraftingMethodContextMenuEntry = true; // yep, i spelled it out
}

[Tweak]
public unsafe partial class EnhancedMaterialList : Tweak<EnhancedMaterialListConfiguration>
{
    private bool _canRefreshMaterialList;
    private bool _pendingMaterialListRefresh;
    private DateTime _timeOfMaterialListRefresh;
    private bool _recipeMaterialListLockPending;

    private bool _canRefreshRecipeTree;
    private bool _pendingRecipeTreeRefresh;
    private DateTime _timeOfRecipeTreeRefresh;
    private bool _handleRecipeResultItemContextMenu;

    public override void OnConfigChange(string fieldName)
    {
        if (fieldName is nameof(Config.EnableZoneNames)
                      or nameof(Config.DisableZoneNameForCrystals)
                      or nameof(Config.DisableClickToOpenMapForCrystals))
        {
            _pendingMaterialListRefresh = true;
            _timeOfMaterialListRefresh = DateTime.UtcNow;
        }

        if (fieldName is nameof(Config.RestoreMaterialList))
        {
            SaveRestoreMaterialList(GetAgent<AgentRecipeMaterialList>());
        }
    }

    public override void Enable()
    {
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostReceiveEvent, "RecipeMaterialList", RecipeMaterialList_PostReceiveEvent);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostReceiveEvent, "RecipeTree", RecipeTree_PostReceiveEvent);
    }

    public override void Disable()
    {
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PostReceiveEvent, "RecipeMaterialList", RecipeMaterialList_PostReceiveEvent);
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PostReceiveEvent, "RecipeTree", RecipeTree_PostReceiveEvent);
    }

    public override void OnAddonOpen(string addonName)
    {
        if (addonName == "RecipeMaterialList")
            _canRefreshMaterialList = true;

        if (addonName == "RecipeTree")
            _canRefreshRecipeTree = true;
    }

    public override void OnInventoryUpdate()
    {
        _pendingMaterialListRefresh = true;
        _timeOfMaterialListRefresh = DateTime.UtcNow;
        _pendingRecipeTreeRefresh = true;
        _timeOfRecipeTreeRefresh = DateTime.UtcNow;
    }

    public override void OnFrameworkUpdate()
    {
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

    public override void OnLogin()
    {
        if (!Config.RestoreMaterialList || Config.RestoreMaterialListRecipeId == 0)
            return;

        var agentRecipeMaterialList = GetAgent<AgentRecipeMaterialList>();
        if (agentRecipeMaterialList->RecipeId != Config.RestoreMaterialListRecipeId)
        {
            _recipeMaterialListLockPending = true;
            Log("Restoring RecipeMaterialList");
            agentRecipeMaterialList->OpenByRecipeId(Config.RestoreMaterialListRecipeId, Math.Max(Config.RestoreMaterialListAmount, 1));
        }
    }

    private void RecipeMaterialList_PostReceiveEvent(AddonEvent type, AddonArgs args)
    {
        if (args is not AddonReceiveEventArgs receiveEventArgs)
            return;

        switch (receiveEventArgs.AtkEventType)
        {
            case (byte)AtkEventType.ButtonClick when receiveEventArgs.EventParam == 1: // refresh button clicked
                _canRefreshMaterialList = false;
                return;

            case (byte)AtkEventType.ListItemToggle:
                if (!Config.ClickToOpenMap)
                    return;

                var data = receiveEventArgs.Data;
                if (data == 0 || *(byte*)(data + 0x18) == 1) // ignore right click
                    return;

                var rowData = **(nint**)(data + 0x08);
                var itemId = *(uint*)(rowData + 0x04);

                var item = GetRow<ExtendedItem>(itemId);
                if (item == null)
                    return;

                if (Config.DisableClickToOpenMapForCrystals && item.ItemUICategory.Row == 59)
                    return;

                var tuple = GetPointForItem(itemId);
                if (tuple == null)
                    return;

                var (totalPoints, point, cost, isSameZone, placeName) = tuple.Value;

                point.OpenMap(item, "HaselTweaks");

                return;

            case 61: // gets fired every second unless it's refreshing the material list
                _canRefreshMaterialList = true;
                return;
        }
    }

    private void RecipeTree_PostReceiveEvent(AddonEvent type, AddonArgs args)
    {
        if (args is not AddonReceiveEventArgs receiveEventArgs)
            return;

        switch (receiveEventArgs.AtkEventType)
        {
            case (byte)AtkEventType.ButtonClick when receiveEventArgs.EventParam == 0: // refresh button clicked
                _canRefreshRecipeTree = false;
                return;

            case 61: // gets fired every second unless it's refreshing the recipe tree
                _canRefreshRecipeTree = true;
                return;
        }
    }

    public void RefreshMaterialList()
    {
        _pendingMaterialListRefresh = false;

        if (!Config.AutoRefreshMaterialList || !_canRefreshMaterialList || !TryGetAddon<AddonRecipeMaterialList>(AgentId.RecipeMaterialList, out var recipeMaterialList))
            return;

        Log("Refreshing RecipeMaterialList");
        var atkEvent = stackalloc AtkEvent[1];
        recipeMaterialList->AtkUnitBase.ReceiveEvent(AtkEventType.ButtonClick, 1, atkEvent, 0);
    }

    public void RefreshRecipeTree()
    {
        _pendingRecipeTreeRefresh = false;

        if (!Config.AutoRefreshRecipeTree || !_canRefreshRecipeTree || !TryGetAddon<AtkUnitBase>(AgentId.RecipeTree, out var recipeTree))
            return;

        Log("Refreshing RecipeTree");
        var atkEvent = stackalloc AtkEvent[1];
        recipeTree->ReceiveEvent(AtkEventType.ButtonClick, 0, atkEvent, 0);
    }

    [VTableHook<AgentRecipeMaterialList>((int)AgentInterfaceVfs.ReceiveEvent)]
    public nint AgentRecipeMaterialList_ReceiveEvent(AgentRecipeMaterialList* agent, AtkEvent* result, AtkValue* values, nint a4, nint a5)
    {
        var ret = AgentRecipeMaterialList_ReceiveEventHook.OriginalDisposeSafe(agent, result, values, a4, a5);
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
            Service.GetService<Configuration>().Save();
        }
    }

    [AddressHook<AddonRecipeMaterialList>(nameof(AddonRecipeMaterialList.Addresses.SetupRow))]
    public nint AddonRecipeMaterialList_SetupRow(AddonRecipeMaterialList* addon, nint a2, nint a3)
    {
        var res = AddonRecipeMaterialList_SetupRowHook.OriginalDisposeSafe(addon, a2, a3);
        RecipeMaterialList_HandleSetupRow(a2, a3);
        return res;
    }

    private void RecipeMaterialList_HandleSetupRow(nint a2, nint a3)
    {
        if (!Config.EnableZoneNames)
            return;

        var data = **(nint**)(a2 + 0x08);
        var itemId = *(uint*)(data + 0x04);

        // TODO: only for missing items?

        var item = GetRow<Item>(itemId);
        if (item == null)
            return;

        // Exclude Crystals
        if (Config.DisableZoneNameForCrystals && item.ItemUICategory.Row == 59)
            return;

        var tuple = GetPointForItem(itemId);
        if (tuple == null)
            return;

        var (totalPoints, point, cost, isSameZone, placeNameSeString) = tuple.Value;

        var nameNode = *(AtkTextNode**)(a3 + 0x08);
        if (nameNode == null)
            return;

        var textPtr = (nint)nameNode->GetText();
        if (textPtr == 0)
            return;

        // when you don't know how to add text nodes... Sadge

        nameNode->AtkResNode.Y = 14;
        nameNode->AtkResNode.DrawFlags |= 0x1;

        nameNode->TextFlags = 192; // allow multiline text (not sure on the actual flags it sets though)
        nameNode->LineSpacing = 17;

        var itemName = MemoryHelper.ReadSeStringNullTerminated(textPtr).TextValue.Replace("\r\n", "");
        if (itemName.Length > 23)
            itemName = itemName[..20] + "...";

        var placeName = placeNameSeString.TextValue;
        if (placeName.Length > 23)
            placeName = placeName[..20] + "...";

        var sb = new SeStringBuilder()
            .AddText(itemName)
            .Add(NewLinePayload.Payload)
            .AddUiForeground((ushort)(isSameZone ? 570 : 4))
            .AddUiGlow(550)
            .AddText(placeName)
            .AddUiGlowOff()
            .AddUiForegroundOff();

        nameNode->SetText(sb.Encode());
    }

    [AddressHook<AgentRecipeMaterialList>(nameof(AgentRecipeMaterialList.Addresses.OpenRecipeResultItemContextMenu))]
    public nint AgentRecipeMaterialList_OpenRecipeResultItemContextMenu(AgentRecipeMaterialList* agent)
    {
        _handleRecipeResultItemContextMenu = true;
        return AgentRecipeMaterialList_OpenRecipeResultItemContextMenuHook.OriginalDisposeSafe(agent);
    }

    [AddressHook<AgentRecipeItemContext>(nameof(AgentRecipeItemContext.Addresses.AddItemContextMenuEntries))]
    public nint AgentRecipeItemContext_AddItemContextMenuEntries(AgentRecipeItemContext* agent, uint itemId, byte flags, byte* itemName)
    {
        UpdateContextMenuFlag(itemId, ref flags);
        return AgentRecipeItemContext_AddItemContextMenuEntriesHook.OriginalDisposeSafe(agent, itemId, flags, itemName);
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

        var agentRecipeMaterialList = GetAgent<AgentRecipeMaterialList>();
        if (agentRecipeMaterialList->Recipe == null || agentRecipeMaterialList->Recipe->ResultItemId != itemId)
            return;

        var localPlayer = (Character*)(Service.ClientState.LocalPlayer?.Address ?? 0);
        if (localPlayer == null || localPlayer->EventState == 5)
            return;

        flags |= 2;
    }

    private (int, ExtendedGatheringPoint, uint, bool, SeString)? GetPointForItem(uint itemId)
    {
        var gatheringItem = ItemGatheringItemLookup.First(itemId);
        if (gatheringItem == null)
            return null;

        var gatheringPointSheet = GetSheet<ExtendedGatheringPoint>();
        var gatheringPoints = GetSheet<GatheringPointBase>()
            .Where(row => row.Item.Any(item => item == gatheringItem.RowId))
            .Select(row => gatheringPointSheet.FirstOrDefault(gprow => gprow?.GatheringPointBase.Row == row.RowId && gprow.TerritoryType.Row > 1, null))
            .Where(row => row != null)
            .ToList();

        if (!gatheringPoints.Any())
            return null;

        var currentTerritoryTypeId = GameMain.Instance()->CurrentTerritoryTypeId;
        var point = gatheringPoints.FirstOrDefault(row => row?.TerritoryType.Row == currentTerritoryTypeId, null);
        var isSameZone = point != null;
        var cost = 0u;
        if (point == null)
        {
            foreach (var p in gatheringPoints)
            {
                var thisCost = (uint)Telepo.GetTeleportCost((ushort)currentTerritoryTypeId, (ushort)p!.TerritoryType.Row, false, false, false);
                if (cost == 0 || thisCost < cost)
                {
                    cost = thisCost;
                    point = p;
                }
            }
        }

        if (point == null)
            return null;

        var placeName = point.TerritoryType.Value?.PlaceName.Value?.Name.ToDalamudString();
        return placeName == null ? null : (gatheringPoints.Count, point, cost, isSameZone, placeName);
    }
}
