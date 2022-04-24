using System;
using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Logging;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace HaselTweaks.Tweaks;

public unsafe class ForcedCutsceneMusic : Tweak
{
    public override string Name => "Forced Cutscene Music";
    public override string Description => "Auto-unmutes background music for cutscenes.";

    [Signature("89 54 24 10 53 55 57 41 54 41 55 41 56 48 83 EC 48 8B C2 45 8B E0 44 8B D2 45 32 F6 44 8B C2 45 32 ED")]
    private SetValueByIndexDelegate SetValueByIndex { get; init; } = null!;
    private delegate IntPtr SetValueByIndexDelegate(IntPtr baseAddress, ulong kind, ulong value, ulong unk1, ulong triggerUpdate, ulong unk3);

    [Signature("4C 8D 0D ?? ?? ?? ?? 44 0F B7 43", ScanType = ScanType.StaticAddress)]
    private int* CurrentCutsceneId { get; init; }

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

            PluginLog.Log($"[ForcedCutsceneMusic] setting IsBgmMuted to {value}");

            SetValueByIndex((IntPtr)configModule, 35, value ? 1u : 0u, 0, 1, 0);
        }
    }

    private static bool IsInCutscene =>
        Service.Condition[ConditionFlag.OccupiedInCutSceneEvent] ||
        Service.Condition[ConditionFlag.WatchingCutscene] ||
        Service.Condition[ConditionFlag.WatchingCutscene78];

    private bool wasInCutscene = false;
    private bool wasBgmMuted = false;

    public override void Enable()
    {
        // initial state
        wasBgmMuted = IsBgmMuted;
        wasInCutscene = IsInCutscene;
    }

    public override void OnFrameworkUpdate(Framework framework)
    {
        var isInCutscene = IsInCutscene;

        if (!wasInCutscene && isInCutscene)
        {
            var isBgmMuted = IsBgmMuted;

            wasBgmMuted = isBgmMuted;
            wasInCutscene = true;

            if (isBgmMuted && *CurrentCutsceneId != 3) // disable for bed cutscene on login/logout
                IsBgmMuted = false;
        }
        else if (wasInCutscene && !isInCutscene)
        {
            wasInCutscene = false;

            if (wasBgmMuted)
                IsBgmMuted = true;
        }
    }
}
