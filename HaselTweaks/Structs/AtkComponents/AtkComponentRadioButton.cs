namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0xF8)]
public struct AtkComponentRadioButton
{
    [FieldOffset(0x0)] public AtkComponentButton AtkComponentButton;

    public bool IsSelected
    {
        get => AtkComponentButton.IsChecked;
        set => AtkComponentButton.IsChecked = value;
    }
}
