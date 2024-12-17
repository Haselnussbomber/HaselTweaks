using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;

namespace HaselTweaks.Structs;

[Addon("Currency")]
[GenerateInterop]
[StructLayout(LayoutKind.Explicit, Size = 0x360)]
public unsafe partial struct AddonCurrency
{
    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;
    [FieldOffset(0x2B0), FixedSizeArray] internal FixedSizeArray5<Pointer<AtkComponentRadioButton>> _tabs;
}
