using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "48 89 5C 24 ?? 57 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 33 FF C6 83 ?? ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 89 03 48 8D 8B"
[StructLayout(LayoutKind.Explicit, Size = 0x310)]
public unsafe partial struct AddonInventoryEvent
{
    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;

    [FixedSizeArray<Pointer<AtkComponentRadioButton>>(5)]
    [FieldOffset(0x258)] public fixed byte Buttons[8 * 5];

    [FieldOffset(0x280)] public int Unk280;

    [FieldOffset(0x308)] public int TabIndex;

    [MemberFunction("E8 ?? ?? ?? ?? EB 09 83 FF 01")]
    public readonly partial void SetTab(int tab);
}
