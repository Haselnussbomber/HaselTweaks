using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor 48 89 5C 24 ?? 57 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 8B CB 48 89 03 E8 ?? ?? ?? ?? 48 8B D0 48 8D 8B ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8D 8B
[StructLayout(LayoutKind.Explicit, Size = 0xD70)]
public unsafe partial struct AddonGoldSaucerCardDeckEdit
{
    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;

    [FieldOffset(0x220)] public TabSwitcherClass TabSwitcher;
}
