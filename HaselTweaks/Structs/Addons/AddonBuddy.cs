using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;

namespace HaselTweaks.Structs;

// temporary struct until https://github.com/aers/FFXIVClientStructs/pull/1083 is merged
[GenerateInterop]
[StructLayout(LayoutKind.Explicit, Size = 0x1E80)]
public unsafe partial struct AddonBuddy
{
    [FieldOffset(0x230)] public int TabIndex;

    [FieldOffset(0x238)] public AtkAddonControl AddonControl;

    [FieldOffset(0x1E58), FixedSizeArray] internal FixedSizeArray3<Pointer<AtkComponentRadioButton>> _radioButtons;

    [MemberFunction("E8 ?? ?? ?? ?? 3B AF ?? ?? ?? ?? 74 27")]
    public partial void SetTab(int tab);
}
