using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Client.UI.Shell;
using HaselCommon.Extensions;
using HaselCommon.Services;
using HaselTweaks.Config;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;
using Lumina.Excel.GeneratedSheets;
using Microsoft.Extensions.Logging;

namespace HaselTweaks.Tweaks;

public unsafe partial class AutoSorter(
    PluginConfig PluginConfig,
    ConfigGui ConfigGui,
    TextService TextService,
    ILogger<AutoSorter> Logger,
    ExcelService ExcelService,
    IClientState ClientState,
    IFramework Framework,
    AddonObserver AddonObserver)
    : IConfigurableTweak
{
    public string InternalName => nameof(AutoSorter);
    public TweakStatus Status { get; set; } = TweakStatus.Outdated;

    private static readonly Dictionary<string, uint> CategorySet = new()
    {
        ["inventory"] = 257,
        ["retainer"] = 261,
        ["armoury"] = 259,
        ["saddlebag"] = 467,
        ["rightsaddlebag"] = 469,
        ["mh"] = 26,
        ["oh"] = 28,
        ["head"] = 37,
        ["body"] = 41,
        ["hands"] = 47,
        ["legs"] = 45,
        ["feet"] = 49,
        ["neck"] = 53,
        ["ears"] = 285,
        ["wrists"] = 287,
        ["rings"] = 289,
        ["soul"] = 291
    };

    private static readonly Dictionary<string, uint> ConditionSet = new()
    {
        ["id"] = 271,
        ["spiritbond"] = 275,
        ["category"] = 263,
        ["lv"] = 265,
        ["ilv"] = 267,
        ["stack"] = 269,
        ["hq"] = 277,
        ["materia"] = 279,
        ["pdamage"] = 293,
        ["mdamage"] = 295,
        ["delay"] = 297,
        ["autoattack"] = 299,
        ["blockrate"] = 301,
        ["blockstrength"] = 303,
        ["defense"] = 305,
        ["mdefense"] = 307,
        ["str"] = 309,
        ["dex"] = 311,
        ["vit"] = 313,
        ["int"] = 315,
        ["mnd"] = 317,
        ["craftsmanship"] = 321,
        ["control"] = 323,
        ["gathering"] = 325,
        ["perception"] = 327,
        ["tab"] = 273
    };

    private static readonly Dictionary<string, uint> OrderSet = new()
    {
        ["asc"] = 281,
        ["des"] = 283
    };

    internal static readonly List<string> ArmourySubcategories =
    [
        "mh",
        "oh",
        "head",
        "body",
        "hands",
        "legs",
        "feet",
        "neck",
        "ears",
        "wrists",
        "rings",
        "soul"
    ];

    private string GetLocalizedParam(uint rowId, string? fallback = null)
    {
        var param = ExcelService.GetRow<TextCommandParam>(rowId)?.Param.ExtractText();
        return string.IsNullOrEmpty(param) ? fallback ?? "" : param.ToLower();
    }

    private string? GetLocalizedParam(Dictionary<string, uint> dict, string? key, string? fallback = null)
    {
        var str = fallback ?? key;

        if (!string.IsNullOrEmpty(key) && dict.TryGetValue(key, out var rowId))
        {
            var param = ExcelService.GetRow<TextCommandParam>(rowId)?.Param.ExtractText();

            if (!string.IsNullOrEmpty(param))
            {
                str = param.ToLower();
            }
        }

        return str;
    }

    private readonly Queue<IGrouping<string, AutoSorterConfiguration.SortingRule>> _queue = new();
    private bool _isBusy = false;
    private byte _lastClassJobId = 0;

    private static bool IsRetainerInventoryOpen => IsAddonOpen("InventoryRetainer") || IsAddonOpen("InventoryRetainerLarge");
    private static bool IsInventoryBuddyOpen => IsAddonOpen("InventoryBuddy");

    public void OnInitialize() { }

    public void OnEnable()
    {
        _queue.Clear();

        ClientState.Login += OnLogin;
        ClientState.Logout += OnLogout;
        Framework.Update += OnFrameworkUpdate;
        AddonObserver.AddonOpen += OnAddonOpen;
    }

    public void OnDisable()
    {
        _queue.Clear();

        ClientState.Login -= OnLogin;
        ClientState.Logout -= OnLogout;
        Framework.Update -= OnFrameworkUpdate;
        AddonObserver.AddonOpen -= OnAddonOpen;
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();

        Status = TweakStatus.Disposed;
        GC.SuppressFinalize(this);
    }

    private void OnLogin()
    {
        _lastClassJobId = (byte)(ClientState.LocalPlayer?.ClassJob.Id ?? 0);
        _queue.Clear();
    }

    private void OnLogout()
    {
        _lastClassJobId = 0;
        _queue.Clear();
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (!ClientState.IsLoggedIn)
            return;

        if (Config.SortArmouryOnJobChange)
        {
            var classJobId = PlayerState.Instance()->CurrentClassJobId;
            if (_lastClassJobId != classJobId)
            {
                _lastClassJobId = classJobId;

                if (IsAddonOpen("ArmouryBoard"))
                {
                    OnOpenArmoury();
                }
            }
        }

        ProcessQueue();
    }

    private void OnAddonOpen(string addonName)
    {
        switch (addonName)
        {
            case "ArmouryBoard":
                OnOpenArmoury();
                break;
            case "InventoryBuddy":
                OnOpenInventoryBuddy();
                break;
            case "InventoryRetainer":
            case "InventoryRetainerLarge":
                OnOpenRetainer();
                break;
            case "Inventory":
            case "InventoryLarge":
            case "InventoryExpansion":
                OnOpenInventory();
                break;
        }
    }

    private void OnOpenArmoury()
    {
        var groups = Config.Settings
            .FindAll(entry => entry.Enabled && (entry.Category is "armoury" || ArmourySubcategories.Any(subcat => subcat == entry.Category)))
            .GroupBy(entry => entry.Category!);

        foreach (var group in groups)
        {
            _queue.Enqueue(group);
        }
    }

    private void OnOpenInventory()
    {
        if (Conditions.IsInBetweenAreas || Conditions.IsOccupiedInQuestEvent || Conditions.IsOccupiedInCutSceneEvent)
            return;

        var groups = Config.Settings
            .FindAll(entry => entry.Enabled && entry.Category is "inventory")
            .GroupBy(entry => entry.Category!);

        foreach (var group in groups)
        {
            _queue.Enqueue(group);
        }
    }

    private void OnOpenInventoryBuddy()
    {
        var groups = Config.Settings
            .FindAll(entry => entry.Enabled && entry.Category is "saddlebag" or "rightsaddlebag")
            .GroupBy(entry => entry.Category!);

        foreach (var group in groups)
        {
            _queue.Enqueue(group);
        }
    }

    private void OnOpenRetainer()
    {
        var groups = Config.Settings
            .FindAll(entry => entry.Enabled && entry.Category is "retainer")
            .GroupBy(entry => entry.Category!);

        foreach (var group in groups)
        {
            _queue.Enqueue(group);
        }
    }

    private void ProcessQueue()
    {
        if (_isBusy || _queue.Count == 0)
            return;

        var nextGroup = _queue.Peek();
        if (nextGroup == null)
            return;

        var itemOrderModule = ItemOrderModule.Instance();
        if (itemOrderModule == null)
            return;

        if (nextGroup.Key is "armoury" || ArmourySubcategories.Any(subcat => subcat == nextGroup.Key))
        {
            // check if ItemOrderModule is busy
            if (itemOrderModule == null || itemOrderModule->UserFileEvent.IsSavePending)
            {
                Logger.LogDebug("ItemOrderModule is busy, waiting.");
                return;
            }

            for (var i = 0; i < itemOrderModule->ArmourySorter.Length; i++)
            {
                var sorter = itemOrderModule->ArmourySorter.GetPointer(i)->Value;
                if (sorter != null && sorter->SortFunctionIndex != -1)
                {
                    Logger.LogDebug("ItemOrderModule: Sorter #{i} ({type}) is busy, waiting.", i, sorter->InventoryType.ToString());
                    return;
                }
            }
        }

        _isBusy = true;

        try
        {
            var group = _queue.Dequeue();

            if (!group.Any())
                return;

            var key = group.Key;

            if (string.IsNullOrEmpty(key))
                return;

            Logger.LogInformation("Sorting Category: {key}", key);

            var category = GetLocalizedParam(CategorySet, key);
            if (string.IsNullOrEmpty(category))
            {
                Logger.LogError("Can not localize category: GetLocalizedParam returned \"{category}\".", category);
                return;
            }

            var raptureShellModule = RaptureShellModule.Instance();
            if (raptureShellModule == null)
            {
                Logger.LogWarning("Could not resolve RaptureShellModule");
                return;
            }

            if (raptureShellModule->IsTextCommandUnavailable)
            {
                Logger.LogWarning("Text commands are unavailable, skipping.");
                return;
            }

            if ((key is "saddlebag" or "rightsaddlebag") && !IsInventoryBuddyOpen)
            {
                Logger.LogWarning("Sorting for saddlebag/rightsaddlebag only works when the window is open, skipping.");
                return;
            }

            var playerState = PlayerState.Instance();
            if (playerState == null)
            {
                Logger.LogWarning("Could not resolve PlayerState");
                return;
            }

            if (key is "rightsaddlebag" && !playerState->HasPremiumSaddlebag)
            {
                Logger.LogWarning("Not subscribed to the Companion Premium Service, skipping.");
                return;
            }

            if (key is "retainer" && !IsRetainerInventoryOpen)
            {
                Logger.LogWarning("Sorting for retainer only works when the window is open, skipping.");
                return;
            }

            var cmd = $"/itemsort clear {category}";
            Logger.LogInformation("Executing {cmd}", cmd);
            ExecuteCommand(cmd);

            foreach (var rule in group)
            {
                var condition = GetLocalizedParam(ConditionSet, rule.Condition);
                if (string.IsNullOrEmpty(condition))
                {
                    Logger.LogError("Can not localize condition \"{ruleCondition}\", skipping. (GetLocalizedParam returned \"{condition}\")", rule.Condition, condition);
                    continue;
                }

                var order = GetLocalizedParam(OrderSet, rule.Order);
                if (string.IsNullOrEmpty(order))
                {
                    Logger.LogError("Can not localize order \"{ruleOrder}\", skipping. (GetLocalizedParam returned \"{order}\")", rule.Order, order);
                    continue;
                }

                cmd = $"/itemsort condition {category} {condition} {order}";
                Logger.LogInformation("Executing {cmd}", cmd);
                ExecuteCommand(cmd);
            }

            cmd = $"/itemsort execute {category}";
            Logger.LogInformation("Executing {cmd}", cmd);
            ExecuteCommand(cmd);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error during sorting");
        }
        finally
        {
            _isBusy = false;
        }
    }
}
