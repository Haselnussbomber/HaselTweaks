using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

[GenerateInterop]
[VirtualTable("48 8D 05 ?? ?? ?? ?? 48 8D 8B ?? ?? ?? ?? 48 89 03 E8 ?? ?? ?? ?? 33 FF 48 89 BB", 3)]
[StructLayout(LayoutKind.Explicit, Size = 0xC60)]
public unsafe partial struct AddonPvPCharacter
{
    public const int NUM_CLASSES = 19;

    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;

    [FieldOffset(0x240), FixedSizeArray] internal FixedSizeArray19<ClassEntry> _classEntries;

    [StructLayout(LayoutKind.Explicit, Size = 0x28)]
    public struct ClassEntry
    {
        [FieldOffset(0x00)] public AtkComponentBase* Base;
        [FieldOffset(0x08)] public AtkTextNode* Name;
        [FieldOffset(0x10)] public AtkTextNode* Level;
        [FieldOffset(0x18)] public AtkImageNode* Icon;
        [FieldOffset(0x20)] public AtkImageNode* UnkImage;
    }

    [VirtualFunction(2)]
    public partial void ReceiveEvent(AtkEventType eventType, int eventParam, AtkEvent* atkEvent, nint atkEventData = 0);

    [MemberFunction("E8 ?? ?? ?? ?? 4C 8B B7 ?? ?? ?? ?? 49 8B 46 20")]
    public partial void UpdateClasses(NumberArrayData** numberArrayData, StringArrayData** stringArrayData);
}
