namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct Character
{
    // [MemberFunction("E8 ?? ?? ?? ?? 83 FE 4F")]
    // public partial void Rotate(float value);

    [MemberFunction("E8 ?? ?? ?? ?? 45 0F B6 86 ?? ?? ?? ?? 33 D2")]
    public partial void SetupBNpc(uint bNpcBaseId, uint bNpcName = 0);
}
