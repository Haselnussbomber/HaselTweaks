using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor 48 89 5C 24 ?? 57 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 33 FF 48 8D 05 ?? ?? ?? ?? 48 89 03 48 8B CB 48 89 BB
[StructLayout(LayoutKind.Explicit, Size = 0x530)]
public unsafe partial struct AddonGoldSaucerCardList
{
    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;

    [FieldOffset(0x298)] public TabSwitcherClass TabSwitcher;
}
