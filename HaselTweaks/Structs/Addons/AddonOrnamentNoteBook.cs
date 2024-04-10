using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B F9 E8 ?? ?? ?? ?? 33 F6 48 8D 05 ?? ?? ?? ?? 48 89 07 48 8B CF"
[StructLayout(LayoutKind.Explicit, Size = 0x540)]
public struct AddonOrnamentNoteBook
{
    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;

    [FieldOffset(0x290)] public TabSwitcher TabSwitcher;
}
