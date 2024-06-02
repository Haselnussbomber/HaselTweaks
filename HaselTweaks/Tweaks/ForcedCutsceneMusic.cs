using FFXIVClientStructs.FFXIV.Client.System.Scheduler;
using FFXIVClientStructs.FFXIV.Client.System.Scheduler.Base;
using HaselCommon.Utils;

namespace HaselTweaks.Tweaks;

public class ForcedCutsceneMusicConfiguration
{
    [BoolConfig]
    public bool Restore = true;
}

[Tweak]
public unsafe partial class ForcedCutsceneMusic : Tweak<ForcedCutsceneMusicConfiguration>
{
    private bool _wasBgmMuted;

    private delegate void CutSceneControllerDtorDelegate(CutSceneController* self, bool free);

    private AddressHook<ScheduleManagement.Delegates.CreateCutSceneController>? CreateCutSceneControllerHook;
    private VFuncHook<CutSceneControllerDtorDelegate>? CutSceneControllerDtorHook;

    public override void SetupHooks()
    {
        CreateCutSceneControllerHook = new(ScheduleManagement.MemberFunctionPointers.CreateCutSceneController, CreateCutSceneControllerDetour);
        CutSceneControllerDtorHook = new(CutSceneController.StaticVirtualTablePointer, 0, CutSceneControllerDtorDetour);
    }

    private static bool IsBgmMuted
    {
        get => Service.GameConfig.System.TryGet("IsSndBgm", out bool value) && value;
        set => Service.GameConfig.System.Set("IsSndBgm", value);
    }

    public CutSceneController* CreateCutSceneControllerDetour(ScheduleManagement* self, byte* path, uint id, byte a4)
    {
        var ret = CreateCutSceneControllerHook!.OriginalDisposeSafe(self, path, id, a4);

        Log($"Cutscene {id} started (Controller @ {(nint)ret:X})");

        var isBgmMuted = IsBgmMuted;

        _wasBgmMuted = isBgmMuted;

        if (isBgmMuted)
            IsBgmMuted = false;

        return ret;
    }

    public void CutSceneControllerDtorDetour(CutSceneController* self, bool free)
    {
        Log($"Cutscene {self->CutsceneId} ended");

        CutSceneControllerDtorHook!.OriginalDisposeSafe(self, free);

        if (_wasBgmMuted && Config.Restore)
            IsBgmMuted = true;
    }
}
