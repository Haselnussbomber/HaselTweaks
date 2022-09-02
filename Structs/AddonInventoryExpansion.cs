using System.Runtime.InteropServices;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor 40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? C6 83 ?? ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 89 03 48 8D 8B ?? ?? ?? ?? 33 C0
[StructLayout(LayoutKind.Explicit, Size = 0x328)]
public unsafe partial struct AddonInventoryExpansion
{
    public const int NUM_TABS = 2;

    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;

    [FieldOffset(0x320)] public int TabIndex;

    // called via InventoryExpansion vf67
    [MemberFunction("E8 ?? ?? ?? ?? BB ?? ?? ?? ?? 83 EB 01")]
    public partial void SetTab(int tab, bool force);
}
