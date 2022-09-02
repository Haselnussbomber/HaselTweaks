using System.Runtime.InteropServices;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0x1A8)]
public partial struct HaselAtkComponentGaugeBar
{
    [FieldOffset(0x0)] public AtkComponentBase AtkComponentBase;

    [MemberFunction("E8 ?? ?? ?? ?? 41 3B 1E")]
    public partial uint SetValue(uint value, uint a3, bool skipAnimation);

    [MemberFunction("48 8D 81 ?? ?? ?? ?? 89 54 24 10")]
    public partial uint SetSecondaryValue(uint value);
}
