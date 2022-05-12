using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor 40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 33 D2 48 8D 05 ?? ?? ?? ?? 48 89 03 48 8D 83 ?? ?? ?? ?? 48 89 93 ?? ?? ?? ?? 89 93
[StructLayout(LayoutKind.Explicit, Size = 0x620)]
public unsafe partial struct AddonAdventureNoteBook
{
    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;

    [FieldOffset(0x570)] public TabSwitcher TabSwitcher;
}
