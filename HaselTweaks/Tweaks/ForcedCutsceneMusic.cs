using FFXIVClientStructs.FFXIV.Client.System.Scheduler;
using FFXIVClientStructs.FFXIV.Client.System.Scheduler.Base;

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

    private static bool IsBgmMuted
    {
        get => Service.GameConfig.System.TryGet("IsSndBgm", out bool value) && value;
        set => Service.GameConfig.System.Set("IsSndBgm", value);
    }

    [AddressHook<ScheduleManagement>(nameof(ScheduleManagement.CreateCutSceneController))]
    public CutSceneController* CreateCutSceneController(ScheduleManagement* self, byte* path, uint id, byte a4)
    {
        var ret = CreateCutSceneControllerHook.OriginalDisposeSafe(self, path, id, a4);

        Log($"Cutscene {id} started (Controller @ {(nint)ret:X})");

        var isBgmMuted = IsBgmMuted;

        _wasBgmMuted = isBgmMuted;

        if (isBgmMuted)
            IsBgmMuted = false;

        return ret;
    }

    [VTableHook<CutSceneController>(0)]
    public CutSceneController* CutSceneControllerDtor(CutSceneController* self, bool free)
    {
        Log($"Cutscene {self->CutsceneId} ended");

        var ret = CutSceneControllerDtorHook.OriginalDisposeSafe(self, free);

        if (_wasBgmMuted && Config.Restore)
            IsBgmMuted = true;

        return ret;
    }
}
