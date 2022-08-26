using System;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace HaselTweaks.Structs;

// copy pasta function 48 89 5C 24 ?? 8B 82
[StructLayout(LayoutKind.Explicit)]
public unsafe struct UIState_PvPState
{
    public static readonly int Offset = 0x292C;
    private static UIState_PvPState* pInstance { get; set; }
    public static UIState_PvPState* Instance()
    {
        if (pInstance == null)
        {
            var uiState = UIState.Instance();
            if (uiState == null) return null;

            pInstance = (UIState_PvPState*)((IntPtr)uiState + Offset);
        }
        return pInstance;
    }

    [FieldOffset(0x0)] public byte IsLoaded;

    [FieldOffset(0x4)] public uint ExperienceMaelstrom;
    [FieldOffset(0x8)] public uint ExperienceTwinAdder;
    [FieldOffset(0xC)] public uint ExperienceImmortalFlames;
    [FieldOffset(0x10)] public byte RankMaelstrom;
    [FieldOffset(0x11)] public byte RankTwinAdder;
    [FieldOffset(0x12)] public byte RankImmortalFlames;

    [FieldOffset(0x28)] public byte Season; // starting at 0
    [FieldOffset(0x29)] public byte SeasonRankWithOverflow; // resets to 30 when rank 31 is claimed
    [FieldOffset(0x2A)] public byte SeasonRank; // capped at 30

    [FieldOffset(0x2C)] public ushort SeasonExperience;
    [FieldOffset(0x2E)] public byte SeasonMaxRank; // i guess?
    //[FieldOffset(0x2F)] public byte SeasonMaxRankAgain??;
    [FieldOffset(0x30)] public uint FrontlineTotalMatches;
    [FieldOffset(0x34)] public uint FrontlineTotalFirstPlace;
    [FieldOffset(0x38)] public uint FrontlineTotalSecondPlace;
    [FieldOffset(0x3C)] public uint FrontlineTotalThirdPlace;
    [FieldOffset(0x40)] public ushort FrontlineWeeklyMatches;
    [FieldOffset(0x42)] public ushort FrontlineWeeklyFirstPlace;
    [FieldOffset(0x44)] public ushort FrontlineWeeklySecondPlace;
    [FieldOffset(0x46)] public ushort FrontlineWeeklyThirdPlace;

    [FieldOffset(0x4A)] public ushort CrystallineConflictCasualMatches;
    [FieldOffset(0x4C)] public ushort CrystallineConflictCasualMatchesWon;
    [FieldOffset(0x4E)] public ushort CrystallineConflictRankedMatches;
    [FieldOffset(0x50)] public ushort CrystallineConflictRankedMatchesWon;

    [FieldOffset(0x5C)] public byte CrystallineConflictCurrentRank; // ColosseumMatchRank RowId
    [FieldOffset(0x5D)] public byte CrystallineConflictHighestRank; // ColosseumMatchRank RowId
    [FieldOffset(0x5E)] public byte CrystallineConflictCurrentRiser;
    [FieldOffset(0x5F)] public byte CrystallineConflictHighestRiser;
    [FieldOffset(0x60)] public byte CrystallineConflictCurrentStars;
    [FieldOffset(0x61)] public byte CrystallineConflictHighestStars;
}
