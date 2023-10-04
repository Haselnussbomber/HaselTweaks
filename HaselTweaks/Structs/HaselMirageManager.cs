namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct HaselMirageManager
{
    [MemberFunction("E8 ?? ?? ?? ?? 84 C0 74 0F 41 B0 01")]
    public partial bool ExtractPrismBoxItem(uint itemIndex);
}
