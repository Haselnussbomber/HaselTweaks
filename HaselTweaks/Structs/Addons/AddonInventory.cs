using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "48 89 5C 24 ?? 57 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 33 FF C6 83 ?? ?? ?? ?? ?? 48 89 BB"
[StructLayout(LayoutKind.Explicit, Size = 0x320)]
public partial struct AddonInventory
{
    public const int NUM_TABS = 5;

    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;

    [FieldOffset(0x228)] public int Unk228; // unknown value passed as AtkValue in OpenKeyItems

    [FieldOffset(0x31C)] public int TabIndex;

    // called via Inventory vf68
    [MemberFunction("E9 ?? ?? ?? ?? 83 FD 11")]
    public readonly partial void SetTab(int tab);
}
