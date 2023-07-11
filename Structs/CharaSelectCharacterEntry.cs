namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0x6F8)]
public unsafe struct CharaSelectCharacterEntry
{
    [FieldOffset(0x8)] public ulong ContentId;

    [FieldOffset(0x18)] public short CurrentWorldId;
    [FieldOffset(0x1A)] public short HomeWorldId;

    [FixedString("Name")]
    [FieldOffset(0x2C)] public fixed char NameBytes[32];
    [FixedString("CurrentWorldName")]
    [FieldOffset(0x4C)] public fixed char CurrentWorldNameBytes[32];
    [FixedString("HomeWorldName")]
    [FieldOffset(0x6C)] public fixed char HomeWorldNameBytes[32];
    [FixedString("RawJson")]
    [FieldOffset(0x8C)] public fixed char RawJsonBytes[1024];

    [FieldOffset(0x4C0)] public ParsedCharaSelectCharacterData ParsedData;
    [FieldOffset(0x5D0)] public ParsedCharaSelectCharacterData ParsedData2;
}

[StructLayout(LayoutKind.Explicit, Size = 0x110)]
public unsafe struct ParsedCharaSelectCharacterData
{
    [FixedString("Name")]
    [FieldOffset(0x08)] public fixed char NameBytes[32];
    [FieldOffset(0x28)] public byte CurrentClassJobId;

    [FieldOffset(0x2A)] public fixed short ClassJobLevelArray[30];
    // guessed
    // [FieldOffset(0x66)] public byte Race;
    // [FieldOffset(0x67)] public byte Tribe;
    // [FieldOffset(0x68)] public byte Sex;
    // [FieldOffset(0x69)] public byte GuardianDeity;
    // [FieldOffset(0x6A)] public byte BirthDay;
    // [FieldOffset(0x6B)] public byte BirthMonth;
    // [FieldOffset(0x6C)] public byte FirstClass;

    [FieldOffset(0x70)] public ushort TerritoryId;
}
