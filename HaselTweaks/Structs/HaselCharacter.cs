namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct HaselCharacter
{
    [FieldOffset(0x920)] public ActionTimelineManager ActionTimelineManager;
    [FieldOffset(0x1418)] public CharacterStruct1418 CharacterStruct1418;

    // [MemberFunction("E8 ?? ?? ?? ?? 83 FE 4F")]
    // public partial void Rotate(float value);

    [MemberFunction("E8 ?? ?? ?? ?? 83 4B 70 01")]
    public readonly partial void SetPosition(float x, float y, float z);
}

[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct CharacterStruct1418
{
    [MemberFunction("E8 ?? ?? ?? ?? 45 0F B6 86 ?? ?? ?? ?? 48 8D 8F")]
    public partial void SetupBNpc(uint bNpcBaseId, uint bNpcNameId = 0);
}
