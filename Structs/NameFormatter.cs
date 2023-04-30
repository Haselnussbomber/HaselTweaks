namespace HaselTweaks.Structs;

public unsafe partial struct NameFormatter
{
    public enum Replacer : int
    {
        ObjStr = 0,
        Item = 1,   // FormatterType irrelevant
        ActStr = 2,
    }

    public enum IdConverter : uint
    {
        None = 0,           // mode: Item
        Action = 1,         // mode: ActStr
        BNpc = 2,           // mode: ObjStr
        ENpcResident = 3,   // mode: ObjStr
        EventAction = 4,    // mode: ActStr
        EObj = 5,           // mode: ActStr
        GatheringPoint = 6, // mode: ObjStr
        MainCommand = 7,    // mode: ActStr
        Companion = 9,      // mode: ObjStr
        CraftAction = 9,    // mode: ActStr
        PetAction = 11,     // mode: ActStr
        CompanyAction = 12, // mode: ActStr
        Mount = 13,         // mode: ActStr
        BgcArmyAction = 19, // mode: ActStr
        Ornament = 20,      // mode: ActStr
    }

    [MemberFunction("E9 ?? ?? ?? ?? 48 8D 47 30")]
    public static partial nint Format(Replacer replacer, uint id, IdConverter idConverter, uint a4);
}
