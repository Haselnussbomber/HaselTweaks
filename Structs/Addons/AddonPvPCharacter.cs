using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "48 89 5C 24 ?? 57 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 8D 8B ?? ?? ?? ?? 48 89 03 E8 ?? ?? ?? ?? BA"
[StructLayout(LayoutKind.Explicit, Size = 0xC60)]
public unsafe partial struct AddonPvPCharacter
{
    public const int NUM_CLASSES = 19;

    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;
    [FixedSizeArray<ClassEntry>(19)]
    [FieldOffset(0x240)] public fixed byte ClassEntries[0x28 * 19];

    [StructLayout(LayoutKind.Explicit, Size = 0x28)]
    public struct ClassEntry
    {
        [FieldOffset(0x00)] public AtkComponentBase* Base;
        [FieldOffset(0x08)] public AtkTextNode* Name;
        [FieldOffset(0x10)] public AtkTextNode* Level;
        [FieldOffset(0x18)] public AtkImageNode* Icon;
        [FieldOffset(0x20)] public AtkImageNode* UnkImage;
    }
}
