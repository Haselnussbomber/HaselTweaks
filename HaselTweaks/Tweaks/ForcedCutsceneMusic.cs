using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.Scheduler;
using FFXIVClientStructs.FFXIV.Client.System.Scheduler.Base;
using HaselCommon.Extensions;
using HaselTweaks.Config;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;
using Microsoft.Extensions.Logging;

namespace HaselTweaks.Tweaks;

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

    private bool _wasBgmMuted;

    private delegate void CutSceneControllerDtorDelegate(CutSceneController* self, bool free);

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
        if (Status == TweakStatus.Disposed)
            return;

        OnDisable();
        CreateCutSceneControllerHook?.Dispose();
        CutSceneControllerDtorHook?.Dispose();

        Status = TweakStatus.Disposed;
        GC.SuppressFinalize(this);
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
