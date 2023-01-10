using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 89 03 48 8D 53 30"
[StructLayout(LayoutKind.Explicit, Size = 0x38)]
public unsafe struct AgentBannerEditor
{
    [FieldOffset(0)] public AgentInterface AgentInterface;
    [FieldOffset(0x28)] public AgentBannerEditPortraitState* PortraitState;
}

[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct AgentBannerEditPortraitState
{
    [FieldOffset(0x40)] public IntPtr BannerItems;
    [FieldOffset(0x48)] public uint BannerItemsCount;

    [FieldOffset(0x70)] public IntPtr FrameItems;
    [FieldOffset(0x78)] public uint FrameItemsCount;

    [FieldOffset(0xA0)] public IntPtr AccentItems;
    [FieldOffset(0xA8)] public uint AccentItemsCount;

    [FieldOffset(0xD0)] public IntPtr PoseItems;
    [FieldOffset(0xD8)] public uint PoseItemsCount;

    [FieldOffset(0x100)] public IntPtr ExpressionItems;
    [FieldOffset(0x108)] public uint ExpressionItemsCount;

    [FieldOffset(0x17C)] public short DirectionalLightingVerticalAngle;
    [FieldOffset(0x17E)] public short DirectionalLightingHorizontalAngle;

    [FieldOffset(0x184)] public byte DirectionalLightingColorRed;
    [FieldOffset(0x185)] public byte DirectionalLightingColorGreen;
    [FieldOffset(0x186)] public byte DirectionalLightingColorBlue;
    [FieldOffset(0x187)] public byte AmbientLightingColorRed;
    [FieldOffset(0x188)] public byte AmbientLightingColorGreen;
    [FieldOffset(0x189)] public byte AmbientLightingColorBlue;

    [FieldOffset(0x190)] public uint PoseEmoteIcon;
    [FieldOffset(0x194)] public uint UpdatedTimestamp;

    [FieldOffset(0x19C)] public short Background;
    [FieldOffset(0x19E)] public short Frame;
    [FieldOffset(0x1A0)] public short Accent;
    [FieldOffset(0x1A2)] public short Pose;

    [FieldOffset(0x1A9)] public byte Expression;

    [FieldOffset(0x1AB)] public byte DirectionalLightingBrightness;
    [FieldOffset(0x1AC)] public byte AmbientLightingBrightness;
    [FieldOffset(0x1AD)] public bool PoseHasNoCustomName;

    [FieldOffset(0x240)] public fixed uint ItemIds[14];

    [FieldOffset(0x2A8)] public CharaViewPortrait* CharaView;

    [FieldOffset(0x2B8)] public uint Unk2B8;
    [FieldOffset(0x2BC)] public uint Unk2BC;

    [FieldOffset(0x2C4)] public byte PortraitIndex;

    [FieldOffset(0x2CC)] public uint CloseDialogAddonId;
    [FieldOffset(0x2D0)] public bool HasDataChanged;

    [MemberFunction("48 89 5C 24 ?? 48 89 7C 24 ?? 80 79 2C 00")]
    public partial int GetPresetIndex(short backgroundIndex, short frameIndex, short accentIndex);

    [MemberFunction("E8 ?? ?? ?? ?? 44 0A E8")]
    public partial void SetFrame(int frameId);

    [MemberFunction("E8 ?? ?? ?? ?? 4C 8B 64 24 ?? 44 0A E8")]
    public partial void SetAccent(int accentId);

    [MemberFunction("E8 ?? ?? ?? ?? 32 C0 48 8B 4D 37")]
    public partial void SetHasChanged(bool hasDataChanged);
}
