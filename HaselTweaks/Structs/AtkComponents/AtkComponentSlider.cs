namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit)]
public partial struct AtkComponentSlider
{
    [MemberFunction("E8 ?? ?? ?? ?? 0F B6 43 3C")]
    public readonly partial void SetValue(int value, bool dispatchEvent29 = false);
}
