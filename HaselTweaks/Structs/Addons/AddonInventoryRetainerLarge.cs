using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "48 89 5C 24 ?? 57 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? C6 83 ?? ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 89 03 48 8D 8B ?? ?? ?? ?? 33 C0"
// aka RetainerInventoryLarge
[StructLayout(LayoutKind.Explicit, Size = 0x308)]
public partial struct AddonInventoryRetainerLarge
{
    public const int NUM_TABS = 3;

    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;

    [FieldOffset(0x2F0)] public int TabIndex;

    // called via RetainerInventoryLarge vf68
    [MemberFunction("E9 ?? ?? ?? ?? 33 D2 E8 ?? ?? ?? ?? 48 83 C4 48")]
    public readonly partial void SetTab(int tab);
}
