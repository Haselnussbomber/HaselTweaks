using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.System.Scheduler;
using FFXIVClientStructs.FFXIV.Client.System.Scheduler.Base;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class ForcedCutsceneMusic : ConfigurableTweak<ForcedCutsceneMusicConfiguration>
{
    private static readonly string[] ConfigOptions = [
        "IsSndMaster",
        "IsSndBgm",
        "IsSndSe",
        "IsSndVoice",
        "IsSndEnv",
        "IsSndSystem",
        "IsSndPerform",
    ];

    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly IGameConfig _gameConfig;
    private readonly IFramework _framework;

    private readonly Dictionary<string, bool> _wasMuted = [];

    private Hook<ScheduleManagement.Delegates.CreateCutSceneController>? _createCutSceneControllerHook;
    private Hook<CutSceneController.Delegates.Dtor>? _cutSceneControllerDtorHook;
    private IDebouncer? _unmuteDebouncer;
    private IDebouncer? _restoreDebouncer;
    private bool _hasTask;

    public override void OnEnable()
    {
        _createCutSceneControllerHook = _gameInteropProvider.HookFromAddress<ScheduleManagement.Delegates.CreateCutSceneController>(
            ScheduleManagement.MemberFunctionPointers.CreateCutSceneController,
            CreateCutSceneControllerDetour);

        _cutSceneControllerDtorHook = _gameInteropProvider.HookFromAddress<CutSceneController.Delegates.Dtor>(
            (nint)CutSceneController.StaticVirtualTablePointer->Dtor,
            CutSceneControllerDtorDetour);

        _createCutSceneControllerHook.Enable();
        _cutSceneControllerDtorHook.Enable();

        _unmuteDebouncer = _framework.CreateDebouncer(TimeSpan.FromMilliseconds(100), Unmute);
        _restoreDebouncer = _framework.CreateDebouncer(TimeSpan.FromMilliseconds(100), Restore);

        _framework.Update += OnUpdate;
    }
    public override void OnDisable()
    {
        _framework.Update -= OnUpdate;

        _createCutSceneControllerHook?.Dispose();
        _createCutSceneControllerHook = null;

        _cutSceneControllerDtorHook?.Dispose();
        _cutSceneControllerDtorHook = null;

        _unmuteDebouncer?.Dispose();
        _unmuteDebouncer = null;

        _restoreDebouncer?.Dispose();
        _restoreDebouncer = null;

        _hasTask = false;
    }

    private void OnUpdate(IFramework framework)
    {
        var hasTask = EventFramework.Instance()->EventSceneModule.TaskManager.Tasks.Any(IsCutsceneTask);

        if (_hasTask == hasTask)
            return;

        _hasTask = hasTask;

        if (hasTask)
        {
            _logger.LogDebug("Cutscene Task started");

            _restoreDebouncer?.Cancel();
            _unmuteDebouncer?.Debounce();
        }
        else
        {
            _logger.LogDebug("Cutscene Task ended");

            _unmuteDebouncer?.Cancel();
            _restoreDebouncer?.Debounce();
        }
    }

    private static bool IsCutsceneTask(Pointer<EventSceneTaskInterface> task)
    {
        return !task.IsNull && task.Value->Type
            is EventSceneTaskType.PlayCutScene
            or EventSceneTaskType.PostCutScene
            or EventSceneTaskType.PlayStaffRoll
            or EventSceneTaskType.PlayToBeContinued;
    }

    private CutSceneController* CreateCutSceneControllerDetour(ScheduleManagement* self, byte* path, uint cutsceneId, byte a4)
    {
        var ret = _createCutSceneControllerHook!.Original(self, path, cutsceneId, a4);

        _logger.LogInformation("Cutscene {id} started (Controller @ {address:X})", cutsceneId, (nint)ret);

        if (cutsceneId != 0 && !_hasTask) // ignore title screen cutscene, skip if we have tasks running
        {
            _restoreDebouncer?.Cancel();
            _unmuteDebouncer?.Debounce();
        }

        return ret;
    }

    private SchedulerState* CutSceneControllerDtorDetour(CutSceneController* self, byte freeFlags)
    {
        var cutsceneId = self->CutsceneId;

        _logger.LogInformation("Cutscene {id} ended", cutsceneId);

        if (cutsceneId != 0 && !_hasTask) // ignore title screen cutscene, skip if we have tasks running
        {
            _unmuteDebouncer?.Cancel();
            _restoreDebouncer?.Debounce();
        }

        return _cutSceneControllerDtorHook!.Original(self, freeFlags);
    }

    private void Unmute()
    {
        foreach (var optionName in ConfigOptions)
        {
            var isMuted = _gameConfig.System.TryGet(optionName, out bool value) && value;

            _wasMuted[optionName] = isMuted;

            if (ShouldHandle(optionName) && isMuted)
            {
                _logger.LogInformation("Setting {optionName} to false", optionName);
                _gameConfig.System.Set(optionName, false);
            }
        }
    }

    private void Restore()
    {
        if (_config.Restore)
        {
            foreach (var optionName in ConfigOptions)
            {
                if (ShouldHandle(optionName) && _wasMuted.TryGetValue(optionName, out var value) && value)
                {
                    _logger.LogInformation("Restoring {optionName} to {value}", optionName, value);
                    _gameConfig.System.Set(optionName, value);
                }
            }
        }
    }

    private bool ShouldHandle(string optionName)
    {
        return optionName switch
        {
            "IsSndMaster" => _config.HandleMaster,
            "IsSndBgm" => _config.HandleBgm,
            "IsSndSe" => _config.HandleSe,
            "IsSndVoice" => _config.HandleVoice,
            "IsSndEnv" => _config.HandleEnv,
            "IsSndSystem" => _config.HandleSystem,
            "IsSndPerform" => _config.HandlePerform,
            _ => false
        };
    }
}
