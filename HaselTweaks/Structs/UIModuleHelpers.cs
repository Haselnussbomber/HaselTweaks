using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit)]
public unsafe struct UIModuleHelpers
{
    public static UIModuleHelpers* Instance() => (UIModuleHelpers*)UIModule.Instance()->GetUIModuleHelpers();

    [FieldOffset(8)] public BannerModuleHelper* BannerModuleHelper; // no clue, but hosts helper functions
}

// inlined ctor "48 89 08 33 C9 48 89 48 10 48 89 48 18"
// TODO: "E8 ?? ?? ?? ?? 84 C0 75 03 40 32 F6" - it copies the current portrait to the BannerModuleHelper?
// TODO: "83 BE ?? ?? ?? ?? ?? 0F 84 ?? ?? ?? ?? 48 8B 8E ?? ?? ?? ?? 48 89 BC 24 ?? ?? ?? ??" - code for sending Adventurer Plate data
[StructLayout(LayoutKind.Explicit, Size = 0x50)]
public unsafe partial struct BannerModuleHelper
{
    [MemberFunction("E8 ?? ?? ?? ?? 84 C0 74 45 41 B0 01")]
    public readonly partial bool InitializeBannerUpdateData(BannerUpdateData* bannerUpdateData);

    [MemberFunction("E8 ?? ?? ?? ?? 48 8D 55 B0 48 8B CF E8 ?? ?? ?? ?? 48 8B BC 24")]
    public readonly partial bool CopyBannerEntryToBannerUpdateData(BannerUpdateData* bannerUpdateData, BannerModuleEntry* bannerModuleEntry);

    [MemberFunction("E8 ?? ?? ?? ?? 48 8B AC 24 ?? ?? ?? ?? 48 8B 4C 24 ?? 48 33 CC E8 ?? ?? ?? ?? 48 83 C4 60")]
    public readonly partial bool SendBannerUpdateData(BannerUpdateData* bannerUpdateData);

    [MemberFunction("E8 ?? ?? ?? ?? 88 44 24 23")]
    public readonly partial bool IsBannerNotExpired(BannerModuleEntry* bannerModuleEntry, int type);

    [MemberFunction("40 53 48 83 EC 20 4C 8B 1D")]
    public readonly partial bool IsBannerCharacterDataNotExpired(BannerModuleEntry* bannerModuleEntry, int type);

    [MemberFunction("41 0F B6 80 ?? ?? ?? ?? 88 42 60")]
    public readonly partial void CopyRaceGenderHeightTribe(BannerModuleEntry* bannerModuleEntry, Character* localPlayer); // from localPlayer to bannerModuleEntry
}

[StructLayout(LayoutKind.Explicit, Size = 0x40)]
public unsafe partial struct BannerUpdateData
{
    [FieldOffset(0x00)] public byte HasData; // probably?
    [FieldOffset(0x01)] public byte Expression;
    [FieldOffset(0x02)] public byte CameraZoom;
    [FieldOffset(0x03)] public byte DirectionalLightingColorRed;
    [FieldOffset(0x04)] public byte DirectionalLightingColorGreen;
    [FieldOffset(0x05)] public byte DirectionalLightingColorBlue;
    [FieldOffset(0x06)] public byte DirectionalLightingBrightness;
    [FieldOffset(0x07)] public byte AmbientLightingColorRed;
    [FieldOffset(0x08)] public byte AmbientLightingColorGreen;
    [FieldOffset(0x09)] public byte AmbientLightingColorBlue;
    [FieldOffset(0x0A)] public byte AmbientLightingBrightness;
    [FieldOffset(0x0B)] public ushort BannerTimeline;
    [FieldOffset(0x0D)] public ushort AnimationProgress;
    [FieldOffset(0x0F)] public ushort HeadDirectionY;
    [FieldOffset(0x11)] public ushort HeadDirectionX;
    [FieldOffset(0x13)] public ushort EyeDirectionY;
    [FieldOffset(0x15)] public ushort EyeDirectionX;
    [FieldOffset(0x17)] public ushort CameraPositionX;
    [FieldOffset(0x19)] public ushort CameraPositionY;
    [FieldOffset(0x1B)] public ushort CameraPositionZ;
    [FieldOffset(0x1D)] public ushort CameraTargetX;
    [FieldOffset(0x1F)] public ushort CameraTargetY;
    [FieldOffset(0x21)] public ushort CameraTargetZ;
    [FieldOffset(0x23)] public ushort ImageRotation;
    [FieldOffset(0x25)] public ushort DirectionalLightingVerticalAngle;
    [FieldOffset(0x27)] public ushort DirectionalLightingHorizontalAngle;
    [FieldOffset(0x29)] public ushort BannerDecoration;
    [FieldOffset(0x2B)] public ushort BannerBg;
    [FieldOffset(0x2D)] public ushort BannerFrame;
    [FieldOffset(0x2F)] public uint Checksum;

    [MemberFunction("E8 ?? ?? ?? ?? 89 6B 5C")]
    public readonly partial void Initialize();
}
