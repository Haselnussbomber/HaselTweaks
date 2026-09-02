using Dalamud.Game.Agent.AgentArgTypes;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class EnhancedMonsterNote : ConfigurableTweak<EnhancedMonsterNoteConfiguration>
{
    private static readonly byte[] ClassIds = [1, 2, 3, 4, 5, 29, 6, 7, 26];

    private readonly IClientState _clientState;
    private readonly IAddonLifecycle _addonLifecycle;
    private readonly IAgentLifecycle _agentLifecycle;
    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly ExcelService _excelService;

    private Hook<AgentMonsterNote.Delegates.OpenWithData> _openWithDataHook;
    private bool _isShowCall;

    public override void OnEnable()
    {
        _disposables = DisposableBag.Create(
            _openWithDataHook = _gameInteropProvider.EnabledHookFromAddress<AgentMonsterNote.Delegates.OpenWithData>(
                AgentMonsterNote.MemberFunctionPointers.OpenWithData,
                OpenWithDataDetour),

            _agentLifecycle.OnPreShow(OnMonsterNotePreShow, AgentId.MonsterNote),
            _addonLifecycle.OnPostShow(OnMonsterNotePostShow, "MonsterNote"),
            _clientState.OnLogout(OnLogout));
    }

    public override void OnDisable()
    {
        DisposeAndNull(ref _disposables);
        _isShowCall = false;
    }

    private void OnLogout(int type, int code)
    {
        _isShowCall = false; // just for safety
    }

    private void OnMonsterNotePostShow(AddonShowArgs args)
    {
        if (!_config.OpenWithIncompleteFilter)
            return;

        _logger.LogDebug("Changing filter to Incomplete.");

        var retVal = stackalloc AtkValue[1];

        Span<AtkValue> values = stackalloc AtkValue[2];
        values.Clear();

        values[0].SetInt(2); // Set Filter
        values[1].SetInt(2); // Filter = 2

        AgentMonsterNote.Instance()->ReceiveEvent(retVal, values.GetPointer(0), 2, 0);
    }

    private void OnMonsterNotePreShow(AgentArgs args)
    {
        var agent = args.GetAgent<AgentMonsterNote>();
        if (!agent->IsAgentActive())
            _isShowCall = true;
    }

    private void OpenWithDataDetour(AgentMonsterNote* thisPtr, byte classIndex, byte rank, byte a4, byte a5)
    {
        if (_isShowCall) // is called with 0xFF, 0, 0, 0
        {
            if (_config.OpenWithCurrentClass && TryGetCurrentClassIndex(out var currentClassIndex))
            {
                _logger.LogDebug("Selecing tab for current class.");
                classIndex = currentClassIndex;
            }
            else if (_config.RememberTabSelection)
            {
                _logger.LogDebug("Re-using last class tab and rank.");
                classIndex = thisPtr->ClassIndex;
                rank = thisPtr->Rank;
            }

            _isShowCall = false;
        }

        _openWithDataHook!.Original(thisPtr, classIndex, rank, a4, a5);
    }

    private bool TryGetCurrentClassIndex(out byte classIndex)
    {
        var classJobId = PlayerState.Instance()->CurrentClassJobId;

        // short path
        var idIndex = ClassIds.IndexOf(classJobId);
        if (idIndex != -1)
        {
            classIndex = (byte)idIndex;
            return true;
        }

        // long path
        if (!_excelService.TryGetRow<ClassJob>(classJobId, out var classJobRow))
        {
            classIndex = byte.MaxValue;
            return false;
        }

        // resolve parent class
        if (classJobRow.ClassJobParent.RowId != 0)
            classJobId = (byte)classJobRow.ClassJobParent.RowId;

        // try again
        idIndex = ClassIds.IndexOf(classJobId);
        if (idIndex != -1)
        {
            classIndex = (byte)idIndex;
            return true;
        }

        classIndex = byte.MaxValue;
        return false;
    }
}
