using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? C7 83 ?? ?? ?? ?? ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 89 03 41 B0 01 33 C0 BA ?? ?? ?? ?? 48 89 83 ?? ?? ?? ?? 48 8B CB 48 89 83 ?? ?? ?? ?? 89 83"
[StructLayout(LayoutKind.Explicit, Size = 0x260)]
public unsafe partial struct AddonAetherCurrent
{
    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;

    [FieldOffset(0x228)] public AtkComponentRadioButton* Tabs; // Tabs[NumTabs]

    [FieldOffset(0x254)] public int TabIndex;
    [FieldOffset(0x258)] public int TabCount;

    // called in AetherCurrent vf55
    [MemberFunction("E8 ?? ?? ?? ?? 84 C0 74 65 39 9D")]
    public readonly partial void SetTab(int tab);
}
