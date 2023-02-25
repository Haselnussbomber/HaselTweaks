using FFXIVClientStructs.FFXIV.Client.UI.Misc;
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

    private bool wasBgmMuted;

    /// <see href="https://github.com/karashiiro/SoundSetter/blob/master/SoundSetter/OptionInternals/OptionKind.cs#L23"/>
    private static uint IsSndBgm => 35; // ConfigOption.IsSndBgm
    private bool IsBgmMuted
    {
        get
        {
            var configModule = ConfigModule.Instance();
            return configModule != null && configModule->GetIntValue(IsSndBgm) == 1;
        }
        set
        {
            var configModule = ConfigModule.Instance();
            if (configModule == null) return;

            Log($"setting IsBgmMuted to {value}");

            configModule->SetOption(IsSndBgm, value ? 1 : 0);
        }
    }

    [SigHook("E8 ?? ?? ?? ?? 48 8B F0 48 89 45 0F")]
    public LuaCutsceneState* CutsceneStateCtor(LuaCutsceneState* self, uint cutsceneId, byte a3, int a4, int a5, int a6, int a7)
    {
        var ret = CutsceneStateCtorHook.Original(self, cutsceneId, a3, a4, a5, a6, a7);

        Log($"Cutscene {cutsceneId} started");

        var isBgmMuted = IsBgmMuted;

        wasBgmMuted = isBgmMuted;

        if (isBgmMuted)
            IsBgmMuted = false;

        return ret;
    }

    [VTableHook<LuaCutsceneState>(0)]
    public LuaCutsceneState* CutsceneStateDtor(LuaCutsceneState* self, bool a2)
    {
        Log($"Cutscene {self->Id} ended");

        var ret = CutsceneStateDtorHook.Original(self, a2);

        if (wasBgmMuted && Config.Restore)
            IsBgmMuted = true;

        return ret;
    }
}
