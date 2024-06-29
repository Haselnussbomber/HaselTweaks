using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

[GenerateInterop]
[VirtualTable("48 8D 05 ?? ?? ?? ?? 48 89 03 48 8D 8B ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 89 BB ?? ?? ?? ?? 48 89 83 ?? ?? ?? ?? 48 89 BB ?? ?? ?? ??", 3)]
[StructLayout(LayoutKind.Explicit, Size = 0x5C0)]
public unsafe partial struct AddonTeleportTown
{
    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;

    [FieldOffset(0x230)] public AtkComponentTreeList* List;

    [VirtualFunction(2)]
    public partial void ReceiveEvent(AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData = null);
}
