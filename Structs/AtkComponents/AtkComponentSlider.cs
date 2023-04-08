namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit)]
public partial struct AtkComponentSlider
{
    [FieldOffset(0)] public FFXIVClientStructs.FFXIV.Component.GUI.AtkComponentSlider Base;

    [MemberFunction("E8 ?? ?? ?? ?? 0F B6 43 3C")]
    public partial void SetValue(int value, bool triggerEvent = false);
}
