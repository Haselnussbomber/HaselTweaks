using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 80 8B ?? ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 80 8B ?? ?? ?? ?? ?? B9"
[StructLayout(LayoutKind.Explicit, Size = 0x2D8)]
public unsafe partial struct AddonMYCWarResultNotebook
{
    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;

    [FieldOffset(0x240)] public AtkCollisionNode* DescriptionCollisionNode;

    [FieldOffset(0x254)] public int MaxNoteIndex;
    [FieldOffset(0x258)] public int CurrentNoteIndex;
    [FieldOffset(0x25C)] public int CurrentPageIndex;

    [VirtualFunction(2)]
    public readonly partial void ReceiveEvent(AtkEventType eventType, int eventParam, AtkEvent* atkEvent, nint a5);
}
