using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class HideMSQComplete : Tweak
{
    private readonly IAddonLifecycle _addonLifecycle;
    private readonly IFramework _framework;
    private readonly ExcelService _excelService;
    private static ExcelService? ExcelService;

    private static byte? _playerExpansion;
    private static byte PlayerExpansion => _playerExpansion ??= PlayerState.Instance()->MaxExpansion;

    [AutoPostConstruct]
    private void Initialize()
    {
        ExcelService = _excelService;
    }

    public override ValueTask OnEnable()
    {
        _disposables = _addonLifecycle.OnPostRefresh(ScenarioTree_PostRefresh, "ScenarioTree");
        _playerExpansion = null;

        return new ValueTask(_framework.Run(Update));
    }

    public override ValueTask OnDisable()
    {
        DisposeAndNull(ref _disposables);

        if (Status is TweakStatus.Enabled)
        {
            return new ValueTask(_framework.Run(() => UpdateVisibility(true, true)));
        }

        return ValueTask.CompletedTask;
    }

    private void ScenarioTree_PostRefresh(AddonArgs args)
    {
        Update();
    }

    private static void Update()
    {
        var agent = AgentScenarioTree.Instance();
        if (agent->Data == null)
        {
            // Invalidate player expansion cache if AgentScenarioTree is not available
            _playerExpansion = null;
            return;
        }

        var data = agent->Data;
        var mainQuest = data->MainScenarioQuestIds[data->MSQPathIndex];
        var hasMainQuest = mainQuest != 0;
        if (hasMainQuest && 
            ExcelService?.TryGetRow<ScenarioTree>(mainQuest | 0x10000U, out var _) == true &&
            ExcelService.TryGetRow<Quest>(mainQuest | 0x10000U, out Quest quest))
        {
            hasMainQuest = quest.Expansion.RowId <= PlayerExpansion;
        }
        var hasJobQuest = data->JobQuestIds[data->JobQuestIndex] != 0;
        UpdateVisibility(hasMainQuest, hasJobQuest);
    }

    private static void UpdateVisibility(bool hasMainQuest, bool hasJobQuest)
    {
        if (!TryGetAddon<AtkUnitBase>(AgentId.ScenarioTree, out var addon))
            return;

        addon->GetNodeById(11)->ToggleVisibility(hasMainQuest); // AtkTextNode (title)
        addon->GetNodeById(12)->ToggleVisibility(hasMainQuest); // AtkNineGridNode (title background)
        addon->GetNodeById(13)->ToggleVisibility(hasMainQuest); // AtkComponentButton (quest button)

        var lowerBox = addon->GetNodeById(6);
        var isInputHintShown = !hasMainQuest && lowerBox != null && lowerBox->Timeline != null && lowerBox->Timeline->ActiveLabelId == 102;
        addon->GetNodeById(9)->ToggleVisibility(!isInputHintShown); // AtkResNode (hide input hint)

        if (!hasMainQuest && !hasJobQuest)
            addon->Flags1A0 |= 0b1000_0000; // disable focusability
        else
            addon->Flags1A0 &= unchecked((byte)~0b1000_0000); // enable focusability
    }
}
