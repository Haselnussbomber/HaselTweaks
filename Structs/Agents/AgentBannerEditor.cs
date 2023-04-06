using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 89 03 48 8D 53 30"
[Agent(AgentId.BannerEditor)]
[StructLayout(LayoutKind.Explicit, Size = 0x38)]
public unsafe struct AgentBannerEditor
{
    [FieldOffset(0)] public AgentInterface AgentInterface;
    [FieldOffset(0x28)] public AgentBannerEditorState* EditorState;
}

[StructLayout(LayoutKind.Explicit, Size = 0x2D8)]
public unsafe partial struct AgentBannerEditorState
{
    public enum EditorOpenType : int
    {
        Portrait = 0,
        Gearset = 1,
        AdventurerPlate = 2,
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x18)]
    public struct GenericDropdownItem
    {
        [FieldOffset(0)] public nint Data;
        [FieldOffset(0x10)] public ushort Id;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x18)]
    public struct ExpressionDropdownItem
    {
        [FieldOffset(0x08)] public nint Data;
        [FieldOffset(0x10)] public ushort Id;
    }

    [FieldOffset(0x20)] public nint PresetItems;
    [FieldOffset(0x28)] public uint PresetItemsCount;

    [FieldOffset(0x40)] public GenericDropdownItem** BackgroundItems;
    [FieldOffset(0x48)] public uint BackgroundItemsCount;

    [FieldOffset(0x70)] public GenericDropdownItem** FrameItems;
    [FieldOffset(0x78)] public uint FrameItemsCount;

    [FieldOffset(0xA0)] public GenericDropdownItem** AccentItems;
    [FieldOffset(0xA8)] public uint AccentItemsCount;

    [FieldOffset(0xD0)] public GenericDropdownItem** BannerTimelineItems;
    [FieldOffset(0xD8)] public uint BannerTimelineItemsCount;

    [FieldOffset(0x100)] public ExpressionDropdownItem** ExpressionItems;
    [FieldOffset(0x108)] public uint ExpressionItemsCount;

    [FieldOffset(0x120)] public BannerModuleEntry BannerEntry;

    [FieldOffset(0x240)] public fixed uint ItemIds[14];

    [FieldOffset(0x298)] public AgentBannerEditor* AgentBannerEditor;
    [FieldOffset(0x2A0)] public UIModule* UIModule;
    [FieldOffset(0x2A8)] public CharaViewPortrait* CharaView;

    [FieldOffset(0x2B8)] public EditorOpenType OpenType;

    [FieldOffset(0x2C4)] public uint FrameCountdown; // starting at 0.5s on open
    [FieldOffset(0x2C8)] public int GearsetId;

    [FieldOffset(0x2D0)] public int CloseDialogAddonId;
    [FieldOffset(0x2D4)] public bool HasDataChanged;

    [MemberFunction("48 89 5C 24 ?? 48 89 7C 24 ?? 80 79 2C 00")]
    public partial int GetPresetIndex(ushort backgroundIndex, ushort frameIndex, ushort accentIndex);

    [MemberFunction("E8 ?? ?? ?? ?? 44 0A E8")]
    public partial void SetFrame(int frameId);

    [MemberFunction("E8 ?? ?? ?? ?? 4C 8B 64 24 ?? 44 0A E8")]
    public partial void SetAccent(int accentId);

    [MemberFunction("E8 ?? ?? ?? ?? 32 C0 48 8B 4D 37")]
    public partial void SetHasChanged(bool hasDataChanged);
}

[StructLayout(LayoutKind.Explicit, Size = 0x90)]
public unsafe struct BannerModuleEntry
{
    [FieldOffset(0x00)] public fixed byte BannerTimelineName[64]; // string
    [FieldOffset(0x40)] public uint Flags; // maybe? unused?
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
    // [FieldOffset(0x78)] public uint N00019C6B; // 140A4AE7D: *(_DWORD *)(0x54i64 * *(int *)(a1 + 0xC8) + *(_QWORD *)(a1 + 0xA8) + 0x48); -- Checksum?
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
