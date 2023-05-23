using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Misc.UserFileManager;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0x48)]
public unsafe partial struct BannerModule
{
    public static BannerModule* Instance() => (BannerModule*)((nint)Framework.Instance()->GetUiModule() + 0x9EB90); // vf58 = GetBannerModule()

    [FieldOffset(0)] public UserFileEvent UserFileEvent;
    [FieldOffset(0x40)] public BannerModuleData* Data;

    [MemberFunction("E8 ?? ?? ?? ?? 48 8B F8 48 85 C0 74 47 41 8B D6")]
    public partial BannerModuleEntry* CreateBanner();

    [MemberFunction("E8 ?? ?? ?? ?? 40 0A F0 FF C3 48 FF C7 83 FB 6E 72 D6 40 0F B6 C6 48 8B 8C 24 ?? ?? ?? ?? 48 33 CC E8 ?? ?? ?? ?? 4C 8D 9C 24 ?? ?? ?? ?? 49 8B 5B 18 49 8B 6B 20 49 8B 73 28 49 8B E3 5F C3 CC CC")]
    public partial bool DeleteBanner();

    [MemberFunction("E8 ?? ?? ?? ?? 83 F8 6E 7C 15")]
    public partial uint GetNextId();

    [MemberFunction("E8 ?? ?? ?? ?? 0F B7 40 7E")]
    public partial BannerModuleEntry* GetBannerById(int id);

    [MemberFunction("E8 ?? ?? ?? ?? 85 C0 79 0C")]
    public partial int GetBannerIdByBannerIndex(int index);
}

[StructLayout(LayoutKind.Explicit, Size = 0x38B8)]
public unsafe partial struct BannerModuleData
{
    [FixedSizeArray<BannerModuleEntry>(100)]
    [FieldOffset(0x00)] public fixed byte Entries[0x90 * 100];
    [FieldOffset(0x3840)] public fixed byte BannerId2BannerIndex[100];
    [FieldOffset(0x38A4)] public byte NextId;

    [MemberFunction("40 56 48 83 EC 20 80 B9 ?? ?? ?? ?? ?? 48 8B F1 7C 08")]
    public partial BannerModuleEntry* CreateBanner();

    [MemberFunction("48 89 5C 24 ?? 57 48 83 EC 20 48 63 FA 48 8B D9 85 D2 0F 88 ?? ?? ?? ??")]
    public partial bool DeleteBanner(int index);
}

[StructLayout(LayoutKind.Explicit, Size = 0x90)]
public unsafe struct BannerModuleEntry
{
    [FieldOffset(0x00)] public fixed byte BannerTimelineName[64]; // string
    [FieldOffset(0x40)] public uint Flags; // maybe? see "8B C2 4C 8B C9 99"
    [FieldOffset(0x44)] public HalfVector4 CameraPosition;
    [FieldOffset(0x4C)] public HalfVector4 CameraTarget;
    [FieldOffset(0x54)] public HalfVector2 HeadDirection;
    [FieldOffset(0x58)] public HalfVector2 EyeDirection;
    [FieldOffset(0x5C)] public short DirectionalLightingVerticalAngle;
    [FieldOffset(0x5E)] public short DirectionalLightingHorizontalAngle;
    [FieldOffset(0x60)] public byte Race; // CustomizeData[0]
    [FieldOffset(0x61)] public byte Gender; // CustomizeData[1]
    [FieldOffset(0x62)] public byte Height; // CustomizeData[3]
    [FieldOffset(0x63)] public byte Tribe; // CustomizeData[4]
    [FieldOffset(0x64)] public byte DirectionalLightingColorRed;
    [FieldOffset(0x65)] public byte DirectionalLightingColorGreen;
    [FieldOffset(0x66)] public byte DirectionalLightingColorBlue;
    [FieldOffset(0x67)] public byte AmbientLightingColorRed;
    [FieldOffset(0x68)] public byte AmbientLightingColorGreen;
    [FieldOffset(0x69)] public byte AmbientLightingColorBlue;
    [FieldOffset(0x6C)] public float AnimationProgress;
    [FieldOffset(0x70)] public uint BannerTimelineIcon;
    [FieldOffset(0x74)] public uint LastUpdated; // unix timestamp
    [FieldOffset(0x78)] public uint GearChecksum; // see "E8 ?? ?? ?? ?? 89 43 48 48 83 C4 20" (BannerModuleEntry is not a parameter of this! the hash is generated when AgentBannerList loads)
    [FieldOffset(0x7C)] public ushort BannerBg;
    [FieldOffset(0x7E)] public ushort BannerFrame;
    [FieldOffset(0x80)] public ushort BannerDecoration;
    [FieldOffset(0x82)] public ushort BannerTimeline;
    [FieldOffset(0x84)] public short ImageRotation;
    [FieldOffset(0x86)] public byte BannerEntryIndex;
    [FieldOffset(0x87)] public byte BannerID;
    [FieldOffset(0x88)] public byte BannerTimelineClassJobCategory;
    [FieldOffset(0x89)] public byte Expression;
    [FieldOffset(0x8A)] public byte CameraZoom;
    [FieldOffset(0x8B)] public byte DirectionalLightingBrightness;
    [FieldOffset(0x8C)] public byte AmbientLightingBrightness;
    [FieldOffset(0x8D)] public byte HasBannerTimelineCustomName;
    // [FieldOffset(0x8E)] public byte N00019BE6;
    // [FieldOffset(0x8F)] public byte N00019BCA;
}

[Flags]
public enum BannerGearVisibilityFlag : uint
{
    None = 0,
    HeadgearHidden = 1 << 0,
    WeaponHidden = 1 << 1,
    VisorClosed = 1 << 2,
}
