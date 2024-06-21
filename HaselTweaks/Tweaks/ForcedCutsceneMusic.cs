using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.Scheduler;
using FFXIVClientStructs.FFXIV.Client.System.Scheduler.Base;
using HaselCommon.Extensions;
using HaselCommon.Services;
using HaselTweaks.Config;
using Microsoft.Extensions.Logging;

namespace HaselTweaks.Tweaks;

public sealed class ForcedCutsceneMusicConfiguration
{
    [BoolConfig]
    public bool Restore = true;
}

public sealed unsafe class ForcedCutsceneMusic(
    PluginConfig pluginConfig,
    TextService textService,
    ILogger<ForcedCutsceneMusic> Logger,
    IGameInteropProvider GameInteropProvider,
    IGameConfig GameConfig)
    : Tweak<ForcedCutsceneMusicConfiguration>(pluginConfig, textService)
{
    private bool _wasBgmMuted;

    private delegate void CutSceneControllerDtorDelegate(CutSceneController* self, bool free);

    private Hook<ScheduleManagement.Delegates.CreateCutSceneController>? CreateCutSceneControllerHook;
    private Hook<CutSceneControllerDtorDelegate>? CutSceneControllerDtorHook;

    public override void OnInitialize()
    {
        CreateCutSceneControllerHook = GameInteropProvider.HookFromAddress<ScheduleManagement.Delegates.CreateCutSceneController>(
            ScheduleManagement.MemberFunctionPointers.CreateCutSceneController,
            CreateCutSceneControllerDetour);

        CutSceneControllerDtorHook = GameInteropProvider.HookFromVTable<CutSceneControllerDtorDelegate>(
            CutSceneController.StaticVirtualTablePointer, 0,
            CutSceneControllerDtorDetour);
    }

    public override void OnEnable()
    {
        CreateCutSceneControllerHook?.Enable();
        CutSceneControllerDtorHook?.Enable();
    }

    public override void OnDisable()
    {
        CreateCutSceneControllerHook?.Disable();
        CutSceneControllerDtorHook?.Disable();
    }

    private bool IsBgmMuted
    {
        get => GameConfig.System.TryGet("IsSndBgm", out bool value) && value;
        set => GameConfig.System.Set("IsSndBgm", value);
    }

    private CutSceneController* CreateCutSceneControllerDetour(ScheduleManagement* self, byte* path, uint id, byte a4)
    {
        var ret = CreateCutSceneControllerHook!.Original(self, path, id, a4);

        Logger.LogInformation("Cutscene {id} started (Controller @ {address:X})", id, (nint)ret);

        var isBgmMuted = IsBgmMuted;

        _wasBgmMuted = isBgmMuted;

        if (isBgmMuted)
            IsBgmMuted = false;

        return ret;
    }

    private void CutSceneControllerDtorDetour(CutSceneController* self, bool free)
    {
        Logger.LogInformation("Cutscene {id} ended", self->CutsceneId);

        CutSceneControllerDtorHook!.Original(self, free);

        if (_wasBgmMuted && Config.Restore)
            IsBgmMuted = true;
    }
}
