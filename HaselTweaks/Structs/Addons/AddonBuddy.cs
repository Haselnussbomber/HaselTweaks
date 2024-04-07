using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "48 89 5C 24 ?? 57 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 33 FF 48 8D 8B ?? ?? ?? ?? 48 89 03 89 BB ?? ?? ?? ?? E8 ?? ?? ?? ?? B9"
[StructLayout(LayoutKind.Explicit, Size = 0x1C00)]
public unsafe partial struct AddonBuddy
{
    public const int NUM_TABS = 3;

    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;
    [FieldOffset(0x220)] public int TabIndex;

    [FixedSizeArray<Pointer<AtkComponentRadioButton>>(3)]
    [FieldOffset(0x1BD8)] public fixed byte RadioButtons[8 * 3];

    [MemberFunction("E8 ?? ?? ?? ?? 3B AF ?? ?? ?? ?? 74 27")]
    public readonly partial void SetTab(int tab);
}
