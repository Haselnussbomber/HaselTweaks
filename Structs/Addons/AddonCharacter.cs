using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B F9 E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 8D 8F ?? ?? ?? ?? 48 89 07 E8 ?? ?? ?? ?? 33 ED"
[StructLayout(LayoutKind.Explicit, Size = 0xF00)]
public unsafe partial struct AddonCharacter
{
    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;

    [FixedSizeArray<Pointer<AtkComponentRadioButton>>(4)]
    [FieldOffset(0x228)] public fixed byte RadioButtons[8 * 4];

    [FieldOffset(0x488)] public int TabIndex;
    [FieldOffset(0x48C)] public int TabCount;

    [FieldOffset(0x4ED)] public bool EmbeddedAddonLoaded;

    [FieldOffset(0xBA8)] public AtkCollisionNode* CharacterPreviewCollisionNode;
    
    [MemberFunction("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 83 C6 EE")]
    public partial void SetTab(int tab);
}
