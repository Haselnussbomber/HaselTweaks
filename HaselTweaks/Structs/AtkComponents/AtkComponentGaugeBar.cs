using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0x1A8)]
public partial struct AtkComponentGaugeBar
{
    [FieldOffset(0x0)] public AtkComponentNode AtkComponentNode;

    [MemberFunction("E8 ?? ?? ?? ?? 41 3B 1E")]
    public readonly partial uint SetValue(uint value, uint a3, bool skipAnimation);

    [MemberFunction("E8 ?? ?? ?? ?? 89 AF ?? ?? ?? ?? 48 8B 46 20")]
    public readonly partial uint SetSecondaryValue(uint value);
}
