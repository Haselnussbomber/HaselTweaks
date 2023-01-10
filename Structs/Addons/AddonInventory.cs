using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "48 89 5C 24 ?? 57 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 33 FF C6 83 ?? ?? ?? ?? ?? 48 89 BB"
[StructLayout(LayoutKind.Explicit, Size = 0x320)]
public unsafe partial struct AddonInventory
{
    public const int NUM_TABS = 5;

    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;

    [FieldOffset(0x31C)] public int TabIndex;

    [MemberFunction("E9 ?? ?? ?? ?? CC CC CC CC CC CC CC CC CC CC 48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 44 88 44 24")]
    public partial void SwitchToInventoryEvent(byte a2);

    // called via Inventory vf67
    [MemberFunction("E9 ?? ?? ?? ?? 83 FD 11")]
    public partial void SetTab(int tab);
}
