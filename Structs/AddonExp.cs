using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = 656)]
public unsafe struct AddonExp
{
    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;

    [FieldOffset(0x250)] public HaselAtkComponentGaugeBar* GaugeBarNode; // #6
    [FieldOffset(0x258)] public AtkTextNode* RightTextNode; // #5
    [FieldOffset(0x260)] public AtkImageNode* RestedAreaImageNode; // #3
    [FieldOffset(0x268)] public AtkImageNode* PvpAreaImageNode; // #2
    [FieldOffset(0x270)] public uint ClassJob;

    [FieldOffset(0x274)] public uint CurrentExp;
    [FieldOffset(0x278)] public uint RequiredExp;
    [FieldOffset(0x280)] public uint RestedExp;
}
