using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using FFXIVClientStructs.FFXIV.Client.UI.Arrays;
using FFXIVClientStructs.FFXIV.Component.GUI;
using TerritoryIntendedUse = FFXIVClientStructs.FFXIV.Client.Enums.TerritoryIntendedUse;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class CosmicResearchTodo : ConfigurableTweak<CosmicResearchTodoConfiguration>
{
    private readonly ExcelService _excelService;
    private readonly LanguageProvider _languageProvider;
    private readonly TextService _textService;
    private readonly IAddonLifecycle _addonLifecycle;
    private readonly IClientState _clientState;
    private readonly IFramework _framework;
    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly ISeStringEvaluator _seStringEvaluator;

    private Hook<WKSModuleBase.Delegates.SetIntData>? _setIntDataHook;

    public override void OnEnable()
    {
        _addonLifecycle.RegisterListener(AddonEvent.PreRequestedUpdate, "_ToDoList", OnPreRequestedUpdate);

        _setIntDataHook = _gameInteropProvider.HookFromSignature<WKSModuleBase.Delegates.SetIntData>("40 53 48 83 EC ?? 48 8B D9 81 EA", SetIntDataDetour);
        _setIntDataHook.Enable();

        _clientState.ClassJobChanged += OnClassJobChanged;
        _languageProvider.LanguageChanged += OnLanguageChanged;

        _framework.RunOnTick(RequestUpdate, delayTicks: 1);
    }

    public override void OnDisable()
    {
        _languageProvider.LanguageChanged -= OnLanguageChanged;
        _clientState.ClassJobChanged -= OnClassJobChanged;

        _setIntDataHook?.Dispose();
        _setIntDataHook = null;

        _addonLifecycle.UnregisterListener(AddonEvent.PreRequestedUpdate, "_ToDoList", OnPreRequestedUpdate);

        RequestUpdate();
    }

    private void OnClassJobChanged(uint classJobId)
    {
        _logger.LogTrace("ClassJobChanged -> RequestUpdate");
        RequestUpdate();
    }

    private void OnLanguageChanged(string obj)
    {
        _logger.LogTrace("LanguageChanged -> RequestUpdate");
        RequestUpdate();
    }

    private bool SetIntDataDetour(WKSModuleBase* thisPtr, int a2, int a3, int a4, int a5, int a6, int a7)
    {
        var retVal = _setIntDataHook!.Original(thisPtr, a2, a3, a4, a5, a6, a7);
        _logger.LogTrace("SetIntDataDetour -> RequestUpdate");
        RequestUpdate();
        return retVal;
    }

    private void RequestUpdate()
    {
        UIState.Instance()->MassivePcContentTodo.FullUpdatePending = true;
    }

    private void OnPreRequestedUpdate(AddonEvent type, AddonArgs args)
    {
        if (GameMain.Instance()->CurrentTerritoryIntendedUseId != TerritoryIntendedUse.CosmicExploration)
            return;

        var director = EventFramework.Instance()->GetMassivePcContentDirector();
        if (director == null)
            return;

        // check if the director already has something going on
        var todos = director->GetDirectorTodos();
        if (todos != null && todos->Any(o => o.Enabled))
            return;

        for (var i = 0; i < 2; i++)
        {
            var mtodos = director->GetMassivePcContentTodos(i);
            if (mtodos != null && mtodos->Any(o => o.Enabled))
                return;
        }

        var wksManager = WKSManager.Instance();
        if (wksManager == null)
            return;

        var researchModule = wksManager->ResearchModule;
        if (researchModule == null || !researchModule->IsLoaded)
            return;

        var numberArray = ToDoListNumberArray.Instance();
        if (numberArray == null)
            return;

        var stringArray = AtkStage.Instance()->GetStringArrayData(StringArrayType.ToDoList);
        if (stringArray == null)
            return;

        if (!_excelService.TryGetRow<ClassJob>(PlayerState.Instance()->CurrentClassJobId, out var classJob))
            return;

        if (!(classJob.IsCrafter || classJob.IsGatherer))
            return;

        var toolClassId = (byte)(classJob.RowId - 7);

        if (!_excelService.TryGetRow<WKSCosmoToolClass>(toolClassId, out var toolClassRow))
            return;

        _logger.LogDebug("Updating ToDo with Cosmic Research values...");

        var stage = researchModule->CurrentStages[toolClassId - 1];
        var nextStage = researchModule->UnlockedStages[toolClassId - 1];
        var maxStage = _excelService.GetSheet<WKSCosmoToolPassiveBuff>().Max(row => row.Unknown0);

        using var rssb = new RentedSeStringBuilder();

        if (stage == maxStage)
        {
            if (_config.ShowCosmicToolScore)
            {
                numberArray->DutyObjectiveCount = 0;
                numberArray->DutyCompletedObjectives = 0;

                var score = wksManager->Scores[toolClassId - 1];
                var max = score switch
                {
                    >= 150000 => 500000,
                    >= 50000 => 150000,
                    _ => 50000,
                };

                AddLine(_seStringEvaluator.EvaluateFromAddon(698, [classJob.RowId]), score, max);
            }
            return;
        }

        numberArray->DutyObjectiveCount = 0;
        numberArray->DutyCompletedObjectives = 0;

        for (byte toolType = 1; toolType <= toolClassRow.Types.Count; toolType++)
        {
            if (!researchModule->IsTypeAvailable(toolClassId, toolType))
                break;

            var neededXP = researchModule->GetNeededAnalysis(toolClassId, toolType);
            if (neededXP == 0)
                continue;

            var currentXP = researchModule->GetCurrentAnalysis(toolClassId, toolType);

            if (!_config.ShowCompletedAnalysis && currentXP >= neededXP)
                continue;

            if (!_excelService.TryGetRow<WKSCosmoToolName>(toolClassRow.Types[toolType - 1].Name.RowId, out var toolNameRow))
                continue;

            AddLine(toolNameRow.Name, currentXP, neededXP);
        }

        void AddLine(ReadOnlySeString text, int currentValue = 0, int neededValue = 0)
        {
            var index = numberArray->DutyObjectiveCount;

            rssb.Builder.Clear();
            rssb.Builder.Append(text);

            if (neededValue > 0)
            {
                rssb.Builder
                    .Append("  ")
                    .Append(_seStringEvaluator.EvaluateFromAddon(19, [currentValue, neededValue]));

                var percentage = currentValue >= neededValue
                    ? 100
                    : (int)MathF.Floor(currentValue / (float)neededValue * 100f);

                numberArray->DutyObjectiveTypes[index] = ToDoListNumberArray.ObjectiveType.Bar;
                numberArray->DutyObjectiveValue[index] = percentage;

                if (percentage == 100)
                    numberArray->DutyCompletedObjectives |= 1u << index;
            }
            else
            {
                numberArray->DutyObjectiveTypes[index] = ToDoListNumberArray.ObjectiveType.Number;
                numberArray->DutyObjectiveValue[index] = 0;
            }

            rssb.Builder.Append(" ");

            stringArray->SetValue(
                165 + index,
                rssb.Builder.GetViewAsSpan(),
                suppressUpdates: true);

            numberArray->DutyObjectiveCount++;
        }
    }
}
