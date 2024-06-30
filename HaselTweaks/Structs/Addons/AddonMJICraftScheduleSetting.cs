using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

[GenerateInterop]
[VirtualTable("48 8D 05 ?? ?? ?? ?? 48 89 03 0F 57 C0 48 89 8B ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 0F 11 83", 3)]
[StructLayout(LayoutKind.Explicit, Size = 0x310)]
public unsafe partial struct AddonMJICraftScheduleSetting
{
    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;
    [FieldOffset(0x230)] public AtkComponentTreeList* TreeList;

    [VirtualFunction(2)]
    public partial void ReceiveEvent(AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData);
}
