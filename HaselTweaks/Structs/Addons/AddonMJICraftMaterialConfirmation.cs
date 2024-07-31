using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;

namespace HaselTweaks.Structs;

[GenerateInterop]
[StructLayout(LayoutKind.Explicit, Size = 0x248)]
public unsafe partial struct AddonMJICraftMaterialConfirmation
{
    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;
    [FieldOffset(0x220), FixedSizeArray] internal FixedSizeArray3<Pointer<AtkComponentRadioButton>> _radioButtons;

    [FieldOffset(0x220)] public AtkComponentRadioButton* RadioButton1;
    [FieldOffset(0x228)] public AtkComponentRadioButton* RadioButton2;
    [FieldOffset(0x230)] public AtkComponentRadioButton* RadioButton3;
    [FieldOffset(0x238)] public AtkComponentList* ItemList;
    [FieldOffset(0x240)] public AtkResNode* TextResNode; // contains a AtkTextNode showing "No items in production."
}
