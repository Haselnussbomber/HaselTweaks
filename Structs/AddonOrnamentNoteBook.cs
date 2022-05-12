using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor 48 89 5C 24 ?? 57 48 83 EC 20 48 8B F9 E8 ?? ?? ?? ?? 33 DB 48 8D 05 ?? ?? ?? ?? 48 89 07 48 8B CF
[StructLayout(LayoutKind.Explicit, Size = 0x4C0)]
public unsafe partial struct AddonOrnamentNoteBook
{
    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;

    [FieldOffset(0x288)] public TabSwitcher TabSwitcher;
}
