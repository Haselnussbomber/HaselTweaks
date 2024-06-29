using System.Linq;
using System.Threading;
using Dalamud.Game.Inventory;
using Dalamud.Game.Inventory.InventoryEventArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Extensions;
using HaselCommon.Services;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;
using Lumina.Excel.GeneratedSheets;
using Microsoft.Extensions.Logging;
using AgentRecipeNote = FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentRecipeNote;

namespace HaselTweaks.Tweaks;

public unsafe class AutoOpenRecipe(
    ILogger<AutoOpenRecipe> Logger,
    ExcelService ExcelService,
    TextService TextService,
    IFramework Framework,
    IGameInventory GameInventory)
    : ITweak
{
    public string InternalName => nameof(AutoOpenRecipe);
    public TweakStatus Status { get; set; } = TweakStatus.Outdated;

    private CancellationTokenSource? CheckCTS;
    private DateTime LastTimeRecipeOpened = DateTime.MinValue;

    public void OnInitialize() { }

    public void OnEnable()
    {
        GameInventory.ItemAddedExplicit += GameInventory_ItemAddedExplicit;
    }

    public void OnDisable()
    {
        CheckCTS?.Cancel();
        CheckCTS = null;
        GameInventory.ItemAddedExplicit -= GameInventory_ItemAddedExplicit;
    }

    public void Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();

        Status = TweakStatus.Disposed;
        GC.SuppressFinalize(this);
    }

    private void GameInventory_ItemAddedExplicit(InventoryItemAddedArgs data)
    {
        if (data.Item.ItemId == 0) // idk, just for safety
            return;

        if (data.Item.ContainerType is not (GameInventoryType.Inventory1 or GameInventoryType.Inventory2 or GameInventoryType.Inventory3 or GameInventoryType.Inventory4 or GameInventoryType.KeyItems)) // only handle inventory
            return;

        if (Conditions.IsCrafting || Conditions.IsCrafting40 || AgentRecipeNote.Instance()->ActiveCraftRecipeId != 0) // skip if crafting
            return;

        if (DateTime.UtcNow - LastTimeRecipeOpened < TimeSpan.FromSeconds(3))
            return;

        Logger.LogDebug($"Inventory item added: {data.Item}");

        CheckCTS?.Cancel();
        CheckCTS = null;
        CheckCTS = new();

        void action()
        {
            if (TryOpenRecipeForItem(data.Item.ItemId))
                LastTimeRecipeOpened = DateTime.UtcNow;
        }

        Framework.RunOnTick(
            action,
            delay: TimeSpan.FromMilliseconds(100),
            cancellationToken: CheckCTS.Token);
    }

    private bool TryOpenRecipeForItem(uint itemId)
    {
        var localPlayer = Control.GetLocalPlayer();
        if (localPlayer == null)
            return false;

        var questManager = QuestManager.Instance();
        foreach (ref var questWork in questManager->NormalQuests)
        {
            if (questWork.QuestId == 0)
                continue;

            var quest = ExcelService.GetRow<Quest>((uint)questWork.QuestId | 0x10000);
            if (quest == null || !(questManager->GetDailyQuestById(questWork.QuestId) != null || quest.BeastTribe.Row != 0)) // check if daily or tribal quest
                continue;

            Logger.LogDebug($"Checking Quest #{questWork.QuestId} ({TextService.GetQuestName((uint)questWork.QuestId | 0x10000)}) (Sequence {questWork.Sequence})");

            var todoOffset = (byte)quest!.ToDoCompleteSeq.IndexOf(questWork.Sequence);
            if (todoOffset < 0 || todoOffset >= quest.ToDoQty.Length)
            {
                Logger.LogDebug($"Skipping: todoOffset = {todoOffset}, quest.ToDoQty.Length = {quest.ToDoQty.Length}");
                continue;
            }

            var questEventHandler = (QuestEventHandler*)EventFramework.Instance()->GetEventHandlerById(quest.RowId);
            if (questEventHandler == null)
            {
                Logger.LogDebug($"Skipping: No QuestEventHandler");
                continue;
            }

            uint GetScriptArg(string scriptArgName = "RITEM1")
            {
                var scriptArgIndex = quest.ScriptInstruction
                    .IndexOf(entry => entry.RawString == scriptArgName);

                return quest.ScriptArg.ElementAtOrDefault(scriptArgIndex);
            }

            var todoCount = quest.ToDoQty[todoOffset];
            for (var todoIndex = todoOffset; todoIndex < todoOffset + todoCount; todoIndex++)
            {
                uint numHave, numNeeded, resultItemId;
                questEventHandler->GetTodoArgs(localPlayer, todoIndex, &numHave, &numNeeded, &resultItemId);

                // for older quests that don't return the item id in GetTodoArgs
                switch ((questWork.QuestId, todoIndex))
                {
                    // Ixal
                    case (1487, 2):
                    case (1487, 3):
                        resultItemId = GetScriptArg("RITEM2");
                        break;
                    case (1493, 12):
                        resultItemId = GetScriptArg("RITEM3");
                        break;
                    case (1488, 3):
                    case (1489, 4):
                    case (1491, 2):
                    case (1493, 1):
                    case (1494, 3):
                    case (1495, 3):
                    case (1496, 2):
                    case (1497, 2):
                    case (1498, 4):
                    case (1499, 4):
                    case (1500, 2):
                    case (1501, 2):
                    case (1502, 3):
                    case (1503, 2):
                    case (1504, 2):
                    case (1505, 2):
                    case (1506, 2):
                    case (1507, 2):
                    case (1508, 2):
                    case (1509, 3):
                    case (1510, 4):
                    case (1511, 4):
                    case (1512, 3):
                    case (1513, 3):
                    case (1514, 2):
                    case (1515, 3):
                    case (1516, 3):
                    case (1517, 2):
                    case (1518, 3):
                    case (1519, 3):
                    case (1520, 3):
                    case (1521, 4):
                    case (1522, 4):
                    case (1523, 3):
                    case (1566, 2):
                    case (1567, 2):
                    case (1568, 2):
                        resultItemId = GetScriptArg();
                        break;

                    // Moogle
                    case (2290, 1):
                    case (2291, 1):
                    case (2292, 3):
                    case (2293, 3):
                    case (2294, 3):
                    case (2296, 1):
                    case (2298, 1):
                    case (2299, 1):
                    case (2300, 1):
                    case (2301, 1):
                    case (2303, 1):
                    case (2304, 2):
                    case (2305, 4):
                    case (2307, 1):
                    case (2310, 2):
                    case (2311, 3):
                    case (2313, 3):
                    case (2314, 2):
                    case (2316, 2):
                    case (2317, 1):
                    case (2318, 5):
                    case (2320, 3):
                    case (2322, 5):
                    case (2324, 4):
                    case (2325, 5):
                    case (2326, 7):
                        resultItemId = GetScriptArg();
                        break;

                    // Namazu
                    case (3098, 1):
                    case (3099, 1):
                    case (3100, 1):
                    case (3101, 1):
                    case (3106, 1):
                    case (3107, 1):
                    case (3108, 1):
                    case (3109, 1):
                    case (3110, 1):
                    case (3111, 1):
                    case (3112, 1):
                    case (3113, 1):
                    case (3114, 1):
                    case (3117, 1):
                    case (3120, 1):
                    case (3123, 1):
                    case (3126, 1):
                    case (3129, 1):
                    case (3130, 1):
                        resultItemId = GetScriptArg("QST_PRODUCT_ITEM");
                        break;
                }

                resultItemId %= 1000000;

                Logger.LogDebug($"TodoArgs #{todoIndex}: {numHave}/{numNeeded} of {resultItemId}");

                if (resultItemId == 0)
                    continue;

                if (TryOpenRecipe(itemId, resultItemId, numNeeded))
                    return true;
            }
        }

        return false;
    }

    private bool TryOpenRecipe(uint materialItemId, uint resultItemId, uint amount)
    {
        var craftType = GetCurrentCraftType();
        var recipe = ExcelService.FindRow<Recipe>(row => row?.ItemResult.Row == resultItemId && row.CraftType.Row == craftType)
                  ?? ExcelService.FindRow<Recipe>(row => row?.ItemResult.Row == resultItemId);
        if (recipe == null)
        {
            Logger.LogDebug($"Not opening Recipe for Item {resultItemId}: Recipe not found");
            return false;
        }

        if (!recipe.UnkData5.Any(item => item.ItemIngredient == materialItemId))
        {
            Logger.LogDebug($"Not opening Recipe for Item {resultItemId}: Required ingredient {materialItemId} not needed");
            return false;
        }

        if (!IngredientsAvailable(recipe, amount))
        {
            Logger.LogDebug($"Not opening Recipe for Item {resultItemId}: Required ingredients not available");
            return false;
        }

        Logger.LogDebug($"Requirements met, opening recipe {recipe.RowId}");

        var agentRecipeNote = AgentRecipeNote.Instance();

        agentRecipeNote->AgentInterface.Hide();

        if (recipe.CraftType.Row == craftType)
            agentRecipeNote->OpenRecipeByRecipeIdInternal(recipe.RowId);
        else
            agentRecipeNote->OpenRecipeByItemId(resultItemId);

        return true;
    }

    private bool IngredientsAvailable(Recipe recipe, uint amount)
    {
        var inventoryManager = InventoryManager.Instance();
        if (inventoryManager == null)
            return false;

        foreach (var ingredient in recipe.UnkData5)
        {
            if (ingredient.ItemIngredient == 0 || ingredient.AmountIngredient == 0)
                continue;

            var itemId = (uint)ingredient.ItemIngredient;

            var item = ExcelService.GetRow<Item>(itemId);
            if (item == null || item.ItemUICategory.Row == 59) // ignore Crystals
                continue;

            var numHave = inventoryManager->GetInventoryItemCount(itemId, false, false, false); // Normal
            numHave += inventoryManager->GetInventoryItemCount(itemId, true, false, false); // HQ

            var numNeeded = ingredient.AmountIngredient * amount;

            Logger.LogDebug($"Checking Ingredient #{ingredient.ItemIngredient}: need {numNeeded}, have {numHave}");

            if (numHave < numNeeded)
                return false;
        }

        return true;
    }

    private int GetCurrentCraftType()
    {
        var craftType = -1;
        var recipeNote = RecipeNote.Instance();
        var playerState = PlayerState.Instance();

        if (recipeNote == null || playerState == null)
            return craftType;

        var numCraftTypes = ExcelService.GetRowCount<CraftType>();
        for (var i = 0; i < numCraftTypes; i++)
        {
            if (recipeNote->Jobs[i] == playerState->CurrentClassJobId)
            {
                craftType = i;
                break;
            }
        }

        return craftType;
    }
}
