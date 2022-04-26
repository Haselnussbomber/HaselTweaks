using System;
using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselTweaks.Structs;

namespace HaselTweaks.Tweaks;

public unsafe class ForcedCutsceneMusic : Tweak
{
    public override string Name => "Forced Cutscene Music";
    public override string Description => "Auto-unmutes background music for cutscenes.";
    public Configuration Config => Plugin.Config.Tweaks.ForcedCutsceneMusic;

    public class Configuration
    {
        [ConfigField(Label = "Restore mute state after cutscene")]
        public bool Restore = true;
    }

    [Signature("89 54 24 10 53 55 57 41 54 41 55 41 56 48 83 EC 48 8B C2 45 8B E0 44 8B D2 45 32 F6 44 8B C2 45 32 ED")]
    private SetValueByIndexDelegate SetValueByIndex { get; init; } = null!;
    private delegate IntPtr SetValueByIndexDelegate(ConfigModule* self, ulong kind, ulong value, ulong a4, ulong triggerUpdate, ulong a6);

    [Signature("E8 ?? ?? ?? ?? 48 8B F0 48 89 45 0F", DetourName = nameof(CutsceneStateCtorDetour))]
    private Hook<CutsceneStateCtorDelegate> CutsceneStateCtorHook { get; init; } = null!;
    private delegate CutsceneState* CutsceneStateCtorDelegate(CutsceneState* self, uint cutsceneId, byte a3, int a4, int a5, int a6, int a7);

    [Signature("48 89 5C 24 ?? 57 48 83 EC 20 48 8D 05 ?? ?? ?? ?? 48 8B F9 48 89 01 8B DA 48 83 C1 10 E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 89 07 F6 C3 01 74 0D BA ?? ?? ?? ?? 48 8B CF E8 ?? ?? ?? ?? 48 8B C7 48 8B 5C 24 ?? 48 83 C4 20 5F C3 CC CC CC CC 40 53 48 83 EC 20 48 8D 05 ?? ?? ?? ?? 48 8B D9 48 89 01 F6 C2 01 74 0A BA ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B C3 48 83 C4 20 5B C3 CC CC CC CC CC 40 53", DetourName = nameof(CutsceneStateDtor_Detour))]
    private Hook<CutsceneStateDtor_Delegate> CutsceneStateDtorHook { get; init; } = null!;
    private delegate CutsceneState* CutsceneStateDtor_Delegate(CutsceneState* self, IntPtr a2, IntPtr a3, IntPtr a4);

    private ConfigModule* ConfigModule
    {
        get
        {
            var framework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance();
            if (framework == null) return null;

            var uiModule = framework->GetUiModule();
            if (uiModule == null) return null;

            return uiModule->GetConfigModule();
        }
    }

    private bool IsBgmMuted
    {
        get
        {
            var configModule = ConfigModule;
            if (configModule == null) return false;

            // BgmMuted from https://github.com/karashiiro/SoundSetter/blob/master/SoundSetter/OptionInternals/OptionKind.cs
            var value = configModule->GetValue(35);
            if (value == null) return false;

            return value->Byte == 0x01;
        }
        set
        {
            var configModule = ConfigModule;
            if (configModule == null) return;

            Log($"setting IsBgmMuted to {value}");

            SetValueByIndex(configModule, 35, value ? 1u : 0u, 0, 1, 0);
        }
    }

    private bool wasBgmMuted = false;

    public override void Enable()
    {
        CutsceneStateCtorHook?.Enable();
        CutsceneStateDtorHook?.Enable();
    }

    public override void Disable()
    {
        CutsceneStateCtorHook?.Disable();
        CutsceneStateDtorHook?.Disable();
    }

    public override void Dispose()
    {
        CutsceneStateCtorHook?.Dispose();
        CutsceneStateDtorHook?.Dispose();
    }

    public CutsceneState* CutsceneStateCtorDetour(CutsceneState* self, uint cutsceneId, byte a3, int a4, int a5, int a6, int a7)
    {
        var ret = CutsceneStateCtorHook.Original(self, cutsceneId, a3, a4, a5, a6, a7);

        Log($"Cutscene {cutsceneId} started");

        var isBgmMuted = IsBgmMuted;

        wasBgmMuted = isBgmMuted;

        if (isBgmMuted)
            IsBgmMuted = false;

        return ret;
    }

    public CutsceneState* CutsceneStateDtor_Detour(CutsceneState* self, IntPtr a2, IntPtr a3, IntPtr a4)
    {
        Log($"Cutscene {self->Id} ended");

        var ret = CutsceneStateDtorHook.Original(self, a2, a3, a4);

        if (wasBgmMuted && Config.Restore)
            IsBgmMuted = true;

        return ret;
    }
}
