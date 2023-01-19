using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B F1 E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 8B D6"
[StructLayout(LayoutKind.Explicit, Size = 0x630)]
public unsafe partial struct AddonMJIMinionNoteBook
{
    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;

    [FieldOffset(0x220)] public AddonMJIMinionNoteBook_Unk220 Unk220;
}

[StructLayout(LayoutKind.Explicit, Size = 0x2C8)]
public unsafe partial struct AddonMJIMinionNoteBook_Unk220
{
    [FieldOffset(0x80)] public TabSwitcher TabSwitcher;
}
