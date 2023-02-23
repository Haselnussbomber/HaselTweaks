namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0x30)]
public unsafe partial struct HAtkEvent
{
    [MemberFunction("E8 ?? ?? ?? ?? 8D 53 9C")]
    public partial void SetEventHandled(bool a2);
}
