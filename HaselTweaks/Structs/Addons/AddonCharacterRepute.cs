using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 45 33 C0 48 8D 05 ?? ?? ?? ?? 48 89 03 48 8D 8B ?? ?? ?? ?? 4C 89 83"
[StructLayout(LayoutKind.Explicit, Size = 0x2A0)]
public struct AddonCharacterRepute
{
    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;

    [FieldOffset(0x294)] public int SelectedExpansion;
    [FieldOffset(0x298)] public int ExpansionsCount;
}
