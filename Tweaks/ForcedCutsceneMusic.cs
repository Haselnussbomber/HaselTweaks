using HaselTweaks.Structs;

namespace HaselTweaks.Tweaks;

public unsafe partial class ForcedCutsceneMusic : Tweak
{
    public override string Name => "Forced Cutscene Music";
    public override string Description => "Auto-unmutes background music for most cutscenes.";
    public static Configuration Config => Plugin.Config.Tweaks.ForcedCutsceneMusic;

    public class Configuration
    {
        [ConfigField(Label = "Restore mute state after cutscene")]
        public bool Restore = true;
    }

    private bool _wasBgmMuted;

    private static bool IsBgmMuted
    {
        get => Service.GameConfig.System.TryGet("IsSndBgm", out bool value) && value;
        set => Service.GameConfig.System.Set("IsSndBgm", value);
    }

    [AddressHook<LuaCutsceneState>(nameof(LuaCutsceneState.Addresses.Ctor))]
    public LuaCutsceneState* CutsceneStateCtor(LuaCutsceneState* self, uint cutsceneId, byte a3, int a4, int a5, int a6, int a7)
    {
        var ret = CutsceneStateCtorHook.OriginalDisposeSafe(self, cutsceneId, a3, a4, a5, a6, a7);

        Log($"Cutscene {cutsceneId} started");

        var isBgmMuted = IsBgmMuted;

        _wasBgmMuted = isBgmMuted;

        if (isBgmMuted)
            IsBgmMuted = false;

        return ret;
    }

    [VTableHook<LuaCutsceneState>(0)]
    public LuaCutsceneState* CutsceneStateDtor(LuaCutsceneState* self, bool a2)
    {
        Log($"Cutscene {self->Id} ended");

        var ret = CutsceneStateDtorHook.OriginalDisposeSafe(self, a2);

        if (_wasBgmMuted && Config.Restore)
            IsBgmMuted = true;

        return ret;
    }
}
