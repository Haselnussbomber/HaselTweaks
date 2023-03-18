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
    public override string Name => "Auto-open Recipe (Experimental)";
    public override string Description => "When a quest step requires an item which is craftable and you have materials in your inventory, it will automatically open the recipe.";

    private ExcelSheet<Quest> questSheet = null!;
    private ExcelSheet<Recipe> recipeSheet = null!;
    private ExcelSheet<CraftType> craftTypeSheet = null!;
    private ExcelSheet<Item> itemSheet = null!;

    private AgentRecipeNote* agentRecipeNote;
    private InventoryManager* inventoryManager;
    private QuestManager* questManager;
    private RecipeNote* recipeNote;
    private PlayerState* playerState;

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

        if (questId > 0 /*&& questManager->GetDailyQuestById(questId) != null*/) // TODO: make this an option? maybe an exclude list?
        {
            var quest = questSheet.GetRow((uint)questId | 0x10000);
            if (quest?.RowId == 0)
            {
                Error($"Could not get Quest {questId}");
                goto originalUpdateQuestWork;
            }

            var sequence = *(byte*)(questData + 2);
            var todoIndex = quest!.ToDoCompleteSeq.IndexOf(sequence);
            var todoCount = quest.ToDoQty[todoIndex];

            var questEventHandler = EventFramework.Instance()->GetEventHandlerById(questId);
            var localPlayer = Control.Instance()->LocalPlayer;
            if (questEventHandler != null && localPlayer != null)
            {
                for (var i = todoIndex; i < todoIndex + todoCount; i++) // TODO: not sure if this is correct
                {
                    uint numHave;
                    uint numNeeded;
                    uint itemId;
                    GetTodoArgs(questEventHandler, localPlayer, i, &numHave, &numNeeded, &itemId);
                    Debug($"TodoArgs #{i}: {numHave}/{numNeeded} of {itemId}");

                    if (numHave < numNeeded && itemId != 0)
                    {
                        OpenRecipe(itemId % 1000000);
                        break;
                    }
                }
            }
        }

        originalUpdateQuestWork:
        return UpdateQuestWorkHook.Original(index, questData, a3, a4, a5);
    }

    [Signature("E8 ?? ?? ?? ?? 8B 44 24 78 89 44 24 44")]
    private readonly GetTodoArgsDelegate GetTodoArgs = null!;
    private delegate void GetTodoArgsDelegate(EventHandler* questEventHandler, BattleChara* localPlayer, int i, uint* numHave, uint* numNeeded, uint* itemId);

    private void OpenRecipe(uint resultItemId)
    {
        if (agentRecipeNote->ActiveCraftRecipeId != 0 || Service.Condition[ConditionFlag.Crafting] || Service.Condition[ConditionFlag.Crafting40])
        {
            Warning($"Not opening Recipe for Item {resultItemId}: Crafting in progress");
            return;
        }

        var craftType = GetCurrentCraftType();
        var recipe = recipeSheet?.FirstOrDefault(row => row?.ItemResult.Row == resultItemId && (craftType == -1 || row.CraftType.Row == craftType), null);
        if (recipe == null)
        {
            Warning($"Not opening Recipe for Item {resultItemId}: Recipe not found");
            return;
        }

        if (!IngredientsAvailable(recipe))
        {
            Warning($"Not opening Recipe for Item {resultItemId}: Required ingredients not available");
            return;
        }

        Debug($"Requirements met, opening recipe {recipe.RowId}");

        // TODO: check if recipe is already open?
        agentRecipeNote->AgentInterface.Hide();

        // TODO: make config option which one to use?
        // agentRecipeNote->OpenRecipeByItemId(resultItemId);
        agentRecipeNote->OpenRecipeByRecipeIdInternal(recipe.RowId);
    }

    private bool IngredientsAvailable(Recipe recipe)
    {
        foreach (var ingredient in recipe.UnkData5)
        {
            if (ingredient.ItemIngredient == 0 || ingredient.AmountIngredient == 0)
                continue;

            var itemId = (uint)ingredient.ItemIngredient;

            var item = itemSheet.GetRow(itemId);
            if (item == null || item.ItemUICategory.Row == 59) // ignore Crystals
                continue;

            var count = inventoryManager->GetInventoryItemCount(itemId, false, false, false);
            Debug($"Checking Ingredient #{ingredient.ItemIngredient}: need {ingredient.AmountIngredient}, have {count}");
            if (count < ingredient.AmountIngredient)
                return false;
        }

        return true;
    }

    private int GetCurrentCraftType()
    {
        var craftType = 0;

        for (; craftType < craftTypeSheet.RowCount; craftType++)
        {
            if (recipeNote->Jobs[craftType] == playerState->CurrentClassJobId)
            {
                break;
            }
        }

        return craftType;
    }
}
