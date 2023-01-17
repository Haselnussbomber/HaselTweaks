using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 45 33 C0 48 8D 8B ?? ?? ?? ?? 48 89 03 33 D2 E8 ?? ?? ?? ?? 80 8B ?? ?? ?? ?? ?? 33 C0"
[StructLayout(LayoutKind.Explicit, Size = 0x290)]
public unsafe partial struct AddonExp
{
    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;

    [FieldOffset(0x250)] public AtkComponentGaugeBar* GaugeBarNode; // #6
    [FieldOffset(0x258)] public AtkTextNode* RightTextNode; // #5
    [FieldOffset(0x260)] public AtkImageNode* RestedAreaImageNode; // #3
    [FieldOffset(0x268)] public AtkImageNode* PvpAreaImageNode; // #2
    [FieldOffset(0x270)] public uint ClassJob;

    [FieldOffset(0x274)] public uint CurrentExp;
    [FieldOffset(0x278)] public uint RequiredExp;
    [FieldOffset(0x280)] public uint RestedExp;

    [VirtualFunction(50)]
    public partial void OnUpdate(NumberArrayData** numberArrayData, StringArrayData** stringArrayData);
}
