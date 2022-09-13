using System.Runtime.InteropServices;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0x248)]
public unsafe partial struct AddonMJICraftMaterialConfirmation
{
    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;
    [FieldOffset(0x220)] public AtkComponentRadioButton* RadioButton1;
    [FieldOffset(0x228)] public AtkComponentRadioButton* RadioButton2;
    [FieldOffset(0x230)] public AtkComponentRadioButton* RadioButton3;
    [FieldOffset(0x238)] public AtkComponentList* ItemList;
    [FieldOffset(0x240)] public AtkResNode* TextResNode; // contains a AtkTextNode showing "No items in production."

    [MemberFunction("E9 ?? ?? ?? ?? 83 EB 04")]
    public partial void* SwitchTab(uint tabIndex);
}
