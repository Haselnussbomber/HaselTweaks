using System.Linq;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using EventHandler = FFXIVClientStructs.FFXIV.Client.Game.Event.EventHandler;

namespace HaselTweaks.Tweaks;

public unsafe partial class AutoOpenRecipe : Tweak
{
    public override string Name => "Auto-open Recipe";
    public override string Description => "When a new daily quest objective requires you to craft an item and you have all materials for it in your inventory at that moment, this tweak will automatically open the recipe.";

    private ExcelSheet<Quest> questSheet = null!;
    private ExcelSheet<Recipe> recipeSheet = null!;
    private ExcelSheet<CraftType> craftTypeSheet = null!;
    private ExcelSheet<Item> itemSheet = null!;

    private AgentRecipeNote* agentRecipeNote;
    private InventoryManager* inventoryManager;
    private QuestManager* questManager;
    private RecipeNote* recipeNote;
    private PlayerState* playerState;

    // for older quests that don't return the item id in GetTodoArgs
    private readonly record struct QuestTodo(ushort QuestId, byte TodoIndex, string ScriptArgName = "RITEM1");
    private readonly QuestTodo[] QuestTodos = new QuestTodo[] {
        // Ixal
        new(1494, 3),
        new(1495, 3),
        new(1496, 2),
        new(1497, 2),
        new(1504, 2),
        new(1505, 2),
        new(1506, 2),
        new(1507, 2),
        new(1508, 2),
        new(1514, 2),
        new(1515, 3),
        new(1516, 3),
        new(1517, 2),
        new(1518, 3),
        new(1498, 4),
        new(1499, 4),
        new(1500, 2),
        new(1501, 2),
        new(1502, 3),
        new(1503, 2),
        new(1509, 3),
        new(1510, 4),
        new(1511, 4),
        new(1512, 3),
        new(1513, 3),
        new(1519, 3),
        new(1520, 3),
        new(1521, 4),
        new(1522, 4),
        new(1523, 3),
        new(1566, 2),
        new(1567, 2),
        new(1568, 2),
        new(1487, 2, "RITEM2"),
        new(1487, 3, "RITEM2"),
        new(1488, 3),
        new(1489, 4),
        new(1491, 2),
        new(1493, 1),
        new(1493, 12, "RITEM3"),

        // Moogle
        new(2320, 3),
        new(2322, 5),
        new(2324, 4),
        new(2325, 5),
        new(2326, 7),
        new(2290, 1),
        new(2291, 1),
        new(2292, 3),
        new(2293, 3),
        new(2294, 3),
        new(2296, 1),
        new(2298, 1),
        new(2299, 1),
        new(2300, 1),
        new(2301, 1),
        new(2303, 1),
        new(2304, 2),
        new(2305, 4),
        new(2307, 1),
        new(2310, 2),
        new(2311, 3),
        new(2313, 3),
        new(2314, 2),
        new(2316, 2),
        new(2317, 1),
        new(2318, 5),
    };

    public override void Setup()
    {
        questSheet = Service.Data.GetExcelSheet<Quest>()!;
        recipeSheet = Service.Data.GetExcelSheet<Recipe>()!;
        craftTypeSheet = Service.Data.GetExcelSheet<CraftType>()!;
        itemSheet = Service.Data.GetExcelSheet<Item>()!;

        agentRecipeNote = GetAgent<AgentRecipeNote>(AgentId.RecipeNote);
        inventoryManager = InventoryManager.Instance();
        questManager = QuestManager.Instance();
        recipeNote = RecipeNote.Instance();
        playerState = PlayerState.Instance();
    }

    [SigHook("66 83 F9 1E 0F 83")]
    public nint UpdateQuestWork(ushort index, nint questData, bool a3, bool a4, bool a5)
    {
        var questId = *(ushort*)questData;
        Debug($"UpdateQuestWork({index}, {questId} / {*(byte*)(questData + 2)}, {a3}, {a4}, {a5})");

        // DailyQuestWork gets updated before QuestWork, so we can check here if it's a daily quest
        if (questId > 0)
        {
            var quest = questSheet.GetRow((uint)questId | 0x10000);
            if (quest?.RowId == 0)
            {
                Warning($"Ignoring quest #{questId}: Quest not found");
                goto originalUpdateQuestWork;
            }

            if (!(questManager->GetDailyQuestById(questId) != null || quest!.BeastTribe.Row != 0))
            {
                Warning($"Ignoring quest #{questId}: Quest is not a daily quest or tribal quest");
                goto originalUpdateQuestWork;
            }

            var sequence = *(byte*)(questData + 2);
            var todoOffset = quest!.ToDoCompleteSeq.IndexOf(sequence);
            if (todoOffset < 0 || todoOffset >= quest.ToDoQty.Length)
                goto originalUpdateQuestWork;

            var questEventHandler = EventFramework.Instance()->GetEventHandlerById(questId);
            var localPlayer = Control.Instance()->LocalPlayer;
            if (questEventHandler == null && localPlayer == null)
                goto originalUpdateQuestWork;

            var todoCount = quest.ToDoQty[todoOffset];
            for (var todoIndex = todoOffset; todoIndex < todoOffset + todoCount; todoIndex++)
            {
                uint numHave, numNeeded, itemId;
                GetTodoArgs(questEventHandler, localPlayer, todoIndex, &numHave, &numNeeded, &itemId);
                Debug($"TodoArgs #{todoIndex}: {numHave}/{numNeeded} of {itemId}");

                if (itemId == 0)
                {
                    foreach (var q in QuestTodos)
                    {
                        if (q.QuestId == questId && q.TodoIndex == todoIndex)
                        {
                            var scriptArgIndex = quest.ScriptInstruction
                                .IndexOf(entry => entry.RawString == q.ScriptArgName);

                            itemId = quest.ScriptArg.ElementAtOrDefault(scriptArgIndex);
                            Debug($"Using fallback script value {itemId}");
                            break;
                        }
                    }
                }

                if (numHave < numNeeded && itemId != 0)
                {
                    OpenRecipe(itemId % 1000000, numNeeded);
                    break;
                }
            }
        }

        originalUpdateQuestWork:
        return UpdateQuestWorkHook.Original(index, questData, a3, a4, a5);
    }

    [Signature("E8 ?? ?? ?? ?? 8B 44 24 78 89 44 24 44")]
    private readonly GetTodoArgsDelegate GetTodoArgs = null!;
    private delegate void GetTodoArgsDelegate(EventHandler* questEventHandler, BattleChara* localPlayer, int i, uint* numHave, uint* numNeeded, uint* itemId);

    private void OpenRecipe(uint resultItemId, uint amount)
    {
        if (agentRecipeNote->ActiveCraftRecipeId != 0 || Service.Condition[ConditionFlag.Crafting] || Service.Condition[ConditionFlag.Crafting40])
        {
            Warning($"Not opening Recipe for Item {resultItemId}: Crafting in progress");
            return;
        }

        var craftType = GetCurrentCraftType();
        var recipe = recipeSheet?.FirstOrDefault(row => row?.ItemResult.Row == resultItemId && row.CraftType.Row == craftType, null);
        recipe ??= recipeSheet?.FirstOrDefault(row => row?.ItemResult.Row == resultItemId, null);
        if (recipe == null)
        {
            Warning($"Not opening Recipe for Item {resultItemId}: Recipe not found");
            return;
        }

        if (!IngredientsAvailable(recipe, amount))
        {
            Warning($"Not opening Recipe for Item {resultItemId}: Required ingredients not available");
            return;
        }

        Debug($"Requirements met, opening recipe {recipe.RowId}");

        agentRecipeNote->AgentInterface.Hide();

        if (recipe.CraftType.Row == craftType)
            agentRecipeNote->OpenRecipeByRecipeIdInternal(recipe.RowId);
        else
            agentRecipeNote->OpenRecipeByItemId(resultItemId);
    }

    private bool IngredientsAvailable(Recipe recipe, uint amount)
    {
        foreach (var ingredient in recipe.UnkData5)
        {
            if (ingredient.ItemIngredient == 0 || ingredient.AmountIngredient == 0)
                continue;

            var itemId = (uint)ingredient.ItemIngredient;

            var item = itemSheet.GetRow(itemId);
            if (item == null || item.ItemUICategory.Row == 59) // ignore Crystals
                continue;

            var numHave = inventoryManager->GetInventoryItemCount(itemId, false, false, false); // Normal
            numHave += inventoryManager->GetInventoryItemCount(itemId, true, false, false); // HQ

            var numNeeded = ingredient.AmountIngredient * amount;

            Debug($"Checking Ingredient #{ingredient.ItemIngredient}: need {numNeeded}, have {numHave}");

            if (numHave < numNeeded)
                return false;
        }

        return true;
    }

    private int GetCurrentCraftType()
    {
        var craftType = -1;

        for (var i = 0; i < craftTypeSheet.RowCount; i++)
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
