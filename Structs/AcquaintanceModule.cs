using FFXIVClientStructs.FFXIV.Client.System.Framework;

namespace HaselTweaks.Structs;

public unsafe partial struct AcquaintanceModule
{
    public static AcquaintanceModule* Instance()
        => (AcquaintanceModule*)Framework.Instance()->GetUiModule()->GetAcquaintanceModule();

    [MemberFunction("E8 ?? ?? ?? ?? 49 8B 45 00 49 8B CD FF 50 48")]
    public readonly partial void ClearTellHistory(bool save = true);
}
