using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;

namespace HaselTweaks.Structs;

// ctor "40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 41 B0 01 48 89 03 BA ?? ?? ?? ?? 33 C0 48 8B CB 48 89 83 ?? ?? ?? ?? 48 89 83 ?? ?? ?? ?? 48 89 83 ?? ?? ?? ?? 48 89 83 ?? ?? ?? ?? 48 89 83 ?? ?? ?? ?? 48 89 83 ?? ?? ?? ?? 48 89 83 ?? ?? ?? ?? 48 89 83 ?? ?? ?? ?? B8"
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

    // [MemberFunction("E9 ?? ?? ?? ?? 83 EB 04")]
    // public partial void* SwitchTab(uint tabIndex); // note: this fires events which result in network communication
}
