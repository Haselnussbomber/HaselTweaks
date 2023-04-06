namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit)]
public partial struct AtkComponentCheckBox
{
    [FieldOffset(0)] public FFXIVClientStructs.FFXIV.Component.GUI.AtkComponentCheckBox Base;

    [MemberFunction("E8 ?? ?? ?? ?? 0F B7 DD")]
    public partial void SetValue(bool isChecked);
}
