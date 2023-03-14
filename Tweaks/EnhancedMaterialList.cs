using System.Linq;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Memory;
using Dalamud.Utility;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Structs;
using HaselTweaks.Utils;
using Lumina.Excel.GeneratedSheets;

namespace HaselTweaks.Tweaks;

public unsafe partial class EnhancedMaterialList : Tweak
{
    public override string Name => "Enhanced Material List";
    public override string Description => "Enhances the Material List (and Recipe Tree).";
    public static Configuration Config => Plugin.Config.Tweaks.EnhancedMaterialList;

    private AgentRecipeMaterialList* agentRecipeMaterialList;

    private DateTime LastRecipeMaterialListRefresh = DateTime.Now;
    private bool RecipeMaterialListRefreshPending = false;
    private bool RecipeMaterialListLockPending = false;

    private DateTime LastRecipeTreeRefresh = DateTime.Now;
    private bool RecipeTreeRefreshPending = false;

    private bool HandleRecipeResultItemContextMenu = false;

    public class Configuration
    {
        [ConfigField(
            Label = "Enable Zone Names",
            Description = "The zone with the lowest teleportation cost is displayed.\nA green zone name means it's the current zone.\nSince space is limited it has to shorten the item and zone name.",
            OnChange = nameof(RequestRecipeMaterialListRefresh)
        )]
        public bool EnableZoneNames = true;

        [ConfigField(
            Label = "Disable for Crystals",
            DependsOn = nameof(EnableZoneNames),
            OnChange = nameof(RequestRecipeMaterialListRefresh)
        )]
        public bool DisableZoneNameForCrystals = true;

        [ConfigField(
            Label = "Enable click to open Map"
        )]
        public bool ClickToOpenMap = true;

        [ConfigField(
            Label = "Disable for Crystals",
            DependsOn = nameof(ClickToOpenMap),
            OnChange = nameof(RequestRecipeMaterialListRefresh)
        )]
        public bool DisableClickToOpenMapForCrystals = true;

        [ConfigField(
            Label = "Auto-refresh Material List",
            Description = "Refreshes the material list after an item has been bought, crafted, fished or gathered."
        )]
        public bool AutoRefreshMaterialList = true;

        [ConfigField(
            Label = "Auto-refresh Recipe Tree",
            Description = "Refreshes the recipe tree after an item has been bought, crafted, fished or gathered."
        )]
        public bool AutoRefreshRecipeTree = true;

        [ConfigField(
            Label = "Restore Material List on Login",
            Description = "The material list will reopen with the same recipe and quantity each time you log in as long as the window is locked.",
            OnChange = nameof(SaveRestoreMaterialList)
        )]
        public bool RestoreMaterialList = true;

        [ConfigField(Type = ConfigFieldTypes.Ignore)]
        public uint RestoreMaterialListRecipeId = 0;

        [ConfigField(Type = ConfigFieldTypes.Ignore)]
        public uint RestoreMaterialListAmount = 0;

        [ConfigField(
            Label = "Add \"Search for Item by Crafting Method\" context menu entry",
            Description = "For the result item. No more need to open the recipe tree first."
        )]
        public bool AddSearchForItemByCraftingMethodContextMenuEntry = true; // yep, i spelled it out
    }

    public override void Setup()
    {
        agentRecipeMaterialList = GetAgent<AgentRecipeMaterialList>(AgentId.RecipeMaterialList);
    }

    public override void Enable()
    {
        Service.AddonObserver.Register("Catch", "Synthesis", "SynthesisSimple", "Gathering", "ItemSearchResult", "InclusionShop", "Shop", "ShopExchangeItem");

        Service.AddonObserver.OnOpen += AddonObserver_OnOpen;
        Service.AddonObserver.OnClose += AddonObserver_OnClose;

        Service.ClientState.TerritoryChanged += ClientState_TerritoryChanged;
        Service.ClientState.Login += ClientState_Login;
    }

    public override void Disable()
    {
        Service.AddonObserver.OnOpen -= AddonObserver_OnOpen;
        Service.AddonObserver.OnClose -= AddonObserver_OnClose;

        Service.AddonObserver.Unregister("Catch", "Synthesis", "SynthesisSimple", "Gathering", "ItemSearchResult", "InclusionShop", "Shop", "ShopExchangeItem");

        Service.ClientState.TerritoryChanged -= ClientState_TerritoryChanged;
        Service.ClientState.Login -= ClientState_Login;
    }

    public override void OnFrameworkUpdate(Dalamud.Game.Framework framework)
    {
        RefreshRecipeMaterialList();
        RefreshRecipeTree();
    }

    private void AddonObserver_OnOpen(AddonObserver sender, string addonName, AtkUnitBase* unitBase)
    {
        if (addonName is "Catch")
            RequestRefresh();
    }

    private void AddonObserver_OnClose(AddonObserver sender, string addonName, AtkUnitBase* unitBase)
    {
        if (addonName is "Synthesis" or "SynthesisSimple" or "Gathering" or "ItemSearchResult" or "InclusionShop" or "Shop" or "ShopExchangeItem")
            RequestRefresh();
    }

    private void RequestRefresh()
    {
        if (Config.AutoRefreshMaterialList)
            RecipeMaterialListRefreshPending = true;

        if (Config.AutoRefreshRecipeTree)
            RecipeTreeRefreshPending = true;
    }

    private void ClientState_TerritoryChanged(object? sender, ushort e)
        => RequestRecipeMaterialListRefresh();

    private void ClientState_Login(object? sender, EventArgs e)
    {
        if (Config.RestoreMaterialList &&
            Config.RestoreMaterialListRecipeId != 0 &&
            agentRecipeMaterialList != null &&
            agentRecipeMaterialList->RecipeId != Config.RestoreMaterialListRecipeId)
        {
            Log("Restoring RecipeMaterialList");
            agentRecipeMaterialList->OpenByRecipeId(Config.RestoreMaterialListRecipeId, Math.Max(Config.RestoreMaterialListAmount, 1));

            RecipeMaterialListLockPending = true;
        }
    }

    private void RequestRecipeMaterialListRefresh()
        => RecipeMaterialListRefreshPending = true;

    private void RefreshRecipeMaterialList()
    {
        if (!RecipeMaterialListRefreshPending && !RecipeMaterialListLockPending)
            return;

        if (DateTime.Now.Subtract(LastRecipeMaterialListRefresh).TotalSeconds < 2)
            return;

        if (!GetAddon<AddonRecipeMaterialList>(AgentId.RecipeMaterialList, out var recipeMaterialList))
        {
            RecipeMaterialListRefreshPending = false;
            return;
        }

        if (RecipeMaterialListRefreshPending)
        {
            Log("Refreshing RecipeMaterialList");
            var atkEvent = (AtkEvent*)IMemorySpace.GetUISpace()->Malloc<AtkEvent>();
            recipeMaterialList->ReceiveEvent(AtkEventType.ButtonClick, 1, atkEvent, 0);
            IMemorySpace.Free(atkEvent);

            RecipeMaterialListRefreshPending = false;
        }

        if (RecipeMaterialListLockPending)
        {
            recipeMaterialList->SetWindowLock(true);
            RecipeMaterialListLockPending = false;
        }

        LastRecipeMaterialListRefresh = DateTime.Now;
    }

    private void RefreshRecipeTree()
    {
        if (!RecipeTreeRefreshPending)
            return;

        if (DateTime.Now.Subtract(LastRecipeTreeRefresh).TotalSeconds < 2)
            return;

        if (!GetAddon<AddonRecipeTree>(AgentId.RecipeTree, out var recipeTree))
        {
            RecipeTreeRefreshPending = false;
            return;
        }

        Log("Refreshing RecipeTree");
        var atkEvent = (AtkEvent*)IMemorySpace.GetUISpace()->Malloc<AtkEvent>();
        recipeTree->ReceiveEvent(AtkEventType.ButtonClick, 0, atkEvent, 0);
        IMemorySpace.Free(atkEvent);

        LastRecipeTreeRefresh = DateTime.Now;
        RecipeTreeRefreshPending = false;
    }

    private void SaveRestoreMaterialList()
    {
        var shouldSave = Config.RestoreMaterialList && agentRecipeMaterialList != null && agentRecipeMaterialList->WindowLocked;
        Config.RestoreMaterialListRecipeId = shouldSave ? agentRecipeMaterialList->RecipeId : 0u;
        Config.RestoreMaterialListAmount = shouldSave ? agentRecipeMaterialList->Amount : 0u;
        Plugin.Config.Save();
    }

    [SigHook("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B D9 49 8B F0 49 8B C8 48 8B FA E8 ?? ?? ?? ?? 48 83 7C 24 ?? ??")]
    public nint AgentRecipeMaterialList_ReceiveEvent(AgentRecipeMaterialList* agent, nint a2, nint a3, nint a4, nint a5)
    {
        var ret = AgentRecipeMaterialList_ReceiveEventHook.Original(agent, a2, a3, a4, a5);
        SaveRestoreMaterialList();
        return ret;
    }

    [SigHook("40 55 53 56 57 48 8D 6C 24 ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 1F 0F B7 C2 41 8B D8 48 8B F1 83 F8 19 0F 84 ?? ?? ?? ?? 83 F8 1B")]
    public void AddonRecipeMaterialList_ReceiveEvent(AddonRecipeMaterialList* addon, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, nint a5)
    {
        AddonRecipeMaterialList_ReceiveEventHook.Original(addon, eventType, eventParam, atkEvent, a5);

        if (!Config.ClickToOpenMap)
            return;

        if (eventType != AtkEventType.ListItemToggle)
            return;

        if (*(byte*)(a5 + 0x18) == 1) // ignore right click
            return;

        var rowData = **(nint**)(a5 + 0x08);
        var itemId = *(uint*)(rowData + 0x04);

        var item = Service.Data.GetExcelSheet<Item>()?.GetRow(itemId);
        if (item == null)
            return;

        if (Config.DisableClickToOpenMapForCrystals && item.ItemUICategory.Row == 59)
            return;

        var tuple = GetPointForItem(itemId);
        if (tuple == null)
            return;

        var (totalPoints, point, cost, isSameZone, placeName) = tuple.Value;

        OpenMapWithGatheringPoint(point, item);
    }

    [SigHook("48 89 5C 24 ?? 48 89 54 24 ?? 48 89 4C 24 ?? 55 56 57 41 54 41 55 41 56 41 57 48 83 EC 50 49 8B 08")]
    public void AddonRecipeMaterialList_SetupRow(AddonRecipeMaterialList* addon, nint a2, nint a3)
    {
        AddonRecipeMaterialList_SetupRowHook.Original(addon, a2, a3);

        if (!Config.EnableZoneNames)
            return;

        var data = **(nint**)(a2 + 0x08);
        var itemId = *(uint*)(data + 0x04);

        // TODO: only for missing items?

        var item = Service.Data.GetExcelSheet<Item>()?.GetRow(itemId);
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
        nameNode->AtkResNode.Flags_2 |= 0x1;

        nameNode->TextFlags = 192; // allow multiline text (not sure on the actual flags it sets though)
        nameNode->LineSpacing = 17;

        var itemName = MemoryHelper.ReadSeStringNullTerminated(textPtr).TextValue;
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

    [SigHook("48 89 5C 24 ?? 57 48 83 EC 20 BA ?? ?? ?? ?? 48 8B D9 E8 ?? ?? ?? ?? 48 8B F8 48 85 C0 74 5A")]
    public nint AgentRecipeMaterialList_OpenRecipeResultItemContextMenu(AgentRecipeMaterialList* agent)
    {
        HandleRecipeResultItemContextMenu = true;
        return AgentRecipeMaterialList_OpenRecipeResultItemContextMenuHook.Original(agent);
    }

    [SigHook("E8 ?? ?? ?? ?? 45 8B C4 41 8B D7")]
    public nint AddItemContextMenuEntries(AgentRecipeItemContext* agent, uint itemId, byte flags, byte* itemName)
    {
        if (!HandleRecipeResultItemContextMenu)
            goto originalAddItemContextMenuEntries;

        HandleRecipeResultItemContextMenu = false;

        if (!Config.AddSearchForItemByCraftingMethodContextMenuEntry)
            goto originalAddItemContextMenuEntries;

        if (GetAddon(AgentId.RecipeMaterialList) == null)
            goto originalAddItemContextMenuEntries;

        if (agentRecipeMaterialList->Recipe == null || agentRecipeMaterialList->Recipe->ResultItemId != itemId)
            goto originalAddItemContextMenuEntries;

        var control = Control.Instance();
        if (control == null || control->LocalPlayer == null || control->LocalPlayer->Character.EventState == 5)
            goto originalAddItemContextMenuEntries;

        flags |= 2;

        originalAddItemContextMenuEntries:
        return AddItemContextMenuEntriesHook.Original(agent, itemId, flags, itemName);
    }

    private (int, GatheringPoint, uint, bool, SeString)? GetPointForItem(uint itemId)
    {
        var GatheringItemSheet = Service.Data.GetExcelSheet<GatheringItem>();
        var GatheringPointBaseSheet = Service.Data.GetExcelSheet<GatheringPointBase>();
        var GatheringPointSheet = Service.Data.GetExcelSheet<GatheringPoint>();

        if (GatheringItemSheet == null || GatheringPointBaseSheet == null || GatheringPointSheet == null)
            return null;

        var gatheringItem = GatheringItemSheet.FirstOrDefault(row => row?.Item == itemId, null);
        if (gatheringItem == null)
            return null;

        var gatheringPoints = GatheringPointBaseSheet
            .Where(row => row.Item.Any(item => item == gatheringItem.RowId))
            .Select(row => GatheringPointSheet.FirstOrDefault(gprow => gprow?.GatheringPointBase.Row == row.RowId, null))
            .Where(row => row != null && row.TerritoryType.Row > 1)
            /* not needed?
            .GroupBy(row => row!.RowId)
            .Select(rows => rows.First())
            */
            .ToList();

        if (gatheringPoints == null || !gatheringPoints.Any())
            return null;

        var currentTerritoryTypeId = GameMain.Instance()->CurrentTerritoryTypeId;
        var point = gatheringPoints.FirstOrDefault(row => row?.TerritoryType.Row == currentTerritoryTypeId, null);
        var isSameZone = point != null;
        var cost = 0u;
        if (point == null)
        {
            foreach (var p in gatheringPoints)
            {
                var thisCost = CalculateTeleportCost(currentTerritoryTypeId, p!.TerritoryType.Row, false, false, false);
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


    [Signature("66 89 54 24 ?? 66 89 4C 24 ?? 53")]
    public readonly CalculateTeleportCostDelegate CalculateTeleportCost = null!;
    public delegate uint CalculateTeleportCostDelegate(uint fromTerritoryTypeId, uint toTerritoryTypeId, bool a3, bool a4, bool a5);

    [Signature("80 F9 07 77 10")]
    public readonly IsGatheringPointTypeOffDelegate IsGatheringPointRare = null!;
    public delegate bool IsGatheringPointTypeOffDelegate(byte gatheringPointType);

    [Signature("E8 ?? ?? ?? ?? 41 B0 07")]
    public readonly FormatAddonTextDelegate FormatAddonText = null!;
    public delegate byte* FormatAddonTextDelegate(RaptureTextModule* module, uint id, int value);

    [Signature("E8 ?? ?? ?? ?? 4C 8B 05 ?? ?? ?? ?? 48 8D 8C 24 ?? ?? ?? ?? 48 8B D0 E8 ?? ?? ?? ?? 8B 4E 08")]
    public readonly GetGatheringPointNameDelegate GetGatheringPointName = null!;
    public delegate byte* GetGatheringPointNameDelegate(RaptureTextModule** module, byte gatheringType, byte gatheringPointType);

    public bool OpenMapWithGatheringPoint(GatheringPoint? gatheringPoint, Item? item = null)
    {
        if (gatheringPoint == null)
            return false;

        var territoryType = gatheringPoint.TerritoryType.Value;
        if (territoryType == null)
            return false;

        var gatheringPointBase = gatheringPoint.GatheringPointBase.Value;
        if (gatheringPointBase == null)
            return false;

        var exportedPoint = Service.Data.GetExcelSheet<ExportedGatheringPoint>()?.GetRow(gatheringPointBase.RowId);
        if (exportedPoint == null)
            return false;

        var gatheringType = exportedPoint.GatheringType.Value;
        if (gatheringType == null)
            return false;

        var raptureTextModule = Framework.Instance()->GetUiModule()->GetRaptureTextModule();

        var levelText = gatheringPointBase.GatheringLevel == 1
            ? raptureTextModule->GetAddonText(242) // "Lv. ???"
            : FormatAddonText(raptureTextModule, 35, gatheringPointBase.GatheringLevel);
        var space = MemoryUtils.FromString(" ");
        var gatheringPointName = GetGatheringPointName(
            &raptureTextModule,
            (byte)exportedPoint.GatheringType.Row,
            exportedPoint.GatheringPointType
        );

        var tooltipPtr = MemoryUtils.strconcat(levelText, space, gatheringPointName);
        var tooltip = IMemorySpace.GetDefaultSpace()->Create<Utf8String>();
        tooltip->SetString(tooltipPtr);

        var iconId = !IsGatheringPointRare(exportedPoint.GatheringPointType)
            ? gatheringType.IconMain
            : gatheringType.IconOff;

        var agentMap = Framework.Instance()->GetUiModule()->GetAgentModule()->GetAgentMap();
        agentMap->TempMapMarkerCount = 0;
        agentMap->AddGatheringTempMarker(
            4u,
            (int)Math.Round(exportedPoint.X),
            (int)Math.Round(exportedPoint.Y),
            (uint)iconId,
            exportedPoint.Radius,
            tooltip
        );

        var titleBuilder = new SeStringBuilder().AddText("\uE078");
        if (item != null)
        {
            titleBuilder
                .AddText(" ")
                .AddUiForeground(549)
                .AddUiGlow(550)
                .Append(item.Name.ToDalamudString())
                .AddUiGlowOff()
                .AddUiForegroundOff();
        }
        var titlePtr = MemoryUtils.FromByteArray(titleBuilder.BuiltString.Encode());
        var title = IMemorySpace.GetDefaultSpace()->Create<Utf8String>();
        title->SetString(titlePtr);

        var mapInfo = stackalloc OpenMapInfo[1];
        mapInfo->Type = FFXIVClientStructs.FFXIV.Client.UI.Agent.MapType.GatheringLog;
        mapInfo->MapId = territoryType.Map.Row;
        mapInfo->TerritoryId = territoryType.RowId;
        mapInfo->TitleString = *title;
        agentMap->OpenMap(mapInfo);
        title->Dtor();
        IMemorySpace.Free(title);
        Marshal.FreeHGlobal((nint)titlePtr);

        tooltip->Dtor();
        IMemorySpace.Free(tooltip);
        Marshal.FreeHGlobal((nint)tooltipPtr);
        Marshal.FreeHGlobal((nint)space);

        return true;
    }
}
