using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "48 89 5C 24 ?? 57 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? B9 ?? ?? ?? ?? 48 89 03 33 FF 48 8D 83 ?? ?? ?? ?? 66 0F 1F 44 00 ?? 48 89 38 48 89 78 08 48 89 78 10 48 89 78 18 48 89 78 20"
[StructLayout(LayoutKind.Explicit, Size = 0xC28)]
public unsafe struct AddonMiragePrismPrismBox
{
    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;

    [FieldOffset(0xA00)] public AtkComponentButton* PrevButton;
    [FieldOffset(0xA08)] public AtkComponentButton* NextButton;

    [FieldOffset(0xA50)] public AtkComponentDropDownList* JobDropdown;

    [FieldOffset(0xA78)] public AtkComponentDropDownList* OrderDropdown;
}
