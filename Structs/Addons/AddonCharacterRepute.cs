using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 45 33 C0 48 8D 05 ?? ?? ?? ?? 48 89 03 48 8D 8B ?? ?? ?? ?? 4C 89 83"
[StructLayout(LayoutKind.Explicit, Size = 0x2A0)]
public unsafe partial struct AddonCharacterRepute
{
    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;

    [FieldOffset(0x294)] public int SelectedExpansion;
    [FieldOffset(0x298)] public int ExpansionsCount;

    [MemberFunction("E8 ?? ?? ?? ?? EB 07 33 D2 E8 ?? ?? ?? ?? 48 8B 5C 24 ?? 48 8B 6C 24 ?? 48 8B 74 24 ?? 48 8B 7C 24 ?? 48 83 C4 50")]
    public readonly partial void UpdateDisplay(NumberArrayData* numberArray62, StringArrayData* stringArray57);
}
