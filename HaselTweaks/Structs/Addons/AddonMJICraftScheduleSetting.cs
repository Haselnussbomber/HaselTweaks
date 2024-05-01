using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 8D 8B ?? ?? ?? ?? 48 89 03 E8 ?? ?? ?? ?? 80 8B ?? ?? ?? ?? ?? B8 ?? ?? ?? ?? 80 8B ?? ?? ?? ?? ?? 81 8B"
[StructLayout(LayoutKind.Explicit, Size = 0x310)]
public unsafe partial struct AddonMJICraftScheduleSetting
{
    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;

    [FieldOffset(0x220)] public AtkComponentTreeList* TreeList;

    [MemberFunction("48 8B C4 48 89 78 18")]
    public readonly partial void ReceiveEvent(AtkEventType eventType, int eventParam, AtkEvent* atkEvent, nint a5);
}
