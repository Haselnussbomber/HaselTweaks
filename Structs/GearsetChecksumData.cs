namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Sequential, Size = 4 * 14 + 14)]
public unsafe partial struct GearsetChecksumData
{
    public fixed uint ItemIds[14];
    public fixed byte StainIds[14];

    [MemberFunction("E8 ?? ?? ?? ?? 89 43 48 48 83 C4 20")]
    public static partial uint GenerateChecksum(uint* itemIds, byte* stainIds, BannerGearVisibilityFlag gearVisibilityFlag);
};
