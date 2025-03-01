using System.Collections.Generic;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.Scheduler;
using FFXIVClientStructs.FFXIV.Client.System.Scheduler.Base;
using HaselCommon.Extensions.Dalamud;
using HaselTweaks.Config;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;
using Microsoft.Extensions.Logging;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class ForcedCutsceneMusic : IConfigurableTweak
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
    private readonly ILogger<ForcedCutsceneMusic> _logger;
    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly IGameConfig _gameConfig;

    private Hook<ScheduleManagement.Delegates.CreateCutSceneController>? _createCutSceneControllerHook;
    private Hook<CutSceneControllerDtorDelegate>? _cutSceneControllerDtorHook;

    private readonly Dictionary<string, bool> _wasMuted = [];

    private delegate void CutSceneControllerDtorDelegate(CutSceneController* self, byte freeFlags);

    public string InternalName => nameof(ForcedCutsceneMusic);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    public void OnInitialize()
    {
        _createCutSceneControllerHook = _gameInteropProvider.HookFromAddress<ScheduleManagement.Delegates.CreateCutSceneController>(
            ScheduleManagement.MemberFunctionPointers.CreateCutSceneController,
            CreateCutSceneControllerDetour);

        _cutSceneControllerDtorHook = _gameInteropProvider.HookFromVTable<CutSceneControllerDtorDelegate>(
            CutSceneController.StaticVirtualTablePointer, 0,
            CutSceneControllerDtorDetour);
    }

    public void OnEnable()
    {
        _createCutSceneControllerHook?.Enable();
        _cutSceneControllerDtorHook?.Enable();
    }

    public void OnDisable()
    {
        _createCutSceneControllerHook?.Disable();
        _cutSceneControllerDtorHook?.Disable();
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();
        _createCutSceneControllerHook?.Dispose();
        _cutSceneControllerDtorHook?.Dispose();

        Status = TweakStatus.Disposed;
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

    private void CutSceneControllerDtorDetour(CutSceneController* self, byte freeFlags)
    {
        _logger.LogInformation("Cutscene {id} ended", self->CutsceneId);

        _cutSceneControllerDtorHook!.Original(self, freeFlags);

        if (!Config.Restore)
            return;

        if (self->CutsceneId == 0) // ignore title screen cutscene
            return;

        foreach (var optionName in ConfigOptions)
        {
            if (ShouldHandle(optionName) && _wasMuted.TryGetValue(optionName, out var value) && value)
            {
                _logger.LogInformation("Restoring {optionName} to {value}", optionName, value);
                _gameConfig.System.Set(optionName, value);
            }
        }
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
