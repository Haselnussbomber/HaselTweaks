namespace HaselTweaks.Structs;

public enum FormatterMode : int
{
    ObjStr = 0,
    Item = 1,   // FormatterType irrelevant
    ActStr = 2,
}

public enum FormatterType : uint
{
    None = 0,               // mode: Item
    Action = 1,             // mode: ActStr
    BNpcName = 2,           // mode: ObjStr
    ENpcResident = 3,       // mode: ObjStr
    EventAction = 4,        // mode: ActStr
    EObjName = 5,           // mode: ActStr
    GatheringPointName = 6, // mode: ObjStr
    MainCommand = 7,        // mode: ActStr
    Companion = 9,          // mode: ObjStr
    CraftAction = 9,        // mode: ActStr
    PetAction = 11,         // mode: ActStr
    CompanyAction = 12,     // mode: ActStr
    Mount = 13,             // mode: ActStr
    BgcArmyAction = 19,     // mode: ActStr
    Ornament = 20,          // mode: ActStr
}

public unsafe partial struct Formatter
{
    [MemberFunction("E9 ?? ?? ?? ?? 48 8D 47 30")]
    public static partial nint FormatObjectName(FormatterMode formatter, uint id, FormatterType type, uint a4);
}
