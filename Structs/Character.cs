namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct Character
{
    [FieldOffset(0x920)] public ActionTimelineManager ActionTimelineManager;

    // [MemberFunction("E8 ?? ?? ?? ?? 83 FE 4F")]
    // public partial void Rotate(float value);

    [MemberFunction("E8 ?? ?? ?? ?? 83 4B 70 01")]
    public readonly partial void SetPosition(float x, float y, float z);

    [MemberFunction("E8 ?? ?? ?? ?? 45 0F B6 86 ?? ?? ?? ?? 33 D2")]
    public readonly partial void SetupBNpc(uint bNpcBaseId, uint bNpcName = 0);
}
