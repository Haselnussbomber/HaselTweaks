using FFXIVClientStructs.FFXIV.Client.System.Scheduler;
using FFXIVClientStructs.FFXIV.Client.System.Scheduler.Base;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class ForcedCutsceneMusic : ConfigurableTweak
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

    private readonly PluginConfig _pluginConfig;
    private readonly ConfigGui _configGui;
    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly IGameConfig _gameConfig;

    private Hook<ScheduleManagement.Delegates.CreateCutSceneController>? _createCutSceneControllerHook;
    private Hook<CutSceneControllerDtorDelegate>? _cutSceneControllerDtorHook;

    private readonly Dictionary<string, bool> _wasMuted = [];

    private delegate CutSceneController* CutSceneControllerDtorDelegate(CutSceneController* self, byte freeFlags);

    public override void OnEnable()
    {
        _createCutSceneControllerHook = _gameInteropProvider.HookFromAddress<ScheduleManagement.Delegates.CreateCutSceneController>(
            ScheduleManagement.MemberFunctionPointers.CreateCutSceneController,
            CreateCutSceneControllerDetour);

        _cutSceneControllerDtorHook = _gameInteropProvider.HookFromVTable<CutSceneControllerDtorDelegate>(
            CutSceneController.StaticVirtualTablePointer, 0,
            CutSceneControllerDtorDetour);

        _createCutSceneControllerHook.Enable();
        _cutSceneControllerDtorHook.Enable();
    }

    public override void OnDisable()
    {
        _createCutSceneControllerHook?.Dispose();
        _createCutSceneControllerHook = null;

        _cutSceneControllerDtorHook?.Dispose();
        _cutSceneControllerDtorHook = null;
    }

    private CutSceneController* CreateCutSceneControllerDetour(ScheduleManagement* self, byte* path, uint id, byte a4)
    {
        var ret = _createCutSceneControllerHook!.Original(self, path, id, a4);

        _logger.LogInformation("Cutscene {id} started (Controller @ {address:X})", id, (nint)ret);

        if (id == 0) // ignore title screen cutscene
            return ret;

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

        return ret;
    }

    private CutSceneController* CutSceneControllerDtorDetour(CutSceneController* self, byte freeFlags)
    {
        var cutsceneId = self->CutsceneId;

        _logger.LogInformation("Cutscene {id} ended", cutsceneId);

        if (Config.Restore && cutsceneId != 0) // ignore title screen cutscene
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

        return _cutSceneControllerDtorHook!.Original(self, freeFlags);
    }

    private bool ShouldHandle(string optionName)
    {
        return optionName switch
        {
            "IsSndMaster" => Config.HandleMaster,
            "IsSndBgm" => Config.HandleBgm,
            "IsSndSe" => Config.HandleSe,
            "IsSndVoice" => Config.HandleVoice,
            "IsSndEnv" => Config.HandleEnv,
            "IsSndSystem" => Config.HandleSystem,
            "IsSndPerform" => Config.HandlePerform,
            _ => false
        };
    }
}
