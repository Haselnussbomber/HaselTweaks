using HaselTweaks.Structs;

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

    [AddressHook<PlayCutSceneTask>(nameof(PlayCutSceneTask.Addresses.Ctor))]
    public PlayCutSceneTask* PlayCutSceneTaskCtor(PlayCutSceneTask* self, uint cutsceneId, byte a3, int a4, int a5, int a6, int a7)
    {
        var ret = PlayCutSceneTaskCtorHook.OriginalDisposeSafe(self, cutsceneId, a3, a4, a5, a6, a7);

        Log($"Cutscene {cutsceneId} started");

        var isBgmMuted = IsBgmMuted;

        _wasBgmMuted = isBgmMuted;

        if (isBgmMuted)
            IsBgmMuted = false;

        return ret;
    }

    [VTableHook<PlayCutSceneTask>(0)]
    public PlayCutSceneTask* PlayCutSceneTaskDtor(PlayCutSceneTask* self, bool a2)
    {
        Log($"Cutscene {self->Id} ended");

        var ret = PlayCutSceneTaskDtorHook.OriginalDisposeSafe(self, a2);

        if (_wasBgmMuted && Config.Restore)
            IsBgmMuted = true;

        return ret;
    }
}
