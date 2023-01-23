using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 89 03 33 C0 89 83 ?? ?? ?? ?? 48 8B C3"
[StructLayout(LayoutKind.Explicit, Size = 0xF8)]
public partial struct AtkComponentRadioButton
{
    [FieldOffset(0)] public AtkComponentButton AtkComponentButton;

    public bool IsSelected => (AtkComponentButton.Flags & 0x40000) != 0;

    [MemberFunction("E8 ?? ?? ?? ?? EB 82")]
    public partial void SetSelected(bool isSelected);
}
