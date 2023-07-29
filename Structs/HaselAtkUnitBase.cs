namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct HaselAtkUnitBase
{
    [MemberFunction("E8 ?? ?? ?? ?? 8B 83 ?? ?? ?? ?? C1 E8 14")]
    public readonly partial void AddonSetup();

    [MemberFunction("E8 ?? ?? ?? ?? 0F BF 8C 24 ?? ?? ?? ?? 01 8F")]
    public readonly partial bool Move(nint xDelta, nint yDelta);
}
