namespace HaselTweaks.Structs;

public unsafe partial struct NameFormatter
{
    public enum Placeholder : int
    {
        ObjStr = 0,
        Item = 1,   // bypasses IdConverter
        ActStr = 2,
    }

    public enum IdConverter : uint
    {
        // ObjStr
        ObjStr_BNpcName = 2,
        ObjStr_ENpcResident = 3,
        ObjStr_Treasure = 4,
        ObjStr_Aetheryte = 5,
        ObjStr_GatheringPointName = 6,
        ObjStr_EObjName = 7,
        // ObjStr_Mount = 8, // does not work?
        ObjStr_Companion = 9,
        // 10-11 unused
        ObjStr_Item = 12,

        // Item
        Item = 0,

        // ActStr
        ActStr_Trait = 0,
        ActStr_Action = 1,
        ActStr_Item = 2,
        // ActStr_EventItem = 3, // does not work?
        ActStr_EventAction = 4,
        // ActStr_EObjName = 5, // does not work?
        ActStr_GeneralAction = 5,
        ActStr_BuddyAction = 6,
        ActStr_MainCommand = 7,
        // ActStr_Companion = 8, // unresolved, use ObjStr_Companion
        ActStr_CraftAction = 9,
        ActStr_Action2 = 10,
        ActStr_PetAction = 11,
        ActStr_CompanyAction = 12,
        ActStr_Mount = 13,
        // 14-18 unused
        ActStr_BgcArmyAction = 19,
        ActStr_Ornament = 20,
    }

    [MemberFunction("E9 ?? ?? ?? ?? 48 8D 47 30")]
    public static partial nint Format(Placeholder placeholder, uint id, IdConverter idConverter, uint a4);
}
