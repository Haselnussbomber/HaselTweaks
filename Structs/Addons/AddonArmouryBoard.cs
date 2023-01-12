using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? B9 ?? ?? ?? ?? 48 89 03 33 D2 48 8D 83 ?? ?? ?? ?? 66 66 0F 1F 84 00 ?? ?? ?? ?? 48 89 90"
[StructLayout(LayoutKind.Explicit, Size = 0x6C0)]
public unsafe partial struct AddonArmouryBoard
{
    public const int NUM_TABS = 12;

    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;

    [FieldOffset(0x690)] public int TabIndex;

    // called via ArmouryBoard_ReceiveEvent event/case 12 -> case a4 == 16
    [MemberFunction("E8 ?? ?? ?? ?? EB E0 84 C9")]
    public partial void NextTab(byte a2);

    // called via ArmouryBoard_ReceiveEvent event/case 12 -> after switch (a4 == 17)
    [MemberFunction("40 53 48 83 EC 20 80 B9 ?? ?? ?? ?? ?? 48 8B D9 75 11")]
    public partial void PreviousTab(byte a2);
}
