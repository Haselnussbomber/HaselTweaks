using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.System.Memory;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit)]
public unsafe struct UIModuleVf35Struct
{
    [FieldOffset(8)] public BannerModuleHelper* BannerModuleHelper; // no clue, but hosts helper functions
}

public unsafe partial struct BannerModuleHelper
{
    [MemberFunction("E8 ?? ?? ?? ?? 84 C0 74 45 41 B0 01")]
    public partial bool InitializeBannerUpdateData(BannerUpdateData* bannerUpdateData);

    [MemberFunction("E8 ?? ?? ?? ?? 48 8D 55 B0 48 8B CF E8 ?? ?? ?? ?? 48 8B BC 24")]
    public partial bool CopyBannerEntryToBannerUpdateData(BannerUpdateData* bannerUpdateData, BannerModuleEntry* bannerModuleEntry);

    [MemberFunction("E8 ?? ?? ?? ?? 48 8B AC 24 ?? ?? ?? ?? 48 8B 4C 24 ?? 48 33 CC E8 ?? ?? ?? ?? 48 83 C4 60")]
    public partial bool SendBannerUpdateData(BannerUpdateData* bannerUpdateData);

    [MemberFunction("E8 ?? ?? ?? ?? 88 44 24 23")]
    public partial bool IsBannerNotExpired(BannerModuleEntry* bannerModuleEntry, int type);

    [MemberFunction("40 53 48 83 EC 20 4C 8B 1D")]
    public partial bool IsBannerCharacterDataNotExpired(BannerModuleEntry* bannerModuleEntry, int type);

    [MemberFunction("41 0F B6 80 ?? ?? ?? ?? 88 42 60")]
    public partial void CopyRaceGenderHeightTribe(BannerModuleEntry* bannerModuleEntry, Character* localPlayer); // from localPlayer to bannerModuleEntry
}

[StructLayout(LayoutKind.Explicit, Size = 0x40)]
public unsafe partial struct BannerUpdateData : ICreatable
{
    [MemberFunction("E8 ?? ?? ?? ?? 89 6B 5C")]
    public partial void Ctor();
}
