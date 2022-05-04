using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor 40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 33 C9 C6 83
[StructLayout(LayoutKind.Explicit, Size = 0x328)]
public unsafe struct AddonInventoryLarge
{
    public const int NUM_TABS = 4;

    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;

    [FieldOffset(0x318)] public int TabIndex;
}
