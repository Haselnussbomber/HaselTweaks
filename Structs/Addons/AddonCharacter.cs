using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B F9 E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 8D 8F ?? ?? ?? ?? 48 89 07 E8 ?? ?? ?? ?? 33 ED"
[StructLayout(LayoutKind.Explicit, Size = 0xF00)]
public unsafe struct AddonCharacter
{
    public const uint NUM_TABS = 4;

    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;

    [FieldOffset(0x488)] public int TabIndex;
}
