using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 33 D2 48 8D 05 ?? ?? ?? ?? 48 89 03 48 8D 83 ?? ?? ?? ?? 48 89 90"
[VTableAddress("48 8D 05 ?? ?? ?? ?? 48 89 03 48 8D 83 ?? ?? ?? ?? 48 89 90", 3)]
[StructLayout(LayoutKind.Explicit, Size = 0x830)]
public unsafe partial struct AddonCharacterClass
{
    public const int NUM_CLASSES = 31;

    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;
    [FixedSizeArray<Pointer<AtkComponentButton>>(31)]
    [FieldOffset(0x220)] public fixed byte ButtonNodes[0x8 * 31];
    [FieldOffset(0x318)] public AtkComponentButton* TabsNode;
    [FieldOffset(0x320)] public AtkTextNode* CurrentXPTextNode;
    [FieldOffset(0x328)] public AtkTextNode* MaxXPTextNode;
    [FieldOffset(0x330)] public AtkTextNode* CurrentDesynthesisLevelTextNode;
    [FieldOffset(0x338)] public AtkTextNode* MaxDesynthesisLevelTextNode;
    [FixedSizeArray<ClassEntry>(31)]
    [FieldOffset(0x340)] public fixed byte ClassEntries[0x28 * 31];

    [FieldOffset(0x828)] public int TabIndex;

    [MemberFunction("E8 ?? ?? ?? ?? 48 8B 8F ?? ?? ?? ?? 48 8B 01 FF 50 78 48 89 87")]
    public readonly partial void SetTab(int tab);

    [StructLayout(LayoutKind.Explicit, Size = 0x28)]
    public struct ClassEntry
    {
        [FieldOffset(0x00)] public uint Level;
        [FieldOffset(0x04)] public uint CurrentXP;
        [FieldOffset(0x08)] public uint LevelMaxXP;
        [FieldOffset(0x10)] public nint DesynthesisLevel;
        [FieldOffset(0x18)] public nint TooltipText;
        [FieldOffset(0x20)] public bool IsMaxLevel;
    }
}
