using System.Threading;
using Dalamud.Game.Inventory;
using Dalamud.Game.Inventory.InventoryEventArgTypes;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using AgentRecipeNote = FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentRecipeNote;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class AutoOpenRecipe : Tweak
{
    private readonly ExcelService _excelService;
    private readonly TextService _textService;
    private readonly IFramework _framework;
    private readonly IGameInventory _gameInventory;

    private CancellationTokenSource? _checkCTS;
    private DateTime _lastTimeRecipeOpened = DateTime.MinValue;

    public override void OnEnable()
    {
        _gameInventory.ItemAddedExplicit += GameInventory_ItemAddedExplicit;
    }

    public override void OnDisable()
    {
        _gameInventory.ItemAddedExplicit -= GameInventory_ItemAddedExplicit;
        _checkCTS?.Cancel();
        _checkCTS = null;
    }

    private void GameInventory_ItemAddedExplicit(InventoryItemAddedArgs data)
    {
        if (data.Item.ItemId == 0) // idk, just for safety
            return;

        if (data.Item.ContainerType is not (GameInventoryType.Inventory1 or GameInventoryType.Inventory2 or GameInventoryType.Inventory3 or GameInventoryType.Inventory4 or GameInventoryType.KeyItems)) // only handle inventory
            return;

        if (Conditions.Instance()->Crafting || Conditions.Instance()->ExecutingCraftingAction || AgentRecipeNote.Instance()->ActiveCraftRecipeId != 0) // skip if crafting
            return;

        if (DateTime.UtcNow - _lastTimeRecipeOpened < TimeSpan.FromSeconds(3))
            return;

        _logger.LogDebug("Inventory item added: {item}", data.Item);

        _checkCTS?.Cancel();
        _checkCTS = null;
        _checkCTS = new();

        void action()
        {
            if (TryOpenRecipeForItem(data.Item.ItemId))
                _lastTimeRecipeOpened = DateTime.UtcNow;
        }

        _framework.RunOnTick(
            action,
            delay: TimeSpan.FromMilliseconds(100),
            cancellationToken: _checkCTS.Token);
    }

    private bool TryOpenRecipeForItem(uint itemId)
    {
        var localPlayer = Control.GetLocalPlayer();
        if (localPlayer == null)
        {
            _logger.LogDebug("Skipping: LocalPlayer is null");
            return false;
        }

        var questManager = QuestManager.Instance();
        foreach (ref var questWork in questManager->NormalQuests)
        {
            if (questWork.QuestId == 0)
            {
                _logger.LogDebug("Skipping: Quest #{questId}", questWork.QuestId);
                continue;
            }

            if (!_excelService.TryGetRow<Quest>((uint)questWork.QuestId | 0x10000, out var quest))
            {
                _logger.LogDebug("Skipping: Quest #{questId} not found", questWork.QuestId | 0x10000);
                continue;
            }

            if (!(questManager->GetDailyQuestById(questWork.QuestId) != null || quest.BeastTribe.RowId != 0)) // check if daily or tribal quest
            {
                _logger.LogDebug("Skipping: Quest #{questId} is not a daily or tribal quest", questWork.QuestId | 0x10000);
                continue;
            }

            _logger.LogDebug("Checking Quest #{questId} ({questName}) (Sequence {questSequence})", questWork.QuestId, _textService.GetQuestName((uint)questWork.QuestId | 0x10000), questWork.Sequence);

            // TODO: check if this still works
            var sequence = questWork.Sequence;
            if (!quest.TodoParams.TryGetFirst(param => param.ToDoCompleteSeq == sequence, out var todoParams, out var todoOffset))
            {
                _logger.LogDebug("Skipping: ToDoCompleteSeq {sequence} not found", sequence);
                continue;
            }

            var questEventHandler = (QuestEventHandler*)EventFramework.Instance()->GetEventHandlerById(quest.RowId);
            if (questEventHandler == null)
            {
                _logger.LogDebug("Skipping: No QuestEventHandler");
                continue;
            }

            uint GetScriptArg(string scriptArgName = "RITEM1")
            {
                return quest.QuestParams.TryGetFirst(p => p.ScriptInstruction == scriptArgName, out var param) ? param.ScriptArg : 0;
            }

            var todoCount = todoParams.ToDoQty;
            for (var todoIndex = (byte)todoOffset; todoIndex < todoOffset + todoCount; todoIndex++)
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

                _logger.LogDebug("TodoArgs #{todoIndex}: {numHave}/{numNeeded} of {resultItemId}", todoIndex, numHave, numNeeded, resultItemId);

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
        if (!_excelService.TryFindRow<Recipe>(row => row.ItemResult.RowId == resultItemId && row.CraftType.RowId == craftType, out var recipe) &&
            !_excelService.TryFindRow(row => row.ItemResult.RowId == resultItemId, out recipe))
        {
            _logger.LogDebug("Not opening Recipe for Item {resultItemId}: Recipe not found", resultItemId);
            return false;
        }

        if (!recipe.Ingredient.Any(item => item.RowId == materialItemId))
        {
            _logger.LogDebug("Not opening Recipe for Item {resultItemId}: Required ingredient {materialItemId} not needed", resultItemId, materialItemId);
            return false;
        }

        if (!IngredientsAvailable(recipe, amount))
        {
            _logger.LogDebug("Not opening Recipe for Item {resultItemId}: Required ingredients not available", resultItemId);
            return false;
        }

        _logger.LogDebug("Requirements met, opening recipe {recipeId}", recipe.RowId);

        var agentRecipeNote = AgentRecipeNote.Instance();

        agentRecipeNote->AgentInterface.Hide();

        if (recipe.CraftType.RowId == craftType)
            agentRecipeNote->OpenRecipeByRecipeIdInternal(recipe.RowId);
        else
            agentRecipeNote->SearchRecipeByItemId(resultItemId);

        return true;
    }

    private bool IngredientsAvailable(Recipe recipe, uint amount)
    {
        var inventoryManager = InventoryManager.Instance();
        if (inventoryManager == null)
            return false;

        for (var i = 0; i < recipe.Ingredient.Count; i++)
        {
            var ingredientItem = recipe.Ingredient[i];
            var ingredientAmount = recipe.AmountIngredient[i];

            if (ingredientAmount == 0 || !ingredientItem.IsValid)
                continue;

            if (ingredientItem.Value.ItemUICategory.RowId == 59) // ignore Crystals
                continue;

            var numHave = inventoryManager->GetInventoryItemCount(ingredientItem.RowId, false, false, false); // Normal
            numHave += inventoryManager->GetInventoryItemCount(ingredientItem.RowId, true, false, false); // HQ

            var numNeeded = ingredientAmount * amount;

            _logger.LogDebug("Checking Ingredient #{itemIngredient}: need {numNeeded}, have {numHave}", ingredientItem.RowId, numNeeded, numHave);

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

        var numCraftTypes = _excelService.GetRowCount<CraftType>();
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
