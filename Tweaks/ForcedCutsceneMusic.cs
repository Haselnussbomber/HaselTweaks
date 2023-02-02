using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselTweaks.Structs;

namespace HaselTweaks.Tweaks;

public unsafe class ForcedCutsceneMusic : Tweak
{
    public override string Name => "Forced Cutscene Music";
    public override string Description => "Auto-unmutes background music for most cutscenes.";
    public static Configuration Config => HaselTweaks.Configuration.Instance.Tweaks.ForcedCutsceneMusic;

    public class Configuration
    {
        [ConfigField(Label = "Restore mute state after cutscene")]
        public bool Restore = true;
    }

    [AutoHook, Signature("E8 ?? ?? ?? ?? 48 8B F0 48 89 45 0F", DetourName = nameof(CutsceneStateCtorDetour))]
    private Hook<CutsceneStateCtorDelegate> CutsceneStateCtorHook { get; init; } = null!;
    private delegate LuaCutsceneState* CutsceneStateCtorDelegate(LuaCutsceneState* self, uint cutsceneId, byte a3, int a4, int a5, int a6, int a7);

    [AutoHook, Signature("48 89 5C 24 ?? 57 48 83 EC 20 48 8D 05 ?? ?? ?? ?? 48 8B F9 48 89 01 8B DA 48 83 C1 10 E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 89 07 F6 C3 01 74 0D BA ?? ?? ?? ?? 48 8B CF E8 ?? ?? ?? ?? 48 8B C7 48 8B 5C 24 ?? 48 83 C4 20 5F C3 CC CC CC CC 40 53 48 83 EC 20 48 8D 05 ?? ?? ?? ?? 48 8B D9 48 89 01 F6 C2 01 74 0A BA ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B C3 48 83 C4 20 5B C3 CC CC CC CC CC 40 53", DetourName = nameof(CutsceneStateDtor_Detour))]
    private Hook<CutsceneStateDtorDelegate> CutsceneStateDtorHook { get; init; } = null!;
    private delegate LuaCutsceneState* CutsceneStateDtorDelegate(LuaCutsceneState* self, bool a2);

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

    public LuaCutsceneState* CutsceneStateCtorDetour(LuaCutsceneState* self, uint cutsceneId, byte a3, int a4, int a5, int a6, int a7)
    {
        var ret = CutsceneStateCtorHook.Original(self, cutsceneId, a3, a4, a5, a6, a7);

        Log($"Cutscene {cutsceneId} started");

        var isBgmMuted = IsBgmMuted;

        wasBgmMuted = isBgmMuted;

        if (isBgmMuted)
            IsBgmMuted = false;

        return ret;
    }

    public LuaCutsceneState* CutsceneStateDtor_Detour(LuaCutsceneState* self, bool a2)
    {
        Log($"Cutscene {self->Id} ended");

        var ret = CutsceneStateDtorHook.Original(self, a2);

        if (wasBgmMuted && Config.Restore)
            IsBgmMuted = true;

        return ret;
    }
}
