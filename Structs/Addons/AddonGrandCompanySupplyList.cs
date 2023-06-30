using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 80 8B ?? ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 81 8B ?? ?? ?? ?? ?? ?? ?? ?? 48 8D 8B ?? ?? ?? ?? 48 89 03 33 D2 33 C0"
[StructLayout(LayoutKind.Explicit, Size = 0x2F8)]
public unsafe partial struct AddonGrandCompanySupplyList
{
    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;

    [VirtualFunction(2)]
    public readonly partial void ReceiveEvent(AtkEventType eventType, int eventParam, AtkEvent* atkEvent, nint a5);
}
