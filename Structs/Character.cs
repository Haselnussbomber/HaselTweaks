namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct Character
{
    [FieldOffset(0x920)] public ActionTimelineManager ActionTimelineManager;

    // found via CharaMake function: E8 ?? ?? ?? ?? EB CB 41 83 C8 FF
    [FieldOffset(0x1B24)] public ushort VoiceId;

    // [MemberFunction("E8 ?? ?? ?? ?? 83 FE 4F")]
    // public partial void Rotate(float value);

    [MemberFunction("E8 ?? ?? ?? ?? 83 4B 70 01")]
    public partial void SetPosition(float x, float y, float z);

    [MemberFunction("E8 ?? ?? ?? ?? 45 0F B6 86 ?? ?? ?? ?? 33 D2")]
    public partial void SetupBNpc(uint bNpcBaseId, uint bNpcName = 0);
}
