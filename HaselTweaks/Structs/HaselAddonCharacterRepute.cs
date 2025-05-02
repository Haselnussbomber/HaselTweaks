using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

[Addon("CharacterRepute")]
[GenerateInterop]
[Inherits<AtkUnitBase>]
[StructLayout(LayoutKind.Explicit, Size = 0x2C0)]
public unsafe partial struct HaselAddonCharacterRepute
{
    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;
    [FieldOffset(0x250)] public AtkComponentDropDownList* ExpansionsDropDownList;
    [FieldOffset(0x2A0), FixedSizeArray] internal FixedSizeArray6<int> _expansionMapping;
    [FieldOffset(0x2B8)] public int SelectedExpansion;
    [FieldOffset(0x2BC)] public int ExpansionsCount;
}
