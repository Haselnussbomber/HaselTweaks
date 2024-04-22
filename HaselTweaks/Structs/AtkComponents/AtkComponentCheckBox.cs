namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0x110)]
public struct AtkComponentCheckBox
{
    [FieldOffset(0x0)] public AtkComponentButton AtkComponentButton;

    public bool IsChecked
    {
        get => AtkComponentButton.IsChecked;
        set => AtkComponentButton.IsChecked = value;
    }
}
