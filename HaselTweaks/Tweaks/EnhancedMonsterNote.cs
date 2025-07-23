using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class EnhancedMonsterNote : ConfigurableTweak
{
    private readonly IClientState _clientState;
    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly AddonObserver _addonObserver;
    private readonly PluginConfig _pluginConfig;
    private readonly ConfigGui _configGui;
    private readonly ExcelService _excelService;

    private Hook<AgentMonsterNote.Delegates.Show>? _showHook;
    private Hook<AgentMonsterNote.Delegates.OpenWithData>? _openWithDataHook;

    private bool _isShowCall;
    private static readonly byte[] ClassIds = [1, 2, 3, 4, 5, 29, 6, 7, 26];

    public override void OnEnable()
    {
        _showHook = _gameInteropProvider.HookFromAddress<AgentMonsterNote.Delegates.Show>(
            AgentMonsterNote.Instance()->VirtualTable->Show,
            ShowDetour);

        _openWithDataHook = _gameInteropProvider.HookFromAddress<AgentMonsterNote.Delegates.OpenWithData>(
            AgentMonsterNote.MemberFunctionPointers.OpenWithData,
            OpenWithDataDetour);

        _showHook.Enable();
        _openWithDataHook.Enable();

        _clientState.Logout += OnLogout;
        _addonObserver.AddonOpen += OnAddonOpen;
    }

    public override void OnDisable()
    {
        _showHook?.Dispose();
        _showHook = null;

        _openWithDataHook?.Dispose();
        _openWithDataHook = null;

        _clientState.Logout -= OnLogout;
        _addonObserver.AddonOpen -= OnAddonOpen;
        _isShowCall = false;
    }

    private void OnLogout(int type, int code)
    {
        _isShowCall = false; // just for safety
    }

    private void OnAddonOpen(string addonName)
    {
        if (!Config.OpenWithIncompleteFilter || addonName != "MonsterNote")
            return;

        _logger.LogDebug("Changing filter to Incomplete.");
        var retVal = stackalloc AtkValue[1];
        var values = stackalloc AtkValue[2];
        values[0].SetInt(2); // Set Filter
        values[1].SetInt(2); // Filter = 2
        AgentMonsterNote.Instance()->ReceiveEvent(retVal, values, 2, 0);
    }

    private void ShowDetour(AgentMonsterNote* thisPtr)
    {
        if (!thisPtr->IsAgentActive())
            _isShowCall = true;

        _showHook!.Original(thisPtr);
    }

    private void OpenWithDataDetour(AgentMonsterNote* thisPtr, byte classIndex, byte rank, byte a4, byte a5)
    {
        if (_isShowCall) // is called with 0xFF, 0, 0, 0
        {
            if (Config.OpenWithCurrentClass && TryGetCurrentClassIndex(out var currentClassIndex))
            {
                _logger.LogDebug("Selecing tab for current class.");
                classIndex = currentClassIndex;
            }
            else if (Config.RememberTabSelection)
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
