using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0xF0)]
public unsafe partial struct AtkComponentButton
{
    [FieldOffset(0x0)] public AtkComponentBase AtkComponentBase;

    // based on the text size
    [FieldOffset(0xC0)] public short Left;
    [FieldOffset(0xC2)] public short Top;
    [FieldOffset(0xC4)] public short Right;
    [FieldOffset(0xC6)] public short Bottom;
    [FieldOffset(0xC8)] public AtkTextNode* ButtonTextNode;
    [FieldOffset(0xD0)] public AtkResNode* ButtonBGNode;
    [FieldOffset(0xE8)] public uint Flags;

    public bool IsEnabled => AtkComponentBase.OwnerNode->AtkResNode.NodeFlags.HasFlag(NodeFlags.Enabled);

    /// <remarks> Used by AtkComponentCheckBox and AtkComponentRadioButton. </remarks>
    public bool IsChecked
    {
        get => (Flags & (1 << 18)) != 0;
        set => SetChecked(value);
    }

    /// <remarks> Used by AtkComponentCheckBox and AtkComponentRadioButton. </remarks>
    [MemberFunction("E8 ?? ?? ?? ?? 0F B7 DD")]
    public partial void SetChecked(bool isChecked);
}
