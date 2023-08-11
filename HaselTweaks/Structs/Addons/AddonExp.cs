using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 45 33 C0 48 8D 8B ?? ?? ?? ?? 48 89 03 33 D2 E8 ?? ?? ?? ?? 80 8B ?? ?? ?? ?? ?? 33 C0"
[Addon("_Exp")]
[VTableAddress("48 8D 05 ?? ?? ?? ?? 45 33 C0 48 8D 8B ?? ?? ?? ?? 48 89 03 33 D2 E8 ?? ?? ?? ?? 80 8B ?? ?? ?? ?? ?? 33 C0", 3)]
[StructLayout(LayoutKind.Explicit, Size = 0x290)]
public unsafe partial struct AddonExp
{
    [FieldOffset(0x0)] public AtkUnitBase AtkUnitBase;

    [FieldOffset(0x270)] public byte ClassJob;

    [FieldOffset(0x278)] public uint CurrentExp;
    [FieldOffset(0x27C)] public uint RequiredExp;
    [FieldOffset(0x280)] public uint RestedExp;

    public readonly float CurrentExpPercent => (float)CurrentExp / RequiredExp * 100;
}
