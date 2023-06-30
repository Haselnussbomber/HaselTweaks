using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "48 89 5C 24 ?? 57 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 33 FF C6 83 ?? ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 89 03 48 8D 8B"
[StructLayout(LayoutKind.Explicit, Size = 0x310)]
public unsafe partial struct AddonInventoryEvent
{
    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;

    [FieldOffset(0x300)] public int NumTabs; // maybe
    [FieldOffset(0x308)] public int TabIndex;

    [MemberFunction("E8 ?? ?? ?? ?? B0 01 EB 02 32 C0 48 8B 5C 24 ?? 48 8B 6C 24 ?? 48 8B 74 24 ?? 48 83 C4 30 41 5F 41 5E 41 5D 41 5C 5F C3 CC CC CC CC CC CC CC")]
    public readonly partial void SwitchToInventory(byte a2);

    [MemberFunction("E8 ?? ?? ?? ?? EB 09 83 FF 01")]
    public readonly partial void SetTab(int tab);
}
