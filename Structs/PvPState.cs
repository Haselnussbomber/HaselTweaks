namespace HaselTweaks.Structs;

// copy pasta function "48 89 5C 24 ?? 8B 82"
[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct PvPState
{
    [StaticAddress("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B 43 08")]
    public static partial PvPState* Instance();

    [FieldOffset(0x0)] public byte IsLoaded;

    [FieldOffset(0x4)] public uint ExperienceMaelstrom;
    [FieldOffset(0x8)] public uint ExperienceTwinAdder;
    [FieldOffset(0xC)] public uint ExperienceImmortalFlames;
    [FieldOffset(0x10)] public byte RankMaelstrom;
    [FieldOffset(0x11)] public byte RankTwinAdder;
    [FieldOffset(0x12)] public byte RankImmortalFlames;

    [FieldOffset(0x1E)] public byte Series;
    [FieldOffset(0x1F)] public byte SeriesClaimedRank; // resets to 30 when rank 31 is claimed
    [FieldOffset(0x20)] public byte SeriesCurrentRank; // capped at 30

    [FieldOffset(0x22)] public ushort SeriesExperience;
    [FieldOffset(0x24)] public byte PreviousSeriesClaimedRank; // guessed
    [FieldOffset(0x25)] public byte PreviousSeriesRank; // guessed

    [FieldOffset(0x28)] public uint FrontlineTotalMatches;
    [FieldOffset(0x2C)] public uint FrontlineTotalFirstPlace;
    [FieldOffset(0x30)] public uint FrontlineTotalSecondPlace;
    [FieldOffset(0x34)] public uint FrontlineTotalThirdPlace;
    [FieldOffset(0x38)] public ushort FrontlineWeeklyMatches;
    [FieldOffset(0x3A)] public ushort FrontlineWeeklyFirstPlace;
    [FieldOffset(0x3C)] public ushort FrontlineWeeklySecondPlace;
    [FieldOffset(0x3E)] public ushort FrontlineWeeklyThirdPlace;

    [FieldOffset(0x41)] public byte CrystallineConflictSeason;
    [FieldOffset(0x42)] public ushort CrystallineConflictCasualMatches;
    [FieldOffset(0x44)] public ushort CrystallineConflictCasualMatchesWon;
    [FieldOffset(0x46)] public ushort CrystallineConflictRankedMatches;
    [FieldOffset(0x48)] public ushort CrystallineConflictRankedMatchesWon;

    [FieldOffset(0x4E)] public ushort CrystallineConflictCurrentCrystalCredit; // used when CrystallineConflictCurrentRank == 6
    [FieldOffset(0x50)] public ushort CrystallineConflictHighestCrystalCredit; // used when CrystallineConflictHighestRank == 6

    [FieldOffset(0x54)] public byte CrystallineConflictCurrentRank; // ColosseumMatchRank RowId
    [FieldOffset(0x55)] public byte CrystallineConflictHighestRank; // ColosseumMatchRank RowId
    [FieldOffset(0x56)] public byte CrystallineConflictCurrentRiser;
    [FieldOffset(0x57)] public byte CrystallineConflictHighestRiser;
    [FieldOffset(0x58)] public byte CrystallineConflictCurrentRisingStars;
    [FieldOffset(0x59)] public byte CrystallineConflictHighestRisingStars;

/* I don't need these, but they exist.

    // for the active Grand Company
    [MemberFunction("E8 ?? ?? ?? ?? 3A 46 4F")]
    public partial byte GetPvPRank();

    // for the active Grand Company
    [MemberFunction("0F B6 15 ?? ?? ?? ?? 8D 42 FF 3C 02 77 0F")]
    public partial uint GetPvPTotalExperience();

    // for the active Grand Company
    [MemberFunction("E8 ?? ?? ?? ?? 49 8B CC 8B F0")]
    public partial uint GetPvPCurrentRankExperience();

    // for the active Grand Company
    [MemberFunction("0F B6 15 ?? ?? ?? ?? 8D 42 FF 3C 02 77 07")]
    public partial uint GetPvPCurrentRankNeededExperience();

    [MemberFunction("E8 ?? ?? ?? ?? 0F B6 F0 8B FE")]
    public partial byte GetSeriesClaimedRank();

    [MemberFunction("E8 ?? ?? ?? ?? 0F B6 C0 FF C0 3B D8")]
    public partial byte GetSeriesCurrentRank();

    [MemberFunction("0F B7 41 22 C3 CC")]
    public partial ushort GetSeriesExperience();
*/
}
