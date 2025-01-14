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

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append)]
public unsafe partial class ForcedCutsceneMusic(
    PluginConfig PluginConfig,
    ConfigGui ConfigGui,
    ILogger<ForcedCutsceneMusic> Logger,
    IGameInteropProvider GameInteropProvider,
    IGameConfig GameConfig)
    : IConfigurableTweak
{
    public string InternalName => nameof(ForcedCutsceneMusic);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    private readonly string[] ConfigOptions = [
        "IsSndMaster",
        "IsSndBgm",
        "IsSndSe",
        "IsSndVoice",
        "IsSndEnv",
        "IsSndSystem",
        "IsSndPerform",
    ];

    private readonly Dictionary<string, bool> WasMuted = [];

    private delegate void CutSceneControllerDtorDelegate(CutSceneController* self, byte freeFlags);

    private Hook<ScheduleManagement.Delegates.CreateCutSceneController>? CreateCutSceneControllerHook;
    private Hook<CutSceneControllerDtorDelegate>? CutSceneControllerDtorHook;

    public void OnInitialize()
    {
        CreateCutSceneControllerHook = GameInteropProvider.HookFromAddress<ScheduleManagement.Delegates.CreateCutSceneController>(
            ScheduleManagement.MemberFunctionPointers.CreateCutSceneController,
            CreateCutSceneControllerDetour);

        CutSceneControllerDtorHook = GameInteropProvider.HookFromVTable<CutSceneControllerDtorDelegate>(
            CutSceneController.StaticVirtualTablePointer, 0,
            CutSceneControllerDtorDetour);
    }

    public void OnEnable()
    {
        CreateCutSceneControllerHook?.Enable();
        CutSceneControllerDtorHook?.Enable();
    }

    public void OnDisable()
    {
        CreateCutSceneControllerHook?.Disable();
        CutSceneControllerDtorHook?.Disable();
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();
        CreateCutSceneControllerHook?.Dispose();
        CutSceneControllerDtorHook?.Dispose();

        Status = TweakStatus.Disposed;
        GC.SuppressFinalize(this);
    }

    private CutSceneController* CreateCutSceneControllerDetour(ScheduleManagement* self, byte* path, uint id, byte a4)
    {
        var ret = CreateCutSceneControllerHook!.Original(self, path, id, a4);

        Logger.LogInformation("Cutscene {id} started (Controller @ {address:X})", id, (nint)ret);

        if (id == 0) // ignore title screen cutscene
            return ret;

        foreach (var optionName in ConfigOptions)
        {
            var isMuted = GameConfig.System.TryGet(optionName, out bool value) && value;

            WasMuted[optionName] = isMuted;

            if (ShouldHandle(optionName) && isMuted)
            {
                Logger.LogInformation("Setting {optionName} to false", optionName);
                GameConfig.System.Set(optionName, false);
            }
        }

        return ret;
    }

    private void CutSceneControllerDtorDetour(CutSceneController* self, byte freeFlags)
    {
        Logger.LogInformation("Cutscene {id} ended", self->CutsceneId);

        CutSceneControllerDtorHook!.Original(self, freeFlags);

        if (!Config.Restore)
            return;

        if (self->CutsceneId == 0) // ignore title screen cutscene
            return;

        foreach (var optionName in ConfigOptions)
        {
            if (ShouldHandle(optionName) && WasMuted.TryGetValue(optionName, out var value) && value)
            {
                Logger.LogInformation("Restoring {optionName} to {value}", optionName, value);
                GameConfig.System.Set(optionName, value);
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
