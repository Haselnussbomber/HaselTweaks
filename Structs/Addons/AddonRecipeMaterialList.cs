using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 45 33 C0 48 89 03 48 8D 8B ?? ?? ?? ??"
[StructLayout(LayoutKind.Explicit, Size = 0x2A0)]
public unsafe partial struct AddonRecipeMaterialList
{
    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;

    [FieldOffset(0x228)] public AtkComponentButton* RefreshButton;

    [VirtualFunction(2)]
    public partial void ReceiveEvent(AtkEventType eventType, int eventParam, AtkEvent* atkEvent, nint a5);
}
